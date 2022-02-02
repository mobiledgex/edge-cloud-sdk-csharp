/**
 * Copyright 2018-2022 MobiledgeX, Inc. All rights and licenses reserved.
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
using System.Collections.Generic;
using System.Collections;

namespace DistributedMatchEngine
{
  /*!
  * QosSessionProfile
  * Defines the Quality of Service session, used for session creation and deletion.
  * \ingroup classes_datastructs
  */
  [DataContract]
  public enum QosSessionProfile
  {
    /// <summary>
    /// Specifies that no priority session should be created.
    /// </summary>
    [EnumMember]
    QOS_NO_PRIORITY = 0,
    /// <summary>
    /// Corresponds to a specific set of network parameters for low latency that will be 
    /// negotiated with the network provider in advance.
    /// </summary>
    [EnumMember]
    QOS_LOW_LATENCY = 1,
    /// <summary>
    /// Downlink traffic from AppInst to client is prioritized up to 20Mbps.
    /// </summary>
    [EnumMember]
    QOS_THROUGHPUT_DOWN_S = 2,
    /// <summary>
    /// Downlink traffic from AppInst to client is prioritized up to 50Mbps.
    /// </summary>
    [EnumMember]
    QOS_THROUGHPUT_DOWN_M = 3,
    /// <summary>
    /// Downlink traffic from AppInst to client is prioritized up to 100Mbps.
    /// </summary>
    [EnumMember]
    QOS_THROUGHPUT_DOWN_L = 4
  }

  [DataContract]
  public enum QosSessionProtocol
  {
    [EnumMember]
    TCP = 0,
    [EnumMember]
    UDP = 1,
    [EnumMember]
    ANY = 2
  }

  [DataContract]
  enum QosSessionResult
  {
    [EnumMember]
    QOS_NOT_ATTEMPTED = 0,
    [EnumMember]
    QOS_SESSION_CREATED = 1,
    [EnumMember]
    QOS_SESSION_FAILED = 2
  }

  [DataContract]
  public enum DeleteStatus
  {
    [EnumMember]
    QDEL_UNKNOWN = 0,
    [EnumMember]
    QDEL_DELETED = 1,
    [EnumMember]
    QDEL_NOT_FOUND = 2
  }

  /*!
  * QosPrioritySessionCreateRequest
  * Requests the creation of a quality of service session. 
  * Required(session_cookie, ip_user_equipment, ip_application_server, profile)
  * \ingroup classes_datastructs
  */
  [DataContract]
  public class QosPrioritySessionCreateRequest
  {
    [DataMember]
    public UInt32 ver;
    /// <summary>
    /// Session Cookie from RegisterClientRequest.
    /// </summary>
    [DataMember]
    public string session_cookie;
    /// <summary>
    /// QOS Priority Session duration in seconds. (optional)
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public UInt32 session_duration;
    /// <summary>
    /// IP address of mobile device.
    /// </summary>
    [DataMember]
    public string ip_user_equipment;
    /// <summary>
    /// IP address of the application server.
    /// </summary>
    [DataMember]
    public string ip_application_server;
    /// <summary>
    /// A list of single ports or port ranges on the user equipment.(optional)
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public string port_user_equipment;
    /// <summary>
    /// A list of single ports or port ranges on the application server.(optional)
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public string port_application_server;
    /// <summary>
    /// The used transport protocol for the uplink. (optional)
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public QosSessionProtocol protocol_in;
    /// <summary>
    /// The used transport protocol for the downlink.(optional)
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public QosSessionProtocol protocol_out;
    /// <summary>
    /// QOS Priority Session profile name.
    /// </summary>
    [DataMember]
    public QosSessionProfile profile;
    /// <summary>
    /// URI of the callback receiver. Allows asynchronous delivery of session related events. (optional)
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public string notification_uri;
    /// <summary>
    /// Authentification token for callback API. (optional)
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public string notification_auth_token;
    /// <summary>
    /// Vendor specific data. (optional)
    /// </summary>
    public Dictionary<string, string> tags;

    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  }
  /*!
  * QosPrioritySessionReply
  * Reply object received via QosPrioritySessionCreateRequest 
  * \ingroup classes_datastructs
  */
  [DataContract]
  public class QosPrioritySessionReply
  {
    [DataMember]
    public UInt32 ver = 1;
    /// <summary>
    /// QOS Priority Session duration in seconds.
    /// </summary>
    [DataMember]
    public UInt32 session_duration;
    /// <summary>
    /// QOS Priority Session profile name.
    /// </summary>
    [DataMember]
    public QosSessionProfile profile;
    /// <summary>
    /// Session ID in UUID format.
    /// </summary>
    [DataMember]
    public string session_id;
    /// <summary>
    /// Timestamp of session start in seconds since unix epoch.
    /// </summary>
    [DataMember]
    public UInt32 started_at;
    /// <summary>
    /// Timestamp of session expiration if the session was not deleted in seconds since unix epoch.
    /// </summary>
    [DataMember]
    public UInt32 expires_at;
    /// <summary>
    /// HTTP Status Code of call to operator's API server.
    /// </summary>
    [DataMember]
    public UInt32 http_status;
    public Dictionary<string, string> tags;
    /// <summary>
    /// Vendor specific data. (optional)
    /// </summary>
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  }
  /*!
  * QosPrioritySessionDeleteRequest
  * Requests the deletion of running quality of service session.
  * Required(session_cookie, profile, session_id)
  * \ingroup classes_datastructs
  */
  [DataContract]
  public class QosPrioritySessionDeleteRequest
  {
    /// <summary>
    /// Reserved for future use.
    /// </summary>
    [DataMember]
    public UInt32 ver;
    /// <summary>
    /// Session Cookie from RegisterClientRequest.
    /// </summary>
    [DataMember]
    public string session_cookie;
    /// <summary>
    /// QOS Priority Session profile name.
    /// </summary>
    [DataMember]
    public QosSessionProfile profile;
    /// <summary>
    /// QOS Priority Session ID to be deleted.
    /// </summary>
    [DataMember]
    public string session_id;
    public Dictionary<string, string> tags;
    /// <summary>
    /// Vendor specific data. (optional)
    /// </summary>
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  }
  /*!
  * QosPrioritySessionDeleteReply
  * Reply object received via QosPrioritySessionDeleteRequest 
  * \ingroup classes_datastructs
  */
  [DataContract]
  public class QosPrioritySessionDeleteReply
  {
    [DataMember]
    public UInt32 ver;
    /// <summary>
    /// Status return.
    /// </summary>
    [DataMember]
    public DeleteStatus status;
    public Dictionary<string, string> tags;
    /// <summary>
    /// Vendor specific data. (optional)
    /// </summary>
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  }
}
