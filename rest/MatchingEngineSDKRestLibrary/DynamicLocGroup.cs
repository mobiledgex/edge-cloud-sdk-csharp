/**
 * Copyright 2020 MobiledgeX, Inc. All rights and licenses reserved.
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
  public enum DlgCommType
  {
    DLG_UNDEFINED = 0,
    DLG_SECURE = 1,
    DLG_OPEN = 2
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

    public DlgCommType comm_type = DlgCommType.DLG_UNDEFINED;

    [DataMember(Name = "comm_type")]
    private string comm_type_string
    {
      get
      {
        return comm_type.ToString();
      }
      set
      {
        comm_type = Enum.TryParse(value, out DlgCommType commType) ? commType : DlgCommType.DLG_UNDEFINED;
      }
    }

    [DataMember]
    public string user_data;
    [DataMember]
    public UInt32 cell_id;
    [DataMember]
    public Tag[] tags;
  }

  [DataContract]
  public class DynamicLocGroupReply
  {
    [DataMember]
    public UInt32 ver;

    // Status of the reply
    public ReplyStatus status = ReplyStatus.RS_UNDEFINED;

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
          status = ReplyStatus.RS_UNDEFINED;
        }
      }
    }

    // Error Code based on Failure
    [DataMember]
    public UInt32 error_code;
    // Group Cookie for Secure Group Communication
    [DataMember]
    public string group_cookie;
    [DataMember]
    public Tag[] tags;
  }
}
