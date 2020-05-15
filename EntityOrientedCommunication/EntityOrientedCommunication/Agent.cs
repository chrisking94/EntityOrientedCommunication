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
using EntityOrientedCommunication.Facilities;
using EntityOrientedCommunication.Messages;
using System.IO;

namespace EntityOrientedCommunication
{
    internal enum ThreadType
    {
        None,
        WatchDog,
        Listen,
    }

    public enum ConnectionPhase
    {
        P0Start,  // start
        P1Connected,  // server and client are successfully connected through TCP/IP
        P2LoggedIn,  // client logged in
    }

    /// <summary>
    /// the underlying communication, based on TCP/IP
    /// </summary>
    internal abstract class Agent
    {
        #region data
        #region property
        public bool IsConnected
        {
            get
            {
                rwlsSocket.EnterReadLock();
                var bConnected = socket == null ? false : socket.Connected; ;
                rwlsSocket.ExitReadLock();

                return bConnected;
            }
        }

        /// <summary>
        /// name of local client
        /// </summary>
        public string ClientName { get; protected set; }

        /// <summary>
        /// name of client on remote computer
        /// </summary>
        public string TeleClientName { get; protected set; }

        /// <summary>
        /// current connection phase
        /// </summary>
        public ConnectionPhase Phase { get; protected set; }
        #endregion

        #region field
        private const byte dogFoodFlag = 0xdf;

        private Socket socket;
        protected readonly ReaderWriterLockSlim rwlsSocket = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);  // the lock for field 'socket' of this agent

        private int bufferSize = 65535;
        private byte[] rbuffer;  // reception buffer
        private byte[] slot;  // reception slot, it could maintain a series of complete json bytes of a message
        private int slotSize;
        private int watchDog = 0;  // watch dog time counter, unit: ms
        protected readonly int timeout = 10000;  // request timeout milliseconds
        private Queue<EMessage> inMsgQueue;  // reception message queue
        private Queue<EMessage> outMsgQueue;  // delivery message queue
        private Queue<TCounter> timeoutTCQ;  // request timeout message queue
        /// <summary>
        /// special id: 2-login
        /// </summary>
        private Dictionary<uint, TCounter> dictMsgIdAndTCounter;
        private Mutex sendMutex;
        protected Logger logger;
        protected uint envelope;
        private Dictionary<ThreadType, ThreadControl> dictThreadTypeAndControl;  // maintains some thread controller
        #endregion
        #endregion

        #region constructor
        /// <summary>
        /// the watch dog thread is started automatically
        /// </summary>
        protected Agent()
        {
            ClientName = "localhost";
            TeleClientName = "";

            dictMsgIdAndTCounter = new Dictionary<uint, TCounter>(8);
            rbuffer = new byte[bufferSize];
            slotSize = bufferSize << 1;
            slot = new byte[slotSize];
            sendMutex = new Mutex();
            inMsgQueue = new Queue<EMessage>(32);
            outMsgQueue = new Queue<EMessage>(32);
            timeoutTCQ = new Queue<TCounter>(32);
            logger = new Logger("@@@");
            dictThreadTypeAndControl = new Dictionary<ThreadType, ThreadControl>()
            {
                { ThreadType.Listen, new ThreadControl(ThreadType.Listen.ToString(), __threadListen, CloseSocket) },
                { ThreadType.WatchDog, new ThreadControl(ThreadType.WatchDog.ToString(), __threadWatchdog) },
            };
            ResetEnvelope();

            // start watchdog
            GetControl(ThreadType.WatchDog).Start();
        }
        #endregion

        #region interface
        /// <summary>
        /// this agent can not be recovered after it is destroyed
        /// </summary>
        public virtual void Destroy()
        {
            ClearMessageQueues();

            // stop all treads
            foreach (var control in dictThreadTypeAndControl.Values)
            {
                control.AsyncSafeAbort();
            }

            // close socket
            CloseSocket();
        }

        /// <summary>
        /// wait for connection phase of this agent encounter the specified 'phase'
        /// </summary>
        /// <param name="ph"></param>
        public void WaitTill(ConnectionPhase ph)
        {
            while (Phase < ph) Thread.Sleep(1);
        }

