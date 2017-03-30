using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RabbitMQ_SendClient.UI;
using static RabbitMQ_SendClient.SystemVariables;
using static RabbitMQ_SendClient.GlobalRabbitMqServerFunctions;
using static RabbitMQ_SendClient.GlobalSerialFunctions;
using static RabbitMQ_SendClient.General_Classes.ModbusConfig;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.Forms.MessageBox;

namespace RabbitMQ_SendClient
{
    /// <summary>
    ///     Main UI for RabbitMQ Client
    /// </summary>
    public partial class MainWindow : IDisposable
    {
        /// <summary>
        ///     RabbitMQ Server Information for setup
        /// </summary>
        protected internal static readonly ObservableCollection<CheckListItem> AvailableModbusSerialPorts =
            new ObservableCollection<CheckListItem>();

        protected internal static readonly ObservableCollection<CheckListItem> AvailableSerialPorts =
            new ObservableCollection<CheckListItem>();

        private static readonly StackTrace StackTracing = new StackTrace();

        private readonly DateTime _previousTime = new DateTime();
        private readonly DispatcherTimer _systemTimer = new DispatcherTimer();

        protected internal ObservableCollection<KeyValuePair<string, double>>[] MessagesSentDataPair =
            new ObservableCollection<KeyValuePair<string, double>>[0];

        private static readonly string DatabaseLoc = AppDomain.CurrentDomain.BaseDirectory + "Database\\MessageData.mdf";
        private readonly string _connString =
            $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=\"{DatabaseLoc}\";Integrated Security=True";

        private DispatcherTimer[] _modbusTimers = new DispatcherTimer[0];

        private readonly Dictionary<DispatcherTimer, Guid> _modbusTimerId = new Dictionary<DispatcherTimer, Guid>();

        protected internal static List<int> ModbusAddressList = new List<int>();

        private readonly double[] _messagesPerSecond = new double[0];

        /// <summary>
        ///     Mainline Executable to the RabbitMQ Client
        /// </summary>
        public MainWindow()
        {
            InitializeSerialPortCheckBoxes();
            InitializeComponent();

            GetFriendlyDeviceNames();

            InitializeSerialPorts();
            InitializeHeartBeatTimer();

            _systemTimer.Start();
        }

        public static LineSeries[] Lineseries { get; set; } = new LineSeries[0];

        public static string DeviceName { get; } = Environment.MachineName;

        /// <summary>
        ///     Close all open channels and serial ports before system closing
        /// </summary>
        public void Dispose()
        {
            for (var i = 0; i < SerialPort.GetPortNames().Length; i++)
            {
                while(SerialPorts[i].IsOpen)
                    SerialPorts[i].Close();
                SerialPorts[i].Dispose();
                CloseSerialPortUnexpectedly(i);
            }

            //Disposes of timer in a threadsafe manner
            if (_systemTimer.IsEnabled)
                _systemTimer.Stop();
        }

