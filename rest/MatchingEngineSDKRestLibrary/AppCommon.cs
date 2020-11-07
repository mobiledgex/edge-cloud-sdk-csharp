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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DistributedMatchEngine
{
  /*!
   * MobiledgeX GPS Location class
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class Loc
  {
    [DataMember]
    public double latitude;
    [DataMember]
    public double longitude;
    [DataMember]
    public double horizontal_accuracy;
    [DataMember]
    public double vertical_accuracy;
    [DataMember]
    public double altitude;
    [DataMember]
    public double course;
    [DataMember]
    public double speed;
    [DataMember]
    public Timestamp timestamp;
  }

  /*!
   * LProto indicates which protocol to use for accessing an application on a particular port.
   * This is required by Kubernetes for port mapping.
   * \ingroup classes_datastructs
   */
  public enum LProto
  {
    //! Unknown protocol
    L_PROTO_UNKNOWN = 0,
    //! TCP (L4) protocol
    L_PROTO_TCP = 1,
    //! UDP (L4) protocol
    L_PROTO_UDP = 2
  }

  /*!
   * Application Port
   * AppPort describes an L4 or L7 public access port/path mapping.
   * This is used to track external to internal mappings for access via a shared load balancer or reverse proxy.
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class AppPort
  {
    //! TCP (L4), UDP (L4), or HTTP (L7) protocol
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

    //! Container port (Specified in Dockerfile)
    [DataMember]
    public Int32 internal_port;
    //! Public facing port for TCP/UDP (may be mapped on shared LB reverse proxy)
    [DataMember]
    public Int32 public_port;
    //! FQDN prefix to prepend to base FQDN in FindCloudlet response. May be empty.
    [DataMember]
    public string fqdn_prefix;
    //! A non-zero end port indicates this is a port range from public port to end port, inclusive.
    [DataMember]
    public Int32 end_port;
    [DataMember]
    //! TLS termination for this port
    public bool tls;
  }

  public enum IDTypes
  {
    ID_UNDEFINED = 0,
    IMEI = 1,
    MSISDN = 2,
    IPADDR = 3
  }

  /*!
   * Status of MatchingEngine API replies
   * \ingroup classes_datastructs
   */
  public enum ReplyStatus
  {
    RS_UNDEFINED = 0,
    RS_SUCCESS = 1,
    RS_FAIL = 2
  }

  /*!
   * Timestamp
   * \ingroup classes_datastructs
   */
  public class Timestamp
  {
    public string seconds;
    public Int32 nanos;
  }

  /* For serialization without generics on IOS */
  // Vendor specific data
  [DataContract]
  public class Tag
  {
    [DataMember]
    public string Key;
    [DataMember]
    public string Value;

    // Get and set won't be called by the serializer (who does reflection), so this is manual.
    static public Tag[] DictionaryToArrayTag(Dictionary<string, string> tags)
    {
      if (tags == null || tags.Count == 0)
      {
        return null;
      }
      int i = 0;
      Tag[] array_tags = new Tag[tags.Count];
      foreach (KeyValuePair<string, string> entry in tags)
      {
        array_tags[i] = new Tag
        {
          Key = entry.Key,
          Value = entry.Value
        };
        i++;
      }
      return array_tags;
    }

    static public Dictionary<string, string> ArrayToDictionaryTag(Tag[] array_tags)
    {
      Dictionary<string, string> tags = new Dictionary<string, string>();
      if (array_tags == null || array_tags.Length == 0)
      {
        return null;
      }
      foreach (Tag entry in array_tags)
      {
        tags[entry.Key] = entry.Value;
      }

      return tags;
    }
  }
}
