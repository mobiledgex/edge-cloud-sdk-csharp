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
  [DataContract]
  public class QosPosition
  {
    [DataMember]
    public string positionid; // *NOT* UInt64 for the purposes of REST.
    [DataMember]
    public Loc gps_location;
  }

  [DataContract]
  public class BandSelection
  {
    // Radio Access Technologies
    [DataMember]
    public string[] rat_2g;
    [DataMember]
    public string[] rat_3g;
    [DataMember]
    public string[] rat_4g;
    [DataMember]
    public string[] rat_5g;
  }

  [DataContract]
  public class QosPositionRequest
  {
    // API version
    [DataMember]
    public UInt32 ver;
    // Session Cookie from RegisterClientRequest
    [DataMember]
    public string session_cookie;
    // list of positions
    [DataMember]
    public QosPosition[] positions;
    // client's device LTE category number, optional
    [DataMember]
    public Int32 lte_category;
    // Band list used by the client, optional
    [DataMember]
    public BandSelection band_selection;
    [DataMember]
    public UInt32 cell_id;
    [DataMember]
    public Tag[] tags;
  }

  [DataContract]
  public class QosPositionResult
  {
    // as set by the client, must be unique within one QosPositionKpiRequest
    [DataMember]
    public Int64 positionid;
    // the location which was requested
    [DataMember]
    public Loc gps_location;
    // throughput 
    [DataMember]
    public float dluserthroughput_min;
    [DataMember]
    public float dluserthroughput_avg;
    [DataMember]
    public float dluserthroughput_max;
    [DataMember]
    public float uluserthroughput_min;
    [DataMember]
    public float uluserthroughput_avg;
    [DataMember]
    public float uluserthroughput_max;
    [DataMember]
    public float latency_min;
    [DataMember]
    public float latency_avg;
    [DataMember]
    public float latency_max;
  }

  [DataContract]
  public class QosPositionKpiReply
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

    // kpi details
    [DataMember]
    public QosPositionResult[] position_results;
    [DataMember]
    public Tag[] tags;
  }

  [DataContract]
  public class QosPositionKpiStreamReply
  {
    [DataMember]
    public QosPositionKpiReply result;
    [DataMember]
    public RuntimeStreamError error;
  }
}
