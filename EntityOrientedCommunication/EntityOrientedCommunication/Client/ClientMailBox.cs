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
        public ClientMailBox(IMailReceiver receiver, ClientPostOffice office)
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
        /// 使用 'localhost' 作收件人可以把信件发到本机
        /// </summary>
        /// <param name="letter"></param>
        public void Send(string recipient, string title, object content, LetterType letterType = LetterType.Normal, StatusCode status = StatusCode.Letter)
        {
            var letter = new TMLetter(recipient, mailAdress, title, content, status, null, letterType);
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
