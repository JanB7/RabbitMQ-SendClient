using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Newtonsoft.Json;
using PostSharp.Patterns.Diagnostics;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using static RabbitMQ_SendClient.SystemVariables;
using CheckBox = System.Windows.Controls.CheckBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.Forms.MessageBox;
using StackTrace = System.Diagnostics.StackTrace;

namespace RabbitMQ_SendClient
{
    /// <summary>
    ///     Main UI for RabbitMQ Client
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private static readonly List<SerialPort> SerialPorts = new List<SerialPort>();
        private static readonly List<CheckedListItem> AvailableSerialPorts = new List<CheckedListItem>();

        private static readonly StackTrace StackTrace = new StackTrace();

        private static IConnectionFactory _factory = new ConnectionFactory();
        private static IConnection _connection;
        private static IModel _channel;

        private static readonly string DeviceName = Environment.MachineName;

        /// <summary>
        /// RabbitMQ Server Information for setup
        /// </summary>
        internal static RabbitServerInformation RabbitServerSetup = new RabbitServerInformation
        {
            ServerAddress = IPAddress.Parse("130.113.130.194"),
            ExchangeName = "default.Default",
            ChannelName = "default.Default",
            UserName = "User",
            Password = "Factory1",
            VirtualHost = "default",
            ServerPort = 5672,
            ServerHeartbeat = 30,
            Encoding = "UTF8",
            MessageType = "Serial",
            MessageFormat = "jsonObject",
            AutoRecovery = true,
            NetworkRecoveryInterval = 5
        };

        /// <summary>
        /// All COM Ports available to the system
        /// </summary>
        private readonly SerialCommunication[] _serialSetup = new SerialCommunication[SerialPort.GetPortNames().Length];

        private readonly DispatcherTimer _systemTimer = new DispatcherTimer();

        /// <summary>
        /// Mainline Executable to the RabbitMQ Client
        /// </summary>
        [Log]
        public MainWindow()
        {
            for (var i = 0; i < SerialPort.GetPortNames().Length; i++)
            {
                var serialPort = new SerialPort(SerialPort.GetPortNames()[i]);
                SerialPorts.Add(serialPort);
                var serialPortCheck = new CheckedListItem
                {
                    Id = i,
                    IsChecked = false,
                    Name = SerialPort.GetPortNames()[i]
                };
                AvailableSerialPorts.Add(serialPortCheck);
            }

            InitializeComponent();

            //RabbitMQ Server Configuration
            cboAutoRecovery.SelectedIndex = 0;

            SetupSerial(SerialPort.GetPortNames());

            InitializeSerialPorts();
            InitializeHeartBeatTimer();
        }

        /// <summary>
        /// Close all open channels and serial ports before system closing
        /// </summary>
        public void Dispose()
        {
            if (_connection.IsOpen)
            {
                if (_channel.IsOpen)
                    _channel.Close();
                _connection.Close();
            }

            foreach (var serialPort in SerialPorts)
            {
                if (serialPort.IsOpen)
                    while (!serialPort.IsOpen)
                        serialPort.Close();

                serialPort.Dispose();
            }

            //Disposes of timer in a threadsafe manner
            if (_systemTimer.IsEnabled)
                _systemTimer.Stop();
        }

