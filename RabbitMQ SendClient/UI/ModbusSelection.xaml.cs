using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using static RabbitMQ_SendClient.MainWindow;

// ReSharper disable once CheckNamespace

namespace RabbitMQ_SendClient.UI
{
    /// <summary>
    ///     Interaction logic for ModbusSelection.xaml
    /// </summary>
    public partial class ModbusSelection
    {
        public ModbusSelection()
        {
            for (var i = 0; i < 10000; i++)
                ModbusAddressControl.Add(new ModBus
                {
                    IsChecked = false,
                    ModBusAddress = (i - 1).ToString("X4"),
                    Nickname = "",
                    Comments = ""
                });

            InitializeComponent();
            LoadSettings();
            ModbusAddresses.ItemsSource = ModbusAddressControl;

            ModbusAddresses.Items.Refresh();

            RbtnAbsolute.IsChecked = IsAbsolute;
            RbtnOffset.IsChecked = !IsAbsolute;
        }

        public string DeviceAddress { get; set; } //IP Address or COM address

        public string DeviceName { get; set; }

        public bool IsAbsolute { get; set; } = true;

        public List<ModBus> ModbusAddressControl { get; set; } = new List<ModBus>();

        private void LoadSettings()
        {
            if (
                !Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\RabbitMQ Client"))
                return;

            var docLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            docLocation += @"\RabbitMQ Client\ModbusSettings.xml";

            if (!File.Exists(docLocation))
            {
                File.Create(docLocation).Close();
                var docWriter = new StreamWriter(docLocation);
                var defaultFile = Properties.Resources.DefaultModbusSettingsFile;
                docWriter.Write(defaultFile);
                docWriter.Close();
            }

            var doc = XDocument.Load(docLocation);

            if (doc.Root?.Elements("Device").FirstOrDefault(
                    e => e.Attribute("DeviceName")?.Value == DeviceName) == null) return;
            {
                var target =
                    doc.Root.Elements("Device")
                        .FirstOrDefault(e => e.Attribute("DeviceName")?.Value == DeviceName)?
                        .Element("MemorySettings")?.Elements("MemoryBlock");
                if (target == null) return;
                foreach (var xElement in target)
                {
                    var activeElement = xElement.Element("Active");
                    if (activeElement != null)
                    {
                        var firstOrDefault = ModbusAddressControl.FirstOrDefault(
                            e => e.ModBusAddress == xElement.Element("ModBusAddress")?.Value);
                        if (firstOrDefault != null)
                            firstOrDefault.IsChecked = bool.Parse(activeElement.Value);
                    }

                    var functionElement = xElement.Element("FunctionCodes");
                    if (functionElement != null)
                        foreach (var element in functionElement.Elements("FunctionCode"))
                        {
                            var attribute = element.Attribute("FunctionType")?.Value;
                            switch (attribute)
                            {
                                case "Read Coil":
                                    var firstOrDefaultCoil = ModbusAddressControl.FirstOrDefault(e =>
                                    {
                                        var ex = xElement.Element("ModBusAddress");
                                        return ex != null && e.ModBusAddress == ex.Value;
                                    });
                                    if (firstOrDefaultCoil != null)
                                        firstOrDefaultCoil.ReadCoil = bool.Parse(element.Value);
                                    break;

                                case "Read Discrete":
                                    var firstOrDefaultDiscrete = ModbusAddressControl.FirstOrDefault(e =>
                                    {
                                        var ex = xElement.Element("ModBusAddress");
                                        return ex != null && e.ModBusAddress == ex.Value;
                                    });
                                    if (firstOrDefaultDiscrete != null)
                                        firstOrDefaultDiscrete.ReadDiscrete = bool.Parse(element.Value);
                                    break;

                                case "Read Holding":
                                    var firstOrDefaultHolding = ModbusAddressControl.FirstOrDefault(e =>
                                    {
                                        var ex = xElement.Element("ModBusAddress");
                                        return ex != null && e.ModBusAddress == ex.Value;
                                    });
                                    if (firstOrDefaultHolding != null)
                                        firstOrDefaultHolding.ReadHoldingRegisters = bool.Parse(element.Value);
                                    break;

                                case "Read Input":
                                    var firstOrDefaultInput = ModbusAddressControl.FirstOrDefault(e =>
                                    {
                                        var ex = xElement.Element("ModBusAddress");
                                        return ex != null && e.ModBusAddress == ex.Value;
                                    });
                                    if (firstOrDefaultInput != null)
                                        firstOrDefaultInput.ReadInputRegisters = bool.Parse(element.Value);
                                    break;
                            }
                        }

                    var commentElement =
                        ModbusAddressControl.FirstOrDefault(
                            e =>
                            {
                                var element = xElement.Element("ModBusAddress");
                                return element != null && e.ModBusAddress == element.Value;
                            });

                    if (commentElement != null)
                    {
                        var element = xElement.Element("Comments");
                        if (element != null)
                            commentElement.Comments = element.Value;
                    }

                    var modbusAddressElement = ModbusAddressControl.FirstOrDefault(
                        e =>
                        {
                            var element = xElement.Element("ModBusAddress");
                            return element != null && e.ModBusAddress == element.Value;
                        });
                    if (modbusAddressElement != null)
                    {
                        var nickNameElement = xElement.Element("Nickname");
                        if (nickNameElement != null) modbusAddressElement.Nickname = nickNameElement.Value;
                    }
                }
            }
        }

        private void ChangeSelectionIndex_ButtonClick(object sender, RoutedEventArgs e)
        {
            var btn = (Button) sender;
            ModbusAddresses.SelectedIndex =
                int.Parse(btn.Content.ToString()
                    .Substring(0, btn.Content.ToString().IndexOf("-", StringComparison.Ordinal)));
        }

        private void Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox) e.OriginalSource;
            var index = ModbusAddresses.SelectedIndex;
            if (index == -1) return;
            if (checkBox.IsChecked != null)
                ModbusAddressControl[index].IsChecked = checkBox.IsChecked.Value;
            SaveToFile(index);

