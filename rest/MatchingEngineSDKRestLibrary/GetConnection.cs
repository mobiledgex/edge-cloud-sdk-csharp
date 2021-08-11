/**
 * Copyright 2018-2021 MobiledgeX, Inc. All rights and licenses reserved.
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
using System.Net;

namespace DistributedMatchEngine
{
  public partial class MatchingEngine
  {
    /*!
     * Default value of GetConnection timeouts (10000 ms or 10 seconds)
     */
    public const int DEFAULT_GETCONNECTION_TIMEOUT_MS = 10000;

    /*!
     * Get a TCP socket bound to the local cellular interface and connected to the application's backend server.
     * If no exceptions thrown and object is not null, the socket is ready to send application data to backend.
     * \ingroup functions_getconnection
     * \param reply (FindCloudletReply)
     * \param appPort (AppPort)
     * \param desiredPort (int): Optional
     * \param timeout (int): Optional
     * \return Task<Socket>
     * \section gettcpconnexample Example
     * \snippet UnitTest1.cs gettcpconnexample
     */
    public async Task<Socket> GetTCPConnection(FindCloudletReply reply, AppPort appPort, int desiredPort = 0, int timeoutMs = DEFAULT_GETCONNECTION_TIMEOUT_MS, IPEndPoint localEndPoint = null)
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      desiredPort = GetPort(appPort, desiredPort);
      string host = GetHost(reply, appPort);

      IPEndPoint useEndPoint = localEndPoint != null ? localEndPoint : GetIPEndPointByHostName(this.LocalIP);
      Socket s = await GetTCPConnection(host, desiredPort, timeoutMs, useEndPoint).ConfigureAwait(false);
      return s;
    }

    /*!
     * Returns a TCP socket with TLS running over it for secure data communication.
     * Bound to local cellular interface and if no exceptions thrown and object is not null, the socket is ready to send application data to backend
     * \ingroup functions_getconnection
     * \param reply (FindCloudletReply)
     * \param appPort (AppPort)
     * \param desiredPort (int): Optional
     * \param timeout (int): Optional
     * \return Task<SslStream>
     * \section gettcptlsconnexample Example
     * \snippet UnitTest1.cs gettcptlsconnexample
     */
    public async Task<SslStream> GetTCPTLSConnection(FindCloudletReply reply, AppPort appPort, int desiredPort = 0, int timeoutMs = DEFAULT_GETCONNECTION_TIMEOUT_MS, bool allowSelfSignedCerts = false, IPEndPoint localEndPoint = null)
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      desiredPort = GetPort(appPort, desiredPort);
      string host = GetHost(reply, appPort);

      IPEndPoint useEndPoint = localEndPoint != null ? localEndPoint : GetIPEndPointByHostName(this.LocalIP);
      SslStream stream = await GetTCPTLSConnection(host, desiredPort, timeoutMs, allowSelfSignedCerts, useEndPoint).ConfigureAwait(false);
      return stream;
    }

    /*!
     * Get a UDP socket bound to the local cellular interface and connected to the application's backend server.
     * If no exceptions thrown and object is not null, the socket is ready to send application data to backend.
     * \ingroup functions_getconnection
     * \param reply (FindCloudletReply)
     * \param appPort (AppPort)
     * \param desiredPort (int): Optional
     * \param timeout (int): Optional
     * \return Task<Socket>
     */
    public async Task<Socket> GetUDPConnection(FindCloudletReply reply, AppPort appPort, int desiredPort = 0, int timeoutMs = DEFAULT_GETCONNECTION_TIMEOUT_MS, IPEndPoint localEndPoint = null)
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      desiredPort = GetPort(appPort, desiredPort);
      string host = GetHost(reply, appPort);

      IPEndPoint useEndPoint = localEndPoint != null ? localEndPoint : GetIPEndPointByHostName(this.LocalIP);
      Socket s = await GetUDPConnection(host, desiredPort, timeoutMs, useEndPoint).ConfigureAwait(false);
      return s;
    }

    /*!
     * Returns an HTTP Client configured to send requests to application backend.
     * \ingroup functions_getconnection
     * \param reply (FindCloudletReply)
     * \param appPort (AppPort)
     * \param desiredPort (int): Optional
     * \param timeout (int): Optional
     * \return Task<HttpClient>
     * \section gethttpexample Example
     * \snippet UnitTest1.cs gethttpexample
     */
    public async Task<HttpClient> GetHTTPClient(FindCloudletReply reply, AppPort appPort, int desiredPort = 0, int timeoutMs = DEFAULT_GETCONNECTION_TIMEOUT_MS, string path = "")
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      if (appPort.tls)
      {
        throw new GetConnectionException("Specified appPort is tls enabled. Use GetHTTPSClient instead");
      }

      desiredPort = GetPort(appPort, desiredPort);
      string url = CreateUrl(reply, appPort, "http", desiredPort, path);

      UriBuilder uriBuilder = new UriBuilder(url);
      Uri uri = uriBuilder.Uri;
      HttpClient client = await GetHTTPClient(uri);
      return client;
    }

    /*!
     * Returns an HTTPS Client configured to send requests to application backend.
     * \ingroup functions_getconnection
     * \param reply (FindCloudletReply)
     * \param appPort (AppPort)
     * \param desiredPort (int): Optional
     * \param timeout (int): Optional
     * \return Task<HttpClient>
     */
    public async Task<HttpClient> GetHTTPSClient(FindCloudletReply reply, AppPort appPort, int desiredPort = 0, int timeoutMs = DEFAULT_GETCONNECTION_TIMEOUT_MS, string path = "")
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      if (!appPort.tls)
      {
        throw new GetConnectionException("Specified appPort is not tls enabled. Use GetHTTPClient instead");
      }

      desiredPort = GetPort(appPort, desiredPort);
      string url = CreateUrl(reply, appPort, "https", desiredPort, path);

      UriBuilder uriBuilder = new UriBuilder(url);
      Uri uri = uriBuilder.Uri;
      HttpClient client = await GetHTTPClient(uri);
      return client;
    }

    /*!
     * Returns an Websocket Client configured to send requests to application backend.
     * \ingroup functions_getconnection
     * \param reply (FindCloudletReply)
     * \param appPort (AppPort)
     * \param desiredPort (int): Optional
     * \param timeout (int): Optional
     * \return Task<ClientWebSocket>
     * \section getwebsocketexample Example
     * \snippet UnitTest1.cs getwebsocketexample
     */
    public async Task<ClientWebSocket> GetWebsocketConnection(FindCloudletReply reply, AppPort appPort, int desiredPort = 0, int timeoutMs = DEFAULT_GETCONNECTION_TIMEOUT_MS, string path = "")
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      if (appPort.tls)
      {
        throw new GetConnectionException("Specified appPort is tls enabled. Use GetSecureWebsocketConnection instead");
      }

      desiredPort = GetPort(appPort, desiredPort);
      string url = CreateUrl(reply, appPort, "ws", desiredPort, path);

      UriBuilder uriBuilder = new UriBuilder(url);
      Uri uri = uriBuilder.Uri;
      ClientWebSocket s = await GetWebsocketConnection(uri, timeoutMs).ConfigureAwait(false);
      return s;
    }

    /*!
     * Returns a Secure Websocket Client configured to send requests to application backend.
     * \ingroup functions_getconnection
     * \param reply (FindCloudletReply)
     * \param appPort (AppPort)
     * \param desiredPort (int): Optional
     * \param timeout (int): Optional
     * \return Task<ClientWebSocket>
     */
    public async Task<ClientWebSocket> GetSecureWebsocketConnection(FindCloudletReply reply, AppPort appPort, int desiredPort = 0, int timeoutMs = DEFAULT_GETCONNECTION_TIMEOUT_MS, string path = "")
    {
      if (timeoutMs <= 0)
      {
        throw new GetConnectionException(timeoutMs + " is an invalid timeout");
      }

      if (!appPort.tls)
      {
        throw new GetConnectionException("Specified appPort is not tls enabled. Use GetWebsocketConnection instead");
      }

      desiredPort = GetPort(appPort, desiredPort);
      string url = CreateUrl(reply, appPort, "wss", desiredPort, path);

      UriBuilder uriBuilder = new UriBuilder(url);
      Uri uri = uriBuilder.Uri;
      ClientWebSocket s = await GetWebsocketConnection(uri, timeoutMs).ConfigureAwait(false);
      return s;
    }
  }
}
