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

using NUnit.Framework;
using DistributedMatchEngine;
using DistributedMatchEngine.Mel;

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
    // Test to an alternate server:
    const string dmeHost = "eu-mexdemo." + MatchingEngine.baseDmeHost;

    const string orgName = "MobiledgeX-Samples";
    const string appName = "HttpEcho";
    const string appVers = "1.0";
    const string connectionTestFqdn = "autoclusterhttpecho.dusseldorf-main.tdg.mobiledgex.net";
    const string connectionTlsTestFqdn = "autoclusterhttpechotls.dusseldorf-main.tdg.mobiledgex.net";
    const string aWebSocketServerFqdn = "vivobrazil-rjo2.telefonica.mobiledgex.net"; // or, localhost.


    static MatchingEngine me;

    class TestCarrierInfo : CarrierInfo
    {
      string CarrierInfo.GetCurrentCarrierName()
      {
        return null;
      }

      string CarrierInfo.GetMccMnc()
      {
        return null;
      }

      ulong CarrierInfo.GetCellID()
      {
        return 0;
      }
    }

    class TestUniqueID : UniqueID
    {
      string UniqueID.GetUniqueIDType()
      {
        return "uniqueIdTypeModel";
      }

      string UniqueID.GetUniqueID()
      {
        return "uniqueId";
      }
    }

    class TestDeviceInfo : DeviceInfoApp
    {

      Dictionary<string, string> DeviceInfoApp.GetDeviceInfo()
      {
        return new Dictionary<string, string>();
      }
    }

    public class TestMelMessaging : MelMessagingInterface
    {
      public bool IsMelEnabled() { return false; }
      public string GetMelVersion() { return ""; }
      public string GetUid() { return ""; }
      public string SetToken(string token, string app_name) { return ""; }
      public string GetManufacturer() { return "DummyManufacturer"; }
    }

    [SetUp]
    public void Setup()
    {
      // Create a network interface abstraction, with named WiFi and Cellular interfaces.
      CarrierInfo carrierInfo = new TestCarrierInfo();
      NetInterface netInterface = new SimpleNetInterface(new MacNetworkInterfaceName());
      UniqueID uniqueIdInterface = new TestUniqueID();
      DeviceInfoApp deviceInfo = new TestDeviceInfo();

      // pass in unknown interfaces at compile and runtime.
      me = new MatchingEngine(carrierInfo, netInterface, uniqueIdInterface, deviceInfo);
      me.SetMelMessaging(new TestMelMessaging());
    }

    private MemoryStream getMemoryStream(string jsonStr)
    {
      var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonStr));
      return ms;
    }

    [Test]
    public async static Task TestTCPConnection()
    {
      string test = "{\"Data\":\"tcp test string\"}";
      string message = "POST / HTTP/1.1\r\n" +
          "Host: 10.227.69.96:3001\r\n" +
          "User-Agent: curl/7.54.0\r\n" +
          "Accept: */*\r\n" +
          "Content-Length: " +
          test.Length + "\r\n" +
          "Content-Type: application/json\r\n" + "\r\n" + test;
      byte[] bytesMessage = Encoding.ASCII.GetBytes(message);

      string receiveMessage = "";
      try
      {

        byte[] bytes = Encoding.UTF8.GetBytes(message);

        var loc = await Util.GetLocationFromDevice();
        FindCloudletReply reply1 = null;

        Console.WriteLine("XXX 0");
        // Overide, test to another server:
        reply1 = await me.RegisterAndFindCloudlet(dmeHost, MatchingEngine.defaultDmeGrpcPort,
          orgName: orgName,
          appName: appName,
          appVersion: appVers,
          loc: loc);

        Console.WriteLine("XXX 1");
        int knownPort = 3001;
        var appPorts = me.GetAppPortsByProtocol(reply1, LProto.Tcp);
        var appPort = appPorts[knownPort]; // Known port of this instance.
        string host = appPort.FqdnPrefix + reply1.Fqdn;
        Console.WriteLine("Ports1: " + appPort.InternalPort);
        string url = me.CreateUrl(reply1, appPort, "https", knownPort);
        Console.WriteLine("Url to use: " + url);
        var appTcpPorts = me.GetTCPAppPorts(reply1);

        Socket tcpConnection = await me.GetTCPConnection(connectionTestFqdn, 3001, 5000);
        Assert.ByVal(tcpConnection, Is.Not.Null);

        tcpConnection.Send(bytesMessage);

        byte[] buffer = new byte[4096]; // Just a big buffer.
        int numRead = tcpConnection.Receive(buffer);
        byte[] readBuffer = new byte[numRead];
        Array.Copy(buffer, readBuffer, numRead);
        receiveMessage = Encoding.UTF8.GetString(readBuffer);

        Console.WriteLine("Echoed tcp string: " + receiveMessage);
        Assert.True(receiveMessage.Contains(test));
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
        Assert.True(responseBody.Contains(dict["data"]));
      }
      catch (HttpRequestException e)
      {
        Assert.Fail("HttpRequestException is " + e.Message);
      }
    }

    [Test]
    public async static Task TestTCPTLSConnection()
    {
      // The server reformats the JSON, so extra spaces are removed.
      string test = "{\"Data\":\"tcp test string\"}";
      string message = "POST / HTTP/1.1\r\n" +
          "Host: 10.227.69.96:3001\r\n" +
          "User-Agent: curl/7.54.0\r\n" +
          "Accept: */*\r\n" +
          "Content-Length: " +
          test.Length + "\r\n" +
          "Content-Type: application/json\r\n" + "\r\n" + test;
      byte[] bytesMessage = Encoding.ASCII.GetBytes(message);


      // TLS on TCP Connection Test
      try
      {
        var loc = await Util.GetLocationFromDevice();
        FindCloudletReply reply1 = null;

        // Overide, test to another server:
        reply1 = await me.RegisterAndFindCloudlet(dmeHost, MatchingEngine.defaultDmeGrpcPort,
          orgName: orgName,
          appName: appName+"Tls", // Deploy a Tls version!
          appVersion: appVers,
          loc: loc);

        int knownPort = 3001;
        var appPorts = me.GetAppPortsByProtocol(reply1, LProto.Tcp);
        var appPort = appPorts[knownPort]; // Known port of this instance.
        string host = appPort.FqdnPrefix + reply1.Fqdn;
        Console.WriteLine("Ports1: " + appPort.InternalPort);
        string url = me.CreateUrl(reply1, appPort, "https", knownPort);
        Console.WriteLine("Url to use: " + url);
        var appTcpPorts = me.GetTCPAppPorts(reply1);

        SslStream stream = await me.GetTCPTLSConnection(host, appPort.PublicPort, 5000);
        Assert.ByVal(stream, Is.Not.Null);
        Assert.ByVal(stream.CipherAlgorithm, Is.Not.Null);

        stream.Write(Encoding.UTF8.GetBytes(message));

        await Task.Delay(200);
        byte[] readBuffer = new byte[4096]; // Just a big buffer.

        Assert.True(stream.CanRead);
        int numRead = stream.Read(readBuffer, 0, readBuffer.Length);

        string response = Encoding.UTF8.GetString(readBuffer, 0, numRead);
        Console.WriteLine("Response: " + response);
        Assert.True(response.Contains(test));
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
        Assert.Fail("ioe exception is " + ioe.Message + "\n" + ioe.StackTrace);
      }
      catch (DmeDnsException dde)
      {
        Console.WriteLine("DmeDnsException is " + dde);
      }
      catch (RegisterClientException rce)
      {
        Console.WriteLine("RegisterClientException is " + rce);
        return;
      }
      catch (FindCloudletException fce)
      {
        Console.WriteLine("FindCloudletException is " + fce);
        return;
      }
    }

    [Test]
    public async static Task TestWebsocketsConnection()
    {
      string message = "Websockets connection test";
      byte[] bytesMessage = Encoding.ASCII.GetBytes(message);

      //! [getwebsocketexample]
      ClientWebSocket socket = null;
      string url = "ws://" + aWebSocketServerFqdn + ":" + 3000;
      UriBuilder uriBuilder = new UriBuilder(url);
      Uri uri = uriBuilder.Uri;

      try
      {
        socket = await me.GetWebsocketConnection(uri, 5000);
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
      //! [getwebsocketexample]
    }

    // Test Workflow with TCP connection and exception handling
    [Test]
    public async static Task TestGetConnectionWorkflow()
    {
      // MatchingEngine APIs developer workflow:

      // findCloudletReply = me.RegisterAndFindCloudlet(carrierName, orgName, appName, appVers, authToken, loc)
      // appPortsDict = me.GetTCpPorts(findCloudletReply)
      // appPort = appPortsDict[internal_port]
      //      internal_port is the Container port specified in the Dockerfile
      // socket = me.GetTCPConnection(findCloudletReply, appPort, desiredPort, timeout)
      //      desiredPort can be set to -1 if user wants to default to public_port

      //! [getconnectionworkflow]
      var loc = await Util.GetLocationFromDevice();
      FindCloudletReply reply = null;

      try
      {
        reply = await me.RegisterAndFindCloudlet(dmeHost, MatchingEngine.defaultDmeGrpcPort,
          orgName: orgName,
          appName: appName,
          appVersion: appVers,
          loc: loc);
      }
      catch (DmeDnsException dde)
      {
        Console.WriteLine("Workflow DmeDnsException is " + dde);
      }
      catch (NotImplementedException nie)
      {
        Console.WriteLine("NotImplementedException is " + nie);
      }
      catch (RegisterClientException rce)
      {
        Console.WriteLine("Workflow RegisterClient is " + rce);
        return;
      }
      Assert.ByVal(reply, Is.Not.Null);

      Dictionary<int, AppPort> appPortsDict = me.GetTCPAppPorts(reply);
      Assert.True(reply.Status.Equals(FindCloudletReply.Types.FindStatus.FindFound));

      // If there's more than one AppPort (or even a range of ports), you really do need to know your own App Port layout.
      int knownPort = 3001;
      var appPort = appPortsDict[knownPort];
      int public_port = appPort.PublicPort;

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
      //! [getconnectionworkflow]
    }

    [Test]
    public async static Task TestAppPortMappings()
    {
      AppPort appPort = new AppPort();
      appPort.Proto = LProto.Tcp;
      appPort.InternalPort = 8008;
      appPort.PublicPort = 3000;
      appPort.EndPort = 8010;
      appPort.FqdnPrefix = "";

      AppPort appPort2 = new AppPort();
      appPort2.Proto = LProto.Tcp;
      appPort2.InternalPort = 8008;
      appPort2.PublicPort = 3000;
      appPort2.EndPort = 0;
      appPort2.FqdnPrefix = "";

      FindCloudletReply fce = new FindCloudletReply();
      fce.Fqdn = "mobiledgexmobiledgexsdkdemo20.sdkdemo-app-cluster.us-los-angeles.gcp.mobiledgex.net";

      fce.Ports.Add(appPort);
      fce.Ports.Add(appPort2);

      // Default -> Use Public Port
      int port = me.GetPort(appPort);
      Console.WriteLine("port is " + port);
      Assert.True(port == appPort.PublicPort, "Default port did not return public port. Returned " + port);

      // Desired == Internal -> Use Public Port
      int port2 = me.GetPort(appPort, 8008);
      Console.WriteLine("port2 is " + port2);
      Assert.True(port2 == appPort.PublicPort, "Internal port did not return public port. Returned " + port2);

      // Desired != Internal && Desired in range -> Use Desired Port
      int port3 = me.GetPort(appPort, 3001);
      Console.WriteLine("port3 is " + port3);
      Assert.True(port3 == 3001, "Desired port in port range did not return desired port. Returned " + port3);

      // Desired != Internal && Desired not in range -> Exception
      try
      {
        int port4 = me.GetPort(appPort, 2999);
        Assert.Fail("Desired port not in port range should have thrown GetConnectionException");
      }
      catch (GetConnectionException gce)
      {
        Console.WriteLine("GetConnectionException for port4 is " + gce.Message);
        Assert.True(gce.Message.Contains("not in AppPort range"), "Wrong GetConnectionException. Should have been about not in AppPort range. " + gce.Message);
      }
      catch (Exception e)
      {
        Assert.Fail("Wrong exception. " + e.Message);
      }

      try
      {
        int port5 = me.GetPort(appPort2, 3001);
        Assert.Fail("Desired port not in port range should have thrown GetConnectionException");
      }
      catch (GetConnectionException gce)
      {
        Console.WriteLine("GetConnectionException for port5 is " + gce.Message);
        Assert.True(gce.Message.Contains("not in AppPort range"), "Wrong GetConnectionException. Should have been about not in AppPort range. " + gce.Message);
      }
      catch (Exception e)
      {
        Assert.Fail("Wrong exception. " + e.Message);
      }

      // AppPort that is not in FindCloudletReply -> Exception
      try
      {
        string url = me.CreateUrl(fce, appPort2, "http");
      }
      catch (GetConnectionException gce)
      {
        Console.WriteLine("GetConnectionException for create url is " + gce.Message);
        Assert.True(gce.Message.Contains("Unable to validate AppPort"), "Wrong GetConnectionException. Should have been \"Unable to validate AppPort\". " + gce.Message);
      }
      catch (Exception e)
      {
        Assert.Fail("Wrong exception. " + e.Message);
      }

      try
      {
        string url = me.CreateUrl(fce, appPort, "http", 8008);
        Console.WriteLine("Correct url is " + url);
        Assert.True(url == "http://mobiledgexmobiledgexsdkdemo20.sdkdemo-app-cluster.us-los-angeles.gcp.mobiledgex.net:3000", "Url created is incorrect. " + url);
      }
      catch (GetConnectionException gce)
      {
        Console.WriteLine("GetConnectionException for create url is " + gce.Message);
        Assert.Fail("Correct CreateURL GetConnectionException is: " + gce.Message);
      }
      catch (Exception e)
      {
        Assert.Fail("Wrong exception. " + e.Message);
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
      FindCloudletReply reply1 = null;

      try
      {
        // Overide, test to another server:
        reply1 = await me.RegisterAndFindCloudlet(dmeHost, MatchingEngine.defaultDmeGrpcPort,
          orgName: orgName,
          appName: appName,
          appVersion: appVers,
          loc: loc);
      }
      catch (DmeDnsException dde)
      {
        Console.WriteLine("Workflow DmeDnsException is " + dde);
      }
      catch (RegisterClientException rce)
      {
        Console.WriteLine("Workflow RegisterClientException is " + rce);
        return;
      }
      catch (FindCloudletException fce)
      {
        Console.WriteLine("Workflow FindCloudletException is " + fce);
        return;
      }
      Assert.ByVal(reply1, Is.Not.Null);
      Assert.ByVal(reply1.Fqdn, Is.Not.Null);
      if (reply1 != null)
      {
        Console.WriteLine("FindCloudlet Reply Status: " + reply1.Status);
        Console.WriteLine("FindCloudlet:" +
                " ver: " + reply1.Ver +
                ", fqdn: " + reply1.Fqdn +
                ", cloudlet_location: " +
                " long: " + reply1.CloudletLocation.Longitude +
                ", lat: " + reply1.CloudletLocation.Latitude);
        // App Ports:
        foreach (AppPort p in reply1.Ports)
        {
          Console.WriteLine("Port: fqdn_prefix: " + p.FqdnPrefix +
                ", protocol: " + p.Proto +
                ", public_port: " + p.PublicPort +
                ", internal_port: " + p.InternalPort +
                ", end_port: " + p.EndPort);
        }
      }

      NetTest netTest = new NetTest(me);

      // Site 1
      Dictionary<int, AppPort> appPortsDict1 = me.GetTCPAppPorts(reply1);
      Assert.True(appPortsDict1.Count > 0, "No dictionary results for TCP Port!");
      Assert.True(reply1.Ports.Count > 0, "No Ports!");

      var appPort = appPortsDict1[3001]; // Known port;

      int public_port1 = appPort.PublicPort;
      Site site1 = new Site { host = appPort.FqdnPrefix + reply1.Fqdn, port = public_port1 };

      // In case you want a local test server:
      /*
      site2 = new Site
      {
        host = "localhost",
        port = 3000,
        testType = TestType.CONNECT
      };
      */
      netTest.sites.Enqueue(site1);

      try
      {
        Site siteOne = new Site();

        netTest.PingIntervalMS = 500;
        netTest.doTest(true);
        await Task.Delay(5000).ConfigureAwait(false);
        netTest.doTest(false);
        netTest.sites.TryPeek(out siteOne);
        double avg1 = siteOne.average;
        Console.WriteLine("Average 1: " + siteOne.average + ", Test running? " + netTest.runTest);
        foreach (Site s in netTest.sites)
        {
          for (int i = 0; i < s.samples.Length; i++)
          {
            Console.WriteLine("Sample 1: " + s.samples[i]);
          }
          Assert.True(s.average < 2000);
        }

        await Task.Delay(3000).ConfigureAwait(false);
        foreach (Site s in netTest.sites)
        {
          for (int i = 0; i < s.samples.Length; i++)
          {
            Console.WriteLine("Sample 1.5: " + s.samples[i]);
          }
          Assert.True(s.average < 2000);
        }
        netTest.sites.TryPeek(out siteOne);
        double avg15 = siteOne.average;
        Console.WriteLine("Average 1.5: " + siteOne.average + ", Test running? " + netTest.runTest);
        Assert.False(netTest.runTest, "Should be stopped, along with the average calculation.");
        Assert.True(avg1 == avg15, "Thread didn't stop. Averages are not equal (or subject to noise)!");

        netTest.doTest(true);
        await Task.Delay(6000).ConfigureAwait(false);
        netTest.doTest(false);
        netTest.sites.TryPeek(out siteOne);
        Console.WriteLine("Average 2: " + siteOne.average);
        Assert.True(avg15 != siteOne.average, "Thread didn't restart!");


        netTest.sites.TryPeek(out siteOne);
        foreach (Site s in netTest.sites)
        {
          for (int i = 0; i < s.samples.Length; i++)
          {
            Console.WriteLine("Sample: " + s.samples[i]);
          }
          Assert.True(s.average < 2000);
        }

        Assert.True(netTest.sites.ToArray()[0].samples[0].Value >= 0);
        netTest.doTest(false);
      }
      catch (Exception e)
      {
        Assert.Fail("Excepton while testing: " + e.Message);
        if (e.InnerException != null)
        {
          Console.WriteLine("Inner Exception: " + e.InnerException.Message + ",\nStacktrace: " + e.InnerException.StackTrace);
        }
      }
    }


    [Test]
    public async static Task TestUniqueIdText()
    {
      RegisterClientRequest req1;


      try
      {
        req1 = me.CreateRegisterClientRequest(
          orgName: orgName,
          appName: appName,
          appVersion: appVers);

        // Micro Test:
        TestMelMessaging mt = new TestMelMessaging();
        Assert.AreEqual("DummyManufacturer", mt.GetManufacturer());

        // It's the actual RegisterClient DME call that grabs the latest
        // values for hashed Advertising ID and unique ID, and does so as late
        // as possible.
        // It's the actual send, not the message creation where it is filled in.

        Console.WriteLine("Testing GRPC's NotNull Empty String if unspecified. This is NOT REST behavior.");
        Assert.AreEqual("", req1.UniqueIdType);
        Assert.AreEqual("", req1.UniqueId);
      }
      catch (Exception e)
      {
        Assert.Fail("Excepton while testing: " + e.Message);
        if (e.InnerException != null)
        {
          Console.WriteLine("Inner Exception: " + e.InnerException.Message + ",\nStacktrace: " + e.InnerException.StackTrace);
        }
      }
    }
  }

}
