using System;
using System.Runtime.Serialization;

namespace CompanionApp
{
  [DataContract]
  public class RegisterDeviceRequest
  {
    [DataMember]
    public string user_name;
    [DataMember]
    public string user_email;
    [DataMember]
    public string user_id;
    [DataMember]
    public string device_name;
    [DataMember]
    public string model;
    [DataMember]
    public string serial;
  }

  [DataContract]
  public class RegisterDeviceReply
  {
    [DataMember]
    public string message;

    [DataMember]
    public bool success;

    [DataMember(Name = "success")]
    private string success_status_string
    {
      get
      {
        return success.ToString();
      }
      set
      {
        try
        {
          success = bool.Parse(value);
        }
        catch
        {
          success = false;
        }
      }
    }
  }
}