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
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace DistributedMatchEngine
{ 

  public class NetworkInterfaceName
  {
    public string[] CELLULAR = null;
    public string[] WIFI = null;
  }

  // Some known network interface profiles:
  public class IOSNetworkInterfaceName : NetworkInterfaceName
  {
    public IOSNetworkInterfaceName()
    {
      CELLULAR = new string[] { "pdp_ip0" };
      WIFI = new string[] { "en0" };
    }
  }

  public class AndroidNetworkInterfaceName : NetworkInterfaceName
  {
    public AndroidNetworkInterfaceName()
    {
      // Profile cellular names are rather dynamic. Callbacks don't work fast enough.
      CELLULAR = new string[] { "radio0", "radio1", "radio2", "radio3", "rmnet_data0", "rmnet_data1", "rmnet_data2", "rmnet_data3"};
      WIFI = new string[] { "wlan0" };
    }
  }

  public class MacNetworkInterfaceName : NetworkInterfaceName
  {
    public MacNetworkInterfaceName()
    {
      // en0 and en1 should be Wifi and Ethernet or vice versa
      CELLULAR = new string[] { "en0", "en1" };
      WIFI = new string[] { "en0", "en1" };
    }
  }

  public class LinuxNetworkInterfaceName : NetworkInterfaceName
  {
    public LinuxNetworkInterfaceName()
    {
      CELLULAR = new string[] { "eth0", "wlan0" };
      WIFI = new string[] { "eth0", "wlan0" };
    }
  }

  public class Windows10NetworkInterfaceName : NetworkInterfaceName
  {
    public Windows10NetworkInterfaceName()
    {
      CELLULAR = new string[] { "Ethernet adapter Ethernet",  "Wireless LAN adapter Wi-Fi", "Ethernet", "WiFi" };
      WIFI = new string[] { "Ethernet adapter Ethernet",  "Wireless LAN adapter Wi-Fi", "Ethernet", "WiFi" };
    }
  }

  public partial class MatchingEngine
  {
    // TLS Utility Variables + Functions
    private static bool serverRequiresClientCertAuth = false;
    private static SslProtocols enabledProtocols = SslProtocols.None; // os chooses the best protocol to use
    private static X509Certificate2Collection clientCertCollection = new X509Certificate2Collection();

    /*!
     * \ingroup functions_getconnectionutils
     */
    public static void ServerRequiresClientCertificateAuthentication(bool required)
    {
      serverRequiresClientCertAuth = required;
    }

    public static void EnableSSLProtocols(SslProtocols[] protocols)
    {
      foreach (SslProtocols protocol in protocols)
      {
        enabledProtocols |= protocol;
      }
    }

    public static void AddClientCert(string clientCertPath)
    {
      X509Certificate2 cert = new X509Certificate2(clientCertPath);
      clientCertCollection.Add(cert);
    }

    // Create a L7Path URL from an AppPort and FindCloudletReply:
    public string CreateUrl(FindCloudletReply findCloudletReply, AppPort appPort, string protocol, int desiredPort = 0, string path = "")
    {
      AppPort foundPort = ValidateAppPort(findCloudletReply, appPort);
      if (foundPort == null)
      {
        throw new GetConnectionException("Unable to validate AppPort");
      }

      int aPortNum = ValidateDesiredPort(appPort, desiredPort);
      if (aPortNum < 0)
      {
        throw new GetConnectionException("Unable to validate desired port: " + desiredPort);
      }

      string url = protocol + "://" +
              appPort.fqdn_prefix +
              findCloudletReply.fqdn +
              ":" +
              aPortNum +
              appPort.path_prefix +
              path;

      return url;
    }

    // Returns the host of the app backend based on FindCloudletReply and AppPort
    public string GetHost(FindCloudletReply findCloudletReply, AppPort appPort)
    {
      return appPort.fqdn_prefix + findCloudletReply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
    }

    // Returns the desired port for app backend service from AppPort
    public int GetPort(AppPort appPort, int desiredPort = 0)
    {
      int aPortNum = ValidateDesiredPort(appPort, desiredPort);
      if (aPortNum <= 0)
      {
        throw new GetConnectionException("Unable to validate desired port: " + desiredPort);
      }

      return aPortNum;
    }

    // Validate specified AppPort is in FindCloudletReply
    private static AppPort ValidateAppPort(FindCloudletReply findCloudletReply, AppPort appPort)
    {
      AppPort found = null;
      foreach (AppPort aPort in findCloudletReply.ports)
      {
        // See if spec matches:
        if (aPort.proto != appPort.proto)
        {
          continue;
        }
        if (AppPortIsEqual(aPort, appPort))
        {
          found = aPort;
        }
      }
      return found;
    }

    // Helper function for ValidateAppPort
    private static bool AppPortIsEqual(AppPort port1, AppPort port2)
    {
      if (port1.end_port != port2.end_port)
      {
        return false;
      }
      if (!port1.fqdn_prefix.Equals(port2.fqdn_prefix))
      {
        return false;
      }
      if (port1.internal_port != port2.internal_port)
      {
        return false;
      }
      if (port1.path_prefix != port2.path_prefix)
      {
        return false;
      }
      if (port1.proto != port2.proto)
      {
        return false;
      }
      if (!port1.public_port.Equals(port2.public_port))
      {
        return false;
      }
      return true;
    }

    // Validate the developer specified port is valid for AppPort
    private static int ValidateDesiredPort(AppPort appPort, int desiredPort)
    {
      // Check if specified port is a valid port number
      if (!IsValidPort(desiredPort))
      {
        throw new GetConnectionException("Desired port: " + desiredPort + " is not a valid port number");
      }

      // If desired port is the port specified in app definition or not specified (ie. 0), then return mapped public port
      if (desiredPort == appPort.internal_port || desiredPort == 0)
      {
        return appPort.public_port;
      }

      if (!IsInPortRange(appPort, desiredPort))
      {
        throw new GetConnectionException("Desired port: " + desiredPort + " is not in AppPort range");
      }

      return desiredPort;
    }

    private static bool IsValidPort(int port)
    {
      return (port <= 65535) && (port >= 0);
    }

    // Checks if the specified port is within the range of public_port and end_port fields in AppPort
    private static bool IsInPortRange(AppPort appPort, int port)
    {
      int mappedEndPort = appPort.public_port + (appPort.end_port - appPort.internal_port);
      // Checks if range exists -> if not, check if specified port equals public port
      if (appPort.end_port == 0 || mappedEndPort < appPort.public_port)
      {
        return port == appPort.public_port;
      }
      return (port >= appPort.public_port && port <= mappedEndPort);
    }

    private static string GetAvailableInterface(NetInterface netInterface, string[] interfaceNames)
    {
      string foundName = "";
      NetworkInterface[] netInterfaces = NetworkInterface.GetAllNetworkInterfaces();

      foreach (NetworkInterface iface in netInterfaces)
      {
        foreach (string iName in interfaceNames)
        {
          if (iface.Name.Equals(iName))
          {
            // Unreliable:
            if (iface.OperationalStatus == OperationalStatus.Up)
            {
              foundName = iName;
              return foundName;
            };

            // Check IP assignment if not in a known state:
            bool foundByIp = false;
            // First one with both IPv4 and IPv6 is a heuristic without NetTest. OperationStatus seems inaccurate or "unknown".
            if (netInterface.GetIPAddress(iName, AddressFamily.InterNetwork) != null &&
                netInterface.GetIPAddress(iName, AddressFamily.InterNetworkV6) != null)
            {
              foundByIp = true;
            }
            else if (netInterface.GetIPAddress(iName, AddressFamily.InterNetworkV6) != null)
            {
              // No-op. Every interface has IpV6.
            }
            else if (netInterface.GetIPAddress(iName, AddressFamily.InterNetwork) != null)
            {
              foundByIp = true;
            }
            if (foundByIp)
            {
              return iName;
            }
          }
        }
      }
      return foundName;
    }

    public string GetAvailableCellularName(NetworkInterfaceName networkInterfaceName)
    {
      return GetAvailableInterface(netInterface, networkInterfaceName.CELLULAR);
    }

    public string GetAvailableWiFiName(NetworkInterfaceName networkInterfaceName)
    {
      return GetAvailableInterface(netInterface, networkInterfaceName.WIFI);
    }

    // Gets IP Address of an available edge network interface, or wifi if that's available.
    private IPEndPoint GetLocalIP(int port = 0)
    {
      if (netInterface == null)
      {
        throw new GetConnectionException("Have not integrated NetworkInterface");
      }

      string host;
      if (useOnlyWifi || !netInterface.HasCellular())
      {
        host = netInterface.GetIPAddress(GetAvailableWiFiName(netInterface.GetNetworkInterfaceName()));
      }
      else
      {
        host = netInterface.GetIPAddress(GetAvailableCellularName(netInterface.GetNetworkInterfaceName()));
      }

      if (host == null || host == "")
      {
        string type = useOnlyWifi ? "Wifi" : "Cellular";
        throw new GetConnectionException("Could not get " + type + " interface");
      }
      // Gets IP address of host
      IPAddress localIP = Dns.GetHostAddresses(host)[0];
      IPEndPoint localEndPoint = new IPEndPoint(localIP, port);
      return localEndPoint;
    }

    public Dictionary<int, AppPort> GetAppPortsByProtocol(FindCloudletReply reply, LProto proto)
    {
      Dictionary<int, AppPort> appPortsByProtocol = new Dictionary<int, AppPort>();
      AppPort[] ports = reply.ports;
      foreach (AppPort port in ports)
      {
        if (port.proto == proto)
        {
          appPortsByProtocol.Add(port.internal_port, port);
        }
      }
      return appPortsByProtocol;
    }

    public Dictionary<int, AppPort> GetTCPAppPorts(FindCloudletReply reply)
    {
      Dictionary<int, AppPort> tcpAppPorts = new Dictionary<int, AppPort>();
      AppPort[] ports = reply.ports;
      foreach (AppPort port in ports)
      {
        if (port.proto == LProto.L_PROTO_TCP)
        {
          tcpAppPorts.Add(port.internal_port, port);
        }
      }
      return tcpAppPorts;
    }

    public Dictionary<int, AppPort> GetUDPAppPorts(FindCloudletReply reply)
    {
      Dictionary<int, AppPort> udpAppPorts = new Dictionary<int, AppPort>();
      AppPort[] ports = reply.ports;
      foreach (AppPort port in ports)
      {
        if (port.proto == LProto.L_PROTO_UDP)
        {
          udpAppPorts.Add(port.internal_port, port);
        }
      }
      return udpAppPorts;
    }

    public Dictionary<int, AppPort> GetHTTPAppPorts(FindCloudletReply reply)
    {
      Dictionary<int, AppPort> httpAppPorts = new Dictionary<int, AppPort>();
      AppPort[] ports = reply.ports;
      foreach (AppPort port in ports)
      {
        if (port.proto == LProto.L_PROTO_HTTP)
        {
          httpAppPorts.Add(port.internal_port, port);
        }
      }
      return httpAppPorts;
    }
  }
}
