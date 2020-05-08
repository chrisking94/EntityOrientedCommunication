/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 5/8/2020 2:26:33 PM					
\********************************************************/
using EntityOrientedCommunication.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication.Server
{
    /// <summary>
    /// route for letters
    /// </summary>
    public class Router
    {
        #region data
        #region property
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        #endregion

        #region interface
        public List<MailRouteInfo> RouteRecipient(ILetter letter, IEnumerable<User> serverUsers)
        {
            var allRecipientInfos = new List<MailRouteInfo>();
            var sInfo = MailRouteInfo.Parse(letter.Sender)[0];

            foreach (var rInfo in MailRouteInfo.Parse(letter.Recipient))
            {
                if (rInfo.UserName.ToLower() == "all")  // to all,  broadcast
                {
                    foreach (var sUser in serverUsers)
                    {
                        if (sUser.Name != sInfo.UserName)  // sender is not included
                        {
                            allRecipientInfos.Add(new MailRouteInfo(sUser.Name, rInfo.ReceiverEntityNames));
                        }
                    }
                }
                else
                {
                    allRecipientInfos.Add(rInfo);
                }
            }

            return allRecipientInfos;
        }
        #endregion

        #region private
        #endregion
    }
}
