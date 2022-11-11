using System;
using System.Globalization;
using System.Threading;
using Dalamud.Logging;
using DDD.Plugins;

namespace DDD
{
    public class Event
    {
        private string logLine = "";

        public EventHandler<string>? OnNewLog;

        private int logIndex;
        protected virtual void NewLog(LogMessageType type, string log)
        {
            PluginLog.Debug(log);
            if (type is LogMessageType.Version or LogMessageType.Territory) logIndex = 0;
            else logIndex++;
            log = ((int)type + "|" + $"{DateTime.Now:O}" + "|" + log).Replace('\0', ' ');
            log = log + "|" + DDD.Plugins.LogOutput.u_65535(log + "|" + logIndex.ToString(CultureInfo.InvariantCulture));

            OnNewLog?.Invoke(this, log);
        }

        public void SetLog(LogMessageType type, string log)
        {
            logLine = log;
            NewLog(type,logLine);
        }

    }




}
