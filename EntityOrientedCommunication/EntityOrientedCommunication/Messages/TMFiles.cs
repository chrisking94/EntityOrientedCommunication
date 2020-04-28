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
    [JsonObject(MemberSerialization.OptIn)]
    public class TMFiles : TMBox<TMFiles.MemoryFile>
    {
        #region nested class
        [JsonObject(MemberSerialization.OptIn)]
        public class MemoryFile
        {
            [JsonProperty]
            public string Name;
            [JsonProperty]
            public byte[] Data;
            [JsonConstructor]
            private MemoryFile() { }
            public MemoryFile(string name, byte[] data)
            {
                Name = name;
                Data = data;
            }
            public void SaveAs(string name)
            {
                Name = name;
            }
            public override string ToString()
            {
                return $"{Name}, {Data.Length}B";
            }
        }
        #endregion

        #region constructor
        [JsonConstructor]
        public TMFiles()
        {
            Object = new List<MemoryFile>();
        }
        public TMFiles(TMessage toReply) : base(toReply, 8)
        {

        }
        #endregion

        #region interface
        public bool Add(string filePathAndPattern)
        {
            try
            {
                var fileName = Path.GetFileName(filePathAndPattern);
                var directory = Path.GetDirectoryName(filePathAndPattern);
                if (directory == "") directory = "./";
                foreach (var pattern in fileName.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    foreach (var fp in Directory.GetFiles(directory, pattern))
                    {
                        fileName = Path.GetFileName(fp);
                        var file = new MemoryFile(fileName, File.ReadAllBytes(fp));
                        Items.Add(file);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                ex.GetHashCode();
                return false;
            }
        }
        public void Save(string folder = null)
        {
            if (folder == null) folder = "./files/";
            else folder = $"{folder}/";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            foreach (MemoryFile file in Items)
            {
                File.WriteAllBytes($"{folder}{file.Name}", file.Data);
            }
        }
        #endregion
    }
}
