/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：8/1/2019 5:05:17 PM
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
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class ObjectPattern
    {
        #region data
        #region property
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        [JsonConstructor]
        protected ObjectPattern() { }
        #endregion

        #region interface
        public abstract bool Match(object obj);
        #endregion

        #region private
        #endregion
    }
}
