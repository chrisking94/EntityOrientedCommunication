/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/23/2020 3:24:38 PM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOrientedCommunication.Mail
{
    /// <summary>
    /// route information of a letter
    /// </summary>
    public class MailRouteInfo
    {
        #region data
        #region property
        #endregion

        #region field
        public readonly string UserName;

        public List<string> ReceiverEntityNames { get; private set; }
        #endregion
        #endregion

        #region constructor
        public MailRouteInfo(string username, List<string> receiverTypeFullNames)
        {
            this.UserName = username;
            this.ReceiverEntityNames = receiverTypeFullNames;
        }

        /// <summary>
        /// type1@Tom;type2@Marry;type3, type4@Jerry
        /// </summary>
        /// <param name="strInfo"></param>
        /// <returns></returns>
        public static List<MailRouteInfo> Parse(string strInfo)
        {
            var dictUser2Info = new Dictionary<string, MailRouteInfo>();

            foreach (var line in strInfo.Split(';'))
            {
                var sections = line.Split('@');
                if (sections.Length == 2)
                {
                    var typeNames = ParseList(sections[0]);

                    foreach (var user in ParseList(sections[1]))
                    {
                        if(dictUser2Info.ContainsKey(user))
                        {
                            dictUser2Info[user].ReceiverEntityNames.AddRange(typeNames);
                        }
                        else
                        {
                            dictUser2Info[user] = new MailRouteInfo(user, typeNames);
                        }
                    }
                }
                else
                {
                    return null;
                }
            }

            var list = dictUser2Info.Values.ToList();

            foreach(var info in list)
            {
                info.ReceiverEntityNames = info.ReceiverEntityNames.Distinct().ToList();
            }

            return list;
        }
        #endregion

        #region interface
        public string ToLiteral()
        {
            return $"{string.Join(",", this.ReceiverEntityNames.ToArray())}@{this.UserName}";
        }

        public static List<MailRouteInfo> Format(IEnumerable<MailRouteInfo> infos)
        {
            var dictUser2Info = new Dictionary<string, List<string>>();

            foreach (var info in infos)
            {
                if (dictUser2Info.ContainsKey(info.UserName))
                {
                    dictUser2Info[info.UserName].AddRange(info.ReceiverEntityNames);
                }
                else
                {
                    var types = new List<string>(info.ReceiverEntityNames);
                    dictUser2Info[info.UserName] = types;
                }
            }

            var list = new List<MailRouteInfo>(dictUser2Info.Count);
            foreach (var kv in dictUser2Info)
            {
                var info = new MailRouteInfo(kv.Key, kv.Value.Distinct().ToList());
                list.Add(info);
            }
            return list;
        }

        /// <summary>
        /// merge MailRouteInfo object to literal route information
        /// </summary>
        /// <param name="infos"></param>
        /// <returns></returns>
        public static string ToLiteral(IEnumerable<MailRouteInfo> infos)
        {
            return string.Join(";", infos.Select(info => info.ToLiteral()));
        }
        #endregion

        #region private
        /// <summary>
        /// list has a literal denotation likes 'a, b, c', the whitespaces are ignored.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static List<string> ParseList(string str)
        {
            var sb = new StringBuilder(256);
            var itemList = new List<string>();

            for (var i = 0; i <= str.Length; ++i)
            {
                var ch = i == str.Length ? ',' : str[i];  // sentinel
                if (char.IsWhiteSpace(ch))  // ignore
                {
                    // pass
                }
                else if (ch == ',')  // split
                {
                    if (sb.Length > 0)
                    {
                        itemList.Add(sb.ToString());
                        sb = new StringBuilder(256);
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }

            return itemList;
        }
        #endregion
    }
}
