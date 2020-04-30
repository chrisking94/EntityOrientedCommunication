/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：8/1/2019 5:25:30 PM
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

namespace EntityOrientedCommunication.Utilities
{
    /// <summary>
    /// carry the 'Type' information along an object
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ObjectMarker
    {
        #region data
        #region property
        internal Type Type => type;
        #endregion

        #region field
        [JsonProperty]
        private object obj;

        [JsonProperty]
        private Type type;
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected ObjectMarker() { }

        public ObjectMarker(object obj)
        {
            this.obj = obj;
            if (obj != null)
            {
                type = obj.GetType();
            }
        }
        #endregion

        #region interface
        public object Restore()
        {
            if (obj != null && obj.GetType() != type)
            {
                obj = type.Cast(obj);
            }

            return obj;
        }

        public override string ToString()
        {
            return $"{type.FullName}, {obj}";
        }
        #endregion

        #region private
        #endregion
    }
}
