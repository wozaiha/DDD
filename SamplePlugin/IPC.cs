using System;
using Dalamud.Logging;
using Dalamud.Plugin.Ipc;

namespace DDD
{
    public class IPC
    {
        public ICallGateProvider<string, string> IpcProvider;
        public ICallGateSubscriber<string, string> CallGateSubscriber;
        private Plugin Plugin;

        public void InitIpc(Plugin plugin)
        {
            try
            {
                Plugin = plugin;
                IpcProvider = DalamudApi.PluginInterface.GetIpcProvider<string, string>("DDD.Ipc");
                Plugin.eventHandle.OnNewLog += (sender, str) => IpcProvider.SendMessage(str); ;
            }
            catch (Exception e)
            {
                PluginLog.Error($"Error registering IPC provider:\n{e}");
            }
        }



        public void InitSub(Plugin plugin)
        {
            var str = "";
            try
            {
                CallGateSubscriber = DalamudApi.PluginInterface.GetIpcSubscriber<string, string>("DDD.Ipc");
                CallGateSubscriber.Subscribe(Action);
            }
            catch (Exception e)
            {
                PluginLog.Error($"Error registering IPC Sub:\n{e}");
            }
        }

        private void Action(string obj)
        {
            //DO something;
            PluginLog.Warning($"UNSUB!!!|{obj}");

        }

        public void Unsub()
        {
            CallGateSubscriber?.Unsubscribe(Action);
        }

    }





}
