/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：5/16/2019 6:14:11 PM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication.Mail
{
    /// <summary>
    /// 收件器
    /// </summary>
    public interface IMailReceiver
    {
        /// <summary>
        /// 用于路由，为空时使用TypeFullName作为EntityName
        /// </summary>
        string EntityName { get; }

        /// <summary>
        /// 接收一封信件
        /// </summary>
        /// <param name="letter"></param>
        void Pickup(TMLetter letter);
    }
}
