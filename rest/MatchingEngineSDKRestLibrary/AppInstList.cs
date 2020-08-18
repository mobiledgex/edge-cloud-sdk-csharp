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
  /*!
   * Appinstance
   * Information about specific app instances
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class Appinstance
  {
    //! App Instance Name
    [DataMember]
    public string app_name;
    //! App Instance Version
    [DataMember]
    public string app_vers;
    //! App Instance FQDN
    [DataMember]
    public string fqdn;
    //! ports to access app
    [DataMember]
    public AppPort[] ports;
    //! App Organization Name
    [DataMember]
    public string org_name;
  }

  /*!
   * CloudletLocation
   * Information about cloudlet. Includes list of application instances
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class CloudletLocation
  {
    //! The carrier name that user is connected to ("Cellular Carrier Name")
    [DataMember]
    public string carrier_name;
    //! Cloudlet Name
    [DataMember]
    public string cloudlet_name;
    //! The GPS Location of the cloudlet
    [DataMember]
    public Loc gps_location;
    //! Distance of cloudlet vs loc in request
    [DataMember]
    public double distance;
    //! App instances
    [DataMember]
    public Appinstance[] appinstances;
  }

  /*!
   * AppInstListRequest
   * Request object sent via GetAppInstList from client side to DME
   * Request requires session_cookie from RegisterClientReply, gps_location, and carrier_name
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class AppInstListRequest
  {
    [DataMember]
    public UInt32 ver;
    //! Session Cookie from RegisterClientRequest
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
    //! Optional. Cell id where the client is
    [DataMember(EmitDefaultValue = false)]
    public UInt32 cell_id;
    //! Optional. Limit the number of results, defaults to 3
    [DataMember(EmitDefaultValue = false)]
    public UInt32 limit;
    //! Vendor specific data
    [DataMember(EmitDefaultValue = false)]
    public Tag[] tags;
  }

  /*!
   * AppInstListReply
   * Reply object received via GetAppInstList
   * If an application instance exists, this will return AI_SUCCESS.
   * Will provide information about all application instances deployed on specified carrier network
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class AppInstListReply
  {
    //! Status of an AppInstListReply
    public enum AIStatus
    {
      AI_UNDEFINED = 0,
      AI_SUCCESS = 1,
      AI_FAIL = 2
    }

    [DataMember]
    public UInt32 ver;

    //! Status return
    public AIStatus status;

    [DataMember(Name = "status")]
    private string ai_status_string
    {
      get
      {
        return status.ToString();
      }
      set
      {
        try
        {
          status = (AIStatus)Enum.Parse(typeof(AIStatus), value);
        }
        catch
        {
          status = AIStatus.AI_UNDEFINED;
        }
      }
    }

    [DataMember]
    public CloudletLocation[] cloudlets;
    //! Optional. Vendor specific data
    [DataMember(EmitDefaultValue = false)]
    public Tag[] tags;
  }
}
