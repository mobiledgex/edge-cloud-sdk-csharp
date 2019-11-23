using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using System.Threading.Tasks;

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

        //public async Task<Socket> GetTCPConnection(string host, uint port)
        public Socket GetTCPConnection(string host, uint port)
        {
            IPEndPoint localEndPoint = GetLocalIP(port);
            // Get remote ip of the provided host
            IPAddress remoteIP = Dns.GetHostAddresses(host)[0];
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteIP, (int)port);
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
        public SslStream GetTCPTLSConnection(string host, uint port)
        {
            IPEndPoint localEndPoint = GetLocalIP(port);

            TcpClient tcpClient = new TcpClient(localEndPoint);
            tcpClient.Connect(host, (int)port);

            using (SslStream sslStream = new SslStream(tcpClient.GetStream(), false,
        new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
            {
                sslStream.AuthenticateAsClient(host);
                return sslStream;
            }
        }

        public Socket GetUDPConnection(string host, uint port)
        {
            IPEndPoint localEndPoint = GetLocalIP(port);
            // Get remote ip of the provided host
            IPAddress remoteIP = Dns.GetHostAddresses(host)[0];
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteIP, (int)port);
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

        public async Task<HttpClient> GetHTTPConnection(string host, uint port)
        {
            HttpClient httpClient = new HttpClient();
            UriBuilder uriBuilder = new UriBuilder("http", host, (int)port);
            Uri uri = uriBuilder.Uri;
            httpClient.BaseAddress = uri;
            string responseBody = await httpClient.GetStringAsync(uri);
            Console.WriteLine("responseBody is " + responseBody);
            return httpClient;
        }

        public async Task<HttpClient> GetHTTPSConnection(string host, uint port)
        {
            HttpClient httpClient = new HttpClient();
            UriBuilder uriBuilder = new UriBuilder("https", host, (int)port);
            Uri uri = uriBuilder.Uri;
            httpClient.BaseAddress = uri;
            string responseBody = await httpClient.GetStringAsync(uri);
            Console.WriteLine("responseBody is " + responseBody);
            return httpClient;
        }

        private IPEndPoint GetLocalIP(uint port)
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
            IPEndPoint localEndPoint = new IPEndPoint(localIP, (int)port);
            foreach (IPAddress address in localHost.AddressList) {
                Console.WriteLine("address list is " + address);
            }
            return localEndPoint;
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
