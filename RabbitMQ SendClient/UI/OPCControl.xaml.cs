namespace RabbitMQ_SendClient.UI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using General_Classes;
    using Opc.UaFx;
    using Opc.UaFx.Client;
    using static General_Classes.GlobalOpcFunctions;
    using static System.UInt16;
    using static SystemVariables;
    using CheckBox = System.Windows.Controls.CheckBox;
    using MessageBox = System.Windows.Forms.MessageBox;
    using TextBox = System.Windows.Controls.TextBox;
    using TreeView = System.Windows.Controls.TreeView;

    /// <summary>
    ///     Interaction logic for OPCControl.xaml
    /// </summary>
    public partial class OPCControl : Window
    {
        public OPCControl()
        {
            InitializeComponent();
            if (this.IsEdit) //Editing node, do not recreate
            {
                _index = GetIndex<OpcUaServer>(this.UidGuid);
            }
            else
            {
                if (this.UidGuid == Guid.Empty) this.UidGuid = Guid.NewGuid();
                Array.Resize(ref OpcUaServers, OpcUaServers.Length + 1);
                _index = OpcUaServers.Length - 1;
                OpcUaServers[_index] = new OpcUaServer {UidGuid = this.UidGuid};

                CboNodeSecurity.Items.Add(SecurityTypes.None);
                _tvNodes = OpcUaServers[_index].OpcNodes;
            }

            var path = AppDomain.CurrentDomain.BaseDirectory + "\\Icons\\broken-link.png";
            var source = new Uri(path, UriKind.Relative);
            BtnConnectDisconnect.Content = new BitmapImage(source);
        }

        #region Variables & Structures

        public Guid UidGuid { get; set; }
        public bool IsEdit { private get; set; }
        private bool Connecting { get; set; }

        private enum SecurityTypes
        {
            None = OpcSecurityMode.None,
            Invalid = OpcSecurityMode.Invalid,
            Sign = OpcSecurityMode.Sign,
            SignAndEncrypt = OpcSecurityMode.SignAndEncrypt
        }

        private readonly DispatcherTimer _autoUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500),
            IsEnabled = false
        };

        private readonly int _index;

        private TreeView _tvNodes = new TreeView();

        #endregion

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            if (this.IsEdit) Close(); //Do not resize
            Array.Resize(ref OpcUaServers, _index);
            Close();
        }

        private void CboCertificates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsInitialized) return;
            OpcUaServers[_index].AcceptUntrustedCertificates =
                (bool) ((ComboBoxItem) CboCertificates.Items[CboCertificates.SelectedIndex]).Content;
        }

        private void CboConnectionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsInitialized) return;
            var type = (OpcSecurityMode) Enum.Parse(typeof(SecurityTypes),
                ((ComboBoxItem) CboConnectionType.Items[CboConnectionType.SelectedIndex]).Content.ToString()
                .Replace(" ", string.Empty), true);
            OpcUaServers[_index].Client.PreferredPolicy = new OpcSecurityPolicy(type);
        }

        private void CboNodeSecurity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsInitialized) return;
            OpcUaServers[_index].Client.PreferredPolicy =
                new OpcSecurityPolicy((OpcSecurityMode) Enum.Parse(typeof(OpcSecurityMode),
                        ((ComboBoxItem) CboNodeSecurity.Items[CboNodeSecurity.SelectedIndex]).Content.ToString(), true),
                    OpcSecurityAlgorithm.Auto, 0);
        }

        private void ConnectDisconnect_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (BtnConnectDisconnect.Content.ToString().Contains("broken"))
            {
                var path = AppDomain.CurrentDomain.BaseDirectory + "Icons\\link.png";
                var source = new Uri(path, UriKind.Relative);
                BtnConnectDisconnect.Content = new BitmapImage(source);

                try
                {
                    OpcUaServers[_index].Client.Connected += Client_Connected;
                    OpcUaServers[_index].Client.StateChanged += ClientOnStateChanged;

                    if (!ConnectClient()) return;

                    GetNodes(OpcUaServers[_index].UidGuid.Value);
                    if (_tvNodes == null)
                        _tvNodes = new TreeView();
                    NodeList.Items.Clear();

                    foreach (var opcNodeInfo in OpcUaServers[_index].Nodes)
                    {
                        var item = new TreeViewItem
                        {
                            Header = opcNodeInfo.NodeName,

                            Tag = opcNodeInfo.NodePath
                        };
                        item.Items.Add(null);

                        item.Expanded += Node_Expanded;

                        _tvNodes.Items.Add(item);

                        var node = new OpcNodes
                        {
                            NodeDescription = opcNodeInfo.NodeDescription,
                            Monitor = false,
                            NodeName = opcNodeInfo.NodeName,
                            NodeType = opcNodeInfo.NodeType,
                            NodeValue = opcNodeInfo.NodeValue
                        };
                        NodeList.Items.Add(node);
                    }
                    TreeViewNodes.ItemsSource = _tvNodes.Items;

                    _autoUpdateTimer.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    LogError(ex, LogLevel.Error, new StackFrame(0));
                    path = AppDomain.CurrentDomain.BaseDirectory + "Icons\\broken-link.png";
                    source = new Uri(path, UriKind.Relative);
                    BtnConnectDisconnect.Content = new BitmapImage(source);
                }
            }
            else
            {
                var path = AppDomain.CurrentDomain.BaseDirectory + "\\Icons\\broken-link.png";
                var source = new Uri(path, UriKind.Relative);
                BtnConnectDisconnect.Content = new BitmapImage(source);

                OpcUaServers[_index].Client.Disconnect();
            }
        }

        private void ClientOnStateChanged(object sender, OpcClientStateChangedEventArgs opcClientStateChangedEventArgs)
        {
            switch (opcClientStateChangedEventArgs.NewState)
            {
                case OpcClientState.Disconnected:
                    _autoUpdateTimer.IsEnabled = false;
                    break;

                case OpcClientState.Connected:
                    MessageBox.Show(opcClientStateChangedEventArgs.NewState.ToString());
                    break;
            }
        }

        private bool ConnectClient()
        {
            var client = OpcUaServers[_index].Client;

            var result = System.Windows.Forms.DialogResult.Retry;
            _autoUpdateTimer.Tick += AutoUpdateTimerOnTick;
            while (result == System.Windows.Forms.DialogResult.Retry)
            {
                if (this.Connecting) continue;
                this.Connecting = true;
                try
                {
                    if (client.State == OpcClientState.Connected) return true;
                    client.Configuration.CertificateManager.ValidateCertificate();
                    client.UseSecureEndpoint = false;
                    client.CertificateValidationFailed += CertificateFialed;
                    client.Connect();
                    this.Connecting = false;
                    return true;
                }
                catch (Exception ex)
                {
                    result = MessageBox.Show($"Unable to connect.\n{ex.Message}", @"Unable to connect",
                        MessageBoxButtons.RetryCancel, MessageBoxIcon.Stop);
                    if (result != System.Windows.Forms.DialogResult.Cancel) continue;
                    this.Connecting = false;
                    return false;
                }
            }
            this.Connecting = false;

            return false;
        }

        private void AutoUpdateTimerOnTick(object sender, EventArgs eventArgs)
        {
            if (OpcUaServers[_index].Client.State != OpcClientState.Connected) return;

            var children = OpcUaServers[_index].Client.BrowseNode(OpcUaServers[_index].parentNode.NodeId).Children();
            var i = 0;

            foreach (var child in children)
            {
                if (child.Attribute(OpcAttribute.Value) != null)
                    ((OpcNodes) NodeList.Items[i]).NodeValue = child.Attribute(OpcAttribute.Value).BrowseName;
                else if (child.Category.ToString() != string.Empty)
                    ((OpcNodes) NodeList.Items[i]).NodeValue = child.Category.ToString();
            }
            NodeList.Items.Refresh();
        }

        private static void CertificateFialed(object sender, OpcCertificateValidationFailedEventArgs args)
        {
            var certificate = args;
            var result = MessageBox.Show($"Trust Certificate?\n\n{certificate.Certificate}", "Trust Certificate",
                MessageBoxButtons.YesNo, MessageBoxIcon.Stop);
            certificate.Accept = result == System.Windows.Forms.DialogResult.Yes;
        }

        private void Client_Connected(object sender, EventArgs e)
        {
            UpdateSubscriptions((OpcClient) sender);

            //GetNodes((OpcClient)sender);
        }

        private void Node_Expanded(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized || (OpcUaServers[_index].Client.State != OpcClientState.Connected))
            {
                MessageBox.Show(@"Connection to Server Failed. Please connect and try again.", @"Connection Closed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var item = (TreeViewItem) sender;

            if (item.Items[0] != null)
            {
                //Return
            }
            else if (item.Items.Count != 1)
            {
                item.Items.Clear();
                var nodeId = (string) item.Tag;

                var info = OpcUaServers[_index]
                    .OriginalOpcNodeInfo
                    .First(element => (element.NodeId.Namespace + $"#" + element.NodeId.Value) == nodeId);
                GetNodes(OpcUaServers[_index].UidGuid.Value, info);

                if (_tvNodes == null)
                    _tvNodes = new TreeView();

                NodeList.Items.Clear();
                foreach (var opcNodeInfo in OpcUaServers[_index].Nodes)
                {
                    var items = new TreeViewItem
                    {
                        Header = opcNodeInfo.NodeName,

                        Tag = opcNodeInfo.NodePath
                    };
                    items.Items.Add(null);

                    items.Expanded += Node_Expanded;
                    items.Selected += Node_Expanded;

                    item.Items.Add(items);

                    var node = new OpcNodes
                    {
                        NodeDescription = opcNodeInfo.NodeDescription,
                        Monitor = false,
                        NodeName = opcNodeInfo.NodeName,
                        NodeType = opcNodeInfo.NodeType,
                        NodeValue = opcNodeInfo.NodeValue
                    };
                    NodeList.Items.Add(node);
                }
                TreeViewNodes.ItemsSource = _tvNodes.Items;
            }
            else
            {
                var nodeId = (string) item.Tag;

                var info = OpcUaServers[_index]
                    .OriginalOpcNodeInfo
                    .First(element => (element.NodeId.Namespace + $"#" + element.NodeId.Value) == nodeId);
                GetNodes(OpcUaServers[_index].UidGuid.Value, info);

                NodeList.Items.Clear();

                foreach (var opcNodeInfo in OpcUaServers[_index].Nodes)
                {
                    var node = new OpcNodes
                    {
                        NodeDescription = opcNodeInfo.NodeDescription,
                        Monitor = false,
                        NodeName = opcNodeInfo.NodeName,
                        NodeType = opcNodeInfo.NodeType,
                        NodeValue = opcNodeInfo.NodeValue
                    };
                    NodeList.Items.Add(node);
                }
            }
        }

        private void IPSection_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;
            for (var index = 0; index < 5; index++)
            {
                var textBox = (TextBox) FindName("TxtIpAddress" + index);
                if ((textBox != null) && (textBox.Text == string.Empty))
                    textBox.Text = "255";
                if ((textBox != null) && (int.Parse(textBox.Text) > 255))
                    textBox.Text = "255";
                else if ((textBox != null) && (int.Parse(textBox.Text) < 0))
                    textBox.Text = "0";
            }
            OpcUaServers[_index].IpAddress =
                IPAddress.Parse($"{TxtIpAddress1.Text}.{TxtIpAddress2.Text}.{TxtIpAddress3.Text}.{TxtIpAddress4.Text}");
            OpcUaServers[_index].SetUri();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            if (!this.IsInitialized) return;
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            for (var tries = 0; tries < 11; tries++)
            {
                if (OpcUaServers[_index].Client.State == OpcClientState.Connected) break;
                try
                {
                    OpcUaServers[_index].Client.Connect();
                }
                catch (Exception ex)
                {
                    LogError(ex, LogLevel.Warning, new StackFrame(0));
                    break;
                }
                Thread.Sleep(10);
            }
            this.DialogResult = true;
            Close();
        }

        private void OPCControl_OnLoaded(object sender, RoutedEventArgs e)
        {
        }

        private static bool? PortOutOfBounds()
        {
            var result = MessageBox.Show(@"Port out of bounds. Please enter a number between 1-65535", @"Out of Range",
                MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Hand);

            if (result == System.Windows.Forms.DialogResult.Retry)
                return true;
            if (result == System.Windows.Forms.DialogResult.Abort)
                return null;
            return false;
        }

        private void TxtPortNumber_OnLostFocus(object sender, RoutedEventArgs e)
        {
            OpcUaServers[_index].PortNum = Parse(TxtPortNumber.Text);
            OpcUaServers[_index].SetUri();
        }

        private void TxtPortNumber_TextChanged(object sender, TextCompositionEventArgs e)
        {
            if (!this.IsInitialized) return;
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);

            try
            {
                if (TryParse(TxtPortNumber.Text, out ushort result)) return;
                var outOfBounds = PortOutOfBounds();
                if (outOfBounds == null)
                    TxtPortNumber.Text = "49320";
                else if (outOfBounds == true)
                    TxtPortNumber.Text = "";
            }
            catch (Exception)
            {
                var outOfBounds = PortOutOfBounds();
                if (outOfBounds == null)
                    TxtPortNumber.Text = "49320";
                else if (outOfBounds == true)
                    TxtPortNumber.Text = "";
            }
        }

        private void MonitorTab_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;
            txtNodeInfo.Text = OpcUaServers[_index].ServerUri.ToString();
        }

        private void Monitor_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;
            var item = (CheckBox) e.OriginalSource;
            var index = NodeList.SelectedIndex;

            MonitorNodesInitialize();
            if (item.IsChecked != null) OpcUaServers[_index].Nodes[index].IsChecked = item.IsChecked.Value;
            MonitorNodes[_index].NodePath.Add(((Node) NodeList.Items[index]).NodePath);

            UpdateSubscriptions(OpcUaServers[_index].Client);
        }

        private void MonitorNodesInitialize()
        {
            if (GetIndex<NodesToMonitor>(this.UidGuid) == -1)
            {
                var monitorNodes = MonitorNodes;
                Array.Resize(ref monitorNodes, MonitorNodes.Length + 1);
                monitorNodes[monitorNodes.Length - 1] = new NodesToMonitor
                {
                    UidGuid = this.UidGuid,
                    NodePath = new List<string>()
                };
                MonitorNodes = monitorNodes;
            }
        }

        private void UpdateSubscriptions(OpcClient client)
        {
            MonitorNodesInitialize();
            var index = GetIndex<OpcUaServer>(this.UidGuid);

            foreach (var nodeToMonitor in MonitorNodes[index].NodePath)
            {
                OpcUaServers[index].Client.SubscribeNode(nodeToMonitor);
                OpcUaServers[_index]
                    .Client
                    .SubscribeDataChange(nodeToMonitor, MainWindow.HandleIsStartedChanged); //Enable for each node
            }
        }

        private void Monitor_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;
            var item = (CheckBox) e.OriginalSource;
            var index = NodeList.SelectedIndex;

            if (item.IsChecked != null) OpcUaServers[_index].Nodes[index].IsChecked = item.IsChecked.Value;
            OpcUaServers[_index]
                .Client.SubscribeDataChange(((Node) NodeList.Items[index]).NodePath, BlankHandleIsStartedChanged)
                .Unsubscribe();
            MonitorNodes[_index].NodePath.Remove(((Node) NodeList.Items[index]).NodePath);

            if (MonitorNodes[_index].NodePath.Count == 0)
                RemoveAtIndex<NodesToMonitor>(_index, MonitorNodes);
        }

        private static void BlankHandleIsStartedChanged(object sender, OpcDataChangeEventArgs e)
        {
            //Do Nothing, unsubscribed
        }

        private void TXTIpAddress_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox) sender;

            textBox.Text = "";
        }
    }

    public class OpcNodes
    {
        #region Variables & Structures

        public bool Monitor { get; set; }
        public string NodeName { get; set; }
        public dynamic NodeValue { get; set; }
        public Type NodeType { get; set; }
        public string NodeDescription { get; set; }

        #endregion
    }
}