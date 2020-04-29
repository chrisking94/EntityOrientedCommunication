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
        public ServerEOCUser SOperator => Operator as ServerEOCUser;
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
            Phase = OperationPhase.P1Connected;

            GetControl(ThreadType.Listen).Start();

            logger.Write(LogType.PR, $"客户端 {TeleClientName} 已连接");
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
                    msg = new TMError(msg, $"'{login.Username}' 已登录，禁止重复登录。", ErrorCode.RedundantLogin);
                }
                else if (server.UserManager.Contains(login.Username))
                {
                    var opr = server.UserManager.GetOperator(login.Username, login.Password);

                    if (opr != null)
                    {
                        TeleClientName = login.Username;
                        this.logger.SetOwner(TeleClientName);  // reset owner of logger
                        Operator = opr;
                        Token = server.GenToken(TeleClientName);
                        msg = new TMLoggedin(login, ClientName, opr, Token);
                        msg.Status |= StatusCode.Command | StatusCode.Time | StatusCode.Push;  // sync time command
                        logger = new Logger(TeleClientName);

                        Phase = OperationPhase.P2LoggedIn;
                        opr.PostOffice.Activate(this);  // activate mailbox
                        opr.IsOnline = true;

                        (msg as TMLoggedin).Object = server.Now;  // set sync time
                    }
                    else
                    {
                        msg = new TMError(msg, $"密码或用户名错误", ErrorCode.IncorrectUsernameOrPassword);
                    }
                }
                else
                {
                    msg = new TMError(msg, $"用户 {login.Username} 未注册", ErrorCode.UnregisteredUser);
                }
            }
            else if (msg.HasFlag(StatusCode.Logout))
            {
                Logout();

                msg = new TMessage(msg, StatusCode.Ok);
            }
            else if (SOperator == null || !SOperator.IsOnline)
            {
                msg = new TMError(msg, "请先登陆");
            }
            else if (msg.HasFlag(StatusCode.Letter))
            {
                if (msg.HasFlag(StatusCode.Pull))
                {
                    try
                    {
                        var pull = msg as TMObject<ObjectPatternSet>;
                        SOperator.PostOffice.Pull(pull.Object);
                        msg = new TMessage(msg, StatusCode.Ok);
                    }
                    catch (Exception ex)
                    {
                        msg = new TMError(msg, $"匹配信件时遇到错误：{ex.Message}");
                    }
                }
                else
                {
                    var letter = msg as TMLetter;
                    var bSend = true;

                    if (bSend)
                    {
                        string error = null;
                        //string cmd;
                        //CommandLine.Resolve(letter.Title, out letter.Title, out cmd);  // title # cmd

                        if (error == null)
                        {
                            error = server.UserManager.Deliver(letter);
                        }  // else error

                        if (error == null)
                        {
                            // process addition command in letter's title
                            //error = cmder.Execute(cmd, letter);  // command is not supported in this version
                        }

                        if (error != null)// error
                        {
                            msg = new TMError(msg, error);
                        }
                    }

                    if (!msg.HasFlag(StatusCode.Denied)) msg = new TMessage(msg, StatusCode.Ok);
                }
            }
            else if (msg.HasFlag(StatusCode.Register))
            {
                if (msg.HasFlag(StatusCode.Entity))
                {
                    var typeFullName = (msg as TMText).Text;

                    SOperator.PostOffice.Register(typeFullName);

                    msg = new TMessage(msg, StatusCode.Ok);
                }
            }
            else
            {
                msg = new TMError(msg, $"当前请求对 '{nameof(ServerLoginAgent)}' 无效", ErrorCode.InvalidOperation);
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
                Response(new TMError(GetEnvelope(), $"客户端执行服务器 '{msg.ID}' 号命令超时，已断开连接"));
                Destroy();
            }
            else if (msg.HasFlag(StatusCode.Letter))
            {
                //// resend when failed
                //var letter = msg as TMLetter;
                //++letter.Trials;
                //SOperator.MailBox.Push(letter);
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
                    Response(new TMText(exp.Tag as TMessage, $"处理请求 '{msg.ID}' 时遇到错误：{exp.InnerException.Message}", StatusCode.Denied));
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
            if (SOperator != null)
            {
                SOperator.PostOffice.Deactivate();  // deactivate mailbox
                SOperator.IsOnline = false;
            }
        }

        protected override void OnThreadListenAborted()  // listen报错
        {
            Destroy();
        }

        protected override void OnConnectionTimeout()  // 连接超时
        {
            Destroy();
        }
        #endregion
    }
}
