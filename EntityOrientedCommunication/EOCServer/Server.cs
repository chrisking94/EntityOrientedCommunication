using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using EntityOrientedCommunication;
using System.IO;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Utilities;

namespace TAPAServer
{
    public partial class Server
    {
        #region property
        public string Name;

        private ServerOperatorManager manager;

        private readonly TimeBlock Now;

        public string serverIp { get; private set; }

        public int serverPort { get; private set; }
        #endregion

        #region field
        private HashSet<ServerLoginAgent> loginAgents;
        private Socket socket;
        private Thread listenThread;
        private Thread transactionThread;  // 事件处理线程
        private Logger logger;
        private const uint idBaffle = 100;  // 前100个id预留做特殊用途
        private List<ServerTransaction> transactionList;
        private bool isBlocked;  // block server
        private string blockMessage;
        #endregion

        #region constructor
        static Server()
        {
            
        }

        public Server(string ip, int port)
        {
            this.serverIp = ip;
            this.serverPort = port;

            Now = new TimeBlock();

            // 初始化成员
            Name = "TServer";
            var maxConnections = 300;
            loginAgents = new HashSet<ServerLoginAgent>();
            var logfolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\TAPALogs\\";
            logfolder = "./logs/";
            if (!Directory.Exists(logfolder))
            {
                Directory.CreateDirectory(logfolder);
            }
            logger = new Logger(Name);
            transactionList = new List<ServerTransaction>(8);

            // 配置ThreadPool
            var nWorkerThreads = 100;
            var nCompletionPortThreads = 10;
            ThreadPool.SetMinThreads(nWorkerThreads, nCompletionPortThreads);
            Console.WriteLine($"thread pool: {nWorkerThreads} worker threads, {nCompletionPortThreads} completion port threads.");

            // 操作员信息管理系统
            manager = new ServerOperatorManager(this);
            Console.WriteLine("operator information management system, OK.");

            // 启动事件线程，注册事件
            transactionThread = new Thread(__transaction);
            transactionThread.IsBackground = true;
            transactionThread.Start();
            Register(new ServerTransaction("intelligently reconfigure loggers", 1000, Logger.IntelliUpdateConfiguration));  // reconfigure logger
            Register(new ServerTransaction("connection monitor", 1000, Transaction_ConnectionMonitor));

            // 配置socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ipAddr = IPAddress.Parse(serverIp.Trim());
            var point = new IPEndPoint(ipAddr, serverPort);
            socket.Bind(point);
            socket.Listen(maxConnections);
            Console.WriteLine($"socket@{serverIp}:{serverPort}, OK.");

            // 创建监听线程
            listenThread = new Thread(__listen);
            listenThread.IsBackground = true;
            listenThread.Start();

            logger.Write(LogType.PR, $"初始化完毕, {ipAddr}:{serverPort}");
        }

        public void Close()
        {
            listenThread.Abort();
            socket.Close();
        }
        #endregion

        #region interface
        #endregion

        #region private
        private void Remove(ServerLoginAgent loginAgent)
        {
            lock (loginAgents)
            {
                if (loginAgents.Contains(loginAgent))
                {
                    loginAgents.Remove(loginAgent);
                }
                //else
                //{
                //    logger.Error($"connection '{loginAgent}' not found.");
                //}
            }
        }

        private List<ServerLoginAgent> GetLoggedInAgents()
        {
            lock (loginAgents)
            {
                return loginAgents.Where(la => la.Token != null).ToList();
            }
        }

        private void Register(ServerTransaction transaction)
        {
            lock(transactionList)
            {
                transactionList.Add(transaction);
            }
        }

        private string GenToken(string username)
        {
            var randPart = (new Random()).Next(100000, int.MaxValue).ToString();

            return $"{username}{randPart}";
        }

        private void __listen()
        {
            for (; ; )
            {
                // 等待客户连接
                var agent = new ServerLoginAgent(socket.Accept(), this);

                loginAgents.Add(agent);  // managed by server
            }
        }

        private void __transaction()
        {
            var ms = 1;

            for (; ; )
            {
                lock (transactionList)
                {
                    for (var i = 0; i < transactionList.Count; ++i)
                    {
                        var transaction = transactionList[i];

                        if (ms % transaction.Interval == 0 || transaction.EmergencyDo)  // cycle
                        {
                            try
                            {
                                transaction.Do();
                            }
                            catch (Exception ex)
                            {
                                logger.Fatal($"error occurred when executing transaction '{transaction.Name}'", ex);
                                isBlocked = true;
                                blockMessage = $"事务处理线程遇到不可恢复的错误，服务器程序已停止服务，请联系服务器管理员";
                                System.Environment.Exit(-1);
                            }
                        }
                    }
                }

                Thread.Sleep(1);  // 1ms
                if (++ms == int.MaxValue / 2)
                {
                    ms = 1;
                }
            }
        }

        private void Transaction_ConnectionMonitor()
        {
            List<ServerLoginAgent> deadList;
            lock (loginAgents)
            {
                deadList = loginAgents.Where(la => la.IsDead || !la.IsConnected).ToList();
            }

            // remove
            foreach (var dead in deadList)
            {
                Remove(dead);
                logger.Info($"removing dead connection '{dead}'");
                dead.Destroy();
            }
        }
        #endregion
    }
}
