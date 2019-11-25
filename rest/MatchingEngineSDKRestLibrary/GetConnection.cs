using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Net.WebSockets;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

using System.Threading.Tasks;
using System.Threading;

namespace DistributedMatchEngine
{

    public interface NetInterface
    {
        string GetIPAddress();
        bool IsWifi();
        bool IsCellular();
    }

    class EmptyNetInterface: NetInterface
    {
        public string GetIPAddress()
        {
            return null;
        }

        public bool IsWifi()
        {
            return false;
        }

        public bool IsCellular()
        {
            return false;
        }
    }

    public partial class MatchingEngine
    {

        public async Task<Socket> RegisterAndFindTCPConnection(string carrierName, string developerName, string appName, string appVersion, string authToken, Loc loc)
        {
            RegisterClientRequest registerRequest = CreateRegisterClientRequest(carrierName, developerName, appName, appVersion, authToken);
            await RegisterClient(registerRequest);

            FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(carrierName, developerName, appName, appVersion, loc);
            FindCloudletReply findCloudletReply = await FindCloudlet(findCloudletRequest);

            List<AppPort> ports = GetTCPPorts(findCloudletReply);

            if (ports.Count == 0) {
                throw new GetConnectionException("No TCP Ports returned in findCloudletReply");
            }

            AppPort port = ports[0];
            string host = findCloudletReply.fqdn;
            string fqdnPrefix = port.fqdn_prefix;
            host = fqdnPrefix + host;
            int publicPort = port.public_port;

            return GetTCPConnection(host, publicPort);
        }

        public async Task<Socket> RegisterAndFindUDPConnection(string carrierName, string developerName, string appName, string appVersion, string authToken, Loc loc)
        {
            RegisterClientRequest registerRequest = CreateRegisterClientRequest(carrierName, developerName, appName, appVersion, authToken);
            await RegisterClient(registerRequest);

            FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(carrierName, developerName, appName, appVersion, loc);
            FindCloudletReply findCloudletReply = await FindCloudlet(findCloudletRequest);

            List<AppPort> ports = GetUDPPorts(findCloudletReply);

            if (ports.Count == 0) {
                throw new GetConnectionException("No UDP Ports returned in findCloudletReply");
            }

            AppPort port = ports[0];

            string host = findCloudletReply.fqdn;
            string fqdnPrefix = port.fqdn_prefix;
            host = fqdnPrefix + host;
            int publicPort = port.public_port;

            return GetUDPConnection(host, publicPort);
        }

        public async Task<HttpClient> RegisterAndFindHTTPConnection(string carrierName, string developerName, string appName, string appVersion, string authToken, Loc loc)
        {
            RegisterClientRequest registerRequest = CreateRegisterClientRequest(carrierName, developerName, appName, appVersion, authToken);
            await RegisterClient(registerRequest);

            FindCloudletRequest findCloudletRequest = CreateFindCloudletRequest(carrierName, developerName, appName, appVersion, loc);
            FindCloudletReply findCloudletReply = await FindCloudlet(findCloudletRequest);

            List<AppPort> ports = GetHTTPPorts(findCloudletReply);

            if (ports.Count == 0) {
                throw new GetConnectionException("No HTTP Ports returned in findCloudletReply");
            }

            AppPort port = ports[0];

            string host = findCloudletReply.fqdn;
            string fqdnPrefix = port.fqdn_prefix;
            host = fqdnPrefix + host;
            int publicPort = port.public_port;

            return await GetHTTPConnection(host, publicPort);
        }

        //public async Task<Socket> GetTCPConnection(string host, uint port)
        public Socket GetTCPConnection(string host, int port)
        {
            IPEndPoint localEndPoint = GetLocalIP(port);
            // Get remote ip of the provided host
            IPAddress remoteIP = Dns.GetHostAddresses(host)[0];
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteIP, port);
            // Create Socket and bind to local ip and connect to remote endpoint
            Socket s = new Socket(remoteEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            s.Bind(localEndPoint);
            s.Connect(remoteEndPoint);

            if (!s.IsBound && !s.Connected) 
            {
                throw new GetConnectionException("Could not bind to interface or connect to server");
            }
            else if (!s.IsBound)
            {
                throw new GetConnectionException("Could not bind to interface");
            }
            else if (!s.Connected)
            {
                throw new GetConnectionException("Could not connect to server");
            }
            return s;
        }

