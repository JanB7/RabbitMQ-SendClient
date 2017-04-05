using static RabbitMQ_SendClient.SystemVariables;

// ReSharper disable once CheckNamespace
namespace RabbitMQ_SendClient.General_Classes
{
    using EasyModbus;
    using System;
    using System.IO.Ports;
    using System.Linq;

    internal static class ModbusConfig
    {
        public static void CloseModbusSerial(Guid uidGuid)
        {
            var index = GetIndex<SerialCommunication>(uidGuid);

            CloseModbusClient(SerialCommunications[index].ComPort);

            RemoveAtIndex<SerialCommunication>(index, SerialCommunications);
        }

        public static void SetupModbusSerial(Guid uidGuid)
        {
            var index = GetIndex<MainWindow.CheckListItem>(uidGuid);
            Array.Resize(ref SerialCommunications, SerialCommunications.Length + 1);

            SerialCommunications[SerialCommunications.Length - 1] = new SerialCommunication
            {
                BaudRate = BaudRates.BaudRate9600,
                ComPort = MainWindow.AvailableSerialPorts[index].Content,
                FlowControl = Handshake.None,
                ReadTimeout = 1000,
                RtsEnable = false,
                SerialBits = 8,
                SerialParity = Parity.Even,
                SerialStopBits = StopBits.One,
                UidGuid = uidGuid
            };
        }

        public static void InitializeModbusClient(ModbusClient client)
        {
            Array.Resize(ref ModbusClients, ModbusClients.Length + 1);
            ModbusClients[ModbusClients.Length - 1] = new ModbusClient
            {
                IPAddress = client.IPAddress,
                Baudrate = client.Baudrate,
                LogFileFilename = "log.txt",
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
                LogFileFilename = "log.txt",
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
                LogFileFilename = "log.txt",
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

        public static void CloseModbusClient(string ipAddress)
        {
            var modbusClient = ModbusClients.FirstOrDefault(c => c.IPAddress == ipAddress);
            modbusClient?.Disconnect();
        }
    }
}