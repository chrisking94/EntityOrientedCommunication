/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/29/2020 6:07:57 PM					
\********************************************************/
using EOCServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerDemo
{
    /// <summary>
    /// 操作员
    /// </summary>
    public class UserInfo : IUser
    {
        #region property
        public long ID { get; set; }

        public string Name { get; set; }

        public string Password { get; private set; }

        public string NickName { get; set; }

        public object Detail { get; set; }
        #endregion

        #region constructor
        public UserInfo()
        {
        }

        public UserInfo(string name, string detail = "")
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

        public void Update(UserInfo opr)
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
        public UserInfo Copy()
        {
            var opr = new UserInfo();

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
