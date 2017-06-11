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
    partial class TrustCertificateDialog
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
            this.ButtonsPanel = new System.Windows.Forms.Panel();
            this.CountdownLabel = new System.Windows.Forms.Label();
            this.PermanentCheckBox = new System.Windows.Forms.CheckBox();
            this.CloseButton = new System.Windows.Forms.Button();
            this.OkButton = new System.Windows.Forms.Button();
            this.MainPanel = new System.Windows.Forms.TableLayoutPanel();
            this.WarningLabel = new System.Windows.Forms.Label();
            this.ThumbprintTextBox = new System.Windows.Forms.TextBox();
            this.ThumbprintLabel = new System.Windows.Forms.Label();
            this.HashAlgorithmLabel = new System.Windows.Forms.Label();
            this.KeySizeTextBox = new System.Windows.Forms.TextBox();
            this.KeySizeLabel = new System.Windows.Forms.Label();
            this.IssuerNameTextBox = new System.Windows.Forms.TextBox();
            this.IssuerNameLabel = new System.Windows.Forms.Label();
            this.SubjectNameTextBox = new System.Windows.Forms.TextBox();
            this.SubjectNameLabel = new System.Windows.Forms.Label();
            this.OrganizationUnitTextBox = new System.Windows.Forms.TextBox();
            this.OrganizationUnitLabel = new System.Windows.Forms.Label();
            this.OrganizationNameTextBox = new System.Windows.Forms.TextBox();
            this.OrganizationNameLabel = new System.Windows.Forms.Label();
            this.DomainNamesTextBox = new System.Windows.Forms.TextBox();
            this.DomainNamesLabel = new System.Windows.Forms.Label();
            this.ApplicationNameLabel = new System.Windows.Forms.Label();
            this.ApplicationUriLabel = new System.Windows.Forms.Label();
            this.ApplicationNameTextBox = new System.Windows.Forms.TextBox();
            this.ApplicationUriTextBox = new System.Windows.Forms.TextBox();
            this.ValidityTimePanel = new System.Windows.Forms.Panel();
            this.ValidToTextBox = new System.Windows.Forms.TextBox();
            this.ValidToLabel = new System.Windows.Forms.Label();
            this.ValidFromTextBox = new System.Windows.Forms.TextBox();
            this.ValidFromLabel = new System.Windows.Forms.Label();
            this.TimeoutTimer = new System.Windows.Forms.Timer(this.components);
            this.ButtonsPanel.SuspendLayout();
            this.MainPanel.SuspendLayout();
            this.ValidityTimePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // ButtonsPanel
            // 
            this.ButtonsPanel.Controls.Add(this.CountdownLabel);
            this.ButtonsPanel.Controls.Add(this.PermanentCheckBox);
            this.ButtonsPanel.Controls.Add(this.CloseButton);
            this.ButtonsPanel.Controls.Add(this.OkButton);
            this.ButtonsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.ButtonsPanel.Location = new System.Drawing.Point(0, 310);
            this.ButtonsPanel.Name = "ButtonsPanel";
            this.ButtonsPanel.Size = new System.Drawing.Size(601, 29);
            this.ButtonsPanel.TabIndex = 0;
            // 
            // CountdownLabel
            // 
            this.CountdownLabel.AutoSize = true;
            this.CountdownLabel.Location = new System.Drawing.Point(446, 8);
            this.CountdownLabel.Name = "CountdownLabel";
            this.CountdownLabel.Size = new System.Drawing.Size(71, 13);
            this.CountdownLabel.TabIndex = 3;
            this.CountdownLabel.Text = "Reject in {0}s";
            // 
            // PermanentCheckBox
            // 
            this.PermanentCheckBox.AutoSize = true;
            this.PermanentCheckBox.Location = new System.Drawing.Point(84, 7);
            this.PermanentCheckBox.Name = "PermanentCheckBox";
            this.PermanentCheckBox.Size = new System.Drawing.Size(155, 17);
            this.PermanentCheckBox.TabIndex = 2;
            this.PermanentCheckBox.Text = "Save Certificate in TrustList";
            this.PermanentCheckBox.UseVisualStyleBackColor = true;
            // 
            // CloseButton
            // 
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CloseButton.Location = new System.Drawing.Point(523, 3);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(75, 23);
            this.CloseButton.TabIndex = 1;
            this.CloseButton.Text = "Reject";
            this.CloseButton.UseVisualStyleBackColor = true;
            // 
            // OkButton
            // 
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.OkButton.Location = new System.Drawing.Point(3, 3);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(75, 23);
            this.OkButton.TabIndex = 0;
            this.OkButton.Text = "Trust";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // MainPanel
            // 
            this.MainPanel.AutoSize = true;
            this.MainPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.MainPanel.ColumnCount = 3;
            this.MainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.MainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.MainPanel.Controls.Add(this.WarningLabel, 0, 1);
            this.MainPanel.Controls.Add(this.ThumbprintTextBox, 1, 11);
            this.MainPanel.Controls.Add(this.ThumbprintLabel, 0, 11);
            this.MainPanel.Controls.Add(this.HashAlgorithmLabel, 0, 10);
            this.MainPanel.Controls.Add(this.KeySizeTextBox, 1, 9);
            this.MainPanel.Controls.Add(this.KeySizeLabel, 0, 9);
            this.MainPanel.Controls.Add(this.IssuerNameTextBox, 1, 8);
            this.MainPanel.Controls.Add(this.IssuerNameLabel, 0, 8);
            this.MainPanel.Controls.Add(this.SubjectNameTextBox, 1, 7);
            this.MainPanel.Controls.Add(this.SubjectNameLabel, 0, 7);
            this.MainPanel.Controls.Add(this.OrganizationUnitTextBox, 1, 4);
            this.MainPanel.Controls.Add(this.OrganizationUnitLabel, 0, 4);
            this.MainPanel.Controls.Add(this.OrganizationNameTextBox, 1, 3);
            this.MainPanel.Controls.Add(this.OrganizationNameLabel, 0, 3);
            this.MainPanel.Controls.Add(this.DomainNamesTextBox, 1, 6);
            this.MainPanel.Controls.Add(this.DomainNamesLabel, 0, 6);
            this.MainPanel.Controls.Add(this.ApplicationNameLabel, 0, 2);
            this.MainPanel.Controls.Add(this.ApplicationUriLabel, 0, 5);
            this.MainPanel.Controls.Add(this.ApplicationNameTextBox, 1, 2);
            this.MainPanel.Controls.Add(this.ApplicationUriTextBox, 1, 5);
            this.MainPanel.Controls.Add(this.ValidityTimePanel, 1, 10);
            this.MainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainPanel.Location = new System.Drawing.Point(0, 0);
            this.MainPanel.Name = "MainPanel";
            this.MainPanel.RowCount = 12;
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.MainPanel.Size = new System.Drawing.Size(601, 310);
            this.MainPanel.TabIndex = 1;
            // 
            // WarningLabel
            // 
            this.WarningLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.WarningLabel.BackColor = System.Drawing.Color.LightGoldenrodYellow;
            this.WarningLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.MainPanel.SetColumnSpan(this.WarningLabel, 3);
            this.WarningLabel.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WarningLabel.Location = new System.Drawing.Point(6, 6);
            this.WarningLabel.Margin = new System.Windows.Forms.Padding(6);
            this.WarningLabel.Name = "WarningLabel";
            this.WarningLabel.Size = new System.Drawing.Size(589, 35);
            this.WarningLabel.TabIndex = 47;
            this.WarningLabel.Text = "<instructions>";
            // 
            // ThumbprintTextBox
            // 
            this.ThumbprintTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ThumbprintTextBox.Location = new System.Drawing.Point(105, 288);
            this.ThumbprintTextBox.Name = "ThumbprintTextBox";
            this.ThumbprintTextBox.ReadOnly = true;
            this.ThumbprintTextBox.Size = new System.Drawing.Size(493, 20);
            this.ThumbprintTextBox.TabIndex = 41;
            // 
            // ThumbprintLabel
            // 
            this.ThumbprintLabel.AutoSize = true;
            this.ThumbprintLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ThumbprintLabel.Location = new System.Drawing.Point(3, 285);
            this.ThumbprintLabel.Name = "ThumbprintLabel";
            this.ThumbprintLabel.Size = new System.Drawing.Size(96, 26);
            this.ThumbprintLabel.TabIndex = 40;
            this.ThumbprintLabel.Text = "Thumbprint";
            this.ThumbprintLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // HashAlgorithmLabel
            // 
            this.HashAlgorithmLabel.AutoSize = true;
            this.HashAlgorithmLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HashAlgorithmLabel.Location = new System.Drawing.Point(3, 255);
            this.HashAlgorithmLabel.Name = "HashAlgorithmLabel";
            this.HashAlgorithmLabel.Size = new System.Drawing.Size(96, 30);
            this.HashAlgorithmLabel.TabIndex = 39;
            this.HashAlgorithmLabel.Text = "Validity Period";
            this.HashAlgorithmLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // KeySizeTextBox
            // 
            this.KeySizeTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.KeySizeTextBox.Location = new System.Drawing.Point(105, 232);
            this.KeySizeTextBox.Name = "KeySizeTextBox";
            this.KeySizeTextBox.ReadOnly = true;
            this.KeySizeTextBox.Size = new System.Drawing.Size(493, 20);
            this.KeySizeTextBox.TabIndex = 38;
            // 
            // KeySizeLabel
            // 
            this.KeySizeLabel.AutoSize = true;
            this.KeySizeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.KeySizeLabel.Location = new System.Drawing.Point(3, 229);
            this.KeySizeLabel.Name = "KeySizeLabel";
            this.KeySizeLabel.Size = new System.Drawing.Size(96, 26);
            this.KeySizeLabel.TabIndex = 37;
            this.KeySizeLabel.Text = "Key Size";
            this.KeySizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // IssuerNameTextBox
            // 
            this.IssuerNameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.IssuerNameTextBox.Location = new System.Drawing.Point(105, 206);
            this.IssuerNameTextBox.Name = "IssuerNameTextBox";
            this.IssuerNameTextBox.ReadOnly = true;
            this.IssuerNameTextBox.Size = new System.Drawing.Size(493, 20);
            this.IssuerNameTextBox.TabIndex = 36;
            // 
            // IssuerNameLabel
            // 
            this.IssuerNameLabel.AutoSize = true;
            this.IssuerNameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.IssuerNameLabel.Location = new System.Drawing.Point(3, 203);
            this.IssuerNameLabel.Name = "IssuerNameLabel";
            this.IssuerNameLabel.Size = new System.Drawing.Size(96, 26);
            this.IssuerNameLabel.TabIndex = 35;
            this.IssuerNameLabel.Text = "Issuer Name";
            this.IssuerNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SubjectNameTextBox
            // 
            this.SubjectNameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubjectNameTextBox.Location = new System.Drawing.Point(105, 180);
            this.SubjectNameTextBox.Name = "SubjectNameTextBox";
            this.SubjectNameTextBox.ReadOnly = true;
            this.SubjectNameTextBox.Size = new System.Drawing.Size(493, 20);
            this.SubjectNameTextBox.TabIndex = 34;
            // 
            // SubjectNameLabel
            // 
            this.SubjectNameLabel.AutoSize = true;
            this.SubjectNameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SubjectNameLabel.Location = new System.Drawing.Point(3, 177);
            this.SubjectNameLabel.Name = "SubjectNameLabel";
            this.SubjectNameLabel.Size = new System.Drawing.Size(96, 26);
            this.SubjectNameLabel.TabIndex = 33;
            this.SubjectNameLabel.Text = "Subject Name";
            this.SubjectNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // OrganizationUnitTextBox
            // 
            this.OrganizationUnitTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OrganizationUnitTextBox.Location = new System.Drawing.Point(105, 102);
            this.OrganizationUnitTextBox.Name = "OrganizationUnitTextBox";
            this.OrganizationUnitTextBox.ReadOnly = true;
            this.OrganizationUnitTextBox.Size = new System.Drawing.Size(493, 20);
            this.OrganizationUnitTextBox.TabIndex = 32;
            // 
            // OrganizationUnitLabel
            // 
            this.OrganizationUnitLabel.AutoSize = true;
            this.OrganizationUnitLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OrganizationUnitLabel.Location = new System.Drawing.Point(3, 99);
            this.OrganizationUnitLabel.Name = "OrganizationUnitLabel";
            this.OrganizationUnitLabel.Size = new System.Drawing.Size(96, 26);
            this.OrganizationUnitLabel.TabIndex = 31;
            this.OrganizationUnitLabel.Text = "Organizational Unit";
            this.OrganizationUnitLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // OrganizationNameTextBox
            // 
            this.OrganizationNameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OrganizationNameTextBox.Location = new System.Drawing.Point(105, 76);
            this.OrganizationNameTextBox.Name = "OrganizationNameTextBox";
            this.OrganizationNameTextBox.ReadOnly = true;
            this.OrganizationNameTextBox.Size = new System.Drawing.Size(493, 20);
            this.OrganizationNameTextBox.TabIndex = 30;
            // 
            // OrganizationNameLabel
            // 
            this.OrganizationNameLabel.AutoSize = true;
            this.OrganizationNameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OrganizationNameLabel.Location = new System.Drawing.Point(3, 73);
            this.OrganizationNameLabel.Name = "OrganizationNameLabel";
            this.OrganizationNameLabel.Size = new System.Drawing.Size(96, 26);
            this.OrganizationNameLabel.TabIndex = 29;
            this.OrganizationNameLabel.Text = "Organization";
            this.OrganizationNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // DomainNamesTextBox
            // 
            this.DomainNamesTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DomainNamesTextBox.Location = new System.Drawing.Point(105, 154);
            this.DomainNamesTextBox.Name = "DomainNamesTextBox";
            this.DomainNamesTextBox.ReadOnly = true;
            this.DomainNamesTextBox.Size = new System.Drawing.Size(493, 20);
            this.DomainNamesTextBox.TabIndex = 9;
            // 
            // DomainNamesLabel
            // 
            this.DomainNamesLabel.AutoSize = true;
            this.DomainNamesLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DomainNamesLabel.Location = new System.Drawing.Point(3, 151);
            this.DomainNamesLabel.Name = "DomainNamesLabel";
            this.DomainNamesLabel.Size = new System.Drawing.Size(96, 26);
            this.DomainNamesLabel.TabIndex = 7;
            this.DomainNamesLabel.Text = "DNS Names";
            this.DomainNamesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ApplicationNameLabel
            // 
            this.ApplicationNameLabel.AutoSize = true;
            this.ApplicationNameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ApplicationNameLabel.Location = new System.Drawing.Point(3, 47);
            this.ApplicationNameLabel.Name = "ApplicationNameLabel";
            this.ApplicationNameLabel.Size = new System.Drawing.Size(96, 26);
            this.ApplicationNameLabel.TabIndex = 0;
            this.ApplicationNameLabel.Text = "Application Name";
            this.ApplicationNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ApplicationUriLabel
            // 
            this.ApplicationUriLabel.AutoSize = true;
            this.ApplicationUriLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ApplicationUriLabel.Location = new System.Drawing.Point(3, 125);
            this.ApplicationUriLabel.Name = "ApplicationUriLabel";
            this.ApplicationUriLabel.Size = new System.Drawing.Size(96, 26);
            this.ApplicationUriLabel.TabIndex = 1;
            this.ApplicationUriLabel.Text = "Application URI";
            this.ApplicationUriLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ApplicationNameTextBox
            // 
            this.ApplicationNameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ApplicationNameTextBox.Location = new System.Drawing.Point(105, 50);
            this.ApplicationNameTextBox.Name = "ApplicationNameTextBox";
            this.ApplicationNameTextBox.ReadOnly = true;
            this.ApplicationNameTextBox.Size = new System.Drawing.Size(493, 20);
            this.ApplicationNameTextBox.TabIndex = 2;
            // 
            // ApplicationUriTextBox
            // 
            this.ApplicationUriTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ApplicationUriTextBox.Location = new System.Drawing.Point(105, 128);
            this.ApplicationUriTextBox.Name = "ApplicationUriTextBox";
            this.ApplicationUriTextBox.ReadOnly = true;
            this.ApplicationUriTextBox.Size = new System.Drawing.Size(493, 20);
            this.ApplicationUriTextBox.TabIndex = 3;
            // 
            // ValidityTimePanel
            // 
            this.ValidityTimePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ValidityTimePanel.Controls.Add(this.ValidToTextBox);
            this.ValidityTimePanel.Controls.Add(this.ValidToLabel);
            this.ValidityTimePanel.Controls.Add(this.ValidFromTextBox);
            this.ValidityTimePanel.Controls.Add(this.ValidFromLabel);
            this.ValidityTimePanel.Location = new System.Drawing.Point(105, 258);
            this.ValidityTimePanel.Name = "ValidityTimePanel";
            this.ValidityTimePanel.Padding = new System.Windows.Forms.Padding(3);
            this.ValidityTimePanel.Size = new System.Drawing.Size(493, 24);
            this.ValidityTimePanel.TabIndex = 44;
            // 
            // ValidToTextBox
            // 
            this.ValidToTextBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.ValidToTextBox.Location = new System.Drawing.Point(173, 3);
            this.ValidToTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.ValidToTextBox.Name = "ValidToTextBox";
            this.ValidToTextBox.ReadOnly = true;
            this.ValidToTextBox.Size = new System.Drawing.Size(100, 20);
            this.ValidToTextBox.TabIndex = 45;
            // 
            // ValidToLabel
            // 
            this.ValidToLabel.Dock = System.Windows.Forms.DockStyle.Left;
            this.ValidToLabel.Location = new System.Drawing.Point(138, 3);
            this.ValidToLabel.Name = "ValidToLabel";
            this.ValidToLabel.Size = new System.Drawing.Size(35, 18);
            this.ValidToLabel.TabIndex = 44;
            this.ValidToLabel.Text = "Until";
            this.ValidToLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ValidFromTextBox
            // 
            this.ValidFromTextBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.ValidFromTextBox.Location = new System.Drawing.Point(38, 3);
            this.ValidFromTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.ValidFromTextBox.Name = "ValidFromTextBox";
            this.ValidFromTextBox.ReadOnly = true;
            this.ValidFromTextBox.Size = new System.Drawing.Size(100, 20);
            this.ValidFromTextBox.TabIndex = 43;
            // 
            // ValidFromLabel
            // 
            this.ValidFromLabel.Dock = System.Windows.Forms.DockStyle.Left;
            this.ValidFromLabel.Location = new System.Drawing.Point(3, 3);
            this.ValidFromLabel.Name = "ValidFromLabel";
            this.ValidFromLabel.Size = new System.Drawing.Size(35, 18);
            this.ValidFromLabel.TabIndex = 46;
            this.ValidFromLabel.Text = "From";
            this.ValidFromLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TimeoutTimer
            // 
            this.TimeoutTimer.Interval = 1000;
            this.TimeoutTimer.Tick += new System.EventHandler(this.TimeoutTimer_Tick);
            // 
            // TrustCertificateDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(601, 339);
            this.Controls.Add(this.MainPanel);
            this.Controls.Add(this.ButtonsPanel);
            this.Name = "TrustCertificateDialog";
            this.Text = "Untrusted Certificate";
            this.ButtonsPanel.ResumeLayout(false);
            this.ButtonsPanel.PerformLayout();
            this.MainPanel.ResumeLayout(false);
            this.MainPanel.PerformLayout();
            this.ValidityTimePanel.ResumeLayout(false);
            this.ValidityTimePanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel ButtonsPanel;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.TableLayoutPanel MainPanel;
        private System.Windows.Forms.Label ApplicationNameLabel;
        private System.Windows.Forms.Label ApplicationUriLabel;
        private System.Windows.Forms.TextBox ApplicationNameTextBox;
        private System.Windows.Forms.TextBox ApplicationUriTextBox;
        private System.Windows.Forms.TextBox DomainNamesTextBox;
        private System.Windows.Forms.Label DomainNamesLabel;
        private System.Windows.Forms.TextBox OrganizationUnitTextBox;
        private System.Windows.Forms.Label OrganizationUnitLabel;
        private System.Windows.Forms.TextBox OrganizationNameTextBox;
        private System.Windows.Forms.Label OrganizationNameLabel;
        private System.Windows.Forms.TextBox SubjectNameTextBox;
        private System.Windows.Forms.Label SubjectNameLabel;
        private System.Windows.Forms.Label IssuerNameLabel;
        private System.Windows.Forms.Label ThumbprintLabel;
        private System.Windows.Forms.Label HashAlgorithmLabel;
        private System.Windows.Forms.TextBox KeySizeTextBox;
        private System.Windows.Forms.Label KeySizeLabel;
        private System.Windows.Forms.TextBox IssuerNameTextBox;
        private System.Windows.Forms.TextBox ThumbprintTextBox;
        private System.Windows.Forms.Panel ValidityTimePanel;
        private System.Windows.Forms.TextBox ValidFromTextBox;
        private System.Windows.Forms.TextBox ValidToTextBox;
        private System.Windows.Forms.Label ValidToLabel;
        private System.Windows.Forms.Label ValidFromLabel;
        private System.Windows.Forms.CheckBox PermanentCheckBox;
        private System.Windows.Forms.Label WarningLabel;
        private System.Windows.Forms.Timer TimeoutTimer;
        private System.Windows.Forms.Label CountdownLabel;
    }
}
