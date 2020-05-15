/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：4/30/2019 9:54:45 AM
 * ============================================================================================*/
using System;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication
{
    /// <summary>
    /// EOC letter, the ID of which will be set before transmission
    /// </summary>
    [Serializable]
    internal class EMLetter : EMObject<object>, ILetter
    {
        #region data
        #region property
        public string Title { get; set; }

        public string Recipient { get; set; }

        public string Sender { get; set; }

        public object Content { get => Object; }  // lazy content

        /// <summary>
        /// time to live
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// deadline, this letter is invalid when DateTime.Now exceed the deadline
        /// </summary>
        public long DDL { get; set; }
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        public EMLetter(string recipient, string sender, string title,
            object content, StatusCode letterType, int timeout)
        {
            Title = title;
            Recipient = recipient;
            Sender = sender;
            Object = content;
            Status = StatusCode.Letter | letterType;
            this.Timeout = timeout;
        }

        public EMLetter(string recipient, string sender, LetterContent content, int timeout) :
            this(recipient, sender, content.Title, content.Content,
                content.Mode == TransmissionMode.Post ? StatusCode.Post : StatusCode.Get, timeout)
        {
        }

        public EMLetter(EMLetter copyFrom) : base(copyFrom)
        {
            this.Title = copyFrom.Title;
            this.Recipient = copyFrom.Recipient;
            this.Sender = copyFrom.Sender;
            this.Timeout = copyFrom.Timeout;
            this.DDL = copyFrom.DDL;
        }
        #endregion

        #region interface
        internal StatusCode GetLetterType()
        {
            return (StatusCode)((uint)this.Status & 0xFF_00_0000);
        }

        internal void UpdateDDL(DateTime now)
        {
            this.DDL = this.GetMilliseconds(now) + Timeout;
        }

        internal int GetTTL(DateTime now)
        {
            var nowMs = this.GetMilliseconds(now);

            if (nowMs > DDL)
            {
                return 0;  // no time to live
            }
            return (int)(DDL - nowMs);
        }

        public override string ToString()
        {
            return Format("ELtr", $"[{Title}] {Sender} -> {Recipient}");
        }
        #endregion

        #region private
        private long GetMilliseconds(DateTime dateTime)  // Now.TotalMilliseconds
        {
            var ms = ((((((((((long)(dateTime.Year * 365) +
                dateTime.DayOfYear) * 24) +
                dateTime.Hour) * 60) +
                dateTime.Minute) * 60) +
                dateTime.Second) * 1000) +
                dateTime.Millisecond);

            return ms;
        }
        #endregion
    }
}
