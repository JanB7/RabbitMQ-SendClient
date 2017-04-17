using static RabbitMQ_SendClient.GlobalRabbitMqServerFunctions;
using static RabbitMQ_SendClient.SystemVariables;

namespace RabbitMQ_SendClient
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Windows.Input;
    using KeyEventArgs = System.Windows.Input.KeyEventArgs;
    using MessageBox = System.Windows.Forms.MessageBox;

    /// <summary>
    /// Interaction logic for SetupServer.xaml 
    /// </summary>
    public partial class SetupServer
    {
        private static readonly StackTrace StackTracing = new StackTrace();

        public SetupServer(Guid uidGuid)
        {
            this.Index = GetIndex<RabbitServerInformation>(uidGuid);
            InitializeComponent();
            InitialzeServerSettings();
        }

        public int Index { get; set; } //number of array that is being configured

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (txtServerAddress.Text.ToLower() == "localhost")
                    txtServerAddress.Text = "127.0.0.1";
                else
                    IPAddress.Parse(txtServerAddress.Text);
            }
            catch (Exception)
            {
                try
                {
                    var uri = new Uri(txtServerAddress.Text);
                    txtServerAddress.Text = Dns.GetHostAddresses(uri.Host)[0].ToString();
                }
                catch (Exception ex)
                {
                    var helpFile = "https://www.google.ca/search?q=fully+qualified+domain+name";
                    MessageBox.Show(Properties.Resources.Invalid_Server_URL, ex.Source, MessageBoxButtons.OK,
                        MessageBoxIcon.Error, 0, 0, helpFile);
                    return; //Exits to allow correction of IP Address before opening Server
                }
            }

            //Sets Information for Server Configuration
            ServerInformation[ServerInformation.Length - 1].ServerAddress = IPAddress.Parse(txtServerAddress.Text);
            ServerInformation[ServerInformation.Length - 1].UserName = txtUserName.Text;
            ServerInformation[ServerInformation.Length - 1].Password = pwdPassword.Password;
            ServerInformation[ServerInformation.Length - 1].ServerPort = int.Parse(txtServerPort.Text);
            ServerInformation[ServerInformation.Length - 1].VirtualHost = txtVirtualHost.Text;
            ServerInformation[ServerInformation.Length - 1].ExchangeName = txtExchangeName.Text;
            ServerInformation[ServerInformation.Length - 1].ChannelName = txtChannelName.Text;
            ServerInformation[ServerInformation.Length - 1].ServerHeartbeat = (int)sldHeartBeat.Value;
            ServerInformation[ServerInformation.Length - 1].NetworkRecoveryInterval = (int)sldNetworokRecInt.Value;

            //Creates Connection
            try
            {
                StartServer();
                FactoryChannel[FactoryChannel.Length - 1].ConfirmSelect();
            }
            catch (Exception ex)
            {
                var msgboxResult = MessageBox.Show(ex.Message + Properties.Resources.SetupServer_btnOK_Click_YesToEdit,
                    ex.Source, MessageBoxButtons.YesNo, MessageBoxIcon.Stop);
                if (msgboxResult == System.Windows.Forms.DialogResult.Yes)
                    return;
                this.DialogResult = false;
                Close();
            }

            this.DialogResult = FactoryChannel[this.Index].IsOpen;

            Close();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            //Dispatcher.Invoke((MethodInvoker) InitialzeServerSettings);
            InitialzeServerSettings();
        }

        /// <summary>
        /// Enables/Disables Recovery. UI Access ONLY. Automatically disables current connection if
        /// system detects changes to settings.
        /// </summary>
        /// <param name="sender">
        /// ComboBox object in UI 
        /// </param>
        /// <param name="e">
        /// Value of ComboBox 
        /// </param>
        private void CboAutoRecovery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!this.IsInitialized) return;

            switch (cboAutoRecovery.SelectedIndex)
            {
                case 0:
                    ServerInformation[this.Index].AutoRecovery = true;
                    sldNetworokRecInt.IsEnabled = true;
                    break;

                case 1:
                    ServerInformation[this.Index].AutoRecovery = false;
                    sldNetworokRecInt.IsEnabled = false;
                    break;

                default:
                    goto case 0;
            }
        }

        /// <summary>
        /// Checkbox for generating a unique UUID to the system. 
        /// </summary>
        /// <param name="sender">
        /// Checkbox Object 
        /// </param>
        /// <param name="e">
        /// Checkbox Value 
        /// </param>
        private void GenerateChannel_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;

            try
            {
                if ((GenerateChannel.IsChecked != null) && GenerateChannel.IsChecked.Value) //True
                {
                    txtChannelName.IsEnabled = !GenerateChannel.IsChecked.Value;
                    txtChannelName.Text = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
                    ServerInformation[this.Index].ChannelName = txtChannelName.Text;
                }
                else if ((GenerateChannel.IsChecked != null) && !GenerateChannel.IsChecked.Value) //False
                {
                    txtChannelName.IsEnabled = !GenerateChannel.IsChecked.Value;
                    txtChannelName.Text = "Default";
                    ServerInformation[this.Index].ChannelName = txtChannelName.Text;
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
            }
        }

        /// <summary>
        /// Checkbox for generating a unique UUID to the system. 
        /// </summary>
        /// <param name="sender">
        /// Checkbox Object 
        /// </param>
        /// <param name="e">
        /// Checkbox Value 
        /// </param>
        private void GenerateExchange_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;
            try
            {
                if ((GenerateExchange.IsChecked != null) && GenerateExchange.IsChecked.Value) //True
                {
                    txtExchangeName.IsEnabled = !GenerateExchange.IsChecked.Value;
                    txtExchangeName.Text = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
                    ServerInformation[this.Index].ExchangeName = txtExchangeName.Text;
                }
                else if ((GenerateExchange.IsChecked != null) && !GenerateExchange.IsChecked.Value) //False
                {
                    txtExchangeName.IsEnabled = !GenerateExchange.IsChecked.Value;
                    txtExchangeName.Text = "Default";
                    ServerInformation[this.Index].ExchangeName = txtExchangeName.Text;
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
            }
        }

        /// <summary>
        /// puts preprogrammed values onto the form for consistent use. TODO replace with settings file 
        /// </summary>
        private void InitialzeServerSettings()
        {
            if (!this.IsInitialized) return;
            txtServerAddress.Text = ServerInformation[this.Index].ServerAddress.ToString();
            txtUserName.Text = ServerInformation[this.Index].UserName;
            pwdPassword.Password = ServerInformation[this.Index].Password;
            txtServerPort.Text = ServerInformation[this.Index].ServerPort.ToString();
            txtVirtualHost.Text = ServerInformation[this.Index].VirtualHost;
            txtExchangeName.Text = ServerInformation[this.Index].ExchangeName;
            txtChannelName.Text = ServerInformation[this.Index].ChannelName;
            sldHeartBeat.Value = ServerInformation[this.Index].ServerHeartbeat;
            sldNetworokRecInt.Value = ServerInformation[this.Index].NetworkRecoveryInterval;
            cboAutoRecovery.SelectedIndex = 0;
        }

        /// <summary>
        /// Provides an update to the password for the server. Automatically disables server on any
        /// password settings change.
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        private void PwdPassword_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!this.IsInitialized) return;
            ServerInformation[this.Index].Password = pwdPassword.Password;
        }

        /// <summary>
        /// Heartbeat to keep connection active to the RabbitMQ Server. Value Range (5-30) 
        /// </summary>
        /// <param name="sender">
        /// Slider Oboject 
        /// </param>
        /// <param name="e">
        /// Slider Value 
        /// </param>
        private void SldHeartBeat_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Prevents actions from occuring during initialization
            if (!this.IsInitialized) return;

            var value = (int)sldHeartBeat.Value;
            txtHeartbeat.Text = value.ToString();
            ServerInformation[this.Index].ServerHeartbeat = value;
        }

        /// <summary>
        /// Recovery Interval of the network. UI Access ONLY Automatically disables current
        /// connection if system detects changes to settings.
        /// </summary>
        /// <param name="sender">
        /// Slider Object in the UI 
        /// </param>
        /// <param name="e">
        /// Value of Slider 
        /// </param>
        private void SldNetworokRecInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Prevents actions from occuring during initialization
            if (!this.IsInitialized) return;
            try
            {
                var value = (int)sldNetworokRecInt.Value;

                NetworkRecIntervalTxt.Text = value.ToString();
                ServerInformation[this.Index].NetworkRecoveryInterval = value;
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
            }
        }

        private void TxtChannelName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!this.IsInitialized) return;

            ServerInformation[this.Index].ChannelName = "default." + txtChannelName.Text;
        }

        private void TxtExchangeName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!this.IsInitialized) return;

            ServerInformation[this.Index].ExchangeName = "default." + txtExchangeName.Text;
        }

        private void TxtServerAddress_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                TxtServerAddress_OnLostFocus(sender, e);
        }

        private void TxtServerAddress_OnLostFocus(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!this.IsInitialized) return;

            try
            {
                if (txtServerAddress.Text.ToLower() == "localhost")
                    txtServerAddress.Text = "127.0.0.1";
                else
                    IPAddress.Parse(txtServerAddress.Text);
            }
            catch (Exception)
            {
                try
                {
                    var uri = new Uri(txtServerAddress.Text);
                    txtServerAddress.Text = Dns.GetHostAddresses(uri.Host)[0].ToString();
                }
                catch (Exception ex)
                {
                    var helpFile = "https://www.google.ca/search?q=fully+qualified+domain+name";
                    MessageBox.Show(Properties.Resources.Invalid_Server_URL, ex.Source, MessageBoxButtons.OK,
                        MessageBoxIcon.Error, 0, 0, helpFile);
                }
            }
        }

        /// <summary>
        /// Address being changed. Do nothing. Checking should be done at the end. 
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        private void TxtServerAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            //do Nothing! checking happens on commit with either focus loss or enter
        }

        private void TxtServerPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!this.IsInitialized) return;

            ServerInformation[this.Index].ServerPort = Convert.ToInt32(txtServerPort.Text);
        }

        private void TxtUserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!this.IsInitialized) return;

            ServerInformation[this.Index].UserName = txtUserName.Text;
        }

        private void TxtVirtualHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.IsInitialized) return;

            ServerInformation[this.Index].VirtualHost = txtVirtualHost.Text;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }
    } //End of Class
}