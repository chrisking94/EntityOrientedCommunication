/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：5/23/2019 9:41:17 AM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication;

namespace EntityOrientedCommunication.Server
{
    public partial class Server
    {
        private class ServerTransaction
        {
            #region data
            #region property
            public string Name { get; private set; }

            /// <summary>
            /// 单位：ms
            /// </summary>
            public int Interval { get; private set; }

            /// <summary>
            /// 设成true会紧急执行一次，执行后变成false
            /// </summary>
            public bool EmergencyDo;
            #endregion

            #region field
            private Action action;

            private Queue<Action> executionOnceQ;
            #endregion
            #endregion

            #region constructor
            public ServerTransaction(string name, int interval, Action action)
            {
                Name = name;
                Interval = interval;
                this.action = action;
                executionOnceQ = new Queue<Action>(4);
            }
            #endregion

            #region interface
            public void Do()
            {
                action();

                lock (executionOnceQ)
                {
                    while (executionOnceQ.Count > 0)
                    {
                        var once = executionOnceQ.Dequeue();

                        once();
                    }
                }
            }

            public void RunOnce(Action executionOnce)
            {
                lock (executionOnceQ)
                {
                    executionOnceQ.Enqueue(executionOnce);
                }
            }
            #endregion

            #region private
            #endregion
        }
    }
}
