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
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;

namespace RabbitMQ_SendClient
{
    public partial class PublishingIntervalDialog : Form
    {
        public PublishingIntervalDialog(Subscription subscription)
        {
            InitializeComponent();

            // get current publishing interval
            m_Subscription = subscription;
            if (m_Subscription != null)
            {
                try
                {
                    double publishingInterval = m_Subscription.PublishingInterval;
                    spinRequestedPublishingInterval.Value = (decimal)publishingInterval;
                }
                catch (Exception exception)
                {
                    txtRevisedPublishingInterval.Text = "Failed: " + exception.Message;
                    buttonApply.Enabled = false;
                }
            }
        }

        #region Fields
        /// <summary>
        /// Provides access to the subscription.
        /// </summary>
        private Subscription m_Subscription;
        #endregion

        private void buttonApply_Click(object sender, EventArgs e)
        {
            if (m_Subscription != null)
            {
                try
                {
                    m_Subscription.Modify();
                    txtRevisedPublishingInterval.Text = m_Subscription.PublishingInterval.ToString();
                }
                catch(Exception exception)
                {
                    txtRevisedPublishingInterval.Text = "Failed: " + exception.Message;
                }
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            // Close dialog.
            Close();
        }

        private void spinRequestedPublishingInterval_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                m_Subscription.PublishingInterval = (double)spinRequestedPublishingInterval.Value;
            }
            catch (Exception)
            {}
        }
    }
}
