/* ============================================================================================
										RADIATION RISK            
				   
 * author		：chris
 * create time	：5/14/2019 9:45:20 AM
 * ============================================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using EntityOrientedCommunication.Utilities;
using EntityOrientedCommunication;
using EntityOrientedCommunication.Client;

namespace ClientDemo
{
    public static class TAPAClient
    {
        #region data
        private static ClientAgent _agent;
        public static ClientAgent Agent  // lazy creation
        {
            get
            {
                if (_agent is null) _agent = new ClientAgent(DefaultIP, DefaultPort);
                return _agent;
            }
        }

        public static TimeBlock Now => Agent.Now;

        public static bool IsAutoLogin
        {
            get => iniFile.Read("Client", "AutoLogin", false);
            set => iniFile.Write("Client", "AutoLogin", value);
        }

        public static string DefaultUsername => iniFile.Read("Client", "Username", "");

        public static string DefaultPassword
        {
            get => iniFile.Read("Client", "Password", "").Decrypt();
            set => iniFile.Write("Client", "Password", value.Encrypt());
        }

        public static string DefaultIP
        {
            get => iniFile.Read("Server", "ip", "10.10.10.254");
        }

        public static int DefaultPort
        {
            get => iniFile.Read("Server", "port", 1350);
        }

        private static readonly IniFile iniFile;
        #endregion

        #region constructor
        static TAPAClient()
        {
            iniFile = new IniFile("./TAPACsBase.ini");
        }
        #endregion

        #region interface
        public static void Login(string username, string password, int timeout = 4000)
        {
            Agent.Login(username, password, timeout);
            
            iniFile.Write("Client", "Username", username);
        }

        public static void Logoff()
        {
            Agent.Logout();
        }
        #endregion

        #region private
        private static readonly string passPhrase = "acbxcv101235..";

        private static string Encrypt(this string plainText)
        {
            return Encryption.EncryptString(plainText, passPhrase);
        }

        private static string Decrypt(this string cipherText)
        {
            return Encryption.DecryptString(cipherText, passPhrase);
        }
        #endregion
    }
}
