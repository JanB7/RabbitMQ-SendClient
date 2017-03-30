using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Windows;
using System.Xml.Linq;
using EasyModbus;
using RabbitMQ.Client;
using RabbitMQ_SendClient.Properties;

namespace RabbitMQ_SendClient
{
    public static class SystemVariables
    {
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

        private static Process CheckProcess
        {
            get { return _withEventsFieldProcess; }
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

        public static int GetIndex<T>(Guid uidGuid)
        {
            var index = 0;
            var type = new Dictionary<Type, Action>
            {
                {
                    typeof(SerialCommunication), () =>
                    {
                        foreach (var serialCommunication in SerialCommunications)
                        {
                            if (serialCommunication.UidGuid == uidGuid)
                                break;
                            index++;
                        }
                        if (index > SerialCommunications.Length) index = -1;
                    }
                },
                {
                    typeof(RabbitServerInformation), () =>
                    {
                        foreach (var servInfo in ServerInformation)
                        {
                            if (servInfo.UidGuid == uidGuid)
                                break;
                            index++;
                        }
                        if (index > ServerInformation.Length - 1) index = -1;
                    }
                },
                {
                    typeof(JsonObject), () =>
                    {
                        foreach (var jsonObject in JsonObjects)
                        {
                            if (jsonObject.UidGuid == uidGuid)
                                break;
                            index++;
                        }
                        if (index > JsonObjects.Length - 1) index = -1;
                    }
                },
                {
                    typeof(MainWindow.CheckListItem), () =>
                    {
                        foreach (var checkListItem in MainWindow.AvailableModbusSerialPorts)
                        {
                            if (checkListItem.Uid == uidGuid.ToString())
                                break;
                            index++;
                        }
                        if (index > MainWindow.AvailableModbusSerialPorts.Count - 1)
                            index = 0;
                        foreach (var checkListItem in MainWindow.AvailableSerialPorts)
                        {
                            if (checkListItem.Uid == uidGuid.ToString())
                                break;
                            index++;
                        }
                        if (index > MainWindow.AvailableModbusSerialPorts.Count - 1)
                            index = -1;
                    }
                }
            };

            type[typeof(T)]();

            return index;
        }

        public static dynamic RemoveAtIndex<T>(int index, Array arrayToResize)
        {

            var resizedArray = arrayToResize.Cast<T>().Select(item => item).ToList();
            resizedArray.RemoveAt(index);
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
        /// <param name="ex">Error generated by system/debugger</param>
        /// <param name="level">LogLevel type indicating severity of error</param>
        /// <param name="stackFrame">Stackframe for software line trace</param>
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

        private static bool CreateFileAndMonitor()
        {
            var docLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            docLocation += @"\RabbitMQ Client\Settings.xml";

            var defaultFile = Resources.defaultXML;
            if (
                !Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\RabbitMQ Client"))
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
                docContent[i] = (byte) docInformation[i];
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
                docContent[i] = (byte) docInformation[i];
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
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                stream?.Close();
            }

            //file is not locked
            return false;
        }

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

            public string MessageType { get; set; }

            public double Ucl { get; set; }

            private double[] _x;

            public void X()
            {
                _x = new double[50];
                for (var i = 0; i < _x.Length; i++)
                    _x[i] = MaximumErrors;
            }

            public void SetX(int index, double value)
            {
                _x[index] = value;
            }

            public double GetX(int index)
            {
                return _x[index];
            }
        }

        /*
         * var indata = JsonConvert.DeserializeObject<Messages[]>(datInfo.Message);
                    var message = new JsonObject
                    {
                        UidGuid = Guid.Parse(AvailableSerialPorts[index].Uid),
                        Message = indata,
                        DeviceName = datInfo.DeviceType,
                        MessageDateTime = datInfo.TimeStamp,
                        MessageType = "JsonMessage"
                    };
                    */
    }

    public class Messages
    {
        public string Name { get; set; }
        public dynamic Value { get; set; }
    }
}
 