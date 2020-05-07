/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/23/2020 6:24:07 PM					
\********************************************************/
using EntityOrientedCommunication.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOrientedCommunication.Mail
{
    internal interface IMailDispatcher
    {
        /// <summary>
        /// it should be a synchronized dispatch
        /// </summary>
        /// <param name="letter"></param>
        void Dispatch(EMLetter letter);
    }
}