        [Log]
        private void InitializeSerialPorts()
        {
            var ports = SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                lstSerial.Items.Add("No Ports Available");
                lstModbusSerial.Items.Add("No Ports Available");
            }
            else
            {
                try
                {
                    var index = 0;
                    foreach (var port in ports)
                    {
                        var serialComm = new SerialCommunication
                        {
                            ComPort = port,
                            BaudRate = BaudRates.BaudRate9600,
                            SerialBits = 8,
                            SerialParity = Parity.None,
                            SerialStopBits = StopBits.One,
                            FlowControl = Handshake.None,
                            RtsEnable = false,
                            ReadTimeout = 250
                        };

                        _serialSetup[index] = serialComm;
                        var serialPort = new SerialPort(port);
                        SerialPorts.Add(serialPort);
                        index++;
                    }
                }
                catch (Exception e)
                {
                    var message = e.Message + "\nError in Port Enumeration";
                    MessageBox.Show(message, e.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// RabbitMQ Heartbeat Timer. Adjusts value of system information in scrollbar on tick
        /// </summary>
        [Log]
        private void InitializeHeartBeatTimer()
        {
            try
            {
                _systemTimer.Tick += SystemTimerOnTick;
                _systemTimer.Interval = TimeSpan.FromMilliseconds(500);
            }
            catch (Exception e)
            {
                var message = e.Message + "\nError in Timer Initialization";
                MessageBox.Show(message, e.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Updates infomration on Statusbar on what system is exeperiencing.
        /// </summary>
        /// <param name="sender">System Timer Thread Object</param>
        /// <param name="eventArgs">Timer Arguments</param>
        private void SystemTimerOnTick(object sender, EventArgs eventArgs)
        {
            //Prevents code from running before intialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && !_connection.IsOpen && srvEnabled.IsChecked.Value)
                txtServStatus.Text = "Server Status: Recovering";
            else
                txtServStatus.Text = _connection.IsOpen ? "Server Status: Connected" : "Server Status: Disconnected";
        }

        /// <summary>
        /// Publishes Message to RabbitMQ server using JSON format
        /// </summary>
        /// <param name="message">JSON type Message. HAS TO BE PREFORMATTED</param>
        /// <param name="exchangeName">Exchange that this will be published to</param>
        /// <returns>Message success state</returns>
        protected static bool PublishMessage(JsonObject message, string exchangeName)
        {
            bool success;
            try
            {
                var output = JsonConvert.SerializeObject(message);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "jsonObject";
                var address = new PublicationAddress(ExchangeType.Direct, exchangeName, "");
                _channel.BasicPublish(address, properties, Encoding.UTF8.GetBytes(output));

                success = true;
            }
            catch (Exception ex)
            {
                //Log Message
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Creates Exchange in RabbitMQ for tying channel to to allow successful message delivery
        /// </summary>
        /// <param name="exchangeName">Exchange Name</param>
        /// <param name="exchangeType">Manner in which Exchange behaves (Direct/Fanout/Headers/Topic)</param>
        /// <param name="exchangeDurability">Exchange Deleted if system shuts down</param>
        /// <param name="autoDelete">Exchange Deleted if no broker is connected</param>
        private static void CreateExchange(string exchangeName, string exchangeType, bool exchangeDurability,
            bool autoDelete)
        {
            try
            {
                switch (exchangeType)
                {
                    case "direct":
                    case "Direct":
                        goto default;
                    case "fanout":
                    case "Fanout":
                        _channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout, exchangeDurability, autoDelete,
                            null);
                        break;

                    case "headers":
                    case "Headers":
                        _channel.ExchangeDeclare(exchangeName, ExchangeType.Headers, exchangeDurability, autoDelete,
                            null);
                        break;

                    case "topic":
                    case "Topic":
                        _channel.ExchangeDeclare(exchangeName, ExchangeType.Topic, exchangeDurability, autoDelete,
                            null);
                        break;

                    default:
                        _channel.ExchangeDeclare(exchangeName, ExchangeType.Direct, exchangeDurability, autoDelete,
                            null);
                        break;
                }
            }
            catch (Exception ex)
            {
                //Log Message
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        /// <summary>
        /// Creates Channel to allow queue to be formed for messaging
        /// </summary>
        /// <param name="queueName">Channel Name</param>
        /// <param name="queueDurable">Channel Deleted or not when system shut down</param>
        /// <param name="queueAutoDelete">Channel Deleted if no broker is connected</param>
        private static void CreateQueue(string queueName, bool queueDurable, bool queueAutoDelete)
        {
            try
            {
                _channel.QueueDeclare(queueName, queueDurable, false, queueAutoDelete, null);
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        /// <summary>
        /// Binds predefined channel and exchange
        /// </summary>
        /// <param name="queueName">Channel Name</param>
        /// <param name="exchangeName">Exchange Name</param>
        private static void QueueBind(string queueName, string exchangeName)
        {
            try
            {
                _channel.QueueBind(queueName, exchangeName, "");
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        /// <summary>
        /// Setup connection to RabbitMQ server
        /// </summary>
        /// <returns>Connection Factory</returns>
        private static ConnectionFactory SetupFactory()
        {
            var factory = new ConnectionFactory
            {
                HostName = RabbitServerSetup.ServerAddress.ToString(),
                UserName = RabbitServerSetup.UserName,
                Password = RabbitServerSetup.Password,
                VirtualHost = RabbitServerSetup.VirtualHost,
                Port = RabbitServerSetup.ServerPort,
                AutomaticRecoveryEnabled = RabbitServerSetup.AutoRecovery,
                RequestedHeartbeat = (ushort)RabbitServerSetup.ServerHeartbeat,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(RabbitServerSetup.NetworkRecoveryInterval),
                TopologyRecoveryEnabled = RabbitServerSetup.AutoRecovery
            };
            return factory;
        }

        /// <summary>
        /// Address being changed. Do nothing. Checking should be done at the end.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtServerAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            //do Nothing! checking happens on commit with either focus loss or enter
        }

        /// <summary>
        /// Recovery Interval of the network. UI Access ONLY
        /// Automatically disables current connection if system detects changes to settings.
        /// </summary>
        /// <param name="sender">Slider Object in the UI</param>
        /// <param name="e">Value of Slider</param>
        private void sldNetworokRecInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }
            try
            {
                var value = (int)sldNetworokRecInt.Value;

                NetworkRecIntervalTxt.Text = value.ToString();
                RabbitServerSetup.NetworkRecoveryInterval = value;
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        /// <summary>
        /// Enables/Disables Recovery. UI Access ONLY.
        /// Automatically disables current connection if system detects changes to settings.
        /// </summary>
        /// <param name="sender">ComboBox object in UI</param>
        /// <param name="e">Value of ComboBox</param>
        [Log]
        private void cboAutoRecovery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }

            switch (cboAutoRecovery.SelectedIndex)
            {
                case 0:
                    RabbitServerSetup.AutoRecovery = true;
                    sldNetworokRecInt.IsEnabled = true;
                    break;

                case 1:
                    RabbitServerSetup.AutoRecovery = false;
                    sldNetworokRecInt.IsEnabled = false;
                    break;

                default:
                    goto case 0;
            }
        }

        /// <summary>
        /// Enables server. UI Access ONLY. Call by setting UI values.
        /// </summary>
        /// <param name="sender">RadioButton Object</param>
        /// <param name="e">Radio Button Values</param>
        [Log]
        private void srvEnabled_Checked(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            txtServStatus.Text = "Server Status: Enabled";
            TabMessageSettings.IsEnabled = true;

            try
            {
                _factory = SetupFactory();
                _connection = _factory.CreateConnection();

                _channel = _connection.CreateModel();

                _connection.AutoClose = false;

                CreateQueue(RabbitServerSetup.ChannelName, true, false);
                CreateExchange(RabbitServerSetup.ExchangeName, ExchangeType.Direct, true, false);
                QueueBind(RabbitServerSetup.ChannelName, RabbitServerSetup.ExchangeName);
                tabTesting.IsEnabled = true;
                _systemTimer.Start();
            }
            catch (BrokerUnreachableException ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
                var message = ErrorType[1003];
                const string caption = "Broker Unreachable Exception";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
                var message = ex.Message;
                var caption = "Error in: " + ex.Source;
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }
        }

        /// <summary>
        /// Disables server. UI Access ONLY. Call by setting UI values.
        /// </summary>
        /// <param name="sender">RadioButton Object</param>
        /// <param name="e">Radio Button Values</param>
        private void srvDisabled_Checked(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            txtServStatus.Text = "Server Status: Disabled";
            TabMessageSettings.IsEnabled = false;
            tabTesting.IsEnabled = false;
            _systemTimer.Stop();
            if (_channel != null)
                while (_channel.IsOpen)
                    _channel.Close();
            if (_connection != null)
                while (_connection.IsOpen)
                    _connection.Close();
        }

        /// <summary>
        /// Heartbeat to keep connection active to the RabbitMQ Server. Value Range (5-30)
        /// </summary>
        /// <param name="sender">Slider Oboject</param>
        /// <param name="e">Slider Value</param>
        private void sldHeartBeat_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }
            var value = (int)sldHeartBeat.Value;
            txtHeartbeat.Text = value.ToString();
            RabbitServerSetup.ServerHeartbeat = value;
        }

        /// <summary>
        /// Checkbox for generating a unique UUID to the system.
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
                    txtExchangeName.Text = Guid.NewGuid().ToString().Replace("-", String.Empty).Substring(0, 12);
                    RabbitServerSetup.ExchangeName = txtExchangeName.Text;
                }
                else if (GenerateExchange.IsChecked != null && !GenerateExchange.IsChecked.Value) //False
                {
                    txtExchangeName.IsEnabled = !GenerateExchange.IsChecked.Value;
                    txtExchangeName.Text = "Default";
                    RabbitServerSetup.ExchangeName = txtExchangeName.Text;
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        /// <summary>
        /// Checkbox for generating a unique UUID to the system.
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
                    txtChannelName.Text = Guid.NewGuid().ToString().Replace("-", String.Empty).Substring(0, 12);
                    RabbitServerSetup.ChannelName = txtChannelName.Text;
                }
                else if (GenerateChannel.IsChecked != null && !GenerateChannel.IsChecked.Value) //False
                {
                    txtChannelName.IsEnabled = !GenerateChannel.IsChecked.Value;
                    txtChannelName.Text = "Default";
                    RabbitServerSetup.ChannelName = txtChannelName.Text;
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        [Log]
        protected void Serial_Port_Initialize(SerialPort port, int index)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            try
            {
                if (port.IsOpen)
                    while (port.IsOpen)
                        port.Close();
                //Initializing the Serial Port
                SerialPorts[index].PortName = SerialCommunications[index].ComPort;
                SerialPorts[index].BaudRate = (int)SerialCommunications[index].BaudRate;
                SerialPorts[index].Parity = SerialCommunications[index].SerialParity;
                SerialPorts[index].StopBits = SerialCommunications[index].SerialStopBits;
                SerialPorts[index].DataBits = SerialCommunications[index].SerialBits;
                SerialPorts[index].Handshake = SerialCommunications[index].FlowControl;
                SerialPorts[index].RtsEnable = SerialCommunications[index].RtsEnable;
                SerialPorts[index].ReadTimeout = SerialCommunications[index].ReadTimeout;
                SerialPorts[index].DataReceived += DataReceivedHandler;

                port.Open();
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        /// <summary>
        /// Serial Commnuication Event Handler.
        /// </summary>
        /// <param name="sender">COM Port Data Receveived Object</param>
        /// <param name="e">Data Received</param>
        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            var message = new JsonObject
            {
                DeviceName = DeviceName,
                MessageDateTime = DateTime.Now,
                MessageType = RabbitServerSetup.MessageFormat
            };
            try
            {
                var sp = (SerialPort)sender;
                sp.ReadTimeout = 1000; //Waits for a second before throwing Exception
                var indata = sp.ReadLine();
                var data = JsonConvert.DeserializeObject<Messages>(indata);
                message.Message.HeatIndexC = data.HeatIndexC;
                message.Message.HeatIndexF = data.HeatIndexF;
                message.Message.Humidity = data.Humidity;
                message.Message.TemperatureC = data.TemperatureC;
                message.Message.TemperatureF = data.TemperatureF;

                while (!PublishMessage(message, RabbitServerSetup.ExchangeName))
                {
                    //loop until message has been published
                }
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        private void txtExchangeName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
                RabbitServerSetup.ExchangeName = "default." + txtExchangeName.Text;
            }
            else
            {
                RabbitServerSetup.ExchangeName = "default." + txtExchangeName.Text;
            }
        }

        private void txtChannelName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
                RabbitServerSetup.ChannelName = "default." + txtChannelName.Text;
            }
            else
            {
                RabbitServerSetup.ChannelName = "default." + txtChannelName.Text;
            }
        }

        private void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            var message = new JsonObject
            {
                DeviceName = DeviceName,
                MessageDateTime = DateTime.Now,
                MessageType = RabbitServerSetup.MessageFormat,
                Message = { HeatIndexC = MessageToSend.Text }
            };

            while (!PublishMessage(message, RabbitServerSetup.ExchangeName))
            {
                //Repeast until published
            }
            MessageToSend.Text = "Message to Send:";
        }

        private void TxtServerAddress_OnLostFocus(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var ip = txtServerAddress.Text;
            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }

            try
            {
                var rxMatch = Regex.Match(ip, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
                //Check IP with Regex expression to match IPv4 requirements
                if (rxMatch.Success || ip.ToLower() == "localhost") //Allows localhost to be used
                {
                    RabbitServerSetup.ServerAddress = ip.ToLower() == "localhost"
                        ? IPAddress.Loopback
                        : IPAddress.Parse(ip);
                }
                else
                {
                    txtServerAddress.Text = "localhost";
                    const string message =
                        "IP Address missmatch. Please provide a valid IPv4 address or \"localhost\" as an address";
                    const string caption = "Error - Address Missmatch";

                    MessageBox.Show(message, caption, MessageBoxButtons.OK);
                }
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        private void TxtServerAddress_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                TxtServerAddress_OnLostFocus(sender, e);
        }

        private void txtVirtualHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }
            RabbitServerSetup.VirtualHost = txtVirtualHost.Text;
        }

        private void MessageToSend_OnGotFocus(object sender, RoutedEventArgs e)
        {
            MessageToSend.Text = "";
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            srvEnabled.IsChecked = false;
            srvDisabled.IsChecked = true;

            foreach (var serialPort in SerialPorts)
                if (serialPort.IsOpen)
                    serialPort.Close();
        }

        private void txtUserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
                RabbitServerSetup.UserName = txtUserName.Text;
            }
            else
            {
                RabbitServerSetup.UserName = txtUserName.Text;
            }
        }

        private void txtServerPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
                RabbitServerSetup.ServerPort = Convert.ToInt32(txtServerPort.Text);
            }
            else
            {
                RabbitServerSetup.ServerPort = Convert.ToInt32(txtServerPort.Text);
            }
        }

