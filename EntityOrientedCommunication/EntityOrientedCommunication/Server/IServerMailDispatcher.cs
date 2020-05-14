using EntityOrientedCommunication.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication.Server
{
    interface IServerMailDispatcher : IMailDispatcher
    {
        void Destroy();
    }
}
