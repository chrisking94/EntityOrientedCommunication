using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Diagnostics;
using EntityOrientedCommunication;

namespace EntityOrientedCommunication
{
    /// <summary>
    /// 工位
    /// </summary>
    [Flags]
    public enum StationType
    {
        Unkown,
        StationMaster,  // 站长
        SystemService,  // 系统服务
    }
    /// <summary>
    /// 操作员
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TapaOperator
    {
        #region property
        [JsonProperty]
        public long ID;

        [JsonProperty]
        public string Name;

        [JsonProperty]
        public string Password { get; private set; }

        [JsonProperty]
        public string NickName;

        [JsonProperty]
        public object Detail;
        #endregion

        #region constructor
        [JsonConstructor]
        public TapaOperator()
        {
        }

        protected TapaOperator(string name, string detail = "")
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

        public void Update(TapaOperator opr)
        {
            Name = opr.Name;
            ID = opr.ID;
            NickName = opr.NickName;
            Detail = opr.Detail;
        }

        /// <summary>
        /// 没有拷贝密码
        /// </summary>
        /// <returns></returns>
        public TapaOperator AsTapaOperator()
        {
            var opr = new TapaOperator();

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
