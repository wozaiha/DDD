using System;
using System.Runtime.InteropServices;
using Dalamud.Logging;

namespace SamplePlugin
{
    public class Event
    {
        private string logLine = "";

        public EventHandler<string>? OnNewLog;
        protected virtual void NewLog(string log)
        {
            PluginLog.Debug(log);
            OnNewLog?.Invoke(this,log);
        }

        public void SetLog(string log)
        {
            logLine = log;
            NewLog(logLine);
        }

    }




}
