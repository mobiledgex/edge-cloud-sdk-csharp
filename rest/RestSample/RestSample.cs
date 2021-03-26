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
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.IO;

using DistributedMatchEngine;
using DistributedMatchEngine.Mel;
using DistributedMatchEngine.PerformanceMetrics;
using static DistributedMatchEngine.PerformanceMetrics.NetTest;

namespace RestSample
{
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

  class DummyDeviceInfo : DeviceInfo
  {
    Dictionary<string, string> DeviceInfo.GetDeviceInfo()
    {
      Dictionary<string, string> dict = new Dictionary<string, string>();
      dict["one"] = "ONE";
      dict["two"] = "TWO";
      return dict;
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
      return "26201";
    }

    public string GetMccMnc()
    {
      return "26201";
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

  class Program
  {
    static string carrierName = "";
    static string orgName = "MobiledgeX-Samples";
    static string appName = "sdktest";
    static string appVers = "9.0";

    // For SDK purposes only, this allows continued operation against default app insts.
    // A real app will get exceptions, and need to skip the DME, and fallback to public cloud.
    static string fallbackDmeHost = "wifi.dme.mobiledgex.net";

    static Timestamp createTimestamp(int futureSeconds)
    {
      long ticks = DateTime.Now.Ticks;
      long sec = ticks / TimeSpan.TicksPerSecond; // Truncates.
      long remainderTicks = ticks - (sec * TimeSpan.TicksPerSecond);
      int nanos = (int)(remainderTicks / TimeSpan.TicksPerMillisecond) * 1000000;

      var timestamp = new Timestamp
      {
        seconds = (sec + futureSeconds).ToString(),
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
          gps_location = new Loc
          {
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

    //! [nettestexample]
    static async Task NetTest(MatchingEngine matchingEngine)
    {
      // Init NetTest
      var netTest = new NetTest(matchingEngine);

      // Register and FindCloudlet to find appinst to test
      var registerClientRequest = matchingEngine.CreateRegisterClientRequest(orgName, appName, appVers);
      var registerClientReply = await matchingEngine.RegisterClient(registerClientRequest);
      if (registerClientReply == null)
      {
        Console.WriteLine("Nothing returned for reply");
      }
      var loc = await Util.GetLocationFromDevice();
      var findCloudletRequest = matchingEngine.CreateFindCloudletRequest(loc, carrierName);
      var findCloudletReply = await matchingEngine.FindCloudlet(findCloudletRequest);

      // Get information about appinst found in FindCloudletReply
      // Create a Site from from our found appinst
      // Enqueue site to NetTest
      Dictionary<int, AppPort> appPorts = matchingEngine.GetTCPAppPorts(findCloudletReply);
      int port = findCloudletReply.ports[0].public_port; // We happen to know it's the first one.
      AppPort appPort = appPorts[port];
      Site site1 = new Site { host = appPort.fqdn_prefix + findCloudletReply.fqdn, port = port, testType = TestType.CONNECT };
      netTest.sites.Enqueue(site1);

      // Create local test site
      // Enqueue site to NetTest
      /*
      Site site2 = new Site
      {
        host = "localhost",
        port = 59925,
        testType = TestType.CONNECT
      };
      netTest.sites.Enqueue(site2);
      */

      // Run 10 latency tests for each site. This will return an ordered array of Sites from fastest to slowest
      var sites = await netTest.RunNetTest(10);

      // If you want to use your own threading model or control when sites are tested, use the following instead of RunNetTest:
      /*
      int numTests = 10;
      for (int i = 0; i < numTests; i++)
      {
        await netTest.TestSite(site1);
      }
      Console.WriteLine("Site: " + site1.host + ", Average latency: " + site1.average + ", Std of latency: " + site1.stddev);
      */

      // Print out latency information for each site
      foreach (Site site in sites)
      {
         Console.WriteLine("Site: " + site.host + ", Average latency: " + site.average + ", Std of latency: " + site.stddev);
      }
    }
    //! [nettestexample]

    async static Task Main(string[] args)
    {
      try
      {
        Console.WriteLine("MobiledgeX RestSample!");

        //! [meconstructorexample]
        MatchingEngine me = new MatchingEngine(new DummyCarrierInfo(), new SimpleNetInterface(new MacNetworkInterfaceName()), new DummyUniqueID(), new DummyDeviceInfo());
        //! [meconstructorexample]

        me.SetMelMessaging(new TestMelMessaging());
        me.SetTimeout(15000);
        me.useSSL = true;
        fallbackDmeHost = me.GenerateDmeHostAddress(); // WiFi if not overridden in test sample.
        // Set SSL.

        await NetTest(me);

        // Start location task. This is for test use only. The source of the
        // location in an Unity application should be from an application context
        // LocationService.
        var locTask = Util.GetLocationFromDevice();

        //! [createregisterexample]
        var registerClientRequest = me.CreateRegisterClientRequest(orgName, appName, appVers);
        //! [createregisterexample]

        // APIs depend on Register client to complete successfully:
        RegisterClientReply registerClientReply;
        try
        {
          try
          {
            //! [registerexample]
            registerClientReply = await me.RegisterClient(registerClientRequest);
            //! [registerexample]
            Console.WriteLine("RegisterClient Reply Status: " + registerClientReply.status);
          }
          catch (DmeDnsException)
          {
            // DME doesn't exist in DNS. This is not a normal path if the SIM card is supported. Fallback to public cloud here.
            //! [registeroverloadexample]
            registerClientReply = await me.RegisterClient(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, registerClientRequest);
            //! [registeroverloadexample]

            Console.WriteLine("RegisterClient Reply Status: " + registerClientReply.status);
          }
          catch (NotImplementedException)
          {
            registerClientReply = await me.RegisterClient(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, registerClientRequest);
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
        //! [createverifylocationexample]
        var verifyLocationRequest = me.CreateVerifyLocationRequest(loc);
        //! [createverifylocationexample]

        //! [createfindcloudletexample]
        var findCloudletRequest = me.CreateFindCloudletRequest(loc, carrierName);
        //! [createfindcloudletexample]

        //! [createappinstexample]
        var appInstListRequest = me.CreateAppInstListRequest(loc, carrierName);
        //! [createappinstexample]

        // These are asynchronious calls, of independent REST APIs.

        // FindCloudlet Proximity Mode:
        try
        {
          FindCloudletReply findCloudletReply = null;
          try
          {
            //! [findcloudletexample]
            findCloudletReply = await me.FindCloudlet(findCloudletRequest);
            //! [findcloudletexample]
          }
          catch (DmeDnsException)
          {
            // DME doesn't exist in DNS. This is not a normal path if the SIM card is supported. Fallback to public cloud here.
            //! [findcloudletoverloadexample]
            findCloudletReply = await me.FindCloudlet(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, findCloudletRequest);
            //! [findcloudletoverloadexample]
          }
          catch (NotImplementedException)
          {
            findCloudletReply = await me.FindCloudlet(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, findCloudletRequest);
          }
          catch (FindCloudletException fce)
          {
            Console.WriteLine("FindCloudletException is " + fce.Message);
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
                    ", end_port: " + p.end_port);
            }
          }
        }
        catch (HttpException httpe)
        {
          Console.WriteLine("FindCloudlet Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }

        // FindCloudlet Performance Mode:
        try
        {
          FindCloudletReply findCloudletReplyPerformance = null;
          try
          {
            //! [findcloudletperformanceexample]
            findCloudletReplyPerformance = await me.FindCloudlet(findCloudletRequest, FindCloudletMode.PERFORMANCE);
            //! [findcloudletperformanceexample]
          }
          catch (DmeDnsException)
          {
            // DME doesn't exist in DNS. This is not a normal path if the SIM card is supported. Fallback to public cloud here.
            findCloudletReplyPerformance = await me.FindCloudlet(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, findCloudletRequest, FindCloudletMode.PERFORMANCE);
          }
          catch (NotImplementedException)
          {
            findCloudletReplyPerformance = await me.FindCloudlet(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, findCloudletRequest, FindCloudletMode.PERFORMANCE);
          }
          catch (FindCloudletException fce)
          {
            Console.WriteLine("FindCloudletPerformanceException is " + fce.Message);
          }

          Console.WriteLine("FindCloudletPerformance Reply: " + findCloudletReplyPerformance);

          if (findCloudletReplyPerformance != null)
          {
            Console.WriteLine("FindCloudletPerformance Reply Status: " + findCloudletReplyPerformance.status);
            Console.WriteLine("FindCloudletPerformance:" +
                    " ver: " + findCloudletReplyPerformance.ver +
                    ", fqdn: " + findCloudletReplyPerformance.fqdn +
                    ", cloudlet_location: " +
                    " long: " + findCloudletReplyPerformance.cloudlet_location.longitude +
                    ", lat: " + findCloudletReplyPerformance.cloudlet_location.latitude);
            // App Ports:
            foreach (AppPort p in findCloudletReplyPerformance.ports)
            {
              Console.WriteLine("Port: fqdn_prefix: " + p.fqdn_prefix +
                    ", protocol: " + p.proto +
                    ", public_port: " + p.public_port +
                    ", internal_port: " + p.internal_port +
                    ", end_port: " + p.end_port);
            }
          }
        }
        catch (HttpException httpe)
        {
          Console.WriteLine("FindCloudletPerformance Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }

        // Verify Location:
        try
        {
          Console.WriteLine("VerifyLocation() may timeout, due to reachability of carrier verification servers from your network.");
          VerifyLocationReply verifyLocationReply = null;
          try
          {
            //! [verifylocationexample]
            verifyLocationReply = await me.VerifyLocation(verifyLocationRequest);
            //! [verifylocationexample]
          }
          catch (DmeDnsException)
          {
            //! [verifylocationoverloadexample]
            verifyLocationReply = await me.VerifyLocation(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, verifyLocationRequest);
            //! [verifylocationoverloadexample]
          }
          catch (NotImplementedException)
          {
            verifyLocationReply = await me.VerifyLocation(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, verifyLocationRequest);
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

        // GetAppInstList:
        try
        {
          AppInstListReply appInstListReply;
          try
          {
            //! [appinstlistexample]
            appInstListReply = await me.GetAppInstList(appInstListRequest);
            //! [appinstlistexample]
          }
          catch (DmeDnsException)
          {
            //! [appinstlistoverloadexample]
            appInstListReply = await me.GetAppInstList(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, appInstListRequest);
            //! [appinstlistoverloadexample]
          }
          catch (NotImplementedException)
          {
            appInstListReply = await me.GetAppInstList(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, appInstListRequest);
          }

          if (appInstListReply == null)
          {
            Console.WriteLine("Unable to GetAppInstList. Reply is null");
          }
          else if (appInstListReply.status != AppInstListReply.AIStatus.AI_SUCCESS)
          {
            Console.WriteLine("GetAppInstList failed. Status is " + appInstListReply.status);
          }
          else
          {
            Console.WriteLine("AppInstListReply: " + appInstListReply);
            CloudletLocation[] cloudlets = appInstListReply.cloudlets;

            if (cloudlets.Length == 0)
            {
              Console.WriteLine("AppInstListReply has no cloudlets");
            }
            else
            {
              CloudletLocation cloudlet = cloudlets[0];
              Appinstance[] appinstances = cloudlet.appinstances;

              if (cloudlets.Length == 0)
              {
                Console.WriteLine("Cloudlet has no appinstances");
              }
              else
              {
                Appinstance appinstance = appinstances[0];
                Console.WriteLine("Appinstance app_name: " + appinstance.app_name);
                Console.WriteLine("Appinstance app_vers: " + appinstance.app_vers);
                Console.WriteLine("Appinstance fqdn: " + appinstance.fqdn);
                Console.WriteLine("Appinstance org_name: " + appinstance.org_name);

                if (appinstance.ports.Length == 0)
                {
                  Console.WriteLine("Appinstance has not AppPorts");
                }
              }
            }
          }
        }
        catch (HttpException httpe)
        {
          Console.WriteLine("GetAppInstList Exception: " + httpe.Message + ", HTTP StatusCode: " + httpe.HttpStatusCode + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
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

          //! [createqospositionexample]
          var qosPositionRequest = me.CreateQosPositionRequest(requestList, 0, null);
          //! [createqospositionexample]

          QosPositionKpiStream qosReplyStream = null;
          try
          {
            //! [getqospositionexample]
            qosReplyStream = await me.GetQosPositionKpi(qosPositionRequest);
            //! [getqospositionexample]
          }
          catch (DmeDnsException)
          {
            //! [getqospositionoverloadexample]
            qosReplyStream = await me.GetQosPositionKpi(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, qosPositionRequest);
            //! [getqospositionoverloadexample]
          }
          catch (NotImplementedException)
          { 
            qosReplyStream = await me.GetQosPositionKpi(fallbackDmeHost, MatchingEngine.defaultDmeRestPort, qosPositionRequest);
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
        if (e.InnerException != null)
        {
          Console.WriteLine(e.InnerException.Message + "\n" + e.InnerException.StackTrace);
        }
      }
    }
  };
}
