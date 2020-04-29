using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication;
using System.Collections;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Mail;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication.Server
{
    public partial class Server
    {
        private class ServerOperatorManager
        {
            #region data
            #region property
            public int Count => dictNameAndOperator.Count;
            #endregion

            #region field
            private Dictionary<string, ServerOperator> dictNameAndOperator;

            private Server server;
            #endregion
            #endregion

            #region constructor
            public ServerOperatorManager(Server server)
            {
                dictNameAndOperator = new Dictionary<string, ServerOperator>(16);

                this.server = server;

                // 注册服务器用户
                Register(ClientLoginAgentSimulator.Default.ServerSimulator.SOperator);

                #region test code
                {
                    // 添加200个测试用户
                    var testUCount = 200;
                    for (var i = 0; i < testUCount; ++i)
                    {
                        var uname = $"user{i}";
                        var user = new ServerOperator(uname, "test user");
                        user.NickName = $"测试用户{i}";
                        Register(user);
                    }
                    Console.WriteLine($"添加了{testUCount}个测试账户");
                }
                #endregion
            }
            #endregion

            #region interface
            public bool Contains(string name)
            {
                lock (dictNameAndOperator)
                {
                    return dictNameAndOperator.ContainsKey(name);
                }
            }

            internal ServerOperator GetOperator(string name)
            {
                ServerOperator opr = null;

                lock (dictNameAndOperator)
                {
                    dictNameAndOperator.TryGetValue(name, out opr);
                }

                return opr;
            }

            /// <summary>
            /// 失败返回null
            /// </summary>
            /// <param name="name"></param>
            /// <param name="password"></param>
            /// <returns></returns>
            public ServerOperator GetOperator(string name, string password)
            {
                ServerOperator opr = null;

                lock (dictNameAndOperator)
                {
                    if (dictNameAndOperator.TryGetValue(name, out opr))
                    {
                        if (opr.Password != password)
                        {
                            opr = null;  // password not matched
                        }
                    }
                }

                return opr;
            }

            public IEnumerable<string> GetAllOperatorNames()
            {
                lock (dictNameAndOperator)
                {
                    return dictNameAndOperator.Values.Select(s => s.Name);
                }
            }

            /// <summary>
            /// 按照Recipient把信件放到指定邮箱，有错误返回错误信息，无错误返回null
            /// </summary>
            /// <param name="letter"></param>
            /// <returns></returns>
            public string Deliver(TMLetter letter)
            {
                var allReceiverInfos = new List<MailRouteInfo>();
                var sInfo = MailRouteInfo.Parse(letter.Sender)[0];

                foreach (var rInfo in MailRouteInfo.Parse(letter.Recipient))
                {
                    if (rInfo.UserName.ToLower() == "all")  // to all,  广播邮件
                    {
                        foreach (var oprName in server.manager.GetAllOperatorNames())
                        {
                            if (oprName != sInfo.UserName)  // 不包括sender
                            {
                                allReceiverInfos.Add(new MailRouteInfo(oprName, rInfo.ReceiverEntityNames));
                            }
                        }
                    }
                    else
                    {
                        allReceiverInfos.Add(rInfo);
                    }
                }
                allReceiverInfos = MailRouteInfo.Format(allReceiverInfos);
                var notExistsUserRouteInfos = allReceiverInfos.Where(info => !this.Contains(info.UserName)).ToList();

                if (notExistsUserRouteInfos.Count > 0)
                {
                    return $"用户 '{string.Join("; ", notExistsUserRouteInfos.Select(info => info.UserName).ToArray())}' 不存在，发送邮件失败";  // operation failed
                }

                if (letter.LetterType == LetterType.Emergency)
                {  // check receiver status
                    var offlineOprRouteInfos = allReceiverInfos.Where(info => !this.GetOperator(info.UserName).IsOnline).ToList();
                    if (offlineOprRouteInfos.Count > 0)
                    {
                        return $"用户 '{string.Join(",", offlineOprRouteInfos.Select(o => o.UserName).ToArray())}' 不在线，发送紧急邮件失败";
                    }
                }

                foreach (var rInfo in allReceiverInfos)
                {
                    var recipientOpr = this.GetOperator(rInfo.UserName);
                    recipientOpr.PostOffice.Push(letter, sInfo, rInfo);
                }

                return null;  // succeeded
            }
            #endregion

            #region private
            /// <summary>
            /// 注册一个操作员到内存
            /// </summary>
            /// <param name="opr"></param>
            private void Register(ServerOperator opr)
            {
                lock (dictNameAndOperator)
                {
                    dictNameAndOperator[opr.Name] = opr;
                    opr.SetManager(this);
                }
            }
            #endregion
        }
    }
}
