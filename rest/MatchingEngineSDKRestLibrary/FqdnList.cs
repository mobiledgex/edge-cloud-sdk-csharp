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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DistributedMatchEngine
{
  [DataContract]
  public class AppFqdn
  {
    // App  Name
    [DataMember]
    public string app_name;
    // App Version
    [DataMember]
    public string app_vers;
    // organization name
    [DataMember]
    public string org_name;
    // App FQDN
    [DataMember]
    public string[] fqdns;
    // optional android package name
    [DataMember]
    public string android_package_name;
  }

  [DataContract]
  public class FqdnListRequest
  {
    [DataMember]
    public UInt32 ver;
    [DataMember]
    public string session_cookie;
    [DataMember(EmitDefaultValue = false)]
    public UInt32 cell_id;
    [DataMember(EmitDefaultValue = false)]
    public Dictionary<string, string> tags;
  };

  [DataContract]
  public class FqdnListReply
  {
    // Status of the reply
    public enum FLStatus
    {
      FL_UNDEFINED = 0,
      FL_SUCCESS = 1,
      FL_FAIL = 2
    }

    [DataMember]
    // API version
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
    public Dictionary<string, string> tags;
  }
}
