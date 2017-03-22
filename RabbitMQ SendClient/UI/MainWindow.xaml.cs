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
using static RabbitMQ_SendClient.GlobalSerialFunctions;
using static RabbitMQ_SendClient.GlobalRabbitMqServerFunctions;
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
        private readonly DispatcherTimer _systemTimer = new DispatcherTimer();

        private DateTime _previousTime;

        protected internal ObservableCollection<KeyValuePair<string, double>>[] MessagesSentDataPair =
            new ObservableCollection<KeyValuePair<string, double>>[0];

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

        public static string DeviceName { get; } = Environment.MachineName;

        /// <summary>
        ///     Close all open channels and serial ports before system closing
        /// </summary>
        public void Dispose()
        {
            foreach
                (var serialPort in SerialPorts)
            {
                if
                    (serialPort.IsOpen)
                    while (!serialPort.IsOpen)
                        serialPort.Close();

                serialPort.Dispose();
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
        protected bool PublishMessage(dynamic message, int index, Guid uidGuid)
        {
            try
            {
                var properties = FactoryChannel[index].CreateBasicProperties();
                string output = null;

                //Create
                var type = new Dictionary<Type, Action>
                {
                    {
                        typeof(JsonObject), () =>
                        {
                            output = JsonConvert.SerializeObject(message);
                            properties.ContentType = "jsonObject";
                        }
                    },
                    {
                        typeof(string), () =>
                        {
                            output = message;
                            properties.ContentType = "plain-text";
                        }
                    }
                };

                type[message](); //Assigns value to string
                properties.Persistent = true;

                var address = new PublicationAddress(ExchangeType.Direct,
                    ServerInformation[index].ExchangeName, "");
                FactoryChannel[index].BasicPublish(address, properties, Encoding.UTF8.GetBytes(output));
                FactoryChannel[index].BasicAcks += (sender, args) =>
                {
                    const string connstring = @"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True";

                    var sqlString = $"DELETE FROM dbo.DataControl WHERE DeliveryTag = {uidGuid}";

                    using (var conn = new SqlConnection(connstring))
                    {
                        conn.Open();
                        var command = new SqlCommand(sqlString, conn);
                        command.ExecuteNonQuery();
                        conn.Close();
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

        /// <summary>
        ///     Initializes serial port with settings from global settings file.
        ///     TODO write settings to file
        /// </summary>
        /// <param name="index">Index of Global Variable related to CheckboxList</param>
        /// <returns>Success of the initiliation</returns>
        protected bool SerialPortInitialize(int index)
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
                SerialPorts[index].BaudRate = (int) SerialCommunications[index].BaudRate;
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
                while (SerialPorts[index].IsOpen)
                    SerialPorts[index].Close(); //Close port if opened

                MessageBox.Show(@"Access to the port '" + SerialPorts[index].PortName + @"' is denied.",
                    @"Error opening Port",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);

                return false;
            }
            catch (Exception ex)
            {
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
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
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
                UpdateGraph(index, nameof(AvailableSerialPorts));

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

                var sqlString =
                    "INSERT dbo.DataControl (Message,TimeStamp,FriendlyName,Channel,Exchange,ServerAddress,DeliveryTag,DeviceType) " +
                    $"VALUES ('{datInfo.Message}','{datInfo.TimeStamp}','{datInfo.FriendlyName}','{datInfo.Channel}','{datInfo.Exchange}'" +
                    $",'{datInfo.ServerAddress}','{datInfo.DeliveryTag}','{datInfo.DeviceType}')";

                const string connstring = @"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True";

                using (var conn = new SqlConnection(connstring))
                {
                    conn.Open();
                    var command = new SqlCommand(sqlString, conn)
                    {
                        CommandType = CommandType.Text
                    };
                    command.ExecuteNonQuery();
                    conn.Close();
                }

                try
                {
                    var indata = JsonConvert.DeserializeObject<JsonObject>(datInfo.Message);
                    var workingThread = new BackgroundWorker();
                    workingThread.DoWork += delegate
                    {
                        while (PublishMessage(indata, index, Guid.Parse(AvailableSerialPorts[index].Uid)))
                        {
                            //do nothing
                        }
                    };
                }
                catch (JsonException)
                {
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
                    ServerInformation[ServerInformation.Length - 1] = setDefaultSettings(uidGuid);

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

                    var init = SerialPortInitialize(index);
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
                    tbmarquee.Text += $"Serial Port {cb.Name} Active";
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

            var cb = (CheckBox) sender;
            var index = int.Parse(cb.Uid.Substring(0, 1));
            var cbo = (CheckListItem) LstSerial.Items[index];
            if (cb.IsChecked != null) cbo.IsChecked = false;

            AvailableSerialPorts.RemoveAt(index);
            AvailableSerialPorts.Insert(index, cbo);

            //Enable Port
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
        }

        private void UpdateGraph(int index, string getName)
        {
            Dispatcher.Invoke((MethodInvoker) delegate
            {
                if (MessagesSentDataPair[index].Count > 118)
                    MessagesSentDataPair[index].RemoveAt(0);
                var timeNow = DateTime.Now.Minute + ":" + DateTime.Now.Second;
                const double messagesPerSecond = 1.00;
                var timeElapsed = DateTime.Now - _previousTime;
                var ls = new LineSeries();
                switch (getName)
                {
                    case "AvailableSerialPorts":
                        ls.Title = AvailableSerialPorts[index].Content;
                        break;

                    case "AvailableModbusSerialPorts":
                        ls.Title = AvailableModbusSerialPorts[index].Content;
                        break;

                    default:
                        ls.Title = "Total";
                        break;
                }

                ls.DependentValuePath = "Value";
                ls.IndependentValuePath = "Key";

                MessagesSentDataPair[index].Add(new KeyValuePair<string, double>(timeNow,
                    messagesPerSecond / timeElapsed.TotalSeconds));
                ls.ItemsSource = MessagesSentDataPair[index];
                LineChart.Series.Add(ls);
            });
        }

        ///TODO add IP address Management
        private void AddModbusTCP_Click(object sender, RoutedEventArgs e)
        {
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