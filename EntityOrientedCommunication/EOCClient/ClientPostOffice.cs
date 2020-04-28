/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/24/2020 9:24:11 AM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Mail;

namespace EOCClient
{
    public enum PostOfficeEventType
    {
        Prompt,
        Error,
    }

    public class PostOfficeEventArgs : EventArgs
    {
        public readonly PostOfficeEventType type;

        public readonly string message;

        public PostOfficeEventArgs(PostOfficeEventType type, string message)
        {
            this.type = type;
            this.message = message;
        }
    }

    public delegate void PostOfficeEventHandler(object sender, PostOfficeEventArgs args);

    public class ClientPostOffice
    {
        #region data
        public PostOfficeEventHandler PostOfficeEvent;

        #region property
        public string OfficeName => dispatcher.ClientName;
        #endregion

        #region field
        private Dictionary<string, ClientMailBox> dictEntityName2MailBox;

        private IClientMailDispatcher dispatcher;
        #endregion
        #endregion

        #region constructor
        public ClientPostOffice(IClientMailDispatcher dispatcher)
        {
            dictEntityName2MailBox = new Dictionary<string, ClientMailBox>(1);
            this.dispatcher = dispatcher;
        }
        #endregion

        #region interface
        /// <summary>
        /// 注册一个信件接收器，一种接收器类型只能同时注册一个实例。增加离线注册
        /// </summary>
        /// <param name="receiver"></param>
        /// <returns></returns>
        public ClientMailBox Register(IMailReceiver receiver)
        {
            var mailBox = new ClientMailBox(receiver, this);

            lock (dictEntityName2MailBox)
            {
                if (dictEntityName2MailBox.ContainsKey(mailBox.EntityName))
                {
                    throw new Exception($"there's already a receiver named '{mailBox.EntityName}' registered, unregister it firstly please.");
                }

                dictEntityName2MailBox[mailBox.EntityName] = mailBox;
            }

            dispatcher.Online(mailBox);

            return mailBox;
        }

        public bool Contains(string typeFullName)
        {
            lock(dictEntityName2MailBox)
                return dictEntityName2MailBox.ContainsKey(typeFullName);
        }

        public void Pickup(TMLetter letter)
        {
            ClientMailBox mailBox = null;

            lock (dictEntityName2MailBox)
            {
                var routeInfo = MailRouteInfo.Parse(letter.Recipient)[0];
                foreach (var receiverType in routeInfo.ReceiverEntityNames)
                {
                    if (dictEntityName2MailBox.ContainsKey(receiverType))
                    {
                        mailBox = dictEntityName2MailBox[receiverType];
                    }
                    else
                    {
                        PostOfficeEvent?.Invoke(this,
                            new PostOfficeEventArgs(PostOfficeEventType.Error,
                            $"当前'{nameof(ClientPostOffice)}'注册了'{receiverType}'类型收件器但是没有找到该类收件器实例，接收信件失败"));
                    }
                }
            }

            mailBox?.Receive(letter);
        }

        internal void Send(TMLetter letter)
        {
            var routeInfos = MailRouteInfo.Parse(letter.Recipient);
            if (routeInfos == null)
            {
                throw new Exception($"cannot deliver letter '{letter.Title}', the '{nameof(letter.Recipient)}' of which is not in a valid format.");
            }

            var teleRouteInfos = new List<MailRouteInfo>(routeInfos.Count);
            MailRouteInfo localRouteInfo = null;
            foreach (var info in routeInfos)  // find letters sent to local host
            {
                if (info.UserName == "localhost")
                {
                    localRouteInfo = info;  // only one
                }
                else
                {
                    teleRouteInfos.Add(info);
                }
            }

            if (teleRouteInfos.Count != routeInfos.Count)  // the route information has been changed
            {
                letter.Recipient = MailRouteInfo.ToLiteral(teleRouteInfos);  // new tele-recipient info
            }

            if (letter.Recipient != "")
            {
                // send to tele-entity
                this.dispatcher.Dispatch(letter);
            }

            if (localRouteInfo != null)
            {
                // send to local-entity
                var copy = new TMLetter(letter);
                copy.Recipient = localRouteInfo.ToLiteral();
                Pickup(letter);
            }
        }

        public void ActivateAll()
        {
            lock (dictEntityName2MailBox)
            {
                // 注册接收器到服务器
                foreach (var reciver in this.dictEntityName2MailBox.Values)
                {
                    this.dispatcher.Online(reciver);
                }
            }
        }

        public void Destroy()
        {
            lock (dictEntityName2MailBox)
            {
                foreach (var mailBox in dictEntityName2MailBox.Values)
                {
                    mailBox.Destroy();
                }
                dictEntityName2MailBox.Clear();
            }
        }
        #endregion

        #region private
        #endregion
    }
}
