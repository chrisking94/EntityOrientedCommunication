using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityOrientedCommunication.Mail;

namespace EntityOrientedCommunication.Client
{
    internal interface IClientMailDispatcher : IMailDispatcher
    {
        string ClientName { get; }
        
        /// <summary>
        /// activate mailboxes, notify other mailboxes that these 'mailBoxes' are online
        /// </summary>
        /// <param name="mailBoxes"></param>
        void Activate(params ClientMailBox[] mailBoxes);
    }
}
