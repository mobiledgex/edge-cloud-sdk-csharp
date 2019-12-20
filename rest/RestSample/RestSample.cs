/**
 * Copyright 2019 MobiledgeX, Inc. All rights and licenses reserved.
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
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Security.Authentication;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;

using DistributedMatchEngine;


namespace RestSample
{
  class Program
  {
    static string carrierName = "GDDT";
    static string devName = "MobiledgeX";
    static string appName = "MobiledgeX SDK Demo";
    static string appVers = "1.0";
    static string developerAuthToken = "";
    static string connectionTestFqdn = "mextest-app-cluster.fairview-main.gddt.mobiledgex.net";

    // For SDK purposes only, this allows continued operation against default app insts.
    // A real app will get exceptions, and need to skip the DME, and fallback to public cloud.
    static string fallbackDmeHost = MatchingEngine.fallbackDmeHost;

    // Get the ephemerial carriername from device specific properties.
    async static Task<string> getCurrentCarrierName()
    {
      var dummy = await Task.FromResult(0);
      return carrierName;
    }

    static Timestamp createTimestamp(int futureSeconds)
    {
      long ticks = DateTime.Now.Ticks;
      long sec = ticks / TimeSpan.TicksPerSecond; // Truncates.
      long remainderTicks = ticks - (sec * TimeSpan.TicksPerSecond);
      int nanos = (int)(remainderTicks / TimeSpan.TicksPerMillisecond) * 1000000;

      var timestamp = new Timestamp
      {
        seconds = (sec+futureSeconds).ToString(),
        nanos = nanos
      };

      return timestamp;
    }

    static List<QosPosition> CreateQosPositionList(Loc firstLocation, double direction_degrees, double totalDistanceKm, double increment)
    {
      var req = new List<QosPosition>();
      double kmPerDegreeLong = 111.32; // at Equator
      double kmPerDegreeLat = 110.57; // at Equator
      double addLongitude = (Math.Cos(direction_degrees / (Math.PI / 180)) * increment) / kmPerDegreeLong;
      double addLatitude = (Math.Sin(direction_degrees / (Math.PI / 180)) * increment) / kmPerDegreeLat;
      double i = 0d;
      double longitude = firstLocation.longitude;
      double latitude = firstLocation.latitude;

      long id = 1;

      while (i < totalDistanceKm)
      {
        longitude += addLongitude;
        latitude += addLatitude;
        i += increment;

        // FIXME: No time is attached to GPS location, as that breaks the server!
        var qloc = new QosPosition
        {
          positionid = id.ToString(),
          gps_location = new Loc {
            longitude = longitude,
            latitude = latitude,
            timestamp = createTimestamp(100)
          }
        };


        req.Add(qloc);
        id++;
      }

      return req;
    }

    async static Task TestTCPConnection(MatchingEngine me)
    {
        string test = "{\"Data\": \"tcp test string\"}";
        string message = "POST / HTTP/1.1\r\n" +
            "Host: 10.227.69.96:3001\r\n" +
            "User-Agent: curl/7.54.0\r\n" +
            "Accept: */*\r\n" +
            "Content-Length: " +
            test.Length + "\r\n" +
            "Content-Type: application/json\r\n" + "\r\n" + test;
        byte[] bytesMessage = Encoding.ASCII.GetBytes(message);

        // TCP Connection Test
        try
        {
            Socket tcpConnection = await me.GetTCPConnection(connectionTestFqdn, 3001, 5);

            tcpConnection.Send(bytesMessage);

            byte[] bytesReceive = new byte[message.Length * 2]; // C# chars are unicode-16 bits
            tcpConnection.Receive(bytesReceive);
            string receiveMessage = Encoding.ASCII.GetString(bytesReceive);

            Console.WriteLine("Echoed tcp string: " + receiveMessage);
            tcpConnection.Close();
        }
        catch (GetConnectionException e)
        {
            Console.WriteLine("TCP GetConnectionException is " + e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("TCP socket exception is " + e);
        }
    }

    async static Task TestHTTPConnection(MatchingEngine me)
    {
        var dict = new Dictionary<string, string>();
        dict["data"] = "HTTP Connection Test";

        var settings = new DataContractJsonSerializerSettings();
        settings.UseSimpleDictionaryFormat = true;
        var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>), settings);

        var ms = new MemoryStream();
        serializer.WriteObject(ms, dict);
        string message = Util.StreamToString(ms);

        string uriString = connectionTestFqdn;
        UriBuilder uriBuilder = new UriBuilder("http", uriString, 3001);
        Uri uri = uriBuilder.Uri;

        // HTTP Connection Test
        try
        { 
            HttpClient httpClient = await me.GetHTTPClient(uri);

            StringContent content = new StringContent(message);
            HttpResponseMessage response = await httpClient.PostAsync(httpClient.BaseAddress, content);

            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("http response body is " + responseBody);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("HttpRequestException is " + e.Message);
        }
    }

    async static Task TestTCPTLSConnection(MatchingEngine me)
    {
        // TLS on TCP Connection Test
        try
        {
            SslStream stream = await me.GetTCPTLSConnection(connectionTestFqdn, 3001, 5);
            stream.Close();
        }
        catch (AuthenticationException e)
        {
            Console.WriteLine("Authentication Exception is " + e.Message);
        }
        catch (GetConnectionException e)
        {
            Console.WriteLine("TCPTLS GetConnectionException is " + e.Message);
        }
    }

    async static Task TestWebsocketsConnection(MatchingEngine me)
    {
        string message = "Websockets connection test";
        byte[] bytesMessage = Encoding.ASCII.GetBytes(message);

        // Websocket Connection Test
        try
        {
            ClientWebSocket socket = await me.GetWebsocketConnection(connectionTestFqdn, 3001, 5);

            // Send message
            ArraySegment<Byte> sendBuffer = new ArraySegment<byte>(bytesMessage);
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            await socket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, token);

            // Receive message
            byte[] bytesReceive = new byte[message.Length * 2];
            ArraySegment<Byte> receiveBuffer = new ArraySegment<byte>(bytesReceive);
            WebSocketReceiveResult result = await socket.ReceiveAsync(receiveBuffer, token);
            string receiveMessage = Encoding.ASCII.GetString(receiveBuffer.Array, receiveBuffer.Offset, result.Count);

            Console.WriteLine("Echoed websocket result is " + receiveMessage);
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "end of test", token);
        }
        catch (GetConnectionException e)
        {
            Console.WriteLine("Websocket GetConnectionException is " + e.Message);
        }
        catch (OperationCanceledException e)
        {
            Console.WriteLine("Websocket OperationCanceledException is " + e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("Websocket Exception is " + e.Message);
        }
    }

    // Test Workflow with TCP connection and exception handling
    async static Task TestGetConnectionWorkflow(MatchingEngine me)
    {
        // MatchingEngine APIs Developer workflow:

        // findCloudletReply = me.RegisterAndFindCloudlet(carrierName, devName, appName, appVers, authToken, loc)
        // appPortsDict = me.GetTCpPorts(findCloudletReply)
        // appPort = appPortsDict[internal_port]
        //      internal_port is the Container port specified in the Dockerfile
        // socket = me.GetTCPConnection(findCloudletReply, appPort, desiredPort, timeout)
        //      desiredPort can be set to -1 if user wants to default to public_port

        var loc = await Util.GetLocationFromDevice();
        FindCloudletReply reply;

        try
        {
            reply = await me.RegisterAndFindCloudlet(carrierName, devName, appName, appVers, developerAuthToken, loc);
        }
        catch (DmeDnsException dde)
        {
            Console.WriteLine("Workflow DmeDnsException is " + dde.InnerException);
            return;
        }
        catch (RegisterClientException rce)
        {
            Console.WriteLine("Workflow RegisterClient is " + rce.InnerException);
            return;
        }

        Dictionary<int, AppPort> appPortsDict = me.GetTCPAppPorts(reply);
        if (appPortsDict.Count == 0)
        {
            Console.WriteLine("No ports with specified protocol");
            return;
        }

        AppPort appPort = appPortsDict[3001];
        if (appPort == null)
        {
            Console.WriteLine("Not AppPorts with specified internal port");
            return;
        }

        try
        {
            Socket tcpConnection = await me.GetTCPConnection(reply, appPort, 3001, 5); // 5 second timeout
            tcpConnection.Close();
        }
        catch (GetConnectionException e)
        {
            Console.WriteLine("Workflow GetConnectionException is " + e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("workflow test exception " + e.Message);
        }
    }

    async static Task TestTimeout(MatchingEngine me)
    {
        // comment out localIP and bind in GetConnectionHelper.cs in order to test timeout
        try
        {
            Socket tcpConnection = await me.GetTCPConnection(connectionTestFqdn, 3001, 0.1);
            tcpConnection.Close();
        }
        catch (GetConnectionException e)
        {
            Console.WriteLine("Timeout test GetConnectionException is " + e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("Timeout test exception " + e.Message);
        }
    }

    async static Task TestGetConnection(MatchingEngine me)
    {
        Task websocketTest = TestWebsocketsConnection(me);
        Task tcpTest = TestTCPConnection(me);
        Task httpTest = TestHTTPConnection(me);
        Task tcpTlsTest = TestTCPTLSConnection(me);
        Task getConnectionWorkflow = TestGetConnectionWorkflow(me);
        Task timeoutTest = TestTimeout(me);

        await websocketTest;
        await tcpTest;
        await httpTest;
        await tcpTlsTest;
        await getConnectionWorkflow;
        await timeoutTest;
    }

    async static Task Main(string[] args)
    {
      try
      {
        carrierName = await getCurrentCarrierName();

        Console.WriteLine("MobiledgeX RestSample!");

        MatchingEngine me = new MatchingEngine(DistributedMatchEngine.OperatingSystem.OTHER);
        me.SetTimeout(15000);

        // Start location task. This is for test use only. The source of the
        // location in an Unity application should be from an application context
        // LocationService.
        var locTask = Util.GetLocationFromDevice();

        Task testing = TestGetConnection(me);
        await testing;
        Console.WriteLine("connection test finished");

        var registerClientRequest = me.CreateRegisterClientRequest(carrierName, devName, appName, appVers, developerAuthToken);

        // APIs depend on Register client to complete successfully:
        RegisterClientReply registerClientReply;
        try
        {
          try
          {
            registerClientReply = await me.RegisterClient(registerClientRequest);
            Console.WriteLine("RegisterClient Reply Status: " + registerClientReply.status);
          }
          catch (DmeDnsException)
          {
            // DME doesn't exist in DNS. This is not a normal path if the SIM card is supported. Fallback to public cloud here.
            registerClientReply = await me.RegisterClient(MatchingEngine.fallbackDmeHost, MatchingEngine.defaultDmeRestPort, registerClientRequest);
            Console.WriteLine("RegisterClient Reply Status: " + registerClientReply.status);
          }
        }
        catch (HttpException httpe) // HTTP status, and REST API call error codes.
        {
          // server error code, and human readable message:
          Console.WriteLine("RegisterClient Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }
        // Do Verify and FindCloudlet in concurrent tasks:
        var loc = await locTask;

        // Independent requests:
        var verifyLocationRequest = me.CreateVerifyLocationRequest(carrierName, loc);
        var findCloudletRequest = me.CreateFindCloudletRequest(carrierName, devName, appName, appVers, loc);
        var getLocationRequest = me.CreateGetLocationRequest(carrierName);


        // These are asynchronious calls, of independent REST APIs.

        // FindCloudlet:
        try
        {
          FindCloudletReply findCloudletReply = null;
          try
          {
            await me.FindCloudlet(findCloudletRequest);
          }
          catch (DmeDnsException)
          {
            // DME doesn't exist in DNS. This is not a normal path if the SIM card is supported. Fallback to public cloud here.
            findCloudletReply = await me.FindCloudlet(MatchingEngine.fallbackDmeHost, MatchingEngine.defaultDmeRestPort, findCloudletRequest);
          }
          Console.WriteLine("FindCloudlet Reply: " + findCloudletReply);

          if (findCloudletReply != null)
          {
            Console.WriteLine("FindCloudlet Reply Status: " + findCloudletReply.status);
            Console.WriteLine("FindCloudlet:" +
                    " ver: " + findCloudletReply.ver +
                    ", fqdn: " + findCloudletReply.fqdn +
                    ", cloudlet_location: " +
                    " long: " + findCloudletReply.cloudlet_location.longitude +
                    ", lat: " + findCloudletReply.cloudlet_location.latitude);
            // App Ports:
            foreach (AppPort p in findCloudletReply.ports)
            {
              Console.WriteLine("Port: fqdn_prefix: " + p.fqdn_prefix +
                    ", protocol: " + p.proto +
                    ", public_port: " + p.public_port +
                    ", internal_port: " + p.internal_port +
                    ", path_prefix: " + p.path_prefix +
                    ", end_port: " + p.end_port);
            }
          }
        }
        catch (HttpException httpe)
        {
          Console.WriteLine("FindCloudlet Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }

        // Get Location:
        GetLocationReply getLocationReply = null;

        try
        {
          try
          {
            getLocationReply = await me.GetLocation(getLocationRequest);
          }
          catch (DmeDnsException)
          {
            getLocationReply = await me.GetLocation(MatchingEngine.fallbackDmeHost, MatchingEngine.defaultDmeRestPort, getLocationRequest);
          }
          Console.WriteLine("GetLocation Reply: " + getLocationReply);

          var location = getLocationReply.network_location;
          Console.WriteLine("GetLocationReply: longitude: " + location.longitude + ", latitude: " + location.latitude);
        }
        catch (HttpException httpe)
        {
          Console.WriteLine("GetLocation Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }

        // Verify Location:
        try
        {
          Console.WriteLine("VerifyLocation() may timeout, due to reachability of carrier verification servers from your network.");
          VerifyLocationReply verifyLocationReply = null;
          try
          {
            verifyLocationReply = await me.VerifyLocation(verifyLocationRequest);
          }
          catch (DmeDnsException)
          {
            verifyLocationReply = await me.VerifyLocation(MatchingEngine.fallbackDmeHost, MatchingEngine.defaultDmeRestPort, verifyLocationRequest);
          }
          if (verifyLocationReply != null)
          {
            Console.WriteLine("VerifyLocation Reply GPS location status: " + verifyLocationReply.gps_location_status);
            Console.WriteLine("VerifyLocation Reply Tower Status: " + verifyLocationReply.tower_status);
          }
        }
        catch (HttpException httpe)
        {
          Console.WriteLine("VerifyLocation Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }
        catch (InvalidTokenServerTokenException itste)
        {
          Console.WriteLine(itste.Message + "\n" + itste.StackTrace);
        }

        // Get QosPositionKpi:
        try
        {
          // Create a list of quality of service position requests:
          var firstLoc = new Loc
          {
            longitude = 8.5821,
            latitude = 50.11
          };
          var requestList = CreateQosPositionList(firstLoc, 45, 2, 1);

          var qosPositionRequest = me.CreateQosPositionRequest(requestList, 0, null);

          QosPositionKpiStream qosReplyStream = null;
          try
          {
            qosReplyStream = await me.GetQosPositionKpi(qosPositionRequest);
          } catch (DmeDnsException)
          {
            qosReplyStream = await me.GetQosPositionKpi(MatchingEngine.fallbackDmeHost, MatchingEngine.defaultDmeRestPort, qosPositionRequest);
          }

          if (qosReplyStream == null)
          {
            Console.WriteLine("Reply result missing: " + qosReplyStream);
          }
          else
          {
            foreach (var qosPositionKpiReply in qosReplyStream)
            {
              // Serialize the DataContract and print everything:
              DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(QosPositionKpiReply));
              MemoryStream ms = new MemoryStream();
              serializer.WriteObject(ms, qosPositionKpiReply);
              string jsonStr = Util.StreamToString(ms);
              Console.WriteLine("QoS of requested gps location(s): " + jsonStr);
            }
            qosReplyStream.Dispose();
          }
        }
        catch (HttpException httpe)
        {
          Console.WriteLine("QosPositionKpi Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }

      }
      catch (Exception e) // Catch All
      {
        Console.WriteLine(e.Message + "\n" + e.StackTrace);
      }
    }
  };
}
