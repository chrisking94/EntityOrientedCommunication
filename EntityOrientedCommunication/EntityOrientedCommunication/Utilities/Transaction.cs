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

namespace EntityOrientedCommunication.Utilities
{
    public class TransactionErrorArgs : EventArgs
    {
        public string ErrorMessage => $"transaction '{transaction.Name}': {exception.Message}";

        public readonly Transaction transaction;

        public readonly Exception exception;

        public TransactionErrorArgs(Transaction transaction, Exception exception)
        {
            this.transaction = transaction;
            this.exception = exception;
        }
    }

    public delegate void TransactionErrorEventHandler(object sender, TransactionErrorArgs args);

    public class Transaction : CycleUpdater
    {
        #region data
        #region property
        public string Name { get; private set; }
        #endregion

        #region field
        public TransactionErrorEventHandler TransactionErrorEvent;

        internal readonly Action action;
        #endregion
        #endregion

        #region constructor
        public Transaction(Action action, int interval, string name) : base(interval)
        {
            this.action = action;
            this.Name = name;

            this.Register();
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
                this.TransactionErrorEvent?.Invoke(this, new TransactionErrorArgs(this, ex));

                this.Unregister();  // stop running when error occurred
            }
        }
        #endregion

        #region private
        public override void Destroy()
        {
            base.Destroy();
            this.TransactionErrorEvent = null;
        }
        #endregion
    }
}
