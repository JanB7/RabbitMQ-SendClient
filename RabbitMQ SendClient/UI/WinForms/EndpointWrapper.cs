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
using System.Text;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;

namespace RabbitMQ_SendClient
{
    class EndpointWrapper
    {
        #region Construction
        public EndpointWrapper(EndpointDescription endpoint)
        {
            m_endpoint = endpoint;
        }
        #endregion

        #region Fields
        private EndpointDescription m_endpoint;
        #endregion

        #region Properties
        /// <summary>
        /// Provides the session being established with an OPC UA server.
        /// </summary>
        public EndpointDescription Endpoint
        {
            get { return m_endpoint; }
            set { m_endpoint = value; }
        }
        #endregion

        public override string ToString()
        {
            string sRet = m_endpoint.Server.ApplicationName.Text;
            sRet += " [";
            char seperator = '#';
            string[] collection = m_endpoint.SecurityPolicyUri.Split(seperator);
            sRet += collection[1];
            sRet += ", ";
            sRet += m_endpoint.SecurityMode.ToString();
            sRet += "] [";
            sRet += m_endpoint.EndpointUrl;
            sRet += "]";
            return sRet;
        }
    }
}
