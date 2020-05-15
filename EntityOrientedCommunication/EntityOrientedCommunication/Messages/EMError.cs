/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：6/5/2019 11:19:17 AM
 * ============================================================================================*/
using System;

namespace EntityOrientedCommunication.Messages
{
    internal enum ErrorCode
    {
        Default,
        PushedOut,  // pushed out by another login
        IncorrectUsernameOrPassword,
        UnregisteredUser,
        InvalidOperation,
        ServerBlocked,
        InvalidMessage,
    }

    /// <summary>
    /// this class is used by server to report errors
    /// </summary>
    [Serializable]
    internal class EMError : EMText
    {
        #region data
        #region property
        public ErrorCode Code;
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        protected EMError() { }

        public EMError(Envelope envelope, string msg, ErrorCode code = ErrorCode.Default) : base(envelope, msg)
        {
            Status = StatusCode.Denied;
            Code = code;
        }

        public EMError(EMessage toReply, string msg, ErrorCode code = ErrorCode.Default) : base(toReply, msg)
        {
            Status = StatusCode.Denied;
            Code = code;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            return Format("EErr", $"E{(int)Code}: {Text}");
        }
        #endregion

        #region private
        #endregion
    }
}
