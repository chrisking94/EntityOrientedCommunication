using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOrientedCommunication.Utilities
{
    /// <summary>
    /// any key exists in this kind of dictionary logically
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class InitializedDictionary<TKey, TValue> :
        Dictionary<TKey, TValue>, IDictionary<TKey, TValue>
    {
        #region field
        private Func<TKey, TValue> createDefault;
        #endregion

        #region constructor
        public InitializedDictionary(TValue defaultValue = default, int capacity = 0) : base(capacity)
        {
            this.createDefault = x => defaultValue;
        }

        public InitializedDictionary(Func<TKey, TValue> createDefault, int capacity = 0) : base(capacity)
        {
            this.createDefault = createDefault;
        }

        public InitializedDictionary(Dictionary<TKey, TValue> copyFrom, TValue defaultValue) : base(copyFrom)
        {
            this.createDefault = x => defaultValue;
        }

        public InitializedDictionary(Dictionary<TKey, TValue> copyFrom, Func<TKey, TValue> createDefault) : base(copyFrom)
        {
            this.createDefault = createDefault;
        }

        public InitializedDictionary(InitializedDictionary<TKey, TValue> copyFrom) : base(copyFrom)
        {
            createDefault = copyFrom.createDefault;
        }
        #endregion

        #region interface
        public new TValue this[TKey key]
        {
            get
            {
                TValue val;
                if (!base.TryGetValue(key, out val))  // key does not exist
                {
                    val = createDefault(key);
                    base[key] = val;
                }
                return val;
            }
            set
            {
                base[key] = value;
            }
        }
        #endregion
    }
}
