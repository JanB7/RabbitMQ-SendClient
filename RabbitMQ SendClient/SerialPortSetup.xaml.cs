using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;
using System.Windows.Forms;
using PostSharp.Patterns.Diagnostics;
using static RabbitMQ_SendClient.SystemVariables;
using MessageBox = System.Windows.Forms.MessageBox;

namespace RabbitMQ_SendClient
{
    /// <summary>
    ///     Interaction logic for SerialPortSetup.xaml
    /// </summary>
    public partial class SerialPortSetup : Window
    {
        private static readonly StackTrace StackTrace = new StackTrace();
        public int SerialPortNum { get; set; } //number of port that is being configured


        /// <summary>
        /// Main Class for the Serial Port form. Used to initialize the system.
        /// </summary>
        public SerialPortSetup()
        {
            InitializeComponent();

            InitializeBaudRates();
            InitializeDataBits();
            InitializeStopBits();
            InitializeParity();
            InitializeHandshake();
            InitializeMessageType();

        }

        

        [LogException]
        private void InitializeBaudRates()
        {
            try
            {
                foreach (var rate in Enum.GetValues(typeof(BaudRates)))
                    cboBaudRate.Items.Add(rate.ToString());

                cboBaudRate.SelectedIndex = 4;
                cboBaudRate.IsEnabled = true;
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
                var message = ex.Message + "\nError in BaudRate Enumeration";
                MessageBox.Show(message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [LogException]
        private void InitializeDataBits()
        {
            try
            {
                for (var i = 8; i >= 4; i--)
                    cboDataBits.Items.Add(i);
                cboDataBits.IsEnabled = true;
                cboDataBits.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
                var message = ex.Message + "\nError in DataBits Enumeration";
                MessageBox.Show(message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [LogException]
        private void InitializeStopBits()
        {
            try
            {
                foreach (var stopBit in Enum.GetValues(typeof(StopBits)))
                    cboStopBits.Items.Add(stopBit);
                cboStopBits.SelectedIndex = 0;
                cboStopBits.IsEnabled = true;
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
                var message = ex.Message + "\nError in StopBits Enumeration";
                MessageBox.Show(message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [LogException]
        private void InitializeParity()
        {
            try
            {
                foreach (var parity in Enum.GetValues(typeof(Parity)))
                    cboParity.Items.Add(parity);
                cboParity.SelectedIndex = 0;
                cboParity.IsEnabled = true;
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
                var message = ex.Message + "\nError in Parity Enumeration";
                MessageBox.Show(message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [LogException]
        private void InitializeHandshake()
        {
            try
            {
                foreach (var shake in Enum.GetValues(typeof(Handshake)))
                    cboFlowControl.Items.Add(shake);
                cboFlowControl.SelectedIndex = 0;
                cboFlowControl.IsEnabled = true;

                sldReadTimeout.IsEnabled = true;
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
                var message = ex.Message + "\nError in Handshake Enumeration";
                MessageBox.Show(message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeMessageType()
        {
            try
            {
                cboMessageType.Items.Add("JSON");
                cboMessageType.Items.Add("Plain-Text");
                //cboMessageType.Items.Add("MODBUS");
            }
            catch (Exception ex)
            {
                var sf = StackTrace.GetFrame(0);
                LogError(ex, SystemVariables.LogLevel.Critical, sf);
            }
        }

        /// <summary>
        ///     Updates text for slider
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SldReadTimeout_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            txtReadTimeout.Text = ((int) sldReadTimeout.Value).ToString();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            SetFlowControl();
            SetDataBits();
            SetReadTimeout();
            SetBaudRate();
            SetParity();
            MaximumErrors();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        ///     Sets the Data bits for the port being configured.
        /// </summary>
        private void SetDataBits()
        {
            SerialCommunications[SerialPortNum].SerialBits =
                int.Parse(cboDataBits.Items[cboDataBits.SelectedIndex].ToString());
        }

        /// <summary>
        ///     Sets FlowControl and Handshake of the port being configured.
        /// </summary>
        private void SetFlowControl()
        {
            var flowControl =
                (Handshake) Enum.Parse(typeof(Handshake), cboFlowControl.Items[cboFlowControl.SelectedIndex].ToString());

            SerialCommunications[SerialPortNum].FlowControl = flowControl;
            switch (flowControl)
            {
                case Handshake.None:
                    SerialCommunications[SerialPortNum].RtsEnable = false;
                    break;

                case Handshake.RequestToSend:
                    SerialCommunications[SerialPortNum].RtsEnable = true;
                    break;

                case Handshake.RequestToSendXOnXOff:
                    SerialCommunications[SerialPortNum].RtsEnable = true;
                    break;

                case Handshake.XOnXOff:
                    SerialCommunications[SerialPortNum].RtsEnable = true;
                    break;

                default:
                    goto
                        case Handshake.None;
            }
        }

        private void SetBaudRate()
        {
            SerialCommunications[SerialPortNum].BaudRate =
                (BaudRates) Enum.Parse(typeof(BaudRates), cboBaudRate.Items[cboBaudRate.SelectedIndex].ToString());
        }

        private void SetMessageType()
        {
            SerialCommunications[SerialPortNum].MessageType = cboMessageType.Items[cboMessageType.SelectedIndex].ToString();
        }

        private void SetReadTimeout()
        {
            SerialCommunications[SerialPortNum].ReadTimeout = (int) sldReadTimeout.Value;
            ;
        }

        private void SetParity()
        {
            var parity = cboParity.Items[cboParity.SelectedIndex].ToString();

            switch (parity)
            {
                case "Odd":
                    SerialCommunications[SerialPortNum].SerialParity = Parity.Odd;
                    break;

                case "Even":
                    SerialCommunications[SerialPortNum].SerialParity = Parity.Even;
                    break;

                case "Space":
                    SerialCommunications[SerialPortNum].SerialParity = Parity.Space;
                    break;

                case "Mark":
                    SerialCommunications[SerialPortNum].SerialParity = Parity.Mark;
                    break;

                case "None":
                case null:
                default:
                    SerialCommunications[SerialPortNum].SerialParity = Parity.None;
                    break;
            }
        }

        private void MaximumErrors()
        {
            SerialCommunications[SerialPortNum].MaximumErrors = (int) sldMaxmumErrors.Value;
        }

        private void SldMaximumErrors_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized) return;
            txtMaximumErrors.Text = ((int) sldMaxmumErrors.Value).ToString();
        }
    }
}