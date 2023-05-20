using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
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
        if (buffs is null) return;
        if (buffs.Contains(buff))
        {
            RefreshStatus(id, buff, time);
            return;
        }
        var target = DalamudApi.ObjectTable.SearchById(id);
        var source = DalamudApi.ObjectTable.SearchById(buff.SourceID);
        var maxhp = source is Character ? (uint?)((Character)source).MaxHp : null;
        var targetMaxHp = target is Character ? (uint?)((Character)target).MaxHp : null;
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
        if (buffs is null) return;
        if (!buffs.Contains(buff))
        {
            AddStatus(id,buff,time);
        }

        buffs.Remove(buff);
        var target = DalamudApi.ObjectTable.SearchById(id);
        var source = DalamudApi.ObjectTable.SearchById(buff.SourceID);
        var maxhp = source is Character ? (uint?)((Character)source).MaxHp : null;
        var targetMaxHp = target is Character ? (uint?)((Character)target).MaxHp : null;
        buff.RemainingTime = buff.RemainingTime == 0f ? 9999.00f : buff.RemainingTime;
        var log =
            $"{Format.FormatNetworkBuffMessage(buff.StatusID, status.GetRow(buff.StatusID)?.Name.RawString, buff.RemainingTime, buff.SourceID, source?.Name.TextValue, id, target?.Name.TextValue, (ushort)((buff.Param << 8) + buff.StackCount), targetMaxHp, maxhp)}";
        //EventHandle.SetLog(LogMessageType.StatusAdd, log, time);
        buffs.Add(buff);
        manager[id] = buffs;
    }

    public void UpdateStatusList(uint id, List<NetStatus> newList, DateTime time)
    {
        if (!manager.TryGetValue(id, out var buffs))
        {
            foreach (var buff in newList) AddStatus(id,buff, time);
            return;
        }

        var add = newList.Except(buffs).ToList();
        var remove = buffs.Except(newList).ToList();
        var update = UpdateList(buffs, newList);
        foreach (var buff in add)
        {
            AddStatus(id,buff,time);
        }

        foreach (var buff in remove)
        {
            RemoveStatus(id,buff,time);
        }

        foreach (var buff in update)
        {
            RefreshStatus(id,buff,time);
        }
    }

    private List<NetStatus> UpdateList(List<NetStatus> oldList, List<NetStatus> newList)
    {
        var list = new List<NetStatus>();
        foreach (var newbuff in newList)
        {
            if (!oldList.Contains(newbuff)) continue;
            var index = oldList.FindIndex(x => x.Equals(newbuff));
            var oldbuff = oldList[index];
            if (oldbuff.Param != newbuff.Param || oldbuff.StackCount != newbuff.StackCount || oldbuff.RemainingTime < newbuff.RemainingTime) list.Add(newbuff);
        }
        
        return list;
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
