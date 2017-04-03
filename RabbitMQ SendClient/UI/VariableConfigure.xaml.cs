using System;
using System.Windows;
using static RabbitMQ_SendClient.SystemVariables;

// ReSharper disable CheckNamespace

namespace RabbitMQ_SendClient.UI
{
    /// <summary>
    ///     Interaction logic for VariableConfigure.xaml
    /// </summary>
    public partial class VariableConfigure
    {
        public VariableConfigure(Guid uidGuid)
        {
            InitializeComponent();
            UidGuid = uidGuid;
            foreach (var friend in FriendlyName)
                CboFriendlies.Items.Add(friend);
            if (CboFriendlies.Items.Count > 0)
                CboFriendlies.SelectedIndex = 0;
        }

        public Guid UidGuid { get; set; }
        protected internal int SelectedIndex { get; set; }

        private static void ResizeJsonObject()
        {
            var jsonObject = JsonObjects;
            if (jsonObject == null) return;
            Array.Resize(ref jsonObject, JsonObjects.Length + 1);
            JsonObjects = jsonObject;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            ResizeJsonObject();
            GetXML(CboFriendlies.Items[CboFriendlies.SelectedIndex].ToString(), UidGuid);
            SelectedIndex = CboFriendlies.SelectedIndex;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            SelectedIndex = -1;
            Close();
        }

        private void ReconnectYes_Checked(object sender, RoutedEventArgs e)
        {
            ReconnectOnStartup = true;
        }

        private void ReconnectNo_Checked(object sender, RoutedEventArgs e)
        {
            ReconnectOnStartup = false;
        }
    }
}