﻿/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/10/2020 3:28:40 PM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication.Facilities
{
    /// <summary>
    /// add some offset to DateTime.Now
    /// </summary>
    internal class TimeBlock
    {
        private long offsetTicks;

        public DateTime Value => DateTime.Now.AddTicks(offsetTicks);

        public void Set(DateTime now)
        {
            offsetTicks = now.Ticks - DateTime.Now.Ticks;
        }

        public static implicit operator DateTime(TimeBlock block)
        {
            return block.Value;
        }
    }
}