        // close tcp client?
        public SslStream GetTCPTLSConnection(string host, int port)
        {
            IPEndPoint localEndPoint = GetLocalIP(port);

            TcpClient tcpClient = new TcpClient(localEndPoint);
            tcpClient.Connect(host, port);

            using (SslStream sslStream = new SslStream(tcpClient.GetStream(), false,
        new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
            {
                sslStream.AuthenticateAsClient(host);
                return sslStream;
            }
        }

        public Socket GetUDPConnection(string host, int port)
        {
            IPEndPoint localEndPoint = GetLocalIP(port);
            // Get remote ip of the provided host
            IPAddress remoteIP = Dns.GetHostAddresses(host)[0];
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteIP, port);
            // Create Socket and bind to local ip and connect to remote endpoint
            Socket s = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(localEndPoint);
            s.Connect(remoteEndPoint);

            if (!s.IsBound && !s.Connected) 
            {
                throw new GetConnectionException("Could not bind to interface or connect to server");
            }
            else if (!s.IsBound)
            {
                throw new GetConnectionException("Could not bind to interface");
            }
            else if (!s.Connected)
            {
                throw new GetConnectionException("Could not connect to server");
            }
            return s;
        }

        public async Task<HttpClient> GetHTTPConnection(string host, int port)
        {
            HttpClient httpClient = new HttpClient();
            UriBuilder uriBuilder = new UriBuilder("http", host, port);
            Uri uri = uriBuilder.Uri;
            httpClient.BaseAddress = uri;
            string responseBody = await httpClient.GetStringAsync(uri);
            Console.WriteLine("responseBody is " + responseBody);
            return httpClient;
        }

        public async Task<HttpClient> GetHTTPSConnection(string host, int port)
        {
            HttpClient httpClient = new HttpClient();
            UriBuilder uriBuilder = new UriBuilder("https", host, port);
            Uri uri = uriBuilder.Uri;
            httpClient.BaseAddress = uri;
            string responseBody = await httpClient.GetStringAsync(uri);
            Console.WriteLine("responseBody is " + responseBody);
            return httpClient;
        }

        public async Task<ClientWebSocket> GetWebsocketConnection(string host, int port)
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            UriBuilder uriBuilder = new UriBuilder("ws", host, port);
            Uri uri = uriBuilder.Uri;
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            await webSocket.ConnectAsync(uri, token);
            if (webSocket.State == WebSocketState.Open)
            {
                return webSocket;
            }
            else
            {
                throw new GetConnectionException("Cannot get websocket connection");
            }
        }

        public async Task<ClientWebSocket> GetSecureWebsocketConnection(string host, int port)
        {
            ClientWebSocket webSocket = new ClientWebSocket();
            UriBuilder uriBuilder = new UriBuilder("wss", host, port);
            Uri uri = uriBuilder.Uri;
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            await webSocket.ConnectAsync(uri, token);
            if (webSocket.State == WebSocketState.Open)
            {
                return webSocket;
            }
            else
            {
                throw new GetConnectionException("Cannot get websocket connection");
            }
        }

        private IPEndPoint GetLocalIP(int port)
        {
            string host = netInterface.GetIPAddress();
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface inter in networkInterfaces)
            {
                Console.WriteLine("ip properties are " + inter.GetIPProperties());
                Console.WriteLine("Interface type is " + inter.NetworkInterfaceType);
                Console.WriteLine("address is " + inter.GetPhysicalAddress().ToString());
            }
            // Get cellular ip to bind to
            IPHostEntry localHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress localIP = localHost.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(localIP, port);
            foreach (IPAddress address in localHost.AddressList) {
                Console.WriteLine("address list is " + address);
            }
            return localEndPoint;
        }


        public List<AppPort> GetPortsByProtocol(FindCloudletReply reply, LProto proto)
        {
            List<AppPort> portsByProtocol = new List<AppPort>();
            AppPort[] ports = reply.ports;
            foreach (AppPort port in ports)
            {
                if (port.proto == proto)
                {
                    portsByProtocol.Add(port);
                }
            }
            return portsByProtocol;
        }

        public List<AppPort> GetTCPPorts(FindCloudletReply reply)
        {
            List<AppPort> tcpPorts = new List<AppPort>();
            AppPort[] ports = reply.ports;
            foreach (AppPort port in ports)
            {
                if (port.proto == LProto.L_PROTO_TCP)
                {
                    tcpPorts.Add(port);
                }
            }
            return tcpPorts;
        }

        public List<AppPort> GetUDPPorts(FindCloudletReply reply)
        {
            List<AppPort> udpPorts = new List<AppPort>();
            AppPort[] ports = reply.ports;
            foreach (AppPort port in ports)
            {
                if (port.proto == LProto.L_PROTO_UDP)
                {
                    udpPorts.Add(port);
                }
            }
            return udpPorts;
        }

        public List<AppPort> GetHTTPPorts(FindCloudletReply reply)
        {
            List<AppPort> httpPorts = new List<AppPort>();
            AppPort[] ports = reply.ports;
            foreach (AppPort port in ports)
            {
                if (port.proto == LProto.L_PROTO_HTTP)
                {
                    httpPorts.Add(port);
                }
            }
            return httpPorts;
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate,
X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None) return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
      
            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }
    }
}
