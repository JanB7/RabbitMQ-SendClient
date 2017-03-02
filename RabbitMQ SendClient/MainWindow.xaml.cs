using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using RabbitMQ.Client;
using Newtonsoft.Json;
using System.IO.Ports;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Input;
using RabbitMQ.Client.Framing.Impl;
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
        private readonly SerialPort _serialPort = new SerialPort();
        static TraceSource ts = new TraceSource("MainWindow");

        private static IConnectionFactory _factory = new ConnectionFactory();
        private static IConnection _connection;
        private static IModel _channel;


        static readonly string DeviceName = Environment.MachineName;
        private IDictionary<int, string> errorType { get; set; } = new Dictionary<int, string>()
        {
            {1001,"RabbitMQ Exception - Broker Unreachable"}
        };



        protected SerialCommunication SerialSetup = new SerialCommunication()
        {
            ComPort = "COM1",
            BaudRate = BaudRates.BaudRate9600,
            SerialBits = 8,
            SerialParity = Parity.None,
            SerialStopBits = StopBits.One,
            FlowControl = Handshake.None,
            RtsEnable = false,
            ReadTimeout = 250
        };

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

        public MainWindow()
        {
            InitializeComponent();

            //RabbitMQ Server Configuration
            cboAutoRecovery.SelectedIndex = 0;

            //Serial Communication Setup
            var ports = SerialPort.GetPortNames();
            cboMessageType.Items.Add("Serial");
            cboMessageType.Items.Add("Modbus");
            cboMessageType.SelectedIndex = 0;

            if (ports.Length == 0)
            {
                cboPortNames.IsEnabled = false;
                cboPortNames.Items.Add("No Ports");
                cboPortNames.SelectedIndex = 0;
            }
            else
            {
                foreach (var port in ports)
                {
                    cboPortNames.Items.Add(port);
                }
                cboPortNames.SelectedIndex = 0;
                SerialSetup.ComPort = cboPortNames.Items[cboPortNames.SelectedIndex].ToString();

                foreach (var rate in Enum.GetValues(typeof(BaudRates)))
                {
                    cboBaudRate.Items.Add(rate);
                }
                cboBaudRate.Text = BaudRates.BaudRate9600.ToString();
                cboBaudRate.IsEnabled = true;

                for (int i = 4; i != 9; i++)
                {
                    cboDataBits.Items.Add(i);
                }
                cboDataBits.SelectedIndex = cboDataBits.Items.Count - 1;
                cboDataBits.IsEnabled = true;

                foreach (var stopBit in Enum.GetValues(typeof(StopBits)))
                {
                    cboStopBits.Items.Add(stopBit);
                }
                cboStopBits.SelectedIndex = 0;
                cboStopBits.IsEnabled = true;

                foreach (var parity in Enum.GetValues(typeof(Parity)))
                {
                    cboParity.Items.Add(parity);
                }
                cboParity.SelectedIndex = 0;
                cboParity.IsEnabled = true;

                foreach (var shake in Enum.GetValues(typeof(Handshake)))
                {
                    cboFlowControl.Items.Add(shake);
                }
                cboFlowControl.SelectedIndex = 0;
                cboFlowControl.IsEnabled = true;

                sldReadTimeout.IsEnabled = true;
            }

        }

        protected static bool PublishMessage(JsonObject message, string exchangeName)
        {
            var success = false;
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
                TopologyRecoveryEnabled = ServerSetup.AutoRecovery

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

        private void txtUserName_TextChange(object sender, TextChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            if (srvEnabled.IsChecked != null && srvEnabled.IsChecked.Value)
            {
                srvEnabled.IsChecked = false;
                srvDisabled.IsChecked = true;
                ServerSetup.UserName = this.txtUserName.Text;
            }
            else
            {
                ServerSetup.UserName = this.txtUserName.Text;
            }

        }

        private void txtServerPort_TextChange(object sender, TextChangedEventArgs e)
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

        private void pwdPassword_TextChange(object sender, TextChangedEventArgs e)
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

        private void txtServerAddress_TextChanged(object sender, TextChangedEventArgs e)
        {


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

            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                LogError(ex, LogLevel.Critical);
                var message = errorType[1001];
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
            //prevents execution during initialization
            if (!IsInitialized) return;

            SerialSetup.ComPort = cboPortNames.Items[cboPortNames.SelectedIndex].ToString();
            Serial_Port_Initialize(_serialPort, SerialSetup);
        }

        protected void Serial_Port_Initialize(SerialPort port, SerialCommunication serialCommunication)
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
                port.PortName = serialCommunication.ComPort;
                port.BaudRate = (int)serialCommunication.BaudRate;
                port.Parity = serialCommunication.SerialParity;
                port.StopBits = serialCommunication.SerialStopBits;
                port.DataBits = serialCommunication.SerialBits;
                port.Handshake = serialCommunication.FlowControl;
                port.RtsEnable = serialCommunication.RtsEnable;
                port.ReadTimeout = serialCommunication.ReadTimeout;
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

            SerialSetup.BaudRate = rates;
            Serial_Port_Initialize(_serialPort, SerialSetup);
        }

        private void cboDataBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var bits = cboDataBits.Items[cboDataBits.SelectedIndex].ToString();
            SerialSetup.SerialBits = int.Parse(bits);
            Serial_Port_Initialize(_serialPort, SerialSetup);
        }

        private void cboFlowControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;


            var flowControl = (Handshake)Enum.Parse(typeof(Handshake), cboFlowControl.Items[cboFlowControl.SelectedIndex].ToString());

            SerialSetup.FlowControl = flowControl;
            switch (flowControl)
            {
                case Handshake.None:
                    SerialSetup.RtsEnable = false;
                    break;
                case Handshake.RequestToSend:
                    SerialSetup.RtsEnable = true;
                    break;
                case Handshake.RequestToSendXOnXOff:
                    SerialSetup.RtsEnable = true;
                    break;
                case Handshake.XOnXOff:
                    SerialSetup.RtsEnable = true;
                    break;
                default:
                    goto case Handshake.None;
            }
            Serial_Port_Initialize(_serialPort, SerialSetup);
        }

        private void sldReadTimeout_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var timeout = (int)sldReadTimeout.Value;
            txtReadTimeout.Text = timeout.ToString();

            SerialSetup.ReadTimeout = timeout;
            Serial_Port_Initialize(_serialPort, SerialSetup);
        }

        private void cboMessageType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            txtMessageType.Text = string.Concat("Using: ", cboMessageType.Items[cboMessageType.SelectedIndex].ToString());

            if (cboMessageType.SelectedIndex == 0)
            {
                Serial_Port_Initialize(_serialPort, SerialSetup);
            }
            else
            {
                _serialPort.Close();
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
            else
            {
                try
                {
                    var rxMatch = Regex.Match(ip, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
                    if (rxMatch.Success || ip.ToLower() == "localhost")
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

            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        public void Dispose()
        {
            _serialPort.Dispose();
        }
    }
}
