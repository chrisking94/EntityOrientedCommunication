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
    /// if a enum type item is serialized at a 'object pointer', then it can not be deserialized to the correct type
    /// <para>this class carries the type information of the enum value, so it is able to convert the serialized enum value to it's original type</para>
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
