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

    string dmeHost = "127.0.0.1"; // demo DME server hostname or ip.
    int dmePort = 50051; // DME port.
    string carrierName = "";
    string orgName = "mobiledgex";
    string appName = "arshooter";
    string appVers = "1";

    MatchEngineApi.MatchEngineApiClient client;

    public async Task RunSampleFlowAsync()
    {
      try
      {
        location = getLocation();
        string uri = dmeHost + ":" + dmePort;
        Console.WriteLine("url is " + uri);

        // Channel:
        // ChannelCredentials channelCredentials = new SslCredentials();
        ChannelCredentials channelCredentials = ChannelCredentials.Insecure;
        Channel channel = new Channel(uri, channelCredentials);

        client = new MatchEngineApi.MatchEngineApiClient(channel);

        var registerClientRequest = CreateRegisterClientRequest(getCarrierName(), orgName, appName, appVers, "");
        if (client == null)
        {
          Console.WriteLine("client is null");
        }
        else
        {
          Console.WriteLine("client: " + client.ToString());
        }
        var regReply = client.RegisterClient(registerClientRequest);
        Console.WriteLine("RegisterClient Reply Status: " + regReply.Status);
        Console.WriteLine("RegisterClient TokenServerURI: " + regReply.TokenServerUri);
        sessionCookie = regReply.SessionCookie;

        var clientEdgeEvent1 = CreateClientEdgeEvent(location);
        var edgeEvent = client.SendEdgeEvent();
        await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent1);

        var readTask = Task.Run(async () =>
        {
          while (await edgeEvent.ResponseStream.MoveNext())
          {
            Console.WriteLine("EdgeServerEvent: Latency: " + edgeEvent.ResponseStream.Current.Latency + ", Msg: " + edgeEvent.ResponseStream.Current.Msg);
          }
        });

        Thread.Sleep(10000);

        location.Latitude = 69.69;
        location.Longitude = 69.69;
        var clientEdgeEvent2 = CreateClientEdgeEvent(location);
        await edgeEvent.RequestStream.WriteAsync(clientEdgeEvent2);

        Thread.Sleep(15000);

        var clientEdgeEvent3 = CreateClientEdgeEvent(location, true);
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
        CarrierName = carrierName,
        GpsLocation = gpsLocation
      };
      return request;
    }

    ClientEdgeEvent CreateClientEdgeEvent(Loc gpsLocation, bool terminate = false)
    {
      Timestamp timestamp = new Timestamp();
      var clientEvent = new ClientEdgeEvent
      {
        SessionCookie = sessionCookie,
        Timestamp = timestamp,
        Event = Event.Newlocation,
        GpsLocation = gpsLocation,
        Terminate = terminate
      };
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
        Longitude = -122.149349,
        Latitude = 37.459609
      };
    }

  }
}
