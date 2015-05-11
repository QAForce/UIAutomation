using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    public class Logging
    {
        public static void SaveLog(string Message, ELogType LogType)
        {
            ExportLog(LogType, Message);
        }
        private static void ExportLog(ELogType LogType, string Message)
        {
            switch (LogType)
            {
                case ELogType.Fatal:
                    LogManager.GetLogger(LogType.ToString()).Fatal(Message);
                    break;
                case ELogType.Error:
                    LogManager.GetLogger(LogType.ToString()).Error(Message);
                    break;
                case ELogType.Warn:
                    LogManager.GetLogger(LogType.ToString()).Warn(Message);
                    break;
                case ELogType.Debug:
                    LogManager.GetLogger(LogType.ToString()).Debug(Message);
                    break;
                case ELogType.Info:
                    LogManager.GetLogger(LogType.ToString()).Info(Message);
                    break;
                default:
                    LogManager.GetLogger(typeof(Logging)).Debug(Message);
                    break;
            }
        }
    }
    public enum ELogType
    {
        /// <summary>
        /// Fatal
        /// </summary>
        Fatal,
        /// <summary>
        /// Error
        /// </summary>
        Error,
        /// <summary>
        /// Warn
        /// </summary>
        Warn,
        /// <summary>
        /// Debug
        /// </summary>
        Debug,
        /// <summary>
        /// Info
        /// </summary>
        Info

    }
}
