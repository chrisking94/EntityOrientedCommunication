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

    /// <summary>
    /// manage the entity register, message route etc.
    /// </summary>
    public class ClientPostOffice : IDisposable
    {
        #region data
        /// <summary>
        /// event emmited by this post office, either 'Promt' or 'Error'
        /// </summary>
        public PostOfficeEventHandler PostOfficeEvent;

        #region property
        /// <summary>
        /// the information of the user has logged in
        /// </summary>
        public IUser User { get; }

        /// <summary>
        /// server time
        /// </summary>
        public DateTime Now => this.transceiver.Now;
        #endregion

        #region field
        private Dictionary<string, ClientMailBox> dictName2MailBox;

        private ReaderWriterLockSlim rwlsDictName2MailBox = new ReaderWriterLockSlim();

        private IClientMailTransceiver transceiver;
        #endregion
        #endregion

        #region constructor
        /// <summary>
        /// instantiate a 'ClientPostOffice'
        /// </summary>
        /// <param name="username">the username of this postoffice, which will be used for message route. The name may be modified by the method 'ClientLoginAgent.Login(string, string, int)'</param>
        public ClientPostOffice(string username = "localhost")
        {
            dictName2MailBox = new Dictionary<string, ClientMailBox>(1);
            this.User = new User(username);
        }
        #endregion

        #region interface
        /// <summary>
        /// connect to the server at sepecified IP and port
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

        /// <summary>
        /// connect to the server through memory, the returned 'ClientAgentSimulator' is effictive only when the related 'ServerSimulator' is registered to server
        /// </summary>
        /// <returns></returns>
        internal ClientAgentSimulator ConnectSimulator()
        {
            var simulator = new ClientAgentSimulator(this);

            this.Connect(simulator);

            return simulator;
        }

        internal IClientMailTransceiver Connect(IClientMailTransceiver dispatcher)
        {
            if (this.transceiver != null)
            {
                // dispose the old dispatcher
                this.transceiver.Dispose();  
                // unregiseter event listenings on old dispatcher
                this.Listen(this.transceiver, false);
            }
            this.transceiver = dispatcher;
            // register event listenings on new dispatcher
            this.Listen(this.transceiver, true);

            return dispatcher;
        }

        /// <summary>
        /// get current dispatcher, and cast it to the specified type 'T'
        /// </summary>
        /// <typeparam name="T">the type dispatcher should be</typeparam>
        /// <returns></returns>
        public T GetDispatcher<T>() where T : class
        {
            return this.transceiver as T;
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
        /// register a 'ClientMailBox' box for 'receiver', one entity name can only be registered once.
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

            transceiver.Activate(mailBox);

            return mailBox;
        }

        /// <summary>
        /// determine whether the given entity name has been registered
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
        /// an interface which is used to send letter to the reomote postoffices
        /// </summary>
        /// <param name="letter"></param>
        internal EMLetter Transfer(EMLetter letter)
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

            // tele transmission
            if (letter.Recipient != "")
            {
                // send to tele-entity
                if (letter.HasFlag(Messages.StatusCode.Get))
                    return this.transceiver.Get(letter);  // Get
                else
                    this.transceiver.Post(letter);  // Post
            }

            // local transmission
            if (localRouteInfo != null)
            {
                var copy = new EMLetter(letter);
                copy.Recipient = localRouteInfo.ToLiteral();

                if (letter.HasFlag(Messages.StatusCode.Get))
                    return this.LocalGet(copy);  // Get
                else
                    this.LocalGet(copy);  // Post
            }

            return null;
        }

        /// <summary>
        /// destroy this postoffice, the registered mailboxes and the transceiver
        /// </summary>
        public void Dispose()
        {
            rwlsDictName2MailBox.EnterWriteLock();
            foreach (var mailBox in dictName2MailBox.Values)
            {
                mailBox.Destroy();
            }
            dictName2MailBox.Clear();
            rwlsDictName2MailBox.ExitWriteLock();
            if (this.transceiver != null)
            {
                this.Listen(this.transceiver, false);
                this.transceiver.Dispose();
                this.transceiver = null;
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
                this.transceiver.Activate(dictName2MailBox.Values.ToArray());
            }
            rwlsDictName2MailBox.ExitReadLock();
        }

        private void OnTransmissionError(EMLetter letter, string errorMessage)
        {
            this.PostOfficeEvent?.Invoke(this, new PostOfficeEventArgs(PostOfficeEventType.Error, $"letter '{letter.Title}' encountered a transmission error: {errorMessage}"));
        }

        /// <summary>
        /// listen or cancel listening to transceiver
        /// </summary>
        /// <param name="transceiver"></param>
        /// <param name="bListen"></param>
        private void Listen(IClientMailTransceiver transceiver, bool bListen)
        {
            if (bListen)
            {
                transceiver.ResetedEvent += this.OnDispatcherReseted;
                transceiver.IncomingLetterEvent += this.OnIncomingLetter;
                transceiver.TransmissionErrorEvent += this.OnTransmissionError;
            }
            else
            {
                transceiver.ResetedEvent -= this.OnDispatcherReseted;
                transceiver.IncomingLetterEvent -= this.OnIncomingLetter;
                transceiver.TransmissionErrorEvent -= this.OnTransmissionError;
            }
        }

        /// <summary>
        /// 'Get' localhost
        /// </summary>
        /// <param name="letter"></param>
        /// <returns></returns>
        private EMLetter LocalGet(EMLetter letter)
        {
            return this.OnIncomingLetter(letter);
        }

        /// <summary>
        /// 'Post' a letter to localhost
        /// </summary>
        /// <param name="letter"></param>
        private void LocalPost(EMLetter letter)
        {
            ThreadPool.QueueUserWorkItem(o => this.OnIncomingLetter(letter), letter);
        }
        #endregion
    }
}
