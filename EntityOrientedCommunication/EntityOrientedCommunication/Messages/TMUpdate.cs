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
    /// 软件升级
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TMUpdate : TMessage
    {
        #region struct
        public struct MemoryFile
        {
            public string Name;
            public byte[] Data;
            public MemoryFile(string name, byte[] data)
            {
                Name = name;
                Data = data;
            }
            public override string ToString()
            {
                return $"{Name}, {Data.Length}B";
            }
        }
        #endregion

        #region field
        [JsonProperty]
        private List<MemoryFile> files;
        [JsonProperty]
        private string excmd;
        #endregion

        #region constructor
        [JsonConstructor]
        public TMUpdate()
        {
            files = new List<MemoryFile>();
            Status = StatusCode.Update;
            excmd = "";
        }
        public TMUpdate(string excmd): this()
        {
            this.excmd = excmd;
        }
        #endregion

        #region interface
        public void Add(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var file = new MemoryFile(fileName, File.ReadAllBytes(filePath));
            files.Add(file);
        }
        public void Update()
        {
            // 准备文件
            var sb = new StringBuilder(1024);
            var startupPath = Environment.GetCommandLineArgs()[0];
            var startFileInfo = new FileInfo(startupPath);
            sb.AppendLine($"taskkill /im {startFileInfo.Name}");
            sb.AppendLine($"ping 127.0.0.1 -n 1 > nul");
            sb.AppendLine(excmd);
            foreach (var file in files)
            {
                File.WriteAllBytes($"./{file.Name}.upd", file.Data);
                sb.AppendLine($"move /y {file.Name}.upd {file.Name}");
            }
            //sb.AppendLine($"start /max {startFileInfo.Name}");
            var batName = "update.bat";
            sb.AppendLine($"del /f {batName}");
            var scmd = sb.ToString();
            File.WriteAllText(batName, scmd);
            // 开始更新
            var process = new Process();
            process.StartInfo.WorkingDirectory = startFileInfo.DirectoryName;
            process.StartInfo.FileName = batName;
            //process.StartInfo.CreateNoWindow = false;
            process.Start();
        }
        public override string ToString()
        {
            return Format("TUpd", $"Count={files.Count}");
        }
        #endregion
    }
}
