using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using EntityOrientedCommunication.Facilities;
using EntityOrientedCommunication.Messages;
using EntityOrientedCommunication.Client;

namespace EntityOrientedCommunication.Server
{
    public class Server
    {
        #region property
        public string Name { get; }

        public ServerMailCenter MailCenter { get; }

        public DateTime Now => nowBlock.Value;

        public string IP { get; }

        public int Port { get; }

        /// <summary>
        /// the username of this local client is 'server'
        /// </summary>
        public ClientPostOffice LocalClient { get; }

        /// <summary>
        /// this property is set by server when some error occurred
        /// </summary>
        public string BlockMessage { get; private set; }
        #endregion

        #region field
        private HashSet<IServerAgent> loginAgents;

        private Socket socket;

        private Thread listenThread;

        private Logger logger;

        private TransactionPool transactionPool;

        private TimeBlock nowBlock;
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

            this.MailCenter = new ServerMailCenter(this);
            this.nowBlock = new TimeBlock();

            // create local client
            this.LocalClient = new ClientPostOffice("server");
            var clientSimulator = this.LocalClient.ConnectSimulator();
            this.MailCenter.Register(clientSimulator.ServerSimulator.User);
        }
        #endregion

        #region interface
        public void Run()
        {
            // initialze simple data members
            var maxConnections = 300;
            loginAgents = new HashSet<IServerAgent>();
            this.Add(this.LocalClient.GetDispatcher<ClientAgentSimulator>().ServerSimulator);  // register server agent simulator
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
            Console.WriteLine($"{this.MailCenter.Count} user(s) registered.");

            // transaction pool, listen error event, register some transactions
            transactionPool.TransactionErrorEvent += TransactionErrorHandler;
            transactionPool.Register(Logger.IntelliUpdateConfiguration, 1000, "intelligently reconfigure loggers");
            transactionPool.Register(Transaction_AgentsMonitor, 10, "connection monitor");

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
        }

        /// <summary>
        /// block/unblock server, pass message as null to unblock server, otherwise to block it.
        /// </summary>
        /// <param name="message"></param>
        public void Block(string message)
        {
            this.BlockMessage = message;
        }

        public void Stop()
        {
            transactionPool.Destroy();
            listenThread.Abort();
            socket.Close();
        }
        #endregion

        #region private
        internal void Remove(IServerAgent loginAgent)
        {
            lock (loginAgents)
            {
                loginAgents.Remove(loginAgent);
            }
        }

        private void Add(IServerAgent agent)
        {
            lock (loginAgents)
            {
                loginAgents.Add(agent);  // managed by server
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
                var agent = new ServerAgent(socket.Accept(), this);

                this.Add(agent);
            }
        }

        private void TransactionErrorHandler(object sender, TransactionErrorArgs args)
        {
            logger.Fatal($"error occurred when executing transaction '{args.transaction.Name}'", args.exception);
            this.Block($"tansaction has encountered an unrecoverable error, server has stopped running, please contact the server administrator for help.");
            System.Environment.Exit(-1);
        }

        private void Transaction_AgentsMonitor()
        {
            List<IServerAgent> deadList;
            lock (loginAgents)
            {
                deadList = loginAgents.Where(la => !la.IsConnected).ToList();
            }

            // remove
            foreach (var dead in deadList)
            {
                logger.Info($"removing dead connection '{dead}'");
                dead.Destroy();

                this.Remove(dead);
            }
        }
        #endregion
    }
}
