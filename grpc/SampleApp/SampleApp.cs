/**
 * Copyright 2018-2021 MobiledgeX, Inc. All rights and licenses reserved.
 * MobiledgeX, Inc. 156 2nd Street #408, San Francisco, CA 94105
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using Grpc.Core;
using System.Net;
using System.Diagnostics;

// MobiledgeX Matching Engine API.
using DistributedMatchEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

using static DistributedMatchEngine.ServerEdgeEvent.Types;
using DistributedMatchEngine.PerformanceMetrics;
using System.Threading;
using System.Net.Sockets;


// TODO: There is no Abstraction API wrapper. Replicate from REST, but return
// the GRPC objects instead.
namespace MexGrpcSampleConsoleApp
{
  class Program
  {
    // Sample Note: Use async Task main, or Task.Result to allow threads to finish
    // in RunSampleFlow().
    static async Task Main(string[] args)
    {
      Console.WriteLine("Hello MobiledgeX GRPC Library Sample App!");


      var mexGrpcLibApp = new MexGrpcLibApp();
      try
      {
        await mexGrpcLibApp.RunSampleFlow();
        Console.WriteLine("Sleeping for some time to receive some info events from the server.");
        await Task.Delay(120 * 1000);
      }
      catch (AggregateException ae)
      {
        Console.Error.WriteLine("Exception running sample: " + ae.Message);
        Console.Error.WriteLine("Excetpion stack trace: " + ae.StackTrace);
      }
      catch (Exception e)
      {
        Console.Error.WriteLine("Exception running sample: " + e.Message);
        Console.Error.WriteLine("Excetpion stack trace: " + e.StackTrace);
      }
    }
  }

  public class TokenException : Exception
  {
    public TokenException(string message)
        : base(message)
    {
    }

    public TokenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
  }

  class DummyCarrierInfo : CarrierInfo
  {
    public ulong GetCellID()
    {
      return 0;
    }

    public string GetCurrentCarrierName()
    {
      return "";
    }

    public string GetMccMnc()
    {
      return "";
    }
  }

  // This interface is optional but is used in the sample.
  class DummyUniqueID : UniqueID
  {
    string UniqueID.GetUniqueIDType()
    {
      return "dummyModel";
    }

    string UniqueID.GetUniqueID()
    {
      return "abcdef0123456789";
    }
  }

  class DummyDeviceInfo : DeviceInfoApp
  {

    public DeviceDynamicInfo GetDeviceDynamicInfo()
    {
      DeviceDynamicInfo DeviceDynamicInfo = new DeviceDynamicInfo()
      {
        CarrierName = "TDG",
        DataNetworkType = "GSM",
        SignalStrength = 0
      };
      return DeviceDynamicInfo;
    }

    public DeviceStaticInfo GetDeviceStaticInfo()
    {
      DeviceStaticInfo DeviceStaticInfo = new DeviceStaticInfo()
      {
        DeviceModel = "iPhone",
        DeviceOs = "iOS 14.2"
      };
      return DeviceStaticInfo;
    }

    public bool IsPingSupported()
    {
      return true;
    }
  }

  class MexGrpcLibApp
  {
    Loc location;
    string sessionCookie;

    string dmeHost = "eu-qa.dme.mobiledgex.net"; // demo DME server hostname or ip.
    uint dmePort = 50051; // DME port.
    string carrierName = "";
    string orgName = "automation_dev_org";
    string appName = "automation-sdk-porttest";
    string appVers = "1.0";

    MatchingEngine me;

    Loc sanfran = new Loc
    {
      Latitude = 37.7749,
      Longitude = -122.4194,
    };
    Loc paloalto = new Loc
    {
      Latitude = 37.4419,
      Longitude = -122.1430,
    };
    Loc anchorage = new Loc
    {
      Latitude = 61.2181,
      Longitude = -149.9003,
    };
    Loc austin = new Loc
    {
      Latitude = 30.2672,
      Longitude = -97.7431,
    };

    Loc[] locs;

    MatchEngineApi.MatchEngineApiClient streamClient;

    RegisterClientRequest CreateRegisterClientRequest(string carrierName, string orgName, string appName, string appVersion, string authToken)
    {
      var request = new RegisterClientRequest
      {
        Ver = 1,
        CarrierName = carrierName,
        OrgName = orgName,
        AppName = appName,
        AppVers = appVersion,
        AuthToken = authToken
      };
      return request;
    }
    FindCloudletRequest CreateFindCloudletRequest(string carrierName, Loc gpsLocation)
    {
      var request = new FindCloudletRequest
      {
        Ver = 1,
        SessionCookie = sessionCookie,
        CarrierName = carrierName,
        GpsLocation = gpsLocation
      };
      return request;
    }

    static String parseToken(String uri)
    {
      string[] uriandparams = uri.Split('?');
      if (uriandparams.Length < 1)
      {
        return null;
      }
      string parameterStr = uriandparams[1];
      if (parameterStr.Equals(""))
      {
        return null;
      }

      string[] parameters = parameterStr.Split('&');
      if (parameters.Length < 1)
      {
        return null;
      }

      foreach (string keyValueStr in parameters)
      {
        string[] keyValue = keyValueStr.Split('=');
        if (keyValue[0].Equals("dt-id"))
        {
          string value = null;
          int pos = keyValue[0].Length + 1;
          if (pos < keyValueStr.Length)
          {
            value = keyValueStr.Substring(pos, keyValueStr.Length - pos);
          }
          return value;
        }
      }

      return null;
    }

    string RetrieveToken(string tokenServerURI)
    {
      HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(tokenServerURI);
      httpWebRequest.AllowAutoRedirect = false;

      HttpWebResponse response = null;
      string token = null;
      string uriLocation = null;
      // 303 See Other is behavior is different between standard C#
      // and what's potentially in Unity.
      try
      {
        response = (HttpWebResponse)httpWebRequest.GetResponse();
        if (response != null)
        {
          if (response.StatusCode != HttpStatusCode.SeeOther)
          {
            throw new TokenException("Expected an HTTP 303 SeeOther.");
          }
          uriLocation = response.Headers["Location"];
        }
      }
      catch (System.Net.WebException we)
      {
        response = (HttpWebResponse)we.Response;
        if (response != null)
        {
          if (response.StatusCode != HttpStatusCode.SeeOther)
          {
            throw new TokenException("Expected an HTTP 303 SeeOther.", we);
          }
          uriLocation = response.Headers["Location"];
        }
      }

      if (uriLocation != null)
      {
        token = parseToken(uriLocation);
      }
      return token;
    }
    VerifyLocationRequest CreateVerifyLocationRequest(string carrierName, Loc gpsLocation, string verifyLocationToken)
    {
      var request = new VerifyLocationRequest
      {
        Ver = 1,
        SessionCookie = sessionCookie,
        CarrierName = carrierName,
        GpsLocation = gpsLocation,
        VerifyLocToken = verifyLocationToken
      };
      return request;
    }
    public async Task RunSampleFlowA()
    {
      try
      {
        me = new MatchingEngine(
          netInterface: new SimpleNetInterface(new MacNetworkInterfaceName()),
          carrierInfo: new DummyCarrierInfo(),
          deviceInfo: new DummyDeviceInfo(),
          uniqueID: new DummyUniqueID());
        location = GetLocation();
        locs = new Loc[] { sanfran, paloalto, anchorage, austin };
        string uri = dmeHost + ":" + dmePort;
        Console.WriteLine("url is " + uri);

        // Channel:
        // ChannelCredentials channelCredentials = new SslCredentials();
        ChannelCredentials channelCredentials = ChannelCredentials.Insecure;
        Channel channel = new Channel(dmeHost, (int)dmePort, channelCredentials);

        streamClient = new MatchEngineApi.MatchEngineApiClient(channel);

        RegisterClientReply regReply = null;
        for (int i = 0; i < 1; i++)
        {
          var registerClientRequest = CreateRegisterClientRequest("", orgName, appName, appVers, "");
          regReply = streamClient.RegisterClient(registerClientRequest);
          Console.WriteLine("RegisterClient Reply Status: " + regReply.Status);
          Console.WriteLine("RegisterClient TokenServerURI: " + regReply.TokenServerUri);
          sessionCookie = regReply.SessionCookie;
          Thread.Sleep(1000);
        }

        var eeSessionCookie = "";
        for (int i = 0; i < 1; i++)
        {
          var findCloudletRequest = CreateFindCloudletRequest("", location);
          var findCloudletReply = streamClient.FindCloudlet(findCloudletRequest);
          Console.WriteLine("FindCloudlet Reply Status: " + findCloudletReply.Status);
          Console.WriteLine("FindCloudlet edge events session cookie: " + findCloudletReply.EdgeEventsCookie);
          Console.WriteLine("FindCloudlet fqdn: " + findCloudletReply.Fqdn);
          eeSessionCookie = findCloudletReply.EdgeEventsCookie;
          Thread.Sleep(1000);
        }

        string token = null;
        try
        {
          token = RetrieveToken(regReply.TokenServerUri);
          Console.WriteLine("VerifyLocation pre-query TokenServer token: " + token);
        }
        catch (System.Net.WebException we)
        {
          Console.WriteLine(we.ToString());
        }
        if (token == null)
        {
          Console.WriteLine("No token");
          return;
        }
        var verifyLocRequest = CreateVerifyLocationRequest("", location, token);
        //var verifyLocationReply = streamClient.VerifyLocation(verifyLocRequest);
        //Console.WriteLine("VerifyLocation status: " + verifyLocationReply.GpsLocationStatus);

        var clientEdgeEvent = new ClientEdgeEvent
        {
          EventType = ClientEdgeEvent.Types.ClientEventType.EventInitConnection,
          SessionCookie = sessionCookie,
          EdgeEventsCookie = eeSessionCookie
        };
        var edgeEvent = streamClient.StreamEdgeEvent();
        Console.WriteLine("Initiating a persistent connection with DME");
        await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent);
        Console.WriteLine("Listening for events");

        var readTask = Task.Run(async () =>
        {
          while (await edgeEvent.ResponseStream.MoveNext())
          {
            switch (edgeEvent.ResponseStream.Current.EventType)
            {
              case ServerEventType.EventInitConnection:
                Console.WriteLine("Successfully initiated persistent edge event connection");
                continue;
              case ServerEventType.EventLatencyRequest:
                // Console.WriteLine("Latency requested. Measuring latency \n");

                // FIXME: This needs to be updated once a more official server lands.
                IPAddress remoteIP = Dns.GetHostAddresses("127.0.0.1")[0];
                IPEndPoint remoteEndPoint = new IPEndPoint(remoteIP, (int)dmePort);
                // calculate min, max, and avg
                float min = 0;
                float max = 0;
                float sum = 0;
                int numTests = 5;
                var times = new List<Sample>();
                // first tcp ping loads network (don't count towards latencies)
                for (int i = 0; i <= numTests; i++)
                {
                  var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                  sock.Blocking = true;

                  var stopwatch = new Stopwatch();

                  // Measure the Connect call only
                  stopwatch.Start();
                  sock.Connect(remoteEndPoint);
                  stopwatch.Stop();

                  float t = (float)stopwatch.Elapsed.TotalMilliseconds;

                  if (i > 0)
                  {
                    if (t < min || min == 0)
                    {
                      min = t;
                    }
                    if (t > max || max == 0)
                    {
                      max = t;
                    }
                    sum += t;
                    var ts = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    var seconds = ts / 1000;
                    var nanos = (ts - (seconds * 1000)) * 1000000;
                    var sample = new Sample
                    {
                      Value = t,
                      Timestamp = new Timestamp
                      {
                        Seconds = seconds,
                        Nanos = (int)nanos,
                      },
                    };
                    times.Add(sample);
                  }

                  sock.Close();
                }
                float avg = sum / numTests;

                // calculate std dev
                float squaredDiffs = 0;
                foreach (Sample time in times)
                {
                  Console.WriteLine(time.Value + " ");
                  float diff = (float)time.Value - avg;
                  squaredDiffs += diff * diff;
                }
                Console.WriteLine("\n");
                float stdDev = (float)Math.Sqrt(squaredDiffs / numTests);
                Statistics latency = new Statistics()
                {
                  Min = min,
                  Max = max,
                  Avg = avg,
                  StdDev = stdDev
                };
                var latencyEdgeEvent = new ClientEdgeEvent
                {
                  EventType = ClientEdgeEvent.Types.ClientEventType.EventLatencySamples,
                  GpsLocation = location
                };
                await edgeEvent.RequestStream.WriteAsync(latencyEdgeEvent);
                continue;
              case ServerEdgeEvent.Types.ServerEventType.EventLatencyProcessed:
                var l = edgeEvent.ResponseStream.Current.Statistics;
                Console.WriteLine("Latency results: \n" +
                "        Latency: " + "Avg: " + l.Avg + ", Min: " + l.Min + ", Max: " + l.Max + ", StdDev: " + l.StdDev + "\n");
                continue;
              case ServerEdgeEvent.Types.ServerEventType.EventCloudletUpdate:
                var newFindCloudletReply = edgeEvent.ResponseStream.Current.NewCloudlet;
                Console.WriteLine("FindCloudlet Reply Status: " + newFindCloudletReply.Status);
                Console.WriteLine("FindCloudlet fqdn: " + newFindCloudletReply.Fqdn);
                continue;
              default:
                Console.WriteLine("EdgeServerEvent: \n" +
                "        AppInstHealth " + edgeEvent.ResponseStream.Current.HealthCheck + "\n");
                continue;
            }
          }
        });


        for (int i = 0; i < 1000; i++)
        {
          if (i % 2 == 0)
          {
            location = locs[i % 3];
            var clientEdgeEvent2 = new ClientEdgeEvent
            {
              GpsLocation = location
            };
            await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent2);
            Console.WriteLine("Sent location edge event");
          }
          else
          {
            var dummySamples = new List<Sample>();
            var sample1 = new Sample() { Value = 1.1 };
            var sample2 = new Sample() { Value = 3.2 };
            dummySamples.Add(sample1);
            dummySamples.Add(sample2);
            //var clientEdgeEvent2 = CreateClientEdgeEvent(locs[i % 3], eventType: ClientEdgeEvent.Types.ClientEventType.EventCustomEvent, samples: dummySamples, customEventName: "testBlah");
            var clientEdgeEvent2 = new ClientEdgeEvent
            {
              EventType = ClientEdgeEvent.Types.ClientEventType.EventCustomEvent,
            };
            string customEventName = "testBlah";
            clientEdgeEvent2.CustomEvent = customEventName;
            foreach (var sample in dummySamples)
            {
              clientEdgeEvent2.Samples.Add(sample);
            }
            await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent2);
            Console.WriteLine("Sent custom edge event");
          }

          Thread.Sleep(10000);
        }
      }
      catch (Exception e)
      {
        Console.Error.WriteLine("Error: " + e.Message);
        Console.Error.WriteLine("Stack: " + e.StackTrace);
      }
    }

    public async Task RunSampleFlow()
    {
      me = new MatchingEngine(
        netInterface: new SimpleNetInterface(new MacNetworkInterfaceName()),
        carrierInfo: new DummyCarrierInfo(),
        deviceInfo: new DummyDeviceInfo(),
        uniqueID: new DummyUniqueID());
      me.useOnlyWifi = true;
      me.useSSL = true; // false --> Local testing only.
      location = GetLocation();
      string uri = dmeHost + ":" + dmePort;

      var registerClientRequest = me.CreateRegisterClientRequest(orgName, appName, appVers);
      var regReply = await me.RegisterClient(host: dmeHost, port: dmePort, registerClientRequest);

      Console.WriteLine("RegisterClient Reply Status: " + regReply.Status);

      // Store sessionCookie, for later use in future requests.
      sessionCookie = regReply.SessionCookie;
      Console.WriteLine("Session Cookie: " + regReply.SessionCookie);


      // Call the remainder. Verify and Find cloudlet.

      // Async version can also be used. Blocking usage:
      var verifyLocationRequest = me.CreateVerifyLocationRequest(location);
      var verifyLocationReply = await me.VerifyLocation(host: dmeHost, port: dmePort, verifyLocationRequest);
      Console.WriteLine("VerifyLocation Status: " + verifyLocationReply.GpsLocationStatus);
      Console.WriteLine("VerifyLocation Accuracy: " + verifyLocationReply.GpsLocationAccuracyKm);

      // Attach an Edge EventBus Receiver (This is not a raw delegate).
      me.EdgeEventsReceiver += async (ServerEdgeEvent serverEdgeEvent) =>
      {
        Console.WriteLine("Got Event: ," + "EventType: " + serverEdgeEvent.EventType + ", Messsage: " + serverEdgeEvent);
        await HandleEdgeEvent(serverEdgeEvent).ConfigureAwait(false);

        // Fling a new location for response:
        if (serverEdgeEvent.EventType == ServerEdgeEvent.Types.ServerEventType.EventInitConnection)
        {
          Loc loc = new Loc { Longitude = -73.935242, Latitude = 40.730610 }; // New York City
          if (me.EdgeEventsConnection != null)
          {
            await me.EdgeEventsConnection.PostLocationUpdate(loc);
          }
        }
      };

      // Blocking GRPC call:
      var fcRequest = me.CreateFindCloudletRequest(location);
      var findCloudletReply = await me.FindCloudlet(host: dmeHost, port: dmePort, fcRequest, mode: FindCloudletMode.PERFORMANCE);
      Console.WriteLine("FindCloudlet Status: " + findCloudletReply.Status);
      // appinst server end port might not exist:
      foreach (AppPort p in findCloudletReply.Ports)
      {
        Console.WriteLine("Port: fqdn_prefix: " + p.FqdnPrefix +
                  ", protocol: " + p.Proto +
                  ", PublicPort: " + p.PublicPort +
                  ", InternalPort: " + p.InternalPort +
                  ", EndPort: " + p.EndPort);
      }
      // Straight reflection print:
      Console.WriteLine("FindCloudlet Reply: " + findCloudletReply);

      // Clean:
      me.Dispose();
      return;
    }

    // TODO: The client must retrieve a real GPS location from the platform, even if it is just the last known location,
    // possibly asynchronously.
    // TEST ONLY!
    Loc GetLocation()
    {
      return new Loc
      {
        Longitude = -122.149349,
        Latitude = 37.459609
      };
    }

    // TEST ONLY!
    static bool locationToggle = false;
    Loc GetToggledLocation()
    {
      if (locationToggle = !locationToggle)
      {
        return new Loc
        {
          Longitude = -122.149349,
          Latitude = 37.459609
        };
      }
      else
      {
        return new Loc
        {
          Longitude = -73.935242,
          Latitude = 40.730610
        };
      }
    }

    // Event Handlers:
    async Task HandleEdgeEvent(ServerEdgeEvent serverEdgeEvent)
    {
      switch (serverEdgeEvent.EventType)
      {
        case ServerEventType.EventInitConnection:
          {
            Console.WriteLine("EventInitConnection: " + serverEdgeEvent);
          }
          break;
        case ServerEventType.EventLatencyRequest:
          {
            await HandleLatencyRequest(serverEdgeEvent).ConfigureAwait(false); // Let UI run.
          }
          break;
        case ServerEventType.EventLatencyProcessed:
          {
            Console.WriteLine("Server has processed sent Latency Request: " + serverEdgeEvent);
          }
          break;
        case ServerEventType.EventCloudletUpdate:
          {
            var newFindCloudletReply = serverEdgeEvent.NewCloudlet;
            Console.WriteLine("FindCloudlet Reply Status: " + newFindCloudletReply.Status);
            Console.WriteLine("FindCloudlet fqdn: " + newFindCloudletReply.Fqdn);
          }
          break;
        case ServerEventType.EventAppinstHealth:
          {
            Console.Error.WriteLine("Unhandled ServerEdgeEvent: " + serverEdgeEvent);
          }
          break;
        case ServerEventType.EventCloudletMaintenance:
          {
            Console.Error.WriteLine("Unhandled ServerEdgeEvent: " + serverEdgeEvent);
          }
          break;
        case ServerEventType.EventCloudletState:
          {
            Console.Error.WriteLine("Unhandled ServerEdgeEvent: " + serverEdgeEvent);
          }
          break;
         case ServerEventType.EventError:
             {
              Console.Error.WriteLine("Unhandled ServerEdgeEvent: " + serverEdgeEvent);
             }
             break;
         case ServerEventType.EventUnknown:
          {
            Console.Error.WriteLine("Unhandled ServerEdgeEvent: " + serverEdgeEvent);
          }
          break;
        default:
          {
            Console.Error.WriteLine("Unhandled ServerEdgeEvent: " + serverEdgeEvent);
          }
          break;
      }
      return;
    }

    // SubMessage handlers:
    async Task HandleLatencyRequest(ServerEdgeEvent serverEdgeEvent)
    {
      Console.WriteLine("EventLatencyRequest" + serverEdgeEvent);
      // To configured DME.

      NetTest latencyTester = new NetTest(me);

      latencyTester.sites.Enqueue(new NetTest.Site
      {
        host = dmeHost,
        port = (int)dmePort,
        testType = NetTest.TestType.PING
      });

      await latencyTester.RunNetTest(5);

      foreach (var site in latencyTester.sites)
      {
        await me.EdgeEventsConnection.PostLatencyUpdate(site, GetToggledLocation());
      }

      // Double post:
      await me.EdgeEventsConnection.TestPingAndPostLatencyUpdate(dmeHost, GetToggledLocation());

      // Test another:
      await me.EdgeEventsConnection.TestConnectAndPostLatencyUpdate(dmeHost, dmePort, GetToggledLocation());
      return;
    }
  }
}
