/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：5/15/2019 5:42:39 PM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using EntityOrientedCommunication.Facilities;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Messages;
using EntityOrientedCommunication.Mail;

namespace EntityOrientedCommunication.Client
{
    /// <summary>
    /// implement the function of client login management
    /// </summary>
    public sealed class ClientAgent : LoginAgent, IClientMailDispatcher
    {
        #region data
        #region property
        public ClientAgentEventHandler ClientAgentEvent;

        public EndPoint EndPoint { get; private set; }

        public bool IsLoggedIn => Phase >= ConeectionPhase.P2LoggedIn;

        public ClientPostOffice PostOffice => postOffice;

        public TimeBlock Now { get; private set; }
        #endregion

        #region field
        private bool bOnWorking;

        private ClientPostOffice postOffice;

        private TransactionPool transPool;
        #endregion
        #endregion

        #region constructor
        public ClientAgent(string serverIpOrUrl, int port)
        {
            this.Now = new TimeBlock();

            TeleClientName = "";
            EndPoint = new DnsEndPoint(serverIpOrUrl, port);

            this.postOffice = new ClientPostOffice(this);

            this.transPool = new TransactionPool();
            this.transPool.Register(Transaction_ConnectionMonitor, 10, "ConnectionMonitor");
        }
        #endregion

        #region interface
        /// <summary>
        /// login to server
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="timeout">unit: ms, login operation will never be overtime when 'timeout' == -1</param>
        public void Login(string username, string password, int timeout)
        {
            if (username != User.Name)
            {
                Logout();
            }

            User.Name = username;
            User.SetPassword(password);
            ClientName = username;
            if (Phase > ConeectionPhase.P1Connected) Phase = ConeectionPhase.P1Connected;  // re-login

            bOnWorking = true;

            if (timeout == -1)
            {
                // pass
            }
            else
            {
                while (Phase < ConeectionPhase.P2LoggedIn && bOnWorking)
                {
                    Thread.Sleep(1);
                    if (--timeout == 0)
                    {
                        bOnWorking = false;
                        throw new TimeoutException("login timeout.");
                    }
                }
            }
        }

        /// <summary>
        /// logout from server
        /// </summary>
        public void Logout()
        {
            if (Phase >= ConeectionPhase.P2LoggedIn)
            {
                var reply = Request(StatusCode.Logout, new EMessage(GetEnvelope()));

                if (!reply.HasFlag(StatusCode.Ok))
                {
                    throw new Exception($"failed to logout, detail：{(reply as EMText).Text}");
                }
            }
        }

        /// <summary>
        /// set server time
        /// </summary>
        /// <param name="dateTime"></param>
        public void Synchronize(DateTime dateTime)
        {
            var smg = new EMObject<DateTime>(GetEnvelope(), dateTime);
            Request(StatusCode.Time | StatusCode.Push, smg);
        }
        #endregion

        #region EOC
        void IMailDispatcher.Dispatch(EMLetter letter)  // dispatch the local letter to server
        {
            CheckLogin();

            letter.SetEnvelope(GetEnvelope());

            var reply = Request(StatusCode.Letter, letter);

            if (reply.HasFlag(StatusCode.Denied))
            {
                throw new TException((reply as EMText).Text);
            }
        }

        void IClientMailDispatcher.Activate(ClientMailBox mailBox)  // activate a mailbox
        {
            if (IsLoggedIn)
            {
                var msg = new EMText(GetEnvelope(), mailBox.EntityName);
                var reply = Request(StatusCode.Register | StatusCode.Entity, msg);

                if (reply.HasFlag(StatusCode.Ok))
                {
                    // pass
                }
                else
                {
                    var error = reply as EMText;

                    ClientAgentEvent?.Invoke(this, new ClientAgentEventArgs(
                        ClientAgentEventType.Error,
                        $"unable to register entity '{mailBox.EntityName}', detail：{error.Text}"));
                }
            }
        }

        /// <summary>
        /// pull retard letters with determined StatusCode
        /// </summary>
        /// <param name="letterStatus"></param>
        public void Pull(StatusCode letterStatus)
        {
            CheckLogin();

            var msg = new EMObject<ObjectPatternSet>(GetEnvelope(),
                new ObjectPatternSet(
                    new OPSinglePropertyFunction(nameof(EMLetter.Status), nameof(Enum.HasFlag), letterStatus)
                    ));

            var reply = Request(StatusCode.Pull | StatusCode.Letter, msg);

            if (reply.HasFlag(StatusCode.Denied))
            {
                throw new Exception((reply as EMText).Text);
            }
        }
        #endregion

