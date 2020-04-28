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
using EntityOrientedCommunication.Utilities;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Messages;
using EntityOrientedCommunication.Mail;

namespace EOCClient
{
    /// <summary>
    /// 实现登陆管理、raw数据存取，等不涉及同步协议的功能 
    /// </summary>
    public sealed class ClientLoginAgent : LoginAgent, IClientMailDispatcher
    {
        #region data
        #region property
        public ClientAgentEventHandler ClientAgentEvent;

        public EndPoint EndPoint { get; private set; }

        public bool IsLoggedIn => Phase >= OperationPhase.P2LoggedIn;

        public DnsEndPoint IPEndPoint => EndPoint as DnsEndPoint;

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
        public ClientLoginAgent(string serverIpOrUrl, int port)
        {
            this.Now = new TimeBlock();

            // login
            TeleClientName = "";
            EndPoint = new DnsEndPoint(serverIpOrUrl, port);
            InitSocket();

            this.postOffice = new ClientPostOffice(this);

            this.transPool = new TransactionPool();
            this.transPool.Register(Transaction_ConnectionMonitor, 10, "ConnectionMonitor");
        }
        #endregion

        #region interface
        /// <summary>
        /// 登陆
        /// </summary>
        /// <param name="timeout">等待超时时间，单位ms，-1代表不等待且永不超时</param>
        /// <returns></returns>
        public void Login(string username, string password, int timeout)
        {
            if (username != Operator.Name)
            {
                Logout();
            }
            if (socket != null)
            {
                lock (socket)
                {
                    if (!IsConnected)
                    {
                        InitSocket();
                    }
                }
            }

            Operator.Name = username;
            Operator.SetPassword(password);
            ClientName = username;
            if (Phase > OperationPhase.P1Connected) Phase = OperationPhase.P1Connected;  // relogin

            bOnWorking = true;

            if (timeout == -1)
            {
                // pass
            }
            else
            {
                while (Phase < OperationPhase.P2LoggedIn && bOnWorking)
                {
                    Thread.Sleep(1);
                    if (--timeout == 0)
                    {
                        bOnWorking = false;
                        throw new TimeoutException("登陆超时");
                    }
                }
            }
        }

        public void Logout()
        {
            if (Phase >= OperationPhase.P2LoggedIn)
            {
                var reply = Request(StatusCode.Login | StatusCode.Not, new TMessage(GetEnvelope()));

                if (!reply.HasFlag(StatusCode.Ok))
                {
                    throw new Exception($"注销失败，错误信息：{(reply as TMText).Text}");
                }
            }
        }

