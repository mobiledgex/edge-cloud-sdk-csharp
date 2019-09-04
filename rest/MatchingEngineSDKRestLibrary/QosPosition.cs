﻿using System;
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
        status = Enum.TryParse(value, out ReplyStatus replyStatus) ? replyStatus : ReplyStatus.RS_UNDEFINED;
      }
    }

    // kpi details
    [DataMember]
    public QosPositionResult[] position_results;
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
