using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Diagnostics;
using EntityOrientedCommunication;
using EOCServer;

namespace EntityOrientedCommunication
{
    [JsonObject(MemberSerialization.OptIn)]
    public class User : IUser
    {
        #region property
        [JsonProperty]
        public long ID { get; set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public string Password { get; private set; }

        [JsonProperty]
        public string NickName { get; set; }

        [JsonProperty]
        public object Detail { get; set; }
        #endregion

        #region constructor
        [JsonConstructor]
        public User()
        {
        }

        protected User(string name, string detail = "")
        {
            Name = name;
            Detail = detail;
            Password = "";
        }
        #endregion

        #region interface
        public void SetPassword(string password)
        {
            this.Password = password;
        }

        public bool CheckPassword(string password)
        {
            return this.Password == password;
        }

        public void Update(User opr)
        {
            Name = opr.Name;
            ID = opr.ID;
            NickName = opr.NickName;
            Detail = opr.Detail;
        }

        /// <summary>
        /// the password is not copied
        /// </summary>
        /// <returns></returns>
        public User Copy()
        {
            var opr = new User();

            opr.Update(this);

            return opr;
        }

        public override string ToString()
        {
            return $"{Name} {Password}";
        }
        #endregion
    }
}
