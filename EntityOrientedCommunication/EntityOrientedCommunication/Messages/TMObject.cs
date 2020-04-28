using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Diagnostics;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// 用于传输单个对象
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TMObject<T> : TMessage, ITMObject<T>
    {
        #region property
        /// <summary>
        /// 使用该属性时相应对象才会被反序列化
        /// </summary>
        public T Object
        {
            get
            {
                if (!bObjectRecovered)
                {
                    _object = Serializer.FromJson<T>(_objJson);
                    bObjectRecovered = true;
                }

                return _object;
            }

            set
            {
                _object = value;
                bObjectRecovered = true;
            }
        }

        #region lazy serialization, deserialization
        /// <summary>
        /// Json序列化时才会产生序列化过后的对象序列化串
        /// </summary>
        [JsonProperty]
        private string _serilizeObject
        {
            get
            {
                if (_objJson == null)
                {
                    if (this._object is Enum en)
                    {
                        _objJson = Serializer.ToJson(new EnumCarrier(en));
                    }
                    else if (this._object is Array arr)
                    {
                        _objJson = Serializer.ToJson(new ArrayCarrier(arr));
                    }
                    else
                    {
                        _objJson = Serializer.ToJson(_object);
                    }
                }

                return _objJson;
            }
            set
            {
                _objJson = value;
                bObjectRecovered = false;
            }
        }

        private string _objJson;

        private T _object;

        /// <summary>
        /// 代表_object已被恢复到目标值
        /// </summary>
        private bool bObjectRecovered;
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected TMObject() { }
        public TMObject(Envelope envelope, T @object) : base(envelope)
        {
            Object = @object;
        }
        public TMObject(TMessage toreply, T @object) : base(toreply)
        {
            Object = @object;
        }
        public TMObject(TMObject<T> copyFrom) : base(copyFrom)
        {
            _objJson = copyFrom._objJson;
            _object = copyFrom._object;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            var objStr = Object.ToString();

            if (objStr.Length > 128)
            {
                objStr = Object.GetType().FullName;
            }

            return Format("TObj", $"{objStr}");
        }
        #endregion
    }
}
