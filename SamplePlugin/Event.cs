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

        public void SetLog(LogMessageType type, string text)
        {
            if (!File.Exists(logFileName)) NewFile();
            if (type is LogMessageType.Version or LogMessageType.Territory) logIndex = 0;
            else logIndex++;
            //log = (((int)type).ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + "|" + $"{DateTime.Now:O}" + "|" + log).Replace('\0', ' ');
            var num = (int)type;
            var array = new string[5];
            array[0] = num.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
            array[1] = "|";
            array[2] = DateTime.Now.ToString("O");
            array[3] = "|";
            array[4] = text.Replace('\0', ' ');
            text = string.Concat(array);
            text = text + "|" + LogOutput.u_65535(text + "|" + Interlocked.Increment(ref logIndex).ToString(CultureInfo.InvariantCulture));
            sw?.WriteLine(text);
            sw?.Flush();
            NewLog(type, text);
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
             if (!exist) SetLog(LogMessageType.Version, ((FormattableString)$"FFXIV_ACT_Plugin Version: 2.6.6.1 (50BCD605C50A749F)").ToString(CultureInfo.InvariantCulture));

         }

         public void CloseFile()
        {
            sw.Close();
            logFileStream.Close();
        }
    }




}
