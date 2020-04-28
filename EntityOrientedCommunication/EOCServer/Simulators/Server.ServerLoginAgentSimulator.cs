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

namespace TAPAServer
{
    public partial class Server
    {
        private sealed class ServerLoginAgentSimulator : IMailDispatcher  // connect client to server through memory
        {
            public ServerOperator SOperator { get; private set; }

            public string ClientName => SOperator.Name;

            private ClientLoginAgentSimulator client;

            public ServerLoginAgentSimulator(ClientLoginAgentSimulator client)
            {
                this.client = client;

                this.SOperator = new ServerOperator(client.ClientName, "system.server");
                this.SOperator.IsOnline = true;  // change opeartor's status to online
                this.SOperator.PostOffice.Activate(this);  // activate post office
            }

            public void Dispatch(TMLetter letter)
            {
                client.ProcessRequest(letter);  // send to client through memory
            }

            public void ProcessRequest(TMLetter letter)  // letter request only
            {
                this.SOperator.Manager.Deliver(letter);
            }
        }
    }
}