        public override string ToString()
        {
            return $"[Agent]{ClientName}<->{TeleClientName}";
        }
        #endregion

        #region private
        protected void ClearMessageQueues()
        {
            lock (inMsgQueue) inMsgQueue.Clear();
            lock (outMsgQueue) outMsgQueue.Clear();
            lock (timeoutTCQ) timeoutTCQ.Clear();
        }

        private void CloseSocket()
        {
            RestSocket(null);
        }

        protected void RestSocket(Socket newSocket)
        {
            rwlsSocket.EnterWriteLock();  // enter write lock

            // dispose the old socket
            if (socket != null)
            {
                try
                {
                    if (socket.Connected)  // close socket
                    {
                        socket.Disconnect(false);
                    }
                    socket.Close();
                }
                catch
                {
                    // pass
                }
            }
            // upgrade field to new socket
            socket = newSocket;
            rwlsSocket.ExitWriteLock();  // exit write lock
        }

        protected ThreadControl GetControl(ThreadType threadType)
        {
            if (dictThreadTypeAndControl.ContainsKey(threadType))
            {
                return dictThreadTypeAndControl[threadType];
            }
            else
            {
                throw new Exception($"there is no thread which is of type '{threadType}'");
            }
        }

        /// <summary>
        /// send a message to the remote agent and wait for a response
        /// </summary>
        /// <param name="status">the extra status code attach to 'msg'</param>
        /// <param name="msg"></param>
        /// <param name="timeout">use this.timeout when -1, else use the 'timeout' parameter inputed into this function</param>
        /// <returns></returns>
        protected EMessage Request(StatusCode status, EMessage msg, int timeout = -1)
        {
            var tc = this.AsyncRequest(status, msg, timeout);
            if (!tc.WaitReply())
            {
                throw new TimeoutException($"request timeout: {msg}");
            }
            return tc.ResponseMsg;
        }

        /// <summary>
        /// asynchronized requst, similiar to 'Request', but this method will not wait for the remote computer to response
        /// </summary>
        /// <param name="status"></param>
        /// <param name="msg"></param>
        /// <param name="timeout"></param>
        /// <returns>the time counter for the request 'msg'</returns>
        internal TCounter AsyncRequest(StatusCode status, EMessage msg, int timeout = -1)
        {
            msg.Status |= status | StatusCode.Request;
            if (timeout == -1) timeout = this.timeout;
            var tCounter = SetWaitFlag(msg, timeout);
            SendMessage(msg);

            return tCounter;
        }

        /// <summary>
        /// response to a request
        /// </summary>
        /// <param name="msg"></param>
        protected void Response(EMessage msg)
        {
            msg.Status |= StatusCode.Response;
            SendMessage(msg);
        }

