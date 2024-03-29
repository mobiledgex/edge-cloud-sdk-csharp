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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Json;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

using DistributedMatchEngine.PerformanceMetrics;
using DistributedMatchEngine.Mel;
using System.Net.Sockets;
using System.Runtime.Serialization;


/*!
 * DistributedMatchEngine Namespace
 * \ingroup namespaces
 */
namespace DistributedMatchEngine
{

  /*!
   * Occurs when MobiledgeX does not have user's MCC and MNC mapped to a DME
   * \ingroup exceptions_dme
   */
  public class DmeDnsException : Exception
  {
    public DmeDnsException(string message, Exception InnerException = null)
       : base(message, InnerException)
    {
    }
  }

  /*!
   * MatchingEngine APIs are implemented via HTTP REST calls. This occurs if MatchingEngine API post request fails.
   * \ingroup exceptions_dme
   */
  public class HttpException : Exception
  {
    public HttpStatusCode HttpStatusCode { get; set; }
    public int ErrorCode { get; set; }
    public HttpException(string message, HttpStatusCode statusCode, int errorCode)
        : base(message)
    {
      this.HttpStatusCode = statusCode;
      this.ErrorCode = errorCode;
    }

    public HttpException(string message, HttpStatusCode statusCode, int errorCode, Exception innerException)
        : base(message, innerException)
    {
      this.HttpStatusCode = statusCode;
      this.ErrorCode = errorCode;
    }
  }

  /*!
   * TokenException
   * \ingroup exceptions_dme
   */
  public class TokenException : Exception
  {
    public TokenException(string message)
        : base(message)
    {
    }

    public TokenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
  }

  /*!
   * RegisterClient failure
   * \ingroup exceptions_dme
   */
  public class RegisterClientException : Exception
  {
    public RegisterClientException(string message)
        : base(message)
    {
    }

    public RegisterClientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
  }

  /*!
   * RegisterClient generates a session cookie on success. If a developer calls another MatchingEngine API before a successful RegisterClient, this exception will probably occur.
   * \ingroup exceptions_dme
   */
  public class SessionCookieException : Exception
  {
    public SessionCookieException(string message)
        : base(message)
    {
    }

    public SessionCookieException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
  }

  /*!
   * FindCloudlet failure
   * \ingroup exceptions_dme
   */
  public class FindCloudletException : Exception
  {
    public FindCloudletException(string message)
        : base(message)
    {
    }

    public FindCloudletException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
  }

  // Minimal logger without log levels:
  static class Log
  {
    // Stdout:
    public static void S(string msg)
    {
      Console.WriteLine(msg);
    }
    // Stderr:
    public static void E(string msg)
    {
      TextWriter errorWriter = Console.Error;
      errorWriter.WriteLine(msg);
    }
    // Stdout:
    [ConditionalAttribute("DEBUG")]
    public static void D(string msg)
    {
      Console.WriteLine(msg);
    }
  }

  public enum OperatingSystem
  {
    ANDROID,
    IOS,
    OTHER
  }

  /*!
   * Two modes to call FindCloudlet. First is Proximity (default) which finds the nearest cloudlet based on gps location with application instance
   * Second is Performance. This mode will test all cloudlets with application instance deployed to find cloudlet with lowest latency. This mode takes longer to finish because of latency test.
   */
  public enum FindCloudletMode
  {
    PROXIMITY,
    PERFORMANCE
  }

  /*!
   * Main MobiledgeX SDK class. This class provides functions to find nearest cloudlet with the
   * developer's application instance deployed and to connect to that application instance.
   */
  public partial class MatchingEngine : IDisposable
  { 

    public const string TAG = "MatchingEngine";
    private static HttpClient httpClient;
    public const UInt32 defaultDmeRestPort = 38001;
    public const string carrierNameDefault = "wifi";
    public const string wifiCarrier = "wifi";
    public const string wifiOnlyDmeHost = wifiCarrier + "." + baseDmeHost; // Demo mode only.
    public const string baseDmeHost = "dme.mobiledgex.net";

    UInt32 dmePort { get; set; } = defaultDmeRestPort; // HTTP REST port

    /*!
     * Enable edge features. If enabled, this may cause permission prompts on
     * some target devices due to the MatchingEngine probing the current network
     * state for edge capabilities. Edge features may be degraded if not enabled.
     */
    public static bool EnableEnhancedLocationServices { get; set; } = false;

    public CarrierInfo carrierInfo { get; set; }
    public NetInterface netInterface { get; set; }
    public UniqueID uniqueID { get; set; }
    public DeviceInfo deviceInfo { get; private set; }
    private MelMessagingInterface melMessaging { get; set; }

    internal DataContractJsonSerializerSettings serializerSettings = new DataContractJsonSerializerSettings
    {
      UseSimpleDictionaryFormat = true
    };

    // API Paths:
    private string registerAPI = "/v1/registerclient";
    private string verifylocationAPI = "/v1/verifylocation";
    private string findcloudletAPI = "/v1/findcloudlet";
    private string appinstlistAPI = "/v1/getappinstlist";
    private string dynamiclocgroupAPI = "/v1/dynamiclocgroup";
    private string getfqdnlistAPI = "/v1/getfqdnlist";
    private string appofficialfqdnAPI = "/v1/getappofficialfqdn";
    private string qospositionkpiAPI = "/v1/getqospositionkpi";
    internal string streamedgeeventAPI = "/v1/streamedgeevent";

    public const int DEFAULT_REST_TIMEOUT_MS = 10000;

    private const bool EXPERIMENTAL_FEATURES = false;

    public bool useOnlyWifi { get; set; } = false;
    // Use SSL for DME.
    public bool useSSL { get; set; } = true;

    public string sessionCookie { get; set; }
    string tokenServerURI;
    private bool disposedValue;

    string authToken { get; set; }

    // Global local endpoint override for FindCloudlet, NetTest, and GetConnection API helpers.
    // This is used for background App related operations like EdgeEvents processing, if set.
    // Default routing otherwise.
    public string LocalIP { get; set; }

    // Delegate for Events.
    public delegate void EventBusDelegate(ServerEdgeEvent serverEdgeEvent);
    public EventBusDelegate EventBusReciever { get; internal set; }
    public DMEConnection DmeConnection { get; internal set; }

    public RegisterClientRequest LastRegisterClientRequest { get; private set; }

    /*!
     * Constructor for MatchingEngine class.
     * \param carrierInfo (CarrierInfo): 
     * \param netInterface (NetInterface): 
     * \param uniqueID (UniqueID):
     * \param deviceInfo (DeviceInfo):
     * \section meconstructorexample Example
     * \snippet RestSample.cs meconstructorexample
     */
    public MatchingEngine(CarrierInfo carrierInfo = null, NetInterface netInterface = null, UniqueID uniqueID = null, DeviceInfo deviceInfo = null)
    {
      httpClient = new HttpClient();
      httpClient.Timeout = TimeSpan.FromMilliseconds(DEFAULT_REST_TIMEOUT_MS);
      if (carrierInfo == null)
      {
        this.carrierInfo = new EmptyCarrierInfo();
      }
      else
      {
        this.carrierInfo = carrierInfo;
      }

      if (netInterface == null)
      {
        this.netInterface = new EmptyNetInterface();
      }
      else
      {
        this.netInterface = netInterface;
      }

      if (uniqueID == null)
      {
        this.uniqueID = new EmptyUniqueID();
      }
      else
      {
        this.uniqueID = uniqueID;
      }

      if (deviceInfo == null)
      {
        this.deviceInfo = new EmptyDeviceInfo();
      }
      else
      {
        this.deviceInfo = deviceInfo;
      }

      // Default to empty.
      SetMelMessaging(null);

      // Setup a dummy event delegate for monitoring events:
      EventBusReciever += (ServerEdgeEvent serverEdgeEvent) =>
      {
        Log.D("MatchingEngine EdgeEvent Notice: " + serverEdgeEvent.event_type);
      };
    }

