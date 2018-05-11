///
//  Generated code. Do not modify.
///
// ignore_for_file: non_constant_identifier_names,library_prefixes
library distributed_match_engine_app_client_pbenum;

// ignore_for_file: UNDEFINED_SHOWN_NAME,UNUSED_SHOWN_NAME
import 'dart:core' show int, dynamic, String, List, Map;
import 'package:protobuf/protobuf.dart';

class Match_Engine_Request_ID_type extends ProtobufEnum {
  static const Match_Engine_Request_ID_type IMEI = const Match_Engine_Request_ID_type._(0, 'IMEI');
  static const Match_Engine_Request_ID_type MSISDN = const Match_Engine_Request_ID_type._(1, 'MSISDN');

  static const List<Match_Engine_Request_ID_type> values = const <Match_Engine_Request_ID_type> [
    IMEI,
    MSISDN,
  ];

  static final Map<int, dynamic> _byValue = ProtobufEnum.initByValue(values);
  static Match_Engine_Request_ID_type valueOf(int value) => _byValue[value] as Match_Engine_Request_ID_type;
  static void $checkItem(Match_Engine_Request_ID_type v) {
    if (v is! Match_Engine_Request_ID_type) checkItemFailed(v, 'Match_Engine_Request_ID_type');
  }

  const Match_Engine_Request_ID_type._(int v, String n) : super(v, n);
}

class Match_Engine_Loc_Verify_Tower_Status extends ProtobufEnum {
  static const Match_Engine_Loc_Verify_Tower_Status UNKNOWN = const Match_Engine_Loc_Verify_Tower_Status._(0, 'UNKNOWN');
  static const Match_Engine_Loc_Verify_Tower_Status CONNECTED_TO_SPECIFIED_TOWER = const Match_Engine_Loc_Verify_Tower_Status._(1, 'CONNECTED_TO_SPECIFIED_TOWER');
  static const Match_Engine_Loc_Verify_Tower_Status NOT_CONNECTED_TO_SPECIFIED_TOWER = const Match_Engine_Loc_Verify_Tower_Status._(2, 'NOT_CONNECTED_TO_SPECIFIED_TOWER');

  static const List<Match_Engine_Loc_Verify_Tower_Status> values = const <Match_Engine_Loc_Verify_Tower_Status> [
    UNKNOWN,
    CONNECTED_TO_SPECIFIED_TOWER,
    NOT_CONNECTED_TO_SPECIFIED_TOWER,
  ];

  static final Map<int, dynamic> _byValue = ProtobufEnum.initByValue(values);
  static Match_Engine_Loc_Verify_Tower_Status valueOf(int value) => _byValue[value] as Match_Engine_Loc_Verify_Tower_Status;
  static void $checkItem(Match_Engine_Loc_Verify_Tower_Status v) {
    if (v is! Match_Engine_Loc_Verify_Tower_Status) checkItemFailed(v, 'Match_Engine_Loc_Verify_Tower_Status');
  }

  const Match_Engine_Loc_Verify_Tower_Status._(int v, String n) : super(v, n);
}

class Match_Engine_Loc_Verify_GPS_Location_Status extends ProtobufEnum {
  static const Match_Engine_Loc_Verify_GPS_Location_Status LOC_UNKNOWN = const Match_Engine_Loc_Verify_GPS_Location_Status._(0, 'LOC_UNKNOWN');
  static const Match_Engine_Loc_Verify_GPS_Location_Status LOC_WITHIN_1M = const Match_Engine_Loc_Verify_GPS_Location_Status._(1, 'LOC_WITHIN_1M');
  static const Match_Engine_Loc_Verify_GPS_Location_Status LOC_WITHIN_10M = const Match_Engine_Loc_Verify_GPS_Location_Status._(2, 'LOC_WITHIN_10M');

  static const List<Match_Engine_Loc_Verify_GPS_Location_Status> values = const <Match_Engine_Loc_Verify_GPS_Location_Status> [
    LOC_UNKNOWN,
    LOC_WITHIN_1M,
    LOC_WITHIN_10M,
  ];

  static final Map<int, dynamic> _byValue = ProtobufEnum.initByValue(values);
  static Match_Engine_Loc_Verify_GPS_Location_Status valueOf(int value) => _byValue[value] as Match_Engine_Loc_Verify_GPS_Location_Status;
  static void $checkItem(Match_Engine_Loc_Verify_GPS_Location_Status v) {
    if (v is! Match_Engine_Loc_Verify_GPS_Location_Status) checkItemFailed(v, 'Match_Engine_Loc_Verify_GPS_Location_Status');
  }

  const Match_Engine_Loc_Verify_GPS_Location_Status._(int v, String n) : super(v, n);
}

