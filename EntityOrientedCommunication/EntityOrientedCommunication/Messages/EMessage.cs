using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.IO.Compression;
using System.IO;
using EntityOrientedCommunication.Facilities;

namespace EntityOrientedCommunication.Messages
{
    [Flags]
    public enum StatusCode
    {
        None            = 0x00_00_0000,

        /* basic status */
        Request         = 0x00_00_0001,
        Response        = 0x00_00_0002,
        Command = Request | Response,  // this command is only emitted by server

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
        Time            = 0x04_00_0000,  // sychronize time.now

        /* EOC letter type code */
        Post            = 0x20_00_0000,  // post a letter to the target entity(ies), ignore all errors in the sending progress
        Get             = 0x40_00_0000,  // send a letter to the target entity(ies), and wait for every entity's reply, error will be reported if failed
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class EMessage
    {
        #region property
        [JsonProperty]
        internal StatusCode Status { get; set; }
        #endregion

        #region field
        [JsonProperty]
        internal uint ID { get; private set; }

        /// <summary>
        /// size after serializing and compressing, in byte
        /// </summary>
        private int size;
        #endregion

        #region constructor
        [JsonConstructor]
        protected EMessage() { }

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

        public byte[] ToBytes()
        {
            var bytes = Encoding.ASCII.GetBytes(Serializer.ToJson(this));
            using (var ms = new MemoryStream())
            {
                var gzip = new GZipStream(ms, CompressionMode.Compress, false);
                gzip.Write(bytes, 0, bytes.Length);
                gzip.Close();
                bytes = ms.ToArray();
                size = bytes.Length;
                return bytes;
            }
        }

        public static EMessage FromBytes(byte[] bytes, int index, int count)
        {
            using (var ms = new MemoryStream(bytes, index, count))
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (var sr = new StreamReader(gzip, Encoding.ASCII))
                    {
                        var json = sr.ReadToEnd();
                        var msg = Serializer.FromJson<EMessage>(json);
                        msg.size = bytes.Length;
                        return msg;
                    }
                }
            }
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

        #region private
        protected string Format(string abbr, string extraInfo = "")
        {
            var sizeString = size == 0 ? "__size__" : Size();

            return string.Format("{0,-5} {1,-4} {4,-7} {2,-35} {3}", ID, abbr, Status.ToString(), extraInfo, sizeString);
        }
        #endregion
    }
}
