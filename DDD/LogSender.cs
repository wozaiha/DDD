using Dalamud.Logging;
using H.Pipes;
using System;
using System.Diagnostics;

namespace DDD
{
    public class LogSender
    {
        private PipeServer<string> pipeServer;
        public  LogSender()
        {
            var pipeName = $"DDD{Process.GetCurrentProcess().Id}";
            pipeServer = new PipeServer<string>(pipeName);
            pipeServer.ClientConnected += async (o, args) =>
            {
                try
                {
                    Send(new() { Type = 251, Message = "接入大喇叭" });
                    PluginLog.Debug($"CLient {args.Connection.PipeName} is conneected");
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex.Message);
                    PluginLog.Error(ex.StackTrace);
                }

            };
            pipeServer.ClientDisconnected += (o, args) =>
            {
                PluginLog.Debug($"CLient {args.Connection.PipeName} is disconneected");

            };
            pipeServer.ExceptionOccurred += (o, args) =>
            {
                PluginLog.Error(args.Exception.Message);
                PluginLog.Error(args.Exception.StackTrace);
            };


            pipeServer.StartAsync();
        }
        public void Dispose()
        {
            pipeServer.DisposeAsync();
        }

        public void Send(LogMessage message)
        {
            pipeServer.WriteAsync($"{(int)message.Type}#{message.Message}");
        }


        public class LogMessage
        {
            public int Type { get; set; }
            public string Message { get; set; }
        }
    }

}
