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
    [DataMember]
    public string org_name;
  }

  /*!
   * CloudletLocation
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
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class AppInstListRequest
  {
    [DataMember]
    public UInt32 ver;
    [DataMember]
    public string session_cookie;
    [DataMember]
    public Loc gps_location;
    [DataMember]
    public string carrier_name;
    [DataMember(EmitDefaultValue = false)]
    public UInt32 cell_id;
    [DataMember(EmitDefaultValue = false)]
    public UInt32 limit;
    [DataMember(EmitDefaultValue = false)]
    public Tag[] tags;
  }

  /*!
   * AppInstListReply
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class AppInstListReply
  {
    //! Status of the reply
    public enum AIStatus
    {
      AI_UNDEFINED = 0,
      AI_SUCCESS = 1,
      AI_FAIL = 2
    }

    [DataMember]
    public UInt32 ver;

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
    [DataMember(EmitDefaultValue = false)]
    public Tag[] tags;
  }
}
