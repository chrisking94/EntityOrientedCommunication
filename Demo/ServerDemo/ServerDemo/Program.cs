using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityOrientedCommunication;
using System.Diagnostics;
using EntityOrientedCommunication;
using System.IO;
using System.Threading;
using EntityOrientedCommunication.Utilities;
using ServerDemo;

namespace EntityOrientedCommunication.Server
{
    class Program
    {
        static string startupPath = Environment.GetCommandLineArgs()[0];
        static string startupFolder;
        static string runningFolder;
        static string appName = "TAPAServer";
        static Server server;
        public static void Main(string[] args)
        {
            startupFolder = startupPath.Replace(@"Debug\TAPAServer.exe", @"Debug\");
            runningFolder = startupPath.Replace(@"Debug\TAPAServer.exe", @"Running\");

            var serverIni = new IniFile(@"./server.ini");
            var bDebug = serverIni.Read("Alpha", "Mode") == "Debug";
            var serverIp = serverIni.Read("Network", "IP", "127.0.0.1");
            var serverPort = serverIni.Read("Network", "Port", 1350);
            if (bDebug)
            {
                Console.WriteLine($"提示：u.升级 s.启动服务 c.启动远程管理");
                for (; ; )
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.U)  // update
                    {
                        StartControl("u");
                    }
                    else if (key.Key == ConsoleKey.S)  // service
                    {
                        server = new Server("TServer", serverIp, serverPort);
                    }
                    else if(key.Key == ConsoleKey.C)  // control
                    {
                        StartControl("");
                    }
                }
            }
            else
            {
                server = new Server("TServer", serverIp, serverPort);
            }

            {
                // 添加200个测试用户
                var testUCount = 200;
                for (var i = 0; i < testUCount; ++i)
                {
                    var uname = $"user{i}";
                    var user = new UserInfo(uname);
                    user.NickName = $"测试用户{i}";
                    server.UserManager.Register(user);
                }
                Console.WriteLine($"添加了{testUCount}个测试账户");
            }

            server?.Run();

            // resource utilization monitor
            var name = Process.GetCurrentProcess().ProcessName;
            var cpuCounter = new PerformanceCounter("Process", "% Processor Time", name);
            var ramCounter = new PerformanceCounter("Process", "Working Set", name);
            for (; ; )
            {
                Console.Title = string.Format("{0}  {1}@{2}:{3}  CPU: {4:F2}%  RAM: {5}",
                    System.Environment.CurrentDirectory,
                    server.Name,
                    server.IP,
                    server.Port,
                    cpuCounter.NextValue(),
                    StringFormatter.ByteCountToString((int)ramCounter.NextValue()));
                Thread.Sleep(1000);
            }
        }

        static void StartControl(string arguments)
        {
            // 启动升级程序
            var updaterPath = @"E:\TAPA_BACK\TAPA3_Back\Updater\bin\Debug\Updater.exe";
            var uinfo = new FileInfo(updaterPath);
            var process = new Process();
            process.StartInfo.WorkingDirectory = uinfo.DirectoryName;
            process.StartInfo.FileName = uinfo.Name;
            process.StartInfo.Arguments = arguments;
            process.Start();
        }

        private static void StartProcess()
        {
            try
            {
                if (!CheckProcessExists())
                {
                    Process p = new Process();
                    p.StartInfo.WorkingDirectory = runningFolder;
                    p.StartInfo.FileName = System.IO.Path.Combine(runningFolder, $"{appName}.exe");
                    p.StartInfo.Arguments = $"{appName}.exe";
                    p.StartInfo.UseShellExecute = true;
                    p.Start();
                    p.WaitForInputIdle(10000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Source + " " + ex.Message);
            }
        }

        private static bool CheckProcessExists()
        {
            Process[] processes = Process.GetProcessesByName(appName);
            foreach (Process p in processes)
            {
                if (System.IO.Path.Combine(runningFolder, $"{appName}.exe") == p.MainModule.FileName)
                    return true;
            }
            return false;
        }

        private static void KillProcessExists()
        {
            Process[] processes = Process.GetProcessesByName(appName);
            foreach (Process p in processes)
            {
                if (System.IO.Path.Combine(runningFolder, $"{appName}.exe") == p.MainModule.FileName)
                {
                    p.Kill();
                    p.Close();
                }
            }
        }
    }
}
