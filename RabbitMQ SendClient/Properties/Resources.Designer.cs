﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RabbitMQ_SendClient.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("RabbitMQ_SendClient.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot;?&gt;
        ///&lt;!--
        ///TYPELIST - DO NOT USE ANYTHING ELSE
        ///╔═════════╦═════════════════════════════════════════════════════════╦══════════════════════════╗
        ///║ bool    ║ (TRUE/FALSE)                                            ║                          ║
        ///╠═════════╬═════════════════════════════════════════════════════════╬══════════════════════════╣
        ///║ byte    ║ 0 to 255                                                ║ Unsigned 8-bit integer   ║
        ///╠═════════╬══════════════════════════ [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string defaultXML {
            get {
                return ResourceManager.GetString("defaultXML", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Not a valid address.
        ///Please enter a valid IP Address or Fully Qualified Domain Name FQDN
        ///See Help for more information..
        /// </summary>
        internal static string Invalid_Server_URL {
            get {
                return ResourceManager.GetString("Invalid_Server_URL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to JSON Standard not followed.\n Please ensure message is in the required JSON Format.\n Device Causing error on:
        ///.
        /// </summary>
        internal static string MainWindow_DataReceivedHandler_ {
            get {
                return ResourceManager.GetString("MainWindow_DataReceivedHandler_", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to JSON Message Error.
        /// </summary>
        internal static string MainWindow_DataReceivedHandler_JSON_Message_Error {
            get {
                return ResourceManager.GetString("MainWindow_DataReceivedHandler_JSON_Message_Error", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Application unable to continue due to incorrect configuration file.
        ///Please check settings file and restart the application..
        /// </summary>
        internal static string MainWindow_MainWindow_FatalError_ConfigurationFile {
            get {
                return ResourceManager.GetString("MainWindow_MainWindow_FatalError_ConfigurationFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///
        ///Do you wish to edit the settings?.
        /// </summary>
        internal static string SetupServer_btnOK_Click_YesToEdit {
            get {
                return ResourceManager.GetString("SetupServer_btnOK_Click_YesToEdit", resourceCulture);
            }
        }
    }
}
