using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EntityOrientedCommunication.Facilities;
using EntityOrientedCommunication.Mail;
using EOCServer;

namespace EntityOrientedCommunication.Server
{
    public class ServerUserManager
    {
        #region data
        #region property
        public int Count => dictName2User.Count;
        #endregion

        #region field
        private Dictionary<string, ServerUser> dictName2User;

        private ReaderWriterLock rwlDictName2User;

        private Server server;
        #endregion
        #endregion

        #region constructor
        public ServerUserManager(Server server)
        {
            dictName2User = new Dictionary<string, ServerUser>(16);
            rwlDictName2User = new ReaderWriterLock();

            this.server = server;
        }
        #endregion

        #region interface
        public bool Contains(string name)
        {
            rwlDictName2User.AcquireReaderLock();
            var bContains = dictName2User.ContainsKey(name);
            rwlDictName2User.ReleaseReaderLock();

            return bContains;
        }

        internal ServerUser GetUser(string name)
        {
            ServerUser user;

            rwlDictName2User.AcquireReaderLock();
            dictName2User.TryGetValue(name, out user);
            rwlDictName2User.ReleaseReaderLock();

            return user;
        }

        /// <summary>
        /// returns null if the name or password has no match
        /// </summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        internal ServerUser GetUser(string name, string password)
        {
            ServerUser user;

            rwlDictName2User.AcquireReaderLock();
            if (dictName2User.TryGetValue(name, out user))
            {
                if (user.Password != password)
                {
                    user = null;  // password not matched
                }
            }
            rwlDictName2User.ReleaseReaderLock();

            return user;
        }

        public List<string> GetAllUserNames()
        {
            rwlDictName2User.AcquireReaderLock();
            var allUsers = dictName2User.Values.Select(s => s.Name).ToList();
            rwlDictName2User.ReleaseReaderLock();

            return allUsers;
        }

        internal void Register(ServerUser serverUser)
        {
            rwlDictName2User.AcquireWriterLock();
            dictName2User[serverUser.Name] = serverUser;
            rwlDictName2User.ReleaseWriterLock();

            serverUser.SetManager(this);
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
                    foreach (var oprName in server.UserManager.GetAllUserNames())
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
                    var opr = this.GetUser(recipientInfo.UserName);
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
                var recipientOpr = this.GetUser(rInfo.UserName);
                recipientOpr.PostOffice.Push(letter, sInfo, rInfo);
            }

            return null;  // succeeded
        }
        #endregion

        #region private
        #endregion
    }
}
