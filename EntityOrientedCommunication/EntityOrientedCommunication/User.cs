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
        /// <summary>
        /// A number managed by the server
        /// </summary>
        [JsonProperty]
        public long ID { get; set; }

        /// <summary>
        /// The unique name of user
        /// </summary>
        [JsonProperty]
        public string Name { get; set; }

        /// <summary>
        /// Any char[]
        /// </summary>
        [JsonProperty]
        public string Password { get; set; }

        /// <summary>
        /// The extra information of user
        /// </summary>
        [JsonProperty]
        public string NickName { get; set; }

        /// <summary>
        /// Curstomized user information, should be a JsonObject, see NewtonSoft.Json. Set this property on server, and it will be distributed to the logged-in client.
        /// </summary>
        [JsonProperty]
        public object Detail { get; set; }
        #endregion

        #region constructor
        [JsonConstructor]
        public User()
        {
            Password = "";
        }

        public User(string name, string detail = "")
        {
            Name = name;
            Detail = detail;
            Password = "";
        }
        #endregion

        #region interface
        internal void SetPassword(string password)
        {
            this.Password = password;
        }

        internal bool CheckPassword(string password)
        {
            return this.Password == password;
        }

        internal void Update(IUser opr)
        {
            Name = opr.Name;
            ID = opr.ID;
            NickName = opr.NickName;
            Detail = opr.Detail;
        }

        /// <summary>
        /// The password is not copied
        /// </summary>
        /// <returns></returns>
        internal User Copy()
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
