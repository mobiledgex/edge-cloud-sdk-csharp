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

using NUnit.Framework;
using DistributedMatchEngine;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Net.Http;
using System.Threading;
using System.Net.Security;
using System.Security.Authentication;
using System.Net.WebSockets;
using DistributedMatchEngine.PerformanceMetrics;
using static DistributedMatchEngine.PerformanceMetrics.NetTest;

namespace Tests
{
  public class Tests
  {
    const string carrierName = "TDG";
    const string devName = "MobiledgeX";
    const string appName = "PongGame2";
    const string appVers = "2019-09-26";
    const string developerAuthToken = "";
    const UInt32 cellID = 0;
    const string uniqueIDType = "";
    const string uniqueID = "";
    static Tag[] tags = new Tag[0];
    const string connectionTestFqdn = "mextest-app-cluster.frankfurt-main.tdg.mobiledgex.net";
    const string aWebSocketServerFqdn = "ponggame2-tcp.frankfurt-main.tdg.mobiledgex.net"; // or, localhost.

    static MatchingEngine me;

    class TestCarrierInfo : CarrierInfo
    {
      string CarrierInfo.GetCurrentCarrierName()
      {
        return carrierName;
      }

      string CarrierInfo.GetMccMnc()
      {
        return "26010";
      }

      UInt32 CarrierInfo.GetCellID()
      {
        return 0;
      }
    }

    class TestUniqueID : UniqueID
    {
      string UniqueID.GetUniqueIDType()
      {
        return "";
      }

      string UniqueID.GetUniqueID()
      {
        return "";
      }
    }

    [SetUp]
    public void Setup()
    {
      // Create a network interface abstraction, with named WiFi and Cellular interfaces.
      CarrierInfo carrierInfo = new TestCarrierInfo();
      NetInterface netInterface = new SimpleNetInterface(new MacNetworkInterfaceName());
      UniqueID uniqueID = new TestUniqueID();

      // pass in unknown interfaces at compile and runtime.
      me = new MatchingEngine(carrierInfo, netInterface, uniqueID);
    }

