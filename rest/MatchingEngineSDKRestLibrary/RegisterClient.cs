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
  /// Defines the (RegisterClient) request  
  /// </summary>
  /// <remarks>
  /// Called by MatchingEngine.CreateRegisterClientRequest
  /// </remarks>
  [DataContract]
  public class RegisterClientRequest
  {
    /// <summary>
    /// API Version
    /// </summary>
    [DataMember]
    public UInt32 ver;
    /// <summary>
    /// Organization Name
    /// </summary>
    [DataMember]
    public string org_name;
    /// <summary>
    /// Application Name
    /// </summary>
    [DataMember]
    public string app_name;
    /// <summary>
    /// Application Version
    /// </summary>
    [DataMember]
    public string app_vers;
    /// <summary>
    /// Authentication token,can be set to authorize access to use backend deployed at MobiledgeX
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public string auth_token;
    /// <summary>
    /// GSM Cell ID is a generally unique number used to identify each base transceiver station
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public UInt32 cell_id;
    /// <summary>
    /// Type of unique id provided by client
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public string unique_id_type;
    [DataMember(EmitDefaultValue = false)]
    /// <summary>
    ///  Optional. Unique identification of the client device or user. May be overridden by the server.
    /// </summary>
    public string unique_id;
    [DataMember(EmitDefaultValue = false)]
    public Tag[] tags;
  }
  /// <summary>
  /// Defines the (RegisterClientReply)  
  /// </summary>
  /// <remarks>
  /// Returned by MatchingEngine.RegisterClient
  /// </remarks>
  [DataContract]
  public class RegisterClientReply
  {
    /// <summary>
    /// API Version
    /// </summary>
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

    [DataMember]
    public string session_cookie;
    [DataMember]
    public string token_server_uri;
    /// <summary>
    /// Type of unique id provided by client
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public string unique_id_type;
    /// <summary>
    ///  Optional. Unique identification of the client device or user. May be overridden by the server.
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    public string unique_id;
    [DataMember(EmitDefaultValue = false)]
    public Tag[] tags;
  }

}
