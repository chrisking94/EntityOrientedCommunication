using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections;
using Newtonsoft.Json;
using EntityOrientedCommunication.Utilities;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication
{
    public enum ThreadType
    {
        None,
        WatchDog,
        Listen,
    }

    public enum OperationPhase
    {
        P0Start,  // start
        P1Connected,  // server and client are successfully connected through TCP/IP
        P2LoggedIn,  // client logged in
    }

    /// <summary>
    /// 底层通信
    /// </summary>
    public abstract class Agent
    {
        #region data
        #region property
        public virtual bool IsConnected => socket == null ? false : socket.Connected;

        public virtual string ClientName { get; protected set; }  // 本地用户名

        public virtual string TeleClientName { get; protected set; }  // 对方客户名

        public OperationPhase Phase { get; protected set; }  // 当前操作阶段

        public bool IsDead { get; private set; }  // 看门狗退出后变为true
        #endregion

        #region field
        private const byte dogFoodFlag = 0xdf;

        protected Socket socket;

        private int bufferSize = 65535;
        private byte[] rbuffer;  // 接收缓冲区
        private byte[] slot;  // 接收槽
        private int slotSize;  // 接收槽大小
        private int watchDog = 0;  // 看门狗计时
        protected readonly int timeout = 10000;  // 请求超时时长，单位ms
        private Queue<TMessage> inMsgQueue;  // 接收消息队列
        private Queue<TMessage> outMsgQueue;  // 发送消息队列
        private Queue<TCounter> timeoutTCQ;  // 超时请求队列
        protected readonly int threadInterval = 10;  // 扫描周期,ms
        /// <summary>
        /// 特殊ID: 0: dog food, 1: envelope, 2: login
        /// </summary>
        private Dictionary<uint, TCounter> dictMsgIdAndTCounter;
        private Mutex sendMutex;
        protected Logger logger;
        protected uint envelope;
        private bool bSingleUse;  // 一次性，在收到第一条消息（非watchDog）后关闭listen线程
        private Dictionary<ThreadType, ThreadControl> dictThreadTypeAndControl;  // 管理线程
        #endregion
        #endregion

        #region constructor
        /// <summary>
        /// 默认启动看门狗线程和事物处理线程
        /// </summary>
        /// <param name="bSingleUse">为true时收到第一条消息后关闭listen线程</param>
        protected Agent(bool bSingleUse = false)
        {
            this.bSingleUse = bSingleUse;
            ClientName = "";
            TeleClientName = "";

            dictMsgIdAndTCounter = new Dictionary<uint, TCounter>(8);
            rbuffer = new byte[bufferSize];
            slotSize = bufferSize << 1;
            slot = new byte[slotSize];
            sendMutex = new Mutex();
            inMsgQueue = new Queue<TMessage>(32);
            outMsgQueue = new Queue<TMessage>(32);
            timeoutTCQ = new Queue<TCounter>(32);
            logger = new Logger("@@@");
            dictThreadTypeAndControl = new Dictionary<ThreadType, ThreadControl>()
            {
                { ThreadType.Listen, new ThreadControl(ThreadType.Listen.ToString(), __threadListen, CloseSocket) },
                { ThreadType.WatchDog, new ThreadControl(ThreadType.WatchDog.ToString(), __threadWatchdog) },
            };
            ResetEnvelope();

            // 启动看门狗
            GetControl(ThreadType.WatchDog).Start();
        }
        #endregion

        #region interface
        /// <summary>
        /// 销毁后不可恢复
        /// </summary>
        public virtual void Destroy()
        {
            Reset();

            // 停止所有线程
            foreach (var control in dictThreadTypeAndControl.Values)
            {
                control.SafeAbort();
            }

            // 关闭socket
            CloseSocket();
        }
        /// <summary>
        /// 等待流程达到ph阶段
        /// </summary>
        /// <param name="ph"></param>
        public void WaitTill(OperationPhase ph)
        {
            while (Phase < ph) Thread.Sleep(1);
        }
        public override string ToString()
        {
            return $"[Agent]{ClientName}<->{TeleClientName}";
        }
        #endregion

        #region private
        /// <summary>
        /// 重置this的数据
        /// </summary>
        protected void Reset()
        {
            lock (inMsgQueue) inMsgQueue.Clear();
            lock (timeoutTCQ) timeoutTCQ.Clear();
        }

        /// <summary>
        /// 关闭socket
        /// </summary>
        private void CloseSocket()
        {
            if (socket != null)
            {
                lock (socket)
                {
                    // close socket
                    if (socket.Connected)
                    {
                        socket.Disconnect(false);
                    }
                    socket.Close();
                }

                //socket = null;
            }
        }

        protected ThreadControl GetControl(ThreadType threadType)
        {
            if (dictThreadTypeAndControl.ContainsKey(threadType))
            {
                return dictThreadTypeAndControl[threadType];
            }
            else
            {
                throw new Exception($"there is no thread which is '{threadType}'");
            }
        }

        /// <summary>
        /// 把消息放回接收队列
        /// </summary>
        /// <param name="msg"></param>
        protected void EnqueueInMsgQ(TMessage msg)
        {
            lock(inMsgQueue)
            {
                inMsgQueue.Enqueue(msg);
                ThreadPool.QueueUserWorkItem(_processTask, inMsgQueue);
            }
        }

        protected void SetTimeOut(uint msgId, int timeout)
        {
            lock (dictMsgIdAndTCounter)
            {
                if (dictMsgIdAndTCounter.ContainsKey(msgId))
                {
                    dictMsgIdAndTCounter[msgId].CountDown = timeout;
                }
                else
                {
                    throw new Exception($"不存在 '{msgId}' 号请求");
                }
            }
        }

        /// <summary>
        /// 发送一个消息并等待回信
        /// </summary>
        /// <param name="status">额外状态码</param>
        /// <param name="msg"></param>
        /// <param name="timeout">默认为-1时使用this.timeout</param>
        /// <returns></returns>
        protected TMessage Request(StatusCode status, TMessage msg, int timeout = -1)
        {
            msg.Status |= status | StatusCode.Request;
            if (timeout == -1) timeout = this.timeout;
            var tc = SetWaitFlag(msg, timeout);
            SendMessage(msg);
            if (!tc.WaitReply())
            {
                throw new TimeoutException($"{msg}请求超时");
            }
            return tc.ResponseMsg;
        }

        /// <summary>
        /// 异步请求
        /// </summary>
        /// <param name="status"></param>
        /// <param name="msg"></param>
        /// <param name="timeout">为0将使用默认值</param>
        protected TCounter AsyncRequest(StatusCode status, TMessage msg, int timeout = -1)
        {
            msg.Status |= status | StatusCode.Request;
            if (timeout == -1) timeout = this.timeout;
            var tCounter = SetWaitFlag(msg, timeout);
            SendMessage(msg);

            return tCounter;
        }

        /// <summary>
        /// 回应一个请求
        /// </summary>
        /// <param name="msg"></param>
        protected void Response(TMessage msg)
        {
            msg.Status |= StatusCode.Response;
            SendMessage(msg);
        }

        protected TCounter SetWaitFlag(TMessage msg, int timeout)
        {
            var tc = new TCounter(msg, timeout, this);
            lock (dictMsgIdAndTCounter) dictMsgIdAndTCounter[msg.ID] = tc;
            return tc;
        }

        protected TCounter RemoveWaitFlag(uint msgId)
        {
            var tc = dictMsgIdAndTCounter[msgId];
            lock (dictMsgIdAndTCounter) dictMsgIdAndTCounter.Remove(msgId);
            return tc;
        }

        protected Envelope GetEnvelope()
        {
            var env = new Envelope(envelope);
            envelope += 2;
            if (uint.MaxValue - envelope < 2)
            {
                ResetEnvelope();
            }
            return env;
        }

        protected virtual void ResetEnvelope()
        {
            envelope = 100;
        }

        protected void SendMessage(TMessage msg)
        {
            lock (outMsgQueue)
            {
                outMsgQueue.Enqueue(msg);  // enqueue out message
            }
            ThreadPool.QueueUserWorkItem(_processTask, outMsgQueue);  // create a task
        }

        /// <summary>
        /// 发送一条消息
        /// </summary>
        /// <param name="msg"></param>
        private void Send(TMessage msg)
        {
            if (msg.Status == StatusCode.None)
            {
                throw new Exception($"{msg.GetType().Name}.{nameof(TMessage.Status)} can't be None");
            }

            // pre-processing
            PreprocessOutMessage(ref msg);

            // send
            var bytes = msg.ToBytes();

            logger.Write(LogType.OT, msg);

            SendBytes(bytes);
        }

        /// <summary>
        /// 发送字节流，自动添加头部和校验
        /// </summary>
        /// <param name="bytes"></param>
        private void SendBytes(byte[] bytes)
        {
            var lenBuff = BitConverter.GetBytes(bytes.Length);
            sendMutex.WaitOne();
            SendRaw(lenBuff, 0, lenBuff.Length);  // 长度码
            SendRaw(bytes, 0, bytes.Length);  // 正文
            SendRaw(lenBuff, 0, lenBuff.Length);  // 校验
            sendMutex.ReleaseMutex();
        }

        /// <summary>
        /// 发送未加工过的字节流，仅供SendBytes调用该方法，其他程序禁止调用，以避免出错
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        private void SendRaw(byte[] bytes, int offset, int size)
        {
            try
            {
                for (var count = 0; offset < size;)
                {
                    lock (socket)
                    {
                        if (socket.Connected)
                        {
                            count = socket.Send(bytes, offset, size - offset, SocketFlags.None);
                        }
                        else
                        {
                            break;
                        }
                    }
                    offset += count;
                }
            }
            catch (SocketException se)
            {
                logger.Write(LogType.ER, se.Message);
            }
        }

        /// <summary>
        /// 每个事件周期会调用一次该方法
        /// </summary>
        /// <returns></returns>
        protected virtual void OnTransaction()
        {
            // pass
        }

        /// <summary>
        /// 处理收到的请求，只需要编辑msg内容，回传操作由Agent完成
        /// 警告：不要阻塞该程序，避免在该函数下调用Request函数
        /// </summary>
        /// <param name="msg"></param>
        protected abstract void ProcessRequest(ref TMessage msg);

        /// <summary>
        /// 处理收到的回应消息，只需处理消息，回应会被自动放入字典对应位置
        /// 警告：不要阻塞该程序，避免在该函数下调用Request函数
        /// </summary>
        /// <param name="responseMsg"></param>
        protected abstract void ProcessResponse(TMessage requestMsg, TMessage responseMsg);

        /// <summary>
        /// 处理超时的请求
        /// </summary>
        /// <param name="requestMsg"></param>
        protected abstract void ProcessTimeoutRequest(TMessage requestMsg);

        /// <summary>
        /// 消息发出前会调用该方法
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void PreprocessOutMessage(ref TMessage msg)
        {
            // pass
        }

        /// <summary>
        /// 收到新消息时会调用该方法进行预处理
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void PreprocessInMessage(ref TMessage msg)
        {
            // pass
        }

        /// <summary>
        /// 所有异常交给该函数处理
        /// </summary>
        /// <param name="exp"></param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected virtual void Catch(TException exp)
        {
            throw exp;
        }

        /// <summary>
        /// threadListen退出后会调用该方法一次
        /// </summary>
        protected virtual void OnThreadListenAborted()
        {
            // pass
        }

        /// <summary>
        /// 看门狗检测到连接超时的时候会调用该方法
        /// </summary>
        protected virtual void OnConnectionTimeout()
        {
            Reset();
        }

        /// <summary>
        /// 消息处理线程，由ThreadPool管理
        /// </summary>
        /// <param name="state"></param>
        private void _processTask(object state)
        {
            TMessage msg = null;

            try
            {
                // #############
                // 消息接收队列
                // #############
                msg = null;
                if (state == inMsgQueue)
                {
                    lock (inMsgQueue)
                    {
                        if (inMsgQueue.Count > 0) msg = inMsgQueue.Dequeue();
                    }
                    if (msg != null)
                    {
                        PreprocessInMessage(ref msg);
                        logger.Write(LogType.IN, msg);

                        if (msg.HasFlag(StatusCode.Response))
                        {
                            TCounter tc = null;
                            if (dictMsgIdAndTCounter.ContainsKey(msg.ID))
                            {
                                lock (dictMsgIdAndTCounter)
                                {
                                    tc = dictMsgIdAndTCounter[msg.ID];
                                    tc.SetReply(msg);
                                    RemoveWaitFlag(msg.ID);
                                }
                                ProcessResponse(tc.RequestMsg, tc.ResponseMsg);
                            }
                        }

                        if (msg.HasFlag(StatusCode.Request))
                        {
                            ProcessRequest(ref msg);
                            if (!msg.HasFlag(StatusCode.NoAutoReply))
                            {
                                Response(msg);
                            }
                        }
                        msg = null;
                    }
                }

                // #############
                // 消息发送队列
                // #############
                msg = null;
                if (state == outMsgQueue)
                {
                    lock (outMsgQueue)
                    {
                        if (outMsgQueue.Count > 0) msg = outMsgQueue.Dequeue();
                    }

                    if (msg != null)
                    {
                        Send(msg);
                    }
                }

                // #############
                // 超时消息队列
                // #############
                msg = null;
                lock (timeoutTCQ)
                {
                    if (timeoutTCQ.Count > 0) msg = timeoutTCQ.Dequeue().RequestMsg;
                }
                if (msg != null)
                {
                    logger.Error($"request {msg.ID} timeout.");
                    ProcessTimeoutRequest(msg);
                }
            }
            catch (Exception ex)
            {
                if (msg == null)
                {
                    Catch(new TException(ex));
                }
                else
                {
                    Catch(new TException(ex, TExceptionType.MessageProcessingFailed, msg));
                }
            }
        }

        /// <summary>
        /// 最高权限，执行一些轻量操作，如：更新计时器
        /// </summary>
        private void __threadWatchdog(ThreadControl control)
        {
            logger.Debug($"{nameof(__threadWatchdog)}() on duty.");

            var feedCycle = 1000;  // 1s喂一次狗
            var feedTCounter = 0;
            var msg = new TMessage(0);
            var foodBag = new[] { dogFoodFlag };

            while (!control.SafelyTerminating)
            {
                Thread.Sleep(threadInterval);
                feedTCounter += threadInterval;
                if (IsConnected)
                {
                    // 检查看门狗是否超时
                    if (watchDog != -1)  // -1代表已经超时，需要由__threadListen重新将其变为0
                    {
                        watchDog += threadInterval;
                        if (watchDog >= timeout)
                        {
                            watchDog = -1;  // time out flag

                            control.AsyncSafeAbort();
                            GetControl(ThreadType.Listen).SafeAbort();
                            OnConnectionTimeout();
                            logger.Error($"{this} connection timeout.");
                        }
                    }
                }
                // 更新请求计时器
                KeyValuePair<uint, TCounter>[] msgIdAndTCounterPairs;
                lock (dictMsgIdAndTCounter) msgIdAndTCounterPairs = dictMsgIdAndTCounter.ToArray();
                foreach (var kv in msgIdAndTCounterPairs)
                {
                    if (kv.Value.Decrease(threadInterval))  // 超时
                    {
                        lock (timeoutTCQ)
                        {
                            timeoutTCQ.Enqueue(kv.Value);
                        }
                        RemoveWaitFlag(kv.Key);
                        ThreadPool.QueueUserWorkItem(_processTask, timeoutTCQ);
                    }
                }
                // 喂狗
                if (feedTCounter >= feedCycle)
                {
                    feedTCounter = 0;  // reset

                    if (IsConnected)  // 喂狗
                    {
                        SendBytes(foodBag);
                    }
                }
            }

            control.SetAbortedFlags();

            logger.Debug($"{nameof(__threadWatchdog)}() aborted.");
            this.IsDead = true;
        }

        /// <summary>
        /// 通过socket接收数据流
        /// </summary>
        /// <param name="control"></param>
        private void __threadListen(ThreadControl control)
        {
            try
            {
                logger.Debug($"{nameof(__threadListen)}() on duty.");

                var count = 1;
                int k = 0, l = 0;
                uint msgLen = 0, verifyLen = 0;
                var lenBuff = new byte[sizeof(uint)];
                var slotBackup = slot;

                socket.SendBufferSize = this.bufferSize;

                while (!control.SafelyTerminating)
                {
                    var result = socket.BeginReceive(rbuffer, 0, rbuffer.Length, SocketFlags.None, x => x = null, null);  // 异步接收，为了实现SafelyTermination
                    while (!control.SafelyTerminating && !result.IsCompleted)  // 等待完成
                    {
                        Thread.Sleep(1);
                    }
                    if (control.SafelyTerminating || !socket.Connected) break;  // 结束监听
                    count = socket.EndReceive(result);  // 获取收到的字节数量
                    for (var j = 0; j < count;)  // 处理收到的字节
                    {
                        if (msgLen == 0)
                        {
                            lenBuff[l] = rbuffer[j];
                            ++j;  // 指向content第一个字节
                            if (++l == lenBuff.Length)
                            {
                                l = 0;
                                msgLen = BitConverter.ToUInt32(lenBuff, 0);
                                if (verifyLen != 0)  // 需要验证
                                {
                                    if (verifyLen == msgLen)  // 验证通过
                                    {
                                        watchDog = 0;  // 重置看门狗
                                        if (msgLen == 1)  // 系统命令
                                        {
                                            if (slot[0] == dogFoodFlag)  // 看门狗，dog food
                                            {
                                                // pass
                                            }
                                        }
                                        else
                                        {
                                            var msg = TMessage.FromBytes(slot, 0, k);

                                            lock (inMsgQueue) inMsgQueue.Enqueue(msg);
                                            ThreadPool.QueueUserWorkItem(_processTask, inMsgQueue);
                                            if (bSingleUse)
                                            {
                                                control.SafelyTerminating = true;  // 关闭listen线程
                                                break;  // 结束数据读取
                                            }
                                        }
                                        msgLen = 0;
                                    }
                                    else
                                    {
                                        Catch(new TException("传输错误，请重连！"));
                                    }
                                    verifyLen = 0;
                                    if (slot.Length != slotSize)
                                    {
                                        slot = slotBackup;
                                    }
                                }
                                else
                                {
                                    if (msgLen > slotSize)
                                    {
                                        slot = new byte[msgLen];  // super slot
                                    }
                                }
                                k = 0;
                            }
                        }
                        else
                        {
                            int n = (int)(msgLen - k);
                            if (n + j > count)
                            {
                                n = count - j;
                            }
                            Array.Copy(rbuffer, j, slot, k, n);
                            k += n;
                            j += n;
                            if (k == msgLen)
                            {
                                verifyLen = msgLen;
                                msgLen = 0;
                            }
                        }
                    }
                }
            }
            catch (SocketException se)
            {
                logger.Error($"{se.Message}");
            }
            catch (Exception ex)
            {
                logger.Fatal("fatal error occurred in __threadListen.", ex);
                Catch(new TException(ex));
            }
            finally
            {
                control.SetAbortedFlags();

                OnThreadListenAborted();

                logger.Debug($"{nameof(__threadListen)}() aborted.");
            }
        }
        #endregion
    }
}
