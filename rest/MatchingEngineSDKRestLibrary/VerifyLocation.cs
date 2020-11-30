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
   * VerifyLocationRequest
   * Request object sent via VerifyLocation from client side to DME
   * Request requires session_cookie from RegisterClientReply, gps_location to be verified, carrier_name, and verify_loc_token
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class VerifyLocationRequest
  {
    [DataMember]
    public UInt32 ver = 1;
    //! Session Cookie from RegisterClientReply
    [DataMember]
    public string session_cookie;
    //! The GPS location to verify
    [DataMember]
    public Loc gps_location;
    //! Unique carrier identification (typically MCC + MNC)
    [DataMember]
    public string carrier_name;
    //! Must be retrieved from TokenServerURI. (Handled by SDK)
    [DataMember]
    public string verify_loc_token;
    //! Optional. Cell ID where the client is
    [DataMember(EmitDefaultValue = false)]
    public UInt32 cell_id;
    //! Optional. Vendor specific data
    //! Optional. Vendor specific data
    public Dictionary<string, string> tags;

    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  };

  /*!
   * VerifyLocationReply
   * Reply object received via VerifyLocation
   * If verified, will return LOC_VERIFIED and CONNECTED_TO_SPECIFIED_TOWER
   * Also contains information about accuracy of provided gps location
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class VerifyLocationReply
  {
    //! Tower Status of a VerifyLocationReply
    public enum TowerStatus
    {
      TOWER_UNKNOWN = 0,
      CONNECTED_TO_SPECIFIED_TOWER = 1,
      NOT_CONNECTED_TO_SPECIFIED_TOWER = 2,
    }

    //! GPS Status of VerifyLocationReply
    public enum GPSLocationStatus
    {
      LOC_UNKNOWN = 0,
      LOC_VERIFIED = 1,
      LOC_MISMATCH_SAME_COUNTRY = 2,
      LOC_MISMATCH_OTHER_COUNTRY = 3,
      LOC_ROAMING_COUNTRY_MATCH = 4,
      LOC_ROAMING_COUNTRY_MISMATCH = 5,
      LOC_ERROR_UNAUTHORIZED = 6,
      LOC_ERROR_OTHER = 7
    }

    [DataMember]
    public UInt32 ver;

    //! Tower status
    public TowerStatus tower_status = TowerStatus.TOWER_UNKNOWN;

    [DataMember(Name = "tower_status")]
    private string tower_status_tring
    {
      get
      {
        return tower_status.ToString();
      }
      set
      {
        try
        {
          tower_status = (TowerStatus)Enum.Parse(typeof(TowerStatus), value);
        }
        catch
        {
          tower_status = TowerStatus.TOWER_UNKNOWN;
        }
      }
    }

    //! GPS location status
    public GPSLocationStatus gps_location_status = GPSLocationStatus.LOC_UNKNOWN;

    [DataMember(Name = "gps_location_status")]
    private string gps_location_status_string
    {
      get
      {
        return gps_location_status.ToString();
      }
      set
      {
        try
        {
          gps_location_status = (GPSLocationStatus)Enum.Parse(typeof(GPSLocationStatus), value);
        }
        catch
        {
          gps_location_status = GPSLocationStatus.LOC_UNKNOWN;
        }
      }
    }

    /*!
     * Location accuracy, the location is verified to be within this number of kilometers.
     * Negative value means no verification was performed
     */
    [DataMember]
    public double gps_location_accuracy_km;

    //! Optional. Vendor specific data
    public Dictionary<string, string> tags;
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  }
}