        private void GetFriendlyDeviceNames()
        {
            var loaded = GenerateFriendlies();

            if (!loaded)
            {
                MessageBox.Show(
                    Properties.Resources.MainWindow_MainWindow_FatalError_ConfigurationFile,
                    @"Fatal Error - Configuration Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                CloseSafely();
                Close();
            }
        }

        /// <summary>
        ///     Publishes Message to RabbitMQ
        /// </summary>
        /// <param name="message">JSON/Plain-Text Message. HAS TO BE PREFORMATTED for Json Serialization</param>
        /// <param name="index">Index for Dynamic Server Allocation</param>
        /// <param name="uidGuid"></param>
        /// <returns>Message success state</returns>
        protected bool PublishMessage(string message, int index, Guid uidGuid)
        {
            try
            {
                var properties = FactoryChannel[index].CreateBasicProperties();
                if (SerialCommunications[index].MessageType == "JSON")
                {
                    try
                    {
                        CalculateNpChart(index);
                        JsonConvert.DeserializeObject<Messages[]>(message);
                        properties.ContentType = "jsonObject";

                    }
                    catch (JsonException ex)
                    {
                        if (OutOfControl(index))
                        {
                            var sf = StackTracing.GetFrame(0);
                            LogError(ex, LogLevel.Critical, sf);
                            CloseSerialPortUnexpectedly(index);
                        }
                    }
                }
                else
                {
                    properties.ContentType = "plain-text";
                }



                properties.Persistent = true;

                var address = new PublicationAddress(ExchangeType.Direct,
                    ServerInformation[index].ExchangeName, "");
                FactoryChannel[index].BasicPublish(address, properties, Encoding.UTF8.GetBytes(message));
                FactoryChannel[index].BasicAcks += (sender, args) =>
                {
                    const string sqlString = "DELETE FROM[dbo].[MessageData] WHERE [DeliveryTag] = @uuid";

                    using (var conn = new SqlConnection(_connString))
                    {
                        try
                        {
                            
                            var command = new SqlCommand(sqlString, conn) {CommandType = CommandType.Text,};
                            command.Parameters.AddWithValue("@uuid", uidGuid);
                            conn.Open();
                            command.ExecuteNonQuery();
                            conn.Close();
                        }
                        catch (Exception ex)
                        {
                            if(conn.State == ConnectionState.Open)
                                conn.Close();
                            var sf = StackTracing.GetFrame(0);
                            LogError(ex, LogLevel.Critical, sf);
                        }

                    }
                };
                return true;
            }
            catch (AlreadyClosedException ex)
            {
                var indexOf = ex.Message.IndexOf("\"", StringComparison.Ordinal);
                var indexOff = ex.Message.IndexOf("\"", indexOf + 1, StringComparison.Ordinal);
                var errmessage = ex.Message.Substring(indexOf + 1, indexOff - indexOf - 1);
                MessageBox.Show(errmessage, @"Connection Already Closed", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

                return false;
            }
            catch (Exception ex)
            {
                //Log Message
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
                return false;
            }
        }



        private void CloseSafely()
        {
            TabMessageSettings.IsEnabled = false;
            _systemTimer.Stop();

            foreach (var port in SerialPorts)
                while (port.IsOpen)
                    port.Close();

            foreach (var model in FactoryChannel)
                while (model.IsOpen)
                    model.Close();
        }

        /// <summary>
        ///     Serial Commnuication Event Handler.
        /// </summary>
        /// <param name="sender">COM Port Data Receveived Object</param>
        /// <param name="e">Data Received</param>
        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var sp = (SerialPort) sender;
                var spdata = sp.ReadLine();

                var index = 0;
                var i = 0;
                foreach (var serialPort in AvailableSerialPorts)
                {
                    if (serialPort.Content == sp.PortName)
                    {
                        index = i;
                        break;
                    }
                    i++;
                }

                SerialCommunications[index].TotalInformationReceived++;
                CalculateNpChart(index);
                

                var datInfo = new DataBaseInfo
                {
                    Message = spdata,
                    TimeStamp = DateTime.Now,
                    FriendlyName = FriendlyName[index],
                    Channel = ServerInformation[index].ChannelName,
                    Exchange = ServerInformation[index].ExchangeName,
                    ServerAddress = ServerInformation[index].ServerAddress.ToString(),
                    DeliveryTag = Guid.NewGuid(),
                    DeviceType = sp.PortName
                };

                const string sqlString =
                    "INSERT [dbo].[MessageData] (Message,TimeStamp,FriendlyName,Channel,Exchange,ServerAddress,DeliveryTag,DeviceType)VALUES"
                    +
                    "(@message,@timestamp,@friendlyname,@channel,@exchange,@serveraddress,@deliverytag,@devicetype)";


                using (var conn = new SqlConnection(_connString))
                {
                    var command = new SqlCommand(sqlString, conn)
                    {
                        CommandType = CommandType.Text
                    };
                    command.Parameters.AddWithValue("@message", datInfo.Message);
                    command.Parameters.AddWithValue("@timestamp", datInfo.TimeStamp);
                    command.Parameters.AddWithValue("@friendlyname", datInfo.FriendlyName);
                    command.Parameters.AddWithValue("@channel", datInfo.Channel);
                    command.Parameters.AddWithValue("@exchange", datInfo.Exchange);
                    command.Parameters.AddWithValue("@serveraddress", datInfo.ServerAddress);
                    command.Parameters.AddWithValue("@deliverytag", datInfo.DeliveryTag);
                    command.Parameters.AddWithValue("@devicetype", datInfo.DeviceType);
                    conn.Open();
                    command.ExecuteNonQuery();
                    conn.Close();
                }

                while (PublishMessage(datInfo.Message, index, Guid.Parse(AvailableSerialPorts[index].Uid)))
                {
                    //Retry
                }
            }
            catch (Exception ex)
            {
                //Log Message
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
            }
        }

