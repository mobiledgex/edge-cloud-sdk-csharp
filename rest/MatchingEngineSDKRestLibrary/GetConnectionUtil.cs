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
      CELLULAR = new string[] { "radio0", "rmnet_data0" };
      WIFI = new string[] { "wlan0" };
    }
  }

  public class MacNetworkInterfaceName : NetworkInterfaceName
  {
    public MacNetworkInterfaceName()
    {
      CELLULAR = new string[] { "en0" };
      WIFI = new string[] { "en0" };
    }
  }

  public partial class MatchingEngine
  {
    private static bool ValidateServerCertificate(object sender,
      X509Certificate certificate,
      X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      if (sslPolicyErrors == SslPolicyErrors.None) return true;

      Log.E(string.Format("Certificate error: {0}", sslPolicyErrors));

      // Do not allow this client to communicate with unauthenticated servers.
      return false;
    }

    // Maybe move to the DataContract instead.
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

    public static AppPort ValidatePublicPort(FindCloudletReply findCloudletReply, AppPort appPort, LProto proto, int portNum)
    {
      AppPort found = null;
      foreach (AppPort aPort in findCloudletReply.ports)
      {
        // See if spec matches:
        if (aPort.proto != proto)
        {
          continue;
        }
        if (IsInPortRange(appPort, portNum) && AppPortIsEqual(aPort, appPort) && IsValidPort(portNum))
        {
          found = aPort;
        }
      }
      return found;
    }

    // Create a L7Path URL from an AppPort and FindCloudletReply:
    public string CreateUrl(FindCloudletReply findCloudletReply, AppPort appPort, int portNum, string protocol, string path = "")
    {
      int aPortNum = portNum <= 0 ? appPort.public_port : portNum;
      AppPort foundPort = ValidatePublicPort(findCloudletReply, appPort, LProto.L_PROTO_TCP, aPortNum);
      if (foundPort == null)
      {
        throw new GetConnectionException("Unabled to validate public port");
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
      // If desiredPort is less than or equal zero, then default to public_port
      if (desiredPort <= 0)
      {
        desiredPort = appPort.public_port;
      }

      if (!IsInPortRange(appPort, desiredPort))
      {
        throw new GetConnectionException("Desired port: " + desiredPort + " is not in AppPort range");
      }

      if (!IsValidPort(desiredPort))
      {
        throw new GetConnectionException("Desired port: " + desiredPort + " is not a valid port number");
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
      // Checks if range exists -> if not, check if specified port equals public port
      if (appPort.end_port == 0 || appPort.end_port < appPort.public_port)
      {
        return port == appPort.public_port;
      }
      return (port >= appPort.public_port && port <= appPort.end_port);
    }

    public string GetAvailableCelluarName(NetworkInterfaceName networkInterfaceName)
    {
      string foundName = "";
      NetworkInterface[] netInterfaces = NetworkInterface.GetAllNetworkInterfaces();
      foreach (NetworkInterface iface in netInterfaces)
      {
        foreach (string cellularName in networkInterfaceName.CELLULAR)
        {
          if (iface.Name.Equals(cellularName))
          {
            if (iface.OperationalStatus == OperationalStatus.Up)
            {
              foundName = cellularName;
              return foundName;
            };
          }
        }
      }
      return foundName;
    }

    public string GetAvailableWiFiName(NetworkInterfaceName networkInterfaceName)
    {
      string foundName = "";
      NetworkInterface[] netInterfaces = NetworkInterface.GetAllNetworkInterfaces();
      foreach (NetworkInterface iface in netInterfaces)
      {
        foreach (string wifiName in networkInterfaceName.WIFI)
        {
          if (iface.Name.Equals(wifiName))
          {
            if (iface.OperationalStatus == OperationalStatus.Up)
            {
              foundName = wifiName;
              return foundName;
            };
          }
        }
      }
      return foundName;
    }

    // Gets IP Address of the specified network interface
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
        host = netInterface.GetIPAddress(GetAvailableCelluarName(netInterface.GetNetworkInterfaceName()));
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
