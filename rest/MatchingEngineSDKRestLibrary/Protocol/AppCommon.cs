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
using System.Collections;
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
   * Latency Sample
   */
  [DataContract]
  public class Sample
  {
    //! latency value
    [DataMember]
    public double value;
    //! gps location
    [DataMember]
    Loc loc;
    //! session cookie to differentiate clients
    [DataMember]
    public string session_cookie;
    //! LTE, 5G, etc.
    [DataMember]
    public string data_network_type;
    [DataMember]
    Timestamp timestamp;

    //! Optional. Vendor specific data
    public Dictionary<string, string> tags;
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  }

  /*!
   * Latency
   */
  [DataContract]
  public class Latency
  {
    [DataMember]
    public double avg;
    [DataMember]
    public double min;
    [DataMember]
    public double max;
    //! Square root of unbiased variance
    [DataMember]
    public double std_dev;
    //! Unbiased variance
    [DataMember]
    public double variance;
    [DataMember]
    public ulong num_samples;
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
    [EnumMember]
    L_PROTO_UNKNOWN = 0,
    //! TCP (L4) protocol
    [EnumMember]
    L_PROTO_TCP = 1,
    //! UDP (L4) protocol
    [EnumMember]
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
    [EnumMember]
    ID_UNDEFINED = 0,
    IMEI = 1,
    [EnumMember]
    MSISDN = 2,
    [EnumMember]
    IPADDR = 3
  }

  //! Health check status
  //
  // Health check status gets set by external, or rootLB health check
  public enum HealthCheck
  {
    //! Health Check is unknown
    [EnumMember]
    HEALTH_CHECK_UNKNOWN = 0,
    //! Health Check failure due to RootLB being offline
    [EnumMember]
    HEALTH_CHECK_FAIL_ROOTLB_OFFLINE = 1,
    //! Health Check failure due to Backend server being unavailable
    [EnumMember]
    HEALTH_CHECK_FAIL_SERVER_FAIL = 2,
    //! Health Check is ok
    [EnumMember]
    HEALTH_CHECK_OK = 3
  }

  //! CloudletState is the state of the Cloudlet.
  public enum CloudletState
  {
    //! Unknown
    [EnumMember]
    CLOUDLET_STATE_UNKNOWN = 0,
    //! Create/Delete/Update encountered errors (see Errors field of CloudletInfo)
    [EnumMember]
    CLOUDLET_STATE_ERRORS = 1,
    //! Cloudlet is created and ready
    [EnumMember]
    CLOUDLET_STATE_READY = 2,
    //! Cloudlet is offline (unreachable)
    [EnumMember]
    CLOUDLET_STATE_OFFLINE = 3,
    //! Cloudlet is not present
    [EnumMember]
    CLOUDLET_STATE_NOT_PRESENT = 4,
    //! Cloudlet is initializing
    [EnumMember]
    CLOUDLET_STATE_INIT = 5,
    //! Cloudlet is upgrading
    [EnumMember]
    CLOUDLET_STATE_UPGRADE = 6,
    //! Cloudlet needs data to synchronize
    [EnumMember]
    CLOUDLET_STATE_NEED_SYNC = 7,
  }

  //! Cloudlet Maintenance States
  //
  // Maintenance allows for planned downtimes of Cloudlets.
  // These states involve message exchanges between the Controller,
  // the AutoProv service, and the CRM. Certain states are only set
  // by certain actors.
  public enum MaintenanceState
  {
    //! Normal operational state
    [EnumMember]
    NORMAL_OPERATION = 0,
    //! Request start of maintenance
    [EnumMember]
    MAINTENANCE_START = 1,
    //! Trigger failover for any HA AppInsts
    [EnumMember]
    FAILOVER_REQUESTED = 2,
    //! Failover done
    FAILOVER_DONE = 3,
    //! Some errors encountered during maintenance failover
    [EnumMember]
    FAILOVER_ERROR = 4,
    //! Request start of maintenance without AutoProv failover
    [EnumMember]
    MAINTENANCE_START_NO_FAILOVER = 5,
    //! Request CRM to transition to maintenance
    [EnumMember]
    CRM_REQUESTED = 6,
    //! CRM request done and under maintenance
    [EnumMember]
    CRM_UNDER_MAINTENANCE = 7,
    //! CRM failed to go into maintenance
    [EnumMember]
    CRM_ERROR = 8,
    //! Request CRM to transition to normal operation
    [EnumMember]
    NORMAL_OPERATION_INIT = 9,
    //! Under maintenance
    [EnumMember]
    UNDER_MAINTENANCE = 31
  }

  /*!
   * Status of MatchingEngine API replies
   * \ingroup classes_datastructs
   */
  public enum ReplyStatus
  {
    [EnumMember]
    RS_UNDEFINED = 0,
    [EnumMember]
    RS_SUCCESS = 1,
    [EnumMember]
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

  /* For serialization without generics on IL2CPP and/or IOS */
  // Vendor specific data
  public class Tag
  {
    // Get and set won't be called by the serializer (who does reflection), so this is manual.
    static public Hashtable DictionaryToHashtable(Dictionary<string, string> tags)
    {
      Log.D("XXX DictionaryToHashtable: " + tags);
      if (tags == null || tags.Count == 0)
      {
        Log.D("DictionaryToHashtable: Nothing: " + tags);
        return null;
      }
      Hashtable htags = new Hashtable();
      foreach (KeyValuePair<string, string> entry in tags)
      {
        if (entry.Value == null)
        {
          continue;
        }
        htags.Add(entry.Key, entry.Value);
        Log.D("XXX Key: " + entry.Key + ", Value: " + htags[entry.Key]);
      }
      return htags;
    }

    static public Dictionary<string, string> HashtableToDictionary(Hashtable htags)
    {
      Dictionary<string, string> tags = new Dictionary<string, string>();
      if (htags == null || htags.Count == 0)
      {
        return null;
      }
      foreach (var key in htags.Keys)
      {
        if (htags[key] == null)
        {
          continue;
        }
        tags[key.ToString()] = htags[key].ToString();
        Log.D("Key: " + key + ", Value: " + tags[key.ToString()]);
      }

      return tags;
    }
  }
}