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
using System.IO;
using System.Threading.Tasks;

namespace DistributedMatchEngine
{
  public class Util
  {
    public Util()
    {

    }

    public static string StreamToString(Stream ms)
    {
      ms.Position = 0;
      StreamReader reader = new StreamReader(ms);
      string jsonStr = reader.ReadToEnd();
      return jsonStr;
    }

    // FIXME: This function needs per device customization.
    public async static Task<Loc> GetLocationFromDevice()
    {
      return await Task.Run(() =>
      {
        long timeLongMs = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        long seconds = timeLongMs / 1000;
        int nanoSec = (int)(timeLongMs % 1000) * 1000000;
        var ts = new Timestamp { nanos = nanoSec, seconds = seconds.ToString() };
        var loc = new Loc()
        {
          course = 0,
          altitude = 100,
          horizontal_accuracy = 5,
          speed = 2,
          longitude = -122.149349,
          latitude = 37.459601,
          vertical_accuracy = 20,
          timestamp = ts
        };
        return loc;
      });
    }
  }
}