        protected TCounter SetWaitFlag(EMessage msg, int timeout)
        {
            var tc = new TCounter(msg, timeout);
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

        /// <summary>
        /// append 'msg' to the delivery queue
        /// </summary>
        /// <param name="msg"></param>
        protected void SendMessage(EMessage msg)
        {
            lock (outMsgQueue)
            {
                outMsgQueue.Enqueue(msg);  // enqueue out message
            }
            ThreadPool.QueueUserWorkItem(_processTask, outMsgQueue);  // create a task
        }

        /// <summary>
        /// send 'msg' through socket
        /// </summary>
        /// <param name="msg"></param>
        private void Send(EMessage msg)
        {
            if (msg.Status == StatusCode.None)
            {
                throw new Exception($"{msg.GetType().Name}.{nameof(EMessage.Status)} can't be None");
            }

            // pre-processing
            PreprocessOutMessage(ref msg);

            // send
            var bytes = msg.ToBytes();
            logger.Write(LogType.OT, msg);
            SendBytes(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// send byte array，the 'header' and 'check code' are auto attached
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        private void SendBytes(byte[] bytes, int offset, int count)
        {
            var lenBuff = BitConverter.GetBytes(count);
            sendMutex.WaitOne();
            SendRaw(lenBuff, 0, lenBuff.Length);  // 长度码
            SendRaw(bytes, offset, count);  // 正文
            SendRaw(lenBuff, 0, lenBuff.Length);  // 校验
            sendMutex.ReleaseMutex();
        }

        /// <summary>
        /// send raw bytes through socket
        /// <para>attention: only 'SendBytes(byte[])' is granted to invoke this method, other method should not touch this method to avoid some latent risk</para>
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        private void SendRaw(byte[] bytes, int offset, int size)
        {
            rwlsSocket.EnterReadLock();  // field access lock
            if (this.socket == null)
            {
                throw new Exception($"connection {this} has been closed, failed to send bytes stream.");
            }

            for (var count = 0; offset < size && socket.Connected;)
            {
                count = socket.Send(bytes, offset, size - offset, SocketFlags.None);
                offset += count;
            }
            rwlsSocket.ExitReadLock();
        }

        /// <summary>
        /// process the request sent from remote agent, overrides shoud edit the 'msg', after to process is completed,
        /// <para>the edited message will be treated as response message which will be sent to the remote agent, use StatusCode 'NoAutoReply' to cancell this auto reply action</para>
        /// <para>warning：do not block this method, otherwise program might encounter a connection timeout error, because the response is not sent to remote agent in time</para>
        /// </summary>
        /// <param name="msg"></param>
        protected abstract void ProcessRequest(ref EMessage msg);

        /// <summary>
        /// process the response message came from the remote agent
        /// </summary>
        /// <param name="requestMsg"></param>
        /// <param name="responseMsg"></param>
        protected abstract void ProcessResponse(EMessage requestMsg, EMessage responseMsg);

        /// <summary>
        /// process the request messages which are timeout
        /// </summary>
        /// <param name="requestMsg"></param>
        protected abstract void ProcessTimeoutRequest(EMessage requestMsg);

        /// <summary>
        /// this method will be invoked before the impending message transmission
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void PreprocessOutMessage(ref EMessage msg)
        {
            // pass
        }

        /// <summary>
        /// this method will be invoked when the new arrived message is dequeued from in message queue
        /// </summary>
        /// <param name="msg"></param>
        protected virtual void PreprocessInMessage(ref EMessage msg)
        {
            // pass
        }

        /// <summary>
        /// some exceptions threw by agent are handled by this function
        /// </summary>
        /// <param name="exp"></param>
        protected virtual void Catch(EOCException exp)
        {
            throw exp;
        }

        /// <summary>
        /// this method will be invoked every time thread listen has exited safely
        /// </summary>
        protected virtual void OnThreadListenAborted()
        {
            // pass
        }

        /// <summary>
        /// this method will be invoked by watch dog when the connection is timeout
        /// </summary>
        protected virtual void OnConnectionTimeout()
        {
            ClearMessageQueues();
        }

        /// <summary>
        /// fetch messages from the 3 message queues and invoke method to process them
        /// </summary>
        /// <param name="state"></param>
        private void _processTask(object state)
        {
            EMessage msg = null;

            //try
            {
                // #######################
                // in message queue
                // #######################
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

                // ######################
                // out message queue
                // ######################
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
                // timeout message
                // #############
                msg = null;
                lock (timeoutTCQ)
                {
                    if (timeoutTCQ.Count > 0) msg = timeoutTCQ.Dequeue().RequestMsg;
                }
                if (msg != null)
                {
                    logger.Error($"request '{msg.ID}' timeout.");
                    ProcessTimeoutRequest(msg);
                }
            }
            //catch (Exception ex)
            //{
            //    if (msg == null)
            //    {
            //        Catch(new EOCException(ex));
            //    }
            //    else
            //    {
            //        Catch(new EOCException(ex, TExceptionType.MessageProcessingFailed, msg));
            //    }
            //}
        }

        /// <summary>
        /// watch dog has the highest authority in agent, it performs some light wight operations, such as update 'TCounters'
        /// </summary>
        private void __threadWatchdog(ThreadControl control)
        {
            logger.Debug($"{nameof(__threadWatchdog)}() on duty.");

            var feedCycle = Math.Min(1000, this.timeout >> 1);  // the cycle of dog feeding, unit: ms
            var feedTCounter = 0;
            var msg = new EMessage(0);
            var foodBag = new[] { dogFoodFlag };
            var threadInterval = 1;  // watch dog scan cycle, ms

            while (!control.SafelyTerminating)
            {
                Thread.Sleep(threadInterval);
                feedTCounter += threadInterval;
                if (IsConnected)
                {
                    // check whether the dog is dead
                    if (watchDog != -1)  // -1 denote the dog is dead，only __threadListen can set it back to '0' to rebirth the dog
                    {
                        watchDog += threadInterval;
                        if (watchDog >= timeout)
                        {
                            watchDog = -1;  // time out flag

                            GetControl(ThreadType.Listen).SafeAbort();
                            OnConnectionTimeout();
                            logger.Error($"{this} connection timeout.");
                        }
                    }
                }
                // udpate TCounter of request messages
                KeyValuePair<uint, TCounter>[] msgIdAndTCounterPairs;
                lock (dictMsgIdAndTCounter) msgIdAndTCounterPairs = dictMsgIdAndTCounter.ToArray();
                foreach (var kv in msgIdAndTCounterPairs)
                {
                    if (kv.Value.Decrease(threadInterval) || !this.IsConnected)  // request timeout
                    {
                        kv.Value.CountDown = 0;  // set countdown to 'timeout'
                        lock (timeoutTCQ)
                        {
                            timeoutTCQ.Enqueue(kv.Value);
                        }
                        RemoveWaitFlag(kv.Key);
                        ThreadPool.QueueUserWorkItem(_processTask, timeoutTCQ);
                    }
                }
                // feed the remote dog
                if (feedTCounter >= feedCycle)
                {
                    feedTCounter = 0;  // reset

                    if (IsConnected)
                    {
                        try
                        {
                            SendBytes(foodBag, 0, foodBag.Length);
                        }
                        catch
                        {
                            // pass, ignore all errors
                        }
                    }
                }
            }

            control.SetAbortedFlags();

            logger.Debug($"{nameof(__threadWatchdog)}() aborted.");

            this.Destroy();
            this.Phase = ConnectionPhase.P0Start;
        }

        /// <summary>
        /// receive date stream through socket
        /// </summary>
        /// <param name="control"></param>
        private void __threadListen(ThreadControl control)
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
                /*******************************
                 * receive bytes through socket
                 * *****************************/
                rwlsSocket.EnterReadLock();
                try
                {
                    var result = socket.BeginReceive(rbuffer, 0, rbuffer.Length, SocketFlags.None, x => x = null, null);  // async reception，to realize 'SafelyTermination'
                    while (!control.SafelyTerminating && !result.IsCompleted)  // wait for completion
                    {
                        Thread.Sleep(1);
                    }
                    if (control.SafelyTerminating || !socket.Connected) break;  // termination signal detected, stop listening
                    count = socket.EndReceive(result);
                }
                catch (SocketException se)
                {
                    logger.Error($"{se.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    logger.Fatal("fatal error occurred in __threadListen.", ex);
                    Catch(new EOCException(ex));
                    break;
                }
                finally
                {
                    rwlsSocket.ExitReadLock();
                }

                /*****************************
                 * process the received bytes
                 * ***************************/
                for (var j = 0; j < count;)  // deal with the bytes received in rbuffer
                {
                    if (msgLen == 0)
                    {
                        lenBuff[l] = rbuffer[j];
                        ++j;  // point to the 1st byte of content
                        if (++l == lenBuff.Length)
                        {
                            l = 0;
                            msgLen = BitConverter.ToUInt32(lenBuff, 0);
                            if (verifyLen != 0)  // need validation
                            {
                                if (verifyLen == msgLen)  // validatin OK
                                {
                                    watchDog = 0;  // feed local dog, reset the timer
                                    if (msgLen == 1)  // system command code
                                    {
                                        if (slot[0] == dogFoodFlag)  // dog food
                                        {
                                            // pass
                                        }
                                    }
                                    else
                                    {
                                        var msg = EMessage.FromBytes(slot, 0, k);
                                        lock (inMsgQueue) inMsgQueue.Enqueue(msg);
                                        ThreadPool.QueueUserWorkItem(_processTask, inMsgQueue);
                                    }
                                    msgLen = 0;
                                }
                                else
                                {
                                    Catch(new EOCException("transmission error, please reconnect!"));
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

            control.SetAbortedFlags();

            OnThreadListenAborted();

            logger.Debug($"{nameof(__threadListen)}() aborted.");
        }
        #endregion
    }
}
