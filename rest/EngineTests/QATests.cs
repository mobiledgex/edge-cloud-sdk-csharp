using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DistributedMatchEngine;
using NUnit.Framework;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static Tests.QATests;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;

namespace Tests
{
  public class QATests
  {
    const string dmeHost = "us-qa." + MatchingEngine.baseDmeHost;
    const string orgName = "automation_dev_org";
    const string appName = "automation-sdk-porttest";
    const string appVers = "1.0";

    public QATests()
    {
    }
    public class DummyUniqueID : UniqueID
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
    public class DummyDeviceInfo : DeviceInfo
    {
      DummyCarrierInfo carrierInfo = new DummyCarrierInfo();

      Dictionary<string, string> DeviceInfo.GetDeviceInfo()
      {
        Dictionary<string, string> dict = new Dictionary<string, string>();
        dict["DataNetworkPath"] = carrierInfo.GetDataNetworkPath();
        dict["CarrierName"] = carrierInfo.GetCurrentCarrierName();
        dict["SignalStrength"] = carrierInfo.GetSignalStrength().ToString();
        dict["DeviceModel"] = "C#SDK";
        dict["DeviceOS"] = "TestOS";
        return dict;
      }

      public bool IsPingSupported()
      {
        return true;
      }

    }
    public class DummyCarrierInfo : CarrierInfo
    {
      public ulong GetCellID()
      {
        return 0;
      }

      public string GetCurrentCarrierName()
      {
        return "TDG";
      }

      public string GetMccMnc()
      {
        return "26201";
      }

      public string GetDataNetworkPath()
      {
        return "GSM";
      }

      public ulong GetSignalStrength()
      {
        return 2;
      }
    }

