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
        /// <summary>
        /// the letter is stored in the buffer box on server and will be sent out once receiver logs in
        /// </summary>
        Normal, 
        /// <summary>
        /// letter would be discarded when server receives it if receiver is not online
        /// </summary>
        RealTime,
        /// <summary>
        /// letter will be stored on the server till the recipients pull it
        /// </summary>
        Retard,
        /// <summary>
        /// an error will be reported by server if recipient is not on line
        /// </summary>
        Emergency,
        /// <summary>
        /// an error will be reported by server if recipient is not on line. there's only 1 recipient permitted for each 'EmergencyGet'.
        /// <para>the difference between 'EmergencyGet' and 'Emergency' is that the recipient should emit a reply to the 'EmergencyGet' letter sender</para>
        /// </summary>
        EmergencyGet,
    }

    /// <summary>
    /// EOC letter, the ID of which will be set before transmission
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class EMLetter : EMObject<object>
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

        /// <summary>
        /// serial is created by ClientMailBox
        /// </summary>
        [JsonProperty]
        internal string Serial { get; private set; }
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected EMLetter() { }

        public EMLetter(string recipient, string sender, string title,
            object content, LetterType type, string serial)
        {
            Title = title;
            Recipient = recipient;
            Sender = sender;
            Object = content;
            LetterType = type;
            this.Serial = serial;
            Status = StatusCode.Letter;
        }

        public EMLetter(EMLetter copyFrom) : base(copyFrom)
        {
            this.Title = copyFrom.Title;
            this.Recipient = copyFrom.Recipient;
            this.Sender = copyFrom.Sender;
            this.Serial = copyFrom.Serial;
            this.LetterType = copyFrom.LetterType;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            return Format("ELtr", $"[{Title}] {Sender} -> {Recipient}");
        }
        #endregion

        #region private
        #endregion
    }
}
