using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// sent by client, request log into server
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TMLogin: TMessage
    {
        #region property
        [JsonProperty]
        public string Username;

        [JsonProperty]
        public string Password;
        #endregion

        #region constructor
        [JsonConstructor]
        protected TMLogin() { }

        public TMLogin(User opr) : base(2)  // special Id
        {
            Username = opr.Name;
            Password = opr.Password;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            return Format("TLgi", $"{Username}");
        }
        #endregion
    }
}
