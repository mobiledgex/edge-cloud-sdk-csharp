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
  public class FindCloudletRequest
  {
    [DataMember]
    public UInt32 ver = 1;
    [DataMember]
    public string session_cookie;
    [DataMember]
    public string carrier_name;
    [DataMember]
    public Loc gps_location;
    [DataMember]
    public string dev_name;
    [DataMember]
    public string app_name;
    [DataMember]
    public string app_vers;
  }

  [DataContract]
  public class FindCloudletReply
  {
    // Standard Enum. DataContract Enum is converted to int64, not string.
    public enum FindStatus
    {
      FIND_UNKNOWN = 0,
      FIND_FOUND = 1,
      FIND_NOTFOUND = 2
    }

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
        status = Enum.TryParse(value, out FindStatus findStatus) ? findStatus : FindStatus.FIND_UNKNOWN;
      }
    }

    [DataMember]
    public string fqdn;
    [DataMember]
    public AppPort[] ports;
    [DataMember]
    public Loc cloudlet_location;
  }

}
