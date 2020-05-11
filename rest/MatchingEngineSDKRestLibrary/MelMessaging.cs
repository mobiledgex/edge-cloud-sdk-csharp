using System;

namespace DistributedMatchEngine.Mel
{
  public interface MelMessagingInterface
  {
    bool IsMelEnabled();
    string GetMelVersion();
    string GetUid();
    string SetLocationToken(string location_token, string app_name);
  }

  public class EmptyMelMessaging : MelMessagingInterface
  {
    public bool IsMelEnabled() { return false; }
    public string GetMelVersion() { return ""; }
    public string GetUid() { return ""; }
    public string SetLocationToken(string location_token, string app_name) { return ""; }
  }
}
