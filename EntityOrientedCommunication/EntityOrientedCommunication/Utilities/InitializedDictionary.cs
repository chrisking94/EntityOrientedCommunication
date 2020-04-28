using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOrientedCommunication.Utilities
{
    /// <summary>
    /// 可以访问任意Key而不会出现Key不存在异常，如果Key不存在会将Key所对应Value设为DefaultValue并返回。
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class InitializedDictionary<TKey, TValue> :
        Dictionary<TKey, TValue>
    {
        #region field
        object defaultValue;  // TValue实例或TValue.Ctor
        #endregion

        #region constructor
        public InitializedDictionary(int capacity, TValue defaultValue = default) : base(capacity)
        {
            this.defaultValue = defaultValue;
        }

        public InitializedDictionary(TValue defaultValue = default, int capacity = 0) : base(capacity)
        {
            this.defaultValue = defaultValue;
        }

        public InitializedDictionary(Func<TValue> valueConstructor, int capacity = 0)
        {
            defaultValue = valueConstructor;
        }

        public InitializedDictionary(Dictionary<TKey, TValue> copyFrom, object defaultValue) : base(copyFrom)
        {
            this.defaultValue = defaultValue;
        }

        public InitializedDictionary(InitializedDictionary<TKey, TValue> copyFrom) : base(copyFrom)
        {
            defaultValue = copyFrom.defaultValue;
        }
        #endregion

        #region interface
        public new TValue this[TKey key]
        {
            get
            {
                if (ContainsKey(key))
                {
                    return base[key];
                }
                else
                {
                    TValue value = default;
                    if (defaultValue is Func<TValue> createValue)
                    {
                        value = createValue();
                    }
                    else //(defaultValue is TValue)
                    {
                        value = (TValue)defaultValue;
                    }
                    base[key] = value;
                    return value;
                }
            }
            set
            {
                base[key] = value;
            }
        }
        #endregion
    }
}