    static string SetLocation(string locLat, string locLong)
    {
      string clientIP = "";

      System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("curl");
      psi.Arguments = " ifconfig.me";
      psi.RedirectStandardOutput = true;
      System.Diagnostics.Process ipCurl;
      ipCurl = System.Diagnostics.Process.Start(psi);
      ipCurl.WaitForExit();
      System.IO.StreamReader reader = ipCurl.StandardOutput;
      ipCurl.WaitForExit();
      if (ipCurl.HasExited)
      {
        clientIP = reader.ReadToEnd();
      }
      //Console.WriteLine(clientIP);

      string resp = null;
      string serverURL = "http://mexdemo.locsim.mobiledgex.net:8888/updateLocation";
      string payload = "{" + '"' + "latitude" + '"' + ':' + locLat + ',' + ' ' + '"' + "longitude" + '"' + ':' + locLong + ',' + ' ' + '"' + "ipaddr" + '"' + ':' + '"' + clientIP + '"' + "}";
      Console.WriteLine(payload);
      byte[] postBytes = Encoding.UTF8.GetBytes(payload);
      WebRequest request = WebRequest.Create(serverURL);
      request.Method = "POST";
      request.ContentType = "application/json; charset=UTF-8";
      //request.ContentType = "text/html; charset=utf-8";
      request.ContentLength = postBytes.Length;
      Stream dataStream = request.GetRequestStream();
      dataStream.Write(postBytes, 0, postBytes.Length);
      dataStream.Close();
      try
      {
        WebResponse response = request.GetResponse();
        Console.WriteLine("Response: " + ((HttpWebResponse)response).StatusDescription);
        dataStream = response.GetResponseStream();
        StreamReader sReader = new StreamReader(dataStream);
        string responseFromServer = sReader.ReadToEnd();
        if (((HttpWebResponse)response).StatusDescription == "OK")
        {
          Console.WriteLine(responseFromServer);
        }
        sReader.Close();
        dataStream.Close();
        response.Close();
        return resp;
      }
      catch (System.Net.WebException we)
      {
        WebResponse respon = (HttpWebResponse)we.Response;
        Console.WriteLine("Response: " + we.Status);
        Stream dStream = respon.GetResponseStream();
        StreamReader sr = new StreamReader(dStream);
        string responseFromServer = sr.ReadToEnd();
        Console.WriteLine(responseFromServer);
        sr.Close();
        dStream.Close();
        respon.Close();
        return resp;
      }
    }
    [Test]
    public async static Task TestRegisterClient()
    {
      string tokenServerURI = "http://mexdemo.tok.mobiledgex.net:9999/its?followURL=https://dme.mobiledgex.net/verifyLoc";
      string carrierName = "tmus";
      string orgName = "automation_dev_org";
      string appName = "automation_api_app";
      string appVers = "1.0";
      string developerAuthToken = "";

      string host = "us-qa.dme.mobiledgex.net";
      uint port = 38001;
      string sessionCookie;

      // Get the ephemerial carriername from device specific properties.
      async Task<string> getCurrentCarrierName()
      {
        var dummy = await Task.FromResult(0);
        return carrierName;
      }
      try
      {
        carrierName = await getCurrentCarrierName();
        Console.WriteLine("RegisterClientRest Testcase");
        MatchingEngine me = new MatchingEngine(null, new SimpleNetInterface(new LinuxNetworkInterfaceName()), new DummyUniqueID(), new DummyDeviceInfo());

        // Start location task:
        var locTask = Util.GetLocationFromDevice();


        var registerClientRequest = me.CreateRegisterClientRequest(orgName, appName, appVers, developerAuthToken);

        // Await synchronously.
        var registerClientReply = await me.RegisterClient(host, port, registerClientRequest);
        if (registerClientReply.status != ReplyStatus.Success)
        {
          Console.WriteLine("RegisterClient Failed! " + registerClientReply.status);
          Console.WriteLine("Test Case Failed!!!");
        }
        long timeLongMs = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        long seconds = timeLongMs / 1000;
        int nanoSec = (int)(timeLongMs % 1000) * 1000000;
        var ts = new Timestamp { nanos = nanoSec, seconds = seconds.ToString() };
        var loc = new Loc()
        {
          course = 0,
          altitude = 100,
          horizontal_accuracy = 5,
          speed = 2,
          longitude = 13.405,
          latitude = 52.52,
          vertical_accuracy = 20,
          timestamp = ts
        };

        //Verify the Token Server URI is correct
        if (registerClientReply.token_server_uri != tokenServerURI)
        {
          Console.WriteLine("TestFailed, Token Server URI InCorrect!");
          return;
        }
        else
        {
          Console.WriteLine("Token Server URI Correct!");
        }

        // Store sessionCookie, for later use in future requests.
        sessionCookie = registerClientReply.session_cookie;

        //Setup to handle the sessiontoken
        var jwtHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken secToken = null;
        secToken = jwtHandler.ReadJwtToken(sessionCookie);
        var claims = secToken.Claims;
        var jwtPayload = "";
        foreach (Claim c in claims)
        {
          jwtPayload += '"' + c.Type + "\":\"" + c.Value + "\",";
        }


        //Extract the sessiontoken contents
        char[] delimiterChars = { ',', '{', '}' };
        string[] words = jwtPayload.Split(delimiterChars);
        long expTime = 0;
        long iatTime = 0;
        bool expParse = false;
        bool iatParse = false;
        string peer;
        string org;
        string app;
        string appver;

        foreach (var word in words)
        {
          if (word.Length > 7)
          {
            //Console.WriteLine(word);
            if (word.Substring(1, 3) == "exp")
            {
              expParse = long.TryParse(word.Substring(7, 10), out expTime);
            }
            if (word.Substring(1, 3) == "iat")
            {
              iatParse = long.TryParse(word.Substring(7, 10), out iatTime);
            }
            if (expParse && iatParse)
            {
              int divider = 60;
              long tokenTime = expTime - iatTime;
              tokenTime /= divider;
              tokenTime /= divider;
              int expLen = 24;
              expParse = false;
              Assert.True(tokenTime == expLen, "Peerip Expression Didn't Match!  ");
              Console.WriteLine("Session Cookie Exparation Time correct:  " + tokenTime);
            }
            if (word.Substring(1, 6) == "peerip")
            {
              peer = word.Substring(10);
              peer = peer.Substring(0, peer.Length - 1);
              string pattern = "^\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}$";

              Assert.True(System.Text.RegularExpressions.Regex.IsMatch(peer, pattern), "Peerip Expression Didn't Match!  ");
              Console.WriteLine("Peerip Expression Matched!  " + peer);

            }
            if (word.Substring(1, 7) == "orgname")
            {
              org = word.Substring(11);
              org = org.Substring(0, org.Length - 1);
              Assert.True(org == orgName, "Orgname Didn't Match!");
            }
            if (word.Substring(1, 7) == "appname")
            {
              app = word.Substring(11);
              app = app.Substring(0, app.Length - 1);
              Assert.True(app == appName, "AppName Didn't Match!");
            }
            if (word.Substring(1, 7) == "appvers")
            {
              appver = word.Substring(11, 3);
              Assert.True(appver == appVers, "App Version Didn't Match!  ");
            }
          }
        }
        Console.WriteLine("Test Case Passed!!!");
      }
      catch (CryptographicException ce)
      {
        Console.WriteLine(ce.StackTrace);
      }
      catch (InvalidTokenServerTokenException itste)
      {
        Console.WriteLine(itste.StackTrace);
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
        if (e.InnerException != null)
        {
          Console.WriteLine("\n\nInner Execption: " + e.InnerException.Message);
        }
      }

    }


