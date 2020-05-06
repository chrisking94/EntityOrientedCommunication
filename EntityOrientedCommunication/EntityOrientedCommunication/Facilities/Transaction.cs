/* ============================================================================================
										☢RADIATION RISK            
 * author		：chris
 * create time	：10/18/2019 10:14:43 AM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace EntityOrientedCommunication.Facilities
{
    public class Transaction : CycleUpdater
    {
        #region data
        #region property
        public string Name { get; private set; }
        #endregion

        #region field
        internal readonly Action action;

        private TransactionPool pool;
        #endregion
        #endregion

        #region constructor
        public Transaction(Action action, int interval, string name) : base(interval)
        {
            this.action = action;
            this.Name = name;

            this.Register();
        }

        internal void SetPool(TransactionPool pool)
        {
            this.pool = pool;
        }
        #endregion

        #region interface
        protected override void Update()
        {
            try
            {
                this.action();
            }
            catch (Exception ex)
            {
                this.pool?.OnError(this, ex);

                this.Unregister();  // stop running when error occurred
            }
        }
        #endregion

        #region private
        public override void Destroy()
        {
            base.Destroy();
            this.pool = null;
        }
        #endregion
    }
}
