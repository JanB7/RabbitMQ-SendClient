using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;
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
using CheckBox = System.Windows.Controls.CheckBox;
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
    public partial class MainWindow : IDisposable
    {
        /// <summary>
        ///     RabbitMQ Server Information for setup
        /// </summary>
        private static readonly ObservableCollection<CheckListItem> AvailableModbusSerialPorts =
            new ObservableCollection<CheckListItem>();

        private static readonly ObservableCollection<CheckListItem> AvailableSerialPorts =
            new ObservableCollection<CheckListItem>();

        private static readonly string DeviceName = Environment.MachineName;
        private static readonly StackTrace StackTracing = new StackTrace();
        private readonly DispatcherTimer _systemTimer = new DispatcherTimer();
        private int[] _messagesPerSecond = new int[0];

        private ObservableCollection<KeyValuePair<string, int>>[] _messagesSentDataPair =
            new ObservableCollection<KeyValuePair<string, int>>[SerialPort.GetPortNames().Length];

        private double _timeElapsed;

        /// <summary>
        ///     Mainline Executable to the RabbitMQ Client
        /// </summary>
        
        public MainWindow()
        {
            InitializeSerialPortCheckBoxes();
            InitializeComponent();

            SystemVariables.SetupSerial(SerialPort.GetPortNames());

            InitializeSerialPorts();
            InitializeHeartBeatTimer();

            for (var index = 0; index < AvailableSerialPorts.Count; index++)
            {
                var loaded = SystemVariables.GetXML(index);
                if (!loaded)
                {
                    MessageBox.Show(
                        Properties.Resources.MainWindow_MainWindow_FatalError_ConfigurationFile,
                        @"Fatal Error - Configuration Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    CloseSafely();
                    Close();
                    break;
                }
                Array.Resize(ref _messagesPerSecond, _messagesPerSecond.Length + 1);
                _messagesPerSecond[_messagesPerSecond.Length - 1] = 0;
            }
            
            _systemTimer.Start();
        }

        /// <summary>
        ///     Close all open channels and serial ports before system closing
        /// </summary>
        public void Dispose()
        {
            foreach (var serialPort in SystemVariables.SerialPorts)
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

        public void ConvertMessage(string message, int index)
        {

            var jsonMessage = SystemVariables.JsonObjects[index];
            jsonMessage.MessageDateTime = DateTime.Now;
            jsonMessage.MessageType = SystemVariables.ServerInformation[index].MessageFormat;
            if (jsonMessage.DeviceName == null) jsonMessage.DeviceName = DeviceName;

            try
            {
                var indata = JsonConvert.DeserializeObject<SystemVariables.JsonObject>(message);


                var delay = 0;
                while (!PublishMessage(message, index))
                {
                    Thread.Sleep(10);
                    delay += 10;

                    if (delay == 1000)
                        break;
                }
            }
            catch (JsonException ex)
            {
                SystemVariables.SerialCommunications[index].InformationErrors++;
                SystemVariables.SerialPorts[index].Close();

                Thread.Sleep(10); //attempt resyncronization of data delivery.

                SystemVariables.SerialPorts[index].Open();

                if (GlobalSerialFunctions.OutOfControl(index)) //Satistically determined to be out of bounds. Close serial ports
                {
                    //Log Message
                    CloseSerialPortUnexpectedly(index);
                    var sf = StackTracing.GetFrame(0);
                    SystemVariables.LogError(ex, SystemVariables.LogLevel.Critical, sf);
                    MessageBox.Show(
                        Properties.Resources.MainWindow_DataReceivedHandler_ + AvailableSerialPorts[index].Content,
                        Properties.Resources.MainWindow_DataReceivedHandler_JSON_Message_Error, MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
                }
            }
        }

        /// <summary>
        ///     Publishes Message to RabbitMQ server using JSON format
        /// </summary>
        /// <param name="message">JSON type Message. HAS TO BE PREFORMATTED</param>
        /// <param name="index">Index for Dynamic Server Allocation</param>
        /// <returns>Message success state</returns>
        protected bool PublishMessage(SystemVariables.JsonObject message, int index)
        {
            try
            {
                var output = JsonConvert.SerializeObject(message);
                var deliveryTag = new ulong();
                deliveryTag = 0;
                var properties = SystemVariables.FactoryChannel[index].CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "jsonObject";
                var address = new PublicationAddress(ExchangeType.Direct, SystemVariables.ServerInformation[index].ExchangeName, "");
                SystemVariables.FactoryChannel[index].BasicPublish(address, properties, Encoding.UTF8.GetBytes(output));
                SystemVariables.FactoryChannel[index].BasicAcks += (sender, args) =>
                {
                    deliveryTag = args.DeliveryTag;
                } ;
                return deliveryTag != 0;
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
                SystemVariables.LogError(ex, SystemVariables.LogLevel.Critical, sf);
                return false;
            }
        }

        

        protected bool PublishMessage(string message, int index)
        {

            try
            {
                var properties = SystemVariables.FactoryChannel[index].CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "Plain-Text";
                var address = new PublicationAddress(ExchangeType.Direct, SystemVariables.ServerInformation[index].ExchangeName, "");

                SystemVariables.FactoryChannel[index].BasicPublish(address, properties, Encoding.UTF8.GetBytes(message));

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
                SystemVariables.LogError(ex, SystemVariables.LogLevel.Critical, sf);
                return false;
            }
        }

        /// <summary>
        ///     Publishes Message to RabbitMQ server using Plain-Text format
        /// </summary>
        /// <param name="message">Plain-Text type Message. ANY FORMATTING ALLOWED</param>
        /// <param name="index">Index for Dynamic Server Allocation</param>
        /// <returns>Message success state</returns>
        /// <summary>
        ///     Initializes serial port with settings from global settings file.
        ///     TODO write settings to file
        /// </summary>
        /// <param name="index">Index of Global Variable related to CheckboxList</param>
        /// <returns></returns>
        protected bool SerialPortInitialize(int index)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return true;
            try
            {
                if (SystemVariables.SerialPorts[index].IsOpen)
                    while (SystemVariables.SerialPorts[index].IsOpen)
                        SystemVariables.SerialPorts[index].Close();

                //Initializing the Serial Port
                SystemVariables.SerialPorts[index].PortName = SystemVariables.SerialCommunications[index].ComPort;
                SystemVariables.SerialPorts[index].BaudRate = (int) SystemVariables.SerialCommunications[index].BaudRate;
                SystemVariables.SerialPorts[index].Parity = SystemVariables.SerialCommunications[index].SerialParity;
                SystemVariables.SerialPorts[index].StopBits = SystemVariables.SerialCommunications[index].SerialStopBits;
                SystemVariables.SerialPorts[index].DataBits = SystemVariables.SerialCommunications[index].SerialBits;
                SystemVariables.SerialPorts[index].Handshake = SystemVariables.SerialCommunications[index].FlowControl;
                SystemVariables.SerialPorts[index].RtsEnable = SystemVariables.SerialCommunications[index].RtsEnable;
                SystemVariables.SerialPorts[index].ReadTimeout = SystemVariables.SerialCommunications[index].ReadTimeout;
                SystemVariables.SerialPorts[index].DataReceived += DataReceivedHandler;
                SystemVariables.SerialCommunications[index].X();

                SystemVariables.SerialPorts[index].Open();

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                while (SystemVariables.SerialPorts[index].IsOpen) SystemVariables.SerialPorts[index].Close(); //Close port if opened

                MessageBox.Show(@"Access to the port '" + SystemVariables.SerialPorts[index].PortName + @"' is denied.",
                    @"Error opening Port",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
                var sf = StackTracing.GetFrame(0);
                SystemVariables.LogError(ex, SystemVariables.LogLevel.Critical, sf);

                return false;
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                SystemVariables.LogError(ex, SystemVariables.LogLevel.Critical, sf);

                return false;
            }
        }

        private void CloseSafely()
        {
            TabMessageSettings.IsEnabled = false;
            _systemTimer.Stop();

            foreach (var port in SystemVariables.SerialPorts)
                while (port.IsOpen)
                    port.Close();

            foreach (var model in SystemVariables.FactoryChannel)
                while (model.IsOpen)
                    model.Close();
        }

        /// <summary>
        ///     Proivdes a threadsafe way to close the serial ports
        /// </summary>
        /// <param name="index">Index for Dynamic Server Allocation</param>
        private void CloseSerialPortUnexpectedly(int index)
        {
            if (!SystemVariables.SerialPorts[index].IsOpen) return;
            while (SystemVariables.SerialPorts[index].IsOpen)
                SystemVariables.SerialPorts[index].Close();

            Dispatcher.Invoke((MethodInvoker) delegate
            {
                var checkList = AvailableSerialPorts[index];
                checkList.IsChecked = false;
                AvailableSerialPorts.RemoveAt(index);
                AvailableSerialPorts.Insert(index, checkList);
            });
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

                SystemVariables.SerialCommunications[index].TotalInformationReceived++;
                GlobalSerialFunctions.CalculateNpChart(index);
                UpdateGraph(index, AvailableSerialPorts);
            }
            catch (Exception ex)
            {
                //Log Message
                var sf = StackTracing.GetFrame(0);
                SystemVariables.LogError(ex, SystemVariables.LogLevel.Critical, sf);
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
        private void InitializeSerialPortCheckBoxes()
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
                    Uid = i + ":" + Guid.NewGuid()
                };
                AvailableSerialPorts.Add(serialPortCheck);

                var serialModbusCheck = new CheckListItem
                {
                    Content = ports[i],
                    IsChecked = false,
                    Name = ports[i] + "Modbus",
                    Uid = i + ":" + Guid.NewGuid()
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
            foreach (var serialPort in SystemVariables.SerialPorts)
                if (serialPort.IsOpen)
                    serialPort.Close();

            foreach (var model in SystemVariables.FactoryChannel)
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

        private void ResizeArrays()
        {
            var ports = SerialPort.GetPortNames();
            var newLenght = ports.Length;
            for (var i = 0; i < SerialPort.GetPortNames().Length; i++)
                if (AvailableSerialPorts[i].Content != ports[i])
                {
                    var serialPortCheck = new CheckListItem
                    {
                        Content = ports[i],
                        IsChecked = false,
                        Name = ports[i] + "Serial",
                        Uid = i + ":" + Guid.NewGuid()
                    };
                    AvailableSerialPorts.Insert(i, serialPortCheck);

                    var serialModbusCheck = new CheckListItem
                    {
                        Content = ports[i],
                        IsChecked = false,
                        Name = ports[i] + "Modbus",
                        Uid = i + ":" + Guid.NewGuid()
                    };
                    AvailableSerialPorts.Insert(i, serialModbusCheck);

                    Array.Resize(ref _messagesSentDataPair, newLenght);
                    Array.Resize(ref _messagesPerSecond, newLenght);
                    Array.Resize(ref SystemVariables.SerialPorts, newLenght);
                    Array.Resize(ref SystemVariables.SerialCommunications, newLenght);
                    Array.Resize(ref SystemVariables.ServerInformation, newLenght);
                    var factory = SystemVariables.Factory;
                    Array.Resize(ref factory, newLenght);
                    SystemVariables.Factory = factory;

                    var connection = SystemVariables.FactoryConnection;
                    Array.Resize(ref connection, newLenght);
                    SystemVariables.FactoryConnection = connection;

                    var channel = SystemVariables.FactoryChannel;
                    Array.Resize(ref channel, newLenght);
                    SystemVariables.FactoryChannel = channel;
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
            var index = int.Parse(cb.Uid.Substring(0, 1));
            var cbo = (CheckListItem) LstModbusSerial.Items[index];
            if (cb.IsChecked != null) cbo.IsChecked = false;

            //disables Modbus COM Port
            AvailableModbusSerialPorts.RemoveAt(index);
            AvailableModbusSerialPorts.Insert(index, cbo);

            //Enable Port for serial communications
            var setupSerialForm = new SerialPortSetup {SerialPortNum = index};
            var activate = setupSerialForm.ShowDialog(); //Confirm Settings

            if (activate != null && activate.Value)
            {
                var init = SerialPortInitialize(index);
                if (init) return;

                //Initialzation of port failed. Closing port and unchecking it
                cb.IsChecked = false;
                AvailableSerialPorts.RemoveAt(index);
                var cli = new CheckListItem
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
                var cli = new CheckListItem
                {
                    Name = cb.Name,
                    Uid = cb.Uid,
                    Content = cbo.Content,
                    IsChecked = false
                };
                AvailableSerialPorts.Insert(index, cli);
            }
        }

        private void SerialEnabled_CheckboxUnchecked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            var cb = (CheckBox) sender;
            var index = int.Parse(cb.Uid.Substring(0, 1));
            if (SystemVariables.SerialPorts[index].IsOpen)
                SystemVariables.SerialPorts[index].Close();
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
        ///     Disables server. UI Access ONLY. Call by setting UI values.
        /// </summary>
        /// <param name="sender">RadioButton Object</param>
        /// <param name="e">Radio Button Values</param>
        private void SrvDisabled_Checked(object sender, RoutedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            CloseSafely();
        }

        private void StartServer(int index)
        {
            try
            {
                SystemVariables.Factory[index] = GlobalRabbitMqServerFunctions.SetupFactory(index);
                SystemVariables.FactoryConnection[index] = SystemVariables.Factory[index].CreateConnection();

                SystemVariables.FactoryChannel[index] = SystemVariables.FactoryConnection[index].CreateModel();

                SystemVariables.FactoryConnection[index].AutoClose = false;

                GlobalRabbitMqServerFunctions.CreateQueue(true, false, index);
                GlobalRabbitMqServerFunctions.CreateExchange(ExchangeType.Direct, true, false, index);
                GlobalRabbitMqServerFunctions.QueueBind(index);
            }
            catch (BrokerUnreachableException ex)
            {
                var sf = StackTracing.GetFrame(0);
                SystemVariables.LogError(ex, SystemVariables.LogLevel.Critical, sf);
                var message = SystemVariables.ErrorType[1003];
                const string caption = "Broker Unreachable Exception";
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                SystemVariables.LogError(ex, SystemVariables.LogLevel.Critical, sf);
                var message = ex.Message;
                var caption = "Error in: " + ex.Source;
                MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

            if (_messagesSentDataPair.Length != SerialPort.GetPortNames().Length)
                Dispatcher.Invoke((MethodInvoker) ResizeArrays);

            _timeElapsed = (double) _systemTimer.Interval.Milliseconds / 1000;
            if (_timeElapsed > 1)
                for (var index = 0; index < SerialPort.GetPortNames().Length; index++)
                    _messagesPerSecond[index] = 0;
        }

        private void UpdateGraph<T>(int index, T info)
        {
            Dispatcher.Invoke((MethodInvoker) delegate
            {
                if (_messagesSentDataPair[index].Count > 118)
                    _messagesSentDataPair[index].RemoveAt(0);
                var timeNow = DateTime.Now.Minute + ":" + DateTime.Now.Second;
                var ls = new LineSeries();
                var getName = nameof(info);
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
                _messagesPerSecond[index]++;
                _messagesSentDataPair[index].Add(new KeyValuePair<string, int>(timeNow, _messagesPerSecond[index]));
                ls.ItemsSource = _messagesSentDataPair[index];
                LineChart.Series.Add(ls);
            });
        }

        private void AddModbusTCP_Click(object sender, RoutedEventArgs e)
        {
            ///TODO add IP address Management
        }
    }
}