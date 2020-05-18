/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：8/12/2019 3:08:14 PM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Mail;
using EntityOrientedCommunication.Messages;
using EntityOrientedCommunication.Client;
using EntityOrientedCommunication.Facilities;
using System.Threading;

namespace EntityOrientedCommunication.Server
{
    /// <summary>
    /// the local client on server, which is a simulation to 'ClientAngent'
    /// </summary>
    internal sealed class ClientAgentSimulator : IClientMailTransceiver
    {
        #region data
        #region property
        public string ClientName => this.postOffice.User.Name;

        internal ServerAgentSimulator ServerSimulator => serverLoginAgentSimulator;

        public DateTime Now => nowBlock.Value;
        #endregion

        #region field
        private ServerAgentSimulator serverLoginAgentSimulator;

        private TimeBlock nowBlock;

        public event IncomingLetterEventHandler IncomingLetterEvent;

        public event ResetedEventHandler ResetedEvent;

        public event TransmissionErrorEventHandler TransmissionErrorEvent;

        private ClientPostOffice postOffice;
        #endregion
        #endregion

        #region constructor
        public ClientAgentSimulator(ClientPostOffice postOffice)
        {
            this.nowBlock = new TimeBlock();
            this.postOffice = postOffice;

            // create server login agent simulator
            this.serverLoginAgentSimulator = new ServerAgentSimulator(this);
        }
        #endregion

        #region interface
        internal void SetTime(DateTime now)
        {
            this.nowBlock.Set(now);
        }

        public void Dispose()
        {
            this.serverLoginAgentSimulator = null;
            this.postOffice = null;
        }
        #endregion

        #region private
        EMLetter IMailTransceiver.Get(EMLetter letter)  // client send, send to server through memory
        {
            // ingore timeout
            return this.serverLoginAgentSimulator.ProcessRequest(letter);
        }

        void IMailTransceiver.Post(EMLetter letter)
        {
            ThreadPool.QueueUserWorkItem(o => this.serverLoginAgentSimulator.ProcessRequest(letter));
        }


        internal EMLetter ProcessRequest(EMLetter letter)  // client receiving
        {
            return this.IncomingLetterEvent?.Invoke(letter);
        }

        internal void AsyncProcessRequest(EMLetter letter)
        {
            this.IncomingLetterEvent?.Invoke(letter);
        }

        void IClientMailTransceiver.Activate(params ClientMailBox[] mailBoxes)
        {
            foreach (var mailBox in mailBoxes)
            {
                this.ServerSimulator.User.PostOffice.Register(mailBox.EntityName);
            }
        }
        #endregion
    }
}
