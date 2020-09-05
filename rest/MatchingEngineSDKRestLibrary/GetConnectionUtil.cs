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
using System.Text.RegularExpressions;

namespace DistributedMatchEngine
{ 

  /*!
   * Base Network Interface Name. Aliases for Cellular and Wifi interfaces.
   * Implement this class based on platform/device
   * \ingroup classes_integration
   */
  public class NetworkInterfaceName
  {
    public Regex CELLULAR = null;
    public Regex WIFI = null;
  }

  // Some known network interface profiles:
  /*!
   * IOS Network Interface aliases for Cellular and Wifi interfaces.
   * Use this to instantiate NetInterface if using IOS device
   * \ingroup classes_integration
   */
  public class IOSNetworkInterfaceName : NetworkInterfaceName
  {
    public IOSNetworkInterfaceName()
    {
      CELLULAR = new Regex(@"^pdp_ip0$");
      WIFI = new Regex(@"^en0$");
    }
  }

  /*!
   * Android Network Interface aliases for Cellular and Wifi interfaces.
   * Use this to instantiate NetInterface if using Android device
   * \ingroup classes_integration
   */
  public class AndroidNetworkInterfaceName : NetworkInterfaceName
  {
    public AndroidNetworkInterfaceName()
    {
      // Profile cellular names are rather dynamic. Callbacks don't work fast enough.
      CELLULAR = new Regex(@"(^radio\d+$)|(^rmnet_data\d+$)");
      WIFI = new Regex(@"^wlan0$");
    }
  }

  /*!
   * Mac Network Interface aliases for Cellular and Wifi interfaces.
   * Use this to instantiate NetInterface if using Mac device
   * \ingroup classes_integration
   */
  public class MacNetworkInterfaceName : NetworkInterfaceName
  {
    public MacNetworkInterfaceName()
    {
      //! en0 and en1 should be Wifi and Ethernet or vice versa
      CELLULAR = new Regex(@"^en\d+$");
      WIFI = new Regex(@"^en\d+$");
    }
  }

  /*!
   * Linux Network Interface aliases for Cellular and Wifi interfaces.
   * Use this to instantiate NetInterface if using Linux device
   * \ingroup classes_integration
   */
  public class LinuxNetworkInterfaceName : NetworkInterfaceName
  {
    public LinuxNetworkInterfaceName()
    {
      CELLULAR = new Regex("(^eth0$)|(^wlan0$)");
      WIFI = new Regex("(^eth0$)|(^wlan0$)");
    }
  }

  /*!
   * Windows10 Network Interface aliases for Cellular and Wifi interfaces.
   * Use this to instantiate NetInterface if using Windows10 device
   * \ingroup classes_integration
   */
  public class Windows10NetworkInterfaceName : NetworkInterfaceName
  {
    public Windows10NetworkInterfaceName()
    {
      CELLULAR = new Regex("(^Ethernet adapter Ethernet$)|(^Wireless LAN adapter Wi-Fi$)|(^Ethernet$)|(^WiFi$)");
      WIFI = new Regex("(^Ethernet adapter Ethernet$)|(^Wireless LAN adapter Wi-Fi$)|(^Ethernet$)|(^WiFi$)");
    }
  }

  public partial class MatchingEngine
  {
    // TLS Utility Variables + Functions
    private static bool serverRequiresClientCertAuth = false;
    private static SslProtocols enabledProtocols = SslProtocols.None; // os chooses the best protocol to use
    private static X509Certificate2Collection clientCertCollection = new X509Certificate2Collection();

    /*!
     * If server requires client certificate authentication, set to true
     * This also requires uploading client certificates via AddClientCert function
     * \ingroup functions_getconnectionutils
     */
    public static void ServerRequiresClientCertificateAuthentication(bool required)
    {
      serverRequiresClientCertAuth = required;
    }

    /*!
     * Enable or disable certin SSL/TLS protocols that OS can use
     * \ingroup functions_getconnectionutils
     */
    public static void EnableSSLProtocols(SslProtocols[] protocols)
    {
      foreach (SslProtocols protocol in protocols)
      {
        enabledProtocols |= protocol;
      }
    }

