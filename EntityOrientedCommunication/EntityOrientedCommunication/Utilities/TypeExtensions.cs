/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/28/2020 2:30:50 PM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOrientedCommunication.Utilities
{
    public static class TypeExtensions
    {
        /// <summary>
        /// 把target对象转成本类型
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

                if (!type.IsAssignableFrom(orginalType))  // 派生类不需要转成基类型
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
                            // 优先隐式转换
                            var cast = type.GetMethod("op_Implicit", new Type[] { typeof(string) });
                            if (cast == null)  // Parse解析
                            {
                                cast = type.GetMethod("Parse", new Type[] { typeof(string) });
                            }
                            try
                            {
                                target = cast.Invoke(null, new object[] { target });
                            }
                            catch
                            {
                                throw new InvalidCastException($"无法转换 '{orginalType.FullName}' 类型对象 '{target}' 成 '{type.FullName}' 类型");
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
