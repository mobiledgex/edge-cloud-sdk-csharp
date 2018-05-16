import Flutter
import UIKit

import CoreTelephony
    
public class SwiftFlutterMexPlugin: NSObject, FlutterPlugin {
  public static func register(with registrar: FlutterPluginRegistrar) {
    let channel = FlutterMethodChannel(name: "flutter_mex_plugin", binaryMessenger: registrar.messenger())
    let instance = SwiftFlutterMexPlugin()
    registrar.addMethodCallDelegate(instance, channel: channel)
  }

  public func handle(_ call: FlutterMethodCall, result: @escaping FlutterResult) {
    switch (call.method) {
      case "getPlatformVersion":
        result("iOS " + UIDevice.current.systemVersion)
        break;

      case "getBatteryLevel":
        self.receiveBatteryLevel(result: result);
        break;
      case "getApp":
        self.getApp(result: result);
        break;
      case "getOperatorInfo":
        self.getOperatorInfo(result: result);
        break;
      //case "getLocationInfo":
      //  self.getLocationInfo(result: result);
      //  break;
      default:
        result(FlutterMethodNotImplemented);
      }
  }

  private func receiveBatteryLevel(result: FlutterResult) {
    let device = UIDevice.current;
    device.isBatteryMonitoringEnabled = true;
    if (device.batteryState == UIDeviceBatteryState.unknown) {
      result(FlutterError.init(code: "UNAVAILABLE",
                               message: "Battery info unavailable",
                               details: nil));
    } else {
      result(Int(device.batteryLevel * 100));
    }
  }

  private func getOperatorInfo(result: FlutterResult) {
    // TODO
    let mob = CTTelephonyNetworkInfo()
    var strMap = [String : String]()
    if let r = mob.subscriberCellularProvider { //creates CTCarrierObject
      strMap["name"] = r.carrierName
      strMap["id"] = nil // IMEI is no longer allowed in Apps.
      strMap["mnc"] = r.mobileNetworkCode
      strMap["mcc"] = r.mobileCountryCode
      result(strMap)
    } else {
      // Empty Map.
      result(strMap)
    }
  }

  private func getApp(result: FlutterResult) {
    var strMap = [String : String]()
    strMap["dev_name"] = "DEV_NAME_NOT_IMPLEMENTED";
    strMap["app_name"] = Bundle.main.object(forInfoDictionaryKey: "CFBundleName") as? String ?? "";
    strMap["version"] = Bundle.main.object(forInfoDictionaryKey: "CFBundleVersion") as? String ?? "";
    result(strMap);
  }

  private func getLocationInfo(result: FlutterResult) {
    // TODO
    var strMap = [String : Double]()
    strMap["lat"] = 0.0;
    strMap["long"] = 0.0;
    strMap["horizontal_accuracy"] = 0.0;
    strMap["vertical_accuracy"] = 0.0;
    strMap["altitude"] = 0.0;
    strMap["course"] = 0.0;
    strMap["speed"] = 0.0;
    strMap["timestamp"] = Double(Date().timeIntervalSince1970 * 1000);
    result(strMap);
  }
}
