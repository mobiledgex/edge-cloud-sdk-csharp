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
using System.Diagnostics;
using System.Runtime.Serialization;

namespace DistributedMatchEngine
{
  /*!
   * RegisterClientRequest
   * Request object sent via RegisterClient from client side to DME
   * Request requires org_name, app_name, and app_vers
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class RegisterClientRequest
  {
    [DataMember]
    public UInt32 ver;
    //! App developer organization name
    [DataMember]
    public string org_name;
    //! Name of your application
    [DataMember]
    public string app_name;
    //! Application version
    [DataMember]
    public string app_vers;
    //! Optional. An authentication token supplied by the application.
    [DataMember(EmitDefaultValue = false)]
    public string auth_token;
    //! Optional. Cellular ID of where the client is connected.
    [DataMember(EmitDefaultValue = false)]
    public UInt32 cell_id;
    //! Optional. Type of unique ID provided by the client, If left blank, a new Unique ID type will be assigned in the RegisterClient Reply.
    [DataMember(EmitDefaultValue = false)]
    public string unique_id_type;
    [DataMember(EmitDefaultValue = false)]
    //! Optional. Unique identification of the client device or user. May be overridden by the server, If left blank, a new Unique ID will be assigned in the RegisterClient Reply.
    public string unique_id;
    //! Optional. Vendor specific data
    public Dictionary<string, string> tags;

    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Tag[] array_tags;
  }

  /*!
   * RegisterClientReply
   * Reply object received via RegisterClient
   * If application exists, this will return RS_SUCCESS and contain a session cookie to be used in other DME APIs 
   * \ingroup classes_datastructs
   */
  [DataContract]
  public class RegisterClientReply
  {
    [DataMember]
    public UInt32 Ver;
    //! Status of the reply
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
    //! Session Cookie to be used in later API calls
    [DataMember]
    public string session_cookie;
    //! URI for the Token Server
    [DataMember]
    public string token_server_uri;
    /*! Optional. Type of unique ID provided by the server
     * A unique_id_type and unique_id may be provided by the client to be registered.
     * During registering, if a unique_id_type and unique_id are provided by the client in their request,
     * the unique_id_type and unique_id will be left blank in the response.
     * But, if the client does not provide a unique_id_type and unique_id, then the server generates
     * one and provides the unique_id in the response. If possible, the unique_id should be saved by the
     * client locally and used for subsequent RegisterClient API calls. Otherwise, a new unique_id will be
     * generated for further API calls.
     */
    [DataMember(EmitDefaultValue = false)]
    public string unique_id_type;
    /*! Optional.Unique identification of the client device or user
     * A unique_id_type and unique_id may be provided by the client to be registered.
     * During registering, if a unique_id_type and unique_id are provided by the client in their request,
     * the unique_id_type and unique_id will be left blank in the response.
     * But, if the client does not provide a unique_id_type and unique_id, then the server generates
     * one and provides the unique_id in the response. If possible, the unique_id should be saved by the
     * client locally and used for subsequent RegisterClient API calls. Otherwise, a new unique_id will be
     * generated for further API calls.
     */
    [DataMember(EmitDefaultValue = false)]
    public string unique_id;

    //! Optional. Vendor specific data
    public Dictionary<string, string> tags;
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Tag[] array_tags;
  }
}