        /// <summary>
        ///     RabbitMQ Heartbeat Timer. Adjusts value of system information in scrollbar on tick
        /// </summary>
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

        /// <summary>
        ///     Initializes SerialPort checkboxes for both Serial and ModBus communication
        /// </summary>
        private static void InitializeSerialPortCheckBoxes()
        {
            AvailableSerialPorts.Clear();
            AvailableModbusSerialPorts.Clear();

            var ports = SerialPort.GetPortNames();
            for (var i = 0; i < ports.Length; i++)
            {
                var serialPortCheck = new CheckListItem
                {
                    Content = ports[i],
                    IsChecked = false,
                    Name = ports[i] + "Serial",
                    Uid = Guid.NewGuid().ToString()
                };
                AvailableSerialPorts.Add(serialPortCheck);

                var serialModbusCheck = new CheckListItem
                {
                    Content = ports[i],
                    IsChecked = false,
                    Name = ports[i] + "Modbus",
                    Uid = Guid.NewGuid().ToString()
                };
                AvailableModbusSerialPorts.Add(serialModbusCheck);
            }
        }

        /// <summary>
        ///     Provides initializing access to the serial ports
        /// </summary>
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

        /// <summary>
        ///     Provides the required closing processes. Allows for safe shutdown of the program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            foreach (var serialPort in SerialPorts)
                if (serialPort.IsOpen)
                    serialPort.Close();

            foreach (var model in FactoryChannel)
                while (model.IsOpen)
                    model.Close();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var doubleAnimation = new DoubleAnimation
            {
                From = -tbmarquee.ActualWidth,
                To = canMain.ActualWidth,
                RepeatBehavior = RepeatBehavior.Forever,
                Duration = new Duration(TimeSpan.Parse("0:0:10"))
            };
            tbmarquee.BeginAnimation(Canvas.LeftProperty, doubleAnimation);
        }

        /// <summary>
        ///     TODO allow for dynamic updates of serial ports in serial port selection while keeping current states
        /// </summary>
        private static void ResizeSerialSelection()
        {
            var index = 0;
            foreach (var portName in SerialPort.GetPortNames())
            {
                if (portName == AvailableSerialPorts[index].Content)
                {
                }
                index++;
            }
        }

        /// <summary>
        ///     Clears relevant modbus related serial port and enables serial port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialEnabled_CheckboxChecked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            var cb = (CheckBox) sender;
            var uidGuid = Guid.Parse(cb.Uid);

            var index = GetIndex<CheckListItem>(uidGuid);

            var cbo = (CheckListItem) LstModbusSerial.Items[index];
            if (cb.IsChecked != null) cbo.IsChecked = false;

            //disables Modbus COM Port
            AvailableModbusSerialPorts.RemoveAt(index);
            AvailableModbusSerialPorts.Insert(index, cbo);

            //Enable Port for serial communications
            SetupSerial(uidGuid);

            var lineSeries = Lineseries;
            Array.Resize(ref lineSeries, Lineseries.Length + 1);
            Lineseries = lineSeries;

            var setupSerialForm = new SerialPortSetup(uidGuid);
            var activate = setupSerialForm.ShowDialog(); //Confirm Settings

