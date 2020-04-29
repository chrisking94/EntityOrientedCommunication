/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：5/16/2019 5:28:38 PM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication.Utilities;
using EntityOrientedCommunication.Mail;
using System.Threading;
using EntityOrientedCommunication.Messages;
using EntityOrientedCommunication;

namespace EntityOrientedCommunication.Server
{
    public partial class Server
    {
        private class LetterInfo
        {
            public readonly TMLetter letter;

            public MailRouteInfo sender;

            public MailRouteInfo recipient;

            public LetterInfo(TMLetter letter, MailRouteInfo sender, MailRouteInfo recipient)
            {
                this.letter = letter;
                this.sender = sender;
                this.recipient = recipient;
            }
        }

        private class ServerPostOffice
        {
            #region data
            #region property
            /// <summary>
            /// get state or activate this mailbox
            /// </summary>
            public bool IsActivated { get; private set; }
            #endregion

            #region field
            private ServerOperator owner;

            private InitializedDictionary<LetterType, List<LetterInfo>> dictlLetterTypeAndInBox;

            IMailDispatcher dispatcher;

            ServerOperatorManager operatorManager => owner.Manager;

            private List<string> registeredReceiverEntityNames = new List<string>(1);

            private ThreadControl currentPopingThread;
            #endregion
            #endregion

            #region constructor
            public ServerPostOffice(ServerOperator owner)
            {
                this.owner = owner;

                dictlLetterTypeAndInBox = new InitializedDictionary<LetterType, List<LetterInfo>>(
                    () => new List<LetterInfo>(1), 2);
            }
            #endregion

            #region interface
            /// <summary>
            /// 把信件放入发件箱，准备发往客户端
            /// </summary>
            /// <param name="info"></param>
            public virtual void Push(TMLetter letter, MailRouteInfo senderInfo, MailRouteInfo recipientInfo)
            {
                var info = new LetterInfo(letter, senderInfo, recipientInfo);

                if (info.letter.LetterType == LetterType.RealTime)
                {
                    if (!IsActivated) return;  // user is not on line, discard
                    lock (this.registeredReceiverEntityNames)
                    {
                        if (!info.recipient.ReceiverEntityNames.Intersect(registeredReceiverEntityNames).Any())  // entity is not online
                        {
                            return;  // discard
                        }
                    }
                }

                info.letter.TimeStamp = DateTime.Now;

                lock (dictlLetterTypeAndInBox)
                {
                    dictlLetterTypeAndInBox[info.letter.LetterType].Add(info);
                }
            }

            public void DiscardRealTimeLetters()
            {
                lock (dictlLetterTypeAndInBox)
                {
                    foreach (var kv in dictlLetterTypeAndInBox.ToArray())
                    {
                        var rtype = kv.Key;
                        var inBox = kv.Value;

                        inBox.RemoveAll(l => l.letter.LetterType == LetterType.RealTime);
                        if (inBox.Count == 0)
                        {
                            dictlLetterTypeAndInBox.Remove(rtype);
                        }
                    }
                }
            }

            /// <summary>
            /// 把满足所有patterns的Retard信件变成Normal类型以待发送
            /// </summary>
            /// <param name="patterns"></param>
            public void Pull(ObjectPatternSet patterns)
            {
                lock (dictlLetterTypeAndInBox)
                {
                    foreach (var info in dictlLetterTypeAndInBox.Values.SelectMany(v => v))
                    {
                        if (info.letter.LetterType == LetterType.Retard)
                        {
                            var bMatch = patterns.All(info);

                            if (bMatch) info.letter.LetterType = LetterType.Normal;  // set type to normal for sending
                        }
                    }
                }
            }

            public void Register(string entityName)
            {
                lock (registeredReceiverEntityNames)
                {
                    registeredReceiverEntityNames.Add(entityName);
                }
            }

            public void Activate(IMailDispatcher dispatcher)
            {
                this.IsActivated = true;
                this.dispatcher = dispatcher;
                RestartPopingThread();
            }

            public void Deactivate()
            {
                this.dispatcher = null;
                registeredReceiverEntityNames.Clear();
                DiscardRealTimeLetters();
            }

            public override string ToString()
            {
                return $"{owner}的邮局，{dictlLetterTypeAndInBox.Values.Sum(v => v.Count)}未读";
            }
            #endregion

            #region private
            private class ThreadControl
            {
                public bool stopped;

                public Thread thread;

                public ThreadControl(ParameterizedThreadStart paramStart)
                {
                    this.stopped = false;
                    this.thread = new Thread(paramStart);

                    this.thread.IsBackground = true;
                    this.thread.Start(this);
                }
            }

            private void RestartPopingThread()
            {
                if (currentPopingThread != null)  // stop last thread
                {
                    currentPopingThread.stopped = true;
                }

                // create new thread
                currentPopingThread = new ThreadControl(__threadPop);
            }

            /// <summary>
            /// 弹出发送给该用户的信件
            /// </summary>
            private List<TMLetter> Pop(LetterType letterType, List<string> receiverTypeFullNames)
            {
                var list = dictlLetterTypeAndInBox[letterType];
                var letters = new List<TMLetter>();

                foreach (var letterInfo in list)
                {
                    var intr = new List<string>(letterInfo.recipient.ReceiverEntityNames.Count);
                    var remain = new List<string>(letterInfo.recipient.ReceiverEntityNames.Count);

                    foreach (var typeStr in letterInfo.recipient.ReceiverEntityNames)
                    {
                        if (receiverTypeFullNames.Contains(typeStr))
                        {
                            intr.Add(typeStr);
                        }
                        else
                        {
                            remain.Add(typeStr);
                        }
                    }

                    if (intr.Count > 0)
                    {
                        letterInfo.recipient = new MailRouteInfo(letterInfo.recipient.UserName, remain);
                        var newRecipInfo = new MailRouteInfo(letterInfo.recipient.UserName, intr);
                        var letter = new TMLetter(newRecipInfo.ToLiteral(), letterInfo.sender.ToLiteral(),
                            letterInfo.letter.Title, letterInfo.letter.Content, letterInfo.letter.Status,
                            null, letterInfo.letter.LetterType);

                        letters.Add(letter);
                    }
                }

                return letters;
            }

            private void __threadPop(object obj)  // mail poper
            {
                var control = obj as ThreadControl;

                while (IsActivated && !control.stopped)
                {
                    // mail management
                    List<TMLetter> letters = new List<TMLetter>(8);

                    lock (registeredReceiverEntityNames)
                    {
                        letters.AddRange(this.Pop(LetterType.RealTime, registeredReceiverEntityNames));
                        letters.AddRange(this.Pop(LetterType.Normal, registeredReceiverEntityNames));
                        letters.AddRange(this.Pop(LetterType.Emergency, registeredReceiverEntityNames));
                    }
                    // pop
                    // TODO: dispatcher可能在循环过程中变为null，需要处理这种情况
                    foreach (var letter in letters.OrderBy(l => l.TimeStamp))
                    {
                        if (dispatcher == null)
                        {
                            // not sent, repush into office
                            this.Push(letter, MailRouteInfo.Parse(letter.Sender)[0], MailRouteInfo.Parse(letter.Recipient)[0]);
                        }
                        else
                        {
                            dispatcher.Dispatch(letter);
                        }
                    }

                    Thread.Sleep(10);
                }
            }
            #endregion
        }
    }
}
