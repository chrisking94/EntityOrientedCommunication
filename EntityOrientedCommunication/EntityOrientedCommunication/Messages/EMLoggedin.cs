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
    /// login succeed message
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class EMLoggedin : EMText, IObject<DateTime>
    {
        #region data
        #region property
        public string Token => Text;

        [JsonProperty]
        public DateTime Object { get; set; }  // sync time
        #endregion

        #region field
        [JsonProperty]
        public User User;

        [JsonProperty]
        public string ServerName;
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
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