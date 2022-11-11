using System;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using Dalamud.Logging;
using DDD.Plugins;

namespace DDD
{
    public class Event
    {
        public EventHandler<string>? OnNewLog;

        private int logIndex;

        private string logFileName;
        FileStream logFileStream;
        private StreamWriter sw;

        protected virtual void NewLog(LogMessageType type, string log)
        {
            
            PluginLog.Debug(log);
            
            
            OnNewLog?.Invoke(this, log);
        }

        public void SetLog(LogMessageType type, string log)
        {
            if (!File.Exists(logFileName)) NewFile();
            if (type is LogMessageType.Version or LogMessageType.Territory) logIndex = 0;
            else logIndex++;
            log = (((int)type).ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + "|" + $"{DateTime.Now:O}" + "|" + log).Replace('\0', ' ');
            log = log + "|" + DDD.Plugins.LogOutput.u_65535(log + "|" + logIndex.ToString(CultureInfo.InvariantCulture));
            sw?.WriteLine(log);
            sw?.Flush();
            NewLog(type,log);
        }

         public void NewFile()
         {
             logFileName = Path.Combine(DalamudApi.PluginInterface.ConfigDirectory.FullName,
                                        $"Network_{DateTime.Today:yyyyMMdd}.log");
             if (!Directory.Exists(Path.Combine(DalamudApi.PluginInterface.ConfigDirectory.FullName)))
             {
                 Directory.CreateDirectory(Path.Combine(DalamudApi.PluginInterface.ConfigDirectory.FullName));
             }

             var exist = File.Exists(logFileName);

             logFileStream = File.Open(logFileName, FileMode.Append,FileAccess.Write,FileShare.Read);
             sw = new StreamWriter(logFileStream, Encoding.UTF8);
             if (!exist) SetLog(LogMessageType.Version, ((FormattableString)$"FFXIV_ACT_Plugin Version: 2.6.6.1 (0000000000000000)").ToString(CultureInfo.InvariantCulture));

         }

         public void CloseFile()
        {
            sw.Close();
            logFileStream.Close();
        }
    }




}
