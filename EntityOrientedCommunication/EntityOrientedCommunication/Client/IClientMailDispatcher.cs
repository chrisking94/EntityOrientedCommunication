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
        /// provide a datetime which represents the now time of communication system
        /// </summary>
        DateTime Now { get; }
        
        /// <summary>
        /// activate mailboxes, notify other mailboxes that these 'mailBoxes' are online
        /// </summary>
        /// <param name="mailBoxes"></param>
        void Activate(params ClientMailBox[] mailBoxes);
    }
}
