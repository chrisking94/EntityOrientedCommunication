/* ==============================================================================
 * author		：chris
 * create time	：3/26/2019 2:47:56 PM
 * ==============================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Newtonsoft.Json;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// there is a problem when convert 'array' to json string, this class is used to wrap 'array'
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ArrayCarrier
    {
        #region data
        #region property
        public int Length => list.Count;
        #endregion

        #region field
        [JsonProperty]
        private List<object> list;

        [JsonProperty]
        private Type elementType;
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected ArrayCarrier() { }

        public ArrayCarrier(Array arr)
        {
            list = new List<object>(arr.Length);

            for (var i = 0; i < arr.Length; ++i)
            {
                list.Add(arr.GetValue(i));
            }

            elementType = arr.GetType().GetElementType();
        }
        #endregion

        #region interface
        public Array ToArray()
        {
            var arr = Array.CreateInstance(elementType, list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                var item = list[i];
                if(item.GetType() != elementType)
                {
                    item = Convert.ChangeType(item, elementType);
                }
                arr.SetValue(item, i);
            }

            return arr;
        }

        public object this[int index]
        {
            get => list[index];
            set => list[index] = value;
        }
        #endregion

        #region private
        #endregion
    }
}
