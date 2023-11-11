using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{
    public enum LogType
    {
        Message,
        Warning,
        Error
    }

    public class Log
    {
        public LogType logType;
        public string message;

        public Log(string message)
        {
            this.message = message;
            logType = LogType.Message;
        }

        public Log(string message, LogType logType)
        {
            this.message = message;
            this.logType = logType;
        }
    }

    public class ConsoleManager
    {
        public bool ConsoleUpdated = false;
        public List<Log> Logs = new List<Log>();
        public Dictionary<LogType, System.Numerics.Vector4> LogColors = new Dictionary<LogType, System.Numerics.Vector4>();

        public ConsoleManager() 
        {
            LogColors.Add(LogType.Message, new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            LogColors.Add(LogType.Warning, new System.Numerics.Vector4(1.0f, 0.5f, 0.0f, 1.0f));
            LogColors.Add(LogType.Error, new System.Numerics.Vector4(1.0f, 0.0f, 0.0f, 1.0f));

            Logs.Add(new Log("Project NAME loaded"));
        }

        public void AddLog(string log, LogType logType = LogType.Message)
        {
            Logs.Add(new Log(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + log, logType));
        }

    }
}
