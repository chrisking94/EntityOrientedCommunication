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
    internal interface IMailTransceiver
    {
        /// <summary>
        /// synchronized dispatch, there might be a reply letter
        /// </summary>
        /// <param name="letter"></param>
        EMLetter Get(EMLetter letter);

        /// <summary>
        /// asynchronized dispatch
        /// </summary>
        /// <param name="letter"></param>
        void Post(EMLetter letter);
    }
}
