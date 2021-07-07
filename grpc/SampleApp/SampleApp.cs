/**
 * Copyright 2018-2020 MobiledgeX, Inc. All rights and licenses reserved.
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
using System.Threading.Tasks;

// MobiledgeX Matching Engine API.
using DistributedMatchEngine;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

namespace MexGrpcSampleConsoleApp
{
  class Program
  {
    static async Task Main(string[] args)
    {
      Console.WriteLine("Hello MobiledgeX GRPC Library Sample App!");


      var mexGrpcLibApp = new MexGrpcLibApp();
      await mexGrpcLibApp.RunSampleFlowAsync();
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

  class MexGrpcLibApp
  {
    Loc location;
    string sessionCookie;
    string eeSessionCookie;

    /*string dmeHost = "127.0.0.1"; // demo DME server hostname or ip.
    int dmePort = 50051; // DME port.
    string carrierName = "";
    string orgName = "mobiledgex";
    string appName = "arshooter";
    string appVers = "1";*/

    string dmeHost = "us-qa.dme.mobiledgex.net"; // demo DME server hostname or ip.
    int dmePort = 50051; // DME port.
    string carrierName = "";
    string orgName = "testmonitor";
    string appName = "app-us-k8s";
    string appVers = "v1";

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

    string[] carriers = new string[]{ "310170", "26201", "26207", "45003", "310260", "11111"}; // ATT, GDDT, Sonoral, SKT, TELUS, Unknown

    string[] dataNetTypes = new string[]{ "Wifi", "LTE", "3G", "5G"};

    string[] deviceOs = new string[]{ "Android", "iOS"};

    string[] deviceModels = new string[]{"Platos", "Apple", "Nokia"};

    int iteration = 0;

    MatchEngineApi.MatchEngineApiClient client;

    public async Task RunSampleFlowAsync()
    {
      try
      {
        location = getLocation();
        locs = new Loc[]{ sanfran, paloalto, anchorage, austin };
        string uri = dmeHost + ":" + dmePort;
        Console.WriteLine("url is " + uri);

        // Channel:
        ChannelCredentials channelCredentials = new SslCredentials();
        //ChannelCredentials channelCredentials = ChannelCredentials.Insecure;
        Channel channel = new Channel(uri, channelCredentials);

        client = new MatchEngineApi.MatchEngineApiClient(channel);

        RegisterClientReply regReply = null;
        for (int i = 0; i < 1; i++)
        {
          try {
            Thread.Sleep(1000);
            var registerClientRequest = CreateRegisterClientRequest(getCarrierName(), orgName, appName, appVers, "");
            regReply = client.RegisterClient(registerClientRequest);
            Console.WriteLine("RegisterClient Reply Status: " + regReply.Status);
            Console.WriteLine("RegisterClient TokenServerURI: " + regReply.TokenServerUri);
            sessionCookie = regReply.SessionCookie;
            //Thread.Sleep(1000);
          }
          catch (Exception e)
          {
            Console.WriteLine("RegisterClient Exception is " + e.Message);
          }
        }

        for (int i = 0; i < 1; i++)
        { 
        var findCloudletRequest = CreateFindCloudletRequest(getCarrierName(), location);
        var findCloudletReply = client.FindCloudlet(findCloudletRequest);
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
        var verifyLocRequest = CreateVerifyLocationRequest(getCarrierName(), location, token);
        var verifyLocationReply = client.VerifyLocation(verifyLocRequest);
        Console.WriteLine("VerifyLocation status: " + verifyLocationReply.GpsLocationStatus);

        var clientEdgeEvent1 = CreateClientEdgeEvent(location, ClientEdgeEvent.Types.ClientEventType.EventInitConnection);
        var edgeEvent = client.StreamEdgeEvent();
        Console.WriteLine("Initiating a persistent connection with DME");
        await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent1);
        Console.WriteLine("Listening for events");

        var times = new List<Sample>();
                var sample1 = new Sample
                {
                  Value = -1,
                };
                times.Add(sample1);
                var sample2 = new Sample
                {
                  Value = 0,
                };
                times.Add(sample2);
                var sample3 = new Sample
                {
                  Value = 2,
                };
                times.Add(sample3);
                var sample4 = new Sample
                {
                  Value = -3,
                };
                times.Add(sample4);
                var sample5 = new Sample
                {
                  Value = 5,
                };
                times.Add(sample5);

        var readTask = Task.Run(async () =>
        {
          while (await edgeEvent.ResponseStream.MoveNext())
          {
            switch (edgeEvent.ResponseStream.Current.EventType)
            {
              case ServerEdgeEvent.Types.ServerEventType.EventInitConnection:
                Console.WriteLine("Successfully initiated persistent edge event connection");
                continue;
              case ServerEdgeEvent.Types.ServerEventType.EventLatencyRequest:
                Console.WriteLine("Latency requested. Measuring latency \n");

                /*IPAddress remoteIP = Dns.GetHostAddresses("127.0.0.1")[0];
                IPEndPoint remoteEndPoint = new IPEndPoint(remoteIP, dmePort);
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

                  float t =(float)stopwatch.Elapsed.TotalMilliseconds;

                  if (i > 0) {
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
                      Timestamp = new Timestamp{
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
                Statistics latency = new Statistics() {
                  Min = min,
                  Max = max,
                  Avg = avg,
                  StdDev = stdDev
                };*/
                
                
                var latencyEdgeEvent = CreateClientEdgeEvent(location, eventType: ClientEdgeEvent.Types.ClientEventType.EventLatencySamples, samples: times);
                await edgeEvent.RequestStream.WriteAsync(latencyEdgeEvent);
                continue;
              case ServerEdgeEvent.Types.ServerEventType.EventLatencyProcessed:
                var l = edgeEvent.ResponseStream.Current.Statistics;
                Console.WriteLine("Latency results: \n" +
                "        Latency: " + "Avg: " + l.Avg + ", Min: " + l.Min + ", Max: " + l.Max + ", StdDev: " + l.StdDev + ", Variance: " + l.Variance + ", NumSamples: " + l.NumSamples + ", Timestamp" + l.Timestamp + "\n");
                continue;
              case ServerEdgeEvent.Types.ServerEventType.EventCloudletUpdate:
                var newFindCloudletReply = edgeEvent.ResponseStream.Current.NewCloudlet;
                Console.WriteLine("FindCloudlet Reply Status: " + newFindCloudletReply.Status);
                Console.WriteLine("FindCloudlet fqdn: " + newFindCloudletReply.Fqdn);
                continue;
              case ServerEdgeEvent.Types.ServerEventType.EventError:
                Console.WriteLine("EVENT_ERROR: error is " + edgeEvent.ResponseStream.Current.ErrorMsg);
                continue;
              default:
                Console.WriteLine("EdgeServerEvent: " + edgeEvent.ResponseStream.Current.EventType + "\n" +
                "        AppInstHealth " + edgeEvent.ResponseStream.Current.HealthCheck + "\n" +
                "        MaintenanceState " + edgeEvent.ResponseStream.Current.MaintenanceState + "\n" +
                "        ErrorMsg " + edgeEvent.ResponseStream.Current.ErrorMsg + "\n");
                continue;
            }
          }
        });

        /*var clientEdgeEvent2 = CreateClientEdgeEvent(null);
            await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent2);
            Console.WriteLine("Sent location edge event");*/
        for (int i = 0; i < 1000; i++) {
          /*if (i % 2 == 0)
          {
            location = locs[i % 3];
            var clientEdgeEvent2 = CreateClientEdgeEvent(location);
            await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent2);
            Console.WriteLine("Sent location edge event");
          }
          else
          {
            var dummySamples = new List<Sample>();
            var sample1 = new Sample() { Value = 1.1};
            var sample2 = new Sample() { Value = 3.2};
            dummySamples.Add(sample1);
            dummySamples.Add(sample2);
            var clientEdgeEvent2 = CreateClientEdgeEvent(locs[i%3], eventType: ClientEdgeEvent.Types.ClientEventType.EventCustomEvent, samples: dummySamples, customEventName: "testBlah");
            await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent2);
            Console.WriteLine("Sent custom edge event");
          }*/
          if (i % 2 == 0)
          {
            var clientEdgeEvent = CreateClientEdgeEvent(location, eventType: ClientEdgeEvent.Types.ClientEventType.EventLatencySamples, samples: times);
            await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent);
          }
          else
          {
            var clientEdgeEvent = CreateClientEdgeEvent(location, eventType: ClientEdgeEvent.Types.ClientEventType.EventLocationUpdate);
            await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent);
          }
          Thread.Sleep(500);
        }

        var clientEdgeEvent3 = CreateClientEdgeEvent(location, eventType: ClientEdgeEvent.Types.ClientEventType.EventTerminateConnection);
        await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent3);

        Console.WriteLine("closing write stream");
        await edgeEvent.RequestStream.CompleteAsync();
      }
      catch (Exception e)
      {
        Console.WriteLine("Exception is " + e.Message);
      }


      /*var registerClientRequest = CreateRegisterClientRequest(getCarrierName(), orgName, appName, "2.0", "");
      var regReply = client.RegisterClient(registerClientRequest);

      Console.WriteLine("RegisterClient Reply Status: " + regReply.Status);
      Console.WriteLine("RegisterClient TokenServerURI: " + regReply.TokenServerUri);

      // Store sessionCookie, for later use in future requests.
      sessionCookie = regReply.SessionCookie;

      // Request the token from the TokenServer:
      string token = null;
      try
      {
        token = RetrieveToken(regReply.TokenServerUri);
        Console.WriteLine("VerifyLocation pre-query TokenServer token: " + token);
      }
      catch (System.Net.WebException we)
      {
        Debug.WriteLine(we.ToString());

      }
      if (token == null)
      {
        return;
      }


      // Call the remainder. Verify and Find cloudlet.

      // Async version can also be used. Blocking:
      var verifyResponse = VerifyLocation(token);
      Console.WriteLine("VerifyLocation Status: " + verifyResponse.GpsLocationStatus);
      Console.WriteLine("VerifyLocation Accuracy: " + verifyResponse.GpsLocationAccuracyKm);

      // Blocking GRPC call:
      var findCloudletReply = FindCloudlet();
      Console.WriteLine("FindCloudlet Status: " + findCloudletReply.Status);
      // appinst server end port might not exist:
      foreach (AppPort p in findCloudletReply.Ports)
      {
        Console.WriteLine("Port: fqdn_prefix: " + p.FqdnPrefix +
                  ", protocol: " + p.Proto +
                  ", public_port: " + p.PublicPort +
                  ", internal_port: " + p.InternalPort +
                  ", end_port: " + p.EndPort);
      }
      // Straight reflection print:
      Console.WriteLine("FindCloudlet Reply: " + findCloudletReply);*/
    }


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

    FindCloudletRequest CreateFindCloudletRequest(string carrierName, Loc gpsLocation)
    {
      var request = new FindCloudletRequest
      {
        Ver = 1,
        SessionCookie = sessionCookie,
        //CarrierName = carriers[0],
        GpsLocation = locs[1],
      };
      return request;
    }

    // gps length: 4
    // carrier length: 6
    // datanettype length: 4
    // deviceos length: 2
    // devicemodels length: 3
    ClientEdgeEvent CreateClientEdgeEvent(Loc gpsLocation, ClientEdgeEvent.Types.ClientEventType eventType = ClientEdgeEvent.Types.ClientEventType.EventLocationUpdate, List<Sample> samples = null, string customEventName = "")
    {
      var deviceInfoStatic = new DeviceInfoStatic
      {
        //DeviceOs = deviceOs[1],
        //DeviceModel = deviceModels[1],
          DeviceOs = "Android_Version_29",
          DeviceModel = "SM-G988U"
      };
      var deviceInfoDynamic = new DeviceInfoDynamic
      {
        //DataNetworkType = dataNetTypes[1],
        DataNetworkType = "NETWORK_TYPE_5G",
        CarrierName = "311480",
        SignalStrength = 3,
      };
      var clientEvent = new ClientEdgeEvent
      {
        SessionCookie = sessionCookie,
        EdgeEventsCookie = eeSessionCookie,
        EventType = eventType, 
        GpsLocation = locs[1],
      };

      //if (eventType == ClientEdgeEvent.Types.ClientEventType.EventInitConnection)
      //{
        clientEvent.DeviceInfoStatic = deviceInfoStatic;
        clientEvent.DeviceInfoDynamic = deviceInfoDynamic;
        clientEvent.GpsLocation = locs[1];
      //}
      /*else
      {
        if (eventType == ClientEdgeEvent.Types.ClientEventType.EventLocationUpdate)
        {
          Console.WriteLine("event location update");
          deviceInfoDynamic.DataNetworkType = dataNetTypes[2];
        }
        clientEvent.DeviceInfoDynamic = deviceInfoDynamic;
      }*/

      if (customEventName != "")
      {
        clientEvent.CustomEvent = customEventName;
      }

      if (samples != null)
      { 
        foreach (Sample sample in samples)
        {
          clientEvent.Samples.Add(sample);
        }
      }
      iteration++;
      return clientEvent;
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

    VerifyLocationReply VerifyLocation(string token)
    {
      var verifyLocationRequest = CreateVerifyLocationRequest(getCarrierName(), getLocation(), token);
      var verifyResult = client.VerifyLocation(verifyLocationRequest);
      return verifyResult;
    }

    FindCloudletReply FindCloudlet()
    {
      // Create a synchronous request for FindCloudlet using RegisterClient reply's Session Cookie (TokenServerURI is now invalid):
      var findCloudletRequest = CreateFindCloudletRequest(getCarrierName(), getLocation());
      var findCloudletReply = client.FindCloudlet(findCloudletRequest);

      return findCloudletReply;
    }

    // TODO: The app must retrieve form they platform this case sensitive value before each DME GRPC call.
    // The device is potentially mobile and may have data roaming.
    String getCarrierName() {
      return carrierName;
    }

    // TODO: The client must retrieve a real GPS location from the platform, even if it is just the last known location,
    // possibly asynchronously.
    Loc getLocation()
    {
      return new DistributedMatchEngine.Loc
      {
        Latitude = 30.2672,
        Longitude = 97
      };
    }

  }
}
