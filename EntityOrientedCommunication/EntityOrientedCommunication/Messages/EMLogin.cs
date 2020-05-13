using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// a kind of message sent by client, request log into server
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal class EMLogin: EMessage
    {
        #region property
        [JsonProperty]
        public string Username;

        [JsonProperty]
        public string Password;
        #endregion

        #region constructor
        [JsonConstructor]
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
