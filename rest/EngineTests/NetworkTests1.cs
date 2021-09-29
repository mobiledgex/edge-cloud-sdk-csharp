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
using DistributedMatchEngine;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Net;

namespace EngineTests
{
  public class NetworkTests1
  {
    // Test to a staging server:
    const string dmeHost = "eu-stage." + MatchingEngine.baseDmeHost;

    const string orgName = "MobiledgeX";
    const string appName = "HttpEcho";
    const string appVers = "20191204";
    const string connectionTestFqdn = "mextest-app-cluster.fairview-main.gddt.mobiledgex.net";
    const string aWebSocketServerFqdn = "pingpong-cluster.fairview-main.gddt.mobiledgex.net"; // or, localhost.

    MatchingEngine me = null;

    class TestCarrierInfo : CarrierInfo
    {
      string CarrierInfo.GetCurrentCarrierName()
      {
        return "";
      }

      string CarrierInfo.GetMccMnc()
      {
        return "";
      }

      ulong CarrierInfo.GetCellID()
      {
        return 0;
      }

      public string GetDataNetworkPath()
      {
        return "";
      }

      public ulong GetSignalStrength()
      {
        return 0;
      }
    }

    [SetUp]
    public void Setup()
    {
      // Create a network interface abstraction, with named WiFi and Cellular interfaces.
      CarrierInfo carrierInfo = new TestCarrierInfo();
      NetInterface netInterface = new SimpleNetInterface(new MacNetworkInterfaceName());
      UniqueID uniqueIdInterface = new EmptyUniqueID();

      // pass in unknown interfaces at compile and runtime.
      me = new MatchingEngine(carrierInfo, netInterface);
    }

    public NetworkTests1()
    {
    }

    // These wont actually work in a test env. Negative test.
    public class AndroidNetworkInterfaceNameBad : NetworkInterfaceName
    {
      public AndroidNetworkInterfaceNameBad()
      {
        CELLULAR = new Regex(@"(^radio1$)|(^rmnet_data1$)");
        WIFI = new Regex(@"^wlan1$");
      }
    }

    [Test]
    public void TestInterfacesNotExist()
    {
      string nameWifi = me.GetAvailableWiFiName(new AndroidNetworkInterfaceNameBad());
      Assert.IsNull(nameWifi);

      string nameCell = me.GetAvailableCellularName(new AndroidNetworkInterfaceNameBad());
      Assert.IsNull(nameCell);
    }

    [Test]
    public void TestMacInterfacesExist()
    {
      // Using the Mac Interface, where this test might run...
      string nameWifi = me.GetAvailableWiFiName(new MacNetworkInterfaceName());
      Assert.NotNull(nameWifi);
      Assert.AreEqual(nameWifi, "en0");

      string nameCell = me.GetAvailableCellularName(new MacNetworkInterfaceName());
      Assert.NotNull(nameCell);
      Assert.AreEqual(nameCell, "en0");
    }

    [Test]
    public void TestMacInterfacesEndPointsExist()
    {
      IPEndPoint endpoint = me.GetIPEndPointByName("lo0");
      Assert.NotNull(endpoint);
      Console.WriteLine("EndPoint IPv4: " + endpoint);

      IPEndPoint endpointv6 = me.GetIPEndPointByName("lo0", 123, System.Net.Sockets.AddressFamily.InterNetworkV6);
      Assert.NotNull(endpointv6);
      Console.WriteLine("EndPoint IPv6: " + endpointv6);

      endpoint = me.GetIPEndPointByHostName("127.0.0.1");
      Assert.NotNull(endpoint);
      Console.WriteLine("EndPoint: " + endpoint);
    }

    [Test]
    public void TestWindowsInterfacesExist()
    {
      // Using the Windows Interface, where this test might run...
      // GetAvailableWiFiName only passes on Windows machine
      string nameWifi = me.GetAvailableWiFiName(new Windows10NetworkInterfaceName());
      Assert.NotNull(nameWifi);
      Assert.AreEqual(nameWifi, "Ethernet");

      string nameCell = me.GetAvailableCellularName(new Windows10NetworkInterfaceName());
      Assert.NotNull(nameCell);
      Assert.AreEqual(nameCell, "Ethernet");


      var windowsInterfaceNames = new Windows10NetworkInterfaceName();
      Assert.True(windowsInterfaceNames.CELLULAR.IsMatch("Wi-Fi"));
      Assert.True(windowsInterfaceNames.CELLULAR.IsMatch("WiFi"));
      Assert.True(windowsInterfaceNames.CELLULAR.IsMatch("WiFi 4"));
    }

    [Test]
    public void TestWindowsInterfacesIPEndPointsExist()
    {
      IPEndPoint endpoint = me.GetIPEndPointByName("localhost");
      Assert.NotNull(endpoint);
      Console.WriteLine("EndPoint IPv4: " + endpoint);

      IPEndPoint endpointv6 = me.GetIPEndPointByName("localhost", 123, System.Net.Sockets.AddressFamily.InterNetworkV6);
      Assert.NotNull(endpointv6);
      Console.WriteLine("EndPoint IPv6: " + endpointv6);

      endpoint = me.GetIPEndPointByHostName("127.0.0.1");
      Assert.NotNull(endpoint);
      Console.WriteLine("EndPoint: " + endpoint);
    }
  }

}
