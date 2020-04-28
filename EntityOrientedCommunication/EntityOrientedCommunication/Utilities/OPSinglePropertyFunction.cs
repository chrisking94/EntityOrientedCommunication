/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：8/1/2019 5:17:11 PM
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
using System.Reflection;

namespace EntityOrientedCommunication.Utilities
{
    /// <summary>
    /// 通过属性类型所带的函数来判断是否匹配
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class OPSinglePropertyFunction : OPSingleProperty
    {
        #region data
        #region property
        #endregion

        #region field
        [JsonProperty]
        private string methodName;

        [JsonProperty]
        private JsonList<object> paras;
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected OPSinglePropertyFunction() { }

        /// <summary>
        /// bool 'propertyName'.Value.'methodName'(paras[]);
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="methodName"></param>
        /// <param name="paras"></param>
        public OPSinglePropertyFunction(string propertyName, string methodName, params object[] paras) : base(propertyName)
        {
            this.methodName = methodName;
            this.paras = new JsonList<object>(paras);
        }
        #endregion

        #region interface
        #endregion

        #region private
        protected override bool MatchProperty(object obj, PropertyInfo propertyInfo, object propertyValue)
        {
            // check method
            var method = propertyInfo.PropertyType.GetMethod(methodName, paras.GetItemsType().ToArray());

            if (method == null)
            {
                throw new Exception($"{propertyInfo.PropertyType} 类型没有定义方法 {methodName}({string.Join(", ", paras.GetItemsType().Select(t => t.Name))})");
            }

            if (method.ReturnType != typeof(bool))
            {
                throw new Exception($"{propertyInfo.PropertyType}.{methodName}() 返回值是 {method.ReturnType} 类型，要求是 {typeof(bool)} 类型");
            }

            var bMatch = (bool)method.Invoke(propertyValue, paras.ToArray());

            return bMatch;
        }
        #endregion
    }
}
