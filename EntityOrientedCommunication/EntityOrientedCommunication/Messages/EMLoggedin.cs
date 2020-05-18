/* ==============================================================================
 * author		：chris
 * create time	：3/13/2019 9:49:34 AM
 * ==============================================================================*/
using System;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// login succeed message
    /// </summary>
    [Serializable]
    internal class EMLoggedin : EMText
    {
        #region data
        #region property
        public string Token => Text;

        public DateTime ServerTime { get; set; }  // sync time
        #endregion

        #region field
        public User User;

        public string ServerName;
        #endregion
        #endregion

        #region constructor
        protected EMLoggedin() { }

        public EMLoggedin(EMLogin toReply, string serverName, User @operator, string token) : 
            base(toReply, token)
        {
            User = @operator.Copy();
            ServerName = serverName;
            Status = StatusCode.Ok;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            return Format("ELgd", $"token={Text}");
        }
        #endregion
    }
}
