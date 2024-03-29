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
  /*!
   * CarrierInfo telephony interface for the platform
   * Function implemented per platform
   * \ingroup classes_integration
   */
  public interface CarrierInfo
  {
    string GetCurrentCarrierName();
    string GetMccMnc();
    ulong GetCellID();
    ulong GetSignalStrength();
    string GetDataNetworkType();
  }

  /*!
   * Empty implementation of CarrierInfo interface
   * \ingroup classes_integration
   */
  public class EmptyCarrierInfo : CarrierInfo
  {
    public string GetCurrentCarrierName()
    {
      throw new NotImplementedException("Required CarrierInfo interface function: GetCurrentCarrierName() is not defined!");
    }

    public string GetMccMnc()
    {
      throw new NotImplementedException("Required CarrierInfo interface function: GetMccMnc() is not defined!");
    }

    public ulong GetCellID()
    {
      throw new NotImplementedException("Required CarrierInfo interface function: GetCellID() is not defined!");
    }

    public ulong GetSignalStrength()
    {
      throw new NotImplementedException("Required CarrierInfo interface function: GetSingalStength() is not defined!");
    }

    public string GetDataNetworkType()
    {
      throw new NotImplementedException("Required CarrierInfo interface function: GetDataNetworkType() is not defined!");
    }
  }
}
