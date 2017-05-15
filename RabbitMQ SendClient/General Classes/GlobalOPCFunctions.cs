namespace RabbitMQ_SendClient.General_Classes
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Windows;
    using System.Windows.Controls;
    using Opc.Ua;
    using Opc.UaFx;
    using Opc.UaFx.Client;
    using UI;
    using static SystemVariables;

    internal static class GlobalOpcFunctions
    {
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

        public static Node GetNode(OpcNodeId nodeId, Guid uidGuid)
        {
            var index = 0;
            var Nodes = OpcUaServers[GetIndex<OpcUaServer>(uidGuid)].Nodes;
            foreach (var node in Nodes)
            {
                if (node.NodePath == nodeId.NamespaceUri.AbsolutePath)
                {
                    return Nodes[index];
                }
                index++;
            }
            return null;
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

        public static void GetNodes(Guid uidGuid, OpcNodeInfo node)
        {
            var index = GetIndex<OpcUaServer>(uidGuid);
            OpcUaServers[index].parentNode = node;

            var orignalOpcNodeInfo = OpcUaServers[index].OriginalOpcNodeInfo;
            var nodes = OpcUaServers[index].Nodes;

            Array.Resize(ref nodes, 0);
            foreach (var nde in node.Children())
            {
                try
                {
                    Array.Resize(ref nodes, nodes.Length + 1);
                    Array.Resize(ref orignalOpcNodeInfo, orignalOpcNodeInfo.Length + 1);
                    nodes[nodes.Length - 1] = new Node();

                    var nodeName = nde.Attribute(OpcAttribute.DisplayName | OpcAttribute.Value);
                    if (nde.Attribute(OpcAttribute.DisplayName) != null && nde.Attribute(OpcAttribute.DisplayName).BrowseName != "DisplayName")
                        nodes[nodes.Length - 1].NodeName = nde.Attribute(OpcAttribute.DisplayName).BrowseName;
                    else if (nde.Name.ToString() != string.Empty)
                        nodes[nodes.Length - 1].NodeName = nde.Name.BrowseName.Name;

                    if (nde.Attribute(OpcAttribute.Description) != null)
                        nodes[nodes.Length - 1].NodeDescription = nde.Attribute(OpcAttribute.Description).BrowseName;
                    else if (nde.Reference.ToString() != string.Empty)
                        nodes[nodes.Length - 1].NodeDescription = nde.Reference.Category.ToString();

                    if (nde.Attribute(OpcAttribute.DisplayName | OpcAttribute.Value) != null)
                        nodes[nodes.Length - 1].NodeValue = nde.Attribute(OpcAttribute.DisplayName | OpcAttribute.Value).Value.Value.ToString();
                    else if (nde.Category.ToString() != string.Empty)
                        nodes[nodes.Length - 1].NodeValue = nde.Category.ToString();

                    nodes[nodes.Length - 1].NodeType = nde.Attribute(OpcAttribute.DisplayName | OpcAttribute.Value) != null ? nde.Attribute(OpcAttribute.DisplayName | OpcAttribute.Value).Value.DataType.GetType() : nde.GetType();
                    if (nodes[nodes.Length - 1].NodeType == typeof(object)) nodes[nodes.Length].NodeType = null;

                    orignalOpcNodeInfo[orignalOpcNodeInfo.Length - 1] = nde;

                    if (nde.Attribute(OpcAttribute.NodeId).BrowseName != null && nde.Attribute(OpcAttribute.NodeId).BrowseName != "NodeId")
                        nodes[nodes.Length - 1].NodePath = nde.Attribute(OpcAttribute.NodeId).BrowseName;
                    else if (nde.NodeId.NamespaceUri != null && nde.NodeId.NamespaceUri.AbsolutePath != string.Empty)
                        nodes[nodes.Length - 1].NodePath = nde.NodeId.NamespaceUri.AbsolutePath;
                    else if (nde.NodeId.Namespace.Uri != null && nde.NodeId.Namespace.Uri.AbsolutePath != string.Empty)
                        nodes[nodes.Length - 1].NodePath = nde.NodeId.Namespace.Uri.AbsolutePath;
                    else if (!nde.NodeId.IsNull)
                        nodes[nodes.Length - 1].NodePath = nde.NodeId.Namespace + "#" + nde.NodeId.Value;
                }
                catch (Exception)
                {
                    return;
                }
                
            }
            OpcUaServers[index].Nodes = nodes;
            OpcUaServers[index].OriginalOpcNodeInfo = orignalOpcNodeInfo;
        }


        public static dynamic UpdateNodeValue(OpcNodeInfo node)
        {
            if (node.Attribute(OpcAttribute.DisplayName | OpcAttribute.Value) != null)
               return node.Attribute(OpcAttribute.DisplayName | OpcAttribute.Value).Value.Value.ToString();
            else if (node.Category.ToString() != string.Empty)
                return node.Category.ToString();

            return null;
        }
        

        public static void GetNodes(Guid uidGuid)
        {
            var index = GetIndex<OpcUaServer>(uidGuid);
            OpcUaServers[index].parentNode = OpcUaServers[index].Client.BrowseNode(OpcObjectTypes.ObjectsFolder);

            var nodes = OpcUaServers[index].Nodes;
            var orignalOpcNodeInfo = OpcUaServers[index].OriginalOpcNodeInfo ?? new OpcNodeInfo[0];

            Array.Resize(ref nodes, 0);

            var node = OpcUaServers[index].Client.BrowseNode(OpcObjectTypes.ObjectsFolder);

            foreach (var nde in node.Children())
            {
                Array.Resize(ref nodes, nodes.Length + 1);
                Array.Resize(ref orignalOpcNodeInfo, orignalOpcNodeInfo.Length + 1);
                nodes[nodes.Length - 1] = new Node();

                if (nde.Attribute(OpcAttribute.DisplayName) != null && nde.Attribute(OpcAttribute.DisplayName).BrowseName != "DisplayName")
                    nodes[nodes.Length - 1].NodeName = nde.Attribute(OpcAttribute.DisplayName).BrowseName;
                else if (nde.Name.ToString() != string.Empty)
                    nodes[nodes.Length - 1].NodeName = nde.Name.BrowseName.Name;

                if (nde.Attribute(OpcAttribute.Description) != null)
                    nodes[nodes.Length - 1].NodeDescription = nde.Attribute(OpcAttribute.Description).BrowseName;
                else if (nde.Reference.ToString() != string.Empty)
                    nodes[nodes.Length - 1].NodeDescription = nde.Reference.Category.ToString();

                if (nde.Attribute(OpcAttribute.Value) != null)
                    nodes[nodes.Length - 1].NodeValue = nde.Attribute(OpcAttribute.Value).BrowseName;
                else if (nde.Category.ToString() != string.Empty)
                    nodes[nodes.Length - 1].NodeValue = nde.Category.ToString();

                nodes[nodes.Length - 1].NodeType = Type.GetType(nodes[nodes.Length - 1].NodeValue);

                orignalOpcNodeInfo[orignalOpcNodeInfo.Length - 1] = nde;

                if (nde.Attribute(OpcAttribute.NodeId).BrowseName != null && nde.Attribute(OpcAttribute.NodeId).BrowseName != "NodeId")
                    nodes[nodes.Length - 1].NodePath = nde.Attribute(OpcAttribute.NodeId).BrowseName;
                else if (nde.NodeId.NamespaceUri != null && nde.NodeId.NamespaceUri.AbsolutePath != string.Empty)
                    nodes[nodes.Length - 1].NodePath = nde.NodeId.NamespaceUri.AbsolutePath;
                else if (nde.NodeId.Namespace.Uri != null && nde.NodeId.Namespace.Uri.AbsolutePath != string.Empty)
                    nodes[nodes.Length - 1].NodePath = nde.NodeId.Namespace.Uri.AbsolutePath;
                else if (!nde.NodeId.IsNull)
                    nodes[nodes.Length - 1].NodePath = nde.NodeId.Namespace + "#" + nde.NodeId.Value.ToString();
            }
            OpcUaServers[index].Nodes = nodes;
            OpcUaServers[index].OriginalOpcNodeInfo = orignalOpcNodeInfo;
        }
    }

    public class OpcUaServer
    {
        public OpcUaServer()
        {
            this.IpAddress = IPAddress.Loopback;
            this.PortNum = 49320;
            this.AcceptUntrustedCertificates = true;
            //this.UidGuid = new Guid();
            this.ServerUri = new Uri(Ports.OpcTcp + $"://{this.IpAddress}:{this.PortNum}/");
            OpcCertificateManager.AutoCreateCertificate = true;
            //ConfigureSecurity();
            SetUri();
            this.Client = new OpcClient(this.ServerUri)
            {
                UseDomainChecks = false,
                //UserIdentity = new UserIdentity(this.SecurityX509Certificate),
                UseSecureEndpoint = true,
                DisconnectTimeout = 10000,
                ReconnectTimeout = 10000,
                SessionTimeout = 60000,
                PreferredPolicy = new OpcSecurityPolicy(OpcSecurityMode.None, OpcSecurityAlgorithm.Auto, 0)
            };
        }
        public OpcNodeInfo[] OriginalOpcNodeInfo { get; set; }
        public Node[] Nodes { get; set; } = new Node[0];
        public TreeView TView { get; set; } = new TreeView();
        public OpcNodeInfo parentNode { get; set; }

        #region Variables & Structures

        public Guid? UidGuid { get; set; }
        public OpcClient Client { get; }

        public IPAddress IpAddress { private get; set; }
        public bool AcceptUntrustedCertificates { get; set; }
        public ushort PortNum { private get; set; }

        public X509Certificate2 SecurityX509Certificate { get; set; }

        public Uri ServerUri { get; private set; }

        public TreeView OpcNodes { get; set; }

        private readonly SecurityConfiguration _securityConfiguration = new SecurityConfiguration();

        #endregion Variables & Structures

        private void ConfigureSecurity()
        {
            _securityConfiguration.ApplicationCertificate = new CertificateIdentifier();
            _securityConfiguration.AutoAcceptUntrustedCertificates = true;
            _securityConfiguration.Validate();
            var certificate = _securityConfiguration.ApplicationCertificate.Certificate.ToString();

            using (var w = File.AppendText("Certificate.cer"))
            {
                w.Write(certificate);
                w.Close();
            }
        }

        public void SetUri()
        {
            this.ServerUri = new Uri(Ports.OpcTcp + $"://{this.IpAddress}:{this.PortNum}/");

            if (this.Client == null) return;
            this.Client.ServerAddress = this.ServerUri;
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
            this.IsChecked = false;
            this.NodeName = "";
            this.NodeType = null;
            this.NodeValue = "";
            this.NodeDescription = "";
        }

        #region Variables & Structures

        public bool IsChecked { get; set; }
        public string NodeName { get; set; }
        public Type NodeType { get; set; }
        public dynamic NodeValue { get; set; }
        public string NodeDescription { get; set; }
        public string NodePath { get; set; }

        #endregion Variables & Structures
    }
}