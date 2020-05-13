/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/24/2020 9:24:11 AM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Mail;
using EntityOrientedCommunication.Server;
using EOCServer;

namespace EntityOrientedCommunication.Client
{
    public enum PostOfficeEventType
    {
        Prompt,
        Error,
    }

    public class PostOfficeEventArgs : EventArgs
    {
        public readonly PostOfficeEventType type;

        public readonly string message;

        public PostOfficeEventArgs(PostOfficeEventType type, string message)
        {
            this.type = type;
            this.message = message;
        }
    }

    public delegate void PostOfficeEventHandler(object sender, PostOfficeEventArgs args);

    public class ClientPostOffice
    {
        #region data
        public PostOfficeEventHandler PostOfficeEvent;

        #region property
        public IUser User { get; }

        public DateTime Now => this.dispatcher.Now;
        #endregion

        #region field
        private Dictionary<string, ClientMailBox> dictName2MailBox;

        private ReaderWriterLockSlim rwlsDictName2MailBox = new ReaderWriterLockSlim();

        private IClientMailDispatcher dispatcher;
        #endregion
        #endregion

        #region constructor
        public ClientPostOffice(string username = "localhost")
        {
            dictName2MailBox = new Dictionary<string, ClientMailBox>(1);
            this.User = new User(username);
        }
        #endregion

        #region interface
        /// <summary>
        /// connect to server at sepecified IP and port
        /// </summary>
        /// <param name="serverIpOrUrl"></param>
        /// <param name="port"></param>
        /// <returns>client agent handle</returns>
        public IClientAgent Connect(string serverIpOrUrl, int port)
        {
            var agent = new ClientAgent(this, serverIpOrUrl, port);

            this.Connect(agent);  // set agent as dispatcher of this PostOffice

            return agent;
        }

        internal ClientAgentSimulator ConnectSimulator()
        {
            var simulator = new ClientAgentSimulator(this);

            this.Connect(simulator);

            return simulator;
        }

        internal IClientMailDispatcher Connect(IClientMailDispatcher dispatcher)
        {
            if (this.dispatcher != null)
            {
                // dispose the old dispatcher
                this.dispatcher.Dispose();  
                // unregiseter event listenings on old dispatcher
                this.Listen(this.dispatcher, false);
            }
            this.dispatcher = dispatcher;
            // register event listenings on new dispatcher
            this.Listen(this.dispatcher, true);

            return dispatcher;
        }

        /// <summary>
        /// get current dispatcher, and cast it to specifield type 'T'
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetDispatcher<T>() where T : class
        {
            return this.dispatcher as T;
        }

        /// <summary>
        /// get a mailbox by entity name
        /// </summary>
        /// <param name="entityName"></param>
        /// <returns></returns>
        public ClientMailBox this[string entityName]
        {
            get
            {
                rwlsDictName2MailBox.EnterReadLock();
                var mailBox = this.dictName2MailBox[entityName];
                rwlsDictName2MailBox.ExitReadLock();

                return mailBox;
            }
        }

        /// <summary>
        /// register a 'ClientMailBox' box for 'receiver', one entity name can only be registered once
        /// <para>if the imminent receiver has same name with the old receiver in this office, then the old receiver will be destroyed and replaced</para>
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        public ClientMailBox Register(IEntity receiver)
        {
            if (receiver.EntityName == null)
            {
                throw new ArgumentNullException($"the {nameof(receiver)}.{nameof(receiver.EntityName)} should not be null.");
            }

            ClientMailBox mailBox;

            rwlsDictName2MailBox.EnterUpgradeableReadLock();  // read lock
            if (dictName2MailBox.TryGetValue(receiver.EntityName, out mailBox))
            {
                PostOfficeEvent?.Invoke(this,
                        new PostOfficeEventArgs(PostOfficeEventType.Prompt, $"there's already a mailbox named '{mailBox.EntityName}' registered, which will be destroyed automatically."));
                mailBox.Destroy();  // destroy the old mailbox
            }

            mailBox = new ClientMailBox(receiver, this);
            rwlsDictName2MailBox.EnterWriteLock();  // enter write lock
            dictName2MailBox[mailBox.EntityName] = mailBox;
            rwlsDictName2MailBox.ExitWriteLock();  // exit write lock

            rwlsDictName2MailBox.ExitUpgradeableReadLock();  // exit read lock

            dispatcher.Activate(mailBox);

            return mailBox;
        }

        /// <summary>
        /// determine whether the entityName has been registered
        /// </summary>
        /// <param name="entityName"></param>
        /// <returns></returns>
        public bool IsRegistered(string entityName)
        {
            rwlsDictName2MailBox.EnterReadLock();
            var bRegistered = dictName2MailBox.ContainsKey(entityName);
            rwlsDictName2MailBox.ExitReadLock();

            return bRegistered;
        }

