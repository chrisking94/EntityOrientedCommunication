using System;
using System.Collections.Generic;
using System.Linq;
using EntityOrientedCommunication.Mail;
using EOCServer;

namespace EntityOrientedCommunication.Server
{
    public class ServerUserManager
    {
        #region data
        #region property
        public int Count => dictNameAndOperator.Count;
        #endregion

        #region field
        private Dictionary<string, ServerUser> dictNameAndOperator;

        private Server server;
        #endregion
        #endregion

        #region constructor
        public ServerUserManager(Server server)
        {
            dictNameAndOperator = new Dictionary<string, ServerUser>(16);

            this.server = server;
        }
        #endregion

        #region interface
        public bool Contains(string name)
        {
            lock (dictNameAndOperator)
            {
                return dictNameAndOperator.ContainsKey(name);
            }
        }

        internal ServerUser GetOperator(string name)
        {
            ServerUser opr = null;

            lock (dictNameAndOperator)
            {
                dictNameAndOperator.TryGetValue(name, out opr);
            }

            return opr;
        }

        /// <summary>
        /// returns null if the name or password has no match
        /// </summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        internal ServerUser GetOperator(string name, string password)
        {
            ServerUser opr = null;

            lock (dictNameAndOperator)
            {
                if (dictNameAndOperator.TryGetValue(name, out opr))
                {
                    if (opr.Password != password)
                    {
                        opr = null;  // password not matched
                    }
                }
            }

            return opr;
        }

        public IEnumerable<string> GetAllOperatorNames()
        {
            lock (dictNameAndOperator)
            {
                return dictNameAndOperator.Values.Select(s => s.Name);
            }
        }

        internal void Register(ServerUser serverUser)
        {
            lock (dictNameAndOperator)
            {
                dictNameAndOperator[serverUser.Name] = serverUser;
                serverUser.SetManager(this);
            }
        }

        /// <summary>
        /// register an user
        /// </summary>
        /// <param name="opr"></param>
        public void Register(IUser iuser)
        {
            var serverUser = new ServerUser(iuser);
            Register(serverUser);
        }
        #endregion

        #region EOC
        /// <summary>
        /// push the letter into corresponding postoffice
        /// </summary>
        /// <param name="letter"></param>
        /// <returns>error message, null if there is no error</returns>
        public string Deliver(EMLetter letter)
        {
            var allRecipientInfos = new List<MailRouteInfo>();
            var sInfo = MailRouteInfo.Parse(letter.Sender)[0];

            foreach (var rInfo in MailRouteInfo.Parse(letter.Recipient))
            {
                if (rInfo.UserName.ToLower() == "all")  // to all,  broadcast
                {
                    foreach (var oprName in server.UserManager.GetAllOperatorNames())
                    {
                        if (oprName != sInfo.UserName)  // sender is not included
                        {
                            allRecipientInfos.Add(new MailRouteInfo(oprName, rInfo.ReceiverEntityNames));
                        }
                    }
                }
                else
                {
                    allRecipientInfos.Add(rInfo);
                }
            }
            allRecipientInfos = MailRouteInfo.Format(allRecipientInfos);
            var notExistsUserRouteInfos = allRecipientInfos.Where(info => !this.Contains(info.UserName)).ToList();

            if (letter.LetterType == LetterType.EmergencyGet)
            {  // check recipient
                if (allRecipientInfos.Count > 1)
                {
                    return $"letter of type '{nameof(LetterType.EmergencyGet)}' should not have multiple recipients.";
                }
            }

            if (notExistsUserRouteInfos.Count > 0)
            {
                return $"user '{string.Join("; ", notExistsUserRouteInfos.Select(info => info.UserName).ToArray())}' not exists，faild to send letter";  // operation failed
            }

            if (letter.LetterType == LetterType.Emergency ||
                letter.LetterType == LetterType.EmergencyGet)
            {  // check receiver status
                var offlineOprRouteInfos = new List<MailRouteInfo>(allRecipientInfos.Count);
                foreach (var recipientInfo in allRecipientInfos)
                {
                    var opr = this.GetOperator(recipientInfo.UserName);
                    if (opr == null)
                    {
                        offlineOprRouteInfos.Add(new MailRouteInfo(recipientInfo));
                    }
                    else
                    {
                        var routeInfo = new MailRouteInfo(recipientInfo.UserName,
                            recipientInfo.ReceiverEntityNames.Where(ren => !opr.PostOffice.IsEntityOnline(ren)).ToList()
                            );
                        if (routeInfo.ReceiverEntityNames.Count > 0)
                        {
                            offlineOprRouteInfos.Add(routeInfo);
                        }
                    }
                }
                if (offlineOprRouteInfos.Count > 0)
                {
                    return $"entity '{MailRouteInfo.ToLiteral(offlineOprRouteInfos)}' is not online, falid to send emergency letter.";
                }
            }

            foreach (var rInfo in allRecipientInfos)
            {
                var recipientOpr = this.GetOperator(rInfo.UserName);
                recipientOpr.PostOffice.Push(letter, sInfo, rInfo);
            }

            return null;  // succeeded
        }
        #endregion

        #region private
        #endregion
    }
}
