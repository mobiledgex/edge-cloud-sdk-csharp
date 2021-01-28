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
using Grpc.Core;
using static DistributedMatchEngine.ClientEdgeEvent.Types;
using static DistributedMatchEngine.PerformanceMetrics.NetTest;

namespace DistributedMatchEngine
{
  public class DMEConnection : IDisposable
  {
    // DME region GRPC Streaming Client.
    MatchEngineApi.MatchEngineApiClient streamClient;
    private AsyncDuplexStreamingCall<ClientEdgeEvent, ServerEdgeEvent> DuplexEventStream;
    CancellationTokenSource ConnectionCancelTokenSource;

    private int IntIsOpen = 0;
    private bool IsOpen
    {
      get
      {
        return IntIsOpen > 0 ? true : false;
      }
      set
      {
        Interlocked.Exchange(ref IntIsOpen, value ? 1 : 0);
      }
    }
    private int IntReOpenDmeConnection = 0;
    private bool ReOpenDmeConnection
    {
      get
      {
        return IntReOpenDmeConnection > 0 ? true : false;
      }
      set
      {
        Interlocked.Exchange(ref IntReOpenDmeConnection, value ? 1 : 0);
      }
    }

    private String HostOverride;
    private uint PortOverride;

    MatchingEngine me;
    private string edgeEventsCoookie { get; set; }

    Task ReadStreamTask;

    internal DMEConnection(MatchingEngine matchingEngine, string host = null, uint port = 0)
    {
      this.me = matchingEngine;
      ConnectionCancelTokenSource = new CancellationTokenSource();

      if (host != null && host.Trim().Length != 0)
      {
        HostOverride = host;
        PortOverride = port == 0 ? me.dmePort : port;
      }
    }

    // Need format for the http delimited message, which is JSON format.
    // Chunched encoding is <size>\r\n, followed by the <data>+\r\n, then 0\r\n to end. Read until message end that way, send (buffer of chuncked sizes) to Deserializer.
    // Same for other direction.

    // HTTP Post would be better than raw writing here...
    string GetString(ClientEdgeEvent clientEdgeEvent)
    {
      string jsonStr;
      MemoryStream ms = new MemoryStream();
      try
      {
        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ClientEdgeEvent), me.serializerSettings);
        ms = new MemoryStream();
        serializer.WriteObject(ms, clientEdgeEvent);
        jsonStr = Util.StreamToString(ms);
      }
      catch (Exception e)
      {
        Log.E("Exception Message: " + e.Message);
        Log.E("Exception Stack: " + e.StackTrace);
        throw e;
      }
      return jsonStr;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    void Close()
    {
      try
      {
        ConnectionCancelTokenSource.Cancel();
      } catch (Exception e)
      {
        Log.E("Exception Closing DMEConnection. Message: " + e.Message);
        Log.E("StackTrace: " + e.StackTrace);
      }
      IsOpen = false;
    }

    // TODO: Throw and print some useful informative Exceptions.
    [MethodImpl(MethodImplOptions.Synchronized)]
    internal bool Open(string openEdgeEventsCookie)
    {
      if (me.sessionCookie == null || openEdgeEventsCookie == null)
      {
        Log.D("Missing session or edge events cookie!");
        return false;
      }
      Log.D("IsOpen: " + IsOpen);
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
      if (me.sessionCookie == null || me.sessionCookie.Equals(""))
      {
        Console.Error.WriteLine("SessionCookie not present!");
        return false;
      }
      if (edgeEventsCoookie == null || edgeEventsCoookie.Equals(""))
      {
        Console.Error.WriteLine("EdgeEventsCookie not present!");
        return false;
      }

      ClientEdgeEvent clientEdgeEvent = new ClientEdgeEvent
      {
        EventType = ClientEventType.EventInitConnection,
        SessionCookie = me.sessionCookie,
        EdgeEventsCookie = openEdgeEventsCookie
      };

      // New source:
      ConnectionCancelTokenSource = new CancellationTokenSource();

      // Attach a reader and loop until gone:
      ReadStreamTask = Task.Run(async () =>
      {
        try
        {
          Console.WriteLine("Write init message to server, with cancelToken: ");
          DuplexEventStream = streamClient.StreamEdgeEvent(
            cancellationToken: ConnectionCancelTokenSource.Token
          );
          await DuplexEventStream.RequestStream.WriteAsync(clientEdgeEvent);
          Console.WriteLine("Now Listening to Events...");
          IsOpen = true;

          while (await DuplexEventStream.ResponseStream.MoveNext())
          {
            me.InvokeEventBusReciever(DuplexEventStream.ResponseStream.Current);
          }
          Log.D("DMEConnection loop has exited.");
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ConnectionCancelTokenSource.Token)
        {
          ConnectionCancelTokenSource = null;
          DuplexEventStream = null;
          ReadStreamTask = null;
        }
      });
      
      return true;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    bool Reconnect()
    {
      if (IsOpen) {
        return false;
      }
      if (!IsShutdown())
      {
        return false;
      }

      Open(edgeEventsCoookie);
      return true;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool IsShutdown()
    {
      if (ConnectionCancelTokenSource.IsCancellationRequested)
      {
        return true;
      }
      return !IsOpen;
    }

    internal async Task<Boolean> Send(ClientEdgeEvent clientEdgeEvent)
    {
      try
      {
        // The connection might still be connecting.
        await DuplexEventStream.RequestStream.WriteAsync(clientEdgeEvent).ConfigureAwait(false);
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
      if (!ConnectionCancelTokenSource.IsCancellationRequested)
      {
        ConnectionCancelTokenSource.Cancel();
      }
      return await Send(terminateEvent);
    }

    public async Task<bool> PostLocationUpdate(Loc location)
    {
      if (location == null)
      {
        return false;
      }

      if (IsShutdown())
      {
        return false;
      }

      ClientEdgeEvent locationUpdate = new ClientEdgeEvent
      {
        EventType = ClientEventType.EventLocationUpdate,
        GpsLocation = location
      };

      return await Send(locationUpdate);
    }

    public async Task<bool> PostLatencyResult(Site site, Loc location)
    {
      if (location == null)
      {
        return false;
      }

      if (IsShutdown())
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
        latencySamplesEvent.Samples.Add(entry);
      }

      return await Send(latencySamplesEvent);
    }

    public void Dispose()
    {
      ConnectionCancelTokenSource.Cancel();
      streamClient = null;
      me = null;
    }
  }
}
