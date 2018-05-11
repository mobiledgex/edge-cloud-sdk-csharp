import 'dart:async';

import 'package:flutter/services.dart';

class FlutterMexPlugin {
  static const MethodChannel _channel =
      const MethodChannel('flutter_mex_plugin');

  static Future<String> get platformVersion async {
    final String version = await _channel.invokeMethod('getPlatformVersion');
    return version;
  }

  /*
  static Future<Map<String,double>> getAppInfo() async {
    final result = await _channel.invokeMethod("getApp");
    return result;
  }

  static Future<Map<String,double>> getOperatorInfo() async {
    final result = await _channel.invokeMethod("getOperatorInfo");
    return result;
  }*/

}
