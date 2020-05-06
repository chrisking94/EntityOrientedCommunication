using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using System.Diagnostics;
using NLog;
using NLog.LayoutRenderers;
using NLog.Config;
using System.IO;
using EntityOrientedCommunication.Messages;

namespace EntityOrientedCommunication
{
    public enum LogType
    {
        IN,  // message in
        OT,  // message out
        ER,  // error
        WR,  // waring
        PR,  // prompt
        FT,  // fatal error
        TC,  // trace
        DB,  // debug
    }

    public class Logger
    {
        #region property
        public string Owner => owner;

        public string LastError { get; private set; }
        #endregion

        #region field
        private string owner;

        private readonly static NLog.Logger logger;

        private static readonly Dictionary<LogType, LogLevel> typeErrorMap =
            new Dictionary<LogType, LogLevel>()
            {
                {LogType.DB, LogLevel.Debug },
                {LogType.ER, LogLevel.Error },
                {LogType.FT, LogLevel.Fatal },
                {LogType.IN, LogLevel.Info },
                {LogType.OT, LogLevel.Info },
                {LogType.PR, LogLevel.Info },
                {LogType.TC, LogLevel.Trace },
                {LogType.WR, LogLevel.Warn },
            };

        private static DateTime lastDateTime;
        #endregion

        #region constructor
        static Logger()
        {
            var appName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

            logger = LogManager.GetLogger(appName);
        }

        public Logger(string owner)
        {
            this.owner = owner;
        }
        #endregion

        #region interface
        public void Write(LogType type, string msg)
        {
            var level = typeErrorMap[type];
            var eventInfo = new LogEventInfo(level, logger.Name, msg);
            eventInfo.Properties["msg_type"] = type;
            eventInfo.Properties["sender"] = owner;
            logger.Log(eventInfo);

            if(level >= LogLevel.Error)
            {
                LastError = $"{DateTime.Now} {msg}";
            }
        }

        public void Info(string msg)
        {
            Write(LogType.PR, msg);
        }

        public void Debug(string msg)
        {
            Write(LogType.DB, msg);
        }

        public void Trace(string msg)
        {
            Write(LogType.TC, msg);
        }

        public void Error(string msg)
        {
            var trace = new StackTrace();
            var frame = trace.GetFrame(1);
            Write(LogType.ER, $"{frame.GetMethod().Name}() {msg}");
        }
        public void Fatal(string msg, Exception ex)
        {
            var errMsg = $"{msg}\r\n----{ex.Message}\r\n{ex.StackTrace}";
            Write(LogType.FT, errMsg);
        }
        public virtual void Write(LogType type, EMessage msg)
        {
            Write(type, msg.ToString());
        }

        public void SetOwner(string owner)
        {
            this.owner = owner;
        }

        /// <summary>
        /// update configuration from NLog.config automatically, this function will be invoked repeatedly by a thread.
        /// </summary>
        public static void IntelliUpdateConfiguration()
        {
            if (DateTime.Now.Day > lastDateTime.Day)  // update target file's name when 'day' of DateTime.Now changed.
            {
                LogManager.ReconfigExistingLoggers();
            }

            lastDateTime = DateTime.Now;
        }
        #endregion
    }
}
