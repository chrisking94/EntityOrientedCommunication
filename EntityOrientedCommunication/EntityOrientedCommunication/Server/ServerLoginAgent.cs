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
        void IMailDispatcher.Dispatch(TMLetter letter)
        {
            letter.SetEnvelope(GetEnvelope());
            Request(StatusCode.Letter, letter);
        }
        #endregion

        #region private
        protected override void ProcessRequest(ref TMessage msg)
        {
            if (msg.HasFlag(StatusCode.Login))
            {
                var login = msg as TMLogin;
                bool hasLoggedIn;

                hasLoggedIn = server.GetLoggedInAgents().Any(a => a.TeleClientName == login.Username);

                if (hasLoggedIn)
                {
                    msg = new TMError(msg, $"'{login.Username}' has logged in，replicated logins are forbidden.", ErrorCode.RedundantLogin);
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
                        msg = new TMLoggedin(login, ClientName, opr, Token);
                        msg.Status |= StatusCode.Command | StatusCode.Time | StatusCode.Push;  // sync time command
                        logger = new Logger(TeleClientName);

                        Phase = ConeectionPhase.P2LoggedIn;
                        opr.PostOffice.Activate(this);  // activate mailbox
                        opr.IsOnline = true;

                        (msg as TMLoggedin).Object = server.Now;  // set sync time
                    }
                    else
                    {
                        msg = new TMError(msg, $"incorrect username/password.", ErrorCode.IncorrectUsernameOrPassword);
                    }
                }
                else
                {
                    msg = new TMError(msg, $"user '{login.Username}' is not registered.", ErrorCode.UnregisteredUser);
                }
            }
            else if (msg.HasFlag(StatusCode.Logout))
            {
                Logout();

                msg = new TMessage(msg, StatusCode.Ok);
            }
            else if (SUser == null || !SUser.IsOnline)
            {
                msg = new TMError(msg, "please login first.");
            }
            else if (msg.HasFlag(StatusCode.Letter))
            {
                if (msg.HasFlag(StatusCode.Pull))
                {
                    try
                    {
                        var pull = msg as TMObject<ObjectPatternSet>;
                        SUser.PostOffice.Pull(pull.Object);
                        msg = new TMessage(msg, StatusCode.Ok);
                    }
                    catch (Exception ex)
                    {
                        msg = new TMError(msg, $"unable to perform patterset match：{ex.Message}");
                    }
                }
                else
                {
                    var letter = msg as TMLetter;

                    var error = server.UserManager.Deliver(letter);

                    if (error != null)  // error
                    {
                        msg = new TMError(msg, error);
                    }
                    else
                    {
                        msg = new TMessage(msg, StatusCode.Ok);
                    }
                }
            }
            else if (msg.HasFlag(StatusCode.Register))
            {
                if (msg.HasFlag(StatusCode.Entity))
                {
                    var typeFullName = (msg as TMText).Text;

                    SUser.PostOffice.Register(typeFullName);

                    msg = new TMessage(msg, StatusCode.Ok);
                }
            }
            else
            {
                msg = new TMError(msg, $"current request is not supported by this server.", ErrorCode.InvalidOperation);
            }
        }

        protected override void ProcessResponse(TMessage requestMsg, TMessage responseMsg)
        {
            // pass
        }

        protected override void ProcessTimeoutRequest(TMessage msg)
        {
            if (msg.HasFlag(StatusCode.Command))  // client refused a command
            {
                Response(new TMError(GetEnvelope(), $"the client did not response command '{msg.ID}' in time, the connection was cut off."));
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
                var msg = exp.Tag as TMessage;
                if (msg.HasFlag(StatusCode.Request))
                {
                    Response(new TMText(exp.Tag as TMessage, $"error occurred when process message '{msg.ID}'：{exp.InnerException.Message}", StatusCode.Denied));
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
