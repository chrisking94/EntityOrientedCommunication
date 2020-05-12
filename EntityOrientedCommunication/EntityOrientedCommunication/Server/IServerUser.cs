using EOCServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication.Server
{
    public interface IServerUser : IUser
    {
        bool IsOnline { get; }
    }
}
