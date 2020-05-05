/**
 * Copyright 2018-2020 MobiledgeX, Inc. All rights and licenses reserved.
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
    /// <summary>
    /// Gets TCP Connection
    /// </summary>
    /// <param name="reply"> FindCloudlet Reply Object </param>
    /// <param name="appPort">AppPort Object </param>
    /// <param name="desiredPort"> Desired port number for TCP connection</param>
    /// <param name="timeoutMs"> (integer value)time in milliseconds to enforce end of connection trail(if response took longer than expected)</param>
    /// <returns>Socket Object (Socket class is part of System.Net.Sockets)</returns>
    public async Task<Socket> GetTCPConnection(FindCloudletReply reply, AppPort appPort, int desiredPort, int timeoutMs)
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      // If desiredPort is -1, then default to public_port
      if (desiredPort == -1)
      {
        desiredPort = appPort.public_port;
      }

      if (!IsInPortRange(appPort, desiredPort))
      {
        throw new GetConnectionException("Desired port: " + desiredPort + " is not in AppPort range");
      }

      string host = appPort.fqdn_prefix + reply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
      Socket s = await GetTCPConnection(host, desiredPort, timeoutMs).ConfigureAwait(false);
      return s;
    }
    
    /// <summary>
    /// Gets TCP TLS Connection
    /// </summary>
    /// <param name="reply"> FindCloudlet Reply Object </param>
    /// <param name="appPort">AppPort Object </param>
    /// <param name="desiredPort"> Desired port number for TCP connection</param>
    /// <param name="timeoutMs"> (integer value)time in milliseconds to enforce end of connection trail(if response took longer than expected)</param>
    /// <returns>SslStream Object (SslStream class is part of System.Net.Security)</returns>
    public async Task<SslStream> GetTCPTLSConnection(FindCloudletReply reply, AppPort appPort, int desiredPort, int timeoutMs)
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      // If desiredPort is not specified, then default to public_port
      if (desiredPort == -1)
      {
        desiredPort = appPort.public_port;
      }

      if (!IsInPortRange(appPort, desiredPort))
      {
        throw new GetConnectionException("Desired port: " + desiredPort + " is not in AppPort range");
      }

      string host = appPort.fqdn_prefix + reply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
      SslStream stream = await GetTCPTLSConnection(host, desiredPort, timeoutMs).ConfigureAwait(false);
      return stream;
    }

    /// <summary>
    /// Gets UDP Connection
    /// </summary>
    /// <param name="reply"> FindCloudlet Reply Object </param>
    /// <param name="appPort">AppPort Object </param>
    /// <param name="desiredPort"> Desired port number for udp connection</param>
    /// <param name="timeoutMs"> (integer value)time in milliseconds to enforce end of connection trail(if response took longer than expected)</param>
    /// <returns>Socket Object (Socket class is part of System.Net.Sockets)</returns>
    public async Task<Socket> GetUDPConnection(FindCloudletReply reply, AppPort appPort, int desiredPort, int timeoutMs)
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      // If desiredPort is not specified, then default to public_port
      if (desiredPort == -1)
      {
        desiredPort = appPort.public_port;
      }

      if (!IsInPortRange(appPort, desiredPort))
      {
        throw new GetConnectionException("Desired port: " + desiredPort + " is not in AppPort range");
      }

      string host = appPort.fqdn_prefix + reply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
      Socket s = await GetUDPConnection(host, desiredPort, timeoutMs).ConfigureAwait(false);
      return s;
    }
    
    /// <summary>
    /// Gets HTTP Connection
    /// </summary>
    /// <param name="reply"> FindCloudlet Reply Object </param>
    /// <param name="appPort">AppPort Object </param>
    /// <param name="desiredPort"> Desired port number for http connection</param>
    /// <param name="timeoutMs"> (integer value)time in milliseconds to enforce end of connection trail(if response took longer than expected)</param>
    /// <param name="path">Optional query parameters for ex. roomid=room1 </param>
    /// <returns>HttpClient Object (HttpClient class is part of System.Net.Http)</returns>
    public async Task<HttpClient> GetHTTPClient(FindCloudletReply reply, AppPort appPort, int desiredPort, int timeoutMs, string path = "")
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      // If desiredPort is not specified, then default to public_port
      if (desiredPort == -1)
      {
        desiredPort = appPort.public_port;
      }

      if (!IsInPortRange(appPort, desiredPort))
      {
        throw new GetConnectionException("Desired port: " + desiredPort + " is not in AppPort range");
      }

      // prepend fqdn prefix given in AppPort to fqdn and append path_prefix to uri
      string uriString = appPort.fqdn_prefix + reply.fqdn + ":" + desiredPort + appPort.path_prefix + path;
      UriBuilder uriBuilder;
      if (appPort.tls)
      {
        uriBuilder = new UriBuilder("https", uriString);
      }
      else
      {
        uriBuilder = new UriBuilder("http", uriString);
      }
      Uri uri = uriBuilder.Uri;
      HttpClient client = await GetHTTPClient(uri);
      return client;
    }

    // FIXME: This API seems redundant outside testing purposes.
    public async Task<HttpClient> GetHTTPSClient(FindCloudletReply reply, AppPort appPort, int desiredPort, int timeoutMs, string path = "")
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }
      
      // If desiredPort is not specified, then default to public_port
      if (desiredPort == -1)
      {
        desiredPort = appPort.public_port;
      }

      if (!IsInPortRange(appPort, desiredPort))
      {
        throw new GetConnectionException("Desired port: " + desiredPort + " is not in AppPort range");
      }

      // prepend fqdn prefix given in AppPort to fqdn and append path_prefix to uri
      string uriString = appPort.fqdn_prefix + reply.fqdn + ":" + desiredPort + appPort.path_prefix + path;
      UriBuilder uriBuilder = new UriBuilder("https", uriString);
      Uri uri = uriBuilder.Uri;
      HttpClient client = await GetHTTPClient(uri);
      return client;
    }

    /// <summary>
    /// Gets Websocket Connection
    /// </summary>
    /// <param name="reply"> FindCloudlet Reply Object </param>
    /// <param name="appPort">AppPort Object </param>
    /// <param name="desiredPort"> Desired port number for websocket connection</param>
    /// <param name="timeoutMs"> (integer value)time in milliseconds to enforce end of connection trail(if response took longer than expected)</param>
    /// <param name="path">Optional query parameters for ex. roomid=room1 </param>
    /// <returns>ClientWebSocket Object (ClientWebSocket class is part of System.Net.WebSockets)</returns>
    public async Task<ClientWebSocket> GetWebsocketConnection(FindCloudletReply reply, AppPort appPort, int desiredPort, int timeoutMs, string path = "")
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      // If desiredPort is not specified, then default to public_port
      if (desiredPort == -1)
      {
        desiredPort = appPort.public_port;
      }

      if (!IsInPortRange(appPort, desiredPort))
      {
        throw new GetConnectionException("Desired port: " + desiredPort + " is not in AppPort range");
      }

      string host = appPort.fqdn_prefix + reply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
      ClientWebSocket s = await GetWebsocketConnection(host, desiredPort, timeoutMs, path).ConfigureAwait(false);
      return s;
    }
    
    /// <summary>
    /// Gets Secure Websocket Connection
    /// </summary>
    /// <param name="reply"> FindCloudlet Reply Object </param>
    /// <param name="appPort">AppPort Object </param>
    /// <param name="desiredPort"> Desired port number for websocket connection</param>
    /// <param name="timeoutMs"> (integer value)time in milliseconds to enforce end of connection trail(if response took longer than expected)</param>
    /// <param name="path">Optional query parameters for ex. roomid=room1 </param>
    /// <returns>ClientWebSocket Object (ClientWebSocket class is part of System.Net.WebSockets)</returns>
    public async Task<ClientWebSocket> GetSecureWebsocketConnection(FindCloudletReply reply, AppPort appPort, int desiredPort, int timeoutMs, string path = "")
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      // If desiredPort is not specified, then default to public_port
      if (desiredPort == -1)
      {
        desiredPort = appPort.public_port;
      }

      if (!IsInPortRange(appPort, desiredPort))
      {
        throw new GetConnectionException("Desired port: " + desiredPort + " is not in AppPort range");
      }

      string host = appPort.fqdn_prefix + reply.fqdn; // prepend fqdn prefix given in AppPort to fqdn
      ClientWebSocket s = await GetSecureWebsocketConnection(host, desiredPort, timeoutMs, path).ConfigureAwait(false);
      return s;
    }
  }
}
