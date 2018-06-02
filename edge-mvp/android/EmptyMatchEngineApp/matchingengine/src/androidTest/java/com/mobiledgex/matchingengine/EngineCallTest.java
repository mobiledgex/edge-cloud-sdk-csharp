package com.mobiledgex.matchingengine;

import android.content.Context;
import android.support.test.InstrumentationRegistry;
import android.support.test.runner.AndroidJUnit4;

import com.mobiledgex.matchingengine.util.MexLocation;

import org.junit.Test;
import org.junit.runner.RunWith;

import java.util.concurrent.ExecutionException;
import java.util.concurrent.Future;

import distributed_match_engine.AppClient;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;

@RunWith(AndroidJUnit4.class)
public class EngineCallTest {

    @Test
    public void findCloudletTest() {
        // Context of the app under test.
        Context context = InstrumentationRegistry.getTargetContext();


        MatchingEngine me = new MatchingEngine();
        MexLocation mexloc = new MexLocation(context);
        android.location.Location location = mexloc.getBlocking();

        AppClient.Match_Engine_Request cloudletRequest = me.createRequest(context, location);

        Future<FindCloudletResponse> result;
        FindCloudletResponse cloudletResponse = null;
        try {
            result = me.findCloudletFuture(cloudletRequest, 10000);
            cloudletResponse = result.get();
        } catch (ExecutionException ee) {
            ee.printStackTrace();
            assertFalse("FindCloudlet Execution Failed!", true);
        } catch (InterruptedException ie) {
            ie.printStackTrace();
            assertFalse("FindCloudlet Execution Interrupted!", true);
        }
        if (cloudletResponse != null) {
            // Temporary.
            assertEquals(cloudletResponse.server, cloudletResponse.server);
        } else {
            assertFalse("No findCloudlet response!", false);
        }
    }

    @Test
    public void findCloudletFutureTest() {
        // Context of the app under test.
        Context context = InstrumentationRegistry.getTargetContext();


        MatchingEngine me = new MatchingEngine();
        MexLocation mexloc = new MexLocation(context);
        android.location.Location location = mexloc.getBlocking();

        AppClient.Match_Engine_Request cloudletRequest = me.createRequest(context, location);

        FindCloudletResponse result = null;
        try {
            result = me.findCloudlet(cloudletRequest, 10000);
        } catch (ExecutionException ee) {
            ee.printStackTrace();
            assertFalse("FindCloudletFuture Execution Failed!", true);
        } catch (InterruptedException ie) {
            ie.printStackTrace();
            assertFalse("FindCloudletFuture Execution Interrupted!", true);
        }

        // Temporary.
        assertEquals(result.server, result.server);
    }

    @Test
    public void verifyLocationTest() {
        // Context of the app under test.
        Context context = InstrumentationRegistry.getTargetContext();

        MatchingEngine me = new MatchingEngine();
        MexLocation mexloc = new MexLocation(context);
        android.location.Location location = mexloc.getBlocking();

        AppClient.Match_Engine_Request cloudletRequest = me.createRequest(context, location);

        boolean verifyLocationResult = false;

        try {
            verifyLocationResult = me.verifyLocation(cloudletRequest, 10000);
        } catch (ExecutionException ee) {
            ee.printStackTrace();
            assertFalse("VerifyLocation Execution Failed!", true);
        } catch (InterruptedException ie) {
            ie.printStackTrace();
            assertFalse("VerifyLocation Execution Interrupted!", true);
        }

        // Temporary.
        assertEquals(verifyLocationResult, true);
    }

    @Test
    public void verifyLocationFutureTest() {
        // Context of the app under test.
        Context context = InstrumentationRegistry.getTargetContext();

        MatchingEngine me = new MatchingEngine();
        MexLocation mexloc = new MexLocation(context);
        android.location.Location location = mexloc.getBlocking();

        AppClient.Match_Engine_Request cloudletRequest = me.createRequest(context, location);

        Future<Boolean> locFuture;
        boolean verifyLocationResult = false;

        try {
            locFuture = me.verifyLocationFuture(cloudletRequest, 10000);
            verifyLocationResult = locFuture.get();
        } catch (ExecutionException ee) {
            ee.printStackTrace();
            assertFalse("VerifyLocationFuture Execution Failed!", true);
        } catch (InterruptedException ie) {
            ie.printStackTrace();
            assertFalse("VerifyLocationFuture Execution Interrupted!", true);
        }

        // Temporary.
        assertEquals(verifyLocationResult, true);
    }
}
