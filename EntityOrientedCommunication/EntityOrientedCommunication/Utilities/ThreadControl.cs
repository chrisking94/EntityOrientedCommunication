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
        /// 设为true后通知threadStart安全退出，threadStart退出之后需要将该值恢复成false
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
        /// 通知相应线程结束运行，并等待停止操作完成
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
        /// 由被控制的线程调用，告诉控制器线程已安全退出
        /// <para>警告：其他程序不要調用這個方法</para>
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
