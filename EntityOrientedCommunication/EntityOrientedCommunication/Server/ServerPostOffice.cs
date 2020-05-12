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
    internal sealed class ServerPostOffice
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

        private IMailDispatcher dispatcher;

        /// <summary>
        /// mutex on operating with field 'dispatcher'
        /// </summary>
        private Mutex dispatcherMutex;

        private List<string> registeredReceiverEntityNames = new List<string>(1);
        #endregion
        #endregion

        #region constructor
        public ServerPostOffice(ServerUser owner)
        {
            this.owner = owner;
            this.dispatcherMutex = new Mutex();
        }
        #endregion

        #region interface
        /// <summary>
        /// push the letter into this postoffice for transfering to remote computer
        /// </summary>
        /// <param name="letter"></param>
        /// <param name="senderInfo"></param>
        /// <param name="recipientInfo"></param>
        internal EMLetter Push(EMLetter letter, MailRouteInfo recipientInfo)
        {
            if (IsActivated)  // postoffice is activated
            {
                List<string> offlineEntities;
                lock (this.registeredReceiverEntityNames)
                {
                    offlineEntities = recipientInfo.EntityNames.Except(registeredReceiverEntityNames).ToList();
                }

                if (letter.HasFlag(StatusCode.Post))  // Post
                {
                    if (offlineEntities.Count < recipientInfo.EntityNames.Count)  // if any entity is online
                    {
                        this.Dispatch(letter, recipientInfo);
                    }

                    return null;
                }
                else  // Get
                {
                    if (offlineEntities.Count > 0)
                    {
                        throw new Exception($"faild to send letter '{letter.Title}', entity(ies) '{string.Join(", ", offlineEntities)}@{recipientInfo.UserName}' is/are offline.");
                    }

                    return this.Dispatch(letter, recipientInfo);
                }
            }
            else  // postoffice is not activated
            {
                if (letter.HasFlag(StatusCode.Post))
                {
                    // pass
                    return null;
                }
                else  // Get
                {
                    throw new Exception($"unable to '{letter.GetLetterType()}' letter '{letter.Title}', target user '{recipientInfo.UserName}' is offline.");
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
        }

        public void Deactivate()
        {
            dispatcherMutex.WaitOne();
            this.dispatcher = null;
            dispatcherMutex.ReleaseMutex();

            registeredReceiverEntityNames.Clear();
        }

        public void Destroy()
        {
            this.owner = null;
            this.dispatcher = null;
        }

        public override string ToString()
        {
            return $"{owner}'s postoffice.";
        }
        #endregion

        #region private
        private EMLetter Dispatch(EMLetter letter, MailRouteInfo recipient)
        {
            var copy = new EMLetter(letter);
            copy.Recipient = recipient.ToLiteral();

            this.dispatcherMutex.WaitOne();
            if (this.dispatcher == null)
            {
                throw new Exception($"unable to dispatch letter '{letter.Title}' to '{letter.Recipient}', the dispatcher was offline.");
            }
            var result = this.dispatcher.Dispatch(copy);
            this.dispatcherMutex.ReleaseMutex();

            return result;
        }
        #endregion
    }
}
