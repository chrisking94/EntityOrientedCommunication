using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// 控制台命令
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TMCmdline : TMText
    {
        #region constructor
        [JsonConstructor]
        protected TMCmdline() { }
        public TMCmdline(string cmd) : base(cmd) { }
        #endregion

        #region interface
        /// <summary>
        /// 返回命令窗输出
        /// </summary>
        /// <returns></returns>
        public string Execute()
        {
            // 准备文件
            var startupPath = Environment.GetCommandLineArgs()[0];
            var startFileInfo = new FileInfo(startupPath);
            var batName = "purecmd.bat";
            File.WriteAllText(batName, Text);
            // 执行命令
            var tbResult = new StringBuilder(4096);
            ProcessStartInfo start = new ProcessStartInfo(batName);//设置运行的命令行文件问ping.exe文件，这个文件系统会自己找到
            start.WorkingDirectory = startFileInfo.DirectoryName;
            start.Arguments = "";//设置命令参数
            start.CreateNoWindow = true;//不显示dos命令行窗口
            start.RedirectStandardOutput = true;
            start.RedirectStandardInput = true;
            start.RedirectStandardError = true;
            start.UseShellExecute = false;//是否指定操作系统外壳进程启动程序
            Process p = Process.Start(start);
            StreamReader reader = p.StandardOutput;//截取输出流
            var errReader = p.StandardError;
            string line = reader.ReadLine();//每次读取一行
            while (!reader.EndOfStream)
            {
                tbResult.AppendLine(line);
                line = reader.ReadLine();
            }
            line = errReader.ReadLine();
            while(!errReader.EndOfStream)
            {
                tbResult.AppendLine(line);
                line = errReader.ReadLine();
            }
            p.WaitForExit();//等待程序执行完退出进程
            p.Close();//关闭进程
            reader.Close();//关闭流
            errReader.Close();
            return tbResult.ToString();
        }
        public override string ToString()
        {
            return Format("TCmd", Text);
        }
        #endregion
    }
}
