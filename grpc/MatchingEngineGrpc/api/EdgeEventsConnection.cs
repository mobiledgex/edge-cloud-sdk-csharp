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
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using DistributedMatchEngine.PerformanceMetrics;
using Grpc.Core;
using static DistributedMatchEngine.ClientEdgeEvent.Types;
using static DistributedMatchEngine.PerformanceMetrics.NetTest;

namespace DistributedMatchEngine
{
  public class EdgeEventsConnection : IDisposable
  {
    // DME region GRPC Streaming Client.
    MatchEngineApi.MatchEngineApiClient streamClient;
    private AsyncDuplexStreamingCall<ClientEdgeEvent, ServerEdgeEvent> DuplexEventStream;
    CancellationTokenSource ConnectionCancelTokenSource;

    private string HostOverride;
    private uint PortOverride;

    MatchingEngine me;
    private string edgeEventsCoookie { get; set; }

    Task ReadStreamTask;
    internal bool DoReconnect = true;

    internal EdgeEventsConnection(MatchingEngine matchingEngine, string host = null, uint port = 0)
    {
      this.me = matchingEngine;

      if (host != null && host.Trim().Length != 0)
      {
        HostOverride = host;
        PortOverride = port == 0 ? me.dmePort : port;
      }
    }

    // TODO: Throw and print some useful informative Exceptions.
    [MethodImpl(MethodImplOptions.Synchronized)]
    internal bool Open(string openEdgeEventsCookie)
    {
      if (!me.EnableEdgeEvents)
      {
        return false;
      }

      if (me.sessionCookie == null || me.sessionCookie.Equals(""))
      {
        Log.E("SessionCookie not present!");
        return false;
      }
      if (openEdgeEventsCookie == null || openEdgeEventsCookie.Equals(""))
      {
        Log.E("EdgeEventsCookie not present!");
        return false;
      }

      if (!IsShutdown())
      {
        return true;
      }

      edgeEventsCoookie = openEdgeEventsCookie;
      var uri = "";
      Channel channel;
      if (HostOverride == null || HostOverride.Trim().Length == 0 || PortOverride == 0)
      {
        channel = me.ChannelPicker(me.GenerateDmeHostAddress(), me.dmePort);
      }
      else
      {
        channel = me.ChannelPicker(HostOverride, PortOverride);
      }

      streamClient = new MatchEngineApi.MatchEngineApiClient(channel);

      // Open a connection:

      ClientEdgeEvent clientEdgeEvent = new ClientEdgeEvent
      {
        EventType = ClientEventType.EventInitConnection,
        SessionCookie = me.sessionCookie,
        EdgeEventsCookie = openEdgeEventsCookie
      };

      // Attach a reader and loop until gone:
      ReadStreamTask = Task.Run(async () =>
      {
        try
        {
          ConnectionCancelTokenSource = new CancellationTokenSource();

          Log.S("Write init message to server, with cancelToken: ");
          DuplexEventStream = streamClient.StreamEdgeEvent(
            cancellationToken: ConnectionCancelTokenSource.Token
          );
          await DuplexEventStream.RequestStream.WriteAsync(clientEdgeEvent);
          Log.D("Now Listening to Events...");

          while (await DuplexEventStream.ResponseStream.MoveNext())
          {
            bool invoked = me.InvokeEdgeEventsReciever(DuplexEventStream.ResponseStream.Current);

            if (DuplexEventStream.ResponseStream.Current.EventType == ServerEdgeEvent.Types.ServerEventType.EventCloudletUpdate &&
                me.AutoMigrateEdgeEventsConnection)
            {
              Close();
            }
          }

          Log.D("DMEConnection loop has exited.");
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ConnectionCancelTokenSource.Token)
        {
          DuplexEventStream = null;
          ReadStreamTask = null;
        }
        finally
        {
          ConnectionCancelTokenSource = null;
          if (DoReconnect)
          {
            Log.D("Reconnecting EdgeEventsConnection!");
            if (!Reconnect())
            {
              Log.E("Failed to restart EdgeEvents Loop in EdgeEventsConnection!");
            }
          }
        }
      });

      return true;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    internal void Close()
    {
      SendTerminate().ConfigureAwait(false);
      HostOverride = null; // Will use new DME on next connect.
      PortOverride = 0;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    bool Reconnect()
    {
      if (!IsShutdown())
      {
        return false;
      }

      return Open(edgeEventsCoookie);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool IsShutdown()
    {
      if (ConnectionCancelTokenSource == null)
      {
        return true;
      }
      if (ConnectionCancelTokenSource.IsCancellationRequested)
      {
        return true;
      }
      return false;
    }

    internal async Task<Boolean> Send(ClientEdgeEvent clientEdgeEvent)
    {
      try
      {
        if (IsShutdown())
        {
          Log.E("Cannot send message over channel that is shutdown!");
          return false;
        }

        // The connection might still be connecting.
        Log.D("Posting this message: " + clientEdgeEvent);
        await DuplexEventStream.RequestStream.WriteAsync(clientEdgeEvent).ConfigureAwait(false);
        Log.D("Posted this message: " + clientEdgeEvent);
        return true;
      }
      catch (IOException ioe)
      {
        Log.E("Error writing to stream. Reason: " + ioe.Message);
      }
      return false;
    }

    public async Task<Boolean> SendTerminate()
    {

      ClientEdgeEvent terminateEvent = new ClientEdgeEvent
      {
        EventType = ClientEventType.EventTerminateConnection
      };
      return await Send(terminateEvent);
    }

    /*!
     * Post GPS locations to EdgeEventsConnection. If there is a closer AppInst
     * for the App, DME will send a new FindCloudletReply to use when the app
     * is ready.
     *
     * \param location DistriutedMatchEngine.Loc
     */
    public async Task<bool> PostLocationUpdate(Loc location)
    {
      Log.D("PostLocationUpdate()");
      if (location == null)
      {
        Log.E("Cannot post null location!");
        return false;
      }

      ClientEdgeEvent locationUpdate = new ClientEdgeEvent
      {
        EventType = ClientEventType.EventLocationUpdate,
        GpsLocation = location
      };

      return await Send(locationUpdate).ConfigureAwait(false);
    }

    /*!
     * Post a PerformanceMetrics Site object to the edge events server with App
     * driven performance test results.
     *
     * \param site Contains stats the app retrieved to send to server.
     * \param location DistriutedMatchEngine.Loc
     */
    public async Task<bool> PostLatencyUpdate(Site site, Loc location)
    {
      Log.D("PostLatencyResult()");
      if (location == null)
      {
        return false;
      }

      if (site == null || (site.samples == null || site.samples.Length == 0))
      {
        // No results to post.
        return false;
      }

      ClientEdgeEvent latencySamplesEvent = new ClientEdgeEvent
      {
        EventType = ClientEventType.EventLatencySamples,
        GpsLocation = location,
      };
      foreach (var entry in site.samples)
      {
        if (entry != null)
        {
          latencySamplesEvent.Samples.Add(entry);
        }
      }

      if (latencySamplesEvent.Samples.Count == 0)
      {
        return false;
      }
      return await Send(latencySamplesEvent).ConfigureAwait(false);
    }

    /*!
     * Post a PerformanceMetrics Ping stats to the EdgeEvents server connection.
     * This call will gather and post the results.
     *
     * \param host
     * \param location DistriutedMatchEngine.Loc
     * \param numSamples (default 5 samples)
     */
    public async Task<bool> TestPingAndPostLatencyUpdate(string host, Loc location,
                                                        int numSamples = 5)
    {
      Log.D("TestPingAndPostLatencyResult()");
      if (location == null)
      {
        return false;
      }

      Site site = new Site(TestType.PING, numSamples: numSamples);
      site.host = host;

      NetTest netTest = new NetTest(me);
      netTest.sites.Enqueue(site);
      await netTest.RunNetTest(numSamples);

      ClientEdgeEvent latencySamplesEvent = new ClientEdgeEvent
      {
        EventType = ClientEventType.EventLatencySamples,
        GpsLocation = location,
      };
      foreach (var entry in site.samples)
      {
        if (entry != null)
        {
          latencySamplesEvent.Samples.Add(entry);
        }
      }

      if (latencySamplesEvent.Samples.Count == 0)
      {
        return false;
      }
      return await Send(latencySamplesEvent).ConfigureAwait(false);
    }

    /*!
     * Post a PerformanceMetrics TCP connect stats to the EdgeEvents server
     * connection. This call will gather and post the results.
     *
     * \param host
     * \param port
     * \param location DistriutedMatchEngine.Loc
     * \param numSamples (default 5 samples)
     */
    public async Task<bool> TestConnectAndPostLatencyUpdate(string host, uint port, Loc location,
                                                        int numSamples = 5)
    {
      Log.D("TestConnectAndPostLatencyResult()");
      if (location == null)
      {
        return false;
      }

      Site site = new Site(TestType.CONNECT, numSamples: numSamples);
      site.host = host;
      site.port = (int)port;

      NetTest netTest = new NetTest(me);
      netTest.sites.Enqueue(site);
      await netTest.RunNetTest(numSamples);

      ClientEdgeEvent latencySamplesEvent = new ClientEdgeEvent
      {
        EventType = ClientEventType.EventLatencySamples,
        GpsLocation = location,
      };
      foreach (var entry in site.samples)
      {
        if (entry != null)
        {
          latencySamplesEvent.Samples.Add(entry);
        }
      }

      if (latencySamplesEvent.Samples.Count == 0)
      {
        return false;
      }

      return await Send(latencySamplesEvent).ConfigureAwait(false);
    }

    public void Dispose()
    {
      DoReconnect = false;
      // Attempt to cancel.
      Close();
      streamClient = null;
      me = null;
    }

  }
}