        /// <summary>
        /// 使用 'localhost' 作收件人可以把信件发到本机
        /// </summary>
        /// <param name="letter"></param>
        void IMailDispatcher.Send(TMLetter letter)
        {
            if (letter.Recipient == ClientName || letter.Recipient == "localhost")  // send to self
            {
                postOffice.Pickup(letter);
            }
            else
            {
                CheckLogin();

                letter.SetEnvelope(GetEnvelope());

                var reply = Request(StatusCode.Letter, letter);

                if (reply.HasFlag(StatusCode.Denied))
                {
                    throw new TException((reply as TMText).Text);
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

            var msg = new TMObject<ObjectPatternSet>(GetEnvelope(),
                new ObjectPatternSet(
                    new OPSinglePropertyFunction(nameof(TMLetter.Status), nameof(Enum.HasFlag), letterStatus)
                    ));

            var reply = Request(StatusCode.Pull | StatusCode.Letter, msg);

            if (reply.HasFlag(StatusCode.Denied))
            {
                throw new Exception((reply as TMText).Text);
            }
        }

        /// <summary>
        /// set server time
        /// </summary>
        /// <param name="dateTime"></param>
        public void Synchronize(DateTime dateTime)
        {
            var smg = new TMObject<DateTime>(GetEnvelope(), dateTime);
            Request(StatusCode.Update | StatusCode.Time, smg);
        }
        #endregion

        #region private
        private void InitSocket()
        {
            if (socket != null)
            {
                lock (socket)
                {
                    try
                    {
                        socket.Close();
                    }
                    catch
                    {

                    }
                }
            }
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.ExclusiveAddressUse = false;
        }

        private void Connect()
        {
            if (Phase < OperationPhase.P1Connected)  // 连接
            {
                ClientAgentEvent?.Invoke(this,
                    new ClientAgentEventArgs(ClientAgentEventType.Connecting, $"正在连接{EndPoint}..."));
                if (socket == null || socket.IsBound)
                {
                    InitSocket();
                }

                // 新建一个线程去连接
                string errorMessage = null;
                var connectThread = new Thread((x) =>
                {
                    try
                    {
                        socket.Connect(EndPoint);
                    }
                    catch (SocketException se)
                    {
                        errorMessage = se.Message;
                    }
                });
                connectThread.IsBackground = true;
                connectThread.Start();
                var tc = timeout;
                while (!socket.Connected && tc > 0 && errorMessage == null)
                {
                    Thread.Sleep(1);
                    --tc;
                    if (tc % 1000 == 0)
                    {
                        ClientAgentEvent?.Invoke(this,
                            new ClientAgentEventArgs(ClientAgentEventType.Connecting, $"正在连接{EndPoint}，{tc / 1000}s"));
                    }
                }

                // 连接成功
                if (socket.Connected)
                {
                    GetControl(ThreadType.Listen).Start();
                    Phase = OperationPhase.P1Connected;
                    ClientAgentEvent?.Invoke(this,
                        new ClientAgentEventArgs(ClientAgentEventType.Connected, $"已连接到{TeleClientName}"));
                }
                else  // 连接失败
                {
                    if (tc <= 0)
                    {
                        errorMessage = $"连接{EndPoint}超时";
                        connectThread.Abort();
                    }
                    else if (errorMessage == null)
                    {
                        errorMessage = $"未知错误，连接{EndPoint}失败";
                    }

                    ClientAgentEvent?.Invoke(this,
                            new ClientAgentEventArgs(ClientAgentEventType.Connecting | ClientAgentEventType.Error, errorMessage));
                }
            }
        }

        private void Login()
        {
            if (Phase < OperationPhase.P2LoggedIn && Phase >= OperationPhase.P1Connected)
            {
                var msg = new TMLogin(Operator);
                var tc = AsyncRequest(StatusCode.Login, msg);
                while (!tc.IsReplied && !tc.IsTimeOut)
                {
                    ClientAgentEvent?.Invoke(this,
                        new ClientAgentEventArgs(ClientAgentEventType.LogingIn, $"正在登陆, {tc.CountDown / 1000}s..."));
                    Thread.Sleep(1000);
                }
                if (tc.IsReplied)
                {
                    var echo = tc.ResponseMsg;
                    if (echo.Status.HasFlag(StatusCode.Ok))
                    {
                        var loggedIn = echo as TMLoggedin;
                        Operator.Update(loggedIn.Operator);
                        TeleClientName = loggedIn.ServerName;
                        Token = loggedIn.Token;
                        logger = new Logger(TeleClientName);
                        Phase = OperationPhase.P2LoggedIn;
                        ClientAgentEvent?.Invoke(this,
                            new ClientAgentEventArgs(ClientAgentEventType.LoggedIn, "登录成功"));

                        postOffice.ActivateAll();
                    }
                    else
                    {
                        if (echo is TMError err)
                        {
                            if (err.Code == ErrorCode.IncorrectUsernameOrPassword ||
                                err.Code == ErrorCode.UnregisteredUser ||
                                err.Code == ErrorCode.RedundantLogin)
                            {
                                bOnWorking = false;  // stop working
                            }
                        }
                        ClientAgentEvent?.Invoke(this,
                            new ClientAgentEventArgs(ClientAgentEventType.LogingIn | ClientAgentEventType.Error, (echo as TMText).Text));
                    }
                }
                else
                {
                    ClientAgentEvent?.Invoke(this,
                            new ClientAgentEventArgs(ClientAgentEventType.LogingIn | ClientAgentEventType.Error, "登陆超时"));
                }
            }
        }

        public void Online(ClientMailBox mailBox)
        {
            if (IsLoggedIn)
            {
                var msg = new TMText(GetEnvelope(), mailBox.EntityName);
                var reply = Request(StatusCode.Register | StatusCode.Receiver, msg);

                if (reply.HasFlag(StatusCode.Ok))
                {
                    // pass
                }
                else
                {
                    var error = reply as TMText;

                    ClientAgentEvent?.Invoke(this, new ClientAgentEventArgs(
                        ClientAgentEventType.Error,
                        $"无法注册接收器 '{mailBox.EntityName}'，错误信息：{error.Text}"));
                }
            }
        }

        protected void Transaction_ConnectionMonitor()
        {
            if (bOnWorking)
            {
                try
                {
                    Connect();
                    Login();
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        logger.Info(se.Message);
                    }

                    ClientAgentEvent?.Invoke(this,
                        new ClientAgentEventArgs(ClientAgentEventType.Error, se.Message));
                }
            }
        }

        protected override void ProcessRequest(ref TMessage msg)
        {
            if (msg.HasFlag(StatusCode.Letter))
            {
                postOffice.Pickup(msg as TMLetter);
            }
            else if (msg.HasFlag(StatusCode.Time | StatusCode.Update))
            {
                if (msg is ITMObject<DateTime> dt)  // sync time
                {
                    this.Now.Set(dt.Object);
                }
            }

            msg = new TMessage(msg, StatusCode.Ok);
        }

        protected override void ProcessResponse(TMessage requestMsg, TMessage responseMsg)
        {
            // pass
        }

        protected override void ProcessTimeoutRequest(TMessage msg)
        {
            // pass
        }

        protected override void ResetEnvelope()
        {
            envelope = 101;
        }

        protected void CheckLogin()
        {
            if (Phase < OperationPhase.P2LoggedIn)
            {
                throw new InvalidOperationException("请先登陆");
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            Phase = OperationPhase.P0Start;
            ClientAgentEvent?.Invoke(this,
                new ClientAgentEventArgs(ClientAgentEventType.Disconnected, $"{this}已销毁"));
            postOffice.Destroy();
            postOffice = null;

            transPool.Destroy();
            transPool = null;
            logger.Debug("transaction pool destroyed.");
        }

        protected override void OnThreadListenAborted()
        {
            Phase = OperationPhase.P0Start;  // reconnect
        }

        protected override void OnConnectionTimeout()
        {
            Phase = OperationPhase.P0Start;  // reconnect
            ClientAgentEvent?.Invoke(this,
                new ClientAgentEventArgs(ClientAgentEventType.Error, "连接超时"));
        }

        private void CheckError(TMessage msg)
        {
            if (msg.HasFlag(StatusCode.Denied))
            {
                int errorCode = -1;
                string errorInfo = "unknown error";
                if (msg is TMText text)
                {
                    errorInfo = text.Text;
                    if (msg is TMError err)
                    {
                        errorCode = (int)err.Code;
                    }
                }
                throw new Exception($"error{errorCode}: {errorInfo}");
            }  // else pass
        }
        #endregion
    }
}
