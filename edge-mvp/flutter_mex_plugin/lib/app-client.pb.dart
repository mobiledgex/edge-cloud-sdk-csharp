///
//  Generated code. Do not modify.
///
// ignore_for_file: non_constant_identifier_names,library_prefixes
library distributed_match_engine_app_client;

// ignore: UNUSED_SHOWN_NAME
import 'dart:core' show int, bool, double, String, List, override;

import 'package:fixnum/fixnum.dart';
import 'package:protobuf/protobuf.dart';

import 'app-client.pbenum.dart';

export 'app-client.pbenum.dart';

class Loc extends GeneratedMessage {
  static final BuilderInfo _i = new BuilderInfo('Loc')
    ..a<double>(1, 'lat', PbFieldType.OD)
    ..a<double>(2, 'long', PbFieldType.OD)
    ..a<double>(3, 'horizontalAccuracy', PbFieldType.OD)
    ..a<double>(4, 'verticalAccuracy', PbFieldType.OD)
    ..a<double>(5, 'altitude', PbFieldType.OD)
    ..a<double>(6, 'course', PbFieldType.OD)
    ..a<double>(7, 'speed', PbFieldType.OD)
    ..hasRequiredFields = false
  ;

  Loc() : super();
  Loc.fromBuffer(List<int> i, [ExtensionRegistry r = ExtensionRegistry.EMPTY]) : super.fromBuffer(i, r);
  Loc.fromJson(String i, [ExtensionRegistry r = ExtensionRegistry.EMPTY]) : super.fromJson(i, r);
  Loc clone() => new Loc()..mergeFromMessage(this);
  BuilderInfo get info_ => _i;
  static Loc create() => new Loc();
  static PbList<Loc> createRepeated() => new PbList<Loc>();
  static Loc getDefault() {
    if (_defaultInstance == null) _defaultInstance = new _ReadonlyLoc();
    return _defaultInstance;
  }
  static Loc _defaultInstance;
  static void $checkItem(Loc v) {
    if (v is! Loc) checkItemFailed(v, 'Loc');
  }

  double get lat => $_getN(0);
  set lat(double v) { $_setDouble(0, v); }
  bool hasLat() => $_has(0);
  void clearLat() => clearField(1);

  double get long => $_getN(1);
  set long(double v) { $_setDouble(1, v); }
  bool hasLong() => $_has(1);
  void clearLong() => clearField(2);

  double get horizontalAccuracy => $_getN(2);
  set horizontalAccuracy(double v) { $_setDouble(2, v); }
  bool hasHorizontalAccuracy() => $_has(2);
  void clearHorizontalAccuracy() => clearField(3);

  double get verticalAccuracy => $_getN(3);
  set verticalAccuracy(double v) { $_setDouble(3, v); }
  bool hasVerticalAccuracy() => $_has(3);
  void clearVerticalAccuracy() => clearField(4);

  double get altitude => $_getN(4);
  set altitude(double v) { $_setDouble(4, v); }
  bool hasAltitude() => $_has(4);
  void clearAltitude() => clearField(5);

  double get course => $_getN(5);
  set course(double v) { $_setDouble(5, v); }
  bool hasCourse() => $_has(5);
  void clearCourse() => clearField(6);

  double get speed => $_getN(6);
  set speed(double v) { $_setDouble(6, v); }
  bool hasSpeed() => $_has(6);
  void clearSpeed() => clearField(7);
}

class _ReadonlyLoc extends Loc with ReadonlyMessageMixin {}

class Match_Engine_Request extends GeneratedMessage {
  static final BuilderInfo _i = new BuilderInfo('Match_Engine_Request')
    ..a<int>(1, 'ver', PbFieldType.OU3)
    ..e<Match_Engine_Request_ID_type>(2, 'idType', PbFieldType.OE, Match_Engine_Request_ID_type.IMEI, Match_Engine_Request_ID_type.valueOf, Match_Engine_Request_ID_type.values)
    ..aOS(3, 'id')
    ..a<Int64>(4, 'carrier', PbFieldType.OU6, Int64.ZERO)
    ..a<Int64>(5, 'tower', PbFieldType.OU6, Int64.ZERO)
    ..a<Loc>(6, 'gpsLocation', PbFieldType.OM, Loc.getDefault, Loc.create)
    ..a<Int64>(7, 'appId', PbFieldType.OU6, Int64.ZERO)
    ..a<List<int>>(8, 'protocol', PbFieldType.OY)
    ..a<List<int>>(9, 'serverPort', PbFieldType.OY)
    ..hasRequiredFields = false
  ;

