/**
 * Copyright 2018-2020 MobiledgeX, Inc. All rights and licenses reserved.
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

namespace DistributedMatchEngine
{
  public class DmeDnsException : Exception
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
    public const string carrierNameDefault = "wifi";
    public const string wifiCarrier = "wifi";
    public const string wifiOnlyDmeHost = wifiCarrier + "." + baseDmeHost; // Demo mode only.
    public const string baseDmeHost = "dme.mobiledgex.net";

    UInt32 dmePort { get; set; } = defaultDmeRestPort; // HTTP REST port

    public CarrierInfo carrierInfo { get; set; }
    public NetInterface netInterface { get; set; }
    public UniqueID uniqueID { get; set; }

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

    public bool useOnlyWifi { get; set; } = false;

    public string sessionCookie { get; set; }
    string tokenServerURI;
    string authToken { get; set; }
    
    /// <summary>
    /// Constructor for Matching Engine 
    /// </summary>
    /// <param name="carrierInfo">Telephony Interface</param>
    /// <param name="netInterface">Network Interface</param>
    /// <param name="uniqueID">Unique ID specified by the user</param>
    public MatchingEngine(CarrierInfo carrierInfo = null, NetInterface netInterface = null, UniqueID uniqueID = null)
    {
      httpClient = new HttpClient();
      httpClient.Timeout = TimeSpan.FromTicks(DEFAULT_REST_TIMEOUT_MS * TICKS_PER_MS);
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
    }

    /// <summary>
    /// Set the REST timeout for DME APIs
    /// </summary>
    /// <param name="timeout_in_milliseconds"> (integer) Time in milliseconds</param>
    /// <returns></returns>
    public TimeSpan SetTimeout(int timeout_in_milliseconds)
    {
      if (timeout_in_milliseconds > 1)
      {
        return httpClient.Timeout = TimeSpan.FromTicks(timeout_in_milliseconds * TICKS_PER_MS);
      }
      return httpClient.Timeout = TimeSpan.FromTicks(DEFAULT_REST_TIMEOUT_MS * TICKS_PER_MS);
    }

    public string GetUniqueIDType()
    {
      return uniqueID.GetUniqueID();
    }

    public string GetUniqueID()
    {
      return uniqueID.GetUniqueID();
    }
    
    /// <summary>
    /// Gets the Cell Id ,GSM Cell ID is a generally unique number used to identify each base transceiver station</param>
    /// </summary>
    /// <returns>(Unsigned 32 bits Integer) Cell Id</returns>
    public UInt32 GetCellID()
    {
      return carrierInfo.GetCellID();
    }
    
    /// <summary>
    /// Gets MccMnc (Mobile Network Code - Mobile Country Code) or wifi if useOnlyWifi bool is true 
    /// </summary>
    /// <returns>MccMnc code or wifi</returns>
    public string GetCarrierName()
    {
      if (useOnlyWifi)
      {
        return wifiCarrier;
      }

      string mccmnc = carrierInfo.GetMccMnc();
      if (mccmnc == "" || mccmnc == null)
      {
        return wifiCarrier;
      }
      return mccmnc;
    }

    /// <summary>
    /// Generate Distributed Matching Engine Host Name
    /// For Telephony devices ( it uses MncMcc code )
    /// <para>
    /// for example for TMobile in the US (310-260)
    /// Where 310 is US Mobile Country Code and 260 is TMobile Mobile Network Code
    /// </para>
    /// </summary>
    /// <returns>(string)Distributed Matching Engine Host Name to be used in the API call</returns>
    public string GenerateDmeHostName()
    {
      if (carrierInfo == null)
      {
        throw new InvalidCarrierInfoException("Missing platform integration interface.");
      }

      if (useOnlyWifi)
      {
        return wifiOnlyDmeHost;
      }

      string mccmnc = carrierInfo.GetMccMnc();
      if (mccmnc == null)
      {
        Log.E("PlatformIntegration CarrierInfo interface does not have a valid MCCMNC string.");
        return wifiOnlyDmeHost; // fallback to wifi, this hostname must/should always exist.
      }

      if (mccmnc.Equals(wifiOnlyDmeHost))
      {
        Log.D("PlatformIntegration CarrierInfo interface does not have a valid MCCMNC string.");
        return wifiOnlyDmeHost;
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
    
    /// <summary>
    /// Creates a uri using a host and port 
    /// </summary>
    /// <param name="host">host name</param>
    /// <param name="port">port number</param>
    /// <returns> uri string </returns>
    private string CreateUri(string host, uint port)
    {
      return "https://" + host + ":" + port;
    }
    
    /// <summary>
    /// Performs a post request
    /// </summary>
    /// <param name="uri"> the uri for the post request </param>
    /// <param name="jsonStr">the json string as the body for the Post request</param>
    /// <returns>Stream (Sequence of bytes)</returns>
    private async Task<Stream> PostRequest(string uri, string jsonStr)
    {
      // FIXME: Choose network TBD (.Net Core 2.1)
      Log.D("URI: " + uri);
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
    
    /// <summary>
    /// Creates Register Client Request
    /// </summary>
    /// <param name="carrierName"> Carrier Name (ex. TDG)</param>
    /// <param name="orgName"> Organization Name</param>
    /// <param name="appName"> Application Name</param>
    /// <param name="appVersion"> Application Version</param>
    /// <param name="authToken">Authentication Token supplied by developer</param>
    /// <param name="cellID">GSM Cell ID is a generally unique number used to identify each base transceiver station</param>
    /// <param name="uniqueIDType"></param>
    /// <param name="uniqueID"></param>
    /// <param name="tags"></param>
    /// <returns>RegisterClientRequest Object</returns>
    public RegisterClientRequest CreateRegisterClientRequest(
      string carrierName, string orgName, string appName, string appVersion, string authToken = null,
      UInt32 cellID = 0, string uniqueIDType = null, string uniqueID = null, Tag[] tags = null)
    {
      return new RegisterClientRequest
      {
        ver = 1,
        carrier_name = carrierName,
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

    /// <summary>
    /// Parse Reply Status 
    /// </summary>
    /// <param name="responseStr">the returned string from the API call</param>
    /// <returns>ReplyStatus Object</returns>
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
    
    /// <summary>
    /// Register Client,called by the client
    /// </summary>
    /// <param name="request">RegisterClientRequest Object</param>
    /// <returns>RegisterClientReply Object</returns>
    public async Task<RegisterClientReply> RegisterClient(RegisterClientRequest request)
    {
      return await RegisterClient(GenerateDmeHostName(), defaultDmeRestPort, request);
    }
    
    /// <summary>
    /// Register Client 
    /// </summary>
    /// <param name="host"> host for the api call</param>
    /// <param name="port"> port for the api call</param>
    /// <param name="request">RegisterClientRequest Object</param>
    /// <returns>RegisterClientReply Object</returns>
    public async Task<RegisterClientReply> RegisterClient(string host, uint port, RegisterClientRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(RegisterClientRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + registerAPI, jsonStr);
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

    /// <summary>
    /// Creates FindCloudlet Request
    /// </summary>
    /// <param name="carrierName"> Carrier Name (ex. TDG)</param>
    /// <param name="loc">Loc Object representing Location</param>
    /// <param name="orgName">Organization Name</param>
    /// <param name="appName">Application Name</param>
    /// <param name="appVers">Application Version</param>
    /// <param name="cellID">GSM Cell ID is a generally unique number used to identify each base transceiver station</param>
    /// <param name="tags"></param>
    /// <returns>FindCloudletRequest Object</returns>
    public FindCloudletRequest CreateFindCloudletRequest(
      string carrierName, Loc loc,
      string orgName = null, string appName = null, string appVers = null, // Optionals
      UInt32 cellID = 0, Tag[] tags = null)
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
        org_name = orgName,
        app_name = appName,
        app_vers = appVers,
        gps_location = loc,
        cell_id = cellID,
        tags = tags
      };
    }
    
    /// <summary>
    /// Parse FindCloudletReply Status
    /// </summary>
    /// <param name="responseStr">the returned string from the API call</param>
    /// <returns>FindCloudlet Reply Status</returns>
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
    
    /// <summary>
    /// Parse Application port types
    /// </summary>
    /// <param name="appports">AppPort Array</param>
    /// <param name="responseStr">the returned string from the API call</param>
    /// <returns>AppPort Array </returns>
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
    
    /// <summary>
    /// FindCloudlet called by the client
    /// </summary>
    /// <param name="request">FindCloudletRequest Object</param>
    /// <returns>FindCloudletReply Object</returns>
    public async Task<FindCloudletReply> FindCloudlet(FindCloudletRequest request)
    {
      return await FindCloudlet(GenerateDmeHostName(), defaultDmeRestPort, request);
    }
    
    /// <summary>
    /// Find Cloudlet
    /// </summary>
    /// <param name="host"> host for the api call</param>
    /// <param name="port"> port for the api call</param>
    /// <param name="request">FindCloudletRequest Object</param>
    /// <returns>FindCloudletReply Object</returns>
    public async Task<FindCloudletReply> FindCloudlet(string host, uint port, FindCloudletRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(FindCloudletRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + findcloudletAPI, jsonStr);
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
    
    /// <summary>
    /// Wrapper function for RegisterClient and FindCloudlet. This API cannot be used for Non-Platform APPs.
    /// </summary>
    /// <param name="carrierName"> Carrier Name (ex. TDG)</param>
    /// <param name="orgName">Organization Name</param>
    /// <param name="appName">Application Name</param>
    /// <param name="appVersion">Application Version</param>
    /// <param name="authToken">Authentication Token supplied by developer</param>
    /// <param name="loc">Loc Object represents Location</param>
    /// <param name="cellID">GSM Cell ID is a generally unique number used to identify each base transceiver station</param>
    /// <param name="uniqueIDType"></param>
    /// <param name="uniqueID"></param>
    /// <param name="tags"></param>
    /// <returns>FindCloudletReply Object</returns>
    // Wrapper function for RegisterClient and FindCloudlet. This API cannot be used for Non-Platform APPs.
    public async Task<FindCloudletReply> RegisterAndFindCloudlet(
      string carrierName, string orgName, string appName, string appVersion, string authToken, Loc loc,
      UInt32 cellID = 0, string uniqueIDType = null, string uniqueID = null, Tag[] tags = null)
    {
      // Register Client
      RegisterClientRequest registerRequest = CreateRegisterClientRequest(
        carrierName: carrierName,
        orgName: orgName,
        appName: appName,
        appVersion: appVersion,
        authToken: authToken,
        cellID: cellID,
        uniqueIDType: uniqueIDType,
        uniqueID: uniqueID,
        tags: tags);
      RegisterClientReply registerClientReply = await RegisterClient(registerRequest);
      if (registerClientReply.status != ReplyStatus.RS_SUCCESS)
      {
        throw new RegisterClientException("RegisterClientReply status is " + registerClientReply.status);
      }
      // Find Cloudlet
      FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(
        carrierName: carrierName,
        loc: loc,
        cellID: cellID,
        tags: tags);
      FindCloudletReply findCloudletReply = await FindCloudlet(findCloudletRequest);

      return findCloudletReply;
    }
    
    /// <summary>
    /// Register Client And Find Cloudlet
    /// </summary>
    /// <param name="host"> host for the api call</param>
    /// <param name="port"> port for the api call</param>
    /// <param name="carrierName"> Carrier Name (ex. TDG)</param>
    /// <param name="orgName">Organization Name</param>
    /// <param name="appName">Application Name</param>
    /// <param name="appVersion">Application Version</param>
    /// <param name="authToken">Authentication Token supplied by developer</param>
    /// <param name="loc">Loc Object represents Location</param>
    /// <param name="cellID">GSM Cell ID is a generally unique number used to identify each base transceiver station</param>
    /// <param name="uniqueIDType"></param>
    /// <param name="uniqueID"></param>
    /// <param name="tags"></param>
    /// <returns></returns>
    // Override with specified dme host and port. This API cannot be used for Non-Platform APPs.
    // Override with specified dme host and port. This API cannot be used for Non-Platform APPs.
    public async Task<FindCloudletReply> RegisterAndFindCloudlet(string host, uint port,
      string carrierName, string orgName, string appName, string appVersion, string authToken, Loc loc,
      UInt32 cellID = 0, string uniqueIDType = null, string uniqueID = null, Tag[] tags = null)
    {
      // Register Client
      RegisterClientRequest registerRequest = CreateRegisterClientRequest(
        carrierName: carrierName,
        orgName: orgName,
        appName: appName,
        appVersion: appVersion,
        authToken: authToken,
        cellID: cellID,
        uniqueIDType: uniqueIDType,
        uniqueID: uniqueID,
        tags: tags);
      RegisterClientReply registerClientReply = await RegisterClient(host, port, registerRequest);
      if (registerClientReply.status != ReplyStatus.RS_SUCCESS)
      {
        throw new RegisterClientException("RegisterClientReply status is " + registerClientReply.status);
      }
      // Find Cloudlet 
      FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(
        carrierName: carrierName,
        loc: loc,
        cellID: cellID,
        tags: tags);
      FindCloudletReply findCloudletReply = await FindCloudlet(host, port, findCloudletRequest);

      return findCloudletReply;
    }
    
    /// <summary>
    /// Create Verify Location Request
    /// </summary>
    /// <param name="carrierName"> Carrier Name (ex. TDG)</param>
    /// <param name="loc">Location Object</param>
    /// <param name="cellID">GSM Cell ID is a generally unique number used to identify each base transceiver station</param>
    /// <param name="tags"></param>
    /// <returns>VerifyLocationRequest Object</returns>
    public VerifyLocationRequest CreateVerifyLocationRequest(string carrierName, Loc loc, UInt32 cellID = 0, Tag[] tags = null)
    {
      if (sessionCookie == null)
      {
        return null;
      }
      return new VerifyLocationRequest
      {
        ver = 1,
        carrier_name = carrierName,
        gps_location = loc,
        session_cookie = this.sessionCookie,
        verify_loc_token = null,
        cell_id = cellID,
        tags = tags
      };
    }
    
    /// <summary>
    /// Parse Tower Status
    /// </summary>
    /// <param name="responseStr">the returned string from the API call</param>
    /// <returns>TowerStatus</returns>
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
    
    /// <summary>
    /// Parse GpsLocation Status
    /// </summary>
    /// <param name="responseStr">the returned string from the API call</param>
    /// <returns>VerifyLocationReply GPSLocationStatus </returns>
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
    
    /// <summary>
    /// VerifyLocation, called by the client
    /// </summary>
    /// <param name="request">VerifyLocationRequest Object</param>
    /// <returns>VerifyLocationReply Object</returns>
    public async Task<VerifyLocationReply> VerifyLocation(VerifyLocationRequest request)
    {
      return await VerifyLocation(GenerateDmeHostName(), defaultDmeRestPort, request);
    }
    
    /// <summary>
    /// Verify Location
    /// </summary>
    /// <param name="host"> host for the api call</param>
    /// <param name="port"> port for the api call</param>
    /// <param name="request">VerifyLocationRequest Object</param>
    /// <returns>VerifyLocationReply Object</returns>
    public async Task<VerifyLocationReply> VerifyLocation(string host, uint port, VerifyLocationRequest request)
    {
      string token = RetrieveToken(tokenServerURI);
      request.verify_loc_token = token;

      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(VerifyLocationRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + verifylocationAPI, jsonStr);
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
    
    /// <summary>
    /// Create Get Location Request
    /// </summary>
    /// <param name="carrierName"> Carrier Name (ex. TDG)</param>
    /// <param name="cellID">GSM Cell ID is a generally unique number used to identify each base transceiver station</param>
    /// <param name="tags"></param>
    /// <returns>GetLocationRequest Object</returns>
    public GetLocationRequest CreateGetLocationRequest(string carrierName, UInt32 cellID = 0, Tag[] tags = null)
    {
      if (sessionCookie == null)
      {
        return null;
      }

      return new GetLocationRequest
      {
        ver = 1,
        carrier_name = carrierName,
        session_cookie = this.sessionCookie,
        cell_id = cellID,
        tags = tags
      };
    }
    
    /// <summary>
    /// Parse Location Reply Status
    /// </summary>
    /// <param name="responseStr">the returned string from the API call</param>
    /// <returns>Location Reply Status</returns>
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
  
    /// <summary>
    /// Gets Location of the carrier based network based geolocation of the network device.
    /// </summary>
    /// <param name="request">GetLocationRequest Object</param>
    /// <returns>GetLocationReply Object</returns>
    public async Task<GetLocationReply> GetLocation(GetLocationRequest request)
    {
      return await GetLocation(GenerateDmeHostName(), defaultDmeRestPort, request);
    }
    /// <summary>
    /// Gets Location of the carrier based network based geolocation of the network device.
    /// </summary>
    /// <param name="host"> host for the api call</param>
    /// <param name="port"> port for the api call</param>
    /// <param name="request">GetLocationRequest Object</param>
    /// <returns>GetLocationReply Object</returns>
    public async Task<GetLocationReply> GetLocation(string host, uint port, GetLocationRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(GetLocationRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + getlocationAPI, jsonStr);
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
    
    /// <summary>
    /// Create Application Instance List Request 
    /// </summary>
    /// <param name="carrierName"> Carrier Name (ex. TDG)</param>
    /// <param name="loc">Loc Object representing Location</param>
    /// <param name="cellID">GSM Cell ID is a generally unique number used to identify each base transceiver station</param>
    /// <param name="tags"></param>
    /// <returns>AppInstListRequest Object</returns>
    public AppInstListRequest CreateAppInstListRequest(string carrierName, Loc loc, UInt32 cellID = 0, Tag[] tags = null)
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
        gps_location = loc,
        cell_id = cellID,
        tags = tags
      };
    }
    
    /// <summary>
    /// Checks Application Instance Reply Status
    /// </summary>
    /// <param name="responseStr">the returned string from the API call</param>
    /// <returns>AppInstListReply  Status</returns>
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
    
    /// <summary>
    /// Gets Application Instances list called by the client
    /// </summary>
    /// <param name="request">AppInstListRequest Object</param>
    /// <returns> AppInstListReply Object</returns>
    public async Task<AppInstListReply> GetAppInstList(AppInstListRequest request)
    {
      return await GetAppInstList(GenerateDmeHostName(), defaultDmeRestPort, request);
    }
    
    /// <summary>
    /// Gets Application Instances list 
    /// </summary>
    /// <param name="host"> host for the api call</param>
    /// <param name="port"> port for the api call</param>
    /// <param name="request">AppInstListRequest Object</param>
    /// <returns>AppInstListReply Object</returns>
    public async Task<AppInstListReply> GetAppInstList(string host, uint port, AppInstListRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppInstListRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + appinstlistAPI, jsonStr);
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
    
    /// <summary>
    /// Create Fqdn List Request (Fqdn = Fully Qualified Domain Name)
    /// </summary>
    /// <param name="cellID">GSM Cell ID is a generally unique number used to identify each base transceiver station</param>
    /// <param name="tags"></param>
    /// <returns>FqdnListRequest (Fqdn = Fully Qualified Domain Name) </returns>
    public FqdnListRequest CreateFqdnListRequest(UInt32 cellID = 0, Tag[] tags = null)
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
    
    /// <summary>
    /// Parse FqdnListReply Status (Fqdn = Fully Qualified Domain Name)
    /// </summary>
    /// <param name="responseStr">the returned string from the API call</param>
    /// <returns>FqdnListReply Status</returns>
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
    
    /// <summary>
    /// Gets FqdnList called by the client (Fqdn = Fully Qualified Domain Name)
    /// </summary>
    /// <param name="request">FqdnListRequest Object</param>
    /// <returns>FqdnListReply object</returns>
    public async Task<FqdnListReply> GetFqdnList(FqdnListRequest request)
    {
      return await GetFqdnList(GenerateDmeHostName(), defaultDmeRestPort, request);
    }
    
    /// <summary>
    /// Gets FqdnList (Fqdn = Fully Qualified Domain Name)
    /// </summary>
    /// <param name="host"> host for the api call</param>
    /// <param name="port"> port for the api call</param>
    /// <param name="request">FqdnListRequest Object</param>
    /// <returns>FqdnListReply Object</returns>
    public async Task<FqdnListReply> GetFqdnList(string host, uint port, FqdnListRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(FqdnListRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + getfqdnlistAPI, jsonStr);
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

    public DynamicLocGroupRequest CreateDynamicLocGroupRequest(UInt64 lgId, DlgCommType dlgCommType,
      string userData = null, UInt32 cellID = 0, Tag[] tags = null)
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
        user_data = userData,
        cell_id = cellID,
        tags = tags
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

      Stream responseStream = await PostRequest(CreateUri(host, port) + dynamiclocgroupAPI, jsonStr);
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

    public QosPositionRequest CreateQosPositionRequest(List<QosPosition> QosPositions, Int32 lteCategory, BandSelection bandSelection,
      UInt32 cellID = 0, Tag[] tags = null)
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
    
    /// <summary>
    ///  Gets Quality of Service Position data used by the client
    /// </summary>
    /// <param name="request"> QosPositionRequest Object </param>
    /// <returns> QosPositionKpiStream </returns>
    public async Task<QosPositionKpiStream> GetQosPositionKpi(QosPositionRequest request)
    {
      return await GetQosPositionKpi(GenerateDmeHostName(), defaultDmeRestPort, request);
    }
    
    /// <summary>
    /// Gets Quality of Service Position data
    /// </summary>
    /// <param name="host"> host for the api call</param>
    /// <param name="port"> port for the api call</param>
    /// <param name="request">QosPositionRequest Object</param>
    /// <returns>QosPositionKpiStream Object</returns>
    public async Task<QosPositionKpiStream> GetQosPositionKpi(string host, uint port, QosPositionRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(QosPositionRequest));
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
  };
}
