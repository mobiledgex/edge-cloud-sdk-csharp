using System;
using System.Collections.Generic;

namespace DistributedMatchEngine
{
  public interface DeviceInfoApp
  {
    Dictionary<string, string> GetDeviceInfo();
  }

  /*!
   * Empty implementation of DeviceInfo interface
   * \ingroup classes_integration
   */
  public class EmptyDeviceInfo : DeviceInfoApp
  {
    public Dictionary<string, string> GetDeviceInfo()
    {
      throw new NotImplementedException("Required DeviceInfo interface function: GetDeviceInfo() is not defined!");
    }
  }
}
