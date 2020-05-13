using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication.Server
{
    internal interface IServerAgent
    {
        bool IsConnected { get; }

        void Destroy();
    }
}