    /*!
     * Upload Client Certificates to be used for client authentication
     * \ingroup functions_getconnectionutils
     */
    public static void AddClientCert(string clientCertPath)
    {
      X509Certificate2 cert = new X509Certificate2(clientCertPath);
      clientCertCollection.Add(cert);
    }

    /*!
     * Returns the L7 path of the developers app backend based on the the findCloudletReply and appPort provided.
     * The desired port number must be specified by the developer (use -1 if you want the SDK to choose a port number).
     * An L7 protocol must also be provided (eg. http, https, ws, wss). The path variable is optional and will be appended to the end of the url.
     * This function is called by L7 GetConnection functions, but can be called by developers if they are using their own communication client.
     * Example return value: https://example.com:8888
     * \ingroup functions_getconnectionutils
     * \param findCloudletReply (FindCloudletReply)
     * \param appPort (AppPort)
     * \param protocol (string): L7 protocol to be prepended to url
     * \param desiredPort (int): Optional
     * \param path (string): Optional
     * \return string
     */
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

    /*!
     * Returns the host of the developers app backend based on the findCloudletReply and appPort provided.
     * This function is called by L4 GetConnection functions, but can be called by developers if they are using their own communication client (use GetPort as well)
     * \ingroup functions_getconnectionutils
     * \param findCloudletReply (FindCloudletReply)
     * \param appPort (AppPort)
     * \return string
     */
    public string GetHost(FindCloudletReply findCloudletReply, AppPort appPort)
    {
      return appPort.fqdn_prefix + findCloudletReply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
    }

    /*!
     * Returns the port of the developers app backend service based on the appPort provided.
     * An optional desiredPort parameter is provided if the developer wants a specific port within their appPort port range (if none provided, the function will default to the public_port field in the AppPort).
     * This function is called by L4 GetConnection functions, but can be called by developers if they are using their own communication client (use GetHost as well).
     * \ingroup functions_getconnectionutils
     * \param appPort (AppPort)
     * \param desiredPort (int): Optional
     */
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

    private static string GetAvailableInterface(NetInterface netInterface, Regex interfaceNamesRegex)
    {
      string foundName = "";
      NetworkInterface[] netInterfaces = NetworkInterface.GetAllNetworkInterfaces();

      foreach (NetworkInterface iface in netInterfaces)
      {
        string iName = iface.Name;
        if (interfaceNamesRegex.IsMatch(iName))
        {
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

    /*!
     * Returns a Dictionary mapping a port that the developer specified when creating their app through MobiledgeX console to an AppPort object.
     * This AppPort object will contain relevant information necessary to connect to the desired port.
     * This object will be used in GetConnection functions.
     * \ingroup functions_getconnectionutils
     * \param reply (FindCloudletReply)
     * \param proto (LProto): Protocol of application ports desired
     * \return Dictionary<int, AppPort>
     */
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

    /*!
     * Returns a Dictionary mapping a TCP port that the developer specified when creating their app through MobiledgeX console to an AppPort object.
     * This AppPort object will contain relevant information necessary to connect to the desired port.
     * This object will be used in GetConnection functions.
     * \ingroup functions_getconnectionutils
     * \param reply (FindCloudletReply)
     * \return Dictionary<int, AppPort>
     */
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

    /*!
     * Returns a Dictionary mapping a UDP port that the developer specified when creating their app through MobiledgeX console to an AppPort object.
     * This AppPort object will contain relevant information necessary to connect to the desired port.
     * This object will be used in GetConnection functions.
     * \ingroup functions_getconnectionutils
     * \param reply (FindCloudletReply)
     * \return Dictionary<int, AppPort>
     */
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

    /*!
     * Returns a Dictionary mapping a HTTP port that the developer specified when creating their app through MobiledgeX console to an AppPort object.
     * This AppPort object will contain relevant information necessary to connect to the desired port.
     * This object will be used in GetConnection functions.
     * \ingroup functions_getconnectionutils
     * \param reply (FindCloudletReply)
     * \return Dictionary<int, AppPort>
     */
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
