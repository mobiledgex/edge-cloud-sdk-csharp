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

using System.Collections.Concurrent;

using System.Net.Sockets;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Threading;

using System;
using System.Threading.Tasks;

namespace DistributedMatchEngine.PerformanceMetrics
{
  public class NetTest : IDisposable
  {
    private MatchingEngine matchingEngine;

    public enum TestType
    {
      PING = 0,
      CONNECT = 1,
    };

    public class Site
    {
      public string host;
      public int port;
      public string L7Path; // This may be load balanced.
      public double lastPingMs;

      public TestType testType;

      int idx;
      int size;
      public double[] samples;

      public double average;
      public double stddev;

      public Site(TestType testType = TestType.CONNECT, int numSamples = 5)
      {
        this.testType = testType;
        samples = new double[numSamples];
      }

      public void addSample(double time)
      {
        samples[idx] = time;
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
          acc += samples[i];
        }
        average = acc / size;
        for (int i = 0; i < size; i++)
        {
          d = samples[i];
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

    public bool runTest;

    private Thread pingThread;
    public int PingIntervalMS { get; set; } = 5000;
    public int TestTimeoutMS = 5000;

    // For testing L7 Sites, possibly load balanced.
    HttpClient httpClient;

    public ConcurrentQueue<Site> sites { get; }

    public NetTest(MatchingEngine matchingEngine)
    {
      stopWatch = new Stopwatch();
      sites = new ConcurrentQueue<Site>();
      this.matchingEngine = matchingEngine;

      httpClient = new HttpClient();
      // TODO: GetConnection to connect from a particular network interface endpoint
      // httpClient.SocketsHttpHandler
      httpClient.Timeout = new TimeSpan(0, 0, TestTimeoutMS / 1000); // seconds
    }

    // Create a client and connect/disconnect on a server port. Not quite ping ICMP.
    // This method uses the cellular interface set in the MatchingEngine instance.
    public async Task<Double> ConnectAndDisconnectSocket(Site site)
    {
      stopWatch.Reset();

      stopWatch.Start();
      TimeSpan ts;
      using (var socket = await matchingEngine.GetTCPConnection(site.host, site.port, TestTimeoutMS).ConfigureAwait(false))
      {
        ts = stopWatch.Elapsed;
        stopWatch.Stop();
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
      if (runTest == true && enable == true)
      {
        return runTest;
      }

      runTest = enable;
      if (runTest)
      {
        pingThread = new Thread(RunNetTest);
        if (pingThread == null)
        {
          throw new Exception("Unable to create a thread!");
        }
        pingThread.Start();
      }
      else
      {
        if (pingThread != null)
        {
          pingThread.Join(PingIntervalMS);
        }
        pingThread = null;
      }
      return runTest;
    }

    // Basic utility funtion to connect and disconnect from any TCP port.
    public async void RunNetTest()
    {
      Log.D("Run Net Test");
      while (runTest)
      {
        Log.D("..");
        double elapsed = -1d;
        foreach (Site site in sites)
        {

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
                  Log.S(e.StackTrace);
                  elapsed = -1;
                }
              }
              break;
            case TestType.PING:
              {
                elapsed = Ping(site);
              }
              break;
          }
          site.lastPingMs = elapsed;
          if (elapsed >= 0)
          {
            site.addSample(elapsed);
            site.recalculateStats();
          }
          Log.S("Round trip to host: " + site.host + ", port: " + site.port + ", l7Path: " + site.L7Path + ", elapsed: " + elapsed + ", average: " + site.average + ", stddev: " + site.stddev);
        }
        await Task.Delay(PingIntervalMS).ConfigureAwait(false);
      }
    }

    public void Dispose()
    {
      if (httpClient != null)
      {
        httpClient.Dispose();
      }
    }
  }

}