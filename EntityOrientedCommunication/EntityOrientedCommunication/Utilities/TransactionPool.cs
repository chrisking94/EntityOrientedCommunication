/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/26/2020 10:21:10 AM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication.Utilities
{
    public class TransactionPool
    {
        #region data
        #region property
        #endregion

        #region field
        private Dictionary<string, Transaction> name2Trans;
        #endregion
        #endregion

        #region constructor
        public TransactionPool()
        {
            this.name2Trans = new Dictionary<string, Transaction>();
        }
        #endregion

        #region interface
        public Transaction this[string name] => this.name2Trans[name];

        /// <summary>
        /// register a transaction and start the transaction automatically
        /// </summary>
        /// <param name="action">the action which will be run repeatedly</param>
        /// <param name="interval">ms</param>
        /// <param name="name"></param>
        /// <returns></returns>
        public Transaction Register(Action action, int interval, string name)
        {
            var transaction = new Transaction(action, interval, name);

            this.name2Trans.Add(name, transaction);

            return transaction;
        }

        /// <summary>
        /// destroy pool and all transactions
        /// </summary>
        public void Destroy()
        {
            foreach (var transactin in this.name2Trans.Values)
            {
                transactin.Destroy();
            }
            this.name2Trans = null;
        }
        #endregion

        #region private
        #endregion
    }
}
