﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Text;
using RabbitMQ.Client;

namespace RabbitMQ_SendClient
{
    public static class SystemVariables
    {
        /// <summary>
        /// Array for Serial Port Configurations
        /// </summary>
        public static SerialCommunication[] SerialCommunications = new SerialCommunication[0];

        /// <summary>
        /// Array for available Serial Ports in the system
        /// </summary>
        public static SerialPort[] SerialPorts = new SerialPort[0];

        /// <summary>
        /// RabbitMQ Server Information
        /// </summary>
        public static RabbitServerInformation[] ServerInformation = new RabbitServerInformation[0];

        public static IModel[] FactoryChannel { get; set; } = new IModel[0];
        public static IConnection[] FactoryConnection { get; set; } = new IConnection[0];
        public static IConnectionFactory[] Factory { get; set; } = new IConnectionFactory[0];

        /// <summary>
        /// Standard Baud Rates used by majority of Serial Communcations
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
        /// Level of error Severity
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

        /// <summary>
        /// Dictionary of Errors that can be thrown
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
        /// Logs errors to file using System generated error and tracing code location
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

        /// <summary>
        /// Setup Serial Port for communication
        /// </summary>
        /// <param name="ports">COM Port Number</param>
        public static void SetupSerial(string[] ports)
        {
            foreach (var port in ports)
            {
                Array.Resize(ref SerialPorts, SerialPorts.Length + 1);
                SerialPorts[SerialPorts.Length - 1] = new SerialPort(port);

                Array.Resize(ref SerialCommunications, SerialCommunications.Length + 1);
                SerialCommunications[SerialCommunications.Length - 1] = new SerialCommunication
                {
                    BaudRate = BaudRates.BaudRate9600,
                    ComPort = port,
                    FlowControl = Handshake.None,
                    ReadTimeout = 1000,
                    RtsEnable = false,
                    SerialBits = 8,
                    SerialParity = Parity.None,
                    SerialStopBits = StopBits.One
                };

                Array.Resize(ref ServerInformation, ServerInformation.Length + 1);
                ServerInformation[ServerInformation.Length - 1] = new RabbitServerInformation
                {
                    AutoRecovery = true,
                    ServerAddress = IPAddress.Parse("130.113.130.194"),
                    ExchangeName = "Default",
                    ChannelName = "Default",
                    UserName = "User",
                    Password = "Factory1",
                    VirtualHost = "default",
                    ServerPort = 5672,
                    ServerHeartbeat = 30,
                    Encoding = "UTF8",
                    MessageType = "Serial",
                    MessageFormat = "jsonObject",
                    NetworkRecoveryInterval = 5
                };

                var factoryChannel = FactoryChannel;
                if (factoryChannel != null)
                {
                    Array.Resize(ref factoryChannel, factoryChannel.Length + 1);
                    factoryChannel = new IModel[factoryChannel.Length-1];
                    FactoryChannel = factoryChannel;
                }

                var factoryConnection = FactoryConnection;
                if (factoryConnection != null)
                {
                    Array.Resize(ref factoryConnection, factoryConnection.Length + 1);
                    factoryConnection = new IConnection[factoryConnection.Length-1];
                    FactoryConnection = factoryConnection;
                }

                var factory = Factory;
                if (factory != null)
                {
                    Array.Resize(ref factory, factory.Length + 1);
                    factory = new IConnectionFactory[factory.Length -1];
                    Factory = factory;
                }
            }
        }

        /// <summary>
        /// JSON Message Parameters.
        /// </summary>
        public struct JsonObject
        {
            public Messages Message;
            public string DeviceName { get; set; }
            public DateTime MessageDateTime { get; set; }
            public string MessageType { get; set; }
        }

        /// <summary>
        /// TO BE CHANGED. VARIABLES FOR MESSAGE THAT WILL BE SENT. TO BE READ FROM XML!!!
        /// </summary>
        public struct Messages
        {
            public string HeatIndexC { get; set; }
            public string HeatIndexF { get; set; }
            public string Humidity { get; set; }
            public string TemperatureC { get; set; }
            public string TemperatureF { get; set; }
        }

        /// <summary>
        /// RabbitMQ Server Information
        /// </summary>
        public struct RabbitServerInformation
        {
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

            public double UCL { get; set; }

            private double[] _x;

            public void X()
            {
                _x = new double[50];
                for (var i = 0; i < _x.Length; i++)
                {
                    _x[i] = MaximumErrors;
                }
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


    }
}