using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

namespace RabbitMQ_SendClient
{
    public class CheckListItem
    {
        public string Content { get; set; }
        public bool IsChecked { get; set; }
        public string Name { get; set; }
        public string Uid { get; set; }
    }

    /// <summary>
    ///     Main UI for RabbitMQ Client
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        /// <summary>
        /// RabbitMQ Server Information for setup
        /// </summary>
        internal static RabbitServerInformation RabbitServerSetup = new RabbitServerInformation
        {
            ServerAddress = IPAddress.Parse("127.0.0.1"),
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

        private static readonly ObservableCollection<CheckListItem> AvailableModbusSerialPorts = new ObservableCollection<CheckListItem>();
        private static readonly ObservableCollection<CheckListItem> AvailableSerialPorts = new ObservableCollection<CheckListItem>();
        private static readonly string DeviceName = Environment.MachineName;
        private static readonly StackTrace StackTrace = new StackTrace();
        private static IModel _channel;
        private static IConnection _connection;
        private static IConnectionFactory _factory = new ConnectionFactory();
        private readonly DispatcherTimer _systemTimer = new DispatcherTimer();
        private int[] _ijk;

        /// <summary>
        /// Mainline Executable to the RabbitMQ Client
        /// </summary>
        [Log]
        public MainWindow()
        {
            InitializeSerialPortCheckBoxes();
            InitializeComponent();

            //RabbitMQ Server Configuration
            cboAutoRecovery.SelectedIndex = 0;

            SetupSerial(SerialPort.GetPortNames());

            InitializeSerialPorts();
            InitializeHeartBeatTimer();
            InitialzeServerSettings();
        }

        private void InitializeSerialPortCheckBoxes()
        {
            AvailableSerialPorts.Clear();
            AvailableModbusSerialPorts.Clear();
            Array.Resize(ref _ijk, SerialPort.GetPortNames().Length);
            for (var i = 0; i < SerialPort.GetPortNames().Length; i++)
            {
                var serialPortCheck = new CheckListItem()
                {
                    Content = SerialPort.GetPortNames()[i],
                    IsChecked = false,
                    Name = SerialPort.GetPortNames()[i] + "Serial",
                    Uid = i + ":" + Guid.NewGuid()
                };
                AvailableSerialPorts.Add(serialPortCheck);

                var serialModbusCheck = new CheckListItem()
                {
                    Content = SerialPort.GetPortNames()[i],
                    IsChecked = false,
                    Name = SerialPort.GetPortNames()[i] + "Modbus",
                    Uid = i + ":" + Guid.NewGuid()
                };
                AvailableModbusSerialPorts.Add(serialModbusCheck);

                _ijk[i] = 0;
            }
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

        private void InitialzeServerSettings()
        {
            if (!IsInitialized) return;
            txtServerAddress.Text = RabbitServerSetup.ServerAddress.ToString();
            txtUserName.Text = RabbitServerSetup.UserName;
            pwdPassword.Password = RabbitServerSetup.Password;
            txtServerPort.Text = RabbitServerSetup.ServerPort.ToString();
            txtVirtualHost.Text = RabbitServerSetup.VirtualHost;
            txtExchangeName.Text = RabbitServerSetup.ExchangeName;
            txtChannelName.Text = RabbitServerSetup.ChannelName;
            sldHeartBeat.Value = RabbitServerSetup.ServerHeartbeat;
            sldNetworokRecInt.Value = RabbitServerSetup.NetworkRecoveryInterval;
        }

        /// <summary>
        /// Publishes Message to RabbitMQ server using JSON format
        /// </summary>
        /// <param name="message">JSON type Message. HAS TO BE PREFORMATTED</param>
        /// <param name="exchangeName">Exchange that this will be published to</param>
        /// <returns>Message success state</returns>
        protected bool PublishMessage(JsonObject message, string exchangeName)
        {

            if (!_connection.IsOpen)
            {
                startServer();
            }
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
            catch (AlreadyClosedException ex)
            {
                var indexOf = ex.Message.IndexOf("\"", StringComparison.Ordinal);
                var indexOff = ex.Message.IndexOf("\"", indexOf + 1, StringComparison.Ordinal);
                var errmessage = ex.Message.Substring(indexOf + 1, indexOff - indexOf -1);
                MessageBox.Show(errmessage, @"Connection Already Closed", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                success = false;


                for (var i = 0; i < SerialPorts.Length; i++)
                {
                        CloseSerialPortUnexpectedly(i);
                }


                DisableServerUnexpectedly();
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

        private void DisableServerUnexpectedly()
        {
            Dispatcher.Invoke((MethodInvoker) delegate
            {
                srvDisabled.IsChecked = true;
                srvEnabled.IsChecked = false;
                TabMessageSettings.IsEnabled = false;
                tabServerSettings.IsSelected = true;
            });
        }

        [Log]
        protected bool Serial_Port_Initialize(int index)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return true;
            try
            {
                if (SerialPorts[index].IsOpen)
                    while (SerialPorts[index].IsOpen)
                        SerialPorts[index].Close();
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
                SerialCommunications[index].X();

                SerialPorts[index].Open();

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                while (SerialPorts[index].IsOpen) SerialPorts[index].Close(); //Close port if opened

                MessageBox.Show(@"Access to the port '" + SerialPorts[index].PortName + @"' is denied.", @"Error opening Port",
                MessageBoxButtons.OK, MessageBoxIcon.Stop);
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);

                return false;
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);

                return false;
            }
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
        /// Serial Commnuication Event Handler.
        /// </summary>
        /// <param name="sender">COM Port Data Receveived Object</param>
        /// <param name="e">Data Received</param>
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            var message = new JsonObject
            {
                DeviceName = DeviceName,
                MessageDateTime = DateTime.Now,
                MessageType = RabbitServerSetup.MessageFormat
            };
            try
            {
                var sp = (SerialPort) sender;
                var spdata = sp.ReadLine();

                var index = 0;
                var i = 0;
                foreach (var serialPort in AvailableModbusSerialPorts)
                {
                    if (serialPort.Content == sp.PortName)
                    {
                        index = i;
                        break;
                    }
                    i++;
                }
                if (index > AvailableSerialPorts.Count)
                {
                    index = AvailableSerialPorts.Count - 1;
                }

                SerialCommunications[index].JsonMeasurements++;
                CalculateNpChart(index);
                try
                {
                    var indata = JsonConvert.DeserializeObject<Messages>(spdata);
                    message.Message.HeatIndexC = indata.HeatIndexC;
                    message.Message.HeatIndexF = indata.HeatIndexF;
                    message.Message.Humidity = indata.Humidity;
                    message.Message.TemperatureC = indata.TemperatureC;
                    message.Message.TemperatureF = indata.TemperatureF;

                    var delay = 0;
                    while (!PublishMessage(message, RabbitServerSetup.ExchangeName))
                    {
                        Thread.Sleep(10);
                        delay += 10;

                        if (delay == 1000)
                            break;
                    }
                }
                catch (JsonException ex)
                {
                    SerialCommunications[index].JsonErrors++;
                    SerialPorts[index].Close();

                    Thread.Sleep(10);

                    SerialPorts[index].Open();

                    if (OutOfControl(index))
                    {
                        //Log Message
                        CloseSerialPortUnexpectedly(index);
                        var sf = StackTrace.GetFrame(0);
                        LogError(ex, SystemVariables.LogLevel.Critical, sf);
                        MessageBox.Show(
                            Properties.Resources.MainWindow_DataReceivedHandler_ + AvailableSerialPorts[index].Content,
                            Properties.Resources.MainWindow_DataReceivedHandler_JSON_Message_Error, MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation);


                    }
                }
                catch (Exception ex)
                {
                    //Log Message
                    var sf = StackTrace.GetFrame(0);
                    LogError(ex, SystemVariables.LogLevel.Critical, sf);
                }
            }
            catch (IndexOutOfRangeException ex)
            {

            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        private void CloseSerialPortUnexpectedly(int index)
        {
            if(!SerialPorts[index].IsOpen) return;
            while (SerialPorts[index].IsOpen)
            {
                SerialPorts[index].Close();
            }

            Dispatcher.Invoke((MethodInvoker)delegate
            {
                
                var checkList = AvailableSerialPorts[index];
                checkList.IsChecked = false;
                AvailableSerialPorts.RemoveAt(index);
                AvailableSerialPorts.Insert(index, checkList);

            });
        }

        private void CalculateNpChart(int index)
        {
            var pVal = SerialCommunications[index].JsonErrors / SerialCommunications[index].JsonMeasurements;
            var xBar = 0.0;
            if (SerialCommunications[index].JsonMeasurements > SerialCommunications[index].MaximumErrors)
            {
                if (_ijk[index] > 49)
                {
                    _ijk[index] = 0;
                }

                _ijk[index]++;

            }
            SerialCommunications[index].SetX(_ijk[index], pVal);
            for (var i = 0; i < 50; i++)
            {
                xBar += SerialCommunications[index].GetX(i);
            }
            xBar = xBar / 50;
            SerialCommunications[index].UCL = xBar + (3 * (Math.Sqrt(Math.Abs(xBar * (1 - xBar)) / SerialCommunications[index].MaximumErrors)));
        }

        private bool OutOfControl(int index)
        {
            var xbar = 0.0;
            for (int i = SerialCommunications[index].MaximumErrors; i >= 0; i--)
            {
                xbar += SerialCommunications[index].GetX(i);
            }
            xbar = xbar /( SerialCommunications[index].MaximumErrors*0.9);

            return xbar > SerialCommunications[index].UCL;
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

        private void BtnSendMessage_Click(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// Enables/Disables Recovery. UI Access ONLY.
        /// Automatically disables current connection if system detects changes to settings.
        /// </summary>
        /// <param name="sender">ComboBox object in UI</param>
        /// <param name="e">Value of ComboBox</param>
        [Log]
        private void CboAutoRecovery_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
        /// RabbitMQ Heartbeat Timer. Adjusts value of system information in scrollbar on tick
        /// </summary>
        [Log]
        private void InitializeHeartBeatTimer()
        {
            try
            {
                _systemTimer.Tick += SystemTimerOnTick;
                _systemTimer.Interval = TimeSpan.FromMilliseconds(100);
            }
            catch (Exception e)
            {
                var message = e.Message + "\nError in Timer Initialization";
                MessageBox.Show(message, e.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [Log]
        private void InitializeSerialPorts()
        {
            var ports = SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                LstSerial.Items.Add("No Ports Available");
                LstModbusSerial.Items.Add("No Ports Available");
            }
            else
            {
                try
                {
                    LstSerial.ItemsSource = AvailableSerialPorts;
                    LstModbusSerial.ItemsSource = AvailableModbusSerialPorts;
                }
                catch (Exception e)
                {
                    var message = e.Message + "\nError in Port Enumeration";
                    MessageBox.Show(message, e.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            srvEnabled.IsChecked = false;
            srvDisabled.IsChecked = true;

            foreach (var serialPort in SerialPorts)
                if (serialPort.IsOpen)
                    serialPort.Close();
        }

        private void MessageToSend_OnGotFocus(object sender, RoutedEventArgs e)
        {
            MessageToSend.Text = "";
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

        private void SerialEnabled_CheckboxChecked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            var cb = (CheckBox)sender;
            var index = int.Parse(cb.Uid.Substring(0, 1));
            var cbo = (CheckListItem)LstModbusSerial.Items[index];
            if (cb.IsChecked != null) cbo.IsChecked = false;

            //disables Modbus COM Port
            AvailableModbusSerialPorts.RemoveAt(index);
            AvailableModbusSerialPorts.Insert(index, cbo);

            //Enable Port for serial communications
            var setupSerialForm = new SerialPortSetup { SerialPortNum = index };
            var activate = setupSerialForm.ShowDialog(); //Confirm Settings

            if (activate != null && activate.Value)
            {
                var init = Serial_Port_Initialize(index);
                if (init) return;

                //Initialzation of port failed. Closing port and unchecking it
                cb.IsChecked = false;
                AvailableSerialPorts.RemoveAt(index);
                var cli = new CheckListItem()
                {
                    Name = cb.Name,
                    Uid = cb.Uid,
                    Content = cbo.Content,
                    IsChecked = false
                };
                AvailableSerialPorts.Insert(index, cli);
            }
            else
            {
                cb.IsChecked = false;
                AvailableSerialPorts.RemoveAt(index);
                var cli = new CheckListItem()
                {
                    Name = cb.Name,
                    Uid = cb.Uid,
                    Content = cbo.Content,
                    IsChecked = false
                };
                AvailableSerialPorts.Insert(index, cli);
            }
        }

        private void SerialModbusEnabled_CheckboxChecked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            var cb = (CheckBox)sender;
            var index = int.Parse(cb.Uid.Substring(0, 1));
            var cbo = (CheckListItem)LstSerial.Items[index];
            if (cb.IsChecked != null) cbo.IsChecked = false;

            AvailableSerialPorts.RemoveAt(index);
            AvailableSerialPorts.Insert(index, cbo);

            //Enable Port
        }

        /// <summary>
        /// Heartbeat to keep connection active to the RabbitMQ Server. Value Range (5-30)
        /// </summary>
        /// <param name="sender">Slider Oboject</param>
        /// <param name="e">Slider Value</param>
        private void SldHeartBeat_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
        /// Recovery Interval of the network. UI Access ONLY
        /// Automatically disables current connection if system detects changes to settings.
        /// </summary>
        /// <param name="sender">Slider Object in the UI</param>
        /// <param name="e">Value of Slider</param>
        private void SldNetworokRecInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
        /// Disables server. UI Access ONLY. Call by setting UI values.
        /// </summary>
        /// <param name="sender">RadioButton Object</param>
        /// <param name="e">Radio Button Values</param>
        private void SrvDisabled_Checked(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            CloseSafely();
        }

        private void CloseSafely()
        {
            if (srvDisabled.IsChecked != null && !srvDisabled.IsChecked.Value)
            {
                srvDisabled.IsChecked = true;
                srvDisabled.IsChecked = false;
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
                foreach (var port in SerialPorts)
                {
                    if (port.IsOpen)
                    {
                        port.Close();
                    }
                }
                InitializeSerialPortCheckBoxes();
            }
            else
            {
                foreach (var port in SerialPorts)
                {
                    if (port.IsOpen)
                    {
                        port.Close();
                    }
                }
            }
        }

        private void startServer()
        {
            if (srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = true;
                srvDisabled.IsChecked = false;
            }
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
        /// Enables server. UI Access ONLY. Call by setting UI values.
        /// </summary>
        /// <param name="sender">RadioButton Object</param>
        /// <param name="e">Radio Button Values</param>
        [Log]
        private void SrvEnabled_Checked(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            startServer();
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

        private void TxtChannelName_TextChanged(object sender, TextChangedEventArgs e)
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

        private void TxtExchangeName_TextChanged(object sender, TextChangedEventArgs e)
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

        private void TxtServerAddress_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                TxtServerAddress_OnLostFocus(sender, e);
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

        /// <summary>
        /// Address being changed. Do nothing. Checking should be done at the end.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtServerAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            //do Nothing! checking happens on commit with either focus loss or enter
        }

        private void TxtServerPort_TextChanged(object sender, TextChangedEventArgs e)
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

        private void TxtUserName_TextChanged(object sender, TextChangedEventArgs e)
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

        private void TxtVirtualHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }
            RabbitServerSetup.VirtualHost = txtVirtualHost.Text;
        }

        private void SerialEnabled_CheckboxUnchecked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            var cb = (CheckBox)sender;
            var index = int.Parse(cb.Uid.Substring(0, 1));
            if (SerialPorts[index].IsOpen)
                SerialPorts[index].Close();
        }
    }
}