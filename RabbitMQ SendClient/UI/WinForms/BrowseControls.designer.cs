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

namespace RabbitMQ_SendClient
{
    partial class BrowseControls
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Accessor methods for private members
        //
        public System.Windows.Forms.TreeView BrowseTree
        {
            get { return tvBrowseTree; }
            set { tvBrowseTree = value; }
        }
        #endregion

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BrowseControls));
            this.tvBrowseTree = new System.Windows.Forms.TreeView();
            this.TreeViewIL = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            //
            // tvBrowseTree
            //
            this.tvBrowseTree.AllowDrop = true;
            this.tvBrowseTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvBrowseTree.ImageIndex = 0;
            this.tvBrowseTree.ImageList = this.TreeViewIL;
            this.tvBrowseTree.Location = new System.Drawing.Point(0, 0);
            this.tvBrowseTree.Name = "tvBrowseTree";
            this.tvBrowseTree.SelectedImageIndex = 0;
            this.tvBrowseTree.Size = new System.Drawing.Size(150, 150);
            this.tvBrowseTree.TabIndex = 0;
            this.tvBrowseTree.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.tvBrowseTree_BeforeExpand);
            this.tvBrowseTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvBrowseTree_AfterSelect);
            this.tvBrowseTree.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tvBrowseTree_MouseDown);
            this.tvBrowseTree.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tvBrowseTree_KeyPress);
            this.tvBrowseTree.ItemDrag += new System.Windows.Forms.ItemDragEventHandler(this.tvBrowseTree_ItemDrag);
            //
            // TreeViewIL
            //
            this.TreeViewIL.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("TreeViewIL.ImageStream")));
            this.TreeViewIL.TransparentColor = System.Drawing.Color.Transparent;
            this.TreeViewIL.Images.SetKeyName(0, "error");
            this.TreeViewIL.Images.SetKeyName(1, "method");
            this.TreeViewIL.Images.SetKeyName(2, "object");
            this.TreeViewIL.Images.SetKeyName(3, "objecttype");
            this.TreeViewIL.Images.SetKeyName(4, "property");
            this.TreeViewIL.Images.SetKeyName(5, "reftype");
            this.TreeViewIL.Images.SetKeyName(6, "treefolder");
            this.TreeViewIL.Images.SetKeyName(7, "type");
            this.TreeViewIL.Images.SetKeyName(8, "variable");
            this.TreeViewIL.Images.SetKeyName(9, "variabletype");
            this.TreeViewIL.Images.SetKeyName(10, "view");
            this.TreeViewIL.Images.SetKeyName(11, "datatype");
            this.TreeViewIL.Images.SetKeyName(12, "browse");
            //
            // BrowseControls
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tvBrowseTree);
            this.Name = "BrowseControls";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView tvBrowseTree;
        private System.Windows.Forms.ImageList TreeViewIL;
    }
}
