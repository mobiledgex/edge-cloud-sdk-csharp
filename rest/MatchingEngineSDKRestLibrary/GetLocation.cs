/**
 * Copyright 2019 MobiledgeX, Inc. All rights and licenses reserved.
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
    [DataContract]
    public class GetLocationRequest
    {
        [DataMember]
        public UInt32 ver;
        [DataMember]
        public string session_cookie;
        [DataMember]
        public string carrier_name;
        [DataMember]
        public UInt32 cell_id;
        [DataMember]
        public Tag[] tags;
    }

    [DataContract]
    public class GetLocationReply
    {
        public enum LocStatus
        {
            LOC_UNKNOWN = 0,
            LOC_FOUND = 1,
            // The user does not allow his location to be tracked
            LOC_DENIED = 2
        }
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

        [DataMember]
        public string carrier_name;
        [DataMember]
        public string tower;
        [DataMember]
        public Loc network_location;
        [DataMember]
        public Tag[] tags;
    }
}
