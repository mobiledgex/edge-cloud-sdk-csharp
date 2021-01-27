﻿using System;
using DistributedMatchEngine;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace EngineTests
{
  public class NetworkTests1
  {
    // Test to a staging server:
    const string dmeHost = "eu-stage." + MatchingEngine.baseDmeHost;

    const string orgName = "MobiledgeX";
    const string appName = "HttpEcho";
    const string appVers = "20191204";
    const string connectionTestFqdn = "mextest-app-cluster.frankfurt-main.tdg.mobiledgex.net";
    const string aWebSocketServerFqdn = "pingpong-cluster.frankfurt-main.tdg.mobiledgex.net"; // or, localhost.

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
      Assert.NotNull(nameWifi);
      Assert.IsEmpty(nameWifi);

      string nameCell = me.GetAvailableCellularName(new AndroidNetworkInterfaceNameBad());
      Assert.NotNull(nameCell);
      Assert.IsEmpty(nameCell);
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
  }

}
