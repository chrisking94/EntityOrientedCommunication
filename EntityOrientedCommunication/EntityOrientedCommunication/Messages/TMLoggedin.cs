﻿/* ==============================================================================
 * author		：chris
 * create time	：3/13/2019 9:49:34 AM
 * ==============================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Newtonsoft.Json;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// 登陆成功
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TMLoggedin : TMText, ITMObject<DateTime>
    {
        #region data
        #region property
        public string Token => Text;

        [JsonProperty]
        public DateTime Object { get; set; }  // sync time
        #endregion

        #region field
        [JsonProperty]
        public TapaOperator Operator;

        [JsonProperty]
        public string ServerName;
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected TMLoggedin() { }
        public TMLoggedin(TMLogin toReply, string serverName, TapaOperator @operator, string token) : 
            base(toReply, token)
        {
            Operator = @operator.AsTapaOperator();
            ServerName = serverName;
            Status = StatusCode.Ok;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            return Format("TLgd", $"token={Text}");
        }
        #endregion
    }
}
