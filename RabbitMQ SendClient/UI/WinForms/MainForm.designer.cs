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

using System.Windows.Forms;

namespace RabbitMQ_SendClient
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this.NodeLBL = new System.Windows.Forms.Label();
            this.NodeTB = new System.Windows.Forms.TextBox();
            this.UrlCB = new System.Windows.Forms.ComboBox();
            this.EndpointsLBL = new System.Windows.Forms.Label();
            this.ConnectDisconnectBTN = new System.Windows.Forms.Button();
            this.MenubarMS = new System.Windows.Forms.MenuStrip();
            this.serverToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.connectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.disconnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.subscriptionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.publishingIntervalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.publishingEnabledToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.StatusLabelSTS = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.ToolTip = new System.Windows.Forms.ToolTip(this.components);
            this._browseControlses = new BrowseControls();// UnifiedAutomation.Sample.BrowseControls();
            this.attributeListControl = new RabbitMQ_SendClient.AttributeListControl();
            this.monitoredItems_Control = new RabbitMQ_SendClient.MonitoredItemsControl();
            this.panel1.SuspendLayout();
            this.MenubarMS.SuspendLayout();
            this.StatusLabelSTS.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoSize = true;
            this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel1.Controls.Add(this.NodeLBL);
            this.panel1.Controls.Add(this.NodeTB);
            this.panel1.Controls.Add(this.UrlCB);
            this.panel1.Controls.Add(this.EndpointsLBL);
            this.panel1.Controls.Add(this.ConnectDisconnectBTN);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(789, 30);
            this.panel1.TabIndex = 0;
            // 
            // NodeLBL
            // 
            this.NodeLBL.AutoSize = true;
            this.NodeLBL.Location = new System.Drawing.Point(12, 7);
            this.NodeLBL.Name = "NodeLBL";
            this.NodeLBL.Size = new System.Drawing.Size(73, 13);
            this.NodeLBL.TabIndex = 7;
            this.NodeLBL.Text = "Discovery Url:";
            // 
            // NodeTB
            // 
            this.NodeTB.Location = new System.Drawing.Point(91, 4);
            this.NodeTB.Name = "NodeTB";
            this.NodeTB.Size = new System.Drawing.Size(162, 20);
            this.NodeTB.TabIndex = 6;
            this.NodeTB.Text = "opc.tcp://localhost:4840";
            this.ToolTip.SetToolTip(this.NodeTB, "Url of the discovery server. Dropping down the combo box will call FindServers on" +
                    " that Url.");
            // 
            // UrlCB
            // 
            this.UrlCB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.UrlCB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.UrlCB.FormattingEnabled = true;
            this.UrlCB.Location = new System.Drawing.Point(322, 4);
            this.UrlCB.Name = "UrlCB";
            this.UrlCB.Size = new System.Drawing.Size(379, 21);
            this.UrlCB.TabIndex = 3;
            this.ToolTip.SetToolTip(this.UrlCB, "Drop down to call FindServers on the DiscoveryUrl. Shows a list of all Endpoints " +
                    "on all servers.");
            this.UrlCB.DropDown += new System.EventHandler(this.UrlCB_DropDown);
            // 
            // EndpointsLBL
            // 
            this.EndpointsLBL.AutoSize = true;
            this.EndpointsLBL.Location = new System.Drawing.Point(259, 7);
            this.EndpointsLBL.Name = "EndpointsLBL";
            this.EndpointsLBL.Size = new System.Drawing.Size(57, 13);
            this.EndpointsLBL.TabIndex = 2;
            this.EndpointsLBL.Text = "Endpoints:";
            // 
            // ConnectDisconnectBTN
            // 
            this.ConnectDisconnectBTN.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ConnectDisconnectBTN.Location = new System.Drawing.Point(707, 4);
            this.ConnectDisconnectBTN.Name = "ConnectDisconnectBTN";
            this.ConnectDisconnectBTN.Size = new System.Drawing.Size(74, 23);
            this.ConnectDisconnectBTN.TabIndex = 0;
            this.ConnectDisconnectBTN.Text = "Connect";
            this.ToolTip.SetToolTip(this.ConnectDisconnectBTN, "Connect or Disconnect");
            this.ConnectDisconnectBTN.UseVisualStyleBackColor = true;
            this.ConnectDisconnectBTN.Click += new System.EventHandler(this.connectDisconnectTriggered);
            // 
            // MenubarMS
            // 
            this.MenubarMS.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.serverToolStripMenuItem,
            this.subscriptionToolStripMenuItem});
            this.MenubarMS.Location = new System.Drawing.Point(0, 0);
            this.MenubarMS.Name = "MenubarMS";
            this.MenubarMS.Size = new System.Drawing.Size(789, 24);
            this.MenubarMS.TabIndex = 2;
            this.MenubarMS.Text = "MenuBar";
            // 
            // serverToolStripMenuItem
            // 
            this.serverToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.connectToolStripMenuItem,
            this.disconnectToolStripMenuItem});
            this.serverToolStripMenuItem.Name = "serverToolStripMenuItem";
            this.serverToolStripMenuItem.Size = new System.Drawing.Size(51, 20);
            this.serverToolStripMenuItem.Text = "Server";
            // 
            // connectToolStripMenuItem
            // 
            this.connectToolStripMenuItem.Name = "connectToolStripMenuItem";
            this.connectToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.connectToolStripMenuItem.Text = "Connect";
            this.connectToolStripMenuItem.Click += new System.EventHandler(this.connectDisconnectTriggered);
            // 
            // disconnectToolStripMenuItem
            // 
            this.disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
            this.disconnectToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.disconnectToolStripMenuItem.Text = "Disconnect";
            this.disconnectToolStripMenuItem.Click += new System.EventHandler(this.connectDisconnectTriggered);
            // 
            // subscriptionToolStripMenuItem
            // 
            this.subscriptionToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.publishingIntervalToolStripMenuItem,
            this.publishingEnabledToolStripMenuItem});
            this.subscriptionToolStripMenuItem.Name = "subscriptionToolStripMenuItem";
            this.subscriptionToolStripMenuItem.Size = new System.Drawing.Size(85, 20);
            this.subscriptionToolStripMenuItem.Text = "Subscription";
            // 
            // publishingIntervalToolStripMenuItem
            // 
            this.publishingIntervalToolStripMenuItem.Name = "publishingIntervalToolStripMenuItem";
            this.publishingIntervalToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.publishingIntervalToolStripMenuItem.Text = "Publishing Interval";
            this.publishingIntervalToolStripMenuItem.Click += new System.EventHandler(this.PublishingInterval_Click);
            // 
            // publishingEnabledToolStripMenuItem
            // 
            this.publishingEnabledToolStripMenuItem.Checked = true;
            this.publishingEnabledToolStripMenuItem.CheckOnClick = true;
            this.publishingEnabledToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.publishingEnabledToolStripMenuItem.Name = "publishingEnabledToolStripMenuItem";
            this.publishingEnabledToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.publishingEnabledToolStripMenuItem.Text = "Publishing Enabled";
            this.publishingEnabledToolStripMenuItem.Click += new System.EventHandler(this.publishingEnabled_Click);
            // 
            // StatusLabelSTS
            // 
            this.StatusLabelSTS.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.StatusLabelSTS.Location = new System.Drawing.Point(0, 469);
            this.StatusLabelSTS.Name = "StatusLabelSTS";
            this.StatusLabelSTS.ShowItemToolTips = true;
            this.StatusLabelSTS.Size = new System.Drawing.Size(789, 22);
            this.StatusLabelSTS.TabIndex = 3;
            this.StatusLabelSTS.Text = "StatusLabelSTS";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Image = RabbitMQ_SendClient.Properties.Resources.success;
            this.toolStripStatusLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.toolStripStatusLabel.Size = new System.Drawing.Size(774, 17);
            this.toolStripStatusLabel.Spring = true;
            this.toolStripStatusLabel.Text = "enter URL and click connect";
            this.toolStripStatusLabel.ToolTipText = "status information";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(new BrowseControls()); //BrowseControls
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(new AttributeListControl()); //AttributeListControl
            this.splitContainer1.Size = new System.Drawing.Size(789, 204);
            this.splitContainer1.SplitterDistance = 303;
            this.splitContainer1.TabIndex = 5;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 54);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.splitContainer1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(new MonitoredItemsControl()); //MonitoredItemsControl
            this.splitContainer2.Size = new System.Drawing.Size(789, 415);
            this.splitContainer2.SplitterDistance = 204;
            this.splitContainer2.TabIndex = 6;
            // 
            // _browseControlses
            // 
            this._browseControlses.AutoSize = true;
            this._browseControlses.Dock = System.Windows.Forms.DockStyle.Fill;
            this._browseControlses.Location = new System.Drawing.Point(0, 0);
            this._browseControlses.Name = "_browseControlses";
            this._browseControlses.RebrowseOnNodeExpande = false;
            this._browseControlses.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._browseControlses.Session = null;
            this._browseControlses.Size = new System.Drawing.Size(303, 204);
            this._browseControlses.TabIndex = 0;
            // 
            // attributeListControl
            // 
            this.attributeListControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.attributeListControl.Location = new System.Drawing.Point(0, 0);
            this.attributeListControl.Name = "attributeListControl";
            this.attributeListControl.Session = null;
            this.attributeListControl.Size = new System.Drawing.Size(482, 204);
            this.attributeListControl.TabIndex = 1;
            // 
            // monitoredItems_Control
            // 
            this.monitoredItems_Control.Dock = System.Windows.Forms.DockStyle.Fill;
            this.monitoredItems_Control.Location = new System.Drawing.Point(0, 0);
            this.monitoredItems_Control.Name = "monitoredItems_Control";
            this.monitoredItems_Control.PublishingEnabled = true;
            this.monitoredItems_Control.PublishingInterval = 500;
            this.monitoredItems_Control.Session = null;
            this.monitoredItems_Control.Size = new System.Drawing.Size(789, 207);
            this.monitoredItems_Control.TabIndex = 4;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(789, 491);
            this.Controls.Add(this.splitContainer2);
            this.Controls.Add(this.StatusLabelSTS);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.MenubarMS);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.MenubarMS;
            this.Name = "MainForm";
            this.Text = "Unified Automation Sample Client Full";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.MenubarMS.ResumeLayout(false);
            this.MenubarMS.PerformLayout();
            this.StatusLabelSTS.ResumeLayout(false);
            this.StatusLabelSTS.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.MenuStrip MenubarMS;
        private System.Windows.Forms.StatusStrip StatusLabelSTS;
        //private BrowseControls _browseControlses;
        private BrowseControls _browseControlses;
        private AttributeListControl attributeListControl;
        private MonitoredItemsControl monitoredItems_Control;
        private System.Windows.Forms.ComboBox UrlCB;
        private System.Windows.Forms.Label EndpointsLBL;
        private System.Windows.Forms.Button ConnectDisconnectBTN;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ToolStripMenuItem serverToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem subscriptionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem connectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disconnectToolStripMenuItem;
        private System.Windows.Forms.Label NodeLBL;
        private System.Windows.Forms.TextBox NodeTB;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.ToolStripMenuItem publishingIntervalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem publishingEnabledToolStripMenuItem;
        private System.Windows.Forms.ToolTip ToolTip;

    }
}