  Match_Engine_Request() : super();
  Match_Engine_Request.fromBuffer(List<int> i, [ExtensionRegistry r = ExtensionRegistry.EMPTY]) : super.fromBuffer(i, r);
  Match_Engine_Request.fromJson(String i, [ExtensionRegistry r = ExtensionRegistry.EMPTY]) : super.fromJson(i, r);
  Match_Engine_Request clone() => new Match_Engine_Request()..mergeFromMessage(this);
  BuilderInfo get info_ => _i;
  static Match_Engine_Request create() => new Match_Engine_Request();
  static PbList<Match_Engine_Request> createRepeated() => new PbList<Match_Engine_Request>();
  static Match_Engine_Request getDefault() {
    if (_defaultInstance == null) _defaultInstance = new _ReadonlyMatch_Engine_Request();
    return _defaultInstance;
  }
  static Match_Engine_Request _defaultInstance;
  static void $checkItem(Match_Engine_Request v) {
    if (v is! Match_Engine_Request) checkItemFailed(v, 'Match_Engine_Request');
  }

  int get ver => $_get(0, 0);
  set ver(int v) { $_setUnsignedInt32(0, v); }
  bool hasVer() => $_has(0);
  void clearVer() => clearField(1);

  Match_Engine_Request_ID_type get idType => $_getN(1);
  set idType(Match_Engine_Request_ID_type v) { setField(2, v); }
  bool hasIdType() => $_has(1);
  void clearIdType() => clearField(2);

  String get id => $_getS(2, '');
  set id(String v) { $_setString(2, v); }
  bool hasId() => $_has(2);
  void clearId() => clearField(3);

  Int64 get carrier => $_getI64(3);
  set carrier(Int64 v) { $_setInt64(3, v); }
  bool hasCarrier() => $_has(3);
  void clearCarrier() => clearField(4);

  Int64 get tower => $_getI64(4);
  set tower(Int64 v) { $_setInt64(4, v); }
  bool hasTower() => $_has(4);
  void clearTower() => clearField(5);

  Loc get gpsLocation => $_getN(5);
  set gpsLocation(Loc v) { setField(6, v); }
  bool hasGpsLocation() => $_has(5);
  void clearGpsLocation() => clearField(6);

  Int64 get appId => $_getI64(6);
  set appId(Int64 v) { $_setInt64(6, v); }
  bool hasAppId() => $_has(6);
  void clearAppId() => clearField(7);

  List<int> get protocol => $_getN(7);
  set protocol(List<int> v) { $_setBytes(7, v); }
  bool hasProtocol() => $_has(7);
  void clearProtocol() => clearField(8);

  List<int> get serverPort => $_getN(8);
  set serverPort(List<int> v) { $_setBytes(8, v); }
  bool hasServerPort() => $_has(8);
  void clearServerPort() => clearField(9);
}

class _ReadonlyMatch_Engine_Request extends Match_Engine_Request with ReadonlyMessageMixin {}

class Match_Engine_Reply extends GeneratedMessage {
  static final BuilderInfo _i = new BuilderInfo('Match_Engine_Reply')
    ..a<int>(1, 'ver', PbFieldType.OU3)
    ..a<List<int>>(2, 'serviceIp', PbFieldType.OY)
    ..a<int>(3, 'serverPort', PbFieldType.OU3)
    ..a<Loc>(4, 'cloudletLocation', PbFieldType.OM, Loc.getDefault, Loc.create)
    ..hasRequiredFields = false
  ;

  Match_Engine_Reply() : super();
  Match_Engine_Reply.fromBuffer(List<int> i, [ExtensionRegistry r = ExtensionRegistry.EMPTY]) : super.fromBuffer(i, r);
  Match_Engine_Reply.fromJson(String i, [ExtensionRegistry r = ExtensionRegistry.EMPTY]) : super.fromJson(i, r);
  Match_Engine_Reply clone() => new Match_Engine_Reply()..mergeFromMessage(this);
  BuilderInfo get info_ => _i;
  static Match_Engine_Reply create() => new Match_Engine_Reply();
  static PbList<Match_Engine_Reply> createRepeated() => new PbList<Match_Engine_Reply>();
  static Match_Engine_Reply getDefault() {
    if (_defaultInstance == null) _defaultInstance = new _ReadonlyMatch_Engine_Reply();
    return _defaultInstance;
  }
  static Match_Engine_Reply _defaultInstance;
  static void $checkItem(Match_Engine_Reply v) {
    if (v is! Match_Engine_Reply) checkItemFailed(v, 'Match_Engine_Reply');
  }

