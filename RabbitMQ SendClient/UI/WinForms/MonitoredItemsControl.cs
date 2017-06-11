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
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;

namespace RabbitMQ_SendClient
{
    public partial class MonitoredItemsControl : UserControl
    {
        /// <summary>
        /// Event handler for the event that the status label of the main form has to be updated.
        /// </summary>
        public delegate void UpdateStatusLabelEventHandler(string strMessage, bool bSuccess);
        /// <summary>
        /// Use the delegate as event.
        /// </summary>
        public event UpdateStatusLabelEventHandler UpdateStatusLabel = null;
        /// <summary>
        /// An exception was thrown.
        /// </summary>
        public void OnUpdateStatusLabel(string strMessage, bool bSuccess)
        {
            if (UpdateStatusLabel != null)
            {
                UpdateStatusLabel(strMessage, bSuccess);
            }
        }

        #region Construction
        public MonitoredItemsControl()
        {
            InitializeComponent();
            //Allow removing the monitored items using the Delete key.
            this.MonitoredItemsLV.KeyDown += OnKeyDownEvent;
        }
        #endregion

        #region Fields
        /// <summary>
        /// Provides access to OPC UA server.
        /// </summary>
        private Session m_Session = null;
        /// <summary>
        /// Provides access to the subscription being created.
        /// </summary>
        private Subscription m_Subscription = null;
        /// <summary>
        /// Publishing enabled for the subscription
        /// </summary>
        private bool m_PublishingEnabled = true;
        /// <summary>
        /// The publishing interval for the subscription
        /// </summary>
        private double m_PublishingInterval = 500;
        #endregion

        #region Properties
        // Server
        public Session Session
        {
            get { return m_Session; }
            set { m_Session = value; }
        }
        // Subscription
        public Subscription Subscription
        {
            get { return m_Subscription; }
        }
        // Publishing interval
        public double PublishingInterval
        {
            get { return m_PublishingInterval; }
            set { m_PublishingInterval = value; }
        }
        // Publishing enabled
        public bool PublishingEnabled
        {
            get { return m_PublishingEnabled; }
            set { m_PublishingEnabled = value; }
        }
        #endregion

        #region Public Interfaces
        public void UpdateSubscription()
        {
            if (m_Subscription != null)
            {
                m_Subscription.PublishingEnabled = m_PublishingEnabled;
                m_Subscription.PublishingInterval = m_PublishingInterval;
                m_Subscription.Modify();
            }
        }

        public void Clear()
        {
            // Cleanup monitored items list.
            MonitoredItemsList.Items.Clear();
            m_Subscription = null;
            m_Session = null;
        }

        public void addMonitoredItem(NodeId nodeId)
        {
            try
            {
                // Create the subscription if it does not already exist.
                if (m_Subscription == null)
                {
                    m_Subscription = new Subscription(m_Session);
                    m_Subscription.PublishingEnabled = m_PublishingEnabled;
                    m_Subscription.PublishingInterval = m_PublishingInterval;
                    m_Subscription.DataChanged += new DataChangedEventHandler(Subscription_DataChanged);
                    m_Subscription.StatusChanged += new SubscriptionStatusChangedEventHandler(Subscription_StatusChanged);
                    m_Subscription.Create();
                }

                // Add the attribute name/value to the list view.
                ListViewItem item = new ListViewItem(nodeId.ToString());

                // Prepare further columns.
                item.SubItems.Add("250"); // Sampling interval by default.
                item.SubItems.Add(String.Empty);
                item.SubItems.Add(String.Empty);
                item.SubItems.Add(String.Empty);
                item.SubItems.Add(String.Empty);
                item.SubItems.Add(String.Empty);

                List<MonitoredItem> monitoredItems = new List<MonitoredItem>();
                List<StatusCode> results = new List<StatusCode>();

                monitoredItems.Add(new DataMonitoredItem(nodeId)
                {
                    DiscardOldest = true,
                    QueueSize = 1,
                    SamplingInterval = 250,
                    UserData = item,
                });

                try
                {
                    // Add the item and apply any changes to it.
                    results = m_Subscription.CreateMonitoredItems(monitoredItems);

                    // Update status label.
                    OnUpdateStatusLabel("Adding monitored item succeeded for NodeId:" +
                        nodeId.ToString(), true);
                }
                catch (Exception exception)
                {
                    item.SubItems[5].Text = results[0].ToString();

                    // Update status label.
                    OnUpdateStatusLabel("An exception occured while adding an item: " +
                        exception.Message, false);
                }

                item.Tag = monitoredItems[0];

                MonitoredItemsLV.Items.Add(item);

                // Fit column width to the longest item and add a few pixel:
                MonitoredItemsLV.Columns[0].Width = -1;
                MonitoredItemsLV.Columns[0].Width += 15;
                // Fit column width to the column content:
                MonitoredItemsLV.Columns[1].Width = -2;
                MonitoredItemsLV.Columns[5].Width = -2;
                // Fix settings:
                MonitoredItemsLV.Columns[2].Width = 95;
                MonitoredItemsLV.Columns[3].Width = 75;
                MonitoredItemsLV.Columns[4].Width = 75;
            }
            catch (Exception exception)
            {
                // Update status label.
                OnUpdateStatusLabel("An exception occured while creating a subscription: " +
                        exception.Message, false);
            }
        }
        #endregion

