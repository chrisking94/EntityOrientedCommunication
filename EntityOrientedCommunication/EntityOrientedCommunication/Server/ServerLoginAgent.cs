/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：5/16/2019 9:34:11 AM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication;
using System.Net.Sockets;
using EntityOrientedCommunication.Mail;
using EntityOrientedCommunication.Utilities;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication.Server
{
    internal class ServerLoginAgent : LoginAgent, IMailDispatcher
    {
        #region data
        #region property
        public ServerUser SUser => User as ServerUser;
        #endregion

        #region field
        private Server server;
        #endregion
        #endregion

        #region constructor
        public ServerLoginAgent(Socket socket, Server server)
        {
            this.socket = socket;
            this.server = server;
            ClientName = server.Name;
            TeleClientName = socket.RemoteEndPoint.ToString();
            logger = new Logger(TeleClientName);
            Phase = ConeectionPhase.P1Connected;

            GetControl(ThreadType.Listen).Start();

            logger.Write(LogType.PR, $"client {TeleClientName} has connected.");
        }
        #endregion

        #region interface
        void IMailDispatcher.Dispatch(EMLetter letter)
        {
            letter.SetEnvelope(GetEnvelope());
            Request(StatusCode.Letter, letter);
        }
        #endregion

        #region private
        protected override void ProcessRequest(ref EMessage msg)
        {
            if (msg.HasFlag(StatusCode.Login))
            {
                var login = msg as EMLogin;
                bool hasLoggedIn;

                hasLoggedIn = server.GetLoggedInAgents().Any(a => a.TeleClientName == login.Username);

                if (hasLoggedIn)
                {
                    msg = new EMError(msg, $"'{login.Username}' has logged in，replicated logins are forbidden.", ErrorCode.RedundantLogin);
                }
                else if (server.UserManager.Contains(login.Username))
                {
                    var opr = server.UserManager.GetOperator(login.Username, login.Password);

                    if (opr != null)
                    {
                        TeleClientName = login.Username;
                        this.logger.SetOwner(TeleClientName);  // reset owner of logger
                        User = opr;
                        Token = server.GenToken(TeleClientName);
                        msg = new EMLoggedin(login, ClientName, opr, Token);
                        msg.Status |= StatusCode.Command | StatusCode.Time | StatusCode.Push;  // sync time command
                        logger = new Logger(TeleClientName);

                        Phase = ConeectionPhase.P2LoggedIn;
                        opr.PostOffice.Activate(this);  // activate mailbox
                        opr.IsOnline = true;

                        (msg as EMLoggedin).Object = server.Now;  // set sync time
                    }
                    else
                    {
                        msg = new EMError(msg, $"incorrect username/password.", ErrorCode.IncorrectUsernameOrPassword);
                    }
                }
                else
                {
                    msg = new EMError(msg, $"user '{login.Username}' is not registered.", ErrorCode.UnregisteredUser);
                }
            }
            else if (msg.HasFlag(StatusCode.Logout))
            {
                Logout();

                msg = new EMessage(msg, StatusCode.Ok);
            }
            else if (SUser == null || !SUser.IsOnline)
            {
                msg = new EMError(msg, "please login first.");
            }
            else if (msg.HasFlag(StatusCode.Letter))
            {
                if (msg.HasFlag(StatusCode.Pull))
                {
                    try
                    {
                        var pull = msg as EMObject<ObjectPatternSet>;
                        SUser.PostOffice.Pull(pull.Object);
                        msg = new EMessage(msg, StatusCode.Ok);
                    }
                    catch (Exception ex)
                    {
                        msg = new EMError(msg, $"unable to perform patterset match：{ex.Message}");
                    }
                }
                else
                {
                    var letter = msg as EMLetter;

                    var error = server.UserManager.Deliver(letter);

                    if (error != null)  // error
                    {
                        msg = new EMError(msg, error);
                    }
                    else
                    {
                        msg = new EMessage(msg, StatusCode.Ok);
                    }
                }
            }
            else if (msg.HasFlag(StatusCode.Register))
            {
                if (msg.HasFlag(StatusCode.Entity))
                {
                    var typeFullName = (msg as EMText).Text;

                    SUser.PostOffice.Register(typeFullName);

                    msg = new EMessage(msg, StatusCode.Ok);
                }
            }
            else
            {
                msg = new EMError(msg, $"current request is not supported by this server.", ErrorCode.InvalidOperation);
            }
        }

        protected override void ProcessResponse(EMessage requestMsg, EMessage responseMsg)
        {
            // pass
        }

        protected override void ProcessTimeoutRequest(EMessage msg)
        {
            if (msg.HasFlag(StatusCode.Command))  // client refused a command
            {
                Response(new EMError(GetEnvelope(), $"the client did not response command '{msg.ID}' in time, the connection was cut off."));
                Destroy();
            }
            else if (msg.HasFlag(StatusCode.Letter))
            {
                // pass
            }
        }

        protected override void Catch(TException exp)
        {
            logger.Write(LogType.FT, exp.InnerException.StackTrace);

            if (exp.ExpceptionType == TExceptionType.MessageProcessingFailed)
            {
                var msg = exp.Tag as EMessage;
                if (msg.HasFlag(StatusCode.Request))
                {
                    Response(new EMText(exp.Tag as EMessage, $"error occurred when process message '{msg.ID}'：{exp.InnerException.Message}", StatusCode.Denied));
                }
            }
            else  // disconnect when fatal error occurs
            {
                Destroy();
            }
        }

        public override void Destroy()
        {
            Logout();

            base.Destroy();
        }

        private void Logout()
        {
            Token = null;
            server.Remove(this);
            if (SUser != null)
            {
                SUser.PostOffice.Deactivate();  // deactivate mailbox
                SUser.IsOnline = false;
            }
        }

        protected override void OnThreadListenAborted()  // listen thread on aborting
        {
            Destroy();
        }

        protected override void OnConnectionTimeout()  // connection timeout
        {
            Destroy();
        }
        #endregion
    }
}