    private MemoryStream getMemoryStream(string jsonStr)
    {
      var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonStr));
      return ms;
    }

    /**
     * Basic equivalence tests for the QosPositionKpiStream internal JSON block parser.
     */
    [Test]
    public void Test1()
    {
      QosPositionKpiStream streamParser;
      var js1 = "{ 'foo' = 1 }";
      streamParser = new QosPositionKpiStream(getMemoryStream(js1));

      string parsed;
      parsed = streamParser.ParseJsonBlock();
      Assert.AreEqual(js1, parsed);

      js1 = @"{ [{""foo"": ""2""}, {'bar' = 3}, { 'doot': '$2' }] }";
      streamParser = new QosPositionKpiStream(getMemoryStream(js1));
      parsed = streamParser.ParseJsonBlock();
      Assert.AreEqual(js1, parsed);

      var js2 = @"{ [{""foo"": ""2""}, {'bar' = 3}, { 'doot': '$2' }] }{ [{""foo"": ""2""}, {'bar' = 3}, { 'doot': '$2' }] }";
      streamParser = new QosPositionKpiStream(getMemoryStream(js2));

      for (int i = 0; i < 2; i++)
      {
        parsed = streamParser.ParseJsonBlock();
        Assert.AreEqual(js1, parsed);
      }
      streamParser.Dispose();
      Console.WriteLine("Test1 finished.");
    }

    /**
     * Basic equivalence tests for the QosPositionKpiStream internal JSON block parser.
     * An escape char.
     */
    [Test]
    public void TestQosPositionStreamEscape()
    {
      QosPositionKpiStream streamParser;
      var js1 = @"{ [{""foo\u005C"": ""2""}, {'bar' = 3}, { 'doot': '$2' }] }";

      string parsed;
      streamParser = new QosPositionKpiStream(getMemoryStream(js1));
      parsed = streamParser.ParseJsonBlock();
      Assert.AreEqual(js1, parsed);
      streamParser.Dispose();
      Console.WriteLine("TestQosPositionStreamEscape finished.");
    }

    /**
     * Basic equivalence tests for the QosPositionKpiStream internal JSON block parser.
     */
    [Test]
    public void TestQosPositionStream()
    {
      QosPositionKpiStream streamParser;
      string parsed;
      string js1 = @"{
 ""result"": {
  ""ver"": 0,
  ""status"": ""RS_SUCCESS"",
  ""position_results"": [
   {
    ""positionid"": ""1"",
    ""gps_location"": {
     ""latitude"": 50.11729018260935,
     ""longitude"": 8.576783680147576,
     ""horizontal_accuracy"": 0,
     ""vertical_accuracy"": 0,
     ""altitude"": 0,
     ""course"": 0,
     ""speed"": 0,
     ""timestamp"": {
      ""seconds"": ""63703198734"",
      ""nanos"": 863000000
     }
},
    ""dluserthroughput_min"": 0,
    ""dluserthroughput_avg"": 31.561584,
    ""dluserthroughput_max"": 121.52567,
    ""uluserthroughput_min"": 0,
    ""uluserthroughput_avg"": 18.889288,
    ""uluserthroughput_max"": 51.594624,
    ""latency_min"": 47.231102,
    ""latency_avg"": 0,
    ""latency_max"": 0
   },
   {
    ""positionid"": ""2"",
    ""gps_location"": {
     ""latitude"": 50.124580365218705,
     ""longitude"": 8.571467360295152,
     ""horizontal_accuracy"": 0,
     ""vertical_accuracy"": 0,
     ""altitude"": 0,
     ""course"": 0,
     ""speed"": 0,
     ""timestamp"": {
      ""seconds"": ""63703198734"",
      ""nanos"": 863000000
     }
    },
    ""dluserthroughput_min"": 0,
    ""dluserthroughput_avg"": 41.597435,
    ""dluserthroughput_max"": 99.60402,
    ""uluserthroughput_min"": 0,
    ""uluserthroughput_avg"": 12.110276,
    ""uluserthroughput_max"": 37.700558,
    ""latency_min"": 47.231102,
    ""latency_avg"": 0,
    ""latency_max"": 0
   }
  ]
 }
}";
      streamParser = new QosPositionKpiStream(getMemoryStream(js1));
      parsed = streamParser.ParseJsonBlock();

      // Light existance pass:
      streamParser = new QosPositionKpiStream(getMemoryStream(js1), 15000);
      foreach (var reply in streamParser)
      {
        Assert.AreEqual(ReplyStatus.RS_SUCCESS, reply.status);
        Assert.AreEqual(2, reply.position_results.Length);
        Assert.AreEqual(50.11729018260935, reply.position_results[0].gps_location.latitude);
        Assert.AreEqual(8.576783680147576, reply.position_results[0].gps_location.longitude);
        Assert.AreEqual(47.231102f, reply.position_results[0].latency_min); // Not "very" exact.
      }
      Assert.AreEqual(js1, parsed);
      streamParser.Dispose();

      Console.WriteLine("TestQosPositionStream finished.");
    }

    [Test]
    public async static Task TestTCPConnection()
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
      string receiveMessage = "";
      try
      {
        Socket tcpConnection = await me.GetTCPConnection(connectionTestFqdn, 3001, 5000);
        Assert.ByVal(tcpConnection, Is.Not.Null);

        tcpConnection.Send(bytesMessage);

        byte[] buffer = new byte[message.Length * 2]; // C# chars are unicode-16 bits
        int numRead = tcpConnection.Receive(buffer);
        byte[] readBuffer = new byte[numRead];
        Array.Copy(buffer, readBuffer, numRead);
        receiveMessage = Encoding.ASCII.GetString(readBuffer);

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
      Assert.True(receiveMessage.Contains("tcp test string"));
      Console.WriteLine("TestTCPConnection finished.");
    }

    [Test]
    public async static Task TestHTTPConnection()
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
        Assert.ByVal(httpClient, Is.Not.Null);

        StringContent content = new StringContent(message);
        HttpResponseMessage response = await httpClient.PostAsync(httpClient.BaseAddress, content);

        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        Assert.ByVal(responseBody, Is.Not.Null);
        Console.WriteLine("http response body is " + responseBody);
        Assert.True(responseBody.Contains("HTTP Connection Test"));
      }
      catch (HttpRequestException e)
      {
        Assert.Fail("HttpRequestException is " + e.Message);
      }
    }

    // Test is incompete. Missing SSL test server.
    //[Test]
    public async static Task TestTCPTLSConnection()
    {
      // TLS on TCP Connection Test
      try
      {
        string data = "{\"data\": \"Http Connection Test\"}";
        string rawpost = "POST / HTTP/1.1\r\n" +
                "Host: 10.227.66.62:3000\r\n" +
                "User-Agent: curl/7.54.0\r\n" +
                "Accept: */*\r\n" +
                "Content-Length: " + data.Length + "\r\n" +
                "Content-Type: application/json\r\n" +
                "\r\n" + data;
        byte[] bytes = Encoding.UTF8.GetBytes(rawpost);

        SslStream stream = await me.GetTCPTLSConnection(connectionTestFqdn, 3001, 5000);
        Assert.ByVal(stream, Is.Not.Null);
        Assert.ByVal(stream.CipherAlgorithm, Is.Not.Null);

        stream.Write(Encoding.UTF8.GetBytes(rawpost));
        stream.Write(Encoding.UTF8.GetBytes(data));

        await Task.Delay(500);
        byte[] readBuffer = new byte[4096 * 2];

        Assert.True(stream.CanRead);
        stream.Read(readBuffer);

        string response = Encoding.UTF8.GetString(readBuffer);
        Assert.True(response.Contains("Http Connection Test"));
        stream.Close();
      }
      catch (AuthenticationException e)
      {
        Assert.Fail("Authentication Exception is " + e.Message);
      }
      catch (GetConnectionException e)
      {
        Assert.Fail("TCPTLS GetConnectionException is " + e.Message);
      }
      catch (IOException ioe)
      {
        // FIXME: The test server doesn't have HTTPs.
        Assert.False(ioe.Message.Contains("The handshake failed due to an unexpected packet format."));
      }
    }

    [Test]
    public async static Task TestWebsocketsConnection()
    {
      string message = "Websockets connection test";
      byte[] bytesMessage = Encoding.ASCII.GetBytes(message);

      // Websocket Connection Test
      ClientWebSocket socket = null;
      try
      {
        socket = await me.GetWebsocketConnection(aWebSocketServerFqdn, 3000, 5000);
      }
      catch (Exception e)
      {
        Console.WriteLine("Websocket Initial GetConnectionException is " + e.Message);
        Assert.ByVal(socket, Is.Not.Null);
        Assert.True(socket.State == WebSocketState.Open);
      }

      try
      {
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
        Assert.True(socket.State == WebSocketState.Open);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "end of test", token);
        await Task.Delay(100);
        Assert.True(socket.State == WebSocketState.Closed || socket.State == WebSocketState.CloseSent);
      }
      catch (GetConnectionException e)
      {
        Console.WriteLine("Websocket GetConnectionException is " + e.Message);
        // Since we don't necessarily know the message format, this should actually be slammed closed by the running server.
        //Assert.True(e.Message.Contains("Cannot get websocket connection"));
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
    [Test]
    public async static Task TestGetConnectionWorkflow()
    {
      // MatchingEngine APIs Developer workflow:

      // findCloudletReply = me.RegisterAndFindCloudlet(carrierName, devName, appName, appVers, authToken, loc)
      // appPortsDict = me.GetTCpPorts(findCloudletReply)
      // appPort = appPortsDict[internal_port]
      //      internal_port is the Container port specified in the Dockerfile
      // socket = me.GetTCPConnection(findCloudletReply, appPort, desiredPort, timeout)
      //      desiredPort can be set to -1 if user wants to default to public_port

      var loc = await Util.GetLocationFromDevice();
      FindCloudletReply reply = null;

      try
      {
        reply = await me.RegisterAndFindCloudlet(carrierName, devName, appName, appVers, developerAuthToken, loc, cellID, uniqueIDType, uniqueID, tags);
      }
      catch (DmeDnsException dde)
      {
        Console.WriteLine("Workflow DmeDnsException is " + dde);
      }
      catch (RegisterClientException rce)
      {
        Console.WriteLine("Workflow RegisterClient is " + rce);
        return;
      }
      Assert.ByVal(reply, Is.Not.Null);

      Dictionary<int, AppPort> appPortsDict = me.GetTCPAppPorts(reply);

      int public_port = reply.ports[0].public_port; // We happen to know it's the first one.
      AppPort appPort = appPortsDict[public_port];

      Assert.ByVal(appPort, Is.Not.Null);

      try
      {
        Socket tcpConnection = await me.GetTCPConnection(reply, appPort, public_port, 5000);
        Assert.ByVal(tcpConnection, Is.Not.Null);
        tcpConnection.Close();
      }
      catch (GetConnectionException e)
      {
        Console.WriteLine("Workflow GetConnectionException is " + e.Message);
        Assert.Fail("Workflow GetConnectionException is " + e.Message);
      }
      catch (Exception e)
      {
        Assert.Fail("workflow test exception " + e.Message);
      }
    }

    [Test]
    public async static Task TestTimeout()
    {
      try
      {
        Socket tcpConnection = await me.GetTCPConnection(connectionTestFqdn, 3001, 10);
        tcpConnection.Close();
      }
      catch (GetConnectionException e)
      {
        Console.WriteLine("Timeout test GetConnectionException is " + e.Message);
        Assert.True(e.Message.Contains("Timeout"));
      }
      catch (Exception e)
      {
        Console.WriteLine("Timeout test exception " + e.Message);
        Assert.False(e.Message.Contains("Timeout"));
      }
    }

    [Test]
    public async static Task TestNetTest()
    {
      var loc = await Util.GetLocationFromDevice();
      FindCloudletReply reply = null;

      try
      {
        reply = await me.RegisterAndFindCloudlet(carrierName, devName, appName, appVers, developerAuthToken, loc, cellID, uniqueIDType, uniqueID, tags);
      }
      catch (DmeDnsException dde)
      {
        Console.WriteLine("Workflow DmeDnsException is " + dde);
      }
      catch (RegisterClientException rce)
      {
        Console.WriteLine("Workflow RegisterClient is " + rce);
        return;
      }
      Assert.ByVal(reply, Is.Not.Null);

      Dictionary<int, AppPort> appPortsDict = me.GetTCPAppPorts(reply);

      int public_port = reply.ports[0].public_port; // We happen to know it's the first one.
      AppPort appPort = appPortsDict[public_port];

      NetTest netTest = new NetTest(me);

      string l7Url = MatchingEngine.CreateUrl(reply, appPort, appPort.public_port);
      Site site = new Site
      {
        L7Path = MatchingEngine.CreateUrl(reply, appPort, appPort.public_port),
        testType = TestType.CONNECT
      };
      // In case you want a local test server:
      /*
      site = new Site
      {
        host = "localhost",
        port = 3000,
        testType = TestType.CONNECT
      };
      */
      netTest.sites.Enqueue(site);

      netTest.PingIntervalMS = 1000;
      netTest.doTest(true);
      await Task.Delay(6000).ConfigureAwait(false);
      foreach (Site s in netTest.sites)
      {
        for (int i = 0; i < s.samples.Length; i++)
        {
          Console.WriteLine("Sample: " + s.samples[i]);
        }
        Assert.True(s.average < 2000);
      }
      Assert.True(netTest.sites.ToArray()[0].samples[0] >= 0);
      netTest.doTest(false);

    }

    [Test]
    public async static Task TestGetConnection()
    {
      Task websocketTest = TestWebsocketsConnection();
      Task tcpTest = TestTCPConnection();
      Task httpTest = TestHTTPConnection();
      Task tcpTlsTest = TestTCPTLSConnection();
      Task getConnectionWorkflow = TestGetConnectionWorkflow();
      Task timeoutTest = TestTimeout();

      await websocketTest;
      await tcpTest;
      await httpTest;
      await tcpTlsTest;
      await getConnectionWorkflow;
      await timeoutTest;
    }
  }
}