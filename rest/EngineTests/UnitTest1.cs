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

namespace Tests
{
  public class Tests
  {
    const string carrierName = "TDG";
    const string devName = "MobiledgeX";
    const string appName = "PongGame2";
    const string appVers = "2019-09-26";
    const string developerAuthToken = "";
    const string connectionTestFqdn = "mextest-app-cluster.frankfurt-main.tdg.mobiledgex.net";

    static MatchingEngine me;

    [SetUp]
    public void Setup()
    {
      me = new MatchingEngine(DistributedMatchEngine.OperatingSystem.OTHER);
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
        Socket tcpConnection = await me.GetTCPConnection(connectionTestFqdn, 3001, 5);
        Assert.ByVal(tcpConnection, Is.Not.Null);

        tcpConnection.Send(bytesMessage);

        byte[] bytesReceive = new byte[message.Length * 2]; // C# chars are unicode-16 bits
        tcpConnection.Receive(bytesReceive);
        receiveMessage = Encoding.ASCII.GetString(bytesReceive);

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

      Assert.AreEqual("tcp test string", receiveMessage, "Test string doesn't match!");
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
      }
      catch (HttpRequestException e)
      {
        Assert.Fail("HttpRequestException is " + e.Message);
      }
    }

    [Test]
    public async static Task TestTCPTLSConnection()
    {
      // TLS on TCP Connection Test
      try
      {
        SslStream stream = await me.GetTCPTLSConnection(connectionTestFqdn, 3001, 5);
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
    }

    [Test]
    public async static Task TestWebsocketsConnection()
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
        reply = await me.RegisterAndFindCloudlet(carrierName, devName, appName, appVers, developerAuthToken, loc);
      }
      catch (DmeDnsException dde)
      {
        Console.WriteLine("Workflow DmeDnsException is " + dde.InnerException);
      }
      catch (RegisterClientException rce)
      {
        Console.WriteLine("Workflow RegisterClient is " + rce.InnerException);
        return;
      }
      Assert.ByVal(reply, Is.Not.Null);

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

    [Test]
    public async static Task TestTimeout()
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