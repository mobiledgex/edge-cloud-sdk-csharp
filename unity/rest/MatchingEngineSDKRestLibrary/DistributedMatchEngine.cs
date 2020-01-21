/**
 * Copyright 2020 MobiledgeX, Inc. All rights and licenses reserved.
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
using System.Text;
using System.Collections.Generic;

using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;

using System.Threading.Tasks;

namespace DistributedMatchEngine
{

  public class HttpException : Exception
  {
    public WebExceptionStatus WebExceptionStatus { get; set; }
    public int ErrorCode { get; set; }
    public HttpException(string message, WebExceptionStatus statusCode, int errorCode)
        : base(message)
    {
      this.WebExceptionStatus = statusCode;
      this.ErrorCode = errorCode;
    }

    public HttpException(string message, WebExceptionStatus statusCode, int errorCode, Exception innerException)
        : base(message, innerException)
    {
      this.WebExceptionStatus = statusCode;
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

  public class MatchingEngine
  {
    public const UInt32 defaultDmeRestPort = 38001;
    public const string carrierNameDefault = "tdg";

    string baseDmeHost = "dme.mobiledgex.net";
    UInt32 dmePort { get; set; } = defaultDmeRestPort; // HTTP REST port

    // API Paths:
    private string registerAPI = "/v1/registerclient";
    private string verifylocationAPI = "/v1/verifylocation";
    private string findcloudletAPI = "/v1/findcloudlet";
    private string getlocationAPI = "/v1/getlocation";
    private string appinstlistAPI = "/v1/getappinstlist";
    private string dynamiclocgroupAPI = "/v1/dynamiclocgroup";
    private string getfqdnlistAPI = "/v1/getfqdnlist";
    private string qospositionkpiAPI = "/v1/getqospositionkpi";

    public string sessionCookie { get; set; }
    string tokenServerURI;
    string authToken { get; set; }

    public MatchingEngine()
    {
    }

    private HttpWebRequest CreateWebRequest(string uri)
    {
      HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
      return httpWebRequest;
    }

    public string GetCarrierName()
    {
      return carrierNameDefault;
    }

    string GenerateDmeHostPath(string carrierName)
    {
      if (carrierName == null || carrierName == "")
      {
        return carrierNameDefault + "." + baseDmeHost;
      }
      return carrierName + "." + baseDmeHost;
    }

    public string GenerateDmeBaseUri(string carrierName, UInt32 port = defaultDmeRestPort)
    {
      return "https://" + GenerateDmeHostPath(carrierName) + ":" + port;
    }

    public string CreateUri(string host, UInt32 port)
    {
      if (host != null && host != "")
      {
        return "https://" + host + ":" + port;
      }
      return GenerateDmeBaseUri(null, port);
    }

    /*
     * This is temporary, and must be updated later.
     */
    private bool SetCredentials(string caCert, string clientCert, string clientPrivKey)
    {
      return false;
    }

    private async Task<Stream> PostRequest(string uri, string jsonStr)
    {
      return await Task.Run(() =>
      {
        var webRequest = CreateWebRequest(uri);
        // Choose network TBD (async)

        webRequest.Method = "POST";
        webRequest.ContentType = "application/json";
        webRequest.ContentLength = jsonStr.Length;

        // Write the data:
        using (var outstream = webRequest.GetRequestStream())
        {
          using (StreamWriter streamWriter = new StreamWriter(outstream))
          {
            streamWriter.Write(jsonStr);
          }
        }

        // try to get extended server repsonse if there's an error:
        HttpWebResponse response;

        try
        {
          response = (HttpWebResponse)webRequest.GetResponse();
        }
        catch (WebException we)
        {
          // ContentLength is -1?
          Log.D("Exception: " + we.Message);

          throw new HttpException(we.Message, we.Status, -1);
        }

        // Normal:
        Stream replyStream = response.GetResponseStream();
        return replyStream;
      });
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
      RegisterClientReply reply = (RegisterClientReply)deserializer.ReadObject(responseStream);
      this.sessionCookie = reply.session_cookie;
      this.tokenServerURI = reply.token_server_uri;
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

      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(FindCloudletReply));
      FindCloudletReply reply = (FindCloudletReply)deserializer.ReadObject(responseStream);
      return reply;
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

      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(VerifyLocationReply));
      VerifyLocationReply reply = (VerifyLocationReply)deserializer.ReadObject(responseStream);
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

    /*
     * Retrieves the carrier based network based geolocation of the network device.
     */
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

      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(GetLocationReply));
      GetLocationReply reply = (GetLocationReply)deserializer.ReadObject(responseStream);
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

    public async Task<AppInstListReply> AppInstList(string host, uint port, AppInstListRequest request)
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

      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(AppInstListReply));
      AppInstListReply reply = (AppInstListReply)deserializer.ReadObject(responseStream);
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

    public async Task<FqdnListReply> FqdnList(string host, uint port, FqdnListRequest request)
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

      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(FqdnListReply));
      FqdnListReply reply = (FqdnListReply)deserializer.ReadObject(responseStream);
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

    public async Task<DynamicLocGroupReply> DynamicLocGroup(string host, uint port, DynamicLocGroupRequest request)
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

      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(DynamicLocGroupReply));
      DynamicLocGroupReply reply = (DynamicLocGroupReply)deserializer.ReadObject(responseStream);
      return reply;
    }

    public QosPositionRequest CreateQosPositionRequest(List<QosPosition> QosPositions)
    {
      if (sessionCookie == null)
      {
        return null;
      }

      return new QosPositionRequest
      {
        ver = 1,
        positions = QosPositions.ToArray(),
        session_cookie = this.sessionCookie
      };
    }

    public async Task<QosPositionKpiStreamReply> GetQosPositionKpi(string host, uint port, QosPositionRequest request)
    {
      DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(QosPositionRequest));
      MemoryStream ms = new MemoryStream();
      serializer.WriteObject(ms, request);
      string jsonStr = Util.StreamToString(ms);

      Stream responseStream = await PostRequest(CreateUri(host, port) + qospositionkpiAPI, jsonStr);
      if (responseStream == null || !responseStream.CanRead)
      {
        return null;
      }

      DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(QosPositionKpiStreamReply));
      QosPositionKpiStreamReply streamReply = (QosPositionKpiStreamReply)deserializer.ReadObject(responseStream);
      return streamReply;
    }

  };
}