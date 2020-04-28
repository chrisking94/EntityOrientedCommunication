using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Diagnostics;

namespace EntityOrientedCommunication.Messages
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TMText: TMessage
    {
        #region field
        [JsonProperty]
        public string Text;
        #endregion

        #region constructor
        [JsonConstructor]
        protected TMText() { }
        protected TMText(string text)
        {
            Text = text;
        }
        public TMText(Envelope envelope, string text) : base(envelope)
        {
            Text = text;
        }
        public TMText(TMessage toBeReplied, string text) : base(toBeReplied)
        {
            Text = text;
            Status = toBeReplied.Status;
        }
        public TMText(TMessage toBeReplied, string text, StatusCode status) : base(toBeReplied)
        {
            Text = text;
            Status = status;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            return Format("TTxt", Text);
        }
        #endregion
    }
}