        private void PwdPassword_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
                RabbitServerSetup.Password = pwdPassword.Password;
            }
            else
            {
                RabbitServerSetup.Password = pwdPassword.Password;
            }
        }

        private void SerialPortEnabled_CheckboxChecked(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox)sender;
            var chk = new CheckedListItem
            {
                IsChecked = cb.IsChecked != null && cb.IsChecked.Value,
                Name = cb.Name
            };
            var index = AvailableSerialPorts.IndexOf(chk);
            var cbo = (CheckBox)lstModbusSerial.Items[index];
            if (cb.IsChecked != null && cbo.IsChecked != null && cb.IsChecked.Value == cbo.IsChecked.Value)
                cbo.IsChecked = !cbo.IsChecked.Value;
        }

        private void SerialModbusEnabled_CheckboxChecked(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox)sender;
            var chk = new CheckedListItem
            {
                IsChecked = cb.IsChecked != null && cb.IsChecked.Value,
                Name = cb.Name
            };
            var index = AvailableSerialPorts.IndexOf(chk);
            var cbo = (CheckBox)lstSerial.Items[index];

            if (cb.IsChecked != null && cbo.IsChecked != null && cb.IsChecked.Value == cbo.IsChecked.Value)
                cbo.IsChecked = !cbo.IsChecked.Value;
        }
    }

    public class CheckedListItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }
}