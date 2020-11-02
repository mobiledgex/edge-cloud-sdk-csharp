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
using System.Collections.Concurrent;
using System.Runtime.Serialization;

namespace DistributedMatchEngine
{
  [DataContract]
  public class AppOfficialFqdnRequest
  {
    [DataMember]
    public UInt32 ver;
    [DataMember]
    public string session_cookie;
    [DataMember]
    public Loc gps_location;
    [DataMember]
    public ConcurrentDictionary<string, string> tags;
  }

  [DataContract]
  public class AppOfficialFqdnReply
  {
    public enum AOFStatus
    {
      AOF_UNDEFINED = 0,
      AOF_SUCCESS = 1,
      // The user does not allow his location to be tracked
      AOF_FAIL = 2
    }
    [DataMember]
    public UInt32 ver;

    [DataMember]
    public string app_official_fqdn;

    [DataMember]
    public string client_token;

    public AOFStatus status = AOFStatus.AOF_UNDEFINED;

    [DataMember(Name = "status")]
    private string aof_status_string
    {
      get
      {
        return status.ToString();
      }
      set
      {
        try
        {
          status = (AOFStatus)Enum.Parse(typeof(AOFStatus), value);
        }
        catch
        {
          status = AOFStatus.AOF_UNDEFINED;
        }
      }
    }

    [DataMember]
    public AppPort[] ports;

    [DataMember]
    public ConcurrentDictionary<string, string> tags;
  }
}
