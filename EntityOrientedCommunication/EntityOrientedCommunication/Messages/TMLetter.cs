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
    /// EOC letter, the ID of which will be set at sending at transist time
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TMLetter : TMObject<object>
    {
        #region data
        #region property
        [JsonProperty]
        public string Title;

        [JsonProperty]
        public string Recipient { get; set; }

        [JsonProperty]
        public string Sender { get; set; }

        public object Content { get => Object; }  // lazy content

        [JsonProperty]
        public LetterType LetterType { get; set; }
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected TMLetter() { }

        public TMLetter(string recipient, string sender, string title,
            object content, LetterType type = LetterType.Normal)
        {
            Title = title;
            Recipient = recipient;
            Sender = sender;
            Object = content;
            LetterType = type;

            Status = StatusCode.Letter;
        }

        public TMLetter(TMLetter copyFrom) : base(copyFrom)
        {
            this.Title = copyFrom.Title;
            this.Recipient = copyFrom.Recipient;
            this.Sender = copyFrom.Sender;
            
            this.LetterType = copyFrom.LetterType;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            return Format("TLtr", $"[{Title}] {Sender} -> {Recipient}");
        }
        #endregion

        #region private
        #endregion
    }
}
