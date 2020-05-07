using System;

namespace DistributedMatchEngine.Mel
{
  public interface MelMessagingInterface
  {
    bool IsMelReady();
    bool IsMelEnabled();
    string GetCookie();
    string SetLocationToken(string location_token, string app_name);
  }

  public class EmptyMelMessaging : MelMessagingInterface
  {
    public bool IsMelReady() { return false;  }
    public bool IsMelEnabled() { return false; }
    public string GetCookie() { return ""; }
    public string SetLocationToken(string location_token, string app_name) { return ""; }
  }
}
