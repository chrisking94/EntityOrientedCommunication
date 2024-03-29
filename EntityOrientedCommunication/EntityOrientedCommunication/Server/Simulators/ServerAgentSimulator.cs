﻿/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/24/2020 2:16:36 PM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Mail;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication.Server
{
   internal sealed class ServerAgentSimulator : IServerMailTransceiver, IServerAgent  // connect client to server through memory
    {
        public ServerUser User { get; private set; }

        public string ClientName => User.Name;

        public bool IsConnected => true;  // constantly connected

        private ClientAgentSimulator client;

        public ServerAgentSimulator(ClientAgentSimulator client)
        {
            this.client = client;

            this.User = new ServerUser(client.ClientName, "system.server");
            this.User.IsOnline = true;  // change opeartor's status to online
            this.User.PostOffice.Activate(this);  // activate post office
        }

        EMLetter IMailTransceiver.Get(EMLetter letter)
        {
            // ingore timeout
            return client.ProcessRequest(letter);  // send to client through memory
        }

        void IMailTransceiver.Post(EMLetter letter)
        {
            ThreadPool.QueueUserWorkItem(o => this.client.ProcessRequest(letter));
        }

        public EMLetter ProcessRequest(EMLetter letter)  // letter request only
        {
            return this.User.MailCenter.Deliver(letter);
        }

        public void PushOut(string message)
        {
            // pass
        }

        public void Destroy()
        {
            this.client.Dispose();
            this.client = null;
            this.User = null;
        }
    }
}
