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

namespace EntityOrientedCommunication.Facilities
{
    /// <summary>
    /// 通过属性类型所带的函数来判断是否匹配
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    internal class OPSinglePropertyFunction : OPSingleProperty
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
        /// <para>e.g. assume object a has a property of 'string' type named 'Company', the parameters of this ctor are shown as the parameter comments</para>
        /// </summary>
        /// <param name="propertyName">one of the property name of target object.
        /// <para>it should be 'Company' in the function comment example</para></param>
        /// <param name="methodName">name of the PropertyValue's method which used to determine whether the property value meets the condition 
        /// <para>name 'Equals' of method 'bool Equals(string)' in the function comment example</para></param>
        /// <param name="paras">parameters for the PropertyValue's method
        /// <para>"abc" in the function comment example</para></param>
        public OPSinglePropertyFunction(string propertyName, string methodName, params object[] paras) : base(propertyName)
        {
            this.methodName = methodName;
            this.paras = new JsonList<object>(paras);
        }
        #endregion

        #region interface
        #endregion

        #region private
        protected override bool MatchProperty(object obj, Type PropertyType, object propertyValue)
        {
            // check method
            var method = PropertyType.GetMethod(methodName, paras.GetItemsType().ToArray());

            if (method == null)
            {
                if (method.ReturnType != typeof(bool))
                {
                    method = null;  // bool method expected
                }
            }

            if (method == null)
            {
                throw new Exception($"'{PropertyType}' does not define a method 'bool {methodName}({string.Join(", ", paras.GetItemsType().Select(t => t.Name))})'");
            }

            var bMatch = (bool)method.Invoke(propertyValue, paras.ToArray());

            return bMatch;
        }
        #endregion
    }
}
