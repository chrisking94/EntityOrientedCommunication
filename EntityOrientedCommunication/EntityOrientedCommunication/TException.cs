/* ==============================================================================
 * author		：chris
 * create time	：3/18/2019 10:07:26 AM
 * ==============================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace EntityOrientedCommunication
{
    public enum TExceptionType
    {
        Default,
        IncorrectUsernameOrPassword,
        MessageProcessingFailed,
    }

    public class TException : Exception
    {
        #region data
        #region property
        public TExceptionType ExpceptionType { get; private set; }

        public object Tag { get; private set; }
        #endregion

        #region field
        #endregion
        #endregion

        #region constructor
        public TException(Exception exp, TExceptionType exceptionType = TExceptionType.Default, object tag = null) : base("TException", exp)
        {
            this.ExpceptionType = exceptionType;
            this.Tag = tag;
        }

        public TException(string msg, TExceptionType exceptionType = TExceptionType.Default) : base(msg)
        {
            ExpceptionType = exceptionType;
        }
        #endregion

        #region interface
        #endregion

        #region private
        #endregion
    }
}
