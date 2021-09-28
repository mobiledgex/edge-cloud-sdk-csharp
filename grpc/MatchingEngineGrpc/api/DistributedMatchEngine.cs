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
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

using DistributedMatchEngine.PerformanceMetrics;
using DistributedMatchEngine.Mel;
using System.Net.Sockets;
using Grpc.Core;
using Google.Protobuf.Collections;
using static DistributedMatchEngine.FindCloudletReply.Types;
using static DistributedMatchEngine.AppInstListReply.Types;
using static DistributedMatchEngine.DynamicLocGroupRequest.Types;


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
    public const uint defaultDmeGrpcPort = 50051;
    public const string carrierNameDefault = "wifi";
    public const string wifiCarrier = "wifi";
    public const string wifiOnlyDmeHost = wifiCarrier + "." + baseDmeHost; // Demo mode only.
    public const string baseDmeHost = "dme.mobiledgex.net";

    public uint dmePort { get; set; } = defaultDmeGrpcPort; // GRPC port

    /*!
     * Enable edge features. If enabled, this may cause permission prompts on
     * some target devices due to the MatchingEngine probing the current network
     * state for edge capabilities. Edge features may be degraded if not enabled.
     */
    public static bool EnableEnhancedLocationServices { get; set; } = false;

    public CarrierInfo carrierInfo { get; set; }
    public NetInterface netInterface { get; set; }
    public UniqueID uniqueID { get; set; }
    public DeviceInfoApp deviceInfo { get; private set; }
    private MelMessagingInterface melMessaging { get; set; }

    internal DataContractJsonSerializerSettings serializerSettings = new DataContractJsonSerializerSettings
    {
      UseSimpleDictionaryFormat = true
    };

    public const int DEFAULT_GRPC_TIMEOUT_MS = 10000;
    public TimeSpan GrpcTimeout = TimeSpan.FromMilliseconds(DEFAULT_GETCONNECTION_TIMEOUT_MS);

    public bool useOnlyWifi { get; set; } = false;
    // Use SSL for DME.
    public bool useSSL { get; set; } = true;

    public string sessionCookie { get; set; }
    public string edgeEventsCookie { get; set; }
    string tokenServerURI;
    private bool disposedValue = false;

    string authToken { get; set; }

    // Global local endpoint override for FindCloudlet, NetTest, and GetConnection API helpers.
    // This is used for background App related operations like EdgeEvents processing, if set.
    // Default routing otherwise.
    public string LocalIP { get; set; }

    // For Event Consumers
    public delegate void EdgeEventsDelegate(ServerEdgeEvent serverEdgeEvent);
    public event EdgeEventsDelegate EdgeEventsReceiver;
    public void InvokeEdgeEventsReciever(ServerEdgeEvent serverEdgeEvent)
    {
      Log.D("EdgeEventsReceiver Message: " + serverEdgeEvent);
      EdgeEventsReceiver.Invoke(serverEdgeEvent);
    }

    /*!
     * Set to false if edge events are not needed.
     */
    public bool EnableEdgeEvents { get; set; } = true;
    public EdgeEventsConnection EdgeEventsConnection { get; internal set; }

    public RegisterClientRequest LastRegisterClientRequest { get; private set; }
    public FindCloudletReply LastFindCloudletReply { get; private set; }

    /*!
     * Constructor for MatchingEngine class.
     * \param carrierInfo (CarrierInfo): 
     * \param netInterface (NetInterface): 
     * \param uniqueID (UniqueID):
     * \param deviceInfo (DeviceInfo):
     * \section meconstructorexample Example
     * \snippet RestSample.cs meconstructorexample
     */
    public MatchingEngine(CarrierInfo carrierInfo = null, NetInterface netInterface = null, UniqueID uniqueID = null, DeviceInfoApp deviceInfo = null)
    {
      httpClient = new HttpClient();
      httpClient.Timeout = TimeSpan.FromMilliseconds(DEFAULT_GRPC_TIMEOUT_MS);
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
      EdgeEventsReceiver += (ServerEdgeEvent serverEdgeEvent) =>
      {
        Log.D("MatchingEngine EdgeEvent Notice: " + serverEdgeEvent.EventType);
      };
    }

    /*!
     * GetEdgeEventsConnection
     */
    public EdgeEventsConnection GetEdgeEventsConnection(string edgeEventCookie, string dmeHost = null, uint dmePort = 0)
    {
      if (!EnableEdgeEvents)
      {
        return null;
      }
      if (EdgeEventsConnection == null)
      {
        EdgeEventsConnection = new EdgeEventsConnection(this, dmeHost, dmePort);
      }

      if (edgeEventCookie == null || edgeEventCookie.Trim().Length == 0)
      {
        // Will not init!
        return null;
      }
      return EdgeEventsConnection;
    }

    EdgeEventsConnection GetEdgeEventsConnection()
    {
      return EdgeEventsConnection;
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
      return httpClient.Timeout = TimeSpan.FromMilliseconds(DEFAULT_GRPC_TIMEOUT_MS);
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
    * GetDeviceInfoStatic
    * \ingroup functions_dmeutils
    */
    public DeviceInfoStatic GetDeviceInfoStatic()
    {
      return deviceInfo.GetDeviceInfoStatic();
    }

    /*!
    * GetDeviceInfoDynamic
    * \ingroup functions_dmeutils
    */
    public DeviceInfoDynamic GetDeviceInfoDynamic()
    {
      if (useOnlyWifi)
      {
        DeviceInfoDynamic deviceInfoDynamic = new DeviceInfoDynamic
        {
          CarrierName = ""
        };
        return deviceInfoDynamic;
      }
      return deviceInfo.GetDeviceInfoDynamic();
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

    internal void CopyTagField(MapField<string, string> dest, Dictionary<string, string> src)
    {
      if (dest == null)
      {
        return;
      }

      if (src == null)
      {
        return;
      }

      foreach (var entry in src)
      {
        if (entry.Value != null)
        {
          dest.Add(entry.Key, entry.Value);
        }
      }
    }

    internal void CopyTagField(MapField<string, string> dest, MapField<string, string> src)
    {
      if (dest == null)
      {
        return;
      }

      if (src == null)
      {
        return;
      }

      foreach (var entry in src)
      {
        if (entry.Value != null)
        {
          dest.Add(entry.Key, entry.Value);
        }
      }
    }

    internal Channel ChannelPicker(string host, uint port)
    {
      Channel channel;

      if (!useSSL)
      {
        Log.E("Creating an INSECURE GRPC channel: " + "Host: " + host + ", Port: " + port);
        channel = new Channel(host, (int)port, ChannelCredentials.Insecure);
      }
      else
      {
        channel = new Channel(host, (int)port, new SslCredentials());
      }

      return channel;
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
    public RegisterClientRequest CreateRegisterClientRequest(string orgName, string appName, string appVersion, string authToken = "",
      UInt32 cellID = 0, string uniqueIDType = "", string uniqueID = "", Dictionary<string, string> tags = null)
    {
      var request = new RegisterClientRequest
      {
        Ver = 1,
        OrgName = orgName,
        AppName = appName,
        AppVers = appVersion,
        CellId = cellID
      };

      if (authToken != null && !authToken.Trim().Equals(""))
      {
        request.AuthToken = authToken;
      }
      if (uniqueID != null && !uniqueID.Trim().Equals(""))
      {
        request.UniqueId = uniqueID;
      }
      if (uniqueIDType != null && !uniqueIDType.Trim().Equals(""))
      {
        request.UniqueIdType = uniqueIDType;
      }

      CopyTagField(request.Tags, tags);

      return request;
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
      return await RegisterClient(GenerateDmeHostAddress(), dmePort, request);
    }

    private RegisterClientRequest UpdateRequestForUniqueID(RegisterClientRequest request)
    {
      string uid = melMessaging.GetUid();
      string aUniqueIdType = GetUniqueIDType(); // Read: device model
      string aUniqueId = GetUniqueID();
      string manufacturer = melMessaging.GetManufacturer();

      if (uid != null && uid != "")
      {
        request.UniqueIdType = "Platos:" + aUniqueIdType + ":PlatosEnablingLayer";
        request.UniqueId = melMessaging.GetUid();
      }
      else if (manufacturer != null &&
        aUniqueIdType != null && aUniqueIdType.Length > 0 &&
        aUniqueId != null && aUniqueId.Length > 0)
      {
        request.UniqueIdType = manufacturer + ":" + aUniqueIdType + ":HASHED_ID";
        request.UniqueId = aUniqueId;
      }

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
      // Whether or not MEL is enabled, if UID is there, include it for App registration.
      request = new RegisterClientRequest()
      {
        Ver = oldRequest.Ver,
        CarrierName = oldRequest.CarrierName,
        OrgName = oldRequest.OrgName,
        AppName = oldRequest.AppName,
        AppVers = oldRequest.AppVers,
        AuthToken = oldRequest.AuthToken,
        CellId = oldRequest.CellId
      };
      CopyTagField(oldRequest.Tags, request.Tags);

      // MEL Enablement:
      request = UpdateRequestForUniqueID(request);

      // One time use Channel:
      Channel channel = ChannelPicker(host, port);

      try
      {
        var client = new MatchEngineApi.MatchEngineApiClient(channel);

        var call = client.RegisterClientAsync(
          request,
          new CallOptions()
            .WithDeadline(DateTime.UtcNow.AddMilliseconds(GrpcTimeout.TotalMilliseconds))
        );
        var responseTask = call.ResponseAsync.ConfigureAwait(false);
        var reply = await responseTask;

        this.sessionCookie = reply.SessionCookie;
        this.tokenServerURI = reply.TokenServerUri;

        if (reply.Status == ReplyStatus.RsSuccess)
        {
          LastRegisterClientRequest = request; // Update last request.
        }
        return reply;
      }
      catch (RpcException e)
      {
        Log.E("Exception during RegisterClient. DME Server used: " + host + ", carrierName: " + request.CarrierName + ", appName: " + request.AppName + ", appVersion: " + request.AppVers + ", organizationName: " + request.OrgName + ", Message: " + e.Message);
        string msg = e.Message;
        if (msg == null || msg.Contains("NotFound"))
        {
          Log.E("Please check that the appName, appVersion, and orgName correspond to a valid app definition on MobiledgeX.");
        }
        throw e;
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

      if (carrierName == null)
      {
        carrierName = GetCarrierName();
      }

      var request = new FindCloudletRequest
      {
        SessionCookie = this.sessionCookie,
        GpsLocation = loc,
        CarrierName = carrierName,
        CellId = cellID,
      };
      if (tags != null)
      {
        foreach (var entry in tags)
        {
          request.Tags.Add(entry.Key, entry.Value);
        }
      }
      return request;
    }

    /*!
     * FindCloudlet returns information needed for the client app to connect to an application backend deployed through MobiledgeX.
     * If there is an application backend instance found, FindCloudetReply will contain the fqdn of the application backend and an array of AppPorts (with information specific to each application backend endpoint)
     * \ingroup functions_dmeapis
     * \param request (FindCloudletRequest)
     * \param mode (FindCloudletMode): Optional. Default is PROXIMITY. PROXIMITY will just return the findCloudletReply sent by DME (Generic REST API to findcloudlet endpoint). PERFORMANCE will test all app insts deployed on the specified carrier network and return the cloudlet with the lowest latency (Note: PERFORMANCE may take some time to return). Default value if mode parameter is not supplied is PROXIMITY.
     * \param localEndpoint: Optional. Specifiy a local interface IPEndPoint for performance mode.
     * \return Task<FindCloudletReply>
     * \section findcloudletexample Example
     * \subsection findcloudletproximityexample Proximity Example
     * \snippet RestSample.cs findcloudletexample
     * \subsection findcloudletperformanceexample Performance Example
     * \snippet RestSample.cs findcloudletperformanceexample
     */
    public async Task<FindCloudletReply> FindCloudlet(FindCloudletRequest request, FindCloudletMode mode = FindCloudletMode.PROXIMITY, IPEndPoint localEndPoint = null)
    {
      return await FindCloudlet(GenerateDmeHostAddress(), dmePort, request, mode, localEndPoint);
    }

    private async Task<FindCloudletReply> FindCloudletMelMode(string host, uint port, FindCloudletRequest request)
    {
      AppOfficialFqdnRequest appOfficialFqdnRequest = new AppOfficialFqdnRequest
      {
        Ver = 1,
        SessionCookie = request.SessionCookie,
        GpsLocation = request.GpsLocation
      };
      CopyTagField(appOfficialFqdnRequest.Tags, request.Tags);

      // One time use Channel:
      Channel channel = ChannelPicker(host, port);

      var client = new MatchEngineApi.MatchEngineApiClient(channel);

      var call = client.GetAppOfficialFqdnAsync(
        appOfficialFqdnRequest,
        new CallOptions()
          .WithDeadline(DateTime.UtcNow.AddMilliseconds(GrpcTimeout.TotalMilliseconds))
      );
      var responseTask = call.ResponseAsync.ConfigureAwait(false);
      var appOfficialFqdnReply = await responseTask;

      // Inform Mel Messaging:
      if (melMessaging.IsMelEnabled() && LastRegisterClientRequest != null)
      {
        melMessaging.SetToken(appOfficialFqdnReply.ClientToken, LastRegisterClientRequest.AppName);
      }

      if (appOfficialFqdnReply.Ports != null && appOfficialFqdnReply.Ports.Count > 0)
      {
        foreach (AppPort aPort in appOfficialFqdnReply.Ports)
        {
          aPort.PublicPort = aPort.PublicPort == 0 ? aPort.InternalPort : aPort.PublicPort;
        }
      }
      else
      {
        // attach empty 0 port, indicating app must determine it's own public port.
        appOfficialFqdnReply.Ports.Add(new AppPort[1]);
      }

      // Repackage as FindCloudletReply:
      FindCloudletReply fcReply = new FindCloudletReply
      {
        Ver = 1,
        Fqdn = appOfficialFqdnReply.AppOfficialFqdn,
        // Don't set location.
      };
      foreach(var aPort in appOfficialFqdnReply.Ports)
      {
        fcReply.Ports.Add(aPort);
      }

      return fcReply;
    }

    private async Task<FindCloudletReply> FindCloudletProximityMode(string host, uint port, FindCloudletRequest request)
    {
      // One time use Channel:
      Channel channel = ChannelPicker(host, port);

      var client = new MatchEngineApi.MatchEngineApiClient(channel);

      var call = client.FindCloudletAsync(
        request,
        new CallOptions()
          .WithDeadline(DateTime.UtcNow.AddMilliseconds(GrpcTimeout.TotalMilliseconds))
      );

      var findCloudletReply = await call.ResponseAsync.ConfigureAwait(false);

      return findCloudletReply;
    }

    // Helper function for FindCloudlet
    private FindCloudletReply CreateFindCloudletReplyFromBestSite(FindCloudletReply reply, NetTest.Site site)
    {
      Appinstance appinstance = site.appInst;

      var fcReply = new FindCloudletReply
      {
        Ver = reply.Ver,
        Status = reply.Status,
        Fqdn = appinstance.Fqdn,
        CloudletLocation = site.cloudletLocation,
        EdgeEventsCookie = appinstance.EdgeEventsCookie
      };

      if (appinstance.Ports != null)
      {
        fcReply.Ports.AddRange(appinstance.Ports);
      }
      CopyTagField(fcReply.Tags, reply.Tags);
      return fcReply;
    }

    private NetTest.Site InitHttpSite(AppPort appPort, Appinstance appinstance, Loc cloudletLocation = null, int numSamples = NetTest.Site.DEFAULT_NUM_SAMPLES, IPEndPoint localEndPoint = null)
    {
      NetTest.Site site = new NetTest.Site(numSamples: numSamples);

      int port = appPort.PublicPort;
      string host = appPort.FqdnPrefix + appinstance.Fqdn;

      site.L7Path = host + ":" + port;
      site.appInst = appinstance;
      site.cloudletLocation = cloudletLocation;
      site.localEndPoint = localEndPoint;
      return site;
    }

    private NetTest.Site InitTcpSite(AppPort appPort, Appinstance appinstance, Loc cloudletLocation = null, int numSamples = NetTest.Site.DEFAULT_NUM_SAMPLES, IPEndPoint localEndPoint = null)
    {
      NetTest.Site site = new NetTest.Site(numSamples: numSamples);

      site.host = appPort.FqdnPrefix + appinstance.Fqdn;
      site.port = appPort.PublicPort;

      site.appInst = appinstance;
      site.cloudletLocation = cloudletLocation;
      site.localEndPoint = localEndPoint;
      return site;
    }

    private NetTest.Site InitUdpSite(AppPort appPort, Appinstance appinstance, Loc cloudletLocation = null, int numSamples = NetTest.Site.DEFAULT_NUM_SAMPLES, IPEndPoint localEndPoint = null)
    {
      NetTest.Site site = new NetTest.Site(testType: NetTest.TestType.PING, numSamples: numSamples);

      site.host = appPort.FqdnPrefix + appinstance.Fqdn;
      site.port = appPort.PublicPort;

      site.appInst = appinstance;
      site.cloudletLocation = cloudletLocation;
      site.localEndPoint = localEndPoint;
      return site;
    }

    // Helper function for FindCloudlet
    private NetTest.Site[] CreateSitesFromAppInstReply(AppInstListReply reply, int testPort = 0, int numSamples = NetTest.Site.DEFAULT_NUM_SAMPLES, IPEndPoint localEndPoint = null)
    {
      List<NetTest.Site> sites = new List<NetTest.Site>();

      foreach (CloudletLocation cloudlet in reply.Cloudlets)
      {
        foreach (Appinstance appinstance in cloudlet.Appinstances)
        {
          if (!deviceInfo.IsPingSupported())
          {
            AppPort tcpPort = null;
            foreach (AppPort appPort in appinstance.Ports)
            {
              if (appPort.Proto == LProto.Tcp)
              {
                tcpPort = appPort;
                break;
              }
            }
            if (tcpPort == null)
            {
              throw new FindCloudletException("FindCloudlet Performance is not supported, Your device doesn't support Ping and your Application Instance doesn't have any TCP Ports.");
            }
            sites.Add(InitTcpSite(tcpPort, appinstance, cloudletLocation: cloudlet.GpsLocation, numSamples: numSamples, localEndPoint: localEndPoint));
          }
          else
          {
            // Find an test AppPort to add from cloudlet.
            AppPort useAppPort = null;
            if (testPort != 0)
            {
              foreach (var aPort in appinstance.Ports)
              {
                if (IsInPortRange(aPort, testPort))
                {
                  useAppPort = aPort;
                  sites.Add(InitTcpSite(useAppPort, appinstance, cloudletLocation: cloudlet.GpsLocation, numSamples: numSamples, localEndPoint: localEndPoint));
                }
              }
            }
            else
            {
              // Many servers block ICMP packets, including AppInsts/Cloudlets. EdgeEventsConfig should specify the TCP port in the application config for testing any particular App.
              foreach (var port in appinstance.Ports)
              {
                if (port.Proto == LProto.Udp)
                {
                  if (useAppPort == null)
                  {
                    useAppPort = port;
                  }
                }
                else if (port.Proto == LProto.Tcp)
                {
                  useAppPort = port;
                  break;
                }
              }

              if (useAppPort.Proto == LProto.Udp)
              {
                Log.E("Warning: Found only UDP port. ICMP Ping testing will likely fail. Please specify a TCP port in your app.");
              }

              switch (useAppPort.Proto)
              {
                case LProto.Tcp:
                  sites.Add(InitTcpSite(useAppPort, appinstance, cloudletLocation: cloudlet.GpsLocation, numSamples: numSamples, localEndPoint: localEndPoint));
                  break;

                case LProto.Udp:
                  sites.Add(InitUdpSite(useAppPort, appinstance, cloudletLocation: cloudlet.GpsLocation, numSamples: numSamples, localEndPoint: localEndPoint));
                  break;

                default:
                  Log.E("Unsupported protocol " + useAppPort.Proto + " found when trying to create sites for NetTest");
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
        if (melMessaging != null && melMessaging.IsMelEnabled() &&
            netInterface.GetIPAddress(
              GetAvailableWiFiName(netInterface.GetNetworkInterfaceName())) == null)
        {
          FindCloudletReply melModeFindCloudletReply = await FindCloudletMelMode(host, port, request).ConfigureAwait(false);
          if (melModeFindCloudletReply == null)
          {
            return new FindCloudletReply { Status = FindStatus.FindNotfound }; ;
          }
          if (melModeFindCloudletReply.Status == FindStatus.FindFound)
          {
            edgeEventsCookie = melModeFindCloudletReply.EdgeEventsCookie;
            EdgeEventsConnection = GetEdgeEventsConnection(melModeFindCloudletReply.EdgeEventsCookie, host, port);
          }
          string appOfficialFqdn = melModeFindCloudletReply.Fqdn;

          IPHostEntry ipHostEntry;
          Stopwatch stopwatch = Stopwatch.StartNew();
          while (stopwatch.ElapsedMilliseconds < httpClient.Timeout.TotalMilliseconds)
          {
            if (appOfficialFqdn == null || appOfficialFqdn.Length == 0)
            {
              break;
            }

            try
            {
              ipHostEntry = Dns.GetHostEntry(appOfficialFqdn);
              if (ipHostEntry.AddressList.Length > 0)
              {
                Log.D("Public AppOfficialFqdn DNS resolved. First entry: " + ipHostEntry.HostName);
                LastFindCloudletReply = melModeFindCloudletReply;
                return melModeFindCloudletReply;
              }
            }
            catch (ArgumentException ae)
            {
              Log.D("ArgumentException. Waiting for update: " + ae.Message);
            }
            catch (SocketException se)
            {
              Log.D("SocketException. Waiting for update: " + se.Message);
            }
            Task.Delay(300).Wait(); // Let the system process SET_TOKEN.
          }

          // Else, don't return, continue to fallback to original MODE:
          Log.E("Public AppOfficialFqdn DNS resolve FAILURE for: " + appOfficialFqdn);
        }

        FindCloudletReply fcReply = null;
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
          Log.E("FindCloudlet error. Did not get any result using DME Server: " + host);
          return new FindCloudletReply { Status = FindStatus.FindNotfound };
        }

        if (fcReply.Status == FindStatus.FindFound)
        {
          edgeEventsCookie = fcReply.EdgeEventsCookie;
          EdgeEventsConnection = GetEdgeEventsConnection(fcReply.EdgeEventsCookie, host, port);
        }
        LastFindCloudletReply = fcReply;
        return fcReply;
      }
      catch (RpcException e)
      {
        Log.E("FindCloudlet exception using DME Server: " + host + ", status: " + e.StatusCode + ", Message: " + e.Message);
        throw e;
      }
    }

    /// <summary>
    /// FindCloudlet based on performance (latency)
    /// </summary>
    /// <param name="host">Distributed Matching Engine Host</param>
    /// <param name="port">Distributed Matching Engine Port</param>
    /// <param name="request">FindCloudletRequest</param>
    /// <param name="testPort">A TCP Test port (Optional)</param>
    /// <param name="numOfSamples">Number of rounds of testing per cloudlet lcoations (Optional)</param>
    /// <param name="localEndPoint">local IPEndPoint to test from (Optional)</param>
    /// <returns>FindCloudletReply</returns>
    public async Task<FindCloudletReply> FindCloudletPerformanceMode(string host, uint port, FindCloudletRequest request, int testPort = 0, int numOfSamples = 5, IPEndPoint localEndPoint = null)
    {

      FindCloudletReply fcReply = await FindCloudletProximityMode(host, port, request);
      if (fcReply.Status != FindStatus.FindFound)
      {
        return fcReply;
      }

      // Dummy bytes to load cellular network path
      Byte[] bytes = new Byte[2048];
      Dictionary<string, string> tags = new Dictionary<string, string>();
      tags["Buffer"] = bytes.ToString();

      AppInstListRequest appInstListRequest = CreateAppInstListRequest(request.GpsLocation, request.CarrierName, tags: tags);
      AppInstListReply aiReply = await GetAppInstList(host, port, appInstListRequest);

      if (aiReply.Status != AIStatus.AiSuccess)
      {
        throw new FindCloudletException("Unable to GetAppInstList. GetAppInstList status is " + aiReply.Status);
      }

      if(aiReply.Cloudlets.Count == 0)
      {
        Log.E("Check FindCloudletPerformance Request parameters. Empty cloudlet list returned from GetAppInstList.");
        return new FindCloudletReply { Status = FindStatus.FindNotfound };
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
        sites = await netTest.RunNetTest(numOfSamples);
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
      return await RegisterAndFindCloudlet(GenerateDmeHostAddress(), dmePort,
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
       string orgName, string appName, string appVersion, Loc loc, string carrierName = "", string authToken = "", 
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
      RegisterClientReply registerClientReply = await RegisterClient(host, port, registerRequest)
        .ConfigureAwait(false);

      if (registerClientReply == null || registerClientReply.Status != ReplyStatus.RsSuccess)
      {
        var status = registerClientReply == null ? "null." : registerClientReply.Status.ToString();
        Log.E("RegisterClient did not succeed: DME Server: " + host + ", status: " + status);
        throw new RegisterClientException("RegisterClientReply status is: " + status);
      }
      // Find Cloudlet 
      FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(
        loc: loc,
        carrierName: carrierName,
        cellID: cellID,
        tags: tags);
      FindCloudletReply findCloudletReply = await FindCloudlet(host, port, findCloudletRequest, mode)
        .ConfigureAwait(false);

      if (findCloudletReply == null || findCloudletReply.Status != FindStatus.FindFound)
      {
        var status = findCloudletReply == null ? "null." : findCloudletReply.Status.ToString();
        Log.E("FindCloudlet did not succeed: DME Server: " + host + ", status: " + status);
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

      var request = new VerifyLocationRequest
      {
        Ver = 1,
        SessionCookie = this.sessionCookie,
        GpsLocation = loc,
        CarrierName = carrierName,
        CellId = cellID,
      };

      CopyTagField(request.Tags, tags);

      return request;
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
      return await VerifyLocation(GenerateDmeHostAddress(), dmePort, request);
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
      try
      {
        string token = RetrieveToken(tokenServerURI);
        request.VerifyLocToken = token;

        // One time use Channel:
        Channel channel = ChannelPicker(host, port);

        var client = new MatchEngineApi.MatchEngineApiClient(channel);

        var call = client.VerifyLocationAsync(
          request,
          new CallOptions()
            .WithDeadline(DateTime.UtcNow.AddMilliseconds(GrpcTimeout.TotalMilliseconds))
        );
        var responseTask = call.ResponseAsync.ConfigureAwait(false);
        var reply = await responseTask;
        return reply;
      }
      catch (RpcException e)
      {
        Log.E("VerifyLocation exception using DME Server: " + host + ", status: " + e.StatusCode + ", Message: " + e.Message);
        throw e;
      }
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

      var request = new AppInstListRequest
      {
        Ver = 1,
        SessionCookie = this.sessionCookie,
        GpsLocation = loc,
        CarrierName = carrierName,
        CellId = cellID
      };
      CopyTagField(request.Tags, tags);

      return request;
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
      return await GetAppInstList(GenerateDmeHostAddress(), dmePort, request);
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
      try
      {
        // One time use Channel:
        Channel channel = ChannelPicker(host, port);

        var client = new MatchEngineApi.MatchEngineApiClient(channel);

        var call = client.GetAppInstListAsync(
          request,
          new CallOptions()
            .WithDeadline(DateTime.UtcNow.AddMilliseconds(GrpcTimeout.TotalMilliseconds))
        );
        var responseTask = call.ResponseAsync.ConfigureAwait(false);
        var reply = await responseTask;

        return reply;
      }
      catch (RpcException e)
      {
        Log.E("GetAppInstList exception using DME Server: " + host + ", status: " + e.StatusCode + ", Message: " + e.Message);
        throw e;
      }
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

      var request = new QosPositionRequest
      {
        Ver = 1,
        SessionCookie = this.sessionCookie,
        LteCategory = lteCategory,
        BandSelection = bandSelection,
        CellId = cellID
      };

      request.Positions.AddRange(QosPositions);
      CopyTagField(request.Tags, tags);

      return request;
    }

    /*!
     * Returns quality of service metrics for each location provided in qos position request
     * \ingroup functions_dmeapis
     * \param request (QosPositionRequest)
     * \return Task<QosPositionKpiStream>
     * \section getqospositionexample Example
     * \snippet RestSample.cs getqospositionexample
     *
     * Here's how to use it (and the channel needs to survive to read from the stream)
     *
     * var objectStream = qosReply.ResponseStream;
     * while (await qosReply.MoveNext())
     * {
     *   foreach (var result in objectStream.Current.PositionResults)
     *   {
     *     Log.D("Latency Result: " + result.ToString());
     *   }
     * }
     */
    public AsyncServerStreamingCall<QosPositionKpiReply> GetQosPositionKpi(QosPositionRequest request)
    {
      var qosReply = GetQosPositionKpi(GenerateDmeHostAddress(), defaultDmeGrpcPort, request);

      return qosReply;
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
    public AsyncServerStreamingCall<QosPositionKpiReply> GetQosPositionKpi(string host, uint port, QosPositionRequest request)
    {
      try
      {
        // One time use Channel:
        Channel channel = ChannelPicker(host, port);

        var client = new MatchEngineApi.MatchEngineApiClient(channel);

        var reply = client.GetQosPositionKpi(
          request,
          new CallOptions()
            .WithDeadline(DateTime.UtcNow.AddMilliseconds(GrpcTimeout.TotalMilliseconds))
        );

        return reply;
      }
      catch (RpcException e)
      {
        Log.E("GetQosPositionKpi exception using DME Server: " + host + ", status: " + e.StatusCode + ", Message: " + e.Message);
        throw e;
      }
    }   

    private FqdnListRequest CreateFqdnListRequest(UInt32 cellID = 0, Dictionary<string, string> tags = null)
    {
      if (sessionCookie == null)
      {
        return null;
      }

      var request = new FqdnListRequest
      {
        Ver = 1,
        SessionCookie = this.sessionCookie,
        CellId = cellID
      };
      CopyTagField(request.Tags, tags);

      return request;

    }

    private async Task<FqdnListReply> GetFqdnList(FqdnListRequest request)
    {
      return await GetFqdnList(GenerateDmeHostAddress(), defaultDmeGrpcPort, request);
    }

    private async Task<FqdnListReply> GetFqdnList(string host, uint port, FqdnListRequest request)
    {
      try
      {
        // One time use Channel:
        Channel channel = ChannelPicker(host, port);

        var client = new MatchEngineApi.MatchEngineApiClient(channel);

        var call = client.GetFqdnListAsync(
          request,
          new CallOptions()
            .WithDeadline(DateTime.UtcNow.AddMilliseconds(GrpcTimeout.TotalMilliseconds))
        );
        var responseTask = call.ResponseAsync.ConfigureAwait(false);
        var reply = await responseTask;

        return reply;
      }
      catch (RpcException e)
      {
        Log.E("GetFqdnList exception using DME Server: " + host + ", status: " + e.StatusCode + ", Message: " + e.Message);
        throw e;
      }
    }

    private DynamicLocGroupRequest CreateDynamicLocGroupRequest(DlgCommType dlgCommType, UInt64 lgId = 0, 
      string userData = null, UInt32 cellID = 0, Dictionary<string, string> tags = null)
    {
      if (sessionCookie == null)
      {
        return null;
      }

      var request = new DynamicLocGroupRequest
      {
        Ver = 1,
        SessionCookie = this.sessionCookie,
        CommType = dlgCommType,
        LgId = lgId,
        UserData = userData,
        CellId = cellID
      };

      CopyTagField(request.Tags, tags);
      return request;
    }

    private async Task<DynamicLocGroupReply> AddUserToGroup(DynamicLocGroupRequest request)
    {
      return await AddUserToGroup(GenerateDmeHostAddress(), defaultDmeGrpcPort, request);
    }

    private async Task<DynamicLocGroupReply> AddUserToGroup(string host, uint port, DynamicLocGroupRequest request)
    {
      try
      {
        // One time use Channel:
        Channel channel = ChannelPicker(host, port);

        var client = new MatchEngineApi.MatchEngineApiClient(channel);

        var call = client.AddUserToGroupAsync(
          request,
          new CallOptions()
            .WithDeadline(DateTime.UtcNow.AddMilliseconds(GrpcTimeout.TotalMilliseconds))
        );
        var responseTask = call.ResponseAsync.ConfigureAwait(false);
        var reply = await responseTask;

        return reply;
      }
      catch (RpcException e)
      {
        Log.E("AddUserToGroup exception using DME Server: " + host + ", status: " + e.StatusCode + ", Message: " + e.Message);
        throw e;
      }
    }

    // Compiler generated.
    protected virtual void Dispose(bool disposing)
    {
      if (disposedValue)
      {
        return;
      }

      if (disposing)
      {
        // dispose managed state (managed objects)
        if (EdgeEventsConnection != null && !EdgeEventsConnection.IsShutdown())
        {
          EdgeEventsConnection.Close();
        }
        EdgeEventsReceiver = null;

      }

      // free unmanaged resources (unmanaged objects) and override finalizer
      // set large fields to null
      if (httpClient != null)
      {
        httpClient.Dispose();
      }
      httpClient = null;
      EdgeEventsConnection = null;
      authToken = null;
      sessionCookie = null;
      LastFindCloudletReply = null;
      LastRegisterClientRequest = null;
      disposedValue = true;
    }

    // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    ~MatchingEngine()
    {
       // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
       Dispose(disposing: false);
    }

    public void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
  };

}
