using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using DistributedMatchEngine;

namespace DistributedMatchEngine
{
  //! Messages from SDK to DME
  [DataContract]
  public class ClientEdgeEvent
  {
    //! Session Cookie from RegisterClientReply
    [DataMember]
    public string session_cookie;
    //! Session Cookie from FindCloudletReply
    [DataMember]
    public string edge_events_cookie;

    public enum ClientEventType
    {
      [EnumMember]
      EVENT_UNKNOWN = 0,
      [EnumMember(Value = "EVENT_INIT_CONNECTION")]
      EVENT_INIT_CONNECTION = 1,
      [EnumMember]
      EVENT_TERMINATE_CONNECTION = 2,
      [EnumMember]
      EVENT_LATENCY_SAMPLES = 3,
      [EnumMember]
      EVENT_LOCATION_UPDATE = 4
    }

    [DataMember]
    public ClientEventType event_type = ClientEventType.EVENT_UNKNOWN;
    //! GPS Location info if event_type is EVENT_LOCATION_UPDATE
    [DataMember]
    public Loc gps_location;
    //! Latency Samples if event_type is EVENT_LATENCY_SAMPLES
    [DataMember]
    public Sample[] samples;
    //! Carrier name used to find closer cloudlet if event_type is EVENT_LOCATION_UPDATE
    [DataMember]
    public string carrier_name;

    //! Optional. Vendor specific data
    public Dictionary<string, string> tags;
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;
  }

  // Wrapped ServerEdgeEvent:
  [DataContract]
  internal class WrappedServerEdgeEvent
  {
    [DataMember]
    public ServerEdgeEvent result; // Double parse...
  }

  //! Message from DME to SDK
  [DataContract]
  public class ServerEdgeEvent
  {
    public enum ServerEventType
    {
      [EnumMember]
      EVENT_UNKNOWN = 0,
      [EnumMember]
      EVENT_INIT_CONNECTION = 1,
      [EnumMember]
      EVENT_LATENCY_REQUEST = 2,
      [EnumMember]
      EVENT_LATENCY_PROCESSED = 3,
      [EnumMember]
      EVENT_CLOUDLET_STATE = 4,
      [EnumMember]
      EVENT_CLOUDLET_MAINTENANCE = 5,
      [EnumMember]
      EVENT_APPINST_HEALTH = 6,
      [EnumMember]
      EVENT_CLOUDLET_UPDATE = 7
    }
    //[DataMember]
    public ServerEventType event_type;
    //! Cloudlet state information
    //[DataMember]
    public CloudletState cloudlet_state;
    //! Cloudlet maintenance state information
    //[DataMember]
    public MaintenanceState maintenance_state;
    //! AppInst health state information
    [DataMember]
    public HealthCheck health_check;
    //! Summarized RTT Latency info from samples provided from client if event_type is EVENT_LATENCY
    [DataMember]
    public Latency latency;
    //! New and closer cloudlet if event_type is EVENT_CLOUDLET_UPDATE
    [DataMember]
    public FindCloudletReply new_cloudlet;

    //! Optional. Vendor specific data
    public Dictionary<string, string> tags;
    [DataMember(Name = "tags", EmitDefaultValue = false)]
    internal Hashtable htags;

    // Not much of System.Json/System.Text.Json survives Unity IL2CPP AOT compiler, link.xml or not.
    // Dual deserialize.
    internal static ServerEdgeEvent Build(string jsonStr, DataContractJsonSerializerSettings serializerSettings)
    {
      // No need to remove "result" from JSON {}\n\n
      byte[] byteArray = Encoding.ASCII.GetBytes(jsonStr);
      MemoryStream ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(ServerEdgeEvent), serializerSettings);
      ServerEdgeEvent reply = (ServerEdgeEvent)deserializer.ReadObject(ms);

      // Enum/Unity is broken, just reparse.
      JsonValue resultObject = (JsonValue)JsonValue.Parse(jsonStr);

      JsonValue serverEdgeEventObj = resultObject["result"];

      if (!Enum.TryParse<ServerEventType>(serverEdgeEventObj["event_type"], out reply.event_type))
      {
        Log.E("Could not convert event type!");
      }
      if (!Enum.TryParse<CloudletState>(serverEdgeEventObj["cloudlet_state"], out reply.cloudlet_state))
      {
        Log.E("Could not convert event type!");
      }
      if (!Enum.TryParse<MaintenanceState>(serverEdgeEventObj["maintenance_state"], out reply.maintenance_state))
      {
        Log.E("Could not convert event type!");
      }
      if (!Enum.TryParse<HealthCheck>(serverEdgeEventObj["health_check"], out reply.health_check))
      {
        Log.E("Could not convert event type!");
      }
      // Convert to Dictionary:
      reply.tags = Tag.HashtableToDictionary(reply.htags);

      return reply;
    }
  }
}
