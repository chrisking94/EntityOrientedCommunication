/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：8/1/2019 5:05:29 PM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication;
using Newtonsoft.Json;

namespace EntityOrientedCommunication.Utilities
{
    public enum PatternMatchType
    {
        All,
        Any,
    }

    /// <summary>
    /// 可序列化传输
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ObjectPatternSet
    {
        #region data
        #region property
        [JsonProperty]
        public PatternMatchType MatchType { get; set; }
        #endregion

        #region field
        [JsonProperty]
        private List<ObjectPattern> patterns;
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected ObjectPatternSet() { }

        public ObjectPatternSet(int capacity)
        {
            patterns = new List<ObjectPattern>(capacity);
        }

        public ObjectPatternSet(params ObjectPattern[] patterns) : this(patterns.Length)
        {
            AddRange(patterns);
        }
        #endregion

        #region interface
        public void Add(ObjectPattern pattern)
        {
            patterns.Add(pattern);
        }

        public void AddRange(IEnumerable<ObjectPattern> patterns)
        {
            foreach (var pattern in patterns)
            {
                Add(pattern);
            }
        }

        /// <summary>
        /// 判断obj是否满足该集合中所有模式
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool All(object obj)
        {
            return patterns.All(p => p.Match(obj));
        }

        /// <summary>
        /// 判断obj是否满足任意一个模式
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Any(object obj)
        {
            return patterns.Any(p => p.Match(obj));
        }

        public bool Matches(object obj)
        {
            if (MatchType == PatternMatchType.All)
            {
                return All(obj);
            }
            else if (MatchType == PatternMatchType.Any)
            {
                return Any(obj);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            return $"PatternSet, Count={patterns.Count}";
        }
        #endregion

        #region private
        #endregion
    }
}