    /*!
     * GetDmeConnection
     */
    public DMEConnection GetDMEConnection(string edgeEventCookie, string dmeHost = null, uint dmePort = 0)
    {
      if (!EXPERIMENTAL_FEATURES)
      {
        return null;
      }
      if (DmeConnection == null)
      {
        DmeConnection = new DMEConnection(this, dmeHost, dmePort);
      }

      if (edgeEventCookie == null || edgeEventCookie.Trim().Length == 0)
      {
        // Will not init!
        return null;
      }

      if (!DmeConnection.IsShutdown())
      {
        return DmeConnection;
      }

      if (!DmeConnection.Open(edgeEventCookie))
      {
        return DmeConnection = null;
      }
      return DmeConnection;
    }

    DMEConnection GetDMEConnection()
    {
      return DmeConnection;
    }

    /*!
     * A device specific interface.
     * @private
     */
    public void SetMelMessaging(MelMessagingInterface melInterface)
    {
      if (melInterface != null)
      {
        this.melMessaging = melInterface;
      }
      else
      {
        this.melMessaging = new EmptyMelMessaging();
      }
    }

    /*!
     * Set the REST timeout for DME APIs.
     * \param timeout_in_milliseconds (int)
     * \return Timespan
     * \ingroup functions_dmeutils
     */
    public TimeSpan SetTimeout(int timeout_in_milliseconds)
    {
      if (timeout_in_milliseconds > 1)
      {
        return httpClient.Timeout = TimeSpan.FromMilliseconds(timeout_in_milliseconds);
      }
      return httpClient.Timeout = TimeSpan.FromMilliseconds(DEFAULT_REST_TIMEOUT_MS);
    }

    /*!
     * GetUniqueIDType
     * \ingroup functions_dmeutils
     */
    public string GetUniqueIDType()
    {
      return uniqueID.GetUniqueIDType();
    }

    /*!
     * GetUniqueID
     * \ingroup functions_dmeutils
     */
    public string GetUniqueID()
    {
      return uniqueID.GetUniqueID();
    }

    /*!
     * GetCellID
     * \ingroup functions_dmeutils
     */
    public ulong GetCellID()
    {
      return carrierInfo.GetCellID();
    }

    /*!
     * Returns the carrier's mcc+mnc which is mapped to a carrier in the backend (ie. 26201 -> GDDT).
     * MCC stands for Mobile Country Code and MNC stands for Mobile Network Code.
     * If useWifiOnly or cellular is off + wifi is up, this will return """".
     * Empty string carrierName is the alias for any, which will search all carriers for application instances.
     * \ingroup functions_dmeutils
     */
    public string GetCarrierName()
    {
      if (useOnlyWifi)
      {
        return "";
      }

      try
      {
        string mccmnc = carrierInfo.GetMccMnc();
        if (mccmnc == null)
        {
          return ""; // fallback carriername
        }
        return mccmnc;
      }
      catch (NotImplementedException nie)
      {
        Log.S("GetMccMnc is not implemented. NotImplementedException: " + nie.Message);
        return "";
      }
    }

    /*!
     * GenerateDmeHostAddress
     * This will generate the dme host name based on GetMccMnc() -> "mcc-mnc.dme.mobiledgex.net".
     * If GetMccMnc fails or returns null, this will return a fallback dme host: "wifi.dme.mobiledgex.net"(this is the EU + GDDT DME).
     * This function is used by any DME APIs calls where no host and port overloads are provided. 
     * \ingroup functions_dmeutils
     */
    public string GenerateDmeHostAddress()
    {
      if (carrierInfo == null)
      {
        throw new InvalidCarrierInfoException("Missing platform integration interface.");
      }

      if (useOnlyWifi)
      {
        return wifiOnlyDmeHost;
      }

      string mccmnc = GetCarrierName();
      if (mccmnc == "")
      {
        Log.E("PlatformIntegration CarrierInfo interface does not have a valid MCCMNC string.");
        if (netInterface.HasWifi())
        {
          return wifiOnlyDmeHost; // fallback to wifi, this hostname must/should always exist.
        }
        else
        {
          throw new DmeDnsException("Cannot generate DME hostname, no mccmnc returned and wifi is not available.");
        }
      }

      // Check minimum size:
      if (mccmnc.Length < 5)
      {
        Log.E("PlatformIntegration CarrierInfo interface does not have a valid MCCMNC string length.");
        throw new DmeDnsException("Cannot generate DME hostname, mccmnc length is invalid: " + mccmnc.Length);
      }

      string mcc = mccmnc.Substring(0, 3);
      string mnc = mccmnc.Substring(3);

      string potentialDmeHost = mcc + "-" + mnc + "." + baseDmeHost;

      try
      {
        // This host might not actually exist (yet):
        IPHostEntry ipHostEntry = Dns.GetHostEntry(potentialDmeHost);
        if (ipHostEntry.AddressList.Length > 0)
        {
          return potentialDmeHost;
        }
      }
      catch (Exception e)
      {
        throw new DmeDnsException("Cannot generate DME hostname: " + potentialDmeHost + ", Message: " + e.Message, e);
      }

      // Let the caller handle an unsupported DME configuration.
      throw new DmeDnsException("Generated mcc-mnc. BaseDmeHost: " + baseDmeHost + ", hostname not found: " + potentialDmeHost);
    }

    internal string CreateUri(string host, uint port)
    {
      string proto = useSSL ? "https://" : "http://";
      return proto + host + ":" + port;
    }

    private async Task<Stream> PostRequest(string uri, string jsonStr)
    {
      // FIXME: Choose network TBD (.Net Core 2.1)
      Log.S("URI: " + uri);
      var stringContent = new StringContent(jsonStr, Encoding.UTF8, "application/json");
      Log.D("Post Body: " + jsonStr);
      HttpResponseMessage response = await httpClient.PostAsync(uri, stringContent).ConfigureAwait(false);

      if (response == null)
      {
        throw new Exception("Null http response object!");
      }

      if (response.StatusCode != HttpStatusCode.OK)
      {
        string responseBodyStr = response.Content.ReadAsStringAsync().Result;
        JsonObject jsObj = (JsonObject)JsonValue.Parse(responseBodyStr);
        string extendedErrorStr;
        int errorCode;
        if (jsObj.ContainsKey("message") && jsObj.ContainsKey("code"))
        {
          extendedErrorStr = jsObj["message"];
          try
          {
            errorCode = jsObj["code"];
          }
          catch (FormatException)
          {
            errorCode = -1; // Bad code number format
          }
          throw new HttpException(extendedErrorStr, response.StatusCode, errorCode);
        }
        else
        {
          // Unknown error message format, throw exception with inner:
          try
          {
            response.EnsureSuccessStatusCode();
          }
          catch (Exception e)
          {
            throw new HttpException(e.Message, response.StatusCode, -1, e);
          }
        }
      }

      // Normal path:
      Stream replyStream = await response.Content.ReadAsStreamAsync();
      return replyStream;
    }

    private static String ParseToken(String uri)
    {
      string[] uriandparams = uri.Split('?');
      if (uriandparams.Length < 1)
      {
        return null;
      }
      string parameterStr = uriandparams[1];
      if (parameterStr.Equals(""))
      {
        return null;
      }

      string[] parameters = parameterStr.Split('&');
      if (parameters.Length < 1)
      {
        return null;
      }

      foreach (string keyValueStr in parameters)
      {
        string[] keyValue = keyValueStr.Split('=');
        int pos = keyValue[0].Length + 1; // step over '='
        if (pos >= keyValueStr.Length)
        {
          return null;
        }

        string value = keyValueStr.Substring(pos, keyValueStr.Length - pos);
        if (keyValue[0].Equals("dt-id"))
        {
          return value;
        }
      }

      return null;
    }

