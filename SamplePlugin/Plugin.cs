using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Game.Network;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
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
        private ExcelSheet<Map>? maps;
        private ExcelSheet<World>? worlds;

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

        private delegate void EffectResultDelegate(uint targetId, IntPtr ptr, byte a3);
        private Hook<EffectResultDelegate> EffectResultHook;

        private List<GameObject> objects = new List<GameObject>();

        public IntPtr MapIdDungeon { get; private set; }
        public IntPtr MapIdWorld { get; private set; }

        private int partyLength = 0;

        private Event eventHandle;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            DalamudApi.Initialize(this, this.PluginInterface);

            eventHandle = new Event();

            #region Hook

            ReceiveAbilityHook = Hook<ReceiveAbilityDelegate>.FromAddress(
                    DalamudApi.SigScanner.ScanText("4C 89 44 24 ?? 55 56 57 41 54 41 55 41 56 48 8D 6C 24 ??"),
                    ReceiveAbilityEffect);
                ReceiveAbilityHook.Enable();
                ActorControlSelfHook = Hook<ActorControlSelfDelegate>.FromAddress(
                    DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64"), ReceiveActorControlSelf);
                ActorControlSelfHook.Enable();
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

                
                EffectResultHook = Hook<EffectResultDelegate>.FromAddress(
                    DalamudApi.SigScanner.ScanText("48 8B C4 44 88 40 18 89 48 08"), EffectResult);
                EffectResultHook.Enable();

                MapIdDungeon = DalamudApi.SigScanner.GetStaticAddressFromSig("44 8B 3D ?? ?? ?? ?? 45 85 FF");
                MapIdWorld = DalamudApi.SigScanner.GetStaticAddressFromSig("44 0F 44 3D ?? ?? ?? ??");
            

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
            maps = DalamudApi.DataManager.Excel.GetSheet<Map>();
            worlds = DalamudApi.DataManager.Excel.GetSheet<World>();

            //DalamudApi.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
            DalamudApi.Framework.Update += PartyChanged;
            DalamudApi.Framework.Update += CompareObjects;


        }

        private void CompareObjects(Dalamud.Game.Framework framework)
        {
            List<GameObject> newlist = new List<GameObject>();
            foreach (var obj in DalamudApi.ObjectTable)
            {
                if (!obj.IsValid()) continue;
                newlist.Add(obj);
            }

            var plus = newlist.Except(objects);
            var minus = objects.Except(newlist);
            foreach (var obj in plus)
            {
                switch (obj.ObjectKind)
                {
                    case ObjectKind.Player:
                        eventHandle.SetLog($"03|{DateTime.Now:O}|{format.FormatCombatantMessage(obj.ObjectId, obj.OwnerId, obj.Name.TextValue, (int)((PlayerCharacter)obj).ClassJob.Id, (int)((Character)obj).Level, ((PlayerCharacter)obj).HomeWorld.Id, worlds.GetRow(((PlayerCharacter)obj).HomeWorld.Id).Name.RawString, 0, 0, (uint)((Character)obj).CurrentHp, (uint)((Character)obj).MaxHp, (uint)((Character)obj).CurrentMp, (uint)((Character)obj).MaxMp, obj.Position.X, obj.Position.Y, obj.Position.Z, obj.Rotation)}");

                        break;
                    case ObjectKind.BattleNpc:
                        eventHandle.SetLog($"03|{DateTime.Now:O}|{format.FormatCombatantMessage(obj.ObjectId, obj.OwnerId, obj.Name.TextValue, 0, ((BattleNpc)obj).Level, 0, "", ((BattleNpc)obj).NameId, ((BattleNpc)obj).DataId, ((BattleNpc)obj).CurrentHp, ((BattleNpc)obj).MaxHp, ((BattleNpc)obj).CurrentMp, ((BattleNpc)obj).MaxMp, obj.Position.X, obj.Position.Y, obj.Position.Z, obj.Rotation)}");
                        break;
                }

            }
            foreach (var obj in minus)
            {
                switch (obj.ObjectKind)
                {
                    case ObjectKind.Player:
                        eventHandle.SetLog($"04|{DateTime.Now:O}|{format.FormatCombatantMessage(obj.ObjectId, obj.OwnerId, obj.Name.TextValue, (int)((PlayerCharacter)obj).ClassJob.Id, (int)((Character)obj).Level, ((PlayerCharacter)obj).HomeWorld.Id, worlds.GetRow(((PlayerCharacter)obj).HomeWorld.Id).Name.RawString, 0, 0, (uint)((Character)obj).CurrentHp, (uint)((Character)obj).MaxHp, (uint)((Character)obj).CurrentMp, (uint)((Character)obj).MaxMp, obj.Position.X, obj.Position.Y, obj.Position.Z, obj.Rotation)}");
                        break;
                    case ObjectKind.BattleNpc:
                        eventHandle.SetLog($"04|{DateTime.Now:O}|{format.FormatCombatantMessage(obj.ObjectId, obj.OwnerId, obj.Name.TextValue, 0, ((BattleNpc)obj).Level, 0, "", ((BattleNpc)obj).NameId, ((BattleNpc)obj).DataId, ((BattleNpc)obj).CurrentHp, ((BattleNpc)obj).MaxHp, ((BattleNpc)obj).CurrentMp, ((BattleNpc)obj).MaxMp, obj.Position.X, obj.Position.Y, obj.Position.Z, obj.Rotation)}");
                        break;
                }
            }

            objects = newlist;
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

                eventHandle.SetLog($"11|{DateTime.Now:O}|{format.FormatPartyMessage(partyLength, new ReadOnlyCollection<uint>(lists))}");
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
            eventHandle.SetLog($"20|{DateTime.Now:O}|{message}");
        }

        private void ReceiveActorControlSelf(uint entityId, ActorControlCategory id, uint arg0, uint arg1, uint arg2,
                                             uint arg3, uint arg4, uint arg5, ulong targetId, byte a10)
        {
            
            //eventHandle.SetLog($"{entityId:X}:{id}:{arg0}:{arg1}:{arg2}:{arg3}:{arg4}:{arg5}:{targetId:X}:{a10}");
            ActorControlSelfHook.Original(entityId, id, arg0, arg1, arg2, arg3, arg4, arg5, targetId, a10);
            var obj = DalamudApi.ObjectTable.SearchById(entityId);
            if (obj is not Character entity) return;
            var target = DalamudApi.ObjectTable.SearchById((uint)targetId);
            var message = (id) switch
            {
                ActorControlCategory.CancelCast => $"23|{DateTime.Now:O}|{format.FormatNetworkCancelMessage(entityId,entity.Name.TextValue,arg2,actions.GetRow(arg2)?.Name,arg1 == 1,arg1 != 1)}",
                ActorControlCategory.Hot => $"24|{DateTime.Now:O}|{format.FormatNetworkDoTMessage(entityId, entity.Name.TextValue,true,arg0,arg1,entity?.CurrentHp,entity.MaxHp,entity.CurrentMp,entity.MaxHp,entity?.Position.X,entity?.Position.Y,entity?.Position.Z,entity?.Rotation)}",
                ActorControlCategory.HoT_DoT => $"24|{DateTime.Now:O}|{format.FormatNetworkDoTMessage(entityId, entity.Name.TextValue, false, arg0, arg1, entity?.CurrentHp, entity?.MaxHp, entity?.CurrentMp, entity?.MaxHp, entity?.Position.X, entity?.Position.Y, entity?.Position.Z, entity?.Rotation)}",
                ActorControlCategory.Death => $"25|{DateTime.Now:O}|{format.FormatNetworkDeathMessage(entityId, entity.Name.TextValue, arg0, DalamudApi.ObjectTable.SearchById(arg0)?.Name.TextValue)}",
                ActorControlCategory.TargetIcon => $"27|{DateTime.Now:O}|{format.FormatNetworkTargetIconMessage(entityId, entity.Name.TextValue, arg1, arg2, arg0, arg3, arg4, arg5)}",
                ActorControlCategory.SetTargetSign => $"29|{DateTime.Now:O}|{format.FormatNetworkSignMessage(targetId == 0xE0000000 ? "Delete":"Add",arg0,entityId,entity.Name.TextValue, targetId == 0xE0000000 ? null : (uint)targetId, target?.Name.TextValue ?? "")}",
                //TODO:删除标志时的ID修正?
                ActorControlCategory.LoseEffect => $"30|{DateTime.Now:O}|{format.FormatNetworkBuffMessage((ushort)arg0,status.GetRow(arg0).Name.RawString,0.00f,arg2, DalamudApi.ObjectTable.SearchById(arg2)?.Name.TextValue ?? "",entityId,entity.Name.TextValue,(ushort)arg1,entity.CurrentHp,entity.MaxHp)}",
                ActorControlCategory.DirectorUpdate => $"33|{DateTime.Now:O}|{arg0:X2}|{arg1:X2}|{arg2:X2}|{arg3:X2}|{arg4:X2}|{arg5:X2}",
                ActorControlCategory.Targetable => $"34|{DateTime.Now:O}|{format.FormatNetworkTargettableMessage(entityId, entity.Name.TextValue, entityId, entity.Name.TextValue, (byte)arg0)}",
                ActorControlCategory.Tether => $"35|{DateTime.Now:O}|{format.FormatNetworkTetherMessage(entityId, entity.Name.TextValue, arg2, DalamudApi.ObjectTable.SearchById(arg2)?.Name.TextValue ?? "",arg0,arg1,arg2,arg3,arg4,arg5)}",
                //TODO:LimitBreak - Line 36
                ActorControlCategory.LogMsg => $"41|{DateTime.Now:O}|{DalamudApi.ClientState.LocalContentId:X2}|{arg0:X2}|{arg1:X2}|{arg2:X2}|{arg3:X2}",
                
                


                //$"TESTING::{id}:{entityId:X}:0={arg0:X}:1={arg1:X}:2={arg2}:3={arg3:X}:4={arg4}:5={arg5}:6={targetId:X}",

                _ => ""
            };
            if (message.Contains("TEST")) PluginLog.Warning($"{message}");
            if (!message.IsNullOrEmpty()) eventHandle.SetLog($"{message}");
            //if (arg0 == 0x40000005 || arg1 == 0x40000005) PluginLog.Warning($"WIPE:{id}");

        }

        private unsafe void ReceiveAbilityEffect(uint sourceId, IntPtr sourceChara, IntPtr pos,
                                                   IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
        {
            
            var targetCount = *(byte*)(effectHeader + 0x21);
            
            if (targetCount <= 1)
            {
                var data = Marshal.PtrToStructure<Ability1>(effectHeader);
                ReceiveAbilityHook.Original(sourceId, sourceChara, pos, effectHeader, effectArray, effectTrail);
                var sourceCharacter = (Character?)DalamudApi.ObjectTable.SearchById(sourceId);
                var targetobject = (Character?)DalamudApi.ObjectTable.SearchById((uint)data.targetId[0]);
                var message = format.FormatNetworkAbilityMessage(sourceId, sourceCharacter?.Name.TextValue ?? "",
                                                                 data.Header.actionId,
                                                                 actions.GetRow(data.Header.actionId)?.Name ?? "",
                                                                 (uint)data.targetId[0], targetobject?.Name.TextValue ?? "",
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
                eventHandle.SetLog($"21|{DateTime.Now:O}|{message}");
            }
            else
            {
                var header = Marshal.PtrToStructure<Header> (effectHeader);
                var targets = new long[targetCount];
                var effects = new long[8 * targetCount];
                Marshal.Copy(effectArray, effects, 0, targetCount * 8);
                Marshal.Copy(effectTrail,targets,0,targetCount);
                ReceiveAbilityHook.Original(sourceId, sourceChara, pos, effectHeader, effectArray, effectTrail);
                var sourceCharacter = (Character?)DalamudApi.ObjectTable.SearchById(sourceId);
                for (byte i = 0; i < targetCount; i++)
                {
                    var targetobject = (Character?)DalamudApi.ObjectTable.SearchById((uint)targets[i]);
                    var message = format.FormatNetworkAbilityMessage(sourceId, sourceCharacter?.Name.TextValue ?? "",
                                                                     header.actionId,
                                                                     actions.GetRow(header.actionId).Name ?? "",
                                                                     (uint)targets[i], targetobject?.Name.TextValue ?? "",
                                                                     sourceCharacter?.CurrentHp, sourceCharacter?.MaxHp,
                                                                     sourceCharacter?.CurrentMp, sourceCharacter?.MaxMp,
                                                                     sourceCharacter?.Position.X, sourceCharacter?.Position.Y,
                                                                     sourceCharacter?.Position.Z, sourceCharacter?.Rotation,
                                                                     targetobject?.CurrentHp, targetobject?.MaxHp,
                                                                     targetobject?.CurrentMp, targetobject?.MaxMp,
                                                                     targetobject?.Position.X, targetobject?.Position.Y,
                                                                     targetobject?.Position.Z, targetobject?.Rotation,
                                                                     (ulong)effects[(i * 8) + 0], (ulong)effects[(i * 8) + 1], (ulong)effects[(i * 8) + 2],
                                                                     (ulong)effects[(i * 8) + 3], (ulong)effects[(i * 8) + 4], (ulong)effects[(i * 8) + 5],
                                                                     (ulong)effects[(i * 8) + 6], (ulong)effects[(i * 8) + 7], header.rotation,
                                                                     i, targetCount);
                    eventHandle.SetLog($"22|{DateTime.Now:O}|{message}");

                }
            }
            //ReceiveAbilityHook.Original(sourceId, sourceChara, pos, effectHeader, effectArray, effectTrail);
        }

        private unsafe void ClientState_TerritoryChanged(object? sender, ushort e)
        {
            var placeName = territory.GetRow(DalamudApi.ClientState.TerritoryType)?.PlaceName.Value?.Name;
            if (eventHandle != null)
            eventHandle.SetLog($"01|{DateTime.Now:O}|{format.FormatChangeZoneMessage(DalamudApi.ClientState.TerritoryType, placeName)}");
            MapChange();
        //TODO MAP change
        //eventHandle.SetLog($"02|{format.FormatChangePrimaryPlayerMessage(DalamudApi.ClientState.LocalPlayer?.ObjectId,DalamudApi.ClientState.LocalPlayer?.Name.TextValue)}");
        //eventHandle.SetLog($"XX|{format.FormatPlayerStatsMessage(DalamudApi.ClientState.TerritoryType,DalamudApi.ClientState.LocalPlayer.ClassJob.Id, DalamudApi.ClientState.LocalPlayer.)}")
        }

        private unsafe void MapChange()
        {
            
            uint MapId = *(uint*)MapIdDungeon == 0 ? *(uint*)MapIdWorld : *(uint*)MapIdDungeon;
            var map = maps.GetRow(MapId);
            //TODO MAP change
            eventHandle.SetLog($"40|{DateTime.Now:O}|{format.FormatChangeMapMessage(MapId,map?.PlaceNameRegion.Value?.Name,map?.PlaceName.Value?.Name,map?.PlaceNameSub.Value?.Name)}");
        }

        private void WayMark(IntPtr ptr)
        {
            var data = Marshal.PtrToStructure<FFXIVIpcPlaceFieldMarker>(ptr);
            WayMarkHook.Original(ptr);
            var source = DalamudApi.ClientState.LocalPlayer;
            //TODO:Source修复
            eventHandle.SetLog($"28|{DateTime.Now:O}|{format.FormatNetworkWaymarkMessage(data.status == 0 ? "Delete" : "Add", (uint)data.markerId, source.ObjectId, source?.Name.TextValue, data.Xint / 1000f, data.Yint / 1000f, data.Zint / 1000f)}");
        }

        private unsafe void WayMarkPresent(IntPtr ptr)
        {
            var data = Marshal.PtrToStructure<FFXIVIpcPlaceFieldMarkerPreset>(ptr);
            WayMarkPresentHook.Original(ptr);
            var source = DalamudApi.ClientState.LocalPlayer;
            //TODO:Source修复
            for (int i = 0; i < 8; i++)
            {
                eventHandle.SetLog($"28|{DateTime.Now:O}|{format.FormatNetworkWaymarkMessage(((uint)data.status & (1 << i)) == 0 ? "Delete" : "Add", (uint)i, source.ObjectId, source?.Name.TextValue, data.Xints[i] / 1000f, data.Yints[i] / 1000f, data.Zints[i] / 1000f)}");
            }
            
        }

        private void Gauge(IntPtr ptr1,IntPtr ptr2)
        {
            var data = Marshal.PtrToStructure<FFXIVIpcActorGauge>(ptr2);
            GaugeHook.Original(ptr1,ptr2);
            eventHandle.SetLog($"31|0|{DalamudApi.ClientState.LocalPlayer?.Name}|{data.Data0:X}|{data.Data1:X}|{data.Data2:X}");
            //TODO:ID不应为0

        }

        private void EffectResult(uint targetId, IntPtr ptr, byte a3)
        {
            var data = Marshal.PtrToStructure<FFXIVIpcEffectResult>(ptr);
            EffectResultHook.Original(targetId, ptr, a3);
            if (a3 != 0) return;
            var target = (Character?)DalamudApi.ObjectTable.SearchById(targetId);
            for (int i = 0; i < data.entryCount; i++)
            {
                var sta = data.statusEntries[i];
                var source = DalamudApi.ObjectTable.SearchById(sta.sourceActorId);
                var maxhp = source is Character ? (uint?)((Character)source).MaxHp : null;
                eventHandle.SetLog($"26|{DateTime.Now:O}|{format.FormatNetworkBuffMessage(sta.id,status.GetRow(sta.id)?.Name.RawString,sta.duration,sta.sourceActorId,source?.Name.TextValue,targetId,target?.Name.TextValue,sta.param,target.MaxHp,maxhp)}");
            }
                
            
            
            
        }


        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
            DalamudApi.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
            DalamudApi.Framework.Update -= PartyChanged;
            ActorControlSelfHook.Dispose();
            ReceiveAbilityHook.Dispose();
            //NpcSpawnHook.Dispose();
            CastHook.Dispose();
            WayMarkHook.Dispose();
            GaugeHook.Dispose();
            EffectResultHook.Dispose();
            DalamudApi.Framework.Update -= CompareObjects;
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
            return (val - 0x8000) / 32.767f;
        }

        public static float? UShortToRotation(ushort? val)
        {
            return (val - 0x7FFF) * 3.1415926f / 32767f;
        }

    }
}
