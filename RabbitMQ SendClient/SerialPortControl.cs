using System;
using System.Diagnostics;
using System.IO.Ports;
using static RabbitMQ_SendClient.SystemVariables;

namespace RabbitMQ_SendClient
{
    internal class SerialPortControl
    {
        private static readonly StackTrace StackTrace = new StackTrace();

        private SerialPort _serialPort { get; set; }
        public SerialPortControl(SerialPort serialPort) //Constructor with arguments
        {
            this._serialPort = serialPort;
        }

        protected void OpenSerialPort()
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
            }
        }

        public void SetBaudRate(SystemVariables.BaudRates rate)
        {
            _serialPort.BaudRate = (int)rate;
        }
        protected void CloseSerialPort(SerialPort port)
        {
            try
            {
                if (port.IsOpen)
                    while (port.IsOpen)
                        port.Close();
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
            }
        }
    }
}
