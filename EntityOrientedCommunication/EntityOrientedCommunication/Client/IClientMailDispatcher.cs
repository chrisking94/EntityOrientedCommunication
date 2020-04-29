using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityOrientedCommunication.Mail;

namespace EntityOrientedCommunication.Client
{
    public interface IClientMailDispatcher : IMailDispatcher
    {
        void Online(ClientMailBox mailBox);
    }
}
