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
    public static class ReaderWriterLockExtensions
    {
        public static void AcquireReaderLock(this ReaderWriterLock rwl)
        {
            rwl.AcquireReaderLock(int.MaxValue);
        }

        public static void AcquireWriterLock(this ReaderWriterLock rwl)
        {
            rwl.AcquireWriterLock(int.MaxValue);
        }
    }
}
