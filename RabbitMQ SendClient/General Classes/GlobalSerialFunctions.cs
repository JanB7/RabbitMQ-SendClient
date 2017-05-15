using static RabbitMQ_SendClient.General_Classes.FormControl;
using static RabbitMQ_SendClient.MainWindow;
using static RabbitMQ_SendClient.SystemVariables;

namespace RabbitMQ_SendClient
{
    using System;
    using System.Diagnostics;
    using System.IO.Ports;
    using System.Linq;
    using System.Windows.Controls;
    using System.Windows.Controls.DataVisualization.Charting;
    using System.Windows.Forms;
    using System.Windows.Threading;
    using RabbitMQ.Client;
    using UI;

    public static class GlobalSerialFunctions
    {
        #region Variables & Structures

        public static int[] StatsGroupings { get; set; } = new int[0];
        public static Guid UidGuid { get; set; }
        private static readonly StackTrace StackTracing = new StackTrace();

        #endregion Variables & Structures

        /// <summary>
        /// Calculates Upper Control limit for the messages 
        /// </summary>
        /// <param name="index">
        /// Index for Dynamic Server Allocation 
        /// </param>
        public static void CalculateNpChart(int index)
        {
            var pVal = SerialCommunications[index].InformationErrors /
                       SerialCommunications[index].TotalInformationReceived;
            var xBar = 0.0;
            var serialVal = SerialCommunications[index].TotalInformationReceived.ToString();

            var grouping = 0;
            if (serialVal.Length > SerialCommunications[index].MaximumErrors.ToString().Length)
                grouping = int.Parse(serialVal.Substring(serialVal.Length -
                                                         SerialCommunications[index].MaximumErrors.ToString().Length));
            else
                grouping = (int)SerialCommunications[index].TotalInformationReceived;

            if ((grouping % SerialCommunications[index].MaximumErrors) == 0)
            {
                if (StatsGroupings[index] > 48)
                    StatsGroupings[index] = 0;

                StatsGroupings[index]++;
                SerialCommunications[index].InformationErrors = 0;
            }

            SerialCommunications[index].SetX(StatsGroupings[index], pVal);

            for (var i = 0; i < 50; i++)
            {
                xBar += SerialCommunications[index].GetX(i);
            }

            xBar = xBar / 50;
            SerialCommunications[index].Ucl = xBar + 3 *
                                              Math.Sqrt(xBar * (1 - xBar /
                                                                SerialCommunications[index].MaximumErrors));

            SerialCommunications[index].Lcl = xBar - 3 *
                                              Math.Sqrt(xBar * (1 - xBar /
                                                                SerialCommunications[index].MaximumErrors));
        }

