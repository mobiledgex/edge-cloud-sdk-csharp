using System;
using System.Collections.Generic;
using System.IO;
using System.Json;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static DistributedMatchEngine.ClientEdgeEvent;
using static DistributedMatchEngine.PerformanceMetrics.NetTest;

namespace DistributedMatchEngine
{
  public class DMEConnection : IDisposable
  {
    private bool IsOpen = false;
    private bool ReOpenDmeConnection = false;

    private String HostOverride;
    private uint PortOverride;

    MatchingEngine me;
    private string edgeEventsCoookie { get; set; }

    // The Main MatchingEngine HTTP Client instance is intended to time out. The
    // instance below is expected to use default HTTP persistent connection behavior.
    private HttpClient DmeHttpClient;
    Stream senderStream;

    CancellationTokenSource cancelTokenSource;

    Stream DmeConnectionStream = null;
    Task readStreamTask;

    internal DMEConnection(MatchingEngine matchingEngine, string host = null, uint port = 0)
    {
      this.me = matchingEngine;
      cancelTokenSource = new CancellationTokenSource();

      if (host != null && host.Trim().Length != 0)
      {
        HostOverride = host;
        PortOverride = port == 0 ? MatchingEngine.defaultDmeRestPort : port; 
      }

      DmeHttpClient = new HttpClient();
      DmeHttpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
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

    // Incoming Server EdgeEvents, is wrapped from the server.
    ServerEdgeEvent ParseServerEdgeEvent(string responseStr)
    {
      ServerEdgeEvent serverEdgeEvent = null;
      try
      {
        // Enum is sort of broken for deserialization. Rather than try to use reflection, we happen to know what the object 
        serverEdgeEvent = ServerEdgeEvent.Build(responseStr, me.serializerSettings);
      }
      catch (Exception e)
      {
        Log.E("Failed to parse: " + e.Message);
        return null;
      }
      return serverEdgeEvent;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    void Close()
    {
      try
      {
        DmeHttpClient.CancelPendingRequests();
        cancelTokenSource.Cancel();
      } catch (Exception e)
      {
        Log.E("Exception Closing DMEConnection. Message: " + e.Message);
        Log.E("StackTrace: " + e.StackTrace);
      }
      {
        IsOpen = false;
      }
    }
#if false
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

      string uri;
      if (HostOverride == null || PortOverride == 0)
      {
        uri = me.CreateUri(me.GenerateDmeHostAddress(), MatchingEngine.defaultDmeRestPort);
      }
      else
      {
        uri = me.CreateUri(HostOverride, PortOverride);
      }
      uri += me.streamedgeeventAPI;


      // Open a connection:
      ClientEdgeEvent clientEdgeEvent = new ClientEdgeEvent
      {
        event_type = ClientEventType.EVENT_INIT_CONNECTION,
        session_cookie = me.sessionCookie,
        edge_events_cookie = openEdgeEventsCookie
      };
      // Stream Body listen loop:
      string jsonStr = GetString(clientEdgeEvent);
      var request = new HttpRequestMessage(HttpMethod.Post, uri);
      request.Content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
      request.Headers.TransferEncodingChunked = true;

      edgeEventsCoookie = openEdgeEventsCookie;
      DmeHttpClient = new HttpClient();
      DmeHttpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
      DmeHttpClient.DefaultRequestHeaders.ConnectionClose = false; // Keep alive.

      using (var response =
        DmeHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result)
      {
        lock (this)
        {
          IsOpen = true;
        }
        Log.D("IsOpen: " + IsOpen);
        int c;
        using (var bodyStream = response.Content.ReadAsStreamAsync().Result)
        {
          // Parse the body:
          // StreamReader is nonBlocking. We want to block on an bursty infinite stream.Read().
          c = 0;
          var sb = new StringBuilder();
          while (bodyStream.CanRead)
          {
            // IF end of strema, it's -1;
            int b = bodyStream.ReadByte();
            int len = (int)b;
            sb.Append(b);
            if (len == 0)
            {
              // Server said no more chunks.
              break;
            }
            Log.D("X: " + c++ + ": " + (char)b);
            //var buffer = new char[len];
            //int readChars = reader.ReadBlock(buffer, 0, len);
            //var currentLine = new String(buffer, 0, readChars);
            //Log.D("Line: " + currentLine);
          }
          string s = sb.ToString();
          Log.D(s);

        }
      }
      return true;
    }
#endif

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
      DmeHttpClient = new HttpClient();
      //DmeHttpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
      //DmeHttpClient.DefaultRequestHeaders.ConnectionClose = false; // Keep alive.

      // DmeHttpClient should only be created in Open (which listens for objects).
      Task.Run(async () =>
      {
        string uri;
        if (HostOverride == null || PortOverride == 0)
        {
          uri = me.CreateUri(me.GenerateDmeHostAddress(), MatchingEngine.defaultDmeRestPort);
        }
        else
        {
          uri = me.CreateUri(HostOverride, PortOverride);
        }
        uri += me.streamedgeeventAPI;

        // Open a connection:
        ClientEdgeEvent clientEdgeEvent = new ClientEdgeEvent
        {
          event_type = ClientEventType.EVENT_INIT_CONNECTION,
          session_cookie = me.sessionCookie,
          edge_events_cookie = openEdgeEventsCookie
        };

        // Stream Body listen loop:
        string jsonStr = GetString(clientEdgeEvent);
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Content = new StringContent(jsonStr, Encoding.UTF8, "text/event-stream");
        request.Headers.TransferEncodingChunked = true;


        using (var response =
         await DmeHttpClient.SendAsync(request))
        {
          IsOpen = true;
          Log.D("IsOpen: " + IsOpen);
          using (var stream = await response.Content.ReadAsStreamAsync())
          // Parse the body:
          // StreamReader is nonBlocking. We want to block on an bursty infinite stream.Read().
          using (var reader = new StreamReader(stream))
          {
            StringBuilder sb = new StringBuilder();
            
            // Server MUST support HTTP/1.2 Server-Sent-Events. Otherwise, this will only get
            // one event. GPRC-Gateway just says use GRPC, where it is HTTP/2 already.
            while (!reader.EndOfStream && !cancelTokenSource.IsCancellationRequested) 
            {
              var jsonObjStr = reader.ReadToEnd(); // One way Server Sourced Event endpoint

              Console.WriteLine("XXX: Read: " + jsonObjStr);
              var serverEdgeEvent = ParseServerEdgeEvent(jsonObjStr);
              Console.WriteLine("XXX: Parsed Type: " + serverEdgeEvent.event_type);
              me.EventBusReciever(serverEdgeEvent);

            }
            Console.WriteLine("XXX: Stream End reached.");
            IsOpen = false; // Exit loop.
          }
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
      if (cancelTokenSource.IsCancellationRequested)
      {
        return true;
      }
      return !IsOpen;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    internal bool Send(ClientEdgeEvent clientEdgeEvent)
    {
      var jsonStr = GetString(clientEdgeEvent);
      if (jsonStr.Length == 0)
      {
        return false;
      }

      string uri = null;
      if (HostOverride != null && PortOverride != 0)
      {
        uri = me.CreateUri(HostOverride, PortOverride) + me.streamedgeeventAPI;
      }
      else
      {
        uri = me.CreateUri(me.GenerateDmeHostAddress(), MatchingEngine.defaultDmeRestPort) + me.streamedgeeventAPI;
      }

      // Fire and forget into DME Connection.
      Task.Run(() =>
      {
        Reconnect(); // Need an awaitable version to use here.
        DmePostRequest(uri, jsonStr);
      }
      );

      return true;
    }

    private async Task<Stream> DmePostRequest(string uri, string jsonStr)
    {
      // FIXME: Choose network TBD (.Net Core 2.1)
      Log.S("URI: " + uri);
      var stringContent = new StringContent(jsonStr, Encoding.UTF8, "application/json");
      Log.D("Post Body: " + jsonStr);
      // Assertion: Use same upstream connection as Open as in HttpClient pool.
      HttpResponseMessage response = await DmeHttpClient.PostAsync(uri, stringContent).ConfigureAwait(false);

      if (response == null)
      {
        throw new Exception("Null http response object!");
      }

      if (response.StatusCode != HttpStatusCode.OK)
      {
        string responseBodyStr = response.Content.ReadAsStringAsync().Result;
        JsonObject jsObj = (JsonObject)JsonValue.Parse(responseBodyStr);
        string extendedErrorStr;
        int errorCode;
        if (jsObj.ContainsKey("message") && jsObj.ContainsKey("code"))
        {
          extendedErrorStr = jsObj["message"];
          try
          {
            errorCode = jsObj["code"];
          }
          catch (FormatException)
          {
            errorCode = -1; // Bad code number format
          }
          throw new HttpException(extendedErrorStr, response.StatusCode, errorCode);
        }
        else
        {
          // Unknown error message format, throw exception with inner:
          try
          {
            response.EnsureSuccessStatusCode();
          }
          catch (Exception e)
          {
            throw new HttpException(e.Message, response.StatusCode, -1, e);
          }
        }
      }

      // Normal path:
      Stream replyStream = await response.Content.ReadAsStreamAsync();
      return replyStream;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    internal bool SendTerminate()
    {

      ClientEdgeEvent terminateEvent = new ClientEdgeEvent
      {
        event_type = ClientEventType.EVENT_TERMINATE_CONNECTION
      };
      if (!cancelTokenSource.IsCancellationRequested)
      {
        cancelTokenSource.Cancel();
      }
      return Send(terminateEvent);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool PostLocationUpdate(Loc location)
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
        event_type = ClientEventType.EVENT_LOCATION_UPDATE,
        gps_location = location
      };

      return Send(locationUpdate);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool PostLatencyResult(Site site, Loc location)
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
        event_type = ClientEventType.EVENT_LATENCY_SAMPLES,
        gps_location = location
      };

      List<Sample> list = new List<Sample>();
      // Bleh.
      Dictionary<string, string> dInfo = null;
      if (me.deviceInfo != null)
      {
        dInfo = me.deviceInfo.GetDeviceInfo();
      }
      for (int i = 0; i < site.samples.Length; i++)
      {
        Sample sample = new Sample();
        if (site.samples[i] > 0d)
        {
          if (dInfo != null && dInfo.Count > 0)
          {
            sample.value = site.samples[i];
          }
          list.Add(sample);
        }
      }
      latencySamplesEvent.samples = list.ToArray();

      return Send(latencySamplesEvent);
    }

    public void Dispose()
    {
      cancelTokenSource.Cancel();
      DmeHttpClient.CancelPendingRequests();
      DmeHttpClient = null;
      me = null;
    }
  }
}
