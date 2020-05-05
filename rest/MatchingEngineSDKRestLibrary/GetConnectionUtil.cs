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

namespace DistributedMatchEngine
{ 
  /// <summary>
  /// Defines The Network Interface Name
  /// <para>
  /// (Network Interface is the point of interconnection between a device and (public or private) cloud)
  /// </para>
  /// </summary>
  public class NetworkInterfaceName
  {
    public string CELLULAR = null;
    public string WIFI = null;
  }

  // Some known network interface profiles:
  
  /// <summary>
  /// Defines the IOS Netwotk Interface
  /// <para>
  /// (Network Interface is the point of interconnection between a device and (public or private) cloud)
  /// </para>
  /// </summary>
  public class IOSNetworkInterfaceName : NetworkInterfaceName
  {
    /// <summary>
    /// Constructor for IOS Network Interface Cellular and Wifi
    /// </summary>
    public IOSNetworkInterfaceName()
    {
      CELLULAR = "pdp_ip0";
      WIFI = "en0";
    }
  }
  /// <summary>
  /// Defines the Android Netwotk Interface
  /// <para>
  /// (Network Interface is the point of interconnection between a device and (public or private) cloud)
  /// </para>
  /// </summary>
  public class AndroidNetworkInterfaceName : NetworkInterfaceName
  {
    /// <summary>
    /// Constructor for Android Network Interface Cellular and Wifi
    /// </summary>
    public AndroidNetworkInterfaceName()
    {
      CELLULAR = "radio0"; // rmnet_data0 for some older version of Android
      WIFI = "wlan0";
    }
  }
  /// <summary>
  /// Defines the MacOS Netwotk Interface
  /// <para>
  /// (Network Interface is the point of interconnection between a device and (public or private) cloud)
  /// </para>
  /// </summary>
  public class MacNetworkInterfaceName : NetworkInterfaceName
  {
    public MacNetworkInterfaceName()
    {
      CELLULAR = "en0";
      WIFI = "en0";
    }
  }

  public partial class MatchingEngine
  {
    /// <summary>
    /// Function for Validating the Service Certificate
    /// <para>Server Certificates are used for identifying Servers</para>
    /// <para>Certificates are being used to serve the rationale of encrypting and decrypting the content.</para>
    /// </summary>
    /// <returns> Boolean value representing validation result </returns>
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
    
    /// <summary>
    /// Validate Public Port ,Used in MatchingEngine.CreateURL
    /// </summary>
    /// <param name="findCloudletReply"> FindCloudletReply Object</param>
    /// <param name="appPort">AppPort Object</param>
    /// <param name="proto">Protocol (HTTP,TCP,UDP)</param>
    /// <param name="portNum">Port Number</param>
    /// <returns>Returns the AppPort </returns>
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
        if (IsInPortRange(appPort, portNum) && AppPortIsEqual(aPort, appPort))
        {
          found = aPort;
        }
      }
      return found;
    }

    /// <summary>
    ///  Create a L7Path URL using findCloudletReply, AppPort Object and desired port number, (L7 is the Application layer)
    /// </summary>
    /// <param name="findCloudletReply"> Reply from MatchingEngine.FindCloudlet</param>
    /// <param name="appPort">App Port Object</param>
    /// <param name="portNum">Desired Port Number</param>
    /// <returns> Returns url if port was found and null if failed</returns>
    public static string CreateUrl(FindCloudletReply findCloudletReply, AppPort appPort, int portNum)
    {
      int aPortNum = portNum <= 0 ? appPort.public_port : portNum;
      AppPort foundPort = ValidatePublicPort(findCloudletReply, appPort, LProto.L_PROTO_TCP, aPortNum);
      if (foundPort == null)
      {
        return null;
      }
      string url = "http://" +
              appPort.fqdn_prefix +
              findCloudletReply.fqdn +
              ":" +
              aPortNum +
              appPort.path_prefix;

      return url;
    }
    
    /// <summary>
    /// Checks if the specified port is within the range of public_port and end_port
    /// </summary>
    /// <param name="appPort">AppPort Object</param>
    /// <param name="port"> Desired Port Number</param>
    /// <returns>boolean value</returns>
    private static bool IsInPortRange(AppPort appPort, int port)
    {
      // Checks if range exists -> if not, check if specified port equals public port
      if (appPort.end_port == 0 || appPort.end_port < appPort.public_port)
      {
        return port == appPort.public_port;
      }
      return (port >= appPort.public_port && port <= appPort.end_port);
    }
    
    /// <summary>
    /// Gets IP Address of the specified network interface
    /// </summary>
    /// <param name="port"> port number </param>
    /// <returns>IPEndPoint Object (IPEndPoint class is part of System.Net.EndPoint)</returns>
    private IPEndPoint GetLocalIP(int port = 0)
    {
      if (netInterface == null)
      {
        throw new GetConnectionException("Have not integrated NetworkInterface");
      }

      string host;
      if (useOnlyWifi || !netInterface.HasCellular())
      {
        host = netInterface.GetIPAddress(netInterface.GetNetworkInterfaceName().WIFI);
      }
      else
      {
        host = netInterface.GetIPAddress(netInterface.GetNetworkInterfaceName().CELLULAR);
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
    
    /// <summary>
    /// Gets AppPorts By Protocol using FindCloudletReply and a specific Protocol
    /// </summary>
    /// <param name="reply"> FindCloudletReply Object</param>
    /// <param name="proto"> Network Protocol (HTTP,UDP,TCP) </param>
    /// <returns> Dictionary of Application ports {Internal port, Related AppPort Object}</returns>
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
    
    /// <summary>
    /// Gets AppPorts for TCP Protocol using FindCloudletReply 
    /// </summary>
    /// <param name="reply"> FindCloudletReply Object </param>
    /// <returns> Dictionary of Application ports {Internal port number, Related AppPort Object}</returns>
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
    /// <summary>
    /// Gets AppPorts for UDP Protocol using FindCloudletReply 
    /// </summary>
    /// <param name="reply"> FindCloudletReply Object </param>
    /// <returns> Dictionary of Application ports {Internal port number, Related AppPort Object}</returns>
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
    /// <summary>
    /// Gets AppPorts for HTTP Protocol using FindCloudletReply 
    /// </summary>
    /// <param name="reply"> FindCloudletReply Object </param>
    /// <returns> Dictionary of Application ports {Internal port number, Related AppPort Object}</returns>
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
