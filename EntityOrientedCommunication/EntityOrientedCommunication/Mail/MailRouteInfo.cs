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

        public List<string> EntityNames { get; private set; }
        #endregion
        #endregion

        #region constructor
        public MailRouteInfo(string username, List<string> entityNames)
        {
            this.UserName = username;
            this.EntityNames = new List<string>(entityNames);
        }

        public MailRouteInfo(MailRouteInfo copyFrom)
        {
            this.UserName = copyFrom.UserName;
            this.EntityNames = new List<string>(copyFrom.EntityNames);
        }

        /// <summary>
        /// format: type1@Tom;type2@Marry;type3, type4@Jerry
        /// </summary>
        /// <param name="strInfo"></param>
        /// <returns></returns>
        public static List<MailRouteInfo> Parse(string strInfo)
        {
            var dictUser2Info = new Dictionary<string, MailRouteInfo>();
            var sb = new StringBuilder(64);
            var entityNames = new List<string>(10);
            var userNames = new List<string>(10);
            var parsingList = entityNames;

            for (var i = 0; i <= strInfo.Length; ++i)
            {
                var chr = i == strInfo.Length ? ';' : strInfo[i];  // sentinel

                switch(chr)
                {
                    case ',':
                        if (sb.Length > 0)
                        {
                            parsingList.Add(sb.ToString());
                            sb.Clear();
                        }
                        break;
                    case '@':  // entity@user
                        if (sb.Length > 0)
                        {
                            parsingList.Add(sb.ToString());
                            sb.Clear();
                        }

                        parsingList = userNames;  // switch to user-name mode
                        break;
                    case ';':  // a set of route info
                        if (sb.Length > 0)
                        {
                            parsingList.Add(sb.ToString());
                            sb.Clear();
                        }

                        // create rotue infos
                        if (userNames.Count > 0 &&
                            entityNames.Count > 0)
                        {
                            foreach (var user in userNames)
                            {
                                if (dictUser2Info.ContainsKey(user))
                                {
                                    dictUser2Info[user].EntityNames.AddRange(entityNames);
                                }
                                else
                                {
                                    dictUser2Info[user] = new MailRouteInfo(user, entityNames);
                                }
                            }
                        }

                        userNames.Clear();  // reset
                        entityNames.Clear();  // reset
                        parsingList = entityNames;  // switch to entity-name mode
                        break;
                    case ' ':  // white chars
                    case '\t':
                    case '\r':
                    case '\n':
                        break;  // ignore
                    default:
                        sb.Append(chr);
                        break;
                }
            }

            var list = dictUser2Info.Values.ToList();

            foreach (var info in list)
            {
                info.EntityNames = info.EntityNames.Distinct().ToList();
            }

            return list;
        }
        #endregion

        #region interface
        public string ToLiteral()
        {
            return $"{string.Join(",", this.EntityNames.ToArray())}@{this.UserName}";
        }

        /// <summary>
        /// merge the duplicate route infos
        /// </summary>
        /// <param name="infos"></param>
        /// <returns></returns>
        public static List<MailRouteInfo> Format(IEnumerable<MailRouteInfo> infos)
        {
            var dictUser2Info = new Dictionary<string, List<string>>();

            foreach (var info in infos)
            {
                if (dictUser2Info.ContainsKey(info.UserName))
                {
                    dictUser2Info[info.UserName].AddRange(info.EntityNames);
                }
                else
                {
                    var types = new List<string>(info.EntityNames);
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

        public override string ToString()
        {
            return $"route: {this.ToLiteral()}";
        }
        #endregion

        #region private
        #endregion
    }
}
