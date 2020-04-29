/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/23/2020 2:59:06 PM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Mail;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication.Client
{
    public sealed class ClientMailBox
    {
        #region data
        #region property
        public readonly string EntityName;

        private string mailAdress => $"{EntityName}@{office.OfficeName}";
        #endregion

        #region field
        private IMailReceiver receiver;

        private ClientPostOffice office;
        #endregion
        #endregion

        #region constructor
        internal ClientMailBox(IMailReceiver receiver, ClientPostOffice office)
        {
            if (receiver.EntityName == null)
            {
                throw new Exception($"the '{nameof(receiver.EntityName)}' of receiver should not be null!");
            }

            this.receiver = receiver;
            this.EntityName = receiver.EntityName;
            this.office = office;
        }
        #endregion

        #region interface
        internal void Receive(TMLetter letter)
        {
            ThreadPool.QueueUserWorkItem(o => receiver.Pickup(letter as TMLetter), letter);  // local
        }

        /// <summary>
        /// the letter will be delivered to local machine if the 'username' part of recipient is set to 'localhost'
        /// </summary>
        /// <param name="letter"></param>
        public void Send(string recipient, string header, object content, LetterType letterType = LetterType.Normal)
        {
            var letter = new TMLetter(recipient, mailAdress, header, content, letterType);
            office.Send(letter);
        }

        public void Reply(TMLetter letter, string title, object content)
        {
            Send(letter.Sender, title, content, LetterType.Normal);
        }

        internal void Destroy()
        {
            receiver = null;
            office = null;
        }
        #endregion

        #region private
        #endregion
    }
}
