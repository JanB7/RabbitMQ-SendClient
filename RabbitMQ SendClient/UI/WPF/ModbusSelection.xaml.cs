using static RabbitMQ_SendClient.General_Classes.ModbusConfig;
using static RabbitMQ_SendClient.SystemVariables;

// ReSharper disable once CheckNamespace

namespace RabbitMQ_SendClient.UI
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using System.Xml.Linq;
    using Button = System.Windows.Controls.Button;
    using CheckBox = System.Windows.Controls.CheckBox;
    using DataGridCell = System.Windows.Controls.DataGridCell;
    using MessageBox = System.Windows.MessageBox;
    using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
    using TextBox = System.Windows.Controls.TextBox;

    /// <summary>
    /// Interaction logic for ModbusSelection.xaml 
    /// </summary>
    public partial class ModbusSelection
    {
        public ModbusSelection()
        {
            for (var i = 0; i < 10000; i++)
            {
                this.ModbusAddressControl.Add(new ModBus
                {
                    IsChecked = false,
                    ModBusAddress = (i - 1).ToString("X4"),
                    Nickname = "",
                    Comments = ""
                });
                _addresses.Add((i - 1).ToString("X4"));
            }

            InitializeComponent();
            LoadSettings();
            ModbusAddresses.ItemsSource = this.ModbusAddressControl;

            ModbusAddresses.Items.Refresh();

            RbtnAbsolute.IsChecked = this.IsAbsolute;
            RbtnOffset.IsChecked = !this.IsAbsolute;
        }

        #region Variables & Structures

        public string DeviceAddress { private get; set; } //IP Address or COM address

        public string DeviceName { private get; set; }

        public bool IsAbsolute { private get; set; } = true;

        private List<ModBus> ModbusAddressControl { get; set; } = new List<ModBus>();

        private struct CsvObject
        {
            ///ToDo connect to FriendlyName for load configuration
            internal string FriendlyName { get; set; }

            internal string Absolute { get; set; }
            internal string Csv { get; set; }
        }

        private List<string> _addresses = new List<string>();

        #endregion Variables & Structures

        private void LoadSettings()
        {
            if (!Directory.Exists(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                @"\RabbitMQ Client"))
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

            if (doc.Root?.Elements("Device").FirstOrDefault(e => e.Attribute("DeviceName")?.Value == this.DeviceName) ==
                null)
                return;
            {
                var target = doc.Root.Elements("Device")
                    .FirstOrDefault(e => e.Attribute("DeviceName")?.Value == this.DeviceName)
                    ?.Element("MemorySettings")
                    ?.Elements("MemoryBlock");
                if (target == null)
                    return;

                foreach (var xElement in target)
                {
                    var activeElement = xElement.Element("Active");
                    if (activeElement != null)
                    {
                        var firstOrDefault = this.ModbusAddressControl.FirstOrDefault(
                            e => e.ModBusAddress ==
                                 xElement.Element("ModBusAddress")?.Value);
                        if (firstOrDefault != null)
                        {
                            firstOrDefault.IsChecked = bool.Parse(activeElement.Value);
                            if (firstOrDefault.IsChecked)
                                UpdateAddressList(firstOrDefault.IsChecked,
                                    this.ModbusAddressControl.IndexOf(firstOrDefault));
                        }
                    }

                    var functionElement = xElement.Element("FunctionCodes");
                    if (functionElement != null)
                        foreach (var element in functionElement.Elements("FunctionCode"))
                        {
                            var attribute = element.Attribute("FunctionType")?.Value;
                            switch (attribute)
                            {
                                case "Read Coil":
                                    var firstOrDefaultCoil = this.ModbusAddressControl.FirstOrDefault(
                                        e =>
                                        {
                                            var ex =
                                                xElement.Element(
                                                    "ModBusAddress");
                                            return
                                                (ex != null) &&
                                                (e.ModBusAddress ==
                                                 ex.Value);
                                        });
                                    if (firstOrDefaultCoil != null)
                                        firstOrDefaultCoil.ReadCoil = bool.Parse(element.Value);
                                    break;

                                case "Read Discrete":
                                    var firstOrDefaultDiscrete = this.ModbusAddressControl.FirstOrDefault(
                                        e =>
                                        {
                                            var ex =
                                                xElement.Element(
                                                    "ModBusAddress");
                                            return
                                                (ex != null) &&
                                                (e.ModBusAddress ==
                                                 ex.Value);
                                        });
                                    if (firstOrDefaultDiscrete != null)
                                        firstOrDefaultDiscrete.ReadDiscrete = bool.Parse(element.Value);
                                    break;

                                case "Read Holding":
                                    var firstOrDefaultHolding = this.ModbusAddressControl.FirstOrDefault(
                                        e =>
                                        {
                                            var ex =
                                                xElement.Element(
                                                    "ModBusAddress");
                                            return
                                                (ex != null) &&
                                                (e.ModBusAddress ==
                                                 ex.Value);
                                        });
                                    if (firstOrDefaultHolding != null)
                                        firstOrDefaultHolding.ReadHoldingRegisters = bool.Parse(element.Value);
                                    break;

                                case "Read Input":
                                    var firstOrDefaultInput = this.ModbusAddressControl.FirstOrDefault(
                                        e =>
                                        {
                                            var ex =
                                                xElement.Element(
                                                    "ModBusAddress");
                                            return
                                                (ex != null) &&
                                                (e.ModBusAddress ==
                                                 ex.Value);
                                        });
                                    if (firstOrDefaultInput != null)
                                        firstOrDefaultInput.ReadInputRegisters = bool.Parse(element.Value);
                                    break;
                            }
                        }

                    var commentElement = this.ModbusAddressControl.FirstOrDefault(
                        e =>
                        {
                            var element =
                                xElement.Element("ModBusAddress");
                            return (element != null) &&
                                   (e.ModBusAddress == element.Value);
                        });

                    if (commentElement != null)
                    {
                        var element = xElement.Element("Comments");
                        if (element != null)
                            commentElement.Comments = element.Value;
                    }

                    var modbusAddressElement = this.ModbusAddressControl.FirstOrDefault(
                        e =>
                        {
                            var element =
                                xElement.Element(
                                    "ModBusAddress");
                            return (element != null) &&
                                   (e.ModBusAddress ==
                                    element.Value);
                        });
                    if (modbusAddressElement != null)
                    {
                        var nickNameElement = xElement.Element("Nickname");
                        if (nickNameElement != null)
                            modbusAddressElement.Nickname = nickNameElement.Value;
                    }
                }
            }
        }

        private void ChangeSelectionIndex_ButtonClick(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            ModbusAddresses.SelectedIndex = int.Parse(
                btn.Content.ToString()
                    .Substring(0,
                        btn.Content.ToString().IndexOf("-", StringComparison.Ordinal)));
            ModbusAddresses.ScrollIntoView(ModbusAddresses.SelectedItem);
        }

        private void Checkbox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox)e.OriginalSource;
            var index = ModbusAddresses.SelectedIndex;
            if (index == -1)
                return;
            if (checkBox.IsChecked != null)
                this.ModbusAddressControl[index].IsChecked = checkBox.IsChecked.Value;
            SaveToFile(index);

            UpdateAddressList((checkBox.IsChecked != null) && checkBox.IsChecked.Value, index);
        }

        private void UpdateAddressList(bool IsChecked, int index)
        {
            if (IsChecked) //true
            {
                var keyPair = new Tuple<bool, bool, bool, bool, int>(this.ModbusAddressControl[index].ReadCoil,
                    this.ModbusAddressControl[index].ReadDiscrete,
                    this.ModbusAddressControl[index].ReadHoldingRegisters,
                    this.ModbusAddressControl[index].ReadInputRegisters,
                    int.Parse(this.ModbusAddressControl[index].ModBusAddress,
                        NumberStyles.HexNumber));
                ModbusControls[ModbusControls.Length - 1].ModbusAddressList.Add(keyPair);
            }
            else //false
            {
                var keyPair = new Tuple<bool, bool, bool, bool, int>(this.ModbusAddressControl[index].ReadCoil,
                    this.ModbusAddressControl[index].ReadDiscrete,
                    this.ModbusAddressControl[index].ReadHoldingRegisters,
                    this.ModbusAddressControl[index].ReadInputRegisters,
                    int.Parse(this.ModbusAddressControl[index].ModBusAddress,
                        NumberStyles.HexNumber));
                ModbusControls[ModbusControls.Length - 1].ModbusAddressList.Remove(keyPair);
            }
        }

        private void Function1Checkbox_Checked(object sender, RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.OriginalSource;
            var index = ModbusAddresses.SelectedIndex;

            if ((index == -1) || (checkBox.IsChecked == null))
                return;

            this.ModbusAddressControl[index].ReadCoil = checkBox.IsChecked.Value;
            SaveToFile(index);
            UpdateAddressList((checkBox.IsChecked != null) && checkBox.IsChecked.Value, index);
        }

        private void Function2Checkbox_Checked(object sender, RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.OriginalSource;
            var index = ModbusAddresses.SelectedIndex;

            if ((index == -1) || (checkBox.IsChecked == null))
                return;

            this.ModbusAddressControl[index].ReadDiscrete = checkBox.IsChecked.Value;
            SaveToFile(index);
            UpdateAddressList((checkBox.IsChecked != null) && checkBox.IsChecked.Value, index);
        }

        private void Function3Checkbox_Checked(object sender, RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.OriginalSource;
            var index = ModbusAddresses.SelectedIndex;

            if ((index == -1) || (checkBox.IsChecked == null))
                return;

            this.ModbusAddressControl[index].ReadHoldingRegisters = checkBox.IsChecked.Value;
            SaveToFile(index);
            UpdateAddressList((checkBox.IsChecked != null) && checkBox.IsChecked.Value, index);
        }

        private void Function4Checkbox_Checked(object sender, RoutedEventArgs eventArgs)
        {
            var checkBox = (CheckBox)eventArgs.OriginalSource;
            var index = ModbusAddresses.SelectedIndex;

            if ((index == -1) || (checkBox.IsChecked == null))
                return;

            this.ModbusAddressControl[index].ReadInputRegisters = checkBox.IsChecked.Value;
            SaveToFile(index);
            UpdateAddressList((checkBox.IsChecked != null) && checkBox.IsChecked.Value, index);
        }

        private void SaveToFile(int index)
        {
            if (!Directory.Exists(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                @"\RabbitMQ Client"))
                Directory.CreateDirectory(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
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
            if (doc.Root?.Elements("Device").FirstOrDefault(e => e.Attribute("DeviceName").Value == this.DeviceName) !=
                null)
            {
                var target = doc.Root.Elements("Device")
                    .Single(e => e.Attribute("DeviceName").Value == this.DeviceName);

                target.Element("DeviceAddress").Value = this.DeviceAddress;

                target = target.Element("MemorySettings");

                if (target.Elements("MemoryBlock")
                        .FirstOrDefault(
                            e => e.Element("ModBusAddress")?.Value == this.ModbusAddressControl[index].ModBusAddress) !=
                    null)
                {
                    target = target.Elements("MemoryBlock")
                        .Single(
                            e => e.Element("ModBusAddress")?.Value == this.ModbusAddressControl[index].ModBusAddress);
                    var xElement = target.Element("Active");
                    if (xElement != null)
                        xElement.Value = this.ModbusAddressControl[index].IsChecked.ToString();

                    target.Element("Nickname").Value = this.ModbusAddressControl[index].Nickname; //update nickname
                    target.Element("Comments").Value = this.ModbusAddressControl[index].Comments; //update comment

                    target = target.Element("FunctionCodes"); //update function codes

                    target.Elements("FunctionCode")
                        .FirstOrDefault(e => e.Attribute("FunctionType")?.Value == "Read Coil")
                        .Value = this.ModbusAddressControl[index].ReadCoil.ToString();

                    target.Elements("FunctionCode")
                        .FirstOrDefault(e => e.Attribute("FunctionType")?.Value == "Read Discrete")
                        .Value = this.ModbusAddressControl[index].ReadDiscrete.ToString();

                    target.Elements("FunctionCode")
                        .FirstOrDefault(e => e.Attribute("FunctionType")?.Value == "Read Holding")
                        .Value = this.ModbusAddressControl[index].ReadHoldingRegisters.ToString();

                    target.Elements("FunctionCode")
                        .FirstOrDefault(e => e.Attribute("FunctionType")?.Value == "Read Input")
                        .Value = this.ModbusAddressControl[index].ReadInputRegisters.ToString();

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

            var readCoil = new XElement("FunctionCode", this.ModbusAddressControl[index].ReadCoil);
            readCoil.Add(new XAttribute("FunctionType", "Read Coil"));
            functionCodeElement.Add(readCoil);

            var readDiscrete = new XElement("FunctionCode", this.ModbusAddressControl[index].ReadDiscrete);
            readDiscrete.Add(new XAttribute("FunctionType", "Read Discrete"));
            functionCodeElement.Add(readDiscrete);

            var readHolding = new XElement("FunctionCode", this.ModbusAddressControl[index].ReadHoldingRegisters);
            readHolding.Add(new XAttribute("FunctionType", "Read Holding"));
            functionCodeElement.Add(readHolding);

            var readInput = new XElement("FunctionCode", this.ModbusAddressControl[index].ReadInputRegisters);
            readInput.Add(new XAttribute("FunctionType", "Read Input"));
            functionCodeElement.Add(readInput);

            var modbusAddressElement = new XElement("ModBusAddress");
            if (cboAddressFormat.SelectedIndex == 0)
                modbusAddressElement.Value = this.ModbusAddressControl[index].ModBusAddress;
            else
                modbusAddressElement.Value = int.Parse(this.ModbusAddressControl[index].ModBusAddress).ToString("X4");

            var nickNameElement = new XElement("Nickname", this.ModbusAddressControl[index].Nickname);
            var commentElement = new XElement("Comments", this.ModbusAddressControl[index].Comments);
            var useElement = new XElement("Active", this.ModbusAddressControl[index].IsChecked);
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
                deviceElement.Add(new XAttribute("DeviceName", this.DeviceName));
                target.Add(deviceElement);
                target = target.Elements("Device").Single(e => e.Attribute("DeviceName").Value == this.DeviceName);
                target.Add(new XElement("DeviceAddress", this.DeviceAddress));
                target.Add(new XElement("MemorySettings"));
            }

            target = doc.Root.Elements("Device")
                .Single(e => e.Attribute("DeviceName").Value == this.DeviceName)
                .Element("MemorySettings");

            if (level < 3)
            {
                if (doc.Root.Elements("Device")
                        .Single(e => e.Attribute("DeviceName").Value == this.DeviceName)
                        .Elements("MemorySettings")
                        .Elements("IsAbsolute")
                        .FirstOrDefault() == null)
                    target.Add(new XElement("IsAbsolute", this.IsAbsolute));
                target.Add(element);
            }

            doc.Save(docLocation);
        }

        private void RbtnAbsolute_Checked(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            RbtnOffset.IsChecked = false;

            this.IsAbsolute = true;
            UpdateTable();
            UpdateAbsolute();
        }

        private void RbtnOffset_OnChecked(object sender, RoutedEventArgs e)
        {
            RbtnAbsolute.IsChecked = false;
            this.IsAbsolute = false;
            UpdateTable();
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
                target.Value = this.IsAbsolute.ToString();
            }
        }

        private void Nickname_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var dataCell = (TextBox)((DataGridCell)sender).Content;

            var index = ModbusAddresses.SelectedIndex;

            if (dataCell.Text == this.ModbusAddressControl[index].Nickname)
                return;

            this.ModbusAddressControl[ModbusAddresses.SelectedIndex].Nickname = dataCell.Text;
            SaveToFile(index);
        }

        private void Comment_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var dataCell = (TextBox)((DataGridCell)sender).Content;
            ;

            var index = ModbusAddresses.SelectedIndex;

            if (dataCell.Text == this.ModbusAddressControl[index].Comments)
                return;

            this.ModbusAddressControl[ModbusAddresses.SelectedIndex].Comments = dataCell.Text;
            SaveToFile(index);
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            foreach (var modbusControl in ModbusControls)
            {
                for (var index = 0; index < modbusControl.ModbusAddressList.Count; index++)
                {
                    var tuple = modbusControl.ModbusAddressList[index];
                    if (tuple.Item1 || tuple.Item2 || tuple.Item3 || tuple.Item4) continue;
                    //all false, remove
                    modbusControl.ModbusAddressList.Remove(tuple);
                    index--;
                }
            }
            this.DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Array.Clear(ModbusControls, 0, ModbusControls.Length - 1);
            this.DialogResult = false;
            Close();
        }

        private void TxtSearchAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            var index = 0;
            var searchField =
                ((ComboBoxItem)CboSearchField.Items[CboSearchField.SelectedIndex]).Content.ToString();
            foreach (var modBus in this.ModbusAddressControl)
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

        private void cboAddressFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsInitialized) return;
            UpdateTable();
        }

        private void BtnLoadCustom_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = "*.xml",
                Filter =
                    "Comma Seperated Files(*.csv)|(*.csv)|eXtensible Markup Language (*.xml)|*.xml|JavaScript Object Notation(*.json)|(*.json)"
            };

            var result = dialog.ShowDialog();
            string fileName;
            if ((result != null) && result.Value)
            {
                fileName = dialog.FileName;
            }
            else
            {
                var docLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                docLocation += @"\RabbitMQ Client\DefaultSettings.xml";

                if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                      @"\RabbitMQ Client"))
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                                              @"\RabbitMQ Client");

                if (!File.Exists(docLocation))
                    try
                    {
                        var allLines = ReadAllResourceLines(Properties.Resources.ModbusAddresses);
                        File.Create(docLocation).Close();
                        File.WriteAllLines(docLocation, allLines);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Data format incorrect.\n\nDetialed Information:\n" + ex.Message,
                            "Incorrect Format", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                fileName = docLocation;
            }

            if (fileName.Contains(".xml"))
            {
                try
                {
                    var ssReader = XDocument.Load(fileName);

                    var target = ssReader.Root?.Element("Absolute");
                    if (target != null) this.IsAbsolute = bool.Parse(target.Value);
                    target = ssReader.Root?.Element("CSV");

                    var allLines = ReadAllResourceLines(target?.Value);
                    _addresses = allLines.SelectMany(line => line.Split(',').ToList()).ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Data format incorrect.\n\nDetialed Information:\n" + ex.Message,
                        "Incorrect Format", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (fileName.Contains(".json"))
            {
                var ssReader = File.ReadAllLines(fileName);

                var allLines = ssReader.Aggregate("", (current, stringItem) => current + stringItem);

                var csv = JsonConvert.DeserializeObject<CsvObject>(allLines);
                ssReader = csv.Csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                this.IsAbsolute = bool.Parse(csv.Absolute);
                _addresses.Clear();
                _addresses = ssReader.SelectMany(line => line.Split(',').ToList()).ToList();
            }
            else if (fileName.Contains(".csv"))
            {
                var allLines = File.ReadAllLines(fileName);
                _addresses.Clear();
                _addresses = allLines.SelectMany(line => line.Split(',').ToList()).ToList();
            }
            else
            {
                MessageBox.Show("Incorrect Fileformat, please choose the correct filetype.", "Incorrect Filetype",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            UpdateTable();
            LoadSettings();

            this.Dispatcher.Invoke((MethodInvoker)delegate
           {
               RbtnAbsolute.IsChecked = this.IsAbsolute;
               RbtnOffset.IsChecked = !this.IsAbsolute;
           });
        }

        private static bool IsDigitsOnly(string str)
        {
            return str.All(c => (c >= '0') && (c <= '9'));
        }

        private void UpdateTable()
        {
            this.ModbusAddressControl.Clear();

            for (var i = 0; i < _addresses.Count; i++)
            {
                _addresses[i] = Regex.Replace(_addresses[i], @"\t|\n|\r", "");

                if (_addresses[i] != "") continue;
                _addresses.RemoveAt(i);
                i = i - 1;
            }

            var isNotHex = _addresses.All(IsDigitsOnly);

            if (isNotHex)
                for (var index = 0; index < _addresses.Count; index++)
                {
                    this.ModbusAddressControl.Add(
                        new ModBus
                        {
                            IsChecked = false,
                            Nickname = "",
                            Comments = ""
                        });

                    if (int.TryParse(_addresses[index], out int num))
                    {
                        if (!this.IsAbsolute) num--;
                        this.ModbusAddressControl[index].ModBusAddress =
                            num.ToString(cboAddressFormat.SelectedIndex == 0 ? "X4" : "D5");
                    }
                    else
                    {
                        var provider = CultureInfo.CurrentCulture;
                        int.TryParse(_addresses[index], NumberStyles.HexNumber, provider, out int nm);
                        if (!this.IsAbsolute) nm--;
                        this.ModbusAddressControl[index].ModBusAddress =
                            nm.ToString(cboAddressFormat.SelectedIndex == 0 ? "X4" : "D5");
                    }
                }
            else
                foreach (var t in _addresses)
                {
                    try
                    {
                        this.ModbusAddressControl.Add(
                            new ModBus
                            {
                                IsChecked = false,
                                Nickname = "",
                                ModBusAddress = !this.IsAbsolute
                                    ? (int.Parse(t, NumberStyles.HexNumber) - 1).ToString("X4")
                                    : t,
                                Comments = ""
                            });
                    }
                    catch (Exception)
                    {
                        this.ModbusAddressControl.Clear();
                        for (var i = 0; i < 10000; i++)
                        {
                            this.ModbusAddressControl.Add(new ModBus
                            {
                                IsChecked = false,
                                ModBusAddress = (i - 1).ToString("X4"),
                                Nickname = "",
                                Comments = ""
                            });
                            _addresses.Add((i - 1).ToString("X4"));
                        }
                        LoadSettings();

                        MessageBox.Show(
                            "Addresses in neither hex nor integer format. Please check address format and try again",
                            "Formatting error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

            LoadSettings();
            ModbusAddresses.Items.Refresh();
        }
    }

    public class ModBus
    {
        #region Variables & Structures

        public bool IsChecked { get; set; }
        public string ModBusAddress { get; set; }
        public string Nickname { get; set; }
        public string Comments { get; set; }

        public bool ReadCoil { get; set; } //Function Code 1
        public bool ReadDiscrete { get; set; } //Function Code 2
        public bool ReadHoldingRegisters { get; set; } //Function Code 3
        public bool ReadInputRegisters { get; set; } //Function Code 4

        #endregion Variables & Structures
    }
}