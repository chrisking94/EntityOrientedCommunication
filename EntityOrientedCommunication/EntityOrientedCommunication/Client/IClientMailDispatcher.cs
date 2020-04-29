using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityOrientedCommunication.Mail;

namespace EntityOrientedCommunication.Client
{
    public interface IClientMailDispatcher : IMailDispatcher
    {
        string ClientName { get; }
        
        /// <summary>
        /// activate a mailbox
        /// </summary>
        /// <param name="mailBox"></param>
        void Activate(ClientMailBox mailBox);
    }
}
