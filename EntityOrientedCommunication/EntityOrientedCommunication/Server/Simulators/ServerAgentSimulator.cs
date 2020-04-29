/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/24/2020 2:16:36 PM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Mail;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication.Server
{
   internal sealed class ServerAgentSimulator : IMailDispatcher  // connect client to server through memory
    {
        public ServerEOCUser SUser { get; private set; }

        public string ClientName => SUser.Name;

        private ClientAgentSimulator client;

        public ServerAgentSimulator(ClientAgentSimulator client)
        {
            this.client = client;

            this.SUser = new ServerEOCUser(client.ClientName, "system.server");
            this.SUser.IsOnline = true;  // change opeartor's status to online
            this.SUser.PostOffice.Activate(this);  // activate post office
        }

        public void Dispatch(TMLetter letter)
        {
            client.ProcessRequest(letter);  // send to client through memory
        }

        public void ProcessRequest(TMLetter letter)  // letter request only
        {
            this.SUser.Manager.Deliver(letter);
        }
    }
}
