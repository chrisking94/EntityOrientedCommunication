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

namespace EntityOrientedCommunication
{
    public enum LetterType
    {
        Normal,  // the letter is stored in the buffer box and will be sent out once receiver logs in
        RealTime,  // letter would be discarded if receiver is not online
        Retard,  // letter will be received only when recipient pulls them
        Emergency,  // an error will be reported if recipient is not on line
    }

    /// <summary>
    /// 信件，在发送时分配信封
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TMLetter : TMObject<object>
    {
        #region data
        #region property
        [JsonProperty]
        public string Recipient { get; set; }

        [JsonProperty]
        public string Sender { get; set; }

        [JsonProperty]
        public string Title;

        public object Content { get => Object; set => Object = value; }  // lazy content

        [Obsolete("do not use this property anymore, set type in property 'Recipient' and 'Sender'", true)]
        public string ReceiverTypeFullName { get; private set; }

        [JsonProperty]
        public LetterType LetterType { get; set; }

        /// <summary>
        /// MailBox赋值供Agent使用
        /// </summary>
        [JsonProperty]
        public DateTime TimeStamp { get; set; }
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected TMLetter() { }

        [Obsolete("为兼容性")]
        public TMLetter(string recipient, string title,
            object content, StatusCode status, string recipientEntityName,
            LetterType type = LetterType.Normal)
        {
            Recipient = recipientEntityName == null ? recipient : $"{recipientEntityName}@{recipient}";
            Sender = "@OldVersion@";
            Title = title;
            Object = content;
            Status = status | StatusCode.Letter;
            //ReceiverTypeFullName = header;
            LetterType = type;
        }

        public TMLetter(string recipient, string sender, string title, 
            object content, StatusCode status, string recipientEntityName,
            LetterType type = LetterType.Normal)
        {
            Recipient = recipientEntityName == null ? recipient : $"{recipientEntityName}@{recipient}";
            Sender = sender;
            Title = title;
            Object = content;
            Status = status | StatusCode.Letter;
            //ReceiverTypeFullName = header;
            LetterType = type;
        }

        public TMLetter(TMLetter copyFrom) : base(copyFrom)
        {
            this.Recipient = copyFrom.Recipient;
            this.Sender = copyFrom.Sender;
            this.Title = copyFrom.Title;
            this.LetterType = copyFrom.LetterType;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            return Format("TLtr", $"《{Title}》{Sender} -> {Recipient}");
        }
        #endregion

        #region private
        #endregion
    }
}
