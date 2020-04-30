/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/28/2020 10:29:46 AM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication.Utilities
{
    /// <summary>
    /// countdown timer for message request
    /// </summary>
    public class TCounter
    {
        public TMessage RequestMsg { get; private set; }

        public TMessage ResponseMsg { get; private set; }

        public int CountDown { get; set; }  // unit: ms

        public bool IsReplied { get; private set; }

        /// <summary>
        /// there are two cases the counter turns to timeout
        /// <para>1、the property 'CountDown' decreases to 0</para>
        /// <para>2、the network connection is broken</para>
        /// </summary>
        public bool IsTimeOut => CountDown <= 0;

        private Agent agent;

        internal TCounter(TMessage msg, int timeout, Agent agent)
        {
            RequestMsg = msg;
            CountDown = timeout;
            IsReplied = false;
            this.agent = agent;
        }
        
        internal void SetReply(TMessage reply)
        {
            ResponseMsg = reply;
            CountDown = int.MaxValue;
            IsReplied = true;
        }
        /// <summary>
        /// invoked by watch dog，return true when timeout
        /// </summary>
        /// <param name="nStep"></param>
        /// <returns></returns>
        internal bool Decrease(int nStep)
        {
            if (agent.IsConnected)
            {
                CountDown -= nStep;
            }
            else
            {
                CountDown = 0;  // timeout
            }

            return CountDown <= 0;
        }

        /// <summary>
        /// wait for response, return false when timeout
        /// </summary>
        /// <returns></returns>
        public bool WaitReply()
        {
            var span = 1;
            for (; ; )
            {
                Thread.Sleep(span);
                lock (this)
                {
                    if (IsReplied)
                    {
                        break;
                    }
                    else if (CountDown <= 0)  // 超时
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
