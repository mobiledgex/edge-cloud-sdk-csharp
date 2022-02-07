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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DistributedMatchEngine
{
  /*!
   * FindCloudletRequest
   * Request object sent via FindCloudlet from client side to DME
   * Request requires session_cookie from RegisterClientReply, gps_location, and carrier_name
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class FindCloudletRequest
  {
    [DataMember]
    public UInt32 ver = 1;
    //! Session Cookie from RegisterClientReply
    [DataMember]
    public string session_cookie;
    //! The GPS location of the user
    [DataMember]
    public Loc gps_location;
    /*! By default, all SDKs will automatically fill in this parameter with the MCC+MNC of your current provider.
     * Only override this parameter if you need to filter for a specific carrier on the DME.
     * The DME will filter for App instances that are associated with the specified carrier.
     * If you wish to search for any App Instance on the DME regardless of carrier name, you can input “” to consider all carriers as “Any”.
     */
    [DataMember]
    public string carrier_name;
    //! Optional. Cell ID where the client is
    [DataMember(EmitDefaultValue = false)]
    public UInt32 cell_id;
    [DataMember]
    public string client_token;

    //! Optional. Vendor specific data
    public Dictionary<string, string> tags;
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  }

  /*!
   * FindCloudletReply
   * Reply object received via FindCloudlet
   * If application instance exists, this will return FIND_FOUND and contain information about the application instance.
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class FindCloudletReply
  {
    // Standard Enum. DataContract Enum is converted to int64, not string.
    //! Status of a FindCloudletReply
    public enum FindStatus
    {
      [EnumMember]
      Unknown = 0,
      [EnumMember]
      Found = 1,
      [EnumMember]
      Notfound = 2
    }

    public enum QosSessionResult
    {
      [EnumMember]
      NotAttempted = 0,
      [EnumMember]
      SessionCreated = 1,
      [EnumMember]
      SessionFailed = 2
    }

    [DataMember]
    public UInt32 ver;

    //! Status return
    public FindStatus status = FindStatus.Unknown;

    [DataMember(Name = "status")]
    private string find_status_string
    {
      get
      {
        return status.ToString();
      }
      set
      {
        try
        {
          status = (FindStatus)Enum.Parse(typeof(FindStatus), value);
        }
        catch
        {
          status = FindStatus.Unknown;
        }
      }
    }

    //! Fully Qualified Domain Name of the Closest App instance
    [DataMember]
    public string fqdn;
    //! List of Service Endpoints for AppInst
    [DataMember]
    public AppPort[] ports;
    //! Location of the cloudlet
    [DataMember]
    public Loc cloudlet_location;

    [DataMember(Name = "qos_result")]
    private string qosResult
    {
      get
      {
        return qos_result.ToString();
      }
      set
      {
        try
        {
          qos_result = (QosSessionResult)Enum.Parse(typeof(QosSessionResult), value);
        }
        catch
        {
          qos_result = QosSessionResult.SessionFailed;
        }
      }
    }

    public QosSessionResult qos_result = QosSessionResult.SessionFailed;


    public string qos_error_msg;

    //! Session Cookie for specific EdgeEvents for specific AppInst
    [DataMember]
    public string edge_events_cookie;

    //! Optional. Vendor specific data
    public Dictionary<string, string> tags;
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  }

}