        #region User Actions and Event Handling
        /// <summary>
        /// Finishes a drag and drop action whereas this control is used as target.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void MonitoredItems_DragDrop(object sender, DragEventArgs e)
        {
            // Retrieve the event data and create the according nodeid.
            String sNodeId = (String)e.Data.GetData(typeof(System.String));
            NodeId nodeId = NodeId.Parse(sNodeId);

            addMonitoredItem(nodeId);
        }

        /// <summary>
        /// Callback to receive data changes from an UA server.
        /// </summary>
        /// <param name="clientHandle">The source of the event.</param>
        /// <param name="value">The instance containing the changed data.</param>
        private void Subscription_DataChanged(Subscription subscription, DataChangedEventArgs e)
        {
            // We have to call an invoke method.
            if (this.InvokeRequired)
            {
                // Asynchronous execution of the valueChanged delegate.
                BeginInvoke(new DataChangedEventHandler(Subscription_DataChanged), subscription, e);
                return;
            }

            // Update the value
            foreach (DataChange change in e.DataChanges)
            {
                ListViewItem item = change.MonitoredItem.UserData as ListViewItem;

                if (item != null)
                {
                    // The node succeeded
                    if (StatusCode.IsGood(change.Value.StatusCode))
                    {
                        // The node succeeded - print the value as string
                        item.SubItems[2].Text = change.Value.WrappedValue.ToString();
                        item.SubItems[3].Text = change.Value.StatusCode.ToString();
                        item.SubItems[4].Text = change.Value.SourceTimestamp.ToLocalTime().ToString();
                        item.SubItems[4].BackColor = Color.White;
                    }
                    // Error
                    else
                    {
                        // The node failed - print the symbolic name of the status code
                        item.SubItems[2].Text = "";
                        item.SubItems[3].Text = change.Value.StatusCode.ToString();
                        item.SubItems[4].Text = change.Value.SourceTimestamp.ToLocalTime().ToString();
                        item.SubItems[4].BackColor = Color.Red;
                    }
                }
            }
        }

        /// <summary>
        /// Callback to receive subscription status change events.
        /// </summary>
        /// <param name="subscription">The source of the event.</param>
        /// <param name="e">The new status of the subscription.</param>
        private void Subscription_StatusChanged(Subscription subscription, SubscriptionStatusChangedEventArgs e)
        {
            // need to make sure this method is called on the UI thread because it updates UI controls.
            if (InvokeRequired)
            {
                BeginInvoke(new SubscriptionStatusChangedEventHandler(Subscription_StatusChanged), subscription, e);
                return;
            }

            try
            {
                // XXX ToDo - show in the GUI e.g. disable monitoring view
                // Deleted,
                // Created,
                // Transferred,
                // Error
            }
            catch (Exception exception)
            {
                ExceptionDlg.Show("Error in Subscription_StatusChanged callback", exception);
            }
        }

        /// <summary>
        /// Handles the drag over event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void MonitoredItems_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        /// <summary>
        /// Handles the click event of the MonitoringMenu_SamplingInterval_Click control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void MonitoringMenu_SamplingInterval_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if operation is currently allowed.
                if (m_Subscription.Session == null || MonitoredItemsLV.SelectedItems.Count == 0)
                {
                    return;
                }

                // Determine the sampling interval being requested.
                double samplingInterval = 0;

                if (sender == toolStripMenuItem_SamplingInterval_100)
                {
                    samplingInterval = 100;
                }
                else if (sender == toolStripMenuItem_SamplingInterval_500)
                {
                    samplingInterval = 500;
                }
                else if (sender == toolStripMenuItem_SamplingInterval_1000)
                {
                    samplingInterval = 1000;
                }

