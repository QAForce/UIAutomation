using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Logic
{
    public class Logging
    {
        /// <summary>
        /// 記錄日誌
        /// </summary>
        /// <param name="Message">訊息</param>
        /// <param name="LogType">log type</param>
        public static void SaveLog(string Message, ELogType LogType)
        {
            ExportLog(LogType, Message);
        }

        public static void LoadLog(string filePath)
        {
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(filePath));
        }
        /// <summary>
        /// Exception
        /// </summary>
        /// <param name="exp">Exception</param>
        /// <param name="LogType">log type</param>
        public static void SaveLog(Exception exp, ELogType LogType)
        {
            string strErrMsg = exp.Message + "\r\n";
            strErrMsg = strErrMsg + "Source:" + exp.Source.ToString() + "\r\n";
            strErrMsg = strErrMsg + "StackTrace:" + exp.StackTrace;
            ExportLog(LogType, strErrMsg);
        }

        private static void ExportLog(ELogType LogType, string Message)
        {
            switch (LogType)
            {
                case ELogType.Fatal:
                    LogManager.Exists(LogType.ToString()).Fatal(Message);
                    break;
                case ELogType.Error:
                    LogManager.Exists(LogType.ToString()).Error(Message);
                    break;
                case ELogType.Warn:
                    LogManager.Exists(LogType.ToString()).Warn(Message);
                    break;
                case ELogType.Debug:
                    LogManager.Exists(LogType.ToString()).Debug(Message);
                    break;
                case ELogType.Info:
                    LogManager.Exists(LogType.ToString()).Info(Message);
                    break;
                default:
                    LogManager.Exists("Debug").Debug(Message);
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
