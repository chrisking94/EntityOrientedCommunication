﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Diagnostics;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// this message is used to transfer an object, the object will be serialized before transfering
    /// <para>when the remote computer received this message, the object will be deserilized when it is accessed</para>
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TMObject<T> : TMessage, IObject<T>
    {
        #region property
        /// <summary>
        /// the object json will be converted to 'T' type object when this property is visited first time.
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
        /// the object will be serialized as string when this message is prepared to transfer to remote computer
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
        /// denote the '_object' of this message has been rehabilitated from json string to it's original form
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
