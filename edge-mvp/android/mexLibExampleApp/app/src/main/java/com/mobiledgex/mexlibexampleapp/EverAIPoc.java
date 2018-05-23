package com.mobiledgex.mexlibexampleapp;

import java.io.File;

import android.graphics.Rect;
import android.content.Context;

import java.io.IOException;
import java.net.MalformedURLException;
import java.net.URL;
import java.util.ArrayList;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

import com.mobiledgex.matchingengine.MatchingEngine;
import com.mobiledgex.matchingengine.FindCloudletResponse;
import android.location.Location;


import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import distributed_match_engine.AppClient;
import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.MultipartBody;
import okhttp3.Response;
import okhttp3.MediaType;

import android.util.Log;

public class EverAIPoc {

    public static final String TAG = "EverAIPoc";

    private static final ExecutorService threadpool = Executors.newFixedThreadPool(1);

    public Location getLocation() {
        return mLocation;
    }

    public void setLocation(Location mLocation) {
        this.mLocation = mLocation;
    }

    Location mLocation = null;

    public interface OnUploadResponseListener {
        void onUploadResponse(ArrayList<FaceDetection> detections);
    }

    private Future<FindCloudletResponse> findClosestCloudlet(Context context, /* AppServiceInfo appInfo, */ Location loc) {
        // Find closest cloudlet:
        MatchingEngine task = new MatchingEngine(context);
        AppClient.Match_Engine_Request req = task.createRequest(loc);

        Future<FindCloudletResponse> future = threadpool.submit(task);
        return future;
    }

    public void uploadToEverAI(Context context, File file, final OnUploadResponseListener onUploadResponseListener) {
        try {
            // Make request object, and pass non-null.
            Future<FindCloudletResponse> future = findClosestCloudlet(context, mLocation);
            FindCloudletResponse response = future.get(); // await answer.

            if (response == null) {
                throw new Exception("No response from Matching Engine! Aborting.");
            }

            // HTTP: Post file, with onResponse() callback:
            if (file == null || !file.exists()) {
                throw new Exception("For some reason, there's no file!");
            }
            System.out.println("File size: " + file.length());
            URL url = new URL("http://" + response.server + response.service);
            OkHttpClient client = new OkHttpClient();

            MediaType type = MediaType.parse("image/jpeg");
            RequestBody requestBody = new MultipartBody.Builder()
                    .setType(MultipartBody.FORM)
                    .addFormDataPart("image", file.getName(),
                            RequestBody.create(type, file))
                    .build();

            Request req = new Request.Builder()
                    .url(url)
                    .post(requestBody)
                    .build();
            client.newCall(req).enqueue(new Callback() {
                @Override
                public void onFailure(Call call, IOException e) {
                    call.cancel();
                }

                @Override
                public void onResponse(Call call, Response response) throws IOException {
                    final String faceRectStr = response.body().string();
                    ArrayList<FaceDetection> detections = GetRectangles(faceRectStr);
                    onUploadResponseListener.onUploadResponse(detections);
                }
            });
        } catch (MalformedURLException mue) {
            mue.printStackTrace();
        }catch (InterruptedException ie) {
            ie.printStackTrace();
        } catch (ExecutionException ee) {
            ee.printStackTrace();
        } catch (Exception e) {
            e.printStackTrace();
        }

    }

    public ArrayList<FaceDetection> GetRectangles(String faceRectStr) {
        // TODO: JSON Parse response
        ArrayList<FaceDetection> detections = new ArrayList<>();
        try {
            JSONObject jsonObject = new JSONObject(faceRectStr);
            JSONArray faces = jsonObject.getJSONArray("faces");
            if (faces != null && faces.length() > 0) {
                FaceDetection face = new FaceDetection();

                for (int i = 0; i < faces.length(); i++) {
                    JSONObject box = faces.getJSONObject(i).getJSONObject("bounding_box");
                    face.x = box.getInt("x");
                    face.y = box.getInt("y");
                    face.width = box.getInt("width");
                    face.height = box.getInt("height");
                    detections.add(face);
                }
            }
        } catch (JSONException jsone) {
            Log.e(TAG, "JSONException: " + jsone.getStackTrace());
        }

        return detections;
    }



}
