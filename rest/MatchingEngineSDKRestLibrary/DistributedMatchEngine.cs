/**
 * Copyright 2019 MobiledgeX, Inc. All rights and licenses reserved.
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Json;
using System.Text;
using System.Collections.Generic;

using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;

namespace DistributedMatchEngine
{
  public class DmeDnsException: Exception
  {
    public DmeDnsException(string message)
        : base(message)
    {
    }
  }
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

  class EmptyCarrierInfo: CarrierInfo
  {
    public string GetCurrentCarrierName()
    {
      return null;
    }

    public string GetMccMnc()
    {
      return null;
    }
  }

  public enum OperatingSystem
  {
     ANDROID,
     IOS,
     OTHER
  }


  public partial class MatchingEngine
  {
    public const string TAG = "MatchingEngine";
    private static HttpClient httpClient;
    public const UInt32 defaultDmeRestPort = 38001;
    public const string carrierNameDefault = "gddt";
    public const string fallbackDmeHost = "sdkdemo.dme.mobiledgex.net";
    public const string baseDmeHost = "dme.mobiledgex.net";

    UInt32 dmePort { get; set; } = defaultDmeRestPort; // HTTP REST port

    public CarrierInfo carrierInfo { get; set; }
    public NetInterface netInterface { get; set; }

    // API Paths:
    private string registerAPI = "/v1/registerclient";
    private string verifylocationAPI = "/v1/verifylocation";
    private string findcloudletAPI = "/v1/findcloudlet";
    private string getlocationAPI = "/v1/getlocation";
    private string appinstlistAPI = "/v1/getappinstlist";
    private string dynamiclocgroupAPI = "/v1/dynamiclocgroup";
    private string getfqdnlistAPI = "/v1/getfqdnlist";
    private string qospositionkpiAPI = "/v1/getqospositionkpi";

    public const int DEFAULT_REST_TIMEOUT_MS = 10000;
    public const long TICKS_PER_MS = 10000;

    public string sessionCookie { get; set; }
    string tokenServerURI;
    string authToken { get; set; }
    public OperatingSystem os { get; set; }

    public MatchingEngine(OperatingSystem os)
    {
      this.os = os;
      httpClient = new HttpClient();
      httpClient.Timeout = TimeSpan.FromTicks(DEFAULT_REST_TIMEOUT_MS * TICKS_PER_MS);
      carrierInfo = new EmptyCarrierInfo();
      netInterface = new EmptyNetInterface();
    }

    // Set the REST timeout for DME APIs.
    public TimeSpan SetTimeout(int timeout_in_milliseconds)
    {
      if (timeout_in_milliseconds > 1)
      {
        return httpClient.Timeout = TimeSpan.FromTicks(timeout_in_milliseconds * TICKS_PER_MS);
      }
      return httpClient.Timeout = TimeSpan.FromTicks(DEFAULT_REST_TIMEOUT_MS * TICKS_PER_MS);
    }

    public string GetCarrierName()
    {
      return carrierInfo.GetCurrentCarrierName();
    }

    public string GenerateDmeHostName()
    {
      if (carrierInfo == null)
      {
        throw new InvalidCarrierInfoException("Missing platform integration interface.");
      }

      string mccmnc = carrierInfo.GetMccMnc();
      if (mccmnc == null)
      {
        Log.E("PlatformIntegration CarrierInfo interface does not have a valid MCCMNC string.");
        throw new DmeDnsException("Cannot generate DME hostname, mccmnc is empty");
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

      // This host might not actually exist (yet):
      IPHostEntry ipHostEntry = Dns.GetHostEntry(potentialDmeHost);
      if (ipHostEntry.AddressList.Length > 0)
      {
        return potentialDmeHost;
      }

      // Let the caller handle an unsupported DME configuration.
      throw new DmeDnsException("Generated mcc-mnc." + baseDmeHost + " hostname not found: " + potentialDmeHost);
    }

    private string CreateUri(string host, uint port)
    {
      return "https://" + host + ":" + port;
    }

    private async Task<Stream> PostRequest(string uri, string jsonStr)
    {
        // Choose network TBD
        Log.D("URI: " + uri);
        // static HTTPClient singleton, with instanced HttpContent is recommended for performance.
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
        Log.D("uriLocation: " + uriLocation);
        token = ParseToken(uriLocation);
      }

      if (token == null)
      {
        throw new InvalidTokenServerTokenException("Token not found or parsable in the URI string: " + uriLocation);
      }

      return token;
    }

    public RegisterClientRequest CreateRegisterClientRequest(string carrierName, string developerName, string appName, string appVersion, string authToken)
    {
      return new RegisterClientRequest
      {
        ver = 1,
        carrier_name = carrierName,
        dev_name = developerName,
        app_name = appName,
        app_vers = appVersion,
        auth_token = authToken
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
        replyStatus = ReplyStatus.RS_UNDEFINED;
      }
      return replyStatus;
    }

    public async Task<RegisterClientReply> RegisterClient(RegisterClientRequest request)
    {
      return await RegisterClient(GenerateDmeHostName(), defaultDmeRestPort, request);
    }

    public async Task<RegisterClientReply> RegisterClient(string host, uint port, RegisterClientRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(RegisterClientRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + registerAPI, jsonStr).ConfigureAwait(false);
      if (responseStream == null || !responseStream.CanRead)
      {
        return null;
      }

      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(RegisterClientReply));
      string responseStr = Util.StreamToString(responseStream);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      RegisterClientReply reply = (RegisterClientReply)deserializer.ReadObject(ms);

      this.sessionCookie = reply.session_cookie;
      this.tokenServerURI = reply.token_server_uri;

      // Some platforms won't parse emums with same library binary.
      reply.status = reply.status == ReplyStatus.RS_UNDEFINED ? ParseReplyStatus(responseStr) : reply.status;

      return reply;
    }

    public FindCloudletRequest CreateFindCloudletRequest(string carrierName, string devName, string appName, string appVers, Loc loc)
    {
      if (sessionCookie == null)
      {
        // Exceptions.
        return null;
      }
      return new FindCloudletRequest
      {
        session_cookie = this.sessionCookie,
        carrier_name = carrierName,
        dev_name = devName,
        app_name = appName,
        app_vers = appVers,
        gps_location = loc
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
        status = FindCloudletReply.FindStatus.FIND_UNKNOWN;
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
            appports[i].proto = LProto.L_PROTO_UNKNOWN;
          }
        }
      }
      return appports;
    }

    public async Task<FindCloudletReply> FindCloudlet(FindCloudletRequest request)
    {
      return await FindCloudlet(GenerateDmeHostName(), defaultDmeRestPort, request);
    }

    public async Task<FindCloudletReply> FindCloudlet(string host, uint port, FindCloudletRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(FindCloudletRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + findcloudletAPI, jsonStr).ConfigureAwait(false);
      if (responseStream == null || !responseStream.CanRead)
      {
        return null;
      }

      string responseStr = Util.StreamToString(responseStream);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(FindCloudletReply));
      FindCloudletReply reply = (FindCloudletReply)deserializer.ReadObject(ms);

      // Reparse if default value.
      reply.status = reply.status == FindCloudletReply.FindStatus.FIND_UNKNOWN ? ParseFindStatus(responseStr) : reply.status;

      // Port has an emum to reparse if set to default as well:
      ParseAppPortTypes(reply.ports, responseStr);

      return reply;
    }

    // Wrapper function for RegisterClient and FindCloudlet
    public async Task<FindCloudletReply> RegisterAndFindCloudlet(string carrierName, string developerName, string appName, string appVersion, string authToken, Loc loc)
    {
        // Register Client
        RegisterClientRequest registerRequest = CreateRegisterClientRequest(carrierName, developerName, appName, appVersion, authToken);
        RegisterClientReply registerClientReply = await RegisterClient(registerRequest);
        if (registerClientReply.status != ReplyStatus.RS_SUCCESS)
        {
            throw new RegisterClientException("RegisterClientReply status is " + registerClientReply.status);
        }
        // Find Cloudlet
        FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(carrierName, developerName, appName, appVersion, loc);
        FindCloudletReply findCloudletReply = await FindCloudlet(findCloudletRequest);

        return findCloudletReply;
    }

    // Override with specified dme host and port
    public async Task<FindCloudletReply> RegisterAndFindCloudlet(string host, uint port, string carrierName, string developerName, string appName, string appVersion, string authToken, Loc loc)
    {
        // Register Client
        RegisterClientRequest registerRequest = CreateRegisterClientRequest(carrierName, developerName, appName, appVersion, authToken);
        RegisterClientReply registerClientReply = await RegisterClient(host, port, registerRequest);
        if (registerClientReply.status != ReplyStatus.RS_SUCCESS)
        {
            throw new RegisterClientException("RegisterClientReply status is " + registerClientReply.status);
        }
        // Find Cloudlet 
        FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(carrierName, developerName, appName, appVersion, loc);
        FindCloudletReply findCloudletReply = await FindCloudlet(host, port, findCloudletRequest);

        return findCloudletReply;
    }

    public VerifyLocationRequest CreateVerifyLocationRequest(string carrierName, Loc loc)
    {
      if (sessionCookie == null)
      {
        return null;
      }
      return new VerifyLocationRequest {
        Ver = 1,
        carrier_name = carrierName,
        gps_location = loc,
        session_cookie = this.sessionCookie,
        verify_loc_token = null
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
        status = VerifyLocationReply.TowerStatus.TOWER_UNKNOWN;
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
        status = VerifyLocationReply.GPSLocationStatus.LOC_UNKNOWN;
      }
      return status;
    }

    public async Task<VerifyLocationReply> VerifyLocation(VerifyLocationRequest request)
    {
      return await VerifyLocation(GenerateDmeHostName(), defaultDmeRestPort, request);
    }

    public async Task<VerifyLocationReply> VerifyLocation(string host, uint port, VerifyLocationRequest request)
    {
      string token = RetrieveToken(tokenServerURI);
      request.verify_loc_token = token;

      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(VerifyLocationRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + verifylocationAPI, jsonStr).ConfigureAwait(false);
      if (responseStream == null || !responseStream.CanRead)
      {
        return null;
      }

      string responseStr = Util.StreamToString(responseStream);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(VerifyLocationReply));
      VerifyLocationReply reply = (VerifyLocationReply)deserializer.ReadObject(ms);

      // Reparse if default value is set.
      reply.tower_status = reply.tower_status == VerifyLocationReply.TowerStatus.TOWER_UNKNOWN ?
        ParseTowerStatus(responseStr) : reply.tower_status;
      reply.gps_location_status = reply.gps_location_status == VerifyLocationReply.GPSLocationStatus.LOC_UNKNOWN ?
        ParseGpsLocationStatus(responseStr) : reply.gps_location_status;

      return reply;
    }

    public GetLocationRequest CreateGetLocationRequest(string carrierName)
    {
      if (sessionCookie == null)
      {
        return null;
      }
      return new GetLocationRequest
      {
        ver = 1,
        carrier_name = carrierName,
        session_cookie = this.sessionCookie
      };
    }

    private GetLocationReply.LocStatus ParseLocationStatus(string responseStr)
    {
      string key = "status";
      JsonObject jsObj = (JsonObject)JsonValue.Parse(responseStr);
      GetLocationReply.LocStatus status;
      try
      {
        status = (GetLocationReply.LocStatus)Enum.Parse(typeof(GetLocationReply.LocStatus), jsObj[key]);
      }
      catch
      {
        status = GetLocationReply.LocStatus.LOC_UNKNOWN;
      }
      return status;
    }

    /*
     * Retrieves the carrier based network based geolocation of the network device.
     */
    public async Task<GetLocationReply> GetLocation(GetLocationRequest request)
    {
      return await GetLocation(GenerateDmeHostName(), defaultDmeRestPort, request);
    }

    public async Task<GetLocationReply> GetLocation(string host, uint port, GetLocationRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(GetLocationRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + getlocationAPI, jsonStr).ConfigureAwait(false);
      if (responseStream == null || !responseStream.CanRead)
      {
        return null;
      }

      string responseStr = Util.StreamToString(responseStream);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(GetLocationReply));
      GetLocationReply reply = (GetLocationReply)deserializer.ReadObject(ms);

      // Reparse if unknown:
      reply.status = reply.status == GetLocationReply.LocStatus.LOC_UNKNOWN ? ParseLocationStatus(responseStr) : reply.status;

      return reply;
    }

    public AppInstListRequest CreateAppInstListRequest(string carrierName, Loc loc)
    {
      if (sessionCookie == null)
      {
        return null;
      }
      if (loc == null)
      {
        return null;
      }

      return new AppInstListRequest
      {
        ver = 1,
        carrier_name = carrierName,
        session_cookie = this.sessionCookie,
        gps_location = loc
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
        status = AppInstListReply.AIStatus.AI_UNDEFINED;
      }
      return status;
    }

    public async Task<AppInstListReply> GetAppInstList(AppInstListRequest request)
    {
      return await GetAppInstList(GenerateDmeHostName(), defaultDmeRestPort, request);
    }

    public async Task<AppInstListReply> GetAppInstList(string host, uint port, AppInstListRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppInstListRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + appinstlistAPI, jsonStr).ConfigureAwait(false);
      if (responseStream == null || !responseStream.CanRead)
      {
        return null;
      }

      string responseStr = Util.StreamToString(responseStream);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(AppInstListReply));
      AppInstListReply reply = (AppInstListReply)deserializer.ReadObject(ms);

      // reparse if undefined.
      reply.status = reply.status == AppInstListReply.AIStatus.AI_UNDEFINED ? ParseAIStatus(responseStr) : reply.status;

      return reply;
    }

    public FqdnListRequest CreateFqdnListRequest()
    {
      if (sessionCookie == null)
      {
        return null;
      }

      return new FqdnListRequest
      {
        ver = 1,
        session_cookie = this.sessionCookie
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
        status = FqdnListReply.FLStatus.FL_UNDEFINED;
      }
      return status;
    }

    public async Task<FqdnListReply> GetFqdnList(FqdnListRequest request)
    {
      return await GetFqdnList(GenerateDmeHostName(), defaultDmeRestPort, request);
    }

    public async Task<FqdnListReply> GetFqdnList(string host, uint port, FqdnListRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(FqdnListRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + getfqdnlistAPI, jsonStr).ConfigureAwait(false);
      if (responseStream == null || !responseStream.CanRead)
      {
        return null;
      }

      string responseStr = Util.StreamToString(responseStream);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(FqdnListReply));
      FqdnListReply reply = (FqdnListReply)deserializer.ReadObject(ms);

      reply.status = reply.status == FqdnListReply.FLStatus.FL_UNDEFINED ? ParseFLStatus(responseStr) : reply.status;

      return reply;
    }

    public DynamicLocGroupRequest CreateDynamicLocGroupRequest(UInt64 lgId, DlgCommType dlgCommType, string userData)
    {
      if (sessionCookie == null)
      {
        return null;
      }

      return new DynamicLocGroupRequest
      {
        ver = 1,
        session_cookie = this.sessionCookie,
        lg_id = lgId,
        comm_type = dlgCommType,
        user_data = userData
      };
    }

    public async Task<DynamicLocGroupReply> AddUserToGroup(DynamicLocGroupRequest request)
    {
      return await AddUserToGroup(GenerateDmeHostName(), defaultDmeRestPort, request);
    }

    public async Task<DynamicLocGroupReply> AddUserToGroup(string host, uint port, DynamicLocGroupRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(DynamicLocGroupRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + dynamiclocgroupAPI, jsonStr).ConfigureAwait(false);
      if (responseStream == null || !responseStream.CanRead)
      {
        return null;
      }

      string responseStr = Util.StreamToString(responseStream);
      byte[] byteArray = Encoding.ASCII.GetBytes(responseStr);
      ms = new MemoryStream(byteArray);
      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(DynamicLocGroupReply));
      DynamicLocGroupReply reply = (DynamicLocGroupReply)deserializer.ReadObject(ms);

      reply.status = reply.status == ReplyStatus.RS_UNDEFINED ? ParseReplyStatus(responseStr) : reply.status;

      return reply;
    }

    public QosPositionRequest CreateQosPositionRequest(List<QosPosition> QosPositions, Int32 lteCategory, BandSelection bandSelection)
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
        band_selection = bandSelection
      };
    }

    public async Task<QosPositionKpiStream> GetQosPositionKpi(QosPositionRequest request)
    {
      return await GetQosPositionKpi(GenerateDmeHostName(), defaultDmeRestPort, request);
    }

    public async Task<QosPositionKpiStream> GetQosPositionKpi(string host, uint port, QosPositionRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(QosPositionRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + qospositionkpiAPI, jsonStr).ConfigureAwait(false);
      if (responseStream == null || !responseStream.CanRead || responseStream.Length == 0)
      {
        return null;
      }

      var qosPositionKpiStream = new QosPositionKpiStream(responseStream);

      return qosPositionKpiStream;
    }
  };
}
