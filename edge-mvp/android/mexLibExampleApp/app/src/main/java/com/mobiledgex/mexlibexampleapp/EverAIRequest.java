package com.mobiledgex.mexlibexampleapp;

import android.content.Context;
import android.location.Location;

import java.io.File;

public class EverAIRequest {
    public EverAIRequest(Context context, Location location, File file, EverAIPoc.OnUploadResponseListener listener) {
        this.setContext(context);
        this.setLocation(location);
        this.setFile(file);
        this.setListener(listener);
    }
    private Context context;
    private Location location;
    private File file;

    private EverAIPoc.OnUploadResponseListener listener;

    public Context getContext() {
        return context;
    }

    public void setContext(Context context) {
        this.context = context;
    }

    public Location getLocation() {
        return location;
    }

    public void setLocation(Location location) {
        this.location = location;
    }

    public File getFile() {
        return file;
    }

    public void setFile(File file) {
        this.file = file;
    }

    public EverAIPoc.OnUploadResponseListener getListener() {
        return listener;
    }

    public void setListener(EverAIPoc.OnUploadResponseListener listener) {
        this.listener = listener;
    }
}