/* ============================================================================================
										☢RADIATION RISK            
 * author		：chris
 * create time	：10/17/2019 2:36:57 PM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication;
using System.Threading;

namespace EntityOrientedCommunication.Utilities
{
    /// <summary>
    /// 'Update()' method of this class's instance will be invoked repeatedly in specified interval
    /// </summary>
    public abstract class CycleUpdater
    {
        #region data
        #region property
        /// <summary>
        /// 'Updater' will run repeatedly only when 'Registered' is true
        /// </summary>
        public bool Registered
        {
            get => timer != null;
            set
            {
                if (value) Register(); else Unregister();
            }
        }
        #endregion

        #region field
        /// <summary>
        /// 'TimerCallBackFunc' will not be invoked while 'bUpdating' is true
        /// </summary>
        private bool bUpdating;

        private Timer timer;

        private int interval;

        private object _timerLock = new object();
        #endregion
        #endregion

        #region constructor
        protected CycleUpdater(int interval)
        {
            this.interval = interval;
        }
        #endregion

        #region interface
        /// <summary>
        /// remember to invoke 'Destory()' when 'this' is no longer used.
        /// </summary>
        public virtual void Destroy()
        {
            Unregister();
        }
        #endregion

        #region private
        /// <summary>
        /// this method will be invoked in every control cycle
        /// </summary>
        protected abstract void Update();

        /// <summary>
        /// register this if not registered, each updater should be registered before using
        /// </summary>
        protected void Register()
        {
            if (this.interval == 0)
            {
                throw new Exception("interval must be greater than 0");
            }

            if (!Registered)
            {
                timer = new Timer(TimerCallBackFunc, this, 10, interval);
            }
        }

        /// <summary>
        /// unregister this if registered
        /// </summary>
        protected void Unregister()
        {
            if (Registered)
            {
                lock (this._timerLock)
                {
                    timer.Dispose();
                    timer = null;
                }
            }
        }

        private void TimerCallBackFunc(object o)
        {
            if (!bUpdating)  // update could only be invoked once all at once
            {
                lock (this._timerLock)  // affirm that at lease one complete update finished
                {
                    bUpdating = true;
                    this.Update();
                    bUpdating = false;
                }
            }
        }
        #endregion
    }
}
