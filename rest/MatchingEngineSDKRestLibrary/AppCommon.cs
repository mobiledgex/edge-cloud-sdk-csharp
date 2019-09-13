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
  public enum LProto
  {
    // Unknown protocol
    L_PROTO_UNKNOWN = 0,
    // TCP (L4) protocol
    L_PROTO_TCP = 1,
    // UDP (L4) protocol
    L_PROTO_UDP = 2,
    // HTTP (L7 tcp) protocol
    L_PROTO_HTTP = 3
  }

  [DataContract]
  public class AppPort
  {
    // TCP (L4), UDP (L4), or HTTP (L7) protocol
    public LProto proto = LProto.L_PROTO_UNKNOWN;

    [DataMember(Name = "proto")]
    private string proto_string
    {
      get
      {
        return proto.ToString();
      }
      set
      {
        proto = Enum.TryParse(value, out LProto lproto) ? lproto : LProto.L_PROTO_UNKNOWN;
      }
    }

    // Container port
    [DataMember]
    public Int32 internal_port;
    // Public facing port for TCP/UDP (may be mapped on shared LB reverse proxy)
    [DataMember]
    public Int32 public_port;
    // Public facing path for HTTP L7 access.
    [DataMember]
    public string path_prefix;
    // FQDN prefix to append to base FQDN in FindCloudlet response. May be empty.
    [DataMember]
    public string fqdn_prefix;
    // A non-zero end port indicates this is a port range from internal port to end port, inclusive.
    [DataMember]
    public Int32 end_port;
  }

  public enum IDTypes
  {
    ID_UNDEFINED = 0,
    IMEI = 1,
    MSISDN = 2,
    IPADDR = 3
  }

  public enum ReplyStatus
  {
    RS_UNDEFINED = 0,
    RS_SUCCESS = 1,
    RS_FAIL = 2
  }

  public class Timestamp
  {
    public string seconds;
    public Int32 nanos;
  }
}
