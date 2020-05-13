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
using EntityOrientedCommunication.Facilities;

namespace EntityOrientedCommunication.Client
{
    public sealed class ClientMailBox
    {
        #region data
        #region property
        public readonly string EntityName;

        private string mailAdress => $"{EntityName}@{postoffice.User.Name}";

        public DateTime Now => this.postoffice.Now;
        #endregion

        #region field
        private IEntity receiver;

        private ClientPostOffice postoffice;
        #endregion
        #endregion

        #region constructor
        internal ClientMailBox(IEntity receiver, ClientPostOffice office)
        {
            if (receiver.EntityName == null)
            {
                throw new Exception($"the '{nameof(receiver.EntityName)}' of receiver should not be null!");
            }

            this.receiver = receiver;
            this.EntityName = receiver.EntityName;
            this.postoffice = office;
        }
        #endregion

        #region interface
        internal EMLetter Receive(EMLetter letter)
        {
            if (letter.HasFlag(StatusCode.Post))
            {
                ThreadPool.QueueUserWorkItem(o =>  // async mode
                {
                    var result = this.Pickup(letter);

                    if (result != null)
                    {
                        this.Reply(letter, result.Title, result.Content, result.Timeout);  // sync reply
                    }
                });

                return null;
            }
            else  // Get
            {
                return this.Pickup(letter);
            }
        }

        /// <summary>
        /// post a letter to the target entity, discard the entity if target is offline
        /// </summary>
        /// <param name="recipient">route string. e.g. entity1,entity2@Mary,Tom;entity1@Jerry</param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="timeout"></param>
        public void Post(string recipient, string title, object content = null, int timeout = int.MaxValue)
        {
            this.Send(recipient, title, content, StatusCode.Post, timeout);
        }

        /// <summary>
        /// send a letter to the remote entity and wait for reply, the reply message will not be pass to the 'Pickup' method of 'receiver',
        /// <para>its content will be passed to the invoking position as return value</para>
        /// </summary>
        /// <param name="recipient">target entity route information, there should be only 1 entity in the recipient route info. e.g. entityA@Mary</param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="timeout">unit: ms</param>
        /// <returns></returns>
        public ILetter Get(string recipient, string title, object content = null, int timeout = int.MaxValue)
        {
            if (MailRouteInfo.Parse(recipient).Count > 1)
            {
                throw new Exception("'Get' letter should not have multiple recipients.");
            }

            var letter = new EMLetter(recipient, mailAdress, title, content, StatusCode.Get, timeout);
            
            return postoffice.Send(letter);
        }

        /// <summary>
        /// reply a letter in post mode
        /// </summary>
        /// <param name="toReply"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        public void Reply(ILetter toReply, string title, object content = null, int timeout = int.MaxValue)
        {
            Send(toReply.Sender, title, content, StatusCode.Post, timeout);
        }

        internal void Destroy()
        {
            receiver = null;
            postoffice = null;
        }

        [Obsolete("please use Post(string, string, object) or Get(string, string, object)", true)]
        public void Send(string recipient, string title, object content, int code)
        {
            throw new Exception();
        }
        #endregion

        #region private
        /// <summary>
        /// the letter will be delivered to local machine if the 'username' part of recipient is set to 'localhost'
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="letterType"></param>
        private void Send(string recipient, string title, object content, StatusCode letterType, int timeout)
        {
            var letter = new EMLetter(recipient, mailAdress, title, content, letterType, timeout);
            postoffice.Send(letter);
        }

        private EMLetter Pickup(EMLetter letter)
        {
            LetterContent feedback;

            try
            {
                feedback = receiver.Pickup(letter);
            }
            catch (Exception ex)  // exception report
            {
                feedback = new LetterContent("error", ex.Message, TransmissionMode.Post);
            }

            if (feedback == null)
            {
                return null;
            }

            var fbLetter = new EMLetter(letter.Sender, this.mailAdress, feedback, letter.GetTTL(this.Now));
            fbLetter.SetEnvelope(new Envelope(letter.ID));

            return fbLetter;
        }
        #endregion
    }
}