    [Test]
    public async static Task TestFindCloudlet()
    {
      string tokenServerURI = "http://mexdemo.tok.mobiledgex.net:9999/its?followURL=https://dme.mobiledgex.net/verifyLoc";
      string carrierName = "tmus";
      string orgName = "automation_dev_org";
      string appName = "automation_api_app";
      string appVers = "1.0";
      string developerAuthToken = "";

      string host = "us-qa.dme.mobiledgex.net";
      uint port = 38001;
      string sessionCookie;
      try
      {
        Console.WriteLine("FindCloudletSuccessRest Testcase");

        MatchingEngine me = new MatchingEngine(null, new SimpleNetInterface(new LinuxNetworkInterfaceName()), new DummyUniqueID(), new DummyDeviceInfo());
        me.SetTimeout(15000);

        var locTask = Util.GetLocationFromDevice();

        var registerClientRequest = me.CreateRegisterClientRequest(orgName, appName, appVers, developerAuthToken);

        var registerClientReply = await me.RegisterClient(host, port, registerClientRequest);

        long timeLongMs = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        long seconds = timeLongMs / 1000;
        int nanoSec = (int)(timeLongMs % 1000) * 1000000;
        var ts = new Timestamp { nanos = nanoSec, seconds = seconds.ToString() };
        var loc = new Loc()
        {
          course = 0,
          altitude = 100,
          horizontal_accuracy = 5,
          speed = 2,
          longitude = -91.405,
          latitude = 31.52,
          vertical_accuracy = 20,
          timestamp = ts
        };

        Assert.True(registerClientReply.token_server_uri == tokenServerURI, "Token Server URI Correct!");
        Console.WriteLine("Token Server URI Correct, tokenServerURI : " + tokenServerURI);

        sessionCookie = registerClientReply.session_cookie;
        var jwtHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken secToken = null;
        secToken = jwtHandler.ReadJwtToken(sessionCookie);
        var claims = secToken.Claims;
        var jwtPayload = "";
        foreach (Claim c in claims)
        {
          jwtPayload += '"' + c.Type + "\":\"" + c.Value + "\",";
        }
        char[] delimiterChars = { ',', '{', '}' };
        string[] words = jwtPayload.Split(delimiterChars);
        long expTime = 0;
        long iatTime = 0;
        bool expParse = false;
        bool iatParse = false;
        string peer;
        string org;
        string app;
        string appver;

        foreach (var word in words)
        {
          if (word.Length > 7)
          {
            if (word.Substring(1, 3) == "exp")
            {
              expParse = long.TryParse(word.Substring(7, 10), out expTime);
            }
            if (word.Substring(1, 3) == "iat")
            {
              iatParse = long.TryParse(word.Substring(7, 10), out iatTime);
            }
            if (expParse && iatParse)
            {
              int divider = 60;
              long tokenTime = expTime - iatTime;
              tokenTime /= divider;
              tokenTime /= divider;
              int expLen = 24;
              expParse = false;
              Assert.True(tokenTime == expLen, "Peerip Expression Didn't Match!  ");
              Console.WriteLine("Session Cookie Exparation Time correct:  " + tokenTime);
            }
            if (word.Substring(1, 6) == "peerip")
            {
              peer = word.Substring(10);
              peer = peer.Substring(0, peer.Length - 1);
              string pattern = "^\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}$";

              Assert.True(System.Text.RegularExpressions.Regex.IsMatch(peer, pattern), "Peerip Expression Didn't Match!  ");
              Console.WriteLine("Peerip Expression Matched!  " + peer);

            }
            if (word.Substring(1, 7) == "orgname")
            {
              org = word.Substring(11);
              org = org.Substring(0, org.Length - 1);
              Assert.True(org == orgName, "Orgname Didn't Match!");
            }
            if (word.Substring(1, 7) == "appname")
            {
              app = word.Substring(11);
              app = app.Substring(0, app.Length - 1);
              Assert.True(app == appName, "AppName Didn't Match!");
            }
            if (word.Substring(1, 7) == "appvers")
            {
              appver = word.Substring(11, 3);
              Assert.True(appver == appVers, "App Version Didn't Match!  ");
            }
          }
        }

        var findCloudletRequest = me.CreateFindCloudletRequest(loc, carrierName);

        var findCloudletTask = me.FindCloudlet(host, port, findCloudletRequest);
        var findCloudletReply = await findCloudletTask;
        Console.WriteLine("status " + (findCloudletReply.status == FindCloudletReply.FindStatus.Found));
        if (findCloudletReply.status == FindCloudletReply.FindStatus.Found)
        {
          Console.WriteLine("FindCloudlet Reply Status: " + findCloudletReply.status);
          Console.WriteLine("FindCloudlet Reply FQDN: " + findCloudletReply.fqdn);
          Console.WriteLine("FindCloudlet Reply Latitude: " + findCloudletReply.cloudlet_location.latitude);
          Console.WriteLine("FindCloudlet Reply Longitude: " + findCloudletReply.cloudlet_location.longitude);
          Console.WriteLine("Test Case Passed!!!");
        }

        else if (findCloudletReply.status == FindCloudletReply.FindStatus.Unknown)
        {
          Console.WriteLine("FindCloudlet Reply: " + findCloudletReply.status);
          Console.WriteLine("FindCloudlet Reply: " + findCloudletReply.fqdn);
          Console.WriteLine("FindCloudlet Reply: " + findCloudletReply.cloudlet_location.latitude);
          Console.WriteLine("FindCloudlet Reply: " + findCloudletReply.cloudlet_location.longitude);
          Console.WriteLine("Test Case Failed!!!");
        }
        else
        {
          if (findCloudletReply.status == FindCloudletReply.FindStatus.NotFound)
          {
            Console.WriteLine("FindCloudlet Reply: " + findCloudletReply.status);
            Console.WriteLine("Test Case Failed!!!");
          }
        }

      }
      catch (InvalidTokenServerTokenException itste)
      {
        Console.WriteLine(itste.StackTrace);
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
        if (e.InnerException != null)
        {
          Console.WriteLine("Inner Exception: " + e.InnerException.Message);
        }
      }
    }

