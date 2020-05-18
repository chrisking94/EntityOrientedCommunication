using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication.Server
{
    internal interface IServerAgent
    {
        ServerUser User { get; }

        bool IsConnected { get; }

        void Destroy();

        /// <summary>
        /// push out this agent from server
        /// </summary>
        /// <param name="message"></param>
        void PushOut(string message);
    }
}
