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
    public partial class WriteValuesDialog : Form
    {
        #region Construction
        public WriteValuesDialog()
        {
            InitializeComponent();
        }
        #endregion

        #region Fields
        /// <summary>
        /// Provides access to OPC UA server.
        /// </summary>
        private Session m_Session;
        #endregion

        #region Public Interfaces
        /// <summary>
        /// Displays the dialog.
        /// </summary>
        /// <param name="server">The server instance.</param>
        /// <param name="itemCollection">The <see cref="System.EventArgs"/> Collection of items to display.</param>
        public void Show(Session server, List<NodeId> lstNodeIds)
        {
            if (server == null) throw new ArgumentNullException("server");

            // Set server,
            m_Session = server;

            // Add the items to the listview.
            foreach (NodeId node in lstNodeIds)
            {
                // build WriteValueInfo
                WriteValueInfo info = new WriteValueInfo();
                info.NodeId = node;

                // Create ListViewItem
                ListViewItem item = new ListViewItem("");

                // add subitems for nodeId and value
                item.SubItems.Add(node.ToString());
                item.SubItems.Add("");

                // Set tag on ListViewItem
                item.Tag = info;

                // add item to listview
                this.listView.Items.Add(item);
            }

            // Fit the width of the columns to header size.
            this.listView.Columns[0].Width = -2;
            this.listView.Columns[1].Width = -2;
            this.listView.Columns[2].Width = -2;

            // Read the attributes
            ReadAttributes();

            // read values
            UpdateCurrentValues();

            // Display the control,
            Show();

            // and bring it to front.
            BringToFront();
        }
        #endregion

        /// <summary>
        /// Handles the Ok click event.
        /// Writes the values and then closes the dialog.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void buttonOk_Click(object sender, EventArgs e)
        {
            // Write the values.
            WriteValues();

            // Close dialog.
            Close();
        }

        /// <summary>
        /// Write values without closing the dialog.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void buttonApply_Click(object sender, EventArgs e)
        {
            // Write the values.
            WriteValues();

            // Display the values.
            UpdateCurrentValues();
        }

        /// <summary>
        /// Writes the values.
        /// </summary>
        private void WriteValues()
        {
            try
            {
                // Prepare call to ClientAPI.
                List<WriteValue> nodesToWrite = new List<WriteValue>();
                List<StatusCode> results = null;
                int index = 0;
                List<int> lstUsedIndex = new List<int>();

                // fill write values
                foreach (ListViewItem item in this.listView.Items)
                {
                    WriteValueInfo writeValInfo = item.Tag as WriteValueInfo;

                    if (writeValInfo.AttributesRead == true && writeValInfo.Error == false)
                    {
                        try
                        {
                            // convert string to type
                            if (writeValInfo.ValueRank == ValueRanks.Scalar)
                            {
                                DataValue dataValue = new DataValue();
                                dataValue.Value = TypeUtils.Cast(this.listView.Items[index].SubItems[0].Text, writeValInfo.DataType);

                                nodesToWrite.Add(new WriteValue()
                                {
                                    NodeId = writeValInfo.NodeId,
                                    Value = dataValue,
                                    AttributeId = Attributes.Value
                                });

                                lstUsedIndex.Add(index);
                            }
                            else if (writeValInfo.ValueRank == ValueRanks.OneDimension)
                            {
                                DataValue dataValue = new DataValue();
                                dataValue.Value = TypeUtils.Cast(this.listView.Items[index].SubItems[0].Text, writeValInfo.DataType);

                                nodesToWrite.Add(new WriteValue()
                                {
                                    NodeId = writeValInfo.NodeId,
                                    Value = dataValue,
                                    AttributeId = Attributes.Value
                                });

                                lstUsedIndex.Add(index);
                            }
                        }
                        catch (Exception exception)
                        {
                            this.listView.Items[index].SubItems[0].Text = exception.Message;
                        }
                    }
                    index++;
                }

                // Call to ClientAPI.
                results = m_Session.Write(
                    nodesToWrite,
                    null);

                // Update status label.
                lblStatus.Text = "Writing values succeeded.";
            }
            catch (Exception e)
            {
                // Update status label.
                lblStatus.Text = "An exception occured while writing values: " + e.Message;
            }
        }

        /// <summary>
        /// Reads and displays the new values.
        /// </summary>
        private void UpdateCurrentValues()
        {
            try
            {
                // Prepare call to ClientAPI.
                ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
                List<DataValue> values;

                foreach (ListViewItem item in this.listView.Items)
                {
                    // NodeIds.
                    String sNodeId = item.SubItems[1].Text;
                    nodesToRead.Add(new ReadValueId()
                    {
                        NodeId = NodeId.Parse(sNodeId),
                        AttributeId = Attributes.Value
                    });
                }

                // Call to ClientAPI.
                values = m_Session.Read(
                    nodesToRead,
                    0,
                    TimestampsToReturn.Both,
                    null);

                int i = 0;
                foreach (ListViewItem item in this.listView.Items)
                {
                    // Update current value.
                    item.SubItems[2].Text = values[i].WrappedValue.ToString();
                    i++;
                }

                // Update status label.
                lblStatus.Text = "Updating current values succeeded.";
            }
            catch (Exception e)
            {
                // Update status label.
                lblStatus.Text = "An exception occured while updating current values: "
                    + e.Message;
            }
        }

        /// <summary>
        /// Reads and displays the new values.
        /// </summary>
        private void ReadAttributes()
        {
            // Prepare call to ClientAPI.
            int i = 0;

            foreach (ListViewItem item in this.listView.Items)
            {
                ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
                WriteValueInfo info = item.Tag as WriteValueInfo;

                // DataType
                nodesToRead.Add(new ReadValueId()
                {
                    NodeId = info.NodeId,
                    AttributeId = Attributes.DataType
                });
                // ValueRank
                nodesToRead.Add(new ReadValueId()
                {
                    NodeId = info.NodeId,
                    AttributeId = Attributes.ValueRank
                });
                // ArrayDimension
                nodesToRead.Add(new ReadValueId()
                {
                    NodeId = info.NodeId,
                    AttributeId = Attributes.ArrayDimensions
                });

                // Call to ClientAPI.
                List<DataValue> readResults = new List<DataValue>();

                try
                {
                    readResults = m_Session.Read(
                        nodesToRead,
                        0,
                        TimestampsToReturn.Both,
                        null);

                    if (StatusCode.IsGood(readResults[0].StatusCode) && StatusCode.IsGood(readResults[1].StatusCode))
                    {
                        // datatype
                        info.DataType = TypeUtils.GetBuiltInType((NodeId)readResults[0].Value, m_Session.Cache);

                        // value rank
                        info.ValueRank = (int)readResults[1].Value;

                        // array dimension
                        if(info.ValueRank == ValueRanks.Scalar)
                        {
                            info.ArrayDimensions = null;
                            info.AttributesRead = true;
                        }
                        else if(info.ValueRank == ValueRanks.OneDimension)
                        {
                            info.ArrayDimensions = readResults[1].Value as List<uint>;
                            info.AttributesRead = true;
                        }
                        // can't handle
                        else
                        {
                            info.Error = true;
                        }
                    }
                    else
                    {
                        info.Error = true;
                    }

                    // Update status label.
                    lblStatus.Text = "Reading attributes succeeded.";
                }

                catch (Exception e)
                {
                    // Update status label.
                    lblStatus.Text = "An exception occured while reading attributes: "
                        + e.Message;
                }
                i++;
            }
        }

        /// <summary>
        /// Cancel writing values.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            // Close dialog.
            Close();
        }
    }

    public class WriteValueInfo
    {
        #region Fields
        /// <summary>
        /// The NodeId of the item.
        /// </summary>
        public NodeId NodeId;
        /// <summary>
        /// The DataType of the item.
        /// </summary>
        public BuiltInType DataType;
        /// <summary>
        /// The ValueRank of the item.
        /// </summary>
        public int ValueRank;
        /// <summary>
        /// The ArrayDimension of the item.
        /// </summary>
        public IList<uint> ArrayDimensions;

        public bool AttributesRead = false;
        public bool Error = false;
        #endregion
    }
}
