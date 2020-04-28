using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityOrientedCommunication.Mail;

namespace EOCClient
{
    public interface IClientMailDispatcher : IMailDispatcher
    {
        void Online(ClientMailBox mailBox);
    }
}
