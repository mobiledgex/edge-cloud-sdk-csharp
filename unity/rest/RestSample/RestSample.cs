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
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.IO;

using DistributedMatchEngine;

namespace RestSample
{
  class Program
  {
    static string carrierName = "tdg";
    static string devName = "MobiledgeX";
    static string appName = "MobiledgeX SDK Demo";
    static string appVers = "1.0";
    static string developerAuthToken = "";

    static string host = "mexdemo.dme.mobiledgex.net";
    static UInt32 port = 38001;

    // Get the ephemerial carriername from device specific properties.
    async static Task<string> getCurrentCarrierName()
    {
      var dummy = await Task.FromResult(0);
      return carrierName;
    }

    static Timestamp createTimestamp(int futureSeconds)
    {
        long ticks = DateTime.Now.Ticks;
        long sec = ticks / TimeSpan.TicksPerSecond; // Truncates.
        long remainderTicks = ticks - (sec * TimeSpan.TicksPerSecond);
        int nanos = (int)(remainderTicks / TimeSpan.TicksPerMillisecond) * 1000000;

        var timestamp = new Timestamp
        {
            seconds = (sec + futureSeconds).ToString(),
            nanos = nanos
        };
        return timestamp;
    }

    static List<QosPosition> CreateQosPositionList(Loc firstLocation, double direction_degrees, double totalDistanceKm, double increment)
    {
      var req = new List<QosPosition>();
      double kmPerDegreeLong = 111.32; // at Equator
      double kmPerDegreeLat = 110.57; // at Equator
      double addLongitude = (Math.Cos(direction_degrees * (Math.PI/180)) * increment) / kmPerDegreeLong;
      double addLatitude = (Math.Sin(direction_degrees * (Math.PI/180)) * increment) / kmPerDegreeLat;
      double i = 0d;
      double longitude = firstLocation.longitude;
      double latitude = firstLocation.latitude;

      long id = 1;

      while (i < totalDistanceKm)
      {
        longitude += addLongitude;
        latitude += addLatitude;
        i += increment;

        // FIXME: No time is attached to GPS location, as that breaks the server!
        var qloc = new QosPosition
        {
          positionid = id.ToString(),
          gps_location = new Loc
          {
            longitude = longitude,
            latitude = latitude,
            timestamp = createTimestamp(100)
          }
        };

        req.Add(qloc);
        id++;
      }
      return req;
    }     

