using System;
using System.IO.Compression;
using System.IO;
using EntityOrientedCommunication.Facilities;
using System.Runtime.Serialization.Formatters.Binary;

namespace EntityOrientedCommunication.Messages
{
    [Flags]
    internal enum StatusCode
    {
        None            = 0x00_00_0000,

        /* basic status */
        Request         = 0x00_00_0001,
        Response        = 0x00_00_0002,
        Duplex          = Request | Response,  // represent request and response simultaneously

        /* operation */
        Register        = 0x00_00_0004,  // register a 'target'
        Unregister      = 0x00_00_0008,  // unregister a 'target'
        Login           = 0x00_00_0010,  // login to server
        Logout          = 0x00_00_0020,  // logout from server
        Push            = 0x00_00_0040,  // push data to remote endpoint
        Pull            = 0x00_00_0080,  // pull data from remote endpoint
        Ok              = 0x00_00_0100,  // request acknowledged
        Denied          = 0x00_00_0200,  // deny a request
        NoAutoReply     = 0x00_00_0400,  // this flag is used to cancel auto reply

        /* target */
        Letter          = 0x01_00_0000,  // EOC letter
        Entity          = 0x02_00_0000,  // EOC entity

        /* EOC letter type code */
        Post            = 0x20_00_0000,  // post a letter to the target entity(ies), ignore all errors in the sending progress
        Get             = 0x40_00_0000,  // send a letter to the target entity(ies), and wait for every entity's reply, error will be reported if failed
    }

    [Serializable]
    internal class EMessage
    {
        #region property
        internal StatusCode Status { get; set; }

        internal uint ID { get; private set; }
        #endregion

        #region field
        /// <summary>
        /// size after serializing and compressing, in byte
        /// </summary>
        [NonSerialized]
        private long size;
        #endregion

        #region constructor
        protected EMessage()
        {

        }

        internal EMessage(uint id)
        {
            ID = id;
        }

        public EMessage(EMessage copyFrom)
        {
            ID = copyFrom.ID;
            Status = copyFrom.Status;
        }

        public EMessage(EMessage toReply, StatusCode status): this(toReply)
        {
            Status = status;
        }

        public EMessage(Envelope envelope, StatusCode status = StatusCode.None)
        {
            ID = envelope.ID;
            Status = status;
        }
        #endregion

        #region interface
        public void SetEnvelope(Envelope env)
        {
            this.ID = env.ID;
        }

        public bool HasFlag(StatusCode flag)
        {
            return (this.Status & flag) == flag;
        }

        public string Size()
        {
            return StringFormatter.ByteCountToString(size);
        }

        public override string ToString()
        {
            return Format("EMsg");
        }
        #endregion

        #region serialization, deserialization
        /// <summary>
        /// fill some data before the incoming transmission
        /// </summary>
        protected virtual void PrepareForTransmission()
        {
            // pass
        }

        private static BinaryFormatter g_bf = new BinaryFormatter();

        public byte[] ToBytes()
        {
            PrepareForTransmission();  // prepare

            using (var zippedStream = new MemoryStream())
            {
                using (var gzip = new GZipStream(zippedStream, CompressionLevel.Optimal))
                {
                    g_bf.Serialize(gzip, this);  // serialize
                }

                var bytes = zippedStream.ToArray();
                this.size = bytes.Length;

                return bytes;
            }
        }

        /// <summary>
        /// the stream will not be closed by this method
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static EMessage FromBytes(byte[] bytes, int offset, int count)
        {
            using (var rawStream = new MemoryStream(bytes, offset, count))
            {
                var os = new MemoryStream();
                using (var gzip = new GZipStream(rawStream, CompressionMode.Decompress))
                {
                    var msg = g_bf.Deserialize(gzip) as EMessage;

                    msg.size = rawStream.Length;

                    return msg;
                }
            }
        }
        #endregion

        #region private
        protected string Format(string abbr, string extraInfo = "")
        {
            var sizeString = size == 0 ? "__size__" : Size();

            return string.Format("{0,-5} {1,-4} {4,-7} {2,-35} {3}", ID, abbr, Status.ToString(), extraInfo, sizeString);
        }
        #endregion
    }
}
