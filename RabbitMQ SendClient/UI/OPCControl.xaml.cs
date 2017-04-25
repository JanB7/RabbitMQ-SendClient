namespace RabbitMQ_SendClient.UI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Media.Imaging;
    using General_Classes;
    using Opc.UaFx;
    using Opc.UaFx.Client;
    using static System.UInt16;
    using static SystemVariables;
    using static General_Classes.GlobalOpcFunctions;
    using MessageBox = System.Windows.Forms.MessageBox;

    /// <summary>
    /// Interaction logic for OPCControl.xaml 
    /// </summary>
    public partial class OPCControl : Window
    {
        public OPCControl()
        {
            InitializeComponent();
            Array.Resize(ref OpcUaServers, OpcUaServers.Length + 1);
            _lenght = OpcUaServers.Length - 1;
            OpcUaServers[_lenght] = new OpcUaServer
            {
                UidGuid = this.UidGuid
            };

            CboNodeSecurity.Items.Add(SecurityTypes.None);
            TvNodes = OpcUaServers[_lenght].OpcNodes;
        }

        #region Variables & Structures

        public Guid UidGuid { get; set; } = Guid.Empty;

        private enum SecurityTypes
        {
            None = OpcSecurityMode.None,
            Invalid = OpcSecurityMode.Invalid,
            Sign = OpcSecurityMode.Sign,
            SignAndEncrypt = OpcSecurityMode.SignAndEncrypt
        }

        private readonly int _lenght;

        private InfiniteList<OpcNodeInfo> NodeInfo = new InfiniteList<OpcNodeInfo>();

        #endregion Variables & Structures

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Array.Resize(ref OpcUaServers, OpcUaServers.Length - 1);
            Close();
        }

        private void CboCertificates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OpcUaServers[_lenght].AcceptUntrustedCertificates =
                (bool) ((ComboBoxItem) CboCertificates.Items[CboCertificates.SelectedIndex]).Content;
        }

        private void CboConnectionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var type = (OpcSecurityMode) Enum.Parse(typeof(SecurityTypes),
                ((ComboBoxItem) CboConnectionType.Items[CboConnectionType.SelectedIndex]).Content.ToString()
                .Replace(" ", string.Empty), true);
            OpcUaServers[_lenght].Client.PreferredPolicy = new OpcSecurityPolicy(type);
        }

        private void CboNodeSecurity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsInitialized) return;
            OpcUaServers[OpcUaServers.Length - 1].Client.PreferredPolicy =
                new OpcSecurityPolicy((OpcSecurityMode) Enum.Parse(typeof(OpcSecurityMode),
                        ((ComboBoxItem) CboNodeSecurity.Items[CboNodeSecurity.SelectedIndex]).Content.ToString(), true),
                    OpcSecurityAlgorithm.Auto, 0);
        }

        private void ConnectDisconnect_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (ImgConnection.Source.ToString().Contains("broken"))
            {
                var source = new Uri("../Icons/link.png");
                ImgConnection.Source = new BitmapImage(source);
            }
            else
            {
                var source = new Uri("../Icons/broken-link.png");
                ImgConnection.Source = new BitmapImage(source);
            }

            try
            {
                OpcUaServers[OpcUaServers.Length - 1].Client.Connect();
                var client = OpcUaServers[OpcUaServers.Length - 1].Client;
                foreach (var opcNodeInfo in client.BrowseNodes())
                {
                    var item = new TreeViewItem
                    {
                        Header = opcNodeInfo.Name.ToString(),

                        Tag = opcNodeInfo.NodeId.ToString()
                    };
                    item.Items.Add(null);

                    item.Expanded += Node_Expanded;

                    TvNodes.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, LogLevel.Error, new StackFrame(0));
            }
        }

        private void Node_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem) sender;

            if ((item.Items.Count != 1) || (item.Items[0] != null)) return;

            item.Items.Clear();
            var nodeId = (string) item.Tag;
            IList<OpcNodeInfo> nodeList;

            try
            {
                var client = OpcUaServers[_lenght].Client;
                if (client.State == OpcClientState.Connected)
                {
                    //Do Nothing
                }
                else
                {
                    client.Connect();
                }
                var node = client.BrowseNode(nodeId);
                GetNodes(node);
                var children = node.Children();
                nodeList = children as IList<OpcNodeInfo> ?? children.ToList();
            }
            catch
            {
                return;
            }

            foreach (var opcNodeInfo in nodeList)
            {
                var subItem = new TreeViewItem
                {
                    Header = opcNodeInfo.Name.ToString(),

                    Tag = opcNodeInfo.NodeId.ToString()
                };
                subItem.Items.Add(null);
                subItem.Expanded += Node_Expanded;

                item.Items.Add(subItem);
            }
        }

        private void IPSection_LostFocus(object sender, RoutedEventArgs e)
        {
            OpcUaServers[_lenght].IpAddress =
                IPAddress.Parse($"{TxtIpAddress1.Text}.{TxtIpAddress2.Text}.{TxtIpAddress3.Text}.{TxtIpAddress4.Text}");
            OpcUaServers[_lenght].SetUri();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
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
            OpcUaServers[_lenght].PortNum = Parse(TxtPortNumber.Text);
            OpcUaServers[_lenght].SetUri();
        }

        private void TxtPortNumber_TextChanged(object sender, TextCompositionEventArgs e)
        {
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
            txtNodeInfo.Text = OpcUaServers[_lenght].ServerUri.ToString();
        }

        private void Monitor_Checked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Monitor_Unchecked(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}