    private string RetrieveToken(string aTokenServerURI)
    {
      HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(aTokenServerURI);
      httpWebRequest.AllowAutoRedirect = false;

      HttpWebResponse response = null;
      string token = null;
      string uriLocation = null;
      // 303 See Other is behavior is different between standard C#
      // and what's potentially in Unity.
      try
      {
        response = (HttpWebResponse)httpWebRequest.GetResponse();
        if (response != null)
        {
          if (response.StatusCode != HttpStatusCode.SeeOther)
          {
            throw new TokenException("Expected an HTTP 303 SeeOther.");
          }
          uriLocation = response.Headers["Location"];
        }
      }
      catch (System.Net.WebException we)
      {
        response = (HttpWebResponse)we.Response;
        if (response != null)
        {
          if (response.StatusCode != HttpStatusCode.SeeOther)
          {
            throw new TokenException("Expected an HTTP 303 SeeOther.", we);
          }
          uriLocation = response.Headers["Location"];
        }
      }

      if (uriLocation != null)
      {
        Log.S("uriLocation: " + uriLocation);
        token = ParseToken(uriLocation);
      }

      if (token == null)
      {
        throw new InvalidTokenServerTokenException("Token not found or parsable in the URI string: " + uriLocation);
      }

      return token;
    }
    
    /*!
     * \anchor RegisterClient
     * Creates the RegisterClientRequest object that will be used in the RegisterClient function.The RegisterClientRequest object wraps the parameters that have been provided to this function. 
     * \ingroup functions_dmeapis
     * \param orgName (string): Organization name
     * \param appName (string): Application name
     * \param appVersion (string): Application version
     * \param authToken (string): Optional authentication token for application. If none supplied, default is null.
     * \param cellID (UInt32): Optional cell tower id. If none supplied, default is 0.
     * \param uniqueIDType (string): Optional
     * \param uniqueID (string): Optional
     * \param tags (Tag[]): Optional
     * \return RegisterClientRequest
     * \section createregisterexample Example
     * \snippet RestSample.cs createregisterexample
     */
    public RegisterClientRequest CreateRegisterClientRequest(string orgName, string appName, string appVersion, string authToken = null,
      UInt32 cellID = 0, string uniqueIDType = null, string uniqueID = null, Dictionary<string, string> tags = null)
    {
      return new RegisterClientRequest
      {
        ver = 1,
        org_name = orgName,
        app_name = appName,
        app_vers = appVersion,
        auth_token = authToken,
        cell_id = cellID,
        unique_id_type = uniqueIDType,
        unique_id = uniqueID,
        tags = tags
      };
    }

    private ReplyStatus ParseReplyStatus(string responseStr)
    {
      string key = "status";
      JsonObject jsObj = (JsonObject)JsonValue.Parse(responseStr);
      ReplyStatus replyStatus;
      try
      {
        replyStatus = (ReplyStatus)Enum.Parse(typeof(ReplyStatus), jsObj[key]);
      }
      catch
      {
        replyStatus = ReplyStatus.Undefined;
      }
      return replyStatus;
    }

    /*!
     * First DME API called. This will register the client with the MobiledgeX backend and
     * check to make sure that the app that the user is running exists. (ie. This confirms
     * that CreateApp in Console/Mcctl has been run successfully). RegisterClientReply
     * contains a session cookie that will be used (automatically) in later API calls.
     * It also contains a uri that will be used to get the verifyLocToken used in VerifyLocation.
     * \ingroup functions_dmeapis
     * \param request (RegisterClientRequest)
     * \return Task<RegisterClientReply>
     * \section registerexample Example
     * \snippet RestSample.cs registerexample
     */
    public async Task<RegisterClientReply> RegisterClient(RegisterClientRequest request)
    {
      return await RegisterClient(GenerateDmeHostAddress(), defaultDmeRestPort, request);
    }

    private RegisterClientRequest UpdateRequestForUniqueID(RegisterClientRequest request)
    {
      string uid = melMessaging.GetUid();
      string aUniqueIdType = GetUniqueIDType(); // Read: device model
      string aUniqueId = GetUniqueID();
      string manufacturer = melMessaging.GetManufacturer();

      if (manufacturer != null &&
        aUniqueIdType != null && aUniqueIdType.Length > 0 &&
        aUniqueId != null && aUniqueId.Length > 0)
      {
        request.unique_id_type = manufacturer + ":" + aUniqueIdType + ":HASHED_ID";
        request.unique_id = aUniqueId;
      }

      return request;
    }

    private RegisterClientRequest UpdateRequestForDeviceInfo(RegisterClientRequest request)
    {
      Dictionary<string, string> dict = deviceInfo.GetDeviceInfo();

      if (dict == null)
      {
        return request;
      }

      if (request.tags == null)
      {
        request.tags = new Dictionary<string, string>();
      }

      foreach (KeyValuePair<string, string> pair in dict)
      {
        if (pair.Key != null && pair.Value != null)
        {
          Log.D("Tags Dictonary Add Key: " + pair.Key + ", Value: " + pair.Value);
          request.tags[pair.Key] = pair.Value;
        }
      }

      return request;
    }


    private FindCloudletRequest UpdateRequestForQoSNetworkPriority(FindCloudletRequest request, IPEndPoint localEndPoint)
    {
      if (localEndPoint == null || localEndPoint.AddressFamily != AddressFamily.InterNetwork)
      {
        IPEndPoint endPoint = Util.GetDefaultLocalEndPointIPV4();
        if (endPoint == null)
        {
          return request;
        }
        localEndPoint = endPoint;
      }
      
      if (request.tags == null)
      {
        request.tags = new Dictionary<string, string>();
      }
      request.tags["ip_user_equipment"] = localEndPoint.Address.ToString();
      return request;
    }

    private AppInstListRequest UpdateRequestForQoSNetworkPriority(AppInstListRequest request, IPEndPoint localEndPoint)
    {
      if (localEndPoint == null || localEndPoint.AddressFamily != AddressFamily.InterNetwork)
      {
        IPEndPoint endPoint = Util.GetDefaultLocalEndPointIPV4();
        if (endPoint == null)
        {
          return request;
        }
        localEndPoint = endPoint;
      }

      if (request.tags == null)
      {
        request.tags = new Dictionary<string, string>();
      }
      request.tags["ip_user_equipment"] = localEndPoint.Address.ToString();
      return request;
    }

    /*!
     * RegisterClient overload with hardcoded DME host and port. Only use for testing.
     * \ingroup functions_dmeapis
     * \param host (string): DME host
     * \param port(uint): DME port (REST: 38001, GRPC: 50051)
     * \param request (RegisterClientRequest)
     * \return Task<RegisterClientReply>
     * \section registeroverloadexample Example
     * \snippet RestSample.cs registeroverloadexample
     */
    public async Task<RegisterClientReply> RegisterClient(string host, uint port, RegisterClientRequest request)
    {
      RegisterClientRequest oldRequest = request;
      request = new RegisterClientRequest()
      {
        ver = oldRequest.ver,
        org_name = oldRequest.org_name,
        app_name = oldRequest.app_name,
        app_vers = oldRequest.app_vers,
        auth_token = oldRequest.auth_token,
        cell_id = oldRequest.cell_id,
        tags = oldRequest.tags
      };

      // DeviceInfo
      request = UpdateRequestForUniqueID(request);
      request = UpdateRequestForDeviceInfo(request);
      if (request.tags != null)
      {
        request.htags = Tag.DictionaryToHashtable(request.tags);
      }
      // Debug Log Serialization issues:
      Log.D("Pre Serialize: Request Reference" + request);
      Log.D("Pre Serialize OrgName: " + request.org_name + ", " + "AppName: " + request.app_name + ", AppVer: " + request.app_vers);
      Log.D("Pre Serialize AuthToken: " + request.auth_token + ", " + "CellID: " + request.cell_id + ", Ver: " + request.ver);
      Log.D("Pre Serialize Tag Reference: " + request.tags);
      if (request.tags != null) {
        Log.D("Pre Serialize Tags Count: " + request.tags.Count);
      }

      string jsonStr = "";
      MemoryStream ms = new MemoryStream();
      try
      {
        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(RegisterClientRequest), serializerSettings);
        ms = new MemoryStream();
        serializer.WriteObject(ms, request);
        jsonStr = Util.StreamToString(ms);
      }
      catch (Exception e)
      {
        // This is critical enough to always print and re-throw, due to potential serialization issues.
        Log.E("Exception Message: " + e.Message);
        Log.E("Exception Stack: " + e.StackTrace);
        throw e;
      }

