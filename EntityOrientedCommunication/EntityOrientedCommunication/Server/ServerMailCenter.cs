using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EntityOrientedCommunication.Facilities;
using EntityOrientedCommunication.Mail;
using EntityOrientedCommunication.Messages;
using EOCServer;

namespace EntityOrientedCommunication.Server
{
    /// <summary>
    /// user management, mail route
    /// </summary>
    public sealed class ServerMailCenter
    {
        #region data
        #region property
        public int Count => dictName2User.Count;
        #endregion

        #region field
        private Dictionary<string, ServerUser> dictName2User;

        private ReaderWriterLockSlim rwlsDictName2User;

        private Server server;

        private Router router;
        #endregion
        #endregion

        #region constructor
        public ServerMailCenter(Server server)
        {
            dictName2User = new Dictionary<string, ServerUser>(16);
            rwlsDictName2User = new ReaderWriterLockSlim();

            this.server = server;
            this.router = new Router();
        }
        #endregion

        #region interface
        public bool Contains(string name)
        {
            rwlsDictName2User.EnterReadLock();
            var bContains = dictName2User.ContainsKey(name);
            rwlsDictName2User.ExitReadLock();

            return bContains;
        }

        internal ServerUser GetUser(string name)
        {
            ServerUser user;

            rwlsDictName2User.EnterReadLock();
            dictName2User.TryGetValue(name, out user);
            rwlsDictName2User.ExitReadLock();

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

            rwlsDictName2User.EnterReadLock();
            if (dictName2User.TryGetValue(name, out user))
            {
                if (user.Password != password)
                {
                    user = null;  // password not matched
                }
            }
            rwlsDictName2User.ExitReadLock();

            return user;
        }

        public List<string> GetAllUserNames()
        {
            rwlsDictName2User.EnterReadLock();
            var allUsers = dictName2User.Values.Select(s => s.Name).ToList();
            rwlsDictName2User.ExitReadLock();

            return allUsers;
        }

        internal void Register(ServerUser serverUser)
        {
            rwlsDictName2User.EnterWriteLock();
            dictName2User[serverUser.Name] = serverUser;
            rwlsDictName2User.ExitWriteLock();

            serverUser.SetMailCenter(this);
        }

        /// <summary>
        /// update user info, if the specified user does not exist(identified by Username), then a new user will be created
        /// <para>attention: only 1 thread could invoke this method at the same time</para>
        /// </summary>
        /// <param name="opr"></param>
        public void Update(IUser iuser)
        {
            rwlsDictName2User.EnterUpgradeableReadLock();
            if (dictName2User.ContainsKey(iuser.Name))
            {
                var oldUser = dictName2User[iuser.Name];
                oldUser.UpdateServerUserInfo(iuser);
            }
            else
            {
                var newUser = new ServerUser(iuser);
                Register(newUser);
            }
            rwlsDictName2User.ExitUpgradeableReadLock();
        }

        public bool IsOnline(string username)
        {
            rwlsDictName2User.EnterReadLock();
            ServerUser user;
            var bOnline = dictName2User.TryGetValue(username, out user);
            if (bOnline) bOnline = user.IsOnline;
            rwlsDictName2User.ExitReadLock();

            return bOnline;
        }
        #endregion

        #region EOC
        public void SetRouter(Router router)
        {
            this.router = router;
        }

        /// <summary>
        /// push the letter into corresponding postoffice
        /// </summary>
        /// <param name="letter"></param>
        /// <returns>error message, null if there is no error</returns>
        internal EMLetter Deliver(EMLetter letter)
        {
            // routing
            rwlsDictName2User.EnterReadLock();
            var allRecipientInfos = this.router.RouteRecipient(letter, this.dictName2User.Values);
            rwlsDictName2User.ExitReadLock();
            allRecipientInfos = MailRouteInfo.Format(allRecipientInfos);
            var notExistsUserRouteInfos = allRecipientInfos.Where(info => !this.Contains(info.UserName)).ToList();

            if (letter.HasFlag(StatusCode.Get))
            {  // check recipient
                if (allRecipientInfos.Count > 1)
                {
                    throw new Exception($"letter of type '{nameof(StatusCode.Get)}' should not have multiple recipients.");
                }
            }

            if (notExistsUserRouteInfos.Count > 0)
            {
                throw new Exception($"user '{string.Join("; ", notExistsUserRouteInfos.Select(info => info.UserName).ToArray())}' not exists，faild to send letter");  // operation failed
            }

            foreach (var rInfo in allRecipientInfos)
            {
                var recipientOpr = this.GetUser(rInfo.UserName);
                var result = recipientOpr.PostOffice.Push(letter, rInfo);

                if (letter.HasFlag(StatusCode.Get))
                {
                    return result;  // Get
                }
            }

            return null;  // Post, SafePost
        }
        #endregion

        #region private
        #endregion
    }
}
