package com.mobiledgex.emptymatchengineapp;

import android.app.Activity;
import android.location.Location;
import android.os.Bundle;
import android.support.design.widget.FloatingActionButton;
import android.support.design.widget.Snackbar;
import android.content.Context;
import android.support.v4.widget.TextViewCompat;
import android.support.v7.app.AppCompatActivity;
import android.support.v7.widget.Toolbar;
import android.util.Log;
import android.view.View;
import android.view.Menu;
import android.view.MenuItem;
import android.widget.TextView;
import android.widget.Toast;

// Matching Engine API:
import com.mobiledgex.matchingengine.FindCloudletResponse;
import com.mobiledgex.matchingengine.MatchingEngine;
import com.mobiledgex.matchingengine.util.MexLocation;
import com.mobiledgex.matchingengine.util.RequestPermissions;

import java.util.concurrent.ExecutionException;

import distributed_match_engine.AppClient;


// Location API:
import com.google.android.gms.location.FusedLocationProviderClient;
import com.google.android.gms.location.LocationServices;
import com.google.android.gms.tasks.OnCompleteListener;
import com.google.android.gms.tasks.Task;

public class MainActivity extends AppCompatActivity {
    private static final String TAG = "MainActivity";
    MatchingEngine mMatchingEngine;
    String someText = null;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        final Activity activity = this;


        /**
         * MatchEngine APIs require special user apporved permissions to READ_PHONE_STATE and
         * one of the following:
         * ACCESS_FINE_LOCATION or ACCESS_COARSE_LOCATION. This creates a dialog.
         */
        RequestPermissions rp = new RequestPermissions();
        rp.requestMultiplePermissions(this);


        mMatchingEngine = new MatchingEngine();


        setContentView(R.layout.activity_main);
        Toolbar toolbar = (Toolbar) findViewById(R.id.toolbar);
        setSupportActionBar(toolbar);

        FloatingActionButton fab = (FloatingActionButton) findViewById(R.id.fab);
        fab.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                doEnhancedLocationVerification();
            }
        });

    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_main, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        //noinspection SimplifiableIfStatement
        if (id == R.id.action_settings) {
            return true;
        }

        return super.onOptionsItemSelected(item);
    }

    @Override
    public void onResume() {
        super.onResume();

        //doEnhancedLocationVerification();
    }

    public void doEnhancedLocationVerification() throws SecurityException {
        final Activity ctx = this;

        // As of Android 23, permissions can be asked for while the app is still running.
        RequestPermissions rp = new RequestPermissions();
        if (rp.getNeededPermissions(this).size() > 0) {
            rp.requestMultiplePermissions(this);
            return;
        }

        FusedLocationProviderClient fusedLocationClient = LocationServices.getFusedLocationProviderClient(ctx);

        fusedLocationClient.getLastLocation().addOnCompleteListener(new OnCompleteListener<Location>() {
            @Override
            public void onComplete(Task<Location> task) {
                if (task.isSuccessful() && task.getResult() != null) {
                    Location location = task.getResult();
                    try {

                        // Location found. Create a request:
                        AppClient.Match_Engine_Request req = mMatchingEngine.createRequest(
                                ctx,
                                location);

                        // Location Verification (Blocking, or use verifyLocationFuture):
                        boolean verifiedLocation = mMatchingEngine.verifyLocation(req, 10000);
                        someText = "[Location Verified: " + verifiedLocation + "]\n";


                        // Closest Cloudlet (Blocking, or use findCloudletFuture):
                        FindCloudletResponse closestCloudlet = mMatchingEngine.findCloudlet(req, 10000);
                        someText += "[Cloudlet Server: " + closestCloudlet.server + ", Service: " + closestCloudlet.service + "]";
                        TextView tv = findViewById(R.id.content_text);
                        tv.setText(someText);
                    } catch (InterruptedException ie) {
                        ie.printStackTrace();
                    } catch (ExecutionException ee) {
                        ee.printStackTrace();
                    }
                } else {
                    Log.w(TAG, "getLastLocation:exception", task.getException());
                }
            }
        });
    }
}
