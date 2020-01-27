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
  [DataContract]
  public class RegisterClientRequest
  {
    [DataMember]
    public UInt32 ver;
    [DataMember]
    public string dev_name;
    [DataMember]
    public string app_name;
    [DataMember]
    public string app_vers;
    [DataMember]
    public string carrier_name;
    [DataMember]
    public string auth_token;
  }

  [DataContract]
  public class RegisterClientReply
  {
    [DataMember]
    public UInt32 Ver;

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
        status = Enum.TryParse(value, out ReplyStatus replyStatus) ? replyStatus : ReplyStatus.RS_UNDEFINED;
      }
    }

    [DataMember]
    public string session_cookie;
    [DataMember]
    public string token_server_uri;
  }

}
