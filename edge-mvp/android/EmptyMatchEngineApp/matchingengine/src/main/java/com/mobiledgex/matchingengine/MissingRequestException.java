package com.mobiledgex.matchingengine;

import java.util.concurrent.ExecutionException;

public class MissingRequestException extends ExecutionException {
    public MissingRequestException(String msg) {
        super(msg);
    }
}
