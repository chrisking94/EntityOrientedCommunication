/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 5/6/2020 10:35:33 AM					
\********************************************************/
using EntityOrientedCommunication;
using EntityOrientedCommunication.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerDemo
{
    class ServerEntityA : IMailReceiver
    {
        public string EntityName => "entityA";

        public object Pickup(ILetter letter)
        {
            throw new NotImplementedException();
        }
    }
}
