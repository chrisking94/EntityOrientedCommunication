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
    public interface IMailDispatcher
    {
        void Dispatch(TMLetter letter);
    }
}
