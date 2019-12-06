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
using System.Net.Sockets;
using System.Net.Http;
using System.Net.WebSockets;
using System.Net.Security;

using System.Threading.Tasks;

namespace DistributedMatchEngine
{
    public partial class MatchingEngine
    {

        public async Task<Socket> GetTCPConnection(FindCloudletReply reply, int containerPort, int desiredPort, int timeout)
        {
            // Gets the AppPort object whose internal port is equal to the specified containerPort
            AppPort appPort = GetMatchingInternalAppPort(reply, containerPort, LProto.L_PROTO_TCP);

            if (appPort == null)
            {
                throw new GetConnectionException("Unable to find AppPort with internal port: " + containerPort);
            }

            // If desiredPort is not specified, then default to public_port
            if (desiredPort == 0)
            {
                desiredPort = appPort.public_port;
            }

            if (!IsInPortRange(appPort, desiredPort))
            {
                throw new GetConnectionException("Desired port: " + desiredPort +  " is not in AppPort range");
            }

            string host = appPort.fqdn_prefix + reply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
            Socket s = await GetTCPConnection(host, desiredPort, timeout);
            return s;
        }

        public async Task<SslStream> GetTCPTLSConnection(FindCloudletReply reply, int containerPort, int desiredPort, int timeout)
        {
            // Gets the AppPort object whose internal port is equal to the specified containerPort
            AppPort appPort = GetMatchingInternalAppPort(reply, containerPort, LProto.L_PROTO_TCP);

            if (appPort == null)
            {
                throw new GetConnectionException("Unable to find AppPort with internal port: " + containerPort);
            }

            // If desiredPort is not specified, then default to public_port
            if (desiredPort == 0)
            {
                desiredPort = appPort.public_port;
            }

            if (!IsInPortRange(appPort, desiredPort))
            {
                throw new GetConnectionException("Desired port: " + desiredPort +  " is not in AppPort range");
            }

            string host = appPort.fqdn_prefix + reply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
            SslStream stream = await GetTCPTLSConnection(host, desiredPort, timeout);
            return stream;
        }

        public async Task<Socket> GetUDPConnection(FindCloudletReply reply, int containerPort, int desiredPort, int timeout)
        {
            // Gets the AppPort object whose internal port is equal to the specified containerPort
            AppPort appPort = GetMatchingInternalAppPort(reply, containerPort, LProto.L_PROTO_UDP);

            if (appPort == null)
            {
                throw new GetConnectionException("Unable to find AppPort with internal port: " + containerPort);
            }

            // If desiredPort is not specified, then default to public_port
            if (desiredPort == 0)
            {
                desiredPort = appPort.public_port;
            }

            if (!IsInPortRange(appPort, desiredPort))
            {
                throw new GetConnectionException("Desired port: " + desiredPort +  " is not in AppPort range");
            }

            string host = appPort.fqdn_prefix + reply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
            Socket s = await GetUDPConnection(host, desiredPort, timeout);
            return s;
        }
        
        public async Task<HttpClient> GetHTTPClient(FindCloudletReply reply, int containerPort, int desiredPort, int timeout)
        {
            // Gets the AppPort object whose internal port is equal to the specified containerPort
            AppPort appPort = GetMatchingInternalAppPort(reply, containerPort, LProto.L_PROTO_HTTP);

            if (appPort == null)
            {
                throw new GetConnectionException("Unable to find AppPort with internal port: " + containerPort);
            }

            // If desiredPort is not specified, then default to public_port
            if (desiredPort == 0)
            {
                desiredPort = appPort.public_port;
            }

            if (!IsInPortRange(appPort, desiredPort))
            {
                throw new GetConnectionException("Desired port: " + desiredPort +  " is not in AppPort range");
            }

            // prepend fqdn prefix given in AppPort to fqdn and append path_prefix to uri
            string uriString = appPort.fqdn_prefix + reply.fqdn + ":" + desiredPort + appPort.path_prefix;
            UriBuilder uriBuilder = new UriBuilder("http", uriString);
            Uri uri = uriBuilder.Uri;
            HttpClient client = await GetHTTPClient(uri);
            return client;
        }

        public async Task<HttpClient> GetHTTPSClient(FindCloudletReply reply, int containerPort, int desiredPort, int timeout)
        {
            // Gets the AppPort object whose internal port is equal to the specified containerPort
            AppPort appPort = GetMatchingInternalAppPort(reply, containerPort, LProto.L_PROTO_HTTP);

            if (appPort == null)
            {
                throw new GetConnectionException("Unable to find AppPort with internal port: " + containerPort);
            }

            // If desiredPort is not specified, then default to public_port
            if (desiredPort == 0)
            {
                desiredPort = appPort.public_port;
            }

            if (!IsInPortRange(appPort, desiredPort))
            {
                throw new GetConnectionException("Desired port: " + desiredPort +  " is not in AppPort range");
            }

            // prepend fqdn prefix given in AppPort to fqdn and append path_prefix to uri
            string uriString = appPort.fqdn_prefix + reply.fqdn + ":" + desiredPort + appPort.path_prefix;
            UriBuilder uriBuilder = new UriBuilder("https", uriString);
            Uri uri = uriBuilder.Uri;
            HttpClient client = await GetHTTPClient(uri);
            return client;
        }

        public async Task<ClientWebSocket> GetWebsocketConnection(FindCloudletReply reply, int containerPort, int desiredPort, int timeout)
        {
            // Gets the AppPort object whose internal port is equal to the specified containerPort
            AppPort appPort = GetMatchingInternalAppPort(reply, containerPort, LProto.L_PROTO_TCP);

            if (appPort == null)
            {
                throw new GetConnectionException("Unable to find AppPort with internal port: " + containerPort);
            }

            // If desiredPort is not specified, then default to public_port
            if (desiredPort == 0)
            {
                desiredPort = appPort.public_port;
            }

            if (!IsInPortRange(appPort, desiredPort))
            {
                throw new GetConnectionException("Desired port: " + desiredPort +  " is not in AppPort range");
            }

            string host = appPort.fqdn_prefix + reply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
            ClientWebSocket s = await GetWebsocketConnection(host, desiredPort, timeout);
            return s;
        }

        public async Task<ClientWebSocket> GetSecureWebsocketConnection(FindCloudletReply reply, int containerPort, int desiredPort, int timeout)
        {
            // Gets the AppPort object whose internal port is equal to the specified containerPort
            AppPort appPort = GetMatchingInternalAppPort(reply, containerPort, LProto.L_PROTO_TCP);

            if (appPort == null)
            {
                throw new GetConnectionException("Unable to find AppPort with internal port: " + containerPort);
            }

            // If desiredPort is not specified, then default to public_port
            if (desiredPort == 0)
            {
                desiredPort = appPort.public_port;
            }

            if (!IsInPortRange(appPort, desiredPort))
            {
                throw new GetConnectionException("Desired port: " + desiredPort +  " is not in AppPort range");
            }

            string host = appPort.fqdn_prefix + reply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
            ClientWebSocket s = await GetSecureWebsocketConnection(host, desiredPort, timeout);
            return s;
        }
    }
}
