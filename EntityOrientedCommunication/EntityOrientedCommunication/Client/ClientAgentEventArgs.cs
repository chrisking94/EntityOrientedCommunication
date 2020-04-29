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
        Default         = 0x0000_0000_0000_0000,
        /* operation */
        Disconnected    = 0x0000_0000_0000_0001 | Connection,  // 连接断开
        Connecting      = 0x0000_0000_0000_0002 | Connection,// 正在连接
        Connected       = Connecting | Ok,  // 已连接
        LogingIn        = 0x0000_0000_0000_0004 | Connection,// 正在登陆
        LoggedIn        = LogingIn | Ok,  // 登陆成功
        //Sync            = 0x0000_0000_0000_0008,  // 同步

        /* describer */
        Error           = 0x0001_0000_0000_0000,  // 发生错误
        Ok              = 0x0002_0000_0000_0000,  // 成功
        Fatal           = 0x0004_0000_0000_0000 | Error,
        Prompt          = 0x0008_0000_0000_0000,  // 提示
        Letter          = 0x0010_0000_0000_0000,  // 普通信件
        Warning        = 0x0020_0000_0000_0000,  // 进度消息
        Connection      = 0x0040_0000_0000_0000,  // 连接消息
        //Time            = 0x0080_0000_0000_0000,  // 时间
    }

    public delegate void ClientAgentEventHandler(object sender, ClientAgentEventArgs args);

    public class ClientAgentEventArgs : EventArgs
    {
        #region data
        #region property
        public ClientAgentEventType EventType { get; private set; }

        public string Title { get; private set; }

        public string Message { get; private set; }

        public string Sender { get; private set; }

        public object Attachment { get; private set; }
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        public ClientAgentEventArgs(ClientAgentEventType eventType, 
            string message = "", string title = "客户端代理消息",
            string sender = nameof(ClientLoginAgent),
            object attachment = null)
        {
            EventType = eventType;
            Message = message;
            Title = title;
            Sender = sender;
            Attachment = attachment;
        }
        #endregion

        #region interface
        #endregion

        #region private
        #endregion
    }
}
