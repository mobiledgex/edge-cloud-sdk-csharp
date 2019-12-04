/**
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
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

using System.Threading.Tasks;
using System.Threading;

namespace DistributedMatchEngine
{

    class EmptyNetInterface: NetInterface
    {
        public string GetIPAddress()
        {
            return null;
        }

        public bool IsWifi()
        {
            return false;
        }

        public bool IsCellular()
        {
            return false;
        }
    }

    public partial class MatchingEngine
    {
        private static ManualResetEvent TimeoutObj = new ManualResetEvent(false);

        public async Task<Socket> RegisterAndFindTCPConnection(string carrierName, string developerName, string appName, string appVersion, string authToken, Loc loc)
        {
            // Register Client
            RegisterClientRequest registerRequest = CreateRegisterClientRequest(carrierName, developerName, appName, appVersion, authToken);
            await RegisterClient(registerRequest);
            // Find Cloudlet
            FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(carrierName, developerName, appName, appVersion, loc);
            FindCloudletReply findCloudletReply = await FindCloudlet(findCloudletRequest);

            List<AppPort> ports = GetTCPPorts(findCloudletReply);
            // Make sure there is a TCP port
            if (ports.Count == 0) {
                throw new GetConnectionException("No TCP Ports returned in findCloudletReply");
            }

            AppPort port = ports[0]; // Choose 1st port in list
            string host = findCloudletReply.fqdn;
            string fqdnPrefix = port.fqdn_prefix;
            host = fqdnPrefix + host;  // concatenate fqdn prefix associated with port chosen
            int publicPort = port.public_port;

            return await GetTCPConnection(host, publicPort, 5);
        }

        public async Task<Socket> RegisterAndFindUDPConnection(string carrierName, string developerName, string appName, string appVersion, string authToken, Loc loc)
        {
            // Register Client
            RegisterClientRequest registerRequest = CreateRegisterClientRequest(carrierName, developerName, appName, appVersion, authToken);
            await RegisterClient(registerRequest);
            // Find Cloudlet
            FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(carrierName, developerName, appName, appVersion, loc);
            FindCloudletReply findCloudletReply = await FindCloudlet(findCloudletRequest);

            List<AppPort> ports = GetUDPPorts(findCloudletReply);
            // Make sure there is a UDP port
            if (ports.Count == 0) {
                throw new GetConnectionException("No UDP Ports returned in findCloudletReply");
            }

            AppPort port = ports[0];

            string host = findCloudletReply.fqdn;
            string fqdnPrefix = port.fqdn_prefix;
            host = fqdnPrefix + host;
            int publicPort = port.public_port;

            return await GetUDPConnection(host, publicPort, 5);
        }

        public async Task<HttpClient> RegisterAndFindHTTPConnection(string carrierName, string developerName, string appName, string appVersion, string authToken, Loc loc)
        {
            // Register Client
            RegisterClientRequest registerRequest = CreateRegisterClientRequest(carrierName, developerName, appName, appVersion, authToken);
            await RegisterClient(registerRequest);
            // Find Cloudlet
            FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(carrierName, developerName, appName, appVersion, loc);
            FindCloudletReply findCloudletReply = await FindCloudlet(findCloudletRequest);

            List<AppPort> ports = GetHTTPPorts(findCloudletReply);
            // Make sure there is an HTTP port
            if (ports.Count == 0) {
                throw new GetConnectionException("No HTTP Ports returned in findCloudletReply");
            }

            AppPort port = ports[0];

            string host = findCloudletReply.fqdn;
            string fqdnPrefix = port.fqdn_prefix;
            host = fqdnPrefix + host;
            int publicPort = port.public_port;

            return await GetHTTPConnection(host, publicPort);
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket) ar.AsyncState;  
            // Complete the connection.  
            client.EndConnect(ar);  
            TimeoutObj.Set();
        }

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
            // Uses milliseconds
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

        public async Task<SslStream> GetTCPTLSConnection(string host, int port, int timeout)
        {
            // Using integration with IOS or Android sdk, get cellular interface
            IPEndPoint localEndPoint = GetLocalIP(port);
            // Create tcp client
            TcpClient tcpClient = new TcpClient(localEndPoint);
            //TcpClient tcpClient = new TcpClient();
            var task = tcpClient.ConnectAsync(host, port);
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

        public async Task<HttpClient> GetHTTPConnection(string host, int port)
        {
            // Initialize http client
            HttpClient httpClient = new HttpClient();
            UriBuilder uriBuilder = new UriBuilder("http", host, port);
            Uri uri = uriBuilder.Uri;
            // Set address of URI http client will send requests to
            httpClient.BaseAddress = uri;
            return httpClient;
        }

        public async Task<HttpClient> GetHTTPSConnection(string host, int port)
        {
            // Initialize http client
            HttpClient httpClient = new HttpClient();
            UriBuilder uriBuilder = new UriBuilder("https", host, port);
            Uri uri = uriBuilder.Uri;
            // Set address of URI https client will send requests to
            httpClient.BaseAddress = uri;
            return httpClient;
        }

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

        private IPEndPoint GetLocalIP(int port)
        {
            if (netInterface == null)
            {
                throw new GetConnectionException("Have not integrated NetworkInterface");
            }
            string host = netInterface.GetIPAddress();
            if (host == null)
            {
                throw new GetConnectionException("Could not get Cellular interface");
            }
            // Gets IP address of host
            IPAddress localIP = Dns.GetHostAddresses(host)[0];
            IPEndPoint localEndPoint = new IPEndPoint(localIP, port);
            return localEndPoint;
        }


        public List<AppPort> GetPortsByProtocol(FindCloudletReply reply, LProto proto)
        {
            List<AppPort> portsByProtocol = new List<AppPort>();
            AppPort[] ports = reply.ports;
            foreach (AppPort port in ports)
            {
                if (port.proto == proto)
                {
                    portsByProtocol.Add(port);
                }
            }
            return portsByProtocol;
        }

        public List<AppPort> GetTCPPorts(FindCloudletReply reply)
        {
            List<AppPort> tcpPorts = new List<AppPort>();
            AppPort[] ports = reply.ports;
            foreach (AppPort port in ports)
            {
                if (port.proto == LProto.L_PROTO_TCP)
                {
                    tcpPorts.Add(port);
                }
            }
            return tcpPorts;
        }

        public List<AppPort> GetUDPPorts(FindCloudletReply reply)
        {
            List<AppPort> udpPorts = new List<AppPort>();
            AppPort[] ports = reply.ports;
            foreach (AppPort port in ports)
            {
                if (port.proto == LProto.L_PROTO_UDP)
                {
                    udpPorts.Add(port);
                }
            }
            return udpPorts;
        }

        public List<AppPort> GetHTTPPorts(FindCloudletReply reply)
        {
            List<AppPort> httpPorts = new List<AppPort>();
            AppPort[] ports = reply.ports;
            foreach (AppPort port in ports)
            {
                if (port.proto == LProto.L_PROTO_HTTP)
                {
                    httpPorts.Add(port);
                }
            }
            return httpPorts;
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate,
X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None) return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
      
            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }
    }
}
