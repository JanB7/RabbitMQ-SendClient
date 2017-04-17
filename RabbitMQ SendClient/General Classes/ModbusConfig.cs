using static RabbitMQ_SendClient.General_Classes.FormControl;
using static RabbitMQ_SendClient.GlobalSerialFunctions;
using static RabbitMQ_SendClient.MainWindow;
using static RabbitMQ_SendClient.SystemVariables;

// ReSharper disable once CheckNamespace
namespace RabbitMQ_SendClient.General_Classes
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO.Ports;
    using System.Linq;
    using System.Windows.Controls.DataVisualization.Charting;
    using System.Windows.Threading;
    using EasyModbus;
    using UI;

    public static class ModbusConfig
    {
        #region Variables & Structures

        public static Dictionary<DispatcherTimer, Guid> ModbusTimerId { get; } =
            new Dictionary<DispatcherTimer, Guid>();

        public static ModbusControl[] ModbusControls = new ModbusControl[0];

        public struct ModbusControl
        {
            public List<Tuple<bool, bool, bool, bool, int>> ModbusAddressList { get; set; }

            internal static ObservableCollection<MessageDataHistory> MessagesSentDataPair { get; set; }

            internal DispatcherTimer ModbusTimers { get; set; }

            internal Guid UidGuid { get; set; }
        }

        #endregion Variables & Structures

        public static bool? UserConfigureSerialModbusAddresses(Guid uidGuid)
        {
            var index = GetIndex<SerialCommunication>(uidGuid);
            var port = new SerialPort
            {
                PortName = SerialCommunications[index].ComPort,
                BaudRate = (int) SerialCommunications[index].BaudRate,
                Parity = SerialCommunications[index].SerialParity,
                StopBits = SerialCommunications[index].SerialStopBits,
                DataBits = SerialCommunications[index].SerialBits,
                Handshake = SerialCommunications[index].FlowControl,
                RtsEnable = SerialCommunications[index].RtsEnable,
                ReadTimeout = SerialCommunications[index].ReadTimeout
            };

            Array.Resize(ref ModbusControls, ModbusControls.Length + 1);
            ModbusControls[ModbusControls.Length - 1].UidGuid = uidGuid;

            ModbusControls[ModbusControls.Length - 1].ModbusTimers = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000),
                IsEnabled = false,
                Tag = uidGuid
            };
            ModbusControls[ModbusControls.Length - 1].ModbusTimers.Tick += ModbusTimerOnTick;
            ModbusTimerId.Add(ModbusControls[ModbusControls.Length - 1].ModbusTimers, uidGuid);

            ModbusControls[ModbusControls.Length - 1].ModbusAddressList =
                new List<Tuple<bool, bool, bool, bool, int>>();

            index = GetIndex<CheckListItem>(uidGuid);

            var modbusSelection = new ModbusSelection
            {
                DeviceAddress = port.PortName,
                DeviceName = AvailableModbusSerialPorts[index].ItemName,
                IsAbsolute = true
            };

            return modbusSelection.ShowDialog();
        }

        public static bool? UserConfigureTcpModbusAddress(Guid uidGuid)
        {
            Array.Resize(ref ModbusControls, ModbusControls.Length + 1);
            ModbusControls[ModbusControls.Length - 1].UidGuid = uidGuid;

            ModbusControls[ModbusControls.Length - 1].ModbusTimers = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000),
                IsEnabled = false,
                Tag = uidGuid
            };

            ModbusControls[ModbusControls.Length - 1].ModbusTimers.Tick += ModbusTimerOnTick;
            ModbusTimerId.Add(ModbusControls[ModbusControls.Length - 1].ModbusTimers, uidGuid);

            ModbusControls[ModbusControls.Length - 1].ModbusAddressList =
                new List<Tuple<bool, bool, bool, bool, int>>();

            var index = GetIndex<CheckListItem>(uidGuid);

            var modbusSelection = new ModbusSelection
            {
                DeviceAddress = IpAddressTables[index].IpAddress.ToString(),
                DeviceName = IpAddressTables[index].IpAddress.ToString(),
                IsAbsolute = true
            };

            return modbusSelection.ShowDialog();
        }

        public static void UserCancelConfigureModbusAddresses(Guid uidGuid)
        {
            var index = GetIndex<SerialCommunication>(uidGuid);
            RemoveAtIndex<ModbusControl>(index, ModbusControls);
            ModbusTimerId.Remove(ModbusTimerId.FirstOrDefault(e => e.Value == uidGuid).Key);
            UserCancelConfigureModbusSerial(uidGuid);
        }

        private static void SetupModbusSerial(Guid uidGuid)
        {
            var index = GetIndex<CheckListItem>(uidGuid);
            Array.Resize(ref SerialCommunications, SerialCommunications.Length + 1);

            SerialCommunications[SerialCommunications.Length - 1] = new SerialCommunication
            {
                BaudRate = BaudRates.BaudRate38400,
                ComPort = AvailableSerialPorts[index].Content,
                FlowControl = Handshake.None,
                ReadTimeout = 1000,
                RtsEnable = false,
                SerialBits = 8,
                SerialParity = Parity.Even,
                SerialStopBits = StopBits.One,
                UidGuid = uidGuid,
                MaximumErrors = 10
            };
            SerialCommunications[SerialCommunications.Length - 1].X(SerialCommunications[SerialCommunications.Length -1].MaximumErrors);
        }

        public static void InitializeModbusClient(ModbusClient client)
        {
            Array.Resize(ref ModbusClients, ModbusClients.Length + 1);
            ModbusClients[ModbusClients.Length - 1] = new ModbusClient
            {
                IPAddress = client.IPAddress,
                Baudrate = client.Baudrate,
                Parity = client.Parity,
                Port = client.Port,
                StopBits = client.StopBits,
                UDPFlag = client.UDPFlag,
                UnitIdentifier = client.UnitIdentifier
            };
            ModbusClients[ModbusClients.Length - 1].Connect();
        }

        public static void InitializeModbusClient(SerialPort port)
        {
            Array.Resize(ref ModbusClients, ModbusClients.Length + 1);
            ModbusClients[ModbusClients.Length - 1] = new ModbusClient(port.PortName)
            {
                Baudrate = port.BaudRate,
                

                Parity = port.Parity,
                StopBits = port.StopBits,
                IPAddress = port.PortName //for reference
            };
            ModbusClients[ModbusClients.Length - 1].Connect();
        }

        public static void InitializeModbusClient(string ipAddress, int port, bool udp)
        {
            Array.Resize(ref ModbusClients, ModbusClients.Length + 1);
            ModbusClients[ModbusClients.Length - 1] = new ModbusClient(ipAddress, port)
            {
                UDPFlag = udp
            };
            ModbusClients[ModbusClients.Length - 1].Connect();
        }

        public static void CloseModbusClient(ModbusClient client)
        {
            var modbusClient = ModbusClients.FirstOrDefault(c => c.IPAddress == client.IPAddress);
            modbusClient?.Disconnect();
        }

        public static void CloseModbusClient(SerialPort port)
        {
            var modbusClient = ModbusClients.FirstOrDefault(c => c.IPAddress == port.PortName);
            modbusClient?.Disconnect();
        }

        private static void CloseModbusClient(string ipAddress)
        {
            var modbusClient = ModbusClients.FirstOrDefault(c => c.IPAddress == ipAddress);
            modbusClient?.Disconnect();
        }

        public static void CloseModbusUnexpectedly(Guid uidGuid)
        {
            var index = GetIndex<CheckListItem>(uidGuid);

            CloseModbusClient(SerialCommunications[index].ComPort);
            RemoveAtIndex<SerialCommunication>(index, SerialCommunications);

            RemoveAtIndex<ModbusControl>(index, ModbusControls);

            RemoveAtIndex<RabbitServerInformation>(index, ServerInformation);

            RemoveAtIndex<LineSeries>(index, Lineseries);
        }

        internal static bool? UserConfigureModbusSerial(Guid uidGuid)
        {
            SetupModbusSerial(uidGuid);
            var index = GetIndex<CheckListItem>(uidGuid);

            switch (CheckType(uidGuid))
            {
                case 1:
                    IncreaseLineSeries(AvailableSerialPorts[index].ItemName);
                    break;

                case 2:
                    IncreaseLineSeries(AvailableModbusSerialPorts[index].ItemName);
                    break;

                default:
                    return false;
            }
            var setupSerialForm = new SerialPortSetup(uidGuid)
            {
                cboMessageType = {SelectedIndex = 1},
                cboBaudRate = {SelectedIndex = 8},
                cboStopBits = {SelectedIndex = 1}
            };

            return setupSerialForm.ShowDialog(); //Confirm Settings
        }

        internal static void UserCancelConfigureModbusSerial(Guid uidGuid)
        {
            var index = GetIndex<SerialCommunication>(uidGuid);
            CloseModbusClient(SerialCommunications[index].ComPort);
            RemoveSerial(uidGuid);
            DecreaseLineSeries(uidGuid);
            Uncheck(uidGuid);
        }
    }
}