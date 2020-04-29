using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using EntityOrientedCommunication.Utilities;

namespace EntityOrientedCommunication.Server
{
    public class Server
    {
        #region property
        public string Name;

        public ServerUserManager UserManager { get; private set; }

        public readonly TimeBlock Now;

        public string serverIp { get; private set; }

        public int serverPort { get; private set; }

        public readonly ClientAgentSimulator LocalClient;
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

        public Server(string name, string ip, int port)
        {
            this.serverIp = ip;
            this.serverPort = port;

            Now = new TimeBlock();

            // initialze simple data members
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
            LocalClient = new ClientAgentSimulator();

            // configure ThreadPool
            var nWorkerThreads = 100;
            var nCompletionPortThreads = 10;
            ThreadPool.SetMinThreads(nWorkerThreads, nCompletionPortThreads);
            Console.WriteLine($"thread pool: {nWorkerThreads} worker threads, {nCompletionPortThreads} completion port threads.");

            // user management system
            UserManager = new ServerUserManager(this);
            Console.WriteLine("user management system, OK.");

            // run the 'Transaction' thread，register some transactions
            transactionThread = new Thread(__transaction);
            transactionThread.IsBackground = true;
            transactionThread.Start();
            Register(new ServerTransaction("intelligently reconfigure loggers", 1000, Logger.IntelliUpdateConfiguration));  // reconfigure logger
            Register(new ServerTransaction("connection monitor", 1000, Transaction_ConnectionMonitor));

            // configure socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ipAddr = IPAddress.Parse(serverIp.Trim());
            var point = new IPEndPoint(ipAddr, serverPort);
            socket.Bind(point);
            socket.Listen(maxConnections);
            Console.WriteLine($"socket@{serverIp}:{serverPort}, OK.");

            // create socket listener thread
            listenThread = new Thread(__listen);
            listenThread.IsBackground = true;
            listenThread.Start();

            logger.Write(LogType.PR, $"{this.Name}@{ipAddr}:{serverPort} is initilized.");
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
        internal void Remove(ServerLoginAgent loginAgent)
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

        internal List<ServerLoginAgent> GetLoggedInAgents()
        {
            lock (loginAgents)
            {
                return loginAgents.Where(la => la.Token != null).ToList();
            }
        }

        private void Register(ServerTransaction transaction)
        {
            lock (transactionList)
            {
                transactionList.Add(transaction);
            }
        }

        internal string GenToken(string username)
        {
            var randPart = (new Random()).Next(100000, int.MaxValue).ToString();

            return $"{username}{randPart}";
        }

        private void __listen()
        {
            for (; ; )
            {
                // wait for new connection request from client
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
                                blockMessage = $"tansaction has encountered an unrecoverable error, server has stopped running, please contact the server administrator for help.";
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
