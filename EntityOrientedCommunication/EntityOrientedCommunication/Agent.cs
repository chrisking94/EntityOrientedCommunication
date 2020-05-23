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
        /// <summary>
        /// the initial state
        /// </summary>
        P0Start,
        /// <summary>
        /// server and client are connected
        /// </summary>
        P1Connected,
        /// <summary>
        /// client has logged in
        /// </summary>
        P2LoggedIn,
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
        private const byte DOG_FOOD_FLAG = 0xdf;

        private Socket socket;  // byte data sender and receiver
        protected readonly ReaderWriterLockSlim rwlsSocket =  // the lock for field 'socket' of this agent
            new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);  

        private int bufferSize = 65535;

        private int slotSize;

        private int watchDog = 0;  // watch dog time counter, unit: ms

        protected readonly int timeout = 10000;  // default request timeout in milliseconds
        /// <summary>
        /// message ID to TCounter mapping
        /// </summary>
        private Dictionary<uint, TCounter> dictMsgId2TC;

        private Mutex sendMutex;

        protected readonly Logger logger;

        private uint envelope;  // the ID of message which will be transferred

        private Dictionary<ThreadType, ThreadControl> dictThreadType2Control;  // maintains some thread controller
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

            dictMsgId2TC = new Dictionary<uint, TCounter>(8);
            slotSize = bufferSize << 1;
            sendMutex = new Mutex();
            logger = new Logger("@@@");
            dictThreadType2Control = new Dictionary<ThreadType, ThreadControl>()
            {
                { ThreadType.Listen, new ThreadControl(ThreadType.Listen.ToString(), __threadListen, CloseSocket) },
                { ThreadType.WatchDog, new ThreadControl(ThreadType.WatchDog.ToString(), __threadWatchdog) },
            };
            this.envelope = GetInitialEnvelope();

            // start watchdog
            GetControl(ThreadType.WatchDog).Start();
        }
        #endregion

        #region interface
        /// <summary>
        /// wait for connection phase of this agent encounter the specified 'phase'
        /// </summary>
        /// <param name="ph"></param>
        public void WaitTill(ConnectionPhase ph)
        {
            while (Phase < ph) Thread.Sleep(1);
        }

        /// <summary>
        /// this agent can not be recovered after it is destroyed
        /// </summary>
        public virtual void Destroy()
        {
            // stop all treads
            foreach (var control in dictThreadType2Control.Values)
            {
                control.AsyncSafeAbort();
            }

            // close socket
            CloseSocket();
        }

        public override string ToString()
        {
            return $"[Agent]{ClientName}<->{TeleClientName}";
        }
        #endregion

        #region private
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
            if (dictThreadType2Control.ContainsKey(threadType))
            {
                return dictThreadType2Control[threadType];
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
        /// asynchronized request, similiar to 'Request', but this method will not wait for the remote computer to response
        /// </summary>
        /// <param name="status"></param>
        /// <param name="msg"></param>
        /// <param name="timeout">in milliseconds</param>
        /// <returns>the time counter for the request 'msg'</returns>
        protected TCounter AsyncRequest(StatusCode status, EMessage msg, int timeout = -1)
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

        private TCounter SetWaitFlag(EMessage msg, int timeout)  // set the wait flag of a request message at given timeout milliseconds
        {
            var tc = new TCounter(msg, timeout);
            lock (dictMsgId2TC) dictMsgId2TC[msg.ID] = tc;
            return tc;
        }

        private TCounter RemoveWaitFlag(uint msgId)  // remove the wait flag of a request message
        {
            lock (dictMsgId2TC)
            {
                var tc = dictMsgId2TC[msgId];
                dictMsgId2TC.Remove(msgId);
                return tc;
            }
        }

        protected Envelope GetEnvelope()
        {
            var env = new Envelope(envelope);
            envelope += 2;
            if (uint.MaxValue - envelope < 2)
            {
                this.envelope = GetInitialEnvelope();
            }
            return env;
        }

        /// <summary>
        /// get the start envelope number, odd for client, even for server
        /// </summary>
        protected virtual uint GetInitialEnvelope()
        {
            return 100;
        }

        /// <summary>
        /// append 'msg' to the delivery queue
        /// </summary>
        /// <param name="msg"></param>
        protected void SendMessage(EMessage msg)
        {
            ThreadPool.QueueUserWorkItem(_processOutMessage, msg);  // create a task
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
            SendRaw(lenBuff, 0, lenBuff.Length);  // header, length code
            SendRaw(bytes, offset, count);  // content
            SendRaw(lenBuff, 0, lenBuff.Length);  // check code
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
        /// <para>warning：do not block this method, otherwise program might encounter a connection timeout error due to that the response is not sent to remote agent in time</para>
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
        /// process the request message which is timeout
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
            // pass
        }

        private void _processOutMessage(object msgObj)  // async method
        {
            var msg = msgObj as EMessage;

            // pre-processing
            PreprocessOutMessage(ref msg);

            if (msg.Status == StatusCode.None)
            {
                throw new Exception($"{msg.GetType().Name}.{nameof(EMessage.Status)} can't be None");
            }

            // transmission
            var bytes = msg.ToBytes();

            logger.Write(LogType.OT, msg);

            SendBytes(bytes, 0, bytes.Length);
        }

        private void _processInMessage(object msgObj)  // async method
        {
            var msg = msgObj as EMessage;

            try
            {
                // pre-processing
                PreprocessInMessage(ref msg);

                logger.Write(LogType.IN, msg);

                if (msg.HasFlag(StatusCode.Response))
                {
                    TCounter tc = null;
                    lock (dictMsgId2TC)
                    {
                        dictMsgId2TC.TryGetValue(msg.ID, out tc);
                    }

                    if (tc != null)
                    {
                        tc.SetReply(msg);
                        RemoveWaitFlag(msg.ID);

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
            }
            catch (Exception ex)
            {
                this.Catch(new EOCException(ex));
            }
        }

        private void _processTimeoutMessage(object msgObj)  // async method
        {
            var msg = msgObj as EMessage;

            logger.Error($"request '{msg.ID}' timeout.");

            try
            {
                // the actual process
                ProcessTimeoutRequest(msg);
            }
            catch (Exception ex)
            {
                Catch(new EOCException(ex, TExceptionType.TimeoutMessageProcessingFailed, msg));
            }
        }

        /// <summary>
        /// watch dog has the highest authority in agent, it performs some light weight operations, such as update 'TCounters'
        /// </summary>
        private void __threadWatchdog(ThreadControl control)
        {
            logger.Debug($"{nameof(__threadWatchdog)}() on duty.");

            var feedCycle = Math.Min(1000, this.timeout >> 1);  // the cycle of dog feeding, unit: ms
            var feedTCounter = 0;
            var msg = new EMessage(0);
            var foodBag = new[] { DOG_FOOD_FLAG };
            var threadInterval = 1;  // watch dog scan cycle, ms

            while (!control.SafelyTerminating)
            {
                Thread.Sleep(threadInterval);

                // -1 denote that the dog is dead，only __threadListen can set it back to '0' to rebirth the dog
                if (watchDog == -1) continue;  // no more actions when dog has died


                /*******************
                 * update TCounters
                 * *****************/
                KeyValuePair<uint, TCounter>[] msgIdAndTCounterPairs;
                lock (dictMsgId2TC) msgIdAndTCounterPairs = dictMsgId2TC.ToArray();
                foreach (var kv in msgIdAndTCounterPairs)
                {
                    if (kv.Value.Decrease(threadInterval) || !this.IsConnected)  // request timeout
                    {
                        kv.Value.CountDown = 0;  // set countdown to 'timeout'
                        RemoveWaitFlag(kv.Key);
                        ThreadPool.QueueUserWorkItem(_processTimeoutMessage, kv.Value.RequestMsg);
                    }
                }

                /************
                 * watch dog
                 * **********/
                if (IsConnected)
                {

                    // check local dog
                    watchDog += threadInterval;
                    if (watchDog >= timeout)
                    {
                        watchDog = -1;  // time out flag

                        GetControl(ThreadType.Listen).SafeAbort();
                        OnConnectionTimeout();
                        logger.Error($"{this} connection timeout.");
                    }

                    // feed remote dog
                    feedTCounter += threadInterval;
                    if (feedTCounter >= feedCycle)
                    {
                        feedTCounter = 0;  // reset

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

            var count = 1;  // the actual received count of bytes by socket
            var lenBuff = new byte[sizeof(uint)];  // store the message length number in byte format
            var slot = new byte[slotSize];  // reception slot, it could maintain the integral data of a message
            int k = 0, l = 0;  // pointer for slot, pointer for lenBuff
            uint msgLen = 0, verifyLen = 0;  // message header, check code
            var rbuffer = new byte[bufferSize];  // reception buffer
            var slotBackup = slot;  // switch back to the primal slot after using super slot

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
                                        if (slot[0] == DOG_FOOD_FLAG)  // dog food
                                        {
                                            // pass
                                        }
                                    }
                                    else
                                    {
                                        var msg = EMessage.FromBytes(slot, 0, k);
                                        ThreadPool.QueueUserWorkItem(_processInMessage, msg);
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