        /// <summary>
        /// Tests if message errors are out of control 
        /// </summary>
        /// <param name="index">
        /// </param>
        /// <returns>
        /// </returns>
        public static bool OutOfControl(int index)
        {
            try
            {
                SerialCommunications[index].InformationErrors++;
                var xbar = 0.0;

                for (var i = SerialCommunications[index].MaximumErrors; i >= 0; i--)
                {
                    xbar += SerialCommunications[index].GetX(i);
                }
                xbar = xbar / SerialCommunications[index]
                           .MaximumErrors; // np̅=(Ʃnp)/k where n = number of defective items and k = number of subgroups

                return xbar > SerialCommunications[index].Ucl;
            }
            catch (Exception ex)
            {
                var sf = StackTracing.GetFrame(0);
                LogError(ex, LogLevel.Critical, sf);
                var listBoxItem = new ListBoxItem()
                {
                    Content = $"{ex.Message}\n\n{ex.Source}"
                };
                ErrorList.Add(listBoxItem);
                //MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
        }

        /// <summary>
        /// Setup Serial Port for communication 
        /// </summary>
        /// <param name="uidGuid">
        /// Guid that is used for identifying correct array index 
        /// </param>
        private static void SetupSerial(Guid uidGuid)
        {
            var index = GetIndex<CheckListItem>(uidGuid);
            Array.Resize(ref SerialPorts, SerialPorts.Length + 1);

            SerialPorts[SerialPorts.Length - 1] = new SerialPort(AvailableSerialPorts[index].Content);
            SerialPorts[SerialPorts.Length - 1].DataReceived += DataReceivedHandler;

            Array.Resize(ref SerialCommunications, SerialCommunications.Length + 1);
            SerialCommunications[SerialCommunications.Length - 1] = new SerialCommunication
            {
                BaudRate = BaudRates.BaudRate9600,
                ComPort = AvailableSerialPorts[index].Content,
                FlowControl = Handshake.None,
                ReadTimeout = 1000,
                RtsEnable = false,
                SerialBits = 8,
                SerialParity = Parity.None,
                SerialStopBits = StopBits.One,
                UidGuid = uidGuid
            };
        }

        internal static bool? UserConfigureSerial(Guid uidGuid)
        {
            SetupSerial(uidGuid);
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
            var setupSerialForm = new SerialPortSetup(uidGuid);
            var success = setupSerialForm.ShowDialog(); //Confirm Settings

            SerialCommunications[SerialCommunications.Length - 1]
                .SetupX(SerialCommunications[SerialCommunications.Length - 1].MaximumErrors);

            return success;
        }

        internal static void UserCancelConfigureSerial(Guid uidGuid)
        {
            RemoveSerial(uidGuid);
            DecreaseLineSeries(uidGuid);
            Uncheck(uidGuid);
        }

        internal static void UserUnchekcSerial(Guid uidGuid)
        {
            RemoveSerial(uidGuid);
            DecreaseLineSeries(uidGuid);
        }

        internal static void RemoveSerial(Guid uidGuid)
        {
            var index = GetIndex<SerialCommunication>(uidGuid);

            if (SerialPorts[index].IsOpen) SerialPorts[index].Close();
            RemoveAtIndex<SerialPort>(index, SerialPorts);
        }

        /// <summary>
        /// Proivdes a threadsafe way to close the serial ports 
        /// </summary>
        /// <param name="index">
        /// Index for Dynamic Server Allocation 
        /// </param>
        public static void CloseSerialPortUnexpectedly(int index, Dispatcher uiDispatcher)
        {
            if (!SerialPorts[index].IsOpen) return;
            while (SerialPorts[index].IsOpen)
            {
                SerialPorts[index].Close();
            }

            uiDispatcher.Invoke((MethodInvoker)delegate
           {
               ;
               var i = 0;
               i += AvailableSerialPorts.TakeWhile(e => Guid.Parse(e.UidGuid) == SerialCommunications[index].UidGuid)
                   .Count();

               if (i > (AvailableSerialPorts.Count - 1))
               {
                   i = 0;
                   i += AvailableModbusSerialPorts
                       .TakeWhile(e => Guid.Parse(e.UidGuid) == SerialCommunications[index].UidGuid)
                       .Count();
                   if (i > (AvailableModbusSerialPorts.Count - 1)) throw new NullReferenceException();

                   var checklistItem = new CheckListItem
                   {
                       IsChecked = false,
                       Content = AvailableModbusSerialPorts[i].Content,
                       ItemName = AvailableModbusSerialPorts[i].ItemName,
                       UidGuid = AvailableModbusSerialPorts[i].UidGuid
                   };

                   AvailableModbusSerialPorts.RemoveAt(i);
                   AvailableModbusSerialPorts.Insert(i, checklistItem);
               }
               else
               {
                   var checkListItem = new CheckListItem
                   {
                       Content = AvailableSerialPorts[i].Content,
                       IsChecked = false,
                       ItemName = AvailableSerialPorts[i].ItemName,
                       UidGuid = AvailableSerialPorts[i].UidGuid
                   };

                   AvailableSerialPorts.RemoveAt(i);
                   AvailableSerialPorts.Insert(i, checkListItem);
               }

               MessageBox.Show(
                   @"Error with Serial Port. Closing connection. Please check settings and connection and try again.",
                   @"Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
           });
            SerialPorts = RemoveAtIndex<SerialPort>(index, SerialPorts);
            SerialCommunications = RemoveAtIndex<SerialCommunication>(index, SerialCommunications);
            StatsGroupings = RemoveAtIndex<int>(index, StatsGroupings);
            ServerInformation = RemoveAtIndex<RabbitServerInformation>(index, ServerInformation);
            Lineseries = RemoveAtIndex<LineSeries>(index, Lineseries);

            while (FactoryChannel[index].IsOpen)
            {
                FactoryChannel[index].Close();
            }
            while (FactoryConnection[index].IsOpen)
            {
                FactoryConnection[index].Close();
            }

            FactoryChannel = RemoveAtIndex<IModel>(index, FactoryChannel);
            FactoryConnection = RemoveAtIndex<IConnection>(index, FactoryConnection);
            Factory = RemoveAtIndex<IConnectionFactory>(index, Factory);
        }

        /// <summary>
        /// Initializes serial port with settings from global settings file. TODO write settings to file
        /// </summary>
        /// <param name="index">
        /// Index of Global Variable related to CheckboxList 
        /// </param>
        /// <param name="isInitialized">
        /// Form Initialization 
        /// </param>
        /// <returns>
        /// Success of the initiliation 
        /// </returns>
        public static bool SerialPortInitialize(int index, bool isInitialized)
        {
            //Prevents actions from occuring during initialization
            if (!isInitialized) return true;
            try
            {
                if (SerialPorts[index].IsOpen)
                    while (SerialPorts[index].IsOpen)
                    {
                        SerialPorts[index].Close();
                    }

                //Initializing the Serial Port
                SerialPorts[index].PortName = SerialCommunications[index].ComPort;
                SerialPorts[index].BaudRate = (int)SerialCommunications[index].BaudRate;
                SerialPorts[index].Parity = SerialCommunications[index].SerialParity;
                SerialPorts[index].StopBits = SerialCommunications[index].SerialStopBits;
                SerialPorts[index].DataBits = SerialCommunications[index].SerialBits;
                SerialPorts[index].Handshake = SerialCommunications[index].FlowControl;
                SerialPorts[index].RtsEnable = SerialCommunications[index].RtsEnable;
                SerialPorts[index].ReadTimeout = SerialCommunications[index].ReadTimeout;
                SerialCommunications[index].SetupX();

                SerialPorts[index].Open();

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                while (SerialPorts[index].IsOpen)
                {
                    SerialPorts[index].Close(); //Close port if opened
                }

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
    }
}