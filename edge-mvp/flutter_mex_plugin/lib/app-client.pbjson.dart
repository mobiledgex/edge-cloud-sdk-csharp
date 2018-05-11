///
//  Generated code. Do not modify.
///
// ignore_for_file: non_constant_identifier_names,library_prefixes
library distributed_match_engine_app_client_pbjson;

const Loc$json = const {
  '1': 'Loc',
  '2': const [
    const {'1': 'lat', '3': 1, '4': 1, '5': 1, '10': 'lat'},
    const {'1': 'long', '3': 2, '4': 1, '5': 1, '10': 'long'},
    const {'1': 'horizontal_accuracy', '3': 3, '4': 1, '5': 1, '10': 'horizontalAccuracy'},
    const {'1': 'vertical_accuracy', '3': 4, '4': 1, '5': 1, '10': 'verticalAccuracy'},
    const {'1': 'altitude', '3': 5, '4': 1, '5': 1, '10': 'altitude'},
    const {'1': 'course', '3': 6, '4': 1, '5': 1, '10': 'course'},
    const {'1': 'speed', '3': 7, '4': 1, '5': 1, '10': 'speed'},
  ],
};

const Match_Engine_Request$json = const {
  '1': 'Match_Engine_Request',
  '2': const [
    const {'1': 'ver', '3': 1, '4': 1, '5': 13, '10': 'ver'},
    const {'1': 'id_type', '3': 2, '4': 1, '5': 14, '6': '.distributed_match_engine.Match_Engine_Request.ID_type', '10': 'idType'},
    const {'1': 'id', '3': 3, '4': 1, '5': 9, '10': 'id'},
    const {'1': 'carrier', '3': 4, '4': 1, '5': 4, '10': 'carrier'},
    const {'1': 'tower', '3': 5, '4': 1, '5': 4, '10': 'tower'},
    const {'1': 'gps_location', '3': 6, '4': 1, '5': 11, '6': '.distributed_match_engine.Loc', '10': 'gpsLocation'},
    const {'1': 'app_id', '3': 7, '4': 1, '5': 4, '10': 'appId'},
    const {'1': 'protocol', '3': 8, '4': 1, '5': 12, '10': 'protocol'},
    const {'1': 'server_port', '3': 9, '4': 1, '5': 12, '10': 'serverPort'},
  ],
  '4': const [Match_Engine_Request_ID_type$json],
};

const Match_Engine_Request_ID_type$json = const {
  '1': 'ID_type',
  '2': const [
    const {'1': 'IMEI', '2': 0},
    const {'1': 'MSISDN', '2': 1},
  ],
};

const Match_Engine_Reply$json = const {
  '1': 'Match_Engine_Reply',
  '2': const [
    const {'1': 'ver', '3': 1, '4': 1, '5': 13, '10': 'ver'},
    const {'1': 'service_ip', '3': 2, '4': 1, '5': 12, '10': 'serviceIp'},
    const {'1': 'server_port', '3': 3, '4': 1, '5': 13, '10': 'serverPort'},
    const {'1': 'cloudlet_location', '3': 4, '4': 1, '5': 11, '6': '.distributed_match_engine.Loc', '10': 'cloudletLocation'},
  ],
};

const Match_Engine_Loc_Verify$json = const {
  '1': 'Match_Engine_Loc_Verify',
  '2': const [
    const {'1': 'ver', '3': 1, '4': 1, '5': 13, '10': 'ver'},
    const {'1': 'tower_status', '3': 2, '4': 1, '5': 14, '6': '.distributed_match_engine.Match_Engine_Loc_Verify.Tower_Status', '10': 'towerStatus'},
    const {'1': 'gps_location_status', '3': 3, '4': 1, '5': 14, '6': '.distributed_match_engine.Match_Engine_Loc_Verify.GPS_Location_Status', '10': 'gpsLocationStatus'},
  ],
  '4': const [Match_Engine_Loc_Verify_Tower_Status$json, Match_Engine_Loc_Verify_GPS_Location_Status$json],
};

const Match_Engine_Loc_Verify_Tower_Status$json = const {
  '1': 'Tower_Status',
  '2': const [
    const {'1': 'UNKNOWN', '2': 0},
    const {'1': 'CONNECTED_TO_SPECIFIED_TOWER', '2': 1},
    const {'1': 'NOT_CONNECTED_TO_SPECIFIED_TOWER', '2': 2},
  ],
};

const Match_Engine_Loc_Verify_GPS_Location_Status$json = const {
  '1': 'GPS_Location_Status',
  '2': const [
    const {'1': 'LOC_UNKNOWN', '2': 0},
    const {'1': 'LOC_WITHIN_1M', '2': 1},
    const {'1': 'LOC_WITHIN_10M', '2': 2},
  ],
};

