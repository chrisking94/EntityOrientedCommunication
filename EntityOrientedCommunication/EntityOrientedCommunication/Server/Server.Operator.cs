/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：5/16/2019 5:25:57 PM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication;
using EntityOrientedCommunication;
using EOCServer;

namespace EntityOrientedCommunication.Server
{
    public partial class Server
    {
        private class ServerOperator : TapaOperator
        {
            #region data
            #region property
            public bool IsOnline { get; internal set; }

            public ServerPostOffice PostOffice { get; protected set; }

            public ServerOperatorManager Manager => manager;
            #endregion

            #region field
            protected ServerOperatorManager manager;
            #endregion
            #endregion

            #region constructor
            public ServerOperator(string name, string detail = "") : base(name, detail)
            {
                PostOffice = new ServerPostOffice(this);
            }

            public ServerOperator(IUser staff)
            {
                ID = staff.ID;
                Name = staff.Name;
                SetPassword(staff.Password);
                Detail = staff.Detail;
                NickName = staff.NickName;
                PostOffice = new ServerPostOffice(this);
            }
            #endregion

            #region interface
            internal void SetManager(ServerOperatorManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region private
            #endregion
        }
    }
}
