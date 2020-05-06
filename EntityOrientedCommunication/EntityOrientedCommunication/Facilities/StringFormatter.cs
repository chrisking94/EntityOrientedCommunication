/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 4/27/2020 12:11:37 PM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EntityOrientedCommunication.Facilities
{
    public static class StringFormatter
    {
        public static string ByteCountToString(int count)
        {
            var unitChars = new char[] { 'B', 'K', 'M', 'G' };
            int i = 0;
            double j = count;
            for (; i < unitChars.Length && j > 1024.0; ++i, j /= 1024.0) ;
            var sizeString = $"{Math.Round(j, 1)}{unitChars[i]}";

            return sizeString;
        }
    }
}
