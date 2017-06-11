using static RabbitMQ_SendClient.General_Classes.FormControl;
using static RabbitMQ_SendClient.General_Classes.GlocalTCPFunctions;
using static RabbitMQ_SendClient.General_Classes.ModbusConfig;
using static RabbitMQ_SendClient.GlobalRabbitMqServerFunctions;
using static RabbitMQ_SendClient.GlobalSerialFunctions;
using static RabbitMQ_SendClient.SystemVariables;
using static RabbitMQ_SendClient.General_Classes.GlobalOpcFunctions;

namespace RabbitMQ_SendClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Ports;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.DataVisualization.Charting;
    using System.Windows.Forms;
    using System.Windows.Media.Animation;
    using System.Windows.Threading;
    using EasyModbus.Exceptions;
    using Newtonsoft.Json;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Exceptions;
    using UI;
    using Button = System.Windows.Controls.Button;
    using CheckBox = System.Windows.Controls.CheckBox;
    using MessageBox = System.Windows.Forms.MessageBox;

    /// <summary>
    /// Main UI for RabbitMQ Client 
    /// </summary>
    public partial class MainWindow : IDisposable
    {
        /// <summary>
        /// Mainline Executable to the RabbitMQ Client 
        /// </summary>
        public MainWindow()
        {
            InitializeSerialPortCheckBoxes();
            InitializeComponent();

            GetFriendlyDeviceNames();

            InitializeSerialPorts();
            InitializeHeartBeatTimer();
            lstModbusTCP.DataContext = ModbusTcp;
            //lstErrors.DataContext = ErrorList;

            _systemTimer.Start();
        }

        #region Variables & Structures

        public static LineSeries[] Lineseries { get; set; } = new LineSeries[0];

        public static string DeviceName { get; } = Environment.MachineName;

        protected internal static readonly ObservableCollection<CheckListItem> AvailableModbusSerialPorts =
            new ObservableCollection<CheckListItem>();

        protected internal static readonly ObservableCollection<CheckListItem> AvailableSerialPorts =
            new ObservableCollection<CheckListItem>();

        private static readonly ObservableCollection<CheckListItem> ModbusTcp =
            new ObservableCollection<CheckListItem>();

        public static readonly ObservableCollection<ListBoxItem> ErrorList = new ObservableCollection<ListBoxItem>();

        public static readonly ObservableCollection<CheckListItem> OpcUaList = new ObservableCollection<CheckListItem>();

        /// <summary>
        /// <para>
        /// Observable collection containing struct of <see cref="MessageDataHistory" /> 
        /// </para>
        /// <example> Struct Format 
        /// <code>
        /// protected internal struct MessageDataHistory
        /// {
        ///     internal KeyValuePair&lt;string, double&gt; KeyPair { get; set; }
        ///     internal Guid UidGuid { get; set; }
        /// }
        /// </code>
        /// </example> 
        /// </summary>
        internal static ObservableCollection<MessageDataHistory>[] MessagesSentDataPair =
            new ObservableCollection<MessageDataHistory>[0];

        private static readonly StackTrace StackTracing = new StackTrace();

        internal static double[] MessagesPerSecond = new double[0];
        private static readonly Dispatcher UiDispatcher = Dispatcher.CurrentDispatcher;

        public struct CheckListItem
        {
            public string Content { get; set; }
            public bool IsChecked { get; set; }
            public string ItemName { get; set; }
            public string UidGuid { get; set; }
        }

        /// <summary>
        /// Contains KeyPairValue(string, double) and UidGuid 
        /// </summary>
        protected internal struct MessageDataHistory
        {
            internal KeyValuePair<string, double> KeyPair { get; set; }
            internal Guid UidGuid { get; set; }
        }

        private readonly DateTime _previousTime = new DateTime();
        private readonly DispatcherTimer _systemTimer = new DispatcherTimer();

        #endregion Variables & Structures

        /// <summary>
        /// Close all open channels and serial ports before system closing 
        /// </summary>
        public void Dispose()
        {
            for (var i = 0; i < SerialPort.GetPortNames().Length; i++)
            {
                while (SerialPorts[i].IsOpen)
                {
                    SerialPorts[i].Close();
                }
                SerialPorts[i].Dispose();
                CloseSerialPortUnexpectedly(i, UiDispatcher);
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
        /// Publishes Message to RabbitMQ 
        /// </summary>
        /// <param name="message">
        /// JSON/Plain-Text Message. HAS TO BE PREFORMATTED for Json Serialization 
        /// </param>
        /// <param name="index">
        /// Index for Dynamic Server Allocation 
        /// </param>
        /// <param name="uidGuid">
        /// </param>
        /// <returns>
        /// Message success state 
        /// </returns>
        private static bool? PublishMessage(string message, int index, Guid uidGuid)
        {
            try
            {
                var properties = FactoryChannel[index].CreateBasicProperties();
                if (SerialCommunications[index].MessageType == "JSON")
                    try
                    {
                        //CalculateNpChart(index);
                        JsonConvert.DeserializeObject<Messages[]>(message);
                        properties.ContentType = "jsonObject";
                    }
                    catch (JsonException ex)
                    {
                        /*if (OutOfControl(index))
                        {
                            var sf = StackTracing.GetFrame(0);
                            LogError(ex, LogLevel.Critical, sf);
                            CloseSerialPortUnexpectedly(index, UiDispatcher);
                            return true;
                        }*/
                    }
                else
                    properties.ContentType = "plain-text";

                properties.Persistent = true;

                var address = new PublicationAddress(ExchangeType.Direct,
                    ServerInformation[index].ExchangeName, "");
                FactoryChannel[index].BasicPublish(address, properties, Encoding.UTF8.GetBytes(message));
                FactoryChannel[index].BasicAcks += (sender, args) => { RemoveProtectedData(uidGuid); };
                return true;
            }
            catch (AlreadyClosedException ex)
            {
                var indexOf = ex.Message.IndexOf("\"", StringComparison.Ordinal);
                var indexOff = ex.Message.IndexOf("\"", indexOf + 1, StringComparison.Ordinal);
                var errmessage = ex.Message.Substring(indexOf + 1, indexOff - indexOf - 1);
                MessageBox.Show(errmessage, @"Connection Already Closed", MessageBoxButtons.OK,
                    MessageBoxIcon.Asterisk);

                return null;
            }
            catch (Exception ex)
            {
                //Log Message
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
                return null;
            }
        }

        private void CloseSafely()
        {
            TabMessageSettings.IsEnabled = false;
            _systemTimer.Stop();

            foreach (var port in SerialPorts)
            {
                while (port.IsOpen)
                {
                    port.Close();
                }
            }

            foreach (var modbusClient in ModbusClients)
            {
                modbusClient.Disconnect();
            }

            foreach (var model in FactoryChannel)
            {
                while (model.IsOpen)
                {
                    try
                    {
                        model.Close();
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Serial Commnuication Event Handler. 
        /// </summary>
        /// <param name="sender">
        /// COM Port Data Receveived Object 
        /// </param>
        /// <param name="e">
        /// Data Received 
        /// </param>
        internal static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var sp = (SerialPort)sender;
                var spdata = sp.ReadLine();

                var i = AvailableSerialPorts.TakeWhile(serialPort => serialPort.Content != sp.PortName).Count();
                var index = GetIndex<SerialCommunication>(Guid.Parse(AvailableSerialPorts[i].UidGuid));

                SerialCommunications[index].TotalInformationReceived++;
                //CalculateNpChart(index);

                ProtectData(Guid.Parse(AvailableSerialPorts[index].UidGuid), spdata, sp.PortName);

                while (!PublishMessage(spdata, index, Guid.Parse(AvailableSerialPorts[i].UidGuid)) ?? false)
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
        /// RabbitMQ Heartbeat Timer. Adjusts value of system information in scrollbar on tick 
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
        /// Initializes SerialPort checkboxes for both Serial and ModBus communication 
        /// </summary>
        private static void InitializeSerialPortCheckBoxes()
        {
            AvailableSerialPorts.Clear();
            AvailableModbusSerialPorts.Clear();

            var ports = SerialPort.GetPortNames();
            foreach (var t in ports)
            {
                var serialPortCheck = new CheckListItem
                {
                    Content = t,
                    IsChecked = false,
                    ItemName = t + "Serial",
                    UidGuid = Guid.NewGuid().ToString()
                };
                AvailableSerialPorts.Add(serialPortCheck);

                var serialModbusCheck = new CheckListItem
                {
                    Content = t,
                    IsChecked = false,
                    ItemName = t + "Modbus",
                    UidGuid = Guid.NewGuid().ToString()
                };
                AvailableModbusSerialPorts.Add(serialModbusCheck);
            }
        }

        /// <summary>
        /// Provides initializing access to the serial ports 
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
        /// Provides the required closing processes. Allows for safe shutdown of the program 
        /// </summary>
        /// <param name="sender">
        /// The source of the event. 
        /// </param>
        /// <param name="e">
        /// An <see cref="T:System.ComponentModel.CancelEventArgs"> CancelEventArgs </see> that
        /// contains the event data.
        /// </param>
        /// <remarks>
        /// </remarks>
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            CloseSafely();
        }

        /// <summary>
        /// Main Window Loading 
        /// </summary>
        /// <param name="sender">
        /// The source of the event. 
        /// </param>
        /// <param name="e">
        /// An <see cref="T:System.Windows.RoutedEventArgs"> RoutedEventArgs </see> that contains the
        /// event data.
        /// </param>
        /// <remarks>
        /// </remarks>
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
            doubleAnimation.BeginAnimation(Canvas.LeftProperty, doubleAnimation);
        }

        /// <summary>
        /// TODO allow for dynamic updates of serial ports in serial port selection while keeping
        /// current states
        /// </summary>
        private static void ResizeSerialSelection()
        {
            var ports = SerialPort.GetPortNames();
            for (var i = 0; i < ports.Length; i++)
            {
                if ((AvailableSerialPorts.Count - 1) < i) break;
                if (ports[i] == AvailableSerialPorts[i].Content)
                {
                }
            }
        }

        /// <summary>
        /// Clears relevant modbus related serial port and enables serial port 
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        private void SerialEnabled_CheckboxChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;
            var uidGuid = Guid.Parse(((CheckBox)sender).Uid);

            ChangeCheck(uidGuid); //Removes check and disables port

            if (!UserConfigureSerial(uidGuid) ?? true)
            {
                UserCancelConfigureSerial(uidGuid);
                return;
            }

            if (!GetFriendlyName(uidGuid) ?? true)
            {
                UserCancelConfigureSerial(uidGuid);
                return;
            }

            if (!UserConfigureFactory(uidGuid) ?? true)
            {
                UserCancelConfigureSerial(uidGuid);
                ShutdownFactory(uidGuid);
                return;
            }

            if (!SerialPortInitialize(SerialPorts.Length - 1, this.IsInitialized))
            {
                UserCancelConfigureSerial(uidGuid);
                ShutdownFactory(uidGuid);
            }
            else
            {
                tbmarquee.Text +=
                    $"Serial Port {AvailableSerialPorts[GetIndex<CheckListItem>(uidGuid)].ItemName} Active ";
            }
        }

        private void SerialEnabled_CheckboxUnchecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;

            var uidGuid = Guid.Parse(((CheckBox)sender).Uid);
            ShutdownFactory(uidGuid);
            UserUnchekcSerial(uidGuid);
        }

        private void SerialModbusEnabled_OnUnchecked(object sender, RoutedEventArgs e)
        {
            var uidGuid = Guid.Parse(((CheckBox)sender).Uid);
            tbmarquee.Text = tbmarquee.Text.Replace(
                $"Serial Port {AvailableModbusSerialPorts[GetIndex<CheckListItem>(uidGuid)].ItemName} Active ", "");
        }

        /// <summary>
        /// Clears related Serial Port that is not 
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        private void SerialModbusEnabled_CheckboxChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized) return;

            var uidGuid = Guid.Parse(((CheckBox)sender).Uid);
            ChangeCheck(uidGuid);

            if (!UserConfigureModbusSerial(uidGuid) ?? true)
            {
                UserCancelConfigureModbusSerial(uidGuid);
                return;
            }
            if (!UserConfigureSerialModbusAddresses(uidGuid) ?? true)
            {
                UserCancelConfigureModbusAddresses(uidGuid);

                MessageBox.Show(@"Failed to create Modbus Configuration", @"ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (!ModbusClients[ModbusClients.Length - 1].Connected || (!UserConfigureFactory(uidGuid) ?? true))
            {
                UserCancelConfigureModbusAddresses(uidGuid);
                ShutdownFactory(uidGuid);
                MessageBox.Show(@"Failed to open RabbitMQ Connection", @"ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else
            {
                InitializeModbusClient(SerialPorts[SerialPorts.Length - 1]);
                InitializeRead(uidGuid);
                ModbusControls[ModbusControls.Length - 1].ModbusTimers.IsEnabled = true;
                tbmarquee.Text +=
                    $"Serial Port {AvailableModbusSerialPorts[GetIndex<CheckListItem>(uidGuid)].ItemName} Active";
            }
        }

        private void InitializeRead(Guid uidGuid)
        {
            var index = ModbusControls.Select((val, ind) =>
                    new { ind, val })
                .First(e => e.val.UidGuid == uidGuid)
                .ind;
            var modbusItem = ModbusControls.FirstOrDefault(e => e.UidGuid == uidGuid);
            var message = "";

            foreach (var modbusAddress in modbusItem.ModbusAddressList)
            {
                var address = modbusAddress.Item5;

                if (modbusAddress.Item1)
                    try
                    {
                        SerialCommunications[index].TotalInformationReceived++;
                        //CalculateNpChart(index);
                        while (true)
                        {
                            try
                            {
                                var readCoil = ModbusClients[index].ReadCoils(address, 1);
                                message =
                                    readCoil.Aggregate(message, (current, b) => current + b.ToString() + "\n");
                            }
                            catch (IOException)
                            {
                                Thread.Sleep(100);

                                continue;
                            }
                            break;
                        }

                        ProtectData(uidGuid, message, ModbusClients[index].IPAddress + "1" + modbusAddress.Item5);

                        PublishMessage(message, index, uidGuid);

                        UpdateGraph(uidGuid,
                            AvailableModbusSerialPorts[
                                    AvailableModbusSerialPorts.Select((val, ind) => new { ind, val })
                                        .First(e => e.val.UidGuid == uidGuid.ToString())
                                        .ind]
                                .ItemName);
                    }
                    catch (CRCCheckFailedException crcCheckFailedException)
                    {
                        /*if (OutOfControl(index))
                        {
                            var sf = StackTracing.GetFrame(0);
                            LogError(crcCheckFailedException, LogLevel.Critical, sf);
                            MessageBox.Show("CRC Failure. Please check settings and connection",
                                "CRC Error Check Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            CloseModbusUnexpectedly(uidGuid);
                            ResetCheckBox(AvailableModbusSerialPorts[index]);
                            return;
                        }*/
                    }

                if (modbusAddress.Item2)
                    try
                    {
                        SerialCommunications[index].TotalInformationReceived++;
                        //CalculateNpChart(index);

                        while (true)
                        {
                            try
                            {
                                var readDiscrete = ModbusClients[index].ReadDiscreteInputs(address, 2);
                                message =
                                    readDiscrete.Aggregate(message,
                                        (current, b) => current + b.ToString() + "\n");
                            }
                            catch (IOException)
                            {
                                Thread.Sleep(100);

                                continue;
                            }
                            break;
                        }

                        ProtectData(uidGuid, message, ModbusClients[index].IPAddress + "1" + modbusAddress.Item5);

                        PublishMessage(message, index, uidGuid);

                        UpdateGraph(uidGuid,
                            AvailableModbusSerialPorts[
                                    AvailableModbusSerialPorts.Select((val, ind) => new { ind, val })
                                        .First(e => e.val.UidGuid == uidGuid.ToString())
                                        .ind]
                                .ItemName);
                    }
                    catch (CRCCheckFailedException crcCheckFailedException)
                    {
                        /*if (OutOfControl(index))
                        {
                            var sf = StackTracing.GetFrame(0);
                            LogError(crcCheckFailedException, LogLevel.Critical, sf);
                            MessageBox.Show("CRC Failure. Please check settings and connection",
                                "CRC Error Check Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            CloseModbusUnexpectedly(uidGuid);
                            ResetCheckBox(AvailableModbusSerialPorts[index]);
                            return;
                        }*/
                    }

                if (modbusAddress.Item3)
                    try
                    {
                        SerialCommunications[index].TotalInformationReceived++;
                        //CalculateNpChart(index);

                        while (true)
                        {
                            try
                            {
                                var readRegister = ModbusClients[index].ReadHoldingRegisters(address, 3);
                                message =
                                    readRegister.Aggregate(message,
                                        (current, i) => current + i.ToString() + "\n");
                            }
                            catch (IOException)
                            {
                                Thread.Sleep(100);

                                continue;
                            }
                            break;
                        }

                        ProtectData(uidGuid, message, ModbusClients[index].IPAddress + "1" + modbusAddress.Item5);

                        PublishMessage(message, index, uidGuid);

                        UpdateGraph(uidGuid,
                            AvailableModbusSerialPorts[
                                    AvailableModbusSerialPorts.Select((val, ind) => new { ind, val })
                                        .First(e => e.val.UidGuid == uidGuid.ToString())
                                        .ind]
                                .ItemName);
                    }
                    catch (CRCCheckFailedException crcCheckFailedException)
                    {
                        /*if (OutOfControl(index))
                        {
                            var sf = StackTracing.GetFrame(0);
                            LogError(crcCheckFailedException, LogLevel.Critical, sf);
                            MessageBox.Show("CRC Failure. Please check settings and connection",
                                "CRC Error Check Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            CloseModbusUnexpectedly(uidGuid);
                            ResetCheckBox(AvailableModbusSerialPorts[index]);
                            return;
                        }*/
                    }

                if (modbusAddress.Item4)
                    try
                    {
                        SerialCommunications[index].TotalInformationReceived++;
                        //CalculateNpChart(index);

                        while (true)
                        {
                            try
                            {
                                var readInputRegister = ModbusClients[index].ReadInputRegisters(address, 4);
                                message =
                                    readInputRegister.Aggregate(message,
                                        (current, i) => current + i.ToString() + "\n");
                            }
                            catch (IOException)
                            {
                                Thread.Sleep(100); //attempt to resyncronize

                                continue;
                            }
                            break;
                        }

                        ProtectData(uidGuid, message, ModbusClients[index].IPAddress + "1" + modbusAddress.Item5);

                        PublishMessage(message, index, uidGuid);

                        UpdateGraph(uidGuid,
                            AvailableModbusSerialPorts[
                                    AvailableModbusSerialPorts.Select((val, ind) => new { ind, val })
                                        .First(e => e.val.UidGuid == uidGuid.ToString())
                                        .ind]
                                .ItemName);
                    }
                    catch (CRCCheckFailedException crcCheckFailedException)
                    {
                        /*if (OutOfControl(index))
                        {
                            var sf = StackTracing.GetFrame(0);
                            LogError(crcCheckFailedException, LogLevel.Critical, sf);
                            MessageBox.Show("CRC Failure. Please check settings and connection",
                                "CRC Error Check Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            CloseModbusUnexpectedly(uidGuid);
                            ResetCheckBox(AvailableModbusSerialPorts[index]);
                            return;
                        }*/
                    }
            }
        }

        public static void ModbusTimerOnTick(object sender, EventArgs eventArgs)
        {
            Guid uidGuid;
            ModbusTimerId.TryGetValue((DispatcherTimer)sender, out uidGuid);

            if (uidGuid == Guid.Empty) return;

            var readData = new MainWindow();
            readData.InitializeRead(uidGuid);
        }

        /// <summary>
        /// Allows for unchecking from outside the Main Form. 
        /// </summary>
        /// <param name="checkListItem">
        /// </param>
        private void ResetCheckBox(CheckListItem checkListItem)
        {
            var index = GetIndex<CheckListItem>(Guid.Parse(checkListItem.UidGuid));

            this.Dispatcher.Invoke((MethodInvoker)delegate
           {
               var checkList = new CheckListItem
               {
                   ItemName = checkListItem.ItemName,
                   UidGuid = checkListItem.UidGuid,
                   Content = checkListItem.Content,
                   IsChecked = false
               };

               if (AvailableSerialPorts.Any(
                   availableSerialPort => checkListItem.UidGuid == availableSerialPort.UidGuid))
               {
                   AvailableSerialPorts.Remove(AvailableSerialPorts[index]);
                   AvailableSerialPorts.Insert(index, checkList);
               }

               if (AvailableModbusSerialPorts.Any(
                   availableModbusSerialPort => checkListItem.UidGuid == availableModbusSerialPort.UidGuid))
               {
                   AvailableModbusSerialPorts.Remove(AvailableModbusSerialPorts[index]);
                   AvailableModbusSerialPorts.Insert(index, checkList);
               }
           });
        }

        /// <summary>
        /// Updates infomration on Statusbar on what system is exeperiencing. 
        /// </summary>
        /// <param name="sender">
        /// System Timer Thread Object 
        /// </param>
        /// <param name="eventArgs">
        /// Timer Arguments 
        /// </param>
        private void SystemTimerOnTick(object sender, EventArgs eventArgs)
        {
            //Prevents code from running before intialization
            if (!this.IsInitialized) return;

            ResizeSerialSelection();
            for (var i = 0; i < AvailableSerialPorts.Count; i++)
            {
                if (AvailableSerialPorts[i].IsChecked)
                    UpdateGraph(Guid.Parse(AvailableSerialPorts[i].UidGuid), AvailableSerialPorts[i].ItemName);
                if (AvailableModbusSerialPorts[i].IsChecked)
                    UpdateGraph(Guid.Parse(AvailableSerialPorts[i].UidGuid), AvailableModbusSerialPorts[i].ItemName);
            }

            for (var i = 0; i < MessagesPerSecond.Length; i++)
            {
                MessagesPerSecond[i] = 0.0;
            }
        }

        private void UpdateGraph(Guid uidGuid, string itemName)
        {
            var timeElapsed = DateTime.Now - _previousTime;

            if (timeElapsed < TimeSpan.FromSeconds(1))
                return; //Only update 1ce per second

            var index = GetIndex<MessageDataHistory>(uidGuid);
            if (index == -1) return;

            this.Dispatcher.Invoke((MethodInvoker)delegate
           {
               if (MessagesSentDataPair[index].Count > 60)
                   MessagesSentDataPair[index].RemoveAt(0);
               var timeNow = DateTime.Now.Minute + ":" + DateTime.Now.Second;
               MessagesPerSecond[index]++;

               if ((Lineseries[index] != null) && ((string)Lineseries[index].Title == itemName))
               {
                   var messageDataHistory = new MessageDataHistory
                   {
                       KeyPair = new KeyValuePair<string, double>(timeNow,
                           MessagesPerSecond[index] / timeElapsed.TotalSeconds),
                       UidGuid = uidGuid
                   };
                   MessagesSentDataPair[index].Add(messageDataHistory);
               }
               else
               {
                   Lineseries[index] = new LineSeries
                   {
                       ItemsSource = MessagesSentDataPair[index],
                       DependentValuePath = "Value",
                       IndependentValuePath = "Key",
                       Title = itemName
                   };
                   LineChart.Series.Add(Lineseries[index]);
                   var messageDataHistory = new MessageDataHistory
                   {
                       KeyPair = new KeyValuePair<string, double>(timeNow,
                           MessagesPerSecond[index] / timeElapsed.TotalSeconds),
                       UidGuid = uidGuid
                   };
                   MessagesSentDataPair[index].Add(messageDataHistory);
               }
           });
        }

        private void AddModbusTCP_Click(object sender, RoutedEventArgs e)
        {
            if (AddModbusTCP.Content != null && AddModbusTCP.Content.ToString() == "Add") AddModbusTCPConfig();
        }

        private void ModbusTCP_EnableChecked(object sender, RoutedEventArgs e)
        {
            AddModbusTCP.Content = "Edit";

            ModbusTCPRemove.IsEnabled = true;
        }

        private void AddModbusTCPConfig()
        {
            var uidGuid = Guid.NewGuid();
            if (!UserConfigureIp(uidGuid) ?? true) return;

            if (!UserConfigureTcpModbusAddress(uidGuid) ?? true)
            {
                UserCancelConfigureModbusAddresses(uidGuid);

                MessageBox.Show(@"Failed to create Modbus Configuration", @"ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (!ModbusClients[ModbusClients.Length - 1].Connected || (!UserConfigureFactory(uidGuid) ?? true))
            {
                UserCancelConfigureModbusAddresses(uidGuid);
                ShutdownFactory(uidGuid);
                MessageBox.Show(@"Failed to open RabbitMQ Connection", @"ERROR", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else
            {
                var checkListItem = new CheckListItem
                {
                    IsChecked = true,
                    Content = IpAddressTables[IpAddressTables.Length - 1].IpAddress + ":" +
                              IpAddressTables[IpAddressTables.Length - 1].Port,
                    UidGuid = uidGuid.ToString(),
                    ItemName = IpAddressTables[IpAddressTables.Length - 1].IpAddress + ":" +
                               IpAddressTables[IpAddressTables.Length - 1].Port
                };
                ModbusTcp.Add(checkListItem);
                InitializeModbusClient(IpAddressTables[IpAddressTables.Length - 1].IpAddress.ToString(),
                    IpAddressTables[IpAddressTables.Length - 1].Port, false);
                InitializeRead(uidGuid);
                ModbusControls[ModbusControls.Length - 1].ModbusTimers.IsEnabled = true;
                tbmarquee.Text += $"Serial Port {ModbusTcp[GetIndex<CheckListItem>(uidGuid)].ItemName} Active";
                tbmarquee.Text += $"Serial Port {ModbusTcp[GetIndex<CheckListItem>(uidGuid)].ItemName} Active";
            }
        }

        private void ModbusTCP_EnableUnchecked(object sender, RoutedEventArgs e)
        {
            if (lstModbusTCP.Items.Cast<object>()
                .Any(checkListItem => ((CheckListItem)checkListItem).IsChecked)) return;
            AddModbusTCP.Content = "Add";

            var anychecked = false;

            foreach (var item in lstModbusTCP.Items)
            {
                if (!((CheckListItem)item).IsChecked)
                {
                    continue;
                }
                anychecked = true;
            }

            ModbusTCPRemove.IsEnabled = anychecked;
        }


        private void ModbusTCPRemove_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (CheckListItem item in lstModbusTCP.Items)
            {
                if (item.IsChecked)
                {
                    ModbusClients[ModbusTcp.IndexOf(item)].Disconnect();
                    ModbusTcp.Remove(item);
                }
            }
        }

        private void BtnOpcUaRemove_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (CheckListItem item in LstOpcUa.Items)
            {
                if (item.IsChecked)
                {
                    //OpcUaServers[OpcUaList.IndexOf(item)].Disconnect();
                    OpcUaList.Remove(item);
                }
            }
        }

        private void BtnOpcUaAddEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            MainForm addOpc = null;
            if (button != null && button.Content.ToString().ToLower() == "add")
            {
                addOpc = new MainForm();
                //{
                //    IsEdit = false
                //};
            }
            else if (button != null)
            {
                //addOpc = new MainForm()
                //{
                //    IsEdit = true
                //};
                foreach (CheckListItem item in LstOpcUa.Items)
                {
                    if (!item.IsChecked) continue;
                    //addOpc.UidGuid = Guid.Parse(item.UidGuid);
                    break;
                }
            }
            else return;

            var success = addOpc.ShowDialog();

            if (success == System.Windows.Forms.DialogResult.OK) //true
            {
                if (button.Content.ToString().ToLower() == "add")
                {
                    var configureRabbit = UserConfigureFactory(new Guid());

                    if (configureRabbit.HasValue && !configureRabbit.Value) //False
                    {
                        var listBoxItem = new ListBoxItem()
                        {
                            Content = "Unable to configure RabbitMQ server"
                        };
                        lstErrors.Items.Add(listBoxItem);
                    }
                }

                var opcList = new CheckListItem
                {
                    //UidGuid = addOpc.UidGuid.ToString(),
                    //Content = OpcUaServers[OpcUaServers.Length - 1].ServerUri.ToString(),
                    IsChecked = false,
                    ItemName = ""//;
                };
                OpcUaList.Add(opcList);
            }
            else
            {
                var listBoxItem = new ListBoxItem { Content = $"Unable to {button.Content} OPC UA server" };
                lstErrors.Items.Add(listBoxItem);
            }
        }

        private void BtnClearErrors_Click(object sender, RoutedEventArgs e)
        {
            ErrorList.Clear();
            lstErrors.Items.Clear();
        }

        private void OpcUACheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var singleCheck = LstOpcUa.Items.Cast<CheckListItem>().Count(item => item.IsChecked);

            if (singleCheck > 1)
            {
                BtnOpcUaAddEdit.Content = "Add";
                BtnOpcUaRemove.IsEnabled = true;
            }
            else
            {
                BtnOpcUaAddEdit.Content = "Edit";
                BtnOpcUaRemove.IsEnabled = true;
            }
        }

        private void OpcUaCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var anychecked = LstOpcUa.Items.Cast<object>().Any(item => ((CheckListItem)item).IsChecked);
            BtnOpcUaRemove.IsEnabled = anychecked;

            if (!anychecked) //None Checked
            {
                BtnOpcUaAddEdit.Content = "Add";
            }
        }
    }
}