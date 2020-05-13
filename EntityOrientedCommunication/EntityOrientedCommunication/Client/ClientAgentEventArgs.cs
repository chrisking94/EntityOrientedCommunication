/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：5/24/2019 10:50:35 AM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace EntityOrientedCommunication.Client
{
    [Flags]
    public enum ClientAgentEventType : ulong
    {
        Unknown         = 0x0000_0000_0000_0000,  // unknown event type

        /* operation */
        /// <summary>
        /// the connection was broken
        /// </summary>
        Disconnected    = 0x0000_0000_0000_0001 | Connection,
        /// <summary>
        /// client agent is trying to connect with server
        /// </summary>
        Connecting      = 0x0000_0000_0000_0002 | Connection,
        /// <summary>
        /// client agent and server are connected
        /// </summary>
        Connected       = Connecting | Ok,
        /// <summary>
        /// the client agent is loging in
        /// </summary>
        LoggingIn       = 0x0000_0000_0000_0004 | Connection,
        /// <summary>
        /// the client agent has logged in
        /// </summary>
        LoggedIn        = LoggingIn | Ok,

        /* describer */
        /// <summary>
        /// some error occurred when execute 'operation'
        /// </summary>
        Error           = 0x0001_0000_0000_0000,
        /// <summary>
        /// succeeded or accepted
        /// </summary>
        Ok              = 0x0002_0000_0000_0000,
        /// <summary>
        /// a higher level of error
        /// </summary>
        Fatal           = 0x0004_0000_0000_0000 | Error,
        /// <summary>
        /// prompt message for operation execution
        /// </summary>
        Prompt          = 0x0008_0000_0000_0000,  
        /// <summary>
        /// warning threw by operation
        /// </summary>
        Warning         = 0x0020_0000_0000_0000, 
        /// <summary>
        /// indicate the operation is a connection
        /// </summary>
        Connection      = 0x0040_0000_0000_0000, 
    }

    public delegate void ClientAgentEventHandler(object sender, ClientAgentEventArgs args);

    public class ClientAgentEventArgs : EventArgs
    {
        #region data
        #region property
        /// <summary>
        /// event type
        /// </summary>
        public ClientAgentEventType EventType { get; private set; }

        /// <summary>
        /// event title
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// event message
        /// </summary>
        public string Message { get; private set; }
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        internal ClientAgentEventArgs(ClientAgentEventType eventType, 
            string message = "", string title = "EOC client message")
        {
            EventType = eventType;
            Title = title;
            Message = message;
        }
        #endregion

        #region interface
        #endregion

        #region private
        #endregion
    }
}
