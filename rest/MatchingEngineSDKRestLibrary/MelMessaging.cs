using System;

namespace DistributedMatchEngine.Mel
{
  public interface MelMessagingInterface
  {
    bool IsMelReady();
    bool IsMelEnabled();
    string GetUuid();
    string SetLocationToken(string location_token, string app_name);
  }

  public class EmptyMelMessaging : MelMessagingInterface
  {
    public bool IsMelReady() { return false;  }
    public bool IsMelEnabled() { return false; }
    public string GetUuid() { return ""; }
    public string SetLocationToken(string location_token, string app_name) { return ""; }
  }
}
