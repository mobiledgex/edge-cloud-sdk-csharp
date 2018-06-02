package com.mobiledgex.matchingengine;

import android.content.Context;
import android.content.pm.ApplicationInfo;
import android.telephony.NeighboringCellInfo;
import android.telephony.TelephonyManager;

import com.google.protobuf.ByteString;

import java.util.List;
import java.util.concurrent.Callable;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

import distributed_match_engine.AppClient;
import distributed_match_engine.AppClient.Match_Engine_Request;
import distributed_match_engine.LocOuterClass.Loc;



// TODO: GRPC (which needs http/2).
public class MatchingEngine {
    final String TAG = "MatchingEngine";
    private String host = "192.168.28.162"; // FIXME: Your available external server IP until the real server is up.
    private int port = 50051;

    // A threadpool all the MatchEngine API callable interfaces:
    final ExecutorService threadpool;

    public MatchingEngine() {
        threadpool = Executors.newSingleThreadExecutor();
    }
    public MatchingEngine(short threadpoolSize) {
        threadpool = Executors.newFixedThreadPool(threadpoolSize);
    }

    public Future submit(Callable task) {
        return threadpool.submit(task);
    }

    /**
     * The library itself will not directly ask for permissions, the application should before use.
     * This keeps responsibilities managed clearly in one spot under the app's control.
     */
    public Match_Engine_Request createRequest(Context context, android.location.Location loc) throws SecurityException {
        if (context == null) {
            throw new IllegalArgumentException("MatchingEngine requires a working application context.");
        }

        if (context == null) {
            throw new IllegalStateException("Context is missing. Match_Engine_Request cannot retrieve data to create request.");
        }

        // Operator
        TelephonyManager telManager = (TelephonyManager)context.getSystemService(Context.TELEPHONY_SERVICE);
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

    /**
     * findCloudlet finds the closest cloudlet instance as per request.
     * @param request
     * @return cloudlet URI.
     * @throws InterruptedException
     * @throws ExecutionException
     */
    public FindCloudletResponse findCloudlet(AppClient.Match_Engine_Request request, long timeoutInMilliseconds)
            throws InterruptedException, ExecutionException {
        FindCloudletResponse uri;

        FindCloudlet findCloudlet = new FindCloudlet(this);
        findCloudlet.setRequest(request, timeoutInMilliseconds);

        Future<FindCloudletResponse> response = submit(findCloudlet);
        uri = response.get();
        return uri;
    }

    /**
     * findCloudlet finds the closest cloudlet instance as per request. Returns a Future.
     * @param request
     * @return cloudlet URI Future.
     */
    public Future<FindCloudletResponse> findCloudletFuture(AppClient.Match_Engine_Request request, long timeoutInMilliseconds) {
        FindCloudlet findCloudlet = new FindCloudlet(this);
        findCloudlet.setRequest(request, timeoutInMilliseconds);
        return submit(findCloudlet);
    }


    /**
     * verifyLocationFuture validates the client submitted information against known network
     * parameters on the subscriber network side.
     * @param request
     * @return boolean validated or not.
     * @throws InterruptedException
     * @throws ExecutionException
     */
    public boolean verifyLocation(AppClient.Match_Engine_Request request, long timeoutInMilliseconds)
                    throws InterruptedException, ExecutionException {
        boolean verifyResponse = false;

        VerifyLocation verifyLocation = new VerifyLocation(this);
        verifyLocation.setRequest(request, timeoutInMilliseconds);

        Future<Boolean> response = submit(verifyLocation);
        verifyResponse = response.get();
        return verifyResponse;
    }

    /**
     * verifyLocationFuture validates the client submitted information against known network
     * parameters on the subscriber network side. Returns a future.
     * @param request
     * @return Future<Boolean> validated or not.
     */
    public Future<Boolean> verifyLocationFuture(AppClient.Match_Engine_Request request, long timeoutInMilliseconds) {
        FindCloudlet findCloudlet = new FindCloudlet(this);
        findCloudlet.setRequest(request, timeoutInMilliseconds);
        return submit(findCloudlet);
    }

    public String getHost() {
        return host;
    }

    public void setHost(String host) {
        this.host = host;
    }

    public int getPort() {
        return port;
    }

    public void setPort(int port) {
        this.port = port;
    }
}
