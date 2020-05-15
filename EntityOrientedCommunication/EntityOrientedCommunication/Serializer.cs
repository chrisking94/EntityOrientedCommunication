using System.Linq;

namespace EntityOrientedCommunication
{
    public interface ISerializer
    {
        /// <summary>
        /// Serialize object to bytes
        /// </summary>
        /// <param name="obj">might be null</param>
        /// <returns></returns>
        byte[] ToBytes(object obj);

        /// <summary>
        /// Deserialize bytes to object
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        object FromBytes(byte[] bytes);
    }
}