        #region private
        private void Connect()
        {
            if (Phase < ConeectionPhase.P1Connected)  // not connected
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // create a new thread to perform socket connection
                string errorMessage = null;
                var connectionThread = new Thread((x) =>
                {
                    try
                    {
                        socket.Connect(EndPoint);
                    }
                    catch (SocketException se)
                    {
                        errorMessage = se.Message;
                        // log errors
                        logger.Error(se.Message);

                        if (se.SocketErrorCode != SocketError.ConnectionRefused)
                        {  // ignore some kind of errors
                            ClientAgentEvent?.Invoke(this,
                                new ClientAgentEventArgs(ClientAgentEventType.Error, se.Message));
                        }
                    }
                });
                connectionThread.IsBackground = true;
                connectionThread.Start();
                var tc = timeout;
                while (!socket.Connected && tc > 0 && errorMessage == null)
                {
                    Thread.Sleep(1);
                    --tc;
                    if (tc % 1000 == 0)
                    {
                        ClientAgentEvent?.Invoke(this,
                            new ClientAgentEventArgs(ClientAgentEventType.Connecting | ClientAgentEventType.Prompt, $"connecting to {EndPoint}, {tc / 1000}s."));
                    }
                }

                // succeeded
                if (socket.Connected)
                {
                    base.RestSocket(socket);
                    GetControl(ThreadType.Listen).Start();
                    Phase = ConeectionPhase.P1Connected;
                    ClientAgentEvent?.Invoke(this,
                        new ClientAgentEventArgs(ClientAgentEventType.Connected | ClientAgentEventType.Prompt, $"connected to {TeleClientName}."));
                }
                else  // failed
                {
                    if (tc <= 0)
                    {
                        errorMessage = $"connect to {EndPoint} timeout";
                        connectionThread.Abort();
                    }
                    else if (errorMessage == null)
                    {
                        errorMessage = $"unkown error, cannot connect to {EndPoint}";
                    }

                    ClientAgentEvent?.Invoke(this,
                            new ClientAgentEventArgs(ClientAgentEventType.Connecting | ClientAgentEventType.Prompt, errorMessage));
                }
            }
        }

        private void Login()
        {
            if (Phase < ConeectionPhase.P2LoggedIn && Phase >= ConeectionPhase.P1Connected)
            {
                var msg = new EMLogin(User);
                var tc = AsyncRequest(StatusCode.Login, msg);
                while (!tc.IsReplied && !tc.IsTimeOut)
                {
                    ClientAgentEvent?.Invoke(this,
                        new ClientAgentEventArgs(ClientAgentEventType.LoggingIn | ClientAgentEventType.Prompt, $"logging in, {tc.CountDown / 1000}s."));
                    Thread.Sleep(1000);
                }
                if (tc.IsReplied)
                {
                    var echo = tc.ResponseMsg;
                    if (echo.Status.HasFlag(StatusCode.Ok))
                    {
                        var loggedIn = echo as EMLoggedin;
                        User.Update(loggedIn.User);
                        TeleClientName = loggedIn.ServerName;
                        Token = loggedIn.Token;
                        logger.SetOwner(TeleClientName);
                        Phase = ConeectionPhase.P2LoggedIn;
                        ClientAgentEvent?.Invoke(this,
                            new ClientAgentEventArgs(ClientAgentEventType.LoggedIn | ClientAgentEventType.Prompt, "login success!"));

                        postOffice.ActivateAll();
                    }
                    else  // denied
                    {
                        if (echo is EMError err)
                        {
                            if (err.Code == ErrorCode.IncorrectUsernameOrPassword ||
                                err.Code == ErrorCode.UnregisteredUser ||
                                err.Code == ErrorCode.RedundantLogin)
                            {
                                bOnWorking = false;  // stop working
                            }
                        }
                        ClientAgentEvent?.Invoke(this,
                            new ClientAgentEventArgs(ClientAgentEventType.LoggingIn | ClientAgentEventType.Error, (echo as EMText).Text));
                    }
                }
                else
                {
                    ClientAgentEvent?.Invoke(this,
                            new ClientAgentEventArgs(ClientAgentEventType.LoggingIn | ClientAgentEventType.Error, "login timout."));
                }
            }
        }

        private void Transaction_ConnectionMonitor()
        {
            if (bOnWorking)
            {  // check connection & login status
                Connect();
                Login();
            }
        }

        protected override void ProcessRequest(ref EMessage msg)
        {
            if (msg.HasFlag(StatusCode.Letter))
            {
                postOffice.Pickup(msg as EMLetter);
            }
            else if (msg.HasFlag(StatusCode.Time | StatusCode.Push))
            {
                if (msg is IObject<DateTime> dt)  // sync time
                {
                    this.Now.Set(dt.Object);
                }
            }

            msg = new EMessage(msg, StatusCode.Ok);
        }

        protected override void ProcessResponse(EMessage requestMsg, EMessage responseMsg)
        {
            // pass
        }

        protected override void ProcessTimeoutRequest(EMessage msg)
        {
            // pass
        }

        protected override void ResetEnvelope()
        {
            envelope = 101;
        }

        private void CheckLogin()
        {
            if (Phase < ConeectionPhase.P2LoggedIn)
            {
                throw new InvalidOperationException("please login first!");
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            Phase = ConeectionPhase.P0Start;
            ClientAgentEvent?.Invoke(this,
                new ClientAgentEventArgs(ClientAgentEventType.Disconnected | ClientAgentEventType.Prompt, $"{this} was destroyed."));
            postOffice.Destroy();
            postOffice = null;

            transPool.Destroy();
            transPool = null;
            logger.Debug("transaction pool destroyed.");
        }

        protected override void OnThreadListenAborted()
        {
            Phase = ConeectionPhase.P0Start;  // reconnect
        }

        protected override void OnConnectionTimeout()
        {
            Phase = ConeectionPhase.P0Start;  // reconnect
            ClientAgentEvent?.Invoke(this,
                new ClientAgentEventArgs(ClientAgentEventType.Connection | ClientAgentEventType.Error, "connection timeout."));
        }
        #endregion
    }
}
