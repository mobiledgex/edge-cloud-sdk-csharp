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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DistributedMatchEngine
{
  public class Util
  {
    public Util()
    {

    }

    public static IPEndPoint GetDefaultLocalEndPointIPV4()
    {
      IPEndPoint defaultEndPoint = null;
      using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
      {
        try
        {
          socket.Connect(MatchingEngine.wifiOnlyDmeHost, 38001);
          if (socket.LocalEndPoint.AddressFamily == AddressFamily.InterNetwork)
          {
            defaultEndPoint = socket.LocalEndPoint as IPEndPoint;
          }
          else
          {
            Log.S("LocalIP is not IPV4, returning null.");
          }
        }
        catch (SocketException se)
        {
          Log.E("Exception trying to acquire endpoint: " + se.Message);
        }
      }
      return defaultEndPoint;
    }

    public static string StreamToString(Stream ms)
    {
      ms.Position = 0;
      StreamReader reader = new StreamReader(ms);
      string jsonStr = reader.ReadToEnd();
      return jsonStr;
    }

    // FIXME: This function needs per device customization.
    public async static Task<Loc> GetLocationFromDevice()
    {
      return await Task.Run(() =>
      {
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
          longitude = -122.149349,
          latitude = 37.459601,
          vertical_accuracy = 20,
          timestamp = ts
        };
        return loc;
      });
    }

    public static byte[] GetStagingCertRawBytes()
    {
      string certText = @"-----BEGIN CERTIFICATE-----
MIIFmDCCA4CgAwIBAgIQU9C87nMpOIFKYpfvOHFHFDANBgkqhkiG9w0BAQsFADBm
MQswCQYDVQQGEwJVUzEzMDEGA1UEChMqKFNUQUdJTkcpIEludGVybmV0IFNlY3Vy
aXR5IFJlc2VhcmNoIEdyb3VwMSIwIAYDVQQDExkoU1RBR0lORykgUHJldGVuZCBQ
ZWFyIFgxMB4XDTE1MDYwNDExMDQzOFoXDTM1MDYwNDExMDQzOFowZjELMAkGA1UE
BhMCVVMxMzAxBgNVBAoTKihTVEFHSU5HKSBJbnRlcm5ldCBTZWN1cml0eSBSZXNl
YXJjaCBHcm91cDEiMCAGA1UEAxMZKFNUQUdJTkcpIFByZXRlbmQgUGVhciBYMTCC
AiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoCggIBALbagEdDTa1QgGBWSYkyMhsc
ZXENOBaVRTMX1hceJENgsL0Ma49D3MilI4KS38mtkmdF6cPWnL++fgehT0FbRHZg
jOEr8UAN4jH6omjrbTD++VZneTsMVaGamQmDdFl5g1gYaigkkmx8OiCO68a4QXg4
wSyn6iDipKP8utsE+x1E28SA75HOYqpdrk4HGxuULvlr03wZGTIf/oRt2/c+dYmD
oaJhge+GOrLAEQByO7+8+vzOwpNAPEx6LW+crEEZ7eBXih6VP19sTGy3yfqK5tPt
TdXXCOQMKAp+gCj/VByhmIr+0iNDC540gtvV303WpcbwnkkLYC0Ft2cYUyHtkstO
fRcRO+K2cZozoSwVPyB8/J9RpcRK3jgnX9lujfwA/pAbP0J2UPQFxmWFRQnFjaq6
rkqbNEBgLy+kFL1NEsRbvFbKrRi5bYy2lNms2NJPZvdNQbT/2dBZKmJqxHkxCuOQ
FjhJQNeO+Njm1Z1iATS/3rts2yZlqXKsxQUzN6vNbD8KnXRMEeOXUYvbV4lqfCf8
mS14WEbSiMy87GB5S9ucSV1XUrlTG5UGcMSZOBcEUpisRPEmQWUOTWIoDQ5FOia/
GI+Ki523r2ruEmbmG37EBSBXdxIdndqrjy+QVAmCebyDx9eVEGOIpn26bW5LKeru
mJxa/CFBaKi4bRvmdJRLAgMBAAGjQjBAMA4GA1UdDwEB/wQEAwIBBjAPBgNVHRMB
Af8EBTADAQH/MB0GA1UdDgQWBBS182Xy/rAKkh/7PH3zRKCsYyXDFDANBgkqhkiG
9w0BAQsFAAOCAgEAncDZNytDbrrVe68UT6py1lfF2h6Tm2p8ro42i87WWyP2LK8Y
nLHC0hvNfWeWmjZQYBQfGC5c7aQRezak+tHLdmrNKHkn5kn+9E9LCjCaEsyIIn2j
qdHlAkepu/C3KnNtVx5tW07e5bvIjJScwkCDbP3akWQixPpRFAsnP+ULx7k0aO1x
qAeaAhQ2rgo1F58hcflgqKTXnpPM02intVfiVVkX5GXpJjK5EoQtLceyGOrkxlM/
sTPq4UrnypmsqSagWV3HcUlYtDinc+nukFk6eR4XkzXBbwKajl0YjztfrCIHOn5Q
CJL6TERVDbM/aAPly8kJ1sWGLuvvWYzMYgLzDul//rUF10gEMWaXVZV51KpS9DY/
5CunuvCXmEQJHo7kGcViT7sETn6Jz9KOhvYcXkJ7po6d93A/jy4GKPIPnsKKNEmR
xUuXY4xRdh45tMJnLTUDdC9FIU0flTeO9/vNpVA8OPU1i14vCz+MU8KX1bV3GXm/
fxlB7VBBjX9v5oUep0o/j68R/iDlCOM4VVfRa8gX6T2FU7fNdatvGro7uQzIvWof
gN9WUwCbEMBy/YhBSrXycKA8crgGg3x1mIsopn88JKwmMBa68oS7EHM9w7C4y71M
7DiA+/9Qdp9RBWJpTS9i/mDnJg1xvo8Xz49mrrgfmcAXTCJqXi24NatI3Oc=
-----END CERTIFICATE-----";
      byte[] certBytes = Encoding.ASCII.GetBytes(certText);
      return certBytes;
    }

    public static string GetHostIPV4Address(string host)
    {
      try
      {
        if (host == "" || host.Length > 255)
        {
          if (host.Length > 255)
          {
            Log.D($"{host} is more than 255 characters");
          }
          return null;
        }
        List<IPAddress> addresses = Dns.GetHostAddresses(host).ToList();
        IPAddress ipv4Address = addresses.Find(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        if(ipv4Address == null)
        {
          ipv4Address = addresses.Find(ip => ip.IsIPv4MappedToIPv6 == true);
          if(ipv4Address == null)
          {
            return null;
          }
          ipv4Address = ipv4Address.MapToIPv4();
        }
        return ipv4Address.ToString();
      }
      catch(SocketException se)
      {
        Log.E($"Error is encountered when resolving {host}, SocketException: {se.Message}");
        return null;
      }
      catch(ArgumentException ae)
      {
        Log.E($"{host} is an invalid host address, SocketException: {ae.Message}");
        return null;
      }
    }
  }
}
