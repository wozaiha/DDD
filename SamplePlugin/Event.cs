using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using DDD.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DDD
{
    public class Event
    {
        public EventHandler<string>? OnNewLog;
        private static LogSender LogSender = new();
        private int logIndex;

        private string logFileName;
        FileStream logFileStream;
        private StreamWriter sw;
        public bool Output = false;

        protected virtual async void NewLog(LogMessageType type, string log)
        {

            PluginLog.Verbose(log);
            OnNewLog?.Invoke(this, log);
        }
        

        public void SetLog(LogMessageType type, string text, DateTime time)
        {
            if (DalamudApi.Condition[ConditionFlag.DutyRecorderPlayback])
            {
                LogSender.Send(new() { Type = (int)type, Message = text });
            }
            
            if (type is LogMessageType.Version or LogMessageType.Territory) logIndex = 0;
            //log = (((int)type).ToString(CultureInfo.InvariantCulture).PadLeft(2, '0') + "|" + $"{DateTime.Now:O}" + "|" + log).Replace('\0', ' ');
            var num = (int)type;
            var array = new string[5];
            array[0] = num.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0');
            array[1] = "|";
            array[2] = time.ToString("O");
            array[3] = "|";
            array[4] = text.Replace('\0', ' ');
            text = string.Concat(array);
            text = text + "|" + LogOutput.u_65535(text + "|" + Interlocked.Increment(ref logIndex).ToString(CultureInfo.InvariantCulture));

            if (Output)
            {
                if (!File.Exists(logFileName)) NewFile();
                sw?.WriteLine(text);
                sw?.Flush();
            }
            
            //send(type,text);
            NewLog(type, text);
        }
        protected  async void send(LogMessageType type, string log)
        {

            string PostUrl = "http://127.0.0.1:2019/act";
            JObject patientinfo = new JObject();
            patientinfo["type"] = (int)type;
            patientinfo["text"] = log;
            string sendData = JsonConvert.SerializeObject(patientinfo);
            var resultData = await SendJson(PostUrl, sendData);

        }
        public async Task<string> SendJson(string url, string json)
        {
            try
            {
                var httpWebRequest = HttpWebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpResponse.StatusCode != HttpStatusCode.NoContent)
                {

                }
                return "";
                //using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                //{
                //    return streamReader.ReadToEnd();
                //}
            }
            catch (Exception)
            {

                return "";
            }
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

            logFileStream = File.Open(logFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
            sw = new StreamWriter(logFileStream, Encoding.UTF8);
            if (!exist) SetLog(LogMessageType.Version, ((FormattableString)$"Created by DDD based on FFXIV_ACT_Plugin Version: 2.6.6.1 @ /wozaiha/DD").ToString(CultureInfo.InvariantCulture),DateTime.Now);

         }

         public void CloseFile()
        {
            sw?.Close();
            logFileStream?.Close();
            LogSender.Dispose();
        }
    }




}
