/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/24/2020 9:24:11 AM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Mail;

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
        public string OfficeName => dispatcher.ClientName;
        #endregion

        #region field
        private Dictionary<string, ClientMailBox> dictEntityName2MailBox;

        private IClientMailDispatcher dispatcher;
        #endregion
        #endregion

        #region constructor
        public ClientPostOffice(IClientMailDispatcher dispatcher)
        {
            dictEntityName2MailBox = new Dictionary<string, ClientMailBox>(1);
            this.dispatcher = dispatcher;
        }
        #endregion

        #region interface
        /// <summary>
        /// get a mailbox by entity name
        /// </summary>
        /// <param name="entityName"></param>
        /// <returns></returns>
        public ClientMailBox this[string entityName]
        {
            get
            {
                lock (this.dictEntityName2MailBox)
                {
                    return this.dictEntityName2MailBox[entityName];
                }
            }
        }

        /// <summary>
        /// register a 'ClientMailBox' box for 'receiver', one entity name can only be registered once
        /// <para>if the imminent receiver has same name with the old receiver in this office, then the old receiver will be destroyed and replaced</para>
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        public ClientMailBox Register(IMailReceiver receiver)
        {
            if (receiver.EntityName == null)
            {
                throw new ArgumentNullException($"the {nameof(receiver)}.{nameof(receiver.EntityName)} should not be null.");
            }

            var mailBox = new ClientMailBox(receiver, this);

            lock (dictEntityName2MailBox)
            {
                if (dictEntityName2MailBox.ContainsKey(mailBox.EntityName))
                {
                    PostOfficeEvent?.Invoke(this, 
                        new PostOfficeEventArgs(PostOfficeEventType.Prompt, $"there's already a receiver named '{mailBox.EntityName}' registered, this old receiver will be destroyed."));
                    var oldBox = dictEntityName2MailBox[mailBox.EntityName];
                    oldBox.Destroy();
                }

                dictEntityName2MailBox[mailBox.EntityName] = mailBox;
            }

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
            lock(dictEntityName2MailBox)
                return dictEntityName2MailBox.ContainsKey(entityName);
        }

        /// <summary>
        /// pickup a letter sent from remote postoffice
        /// </summary>
        /// <param name="letter"></param>
        internal void Pickup(EMLetter letter)
        {
            ClientMailBox mailBox = null;

            lock (dictEntityName2MailBox)
            {
                var routeInfo = MailRouteInfo.Parse(letter.Recipient)[0];
                foreach (var entityName in routeInfo.ReceiverEntityNames)
                {
                    if (dictEntityName2MailBox.ContainsKey(entityName))
                    {
                        mailBox = dictEntityName2MailBox[entityName];
                    }
                    else
                    {
                        PostOfficeEvent?.Invoke(this,
                            new PostOfficeEventArgs(PostOfficeEventType.Error,
                            $"unable to pickup letter: postoffice '{this.OfficeName}' has registered a receiver named '{entityName}', but the corresponding instance if not found."));
                    }
                }
            }

            mailBox?.Receive(letter);
        }

        /// <summary>
        /// provide a send interface for every mailbox in this postoffice
        /// </summary>
        /// <param name="letter"></param>
        internal void Send(EMLetter letter)
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

            if (letter.Recipient != "")
            {
                // send to tele-entity
                this.dispatcher.Dispatch(letter);
            }

            if (localRouteInfo != null)
            {
                // send to local-entity
                var copy = new EMLetter(letter);
                copy.Recipient = localRouteInfo.ToLiteral();
                Pickup(letter);
            }
        }

        /// <summary>
        /// activate all mailboxes
        /// </summary>
        public void ActivateAll()
        {
            lock (dictEntityName2MailBox)
            {
                // tele-register
                foreach (var reciver in this.dictEntityName2MailBox.Values)
                {
                    this.dispatcher.Activate(reciver);
                }
            }
        }

        public void Destroy()
        {
            lock (dictEntityName2MailBox)
            {
                foreach (var mailBox in dictEntityName2MailBox.Values)
                {
                    mailBox.Destroy();
                }
                dictEntityName2MailBox.Clear();
            }
        }
        #endregion

        #region private
        #endregion
    }
}
