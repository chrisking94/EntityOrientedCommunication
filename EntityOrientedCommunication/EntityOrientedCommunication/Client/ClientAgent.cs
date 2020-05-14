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
    /// implement the function of client login management, provide full duplex communication
    /// </summary>
    internal sealed class ClientAgent : LoginAgent, IClientMailDispatcher, IClientAgent
    {
        #region data
        #region property
        public event ClientAgentEventHandler ClientAgentEvent;

        public EndPoint EndPoint { get; private set; }

        public bool LoggedIn => Phase >= ConnectionPhase.P2LoggedIn;

        public DateTime Now => this.nowBlock.Value;

        public User User => this.postOffice.User as User;
        #endregion

        #region field
        private bool bOnWorking;

        private TransactionPool transPool;

        private TimeBlock nowBlock;

        private ClientPostOffice postOffice;
        #endregion
        #endregion

        #region constructor
        public ClientAgent(ClientPostOffice postOffice, string serverIpOrUrl, int port)
        {
            TeleClientName = "";
            EndPoint = new DnsEndPoint(serverIpOrUrl, port);

            this.nowBlock = new TimeBlock();

            this.postOffice = postOffice;

            this.transPool = new TransactionPool();
            this.transPool.Register(Transaction_ConnectionMonitor, 10, "ConnectionMonitor");
            this.transPool.TransactionErrorEvent += (object sender, TransactionErrorArgs args) =>
            {
                throw new Exception($"transaction [{args.transaction.Name}]: {args.exception.Message}");
            };
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
            if (Phase > ConnectionPhase.P1Connected) Phase = ConnectionPhase.P1Connected;  // re-login

            bOnWorking = true;

            if (timeout == -1)
            {
                // pass
            }
            else
            {
                while (Phase < ConnectionPhase.P2LoggedIn && bOnWorking)
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
            if (Phase >= ConnectionPhase.P2LoggedIn)
            {
                var reply = Request(StatusCode.Logout, new EMessage(GetEnvelope()));

                if (!reply.HasFlag(StatusCode.Ok))
                {
                    throw new Exception($"failed to logout, detail：{(reply as EMText).Text}");
                }

                this.SetOfflineState();
            }
        }

        public override void Destroy()
        {
            this.Logout();

            base.Destroy();
            Phase = ConnectionPhase.P0Start;
            ClientAgentEvent?.Invoke(this,
                new ClientAgentEventArgs(ClientAgentEventType.Disconnected | ClientAgentEventType.Prompt, $"{this} was destroyed."));

            transPool.Destroy();
            transPool = null;
            this.postOffice = null;
            logger.Debug("transaction pool destroyed.");
        }

        void IDisposable.Dispose()
        {
            this.Destroy();
        }
        #endregion

        #region EOC
        public event IncomingLetterEventHandler IncomingLetterEvent;

        public event ResetedEventHandler ResetedEvent;

        public event TransmissionErrorEventHandler TransmissionErrorEvent;

        EMLetter IMailDispatcher.Dispatch(EMLetter letter)  // dispatch the local letter to server
        {
            CheckLogin();

            letter.SetEnvelope(GetEnvelope());

            if (letter.HasFlag(StatusCode.Get))
            {
                var reply = Request(StatusCode.Letter, letter, letter.GetTTL(this.Now));

                if (reply.HasFlag(StatusCode.Denied))
                {
                    throw new EOCException((reply as EMText).Text);
                }

                return reply as EMLetter;
            }
            else
            {
                this.AsyncRequest(StatusCode.Letter, letter, letter.GetTTL(this.Now));

                return null;
            }
        }

        void IClientMailDispatcher.Activate(params ClientMailBox[] mailBoxes)
        {
            if (LoggedIn)
            {
                var entityNames = string.Join(",", mailBoxes.Select(mb => mb.EntityName));
                var msg = new EMText(GetEnvelope(), entityNames);
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
                        $"unable to register entity '{entityNames}', detail：{error.Text}"));
                }
            }
        }
        #endregion

        #region private
        private void Connect()
        {
            if (Phase < ConnectionPhase.P1Connected)  // not connected
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
                    Phase = ConnectionPhase.P1Connected;
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
            if (Phase < ConnectionPhase.P2LoggedIn && Phase >= ConnectionPhase.P1Connected)
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
                        this.nowBlock.Set(loggedIn.Object);
                        User.Update(loggedIn.User);
                        TeleClientName = loggedIn.ServerName;
                        Token = loggedIn.Token;
                        logger.SetOwner(TeleClientName);
                        Phase = ConnectionPhase.P2LoggedIn;
                        ClientAgentEvent?.Invoke(this,
                            new ClientAgentEventArgs(ClientAgentEventType.LoggedIn | ClientAgentEventType.Prompt, "login success!"));


                        this.ResetedEvent?.Invoke();
                    }
                    else  // denied
                    {
                        if (echo is EMError err)
                        {
                            if (err.Code == ErrorCode.IncorrectUsernameOrPassword ||
                                err.Code == ErrorCode.UnregisteredUser ||
                                err.Code == ErrorCode.PushedOut)
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

        private void SetOfflineState()
        {
            this.Phase = ConnectionPhase.P1Connected;
            this.bOnWorking = false;
        }

        protected override void ProcessRequest(ref EMessage msg)
        {
            if (msg.HasFlag(StatusCode.Letter))
            {
                var replyLetter = this.IncomingLetterEvent?.Invoke(msg as EMLetter);
                if (replyLetter == null)
                {
                    msg = new EMessage(msg, StatusCode.Ok);
                }
                else
                {
                    replyLetter.SetEnvelope(new Envelope(msg.ID));
                    msg = replyLetter;
                }
            }
            else if (msg.HasFlag(StatusCode.Push))
            {
                if (msg.HasFlag(StatusCode.Denied))  // error message
                {
                    var errorMsg = msg as EMError;

                    this.ClientAgentEvent?.Invoke(this, 
                        new ClientAgentEventArgs(ClientAgentEventType.Error, errorMsg.Text, "server notification"));

                    this.SetOfflineState();

                    msg = new EMessage(msg, StatusCode.Ok);
                }
            }
            else
            {
                msg = new EMessage(msg, StatusCode.Ok);
            }
        }

        protected override void ProcessResponse(EMessage requestMsg, EMessage responseMsg)
        {
            if (requestMsg.HasFlag(StatusCode.Letter) && responseMsg.HasFlag(StatusCode.Denied))
            {
                var errorMsg = responseMsg as EMError;
                this.TransmissionErrorEvent?.Invoke(requestMsg as EMLetter, $"E{(int)errorMsg.Code}: {errorMsg.Text}");
            }
        }

        protected override void ProcessTimeoutRequest(EMessage msg)
        {
            if (msg.HasFlag(StatusCode.Letter))
            {
                this.TransmissionErrorEvent(msg as EMLetter, "request to timeout.");
            }
        }

        protected override void ResetEnvelope()
        {
            envelope = 101;
        }

        private void CheckLogin()
        {
            if (Phase < ConnectionPhase.P2LoggedIn)
            {
                throw new InvalidOperationException("please login first!");
            }
        }

        protected override void OnThreadListenAborted()
        {
            Phase = ConnectionPhase.P0Start;  // reconnect
        }

        protected override void OnConnectionTimeout()
        {
            Phase = ConnectionPhase.P0Start;  // reconnect
            ClientAgentEvent?.Invoke(this,
                new ClientAgentEventArgs(ClientAgentEventType.Connection | ClientAgentEventType.Error, "connection timeout."));
        }
        #endregion
    }
}
