/********************************************************\
  					RADIATION RISK						
  														
   author: chris	create time: 5/15/2020 5:35:48 PM					
\********************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace EntityOrientedCommunication
{
    class BinarySerializer : ISerializer
    {
        private BinaryFormatter bf = new BinaryFormatter();

        public object FromBytes(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return null;
            }

            using (var ms = new MemoryStream(bytes, 0, bytes.Length))
            {
                var obj = bf.Deserialize(ms);

                return obj;
            }
        }

        public byte[] ToBytes(object obj)
        {
            if (obj == null)
            {
                return new byte[0];
            }

            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);

                return ms.ToArray();
            }
        }
    }
}
