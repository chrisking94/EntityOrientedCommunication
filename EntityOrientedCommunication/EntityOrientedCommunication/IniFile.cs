using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using EntityOrientedCommunication.Utilities;

namespace EntityOrientedCommunication
{
    public class IniFile
    {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        #region field
        public static readonly IniFile Default;

        private string path;
        #endregion

        #region constructor
        static IniFile()
        {
            Default = new IniFile("./config.ini");
        }

        public IniFile(string path)
        {
            this.path = path;
        }
        #endregion

        #region interface
        public void Write(string section, string key, object value)
        {
            WritePrivateProfileString(section, key, value.ToString(), path);
        }

        public string Read(string section, string key, string defaultValue = "")
        {
            int size = 255;
            StringBuilder temp = new StringBuilder(size);
            GetPrivateProfileString(section, key, defaultValue, temp, size, path);
            return temp.ToString();
        }

        public T Read<T>(string section, string key, T defaultValue)
        {
            var s = Read(section, key, defaultValue.ToString());

            return (T)typeof(T).Cast(s);
        }

        public override string ToString()
        {
            return Path.GetFileName(path);
        }
        #endregion

        #region global
        public static void GlobalWrite(string section, string key, string value)
        {
            Default.Write(section, key, value);
        }

        public static string GlobalRead(string section, string key, string defaultValue = "")
        {
            return Default.Read(section, key, defaultValue);
        }
        #endregion
    }
}