        /// <summary>
        /// provide a send interface for every mailbox in this postoffice
        /// </summary>
        /// <param name="letter"></param>
        internal EMLetter Send(EMLetter letter)
        {
            var routeInfos = MailRouteInfo.Parse(letter.Recipient);
            if (routeInfos == null)
            {
                throw new Exception($"cannot deliver letter '{letter.Title}', the '{nameof(letter.Recipient)}' of which is not in a valid format.");
            }

            var teleRouteInfos = new List<MailRouteInfo>(routeInfos.Count);
            MailRouteInfo localRouteInfo = null;
            foreach (var info in routeInfos)  // find letters sent to local host
            {
                if (info.UserName == "localhost")
                {
                    localRouteInfo = info;  // only one
                }
                else
                {
                    teleRouteInfos.Add(info);
                }
            }

            if (teleRouteInfos.Count != routeInfos.Count)  // the route information has been changed
            {
                letter.Recipient = MailRouteInfo.ToLiteral(teleRouteInfos);  // new tele-recipient info
            }

            letter.UpdateDDL(this.Now);

            if (letter.Recipient != "")
            {
                // send to tele-entity
                return this.dispatcher.Dispatch(letter);
            }

            if (localRouteInfo != null)
            {
                // send to local-entity
                var copy = new EMLetter(letter);
                copy.Recipient = localRouteInfo.ToLiteral();
                return this.Dispatch(copy);
            }

            return null;
        }

        public void Destroy()
        {
            rwlsDictName2MailBox.EnterWriteLock();
            foreach (var mailBox in dictName2MailBox.Values)
            {
                mailBox.Destroy();
            }
            dictName2MailBox.Clear();
            rwlsDictName2MailBox.ExitWriteLock();
            if (this.dispatcher != null)
            {
                this.Listen(this.dispatcher, false);
                this.dispatcher.Dispose();
                this.dispatcher = null;
            }
        }
        #endregion

        #region private
        /// <summary>
        /// pickup a letter sent from remote postoffice
        /// </summary>
        /// <param name="letter"></param>
        /// <returns>replies emit by the mailboxes in this postoffice</returns>
        private EMLetter OnIncomingLetter(EMLetter letter)
        {
            ClientMailBox mailBox;
            var replies = new List<EMLetter>(1);
            var routeInfo = MailRouteInfo.Parse(letter.Recipient)[0];

            foreach (var entityName in routeInfo.EntityNames)
            {
                rwlsDictName2MailBox.EnterReadLock();
                if (dictName2MailBox.TryGetValue(entityName, out mailBox))
                {
                    // pass
                }
                else  // report an error
                {
                    PostOfficeEvent?.Invoke(this,
                        new PostOfficeEventArgs(PostOfficeEventType.Error,
                        $"unable to pickup letter: {this.User.Name}'s postoffice has registered a receiver named '{entityName}', but the corresponding instance if not found."));
                }
                rwlsDictName2MailBox.ExitReadLock();

                // dispatch letter to target mailbox
                var reply = mailBox?.Receive(letter);
                if (reply != null)
                {
                    replies.Add(reply);
                }
            }

            return replies.Count == 0 ? null : replies[0];  // one reply for 'Get'
        }

        /// <summary>
        /// re-activate all mailboxes
        /// </summary>
        private void OnDispatcherReseted()
        {
            rwlsDictName2MailBox.EnterReadLock();
            if (dictName2MailBox.Count > 0)
            {
                this.dispatcher.Activate(dictName2MailBox.Values.ToArray());
            }
            rwlsDictName2MailBox.ExitReadLock();
        }

        private void OnTransmissionError(EMLetter letter, string errorMessage)
        {
            this.PostOfficeEvent?.Invoke(this, new PostOfficeEventArgs(PostOfficeEventType.Error, $"letter '{letter.Title}' encountered a transmission error: {errorMessage}"));
        }

        private void Listen(IClientMailDispatcher dispatcher, bool bListen)
        {
            if (bListen)
            {
                dispatcher.ResetedEvent += this.OnDispatcherReseted;
                dispatcher.IncomingLetterEvent += this.OnIncomingLetter;
                dispatcher.TransmissionErrorEvent += this.OnTransmissionError;
            }
            else
            {
                dispatcher.ResetedEvent -= this.OnDispatcherReseted;
                dispatcher.IncomingLetterEvent -= this.OnIncomingLetter;
                dispatcher.TransmissionErrorEvent -= this.OnTransmissionError;
            }
        }

        /// <summary>
        /// deliver to localhost
        /// </summary>
        /// <param name="letter"></param>
        /// <returns></returns>
        private EMLetter Dispatch(EMLetter letter)
        {
            if (letter.HasFlag(Messages.StatusCode.Get))
            {
                return this.OnIncomingLetter(letter);
            }
            else  // Post
            {
                ThreadPool.QueueUserWorkItem(o => this.OnIncomingLetter(letter), letter);

                return null;
            }
        }
        #endregion
    }
}
