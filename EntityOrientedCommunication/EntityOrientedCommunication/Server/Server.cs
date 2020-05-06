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
        public string Name { get; }

        public ServerUserManager UserManager { get; }

        public TimeBlock Now { get; }

        public string IP { get; }

        public int Port { get; }

        /// <summary>
        /// the user name of this local client is 'server'
        /// </summary>
        public ClientAgentSimulator LocalClient { get; }
        #endregion

        #region field
        private HashSet<ServerLoginAgent> loginAgents;
        private Socket socket;
        private Thread listenThread;
        private Logger logger;
        private TransactionPool transactionPool;
        private bool isBlocked;  // block server
        private string blockMessage;
        #endregion

        #region constructor
        static Server()
        {

        }

        public Server(string name, string ip, int port)
        {
            this.Name = name;
            this.IP = ip;
            this.Port = port;

            this.UserManager = new ServerUserManager(this);
            this.Now = new TimeBlock();
            this.LocalClient = new ClientAgentSimulator();

            // register local client user
            this.UserManager.Register(this.LocalClient.ServerSimulator.SUser);
        }
        #endregion

        #region interface
        public Server Run()
        {
            // initialze simple data members
            var maxConnections = 300;
            loginAgents = new HashSet<ServerLoginAgent>();
            var logfolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\\TAPALogs\\";
            logfolder = "./logs/";
            if (!Directory.Exists(logfolder))
            {
                Directory.CreateDirectory(logfolder);
            }
            logger = new Logger(Name);
            transactionPool = new TransactionPool();

            // configure ThreadPool
            var nWorkerThreads = 100;
            var nCompletionPortThreads = 10;
            ThreadPool.SetMinThreads(nWorkerThreads, nCompletionPortThreads);
            Console.WriteLine($"thread pool: {nWorkerThreads} worker threads, {nCompletionPortThreads} completion port threads.");

            // user management system
            Console.WriteLine($"{this.UserManager.Count} user(s) registered.");

            // transaction pool, listen error event, register some transactions
            transactionPool.TransactionErrorEvent += TransactionErrorHandler;
            transactionPool.Register(Logger.IntelliUpdateConfiguration, 1000, "intelligently reconfigure loggers");
            transactionPool.Register(Transaction_AgentsMonitor, 1000, "connection monitor");

            // configure socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ipAddr = IPAddress.Parse(IP.Trim());
            var point = new IPEndPoint(ipAddr, this.Port);
            socket.Bind(point);
            socket.Listen(maxConnections);
            Console.WriteLine($"socket@{IP}:{this.Port}, OK.");

            // create socket listener thread
            listenThread = new Thread(__listen);
            listenThread.IsBackground = true;
            listenThread.Start();

            logger.Write(LogType.PR, $"{this.Name}@{ipAddr}:{this.Port} is initilized.");

            return this;
        }

        public void Stop()
        {
            transactionPool.Destroy();
            listenThread.Abort();
            socket.Close();
        }
        #endregion

        #region private
        internal void Remove(ServerLoginAgent loginAgent)
        {
            lock (loginAgents)
            {
                loginAgents.Remove(loginAgent);
            }
        }

        internal List<ServerLoginAgent> GetLoggedInAgents()
        {
            lock (loginAgents)
            {
                return loginAgents.Where(la => la.Token != null).ToList();
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

        private void TransactionErrorHandler(object sender, TransactionErrorArgs args)
        {
            logger.Fatal($"error occurred when executing transaction '{args.transaction.Name}'", args.exception);
            isBlocked = true;
            blockMessage = $"tansaction has encountered an unrecoverable error, server has stopped running, please contact the server administrator for help.";
            System.Environment.Exit(-1);
        }

        private void Transaction_AgentsMonitor()
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
