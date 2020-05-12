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

namespace EntityOrientedCommunication.Server
{
    /// <summary>
    /// the local client on server, which is a simulation to 'ClientAngent'
    /// </summary>
    public sealed class ClientAgentSimulator : IClientMailDispatcher
    {
        #region data
        #region property
        public ClientPostOffice PostOffice { get; private set; }

        public string ClientName { get; private set; }

        internal ServerAgentSimulator ServerSimulator => serverLoginAgentSimulator;

        public DateTime Now => nowBlock.Value;
        #endregion

        #region field
        private ServerAgentSimulator serverLoginAgentSimulator;

        private TimeBlock nowBlock;
        #endregion
        #endregion

        #region constructor
        public ClientAgentSimulator(string clientName = "server")
        {
            this.ClientName = clientName;
            this.nowBlock = new TimeBlock();

            // create client office
            PostOffice = new ClientPostOffice(this);

            // create server login agent simulator, and register agent
            this.serverLoginAgentSimulator = new ServerAgentSimulator(this);
        }
        #endregion

        #region interface
        internal void SetTime(DateTime now)
        {
            this.nowBlock.Set(now);
        }

        internal void Destroy()
        {
            this.PostOffice.Destroy();
            this.PostOffice = null;
            this.serverLoginAgentSimulator = null;
        }
        #endregion

        #region private
        EMLetter IMailDispatcher.Dispatch(EMLetter letter)  // client send, send to server through memory
        {
            // ingore timeout
            return this.serverLoginAgentSimulator.ProcessRequest(letter);
        }

        internal EMLetter ProcessRequest(EMLetter letter)  // client receiving
        {
            return this.PostOffice.Pickup(letter);
        }

        internal void AsyncProcessRequest(EMLetter letter)
        {
            this.PostOffice.Pickup(letter);
        }

        void IClientMailDispatcher.Activate(params ClientMailBox[] mailBoxes)
        {
            foreach (var mailBox in mailBoxes)
            {
                this.ServerSimulator.SUser.PostOffice.Register(mailBox.EntityName);
            }
        }
        #endregion
    }
}
