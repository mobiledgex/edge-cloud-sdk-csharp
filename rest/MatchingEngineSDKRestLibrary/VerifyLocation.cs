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
    /// Defines the (Verfication of Location) request  
    /// </summary>
    /// <remarks>
    /// Returned by MatchingEngine.CreateVerifyLocationRequest(string carrierName, Loc loc, UInt32 cellID = 0, Tag[] tags = null)
    /// </remarks>

    [DataContract]
    public class VerifyLocationRequest
    {
        /// <summary>
        /// Version
        /// </summary>
        [DataMember]
        public UInt32 ver = 1;

        [DataMember]
        public string session_cookie;
        /// <summary>
        /// Mobile carrier name where mobile carrier is the wireless service provider that supplies cellular connectivity services.
        /// </summary>
        [DataMember]
        public string carrier_name;
        /// <summary>
        /// Gps Location
        /// <para><strong> Params</strong> </para>
        /// <para>
        /// (double latitude, double longitude, double altitude = 0, double horizontal_accuracy = 0, double vertical_accuracy = 0, double course = 0, double speed =0, Timestamp timestamp = null)
        /// </para>
        /// </summary>
        [DataMember]
        public Loc gps_location;
        [DataMember]
        public string verify_loc_token;
        /// <summary>
        /// GSM Cell ID is a generally unique number used to identify each base transceiver station
        /// <para>
        /// <see cref="https://en.wikipedia.org/wiki/GSM_Cell_ID"/>
        /// </para>
        /// </summary>
        [DataMember]
        public UInt32 cell_id;
       
        [DataMember]
        public Tag[] tags;
    };
    /// <summary>
    /// Defines the (Verfication of Location) reply, Can be used to define Application Policy
    /// <para>Example </para>
    /// <para>
    /// <example>
    /// <code>
    /// <para/>// Assuming the app policy is to connect only to cloudlets not further than 100 kms
    /// <para/>VerficationReply reply= await MatchingEngineInstance.VerifyLocation(Request);
    /// <para/>if(reply.gps_location_accuracy_km >100){ return false};
    /// </code>
    /// </example>
    /// </para>
    /// </summary>
    [DataContract]
    public class VerifyLocationReply
    {
        /// <summary>
        /// Enum to hold the cell tower reply status
        /// <para>
        /// TOWER_UNKNOWN = 0
        /// </para>
        ///  <para>
        /// CONNECTED_TO_SPECIFIED_TOWER = 1
        /// </para>
        ///  <para>
        /// NOT_CONNECTED_TO_SPECIFIED_TOWER = 2
        /// </para>
        /// </summary>
        // Status of the reply
        public enum TowerStatus
        {
            TOWER_UNKNOWN = 0,
            CONNECTED_TO_SPECIFIED_TOWER = 1,
            NOT_CONNECTED_TO_SPECIFIED_TOWER = 2,
        }
        /// <summary>
        /// Enum to hold the (GPS Location Reply) status
        /// <para>
        /// LOC_UNKNOWN = 0
        /// </para>
        ///  <para>
        /// LOC_VERIFIED = 1
        /// </para>
        ///  <para>
        /// LOC_MISMATCH_SAME_COUNTRY = 2
        /// </para>
        /// <para>
        /// LOC_MISMATCH_OTHER_COUNTRY = 3
        /// </para>
        /// <para>
        /// LOC_ROAMING_COUNTRY_MATCH = 4
        /// </para>
        /// <para>
        /// LOC_ROAMING_COUNTRY_MISMATCH = 5
        /// </para>
        /// <para>
        /// LOC_ERROR_UNAUTHORIZED = 6
        /// </para>
        /// <para>
        /// LOC_ERROR_OTHER = 7
        /// </para>
        /// </summary>
        // Status of the reply
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

        /// <summary>
        /// Version
        /// </summary>
        [DataMember]
        public UInt32 ver;

        public TowerStatus tower_status = TowerStatus.TOWER_UNKNOWN;

        [DataMember(Name = "tower_status")]
        private string tower_status_string
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
        /// <summary>
        /// GPS Location Accuracy in kms 
        /// <para>
        /// Can be used for Application Policy
        /// </para>
        /// </summary>
        [DataMember]
        public double gps_location_accuracy_km;

        [DataMember]
        public Tag[] tags;
    }
}