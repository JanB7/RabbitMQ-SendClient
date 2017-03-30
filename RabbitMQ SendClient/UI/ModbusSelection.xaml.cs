using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

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
            RbtnAbsolute.IsChecked = IsAbsolute;
            RbtnOffset.IsChecked = !IsAbsolute;
        }

        public string DeviceAddress { get; set; } = "127.0.0.1"; //IP Address or COM address
        public string DeviceName { get; set; } = "TestDevice";
        public int FunctionCode { get; set; } = 4;
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
                if (target != null)
                    foreach (var xElement in target)
                    {
                        var o = xElement.Element("Active");
                        if (o != null)
                        {
                            var firstOrDefault = ModbusAddressControl.FirstOrDefault(
                                e => e.ModBusAddress == xElement.Element("ModBusAddress")?.Value);
                            if (firstOrDefault != null)
                                firstOrDefault.IsChecked = bool.Parse(o.Value);
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
            ModbusAddressControl[ModbusAddresses.SelectedIndex].IsChecked = checkBox.IsChecked.Value;
            SaveToFile(ModbusAddresses.SelectedIndex);
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
            if (doc.Root.Elements("Device").FirstOrDefault(
                    e => e.Attribute("DeviceName").Value == DeviceName) != null)
            {
                var target = doc.Root.Elements("Device")
                    .Single(e => e.Attribute("DeviceName").Value == DeviceName);

                target.Element("DeviceAddress").Value = DeviceAddress;

                target = target.Element("MemorySettings");

                if (
                    target.Elements("MemoryBlock")
                        .FirstOrDefault(
                            e => e.Element("ModBusAddress").Value == ModbusAddressControl[index].ModBusAddress) !=
                    null)
                {
                    target = target.Elements("MemoryBlock")
                        .Single(e => e.Element("ModBusAddress").Value == ModbusAddressControl[index].ModBusAddress);
                    target.Element("Active").Value = ModbusAddressControl[index].IsChecked.ToString();
                    target.Element("FunctionCode").Value = FunctionCode.ToString(); //update function code
                    target.Element("Nickname").Value = ModbusAddressControl[index].Nickname; //update nickname
                    target.Element("Comments").Value = ModbusAddressControl[index].Comments; //update comment
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
            var functionCodeElement = new XElement("FunctionCode", FunctionCode);
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
                target.Add(new XElement("Device"));
                target = target.Element("Device");
                target.Add(new XAttribute("DeviceName", DeviceName));
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
            updateAbsolute();
        }

        private void RbtnOffset_OnChecked(object sender, RoutedEventArgs e)
        {
            RbtnAbsolute.IsChecked = false;
            IsAbsolute = false;
            updateAbsolute();
        }

        private void updateAbsolute()
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
    }

    public class ModBus
    {
        public bool IsChecked { get; set; }
        public string ModBusAddress { get; set; }
        public string Nickname { get; set; }
        public string Comments { get; set; }
    }
}