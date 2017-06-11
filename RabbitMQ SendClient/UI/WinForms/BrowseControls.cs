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
    public partial class BrowseControls : UserControl
    {
        /// <summary>
        /// Event handler for the event that the selection of the OPC server in the browse tree changed.
        /// </summary>
        public delegate void SelectionChangedEventHandler(TreeNode selectedNode);
        /// <summary>
        /// Use the delegate as event.
        /// </summary>
        public event SelectionChangedEventHandler SelectionChanged = null;
        /// <summary>
        /// The selection of the OPC server in the browse tree changed.
        /// </summary>
        public void OnSelectionChanged(TreeNode node)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(node);
            }
        }

        /// <summary>
        /// Event handler for the event that a node was activated by pressing enter.
        /// </summary>
        public delegate void NodeActivatedEventHandler(NodeId activatedNode);
        /// <summary>
        /// Use the delegate as event.
        /// </summary>
        public event NodeActivatedEventHandler NodeActivated = null;
        /// <summary>
        /// The selected node was activated.
        /// </summary>
        public void OnNodeActivated(NodeId activatedNode)
        {
            if (NodeActivated != null)
            {
                NodeActivated(activatedNode);
            }
        }

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
        public BrowseControls()
        {
            InitializeComponent();
        }
        #endregion

        #region Fields
        /// <summary>
        /// Provides access to OPC UA server.
        /// </summary>
        private Session m_Session;
        /// <summary>
        /// Indicates a rebrowse procedure.
        /// </summary>
        private bool m_RebrowseOnExpandNode = false;
        /// <summary>
        /// Keeps the current tree node.
        /// </summary>
        private TreeNode m_CurrentNode;
        #endregion

        #region Properties
        // Flag to set rebrowse if node is expanded.
        public bool RebrowseOnNodeExpande
        {
            get { return m_RebrowseOnExpandNode; }
            set { m_RebrowseOnExpandNode = value; }
        }
        // Server
        public Session Session
        {
            get { return m_Session; }
            set { m_Session = value; }
        }
        #endregion

        #region Calls to ClientAPI
        /// <summary>
        /// Call the Browse service of an UA server.
        /// </summary>
        /// <param name="parentNode">The node of the treeview to browse.</param>
        public int Browse(TreeNode parentNode)
        {
            NodeId nodeToBrowse = null;
            TreeNodeCollection parentCollection = null;
            int ret = 0;

            // Check if we browse from root
            if (parentNode == null)
            {
                nodeToBrowse = new NodeId(Objects.RootFolder, 0);
                // Get all the tree nodes that are assigned to the control
                parentCollection = tvBrowseTree.Nodes;
            }
            else
            {
                // Get the data about the parent tree node
                ReferenceDescription parentRefDescription = (ReferenceDescription)parentNode.Tag;

                if (parentRefDescription == null)
                {
                    return -1;
                }

                // XXX ToDo
                // set nodeid
                nodeToBrowse = ExpandedNodeId.ToNodeId(parentRefDescription.NodeId, Session.NamespaceUris);
                parentCollection = parentNode.Nodes;
            }

            bool bBrowse;

            // Set wait cursor.
            Cursor.Current = Cursors.WaitCursor;

            // Check if we want to browse on the server.
            if (m_RebrowseOnExpandNode)
            {
                bBrowse = true;
            }
            else if (parentNode == null)
            {
                bBrowse = true;
            }
            else if (parentNode.Checked)
            {
                bBrowse = false;
            }
            else
            {
                bBrowse = true;
            }

            // Delete children if required.
            if (bBrowse)
            {
                if (parentNode == null)
                {
                    BrowseTree.BeginUpdate();
                    BrowseTree.Nodes.Clear();
                    BrowseTree.EndUpdate();
                }
                else
                {
                    BrowseTree.BeginUpdate();
                    parentNode.Nodes.Clear();
                    BrowseTree.EndUpdate();
                }

                byte[] continuationPoint;
                List<ReferenceDescription> results;

                BrowseContext browseContext = new BrowseContext()
                {
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = UnifiedAutomation.UaBase.ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = 0,
                    ResultMask = (uint)BrowseResultMask.All,
                    MaxReferencesToReturn = 100
                };

                try
                {
                    // Call browse service.
                    results = m_Session.Browse(
                        nodeToBrowse,
                        browseContext,
                        out continuationPoint);

                    // Add children.
                    tvBrowseTree.BeginUpdate();

                    // Mark parent node as browsed.
                    if (parentNode != null)
                    {
                        parentNode.Checked = true;
                    }

                    AddResultsToTree(parentCollection, results);

                    while (continuationPoint != null)
                    {
                        results = m_Session.BrowseNext(ref continuationPoint);
                        AddResultsToTree(parentCollection, results);
                    }

                    // Sort the tree nodes of the particular node collection
                    this.SortTreeNode((parentNode == null) ? tvBrowseTree.Nodes : parentNode.Nodes);
                    tvBrowseTree.EndUpdate();

                    // Update status label.
                    OnUpdateStatusLabel("Browse succeeded.", true);
                }
                catch (Exception e)
                {
                    ret = -1;
                    // Update status label.
                    OnUpdateStatusLabel("An exception occured while browsing: " + e.Message, false);
                }
            }
            // Restore default cursor.
            Cursor.Current = Cursors.Default;
            return ret;
        }

        private void AddResultsToTree(TreeNodeCollection parentCollection, List<ReferenceDescription> results)
        {
            foreach (ReferenceDescription refDescr in results)
            {
                TreeNode node = new TreeNode();
                if (refDescr.DisplayName.Text == null)
                {
                    node.Text = "null";
                }
                else
                {
                    node.Text = refDescr.DisplayName.Text;
                }

                node.Tag = refDescr;
                CustomizeTreeNode(ref node);

                // Add dummy child.
                TreeNode dummy = new TreeNode("dummy");
                node.Nodes.Add(dummy);
                parentCollection.Add(node);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Set icon for tree node dependent on the type.
        /// </summary>
        /// <param name="node">The current node of the treeview.</param>
        private void CustomizeTreeNode(ref TreeNode node)
        {
            // Get the data about the tree node.
            ReferenceDescription refDescr = (ReferenceDescription)node.Tag;

            if (refDescr == null)
            {
                // Error case.
                return;
            }

            // Check for folder.
            if (ExpandedNodeId.ToNodeId(refDescr.TypeDefinition, Session.NamespaceUris) == new NodeId(ObjectTypes.FolderType))
            {
                node.ImageKey = "treefolder";
            }
            // Check for property.
            else if (ExpandedNodeId.ToNodeId(refDescr.ReferenceTypeId, Session.NamespaceUris) == new NodeId(ReferenceTypes.HasProperty))
            {
                node.ImageKey = "property";
            }
            // Check node class.
            else
            {
                switch (refDescr.NodeClass)
                {
                    case NodeClass.Object:
                        node.ImageKey = "object";
                        break;
                    case NodeClass.Variable:
                        node.ImageKey = "variable";
                        break;
                    case NodeClass.Method:
                        node.ImageKey = "method";
                        break;
                    case NodeClass.ObjectType:
                        node.ImageKey = "objecttype";
                        break;
                    case NodeClass.VariableType:
                        node.ImageKey = "variabletype";
                        break;
                    case NodeClass.ReferenceType:
                        node.ImageKey = "reftype";
                        break;
                    case NodeClass.DataType:
                        node.ImageKey = "datatype";
                        break;
                    case NodeClass.View:
                        node.ImageKey = "view";
                        break;
                    default:
                        node.ImageKey = "error";
                        break;
                }
            }

            // Set the key of the image.
            node.SelectedImageKey = node.ImageKey;
            node.StateImageKey = node.ImageKey;
        }

        /// <summary>
        /// Sorts all tree nodes in a tree node collection.
        /// </summary>
        /// <param name="nodes">Collection of TreeNodes to be sorted</param>
        private void SortTreeNode(TreeNodeCollection nodes)
        {
            if (nodes.Count > 1)
            {
                TreeNode[] arrNodes = new TreeNode[nodes.Count];

                nodes.CopyTo(arrNodes, 0);
                // Sort the nodes being copied according to (1) node class and (2) text property.
                Array.Sort<TreeNode>(arrNodes, this.CompareTreeNodes);
                // Add the nodes to the collection.
                nodes.Clear();
                nodes.AddRange(arrNodes);
            }
        }

        /// <summary>
        /// Compare method for TreeNode sorting.
        /// 1st. characteristic: object class (defined by index of image in ImageList).
        /// 2nd. characteristic: Text property of the tree node.
        /// </summary>
        /// <param name="nodeA">tree node to be compared</param>
        /// <param name="nodeB">tree node to be compared</param>
        /// <returns></returns>
        private int CompareTreeNodes(TreeNode nodeA, TreeNode nodeB)
        {
            int imageIndexA = this.tvBrowseTree.ImageList.Images.IndexOfKey(nodeA.ImageKey);
            int imageIndexB = this.tvBrowseTree.ImageList.Images.IndexOfKey(nodeB.ImageKey);
            int compClass = Decimal.Compare(imageIndexA, imageIndexB);

            return (compClass == 0) ? String.Compare(nodeA.Text, nodeB.Text, true) : compClass;
        }
        #endregion

        #region User Actions and Event Handling

        /// <summary>
        /// Event fired before the browse tree will be expanded.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void tvBrowseTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            // Get current node.
            TreeNode node = e.Node;

            if (node != null)
            {
                // Browse next level.
                Browse(node);
            }
        }

        /// <summary>
        /// Event fired after a node has been selected.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void tvBrowseTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // Get current node.
            TreeNode node = e.Node;
            // Fire SelectionChanged event.
            OnSelectionChanged(node);
        }

        /// <summary>
        /// Event being fired when an item is dragged over the tree view control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void tvBrowseTree_ItemDrag(object sender, System.Windows.Forms.ItemDragEventArgs e)
        {
            if (((TreeNode)e.Item).Tag.GetType() == typeof(ReferenceDescription))
            {
                // Get the data about the tree node.
                ReferenceDescription reference = (ReferenceDescription)((TreeNode)e.Item).Tag;

                // Allow only variables drag and drop.
                if (reference.NodeId.IsAbsolute || reference.NodeClass != NodeClass.Variable)
                {
                    return;
                }

                // Start the drag and drop action.
                // We have to copy serializable data like strings.
                String sNodeId = reference.NodeId.ToString();

                // The data is copied to the target control.
                DragDropEffects dde = tvBrowseTree.DoDragDrop(sNodeId, DragDropEffects.Copy);
            }
        }

        /// <summary>
        /// Event being fired when an item is being clicked.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void tvBrowseTree_MouseDown(object sender, MouseEventArgs e)
        {
            // Context menu(s):
            if (e.Button == MouseButtons.Right)
            {
                // Get item at events position.
                TreeNode node = tvBrowseTree.GetNodeAt(e.X, e.Y);

                // Select this node in the tree view.
                tvBrowseTree.SelectedNode = node;

                // Check if node is valid.
                if (node != null)
                {
                    // Check if node is a method.
                    ReferenceDescription refDescr = node.Tag as ReferenceDescription;

                    if (refDescr != null)
                    {
                        if (refDescr.NodeClass == NodeClass.Variable)
                        {
                            m_CurrentNode = node;
                        }
                        else if (refDescr.NodeClass == NodeClass.Object)
                        {
                            m_CurrentNode = node;
                        }
                        else
                        {
                            m_CurrentNode = node;
                        }
                    }
                }
            }
        }
        #endregion

        private void tvBrowseTree_KeyPress(object sender, KeyPressEventArgs e)
        {
            // check for enter key
            if (e.KeyChar != (char)Keys.Enter)
            {
                return;
            }

            // currently selected node
            if (tvBrowseTree.SelectedNode == null)
            {
                return;
            }

            TreeNode selectedNode = tvBrowseTree.SelectedNode;

            if (selectedNode.Tag.GetType() == typeof(ReferenceDescription))
            {
                // Get the data about the tree node.
                ReferenceDescription reference = (ReferenceDescription)(selectedNode.Tag);

                // Allow only variables
                if (reference.NodeId.IsAbsolute || reference.NodeClass != NodeClass.Variable)
                {
                    return;
                }

                // trigger event
                OnNodeActivated(ExpandedNodeId.ToNodeId(reference.NodeId, Session.NamespaceUris));
            }
        }
    }
}
