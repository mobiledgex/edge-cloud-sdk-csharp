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
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Net.WebSockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace DistributedMatchEngine
{

  public partial class MatchingEngine
  {
    // GetTCPConnection helper function
    public async Task<Socket> GetTCPConnection(string host, int port, int timeoutMs)
    {
      ManualResetEvent TimeoutObj = new ManualResetEvent(false);
      Exception handlerException = new Exception();

      // Using integration with IOS or Android sdk, get cellular interface
      IPEndPoint localEndPoint = GetLocalIP();
      Console.WriteLine("got local endpoint: " + localEndPoint);

      // Get remote ip of the provided host
      IPAddress remoteIP = Dns.GetHostAddresses(host)[0];
      IPEndPoint remoteEndPoint = new IPEndPoint(remoteIP, port);
      Console.WriteLine("got remote endpoint: " + remoteEndPoint);

      // Create Socket and bind to local ip and connect to remote endpoint
      Socket s = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      s.Bind(localEndPoint);
      Console.WriteLine("bound local endpoint: " + localEndPoint);

      // Reset static variables that handler uses
      TimeoutObj.Reset();
      handlerException = null;
      s.BeginConnect(remoteEndPoint,
        new AsyncCallback( // Closure to retrieve exceptions:
          ar =>
          {
            try
            {
              Console.WriteLine("Connect try: " + remoteEndPoint);
                    // Retrieve the socket from the state object.
                    Socket client = (Socket)ar.AsyncState;
                    // Complete the connection.
                    client.EndConnect(ar);
              TimeoutObj.Set();
            }
            catch (Exception e)
            {
              Console.WriteLine("Connect exception: " + e);
              handlerException = e;
              TimeoutObj.Set();
            }
          }
        ),
        s);

      // WaitOne returns true if TimeoutObj receives a signal (ie. when .Set() is called in the connect callback)
      if (TimeoutObj.WaitOne(timeoutMs, false)) // WaitOne timeout is in milliseconds
      {
        if (handlerException != null)
        {
          Console.WriteLine("Connect found exception: " + handlerException);
          throw handlerException;
        }
        if (!s.IsBound && !s.Connected)
        {
          Console.WriteLine("Could not bind to interface or connect to server");
          throw new GetConnectionException("Could not bind to interface or connect to server");
        }
        else if (!s.IsBound)
        {
          Console.WriteLine("Could not bind to interface");
          throw new GetConnectionException("Could not bind to interface");
        }
        else if (!s.Connected)
        {
          Console.WriteLine("Connect Success: " + remoteEndPoint);
          throw new GetConnectionException("Could not connect to server");
        }
        Console.WriteLine("Connect Success: " + remoteEndPoint);
        await Task.Delay(0); // For Unity.
        return s;
      }
      else
      {
        Console.WriteLine("Connect timeout: " + remoteEndPoint);
        throw new GetConnectionException("Timeout");
      }
    }

    // GetTCPTLSConnection helper function
    public async Task<SslStream> GetTCPTLSConnection(string host, int port, int timeoutMs, bool allowSelfSignedCerts = false)
    {
      CancellationTokenSource source = new CancellationTokenSource();
      CancellationToken token = source.Token;

      // Using integration with IOS or Android sdk, get cellular interface
      IPEndPoint localEndPoint = GetLocalIP();

      // Create tcp client
      TcpClient tcpClient = new TcpClient(localEndPoint);
      var task = tcpClient.ConnectAsync(host, port);

      try
      {
        // Wait returns true if Task completes execution before timeout, false otherwise
        if (await Task.WhenAny(task, Task.Delay(timeoutMs, token)).ConfigureAwait(false) == task)
        {
          // Create ssl stream on top of tcp client and validate server cert
          SslStream sslStream = new SslStream(tcpClient.GetStream(), false,
            new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
          // Map this sslStream to the allowSelfSignedCerts value set by the developer
          allowSelfSignedServerCertsDict[sslStream.GetHashCode()] = allowSelfSignedCerts;
          // Grab client certificates if user configures server to require client certs
          X509CertificateCollection clientCerts = null;
          if (serverRequiresClientCertAuth)
          {
            clientCerts = clientCertCollection;
            // Print out info about Client Certificates
            if (clientCerts != null)
            {
              foreach (X509Certificate2 cert in clientCerts)
              {
                Console.WriteLine("Client certificate subject: {0}, Effective date: {1}, Expiration date: {2}", cert.Subject, cert.GetEffectiveDateString(), cert.GetExpirationDateString());
              }
            }
          }
          // This function allows for two-way TLS/SSL handshake. If no certs provided, falls back to one-way handshake
          sslStream.AuthenticateAsClient(host, clientCerts, enabledProtocols, true);
          return sslStream;
        }
        source.Cancel();
      }
      catch (TaskCanceledException tce)
      {
        throw new GetConnectionException("Task cancelled: ", tce);
      }
      finally
      {
        source.Dispose();
      }
      throw new GetConnectionException("Timeout");
    }

    // GetUDPConnection helper function
    public async Task<Socket> GetUDPConnection(string host, int port, int timeoutMs)
    {
      // For retrieving exceptions:
      ManualResetEvent TimeoutObj = new ManualResetEvent(false);
      Exception handlerException = new Exception();

      // Using integration with IOS or Android sdk, get cellular interface
      IPEndPoint localEndPoint = GetLocalIP();

      // Get remote ip of the provided host
      IPAddress remoteIP = Dns.GetHostAddresses(host)[0];
      IPEndPoint remoteEndPoint = new IPEndPoint(remoteIP, port);

      // Create Socket and bind to local ip and connect to remote endpoint
      Socket s = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
      s.Bind(localEndPoint);

      // Reset static variables that handler uses
      TimeoutObj.Reset();
      handlerException = null;
      s.BeginConnect(remoteEndPoint,
        new AsyncCallback( // Closure to retrieve exceptions:
          ar =>
          {
            try
            {
              // Retrieve the socket from the state object.  
              Socket client = (Socket)ar.AsyncState;
              // Complete the connection.  
              client.EndConnect(ar);
              TimeoutObj.Set();
            }
            catch (Exception e)
            {
              handlerException = e;
              TimeoutObj.Set();
            }
          }
        ),
        s);

      // WaitOne returns true if TimeoutObj receives a signal (ie. when .Set() is called in the connect callback)
      if (TimeoutObj.WaitOne(timeoutMs, false))
      {
        if (handlerException != null)
        {
          throw handlerException;
        }
        if (!s.IsBound && !s.Connected)
        {
          throw new GetConnectionException("Could not bind to interface or connect to server");
        }

        if (!s.IsBound)
        {
          throw new GetConnectionException("Could not bind to interface");
        }

        if (!s.Connected)
        {
          throw new GetConnectionException("Could not connect to server");
        }
        await Task.Delay(0); // Unity doesn't like Task.Run();
        return s;
      }
      throw new GetConnectionException("Timeout");

    }

    // GetHTTPClient and GetHTTPSClient helper function.
    // TODO: .Net Core 2.1: The httpclient socket handler needs to be set to use a cellular socket.
    public async Task<HttpClient> GetHTTPClient(Uri uri)
    {
      HttpClient appHttpClient = new HttpClient();
      appHttpClient.BaseAddress = uri;
      await Task.Delay(0); // For Unity.
      return appHttpClient;
    }


    // GetWebsocketConnection helper function, if interface available.
    // TODO: This requires a socket handler to set network interfaces.
    public async Task<ClientWebSocket> GetWebsocketConnection(Uri uri, int timeoutMs, bool waitForOpen = true)
    {
      // Initialize websocket client
      ClientWebSocket webSocket = new ClientWebSocket();

      // Token is used to notify listeners/ delegates of task state
      CancellationTokenSource source = new CancellationTokenSource();
      CancellationToken token = source.Token;

      Stopwatch stopWatch = new Stopwatch();
      TimeSpan tstimeout = new TimeSpan(0, 0, 0, 0, timeoutMs);
      stopWatch.Reset();
      // initiate websocket handshake with server
      var task = webSocket.ConnectAsync(uri, token);
      try
      {
        if (await Task.WhenAny(task, Task.Delay(timeoutMs, token)).ConfigureAwait(false) == task)
        {
          // Check for connecting timeout until connected (loop observeable):
          do
          {
            // Return if open:
            if (webSocket.State == WebSocketState.Open)
            {
              return webSocket;
            }

            // Timeout...
            if (stopWatch.Elapsed <= tstimeout && webSocket.State == WebSocketState.Connecting)
            {
              Log.D("Waiting to connect...");
              await Task.Delay(50).ConfigureAwait(false);
            }
            else
            {
              Log.D("Cancelling");
              source.Cancel();
            }
          }
          while (webSocket.State == WebSocketState.Connecting && waitForOpen);
        }
      }
      catch (TaskCanceledException tce)
      {
        throw new GetConnectionException("Timeout getting websocket connection.", tce);
      }
      finally
      {
        source.Dispose();
      }
      throw new GetConnectionException("Cannot get websocket connection");
    }

    // GetSecureWebsocketConnection helper function
    // TODO: This requires a socket handler to set network interfaces.
    public async Task<ClientWebSocket> GetSecureWebsocketConnection(Uri uri, int timeoutMs, bool waitForOpen = true)
    {
      // Initialize websocket class
      ClientWebSocket webSocket = new ClientWebSocket();

      // Token is used to notify listeners/ delegates of task state
      CancellationTokenSource source = new CancellationTokenSource();
      CancellationToken token = source.Token;

      Stopwatch stopWatch = new Stopwatch();
      TimeSpan tstimeout = new TimeSpan(0, 0, 0, 0, timeoutMs);
      stopWatch.Reset();
      // initiate websocket handshake  with server
      var task = webSocket.ConnectAsync(uri, token);
      try
      {
        if (await Task.WhenAny(task, Task.Delay(timeoutMs, token)).ConfigureAwait(false) == task)
        {
          // Check for connecting timeout until connected (loop observeable):
          do
          {
            // Return if open:
            if (webSocket.State == WebSocketState.Open)
            {
              return webSocket;
            }

            // Timeout...
            if (stopWatch.Elapsed <= tstimeout && webSocket.State == WebSocketState.Connecting)
            {
              Log.D("Waiting to connect...");
              await Task.Delay(50).ConfigureAwait(false);
            }
            else
            {
              Log.D("Cancelling");
              source.Cancel();
            }
          }
          while (webSocket.State == WebSocketState.Connecting && waitForOpen);
        }
      }
      catch (TaskCanceledException tce)
      {
        throw new GetConnectionException("Cannot get websocket connection", tce);
      }
      finally
      {
        source.Dispose();
      }
      throw new GetConnectionException("Cannot get websocket connection");
    }
  }
}
