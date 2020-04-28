/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：8/1/2019 5:10:12 PM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication;
using Newtonsoft.Json;
using System.Reflection;

namespace EntityOrientedCommunication.Utilities
{
    /// <summary>
    /// 单属性匹配
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class OPSingleProperty : ObjectPattern
    {
        #region data
        #region property
        #endregion

        #region field
        [JsonProperty]
        protected string propertyName;
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected OPSingleProperty() { }

        protected OPSingleProperty(string propertyName)
        {
            this.propertyName = propertyName;
        }
        #endregion

        #region interface
        public override bool Match(object obj)
        {
            var propInfo = obj.GetType().GetProperty(propertyName);

            if (propInfo == null)
            {
                throw new Exception($"{obj.GetType()} 类型没有属性 {propertyName}");
            }
            else
            {
                var value = propInfo.GetValue(obj);

                return MatchProperty(obj, propInfo, value);
            }
        }
        #endregion

        #region private
        protected abstract bool MatchProperty(object obj, PropertyInfo propertyInfo, object propertyValue);
        #endregion
    }
}
