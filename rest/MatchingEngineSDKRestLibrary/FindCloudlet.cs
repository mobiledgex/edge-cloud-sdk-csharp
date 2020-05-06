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
  /// Defines FindCloudletRequest structure
  /// </summary>
  [DataContract]
  public class FindCloudletRequest
  {       
    /// <summary>
    /// API Version
    /// </summary>
    [DataMember]
    public UInt32 ver = 1;
    [DataMember]
    public string session_cookie;
    /// <summary>
    /// Carrier Name
    /// </summary>
    [DataMember]
    public string carrier_name;
    [DataMember]
    public Loc gps_location;
    /// <summary>
    /// Organization Name
    /// </summary>
    [DataMember]
    public string org_name;
    /// <summary>
    /// Application Name
    /// </summary>
    [DataMember]
    public string app_name;
    /// <summary>
    /// Application Version
    /// </summary>
    [DataMember]
    public string app_vers;
    /// <summary>
    /// GSM Cell ID is a generally unique number used to identify each base transceiver station
    /// </summary>
    [DataMember]
    public UInt32 cell_id;
    [DataMember]
    public Tag[] tags;
  }
  /// <summary>
  /// Defines FindCloudletReply structure
  /// </summary>
  [DataContract]
  public class FindCloudletReply
  {
    /// <summary>
    /// FindCloudlet Reply Status
    /// <para>FIND_UNKNOWN = 0</para>
    /// <para>FIND_FOUND = 1</para>
    /// <para>FIND_NOTFOUND = 2</para>
    /// </summary>
    // Standard Enum. DataContract Enum is converted to int64, not string.
    public enum FindStatus
    {
      FIND_UNKNOWN = 0,
      FIND_FOUND = 1,
      FIND_NOTFOUND = 2
    }
    /// <summary>
    /// API Version
    /// </summary>
    [DataMember]
    public UInt32 ver;

    public FindStatus status = FindStatus.FIND_UNKNOWN;

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
          status = FindStatus.FIND_UNKNOWN;
        }
      }
    }
    /// <summary>
    /// Fully Qualified Domain Name
    /// </summary>
    [DataMember]
    public string fqdn;
    /// <summary>
    /// Array of AppPort Objects
    /// </summary>
    [DataMember]
    public AppPort[] ports;
    /// <summary>
    /// Loc Object representing CLoudlet Location
    /// </summary>
    [DataMember]
    public Loc cloudlet_location;
    [DataMember]
    public Tag[] tags;
  }

}
