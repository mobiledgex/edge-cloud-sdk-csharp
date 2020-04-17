using System;

namespace DistributedMatchEngine.Mel
{
  public interface MelMessagingInterface
  {
    public bool IsMelReady();
    public bool IsMelEnabled();
    public string GetUuid();
    public string SetLocationToken(string location_token, string app_name);
  }

  class EmptyMelMessaging : MelMessagingInterface
  {
    public bool IsMelReady() { return false;  }
    public bool IsMelEnabled() { return false; }
    public string GetUuid() { return ""; }
    public string SetLocationToken(string location_token, string app_name) { return ""; }
  }
}
