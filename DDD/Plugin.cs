using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using DDD.Windows;
using Action = Lumina.Excel.GeneratedSheets.Action;
using DDD.Struct;
using DDD.Plugins;
using Dalamud.Game;
using Dalamud.Logging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DDD
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "DDD";
        private const string CommandName = "/ddd";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public WindowSystem WindowSystem = new("ddd");
        private Dictionary<string, uint> opCodes;
        private readonly LogFormat format = new();
        private ExcelSheet<TerritoryType>? territory;
        private ExcelSheet<Action>? actions;
        private ExcelSheet<Map>? maps;
        private ExcelSheet<World>? worlds;
        private BuffManager manager;

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

        private delegate void GaugeDelegate(IntPtr ptr1, IntPtr ptr2);
        private Hook<GaugeDelegate> GaugeHook;

        private delegate void EffectResultDelegate(uint targetId, IntPtr ptr, byte a3);
        private Hook<EffectResultDelegate> EffectResultHook;

        private delegate void EffectResultBasicHookDelegate(uint targetId, IntPtr ptr, byte a3);
        private Hook<EffectResultBasicHookDelegate> EffectResultBasicHook;
        private delegate void BuffList1(uint sourceId, IntPtr effectList, byte c);
        private Hook<BuffList1> BuffList1Hook;

        private delegate void EnvControl(long a1, IntPtr a2);
        private Hook<EnvControl> EnvControlHook;

        private delegate void UpdateHPMP(uint a1, IntPtr a2, byte a3);
        private Hook<UpdateHPMP> UpdateHPMPHook;

        private delegate void UpdateParty(IntPtr header, IntPtr data, byte a3);
        private Hook<UpdateParty> UpdatePartyHook;
        private delegate void CreateObject(long a1, IntPtr a2);
        private Hook<CreateObject> CreateObjectHook;
        private List<GameObject> objects = new();

        public IntPtr MapIdDungeon { get; private set; }
        public IntPtr MapIdWorld { get; private set; }

        public IntPtr PlayerStat;
        private PlayerStruct64 playerstat;

        private int partyLength = 0;
        private long lastTime;
        private uint oldMap=0;
        private uint plID;

        public Event eventHandle;
        public unsafe static ContentsReplayModule* contentsReplayModule;
        unsafe public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            DalamudApi.Initialize(this, PluginInterface);

            eventHandle = new Event();
            //ipc = new IPC();
            //ipc.InitIpc(this);
            manager = new BuffManager(format, eventHandle);
            //DalamudApi.Framework.Update += PartyChanged;
            contentsReplayModule = (ContentsReplayModule*)DalamudApi.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 88 44 24 24");
            PluginLog.Log($"{contentsReplayModule->speed}");
            #region Hook

            ReceiveAbilityHook = Hook<ReceiveAbilityDelegate>.FromAddress(
                DalamudApi.SigScanner.ScanText("4C 89 44 24 ?? 55 56 41 54 41 55 41 56"),
                ReceiveAbilityEffect);
            ReceiveAbilityHook.Enable();
            ActorControlSelfHook = Hook<ActorControlSelfDelegate>.FromAddress(
                DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64"), ReceiveActorControl);
            ActorControlSelfHook.Enable();
            CastHook = Hook<CastDelegate>.FromAddress(
                DalamudApi.SigScanner.ScanText("40 55 56 48 81 EC ?? ?? ?? ?? 48 8B EA"), StartCast);
            CastHook.Enable();

            WayMarkHook = Hook<WayMarkDelegate>.FromAddress(
                DalamudApi.SigScanner.ScanText("48 8B D1 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 40 55 56"), WayMark);
            WayMarkHook.Enable();
            if (Loclization.isCN)
            {
                WayMarkPresentHook = Hook<WayMarkPresentDelegate>.FromAddress(
    DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? B0 01 48 8B 5C 24 ?? 48 8B 74 24 ?? 48 83 C4 50 5F C3 48 8B D3 "), WayMarkPresent);
            }
            else
            {
                WayMarkPresentHook = Hook<WayMarkPresentDelegate>.FromAddress(
DalamudApi.SigScanner.ScanText("E9 ?? ?? ?? ?? 4C 8D 43 10 8B D6 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? F6 05 ?? ?? ?? ?? ?? 0F 85 ?? ?? ?? ?? 48 8D 4B 10 48 8B 7C 24 ?? 48 8B 5C 24 ?? 48 83 C4 50 5E E9 ?? ?? ?? ?? 4C 8D 43 10 8B D6 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? F6 05 ?? ?? ?? ?? ?? 0F 85 ?? ?? ?? ?? 48 8D 53"), WayMarkPresent);
            }

            WayMarkPresentHook.Enable();

            GaugeHook = Hook<GaugeDelegate>.FromAddress(
                DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? 80 BE ?? ?? ?? ?? ?? 0F 83 ?? ?? ?? ??"), Gauge);
            GaugeHook.Enable();

            EffectResultHook = Hook<EffectResultDelegate>.FromAddress(
                DalamudApi.SigScanner.ScanText("48 8B C4 44 88 40 18 89 48 08"), EffectResult);
            EffectResultHook.Enable();

            EffectResultBasicHook = Hook<EffectResultBasicHookDelegate>.FromAddress(
                DalamudApi.SigScanner.ScanText("40 53 41 54 41 55 48 83 EC 40"), EffectResultBasic);
            EffectResultBasicHook.Enable();
            

            BuffList1Hook = Hook<BuffList1>.FromAddress(
                DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? 40 84 ED 75 0D"), BuffList1Do);
            BuffList1Hook.Enable();

            EnvControlHook = Hook<EnvControl>.FromAddress(
                DalamudApi.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC ?? 48 8B F9 48 8B DA 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 ?? 48 8B 01 FF 50 ?? 84 C0 74 ?? 48 8B 8F ?? ?? ?? ?? 8B 03 48 8B 91 ?? ?? ?? ?? 39 02 75 ?? 48 83 B9 ?? ?? ?? ?? ?? 74 ?? 0F B6 53 ?? 44 0F B7 4B ?? 44 0F B7 43 ?? E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 83 C4 ?? 5F C3 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 48 89 5C 24 ?? 57 48 83 EC ?? 33 C0"), EnvControlFunc);
            EnvControlHook.Enable();

            UpdateHPMPHook = Hook<UpdateHPMP>.FromAddress(DalamudApi.SigScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 56 48 83 EC 20 83 3D ?? ?? ?? ?? ?? 41 0F B6 E8 48 8B DA 8B F1 0F 84 ?? ?? ?? ?? 48 89 7C 24 ??"),UpdateHPMPTP);
            UpdateHPMPHook.Enable();

            UpdatePartyHook = Hook<UpdateParty>.FromAddress(DalamudApi.SigScanner.ScanText("48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ??"), UpdatePartyDetor);
            UpdatePartyHook.Enable();


            CreateObjectHook = Hook<CreateObject>.FromAddress(DalamudApi.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 50 F6 42 02 02"), CreateObjectyDetor);
            CreateObjectHook.Enable();
            UpdatePartyHook.Enable();
            MapIdDungeon = DalamudApi.SigScanner.GetStaticAddressFromSig("44 8B 3D ?? ?? ?? ?? 45 85 FF");
            MapIdWorld = DalamudApi.SigScanner.GetStaticAddressFromSig("44 0F 44 3D ?? ?? ?? ??");
            PlayerStat = DalamudApi.SigScanner.GetStaticAddressFromSig("83 F9 FF 74 12 44 8B 04 8E 8B D3 48 8D 0D");

            #endregion


            WindowSystem.AddWindow(new ConfigWindow(this));
            //WindowSystem.AddWindow(new MainWindow(this, goatImage));

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            territory = DalamudApi.DataManager.Excel.GetSheet<TerritoryType>();
            actions = DalamudApi.DataManager.Excel.GetSheet<Action>();
            DalamudApi.DataManager.Excel.GetSheet<Status>();
            maps = DalamudApi.DataManager.Excel.GetSheet<Map>();
            worlds = DalamudApi.DataManager.Excel.GetSheet<World>();

            DalamudApi.Framework.Update += FrameworkOnUpdate;

        }

        private void CreateObjectyDetor(long a1, IntPtr a2)
        {
            CreateObjectHook.Original(a1,a2); 
            var data = Marshal.PtrToStructure<FFXIVIpcObjectSpawn>(a2);
            eventHandle.SetLog((LogMessageType)254, $"] ChatLog 00:0:101:"+$"{a1:X8}:{data.InvisibilityGroup:X2}{data.flag:X2}:{data.objKind:X2}{data.spawnIndex:X2}:{data.objId:X4}:{data.OwnerId:X8}:{data.position.x:f2}:{data.position.z:f2}:{data.position.y:f2}:", DateTime.Now);
        }

        private void FrameworkOnUpdate(Framework framework)
        {
            if (DalamudApi.ObjectTable.Count() < 10) return;
            CompareObjects();
        }
        
        private void CompareObjects()
        {
            MapChange();
            CheckPlayer();
            //PluginLog.Debug($"CompareObjects");
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (now - lastTime < 1000 / 60) return;
            lastTime = now;
            var newlist = DalamudApi.ObjectTable.ToList();

            var plus = newlist.Except(objects).ToList();
            var minus = objects.Except(newlist).ToList();
            var time = DateTime.Now;
            foreach (var obj in plus)
            {
                switch (obj.ObjectKind)
                {
                    case ObjectKind.Player:
                        eventHandle.SetLog(LogMessageType.AddCombatant, $"{format.FormatCombatantMessage(obj.ObjectId, obj.OwnerId, obj.Name.TextValue, (int)((PlayerCharacter)obj).ClassJob.Id, ((Character)obj).Level, ((PlayerCharacter)obj).HomeWorld.Id, worlds.GetRow(((PlayerCharacter)obj).HomeWorld.Id).InternalName.RawString, 0, 0, ((Character)obj).CurrentHp, ((Character)obj).MaxHp, ((Character)obj).CurrentMp, ((Character)obj).MaxMp, obj.Position.X, obj.Position.Z, obj.Position.Y, obj.Rotation)}",time);

                        break;
                    case ObjectKind.BattleNpc:
                        eventHandle.SetLog(LogMessageType.AddCombatant, $"{format.FormatCombatantMessage(obj.ObjectId, obj.OwnerId, obj.Name.TextValue, 0, ((BattleNpc)obj).Level, 0, "", ((BattleNpc)obj).NameId, ((BattleNpc)obj).DataId, ((BattleNpc)obj).CurrentHp, ((BattleNpc)obj).MaxHp, ((BattleNpc)obj).CurrentMp, ((BattleNpc)obj).MaxMp, obj.Position.X, obj.Position.Z, obj.Position.Y, obj.Rotation)}", time);
                        break;
                }
            }
            foreach (var obj in minus)
            {
                switch (obj.ObjectKind)
                {
                    case ObjectKind.Player:
                        eventHandle.SetLog(LogMessageType.RemoveCombatant, $"{format.FormatCombatantMessage(obj.ObjectId, obj.OwnerId, obj.Name.TextValue, (int)((PlayerCharacter)obj).ClassJob.Id, ((Character)obj).Level, ((PlayerCharacter)obj).HomeWorld.Id, worlds.GetRow(((PlayerCharacter)obj).HomeWorld.Id).InternalName.RawString, 0, 0, ((Character)obj).CurrentHp, ((Character)obj).MaxHp, ((Character)obj).CurrentMp, ((Character)obj).MaxMp, obj.Position.X, obj.Position.Z, obj.Position.Y, obj.Rotation)}", time);
                        break;
                    case ObjectKind.BattleNpc:
                        eventHandle.SetLog(LogMessageType.RemoveCombatant, $"{format.FormatCombatantMessage(obj.ObjectId, obj.OwnerId, obj.Name.TextValue, 0, ((BattleNpc)obj).Level, 0, "", ((BattleNpc)obj).NameId, ((BattleNpc)obj).DataId, ((BattleNpc)obj).CurrentHp, ((BattleNpc)obj).MaxHp, ((BattleNpc)obj).CurrentMp, ((BattleNpc)obj).MaxMp, obj.Position.X, obj.Position.Z, obj.Position.Y, obj.Rotation)}", time);
                        break;
                }
                manager.RemoveAll(obj.ObjectId);
            }
            objects = newlist;
            //PluginLog.Debug($"CompareObjects Finish");
        }

        private void UpdatePartyDetor(IntPtr header, IntPtr dataptr, byte a3)
        {
            //PluginLog.Debug($"PartyLength = {partyLength}");
            //PluginLog.Debug($"PartyUpdate");
            partyLength = Marshal.ReadByte(dataptr, (440 * 8) + 17);
            //var party = Marshal.PtrToStructure<Party>(dataptr);
            var lists = new List<uint>();
            for (int i = 32 + 8; i < 440 * partyLength; i+= 440)
            {
                lists.Add((uint)Marshal.ReadInt32(dataptr,i));
            }
            UpdatePartyHook.Original(header, dataptr, a3);
            eventHandle.SetLog(LogMessageType.PartyList, $"{format.FormatPartyMessage(partyLength, new ReadOnlyCollection<uint>(lists))}", DateTime.Now);
        }

        private void StartCast(uint source, IntPtr ptr)
        {
            //PluginLog.Debug($"StartCast");
            var data = Marshal.PtrToStructure<ActorCast>(ptr);
            CastHook.Original(source, ptr);
            var soutceobj = DalamudApi.ObjectTable.SearchById(source);
            var message = format.FormatNetworkCastMessage(source, DalamudApi.ObjectTable.SearchById(source)?.Name.TextValue,
                                            data.TargetID, DalamudApi.ObjectTable.SearchById(data.TargetID)?.Name.TextValue,
                                            data.ActionID, actions.GetRow(data.ActionID).Name, data.CastTime,
                                            soutceobj?.Position.X, soutceobj?.Position.Z, soutceobj?.Position.Y,
                                            soutceobj?.Rotation);
            eventHandle.SetLog(LogMessageType.StartsCasting, $"{message}",DateTime.Now);
        }

        private void ReceiveActorControl(uint entityId, ActorControlCategory id, uint arg0, uint arg1, uint arg2,
                                             uint arg3, uint arg4, uint arg5, ulong targetId, byte a10)
        {

            //PluginLog.Debug($"{entityId:X}:{id}:{arg0}:{arg1}:{arg2}:{arg3}:{arg4}:{arg5}:{targetId:X}:{a10}");
            ActorControlSelfHook.Original(entityId, id, arg0, arg1, arg2, arg3, arg4, arg5, targetId, a10);
            var obj = DalamudApi.ObjectTable.SearchById(entityId);
            if (obj is not Character entity) return;
            var target = DalamudApi.ObjectTable.SearchById((uint)targetId);
            var type = LogMessageType.Debug;
            (type,var message) = id switch
            {
                ActorControlCategory.CancelCast => (LogMessageType.CancelAction, $"{format.FormatNetworkCancelMessage(entityId, entity?.Name.TextValue, arg2, actions.GetRow(arg2)?.Name, arg1 == 1, arg1 != 1)}"),
                ActorControlCategory.Hot => (LogMessageType.DoTHoT, $"{format.FormatNetworkDoTMessage(entityId, entity?.Name.TextValue, true, arg0, arg1, entity?.CurrentHp, entity.MaxHp, entity.CurrentMp, entity.MaxMp, entity?.Position.X, entity?.Position.Z, entity?.Position.Y, entity?.Rotation)}"),
                ActorControlCategory.HoT_DoT => (LogMessageType.DoTHoT, $"{format.FormatNetworkDoTMessage(entityId, entity?.Name.TextValue, false, arg0, arg2, entity?.CurrentHp, entity?.MaxHp, entity?.CurrentMp, entity?.MaxHp, entity?.Position.X, entity?.Position.Z, entity?.Position.Y, entity?.Rotation)}"),
                ActorControlCategory.Death => (LogMessageType.Death, $"{format.FormatNetworkDeathMessage(entityId, entity?.Name.TextValue, arg0, DalamudApi.ObjectTable.SearchById(arg0)?.Name.TextValue)}"),
                ActorControlCategory.TargetIcon => (LogMessageType.TargetIcon, $"{format.FormatNetworkTargetIconMessage(entityId, entity?.Name.TextValue, arg1, arg2, arg0, arg3, arg4, arg5)}"),
                ActorControlCategory.SetTargetSign => (LogMessageType.SignMarker, $"{format.FormatNetworkSignMessage(targetId == 0xE0000000 ? "Delete" : "Add", arg0, entityId, entity.Name.TextValue, targetId == 0xE0000000 ? null : (uint)targetId, target?.Name.TextValue ?? "")}"),
                //TODO:删除标志时的ID修正?
                //ActorControlCategory.LoseEffect => (LogMessageType.StatusRemove, $"{format.FormatNetworkBuffMessage((ushort)arg0, status.GetRow(arg0).Name.RawString, 0.00f, arg2, DalamudApi.ObjectTable.SearchById(arg2)?.Name.TextValue ?? "", entityId, entity.Name.TextValue, (ushort)arg1, entity.CurrentHp, entity.MaxHp)}"),
                ActorControlCategory.DirectorUpdate => (LogMessageType.Director, $"{arg0:X2}|{arg1:X2}|{arg2:X2}|{arg3:X2}|{arg4:X2}|{arg5:X2}"),
                ActorControlCategory.Targetable => (LogMessageType.NameToggle, $"{format.FormatNetworkTargettableMessage(entityId, entity.Name.TextValue, entityId, entity.Name.TextValue, (byte)arg0)}"),
                ActorControlCategory.Tether => (LogMessageType.Tether, $"{format.FormatNetworkTetherMessage(entityId, entity.Name.TextValue, arg2, DalamudApi.ObjectTable.SearchById(arg2)?.Name.TextValue ?? "", arg0, arg4, arg1, arg2, arg3, arg5)}"),
                //TODO:LimitBreak - Line 36
                ActorControlCategory.LogMsg => (LogMessageType.SystemLogMessage, $"{DalamudApi.ClientState.LocalContentId:X2}|{arg0:X2}|{arg1:X2}|{arg2:X2}|{arg3:X2}"),
                //ActorControlCategory.HpSetStat => $"{format.FormatUpdateHpMpTp()}
                //(ActorControlCategory) => 

                //ActorControlCategory.HoT_DoT => $"TESTING::{id}:{entityId:X}:0={arg0:X}:1={arg1:X}:2={arg2}:3={arg3:X}:4={arg4}:5={arg5}:6={targetId:X}",
                _ => (LogMessageType.Debug, "")
            };
            if (type==(LogMessageType)407|| type==(LogMessageType)0x1e|| type == (LogMessageType)49)
            {
                eventHandle.SetLog((LogMessageType)254, $"] ChatLog 00:0:106:" + $"{target.ObjectId:X}:{id:X8}:{arg0:X8}:{arg1:X8}:{arg2:X8}:", DateTime.Now);
            }
            if (id == ActorControlCategory.HoT_DoT && arg1 != 3) type = LogMessageType.Debug;
            var time = DateTime.Now;
            if (type != LogMessageType.Debug) eventHandle.SetLog(type, $"{message}",time);
            if (id == ActorControlCategory.LoseEffect) manager.RemoveStatus(entityId,new NetStatus(){Param = (byte)(arg1>>8),RemainingTime = 0f,SourceID = arg2,StackCount =(byte)(arg1&0xF),StatusID = (ushort)arg0}, time);
            if (id == ActorControlCategory.UpdateEffect) manager.RefreshStatus(entityId, new NetStatus() { Param = (byte)(arg1 >> 8), RemainingTime = 0f, SourceID = arg2, StackCount = (byte)(arg1 & 0xF), StatusID = (ushort)arg0 }, time);
            if (type == LogMessageType.DoTHoT) manager.OutputStatusList(entityId,time);
        }

        private unsafe void ReceiveAbilityEffect(uint sourceId, IntPtr sourceChara, IntPtr pos,
                                                   IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
        {
            //PluginLog.Debug($"Ability");
            var targetCount = *(byte*)(effectHeader + 0x21);
            var data = Marshal.PtrToStructure<Ability1>(effectHeader);
            var effects = Marshal.PtrToStructure<EffectsEntry>(effectArray);
            var targets = Marshal.PtrToStructure<TargetsEntry>(effectTrail);
            ReceiveAbilityHook.Original(sourceId, sourceChara, pos, effectHeader, effectArray, effectTrail);
            var time = DateTime.Now;
            if (targetCount <= 1)
            {
                
                var sourceCharacter = (Character?)DalamudApi.ObjectTable.SearchById(sourceId);
                var targetobject = (Character?)DalamudApi.ObjectTable.SearchById((uint)targets.entry[0]);
                var message = format.FormatNetworkAbilityMessage(sourceId, sourceCharacter?.Name.TextValue ?? "",
                                                                 data.Header.actionId,
                                                                 actions.GetRow(data.Header.actionId)?.Name ?? "",
                                                                 (uint)targets.entry[0], targetobject?.Name.TextValue ?? "",
                                                                 sourceCharacter?.CurrentHp, sourceCharacter?.MaxHp,
                                                                 sourceCharacter?.CurrentMp, sourceCharacter?.MaxMp,
                                                                 sourceCharacter?.Position.X, sourceCharacter?.Position.Z,
                                                                 sourceCharacter?.Position.Y, sourceCharacter?.Rotation,
                                                                 targetobject?.CurrentHp, targetobject?.MaxHp,
                                                                 targetobject?.CurrentMp, targetobject?.MaxMp,
                                                                 targetobject?.Position.X, targetobject?.Position.Z,
                                                                 targetobject?.Position.Y, targetobject?.Rotation,
                                                                 effects.entry[0], effects.entry[1], effects.entry[2],
                                                                 effects.entry[3], effects.entry[4], effects.entry[5],
                                                                 effects.entry[6], effects.entry[7], data.Header.globalSequence,
                                                                 0, targetCount);
                eventHandle.SetLog(LogMessageType.ActionEffect, $"{message}",time);
            }
            else
            {
                var sourceCharacter = (Character?)DalamudApi.ObjectTable.SearchById(sourceId);
                for (byte i = 0; i < targetCount; i++)
                {
                    var targetobject = (Character?)DalamudApi.ObjectTable.SearchById((uint)targets.entry[i]);
                    var message = format.FormatNetworkAbilityMessage(sourceId, sourceCharacter?.Name.TextValue ?? "",
                                                                     data.Header.actionId,
                                                                     actions.GetRow(data.Header.actionId).Name ?? "",
                                                                     (uint)targets.entry[i], targetobject?.Name.TextValue ?? "",
                                                                     sourceCharacter?.CurrentHp, sourceCharacter?.MaxHp,
                                                                     sourceCharacter?.CurrentMp, sourceCharacter?.MaxMp,
                                                                     sourceCharacter?.Position.X, sourceCharacter?.Position.Z,
                                                                     sourceCharacter?.Position.Y, sourceCharacter?.Rotation,
                                                                     targetobject?.CurrentHp, targetobject?.MaxHp,
                                                                     targetobject?.CurrentMp, targetobject?.MaxMp,
                                                                     targetobject?.Position.X, targetobject?.Position.Z,
                                                                     targetobject?.Position.Y, targetobject?.Rotation,
                                                                     (ulong)effects.entry[i * 8 + 0], (ulong)effects.entry[i * 8 + 1], (ulong)effects.entry[i * 8 + 2],
                                                                     (ulong)effects.entry[i * 8 + 3], (ulong)effects.entry[i * 8 + 4], (ulong)effects.entry[i * 8 + 5],
                                                                     (ulong)effects.entry[i * 8 + 6], (ulong)effects.entry[i * 8 + 7], data.Header.globalSequence,
                                                                     i, targetCount);
                    eventHandle.SetLog(LogMessageType.AOEActionEffect, $"{message}",time);

                }
            }
            //ReceiveAbilityHook.Original(sourceId, sourceChara, pos, effectHeader, effectArray, effectTrail);
        }

        private unsafe void ClientState_TerritoryChanged()
        {
            //PluginLog.Debug($"Terryitory");
            CheckPlayer();
            var placeName = territory.GetRow(DalamudApi.ClientState.TerritoryType)?.PlaceName.Value?.Name.RawString;
            if (!string.IsNullOrEmpty(territory.GetRow(DalamudApi.ClientState.TerritoryType)?.ContentFinderCondition.Value?.Name.RawString))
                placeName = territory.GetRow(DalamudApi.ClientState.TerritoryType)?.ContentFinderCondition.Value?.Name
                                     .RawString;
            eventHandle.SetLog(LogMessageType.Territory, $"{format.FormatChangeZoneMessage(DalamudApi.ClientState.TerritoryType, placeName)}",DateTime.Now);
            
            //MapChange();
        }

        private unsafe void MapChange()
        {
            //PluginLog.Debug($"Map");
            if (MapIdDungeon == IntPtr.Zero || MapIdDungeon == IntPtr.Zero) return;
            var MapId = *(uint*)MapIdDungeon == 0 ? *(uint*)MapIdWorld : *(uint*)MapIdDungeon;
            if (oldMap == MapId) return;
            oldMap = MapId;
            var map = maps.GetRow(MapId);
            ClientState_TerritoryChanged();
            eventHandle.SetLog(LogMessageType.ChangeMap, $"{format.FormatChangeMapMessage(MapId, map?.PlaceNameRegion.Value?.Name, map?.PlaceName.Value?.Name, map?.PlaceNameSub.Value?.Name)}",DateTime.Now);
        }

        private void CheckPlayer()
        {
            //PluginLog.Debug($"CheckPlayer");
            if (DalamudApi.ClientState.LocalPlayer is null || plID == DalamudApi.ClientState.LocalPlayer.ObjectId) return;
            plID = DalamudApi.ClientState.LocalPlayer.ObjectId;
            eventHandle.SetLog(LogMessageType.ChangePrimaryPlayer, $"{format.FormatChangePrimaryPlayerMessage(plID, DalamudApi.ClientState.LocalPlayer.Name.TextValue)}",DateTime.Now);
            PlayerState();
        }

        private void WayMark(IntPtr ptr)
        {
            var data = Marshal.PtrToStructure<FFXIVIpcPlaceFieldMarker>(ptr);
            WayMarkHook.Original(ptr);
            var source = DalamudApi.ClientState.LocalPlayer;
            //TODO:Source修复
            eventHandle.SetLog(LogMessageType.WaymarkMarker, $"{format.FormatNetworkWaymarkMessage(data.status == 0 ? "Delete" : "Add", (uint)data.markerId, source.ObjectId, source?.Name.TextValue, data.Xint / 1000f, data.Zint / 1000f, data.Yint / 1000f)}",DateTime.Now);
        }

        private unsafe void WayMarkPresent(IntPtr ptr)
        {
            var data = Marshal.PtrToStructure<FFXIVIpcPlaceFieldMarkerPreset>(ptr);
            WayMarkPresentHook.Original(ptr);
            var time = DateTime.Now;
            var source = DalamudApi.ClientState.LocalPlayer;
            //TODO:Source修复
            for (var i = 0; i < 8; i++)
            {
                eventHandle.SetLog(LogMessageType.WaymarkMarker, $"{format.FormatNetworkWaymarkMessage(((uint)data.status & 1 << i) == 0 ? "Delete" : "Add", (uint)i, 0xE0000000, "", data.Xints[i] / 1000f, data.Zints[i] / 1000f, data.Yints[i] / 1000f)}",time);
            }

        }

        private void Gauge(IntPtr ptr1, IntPtr ptr2)
        {
            var data = Marshal.PtrToStructure<FFXIVIpcActorGauge>(ptr2);
            GaugeHook.Original(ptr1, ptr2);
            eventHandle.SetLog(LogMessageType.Gauge, $"31|0|{DalamudApi.ClientState.LocalPlayer?.Name}|{data.Data0:X}|{data.Data1:X}|{data.Data2:X}",DateTime.Now);
            //TODO:ID不应为0

        }

        private void EffectResult(uint targetId, IntPtr ptr, byte a3)
        {
            //PluginLog.Debug($"EffectR");
            var data = Marshal.PtrToStructure<FFXIVIpcEffectResult>(ptr);
            
            EffectResultHook.Original(targetId, ptr, a3);
            if (a3 != 0) return;
            var target = (Character?)DalamudApi.ObjectTable.SearchById(targetId);
            uint[] array = new uint[20]
            {
                data.Unknown3, 0u, data.Unknown6, data.EffectCount, 0u, 0u, 0u, 0u, 0u, 0u,
                0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u
            };
            var ptr2 = data.Effects;
            //PluginLog.Warning($"{data.EffectCount}");
            var time = DateTime.Now;
            for (int i = 0; i < data.EffectCount; i++)
            {
                if (i > 3) continue;
                var sta = data.Effects[i];
                if (sta.EffectID == 0) continue;
                manager.AddStatus(targetId, new NetStatus(sta),time);
                array[4 + i * 4] = (uint)(ptr2[i].EffectID + (ptr2[i].unknown1 << 16) + (ptr2[i].EffectIndex << 24));
                array[4 + i * 4 + 1] = (uint)(ptr2[i].param + (ptr2[i].unknown3 << 16));
                array[4 + i * 4 + 2] = BitConverter.ToUInt32(BitConverter.GetBytes(ptr2[i].duration), 0);
                array[4 + i * 4 + 3] = ptr2[i].SourceActorID;
            }
            
            eventHandle.SetLog(LogMessageType.EffectResult, format.FormatEffectResultMessage(targetId,target?.Name.TextValue,data.RelatedActionSequence,data.CurrentHP,data.MaxHP,data.CurrentMP, 10000u, data.DamageShield,target?.Position.X,target?.Position.Z,target?.Position.Y,target?.Rotation,array),time);
            


        }

        private void EffectResultBasic(uint targetId, IntPtr ptr, byte a3)
        {
            //PluginLog.Debug($"EffectResultBasic");
            var data = Marshal.PtrToStructure<FFXIVIpcEffectResult>(ptr);
            //PluginLog.Warning($"Basic:{data.Effects[0].EffectID}");
            EffectResultBasicHook.Original(targetId, ptr, a3);
            
            var target = (Character?)DalamudApi.ObjectTable.SearchById(targetId);
            uint[] array = Array.Empty<uint>();
            var ptr2 = data.Effects;

            eventHandle.SetLog(LogMessageType.EffectResult, format.FormatEffectResultMessage(targetId, target?.Name.TextValue, data.RelatedActionSequence, data.CurrentHP, null,null,null, null, target?.Position.X, target?.Position.Z, target?.Position.Y, target?.Rotation, array),DateTime.Now);
        }

        private void PlayerState()
        {
            if (PlayerStat == IntPtr.Zero) return;
            var player = Marshal.PtrToStructure<PlayerStruct64>(PlayerStat);
            if (playerstat.Equals(player)) return;
            playerstat = player;
            eventHandle.SetLog(LogMessageType.PlayerStats, $"{format.FormatPlayerStatsMessage(player.LocalContentId, player.Job, player.Str, player.Dex, player.Vit, player.Int, player.Mnd, player.Pie, player.Attack, player.DirectHit, player.Crit, player.AttackMagicPotency, player.HealMagicPotency, player.Det, player.SkillSpeed, player.SpellSpeed, player.Tenacity)}",DateTime.Now);
        }

        private void BuffList1Do(uint targetId, IntPtr b, byte c)
        {
            //PluginLog.Debug($"BuffList");
            var effectList = Marshal.PtrToStructure<StatusEffectList>(b);
            BuffList1Hook.Original(targetId, b, c);
            var array = new uint[93];
            array[0] = effectList.Unknown1;
            array[1] = effectList.Unknown2;
            List<NetStatus> netStatusList = new List<NetStatus>();
            for (var i = 0; i < 30; i++)
            {
                if (effectList.Effects[i].StatusID == 0) continue;
                
                array[i * 3 + 3] = (uint)(effectList.Effects[i].StatusID + (effectList.Effects[i].StackCount << 16) + (effectList.Effects[i].Param << 24));
                array[i * 3 + 3 + 1] = BitConverter.ToUInt32(BitConverter.GetBytes(effectList.Effects[i].RemainingTime), 0);
                array[i * 3 + 3 + 2] = effectList.Effects[i].SourceID;
                effectList.Effects[i].RemainingTime = effectList.Effects[i].RemainingTime < 0
                                                          ? -effectList.Effects[i].RemainingTime
                                                          : effectList.Effects[i].RemainingTime;
                netStatusList.Add(effectList.Effects[i]);
                //PluginLog.Log(effectList.Effects[i].RemainingTime.ToString());
            }
            manager.UpdateStatusList(targetId,netStatusList,DateTime.Now);

            var jobLevels = (uint)(effectList.JobID + (effectList.Level1 << 8) + (effectList.Level2 << 16) + (effectList.Level3 << 24));
            var combatantById = DalamudApi.ObjectTable.SearchById(targetId);
            var text = format.FormatStatusListMessage(targetId, combatantById?.Name.TextValue, jobLevels, effectList.CurrentHP, effectList.MaxHP, effectList.CurrentMP, effectList.MaxMP, effectList.DamageShield, combatantById?.Position.X, combatantById?.Position.Z, combatantById?.Position.Y, combatantById?.Rotation, array);
            eventHandle.SetLog(LogMessageType.StatusList, text,DateTime.Now);
        }

        unsafe void EnvControlFunc(long a1, IntPtr a2)
        {
            var data = Marshal.PtrToStructure<Server_EnvironmentControl>(a2);
            EnvControlHook.Original(a1, a2);
            eventHandle.SetLog((LogMessageType)254, $"] ChatLog 00:0:103:"+ $"00000000:{data.directorId:X8}:{data.State:X8}:{data.parm3:X8}:{data.parm4:X8}:", DateTime.Now);
        }

        void UpdateHPMPTP(uint id, IntPtr a2, byte a3)
        {
            var data = Marshal.PtrToStructure<FFXIVIpcUpdateHpMpTp>(a2);
            UpdateHPMPHook.Original(id,a2,a3);
            var combatantById = DalamudApi.ObjectTable.SearchById(id);
            if (combatantById is not Character target) return;
            var text = format.FormatUpdateHpMpTp(id, combatantById?.Name.TextValue, data.hp, target.MaxHp, data.mp,
                                                 target.MaxMp, target.Position.X, target.Position.Z, target.Position.Y,target.Rotation);
            eventHandle.SetLog(LogMessageType.UpdateHp,text,DateTime.Now);
        }



        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            CommandManager.RemoveHandler(CommandName);
            ActorControlSelfHook.Dispose();
            ReceiveAbilityHook.Dispose();
            CastHook.Dispose();
            WayMarkHook.Dispose();
            WayMarkPresentHook.Dispose();
            GaugeHook.Dispose();
            EffectResultHook.Dispose();
            EffectResultBasicHook.Dispose();
            BuffList1Hook.Dispose();
            EnvControlHook.Dispose();
            UpdateHPMPHook.Dispose();
            UpdatePartyHook.Dispose();
            eventHandle.CloseFile();
            CreateObjectHook.Dispose();
            //new IPC().Unsub();

            DalamudApi.Framework.Update -= FrameworkOnUpdate;
            DalamudApi.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            WindowSystem.GetWindow("DDD Config").IsOpen = true;
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            WindowSystem.GetWindow("DDD Config").IsOpen = true;
        }

    }
}
