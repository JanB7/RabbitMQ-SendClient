using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;

// ReSharper disable once CheckNamespace
namespace RabbitMQ_SendClient.UI
{
    /// <summary>
    /// Interaction logic for ModbusIPConfig.xaml
    /// </summary>
    public partial class ModbusIpConfig : Window
    {
        public bool ReadRegisters = true;
        public bool ReadCoils = false;
        public ModbusIpConfig()
        {
            InitializeComponent();
            ChkReadRegisters.IsChecked = ReadRegisters;
            ChkReadCoils.IsChecked = ReadCoils;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ChkReadRegisters_OnChecked(object sender, RoutedEventArgs e)
        {
            ReadRegisters = true;
        }

        private void ChkReadCoils_Checked(object sender, RoutedEventArgs e)
        {
            ReadCoils = true;
        }

        private void ChkReadRegisters_OnUnchecked(object sender, RoutedEventArgs e)
        {
            ReadRegisters = false;
            
            if (!ReadCoils && !ReadRegisters)
                Dispatcher.Invoke((MethodInvoker) delegate
                {
                    OK.IsEnabled = false;
                });
            else if (!OK.IsEnabled)
                Dispatcher.Invoke((MethodInvoker) delegate
                {
                    OK.IsEnabled = true;
                });
        }

        private void ChkReadCoils_OnUnchecked(object sender, RoutedEventArgs e)
        {
            ReadCoils = false;

            if (!ReadCoils && !ReadRegisters)
                Dispatcher.Invoke((MethodInvoker)delegate
                {
                    OK.IsEnabled = false;
                });
            else if (!OK.IsEnabled)
                Dispatcher.Invoke((MethodInvoker)delegate
                {
                    OK.IsEnabled = true;
                });
        }

        private void ModbusIpConfig_OnClosing(object sender, CancelEventArgs e)
        {
            if (!DialogResult.HasValue)
                DialogResult = false;
        }
    }
}
