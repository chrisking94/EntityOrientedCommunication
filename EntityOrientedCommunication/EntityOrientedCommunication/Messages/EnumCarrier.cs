/* ==============================================================================
 * author		：chris
 * create time	：3/12/2019 10:18:53 AM
 * ==============================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Reflection;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// 枚举直接作为Operation参数时以整数传输，反序列化时会出现无法识别枚举类型问题，该类主要负责携带附加类型信息
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class EnumCarrier
    {
        #region field
        [JsonProperty]
        public Type Type;
        [JsonProperty]
        public string Value;
        #endregion

        #region constructor
        [JsonConstructor]
        protected EnumCarrier() { }
        public EnumCarrier(Enum obj)
        {
            Value = obj.ToString();
            Type = obj.GetType();
        }
        #endregion

        #region interface
        public object ToEnum()
        {
            return Enum.Parse(Type, Value);
        }
        public override string ToString()
        {
            return Value;
        }
        #endregion
    }
}
