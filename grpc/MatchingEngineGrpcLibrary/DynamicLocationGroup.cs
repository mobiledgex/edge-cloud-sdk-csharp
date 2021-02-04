// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: dynamic-location-group.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace DistributedMatchEngine {

  /// <summary>Holder for reflection information generated from dynamic-location-group.proto</summary>
  public static partial class DynamicLocationGroupReflection {

    #region Descriptor
    /// <summary>File descriptor for dynamic-location-group.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static DynamicLocationGroupReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChxkeW5hbWljLWxvY2F0aW9uLWdyb3VwLnByb3RvEhhkaXN0cmlidXRlZF9t",
            "YXRjaF9lbmdpbmUi8wEKCkRsZ01lc3NhZ2USCwoDdmVyGAEgASgNEg0KBWxn",
            "X2lkGAIgASgEEhQKDGdyb3VwX2Nvb2tpZRgDIAEoCRISCgptZXNzYWdlX2lk",
            "GAQgASgEEj0KCGFja190eXBlGAUgASgOMisuZGlzdHJpYnV0ZWRfbWF0Y2hf",
            "ZW5naW5lLkRsZ01lc3NhZ2UuRGxnQWNrEg8KB21lc3NhZ2UYBiABKAkiTwoG",
            "RGxnQWNrEhgKFERMR19BQ0tfRUFDSF9NRVNTQUdFEAASGwoXRExHX0FTWV9F",
            "VkVSWV9OX01FU1NBR0UQARIOCgpETEdfTk9fQUNLEAIiPQoIRGxnUmVwbHkS",
            "CwoDdmVyGAEgASgNEg4KBmFja19pZBgCIAEoBBIUCgxncm91cF9jb29raWUY",
            "AyABKAkybwoSRHluYW1pY0xvY0dyb3VwQXBpElkKC1NlbmRUb0dyb3VwEiQu",
            "ZGlzdHJpYnV0ZWRfbWF0Y2hfZW5naW5lLkRsZ01lc3NhZ2UaIi5kaXN0cmli",
            "dXRlZF9tYXRjaF9lbmdpbmUuRGxnUmVwbHkiAGIGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::DistributedMatchEngine.DlgMessage), global::DistributedMatchEngine.DlgMessage.Parser, new[]{ "Ver", "LgId", "GroupCookie", "MessageId", "AckType", "Message" }, null, new[]{ typeof(global::DistributedMatchEngine.DlgMessage.Types.DlgAck) }, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::DistributedMatchEngine.DlgReply), global::DistributedMatchEngine.DlgReply.Parser, new[]{ "Ver", "AckId", "GroupCookie" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class DlgMessage : pb::IMessage<DlgMessage> {
    private static readonly pb::MessageParser<DlgMessage> _parser = new pb::MessageParser<DlgMessage>(() => new DlgMessage());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<DlgMessage> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::DistributedMatchEngine.DynamicLocationGroupReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public DlgMessage() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public DlgMessage(DlgMessage other) : this() {
      ver_ = other.ver_;
      lgId_ = other.lgId_;
      groupCookie_ = other.groupCookie_;
      messageId_ = other.messageId_;
      ackType_ = other.ackType_;
      message_ = other.message_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public DlgMessage Clone() {
      return new DlgMessage(this);
    }

    /// <summary>Field number for the "ver" field.</summary>
    public const int VerFieldNumber = 1;
    private uint ver_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public uint Ver {
      get { return ver_; }
      set {
        ver_ = value;
      }
    }

    /// <summary>Field number for the "lg_id" field.</summary>
    public const int LgIdFieldNumber = 2;
    private ulong lgId_;
    /// <summary>
    /// Dynamic Location Group Id
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ulong LgId {
      get { return lgId_; }
      set {
        lgId_ = value;
      }
    }

    /// <summary>Field number for the "group_cookie" field.</summary>
    public const int GroupCookieFieldNumber = 3;
    private string groupCookie_ = "";
    /// <summary>
    /// Group Cookie if secure
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string GroupCookie {
      get { return groupCookie_; }
      set {
        groupCookie_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "message_id" field.</summary>
    public const int MessageIdFieldNumber = 4;
    private ulong messageId_;
    /// <summary>
    /// Message ID
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ulong MessageId {
      get { return messageId_; }
      set {
        messageId_ = value;
      }
    }

    /// <summary>Field number for the "ack_type" field.</summary>
    public const int AckTypeFieldNumber = 5;
    private global::DistributedMatchEngine.DlgMessage.Types.DlgAck ackType_ = 0;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::DistributedMatchEngine.DlgMessage.Types.DlgAck AckType {
      get { return ackType_; }
      set {
        ackType_ = value;
      }
    }

    /// <summary>Field number for the "message" field.</summary>
    public const int MessageFieldNumber = 6;
    private string message_ = "";
    /// <summary>
    /// Message
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Message {
      get { return message_; }
      set {
        message_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as DlgMessage);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(DlgMessage other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Ver != other.Ver) return false;
      if (LgId != other.LgId) return false;
      if (GroupCookie != other.GroupCookie) return false;
      if (MessageId != other.MessageId) return false;
      if (AckType != other.AckType) return false;
      if (Message != other.Message) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Ver != 0) hash ^= Ver.GetHashCode();
      if (LgId != 0UL) hash ^= LgId.GetHashCode();
      if (GroupCookie.Length != 0) hash ^= GroupCookie.GetHashCode();
      if (MessageId != 0UL) hash ^= MessageId.GetHashCode();
      if (AckType != 0) hash ^= AckType.GetHashCode();
      if (Message.Length != 0) hash ^= Message.GetHashCode();
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
      if (Ver != 0) {
        output.WriteRawTag(8);
        output.WriteUInt32(Ver);
      }
      if (LgId != 0UL) {
        output.WriteRawTag(16);
        output.WriteUInt64(LgId);
      }
      if (GroupCookie.Length != 0) {
        output.WriteRawTag(26);
        output.WriteString(GroupCookie);
      }
      if (MessageId != 0UL) {
        output.WriteRawTag(32);
        output.WriteUInt64(MessageId);
      }
      if (AckType != 0) {
        output.WriteRawTag(40);
        output.WriteEnum((int) AckType);
      }
      if (Message.Length != 0) {
        output.WriteRawTag(50);
        output.WriteString(Message);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Ver != 0) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(Ver);
      }
      if (LgId != 0UL) {
        size += 1 + pb::CodedOutputStream.ComputeUInt64Size(LgId);
      }
      if (GroupCookie.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(GroupCookie);
      }
      if (MessageId != 0UL) {
        size += 1 + pb::CodedOutputStream.ComputeUInt64Size(MessageId);
      }
      if (AckType != 0) {
        size += 1 + pb::CodedOutputStream.ComputeEnumSize((int) AckType);
      }
      if (Message.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Message);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(DlgMessage other) {
      if (other == null) {
        return;
      }
      if (other.Ver != 0) {
        Ver = other.Ver;
      }
      if (other.LgId != 0UL) {
        LgId = other.LgId;
      }
      if (other.GroupCookie.Length != 0) {
        GroupCookie = other.GroupCookie;
      }
      if (other.MessageId != 0UL) {
        MessageId = other.MessageId;
      }
      if (other.AckType != 0) {
        AckType = other.AckType;
      }
      if (other.Message.Length != 0) {
        Message = other.Message;
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
            Ver = input.ReadUInt32();
            break;
          }
          case 16: {
            LgId = input.ReadUInt64();
            break;
          }
          case 26: {
            GroupCookie = input.ReadString();
            break;
          }
          case 32: {
            MessageId = input.ReadUInt64();
            break;
          }
          case 40: {
            AckType = (global::DistributedMatchEngine.DlgMessage.Types.DlgAck) input.ReadEnum();
            break;
          }
          case 50: {
            Message = input.ReadString();
            break;
          }
        }
      }
    }

    #region Nested types
    /// <summary>Container for nested types declared in the DlgMessage message type.</summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static partial class Types {
      /// <summary>
      /// Need acknowledgement
      /// </summary>
      public enum DlgAck {
        [pbr::OriginalName("DLG_ACK_EACH_MESSAGE")] EachMessage = 0,
        [pbr::OriginalName("DLG_ASY_EVERY_N_MESSAGE")] DlgAsyEveryNMessage = 1,
        [pbr::OriginalName("DLG_NO_ACK")] DlgNoAck = 2,
      }

    }
    #endregion

  }

  public sealed partial class DlgReply : pb::IMessage<DlgReply> {
    private static readonly pb::MessageParser<DlgReply> _parser = new pb::MessageParser<DlgReply>(() => new DlgReply());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<DlgReply> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::DistributedMatchEngine.DynamicLocationGroupReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public DlgReply() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public DlgReply(DlgReply other) : this() {
      ver_ = other.ver_;
      ackId_ = other.ackId_;
      groupCookie_ = other.groupCookie_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public DlgReply Clone() {
      return new DlgReply(this);
    }

    /// <summary>Field number for the "ver" field.</summary>
    public const int VerFieldNumber = 1;
    private uint ver_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public uint Ver {
      get { return ver_; }
      set {
        ver_ = value;
      }
    }

    /// <summary>Field number for the "ack_id" field.</summary>
    public const int AckIdFieldNumber = 2;
    private ulong ackId_;
    /// <summary>
    /// AckId
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ulong AckId {
      get { return ackId_; }
      set {
        ackId_ = value;
      }
    }

    /// <summary>Field number for the "group_cookie" field.</summary>
    public const int GroupCookieFieldNumber = 3;
    private string groupCookie_ = "";
    /// <summary>
    /// Group Cookie for Secure comm
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string GroupCookie {
      get { return groupCookie_; }
      set {
        groupCookie_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as DlgReply);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(DlgReply other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Ver != other.Ver) return false;
      if (AckId != other.AckId) return false;
      if (GroupCookie != other.GroupCookie) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Ver != 0) hash ^= Ver.GetHashCode();
      if (AckId != 0UL) hash ^= AckId.GetHashCode();
      if (GroupCookie.Length != 0) hash ^= GroupCookie.GetHashCode();
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
      if (Ver != 0) {
        output.WriteRawTag(8);
        output.WriteUInt32(Ver);
      }
      if (AckId != 0UL) {
        output.WriteRawTag(16);
        output.WriteUInt64(AckId);
      }
      if (GroupCookie.Length != 0) {
        output.WriteRawTag(26);
        output.WriteString(GroupCookie);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Ver != 0) {
        size += 1 + pb::CodedOutputStream.ComputeUInt32Size(Ver);
      }
      if (AckId != 0UL) {
        size += 1 + pb::CodedOutputStream.ComputeUInt64Size(AckId);
      }
      if (GroupCookie.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(GroupCookie);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(DlgReply other) {
      if (other == null) {
        return;
      }
      if (other.Ver != 0) {
        Ver = other.Ver;
      }
      if (other.AckId != 0UL) {
        AckId = other.AckId;
      }
      if (other.GroupCookie.Length != 0) {
        GroupCookie = other.GroupCookie;
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
            Ver = input.ReadUInt32();
            break;
          }
          case 16: {
            AckId = input.ReadUInt64();
            break;
          }
          case 26: {
            GroupCookie = input.ReadString();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
