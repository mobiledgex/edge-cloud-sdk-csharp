// Copyright (c) 2018, MobiledgeX
import 'dart:async';
import 'package:location/location.dart';
/*
import 'package:flutter_mex_plugin/flutter_mex_plugin.dart';

import 'package:flutter_mex_plugin/app-client.pb.dart';
import 'package:flutter_mex_plugin/app-client.pbgrpc.dart';
*/

import 'package:grpc/grpc.dart';

class CloudletRequest {
  // TODO: Define
  String id;
  String carrier;
  String tower;
  List<double> gpsInfo = new List(2); // Long: 0, Lat: 1
  String appInfo;

  CloudletRequest() {
    gpsInfo[0] = 0.0; // Long (0, +/-180 degrees)
    gpsInfo[1] = 1.0; // Lat (0, +/-90 degrees)
  }
}

// Matching Engine wraps grpc and dme together.
class MatchingEngine {

  final String matchingEngineServer = "service.mobiledgex.com/matchingengine/";
  final String matchingEngine = "matchingengine";

  final channel = new ClientChannel('localhost',
      port: 50051,
      options: const ChannelOptions(
          credentials: const ChannelCredentials.insecure()));

  MatchingEngine(); // Empty.
  //Match_Engine_Request dme = new Match_Engine_Request();



  var location = new Location();
  var currentLocation = Map<String, double>();



  // CloudletRequest needs info. Call Platform Plugins:


  /// Returns a Map<String, String> of 2 parts of a request: The server, and the
  /// Service API resource
  Future<Map<String, String>> getCloudletURI(CloudletRequest req) async {
    if (req == null) {
      throw new Exception("CloudletRequest object required."
      );
    }

    try {
      currentLocation = await location.getLocation;
    } catch (e){
      currentLocation = null;
    }

    //var fmexp = new FlutterMexPlugin();
    //var ver = await FlutterMexPlugin.platformVersion;

    // From GPRC
    var channel = new ClientChannel('localhost',
        port: 50051,
        options: const ChannelOptions(
            credentials: const ChannelCredentials.insecure()));


    // FROM Protobuf's Dart Plugin output: Channel != RpcClient
    //final stub = new Match_Engine_ApiClient(channel);

    //var request = new Match_Engine_Request();

    //var response = await stub.findCloudlet(dme);
    //print('client ver received: ${response.ver}');

    // Do post to matching engine
    // TODO: Stub map return
    return {
      "server": "ec2-52-3-246-92.compute-1.amazonaws.com",
      "service": "/api/detect"
    };
  }

}