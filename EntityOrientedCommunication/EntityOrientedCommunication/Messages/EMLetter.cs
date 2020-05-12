/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：4/30/2019 9:54:45 AM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Newtonsoft.Json;
using EntityOrientedCommunication.Messages;
using EntityOrientedCommunication.Mail;
using EntityOrientedCommunication.Client;
using EntityOrientedCommunication.Facilities;

namespace EntityOrientedCommunication
{
    /// <summary>
    /// EOC letter, the ID of which will be set before transmission
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal class EMLetter : EMObject<object>, ILetter
    {
        #region data
        #region property
        [JsonProperty]
        public string Title { get; set; }

        [JsonProperty]
        public string Recipient { get; set; }

        [JsonProperty]
        public string Sender { get; set; }

        public object Content { get => Object; }  // lazy content

        /// <summary>
        /// this letter is invalid when DateTime.Now exceed the deadline
        /// </summary>
        [JsonProperty]
        public long Deadline { get; set; }
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected EMLetter() { }

        public EMLetter(string recipient, string sender, string title,
            object content, StatusCode letterType, int timeout)
        {
            Title = title;
            Recipient = recipient;
            Sender = sender;
            Object = content;
            Status = StatusCode.Letter | letterType;
            this.SetTimeout(timeout);
        }

        public EMLetter(EMLetter copyFrom) : base(copyFrom)
        {
            this.Title = copyFrom.Title;
            this.Recipient = copyFrom.Recipient;
            this.Sender = copyFrom.Sender;
            this.Deadline = copyFrom.Deadline;
        }
        #endregion

        #region interface
        internal StatusCode GetLetterType()
        {
            return (StatusCode)((uint)this.Status & 0xFF_00_0000);
        }

        internal int GetTTL()
        {
            var nowMs = this.BaseMilliseconds();

            if (nowMs > Deadline)
            {
                return 0;  // no time to live
            }
            return (int)(Deadline - nowMs);
        }

        internal void SetTimeout(int timeout)
        {
            this.Deadline = this.BaseMilliseconds() + timeout;
        }

        public override string ToString()
        {
            return Format("ELtr", $"[{Title}] {Sender} -> {Recipient}");
        }
        #endregion

        #region private
        private long BaseMilliseconds()  // Now.TotalMilliseconds
        {
            var now = TimeBlock.Now.Value;

            var ms = ((((((((((long)(now.Year * 365) +
                now.DayOfYear) * 24) +
                now.Hour) * 60) +
                now.Minute) * 60) +
                now.Second) * 1000) +
                now.Millisecond);

            return ms;
        }
        #endregion
    }
}
