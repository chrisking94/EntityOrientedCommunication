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
using EntityOrientedCommunication.Utilities;

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
        internal void Receive(TMLetter letter)
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

        public void Send(TMLetter letter)
        {
            postoffice.Send(letter);
        }

        /// <summary>
        /// the letter will be delivered to local machine if the 'username' part of recipient is set to 'localhost'
        /// </summary>
        /// <param name="letter"></param>
        public void Send(string recipient, string header, object content, LetterType letterType = LetterType.Normal)
        {
            var letter = new TMLetter(recipient, mailAdress, header, content, letterType, CreateSerialNumber());
            postoffice.Send(letter);
        }

        public object Get(string recipient, string header, object content, int timeout = 2000)
        {
            // simple check
            var routeInfos = MailRouteInfo.Parse(recipient);
            if (routeInfos.Count > 1)
            {
                throw new Exception($"letter of type '{nameof(LetterType.RealTimeGet)}' should not have multiple recipients.");
            }

            var letter = new TMLetter(recipient, mailAdress, header, content, LetterType.RealTimeGet, CreateSerialNumber());
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
                    var responseLetter = tCounter.ResponseMsg as TMLetter;
                    if (responseLetter.Title == "error")
                    {
                        throw new Exception($"an error has been reported by the remote entity: {responseLetter.Content}");
                    }
                    return responseLetter.Content;
                }

                Thread.Sleep(1);
                tCounter.Decrease(1);
            }

            throw new Exception($"get failed, no response from the remote entity.");
        }

        public void Reply(TMLetter letter, string title, object content)
        {
            Send(letter.Sender, title, content, LetterType.Normal);
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

        private TCounter PopWaitHandler(TMLetter letter)  // null if no handler for this letter
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

        private TCounter CreateWaitHandler(TMLetter letter, int timeout)
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
            var letter = obj as TMLetter;
            var feedbackTitle = $"RE:{letter.Title}";
            var feedback = receiver.Pickup(letter);
            var feedbackType = letter.LetterType;

            if (letter.LetterType == LetterType.RealTimeGet)  // must return a value to the sender
            {
                if (feedback == null)
                {
                    feedbackTitle = "error";
                    feedback = $"recipient '{letter.Recipient}' has no response to letter of title '{letter.Title}'";
                }
                feedbackType = LetterType.RealTime;  // change letter type
            }

            if (feedback != null)
            {
                this.Send(new TMLetter(letter.Sender, this.mailAdress, feedbackTitle, feedback, feedbackType, letter.Serial));
            }
        }
        #endregion
    }
}