  int get ver => $_get(0, 0);
  set ver(int v) { $_setUnsignedInt32(0, v); }
  bool hasVer() => $_has(0);
  void clearVer() => clearField(1);

  List<int> get serviceIp => $_getN(1);
  set serviceIp(List<int> v) { $_setBytes(1, v); }
  bool hasServiceIp() => $_has(1);
  void clearServiceIp() => clearField(2);

  int get serverPort => $_get(2, 0);
  set serverPort(int v) { $_setUnsignedInt32(2, v); }
  bool hasServerPort() => $_has(2);
  void clearServerPort() => clearField(3);

  Loc get cloudletLocation => $_getN(3);
  set cloudletLocation(Loc v) { setField(4, v); }
  bool hasCloudletLocation() => $_has(3);
  void clearCloudletLocation() => clearField(4);
}

class _ReadonlyMatch_Engine_Reply extends Match_Engine_Reply with ReadonlyMessageMixin {}

class Match_Engine_Loc_Verify extends GeneratedMessage {
  static final BuilderInfo _i = new BuilderInfo('Match_Engine_Loc_Verify')
    ..a<int>(1, 'ver', PbFieldType.OU3)
    ..e<Match_Engine_Loc_Verify_Tower_Status>(2, 'towerStatus', PbFieldType.OE, Match_Engine_Loc_Verify_Tower_Status.UNKNOWN, Match_Engine_Loc_Verify_Tower_Status.valueOf, Match_Engine_Loc_Verify_Tower_Status.values)
    ..e<Match_Engine_Loc_Verify_GPS_Location_Status>(3, 'gpsLocationStatus', PbFieldType.OE, Match_Engine_Loc_Verify_GPS_Location_Status.LOC_UNKNOWN, Match_Engine_Loc_Verify_GPS_Location_Status.valueOf, Match_Engine_Loc_Verify_GPS_Location_Status.values)
    ..hasRequiredFields = false
  ;

  Match_Engine_Loc_Verify() : super();
  Match_Engine_Loc_Verify.fromBuffer(List<int> i, [ExtensionRegistry r = ExtensionRegistry.EMPTY]) : super.fromBuffer(i, r);
  Match_Engine_Loc_Verify.fromJson(String i, [ExtensionRegistry r = ExtensionRegistry.EMPTY]) : super.fromJson(i, r);
  Match_Engine_Loc_Verify clone() => new Match_Engine_Loc_Verify()..mergeFromMessage(this);
  BuilderInfo get info_ => _i;
  static Match_Engine_Loc_Verify create() => new Match_Engine_Loc_Verify();
  static PbList<Match_Engine_Loc_Verify> createRepeated() => new PbList<Match_Engine_Loc_Verify>();
  static Match_Engine_Loc_Verify getDefault() {
    if (_defaultInstance == null) _defaultInstance = new _ReadonlyMatch_Engine_Loc_Verify();
    return _defaultInstance;
  }
  static Match_Engine_Loc_Verify _defaultInstance;
  static void $checkItem(Match_Engine_Loc_Verify v) {
    if (v is! Match_Engine_Loc_Verify) checkItemFailed(v, 'Match_Engine_Loc_Verify');
  }

  int get ver => $_get(0, 0);
  set ver(int v) { $_setUnsignedInt32(0, v); }
  bool hasVer() => $_has(0);
  void clearVer() => clearField(1);

  Match_Engine_Loc_Verify_Tower_Status get towerStatus => $_getN(1);
  set towerStatus(Match_Engine_Loc_Verify_Tower_Status v) { setField(2, v); }
  bool hasTowerStatus() => $_has(1);
  void clearTowerStatus() => clearField(2);

  Match_Engine_Loc_Verify_GPS_Location_Status get gpsLocationStatus => $_getN(2);
  set gpsLocationStatus(Match_Engine_Loc_Verify_GPS_Location_Status v) { setField(3, v); }
  bool hasGpsLocationStatus() => $_has(2);
  void clearGpsLocationStatus() => clearField(3);
}

class _ReadonlyMatch_Engine_Loc_Verify extends Match_Engine_Loc_Verify with ReadonlyMessageMixin {}

