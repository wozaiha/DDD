using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Network;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using SamplePlugin.Windows;
using SamplePlugin.Plugins;
using SamplePlugin.Struct;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace SamplePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Sample Plugin";
        private const string CommandName = "/pmycommand";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("SamplePlugin");
        private Dictionary<string, uint> opCodes;
        private readonly LogFormat format = new();
        private ExcelSheet<TerritoryType>? territory;
        private ExcelSheet<Action>? actions;
        private ExcelSheet<Status>? status;

        private delegate void EffectDelegate(uint sourceId, IntPtr sourceCharacter);
        private Hook<EffectDelegate> EffectHook;

        private delegate void ReceiveAbilityDelegate(uint sourceId, IntPtr sourceCharacter, IntPtr pos,
                                                     IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);
        private Hook<ReceiveAbilityDelegate> ReceiveAbilityHook;

        private delegate void ActorControlSelfDelegate(uint entityId, ActorControlCategory id, uint arg0, uint arg1, uint arg2,
                                                       uint arg3, uint arg4, uint arg5, ulong targetId, byte a10);
        private Hook<ActorControlSelfDelegate> ActorControlSelfHook;

        private delegate void NpcSpawnDelegate(long a, uint sourceId, IntPtr sourceCharacter);
        private Hook<NpcSpawnDelegate> NpcSpawnHook;

        private delegate void CastDelegate(uint sourceId, IntPtr sourceCharacter);
        private Hook<CastDelegate> CastHook;

        private delegate void WayMarkDelegate(IntPtr ptr);
        private Hook<WayMarkDelegate> WayMarkHook;

        private delegate void WayMarkPresentDelegate(IntPtr ptr);
        private Hook<WayMarkPresentDelegate> WayMarkPresentHook;

        private delegate void GaugeDelegate(IntPtr ptr1,IntPtr ptr2);
        private Hook<GaugeDelegate> GaugeHook;



        private int partyLength = 0;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            DalamudApi.Initialize(this, this.PluginInterface);

            #region Hook

            {
                //EffectHook = new Hook<EffectDelegate>(
                //    DalamudApi.SigScanner.ScanText(
                //        "48 89 5C 24 ?? 57 48 83 EC 60 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 48 8B DA"), Effect);
                //EffectHook.Enable();
                ReceiveAbilityHook = Hook<ReceiveAbilityDelegate>.FromAddress(
                    DalamudApi.SigScanner.ScanText("4C 89 44 24 ?? 55 56 57 41 54 41 55 41 56 48 8D 6C 24 ??"),
                    ReceiveAbilityEffect);
                ReceiveAbilityHook.Enable();
                ActorControlSelfHook = Hook<ActorControlSelfDelegate>.FromAddress(
                    DalamudApi.SigScanner.ScanText("40 55 53 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ??"), ReceiveActorControlSelf);
                ActorControlSelfHook.Enable();
                //NpcSpawnHook = Hook<NpcSpawnDelegate>.FromAddress(
                //    DalamudApi.SigScanner.ScanText(
                //        "E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 83 C4 20 5F C3 CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 8B F9 "),
                //    ReviceNpcSpawn);
                //NpcSpawnHook.Enable();
                CastHook = Hook<CastDelegate>.FromAddress(
                    DalamudApi.SigScanner.ScanText("40 55 56 48 81 EC ?? ?? ?? ?? 48 8B EA"), StartCast);
                CastHook.Enable();

                WayMarkHook = Hook<WayMarkDelegate>.FromAddress(
                    DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? B0 01 48 8B 5C 24 ?? 48 8B 74 24 ?? 48 83 C4 50 5F C3 8B 57 08"), WayMark);
                WayMarkHook.Enable();
                WayMarkPresentHook = Hook<WayMarkPresentDelegate>.FromAddress(
                    DalamudApi.SigScanner.ScanText("48 8B D1 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 48 8B D1 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 40 55"), WayMarkPresent);
                WayMarkPresentHook.Enable();

                GaugeHook = Hook<GaugeDelegate>.FromAddress(
                    DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? 80 BE ?? ?? ?? ?? ?? 0F 83 ?? ?? ?? ??"), Gauge);
                GaugeHook.Enable();
                

            }

            #endregion

            // you might normally want to embed resources and load them from the manifest stream
            var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

            WindowSystem.AddWindow(new ConfigWindow(this));
            WindowSystem.AddWindow(new MainWindow(this, goatImage));

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            //opCodes = ReadOpcode();
            territory = DalamudApi.DataManager.Excel.GetSheet<TerritoryType>();
            actions = DalamudApi.DataManager.Excel.GetSheet<Action>();
            status = DalamudApi.DataManager.Excel.GetSheet<Status>();

            DalamudApi.GameNetwork.NetworkMessage += GameNetwork_NetworkMessage;
            DalamudApi.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
            DalamudApi.Framework.Update += PartyChanged;
            
        }

        private void PartyChanged(Dalamud.Game.Framework framework)
        {
            if (partyLength == DalamudApi.PartyList.Length) return;
            partyLength = DalamudApi.PartyList.Length;
                var lists = new List<uint>();
                foreach (var member in DalamudApi.PartyList)
                {
                    lists.Add(member.ObjectId);
                }

                PluginLog.Log($"11||{format.FormatPartyMessage(partyLength, new ReadOnlyCollection<uint>(lists))}");
        }

        private void StartCast(uint source, IntPtr ptr)
        {
            var data = Marshal.PtrToStructure<ActorCast>(ptr);
            CastHook.Original(source, ptr);
            var message =format.FormatNetworkCastMessage(source, DalamudApi.ObjectTable.SearchById(source)?.Name.TextValue,
                                            data.TargetID, DalamudApi.ObjectTable.SearchById(data.TargetID)?.Name.TextValue,
                                            data.ActionID, actions.GetRow(data.ActionID).Name, data.CastTime,
                                            DalamudApi.ObjectTable.SearchById(data.TargetID)?.Position.X,DalamudApi.ObjectTable.SearchById(data.TargetID)?.Position.Y,DalamudApi.ObjectTable.SearchById(data.TargetID)?.Position.Z,
                                            DalamudApi.ObjectTable.SearchById(data.TargetID)?.Rotation);
            PluginLog.Log($"20||{message}");
        }

        private void ReceiveActorControlSelf(uint entityId, ActorControlCategory id, uint arg0, uint arg1, uint arg2,
                                             uint arg3, uint arg4, uint arg5, ulong targetId, byte a10)
        {
            
            PluginLog.Log($"{entityId:X}:{id}:{arg0}:{arg1}:{arg2}:{arg3}:{arg4}:{arg5}:{targetId:X}:{a10}");
            ActorControlSelfHook.Original(entityId, id, arg0, arg1, arg2, arg3, arg4, arg5, targetId, a10);
            var obj = DalamudApi.ObjectTable.SearchById(entityId);
            if (obj is not Character entity) return;
            var target = DalamudApi.ObjectTable.SearchById((uint)targetId);
            var message = (id) switch
            {
                ActorControlCategory.CancelCast => $"23||{format.FormatNetworkCancelMessage(entityId,entity?.Name.TextValue,arg2,actions.GetRow(arg2)?.Name,arg1 == 1,arg1 != 1)}",
                ActorControlCategory.Hot => $"24||{format.FormatNetworkDoTMessage(entityId, entity.Name.TextValue,true,arg0,arg1,entity?.CurrentHp,entity.MaxHp,entity.CurrentMp,entity.MaxHp,entity?.Position.X,entity?.Position.Y,entity?.Position.Z,entity?.Rotation)}",
                ActorControlCategory.HoT_DoT => $"24||{format.FormatNetworkDoTMessage(entityId, entity.Name.TextValue, false, arg0, arg1, entity?.CurrentHp, entity?.MaxHp, entity?.CurrentMp, entity?.MaxHp, entity?.Position.X, entity?.Position.Y, entity?.Position.Z, entity?.Rotation)}",
                ActorControlCategory.Death => $"25||{format.FormatNetworkDeathMessage(entityId, entity.Name.TextValue, arg0, DalamudApi.ObjectTable.SearchById(arg0)?.Name.TextValue)}",
                ActorControlCategory.SetTargetSign => $"29||{format.FormatNetworkSignMessage(targetId == 0xE0000000 ? "Delete":"Add",arg0,entityId,entity.Name.TextValue, targetId == 0xE0000000 ? null : (uint)targetId, target?.Name.TextValue)}",
                //TODO:删除标志时的ID修正
                ActorControlCategory.LoseEffect => $"30||{format.FormatNetworkBuffMessage((ushort)arg0,status.GetRow(arg0).Name.RawString,0.00f,arg2, DalamudApi.ObjectTable.SearchById(arg2)?.Name.TextValue,entityId,entity?.Name.TextValue,(ushort)arg1,entity?.CurrentHp,entity?.MaxHp)}",
                ActorControlCategory.Tether => $"35||{format.FormatNetworkTetherMessage(entityId, entity.Name.TextValue, arg2, DalamudApi.ObjectTable.SearchById(arg2)?.Name.TextValue,arg0,arg1,arg2,arg3,arg4,arg5)}",
                //TODO:确定Tether顺序
                ActorControlCategory.SetBGM => 

                //TODO:LimitBreak - Line 36

                $"TESTING::{id}:{entityId:X}:0={arg0}:1={arg1:X}:2={arg2}:3={arg3:X}:4={arg4}:5={arg5}:6={targetId:X}",

                _ => ""
            };
            if (message !="") PluginLog.Log($"{message}");

        }
        private unsafe void ReceiveAbilityEffect(uint sourceId, IntPtr sourceChara, IntPtr pos,
                                                   IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
        {
            
            var targetCount = *(byte*)(effectHeader + 0x21);
            var sourceCharacter = (Character)DalamudApi.ObjectTable.SearchById(sourceId);
            if (targetCount <= 1)
            {
                var data = Marshal.PtrToStructure<Ability1>(effectHeader);
                var targetobject = (Character)DalamudApi.ObjectTable.SearchById((uint)data.targetId[0]);
                var message = format.FormatNetworkAbilityMessage(sourceId, sourceCharacter?.Name.TextValue,
                                                                 data.Header.actionId,
                                                                 actions.GetRow(data.Header.actionId)?.Name,
                                                                 (uint)data.targetId[0], targetobject?.Name.TextValue,
                                                                 sourceCharacter?.CurrentHp, sourceCharacter?.MaxHp,
                                                                 sourceCharacter?.CurrentMp, sourceCharacter?.MaxMp,
                                                                 sourceCharacter?.Position.X, sourceCharacter?.Position.Y,
                                                                 sourceCharacter?.Position.Z, sourceCharacter?.Rotation,
                                                                 targetobject?.CurrentHp, targetobject?.MaxHp,
                                                                 targetobject?.CurrentMp, targetobject?.MaxMp,
                                                                 targetobject?.Position.X, targetobject?.Position.Y,
                                                                 targetobject?.Position.Z, targetobject?.Rotation,
                                                                 data.Effects[0], data.Effects[1], data.Effects[2],
                                                                 data.Effects[3], data.Effects[4], data.Effects[5],
                                                                 data.Effects[6], data.Effects[7], data.Header.rotation,
                                                                 0, targetCount);
                PluginLog.Log($"21||{message}");
            }
            else
            {
                var header = Marshal.PtrToStructure<Header> (effectHeader);
                for (byte i = 0; i < targetCount; i++)
                {
                    var effect = (ulong*)(effectArray + i * 8 * sizeof(ulong)).ToPointer();
                    var length = (targetCount + 7) / 8 * 8;
                    var target = (ulong*)(effectTrail + i * sizeof(ulong));
                    var targetobject = (Character?)DalamudApi.ObjectTable.SearchById((uint)*target);
                    var message = format.FormatNetworkAbilityMessage(sourceId, sourceCharacter?.Name.TextValue,
                                                                     header.actionId,
                                                                     actions.GetRow(header.actionId).Name,
                                                                     (uint)target, targetobject?.Name.TextValue,
                                                                     sourceCharacter?.CurrentHp, sourceCharacter?.MaxHp,
                                                                     sourceCharacter?.CurrentMp, sourceCharacter?.MaxMp,
                                                                     sourceCharacter?.Position.X, sourceCharacter?.Position.Y,
                                                                     sourceCharacter?.Position.Z, sourceCharacter?.Rotation,
                                                                     targetobject?.CurrentHp, targetobject?.MaxHp,
                                                                     targetobject?.CurrentMp, targetobject?.MaxMp,
                                                                     targetobject?.Position.X, targetobject?.Position.Y,
                                                                     targetobject?.Position.Z, targetobject?.Rotation,
                                                                     *effect, *(effect + 1), *(effect + 2),
                                                                     *(effect + 3), *(effect + 4), *(effect + 5),
                                                                     *(effect + 6), *(effect + 7), header.rotation,
                                                                     i, targetCount);
                    PluginLog.Log($"22||{message}");

                }
            }
            ReceiveAbilityHook.Original(sourceId, sourceChara, pos, effectHeader, effectArray, effectTrail);
        }

        private void ClientState_TerritoryChanged(object? sender, ushort e)
        {
            var placeName = territory.GetRow(DalamudApi.ClientState.TerritoryType)?.PlaceName.Value?.Name;
            PluginLog.Log($"01||{format.FormatChangeZoneMessage(DalamudApi.ClientState.TerritoryType, placeName)}");
            //TODO
            //PluginLog.Log($"02|{format.FormatChangePrimaryPlayerMessage(DalamudApi.ClientState.LocalPlayer?.ObjectId,DalamudApi.ClientState.LocalPlayer?.Name.TextValue)}");
            //PluginLog.Log($"XX|{format.FormatPlayerStatsMessage(DalamudApi.ClientState.TerritoryType,DalamudApi.ClientState.LocalPlayer.ClassJob.Id, DalamudApi.ClientState.LocalPlayer.)}")
        }

        //TODO 03-04


        public static Dictionary<string, uint> ReadOpcode()
        {
           
            //文件流读取
            FileStream fs = new FileStream("\\cn-opcodes.txt", FileMode.Open);
            StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("UTF-8"));

            string tempText = "";
            Dictionary<string, uint> opCode = new Dictionary<string, uint>();
            bool isFirst = true;
            while ((tempText = sr.ReadLine()) != null)
            {
                string[] arr = tempText.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);

                if (isFirst)
                {
                    isFirst = false;
                    continue;
                }
                else
                {
                    opCode.Add(arr[0], uint.Parse(arr[-1]));
                }
            }
            return opCode;
        }

        private void WayMark(IntPtr ptr)
        {
            var data = Marshal.PtrToStructure<FFXIVIpcPlaceFieldMarker>(ptr);
            WayMarkHook.Original(ptr);
            var source = DalamudApi.ClientState.LocalPlayer;
            PluginLog.Log($"28||{format.FormatNetworkWaymarkMessage(data.status == 0 ? "Delete" : "Add", (uint)data.markerId, source.ObjectId, source?.Name.TextValue, data.Xint / 1000f, data.Yint / 1000f, data.Zint / 1000f)}");
        }

        private unsafe void WayMarkPresent(IntPtr ptr)
        {
            var data = Marshal.PtrToStructure<FFXIVIpcPlaceFieldMarkerPreset>(ptr);
            WayMarkPresentHook.Original(ptr);
            var source = DalamudApi.ClientState.LocalPlayer;
            for (int i = 0; i < 8; i++)
            {
                PluginLog.Log($"28||{format.FormatNetworkWaymarkMessage(((uint)data.status & (1 << i)) == 0 ? "Delete" : "Add", (uint)i, source.ObjectId, source?.Name.TextValue, data.Xints[i] / 1000f, data.Yints[i] / 1000f, data.Zints[i] / 1000f)}");
            }
            
        }

        private void Gauge(IntPtr ptr1,IntPtr ptr2)
        {
            var data = Marshal.PtrToStructure<FFXIVIpcActorGauge>(ptr2);
            GaugeHook.Original(ptr1,ptr2);
            PluginLog.Log($"31|0|{DalamudApi.ClientState.LocalPlayer?.Name}|{data.Data0:X}|{data.Data1:X}|{data.Data2:X}");
            //TODO:ID不应为0

        }


        private void GameNetwork_NetworkMessage(System.IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            if (direction == NetworkMessageDirection.ZoneUp) return;
            //PluginLog.Log($"{opCode:X}|{dataPtr}");
            string log = opCode switch
            {
                0x0 => HandleChatLog(dataPtr, sourceActorId, targetActorId),
                //1 => Territory
                //ChangePrimaryPlayer = 2,
                //AddCombatant = 3,
                //RemoveCombatant = 4,
                //PartyList = 11,
                //PlayerStats = 12,
                //StartsCasting = 20,
                //ActionEffect = 21,
                //0x0228 => ReceiveAbilityEffect(dataPtr, targetActorId),
                //AOEActionEffect = 22,
                //CancelAction = 23,
                //DoTHoT = 24,
                //Death = 25,
                //StatusAdd = 26,
                0x01D4 => $"{Marshal.PtrToStructure<ActorControl.ActorControlStruct>(dataPtr).category}",
                //TargetIcon = 27,
                //WaymarkMarker = 28,
                //SignMarker = 29,
                //StatusRemove = 30,
                //Gauge = 31,
                //World = 32,
                //Director = 33,
                //NameToggle = 34,
                //Tether = 35,
                //LimitBreak = 36,
                //EffectResult = 37,
                //StatusList = 38,
                //UpdateHp = 39,
                //ChangeMap = 40,
                //SystemLogMessage = 41,
                //StatusList3 = 42,
                //Settings = 249,
                //Process = 250,
                //Debug = 251,
                //PacketDump = 252,
                //Version = 253,
                //Error = 254


                _ => ""
            };
            if (!string.IsNullOrEmpty(log)) PluginLog.Log(log);

        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
            DalamudApi.GameNetwork.NetworkMessage -= GameNetwork_NetworkMessage;
            DalamudApi.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
            DalamudApi.Framework.Update -= PartyChanged;
            ActorControlSelfHook.Dispose();
            ReceiveAbilityHook.Dispose();
            //NpcSpawnHook.Dispose();
            CastHook.Dispose();
            WayMarkHook.Dispose();
            GaugeHook.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            WindowSystem.GetWindow("My Amazing Window").IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            WindowSystem.GetWindow("A Wonderful Configuration Window").IsOpen = true;
        }

        public string HandleChatLog(IntPtr ptr, uint sourceActorId, uint targetActorId)
        {
            var data = Marshal.PtrToStructure<Machina.FFXIV.Headers.Server_SystemLogMessage>(ptr);
            return format.FormatChatMessage(0, "", "");
        }

        public string Dump(IntPtr ptr, uint sourceActorId, uint targetActorId, ushort opCode)
        {
            return string.Empty;
        }

        public string HandleAbility(IntPtr ptr, uint sourceActorId, uint targetActorId)
        {
            var data = Marshal.PtrToStructure<Struct.FFXIVIpcEffectResult>(ptr);
            var target = DalamudApi.ObjectTable.SearchById(targetActorId);
            var source = DalamudApi.ObjectTable.SearchById(sourceActorId);
            return null;
        }

        public static float? UShortToFloat(ushort? val)
        {
            PluginLog.Log($"{val:X}");
            return (val - 0x8000) / 32.767f;
        }

        public static float? UShortToRotation(ushort? val)
        {
            PluginLog.Log($"{val:X}");
            return (val - 0x7FFF) * 3.1415926f / 32767f;
        }

    }
}
