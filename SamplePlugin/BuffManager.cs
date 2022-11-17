using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using DDD.Plugins;
using DDD.Struct;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace DDD;

public class BuffManager
{
    private Dictionary<uint, List<NetStatus>> manager = new Dictionary<uint, List<NetStatus>>();
    private LogFormat Format;
    Event EventHandle;
    private ExcelSheet<Status>? status;

    public BuffManager(LogFormat format,Event eventHandle)
    {
        
        Format = format;
        EventHandle = eventHandle;
        status = DalamudApi.DataManager.GetExcelSheet<Status>();
    }

    public void RemoveAll(uint id)
    {
        var time = DateTime.Now;
        if (!manager.TryGetValue(id, out var buffs)) return;
        foreach (var buff in buffs)
        {
            RemoveStatus(id, buff, time,false);
        }

        manager.Remove(id);
    }

    public void AddStatus(uint id, NetStatus buff, DateTime time)
    {
        
        if (!manager.TryGetValue(id, out var buffs)) manager.Add(id,new List<NetStatus>());
        if (buffs is null || buffs.Contains(buff)) return;
        var oldbuff = SearchBuff(buffs, buff);
        buffs.Remove(oldbuff);
        var target = DalamudApi.ObjectTable.SearchById(id);
        var source = DalamudApi.ObjectTable.SearchById(buff.SourceID);
        var maxhp = source is Character ? (uint?)((Character)source).MaxHp : null;
        var targetMaxHp = target is Character ? (uint?)((Character)target).MaxHp : null;
        //PluginLog.Warning($"Adding:{status.GetRow(buff.StatusID)?.Name.RawString} from {source?.Name.TextValue} to {target?.Name.TextValue} duration {buff.RemainingTime:0.0}s");
        buff.RemainingTime = buff.RemainingTime == 0f ? 9999.00f : buff.RemainingTime;
        var log =
            $"{Format.FormatNetworkBuffMessage(buff.StatusID, status.GetRow(buff.StatusID)?.Name.RawString, buff.RemainingTime, buff.SourceID, source?.Name.TextValue, id, target?.Name.TextValue, (ushort)((buff.Param << 8) + buff.StackCount), targetMaxHp, maxhp)}";
        EventHandle.SetLog(LogMessageType.StatusAdd, log, time);
        buffs.Add(buff);
        manager[id] = buffs;
    }

    public void RemoveStatus(uint id, NetStatus buff, DateTime time, bool update = true)
    {
        if (!manager.TryGetValue(id, out var buffs)) manager.Add(id, new List<NetStatus>());
        if (buffs is null || !buffs.Contains(buff)) return;
        var target = DalamudApi.ObjectTable.SearchById(id);
        var source = DalamudApi.ObjectTable.SearchById(buff.SourceID);
        var maxhp = source is Character ? (uint?)((Character)source).MaxHp : null;
        var targetMaxHp = target is Character ? (uint?)((Character)target).MaxHp : null;
        //PluginLog.Warning($"Removing:{status.GetRow(buff.StatusID)?.Name.RawString} from {source?.Name.TextValue} to {target?.Name.TextValue}");
        buff.RemainingTime = buff.RemainingTime == 0f ? 9999.00f : buff.RemainingTime;
        var log =
            $"{Format.FormatNetworkBuffMessage(buff.StatusID, status.GetRow(buff.StatusID)?.Name.RawString, 0f, buff.SourceID, source?.Name.TextValue, id, target?.Name.TextValue, (ushort)((buff.Param << 8) + buff.StackCount), targetMaxHp, maxhp)}";
        EventHandle.SetLog(LogMessageType.StatusRemove, log, time);
        if (update)
        {
            buffs.Remove(buff);
            manager[id] = buffs;
        }
    }

    public void RefreshStatus(uint id, NetStatus buff, DateTime time)
    {
        if (buff.StatusID == 0) return;
        if (!manager.TryGetValue(id, out var buffs)) manager.Add(id, new List<NetStatus>());
        if (buffs is null || !buffs.Contains(buff))
        {
            PluginLog.Error($"Trying to Update Effect: Target={id:X} BuffId={buff.StatusID}");
            buffs.Add(buff);
            manager[id] = buffs;
        }
        var target = DalamudApi.ObjectTable.SearchById(id);
        var source = DalamudApi.ObjectTable.SearchById(buff.SourceID);
        var maxhp = source is Character ? (uint?)((Character)source).MaxHp : null;
        var targetMaxHp = target is Character ? (uint?)((Character)target).MaxHp : null;
        buff.RemainingTime = buff.RemainingTime == 0f ? 9999.00f : buff.RemainingTime;
        var log =
            $"{Format.FormatNetworkBuffMessage(buff.StatusID, status.GetRow(buff.StatusID)?.Name.RawString, buff.RemainingTime, buff.SourceID, source?.Name.TextValue, id, target?.Name.TextValue, (ushort)((buff.Param << 8) + buff.StackCount), targetMaxHp, maxhp)}";
        EventHandle.SetLog(LogMessageType.StatusAdd, log, time);
    }

    public void UpdateStatusList(uint id, List<NetStatus> newList, DateTime time)
    {
        if (!manager.TryGetValue(id, out var buffs))
        {
            foreach (var buff in newList) AddStatus(id,buff, time);
            return;
        }

        var add = newList.Except(buffs).ToList();
        var minus = buffs.Except(newList).ToList();
        var remove = Except(minus, add);
        foreach (var buff in add)
        {
            AddStatus(id, buff, time);
        }
        foreach (var buff in remove)
        {
            RemoveStatus(id, buff, time);
        }

    }

    private List<NetStatus> Except(List<NetStatus> remove, List<NetStatus> add)
    {
        var list = new List<NetStatus>();
        foreach (var buff in remove)
        {
            foreach (var newBuff in add)
            {
                if (buff.StatusID == newBuff.StatusID && buff.SourceID == newBuff.SourceID) list.Add(buff);
            }
        }

        list = remove.Except(list).ToList();
        return list;
    }

    private NetStatus SearchBuff(List<NetStatus> list, NetStatus buff)
    {
        var result = new NetStatus();
        foreach (var v in list)
        {
            if (buff.StatusID == v.StatusID && buff.SourceID == v.SourceID) result = v;
        }
        return result;
    }

    public void OutputStatusList(uint id, DateTime time)
    {
        if (!manager.TryGetValue(id, out var effectList)) return;
        var array = new uint[93];
        //array[0] = effectList.Unknown1;
        //array[1] = effectList.Unknown2;
        for (var i = 0; i < effectList.Count; i++)
        {
            array[i * 3 + 3] = (uint)(effectList[i].StatusID + (effectList[i].StackCount << 16) + (effectList[i].Param << 24));
            array[i * 3 + 3 + 1] = BitConverter.ToUInt32(BitConverter.GetBytes(effectList[i].RemainingTime), 0);
            array[i * 3 + 3 + 2] = effectList[i].SourceID;
        }
        var combatantById = (Character?)DalamudApi.ObjectTable.SearchById(id);
        if (combatantById == null) return;
        var jobLevels = (uint)(combatantById.ClassJob.Id + (combatantById.Level << 8) + (combatantById.Level << 16) + (0 << 24));
        var text = Format.FormatStatusListMessage(id, combatantById?.Name.TextValue, jobLevels, combatantById.CurrentHp, combatantById.MaxHp, combatantById.CurrentMp, combatantById.MaxMp, 0, combatantById?.Position.X, combatantById?.Position.Z, combatantById?.Position.Y, combatantById?.Rotation, array);
        EventHandle.SetLog(LogMessageType.StatusList, text, time);
    }
}
