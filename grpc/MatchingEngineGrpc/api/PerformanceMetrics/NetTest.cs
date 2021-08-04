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

using System.Collections.Concurrent;

using System.Net.Sockets;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Threading;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;

/*!
 * PerformanceMetrics Namespace
 * \ingroup namespaces
 */
namespace DistributedMatchEngine.PerformanceMetrics
{
  /*!
   * Class that allows developers to easily test latency of their various backend servers.
   * This is used in the implementation of FindCloudlet Performance Mode.
   * \ingroup classes_util
   */
  public class NetTest : IDisposable
  {
    private MatchingEngine matchingEngine;

    /*!
     * TestType is either PING or CONNECT, where PING is ICMP Ping (not implemented) and CONNECT is is actually setting up a connection and then disconnecting immediately. 
     */
    public enum TestType
    {
      PING = 0,
      CONNECT = 1,
    }

    /*!
     * Object used by NetTest to test latency of server.
     * Each site object contains the server path or host + port, avg latency, standard deviation, list of latency times, and the TestType.
     * TestType is either PING or CONNECT, where PING is ICMP Ping (not implemented) and CONNECT is is actually setting up a connection and then disconnecting immediately. 
     * \ingroup classes_util
     */
    public class Site
    {
      public string host;
      public int port;
      public string L7Path; // This may be load balanced.
      public double lastPingMs;

      public TestType testType;

      int idx;

      /*!
       * Number of rolling samples. Default is 3
       */
      public int size { get; private set; }
      public Sample[] samples;
      public const int DEFAULT_NUM_SAMPLES = 3;

      public double average;
      public double stddev;

      /*!
       * Application instance of site. Used to test specific application instance
       */
      public Appinstance appInst;
      public Loc cloudletLocation;

      /*!
       * optional endpoint
       */
      public IPEndPoint localEndPoint;

      /*!
       * Constructor for Site class.
       * \param testType (TestType): Optional. Defaults to CONNECT
       * \param numSamples (int): Optional. Size of rolling sample set. Defaults to 3
       */
      public Site(TestType testType = TestType.CONNECT, int numSamples = DEFAULT_NUM_SAMPLES, IPEndPoint localEndPoint = null)
      {
        this.testType = testType;
        samples = new Sample[numSamples];
        this.localEndPoint = localEndPoint;
      }

      public void addSample(double time, long timestampMilliseconds)
      {
        var seconds = timestampMilliseconds / 1000;
        var nanos = (timestampMilliseconds - (seconds * 1000)) * 1000000;
        var sample = new Sample
        {
          Value = time,
          Timestamp = new Timestamp
          {
            Seconds = seconds,
            Nanos = (int)nanos,
          },
        };
        samples[idx] = sample;
        idx++;
        if (size < samples.Length) size++;
        idx = idx % samples.Length;
      }

      public void recalculateStats()
      {
        double acc = 0d;
        double vsum = 0d;
        double d;
        for (int i = 0; i < size; i++)
        {
          acc += samples[i].Value;
        }
        average = acc / size;
        for (int i = 0; i < size; i++)
        {
          d = samples[i].Value;
          vsum += (d - average) * (d - average);
        }
        if (size > 1)
        {
          // Bias Corrected Sample Variance
          vsum /= (size - 1);
        }
        stddev = Math.Sqrt(vsum);
      }
    }
    private Stopwatch stopWatch;

    public bool runTest = false;

    private Thread pingThread;
    public int PingIntervalMS { get; set; } = 5000;
    public int TestTimeoutMS = 5000;

    // For testing L7 Sites, possibly load balanced.
    HttpClient httpClient;

    public ConcurrentQueue<Site> sites { get; }

    /*!
     * NetTest constructor
     * \param matchingEngine (MatchingEngine)
     */
    public NetTest(MatchingEngine matchingEngine)
    {
      stopWatch = new Stopwatch();
      sites = new ConcurrentQueue<Site>();
      this.matchingEngine = matchingEngine;

      httpClient = new HttpClient();
      // TODO: GetConnection to connect from a particular network interface endpoint
      // httpClient.SocketsHttpHandler
      if (TestTimeoutMS / 1000 > 0)
      {
        httpClient.Timeout = new TimeSpan(0, 0, TestTimeoutMS / 1000); // seconds
      }
    }

    // Create a client and connect/disconnect on a server port. Not quite ping ICMP.
    // This method uses the cellular interface set in the MatchingEngine instance.
    public async Task<Double> ConnectAndDisconnectSocket(Site site)
    {
      stopWatch.Reset();

      stopWatch.Start();
      TimeSpan ts;
      using (var socket = await matchingEngine.GetTCPConnection(site.host, site.port, TestTimeoutMS, site.localEndPoint).ConfigureAwait(false))
      {
        ts = stopWatch.Elapsed;
        stopWatch.Stop();
        socket.Close();
      }

      return ts.TotalMilliseconds;
    }

