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
using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace DistributedMatchEngine
{
  public interface NetInterface
  {
    NetworkInterfaceName GetNetworkInterfaceName();
    void SetNetworkInterfaceName(NetworkInterfaceName networkInterfaceName);
    string GetIPAddress(String netInterfaceType, AddressFamily adressFamily = AddressFamily.InterNetwork);
    bool HasWifi();
    bool HasCellular();
  }

  // Empty implementation of NetInterface that throws NotImplementedExceptions
  public class EmptyNetInterface : NetInterface
  {
    public NetworkInterfaceName GetNetworkInterfaceName()
    {
      throw new NotImplementedException("Required NetInferface interface function: GetNetworkInterfaceName() is not defined!");
    }

    public void SetNetworkInterfaceName(NetworkInterfaceName networkInterfaceName)
    {
      throw new NotImplementedException("Required NetInferface interface function: SetNetworkInterfaceName() is not defined!");
    }

    public string GetIPAddress(String netInterface, AddressFamily addressFamily = AddressFamily.InterNetwork)
    {
      throw new NotImplementedException("Required NetInterface interface function: GetIPAddress() is not defined!");
    }

    public bool HasWifi()
    {
      throw new NotImplementedException("Required NetInterface interface function: HasWifi() is not defined!");
    }

    public bool HasCellular()
    {
      throw new NotImplementedException("Required NetInterface interface function: HasCellular() is not defined!");
    }
  }

  // A generic network interface for most systems, with an interface names parameter.
  public class SimpleNetInterface : NetInterface
  {

    NetworkInterfaceName networkInterfaceName;

    public SimpleNetInterface(NetworkInterfaceName networkInterfaceName)
    {
      SetNetworkInterfaceName(networkInterfaceName);
    }

    public NetworkInterfaceName GetNetworkInterfaceName()
    {
      return networkInterfaceName;
    }
    public void SetNetworkInterfaceName(NetworkInterfaceName networkInterfaceName)
    {
      this.networkInterfaceName = networkInterfaceName;
    }
    private NetworkInterface[] GetInterfaces()
    {
      return NetworkInterface.GetAllNetworkInterfaces();
    }
    public string GetIPAddress(string sourceNetInterfaceName, AddressFamily addressfamily = AddressFamily.InterNetwork)
    {
      if (!NetworkInterface.GetIsNetworkAvailable())
      {
        return null;
      }

      NetworkInterface[] netInterfaces = GetInterfaces();

      string ipAddress = null;
      string ipAddressV4 = null;
      string ipAddressV6 = null;
      Log.S("Looking for: " + sourceNetInterfaceName + ", known Wifi: " + networkInterfaceName.WIFI + ", known Cellular: " + networkInterfaceName.CELLULAR);

      foreach (NetworkInterface iface in netInterfaces)
      {
        if (iface.Name.Equals(sourceNetInterfaceName))
        {
          IPInterfaceProperties ipifaceProperties = iface.GetIPProperties();
          foreach (UnicastIPAddressInformation ip in ipifaceProperties.UnicastAddresses)
          {
            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
            {
              ipAddressV4 = ip.Address.ToString();
            }
            if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
            {
              ipAddressV6 = ip.Address.ToString();
            }
          }

          if (addressfamily == AddressFamily.InterNetworkV6)
          {
            return ipAddressV6;
          }

          if (addressfamily == AddressFamily.InterNetwork)
          {
            return ipAddressV4;
          }
        }
      }
      return ipAddress;
    }

    public bool HasCellular()
    {
      NetworkInterface[] netInterfaces = GetInterfaces();
      foreach (NetworkInterface iface in netInterfaces)
      {
        foreach (string cellularName in networkInterfaceName.CELLULAR)
        {
          if (iface.Name.Equals(cellularName))
          {
            return iface.OperationalStatus == OperationalStatus.Up;
          }
        }
      }
      return false;
    }

    public bool HasWifi()
    {
      NetworkInterface[] netInterfaces = GetInterfaces();
      foreach (NetworkInterface iface in netInterfaces)
      {
        foreach (string wifiName in networkInterfaceName.WIFI)
        {
          if (iface.Name.Equals(wifiName))
          {
            return iface.OperationalStatus == OperationalStatus.Up;
          }
        }
      }
      return false;
    }
  }
}