            UpdateAddressList(checkBox.IsChecked != null && checkBox.IsChecked.Value, index);
        }

        private void UpdateAddressList(bool IsChecked, int index)
        {
            if (IsChecked) //true
            {
                var keyPair = new Tuple<bool, bool, bool, bool, int>(ModbusAddressControl[index].ReadCoil,
                    ModbusAddressControl[index].ReadDiscrete, ModbusAddressControl[index].ReadHoldingRegisters,
                    ModbusAddressControl[index].ReadInputRegisters, int.Parse(ModbusAddressControl[index].ModBusAddress, System.Globalization.NumberStyles.HexNumber));
                ModbusControls[ModbusControls.Length - 1].ModbusAddressList.Add(keyPair);
            }
            else //false
            {
                var keyPair = new Tuple<bool, bool, bool, bool, int>(ModbusAddressControl[index].ReadCoil,
                    ModbusAddressControl[index].ReadDiscrete, ModbusAddressControl[index].ReadHoldingRegisters,
                    ModbusAddressControl[index].ReadInputRegisters, int.Parse(ModbusAddressControl[index].ModBusAddress,System.Globalization.NumberStyles.HexNumber));
                ModbusControls[ModbusControls.Length - 1].ModbusAddressList.Remove(keyPair);
            }
        }

        private void Function1Checkbox_Checked(object sender, RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox) eventArgs.OriginalSource;
            var index = ModbusAddresses.SelectedIndex;

            if (index == -1 || checkBox.IsChecked == null) return;