    // Create a client and connect/disconnect on a partcular site.
    // TODO: This method does NOT use the Cellular interface pending availbility of a socket handler.
    public async Task<Double> ConnectAndDisconnect(Site site)
    {
      stopWatch.Reset();

      // The nature of this app specific GET API call is to expect some kind of
      // stateless empty body return also 200 OK.
      stopWatch.Start();
      using (var result = await httpClient.GetAsync(site.L7Path).ConfigureAwait(false))
      {
        TimeSpan ts = stopWatch.Elapsed;
        stopWatch.Stop();
        if (result.StatusCode == System.Net.HttpStatusCode.OK)
        {
          return ts.TotalMilliseconds;
        }
      }

      // Error, GET on L7 Path didn't return success.
      return -1d;
    }

    // Basic ICMP ping.
    // This does not swap interfaces to cellular.
    public double Ping(Site site)
    {
      Ping ping = new Ping();
      PingReply reply = ping.Send(site.host, TestTimeoutMS);
      long elapsedMs = reply.RoundtripTime;

      return elapsedMs;
    }

    public bool doTest(bool enable)
    {
      if ((enable && pingThread != null) ||
          (!enable && pingThread == null)) // Already running or already not running.
      {
        runTest = enable;
        return runTest;
      }
      else if (enable && pingThread == null) // Start thread
      {
        runTest = true;
        pingThread = new Thread(RunNetTest);
        if (pingThread == null)
        {
          throw new Exception("Unable to create a thread!");
        }
        pingThread.Start();
        return runTest;
      }
      else // Stop thread:
      {
        runTest = false;
        if (pingThread != null)
        {
          pingThread.Join(PingIntervalMS);
        }
        pingThread = null;
        return runTest;
      }
    }

    /*!
     * NetTest Runloop
     * Tests the list of Sites in a loop until developer cancels.
     * Developer can access the array of sites from the NetTest object or the ordered list by calling netTest.returnSortedSites()
     */
    public async void RunNetTest()
    {
      Log.D("Run Net Test");
      while (runTest)
      {
        Log.D("..");
        foreach (Site site in sites)
        {
          await TestSite(site);
        }
        await Task.Delay(PingIntervalMS).ConfigureAwait(false);
      }
    }

    /*!
     * Tests each site in the list of sites for numSamples and returns a list of Sites in order from lowest latency to highest.
     * \param numSamples (int): Number of tests per site
     * \return Task<Site[]>: Ordered list of sites from lowest latency to highest
     */
    public async Task<Site[]> RunNetTest(int numSamples)
    {
      Log.D("Running NetTest for " + numSamples + " iterations.");

      var tasks = new List<Task>();
      for (int i = 0; i < numSamples; i++)
      {
        foreach (Site site in sites)
        {
          tasks.Add(TestSite(site));
        }
      }

      try
      {
        await Task.WhenAll(tasks.ToArray());
        return GetSortedSites();
      }
      catch (AggregateException ae)
      {
        Log.E("Unable to complete all NetTest Tasks. Exception is " + ae.Message);
        throw ae;
      }
    }

    // Tests site based on TestType of site and protocol (l7Path)
    public async Task TestSite(Site site)
    {
      double elapsed = -1d;
      // Timestamp for test.
      var ts = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

      switch (site.testType)
      {

        case TestType.CONNECT:
          if (site.L7Path == null) // Simple host and port ping.
          {
            try
            {
              elapsed = await ConnectAndDisconnectSocket(site);
            }
            catch (Exception e)
            {
              Log.S("Error testing site: " + site.host + ":" + site.port);
              Log.S(e.StackTrace);
              elapsed = -1;
            }
          }
          else // Use L7 Path.
          {
            try
            {
              elapsed = await ConnectAndDisconnect(site);
            }
            catch (Exception e)
            {
              Log.S("Error testing l7Path site: " + site.L7Path);
              Log.S(e.Message);
              Log.S(e.StackTrace);
              if (e.InnerException != null)
              {
                Log.S(e.InnerException.Message);
                Log.S(e.InnerException.StackTrace);
              }
              elapsed = -1;
            }
          }
          break;

        case TestType.PING:
          elapsed = Ping(site);
          break;
      }

      site.lastPingMs = elapsed;
      if (elapsed >= 0)
      {
        site.addSample(elapsed, ts);
        site.recalculateStats();
      }
      Log.S("Round trip to host: " + site.host + ", port: " + site.port + ", l7Path: " + site.L7Path + ", elapsed: " + elapsed + ", average: " + site.average + ", stddev: " + site.stddev);
    }

    // Sort array of sites by lowest avg and lowest stddev
    public Site[] GetSortedSites()
    {
      var siteArr = sites.ToArray();
      Array.Sort(siteArr, delegate (Site x, Site y)
      {
        if (x.size == 0 || y.size == 0 )
        {
          return x.size > y.size ? -1 : 1;
        }

        if (x.average == 0 || y.average == 0)
        {
          return x.average > y.average ? -1 : 1;
        }

        if (x.average != y.average)
        {
          return x.average < y.average ? -1 : 1;
        }

        if (x.stddev == y.stddev) return 0;
        return x.stddev < y.stddev ? -1 : 1;
      }
      );
      return siteArr;
    }

    public void Dispose()
    {
      if (httpClient != null)
      {
        httpClient.CancelPendingRequests();
        httpClient = null;
      }
    }
  }
}
