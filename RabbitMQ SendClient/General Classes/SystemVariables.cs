﻿using static RabbitMQ_SendClient.General_Classes.GlocalTCPFunctions;
using static RabbitMQ_SendClient.GlobalSerialFunctions;
using static RabbitMQ_SendClient.MainWindow;

namespace RabbitMQ_SendClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.Ports;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Windows;
    using System.Windows.Controls.DataVisualization.Charting;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Xml.Linq;
    using EasyModbus;
    using EnvDTE;
    using General_Classes;
    using Properties;
    using RabbitMQ.Client;
    using Process = System.Diagnostics.Process;
    using StackFrame = System.Diagnostics.StackFrame;

    public static class SystemVariables
    {
        #region Variables & Structures

        private static Process CheckProcess
        {
            get => _withEventsFieldProcess;
            set
            {
                if (_withEventsFieldProcess != null)
                    _withEventsFieldProcess.Exited -= checkProcess_Exited;
                _withEventsFieldProcess = value;
                if (_withEventsFieldProcess != null)
                    _withEventsFieldProcess.Exited += checkProcess_Exited;
            }
        }

        public static IModel[] FactoryChannel { get; set; } = new IModel[0];
        public static IConnection[] FactoryConnection { get; set; } = new IConnection[0];
        public static IConnectionFactory[] Factory { get; set; } = new IConnectionFactory[0];
        public static JsonObject[] JsonObjects { get; set; } = new JsonObject[0];
        public static IPAddressTable[] IpAddressTables { get; set; } = new IPAddressTable[0];

        public static NodesToMonitor[] MonitorNodes { get; set; } = new NodesToMonitor[0];

        /// <summary>
        ///     Dictionary of Errors that can be thrown
        /// </summary>
        public static IDictionary<int, string> ErrorType { get; } = new Dictionary<int, string>
        {
            {0001, "Null Reference Exception"},
            {1001, "RabbitMQ Exception - Connection Already Closed"},
            {1003, "RabbitMQ Exception - Broker Unreachable"},
            {1002, "RabbitMQ Exception - Authentication Failure"},
            {1004, "RabbitMQ Exception - Channel Already Allocated and in Use"},
            {1005, "RabbitMQ Exception - Connection Failure"},
            {1006, "RabbitMQ Exception - Operation Interrupted"},
            {1007, "RabbitMQ Exception - Packet Not Recognized"},
            {1008, "RabbitMQ Exception - Possible Authentication Failure"},
            {1009, "RabbitMQ Exception - Connection Already Closed"},
            {1010, "RabbitMQ Exception - Protocol Version Mismatch"},
            {1011, "RabbitMQ Exception - Unsupported Method"},
            {1012, "RabbitMQ Exception - Unsupported Method Field"},
            {1013, "RabbitMQ Exception - Wire Formatting"}
        };

        /// <summary>
        ///     Standard Baud Rates used by majority of Serial Communcations
        /// </summary>
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

        /// <summary>
        ///     Level of error Severity
        /// </summary>
        public enum LogLevel
        {
            Debug = 1,
            Verbose = 2,
            Information = 3,
            Warning = 4,
            Error = 5,
            Critical = 6,
            None = 0
        }

        public static OpcUaServer[] OpcUaServers = new OpcUaServer[0];

        public static bool ReconnectOnStartup;

        private static Process _withEventsFieldProcess;
        private static bool _checkProcessIsRunning;

        /// <summary>
        ///     Array for Serial Port Configurations
        /// </summary>
        public static SerialCommunication[] SerialCommunications = new SerialCommunication[0];

        /// <summary>
        ///     Array for available Serial Ports in the system
        /// </summary>
        public static SerialPort[] SerialPorts = new SerialPort[0];

        /// <summary>
        ///     RabbitMQ Server Information
        /// </summary>
        public static RabbitServerInformation[] ServerInformation = new RabbitServerInformation[0];

        public static ModbusClient[] ModbusClients = new ModbusClient[0];

        public static string[] FriendlyName = new string[0];

        /// <summary>
        ///     JSON Message Parameters.
        /// </summary>
        public struct JsonObject
        {
            public Guid UidGuid { get; set; }
            public string DeviceName { get; set; }
            public DateTime MessageDateTime { get; set; }
            public string MessageType { get; set; }
            public Messages[] Message { get; set; }
        }

        /// <summary>
        ///     RabbitMQ Server Information
        /// </summary>
        public struct RabbitServerInformation
        {
            public Guid UidGuid { get; set; }
            public bool AutoRecovery { get; set; }
            public string ChannelName { get; set; }
            public string Encoding { get; set; }
            public string ExchangeName { get; set; }
            public string MessageFormat { get; set; }
            public string MessageType { get; set; }
            public int NetworkRecoveryInterval { get; set; }
            public string Password { get; set; }
            public IPAddress ServerAddress { get; set; }
            public int ServerHeartbeat { get; set; }
            public int ServerPort { get; set; }
            public string UserName { get; set; }
            public string VirtualHost { get; set; }
        }

        public struct SerialCommunication
        {
            public Guid UidGuid { get; set; }
            public BaudRates BaudRate { get; set; }
            public string ComPort { get; set; }
            public Handshake FlowControl { get; set; }
            public int ReadTimeout { get; set; }
            public bool RtsEnable { get; set; }
            public int SerialBits { get; set; }
            public Parity SerialParity { get; set; }
            public StopBits SerialStopBits { get; set; }
            public int TimeoutCounts { get; set; }
            public int MaximumErrors { get; set; }
            public double InformationErrors { get; set; }
            public double TotalInformationReceived { get; set; }

            private int currentVal { get; set; }

            public string MessageType { get; set; }

            public double Ucl { get; set; }
            public double Lcl { get; set; }

            private double[,] _x;

            public void SetupX()
            {
                ResizeArray(_x, 50, this.MaximumErrors);
                _x = new double[50, this.MaximumErrors];
                for (var i = 0; i < (_x.Length / this.MaximumErrors - 1); i++)
                {
                    _x[i, StatsGroupings[StatsGroupings.Length - 1]] = this.MaximumErrors;
                }
                this.currentVal = 0;
            }

            public void SetupX(int maximumErrors)
            {
                ResizeArray(_x, 50, maximumErrors);
                _x = new double[50, maximumErrors];
                for (var i = 0; i < (_x.Length / maximumErrors - 1); i++)
                {
                    _x[i, StatsGroupings[StatsGroupings.Length - 1]] = maximumErrors;
                }
                this.currentVal = 0;
            }

            public void SetX(int index, double value)
            {
                _x[StatsGroupings[index], this.currentVal] = value;
                this.currentVal++;
                if (this.currentVal == this.MaximumErrors)
                    this.currentVal = 0;
            }

            public double GetX(int index)
            {
                var val = 0.0;
                for (var i = 0; i < this.MaximumErrors; i++)
                {
                    val += _x[StatsGroupings[index], i];
                }
                val = val / this.MaximumErrors;
                return val;
            }
        }

        #endregion

        private static T[,] ResizeArray<T>(T[,] original, int rows, int cols)
        {
            if (original == null) original = new T[0, 0];
            var newArray = new T[rows, cols];
            var minRows = Math.Min(rows, original.GetLength(0));
            var minCols = Math.Min(cols, original.GetLength(1));
            for (var i = 0; i < minRows; i++)
            for (var j = 0; j < minCols; j++)
            {
                newArray[i, j] = original[i, j];
            }
            return newArray;
        }

        public static void IncreaseLineSeries(string itemName)
        {
            Array.Resize(ref MessagesSentDataPair, MessagesSentDataPair.Length + 1);
            MessagesSentDataPair[MessagesSentDataPair.Length - 1] = new ObservableCollection<MessageDataHistory>();

            var messagesPerSecond = MessagesPerSecond;
            Array.Resize(ref messagesPerSecond, messagesPerSecond.Length + 1);
            messagesPerSecond[messagesPerSecond.Length - 1] = new double();
            MessagesPerSecond = messagesPerSecond;

            var statsGroupings = StatsGroupings;
            Array.Resize(ref statsGroupings, StatsGroupings.Length + 1);
            statsGroupings[statsGroupings.Length - 1] = new int();
            StatsGroupings = statsGroupings;

            var lineSeries = Lineseries;
            Array.Resize(ref lineSeries, Lineseries.Length + 1);
            lineSeries[lineSeries.Length - 1] = new LineSeries
            {
                ItemsSource = MessagesSentDataPair[MessagesSentDataPair.Length - 1],
                DependentValuePath = "Value",
                IndependentValuePath = "Key",
                Title = itemName
            };
            Lineseries = lineSeries;
        }

        public static void DecreaseLineSeries(Guid uidGuid)
        {
            var index = GetIndex<MessageDataHistory>(uidGuid);
            foreach (var observableCollection in MessagesSentDataPair[index])
            {
                if (observableCollection.UidGuid != uidGuid) continue;
                MessagesSentDataPair[index].Remove(observableCollection);
                break;
            }
            //RemoveAtIndex<MessageDataHistory>(index, MessagesSentDataPair);
            RemoveAtIndex<double>(index, MessagesPerSecond);
            RemoveAtIndex<int>(index, StatsGroupings);
            RemoveAtIndex<LineSeries>(index, Lineseries);
        }

        public static string[] ReadAllResourceLines(string resourceText)
        {
            using (var reader = new StringReader(resourceText))
            {
                return EnumerateLines(reader).ToArray();
            }
        }

        private static IEnumerable<string> EnumerateLines(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        /// <summary>
        ///     <para>
        ///         Gets array index based on type of array used.
        ///     </para>
        ///     <para>
        ///         Can be of items: <see cref="SerialCommunication" /> |
        ///         <see cref="RabbitServerInformation" /> | <see cref="JsonObject" /> |
        ///         <see cref="CheckListItem" /> | <see cref="MessageDataHistory" /> |
        ///         <see cref="IPAddressTable" /> | <see cref="OpcUaServer" /><see cref="NodesToMonitor" />
        ///     </para>
        /// </summary>
        /// <typeparam name="T">
        ///     Struct type to check
        /// </typeparam>
        /// <param name="uidGuid">
        ///     Unique Identifier to match with the stuct
        /// </param>
        /// <returns>
        ///     Int value for index in the array
        /// </returns>
        public static int GetIndex<T>(Guid uidGuid)
        {
            var index = 0;
            var type = new Dictionary<Type, Action>
            {
                {
                    typeof(SerialCommunication), () =>
                    {
                        index +=
                            SerialCommunications
                                .TakeWhile(serialCommunication => serialCommunication.UidGuid != uidGuid)
                                .Count();
                        if (index > SerialCommunications.Length) index = -1;
                    }
                },
                {
                    typeof(RabbitServerInformation), () =>
                    {
                        index += ServerInformation.TakeWhile(servInfo => servInfo.UidGuid != uidGuid).Count();
                        if (index > ServerInformation.Length) index = -1;
                    }
                },
                {
                    typeof(JsonObject), () =>
                    {
                        index += JsonObjects.TakeWhile(jsonObject => jsonObject.UidGuid != uidGuid).Count();
                        if (index > (JsonObjects.Length - 1)) index = -1;
                    }
                },
                {
                    typeof(CheckListItem), () =>
                    {
                        index +=
                            AvailableModbusSerialPorts.TakeWhile(
                                    checkListItem => checkListItem.UidGuid != uidGuid.ToString())
                                .Count();
                        if (index > (AvailableModbusSerialPorts.Count - 1))
                            index = 0;
                        else
                            return;
                        index +=
                            AvailableSerialPorts.TakeWhile(checkListItem => checkListItem.UidGuid != uidGuid.ToString())
                                .Count();
                        if (index > (AvailableModbusSerialPorts.Count - 1))
                            index = 0;
                        else
                            return;
                        index += OpcUaList.TakeWhile(checkLitItem => checkLitItem.UidGuid != uidGuid.ToString())
                            .Count();
                        if (index > (OpcUaList.Count - 1))
                            index = -1;
                    }
                },
                {
                    typeof(MessageDataHistory), () =>
                    {
                        index +=
                            MessagesSentDataPair.Sum(
                                observableCollection =>
                                    observableCollection.TakeWhile(
                                            messageDataHistory => messageDataHistory.UidGuid != uidGuid)
                                        .Count());
                        if (index > (MessagesSentDataPair.Length - 1)) index = -1;
                    }
                },
                {
                    typeof(IPAddressTable), () =>
                    {
                        index += IpAddressTables.TakeWhile(e => e.UidGuid != uidGuid).Count();
                        if (index > (IpAddressTables.Length - 1)) index = -1;
                    }
                },
                {
                    typeof(OpcUaServer), () =>
                    {
                        index += OpcUaServers.TakeWhile(e => e.UidGuid != uidGuid).Count();
                        if (index > (OpcUaServers.Length - 1)) index = -1;
                    }
                },
                {
                    typeof(NodesToMonitor), () =>
                    {
                        index += MonitorNodes.TakeWhile(e => e.UidGuid != uidGuid).Count();
                        if (index > (MonitorNodes.Length - 1)) index = -1;
                    }
                }
            };

            type[typeof(T)]();

            return index;
        }

        internal static ImageSource GetImage(string psAssemblyName, string psResourceName)
        {
            var oUri = new Uri("pack://application:,,,/" + psAssemblyName + ";component/" + psResourceName,
                UriKind.RelativeOrAbsolute);
            return BitmapFrame.Create(oUri);
        }

        internal static ImageSource GetImage(string psResourceName)
        {
            var psAssemblyName = typeof(Program).Assembly.GetName().Name;
            var oUri = new Uri("pack://application:,,,/" + psAssemblyName + ";component/" + psResourceName,
                UriKind.RelativeOrAbsolute);
            try
            {
                return BitmapFrame.Create(oUri);
            }
            catch (Exception)
            {
                oUri = new Uri("pack://application:,,,/" + psAssemblyName + ";component/Resources/" + psResourceName,
                    UriKind.RelativeOrAbsolute);
                return BitmapFrame.Create(oUri);
            }
        }

        public static CheckListItem? OppositeCheck(Guid uidGuid)
        {
            var index = 0;
            index +=
                AvailableModbusSerialPorts.TakeWhile(
                        checkListItem => checkListItem.UidGuid != uidGuid.ToString())
                    .Count();
            if (index > (AvailableModbusSerialPorts.Count - 1))
                index = 0;
            else
                return AvailableSerialPorts[index];

            index += AvailableSerialPorts.TakeWhile(checkListItem => checkListItem.UidGuid != uidGuid.ToString())
                .Count();

            if (index > (AvailableSerialPorts.Count - 1))
                return null;

            return AvailableModbusSerialPorts[index];
        }

        public static int? CheckType(Guid uidGuid)
        {
            var index = 0;
            index +=
                AvailableModbusSerialPorts.TakeWhile(
                        checkListItem => checkListItem.UidGuid != uidGuid.ToString())
                    .Count();
            if (index > (AvailableModbusSerialPorts.Count - 1))
                index = 0;
            else
                return 2;

            index += AvailableSerialPorts.TakeWhile(checkListItem => checkListItem.UidGuid != uidGuid.ToString())
                .Count();

            if (index > (AvailableSerialPorts.Count - 1))
                return null;

            return 1;
        }

        public static dynamic RemoveAtIndex<T>(int index, Array arrayToResize)
        {
            var resizedArray = arrayToResize.Cast<T>().Select(item => item).ToList();
            try
            {
                resizedArray.RemoveAt(index);
            }
            catch (Exception e) //out of bounds
            {
                return -1;
            }

            arrayToResize = resizedArray.ToArray();
            return arrayToResize;
        }

        private static void checkProcess_Exited(object sender, EventArgs e)
        {
            _checkProcessIsRunning = false;
        }

        /// <summary>
        ///     Logs errors to file using System generated error and tracing code location
        /// </summary>
        /// <param name="ex">
        ///     Error generated by system/debugger
        /// </param>
        /// <param name="level">
        ///     LogLevel type indicating severity of error
        /// </param>
        /// <param name="stackFrame">
        ///     Stackframe for software line trace
        /// </param>
        public static void LogError(Exception ex, LogLevel level, StackFrame stackFrame)
        {
            using (var w = File.AppendText("log.txt"))
            {
                w.Write("{0}:\t", level);
                w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                w.WriteLine(stackFrame.GetMethod().Name);
                w.WriteLine(stackFrame.GetMethod().Attributes);
                w.WriteLine("{0}", ex.StackTrace);
                w.WriteLine("{0}", ex.Message);
            }
        }

        // ReSharper disable once InconsistentNaming
        public static void GetXML(string friendly, Guid uidGuid)
        {
            var docLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            docLocation += @"\RabbitMQ Client\Settings.xml";

            var doc = XDocument.Load(docLocation);

            if (doc.Root != null)
                foreach (var element in doc.Root.Elements("FriendlyName"))
                {
                    if ((string) element.Attribute("FriendlyName") == friendly) continue;
                    var o = doc.Root.Element("variables");
                    var index = 0;
                    JsonObjects[JsonObjects.Length - 1].DeviceName = friendly;
                    JsonObjects[JsonObjects.Length - 1].Message = new Messages[o.Elements("Variable").Count()];
                    JsonObjects[JsonObjects.Length - 1].UidGuid = uidGuid;
                    JsonObjects[JsonObjects.Length - 1].MessageType =
                        SerialCommunications[GetIndex<SerialCommunication>(uidGuid)].MessageType;
                    foreach (var xElement in o.Elements("Variable"))
                    {
                        var type = (string) xElement.Element("type");
                        var jsonObject = JsonObjects[JsonObjects.Length - 1].Message[index];

                        jsonObject = new Messages();

                        var xAttribute = (string) xElement.Element("name");
                        jsonObject.Name = xAttribute;

                        jsonObject.Value = GenerateNull(type);

                        JsonObjects[JsonObjects.Length - 1].Message[index] = jsonObject;
                        index++;
                    }
                }
        }

        public static bool GenerateFriendlies()
        {
            var docLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            docLocation += @"\RabbitMQ Client\Settings.xml";

            if (File.Exists(docLocation))
                try
                {
                    var doc = XDocument.Load(docLocation);
                    if (doc.Root == null) return CreateFileAndMonitor();
                    foreach (var xElement in doc.Root.Elements("FriendlyName"))
                    {
                        Array.Resize(ref FriendlyName, FriendlyName.Length + 1);
                        FriendlyName[FriendlyName.Length - 1] = xElement.Value;
                    }
                    return true;
                }
                catch (Exception)
                {
                    return CreateFileAndMonitor();
                }
            return CreateFileAndMonitor();
        }

        public static bool IsHex(IEnumerable<char> chars)
        {
            return chars.Select(c => ((c >= '0') && (c <= '9')) || ((c >= 'a') && (c <= 'f')) ||
                                     ((c >= 'A') && (c <= 'F')))
                .All(isHex => isHex);
        }

        private static bool CreateFileAndMonitor()
        {
            var docLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            docLocation += @"\RabbitMQ Client\Settings.xml";

            var defaultFile = Resources.defaultXML;
            if (
                !Directory.Exists(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\RabbitMQ Client"))
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                          @"\RabbitMQ Client");

            File.Create(docLocation).Close();

            while (IsFileLocked(new FileInfo(docLocation)))
            {
                //Wait
            }
            var docWriter = new StreamWriter(docLocation);
            docWriter.Write(defaultFile);
            docWriter.Close();

            var result = MessageBox.Show(
                "Please configure the settings file with the correct settings for your device", "File Error!",
                MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);

            if (result != MessageBoxResult.OK) return false;

            var hashMe = HashAlgorithm.Create();
            var docInformation = File.ReadAllText(docLocation);
            var docContent = new byte[docInformation.Length];
            for (var i = 0; i < docInformation.Length; i++)
            {
                docContent[i] = (byte) docInformation[i];
            }
            var prehash = hashMe.ComputeHash(docContent);
            CheckProcess = Process.Start(docLocation);
            if (CheckProcess != null)
            {
                CheckProcess.EnableRaisingEvents = true;
                _checkProcessIsRunning = true;
                while (_checkProcessIsRunning)
                {
                    //wait
                }
            }
            else
            {
                return false;
            }

            var updated = GenerateFriendlies();
            if (!updated) return false;

            docInformation = File.ReadAllText(docLocation);
            Array.Resize(ref docContent, 0);
            Array.Resize(ref docContent, docInformation.Length);
            for (var i = 0; i < docInformation.Length; i++)
            {
                docContent[i] = (byte) docInformation[i];
            }
            var posthash = hashMe.ComputeHash(docContent);
            return prehash != posthash;
        }

        private static dynamic GenerateNull(string type)
        {
            if (type == null)
                return string.Empty;
            const string nullString = "0.0";
            switch (type.ToLower())
            {
                case "bool":
                    return false;

                case "byte":
                    return byte.Parse(nullString);

                case "sbyte":
                    return sbyte.Parse(nullString);

                case "char":
                    return '\0';

                case "decimal":
                    return decimal.Parse(nullString);

                case "double":
                    return double.Parse(nullString);

                case "float":
                    return float.Parse(nullString);

                case "integer":
                    return int.Parse(nullString);

                case "uint":
                    return uint.Parse(nullString);

                case "long":
                    return long.Parse(nullString);

                case "ulong":
                    return ulong.Parse(nullString);

                case "short":
                    return short.Parse(nullString);

                case "ushort":
                    return ushort.Parse(nullString);

                default: //Inlude Case String
                    return string.Empty;
            }
        }

        private static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                if (file.Exists) return true;
                file.Create().Close();
                return IsFileLocked(file);
            }
            finally
            {
                stream?.Close();
            }

            //file is not locked
            return false;
        }

        /*
         * var indata = JsonConvert.DeserializeObject<Messages[]>(datInfo.Message);
                    var message = new JsonObject
                    {
                        UidGuid = Guid.Parse(AvailableSerialPorts[index].UidGuid),
                        Message = indata,
                        DeviceName = datInfo.DeviceType,
                        MessageDateTime = datInfo.TimeStamp,
                        MessageType = "JsonMessage"
                    };
                    */
    }

    public class Messages
    {
        #region Variables & Structures

        public string Name { get; set; }
        public dynamic Value { get; set; }

        #endregion
    }

    public class NodesToMonitor
    {
        #region Variables & Structures

        public List<string> NodePath { get; set; }
        public Guid UidGuid { get; set; }

        #endregion
    }

    public class InfiniteList<T> : List<InfiniteList<T>>
    {
        #region Variables & Structures

        public T Value { set; get; }

        #endregion
    }
}