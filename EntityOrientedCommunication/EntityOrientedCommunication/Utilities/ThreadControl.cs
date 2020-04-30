/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/28/2020 10:34:49 AM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EntityOrientedCommunication.Utilities
{
    public class ThreadControl
    {
        private string name;
        private Thread thread;
        private Action<ThreadControl> threadStart;
        private Action stopThread;

        public bool IsRunning { get; private set; }

        /// <summary>
        /// set it to true to inform 'threadStart' to exit safely, 'threadStart' should set it to false when it exits safely by invoking 'SetAbortedFlag' function.
        /// </summary>
        public bool SafelyTerminating { get; internal set; }

        internal ThreadControl(string name, Action<ThreadControl> threadStart, Action stopThread = null)
        {
            this.name = name;
            this.threadStart = threadStart;
            this.stopThread = stopThread;
        }

        /// <summary>
        /// start in new thread
        /// </summary>
        public void Start()
        {
            lock (this)
            {
                if (thread != null && thread.IsAlive)
                {
                    throw new Exception($"必须先关闭{name}线程");
                }

                thread = new Thread(__threadStart);
                thread.IsBackground = true;
                thread.Start(this);

                IsRunning = true;
            }
        }

        /// <summary>
        /// notify 'threadStart' to abort, then wait till it stops
        /// </summary>
        public void SafeAbort()
        {
            lock (this)
            {
                if (IsRunning)
                {
                    SafelyTerminating = true;
                    stopThread?.Invoke();

                    while (SafelyTerminating) Thread.Sleep(1);

                    thread = null;
                }
            }
        }

        /// <summary>
        /// notify 'threadStart' to abort, but do not wait
        /// </summary>
        public void AsyncSafeAbort()
        {
            lock (this)
            {
                if (IsRunning)
                {
                    SafelyTerminating = true;
                    stopThread?.Invoke();

                    thread = null;
                }
            }
        }

        /// <summary>
        /// invoked by 'threadStart' to tell this controller that it has aborted safely
        /// <para>warning: only 'threadStart' is entitled to invoke this method</para>
        /// </summary>
        internal void SetAbortedFlags()
        {
            SafelyTerminating = false;
            IsRunning = false;
        }

        private void __threadStart(object o)
        {
            threadStart(o as ThreadControl);
        }

        public override string ToString()
        {
            return $"ctrl[{name}], running={IsRunning}";
        }
    }
}
