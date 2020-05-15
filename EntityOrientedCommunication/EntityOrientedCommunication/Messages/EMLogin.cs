using System;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// a kind of message sent by client, request log into server
    /// </summary>
    [Serializable]
    internal class EMLogin: EMessage
    {
        #region property
        public string Username;

        public string Password;
        #endregion

        #region constructor
        protected EMLogin() { }

        public EMLogin(User opr) : base(2)  // special ID
        {
            Username = opr.Name;
            Password = opr.Password;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            return Format("ELgi", $"{Username}");
        }
        #endregion
    }
}
