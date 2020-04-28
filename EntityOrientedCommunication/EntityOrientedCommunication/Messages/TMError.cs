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
    public enum ErrorCode
    {
        Default,
        RedundantLogin,  // 重复登陆
        IncorrectUsernameOrPassword,  // 用户名或密码错误
        UnregisteredUser,  // 未注册
        InvalidOperation,  // 无效操作
        ServerBlocked,
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class TMError : TMText
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
        protected TMError() { }

        public TMError(Envelope envelope, string msg, ErrorCode code = ErrorCode.Default) : base(envelope, msg)
        {
            Status = StatusCode.Denied;
            Code = code;
        }

        public TMError(TMessage toReply, string msg, ErrorCode code = ErrorCode.Default) : base(toReply, msg)
        {
            Status = StatusCode.Denied;
            Code = code;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            return Format("TErr", $"E{(int)Code}: {Text}");
        }
        #endregion

        #region private
        #endregion
    }
}
