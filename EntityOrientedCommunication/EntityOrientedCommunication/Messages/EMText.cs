using System;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// used to transfer a simple literal string
    /// </summary>
    [Serializable]
    internal class EMText: EMessage
    {
        #region field
        public string Text;
        #endregion

        #region constructor
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
