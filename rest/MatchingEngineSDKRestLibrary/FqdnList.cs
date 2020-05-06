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
   /// Application Fully Qualified Domain Class
   /// </summary>
  [DataContract]
  public class AppFqdn
  {
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
    /// Organization Name
    /// </summary>
    [DataMember]
    public string org_name;
    /// <summary>
    /// Array of Fully Qualified Domain Names
    /// </summary>
    [DataMember]
    public string[] fqdns;
    [DataMember]
    public string android_package_name;
  }
  /// <summary>
  /// Defines the Fully Qualifed Domain Name requests
  /// </summary>
  [DataContract]
  public class FqdnListRequest
  {
    /// <summary>
    /// API Version
    /// </summary>
    [DataMember]
    public UInt32 ver;
    [DataMember]
    public string session_cookie;
    /// <summary>
    /// GSM Cell ID is a generally unique number used to identify each base transceiver station
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public UInt32 cell_id;
    [DataMember(EmitDefaultValue = false)]
    public Tag[] tags;
  };
  /// <summary>
  /// (Fully Qualified Domain) Request Structure
  /// </summary>
  [DataContract]
  public class FqdnListReply
  {
    /// <summary>
    /// Status of the reply
    /// <para> FL_UNDEFINED = 0 </para>
    /// <para> FL_SUCCESS = 1 </para>
    /// <para> FL_FAIL = 2 </para>
    /// </summary>
    public enum FLStatus
    {
      FL_UNDEFINED = 0,
      FL_SUCCESS = 1,
      FL_FAIL = 2
    }
    /// <summary>
    /// API Version
    /// </summary>
    [DataMember]
    public UInt32 ver;
    [DataMember]
    public AppFqdn[] app_fqdns;

    public FLStatus status = FLStatus.FL_UNDEFINED;

    [DataMember(Name = "status")]
    private string fl_status_string
    {
      get
      {
        return status.ToString();
      }
      set
      {
        try
        {
          status = (FLStatus)Enum.Parse(typeof(FLStatus), value);
        }
        catch
        {
          status = FLStatus.FL_UNDEFINED;
        }
      }
    }
    [DataMember(EmitDefaultValue = false)]
    public static Tag[] tags;
  }
}
