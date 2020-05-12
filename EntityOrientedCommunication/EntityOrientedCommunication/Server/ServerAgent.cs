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
using EntityOrientedCommunication.Facilities;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication.Server
{
    internal class ServerAgent : LoginAgent, IMailDispatcher, IServerAgent
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
        public ServerAgent(Socket socket, Server server)
        {
            this.RestSocket(socket);
            this.server = server;
            ClientName = server.Name;
            TeleClientName = socket.RemoteEndPoint.ToString();
            logger = new Logger(TeleClientName);
            Phase = ConnectionPhase.P1Connected;

            GetControl(ThreadType.Listen).Start();

            logger.Write(LogType.PR, $"client {TeleClientName} has connected.");
        }
        #endregion

        #region interface
        EMLetter IMailDispatcher.Dispatch(EMLetter letter)
        {
            letter.SetEnvelope(GetEnvelope());

            if (letter.HasFlag(StatusCode.Get))  // Get
            {
                var reply = Request(StatusCode.Letter, letter, letter.GetTTL(server.Now));
                return reply as EMLetter;
            }
            else  // Post
            {
                this.AsyncRequest(StatusCode.Letter, letter, letter.GetTTL(server.Now));
                return null;
            }
        }
        #endregion

        #region private
        protected override void ProcessRequest(ref EMessage msg)
        {
            if (msg.HasFlag(StatusCode.Login))
            {
                var login = msg as EMLogin;
                bool hasLoggedIn;

                hasLoggedIn = server.MailCenter.IsOnline(login.Username);

                if (hasLoggedIn)
                {
                    msg = new EMError(msg, $"'{login.Username}' has logged in，replicated logins are forbidden.", ErrorCode.RedundantLogin);
                }
                else if (server.MailCenter.Contains(login.Username))
                {
                    var user = server.MailCenter.GetUser(login.Username, login.Password);

                    if (user != null)
                    {
                        TeleClientName = login.Username;
                        this.logger.SetOwner(TeleClientName);  // reset owner of logger
                        User = user;
                        Token = server.GenToken(TeleClientName);
                        msg = new EMLoggedin(login, ClientName, user, Token);
                        msg.Status |= StatusCode.Command;  // sync time command
                        logger = new Logger(TeleClientName);

                        Phase = ConnectionPhase.P2LoggedIn;
                        user.PostOffice.Activate(this);  // activate mailbox
                        user.IsOnline = true;

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
            else if (SUser == null || !SUser.IsOnline)
            {
                msg = new EMError(msg, "please login first.");
            }
            else if (msg.HasFlag(StatusCode.Logout))
            {
                Logout();

                msg = new EMessage(msg, StatusCode.Ok);
            }
            else if (msg.HasFlag(StatusCode.Letter))
            {
                var letter = msg as EMLetter;

                try
                {
                    var result = server.MailCenter.Deliver(letter);
                    if (result == null)
                    {
                        msg = new EMessage(msg, StatusCode.Ok);  // Post, or SafePost
                    }
                    else
                    {
                        result.SetEnvelope(new Envelope(msg.ID));
                        msg = result;
                    }
                }
                catch (Exception ex)
                {
                    msg = new EMError(msg, ex.Message);
                }
            }
            else if (msg.HasFlag(StatusCode.Register))
            {
                if (msg.HasFlag(StatusCode.Entity))
                {
                    var entityNames = (msg as EMText).Text.Split(',');

                    foreach (var name in entityNames)
                    {
                        SUser.PostOffice.Register(name);
                    }

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
                Response(new EMError(GetEnvelope(), $"client did not response command '{msg.ID}' in time, the connection was cut off."));

                GetControl(ThreadType.WatchDog).SafeAbort();
            }
            else if (msg.HasFlag(StatusCode.Letter))
            {
                // pass
            }
        }

        protected override void Catch(EOCException exp)
        {
            logger.Write(LogType.FT, exp.InnerException.StackTrace);

            if (exp.ExpceptionType == TExceptionType.MessageProcessingFailed)
            {
                var msg = exp.Tag as EMessage;
                if (msg.HasFlag(StatusCode.Request))
                {
                    Response(new EMError(exp.Tag as EMessage, $"error occurred when process message '{msg.ID}'：{exp.InnerException.Message}", ErrorCode.InvalidMessage));
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
            if (SUser != null)
            {
                SUser.PostOffice.Deactivate();  // deactivate mailbox
                SUser.IsOnline = false;
            }
        }

        protected override void OnThreadListenAborted()  // listen thread on aborting
        {
            // pass
        }

        protected override void OnConnectionTimeout()  // connection timeout
        {
            // pass
        }
        #endregion
    }
}
