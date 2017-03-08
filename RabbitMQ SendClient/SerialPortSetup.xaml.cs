using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using PostSharp.Patterns.Diagnostics;
using MessageBox = System.Windows.Forms.MessageBox;
using static RabbitMQ_SendClient.SystemVariables;

namespace RabbitMQ_SendClient
{
    /// <summary>
    /// Interaction logic for SerialPortSetup.xaml
    /// </summary>
    public partial class SerialPortSetup : Window
    {
        private static readonly StackTrace StackTrace = new StackTrace();

        public static int SerialPortNum
        {
            get;
            set;
        } //number of port that is being configured

        public SerialPortSetup()
        {
            InitializeComponent();

            InitializeBaudRates();
            InitializeDataBits();
            InitializeStopBits();
            InitializeParity();
            InitializeHandshake();
        }

        [LogException]
        private void InitializeBaudRates()
        {
            try
            {
                foreach (var rate in Enum.GetValues(typeof(BaudRates)))
                {
                    cboBaudRate.Items.Add(rate);
                }

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
                for (var i = 4; i != 9; i++)
                {
                    cboDataBits.Items.Add(i);
                }
                cboDataBits.SelectedIndex = cboDataBits.Items.Count - 1;
                cboDataBits.IsEnabled = true;
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
                {
                    cboStopBits.Items.Add(stopBit);
                }
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
                {
                    cboParity.Items.Add(parity);
                }
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
                {
                    cboFlowControl.Items.Add(shake);
                }
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

        private void cboBaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var rates = (BaudRates)Enum.Parse(typeof(BaudRates), cboBaudRate.Items[cboBaudRate.SelectedIndex].ToString());

            SerialPorts[SerialPortNum].BaudRate = (int)rates;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }



        private void cboDataBits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var cb = (System.Windows.Controls.ComboBox)sender;
            var bits = cb.Text;
            SerialPorts[SerialPortNum].DataBits = int.Parse(bits);
        }

        private void cboFlowControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var flowControl = (Handshake)Enum.Parse(typeof(Handshake), cboFlowControl.Items[cboFlowControl.SelectedIndex].ToString());

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

        private void sldReadTimeout_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Prevents actions from occuring during initialization
            if (!IsInitialized) return;

            var timeout = (int)sldReadTimeout.Value;
            txtReadTimeout.Text = timeout.ToString();

            SerialCommunications[SerialPortNum].ReadTimeout = timeout;
        }
    }
}