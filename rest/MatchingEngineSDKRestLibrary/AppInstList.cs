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
    /// Application Instance 
    /// </summary>
    [DataContract]
    public class Appinstance
    {
        /// <summary>
        /// Application Instance Name
        /// </summary>
        [DataMember]
        string app_name;
        /// <summary>
        /// Application Instance Version
        /// </summary>
        [DataMember]
        string app_vers;
        /// <summary>
        /// Application Instance FQDN (Fully Qualified Domain Name)
        /// </summary>
        [DataMember]
        string fqdn;
        /// <summary>
        /// Application Port Array
        /// </summary>
        [DataMember]
        AppPort[] ports;
    }
    /// <summary>
    /// Cloudlet Location, Cloudlet is a mobility-enhanced small-scale cloud datacenter
    /// </summary>
    [DataContract]
    public class CloudletLocation
    {
        /// <summary>
        /// The carrier name that user is connected to ("Cellular Carrier Name")
        /// </summary>
        [DataMember]
        string carrier_name;
        /// <summary>
        /// Cloudlet Name
        /// </summary>
        [DataMember]
        string cloudlet_name;
        /// <summary>
        /// GPS Location of Cloudlet
        /// </summary>
        [DataMember]
        Loc gps_location;
        /// <summary>
        /// Distance of cloudlet vs loc in request
        /// </summary> 
        [DataMember]
        double distance;
        /// <summary>
        /// Application Instances Array
        /// </summary>
        [DataMember]
        Appinstance[] appinstances;
    }
    /// <summary>
    /// (Application Instances) Request Structure
    /// </summary>
    [DataContract]
    public class AppInstListRequest
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
        [DataMember]
        public Loc gps_location;
        /// <summary>
        /// GSM Cell ID is a generally unique number used to identify each base transceiver station
        /// </summary>
        [DataMember]
        public UInt32 cell_id;
        [DataMember]
        public Tag[] tags;
    }
    /// <summary>
    /// (Application Instances) Reply Structure
    /// </summary>
    [DataContract]
    public class AppInstListReply
    {
        /// <summary>
        /// Status of the reply
        /// <para>AI_UNDEFINED = 0</para>
        /// <para>AI_SUCCESS = 1</para>
        /// <para>AI_FAIL = 2</para>
        /// </summary>
        public enum AIStatus
        {
            AI_UNDEFINED = 0,
            AI_SUCCESS = 1,
            AI_FAIL = 2
        }
        /// <summary>
        /// API Version
        /// </summary>
        [DataMember]
        public UInt32 ver;
        /// <summary>
        /// Application Instance Reply Status
        /// </summary>
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
        /// <summary>
        /// Array of Cloudlet Locations
        /// </summary>
        [DataMember]
        public CloudletLocation[] cloudlets;
   
    }
}