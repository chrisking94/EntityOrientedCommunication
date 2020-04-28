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
using EOCClient;

namespace TAPAServer
{
    public partial class Server
    {
        /// <summary>
        /// 服务器本地的客户端，与ClientLoginAgent同等级
        /// </summary>
        private sealed class ClientLoginAgentSimulator :  IClientMailDispatcher
        {
            #region data
            #region property
            public ClientPostOffice PostOffice { get; private set; }

            public string ClientName { get; private set; }

            public ServerLoginAgentSimulator ServerSimulator => serverLoginAgentSimulator;
            #endregion

            #region field
            public static readonly ClientLoginAgentSimulator Default;

            private ServerLoginAgentSimulator serverLoginAgentSimulator;
            #endregion
            #endregion

            #region constructor
            static ClientLoginAgentSimulator()
            {
                Default = new ClientLoginAgentSimulator();
            }

            public ClientLoginAgentSimulator(string clientName = "server")
            {
                this.ClientName = clientName;

                // create client office
                PostOffice = new ClientPostOffice(this);

                // create server login agent simulator, and register agent
                this.serverLoginAgentSimulator = new ServerLoginAgentSimulator(this);
            }
            #endregion

            #region interface
            #endregion

            #region private
            void IMailDispatcher.Send(TMLetter letter)  // client send, send to server through memory
            {
                this.serverLoginAgentSimulator.ProcessRequest(letter);
            }

            internal void ProcessRequest(TMLetter letter)  // client receiving
            {
                this.PostOffice.Pickup(letter);
            }

            void IClientMailDispatcher.Online(ClientMailBox mailBox)
            {
                this.ServerSimulator.SOperator.PostOffice.Register(mailBox.EntityName);
            }
            #endregion
        }
    }
}
