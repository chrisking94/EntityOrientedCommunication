/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：5/16/2019 10:11:06 AM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace EntityOrientedCommunication
{
    public abstract class LoginAgent : Agent
    {
        #region data
        #region property
        public string Token { get; protected set; }

        public EOCUser User { get; protected set; }
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        protected LoginAgent()
        {
            User = new EOCUser();
        }
        #endregion

        #region interface
        public override void Destroy()
        {
            Token = null;

            base.Destroy();
        }
        #endregion

        #region private
        #endregion
    }
}
