using Newtonsoft.Json;
using PostSharp.Patterns.Diagnostics;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using CheckBox = System.Windows.Controls.CheckBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.Forms.MessageBox;

public enum LogLevel
{
    Debug = 1,
    Verbose = 2,
    Information = 3,
    Warning = 4,
    Error = 5,
    Critical = 6,
    None = int.MaxValue
}

public enum BaudRates
{
    BaudRate300 = 300,
    BaudRate1200 = 1200,
    BaudRate2400 = 2400,
    BaudRate4800 = 4800,
    BaudRate9600 = 9600,
    BaudRate14400 = 14400,
    BaudRate19200 = 19200,
    BaudRate28800 = 28800,
    BaudRate38400 = 38400,
    BaudRate57600 = 57600,
    BaudRate115200 = 115200,
    BaudRate230400 = 230400
}

public struct JsonObject
{
    public string DeviceName { get; set; }
    public string MessageType { get; set; }
    public Messages Message;
    public DateTime MessageDateTime { get; set; }
}

public struct Messages
{
    public string TemperatureC { get; set; }
    public string TemperatureF { get; set; }
    public string Humidity { get; set; }
    public string HeatIndexC { get; set; }
    public string HeatIndexF { get; set; }
}

public struct ServerInformation
{
    public IPAddress ServerAddress;
    public string ExchangeName;
    public string ChannelName;
    public string UserName;
    public string Password;
    public string VirtualHost;
    public int ServerPort;
    public int ServerHeartbeat;
    public string MessageType;
    public string Encoding;
    public bool AutoRecovery;
    public int NetworkRecoveryInterval;
    public string MessageFormat;
}

public struct SerialCommunication
{
    public string ComPort;
    public BaudRates BaudRate;
    public Parity SerialParity;
    public StopBits SerialStopBits;
    public int SerialBits;
    public Handshake FlowControl;
    public bool RtsEnable;
    public int ReadTimeout;
};

