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
        Disconnected    = 0x0000_0000_0000_0001 | Connection,  // the connection was broken
        Connecting      = 0x0000_0000_0000_0002 | Connection,// client is trying to connect with server
        Connected       = Connecting | Ok,  // client and server are connected
        LoggingIn        = 0x0000_0000_0000_0004 | Connection,// client is loging in
        LoggedIn        = LoggingIn | Ok,  // successfully logged in 

        /* describer */
        Error           = 0x0001_0000_0000_0000,  // some error occurred when execute 'operation'
        Ok              = 0x0002_0000_0000_0000,  // operation succeeded
        Fatal           = 0x0004_0000_0000_0000 | Error,  // a higher level of error
        Prompt          = 0x0008_0000_0000_0000,  // prompt for operation execution
        Warning        = 0x0020_0000_0000_0000,  // warning threw by operation
        Connection      = 0x0040_0000_0000_0000,  // indicate the operation is a connection-type operation
    }

    public delegate void ClientAgentEventHandler(object sender, ClientAgentEventArgs args);

    public class ClientAgentEventArgs : EventArgs
    {
        #region data
        #region property
        public ClientAgentEventType EventType { get; private set; }

        public string Title { get; private set; }

        public string Message { get; private set; }
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        public ClientAgentEventArgs(ClientAgentEventType eventType, 
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
