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
    public partial class AttributeListControl : UserControl
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
        public AttributeListControl()
        {
            InitializeComponent();
        }
        #endregion

        #region Fields
        /// <summary>
        /// Provides access to OPC UA server.
        /// </summary>
        private Session m_Session = null;
        /// <summary>
        /// Keeps current value to write.
        /// </summary>
        private WriteValue m_CurrentWriteValue;
        /// <summary>
        /// Keeps current value.
        /// </summary>
        private Object m_CurrentValue;
        /// <summary>
        /// Keeps the name of the node to write.
        /// </summary>
        private string m_CurrentWriteNodeName;
        #endregion

        #region Properties
        public Session Session
        {
            get { return m_Session; }
            set { m_Session = value; }
        }
        #endregion

        #region Calls to ClientWrapper API
        /// <summary>
        /// Helper function for reading attributes.
        /// </summary>
        /// <param name="treeNodeToRead"></param>
        /// <returns></returns>
        public int ReadAttributes(TreeNode treeNodeToRead)
        {
            ReadValueIdCollection nodesToRead;
            List<DataValue> results = null;

            ReferenceDescription refDescr = (ReferenceDescription)treeNodeToRead.Tag;
            if (refDescr == null)
            {
                return -1;
            }

            // Create a read request.
            buildAttributeList(refDescr, out nodesToRead);

            try
            {
                // Clear list view.
                this.lvAttributes.Items.Clear();

                results = m_Session.Read(
                    nodesToRead,
                    0,
                    TimestampsToReturn.Both,
                    null);

                // Show results in the listview.
                updateAttributeList(nodesToRead, results);
            }
            catch (Exception e)
            {
                // Update status label.
                OnUpdateStatusLabel("An exception occured while reading: " + e.Message, false);
                return -1;
            }

            // Update status label.
            OnUpdateStatusLabel("Read succeeded for Node \"" + refDescr.DisplayName + "\".", true);
            return 0;
        }

        /// <summary>
        /// Update the attribute listview.
        /// </summary>
        /// <param name="nodesToRead"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private void updateAttributeList(ReadValueIdCollection nodesToRead, List<DataValue> results)
        {
            if (nodesToRead.Count != results.Count)
            {
                // Error case.
                return;
            }

            try
            {
                for (int i = 0; i < nodesToRead.Count; i++)
                {
                    string attributeName = (string)nodesToRead[i].UserData;
                    string attributeValue;
                    string attributeStatus;

                    // Add the attribute name / value to the list view.
                    ListViewItem item = new ListViewItem(attributeName);
                    // Add value.
                    attributeValue = results[i].WrappedValue.ToString();
                    item.SubItems.Add(attributeValue);

                    if (nodesToRead[i].AttributeId == Attributes.Value)
                    {
                        m_CurrentWriteValue = new WriteValue();
                        m_CurrentWriteValue.AttributeId = Attributes.Value;
                        m_CurrentWriteValue.NodeId = nodesToRead[i].NodeId;
                        m_CurrentWriteNodeName = nodesToRead[i].NodeId.ToString();
                        m_CurrentValue = results[i].Value;
                    }

                    // Add status.
                    attributeStatus = results[i].StatusCode.ToString();
                    if (StatusCode.IsBad(results[i].StatusCode))
                    {
                        item.SubItems[1].Text = (String)attributeStatus;
                        item.SubItems[1].ForeColor = Color.Red;
                    }

                    // Add item to the listview.
                    this.lvAttributes.Items.Add(item);

                    // Set column width.
                    this.lvAttributes.Columns[0].Width = 150;
                    this.lvAttributes.Columns[1].Width = 250;
                }
            }
            catch (Exception e)
            {
                // Update status label.
                OnUpdateStatusLabel("Error while processing read results: " + e.Message, false);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Create read request.
        /// </summary>
        /// <param name="refDescription"></param>
        /// <param name="nodesToRead"></param>
        private void buildAttributeList(ReferenceDescription refDescription, out ReadValueIdCollection nodesToRead)
        {
            // Build list of attributes to read.
            nodesToRead = new ReadValueIdCollection();

            // Add default attributes (for all nodeclasses)
            addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.NodeId, nodesToRead);
            addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.NodeClass, nodesToRead);
            addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.BrowseName, nodesToRead);
            addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.DisplayName, nodesToRead);
            addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.Description, nodesToRead);
            addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.WriteMask, nodesToRead);
            addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.UserWriteMask, nodesToRead);

            // Add nodeclass specific attributes
            switch (refDescription.NodeClass)
            {
                case NodeClass.Object:
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.EventNotifier, nodesToRead);
                    break;
                case NodeClass.Variable:
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.Value, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.DataType, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.ValueRank, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.ArrayDimensions, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.AccessLevel, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.UserAccessLevel, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.MinimumSamplingInterval, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.Historizing, nodesToRead);
                    break;
                case NodeClass.Method:
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.Executable, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.UserExecutable, nodesToRead);
                    break;
                case NodeClass.ObjectType:
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.IsAbstract, nodesToRead);
                    break;
                case NodeClass.VariableType:
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.Value, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.DataType, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.ValueRank, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.ArrayDimensions, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.IsAbstract, nodesToRead);
                    break;
                case NodeClass.ReferenceType:
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.IsAbstract, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.Symmetric, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.InverseName, nodesToRead);
                    break;
                case NodeClass.DataType:
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.IsAbstract, nodesToRead);
                    break;
                case NodeClass.View:
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.ContainsNoLoops, nodesToRead);
                    addAttribute(refDescription.NodeId.ToNodeId(Session.NamespaceUris), Attributes.EventNotifier, nodesToRead);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Add attribute for a particular node to ReadValueCollection
        /// </summary>
        /// <param name="node"></param>
        /// <param name="attributeId"></param>
        /// <param name="nodesToRead"></param>
        private void addAttribute(NodeId node, uint attributeId, ReadValueIdCollection nodesToRead)
        {
            // Get NodeId from tree node .
            ReadValueId attributeToRead = new ReadValueId();
            attributeToRead.NodeId = node;
            attributeToRead.AttributeId = attributeId;
            attributeToRead.UserData = Attributes.GetDisplayText(attributeId);
            nodesToRead.Add(attributeToRead);
        }

        /// <summary>
        /// Display current attribute values.
        /// </summary>
        /// <param name="nodeToRead"></param>
        /// <param name="attrIds"></param>
        /// <param name="results"></param>
        /// /// <param name="response"></param>
        private void updateAttributes(
            NodeId nodeToRead,
            UInt32Collection attrIds,
            DataValueCollection results,
            ResponseHeader response)
        {
            if (attrIds.Count != results.Count)
            {
                // Error case.
                return;
            }

            try
            {
                for (int i = 0; i < attrIds.Count; i++)
                {
                    string attributeName = (string)attrIds[i].ToString();
                    string attributeValue = results[i].ToString();
                    string attributeStatus;

                    // Add the attribute name / value to the list view.
                    ListViewItem item = new ListViewItem(attributeName);

                    // Add the value
                    item.SubItems.Add(attributeValue);

                    if (attrIds[i] == Attributes.Value)
                    {
                        m_CurrentWriteValue = new WriteValue();
                        m_CurrentWriteValue.AttributeId = Attributes.Value;
                        m_CurrentWriteValue.NodeId = nodeToRead;
                        m_CurrentWriteNodeName = nodeToRead.ToString();
                        m_CurrentValue = results[i].Value;
                    }

                    // Add status.
                    attributeStatus = results[i].StatusCode.ToString();
                    if (StatusCode.IsBad(results[i].StatusCode))
                    {
                        item.SubItems[0].Text = (String)attributeStatus;
                        item.SubItems[0].ForeColor = Color.Red;
                    }

                    // Add item to listview.
                    this.lvAttributes.Items.Add(item);

                    // Fit the width of the nodeid column to the size of the header.
                    this.lvAttributes.Columns[0].Width = -2;
                }
            }
            catch (Exception e)
            {
                // Update status label.
                OnUpdateStatusLabel("Error while processing read results: " + e.Message, false);
            }
        }
        #endregion
    }
}
