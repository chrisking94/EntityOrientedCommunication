/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 5/8/2020 5:48:05 PM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication.Facilities
{
    internal static class QueueExtensions
    {
        /// <summary>
        /// remove items which satisfy the predicate from queue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="predicate"></param>
        public static void Remove<T>(this Queue<T> queue, Func<T, bool> predicate)
        {
            var list = queue.Where(o => predicate(o)).ToArray();

            queue.Clear();

            foreach (var item in list)
            {
                queue.Enqueue(item);
            }
        }
    }
}