            ModbusAddressControl[index].ReadCoil = checkBox.IsChecked.Value;
            SaveToFile(index);
            UpdateAddressList(checkBox.IsChecked != null && checkBox.IsChecked.Value, index);
        }

        private void Function2Checkbox_Checked(object sender, RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox) eventArgs.OriginalSource;
            var index = ModbusAddresses.SelectedIndex;

            if (index == -1 || checkBox.IsChecked == null) return;

            ModbusAddressControl[index].ReadDiscrete = checkBox.IsChecked.Value;
            SaveToFile(index);
            UpdateAddressList(checkBox.IsChecked != null && checkBox.IsChecked.Value, index);
        }

        private void Function3Checkbox_Checked(object sender, RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox) eventArgs.OriginalSource;
            var index = ModbusAddresses.SelectedIndex;

            if (index == -1 || checkBox.IsChecked == null) return;

            ModbusAddressControl[index].ReadHoldingRegisters = checkBox.IsChecked.Value;
            SaveToFile(index);
            UpdateAddressList(checkBox.IsChecked != null && checkBox.IsChecked.Value, index);
        }

        private void Function4Checkbox_Checked(object sender, RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox) eventArgs.OriginalSource;
            var index = ModbusAddresses.SelectedIndex;

            if (index == -1 || checkBox.IsChecked == null) return;

            ModbusAddressControl[index].ReadInputRegisters = checkBox.IsChecked.Value;
            SaveToFile(index);
            UpdateAddressList(checkBox.IsChecked != null && checkBox.IsChecked.Value, index);
        }

        private void SaveToFile(int index)
        {
            if (
                !Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\RabbitMQ Client"))
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                          @"\RabbitMQ Client");

            var docLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            docLocation += @"\RabbitMQ Client\ModbusSettings.xml";

            if (!File.Exists(docLocation))
            {
                File.Create(docLocation).Close();
                var docWriter = new StreamWriter(docLocation);
                var defaultFile = Properties.Resources.DefaultModbusSettingsFile;
                docWriter.Write(defaultFile);
                docWriter.Close();
            }

            var doc = XDocument.Load(docLocation);
            if (doc.Root?.Elements("Device").FirstOrDefault(
                    e => e.Attribute("DeviceName").Value == DeviceName) != null)
            {
                var target = doc.Root.Elements("Device")
                    .Single(e => e.Attribute("DeviceName").Value == DeviceName);

                target.Element("DeviceAddress").Value = DeviceAddress;

                target = target.Element("MemorySettings");

                if (
                    target.Elements("MemoryBlock")
                        .FirstOrDefault(
                            e => e.Element("ModBusAddress")?.Value == ModbusAddressControl[index].ModBusAddress) !=
                    null)
                {
                    target = target.Elements("MemoryBlock")
                        .Single(e => e.Element("ModBusAddress")?.Value == ModbusAddressControl[index].ModBusAddress);
                    var xElement = target.Element("Active");
                    if (xElement != null)
                        xElement.Value = ModbusAddressControl[index].IsChecked.ToString();

                    target.Element("Nickname").Value = ModbusAddressControl[index].Nickname; //update nickname
                    target.Element("Comments").Value = ModbusAddressControl[index].Comments; //update comment

                    target = target.Element("FunctionCodes"); //update function codes

                    target.Elements("FunctionCode")
                        .FirstOrDefault(e => e.Attribute("FunctionType")?.Value == "Read Coil")
                        .Value = ModbusAddressControl[index].ReadCoil.ToString();

                    target.Elements("FunctionCode")
                        .FirstOrDefault(e => e.Attribute("FunctionType")?.Value == "Read Discrete")
                        .Value = ModbusAddressControl[index].ReadDiscrete.ToString();

                    target.Elements("FunctionCode")
                        .FirstOrDefault(e => e.Attribute("FunctionType")?.Value == "Read Holding")
                        .Value = ModbusAddressControl[index].ReadHoldingRegisters.ToString();

                    target.Elements("FunctionCode")
                        .FirstOrDefault(e => e.Attribute("FunctionType")?.Value == "Read Input")
                        .Value = ModbusAddressControl[index].ReadInputRegisters.ToString();

                    doc.Save(docLocation);
                }
                else
                {
                    CreateDevice(2, index); //MemoryBlock doesnt exist create new
                }
            }
            else
            {
                CreateDevice(1, index); //Device Doesnt exist, create new
            }
        }

        private void CreateDevice(int level, int index)
        {
            var functionCodeElement = new XElement("FunctionCodes");

            var readCoil = new XElement("FunctionCode", ModbusAddressControl[index].ReadCoil);
            readCoil.Add(new XAttribute("FunctionType", "Read Coil"));
            functionCodeElement.Add(readCoil);

            var readDiscrete = new XElement("FunctionCode", ModbusAddressControl[index].ReadDiscrete);
            readDiscrete.Add(new XAttribute("FunctionType", "Read Discrete"));
            functionCodeElement.Add(readDiscrete);

            var readHolding = new XElement("FunctionCode", ModbusAddressControl[index].ReadHoldingRegisters);
            readHolding.Add(new XAttribute("FunctionType", "Read Holding"));
            functionCodeElement.Add(readHolding);

            var readInput = new XElement("FunctionCode", ModbusAddressControl[index].ReadInputRegisters);
            readInput.Add(new XAttribute("FunctionType", "Read Input"));
            functionCodeElement.Add(readInput);

            var modbusAddressElement = new XElement("ModBusAddress", ModbusAddressControl[index].ModBusAddress);
            var nickNameElement = new XElement("Nickname", ModbusAddressControl[index].Nickname);
            var commentElement = new XElement("Comments", ModbusAddressControl[index].Comments);
            var useElement = new XElement("Active", ModbusAddressControl[index].IsChecked);
            var element = new XElement("MemoryBlock");
            element.Add(functionCodeElement);
            element.Add(modbusAddressElement);
            element.Add(nickNameElement);
            element.Add(commentElement);
            element.Add(useElement);

            var docLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            docLocation += @"\RabbitMQ Client\ModbusSettings.xml";

            var doc = XDocument.Load(docLocation);

            var target = doc.Root;
            if (level < 2)
            {
                var deviceElement = new XElement("Device");
                deviceElement.Add(new XAttribute("DeviceName", DeviceName));
                target.Add(deviceElement);
                target = target.Elements("Device").Single(e => e.Attribute("DeviceName").Value == DeviceName);
                target.Add(new XElement("DeviceAddress", DeviceAddress));
                target.Add(new XElement("MemorySettings"));
            }

            target =
                doc.Root.Elements("Device")
                    .Single(e => e.Attribute("DeviceName").Value == DeviceName)
                    .Element("MemorySettings");

            if (level < 3)
            {
                if (doc.Root.Elements("Device")
                        .Single(e => e.Attribute("DeviceName").Value == DeviceName)
                        .Elements("MemorySettings").Elements("IsAbsolute").FirstOrDefault() == null)
                    target.Add(new XElement("IsAbsolute", IsAbsolute));
                target.Add(element);
            }

            doc.Save(docLocation);
        }

        private void RbtnAbsolute_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            RbtnOffset.IsChecked = false;
            IsAbsolute = true;
            UpdateAbsolute();
        }

        private void RbtnOffset_OnChecked(object sender, RoutedEventArgs e)
        {
            RbtnAbsolute.IsChecked = false;
            IsAbsolute = false;
            UpdateAbsolute();
        }

        private void UpdateAbsolute()
        {
            var docLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            docLocation += @"\RabbitMQ Client\ModbusSettings.xml";

            var doc = XDocument.Load(docLocation);

            foreach (var xElement in doc.Root.Elements("Device"))
            {
                var target = xElement.Element("MemorySettings");
                target.Value = IsAbsolute.ToString();
            }
        }

        private void Nickname_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var dataCell = (TextBox) ((DataGridCell) sender).Content;

            var index = ModbusAddresses.SelectedIndex;

            if (dataCell.Text == ModbusAddressControl[index].Nickname) return;

            ModbusAddressControl[ModbusAddresses.SelectedIndex].Nickname = dataCell.Text;
            SaveToFile(index);
        }

        private void Comment_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var dataCell = (TextBox) ((DataGridCell) sender).Content;
            ;

            var index = ModbusAddresses.SelectedIndex;

            if (dataCell.Text == ModbusAddressControl[index].Comments) return;

            ModbusAddressControl[ModbusAddresses.SelectedIndex].Comments = dataCell.Text;
            SaveToFile(index);
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtSearchAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            var index = 0;
            var searchField =((ComboBoxItem) CboSearchField.Items[CboSearchField.SelectedIndex]).Content.ToString();
            foreach (var modBus in ModbusAddressControl)
            {
               if (searchField == "Modbus Address")
                {
                    if (modBus.ModBusAddress.ToLower().StartsWith(TxtSearchAddress.Text.ToLower()))
                    {
                        ModbusAddresses.SelectedIndex = index;
                        ModbusAddresses.ScrollIntoView(ModbusAddresses.SelectedItem);
                        break;
                    }
                }
                else if (searchField == "Nickname")
                {
                    if (modBus.Nickname.ToLower().StartsWith(TxtSearchAddress.Text.ToLower()))
                    {
                        ModbusAddresses.SelectedIndex = index;
                        ModbusAddresses.ScrollIntoView(ModbusAddresses.SelectedItem);
                        break;
                    }
                }
                else
                {
                    if (modBus.Comments.ToLower().StartsWith(TxtSearchAddress.Text.ToLower()))
                    {
                        ModbusAddresses.SelectedIndex = index;
                        ModbusAddresses.ScrollIntoView(ModbusAddresses.SelectedItem);
                        break;
                    }
                }

                index++;
            }
        }

        private void ModbusSelection_OnContentRendered(object sender, EventArgs e)
        {
            LoadSettings();
            ModbusAddresses.Items.Refresh();
        }
    }

    public class ModBus
    {
        public bool IsChecked { get; set; }
        public string ModBusAddress { get; set; }
        public string Nickname { get; set; }
        public string Comments { get; set; }

        public bool ReadCoil { get; set; } //Function Code 1
        public bool ReadDiscrete { get; set; } //Function Code 2
        public bool ReadHoldingRegisters { get; set; } //Function Code 3
        public bool ReadInputRegisters { get; set; } //Function Code 4
    }
}