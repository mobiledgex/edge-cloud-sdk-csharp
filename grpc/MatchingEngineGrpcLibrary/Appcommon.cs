// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: appcommon.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace DistributedMatchEngine {

  /// <summary>Holder for reflection information generated from appcommon.proto</summary>
  public static partial class AppcommonReflection {

    #region Descriptor
    /// <summary>File descriptor for appcommon.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static AppcommonReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Cg9hcHBjb21tb24ucHJvdG8SGGRpc3RyaWJ1dGVkX21hdGNoX2VuZ2luZRog",
            "Z29vZ2xlL3Byb3RvYnVmL2Rlc2NyaXB0b3IucHJvdG8iqQEKB0FwcFBvcnQS",
            "LwoFcHJvdG8YASABKA4yIC5kaXN0cmlidXRlZF9tYXRjaF9lbmdpbmUuTFBy",
            "b3RvEhUKDWludGVybmFsX3BvcnQYAiABKAUSEwoLcHVibGljX3BvcnQYAyAB",
            "KAUSEwoLZnFkbl9wcmVmaXgYBSABKAkSEAoIZW5kX3BvcnQYBiABKAUSCwoD",
            "dGxzGAcgASgIEg0KBW5naW54GAggASgIImkKCkRldmljZUluZm8SGQoRZGF0",
            "YV9uZXR3b3JrX3R5cGUYASABKAkSEQoJZGV2aWNlX29zGAIgASgJEhQKDGRl",
            "dmljZV9tb2RlbBgDIAEoCRIXCg9zaWduYWxfc3RyZW5ndGgYBCABKA0qPwoG",
            "TFByb3RvEhMKD0xfUFJPVE9fVU5LTk9XThAAEg8KC0xfUFJPVE9fVENQEAES",
            "DwoLTF9QUk9UT19VRFAQAiqFAQoLSGVhbHRoQ2hlY2sSGAoUSEVBTFRIX0NI",
            "RUNLX1VOS05PV04QABIkCiBIRUFMVEhfQ0hFQ0tfRkFJTF9ST09UTEJfT0ZG",
            "TElORRABEiEKHUhFQUxUSF9DSEVDS19GQUlMX1NFUlZFUl9GQUlMEAISEwoP",
            "SEVBTFRIX0NIRUNLX09LEAMq7wEKDUNsb3VkbGV0U3RhdGUSGgoWQ0xPVURM",
            "RVRfU1RBVEVfVU5LTk9XThAAEhkKFUNMT1VETEVUX1NUQVRFX0VSUk9SUxAB",
            "EhgKFENMT1VETEVUX1NUQVRFX1JFQURZEAISGgoWQ0xPVURMRVRfU1RBVEVf",
            "T0ZGTElORRADEh4KGkNMT1VETEVUX1NUQVRFX05PVF9QUkVTRU5UEAQSFwoT",
            "Q0xPVURMRVRfU1RBVEVfSU5JVBAFEhoKFkNMT1VETEVUX1NUQVRFX1VQR1JB",
            "REUQBhIcChhDTE9VRExFVF9TVEFURV9ORUVEX1NZTkMQByrAAgoQTWFpbnRl",
            "bmFuY2VTdGF0ZRIUChBOT1JNQUxfT1BFUkFUSU9OEAASFQoRTUFJTlRFTkFO",
            "Q0VfU1RBUlQQARIcChJGQUlMT1ZFUl9SRVFVRVNURUQQAhoEkPYYARIXCg1G",
            "QUlMT1ZFUl9ET05FEAMaBJD2GAESGAoORkFJTE9WRVJfRVJST1IQBBoEkPYY",
            "ARIhCh1NQUlOVEVOQU5DRV9TVEFSVF9OT19GQUlMT1ZFUhAFEhcKDUNSTV9S",
            "RVFVRVNURUQQBhoEkPYYARIfChVDUk1fVU5ERVJfTUFJTlRFTkFOQ0UQBxoE",
            "kPYYARITCglDUk1fRVJST1IQCBoEkPYYARIfChVOT1JNQUxfT1BFUkFUSU9O",
            "X0lOSVQQCRoEkPYYARIbChFVTkRFUl9NQUlOVEVOQU5DRRAfGgSQ9hgBOjkK",
            "DGVudW1fYmFja2VuZBIhLmdvb2dsZS5wcm90b2J1Zi5FbnVtVmFsdWVPcHRp",
            "b25zGOKOAyABKAhiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { pbr::FileDescriptor.DescriptorProtoFileDescriptor, },
          new pbr::GeneratedClrTypeInfo(new[] {typeof(global::DistributedMatchEngine.LProto), typeof(global::DistributedMatchEngine.HealthCheck), typeof(global::DistributedMatchEngine.CloudletState), typeof(global::DistributedMatchEngine.MaintenanceState), }, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::DistributedMatchEngine.AppPort), global::DistributedMatchEngine.AppPort.Parser, new[]{ "Proto", "InternalPort", "PublicPort", "FqdnPrefix", "EndPort", "Tls", "Nginx" }, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::DistributedMatchEngine.DeviceInfo), global::DistributedMatchEngine.DeviceInfo.Parser, new[]{ "DataNetworkType", "DeviceOs", "DeviceModel", "SignalStrength" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Enums
  /// <summary>
  /// LProto indicates which protocol to use for accessing an application on a particular port. This is required by Kubernetes for port mapping.
  /// </summary>
  public enum LProto {
    /// <summary>
    /// Unknown protocol
    /// </summary>
    [pbr::OriginalName("L_PROTO_UNKNOWN")] Unknown = 0,
    /// <summary>
    /// TCP (L4) protocol
    /// </summary>
    [pbr::OriginalName("L_PROTO_TCP")] Tcp = 1,
    /// <summary>
    /// UDP (L4) protocol
    /// </summary>
    [pbr::OriginalName("L_PROTO_UDP")] Udp = 2,
  }

  /// <summary>
  /// Health check status
  ///
  /// Health check status gets set by external, or rootLB health check
  /// </summary>
  public enum HealthCheck {
    /// <summary>
    /// Health Check is unknown
    /// </summary>
    [pbr::OriginalName("HEALTH_CHECK_UNKNOWN")] Unknown = 0,
    /// <summary>
    /// Health Check failure due to RootLB being offline
    /// </summary>
    [pbr::OriginalName("HEALTH_CHECK_FAIL_ROOTLB_OFFLINE")] FailRootlbOffline = 1,
    /// <summary>
    /// Health Check failure due to Backend server being unavailable
    /// </summary>
    [pbr::OriginalName("HEALTH_CHECK_FAIL_SERVER_FAIL")] FailServerFail = 2,
    /// <summary>
    /// Health Check is ok
    /// </summary>
    [pbr::OriginalName("HEALTH_CHECK_OK")] Ok = 3,
  }

  /// <summary>
  /// CloudletState is the state of the Cloudlet.
  /// </summary>
  public enum CloudletState {
    /// <summary>
    /// Unknown
    /// </summary>
    [pbr::OriginalName("CLOUDLET_STATE_UNKNOWN")] Unknown = 0,
    /// <summary>
    /// Create/Delete/Update encountered errors (see Errors field of CloudletInfo)
    /// </summary>
    [pbr::OriginalName("CLOUDLET_STATE_ERRORS")] Errors = 1,
    /// <summary>
    /// Cloudlet is created and ready
    /// </summary>
    [pbr::OriginalName("CLOUDLET_STATE_READY")] Ready = 2,
    /// <summary>
    /// Cloudlet is offline (unreachable)
    /// </summary>
    [pbr::OriginalName("CLOUDLET_STATE_OFFLINE")] Offline = 3,
    /// <summary>
    /// Cloudlet is not present
    /// </summary>
    [pbr::OriginalName("CLOUDLET_STATE_NOT_PRESENT")] NotPresent = 4,
    /// <summary>
    /// Cloudlet is initializing
    /// </summary>
    [pbr::OriginalName("CLOUDLET_STATE_INIT")] Init = 5,
    /// <summary>
    /// Cloudlet is upgrading
    /// </summary>
    [pbr::OriginalName("CLOUDLET_STATE_UPGRADE")] Upgrade = 6,
    /// <summary>
    /// Cloudlet needs data to synchronize
    /// </summary>
    [pbr::OriginalName("CLOUDLET_STATE_NEED_SYNC")] NeedSync = 7,
  }

  /// <summary>
  /// Cloudlet Maintenance States
  ///
  /// Maintenance allows for planned downtimes of Cloudlets.
  /// These states involve message exchanges between the Controller,
  /// the AutoProv service, and the CRM. Certain states are only set
  /// by certain actors.
  /// </summary>
  public enum MaintenanceState {
    /// <summary>
    /// Normal operational state
    /// </summary>
    [pbr::OriginalName("NORMAL_OPERATION")] NormalOperation = 0,
    /// <summary>
    /// Request start of maintenance
    /// </summary>
    [pbr::OriginalName("MAINTENANCE_START")] MaintenanceStart = 1,
    /// <summary>
    /// Trigger failover for any HA AppInsts
    /// </summary>
    [pbr::OriginalName("FAILOVER_REQUESTED")] FailoverRequested = 2,
    /// <summary>
    /// Failover done
    /// </summary>
    [pbr::OriginalName("FAILOVER_DONE")] FailoverDone = 3,
    /// <summary>
    /// Some errors encountered during maintenance failover
    /// </summary>
    [pbr::OriginalName("FAILOVER_ERROR")] FailoverError = 4,
    /// <summary>
    /// Request start of maintenance without AutoProv failover
    /// </summary>
    [pbr::OriginalName("MAINTENANCE_START_NO_FAILOVER")] MaintenanceStartNoFailover = 5,
    /// <summary>
    /// Request CRM to transition to maintenance
    /// </summary>
    [pbr::OriginalName("CRM_REQUESTED")] CrmRequested = 6,
    /// <summary>
    /// CRM request done and under maintenance
    /// </summary>
    [pbr::OriginalName("CRM_UNDER_MAINTENANCE")] CrmUnderMaintenance = 7,
    /// <summary>
    /// CRM failed to go into maintenance
    /// </summary>
    [pbr::OriginalName("CRM_ERROR")] CrmError = 8,
    /// <summary>
    /// Request CRM to transition to normal operation
    /// </summary>
    [pbr::OriginalName("NORMAL_OPERATION_INIT")] NormalOperationInit = 9,
    /// <summary>
    /// Under maintenance
    /// </summary>
    [pbr::OriginalName("UNDER_MAINTENANCE")] UnderMaintenance = 31,
  }

  #endregion

  #region Messages
  /// <summary>
  /// Application Port
  ///
  /// AppPort describes an L4 or L7 public access port/path mapping. This is used to track external to internal mappings for access via a shared load balancer or reverse proxy.
  /// </summary>
  public sealed partial class AppPort : pb::IMessage<AppPort> {
    private static readonly pb::MessageParser<AppPort> _parser = new pb::MessageParser<AppPort>(() => new AppPort());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<AppPort> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::DistributedMatchEngine.AppcommonReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public AppPort() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public AppPort(AppPort other) : this() {
      proto_ = other.proto_;
      internalPort_ = other.internalPort_;
      publicPort_ = other.publicPort_;
      fqdnPrefix_ = other.fqdnPrefix_;
      endPort_ = other.endPort_;
      tls_ = other.tls_;
      nginx_ = other.nginx_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public AppPort Clone() {
      return new AppPort(this);
    }

    /// <summary>Field number for the "proto" field.</summary>
    public const int ProtoFieldNumber = 1;
    private global::DistributedMatchEngine.LProto proto_ = 0;
    /// <summary>
    /// TCP (L4) or UDP (L4) protocol
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::DistributedMatchEngine.LProto Proto {
      get { return proto_; }
      set {
        proto_ = value;
      }
    }

    /// <summary>Field number for the "internal_port" field.</summary>
    public const int InternalPortFieldNumber = 2;
    private int internalPort_;
    /// <summary>
    /// Container port
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int InternalPort {
      get { return internalPort_; }
      set {
        internalPort_ = value;
      }
    }

    /// <summary>Field number for the "public_port" field.</summary>
    public const int PublicPortFieldNumber = 3;
    private int publicPort_;
    /// <summary>
    /// Public facing port for TCP/UDP (may be mapped on shared LB reverse proxy)
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int PublicPort {
      get { return publicPort_; }
      set {
        publicPort_ = value;
      }
    }

    /// <summary>Field number for the "fqdn_prefix" field.</summary>
    public const int FqdnPrefixFieldNumber = 5;
    private string fqdnPrefix_ = "";
    /// <summary>
    /// skip 4 to preserve the numbering. 4 was path_prefix but was removed since we dont need it after removed http
    /// FQDN prefix to append to base FQDN in FindCloudlet response. May be empty.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string FqdnPrefix {
      get { return fqdnPrefix_; }
      set {
        fqdnPrefix_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "end_port" field.</summary>
    public const int EndPortFieldNumber = 6;
    private int endPort_;
    /// <summary>
    /// A non-zero end port indicates a port range from internal port to end port, inclusive.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int EndPort {
      get { return endPort_; }
      set {
        endPort_ = value;
      }
    }

    /// <summary>Field number for the "tls" field.</summary>
    public const int TlsFieldNumber = 7;
    private bool tls_;
    /// <summary>
    /// TLS termination for this port
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Tls {
      get { return tls_; }
      set {
        tls_ = value;
      }
    }

    /// <summary>Field number for the "nginx" field.</summary>
    public const int NginxFieldNumber = 8;
    private bool nginx_;
    /// <summary>
    /// use nginx proxy for this port if you really need a transparent proxy (udp only)
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Nginx {
      get { return nginx_; }
      set {
        nginx_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as AppPort);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(AppPort other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Proto != other.Proto) return false;
      if (InternalPort != other.InternalPort) return false;
      if (PublicPort != other.PublicPort) return false;
      if (FqdnPrefix != other.FqdnPrefix) return false;
      if (EndPort != other.EndPort) return false;
      if (Tls != other.Tls) return false;
      if (Nginx != other.Nginx) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Proto != 0) hash ^= Proto.GetHashCode();
      if (InternalPort != 0) hash ^= InternalPort.GetHashCode();
      if (PublicPort != 0) hash ^= PublicPort.GetHashCode();
      if (FqdnPrefix.Length != 0) hash ^= FqdnPrefix.GetHashCode();
      if (EndPort != 0) hash ^= EndPort.GetHashCode();
      if (Tls != false) hash ^= Tls.GetHashCode();
      if (Nginx != false) hash ^= Nginx.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Proto != 0) {
        output.WriteRawTag(8);
        output.WriteEnum((int) Proto);
      }
      if (InternalPort != 0) {
        output.WriteRawTag(16);
        output.WriteInt32(InternalPort);
      }
      if (PublicPort != 0) {
        output.WriteRawTag(24);
        output.WriteInt32(PublicPort);
      }
      if (FqdnPrefix.Length != 0) {
        output.WriteRawTag(42);
        output.WriteString(FqdnPrefix);
      }
      if (EndPort != 0) {
        output.WriteRawTag(48);
        output.WriteInt32(EndPort);
      }
      if (Tls != false) {
        output.WriteRawTag(56);
        output.WriteBool(Tls);
      }
      if (Nginx != false) {
        output.WriteRawTag(64);
        output.WriteBool(Nginx);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Proto != 0) {
        size += 1 + pb::CodedOutputStream.ComputeEnumSize((int) Proto);
      }
      if (InternalPort != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(InternalPort);
      }
      if (PublicPort != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(PublicPort);
      }
      if (FqdnPrefix.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(FqdnPrefix);
      }
      if (EndPort != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(EndPort);
      }
      if (Tls != false) {
        size += 1 + 1;
      }
      if (Nginx != false) {
        size += 1 + 1;
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(AppPort other) {
      if (other == null) {
        return;
      }
      if (other.Proto != 0) {
        Proto = other.Proto;
      }
      if (other.InternalPort != 0) {
        InternalPort = other.InternalPort;
      }
      if (other.PublicPort != 0) {
        PublicPort = other.PublicPort;
      }
      if (other.FqdnPrefix.Length != 0) {
        FqdnPrefix = other.FqdnPrefix;
      }
      if (other.EndPort != 0) {
        EndPort = other.EndPort;
      }
      if (other.Tls != false) {
        Tls = other.Tls;
      }
      if (other.Nginx != false) {
        Nginx = other.Nginx;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            Proto = (global::DistributedMatchEngine.LProto) input.ReadEnum();
            break;
          }
          case 16: {
            InternalPort = input.ReadInt32();
            break;
          }
          case 24: {
            PublicPort = input.ReadInt32();
            break;
          }
          case 42: {
            FqdnPrefix = input.ReadString();
            break;
          }
          case 48: {
            EndPort = input.ReadInt32();
            break;
          }
          case 56: {
            Tls = input.ReadBool();
            break;
          }
          case 64: {
            Nginx = input.ReadBool();
            break;
          }
        }
      }
    }

  }

  /// <summary>
  ///
  /// DeviceInfo
  /// </summary>
  public sealed partial class DeviceInfo : pb::IMessage<DeviceInfo> {
    private static readonly pb::MessageParser<DeviceInfo> _parser = new pb::MessageParser<DeviceInfo>(() => new DeviceInfo());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<DeviceInfo> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::DistributedMatchEngine.AppcommonReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public DeviceInfo() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public DeviceInfo(DeviceInfo other) : this() {
      dataNetworkType_ = other.dataNetworkType_;
      deviceOs_ = other.deviceOs_;
      deviceModel_ = other.deviceModel_;
      signalStrength_ = other.signalStrength_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public DeviceInfo Clone() {
      return new DeviceInfo(this);
    }

    /// <summary>Field number for the "data_network_type" field.</summary>
    public const int DataNetworkTypeFieldNumber = 1;
    private string dataNetworkType_ = "";
    /// <summary>
    /// LTE, 5G, etc.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string DataNetworkType {
      get { return dataNetworkType_; }
      set {
        dataNetworkType_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "device_os" field.</summary>
    public const int DeviceOsFieldNumber = 2;
    private string deviceOs_ = "";
    /// <summary>
    /// Android or iOS
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string DeviceOs {
      get { return deviceOs_; }
      set {
        deviceOs_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "device_model" field.</summary>
    public const int DeviceModelFieldNumber = 3;
    private string deviceModel_ = "";
    /// <summary>
    /// Device model
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string DeviceModel {
      get { return deviceModel_; }
      set {
        deviceModel_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "signal_strength" field.</summary>
    public const int SignalStrengthFieldNumber = 4;
    private uint signalStrength_;
    /// <summary>
    /// Device signal strength (0-5)
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public uint SignalStrength {
      get { return signalStrength_; }
      set {
        signalStrength_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as DeviceInfo);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(DeviceInfo other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (DataNetworkType != other.DataNetworkType) return false;
      if (DeviceOs != other.DeviceOs) return false;
      if (DeviceModel != other.DeviceModel) return false;
      if (SignalStrength != other.SignalStrength) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (DataNetworkType.Length != 0) hash ^= DataNetworkType.GetHashCode();
      if (DeviceOs.Length != 0) hash ^= DeviceOs.GetHashCode();
      if (DeviceModel.Length != 0) hash ^= DeviceModel.GetHashCode();
      if (SignalStrength != 0) hash ^= SignalStrength.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (DataNetworkType.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(DataNetworkType);
      }
      if (DeviceOs.Length != 0) {
        output.WriteRawTag(18);
        output.WriteString(DeviceOs);
      }
      if (DeviceModel.Length != 0) {
        output.WriteRawTag(26);
        output.WriteString(DeviceModel);
      }
      if (SignalStrength != 0) {
        output.WriteRawTag(32);
        output.WriteUInt32(SignalStrength);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (DataNetworkType.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(DataNetworkType);
      }
      if (DeviceOs.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(DeviceOs);
      }
      if (DeviceModel.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(DeviceModel);
      }
      if (SignalStrength != 0) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(SignalStrength);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(DeviceInfo other) {
      if (other == null) {
        return;
      }
      if (other.DataNetworkType.Length != 0) {
        DataNetworkType = other.DataNetworkType;
      }
      if (other.DeviceOs.Length != 0) {
        DeviceOs = other.DeviceOs;
      }
      if (other.DeviceModel.Length != 0) {
        DeviceModel = other.DeviceModel;
      }
      if (other.SignalStrength != 0) {
        SignalStrength = other.SignalStrength;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            DataNetworkType = input.ReadString();
            break;
          }
          case 18: {
            DeviceOs = input.ReadString();
            break;
          }
          case 26: {
            DeviceModel = input.ReadString();
            break;
          }
          case 32: {
            SignalStrength = input.ReadUInt32();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
