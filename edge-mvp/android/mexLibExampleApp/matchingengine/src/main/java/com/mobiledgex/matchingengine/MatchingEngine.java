package com.mobiledgex.matchingengine;

import android.content.Context;
import android.content.pm.ApplicationInfo;
import android.telephony.TelephonyManager;
import android.telephony.NeighboringCellInfo;


import com.google.protobuf.ByteString;

import distributed_match_engine.AppClient;
import distributed_match_engine.Match_Engine_ApiGrpc;

import distributed_match_engine.AppClient.Match_Engine_Request;
import distributed_match_engine.LocOuterClass.Loc;
import io.grpc.ManagedChannel;
import io.grpc.ManagedChannelBuilder;

// Concurrency FutureTasks:
import java.util.concurrent.Callable;
import java.lang.ref.WeakReference;
import java.util.ArrayList;
import java.util.List;
import java.util.concurrent.TimeUnit;

import android.location.Location;
import android.util.Log;

// TODO: GRPC (which needs http/2).
public class MatchingEngine implements Callable {
    final String TAG = "MatchingEngine";
    private WeakReference<Context> wContext = null;
    private ManagedChannel mChannel;
    private String host = "192.168.28.162"; // FIXME: Your available external server IP until the real server is up.
    private int port = 50051;
    private int mTimeoutInMilliseconds = 200;

    AppClient.Match_Engine_Request mMatchEngineRequest;

    public MatchingEngine(Context context, int timeoutInMilliseconds) throws SecurityException {
        if (context == null) {
            throw new IllegalArgumentException("MatchingEngine requires a working application context.");
        }
        this.wContext = new WeakReference<Context>(context);
        if (timeoutInMilliseconds > 0) {
            mTimeoutInMilliseconds = timeoutInMilliseconds;
        }
    }

    @Override
    public FindCloudletResponse call() {
        FindCloudletResponse uri = findCloudlet(mMatchEngineRequest);
        return uri;
    }

    public boolean setRequest(Match_Engine_Request request) {
        mMatchEngineRequest = request;
        return true;
    }

    /**
     * The library itself will not directly ask for permissions, the application should before use.
     * This keeps responsibilities managed clearly in one spot under the app's control.
     */
    public Match_Engine_Request createRequest(Location loc) throws SecurityException {
        Context context = wContext.get();

        if (context == null) {
            throw new IllegalStateException("Context is missing. Match_Engine_Request cannot retrieve data to create request.");
        }

        // Operator
        TelephonyManager telManager = (TelephonyManager)wContext.get().getSystemService(Context.TELEPHONY_SERVICE);
        String telName = telManager.getNetworkOperatorName();
        // READ_PHONE_STATE or
        Match_Engine_Request.ID_type id_type = Match_Engine_Request.ID_type.MSISDN;
        String id = telManager.getLine1Number(); // NOT IMEI, if this throws a SecurityException, application must handle it.
        String mnc = telManager.getNetworkOperator();
        String mcc = telManager.getNetworkCountryIso();

        // Tower
        TelephonyManager tm = (TelephonyManager) context.getSystemService(Context.TELEPHONY_SERVICE);
        List<NeighboringCellInfo> neighbors = tm.getNeighboringCellInfo();
        int lac = 0;
        int cid = 0;
        if (neighbors.size() > 0) {
            lac = neighbors.get(0).getLac();
            cid = neighbors.get(0).getCid();
        }

        // App
        ApplicationInfo appInfo = context.getApplicationInfo();
        String packageLabel = "";
        if (context.getPackageManager() != null) {
            CharSequence seq = appInfo.loadLabel(context.getPackageManager());
            if (seq != null) {
                packageLabel = seq.toString();
            }
        }

        // Passed in Location (which is a callback interface)
        Loc aLoc = Loc.newBuilder()
                .setLat((loc == null) ? 0.0d : loc.getLatitude())
                .setLong((loc == null) ? 0.0d : loc.getLongitude())
                .setHorizontalAccuracy((loc == null) ? 0.0d :loc.getAccuracy())
                //.setVerticalAccuracy(loc.getVerticalAccuracyMeters()) // API Level 26 required.
                .setVerticalAccuracy(0d)
                .setAltitude((loc == null) ? 0.0d : loc.getAltitude())
                .setCourse((loc == null) ? 0.0d : loc.getBearing())
                .setSpeed((loc == null) ? 0.0d : loc.getSpeed())
                .build();

        Match_Engine_Request request = AppClient.Match_Engine_Request.newBuilder()
                .setVer(5)
                .setIdType(id_type)
                .setId((id == null) ? "" : id)
                .setCarrier(123456l) // String?
                .setTower(cid) // cid and lac.
                .setTower(123456l)
                .setAppId(87654321l) // String again.
                .setProtocol(ByteString.copyFromUtf8("http")) // This one is appId context sensitive.
                .setServerPort(ByteString.copyFromUtf8("1234")) // App dependent.
                .setGpsLocation(aLoc)
                .build();


        return request;
    }

    private FindCloudletResponse findCloudlet(AppClient.Match_Engine_Request request) {
        FindCloudletResponse uri = new FindCloudletResponse();
        uri.server = "ec2-52-3-246-92.compute-1.amazonaws.com";
        uri.service = "/api/detect";

        AppClient.Match_Engine_Reply reply = null;
        // FIXME: UsePlaintxt means no encryption is enabled to the MatchEngine server!
        ManagedChannel channel = null;
        try {
            channel = ManagedChannelBuilder.forAddress(host, port).usePlaintext().build();
            Match_Engine_ApiGrpc.Match_Engine_ApiBlockingStub stub = Match_Engine_ApiGrpc.newBlockingStub(channel);


            reply = stub.withDeadlineAfter(mTimeoutInMilliseconds, TimeUnit.MILLISECONDS)
                        .findCloudlet(request);
        } catch (Exception e) {
            e.printStackTrace();
        } finally {
            if (channel != null) {
                channel.shutdown();
            }
        }
        // FIXME: Reply TBD.
        if (reply != null) {
            int ver = reply.getVer();
            Log.i(TAG, "Version of Match_Engine_Reply: " + ver);
            Log.i(TAG, "Reply: " + reply.toString());
        }
        return uri;
    }
}
