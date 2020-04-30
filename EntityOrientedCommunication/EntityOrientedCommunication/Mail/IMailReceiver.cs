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
    /// mail receiver
    /// </summary>
    public interface IMailReceiver
    {
        /// <summary>
        /// use for routing, the route info is likes 'EntityName'@'UserName'
        /// </summary>
        string EntityName { get; }

        /// <summary>
        /// receive a new coming letter
        /// </summary>
        /// <param name="letter"></param>
        /// <returns>the item which will be sent to the sender after picking up, null if nothing to return</returns>
        object Pickup(TMLetter letter);
    }
}
