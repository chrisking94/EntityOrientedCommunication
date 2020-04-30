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
    /// match a single property/field
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class OPSingleProperty : IObjectPattern
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
        public bool Match(object obj)
        {
            var type = obj.GetType();
            var propInfo = type.GetProperty(propertyName);

            if (propInfo == null)
            {
                var fieldInfo = type.GetField(propertyName);
                if (fieldInfo == null)
                {
                    throw new Exception($"'{obj.GetType()}' does not contain a property or field named '{propertyName}'");
                }
                var value = fieldInfo.GetValue(obj);

                return MatchProperty(obj, fieldInfo.FieldType, value);
            }
            else
            {
                var value = propInfo.GetValue(obj);

                return MatchProperty(obj, propInfo.PropertyType, value);
            }
        }
        #endregion

        #region private
        protected abstract bool MatchProperty(object obj, Type propertyType, object propertyValue);
        #endregion
    }
}
