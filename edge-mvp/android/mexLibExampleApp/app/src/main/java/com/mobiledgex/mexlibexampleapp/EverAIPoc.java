package com.mobiledgex.mexlibexampleapp;

import java.io.File;

import android.content.Context;

import java.io.FileNotFoundException;
import java.io.IOException;
import java.net.MalformedURLException;
import java.net.URL;
import java.util.ArrayList;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;


import distributed_match_engine.AppClient;
import com.mobiledgex.matchingengine.MatchingEngine;
import com.mobiledgex.matchingengine.FindCloudletResponse;
import android.location.Location;


import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.MultipartBody;
import okhttp3.Response;
import okhttp3.MediaType;


import android.util.Log;


import java.util.concurrent.locks.ReentrantLock;

public class EverAIPoc {

    public static final String TAG = "EverAIPoc";

    private static final ExecutorService threadpool = Executors.newFixedThreadPool(2);

    private ReentrantLock mFileLock;

    public EverAIPoc(ReentrantLock reentrantLock) {
        mFileLock = reentrantLock;
    }

    public interface OnUploadResponseListener {
        void onUploadResponse(final int width, final int height, final ArrayList<FaceDetection> detections);
    }

    private Future<FindCloudletResponse> findClosestCloudlet(Context context, Location loc) {
        // Find closest cloudlet:
        MatchingEngine task = new MatchingEngine(context, 40000);
        AppClient.Match_Engine_Request req = task.createRequest(loc);
        task.setRequest(req);

        Future<FindCloudletResponse> future = threadpool.submit(task);

        return future;
    }

    /**
     * uploadToEverAI finds the closest cloudlet based on context and location, and responds with the passed upload listener.
     * @param context
     * @param location
     * @param file
     * @param onUploadResponseListener
     * @return
     */
    public void uploadToEverAI(Context context, Location location, File file,
                               final int width, final int height, final OnUploadResponseListener onUploadResponseListener) {

        try {
            if (mFileLock.tryLock()) {
                // Make request object, and pass non-null.
                Future<FindCloudletResponse> future = findClosestCloudlet(context, location); // TODO: Probably don't do this every request.
                FindCloudletResponse response = future.get(); // await answer.
                Log.i(TAG, "YYYYYYYYYY Find Cloudlet Response " + response);


                if (response == null) {
                    //throw new Exception("No response from Matching Engine! Aborting.");
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
                        onUploadResponseListener.onUploadResponse(width, height, detections);
                    }
                });

            }

        } catch (MalformedURLException mue) {
            mue.printStackTrace();
        } catch (InterruptedException ie) {
            ie.printStackTrace();
        } catch (ExecutionException ee) {
            ee.printStackTrace();
        } catch (Exception e) {
            e.printStackTrace();
        } finally {
            mFileLock.unlock();
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
        Log.i(TAG, "Detections found: " + detections);
        return detections;
    }



}
