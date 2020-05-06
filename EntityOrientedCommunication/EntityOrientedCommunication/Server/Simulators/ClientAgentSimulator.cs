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
        #endregion

        #region field
        private ServerAgentSimulator serverLoginAgentSimulator;
        #endregion
        #endregion

        #region constructor
        static ClientAgentSimulator()
        {

        }

        public ClientAgentSimulator(string clientName = "server")
        {
            this.ClientName = clientName;

            // create client office
            PostOffice = new ClientPostOffice(this);

            // create server login agent simulator, and register agent
            this.serverLoginAgentSimulator = new ServerAgentSimulator(this);
        }
        #endregion

        #region interface
        #endregion

        #region private
        void IMailDispatcher.Dispatch(EMLetter letter)  // client send, send to server through memory
        {
            this.serverLoginAgentSimulator.ProcessRequest(letter);
        }

        internal void ProcessRequest(EMLetter letter)  // client receiving
        {
            this.PostOffice.Pickup(letter);
        }

        void IClientMailDispatcher.Activate(ClientMailBox mailBox)
        {
            this.ServerSimulator.SUser.PostOffice.Register(mailBox.EntityName);
        }
        #endregion
    }
}
