using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.IO.Compression;
using System.IO;
using EntityOrientedCommunication.Utilities;

namespace EntityOrientedCommunication.Messages
{
    [Flags]
    public enum StatusCode
    {
        None         = 0x00_00_0000,
        Request         = 0x00_00_0001,
        Response        = 0x00_00_0002,
        Register        = 0x00_00_0004,  // 注册
        Unregister      = Register | Not,
        GetVersions     = 0x00_00_0008,  // 读取server版本控制表
        Command         = Request | Response,  // 只能由服务器发出的命令
        Operate         = 0x00_00_0010,  // 申请操作一个数据单元，服务器同意后返回Ok，否则返回码中不包含Ok
        Login           = 0x00_00_0020,  // 客户端登陆
        Logout          = Login | Not,
        Pull            = 0x00_00_0040,  // 拉取服务器数据
        Ok              = 0x00_00_0080,
        Denied          = 0x00_00_0100,
        NoAutoReply     = 0x00_00_0200,  // 用于取消自动回复
        Push            = 0x00_00_0400,  // 添加到版本控制
        Update          = 0x00_00_0800,  // 软件升级
        Cmdline         = 0x00_00_1000,  // 执行命令行
        Upload          = 0x00_00_2000,  // 上载文件
        Download        = 0x00_00_4000,  // 下载文件
        Optimize        = 0x00_00_8000,  // 调用求解器
        Delete          = 0x00_01_0000,  // 删除一个托管对象
        /* describer */
        Raw             = 0x01_00_0000,  // 代表一个普通文档对象
        Letter          = 0x02_00_0000,  // 信件，可延迟收发
        Receiver        = 0x04_00_0000,  // 信件接收器
        Entity          = 0x08_00_0000,  // 标记与实体相关的操作
        Not             = 0x10_00_0000,  // 取反
        Decision        = 0x20_00_0000,  // 决策文件
        Time            = 0x40_00_0000,  // 时间操作
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class TMessage
    {
        #region property
        [JsonProperty]
        public StatusCode Status { get; set; }
        #endregion

        #region field
        [JsonProperty]
        public uint ID { get; private set; }

        /// <summary>
        /// 一些消息发送失败后会重新发送，这个字段存储发送次数，超次消息可能会被遗弃
        /// </summary>
        public int Trials;

        private int size;
        #endregion

        #region constructor
        [JsonConstructor]
        public TMessage() { }
        internal TMessage(uint id)
        {
            ID = id;
        }
        public TMessage(TMessage toReply)
        {
            ID = toReply.ID;
            Status = toReply.Status;
        }
        public TMessage(TMessage copyFrom, StatusCode status): this(copyFrom)
        {
            Status = status;
        }
        public TMessage(Envelope envelope, StatusCode status = StatusCode.None)
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
        public static TMessage FromBytes(byte[] bytes, int index, int count)
        {
            using (var ms = new MemoryStream(bytes, index, count))
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (var sr = new StreamReader(gzip, Encoding.ASCII))
                    {
                        var json = sr.ReadToEnd();
                        var msg = Serializer.FromJson<TMessage>(json);
                        msg.size = bytes.Length;
                        return msg;
                    }
                }
            }
        }
        public bool HasFlag(StatusCode flag)
        {
            return (Status & flag) == flag;
        }
        public string Size()
        {
            return StringFormatter.ByteCountToString(size);
        }
        public override string ToString()
        {
            return Format("TMsg");
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
