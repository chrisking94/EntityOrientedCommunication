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

        private string mailAdress => $"{EntityName}@{postoffice.OfficeName}";
        #endregion

        #region field
        private IMailReceiver receiver;

        private ClientPostOffice postoffice;

        private Dictionary<string, TCounter> dictSerial2TCounter;
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
            this.postoffice = office;
            this.dictSerial2TCounter = new Dictionary<string, TCounter>();
        }
        #endregion

        #region interface
        internal void Receive(EMLetter letter)
        {
            var waitHandle = this.PopWaitHandler(letter);

            if (waitHandle == null)
            {
                ThreadPool.QueueUserWorkItem(_processPickupLetter, letter);
            }
            else
            {
                waitHandle.SetReply(letter);  // Get
            }
        }

        public void Send(EMLetter letter)
        {
            postoffice.Send(letter);
        }

        /// <summary>
        /// the letter will be delivered to local machine if the 'username' part of recipient is set to 'localhost'
        /// </summary>
        /// <param name="recipient"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="letterType"></param>
        public void Send(string recipient, string title, object content, LetterType letterType = LetterType.Normal)
        {
            var letter = new EMLetter(recipient, mailAdress, title, content, letterType, CreateSerialNumber());
            postoffice.Send(letter);
        }

        /// <summary>
        /// send a letter to the remote entity and wait for reply, the reply message will not be pass to the 'Pickup' method of 'receiver',
        /// <para>its content will be passed to the invoking position as return value</para>
        /// </summary>
        /// <param name="recipient">target entity route information, there should be only 1 entity in the recipient route info</param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="timeout">unit: ms</param>
        /// <returns></returns>
        public object Get(string recipient, string title, object content, int timeout = int.MaxValue)
        {
            // simple check
            var routeInfos = MailRouteInfo.Parse(recipient);
            if (routeInfos.Count > 1)
            {
                throw new Exception($"letter of type '{nameof(LetterType.EmergencyGet)}' should not have multiple recipients.");
            }

            var letter = new EMLetter(recipient, mailAdress, title, content, LetterType.EmergencyGet, CreateSerialNumber());
            var tCounter = this.CreateWaitHandler(letter, timeout);
            postoffice.Send(letter);

            // wait
            while (true)
            {
                if (tCounter.IsTimeOut)
                {
                    break;
                }

                if (tCounter.IsReplied)
                {
                    var responseLetter = tCounter.ResponseMsg as EMLetter;
                    if (responseLetter.Title == "error")
                    {
                        throw new Exception($"an error has been reported by '{responseLetter.Sender}': {responseLetter.Content}");
                    }
                    return responseLetter.Content;
                }

                Thread.Sleep(1);
                tCounter.Decrease(1);
            }

            throw new Exception($"get failed, no response from the remote entity.");
        }

        /// <summary>
        /// reply a letter, the 'LetterType' of the reply letter is same with the <paramref name="toReply"/> letter
        /// </summary>
        /// <param name="toReply"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        public void Reply(EMLetter toReply, string title, object content)
        {
            Send(toReply.Sender, title, content, toReply.LetterType);
        }

        internal void Destroy()
        {
            receiver = null;
            postoffice = null;
        }
        #endregion

        #region private
        private int _serialCounter = 1;
        private string CreateSerialNumber()
        {
            return $"{this.mailAdress}{this._serialCounter++}";
        }

        private TCounter PopWaitHandler(EMLetter letter)  // null if no handler for this letter
        {
            lock (dictSerial2TCounter)
            {
                TCounter tCounter;
                if (dictSerial2TCounter.TryGetValue(letter.Serial, out tCounter))  // exists
                {
                    dictSerial2TCounter.Remove(letter.Serial);
                    return tCounter;
                }
            }
            return null;
        }

        private TCounter CreateWaitHandler(EMLetter letter, int timeout)
        {
            TCounter tCounter;
            lock (dictSerial2TCounter)
            {
                tCounter = new TCounter(letter, timeout);
                this.dictSerial2TCounter[letter.Serial] = tCounter;

                return tCounter;
            }
        }

        private void _processPickupLetter(object obj)
        {
            var letter = obj as EMLetter;
            var feedbackTitle = $"RE:{letter.Title}";
            var feedback = receiver.Pickup(letter);
            var feedbackType = letter.LetterType;

            if (letter.LetterType == LetterType.EmergencyGet)  // must return a value to the sender
            {
                if (feedback == null)
                {
                    feedbackTitle = "error";
                    feedback = $"entity '{this.EntityName}' did not response anything after picking up letter '{letter.Title}'";
                }
                feedbackType = LetterType.RealTime;  // change letter type
            }

            if (feedback != null)
            {
                this.Send(new EMLetter(letter.Sender, this.mailAdress, feedbackTitle, feedback, feedbackType, letter.Serial));
            }
        }
        #endregion
    }
}
