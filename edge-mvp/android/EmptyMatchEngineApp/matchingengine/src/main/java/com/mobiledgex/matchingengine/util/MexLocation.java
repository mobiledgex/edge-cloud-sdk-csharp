package com.mobiledgex.matchingengine.util;

import android.content.Context;
import android.location.Location;

import com.google.android.gms.location.FusedLocationProviderClient;
import com.google.android.gms.location.LocationServices;
import com.google.android.gms.tasks.OnCompleteListener;
import com.google.android.gms.tasks.Task;

import android.util.Log;

public class MexLocation {
    private static final String TAG = "MexLocation";
    Context mContext;
    Location mLastLocation;

    public MexLocation(Context context) {
        mContext = context;
    }

    /**
     * A utility blocking call to location services, otherwise, use standard asynchronous location APIs.
     * Location Access Permissions must be enabled.
     */
    public android.location.Location getBlocking() throws IllegalStateException, SecurityException, InterruptedException {
        if (mContext == null) {
            throw new IllegalStateException("Location util requires a Context.");
        }

        Location newLocation;
        FusedLocationProviderClient fusedLocationClient;
        fusedLocationClient = LocationServices.getFusedLocationProviderClient(mContext);
        Task<android.location.Location> locationTask;
        final Object waiter = new Object();
        try {
            // Simple last Location only.
            locationTask = fusedLocationClient.getLastLocation().addOnCompleteListener(new OnCompleteListener<Location>() {
                @Override
                public void onComplete(Task<Location> task) {
                    if (task.isSuccessful() && task.getResult() != null) {
                        mLastLocation = task.getResult();
                        waiter.notify();
                    } else {
                        Log.w(TAG, "getLastLocation:exception", task.getException());
                    }
                }
            });
            while (!locationTask.isComplete()) {
                waiter.wait();
            }
        } catch (SecurityException se) {
            throw se;
        } catch (InterruptedException ie) {
            throw ie;
        }

        return mLastLocation;
    }

}
