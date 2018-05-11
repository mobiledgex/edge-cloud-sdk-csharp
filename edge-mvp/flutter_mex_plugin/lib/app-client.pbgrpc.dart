///
//  Generated code. Do not modify.
///
// ignore_for_file: non_constant_identifier_names,library_prefixes
library distributed_match_engine_app_client_pbgrpc;

import 'dart:async';

import 'package:grpc/grpc.dart';

import 'app-client.pb.dart';
export 'app-client.pb.dart';

class Match_Engine_ApiClient extends Client {
  static final _$findCloudlet =
      new ClientMethod<Match_Engine_Request, Match_Engine_Reply>(
          '/distributed_match_engine.Match_Engine_Api/FindCloudlet',
          (Match_Engine_Request value) => value.writeToBuffer(),
          (List<int> value) => new Match_Engine_Reply.fromBuffer(value));
  static final _$verifyLocation =
      new ClientMethod<Match_Engine_Request, Match_Engine_Loc_Verify>(
          '/distributed_match_engine.Match_Engine_Api/VerifyLocation',
          (Match_Engine_Request value) => value.writeToBuffer(),
          (List<int> value) => new Match_Engine_Loc_Verify.fromBuffer(value));

  Match_Engine_ApiClient(ClientChannel channel, {CallOptions options})
      : super(channel, options: options);

  ResponseFuture<Match_Engine_Reply> findCloudlet(Match_Engine_Request request,
      {CallOptions options}) {
    final call = $createCall(_$findCloudlet, new Stream.fromIterable([request]),
        options: options);
    return new ResponseFuture(call);
  }

  ResponseFuture<Match_Engine_Loc_Verify> verifyLocation(
      Match_Engine_Request request,
      {CallOptions options}) {
    final call = $createCall(
        _$verifyLocation, new Stream.fromIterable([request]),
        options: options);
    return new ResponseFuture(call);
  }
}

abstract class Match_Engine_ApiServiceBase extends Service {
  String get $name => 'distributed_match_engine.Match_Engine_Api';

  Match_Engine_ApiServiceBase() {
    $addMethod(new ServiceMethod<Match_Engine_Request, Match_Engine_Reply>(
        'FindCloudlet',
        findCloudlet_Pre,
        false,
        false,
        (List<int> value) => new Match_Engine_Request.fromBuffer(value),
        (Match_Engine_Reply value) => value.writeToBuffer()));
    $addMethod(new ServiceMethod<Match_Engine_Request, Match_Engine_Loc_Verify>(
        'VerifyLocation',
        verifyLocation_Pre,
        false,
        false,
        (List<int> value) => new Match_Engine_Request.fromBuffer(value),
        (Match_Engine_Loc_Verify value) => value.writeToBuffer()));
  }

  Future<Match_Engine_Reply> findCloudlet_Pre(
      ServiceCall call, Future request) async {
    return findCloudlet(call, await request);
  }

  Future<Match_Engine_Loc_Verify> verifyLocation_Pre(
      ServiceCall call, Future request) async {
    return verifyLocation(call, await request);
  }

  Future<Match_Engine_Reply> findCloudlet(
      ServiceCall call, Match_Engine_Request request);
  Future<Match_Engine_Loc_Verify> verifyLocation(
      ServiceCall call, Match_Engine_Request request);
}