namespace RabbitMQ_SendClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        private static readonly List<SerialPort> SerialPort = new List<SerialPort>();
        private static TraceSource ts = new TraceSource("MainWindow"); //to be implimented

        private readonly DispatcherTimer _systemTimer = new DispatcherTimer();

        private static IConnectionFactory _factory = new ConnectionFactory();
        private static IConnection _connection;
        private static IModel _channel;

        private static readonly string DeviceName = Environment.MachineName;

        private IDictionary<int, string> ErrorType { get; set; } = new Dictionary<int, string>()
        {
            {0001, "Null Reference Exception" },
            {1001, "RabbitMQ Exception - Connection Already Closed" },
            {1003, "RabbitMQ Exception - Broker Unreachable"},
            {1002, "RabbitMQ Exception - Authentication Failure" },
            {1004, "RabbitMQ Exception - Channel Already Allocated and in Use" },
            {1005, "RabbitMQ Exception - Connection Failure" },
            {1006, "RabbitMQ Exception - Operation Interrupted" },
            {1007, "RabbitMQ Exception - Packet Not Recognized" },
            {1008, "RabbitMQ Exception - Possible Authentication Failure" },
            {1009, "RabbitMQ Exception - Connection Already Closed" },
            {1010, "RabbitMQ Exception - Protocol Version Mismatch" },
            {1011, "RabbitMQ Exception - Unsupported Method" },
            {1012, "RabbitMQ Exception - Unsupported Method Field" },
            {1013, "RabbitMQ Exception - Wire Formatting" }
        };

        private readonly SerialCommunication[] _serialSetup = new SerialCommunication[System.IO.Ports.SerialPort.GetPortNames().Length];

        protected static ServerInformation ServerSetup = new ServerInformation()
        {
            ServerAddress = IPAddress.Parse("192.168.0.10"),
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

        [LogException]
        public MainWindow()
        {
            InitializeComponent();

            //RabbitMQ Server Configuration
            cboAutoRecovery.SelectedIndex = 0;

            //Serial Communication Setup
            cboMessageType.Items.Add("Serial");
            cboMessageType.Items.Add("Modbus");
            cboMessageType.Items.Add("OPC");
            cboMessageType.SelectedIndex = 0;

            InitializeSerialPorts();
            InitializeBaudRates();
            InitializeDataBits();
            InitializeStopBits();
            InitializeParity();
            InitializeHandshake();
            InitializeHeartBeatTimer();
        }

        [LogException]
        private void InitializeSerialPorts()
        {
            var ports = System.IO.Ports.SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                cboPortNames.IsEnabled = false;
                cboPortNames.Items.Add("No Ports");
                cboPortNames.SelectedIndex = 0;
            }
            else
            {
                try
                {
                    var index = 0;
                    foreach (var port in ports)
                    {
                        var cb = new CheckBox
                        {
                            Name = "cbo" + port,
                            Content = port
                        };
                        cb.Checked += CbOnChecked;
                        cb.Unchecked += CbOnChecked;

                        cboPortNames.Items.Add(cb);
                        cboPortNames.RegisterName(cb.Name, cb);
                        var serialComm = new SerialCommunication()
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
                        SerialPort.Add(serialPort);
                        index++;
                    }
                    cboPortNames.SelectedIndex = 0;
                }
                catch (Exception e)
                {
                    var message = e.Message + "\nError in Port Enumeration";
                    MessageBox.Show(message, e.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        [LogException]
        private void InitializeBaudRates()
        {
            try
            {
                foreach (var rate in Enum.GetValues(typeof(BaudRates)))
                {
                    cboBaudRate.Items.Add(rate);
                }

                cboBaudRate.SelectedIndex = cboBaudRate.Items.IndexOf(BaudRates.BaudRate9600.ToString());
                cboBaudRate.IsEnabled = true;
            }
            catch (Exception exception)
            {
                var message = exception.Message + "\nError in BaudRate Enumeration";
                MessageBox.Show(message, exception.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [LogException]
        private void InitializeDataBits()
        {
            try
            {
                for (var i = 4; i != 9; i++)
                {
                    cboDataBits.Items.Add(i);
                }
                //cboDataBits.SelectedIndex = cboDataBits.Items.Count - 1;
                cboDataBits.IsEnabled = true;
            }
            catch (Exception e)
            {
                var message = e.Message + "\nError in DataBits Enumeration";
                MessageBox.Show(message, e.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [LogException]
        private void InitializeStopBits()
        {
            try
            {
                foreach (var stopBit in Enum.GetValues(typeof(StopBits)))
                {
                    cboStopBits.Items.Add(stopBit);
                }
                //cboStopBits.SelectedIndex = 0;
                cboStopBits.IsEnabled = true;
            }
            catch (Exception e)
            {
                var message = e.Message + "\nError in StopBits Enumeration";
                MessageBox.Show(message, e.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [LogException]
        private void InitializeParity()
        {
            try
            {
                foreach (var parity in Enum.GetValues(typeof(Parity)))
                {
                    cboParity.Items.Add(parity);
                }
                //cboParity.SelectedIndex = 0;
                cboParity.IsEnabled = true;
            }
            catch (Exception e)
            {
                var message = e.Message + "\nError in Parity Enumeration";
                MessageBox.Show(message, e.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [LogException]
        private void InitializeHandshake()
        {
            try
            {
                foreach (var shake in Enum.GetValues(typeof(Handshake)))
                {
                    cboFlowControl.Items.Add(shake);
                }
                //cboFlowControl.SelectedIndex = 0;
                cboFlowControl.IsEnabled = true;

                sldReadTimeout.IsEnabled = true;
            }
            catch (Exception e)
            {
                var message = e.Message + "\nError in Handshake Enumeration";
                MessageBox.Show(message, e.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [LogException]
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

        private void CbOnChecked(object sender, RoutedEventArgs routedEventArgs)
        {
            //prevents execution during initialization
            if (!IsInitialized) return;

            Serial_Port_Initialize(SerialPort[cboPortNames.SelectedIndex], cboPortNames.SelectedIndex);
        }

        private void SystemTimerOnTick(object sender, EventArgs eventArgs)
        {
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && (!_connection.IsOpen && srvEnabled.IsChecked.Value))
            {
                txtServStatus.Text = "Server Status: Recovering";
            }
            else
            {
                txtServStatus.Text = _connection.IsOpen ? "Server Status: Connected" : "Server Status: Disconnected";
            }
        }

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
                LogError(ex, LogLevel.Critical);
                success = false;
            }

            return success;
        }

        private static void CreateExchange(string exchangeName, string exchangeType, bool exchangeDurability, bool autoDelete)
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
                LogError(ex, LogLevel.Critical);
            }
        }

        private static void CreateQueue(string queueName, bool queueDurable, bool queueAutoDelete)
        {
            try
            {
                _channel.QueueDeclare(queueName, queueDurable, false, queueAutoDelete, null);
            }
            catch (Exception ex)
            {
                LogError(ex, LogLevel.Critical);
            }
        }

        private static void QueueBind(string queueName, string exchangeName)
        {
            try
            {
                _channel.QueueBind(queueName, exchangeName, "");
            }
            catch (Exception ex)
            {
                LogError(ex, LogLevel.Critical);
            }
        }

        private static ConnectionFactory SetupFactory()
        {
            var factory = new ConnectionFactory()
            {
                HostName = ServerSetup.ServerAddress.ToString(),
                UserName = ServerSetup.UserName,
                Password = ServerSetup.Password,
                VirtualHost = ServerSetup.VirtualHost,
                Port = ServerSetup.ServerPort,
                AutomaticRecoveryEnabled = ServerSetup.AutoRecovery,
                RequestedHeartbeat = (ushort)ServerSetup.ServerHeartbeat,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(ServerSetup.NetworkRecoveryInterval),
                TopologyRecoveryEnabled = ServerSetup.AutoRecovery,
            };
            return factory;
        }

        public static void LogError(Exception ex, LogLevel level)
        {
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                w.Write("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                w.WriteLine("\t:{0}", level);
                w.WriteLine("{0}", ex.Source);
                w.WriteLine("{0}", ex.Message);
            }
        }

        private void cbxMessageType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void txtServerAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            //do Nothing! checking happens on commit with either focus loss or enter
        }

        private void sldNetworokRecInt_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;

                try
                {
                    var value = (int)sldNetworokRecInt.Value;

                    NetworkRecIntervalTxt.Text = value.ToString();
                    ServerSetup.NetworkRecoveryInterval = value;
                }
                catch (Exception exception)
                {
                    LogError(exception, LogLevel.Critical);
                }
            }
            else
            {
                try
                {
                    var value = (int)sldNetworokRecInt.Value;

                    NetworkRecIntervalTxt.Text = value.ToString();
                    ServerSetup.NetworkRecoveryInterval = value;
                }
                catch (Exception exception)
                {
                    LogError(exception, LogLevel.Critical);
                }
            }
        }

        private void cboAutoRecovery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }
            else
            {
                switch (cboAutoRecovery.SelectedIndex)
                {
                    case 0:
                        ServerSetup.AutoRecovery = true;
                        sldNetworokRecInt.IsEnabled = true;
                        break;

                    case 1:
                        ServerSetup.AutoRecovery = false;
                        sldNetworokRecInt.IsEnabled = false;
                        break;

                    default:
                        goto case 0;
                }
            }
        }

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

                CreateQueue(ServerSetup.ChannelName, true, false);
                CreateExchange(ServerSetup.ExchangeName, ExchangeType.Direct, true, false);
                QueueBind(ServerSetup.ChannelName, ServerSetup.ExchangeName);
                tabTesting.IsEnabled = true;
                _systemTimer.Start();
            }
            catch (BrokerUnreachableException ex)
            {
                LogError(ex, LogLevel.Critical);
                var message = ErrorType[1003];
                var caption = "Broker Unreachable Exception";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }
            catch (Exception ex)
            {
                LogError(ex, LogLevel.Critical);
                var message = ex.Message;
                var caption = "Error in: " + ex.Source;
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }
        }

        private void srvDisabled_Checked(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            txtServStatus.Text = "Server Status: Disabled";
            TabMessageSettings.IsEnabled = false;
            tabTesting.IsEnabled = false;
            _systemTimer.Stop();
            if (_channel != null)
            {
                while (_channel.IsOpen)
                {
                    _channel.Close();
                }
            }
            if (_connection != null)
            {
                while (_connection.IsOpen)
                {
                    _connection.Close();
                }
            }
        }

        private void sldHeartBeat_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;
            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
                var value = (int)sldHeartBeat.Value;
                txtHeartbeat.Text = value.ToString();
                ServerSetup.ServerHeartbeat = value;
            }
            else
            {
                var value = (int)sldHeartBeat.Value;
                txtHeartbeat.Text = value.ToString();
                ServerSetup.ServerHeartbeat = value;
            }
        }

        private void GenerateExchange_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;
            try
            {
                if (GenerateExchange.IsChecked != null && GenerateExchange.IsChecked.Value) //True
                {
                    txtExchangeName.IsEnabled = !GenerateExchange.IsChecked.Value;
                    txtExchangeName.Text = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
                    ServerSetup.ExchangeName = txtExchangeName.Text;
                }
                else if (GenerateExchange.IsChecked != null && !GenerateExchange.IsChecked.Value) //False
                {
                    txtExchangeName.IsEnabled = !GenerateExchange.IsChecked.Value;
                    txtExchangeName.Text = "Default";
                    ServerSetup.ExchangeName = txtExchangeName.Text;
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            catch (Exception ex)
            {
                LogError(ex, LogLevel.Error);
            }
        }

        private void GenerateChannel_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            try
            {
                if (GenerateChannel.IsChecked != null && GenerateChannel.IsChecked.Value) //True
                {
                    txtChannelName.IsEnabled = !GenerateChannel.IsChecked.Value;
                    txtChannelName.Text = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 12);
                    ServerSetup.ChannelName = txtChannelName.Text;
                }
                else if (GenerateChannel.IsChecked != null && !GenerateChannel.IsChecked.Value) //False
                {
                    txtChannelName.IsEnabled = !GenerateChannel.IsChecked.Value;
                    txtChannelName.Text = "Default";
                    ServerSetup.ChannelName = txtChannelName.Text;
                }
                else
                {
                    throw new NullReferenceException();
                }
            }
            catch (Exception ex)
            {
                LogError(ex, LogLevel.Error);
            }
        }

        private void cboPortNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        protected void Serial_Port_Initialize(SerialPort port, int index)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            try
            {
                if (port.IsOpen)
                {
                    while (port.IsOpen)
                    {
                        port.Close();
                    }
                }
                //Initializing the Serial Port
                port.PortName = _serialSetup[index].ComPort;
                port.BaudRate = (int)_serialSetup[index].BaudRate;
                port.Parity = _serialSetup[index].SerialParity;
                port.StopBits = _serialSetup[index].SerialStopBits;
                port.DataBits = _serialSetup[index].SerialBits;
                port.Handshake = _serialSetup[index].FlowControl;
                port.RtsEnable = _serialSetup[index].RtsEnable;
                port.ReadTimeout = _serialSetup[index].ReadTimeout;
                port.DataReceived += DataReceivedHandler;

                port.Open();
            }
            catch (Exception e)
            {
                LogError(e, LogLevel.Error);
            }
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            var message = new JsonObject()
            {
                DeviceName = DeviceName,
                MessageDateTime = DateTime.Now,
                MessageType = ServerSetup.MessageFormat
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

                while (!PublishMessage(message, ServerSetup.ExchangeName))
                {
                    //loop until message has been published
                }
            }
            catch (Exception ex)
            {
                LogError(ex, LogLevel.Critical);
            }
        }

        private void cboBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var rates = (BaudRates)Enum.Parse(typeof(BaudRates), cboBaudRate.Items[cboBaudRate.SelectedIndex].ToString());

            _serialSetup[cboPortNames.SelectedIndex].BaudRate = rates;
            foreach (var serialPort in SerialPort)
            {
                var cb = (CheckBox)FindName("cbo" + serialPort);
                var index = cboPortNames.Items.Cast<object>().TakeWhile(item => cb?.Content != item).Count() - 1;
                Serial_Port_Initialize(SerialPort[index], index);
            }
        }

        private void cboDataBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var bits = cboDataBits.Items[cboDataBits.SelectedIndex].ToString();
            _serialSetup[cboPortNames.SelectedIndex].SerialBits = int.Parse(bits);
            foreach (var serialPort in SerialPort)
            {
                var cb = (CheckBox)FindName("cbo" + serialPort);
                var index = cboPortNames.Items.Cast<object>().TakeWhile(item => cb?.Content != item).Count() - 1;
                Serial_Port_Initialize(SerialPort[index], index);
            }
        }

        private void cboFlowControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var flowControl = (Handshake)Enum.Parse(typeof(Handshake), cboFlowControl.Items[cboFlowControl.SelectedIndex].ToString());

            _serialSetup[cboPortNames.SelectedIndex].FlowControl = flowControl;
            switch (flowControl)
            {
                case Handshake.None:
                    _serialSetup[cboPortNames.SelectedIndex].RtsEnable = false;
                    break;

                case Handshake.RequestToSend:
                    _serialSetup[cboPortNames.SelectedIndex].RtsEnable = true;
                    break;

                case Handshake.RequestToSendXOnXOff:
                    _serialSetup[cboPortNames.SelectedIndex].RtsEnable = true;
                    break;

                case Handshake.XOnXOff:
                    _serialSetup[cboPortNames.SelectedIndex].RtsEnable = true;
                    break;

                default:
                    goto case Handshake.None;
            }
            foreach (var serialPort in SerialPort)
            {
                var cb = (CheckBox)FindName("cbo" + serialPort);
                var index = cboPortNames.Items.Cast<object>().TakeWhile(item => cb?.Content != item).Count() - 1;
                Serial_Port_Initialize(SerialPort[index], index);
            }
        }

        private void sldReadTimeout_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var timeout = (int)sldReadTimeout.Value;
            txtReadTimeout.Text = timeout.ToString();

            _serialSetup[cboPortNames.SelectedIndex].ReadTimeout = timeout;
            foreach (var serialPort in SerialPort)
            {
                var cb = (CheckBox)FindName("cbo" + serialPort);
                var index = cboPortNames.Items.Cast<object>().TakeWhile(item => cb?.Content != item).Count() - 1;
                Serial_Port_Initialize(SerialPort[index], index);
            }
        }

        private void cboMessageType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            txtMessageType.Text = string.Concat("Using: ", cboMessageType.Items[cboMessageType.SelectedIndex].ToString());

            if (cboMessageType.SelectedIndex == 0)
            {
                foreach (var serialPort in SerialPort)
                {
                    var cb = (CheckBox)FindName("cbo" + serialPort);
                    var index = cboPortNames.Items.Cast<object>().TakeWhile(item => cb?.Content != item).Count() - 1;
                    Serial_Port_Initialize(SerialPort[index], index);
                }
            }
            else
            {
                foreach (var serialPort in SerialPort)
                {
                    serialPort.Close();
                }
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
                ServerSetup.ExchangeName = "default." + txtExchangeName.Text;
            }
            else
            {
                ServerSetup.ExchangeName = "default." + txtExchangeName.Text;
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
                ServerSetup.ChannelName = "default." + txtChannelName.Text;
            }
            else
            {
                ServerSetup.ChannelName = "default." + txtChannelName.Text;
            }
        }

        private void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            var message = new JsonObject
            {
                DeviceName = DeviceName,
                MessageDateTime = DateTime.Now,
                MessageType = ServerSetup.MessageFormat,
                Message = { HeatIndexC = MessageToSend.Text },
            };

            while (!PublishMessage(message, ServerSetup.ExchangeName))
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
                var rxMatch = Regex.Match(ip, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"); //Check IP with Regex expression to match IPv4 requirements
                if (rxMatch.Success || ip.ToLower() == "localhost") //Allows localhost to be used
                {
                    ServerSetup.ServerAddress = ip.ToLower() == "localhost" ? IPAddress.Loopback : IPAddress.Parse(ip);
                }
                else
                {
                    txtServerAddress.Text = "localhost";
                    const string message = "IP Address missmatch. Please provide a valid IPv4 address or \"localhost\" as an address";
                    const string caption = "Error - Address Missmatch";

                    MessageBox.Show(message, caption, MessageBoxButtons.OK);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, LogLevel.Critical);
            }
        }

        private void TxtServerAddress_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TxtServerAddress_OnLostFocus(sender, e);
            }
        }

        private void txtVirtualHost_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
            }
            ServerSetup.VirtualHost = txtVirtualHost.Text;
        }

        private void MessageToSend_OnGotFocus(object sender, RoutedEventArgs e)
        {
            MessageToSend.Text = "";
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            srvEnabled.IsChecked = false;
            srvDisabled.IsChecked = true;

            foreach (var serialPort in SerialPort)
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
            }
        }

        public void Dispose()
        {
            if (_connection.IsOpen)
            {
                if (_channel.IsOpen)
                {
                    _channel.Close();
                }
                _connection.Close();
            }

            foreach (var serialPort in SerialPort)
            {
                serialPort.Dispose();
            }

            if (_systemTimer.IsEnabled)
            {
                _systemTimer.Stop();
            }
        }

        private void txtUserName_TextChanged(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
                ServerSetup.UserName = txtUserName.Text;
            }
            else
            {
                ServerSetup.UserName = txtUserName.Text;
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
                ServerSetup.ServerPort = Convert.ToInt32(txtServerPort.Text);
            }
            else
            {
                ServerSetup.ServerPort = Convert.ToInt32(txtServerPort.Text);
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
                ServerSetup.Password = pwdPassword.Password;
            }
            else
            {
                ServerSetup.Password = pwdPassword.Password;
            }
        }
    }
}