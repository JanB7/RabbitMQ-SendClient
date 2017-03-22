using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows.Forms;
using static RabbitMQ_SendClient.SystemVariables;
using Application = System.Windows.Application;

namespace RabbitMQ_SendClient
{
    public static class GlobalSerialFunctions
    {
        private static readonly StackTrace StackTracing = new StackTrace();
        private static int[] StatsGroupings { get; set; } = new int[0];
        public static Guid UidGuid { get; set; }

        /// <summary>
        ///     Calculates Upper Control limit for the messages
        /// </summary>
        /// <param name="index">Index for Dynamic Server Allocation</param>
        public static void CalculateNpChart(int index)
        {
            var pVal = SerialCommunications[index].InformationErrors /
                       SerialCommunications[index].TotalInformationReceived;
            var xBar = 0.0;
            if (SerialCommunications[index].TotalInformationReceived > SerialCommunications[index].MaximumErrors)
            {
                if (StatsGroupings[index] > 48)
                    StatsGroupings[index] = 0;

                StatsGroupings[index]++;
            }
            SerialCommunications[index].SetX(StatsGroupings[index], pVal);
            for (var i = 0; i < 50; i++)
                xBar += SerialCommunications[index].GetX(i);

            xBar = xBar / 50;

            SerialCommunications[index].Ucl = xBar + 3 *
                                              Math.Sqrt(Math.Abs(xBar * (1 - xBar)) /
                                                        SerialCommunications[index].MaximumErrors);
        }

        /// <summary>
        ///     Tests if message errors are out of control
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static bool OutOfControl(int index)
        {
            try
            {
                var xbar = 0.0;
                for (var i = SerialCommunications[index].MaximumErrors; i >= 0; i--)
                    xbar += SerialCommunications[index].GetX(i);
                xbar = xbar / (SerialCommunications[index].MaximumErrors * 0.9);

                return xbar > SerialCommunications[index].Ucl;
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
        }

        /// <summary>
        ///     Setup Serial Port for communication
        /// </summary>
        /// <param name="port">COM Port Number</param>
        public static void SetupSerial(Guid uidGuid)
        {
            var index = GetIndex<MainWindow.CheckListItem>(uidGuid);
            Array.Resize(ref SerialPorts, SerialPorts.Length + 1);

            SerialPorts[SerialPorts.Length - 1] = new SerialPort(MainWindow.AvailableSerialPorts[index].Content);

            Array.Resize(ref SerialCommunications, SerialCommunications.Length + 1);
            SerialCommunications[SerialCommunications.Length - 1] = new SerialCommunication
            {
                BaudRate = BaudRates.BaudRate9600,
                ComPort = MainWindow.AvailableSerialPorts[index].Content,
                FlowControl = Handshake.None,
                ReadTimeout = 1000,
                RtsEnable = false,
                SerialBits = 8,
                SerialParity = Parity.None,
                SerialStopBits = StopBits.One,
                UidGuid = uidGuid
            };

            var statsGroupings = StatsGroupings;
            Array.Resize(ref statsGroupings, StatsGroupings.Length + 1);
            StatsGroupings = statsGroupings;
        }

        /// <summary>
        ///     Proivdes a threadsafe way to close the serial ports
        /// </summary>
        /// <param name="index">Index for Dynamic Server Allocation</param>
        private static void CloseSerialPortUnexpectedly(int index)
        {
            if (!SerialPorts[index].IsOpen) return;
            while (SerialPorts[index].IsOpen)
                SerialPorts[index].Close();

            Application.Current.Dispatcher.Invoke((MethodInvoker) delegate
            {
                var checkList = MainWindow.AvailableSerialPorts[index];
                checkList.IsChecked = false;
                MainWindow.AvailableSerialPorts.RemoveAt(index);
                MainWindow.AvailableSerialPorts.Insert(index, checkList);
            });
        }
    }
}