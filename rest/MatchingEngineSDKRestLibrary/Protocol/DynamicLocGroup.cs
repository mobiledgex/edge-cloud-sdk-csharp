﻿/**
 * Copyright 2018-2021 MobiledgeX, Inc. All rights and licenses reserved.
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
  public enum DlgCommType
  {
    [EnumMember]
    Undefined = 0,
    [EnumMember]
    Secure = 1,
    [EnumMember]
    Open = 2
  }

  [DataContract]
  public class DynamicLocGroupRequest
  {
    [DataMember]
    public UInt32 ver;
    // Session Cookie from RegisterClientRequest
    [DataMember]
    public string session_cookie;
    [DataMember]
    public UInt64 lg_id;

    public DlgCommType comm_type = DlgCommType.Undefined;

    [DataMember(Name = "comm_type")]
    private string comm_type_string
    {
      get
      {
        return comm_type.ToString();
      }
      set
      {
        comm_type = Enum.TryParse(value, out DlgCommType commType) ? commType : DlgCommType.Undefined;
      }
    }

    [DataMember(EmitDefaultValue = false)]
    public string user_data;
    [DataMember(EmitDefaultValue = false)]
    public UInt32 cell_id;

    //! Optional. Vendor specific data
    public Dictionary<string, string> tags;
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  }

  [DataContract]
  public class DynamicLocGroupReply
  {
    [DataMember]
    public UInt32 ver;

    // Status of the reply
    public ReplyStatus status = ReplyStatus.Undefined;

    [DataMember(Name = "status")]
    private string reply_status_string
    {
      get
      {
        return status.ToString();
      }

      set
      {
        try
        {
          status = (ReplyStatus)Enum.Parse(typeof(ReplyStatus), value);
        }
        catch
        {
          status = ReplyStatus.Undefined;
        }
      }
    }

    // Error Code based on Failure
    [DataMember]
    public UInt32 error_code;
    // Group Cookie for Secure Group Communication
    [DataMember]
    public string group_cookie;

    //! Optional. Vendor specific data
    public Dictionary<string, string> tags;
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  }
}
