using System;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using PostSharp.Patterns.Diagnostics;
using static RabbitMQ_SendClient.SystemVariables;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using MessageBox = System.Windows.Forms.MessageBox;


namespace RabbitMQ_SendClient
{
    /// <summary>
    ///     Interaction logic for SetupServer.xaml
    /// </summary>
    public partial class SetupServer : Window
    {
        private static readonly StackTrace StackTracing = new StackTrace();

        public SetupServer()
        {
            InitializeComponent();
            InitialzeServerSettings();

            //RabbitMQ Server Configuration
            cboAutoRecovery.SelectedIndex = 0;
        }

        public int Index { get; set; } //number of array that is being configured

        /// <summary>
        ///     puts preprogrammed values onto the form for consistent use.
        ///     TODO replace with settings file
        /// </summary>
        private void InitialzeServerSettings()
        {
            if (!IsInitialized) return;
            txtServerAddress.Text = ServerInformation[Index].ServerAddress.ToString();
            txtUserName.Text = ServerInformation[Index].UserName;
            pwdPassword.Password = ServerInformation[Index].Password;
            txtServerPort.Text = ServerInformation[Index].ServerPort.ToString();
            txtVirtualHost.Text = ServerInformation[Index].VirtualHost;
            txtExchangeName.Text = ServerInformation[Index].ExchangeName;
            txtChannelName.Text = ServerInformation[Index].ChannelName;
            sldHeartBeat.Value = ServerInformation[Index].ServerHeartbeat;
            sldNetworokRecInt.Value = ServerInformation[Index].NetworkRecoveryInterval;
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke((MethodInvoker)InitialzeServerSettings);
        }

        /// <summary>
        ///     Heartbeat to keep connection active to the RabbitMQ Server. Value Range (5-30)
        /// </summary>
        /// <param name="sender">Slider Oboject</param>
        /// <param name="e">Slider Value</param>
        private void SldHeartBeat_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var value = (int)sldHeartBeat.Value;
            txtHeartbeat.Text = value.ToString();
            ServerInformation[Index].ServerHeartbeat = value;
        }

        /// <summary>
        ///     Enables/Disables Recovery. UI Access ONLY.
        ///     Automatically disables current connection if system detects changes to settings.
        /// </summary>
        /// <param name="sender">ComboBox object in UI</param>
        /// <param name="e">Value of ComboBox</param>
        [Log]
        private void CboAutoRecovery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            switch (cboAutoRecovery.SelectedIndex)
            {
                case 0:
                    ServerInformation[Index].AutoRecovery = true;
                    sldNetworokRecInt.IsEnabled = true;
                    break;

                case 1:
                    ServerInformation[Index].AutoRecovery = false;
                    sldNetworokRecInt.IsEnabled = false;
                    break;

                default:
                    goto case 0;
            }
        }

        /// <summary>
        ///     Recovery Interval of the network. UI Access ONLY
        ///     Automatically disables current connection if system detects changes to settings.
        /// </summary>
        /// <param name="sender">Slider Object in the UI</param>
        /// <param name="e">Value of Slider</param>
        private void SldNetworokRecInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;
            try
            {
                var value = (int)sldNetworokRecInt.Value;

                NetworkRecIntervalTxt.Text = value.ToString();
                ServerInformation[Index].NetworkRecoveryInterval = value;
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        /// <summary>
        ///     Checkbox for generating a unique UUID to the system.
        /// </summary>
        /// <param name="sender">Checkbox Object</param>
        /// <param name="e">Checkbox Value</param>
        private void GenerateChannel_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            try
            {
                if (GenerateChannel.IsChecked != null && GenerateChannel.IsChecked.Value) //True
                {
                    txtChannelName.IsEnabled = !GenerateChannel.IsChecked.Value;
                    txtChannelName.Text = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
                    ServerInformation[Index].ChannelName = txtChannelName.Text;
                }
                else if (GenerateChannel.IsChecked != null && !GenerateChannel.IsChecked.Value) //False
                {
                    txtChannelName.IsEnabled = !GenerateChannel.IsChecked.Value;
                    txtChannelName.Text = "Default";
                    ServerInformation[Index].ChannelName = txtChannelName.Text;
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

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
                }
            }

            ServerInformation[Index].ServerAddress = IPAddress.Parse(txtServerAddress.Text);
            ServerInformation[Index].UserName = txtUserName.Text;
            ServerInformation[Index].Password = pwdPassword.Password;
            ServerInformation[Index].ServerPort = int.Parse(txtServerPort.Text);
            ServerInformation[Index].VirtualHost = txtVirtualHost.Text;
            ServerInformation[Index].ExchangeName = txtExchangeName.Text;
            ServerInformation[Index].ChannelName = txtChannelName.Text;
            ServerInformation[Index].ServerHeartbeat = (int)sldHeartBeat.Value;
            ServerInformation[Index].NetworkRecoveryInterval = (int)sldNetworokRecInt.Value;
            DialogResult = true;
            Close();
        }

        /// <summary>
        ///     Checkbox for generating a unique UUID to the system.
        /// </summary>
        /// <param name="sender">Checkbox Object</param>
        /// <param name="e">Checkbox Value</param>
        private void GenerateExchange_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;
            try
            {
                if (GenerateExchange.IsChecked != null && GenerateExchange.IsChecked.Value) //True
                {
                    txtExchangeName.IsEnabled = !GenerateExchange.IsChecked.Value;
                    txtExchangeName.Text = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
                    ServerInformation[Index].ExchangeName = txtExchangeName.Text;
                }
                else if (GenerateExchange.IsChecked != null && !GenerateExchange.IsChecked.Value) //False
                {
                    txtExchangeName.IsEnabled = !GenerateExchange.IsChecked.Value;
                    txtExchangeName.Text = "Default";
                    ServerInformation[Index].ExchangeName = txtExchangeName.Text;
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        /// <summary>
        ///     Provides an update to the password for the server.
        ///     Automatically disables server on any password settings change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PwdPassword_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;
            ServerInformation[Index].Password = pwdPassword.Password;
        }

        private void TxtChannelName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            ServerInformation[Index].ChannelName = "default." + txtChannelName.Text;
        }

        private void TxtExchangeName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            ServerInformation[Index].ExchangeName = "default." + txtExchangeName.Text;
        }

        private void TxtServerAddress_OnLostFocus(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

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

        private void TxtServerAddress_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter )
                TxtServerAddress_OnLostFocus(sender, e);
        }

        private void TxtVirtualHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) return;

            ServerInformation[Index].VirtualHost = txtVirtualHost.Text;
        }

        private void TxtUserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            ServerInformation[Index].UserName = txtUserName.Text;
        }

        private void TxtServerPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;


            ServerInformation[Index].ServerPort = Convert.ToInt32(txtServerPort.Text);
        }

        /// <summary>
        ///     Address being changed. Do nothing. Checking should be done at the end.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtServerAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            //do Nothing! checking happens on commit with either focus loss or enter
        }
    }//End of Class
}