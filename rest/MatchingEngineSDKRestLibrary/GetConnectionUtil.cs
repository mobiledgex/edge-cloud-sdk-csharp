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

using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

namespace DistributedMatchEngine
{
  class EmptyNetInterface : NetInterface
  {
    public string GetIPAddress(String netInterface)
    {
      return null;
    }

    public bool IsWifi()
    {
      return false;
    }

    public bool IsCellular()
    {
      return false;
    }
  }

  public class IOSNetworkInterface
  {
    public static string CELLULAR = "pdp_ip0";
    public static string WIFI = "en0";
  }

  public class AndroidNetworkInterface
  {
    public static string CELLULAR = "radio0"; // rmnet_data0 for some older version of Android
    public static string WIFI = "wlan0";
  }

  public class MacNetworkInterface
  {
    public static string CELLULAR = "en0";
    public static string WIFI = "en0";
  }

  public partial class MatchingEngine
  {
    private static bool ValidateServerCertificate(object sender,
      X509Certificate certificate,
      X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
      if (sslPolicyErrors == SslPolicyErrors.None) return true;

      Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

      // Do not allow this client to communicate with unauthenticated servers.
      return false;
    }

    // Checks if the specified port is within the range of public_port and end_port
    private bool IsInPortRange(AppPort appPort, int port)
    {
      // Checks if range exists -> if not, check if specified port equals public port
      if (appPort.end_port == 0 || appPort.end_port < appPort.public_port)
      {
        return port == appPort.public_port;
      }
      return (port >= appPort.public_port && port <= appPort.end_port);
    }

    // Gets IP Address of the specified network interface
    private IPEndPoint GetLocalIP(int port)
    {
      if (netInterface == null)
      {
        throw new GetConnectionException("Have not integrated NetworkInterface");
      }

      string host = "";

      if (this.os is OperatingSystem.ANDROID)
      {
        host = netInterface.GetIPAddress(AndroidNetworkInterface.CELLULAR);
      }
      else if (this.os is OperatingSystem.IOS)
      {
        host = netInterface.GetIPAddress(IOSNetworkInterface.CELLULAR);
      }
      // Unsupported or Test only interface profile:
      else if (os is OperatingSystem.OTHER && netInterface != null)
      {
        host = netInterface.GetIPAddress(MacNetworkInterface.CELLULAR);
      }

      if (host == null || host == "")
      {
        throw new GetConnectionException("Could not get Cellular interface");
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
