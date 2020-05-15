/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 5/15/2020 5:27:11 PM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication
{
    /// <summary>
    /// Global configuration of EOC library
    /// </summary>
    public static class Configuration
    {
        public static ISerializer Serializer { get; set; } = new BinarySerializer();
    }
}