    async static Task Main(string[] args)
    {
      try
      {
        carrierName = await getCurrentCarrierName();

        Console.WriteLine("RestSample!");

        MatchingEngine me = new MatchingEngine();
        port = MatchingEngine.defaultDmeRestPort;

        // Start location task. This is for test use only. The source of the
        // location in an Unity application should be from an application context
        // LocationService.
        var locTask = Util.GetLocationFromDevice();

        var registerClientRequest = me.CreateRegisterClientRequest(carrierName, devName, appName, appVers, developerAuthToken);

        // APIs depend on Register client to complete successfully:
        try
        {
          var registerClientReply = await me.RegisterClient(host, port, registerClientRequest);
          Console.WriteLine("RegisterClient Reply Status: " + registerClientReply.status);
        }
        catch (HttpException httpe) // HTTP status, and REST API call error codes.
        {
          // server error code, and human readable message:
          Console.WriteLine("RegisterClient Exception: " + httpe.Message + ", WebException StatusCode: " + httpe.WebExceptionStatus + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }
        // Do Verify and FindCloudlet in concurrent tasks:
        var loc = await locTask;

        // Independent requests:
        var verifyLocationRequest = me.CreateVerifyLocationRequest(carrierName, loc);
        var findCloudletRequest = me.CreateFindCloudletRequest(carrierName, devName, appName, appVers, loc);
        var getLocationRequest = me.CreateGetLocationRequest(carrierName);


        // These are asynchronious calls, of independent REST APIs.

        // FindCloudlet:
        try
        {
          var findCloudletReply = await me.FindCloudlet(host, port, findCloudletRequest);
          Console.WriteLine("FindCloudlet Reply: " + findCloudletReply.status);
          Console.WriteLine("FindCloudlet:" +
                  " ver: " + findCloudletReply.ver +
                  ", fqdn: " + findCloudletReply.fqdn +
                  ", cloudlet_location: " +
                  " long: " + findCloudletReply.cloudlet_location.longitude +
                  ", lat: " + findCloudletReply.cloudlet_location.latitude);
          // App Ports:
          foreach (AppPort p in findCloudletReply.ports)
          {
            Console.WriteLine("Port: fqdn_prefix: " + p.fqdn_prefix +
                  ", protocol: " + p.proto +
                  ", public_port: " + p.public_port +
                  ", internal_port: " + p.internal_port +
                  ", path_prefix: " + p.path_prefix +
                  ", end_port: " + p.end_port);
          }
        }
        catch (HttpException httpe)
        {
          Console.WriteLine("FindCloudlet Exception: " + httpe.Message + ", WebException StatusCode: " + httpe.WebExceptionStatus + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }

        // Get Location:
        try
        {
          var getLocationReply = await me.GetLocation(host, port, getLocationRequest);
          var location = getLocationReply.network_location;
          Console.WriteLine("GetLocationReply: longitude: " + location.longitude + ", latitude: " + location.latitude);
        }
        catch (HttpException httpe)
        {
          Console.WriteLine("GetLocation Exception: " + httpe.Message + ", WebException StatusCode: " + httpe.WebExceptionStatus + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }

        // Verify Location:
        try
        {
          Console.WriteLine("VerifyLocation() may timeout, due to reachability of carrier verification servers from your network.");
          var verifyLocationReply = await me.VerifyLocation(host, port, verifyLocationRequest);
          Console.WriteLine("VerifyLocation Reply: " + verifyLocationReply.gps_location_status);
        }
        catch (HttpException httpe)
        {
          Console.WriteLine("VerifyLocation Exception: " + httpe.Message + ", WebException StatusCode: " + httpe.WebExceptionStatus + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }
        catch (InvalidTokenServerTokenException itste)
        {
          Console.WriteLine(itste.Message + "\n" + itste.StackTrace);
        }

        // Get QosPositionKpi:
        try
        {
          // Create a list of quality of service position requests:
          var firstLoc = new Loc
          {
            longitude = -121.892558,
            latitude = 37.327820,
            timestamp = new Timestamp { seconds = "0", nanos = 0 }
          };
          var requestList = CreateQosPositionList(firstLoc, 45, 2, 0.1);

          var qosPositionKpiRequest = me.CreateQosPositionRequest(requestList);
          QosPositionKpiStreamReply qosPositionKpiStreamReply = await me.GetQosPositionKpi(host, port, qosPositionKpiRequest); // FIXME: Stream of objects

          if (qosPositionKpiStreamReply.result == null || qosPositionKpiStreamReply.error != null)
          {
            Console.WriteLine("Reply result missing: " + qosPositionKpiStreamReply);
          }
          else
          {
            Console.WriteLine("Result: " + qosPositionKpiStreamReply.result);
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(QosPositionResult));
            MemoryStream ms = new MemoryStream();
            foreach (QosPositionResult qpr in qosPositionKpiStreamReply.result.position_results)
            {
              ms.Position = 0;
              serializer.WriteObject(ms, qpr);
              string jsonStr = Util.StreamToString(ms);
              Console.WriteLine("QosPositionResult: " + jsonStr);
            }
          }
        }
        catch (HttpException httpe)
        {
          Console.WriteLine("VerifyLocation Exception: " + httpe.Message + ", WebException StatusCode: " + httpe.WebExceptionStatus + ", API ErrorCode: " + httpe.ErrorCode + "\nStack: " + httpe.StackTrace);
        }

      }
      catch (Exception e) // Catch All
      {
        Console.WriteLine(e.Message + "\n" + e.StackTrace);
      }

    }
  };
}