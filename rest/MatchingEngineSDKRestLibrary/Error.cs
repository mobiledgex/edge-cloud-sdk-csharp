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
  public class ProtobufAny
  {
    [DataMember]
    public string type_url;
    [DataMember]
    public string value; // byte
  }

  public class RuntimeStreamError
  {
    [DataMember]
    public Int32 grpc_code;
    [DataMember]
    Int32 http_code;
    [DataMember]
    public string message;
    [DataMember]
    public string http_status;
    [DataMember]
    public ProtobufAny details;
  }
}
