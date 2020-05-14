/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：6/5/2019 11:19:17 AM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Newtonsoft.Json;

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
    [JsonObject(MemberSerialization.OptIn)]
    internal class EMError : EMText
    {
        #region data
        #region property
        [JsonProperty]
        public ErrorCode Code;
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
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
