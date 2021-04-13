﻿/**
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

namespace DistributedMatchEngine
{
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

  public class EdgeEventsConfig
  {
    // Configure how to send events

    /*!
     * port information for latency testing. This is the AppPort's internal port, not public mapped port for current AppInst.
     */
    public int latencyInternalPort;
    public NetTest.TestType latencyTestType = NetTest.TestType.CONNECT; // TCP connect. Use ping for UDP.
    public ClientEventsConfig latencyUpdateConfig; // config for latency updates
    public ClientEventsConfig locationUpdateConfig;// config for gps location updates

    // Configure how to respond to events
    public double latencyThresholdTrigger; // latency threshold in ms when new FindCloudlet is triggered
    public FindCloudletEventTrigger[] triggers;// events that application wants a new find cloudlet for

    // Defaults:
    public EdgeEventsConfig()
    {
      latencyInternalPort = 0; // implicit Ping only.
      latencyTestType = NetTest.TestType.CONNECT;
      latencyThresholdTrigger = 50;
      triggers = new FindCloudletEventTrigger[] {
                FindCloudletEventTrigger.CloudletStateChanged, FindCloudletEventTrigger.LatencyTooHigh
        };

      // Sane defaults, onTrigger, and once.
      latencyUpdateConfig = new ClientEventsConfig();
      locationUpdateConfig = new ClientEventsConfig();
    }

    public EdgeEventsConfig(EdgeEventsConfig edgeEventsConfig)
    {
      latencyInternalPort = edgeEventsConfig.latencyInternalPort; // implicit Ping only.
      latencyTestType = edgeEventsConfig.latencyTestType;
      latencyThresholdTrigger = edgeEventsConfig.latencyThresholdTrigger;
      if (edgeEventsConfig.triggers == null)
      {
        triggers = new FindCloudletEventTrigger[] {
                    FindCloudletEventTrigger.CloudletStateChanged, FindCloudletEventTrigger.LatencyTooHigh, FindCloudletEventTrigger.CloserCloudlet
            };
      }
      else
      {
        if (edgeEventsConfig.triggers.Length > 0)
        {
          triggers = new FindCloudletEventTrigger[edgeEventsConfig.triggers.Length];
        }
        for (int i = 0; i < edgeEventsConfig.triggers.Length; i++)
        {
          triggers[i] = edgeEventsConfig.triggers[i];
        }
      }

      // Sane defaults, onTrigger, and once.
      latencyUpdateConfig = new ClientEventsConfig(edgeEventsConfig.latencyUpdateConfig);
      locationUpdateConfig = new ClientEventsConfig(edgeEventsConfig.locationUpdateConfig);
    }

    public static EdgeEventsConfig createDefaultEdgeEventsConfig()
    {
      EdgeEventsConfig eeConfig = new EdgeEventsConfig();
      eeConfig.latencyThresholdTrigger = 50;

      eeConfig.latencyUpdateConfig.updateIntervalSeconds = 30;
      eeConfig.latencyUpdateConfig.updatePattern = ClientEventsConfig.UpdatePattern.onInterval;

      // This one will require location to be posted to the EdgeEvents state machine
      // by the Android location handler. Then, it posts to EdgeEvents that result at this interval.
      eeConfig.locationUpdateConfig.updateIntervalSeconds = 30;
      eeConfig.locationUpdateConfig.updatePattern = ClientEventsConfig.UpdatePattern.onInterval;

      return eeConfig;
    }

    /*!
     * Helper util to create a useful config.
     */
    public static EdgeEventsConfig createDefaultEdgeEventsConfig(double latencyUpdateIntervalSeconds,
                                                                 double locationUpdateIntervalSeconds,
                                                                 double latencyThresholdTriggerMs,
                                                                 ClientEventsConfig.UpdatePattern updatePattern)
    {
      EdgeEventsConfig eeConfig = new EdgeEventsConfig();
      eeConfig.latencyThresholdTrigger = latencyThresholdTriggerMs;

      eeConfig.latencyUpdateConfig.updateIntervalSeconds = latencyUpdateIntervalSeconds;
      eeConfig.latencyUpdateConfig.updatePattern = updatePattern;

      // This one will require location to be posted to the EdgeEvents state machine
      // by the Android location handler. Then, it posts that result at this interval.
      eeConfig.locationUpdateConfig.updateIntervalSeconds = locationUpdateIntervalSeconds;
      eeConfig.locationUpdateConfig.updatePattern = updatePattern;

      return eeConfig;
    }
  }
}
