/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/28/2020 2:30:50 PM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOrientedCommunication.Facilities
{
    internal static class TypeExtensions
    {
        /// <summary>
        /// convert target to object of 'type'
        /// </summary>
        /// <param name="type"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static object Cast(this Type type, object target)
        {
            if (target == null)
            {
                // pass
            }
            else
            {
                var orginalType = target.GetType();

                if (!type.IsAssignableFrom(orginalType))  // there is no need to convert inherited class to base class
                {
                    if (type.IsEnum)  // enum conversion
                    {
                        target = Enum.ToObject(type, target);
                    }
                    else
                    {
                        try
                        {
                            target = Convert.ChangeType(target, type);
                        }
                        catch  // invalid conversion
                        {
                            // implicit operator conversion first
                            var cast = type.GetMethod("op_Implicit", new Type[] { typeof(string) });
                            if (cast == null)
                            {  // inspect whether there is a 'Parse' method
                                cast = type.GetMethod("Parse", new Type[] { typeof(string) });
                            }
                            try
                            {
                                target = cast.Invoke(null, new object[] { target });
                            }
                            catch
                            {
                                throw new InvalidCastException($"unable to convert '{orginalType.FullName}' type object '{target}' to '{type.FullName}' type");
                            }
                        }
                    }
                }
            }

            return target;
        }

        public static T Cast<T>(this Type type, object target)
        {
            return (T)(type.Cast(target));
        }
    }
}
