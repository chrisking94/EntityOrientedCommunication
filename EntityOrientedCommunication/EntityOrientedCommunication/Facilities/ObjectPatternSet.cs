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

namespace EntityOrientedCommunication.Facilities
{
    internal enum PatternMatchType
    {
        All,
        Any,
    }

    /// <summary>
    /// serializable pattern set
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal class ObjectPatternSet
    {
        #region data
        #region property
        [JsonProperty]
        public PatternMatchType MatchType { get; set; }
        #endregion

        #region field
        [JsonProperty]
        private List<IObjectPattern> patterns;
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected ObjectPatternSet() { }

        public ObjectPatternSet(int capacity)
        {
            patterns = new List<IObjectPattern>(capacity);
        }

        public ObjectPatternSet(params IObjectPattern[] patterns) : this(patterns.Length)
        {
            AddRange(patterns);
        }
        #endregion

        #region interface
        public void Add(IObjectPattern pattern)
        {
            patterns.Add(pattern);
        }

        public void AddRange(IEnumerable<IObjectPattern> patterns)
        {
            foreach (var pattern in patterns)
            {
                Add(pattern);
            }
        }

        /// <summary>
        /// determine whether an 'obj' meets all of the conditions in this pattern set
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool All(object obj)
        {
            return patterns.All(p => p.Match(obj));
        }

        /// <summary>
        /// determine whether an 'obj' meets any of the conditions in this pattern set
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Any(object obj)
        {
            return patterns.Any(p => p.Match(obj));
        }

        /// <summary>
        /// determine whether an 'obj' meets '[MatchType]' of the conditions in this pattern set
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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
