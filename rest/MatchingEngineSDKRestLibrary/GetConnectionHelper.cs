﻿/**
 * Copyright 2019 MobiledgeX, Inc. All rights and licenses reserved.
 * MobiledgeX, Inc. 156 2nd Street #408, San Francisco, CA 94105
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Net.WebSockets;
using System.Net.Security;

using System.Threading.Tasks;
using System.Threading;

namespace DistributedMatchEngine
{

    public partial class MatchingEngine
    {
        // GetTCPConnection helper function
        public async Task<Socket> GetTCPConnection(string host, int port, int timeout)
        {
            // Using integration with IOS or Android sdk, get cellular interface
            IPEndPoint localEndPoint = GetLocalIP(port);
            // Get remote ip of the provided host
            IPAddress remoteIP = Dns.GetHostAddresses(host)[0];
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteIP, port);
            // Create Socket and bind to local ip and connect to remote endpoint
            Socket s = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            s.Bind(localEndPoint);

            TimeoutObj.Reset();
            s.BeginConnect(remoteEndPoint, new AsyncCallback(ConnectCallback), s);
            // WaitOne returns true if TimeoutObj receives a signal (ie. when .Set() is called in the connect callback)
            if (TimeoutObj.WaitOne(timeout * 1000, false)) // WaitOne timeout is in milliseconds

            { 
                if (!s.IsBound && !s.Connected) 
                {
                    throw new GetConnectionException("Could not bind to interface or connect to server");
                }
                else if (!s.IsBound)
                {
                    throw new GetConnectionException("Could not bind to interface");
                }
                else if (!s.Connected)
                {
                    throw new GetConnectionException("Could not connect to server");
                }
                return s;
            }
            else
            {
                throw new GetConnectionException("Timeout");
            }
        }

        // GetTCPTLSConnection helper function
        public async Task<SslStream> GetTCPTLSConnection(string host, int port, int timeout)
        {
            // Using integration with IOS or Android sdk, get cellular interface
            IPEndPoint localEndPoint = GetLocalIP(port);
            // Create tcp client
            TcpClient tcpClient = new TcpClient(localEndPoint);
            //TcpClient tcpClient = new TcpClient();
            var task = tcpClient.ConnectAsync(host, port);
            // Wait returns true if Task completes execution before timeout, false otherwise
            if (task.Wait(TimeSpan.FromSeconds(timeout))) {
                // Create ssl stream on top of tcp client and validate server cert
                using (SslStream sslStream = new SslStream(tcpClient.GetStream(), false,
            new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
                {
                    sslStream.AuthenticateAsClient(host);
                    return sslStream;
                }
            }
            else
            {
                throw new GetConnectionException("Timeout");
            }
        }

        // GetUDPConnection helper function
        public async Task<Socket> GetUDPConnection(string host, int port, int timeout)
        {
            // Using integration with IOS or Android sdk, get cellular interface
            IPEndPoint localEndPoint = GetLocalIP(port);
            // Get remote ip of the provided host
            IPAddress remoteIP = Dns.GetHostAddresses(host)[0];
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteIP, port);
            // Create Socket and bind to local ip and connect to remote endpoint
            Socket s = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(localEndPoint);

            TimeoutObj.Reset();
            s.BeginConnect(remoteEndPoint, new AsyncCallback(ConnectCallback), s);
            // WaitOne returns true if TimeoutObj receives a signal (ie. when .Set() is called in the connect callback)
            if (TimeoutObj.WaitOne(timeout * 1000, false))
            { 
                if (!s.IsBound && !s.Connected) 
                {
                    throw new GetConnectionException("Could not bind to interface or connect to server");
                }
                else if (!s.IsBound)
                {
                    throw new GetConnectionException("Could not bind to interface");
                }
                else if (!s.Connected)
                {
                    throw new GetConnectionException("Could not connect to server");
                }
                return s;
            }
            else
            {
                throw new GetConnectionException("Timeout");
            }
        }

        // GetHTTPClient and GetHTTPSClient helper function
        public async Task<HttpClient> GetHTTPClient(Uri uri)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = uri;
            return httpClient;
        }

        // GetWebsocketConnection helper function
        public async Task<ClientWebSocket> GetWebsocketConnection(string host, int port, int timeout)
        {
            // Initialize websocket client
            ClientWebSocket webSocket = new ClientWebSocket();
            UriBuilder uriBuilder = new UriBuilder("ws", host, port);
            Uri uri = uriBuilder.Uri;
            // Token is used to notify listeners/ delegates of task state
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            // initialize websocket handshake with server
            var task = webSocket.ConnectAsync(uri, token);
            // Wait returns true if Task completes execution before timeout, false otherwise
            if (task.Wait(TimeSpan.FromSeconds(timeout)))
            { 
                if (webSocket.State != WebSocketState.Open)
                {
                    throw new GetConnectionException("Cannot get websocket connection");    
                }
                return webSocket;
            }
            else
            {
                throw new GetConnectionException("Timeout");
            }
        }

        // GetSecureWebsocketConnection helper function
        public async Task<ClientWebSocket> GetSecureWebsocketConnection(string host, int port, int timeout)
        {
            // Initialize websocket class
            ClientWebSocket webSocket = new ClientWebSocket();
            UriBuilder uriBuilder = new UriBuilder("wss", host, port);
            Uri uri = uriBuilder.Uri;
            // Token is used to notify listeners/ delegates of task state
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            // initialize websocket handshake  with server
            var task = webSocket.ConnectAsync(uri, token);
            // Wait returns true if Task completes execution before timeout, false otherwise
            if (task.Wait(TimeSpan.FromSeconds(timeout)))
            { 
                if (webSocket.State != WebSocketState.Open)
                {
                    throw new GetConnectionException("Cannot get websocket connection");
                }
                return webSocket;
            }
            else
            {
                throw new GetConnectionException("Timeout");
            }
        }

    }
}
