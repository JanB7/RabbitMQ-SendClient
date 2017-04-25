// ReSharper disable once CheckNamespace

namespace RabbitMQ_SendClient.UI
{
    using System;
    using System.ComponentModel;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Input;
    using static System.UInt16;
    using MessageBox = System.Windows.Forms.MessageBox;

    /// <summary>
    /// Interaction logic for ModbusIPConfig.xaml 
    /// </summary>
    public partial class ModbusIpConfig : Window
    {
        public ModbusIpConfig()
        {
            InitializeComponent();
        }

        #region Variables & Structures

        public IPAddress IpAddress { get; set; } = new IPAddress(IPAddress.Parse("127.0.0.1").Address);
        public ushort Port { get; set; }

        #endregion Variables & Structures

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ModbusIpConfig_OnClosing(object sender, CancelEventArgs e)
        {
            if (!this.DialogResult.HasValue)
                this.DialogResult = false;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.IpAddress = IPAddress.Parse(TxtIpAddress1.Text + "." + TxtIpAddress2.Text + "." + TxtIpAddress3.Text +
                                             "." + TxtIpAddress4.Text);
            if (!TryParse(TxtPortNumber.Text, out ushort result))
                result = MinValue;
            this.Port = result;
            this.DialogResult = true;
        }

        private void TxtPortNumber_TextChanged(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);

            try
            {
                if (TryParse(TxtPortNumber.Text, out ushort result)) return;
                var outOfBounds = PortOutOfBounds();
                if (outOfBounds == null)
                    TxtPortNumber.Text = "502";
                else if (outOfBounds == true)
                    TxtPortNumber.Text = "";
            }
            catch (Exception)
            {
                var outOfBounds = PortOutOfBounds();
                if (outOfBounds == null)
                    TxtPortNumber.Text = "502";
                else if (outOfBounds == true)
                    TxtPortNumber.Text = "";
            }
        }

        private bool? PortOutOfBounds()
        {
            var result = MessageBox.Show(@"Port out of bounds. Please enter a number between 1-65535", @"Out of Range",
                MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Hand);

            if (result == System.Windows.Forms.DialogResult.Retry)
                return true;
            if (result == System.Windows.Forms.DialogResult.Abort)
                return null;
            return false;
        }
    }
}