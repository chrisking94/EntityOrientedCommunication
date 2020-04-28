using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Diagnostics;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication
{
    /// <summary>
    /// 装有许多独立小部件
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class TMBox<T> : TMObject<List<T>>
    {
        #region property
        public List<T> Items => Object;

        public int Count => Items.Count;
        #endregion

        #region constructor
        [JsonConstructor]
        protected TMBox() { }

        protected TMBox(Envelope envelope, int capacity = 0) : base(envelope, new List<T>(capacity))
        {
        }

        protected TMBox(TMessage toReply, int capacity) : base(toReply, new List<T>(capacity))
        {
        }
        #endregion

        #region interface
        public T this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }
        public virtual void Add(T item)
        {
            Items.Add(item);
        }
        public override string ToString()
        {
            return Format("TBox", $"Count={Count}");
        }
        #endregion
    }
}