    [Test]
    [TestCase("52.52", "13.405", 2d)]
    [TestCase("52.54", "13.405", 10d)]
    [TestCase("53.4100", "13.405", 100d)]
    public async static Task TestVerifyLocation(string locLat, string locLong, double gpsAccuracy)
    {
      string tokenServerURI = "http://mexdemo.tok.mobiledgex.net:9999/its?followURL=https://dme.mobiledgex.net/verifyLoc";
      string orgName = "automation_dev_org";
      string appName = "automation_api_app";
      string appVers = "1.0";
      string developerAuthToken = "";
      string host = "us-qa.dme.mobiledgex.net";
      uint port = 38001;
      string sessionCookie;
      try
      {

        Console.WriteLine("VerifyLocationRest Testcase");

        MatchingEngine me = new MatchingEngine(new DummyCarrierInfo(), new SimpleNetInterface(new LinuxNetworkInterfaceName()), new DummyUniqueID(), new DummyDeviceInfo());

        Console.WriteLine("Seting the location in the Location Server");
        SetLocation(locLat, locLong);
        Console.WriteLine("Location Set to {0},{1}\n", locLat, locLong);

        var locTask = Util.GetLocationFromDevice();

        var registerClientRequest = me.CreateRegisterClientRequest(orgName, appName, appVers, developerAuthToken);

        // Await synchronously.
        var registerClientReply = await me.RegisterClient(host, port, registerClientRequest);
        long timeLongMs = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        long seconds = timeLongMs / 1000;
        int nanoSec = (int)(timeLongMs % 1000) * 1000000;
        var ts = new Timestamp { nanos = nanoSec, seconds = seconds.ToString() };
        var loc = new Loc()
        {
          course = 0,
          altitude = 100,
          horizontal_accuracy = 5,
          speed = 2,
          longitude = 13.405,
          latitude = 52.52,
          vertical_accuracy = 20,
          timestamp = ts
        };
        Console.WriteLine("gpsAccuracy" + gpsAccuracy);
        //Verify the Token Server URI is correct
        Assert.True(registerClientReply.token_server_uri == tokenServerURI
  , "Verify Location Test, Token Server URI InCorrect!");


        // Store sessionCookie, for later use in future requests.
        sessionCookie = registerClientReply.session_cookie;

        var verifyLocationRequest = me.CreateVerifyLocationRequest(loc);
        var verfiyLocationTask = me.VerifyLocation(host, port, verifyLocationRequest);

        // Awaits:
        var verifyLocationReply = await verfiyLocationTask;

        Assert.True(verifyLocationReply.gps_location_status != VerifyLocationReply.GPSLocationStatus.Unknown
          , "Verify Location Failed!");
        Assert.True(verifyLocationReply.gps_location_status == VerifyLocationReply.GPSLocationStatus.Verified
  , "Verify Location Failed, Status != Verified");
        Assert.True(verifyLocationReply.gps_location_accuracy_km == gpsAccuracy
, "Verify Location Test Failed, Wrong gps_location_accuracy_km !");

        Console.WriteLine("VerifyLocation Reply - Status: " + verifyLocationReply.gps_location_status);
        Console.WriteLine("VerifyLocation Reply - Accuracy: " + verifyLocationReply.gps_location_accuracy_km + "KM");
        Console.WriteLine("Test Case Passed!!!");
      }
      catch (InvalidTokenServerTokenException itste)
      {
        Console.WriteLine(itste.StackTrace);
      }
      catch (Exception e)
      {
        Console.WriteLine(e.Message);
        if (e.InnerException != null)
        {
          Console.WriteLine("Inner Exception: " + e.InnerException.Message);
        }
      }
    }

