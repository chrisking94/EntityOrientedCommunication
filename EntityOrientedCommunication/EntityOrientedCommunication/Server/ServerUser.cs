/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：5/16/2019 5:25:57 PM
 * ============================================================================================*/
using EOCServer;

namespace EntityOrientedCommunication.Server
{
    /// <summary>
    /// has more functinalities than 'User'
    /// </summary>
    internal class ServerUser : User
    {
        #region data
        #region property
        public bool IsOnline { get; internal set; }

        public ServerPostOffice PostOffice { get; protected set; }

        public ServerUserManager Manager => manager;
        #endregion

        #region field
        protected ServerUserManager manager;
        #endregion
        #endregion

        #region constructor
        public ServerUser(string name, string detail = "") : base(name, detail)
        {
            PostOffice = new ServerPostOffice(this);
        }

        public ServerUser(IUser staff)
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
        internal void SetManager(ServerUserManager manager)
        {
            this.manager = manager;
        }
        #endregion

        #region private
        #endregion
    }
}
