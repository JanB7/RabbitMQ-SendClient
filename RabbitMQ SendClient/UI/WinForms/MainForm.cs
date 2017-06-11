/******************************************************************************
** Copyright (c) 2006-2016 Unified Automation GmbH All rights reserved.
**
** Software License Agreement ("SLA") Version 2.5
**
** Unless explicitly acquired and licensed from Licensor under another
** license, the contents of this file are subject to the Software License
** Agreement ("SLA") Version 2.5, or subsequent versions
** as allowed by the SLA, and You may not copy or use this file in either
** source code or executable form, except in compliance with the terms and
** conditions of the SLA.
**
** All software distributed under the SLA is provided strictly on an
** "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED,
** AND LICENSOR HEREBY DISCLAIMS ALL SUCH WARRANTIES, INCLUDING WITHOUT
** LIMITATION, ANY WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
** PURPOSE, QUIET ENJOYMENT, OR NON-INFRINGEMENT. See the SLA for specific
** language governing rights and limitations under the SLA.
**
** Project: .NET based OPC UA Client Server SDK
**
** Description: OPC Unified Architecture Software Development Kit.
**
** The complete license agreement can be found here:
** http://unifiedautomation.com/License/SLA/2.5/
******************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;

namespace RabbitMQ_SendClient
{
    /// <summary>
    /// The main form of the user interface.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Construction
        /// <summary>
        /// Initializes the controls of this form. Registers for an particular event.
        /// </summary>
        public MainForm(ApplicationInstance applicationInstance)
        {
            // Initialize controls.
            InitializeComponent();

            m_Application = applicationInstance;

            // Register for the SelectionChanged event of BrowseControls in order to update
            // the ListView of AttributeListControl.
            _browseControlses.SelectionChanged += new BrowseControls.SelectionChangedEventHandler(browserControl_SelectionChanged);

            // Register for the Node Activated event of BrowseControls in order to add item for monitoring in monitoredItems_Control
            _browseControlses.NodeActivated += new BrowseControls.NodeActivatedEventHandler(browserControl_NodeActivated);

            // Register for the update statuslabel event of AttriuteListControl in order to update
            // the status label.
            attributeListControl.UpdateStatusLabel +=
                new AttributeListControl.UpdateStatusLabelEventHandler(UserControl_UpdateStatusLabel);

            // Register for the update statuslabel event of MonitoredItemsControl in order to update
            // the status label.
            monitoredItems_Control.UpdateStatusLabel +=
                new MonitoredItemsControl.UpdateStatusLabelEventHandler(UserControl_UpdateStatusLabel);

            m_timer = new Timer();
            m_timer.Tick += new EventHandler(TimerElapsed);
            m_timer.Interval = 5000;
            m_timer.Enabled = true;

            StatusLabelSTS.LayoutStyle = ToolStripLayoutStyle.Flow;
        }

        public MainForm()
        {
        }
        #endregion

        #region Fields
        /// <summary>
        /// Provides access to the OPC UA server and its services.
        /// </summary>
        private Session m_Session = null;
        /// <summary>
        /// Provides access to the OPC UA server and its services.
        /// </summary>
        private ApplicationInstance m_Application = null;
        /// <summary>
        /// Flag indicates if the connection is successfully established or not.
        /// </summary>
        private bool m_bConnected = false;
        /// <summary>
        /// Timer to monitor if the license has expired
        /// </summary>
        private Timer m_timer = null;
        #endregion

        #region Properties
        /// <summary>
        /// Provides the text of the selected item of the combobox.
        /// </summary>
        public string ServerURL
        {
            get { return UrlCB.Text; }
        }
        /// <summary>
        /// Provides the status label toolstrip.
        /// </summary>
        public System.Windows.Forms.ToolStripStatusLabel StatusLabel
        {
            get { return toolStripStatusLabel; }
        }
        #endregion

        #region Calls to Client API
        /// <summary>
        /// Connect to server.
        /// </summary>
        private int Connect()
        {
            if (m_Session == null)
            {
                m_Session = new Session(m_Application);
                m_Session.UseDnsNameAndPortFromDiscoveryUrl = true;

                // Attach to events
                m_Session.ConnectionStatusUpdate += new ServerConnectionStatusUpdateEventHandler(Session_ServerConnectionStatusUpdate);
            }

            // Check the content of the combobox.
            if( UrlCB.Text.Length == 0 )
            {
                return -1;
            }

            // Set wait cursor.
            Cursor = Cursors.WaitCursor;
            int result = 0;

            try
            {
                string endpointUrl;

                // Extract Url from combobox text.
                object item = UrlCB.SelectedItem;
                if ((item == null) || (item.GetType() == typeof(string)))
                {
                    // The URL has been entered as text.
                    endpointUrl = UrlCB.Text;

                    // Call connect with URL
                    m_Session.Connect(endpointUrl, SecuritySelection.None);
                }
                else
                {
                    // The endpoint was provided through discovery.
                    EndpointWrapper endpoint = (EndpointWrapper)item;

                    // Call connect with endpoint
                    m_Session.Connect(endpoint.Endpoint, null);
                }
            }
            catch (Exception e)
            {
                result = -1;

                // Update status label.
                StatusException se = e as StatusException;

                if (se != null)
                {
                    toolStripStatusLabel.Text = String.Concat("Connect failed. Error [", se.StatusCode.ToString(), "] ", e.Message);
                }
                else
                {
                    toolStripStatusLabel.Text = "Connect failed. Error: " + e.Message;
                }

                toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.error;
            }

            // Set default cursor.
            Cursor = Cursors.Default;
            return result;
        }

        /// <summary>
        /// Disconnect from server.
        /// </summary>
        private void Disconnect()
        {
            try
            {
                // Call the disconnect service of the server.
                m_Session.Disconnect(SubscriptionCleanupPolicy.Delete, null);
            }
            catch (Exception exception)
            {
                // Update status label.
                toolStripStatusLabel.Text = "Disconnect failed. Error: " + exception.Message;
                toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.error;
            }
        }
        #endregion

        #region User Actions
        /// <summary>
        /// Callback of the exception thrown event of BrowseControls and AttributeListControl.
        /// </summary>
        /// <param name="node">The source of the event.</param>
        private void UserControl_UpdateStatusLabel(string strMessage, bool bSuccess)
        {
            toolStripStatusLabel.Text = strMessage;

            if (bSuccess == true)
            {
                toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.success;
            }
            else
            {
                toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.error;
            }
        }

        /// <summary>
        /// Callback of the selection changed event of BrowseControls.
        /// </summary>
        /// <param name="node">The source of the event.</param>
        private void browserControl_SelectionChanged(TreeNode node)
        {
            // Read all the attributes of the selected tree node.
            attributeListControl.ReadAttributes(node);
        }

        private void browserControl_NodeActivated(NodeId activatedNode)
        {
            // Add monitored items
            monitoredItems_Control.addMonitoredItem(activatedNode);
        }

        /// <summary>
        /// Expands the drop down list of the ComboBox to display available servers and endpoints.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void UrlCB_DropDown(object sender, EventArgs e)
        {
            // get discovery url
            string sUrl;

            // Check the text property of the Server textbox
            if (NodeTB.Text.Length == 0)
            {
                // Set the uri of the local discovery server by default.
                sUrl = "opc.tcp://localhost:4840";
            }
            else
            {
                // Has the port been entered by the user?
                char seperator = ':';
                string[] strPortCheck = NodeTB.Text.Split(seperator);
                if (strPortCheck.Length > 1)
                {
                    sUrl = NodeTB.Text;
                }
                else
                {
                    sUrl = NodeTB.Text + ":4840";
                }
            }

            // Set wait cursor.
            Cursor = Cursors.WaitCursor;

            // Clear all items of the ComboBox.
            UrlCB.Items.Clear();
            UrlCB.Text = "";

            // Look for servers
            List<ApplicationDescription> serverList = null;
            using (Discovery discovery = new Discovery(m_Application))
            {
                try
                {
                    serverList = discovery.FindServers(sUrl);
                }
                catch (Exception exception)
                {
                    // Update status label.
                    toolStripStatusLabel.Text = "FindServers failed:" + exception.Message;
                    toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.error;
                    // Set default cursor.
                    Cursor = Cursors.Default;
                    return;
                }

                bool bGetEndpointsError = false;
                bool bIgnoreError = false;
                string errorText = "";
                List<EndpointWrapper> lstEndpointWrappers = new List<EndpointWrapper>();

                // Populate the drop down list with the endpoints for the available servers.
                foreach (ApplicationDescription server in serverList)
                {
                    if (server.ApplicationType == ApplicationType.Client || server.ApplicationType == ApplicationType.DiscoveryServer)
                    {
                        continue;
                    }

                    try
                    {
                        StringCollection lstEndpoint = new StringCollection();

                        foreach (string discoveryUrl in server.DiscoveryUrls)
                        {
                            // Call GetEndpoints
                            List<EndpointDescription> lstEndpoints = null;

                            try
                            {
                                lstEndpoints = discovery.GetEndpoints(discoveryUrl);

                                foreach (EndpointDescription endpoint in lstEndpoints)
                                {
                                    // build display name for combo
                                    EndpointWrapper endpointWrap = new EndpointWrapper(endpoint);
                                    if (!lstEndpoint.Contains(endpointWrap.ToString()))
                                    {
                                        lstEndpointWrappers.Add(endpointWrap);
                                        lstEndpoint.Add(endpointWrap.ToString());
                                        bIgnoreError = true;
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        // Update status label.
                        errorText = "GetEndpoints failed. Error: " + exception.Message;
                        bGetEndpointsError = true;
                    }
                }

                // error occured during get endpoints
                if (bGetEndpointsError && !bIgnoreError)
                {
                    // Update status label.
                    toolStripStatusLabel.Text = "GetEndpoints failed. Error: " + errorText;
                    toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.error;
                }
                // add list of endpoints
                else
                {
                    UrlCB.Items.AddRange(lstEndpointWrappers.ToArray());
                }
            }

            // Set default cursor.
            Cursor = Cursors.Default;
        }

        /// <summary>
        /// Handles the connect procedure being started from the menu bar.
        /// <summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void connectDisconnectTriggered(object sender, EventArgs e)
        {
            // Currently connected -> disconnect.
            if (m_bConnected)
            {
                Disconnect();
            }
            // Currently not connected -> connect to server.
            else
            {
                Connect();
            }
        }

        /// <summary>
        // Handles the publishing interval procedure started from the menu bar.
        /// <summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void PublishingInterval_Click(object sender, EventArgs e)
        {
            if(monitoredItems_Control.Subscription != null)
            {
                PublishingIntervalDialog dlg = new PublishingIntervalDialog(monitoredItems_Control.Subscription);
                dlg.Show();
            }
        }

        /// <summary>
        /// Change the enabled state for the subscription
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void publishingEnabled_Click(object sender, EventArgs e)
        {
            monitoredItems_Control.PublishingEnabled = publishingEnabledToolStripMenuItem.Checked;
            monitoredItems_Control.UpdateSubscription();
        }
        #endregion

        #region Event Handler
        /// <summary>
        ///
        /// </summary>
        private void Session_ServerConnectionStatusUpdate(Session sender, ServerConnectionStatusUpdateEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new ServerConnectionStatusUpdateEventHandler(Session_ServerConnectionStatusUpdate), sender, e);
                return;
            }

            // check that the current session matches the session that raised the event.
            if (!Object.ReferenceEquals(m_Session, sender))
            {
                return;
            }

            lock (this)
            {

                bool bClearControls = false;

                switch (e.Status)
                {
                    case ServerConnectionStatus.Disconnected:
                        m_bConnected = false;
                        // Update Button
                        ConnectDisconnectBTN.Text = "Connect";
                        // Update ToolStripMenu
                        connectToolStripMenuItem.Enabled = true;
                        disconnectToolStripMenuItem.Enabled = false;
                        // Set enabled state for combobox
                        UrlCB.Enabled = true;
                        // Update status label.
                        toolStripStatusLabel.Text = "Disconnected";
                        toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.warning;
                        // clear controls
                        bClearControls = true;
                        break;
                    case ServerConnectionStatus.Connected:
                        m_bConnected = true;
                        // Update Button
                        ConnectDisconnectBTN.Text = "Disconnect";
                        // Update ToolStripMenu
                        connectToolStripMenuItem.Enabled = false;
                        disconnectToolStripMenuItem.Enabled = true;
                        // Set enabled state for combobox
                        UrlCB.Enabled = false;

                        // Aggregate the UserControls.
                        _browseControlses.Session = m_Session;
                        attributeListControl.Session = m_Session;
                        monitoredItems_Control.Session = m_Session;

                        // Update status label.
                        toolStripStatusLabel.Text = "Connected to " + m_Session.EndpointDescription.EndpointUrl;
                        toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.success;

                        // Browse first level.
                        _browseControlses.Browse(null);
                        break;
                    case ServerConnectionStatus.ConnectionWarningWatchdogTimeout:
                        // Update status label.
                        toolStripStatusLabel.Text = "Watchdog timed out";
                        toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.warning;
                        break;
                    case ServerConnectionStatus.ConnectionErrorClientReconnect:
                        // Update status label.
                        toolStripStatusLabel.Text = "Trying to reconnect";
                        toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.warning;
                        break;
                    case ServerConnectionStatus.ServerShutdownInProgress:
                        // Update status label.
                        toolStripStatusLabel.Text = "Server is shutting down";
                        toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.warning;
                        break;
                    case ServerConnectionStatus.ServerShutdown:
                        // Update status label.
                        toolStripStatusLabel.Text = "Server has shut down";
                        toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.warning;
                        break;
                    case ServerConnectionStatus.SessionAutomaticallyRecreated:
                        // Update status label.
                        toolStripStatusLabel.Text = "A new Session was created";
                        toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.success;
                        // clear controls
                        bClearControls = true;
                        break;
                    case ServerConnectionStatus.Connecting:
                        // Update status label.
                        toolStripStatusLabel.Text = "Trying to connect to " + ((m_Session.EndpointDescription != null) ? m_Session.EndpointDescription.EndpointUrl : "<unknown>");
                        toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.warning;
                        break;
                    case ServerConnectionStatus.LicenseExpired:
                        // Update status label.
                        toolStripStatusLabel.Text = "The license has expired.";
                        toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.warning;
                        // disable GUI
                        serverToolStripMenuItem.Enabled = false;
                        subscriptionToolStripMenuItem.Enabled = false;
                        ConnectDisconnectBTN.Enabled = false;
                        UrlCB.Enabled = false;
                        bClearControls = true;
                        break;
                }

                if (bClearControls)
                {
                    // Cleanup attribute list.
                    this.attributeListControl.AttributeList.Items.Clear();

                    // Cleanup treeview.
                    _browseControlses.BrowseTree.BeginUpdate();
                    _browseControlses.BrowseTree.Nodes.Clear();
                    _browseControlses.BrowseTree.EndUpdate();

                    // Aggregate the UserControls.
                    _browseControlses.Session = null;
                    attributeListControl.Session = null;

                    monitoredItems_Control.Clear();
                }
            }
        }

        private void TimerElapsed(object sender, EventArgs e)
        {
            if(m_Session != null && m_Session.ConnectionStatus == ServerConnectionStatus.LicenseExpired)
            {
                // show dialog once only - so we just stop the timer
                m_timer.Enabled = false;
                MessageBox.Show("The demo license has expired.\nRestart the application.", "License expired", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        #endregion
    }
}