                // Update the monitoring mode.
                List<MonitoredItem> itemsToChange = new List<MonitoredItem>();

                for (int ii = 0; ii < MonitoredItemsLV.SelectedItems.Count; ii++)
                {
                    MonitoredItem monitoredItem = MonitoredItemsLV.SelectedItems[ii].Tag as MonitoredItem;

                    if (monitoredItem != null)
                    {
                        monitoredItem.SamplingInterval = (int)samplingInterval;
                        itemsToChange.Add(monitoredItem);
                    }
                }

                // Apply the changes to the server.
                List<StatusCode> results = new List<StatusCode>();

                results = m_Subscription.ModifyMonitoredItems(itemsToChange);

                for (int ii = 0; ii < results.Count; ii++)
                {
                    // Update the display.
                    MonitoredItemsLV.SelectedItems[ii].SubItems[5].Text = String.Empty;

                    MonitoredItemsLV.SelectedItems[ii].SubItems[1].Text = samplingInterval.ToString();

                    if (StatusCode.IsBad(results[ii]))
                    {
                        MonitoredItemsLV.SelectedItems[ii].SubItems[5].Text = results[ii].ToString();
                    }
                }

                // Update status label.
                OnUpdateStatusLabel("Setting sampling interval succeeded.", true);
            }
            catch (Exception exception)
            {
                // Update status label.
                OnUpdateStatusLabel("An exception occured while setting sampling interval: " + exception.Message, false);
            }
        }

        /// <summary>
        /// Handles the click event of the MonitoringMenu_WriteValues_Click control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void MonitoringMenu_WriteValues_Click(object sender, EventArgs e)
        {
            // build a list of nodeids for the WriteValueDialog
            List<NodeId> lstNodeIds = new List<NodeId>();

            foreach (ListViewItem selectedItem in this.MonitoredItemsLV.SelectedItems)
            {
                String sNodeId = selectedItem.SubItems[0].Text;
                NodeId nodeId = NodeId.Parse(sNodeId);
                lstNodeIds.Add(nodeId);
            }

            // Show write values dialog.
            try
            {
                new WriteValuesDialog().Show(m_Session, lstNodeIds);
            }
            catch (Exception exception)
            {
                // Update status label.
                OnUpdateStatusLabel("An exception occured while writing values: " +
                    exception.Message, false);
            }
        }

        /// <summary>
        /// Handles the click event of the MonitoringMenu_RemoveItems_Click control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void MonitoringMenu_RemoveItems_Click(object sender, EventArgs e)
        {
            try
            {
                // check if operation is currently allowed.
                if (m_Subscription.Session == null || MonitoredItemsLV.SelectedItems.Count == 0)
                {
                    return;
                }

                // Collect the items to delete.
                List<MonitoredItem> monitoredItems = new List<MonitoredItem>();
                List<ListViewItem> itemsToDelete = new List<ListViewItem>();
                List<StatusCode> results = new List<StatusCode>();

                foreach (ListViewItem lvItem in MonitoredItemsLV.SelectedItems)
                {
                    MonitoredItem monitoredItem = lvItem.Tag as MonitoredItem;

                    if (monitoredItem != null)
                    {
                        monitoredItems.Add(monitoredItem);
                        itemsToDelete.Add(lvItem);
                    }
                }

                // remove items on server
                results = m_Subscription.DeleteMonitoredItems(
                            monitoredItems);

                // Remove item(s)
                foreach (ListViewItem lvItem in itemsToDelete)
                {
                    // ToDo - maybe we want to check the status - biut what to do if remove failed??
                    MonitoredItemsLV.Items.Remove(lvItem);
                }

                // Fit column width.
                // NodeId.
                MonitoredItemsLV.Columns[0].Width = -2;
                // Error.
                MonitoredItemsLV.Columns[5].Width = -2;

                // Update status label.
                OnUpdateStatusLabel("Removing monitored items succeeded.", true);
            }
            catch (Exception exception)
            {
                // Update status label.
                OnUpdateStatusLabel("An exception occured while removing monitored items: " +
                    exception.Message, false);
            }
        }

        /// <summary>
        /// Removes the selected monitored items if the pressed key is the Delete key.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnKeyDownEvent(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                MonitoringMenu_RemoveItems_Click(null, null);
            }
        }
        #endregion
    }
}
