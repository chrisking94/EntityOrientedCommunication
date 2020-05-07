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
using EntityOrientedCommunication.Facilities;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// if a value type item is serialized at an 'object pointer', then it can not be deserialized to the correct type
    /// <para>this class carries the type information of the item, so it is able to convert the serialized item to it's original type correctly</para>
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ValueCarrier
    {
        #region field
        [JsonProperty]
        public Type Type;

        [JsonProperty]
        public object Value;
        #endregion

        #region constructor
        [JsonConstructor]
        protected ValueCarrier() { }

        public ValueCarrier(object obj)
        {
            Value = obj;
            Type = obj.GetType();
        }
        #endregion

        #region interface
        public object ToEnum()
        {
            return Type.Cast(Value);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
        #endregion
    }
}