      RegisterClientReply reply = null;
      string responseStr = "";
      try
      {
        Stream responseStream = await PostRequest(CreateUri(host, port) + registerAPI, jsonStr);
        if (responseStream == null || !responseStream.CanRead)
        {
          Log.E("Unreadable RegisterClient stream! This should not happen.");
          return null;
        }

        DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(RegisterClientReply), serializerSettings);
        responseStr = Util.StreamToString(responseStream);
        Log.D("Response: " + responseStr);
        byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
        ms = new MemoryStream(byteArray);
        reply = (RegisterClientReply)deserializer.ReadObject(ms);

        if (reply.tags == null)
        {
          reply.tags = Tag.HashtableToDictionary(reply.htags);
        }

        this.sessionCookie = reply.session_cookie;
        this.tokenServerURI = reply.token_server_uri;

        // Some platforms won't parse emums with same library binary.
        reply.status = reply.status == ReplyStatus.Undefined ? ParseReplyStatus(responseStr) : reply.status;

        if (reply.status == ReplyStatus.Success)
        {
          LastRegisterClientRequest = request; // Update last successful request.
        }
        else
        {
          Log.E("RegisterClient not successful. DME Server used: " + host + ", carrierName: " + GetCarrierName() + ", appName: " + request.app_name + ", appVersion: " + request.app_vers + ", organizationName: " + request.org_name + ", status: " + reply.status + ", Message: " + responseStr);
        }
        return reply;
      }
      catch (HttpException he)
      {
        Log.E("Exception during RegisterClient. DME Server used: " + host + ", carrierName: " + GetCarrierName() + ", appName: " + request.app_name + ", appVersion: " + request.app_vers + ", organizationName: " + request.org_name + ", status: " + he.HttpStatusCode + ", Message: " + he.Message);
        if (he.HttpStatusCode == HttpStatusCode.NotFound)
        {
          Log.E("Please check that the appName, appVersion, and orgName correspond to a valid app definition on MobiledgeX.");
        }
        throw he;
      }
      catch (HttpRequestException hre)
      {
        // DME might not exist at all:
        Log.E("Exception during RegisterClient. DME Server used: " + host + ", carrierName: " + GetCarrierName() + ", appName: " + request.app_name + ", appVersion: " + request.app_vers + ", organizationName: " + request.org_name + ", Message: " + hre.Message);
        throw hre;
      }
    }

    /*!
     * \anchor FindCloudlet
     * Creates the FindCloudletRequest object that will be used in the FindCloudlet function.
     * The FindCloudletRequest object wraps the parameters that have been provided to this function.
     * \ingroup functions_dmeapis
     * \param loc (Loc): User location
     * \param carrierName (string): Optional device carrier (if not provided, carrier information will be pulled from device)
     * \param cellID (UInt32): Optional cell tower id. If none supplied, default is 0.
     * \param tags (Tag[]): Optional
     * \return FindCloudletRequest
     * \section createfindcloudletexample Example
     * \snippet RestSample.cs createfindcloudletexample
     */
    public FindCloudletRequest CreateFindCloudletRequest(Loc loc, string carrierName = null, UInt32 cellID = 0, Dictionary<string, string> tags = null)
    {
      if (sessionCookie == null)
      {
        throw new SessionCookieException("Unable to find session cookie. Please register client again");
      }

      if (carrierName == null) {
        carrierName = GetCarrierName();
      }

      return new FindCloudletRequest
      {
        session_cookie = this.sessionCookie,
        gps_location = loc,
        carrier_name = carrierName,
        cell_id = cellID,
        tags = tags
      };
    }

    private FindCloudletReply.FindStatus ParseFindStatus(string responseStr)
    {
      string key = "status";
      JsonObject jsObj = (JsonObject)JsonValue.Parse(responseStr);
      FindCloudletReply.FindStatus status;
      try
      {
        status = (FindCloudletReply.FindStatus)Enum.Parse(typeof(FindCloudletReply.FindStatus), jsObj[key]);
      }
      catch
      {
        status = FindCloudletReply.FindStatus.Unknown;
      }
      return status;
    }

    private AppPort[] ParseAppPortTypes(AppPort[] appports, string responseStr)
    {
      JsonObject jsObj = (JsonObject)JsonValue.Parse(responseStr);
      JsonArray ports;

      if (jsObj.ContainsKey("ports"))
      {
        ports = (JsonArray)jsObj["ports"];
        for (int i = 0; i < ports.Count; i++)
        {
          try
          {
            JsonValue jval = ports[i];
            appports[i].proto = (LProto)Enum.Parse(typeof(LProto), jval["proto"]);
          }
          catch
          {
            appports[i].proto = LProto.Unknown;
          }
        }
      }
      return appports;
    }

    /*!
     * FindCloudlet returns information needed for the client app to connect to an application backend deployed through MobiledgeX.
     * If there is an application backend instance found, FindCloudetReply will contain the fqdn of the application backend and an array of AppPorts (with information specific to each application backend endpoint)
     * \ingroup functions_dmeapis
     * \param request (FindCloudletRequest)
     * \param mode (FindCloudletMode): Optional. Default is PROXIMITY. PROXIMITY will just return the findCloudletReply sent by DME (Generic REST API to findcloudlet endpoint). PERFORMANCE will test all app insts deployed on the specified carrier network and return the cloudlet with the lowest latency (Note: PERFORMANCE may take some time to return). Default value if mode parameter is not supplied is PROXIMITY.
     * \return Task<FindCloudletReply>
     * \section findcloudletexample Example
     * \subsection findcloudletproximityexample Proximity Example
     * \snippet RestSample.cs findcloudletexample
     * \subsection findcloudletperformanceexample Performance Example
     * \snippet RestSample.cs findcloudletperformanceexample
     */
    public async Task<FindCloudletReply> FindCloudlet(FindCloudletRequest request, FindCloudletMode mode = FindCloudletMode.PROXIMITY, IPEndPoint localEndPoint = null)
    {
      return await FindCloudlet(GenerateDmeHostAddress(), defaultDmeRestPort, request, mode, localEndPoint);
    }

    private async Task<FindCloudletReply> FindCloudletProximityMode(string host, uint port, FindCloudletRequest request)
    {
      request.htags = Tag.DictionaryToHashtable(request.tags);

      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(FindCloudletRequest), serializerSettings);
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = null;
      responseStream = await PostRequest(CreateUri(host, port) + findcloudletAPI, jsonStr);
      if (responseStream == null || !responseStream.CanRead)
      {
        Log.E("Unreadable FindCloudletProximityMode reply stream!");
        return null;
      }

      string responseStr = Util.StreamToString(responseStream);
      Log.D("ResponseStr: " + responseStr);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(FindCloudletReply), serializerSettings);
      FindCloudletReply reply = (FindCloudletReply)deserializer.ReadObject(ms);
      if (reply.tags == null)
      {
        reply.tags = Tag.HashtableToDictionary(reply.htags);
      }

      // Reparse if default value.
      reply.status = reply.status == FindCloudletReply.FindStatus.Unknown ? ParseFindStatus(responseStr) : reply.status;

      // Port has an emum to reparse if set to default as well:
      ParseAppPortTypes(reply.ports, responseStr);

      return reply;
    }

    // Helper function for FindCloudlet
    private FindCloudletReply CreateFindCloudletReplyFromBestSite(FindCloudletReply reply, NetTest.Site site)
    {
      Appinstance appinstance = site.appInst;

      return new FindCloudletReply
      {
        ver = reply.ver,
        status = reply.status,
        fqdn = appinstance.fqdn,
        ports = appinstance.ports,
        cloudlet_location = site.cloudletLocation,
        tags = reply.tags
      };
    }

    private NetTest.Site InitHttpSite(AppPort appPort, Appinstance appinstance, Loc cloudletLocation = null, int numSamples = NetTest.Site.DEFAULT_NUM_SAMPLES)
    {
      NetTest.Site site = new NetTest.Site(numSamples: numSamples);

      int port = appPort.public_port;
      string host = appPort.fqdn_prefix + appinstance.fqdn;

      site.L7Path = host + ":" + port;
      site.appInst = appinstance;
      site.cloudletLocation = cloudletLocation;
      return site;
    }

    private NetTest.Site InitTcpSite(AppPort appPort, Appinstance appinstance, Loc cloudletLocation = null, int numSamples = NetTest.Site.DEFAULT_NUM_SAMPLES, IPEndPoint localEndPoint = null)
    {
      NetTest.Site site = new NetTest.Site(numSamples: numSamples);

      site.host = appPort.fqdn_prefix + appinstance.fqdn;
      site.port = appPort.public_port;

      site.appInst = appinstance;
      site.cloudletLocation = cloudletLocation;
      site.localEndPoint = localEndPoint;
      return site;
    }

    private NetTest.Site InitUdpSite(AppPort appPort, Appinstance appinstance, Loc cloudletLocation = null, int numSamples = NetTest.Site.DEFAULT_NUM_SAMPLES, IPEndPoint localEndPoint = null)
    {
      NetTest.Site site = new NetTest.Site(testType: NetTest.TestType.PING, numSamples: numSamples);

      site.host = appPort.fqdn_prefix + appinstance.fqdn;
      site.port = appPort.public_port;

      site.appInst = appinstance;
      site.cloudletLocation = cloudletLocation;
      site.localEndPoint = localEndPoint;
      return site;
    }

    // Helper function for FindCloudlet
    private NetTest.Site[] CreateSitesFromAppInstReply(AppInstListReply reply, int testPort = 0, int numSamples = NetTest.Site.DEFAULT_NUM_SAMPLES, IPEndPoint localEndPoint = null)
    {
      List<NetTest.Site> sites = new List<NetTest.Site>();

      foreach (CloudletLocation cloudlet in reply.cloudlets)
      {
        foreach (Appinstance appinstance in cloudlet.appinstances)
        {
          // Find an test AppPort to add from cloudlet.
          AppPort useAppPort = null;
          if (testPort != 0)
          {
            foreach (var aPort in appinstance.ports)
            {
              if (IsInPortRange(aPort, testPort))
              {
                useAppPort = aPort;
                if (aPort.proto == LProto.Tcp)
                {
                  sites.Add(InitTcpSite(useAppPort, appinstance, cloudletLocation: cloudlet.gps_location, numSamples: numSamples, localEndPoint: localEndPoint));
                }
                else
                {
                  throw new FindCloudletException("FindCloudletPerformance error, the Tcp testPort supplied was not found");
                }
              }
            }
            if(useAppPort == null)
            {
              throw new FindCloudletException("FindCloudletPerformance error, the Tcp testPort supplied was not found");
            }
          }
          else
          {
            if (!deviceInfo.IsPingSupported())
            {
              AppPort tcpPort = null;
              foreach (AppPort appPort in appinstance.ports)
              {
                if (appPort.proto == LProto.Tcp)
                {
                  tcpPort = appPort;
                  break;
                }
              }
              if (tcpPort == null)
              {
                throw new FindCloudletException("FindCloudlet Performance is not supported, Your device doesn't support Ping and your Application Instance doesn't have any TCP Ports.");
              }
              sites.Add(InitTcpSite(tcpPort, appinstance, cloudletLocation: cloudlet.gps_location, numSamples: numSamples, localEndPoint: localEndPoint));
            }
            else
            {
              foreach (var port in appinstance.ports)
              {
                if (port.proto == LProto.Udp)
                {
                  if (useAppPort == null)
                  {
                    useAppPort = port;
                  }
                }
                else if (port.proto == LProto.Tcp)
                {
                  useAppPort = port;
                  break;
                }
              }

              if (useAppPort.proto == LProto.Udp)
              {
                Log.E("Warning: Found only UDP port. ICMP Ping testing will likely fail. Please specify a TCP port in your app.");
              }

              switch (useAppPort.proto)
              {
                case LProto.Tcp:
                  sites.Add(InitTcpSite(useAppPort, appinstance, cloudletLocation: cloudlet.gps_location, numSamples: numSamples, localEndPoint: localEndPoint));
                  break;

                case LProto.Udp:
                  sites.Add(InitUdpSite(useAppPort, appinstance, cloudletLocation: cloudlet.gps_location, numSamples: numSamples, localEndPoint: localEndPoint));
                  break;

                default:
                  Log.E("Unsupported protocol " + useAppPort.proto + " found when trying to create sites for NetTest");
                  break;
              }

            }
          }
        }
      }
      return sites.ToArray();
    }

    /*!
     * FindCloudlet overload with hardcoded DME host and port. Only use for testing.
     * \ingroup functions_dmeapis
     * \param host (string): DME host
     * \param port(uint): DME port (REST: 38001, GRPC: 50051)
     * \param request (FindCloudletRequest)
     * \param mode (FindCloudletMode): Optional. Default is PROXIMITY. PROXIMITY will just return the findCloudletReply sent by DME (Generic REST API to findcloudlet endpoint). PERFORMANCE will test all app insts deployed on the specified carrier network and return the cloudlet with the lowest latency (Note: PERFORMANCE may take some time to return). Default value if mode parameter is not supplied is PROXIMITY.
     * \param localEndpoint (IPEndPoint) Optional. Specifiy a local interface IPEndPoint for performance mode.
     * \return Task<FindCloudletReply>
     * \section findcloudletoverloadexample Example
     * \snippet RestSample.cs findcloudletoverloadexample
     */
    public async Task<FindCloudletReply> FindCloudlet(string host, uint port, FindCloudletRequest request, FindCloudletMode mode = FindCloudletMode.PROXIMITY, IPEndPoint localEndPoint = null)
    {
      try
      {
        FindCloudletReply fcReply = null;
        request = UpdateRequestForQoSNetworkPriority(request, localEndPoint);
        if (mode == FindCloudletMode.PROXIMITY)
        {
          fcReply = await FindCloudletProximityMode(host, port, request).ConfigureAwait(false);
        }
        else
        {
          fcReply = await FindCloudletPerformanceMode(host, port, request, localEndPoint: localEndPoint).ConfigureAwait(false);
        }

        if (fcReply == null)
        {
          return null;
        }

        // TODO: Refactor.
        if (fcReply.status == FindCloudletReply.FindStatus.Found)
        {
          DmeConnection = GetDMEConnection(fcReply.edge_events_cookie, host, port);
        }
        else
        {
          Log.E("FindCloudlet not successful, using DME Server: " + host + ", status: " + fcReply.status);
        }
        return fcReply;
      }
      catch (HttpException he)
      {
        Log.E("Exception during FindCloudlet, using DME Server: " + host + ", status: " + he.HttpStatusCode + ", Message: " + he.Message);
        if (he.HttpStatusCode == HttpStatusCode.NotFound)
        {
          Log.E("Please verify app registration details.");
        }
        throw he;
      }
      catch (HttpRequestException hre)
      {
        // DME might not exist at all:
        Log.E("Exception during FindCloudlet. DME Server used: " + host + ", Message: " + hre.Message);
        throw hre;
      }
    }

    // FindCloudlet with GetAppInstList and NetTest
    public async Task<FindCloudletReply> FindCloudletPerformanceMode(string host, uint port, FindCloudletRequest request, int testPort = 0, int numOfSamples = 5, IPEndPoint localEndPoint = null)
    {

      FindCloudletReply fcReply = await FindCloudletProximityMode(host, port, request);
      if (fcReply.status != FindCloudletReply.FindStatus.Found)
      {
        return fcReply;
      }

      // Dummy bytes to load cellular network path
      Byte[] bytes = new Byte[2048];
      Dictionary<string, string> tags = new Dictionary<string, string>();
      tags["Buffer"] = bytes.ToString();

      AppInstListRequest appInstListRequest = CreateAppInstListRequest(request.gps_location, request.carrier_name, tags: tags);
      appInstListRequest = UpdateRequestForQoSNetworkPriority(appInstListRequest, localEndPoint);
      AppInstListReply aiReply = await GetAppInstList(host, port, appInstListRequest);

      if (aiReply.tags == null)
      {
        aiReply.tags = Tag.HashtableToDictionary(aiReply.htags);
      }
      if (aiReply.status != AppInstListReply.AIStatus.Success)
      {
        throw new FindCloudletException("Unable to GetAppInstList. GetAppInstList status is " + aiReply.status);
      }

      if (aiReply.cloudlets.Length == 0)
      {
        Log.E("Check FindCloudletPerformance Request parameters. Empty cloudlet list returned from GetAppInstList.");
        return new FindCloudletReply { status = FindCloudletReply.FindStatus.Found};
      }

      // Check for global override for site performance testing:
      IPEndPoint useEndpoint = localEndPoint != null ? localEndPoint : GetIPEndPointByHostName(this.LocalIP);
      NetTest.Site[] sites = CreateSitesFromAppInstReply(aiReply, testPort: testPort, localEndPoint: useEndpoint);
      if (sites.Length == 0)
      {
        throw new FindCloudletException("No sites returned from CreateSitesFromAppInstReply");
      }

      NetTest netTest = new NetTest(this);
      foreach (NetTest.Site site in sites)
      {
        netTest.sites.Enqueue(site);
      }

      try
      {
        sites = await netTest.RunNetTest(5);
      }
      catch (AggregateException ae)
      {
        throw new FindCloudletException("Unable to RunNetTest. AggregateException is " + ae.Message);
      }

      return CreateFindCloudletReplyFromBestSite(fcReply, sites[0]);
    }

    /*!
     * Wrapper function for RegisterClient and FindCloudlet. Same functionality as calling them separately. This API cannot be used for Non-Platform APPs.
     * \ingroup functions_dmeapis
     * \param orgName (string): Organization name
     * \param appName (string): Application name
     * \param appVersion (string): Application version
     * \param loc (Loc): User location
     * \param carrierName (string): Optional device carrier (if not provided, carrier information will be pulled from device)
     * \param authToken (string): Optional authentication token for application. If none supplied, default is null.
     * \param cellID (UInt32): Optional cell tower id. If none supplied, default is 0.
     * \param uniqueIDType (string): Optional
     * \param uniqueID (string): Optional
     * \param tags (Dictionary<string, string>): Optional
     * \param mode (FindCloudletMode): Optional. Default is PROXIMITY. PROXIMITY will just return the findCloudletReply sent by DME (Generic REST API to findcloudlet endpoint). PERFORMANCE will test all app insts deployed on the specified carrier network and return the cloudlet with the lowest latency (Note: PERFORMANCE may take some time to return). Default value if mode parameter is not supplied is PROXIMITY.
     * \return Task<FindCloudletReply>
     */
    public async Task<FindCloudletReply> RegisterAndFindCloudlet(
      string orgName, string appName, string appVersion, Loc loc, string carrierName = "", string authToken = null, 
      UInt32 cellID = 0, string uniqueIDType = null, string uniqueID = null, Dictionary<string, string> tags = null, FindCloudletMode mode = FindCloudletMode.PROXIMITY)
    {
      return await RegisterAndFindCloudlet(GenerateDmeHostAddress(), defaultDmeRestPort,
        orgName, appName, appVersion, loc,
        carrierName, authToken, cellID, uniqueIDType, uniqueID, tags, mode);
    }

    /*!
     * RegisterAndFindCloudlet overload with hardcoded DME host and port. Only use for testing. This API cannot be used for Non-Platform APPs.
     * \ingroup functions_dmeapis
     * \param host (string): DME host
     * \param port(uint): DME port (REST: 38001, GRPC: 50051)
     * \param orgName (string): Organization name
     * \param appName (string): Application name
     * \param appVersion (string): Application version
     * \param loc (Loc): User location
     * \param carrierName (string): Optional device carrier (if not provided, carrier information will be pulled from device)
     * \param authToken (string): Optional authentication token for application. If none supplied, default is null.
     * \param cellID (UInt32): Optional cell tower id. If none supplied, default is 0.
     * \param uniqueIDType (string): Optional
     * \param uniqueID (string): Optional
     * \param tags (Tag[]): Optional
     * \param mode (FindCloudletMode): Optional. Default is PROXIMITY. PROXIMITY will just return the findCloudletReply sent by DME (Generic REST API to findcloudlet endpoint). PERFORMANCE will test all app insts deployed on the specified carrier network and return the cloudlet with the lowest latency (Note: PERFORMANCE may take some time to return). Default value if mode parameter is not supplied is PROXIMITY.
     * \return Task<FindCloudletReply>
     */
    public async Task<FindCloudletReply> RegisterAndFindCloudlet(string host, uint port,
       string orgName, string appName, string appVersion, Loc loc, string carrierName = "", string authToken = null, 
      UInt32 cellID = 0, string uniqueIDType = null, string uniqueID = null, Dictionary<string, string> tags = null, FindCloudletMode mode = FindCloudletMode.PROXIMITY)
    {
      // Register Client
      RegisterClientRequest registerRequest = CreateRegisterClientRequest(
        orgName: orgName,
        appName: appName,
        appVersion: appVersion,
        authToken: authToken,
        cellID: cellID,
        uniqueIDType: uniqueIDType,
        uniqueID: uniqueID,
        tags: tags);
      RegisterClientReply registerClientReply = await RegisterClient(host, port, registerRequest);
      if (registerClientReply.tags == null)
      {
        registerClientReply.tags = Tag.HashtableToDictionary(registerClientReply.htags);
      }
      if (registerClientReply.status != ReplyStatus.Success)
      {
        throw new RegisterClientException("RegisterClientReply status is " + registerClientReply.status);
      }
      // Find Cloudlet 
      FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(
        loc: loc,
        carrierName: carrierName,
        cellID: cellID,
        tags: tags);
      FindCloudletReply findCloudletReply = await FindCloudlet(host, port, findCloudletRequest, mode);
      if (findCloudletReply.tags == null)
      {
        findCloudletReply.tags = Tag.HashtableToDictionary(findCloudletReply.htags);
      }

      return findCloudletReply;
    }

    /*!
     * Creates the VerifyLocationRequest object that will be used in VerifyLocation
     * \ingroup functions_dmeapis
     * \param loc (Loc): User location
     * \param carrierName (string): Optional device carrier (if not provided, carrier information will be pulled from device)
     * \param cellID (UInt32): Optional cell tower id. If none supplied, default is 0.
     * \param tags (Tag[]): Optional
     * \return VerifyLocationRequest
     * \section createverifylocationexample Example
     * \snippet RestSample.cs createverifylocationexample
     */
    public VerifyLocationRequest CreateVerifyLocationRequest(Loc loc, string carrierName = null, UInt32 cellID = 0, Dictionary<string, string> tags = null)
    {
      if (sessionCookie == null)
      {
        return null;
      }

      if (carrierName == null) {
        carrierName = GetCarrierName();
      }

      return new VerifyLocationRequest
      {
        ver = 1,
        session_cookie = this.sessionCookie,
        gps_location = loc,
        carrier_name = carrierName,
        verify_loc_token = null,
        cell_id = cellID,
        tags = tags
      };
    }

    private VerifyLocationReply.TowerStatus ParseTowerStatus(string responseStr)
    {
      string key = "tower_status";
      JsonObject jsObj = (JsonObject)JsonValue.Parse(responseStr);
      VerifyLocationReply.TowerStatus status;
      try
      {
        status = (VerifyLocationReply.TowerStatus)Enum.Parse(typeof(VerifyLocationReply.TowerStatus), jsObj[key]);
      }
      catch
      {
        status = VerifyLocationReply.TowerStatus.TowerUnknown;
      }
      return status;
    }

    private VerifyLocationReply.GPSLocationStatus ParseGpsLocationStatus(string responseStr)
    {
      string key = "gps_location_status";
      JsonObject jsObj = (JsonObject)JsonValue.Parse(responseStr);
      VerifyLocationReply.GPSLocationStatus status;
      try
      {
        status = (VerifyLocationReply.GPSLocationStatus)Enum.Parse(typeof(VerifyLocationReply.GPSLocationStatus), jsObj[key]);
      }
      catch
      {
        status = VerifyLocationReply.GPSLocationStatus.Unknown;
      }
      return status;
    }

    /*!
     * Makes sure that the user's location is not spoofed based on cellID and gps location.
     * Returns the Cell Tower status (CONNECTED_TO_SPECIFIED_TOWER if successful) and Gps Location status (LOC_VERIFIED if successful).
     * Also provides the distance between where the user claims to be and where carrier believes user to be (via gps and cell id) in km.
     * \ingroup functions_dmeapis
     * \param request (VerifyLocationRequest)
     * \return Task<VerifyLocationReply>
     * \section verifylocationexample Example
     * \snippet RestSample.cs verifylocationexample
     */
    public async Task<VerifyLocationReply> VerifyLocation(VerifyLocationRequest request)
    {
      return await VerifyLocation(GenerateDmeHostAddress(), defaultDmeRestPort, request);
    }

    /*!
     * VerifyLocation overload with hardcoded DME host and port. Only use for testing.
     * \ingroup functions_dmeapis
     * \param host (string): DME host
     * \param port(uint): DME port (REST: 38001, GRPC: 50051)
     * \param request (VerifyLocationRequest)
     * \return Task<VerifyLocationReply>
     * \section verifylocationoverloadexample Example
     * \snippet RestSample.cs verifylocationoverloadexample
     */
    public async Task<VerifyLocationReply> VerifyLocation(string host, uint port, VerifyLocationRequest request)
    {
      string token = RetrieveToken(tokenServerURI);
      request.verify_loc_token = token;
      request.htags = Tag.DictionaryToHashtable(request.tags);

      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(VerifyLocationRequest), serializerSettings);
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);
      Log.D("RequestStr: " + jsonStr);

      Stream responseStream = null;
      try {
        responseStream = await PostRequest(CreateUri(host, port) + verifylocationAPI, jsonStr);
        if (responseStream == null || !responseStream.CanRead)
        {
          Log.E("Unreadable VerifyLocation stream! This should not happen.");
          return null;
        }
      }
      catch (HttpException he)
      {
        Log.E("Exception hit: DME Server host: " + host + ", status: " + he.HttpStatusCode + ", Message: " + he.Message);
        throw he;
      }
      catch (HttpRequestException hre)
      {
        // DME might not exist at all:
        Log.E("Exception during VerifyLocation. DME Server used: " + host + ", Message: " + hre.Message);
        throw hre;
      }

      string responseStr = Util.StreamToString(responseStream);
      Log.D("ResponseStr: " + responseStr);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(VerifyLocationReply), serializerSettings);
      VerifyLocationReply reply = (VerifyLocationReply)deserializer.ReadObject(ms);
      if (reply.tags == null)
      {
        reply.tags = Tag.HashtableToDictionary(reply.htags);
      }

      // Reparse if default value is set.
      reply.tower_status = reply.tower_status == VerifyLocationReply.TowerStatus.TowerUnknown ?
        ParseTowerStatus(responseStr) : reply.tower_status;
      reply.gps_location_status = reply.gps_location_status == VerifyLocationReply.GPSLocationStatus.Unknown ?
        ParseGpsLocationStatus(responseStr) : reply.gps_location_status;

      return reply;
    }

    /*!
     * Creates the AppInstListRequest object that will be used in GetAppInstList
     * \ingroup functions_dmeapis
     * \param loc (Loc): User location
     * \param carrierName (string): Optional device carrier (if not provided, carrier information will be pulled from device)
     * \param cellID (UInt32): Optional cell tower id. If none supplied, default is 0.
     * \param tags (Tag[]): Optional
     * \return AppInstListRequest
     * \section createappinstexample Example
     * \snippet RestSample.cs createappinstexample
     */
    public AppInstListRequest CreateAppInstListRequest(Loc loc, string carrierName = null, UInt32 cellID = 0, Dictionary<string, string> tags = null)
    {
      if (sessionCookie == null)
      {
        return null;
      }

      if (loc == null)
      {
        return null;
      }

      if (carrierName == null) {
        carrierName = GetCarrierName();
      }

      return new AppInstListRequest
      {
        ver = 1,
        session_cookie = this.sessionCookie,
        gps_location = loc,
        carrier_name = carrierName,
        cell_id = cellID,
        tags = tags
      };
    }

    private AppInstListReply.AIStatus ParseAIStatus(string responseStr)
    {
      string key = "status";
      JsonObject jsObj = (JsonObject)JsonValue.Parse(responseStr);
      AppInstListReply.AIStatus status;
      try
      {
        status = (AppInstListReply.AIStatus)Enum.Parse(typeof(AppInstListReply.AIStatus), jsObj[key]);
      }
      catch
      {
        status = AppInstListReply.AIStatus.Undefined;
      }
      return status;
    }

    /*!
     * Returns a list of the developer's backend instances deployed on the specified carrier's network.
     * If carrier was "", returns all backend instances regardless of carrier network.
     * This is used internally in FindCloudlet Performance mode to grab the list of cloudlets to test.
     * \ingroup functions_dmeapis
     * \param request (AppInstListRequest)
     * \return Task<AppInstListReply>
     * \section appinstlistexample Example
     * \snippet RestSample.cs appinstlistexample
     */
    public async Task<AppInstListReply> GetAppInstList(AppInstListRequest request)
    {
      return await GetAppInstList(GenerateDmeHostAddress(), defaultDmeRestPort, request);
    }

    /*!
     * GetAppInstList overload with hardcoded DME host and port. Only use for testing.
     * \ingroup functions_dmeapis
     * \param host (string): DME host
     * \param port(uint): DME port (REST: 38001, GRPC: 50051)
     * \param request (AppInstListRequest)
     * \return Task<AppInstListReply>
     * \section appinstlistoverloadexample Example
     * \snippet RestSample.cs appinstlistoverloadexample
     */
    public async Task<AppInstListReply> GetAppInstList(string host, uint port, AppInstListRequest request)
    {
      request.htags = Tag.DictionaryToHashtable(request.tags);
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppInstListRequest), serializerSettings);
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = null;
      try
      {
        responseStream = await PostRequest(CreateUri(host, port) + appinstlistAPI, jsonStr);
        if (responseStream == null || !responseStream.CanRead)
        {
          Log.E("Unreadable GetAppInstList stream! This should not happen.");
          return null;
        }
      }
      catch (HttpException he)
      {
        Log.E("Exception hit: DME Server host: " + host + ", status: " + he.HttpStatusCode + ", Message: " + he.Message);
        throw he;
      }
      catch (HttpRequestException hre)
      {
        // DME might not exist at all:
        Log.E("Exception during GetAppInstList. DME Server used: " + host + ", Message: " + hre.Message);
        throw hre;
      }

      string responseStr = Util.StreamToString(responseStream);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(AppInstListReply), serializerSettings);
      AppInstListReply reply = (AppInstListReply)deserializer.ReadObject(ms);
      if (reply.tags == null)
      {
        reply.tags = Tag.HashtableToDictionary(reply.htags);
      }

      // reparse if undefined.
      reply.status = reply.status == AppInstListReply.AIStatus.Undefined ? ParseAIStatus(responseStr) : reply.status;

      return reply;
    }

    /*!
     * Creates the QosPositionRequest object that will be used in CreateQosPositionRequest
     * \ingroup functions_dmeapis
     * \param QosPositions (List<QosPosition): List of gps positions
     * \param lteCategory (Int32): Client's device LTE category number
     * \param bandSelection (BandSelection): Band list used by client
     * \param cellID (UInt32): Optional cell tower id. If none supplied, default is 0.
     * \param tags (Tag[]): Optional
     * \return QosPositionRequest
     * \section createqospositionexample Example
     * \snippet RestSample.cs createqospositionexample
     */
    public QosPositionRequest CreateQosPositionRequest(List<QosPosition> QosPositions, Int32 lteCategory, BandSelection bandSelection,
      UInt32 cellID = 0, Dictionary<string, string> tags = null)
    {
      if (sessionCookie == null)
      {
        return null;
      }

      return new QosPositionRequest
      {
        ver = 1,
        positions = QosPositions.ToArray(),
        session_cookie = this.sessionCookie,
        lte_category = lteCategory,
        band_selection = bandSelection,
        cell_id = cellID,
        tags = tags
      };
    }

    /*!
     * Returns quality of service metrics for each location provided in qos position request
     * \ingroup functions_dmeapis
     * \param request (QosPositionRequest)
     * \return Task<QosPositionKpiStream>
     * \section getqospositionexample Example
     * \snippet RestSample.cs getqospositionexample
     */
    public async Task<QosPositionKpiStream> GetQosPositionKpi(QosPositionRequest request)
    {
      return await GetQosPositionKpi(GenerateDmeHostAddress(), defaultDmeRestPort, request);
    }

    /*!
     * GetQosPositionKpi overload with hardcoded DME host and port. Only use for testing.
     * \ingroup functions_dmeapis
     * \param host (string): DME host
     * \param port(uint): DME port (REST: 38001, GRPC: 50051)
     * \param request (QosPositionRequest)
     * \return Task<QosPositionKpiStream>
     * \section getqospositionoverloadexample Example
     * \snippet RestSample.cs getqospositionoverloadexample
     */
    public async Task<QosPositionKpiStream> GetQosPositionKpi(string host, uint port, QosPositionRequest request)
    {
      request.htags = Tag.DictionaryToHashtable(request.tags);
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(QosPositionRequest), serializerSettings);
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + qospositionkpiAPI, jsonStr);
      if (responseStream == null || !responseStream.CanRead || responseStream.Length == 0)
      {
        return null;
      }

      var qosPositionKpiStream = new QosPositionKpiStream(responseStream);

      return qosPositionKpiStream;
    }   

    private FqdnListRequest CreateFqdnListRequest(UInt32 cellID = 0, Dictionary<string, string> tags = null)
    {
      if (sessionCookie == null)
      {
        return null;
      }

      return new FqdnListRequest
      {
        ver = 1,
        session_cookie = this.sessionCookie,
        cell_id = cellID,
        tags = tags
      };
    }

    private FqdnListReply.FLStatus ParseFLStatus(string responseStr)
    {
      string key = "status";
      JsonObject jsObj = (JsonObject)JsonValue.Parse(responseStr);
      FqdnListReply.FLStatus status;
      try
      {
        status = (FqdnListReply.FLStatus)Enum.Parse(typeof(FqdnListReply.FLStatus), jsObj[key]);
      }
      catch
      {
        status = FqdnListReply.FLStatus.Undefined;
      }
      return status;
    }

    private async Task<FqdnListReply> GetFqdnList(FqdnListRequest request)
    {
      return await GetFqdnList(GenerateDmeHostAddress(), defaultDmeRestPort, request);
    }

    private async Task<FqdnListReply> GetFqdnList(string host, uint port, FqdnListRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(FqdnListRequest), serializerSettings);
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream;
      try
      {
        responseStream = await PostRequest(CreateUri(host, port) + getfqdnlistAPI, jsonStr);
        if (responseStream == null || !responseStream.CanRead)
        {
          Log.E("Unreadable GetFqdnList stream! This should not happen.");
          return null;
        }
      }
      catch (HttpException he)
      {
        Log.E("Exception hit: DME Server host: " + host + ", status: " + he.HttpStatusCode + ", Message: " + he.Message);
        throw he;
      }
      catch (HttpRequestException hre)
      {
        // DME might not exist at all:
        Log.E("Exception during GetFqdnList. DME Server used: " + host + ", Message: " + hre.Message);
        throw hre;
      }

      string responseStr = Util.StreamToString(responseStream);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(FqdnListReply), serializerSettings);
      FqdnListReply reply = (FqdnListReply)deserializer.ReadObject(ms);
      if (reply.tags == null)
      {
        reply.tags = Tag.HashtableToDictionary(reply.htags);
      }

      reply.status = reply.status == FqdnListReply.FLStatus.Undefined ? ParseFLStatus(responseStr) : reply.status;

      return reply;
    }

    private DynamicLocGroupRequest CreateDynamicLocGroupRequest(DlgCommType dlgCommType, UInt64 lgId = 0, 
      string userData = null, UInt32 cellID = 0, Dictionary<string, string> tags = null)
    {
      if (sessionCookie == null)
      {
        return null;
      }

      return new DynamicLocGroupRequest
      {
        ver = 1,
        session_cookie = this.sessionCookie,
        comm_type = dlgCommType,
        lg_id = lgId,
        user_data = userData,
        cell_id = cellID,
        tags = tags
      };
    }

    private async Task<DynamicLocGroupReply> AddUserToGroup(DynamicLocGroupRequest request)
    {
      return await AddUserToGroup(GenerateDmeHostAddress(), defaultDmeRestPort, request);
    }

    private async Task<DynamicLocGroupReply> AddUserToGroup(string host, uint port, DynamicLocGroupRequest request)
    {
      request.htags = Tag.DictionaryToHashtable(request.tags);
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DynamicLocGroupRequest), serializerSettings);
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream;
      try
      {
        responseStream = await PostRequest(CreateUri(host, port) + dynamiclocgroupAPI, jsonStr);
        if (responseStream == null || !responseStream.CanRead)
        {
          Log.E("Unreadable AddUserToGroup stream! This should not happen.");
          return null;
        }
      } catch (HttpException he)
      {
        Log.E("Exception hit: DME Server host: " + host + ", status: " + he.HttpStatusCode + ", Message: " + he.Message);
        throw he;
      }
      catch (HttpRequestException hre)
      {
        // DME might not exist at all:
        Log.E("Exception during AddUserToGroup. DME Server used: " + host + ", Message: " + hre.Message);
        throw hre;
      }

      string responseStr = Util.StreamToString(responseStream);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(DynamicLocGroupReply), serializerSettings);
      DynamicLocGroupReply reply = (DynamicLocGroupReply)deserializer.ReadObject(ms);
      if (reply.tags == null)
      {
        reply.tags = Tag.HashtableToDictionary(reply.htags);
      }

      reply.status = reply.status == ReplyStatus.Undefined ? ParseReplyStatus(responseStr) : reply.status;

      return reply;
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          httpClient.Dispose();
        }

        // TODO: free unmanaged resources (unmanaged objects) and override finalizer
        // TODO: set large fields to null
        httpClient = null;
        disposedValue = true;
      }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~MatchingEngine()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
  }
}
