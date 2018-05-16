package com.mobiledgex.fluttermexplugin

import io.flutter.plugin.common.MethodChannel
import io.flutter.plugin.common.MethodChannel.MethodCallHandler
import io.flutter.plugin.common.MethodChannel.Result
import io.flutter.plugin.common.MethodCall
import io.flutter.plugin.common.PluginRegistry
import io.flutter.plugin.common.PluginRegistry.Registrar

import android.Manifest
import android.content.pm.PackageManager
import android.support.v4.app.ActivityCompat
import android.app.Activity
import android.content.Context

private class MexPluginPermissionsListener

class FlutterMexPlugin(): MethodCallHandler {
  private var mRegistrar: Registrar? = null

  companion object {
    @JvmStatic
    fun registerWith(registrar: Registrar): Unit {
      val channel = MethodChannel(registrar.messenger(), "flutter_mex_plugin")
      channel.setMethodCallHandler(FlutterMexPlugin(registrar))
    }

    @JvmStatic
    val APP_ON_PERMISSION_RESULT_READ_PHONE_STATE: Int = 1
  }

  constructor(registrar: Registrar): this() {
    this.mRegistrar = registrar
    doCheckPermissions()
  }

  override fun onMethodCall(call: MethodCall, result: Result): Unit {
    println("XXXXXXXXXXXXXXX")
    when (call.method) {
      "getPlatformVersion" -> {
        println("ZXXXXXXXXX")
        result.success("Android ${android.os.Build.VERSION.RELEASE}")
      }
      "getOperatorInfo" -> {
        println("YYYYYYYYYY")
        val operatorInfo = getOperatorInfo()
        if (operatorInfo != null) {
          result.success(operatorInfo)
        } else {
          result.error("UNAVAILABLE", "Operator Not Available.", null)
        }
      }
      else -> {
        result.notImplemented()
      }
    }

  }

  private fun getOperatorInfo(): Map<String, String> {
    if (mRegistrar == null) {
      val ops = mapOf("foobort" to "nuke")
      return ops
    }
    val context = mRegistrar?.context() as android.content.Context
    val telephonyManager = context.getSystemService(android.content.Context.TELEPHONY_SERVICE)
            as android.telephony.TelephonyManager

    // https://developer.android.com/reference/android/telephony/TelephonyManager.html
    val ops = mapOf(
            "name" to telephonyManager.getNetworkOperatorName() as String,
            "id" to telephonyManager.getDeviceId() as String, // IMEI
            "mnc" to telephonyManager.getNetworkOperator() as String, // Mnc + mcc. Hm...
            "mcc" to telephonyManager.getNetworkCountryIso() as String
    )
    return ops
  }

  private fun doCheckPermissions() {
    val context: Context = mRegistrar?.context() as Context
    val activity: Activity = mRegistrar?.activity() as Activity

    if (activity == null || context == null) {
      return
    }

    if (activity.checkSelfPermission(Manifest.permission.READ_PHONE_STATE)
            != PackageManager.PERMISSION_GRANTED) {

      // Permission is not granted
      // Should we show an explanation?
      if (ActivityCompat.shouldShowRequestPermissionRationale(activity, Manifest.permission.READ_PHONE_STATE)) {
        // Show an explanation to the user *asynchronously* -- don't block
        // this thread waiting for the user's response! After the user
        // sees the explanation, try again to request the permission.
      } else {
        // No explanation needed, we can request the permission.
        //mRegistrar?.addRequestPermissionsResultListener(MexPluginRequestPermissionsResultListener(activity))
        //ActivityCompat.requestPermissions(activity, arrayOf(Manifest.permission.READ_PHONE_STATE),
        //        APP_ON_PERMISSION_RESULT_READ_PHONE_STATE)
        activity.requestPermissions(arrayOf(Manifest.permission.READ_PHONE_STATE),
                APP_ON_PERMISSION_RESULT_READ_PHONE_STATE)
      }
    } else {
      // Permission has already been granted
    }
  }
}


private class MexPluginRequestPermissionsResultListener :
        PluginRegistry.RequestPermissionsResultListener {
  private var activity: Activity
  constructor(activity: Activity): super() {
    this.activity = activity
  }

  override fun onRequestPermissionsResult(id: Int,
                                 permissions: Array<String>,
                                 grantResults: IntArray): Boolean {
    if (id == FlutterMexPlugin.APP_ON_PERMISSION_RESULT_READ_PHONE_STATE) {
      ActivityCompat.requestPermissions(activity, arrayOf(Manifest.permission.READ_PHONE_STATE),
              FlutterMexPlugin.APP_ON_PERMISSION_RESULT_READ_PHONE_STATE)
      return true
    }
    return false
  }
}