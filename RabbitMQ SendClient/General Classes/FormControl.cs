using static RabbitMQ_SendClient.MainWindow;
using static RabbitMQ_SendClient.SystemVariables;

namespace RabbitMQ_SendClient.General_Classes
{
    using System;
    using System.Linq;
    using UI;

    internal static class FormControl
    {
        public static void Uncheck(Guid uidGuid)
        {
            var index = GetIndex<CheckListItem>(uidGuid);

            if (AvailableSerialPorts.Any(availableSerialPort => uidGuid == Guid.Parse(availableSerialPort.UidGuid)))
            {
                var checkItem = new CheckListItem
                {
                    ItemName = AvailableSerialPorts[index].ItemName,
                    UidGuid = AvailableSerialPorts[index].UidGuid,
                    Content = AvailableSerialPorts[index].Content,
                    IsChecked = false
                };
                AvailableSerialPorts.RemoveAt(index);
                AvailableSerialPorts.Insert(index, checkItem);
            }
            else if (AvailableModbusSerialPorts.Any(
                availableModbusSerialPort => uidGuid == Guid.Parse(availableModbusSerialPort.UidGuid)))
            {
                var checkItem = new CheckListItem
                {
                    ItemName = AvailableModbusSerialPorts[index].ItemName,
                    UidGuid = AvailableModbusSerialPorts[index].UidGuid,
                    Content = AvailableModbusSerialPorts[index].Content,
                    IsChecked = false
                };
                AvailableModbusSerialPorts.RemoveAt(index);
                AvailableModbusSerialPorts.Insert(index, checkItem);
            }
            else
            {
                throw new NullReferenceException();
            }
        }

        public static void ChangeCheck(Guid uidGuid)
        {
            var cboCheckListItem = OppositeCheck(uidGuid);
            var index = GetIndex<CheckListItem>(uidGuid);

            if (cboCheckListItem == null) return;
            var cbo = (CheckListItem) cboCheckListItem;

            cbo.IsChecked = false;
            CheckListItem item;
            switch (CheckType(uidGuid))
            {
                case 1:
                    //disables Modbus COM Port
                    item = AvailableSerialPorts.First(
                        availableSerialPorts => Guid.Parse(availableSerialPorts.UidGuid) == uidGuid);
                    item.IsChecked = false;
                    AvailableModbusSerialPorts.RemoveAt(index);
                    AvailableModbusSerialPorts.Insert(index, cbo);
                    break;

                case 2:

                    //disables Serial COM Port
                    item = AvailableModbusSerialPorts.First(
                        availableModbusSerialPorts => Guid.Parse(availableModbusSerialPorts.UidGuid) == uidGuid);
                    item.IsChecked = false;

                    AvailableSerialPorts.RemoveAt(index);
                    AvailableSerialPorts.Insert(index, cbo);
                    break;

                case null:
                    return;

                default:
                    return;
            }
        }

        public static bool? GetFriendlyName(Guid uidGuid)
        {
            var friendlyNameForm = new VariableConfigure(uidGuid);
            return friendlyNameForm.ShowDialog();
        }
    }
}