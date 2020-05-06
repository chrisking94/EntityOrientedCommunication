/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：8/1/2019 5:34:39 PM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication;
using Newtonsoft.Json;
using System.Collections;

namespace EntityOrientedCommunication.Facilities
{
    /// <summary>
    /// to solve the serilization and deserilization problem of value type items in List&lt;object&gt;
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class JsonList<T> : IEnumerable<T>
    {
        #region data
        #region property
        public int Count => items.Count;
        #endregion

        #region field
        [JsonProperty]
        private List<ObjectMarker> items;
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected JsonList() { }

        public JsonList(IEnumerable<T> collection)
        {
            items = new List<ObjectMarker>(collection.Count());

            AddRange(collection);
        }
        #endregion

        #region interface
        public void Add(T item)
        {
            if (item is ObjectMarker)
            {
                items.Add(item as ObjectMarker);
            }
            else
            {
                items.Add(new ObjectMarker(item));
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in items)
            {
                yield return (T)item.Restore();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<Type> GetItemsType()
        {
            return items.Select(item => item.Type);
        }

        public override string ToString()
        {
            return $"Count={Count}";
        }
        #endregion

        #region private
        #endregion
    }
}
