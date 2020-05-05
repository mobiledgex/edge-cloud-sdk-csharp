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
  /// Defines the (GetLocationRequest)  
  /// </summary>
  /// <remarks>
  /// Returned by MatchingEngine.CreateGetLocationRequest
  /// </remarks>
  [DataContract]
  public class GetLocationRequest
  {
    /// <summary>
    /// API Version
    /// </summary>
    [DataMember]
    public UInt32 ver;
    [DataMember]
    public string session_cookie;
    /// <summary>
    /// Carrier Name 
    /// </summary>
    [DataMember]
    public string carrier_name;
    /// <summary>
    /// GSM Cell ID is a generally unique number used to identify each base transceiver station
    /// </summary>
    [DataMember]
    public UInt32 cell_id;
    [DataMember]
    public Tag[] tags;
  }

  [DataContract]
  public class GetLocationReply
  {
    /// <summary>
    /// Enum to hold the (GPS Location Reply) status
    /// <para>
    /// LOC_UNKNOWN = 0
    /// </para>
    ///  <para>
    /// LOC_FOUND = 1
    /// </para>
    ///  <para>
    /// LOC_DENIED = 2  (The user does not allow his location to be tracked)
    /// </para>
    /// </summary>
    public enum LocStatus
    {
      LOC_UNKNOWN = 0,
      LOC_FOUND = 1,
      // The user does not allow his location to be tracked
      LOC_DENIED = 2
    }
    /// <summary>
    /// API Version
    /// </summary>
    [DataMember]
    public UInt32 ver;

    public LocStatus status = LocStatus.LOC_UNKNOWN;

    [DataMember(Name = "status")]
    private string loc_status_string
    {
      get
      {
        return status.ToString();
      }
      set
      {
        try
        {
          status = (LocStatus)Enum.Parse(typeof(LocStatus), value);
        }
        catch
        {
          status = LocStatus.LOC_UNKNOWN;
        }
      }
    }
    /// <summary>
    /// Carrier Name 
    /// </summary>
    [DataMember]
    public string carrier_name;
    /// <summary>
    /// Tower Name
    /// </summary>
    [DataMember]
    public string tower;
    /// <summary>
    /// Loc Object defining the geolocation of the network tower
    /// </summary>
    [DataMember]
    public Loc network_location;
    [DataMember]
    public Tag[] tags;
  }
}
