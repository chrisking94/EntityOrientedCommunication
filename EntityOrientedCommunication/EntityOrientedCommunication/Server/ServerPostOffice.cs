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
using EntityOrientedCommunication.Facilities;
using EntityOrientedCommunication.Mail;
using System.Threading;
using EntityOrientedCommunication.Messages;
using EntityOrientedCommunication;
using System.Text.RegularExpressions;

namespace EntityOrientedCommunication.Server
{
    internal class LetterInfo
    {
        public readonly EMLetter letter;

        public MailRouteInfo sender;

        public MailRouteInfo recipient;

        public readonly DateTime timeStamp;

        public LetterInfo(EMLetter letter, MailRouteInfo sender, MailRouteInfo recipient)
        {
            this.letter = letter;
            this.sender = sender;
            this.recipient = recipient;
            timeStamp = DateTime.Now;
        }

        internal LetterInfo(LetterInfo src, MailRouteInfo newRecipInfo)
        {
            this.letter = new EMLetter(newRecipInfo.ToLiteral(), src.letter.Sender,
                        src.letter.Title, src.letter.Content, src.letter.LetterType, src.letter.Serial);
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

        private List<LetterInfo> popList;

        private List<LetterInfo> retardList;

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
            this.popList = new List<LetterInfo>();
            this.retardList = new List<LetterInfo>();
        }
        #endregion

        #region interface
        /// <summary>
        /// push the letter into this postoffice for transfering to remote computer
        /// </summary>
        /// <param name="letter"></param>
        /// <param name="senderInfo"></param>
        /// <param name="recipientInfo"></param>
        public virtual void Push(EMLetter letter, MailRouteInfo senderInfo, MailRouteInfo recipientInfo)
        {
            var info = new LetterInfo(letter, senderInfo, recipientInfo);

            if (info.letter.LetterType == LetterType.RealTime)
            {
                if (!IsActivated) return;  // user is not on line, discard
                lock (this.registeredReceiverEntityNames)
                {
                    if (!info.recipient.ReceiverEntityNames.Intersect(registeredReceiverEntityNames).Any())  // no entity in recipients is not online
                    {
                        return;  // discard
                    }
                }
            }

            if (info.letter.LetterType == LetterType.Retard)
            {
                lock (retardList)
                {
                    retardList.Add(info);  // append to retard list
                }
            }
            else
            {
                lock (popList)  // append to pop queue
                {
                    popList.Add(info);
                }
            }
        }

        /// <summary>
        /// change the 'LetterType' of letters whose title meets the specified pattern to 'Normal' to trigger transmission
        /// </summary>
        /// <param name="letterTitlePattern"></param>
        public void Pull(string letterTitlePattern)
        {
            var regex = new Regex(letterTitlePattern);
            lock (retardList)
            {
                var copyList = new List<LetterInfo>(retardList);  // copy list
                retardList.Clear();  // reset

                foreach (var info in copyList)
                {
                    if (info.letter.LetterType == LetterType.Retard)
                    {
                        var bMatch = regex.IsMatch(info.letter.Title);

                        if (bMatch)
                        {
                            lock (popList)
                            {
                                popList.Add(info);  // put into pop queue
                            }
                        }
                        else  // none-match
                        {
                            retardList.Add(info);  // put back
                        }
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

        public bool IsEntityOnline(string entityName)
        {
            if (!IsActivated) return false;
            lock (this.registeredReceiverEntityNames)
            {
                return this.registeredReceiverEntityNames.Contains(entityName);
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

        public void Destroy()
        {
            this.owner = null;
            this.dispatcher = null;
            this.popList = null;
            this.retardList = null;
            this.currentPopingThread.stopped = true;  // stop the thread
        }

        public override string ToString()
        {
            return $"{owner}'s postoffice，{popList.Count + retardList.Count} unread.";
        }
        #endregion

        #region private
        private void DiscardRealTimeLetters()
        {
            lock (popList)
            {
                popList.RemoveAll(info => info.letter.LetterType == LetterType.RealTime);
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

        private List<LetterInfo> Pop(List<string> receiverTypeFullNames)
        {
            List<LetterInfo> list;
            lock (popList)
            {
                list = popList.ToList();  // copy
                popList.Clear();
            }
            var popedInfos = new List<LetterInfo>();

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
                    letterInfo.recipient = new MailRouteInfo(letterInfo.recipient.UserName, remain);  // change the recipient info to remain
                    var newRecipInfo = new MailRouteInfo(letterInfo.recipient.UserName, intersection);  // create a new letter info with recipient of 'intersection'
                    var popInfo = new LetterInfo(letterInfo, newRecipInfo);

                    popedInfos.Add(popInfo);
                }

                if (remain.Count > 0)  // part of letter was not poped
                {
                    lock (popList)
                    {
                        popList.Add(letterInfo);  // put residue back
                    }
                }
            }

            return popedInfos;
        }

        private void __threadPop(object obj)  // mail poper
        {
            var control = obj as ThreadControl;

            while (IsActivated && !control.stopped)
            {
                // pop
                List<LetterInfo> letterInfos;
                lock (registeredReceiverEntityNames)
                {
                    letterInfos = this.Pop(registeredReceiverEntityNames);
                }

                // dispatch
                dispatcherMutex.WaitOne();
                foreach (var info in letterInfos.OrderBy(info => info.timeStamp))
                {
                    try
                    {
                        dispatcher.Dispatch(info.letter);
                    }
                    catch
                    {
                        if (info.letter.LetterType != LetterType.RealTime)  // ignore 'RealTime' failure
                        {
                            this.Push(info.letter, info.sender, info.recipient);  // try to resend
                        }
                    }
                }
                dispatcherMutex.ReleaseMutex();

                Thread.Sleep(1);
            }
        }
        #endregion
    }
}
