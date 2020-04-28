using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace EntityOrientedCommunication.Messages
{
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
        public TMLogin(TapaOperator opr) : base(2)
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
