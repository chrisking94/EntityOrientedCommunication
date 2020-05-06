/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 5/6/2020 11:56:09 AM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EntityOrientedCommunication.Facilities
{
    internal enum RWLSHandleType
    {
        Read,
        Write,
        Upgrade,
    }

    /// <summary>
    /// manage the exit operation of ReaderWriterLockSlim instance in the 'Dispose' method, 
    /// <para>so it's able to use ReaderWriterLockSlim like the key word 'lock(xxx)' in c# through code like using(lck = rwls.GetReadHandle()),
    /// <para>see also: ReaderWriterLockSlimExtensions</para></para>
    /// </summary>
    public class RWLSHandle : IDisposable
    {
        private readonly ReaderWriterLockSlim rwls;

        private readonly RWLSHandleType handleType;

        internal RWLSHandle(ReaderWriterLockSlim rwls, RWLSHandleType handleType)
        {
            this.rwls = rwls;
            this.handleType = handleType;
        }

        public void Dispose()
        {
            switch (handleType)
            {
                case RWLSHandleType.Read:
                    this.rwls.ExitReadLock();
                    break;
                case RWLSHandleType.Write:
                    this.rwls.ExitWriteLock();
                    break;
                case RWLSHandleType.Upgrade:
                    this.rwls.ExitUpgradeableReadLock();
                    break;
            }
        }
    }

    public static class ReaderWriterLockSlimExtensions
    {
        public static RWLSHandle GetReadHandle(this ReaderWriterLockSlim rwls)
        {
            rwls.EnterReadLock();
            return new RWLSHandle(rwls, RWLSHandleType.Read);
        }

        public static RWLSHandle GetWriteHandle(this ReaderWriterLockSlim rwls)
        {
            rwls.EnterWriteLock();
            return new RWLSHandle(rwls, RWLSHandleType.Write);
        }

        public static RWLSHandle GetUpgradeHandle(this ReaderWriterLockSlim rwls)
        {
            rwls.EnterUpgradeableReadLock();
            return new RWLSHandle(rwls, RWLSHandleType.Upgrade);
        }
    }
}