            switch (activate)
            {
                case true:

                    var friendlyNameForm = new VariableConfigure(uidGuid);
                    var nameSet = friendlyNameForm.ShowDialog();
                    if (nameSet != null && !nameSet.Value)
                        goto case null;

                    SetupFactory(uidGuid);
                    ServerInformation[ServerInformation.Length - 1] = SetDefaultSettings(uidGuid);

                    var configureServer = new SetupServer(uidGuid);
                    var serverConfigured = configureServer.ShowDialog();

                    if (serverConfigured != null && !serverConfigured.Value)
                    {
                        //Canceled
                        Array.Resize(ref ServerInformation, ServerInformation.Length - 1);
                        goto case null;
                    }
                    Array.Resize(ref MessagesSentDataPair, MessagesSentDataPair.Length + 1);
                    MessagesSentDataPair[MessagesSentDataPair.Length - 1] =
                        new ObservableCollection<KeyValuePair<string, double>>();

                    SerialPorts[index].DataReceived += DataReceivedHandler;
                    var init = SerialPortInitialize(index, IsInitialized);
                    tbmarquee.Text += $"Serial Port {cb.Name} Active";
                    if (init) return;


                    //Initialzation of port failed. Closing port and unchecking it
                    cb.IsChecked = false;
                    AvailableSerialPorts.RemoveAt(index);
                    var cliT = new CheckListItem
                    {
                        Name = cb.Name,
                        Uid = cb.Uid,
                        Content = cbo.Content,
                        IsChecked = false
                    };
                    AvailableSerialPorts.Insert(index, cliT);
                    tbmarquee.Text.Replace($"Serial Port {cb.Name} Active", "");
                    break;

                case null:
                default: //incl case false
                    cb.IsChecked = false;
                    AvailableSerialPorts.RemoveAt(index);
                    var cliF = new CheckListItem
                    {
                        Name = cb.Name,
                        Uid = cb.Uid,
                        Content = cbo.Content,
                        IsChecked = false
                    };
                    AvailableSerialPorts.Insert(index, cliF);
                    Array.Resize(ref SerialPorts, SerialPorts.Length - 1); //Removes last initialization of Serial Port
                    Array.Resize(ref SerialPorts, SerialPorts.Length - 1);
                    break;
            }
        }

        private void SerialEnabled_CheckboxUnchecked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            var cb = (CheckBox) sender;
            var index = GetIndex<CheckListItem>(Guid.Parse(cb.Uid));
            if (SerialPorts[index].IsOpen)
                SerialPorts[index].Close();
        }

        /// <summary>
        ///     Clears related Serial Port that is not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialModbusEnabled_CheckboxChecked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            var cb = (CheckBox)sender;
            var uidGuid = Guid.Parse(cb.Uid);

            var index = GetIndex<CheckListItem>(uidGuid);

            var cbo = (CheckListItem)LstModbusSerial.Items[index];
            if (cb.IsChecked != null) cbo.IsChecked = false;

            //disables Modbus COM Port
            AvailableSerialPorts.RemoveAt(index);
            AvailableSerialPorts.Insert(index, cbo);

            //Enable Port
            SetupModbusSerial(uidGuid);

            var setupSerialForm = new SerialPortSetup(uidGuid);
            var activate = setupSerialForm.ShowDialog();

            if (activate == null || !activate.Value)
            {
                CloseModbusSerial(uidGuid);
                return;
            }

            index = SerialCommunications.Length - 1;
            
            var port = new SerialPort
            {
                PortName = SerialCommunications[index].ComPort,
                BaudRate = (int)SerialCommunications[index].BaudRate,
                Parity =SerialCommunications[index].SerialParity,
                StopBits = SerialCommunications[index].SerialStopBits,
                DataBits = SerialCommunications[index].SerialBits,
                Handshake = SerialCommunications[index].FlowControl,
                RtsEnable = SerialCommunications[index].RtsEnable,
                ReadTimeout = SerialCommunications[index].ReadTimeout
            };

            InitializeModbusClient(port);

            Array.Resize(ref _modbusTimers, _modbusTimers.Length + 1);
            _modbusTimers[_modbusTimers.Length - 1] = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000),
                IsEnabled = false
            };
            _modbusTimers[_modbusTimers.Length - 1].Tick += ModbusTimerOnTick;
            _modbusTimerId.Add(_modbusTimers[_modbusTimers.Length - 1], uidGuid);

            SetupFactory(uidGuid);
            ServerInformation[ServerInformation.Length - 1] = SetDefaultSettings(uidGuid);

            var configureServer = new SetupServer(uidGuid);
            var serverConfigured = configureServer.ShowDialog();

            if (serverConfigured != null && !serverConfigured.Value)
            {
                Array.Resize(ref ServerInformation, ServerInformation.Length -1);
            }
            InitializeModbusClient(port);
            tbmarquee.Text += $"Serial Port {cb.Name} Active";

            if (port.IsOpen)
            {

                _modbusTimers[_modbusTimers.Length - 1].IsEnabled = true;
                return;
            }

            cb.IsChecked = false;
            AvailableModbusSerialPorts.RemoveAt(index);
            var cliT = new CheckListItem
            {
                Name = cb.Name,
                Uid = cb.Uid,
                Content = cbo.Content,
                IsChecked = false
            };
            AvailableSerialPorts.Insert(index, cliT);
            tbmarquee.Text.Replace($"Serial Port {cb.Name} Active", "");
        }

        private void ModbusTimerOnTick(object sender, EventArgs eventArgs)
        {
            _modbusTimerId.TryGetValue((DispatcherTimer)sender, out Guid uidGuid);
            var modbusInformation = modb
        }


        /// <summary>
        ///     Updates infomration on Statusbar on what system is exeperiencing.
        /// </summary>
        /// <param name="sender">System Timer Thread Object</param>
        /// <param name="eventArgs">Timer Arguments</param>
        private void SystemTimerOnTick(object sender, EventArgs eventArgs)
        {
            //Prevents code from running before intialization
            if (!IsInitialized) return;

            ResizeSerialSelection();
            for (var i = 0; i < AvailableSerialPorts.Count; i++)
            {
                if(AvailableSerialPorts[i].IsChecked)
                    UpdateGraph(i, nameof(AvailableSerialPorts));
                if(AvailableModbusSerialPorts[i].IsChecked)
                    UpdateGraph(i,nameof(AvailableModbusSerialPorts));

            }

            for (var i = 0; i < _messagesPerSecond.Length; i++)
            {
                _messagesPerSecond[i] = 0.0;
            }
            
        }

        private void UpdateGraph(int index, string getName)
        {
            var timeElapsed = DateTime.Now - _previousTime;

            if (timeElapsed < TimeSpan.FromSeconds(1))
                return; //Only update 1ce per second
            
            string title;
            switch (getName)
            {
                case "AvailableSerialPorts":
                    title = AvailableSerialPorts[index].Content;
                    break;

                case "AvailableModbusSerialPorts":
                    title = AvailableModbusSerialPorts[index].Content;
                    break;

                default:
                    title = "Total";
                    break;
            }
            Dispatcher.Invoke((MethodInvoker) delegate
            {
                if (MessagesSentDataPair[index].Count > 60)
                    MessagesSentDataPair[index].RemoveAt(0);
                var timeNow = DateTime.Now.Minute + ":" + DateTime.Now.Second;
                _messagesPerSecond[index]++;


                if (Lineseries[index] != null && (string) Lineseries[index].Title == title)

                {
                    MessagesSentDataPair[index].Add(new KeyValuePair<string, double>(timeNow,
                        _messagesPerSecond[index] / timeElapsed.TotalSeconds));
                }
                else
                {
                    Lineseries[index] = new LineSeries
                    {
                        ItemsSource = MessagesSentDataPair[index],
                        DependentValuePath = "Value",
                        IndependentValuePath = "Key",
                        Title = title
                    };
                    LineChart.Series.Add(Lineseries[index]);
                    MessagesSentDataPair[index].Add(new KeyValuePair<string, double>(timeNow,
                        _messagesPerSecond[index] / timeElapsed.TotalSeconds));
                }
            });
        }

        ///TODO add IP address Management
        private void AddModbusTCP_Click(object sender, RoutedEventArgs e)
        {
            //
        }

        protected internal struct CheckListItem
        {
            public string Content { get; set; }
            public bool IsChecked { get; set; }
            public string Name { get; set; }
            public string Uid { get; set; }
        }

        private struct DataBaseInfo
        {
            internal string Message { get; set; }
            internal DateTime TimeStamp { get; set; }
            internal string FriendlyName { get; set; }
            internal string Channel { get; set; }
            internal string Exchange { get; set; }
            internal string ServerAddress { get; set; }
            internal Guid DeliveryTag { get; set; }
            internal string DeviceType { get; set; }
        }
    }
}