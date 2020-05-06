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
using System.Runtime.Serialization;

namespace DistributedMatchEngine
{
  /// <summary>
  /// Tags stored in the Sim Card (Vendor specific data)
  /// </summary>
  // Vendor specific data
  [DataContract]
  public class Tag
  {
    [DataMember]
    public string type;
    [DataMember]
    public string data;
  }
  /// <summary>
  /// Loc Class represents gps location
  /// </summary>
  [DataContract]
  public class Loc
  {
    /// <summary>
    /// Latitude (GPS Coordinate)
    /// </summary>
    [DataMember]
    public double latitude;
    /// <summary>
    /// Longitude (GPS Coordinate)
    /// </summary>
    [DataMember]
    public double longitude;
    /// <summary>
    /// Accuracy refers to the degree of closeness of the indicated readings to the actual position.
    /// <para>Horizontal Accuracy of 3 meters considered to be very good</para>
    /// </summary>
    [DataMember]
    public double horizontal_accuracy;
    /// <summary>
    /// Accuracy refers to the degree of closeness of the indicated readings to the actual position.
    /// <para>Vertical Accuracy of 5 meters considered to be very good</para>
    /// </summary>
    [DataMember]
    public double vertical_accuracy;
    /// <summary>
    /// Altitude (GPS Coordinate)
    /// </summary>
    [DataMember]
    public double altitude;
    /// <summary>
    /// For moving devices, Course is the intended direction of travel
    /// </summary>
    [DataMember]
    public double course;
    /// <summary>
    /// Speed
    /// </summary>
    [DataMember]
    public double speed;
    /// <summary>
    /// A timestamp is a sequence of characters or encoded information identifying when a certain event occurred.
    /// </summary>
    [DataMember]
    public Timestamp timestamp;
  }
    
  /// <summary>
  /// Enum to hold (protocol buffers) Transport Protocl references
  /// <para>L_PROTO_UNKNOWN = 0</para>
  /// <para> For TCP (Layer 4): L_PROTO_TCP = 1</para>
  /// <para> For UDP (Layer 4) : L_PROTO_TCP = 2</para>
  /// <para> For HTTP (Layer 7) : L_PROTO_TCP = 3</para>
  /// </summary>
  public enum LProto
  {
    // Unknown protocol
    L_PROTO_UNKNOWN = 0,
    // TCP (L4) protocol
    L_PROTO_TCP = 1,
    // UDP (L4) protocol
    L_PROTO_UDP = 2,
    // HTTP (L7 tcp) protocol
    L_PROTO_HTTP = 3
  }
  
  /// <summary>
  /// Application port class 
  /// </summary>
  [DataContract]
  public class AppPort
  {
    // TCP (L4), UDP (L4), or HTTP (L7) protocol
    public LProto proto = LProto.L_PROTO_UNKNOWN;

    [DataMember(Name = "proto")]
    private string proto_string
    {
      get
      {
        return proto.ToString();
      }
      set
      {
        try
        {
          proto = (LProto)Enum.Parse(typeof(LProto), value);
        }
        catch
        {
          proto = LProto.L_PROTO_UNKNOWN;
        }
      }
    }
      
    /// <summary>
    /// internal_port refers to the Container port (Specified in Dockerfile)
    /// </summary>
    [DataMember]
    public Int32 internal_port;
    /// <summary>
    /// Public facing port TCP/UDP (may be mapped on shared LB reverse proxy)
    /// </summary>
    [DataMember]
    public Int32 public_port;
    /// <summary>
    /// Public facing path prefix for HTTP L7 access.
    /// </summary>
    [DataMember]
    public string path_prefix;
    /// <summary>
    /// FQDN prefix to prepend to base FQDN in FindCloudlet response. May be empty. (FQDN = Fully Qualified Domain Name)
    /// </summary>
    [DataMember]
    public string fqdn_prefix;
    /// <summary>
    /// A non-zero end port indicates this is a port range from public port to end port, inclusive.
    /// </summary>
    [DataMember]
    public Int32 end_port;
    /// <summary>
    /// TLS termination for this port
    /// </summary>
    [DataMember]
    public bool tls;
  }
    
  /// <summary>
  /// Enum for holding Cell ID types
  /// <para>ID_UNDEFINED = 0</para>
  /// <para>IMEI = 1</para>
  /// <para>The International Mobile station Equipment Identity  used to identify a device that uses terrestrial cellular networks.</para>
  /// <para>MSISDN = 2</para>
  /// <para>Mobile Station International Subscriber Directory Number used to identify a mobile phone number internationally</para>
  /// <para>IPADDR </para>
  /// <para>IP Adress</para>
  /// </summary>
  public enum IDTypes
  {
    ID_UNDEFINED = 0,
    IMEI = 1,
    MSISDN = 2,
    IPADDR = 3
  }
  
  /// <summary>
  /// Enum to hold ReplyStatus type
  /// <para>Undefiend RS_UNDEFINED = 0</para>
  /// <para>Success RS_SUCCESS = 1</para>
  /// <para>Failure RS_FAIL = 2</para>
  /// </summary>
  public enum ReplyStatus
  {
    RS_UNDEFINED = 0,
    RS_SUCCESS = 1,
    RS_FAIL = 2
  }
  
  /// <summary>
  /// A timestamp is a sequence of characters or encoded information identifying when a certain event occurred.
  /// </summary>
  public class Timestamp
  {
    public string seconds;
    public Int32 nanos;
  }
}
