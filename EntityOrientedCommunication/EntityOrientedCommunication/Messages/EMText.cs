using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Diagnostics;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// used to transfer a simple literal string
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class EMText: EMessage
    {
        #region field
        [JsonProperty]
        public string Text;
        #endregion

        #region constructor
        [JsonConstructor]
        protected EMText() { }

        protected EMText(string text)
        {
            Text = text;
        }

        public EMText(Envelope envelope, string text) : base(envelope)
        {
            Text = text;
        }

        public EMText(EMessage toBeReplied, string text) : base(toBeReplied)
        {
            Text = text;
            Status = toBeReplied.Status;
        }

        public EMText(EMessage toBeReplied, string text, StatusCode status) : base(toBeReplied)
        {
            Text = text;
            Status = status;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            return Format("ETxt", Text);
        }
        #endregion
    }
}
