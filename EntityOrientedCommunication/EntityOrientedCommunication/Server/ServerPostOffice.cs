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
    internal class LetterInfo
    {
        public readonly TMLetter letter;

        public MailRouteInfo sender;

        public MailRouteInfo recipient;

        public readonly DateTime timeStamp;

        public LetterInfo(TMLetter letter, MailRouteInfo sender, MailRouteInfo recipient)
        {
            this.letter = letter;
            this.sender = sender;
            this.recipient = recipient;
            timeStamp = DateTime.Now;
        }

        internal LetterInfo(LetterInfo src, MailRouteInfo newRecipInfo)
        {
            this.letter = new TMLetter(newRecipInfo.ToLiteral(), src.letter.Sender,
                        src.letter.Title, src.letter.Content, src.letter.LetterType);
            this.sender = src.sender;
            this.recipient = newRecipInfo;
            this.timeStamp = src.timeStamp;  // hold timestamp
        }
    }

    internal class ServerPostOffice
    {
        #region data
        #region property
        /// <summary>
        /// get state or activate this mailbox
        /// </summary>
        public bool IsActivated { get; private set; }
        #endregion

        #region field
        private ServerUser owner;

        private InitializedDictionary<LetterType, List<LetterInfo>> dictlLetterTypeAndInBox;

        private IMailDispatcher dispatcher;

        /// <summary>
        /// mutex on operating with field 'dispatcher'
        /// </summary>
        private Mutex dispatcherMutex;

        private List<string> registeredReceiverEntityNames = new List<string>(1);

        private ThreadControl currentPopingThread;
        #endregion
        #endregion

        #region constructor
        public ServerPostOffice(ServerUser owner)
        {
            this.owner = owner;
            this.dispatcherMutex = new Mutex();

            dictlLetterTypeAndInBox = new InitializedDictionary<LetterType, List<LetterInfo>>(
                t => new List<LetterInfo>(1), 2);
        }
        #endregion

        #region interface
        /// <summary>
        /// push the letter into this postoffice for transfering to remote computer
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

            lock (dictlLetterTypeAndInBox)
            {
                dictlLetterTypeAndInBox[info.letter.LetterType].Add(info);
            }
        }

        /// <summary>
        /// change the type of letters which satisfies the specified pattern to 'Normal' for incoming transmission
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

            dispatcherMutex.WaitOne();
            this.dispatcher = dispatcher;
            dispatcherMutex.ReleaseMutex();

            RestartPopingThread();
        }

        public void Deactivate()
        {
            dispatcherMutex.WaitOne();
            this.dispatcher = null;
            dispatcherMutex.ReleaseMutex();

            registeredReceiverEntityNames.Clear();
            DiscardRealTimeLetters();
        }

        public override string ToString()
        {
            return $"{owner}'s postoffice，{dictlLetterTypeAndInBox.Values.Sum(v => v.Count)} unread.";
        }
        #endregion

        #region private
        private void DiscardRealTimeLetters()
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

        private List<LetterInfo> Pop(LetterType letterType, List<string> receiverTypeFullNames)
        {
            var list = dictlLetterTypeAndInBox[letterType];
            var popInfos = new List<LetterInfo>();

            foreach (var letterInfo in list)
            {
                var intersection = new List<string>(letterInfo.recipient.ReceiverEntityNames.Count);
                var remain = new List<string>(letterInfo.recipient.ReceiverEntityNames.Count);

                foreach (var typeStr in letterInfo.recipient.ReceiverEntityNames)
                {
                    if (receiverTypeFullNames.Contains(typeStr))
                    {
                        intersection.Add(typeStr);
                    }
                    else
                    {
                        remain.Add(typeStr);
                    }
                }

                if (intersection.Count > 0)
                {
                    letterInfo.recipient = new MailRouteInfo(letterInfo.recipient.UserName, remain);
                    var newRecipInfo = new MailRouteInfo(letterInfo.recipient.UserName, intersection);
                    var popInfo = new LetterInfo(letterInfo, newRecipInfo);

                    popInfos.Add(popInfo);
                }
            }

            return popInfos;
        }

        private void __threadPop(object obj)  // mail poper
        {
            var control = obj as ThreadControl;

            while (IsActivated && !control.stopped)
            {
                // mail management
                List<LetterInfo> letterInfos = new List<LetterInfo>(8);

                lock (registeredReceiverEntityNames)
                {
                    letterInfos.AddRange(this.Pop(LetterType.RealTime, registeredReceiverEntityNames));
                    letterInfos.AddRange(this.Pop(LetterType.Normal, registeredReceiverEntityNames));
                    letterInfos.AddRange(this.Pop(LetterType.Emergency, registeredReceiverEntityNames));
                }
                // pop
                dispatcherMutex.WaitOne();
                foreach (var info in letterInfos.OrderBy(info => info.timeStamp))
                {
                    dispatcher.Dispatch(info.letter);
                }
                dispatcherMutex.ReleaseMutex();

                Thread.Sleep(1);
            }
        }
        #endregion
    }
}
