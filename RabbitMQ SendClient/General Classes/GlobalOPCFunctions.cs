namespace RabbitMQ_SendClient.General_Classes
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Windows;
    using System.Windows.Controls;
    using Opc.Ua;
    using Opc.UaFx;
    using Opc.UaFx.Client;

    internal static class GlobalOpcFunctions
    {
        public static Node[] Nodes = new Node[0];
        public static OpcClient ConfigureOpcClient(IPAddress ipAddress, ushort portNum)
        {
            var certificate = OpcCertificateManager.CreateCertificate(Application.Current.MainWindow.Title,
                Application.Current.StartupUri);

            var uri = new Uri(Ports.OpcTcp + "://" + ipAddress + ":" + portNum + "/");

            var client =
                new OpcClient(uri)
                {
                    UserIdentity = new UserIdentity(certificate),
                    UseDomainChecks = false,
                    UseSecureEndpoint = true,
                    DisconnectTimeout = 10000,
                    ReconnectTimeout = 10000,
                    SessionTimeout = 60000,
                    PreferredPolicy = new OpcSecurityPolicy(OpcSecurityMode.None, OpcSecurityAlgorithm.Auto, 0)
                };

            return client;
        }

        public static OpcClient ConfigureOpcClient(OpcUaServer uaServer)
        {
            uaServer.SecurityX509Certificate = OpcCertificateManager.CreateCertificate(
                Application.Current.MainWindow.Title,
                Application.Current.StartupUri);
            uaServer.SetUri();

            var client = new OpcClient(uaServer.ServerUri)
            {
                UseDomainChecks = false,
                UserIdentity = new UserIdentity(uaServer.SecurityX509Certificate),
                UseSecureEndpoint = true,
                DisconnectTimeout = 10000,
                ReconnectTimeout = 10000,
                SessionTimeout = 60000,
                PreferredPolicy = new OpcSecurityPolicy(OpcSecurityMode.None, OpcSecurityAlgorithm.Auto, 0)
            };

            return client;
        }

        public static void GetNodes(OpcNodeInfo node)
        {
            var nodes = node.Children();
            foreach (var opcNodeInfo in nodes)
            {
                Array.Resize(ref Nodes, Nodes.Length + 1);
                Nodes[Nodes.Length - 1].NodeName = opcNodeInfo.Name.ToString();
                Nodes[Nodes.Length - 1].NodeDescription = opcNodeInfo.Reference.ToString();
                Nodes[Nodes.Length - 1].NodeValue = opcNodeInfo.Category.ToString();
                Nodes[Nodes.Length - 1].NodeType = opcNodeInfo.GetType();
            }
        }
    }

    public class OpcUaServer
    {
        public OpcUaServer()
        {
            this.IpAddress = IPAddress.Loopback;
            this.PortNum = 49320;
            this.AcceptUntrustedCertificates = true;
            this.UidGuid = new Guid();
            this.SecurityX509Certificate = new X509Certificate2(OpcCertificateManager.CreateCertificate(
                Application.Current.MainWindow.Title,
                Application.Current.StartupUri));
            this.ServerUri = new Uri(Ports.OpcTcp + $"://{this.IpAddress}:{this.PortNum}/");
            ConfigureSecurity();
            SetUri();
            this.Client = new OpcClient(this.ServerUri)
            {
                UseDomainChecks = false,
                UserIdentity = new UserIdentity(this.SecurityX509Certificate),
                UseSecureEndpoint = true,
                DisconnectTimeout = 10000,
                ReconnectTimeout = 10000,
                SessionTimeout = 60000,
                PreferredPolicy = new OpcSecurityPolicy(OpcSecurityMode.None, OpcSecurityAlgorithm.Auto, 0)
            };
        }

        #region Variables & Structures

        public Guid UidGuid { get; set; }
        public OpcClient Client { get; }
        public IPAddress IpAddress { private get; set; }
        public bool AcceptUntrustedCertificates { get; set; }
        public ushort PortNum { private get; set; }

        public X509Certificate2 SecurityX509Certificate { get; set; }

        public Uri ServerUri { get; private set; }

        public TreeView OpcNodes { get; set; }

        #endregion Variables & Structures

        private readonly SecurityConfiguration _securityConfiguration = new SecurityConfiguration();

        private void ConfigureSecurity()
        {
            _securityConfiguration.ApplicationCertificate = new CertificateIdentifier(this.SecurityX509Certificate);
            _securityConfiguration.Validate();
        }

        public void SetUri()
        {
            this.ServerUri = new Uri(Ports.OpcTcp + $"://{this.IpAddress}:{this.PortNum}/");
        }
    }

    public static class Ports
    {
        #region Variables & Structures

        public const string OpcTcp = "opc.tcp";
        public const string NetTcp = "net.tcp";
        public const string Http = "http";
        public const string Https = "https";

        #endregion Variables & Structures
    }

    public class Node
    {
        public Node()
        {
            IsChecked = false;
            NodeName = "";
            NodeType = null;
            NodeValue = "";
            NodeDescription = "";
        }
        public bool IsChecked { get; set; }
        public string NodeName { get; set; }
        public Type NodeType { get; set; }
        public string NodeValue { get; set; }
        public string NodeDescription { get; set; }
    }
}