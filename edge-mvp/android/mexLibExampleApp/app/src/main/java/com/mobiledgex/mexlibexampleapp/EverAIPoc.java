package com.mobiledgex.mexlibexampleapp;

import java.io.File;

import android.graphics.Rect;

import java.io.IOException;
import java.net.MalformedURLException;
import java.net.URL;
import java.util.ArrayList;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.Future;
import java.util.concurrent.Callable;


import com.mobiledgex.matchingengine.MatchingEngine;
import com.mobiledgex.matchingengine.CloudletResponseListener;
import com.mobiledgex.matchingengine.FindCloudletResponse;

import com.mobiledgex.mexlibexampleapp.EverAIUploadResponseListener;


import distributed_match_engine.AppClient;
import okhttp3.Call;
import okhttp3.Callback;
import okhttp3.OkHttpClient;
import okhttp3.Request;
import okhttp3.RequestBody;
import okhttp3.MultipartBody;
import okhttp3.Response;
import okhttp3.MediaType;

public class EverAIPoc implements EverAIUploadResponseListener {
    File mFile;

    @Override
    public void OnUploadResponse(ArrayList<Rect> rects) {
        System.out.println(rects.toString());
    }

    private Future<FindCloudletResponse> findClosestCloudlet(AppClient.Match_Engine_Request req) {
        // Find closest cloudlet:
        MatchingEngine task = new MatchingEngine(null);
        Future<FindCloudletResponse> future = task.submit();
        return future;
    }

    public void uploadToEverAI(File file) {
        try {
            // Make request object, and pass non-null.

            Future<FindCloudletResponse> future = findClosestCloudlet(null);
            FindCloudletResponse response = future.get(); // await answer.

            if (response == null) {
                throw new Exception("No response from Matching Engine! Aborting.");
            }

            //return;

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
                    System.out.println(faceRectStr);
                    ArrayList<Rect> rects = GetRectangles(response);
                    OnUploadResponse(rects);
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
    public ArrayList<Rect> GetRectangles(Response response) {
        // TODO: JSON Parse response.
        return new ArrayList<Rect>(); // Empty.
    }



}
