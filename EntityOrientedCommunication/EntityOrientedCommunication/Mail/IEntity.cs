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
using EntityOrientedCommunication.Mail;

namespace EntityOrientedCommunication
{
    /// <summary>
    /// mail receiver
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// used for routing, this property's value is significative only when registering the entity
        /// </summary>
        string EntityName { get; }

        /// <summary>
        /// receive a new coming letter
        /// </summary>
        /// <param name="letter"></param>
        /// <returns>the content which will be sent to the sender after picking up, 'null' if nothing to return</returns>
        LetterContent Pickup(ILetter letter);
    }
}