    [Test]
    public async static Task TestGetUDPConnection()
    {

      string developerAuthToken = "";
      string aWebSocketServerFqdn = "";
      AppPort appPort = null;
      int myPort = 0;
      try
      {
        Console.WriteLine("Get UDP Connections Testcase!!");

        CarrierInfo dummyCarrier = new DummyCarrierInfo();
        MatchingEngine me = new MatchingEngine(dummyCarrier, new SimpleNetInterface(new LinuxNetworkInterfaceName()), new DummyUniqueID(), new DummyDeviceInfo());
        me.SetTimeout(15000);

        FindCloudletReply findCloudletInfo = null;
        var locTask = Util.GetLocationFromDevice();
        var registerClientRequest = me.CreateRegisterClientRequest(orgName, appName, appVers, developerAuthToken, (uint)me.carrierInfo.GetCellID(), me.GetUniqueIDType(), me.GetUniqueIDType());
        RegisterClientReply registerClientReply;
        registerClientReply = await me.RegisterClient(dmeHost, MatchingEngine.defaultDmeRestPort, registerClientRequest);
        Assert.True(registerClientReply.status == ReplyStatus.Success, "GetUDPConnectionTest RegisterClient Failed");

        var loc = await locTask;

        var findCloudletRequest = me.CreateFindCloudletRequest(loc, dummyCarrier.GetCurrentCarrierName());

        FindCloudletReply findCloudletReply = null;

        findCloudletReply = await me.FindCloudlet(dmeHost, MatchingEngine.defaultDmeRestPort, findCloudletRequest);
        Assert.True(findCloudletReply.status == FindCloudletReply.FindStatus.Found, "GetUDPConnectionTest FindCloudletReply Failed");


        Console.WriteLine("\nFindCloudlet Reply: " + findCloudletReply);
        findCloudletInfo = findCloudletReply;

        Assert.True(findCloudletReply != null && findCloudletReply.status == FindCloudletReply.FindStatus.Found, "GetUDPConnectionTest FindCloudletReply Failed");
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
          if (p.public_port == 2015)
          {
            appPort = p;
          }
          Console.WriteLine("Port: fqdn_prefix: " + p.fqdn_prefix +
                ", protocol: " + p.proto +
                ", public_port: " + p.public_port +
                ", internal_port: " + p.internal_port +
                ", end_port: " + p.end_port);
        }

        aWebSocketServerFqdn = me.GetHost(findCloudletInfo, appPort);
        myPort = me.GetPort(appPort, 2015);
        Console.WriteLine("\nTest UDP 2015 Connection Starting.");

        string test = "ping";
        string message = test;
        byte[] bytesMessage = Encoding.ASCII.GetBytes(message);

        // TCP Connection Test
        string receiveMessage = "";
        Socket stream = await me.GetUDPConnection(findCloudletInfo, appPort, myPort, 10000);

        stream.Send(bytesMessage);
        Console.WriteLine("Message Sent: " + message.ToString());

        byte[] buffer = new byte[message.Length * 2]; // C# chars are unicode-16 bits 
        int numRead = stream.Receive(buffer);

        byte[] readBuffer = new byte[numRead];
        Array.Copy(buffer, readBuffer, numRead);
        receiveMessage = Encoding.ASCII.GetString(readBuffer);
        Assert.True(receiveMessage == "pong", "UDP Get Connection DID NOT work!");
        Console.WriteLine("UDP Get Connection worked!: ");
        Console.WriteLine("Recieved Message: " + receiveMessage);
        stream.Close();
      }
      catch (Exception e)
      {
        Console.WriteLine("UDP socket exception is " + e);
        if (e.InnerException != null)
        {
          Console.WriteLine("Inner Exception: " + e.InnerException.Message);
        }
        Assert.Fail("UDP Test Case Failed!!!");
      }
      Console.WriteLine("Test UDP Connection finished.\n");

      Console.WriteLine("UDP Connections Test Case Passed!!!");

    }
  }
}
