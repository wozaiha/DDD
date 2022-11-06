using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SamplePlugin.Plugins;

public class LogFormat : ILogFormat
{

    public string FormatVersion()
    {
        return ((FormattableString)$"This is IINACT based on FFXIV_ACT_Plugin {typeof(LogOutput).Assembly.GetName().Version}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatProcess(string clientMode, int? processId, bool isAdministrator, string gameVersion)
    {
        return ((FormattableString)$"Detected Process ID: {processId.GetValueOrDefault()}, Client Mode: {clientMode}, IsAdmin: {isAdministrator}, Game Version: {gameVersion}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatMemorySettings(int ProcessID, string LogFileFolder, bool LogAllNetworkData, bool DisableCombatLog, string NetworkIP, bool UseWinPCap, bool UseSocketFilter)
    {
        return ((FormattableString)$"Selected Process ID: {ProcessID}, Dump All Network Data: {LogAllNetworkData}, Disable Combat Log: {DisableCombatLog}, Selected IP: {NetworkIP}, WinPcap: {UseWinPCap}, Socket Filter: {UseSocketFilter}").ToString(CultureInfo.InvariantCulture);
    }

    //public string FormatParseSettings(bool DisableDamageShield, bool DisableCombinePets, Language LanguageID, ParseFilterMode ParseFilter, bool SimulateIndividualDoTCrits, bool ShowRealDoTTicks)
    //{
    //	return ((FormattableString)$"Selected Language ID: {LanguageID}, Disable Damage Shield: {DisableDamageShield}, Disable Combine Pets: {DisableCombinePets}, Parse Filter: {ParseFilter}, DoTCrits: {SimulateIndividualDoTCrits}, RealDoTs: {ShowRealDoTTicks}").ToString(CultureInfo.InvariantCulture);
    //}

    public string FormatCombatantMessage(uint CombatantID, uint OwnerID, string CombatantName, int JobID, int Level, uint WorldID, string WorldName, uint BNpcNameID, uint BNpcID, uint currentHp, uint maxHp, uint currentMp, uint maxMp, float PosX, float PosY, float PosZ, float Heading)
    {
        return ((FormattableString)$"{CombatantID:X4}|{CombatantName}|{JobID:X2}|{Level:X1}|{OwnerID:X4}|{WorldID:X2}|{WorldName}|{BNpcNameID}|{BNpcID}|").ToString(CultureInfo.InvariantCulture) + FormatCombatantProperties(currentHp, maxHp, currentMp, maxMp, null, PosX, PosY, PosZ, Heading);
    }

    public string FormatChangeZoneMessage(uint ZoneId, string ZoneName)
    {
        return ((FormattableString)$"{ZoneId:X2}|{ZoneName}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatChangeMapMessage(uint MapId, string Region, string Name, string SubName)
    {
        return ((FormattableString)$"{MapId}|{Region}|{Name}|{SubName}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatChangePrimaryPlayerMessage(uint? PlayerID, string PlayerName)
    {
        return ((FormattableString)$"{PlayerID.GetValueOrDefault():X4}|{PlayerName}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatPartyMessage(int partyCount, ReadOnlyCollection<uint> partyList)
    {
        FormattableString formattableString = $"{partyCount}";
        if (partyList.Any())
        {
            return formattableString.ToString(CultureInfo.InvariantCulture) + "|" + string.Join("|", partyList.Select((x) => $"{x:X4}").ToArray());
        }
        return formattableString.ToString(CultureInfo.InvariantCulture);
    }

    public string FormatPlayerStatsMessage(ulong LocalContentId, uint JobID, uint Str, uint Dex, uint Vit, uint Intel, uint Mnd, uint Pie, uint Attack, uint DirectHit, uint Crit, uint AttackMagicPotency, uint HealMagicPotency, uint Det, uint SkillSpeed, uint SpellSpeed, uint Tenacity)
    {
        return ((FormattableString)$"{JobID}|{Str}|{Dex}|{Vit}|{Intel}|{Mnd}|{Pie}|{Attack}|{DirectHit}|{Crit}|{AttackMagicPotency}|{HealMagicPotency}|{Det}|{SkillSpeed}|{SpellSpeed}|0|{Tenacity}|{LocalContentId:X8}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatChatMessage(uint eventType, string player, string text)
    {
        return ((FormattableString)$"{eventType:X4}|{player}|{text}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatByteArray(byte[] data)
    {
        if (data == null)
        {
            return "";
        }
        var stringBuilder = new StringBuilder(data.Length * 3);
        for (var i = 0; i < data.Length / 4; i++)
        {
            stringBuilder.Append($"{BitConverter.ToUInt32(data, i * 4):X8}|");
        }
        return stringBuilder.ToString();
    }

    public string FormatUIntArray(params uint[] param)
    {
        var stringBuilder = new StringBuilder(param.Length * 4);
        for (var i = 0; i < param.Length; i++)
        {
            stringBuilder.Append(string.Format("{0:X2}{1}", param[i], i < param.Length - 1 ? "|" : ""));
        }
        return stringBuilder.ToString();
    }

    public string FormatNetworkBuffMessage(ushort BuffID, string buffName, float Duration, uint sourceId, string sourceName, uint TargetID, string TargetName, ushort BuffExtra, uint? TargetMaxHP, uint? sourceMaxHP)
    {
        return ((FormattableString)$"{BuffID:X2}|{buffName}|{Duration:0.00}|{sourceId:X4}|{sourceName}|{TargetID:X4}|{TargetName}|{BuffExtra:X2}|{TargetMaxHP}|{sourceMaxHP}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatNetworkLimitBreakfMessage(byte maxLB, uint limitbreak)
    {
        return ((FormattableString)$"{limitbreak:X4}|{maxLB}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatNetworkAbilityMessage(uint sourceId, string sourceName, uint skillId, string skillName, uint targetId, string targetName, uint? sourceCurrentHp, uint? sourceMaxHp, uint? sourceCurrentMp, uint? sourceMaxMp, float? sourcePosX, float? sourcePosY, float? sourcePosZ, float? sourceHeading, uint? targetCurrentHp, uint? targetMaxHp, uint? targetCurrentMp, uint? targetMaxMp, float? targetPosX, float? targetPosY, float? targetPosZ, float? targetHeading, ulong effectData1, ulong effectData2, ulong effectData3, ulong effectData4, ulong effectData5, ulong effectData6, ulong effectData7, ulong effectData8, uint sequence, byte targetIndex, byte totalTargets)
    {
        return string.Concat(string.Concat(string.Concat($"{sourceId:X2}|{sourceName}|{skillId:X2}|{skillName}|{targetId:X2}|{targetName}|" + $"{effectData1 & 0xFFFFFFFFu:X1}|{effectData1 >> 32:X1}|" + $"{effectData2 & 0xFFFFFFFFu:X1}|{effectData2 >> 32:X1}|" + $"{effectData3 & 0xFFFFFFFFu:X1}|{effectData3 >> 32:X1}|" + $"{effectData4 & 0xFFFFFFFFu:X1}|{effectData4 >> 32:X1}|" + $"{effectData5 & 0xFFFFFFFFu:X1}|{effectData5 >> 32:X1}|" + $"{effectData6 & 0xFFFFFFFFu:X1}|{effectData6 >> 32:X1}|" + $"{effectData7 & 0xFFFFFFFFu:X1}|{effectData7 >> 32:X1}|" + $"{effectData8 & 0xFFFFFFFFu:X1}|{effectData8 >> 32:X1}|", FormatCombatantProperties(targetCurrentHp, targetMaxHp, targetCurrentMp, targetMaxMp, null, targetPosX, targetPosY, targetPosZ, targetHeading)), "|", FormatCombatantProperties(sourceCurrentHp, sourceMaxHp, sourceCurrentMp, sourceMaxMp, null, sourcePosX, sourcePosY, sourcePosZ, sourceHeading)), $"|{sequence:X8}|{targetIndex}|{totalTargets}".ToString(CultureInfo.InvariantCulture));
    }

    public string FormatNetworkCastMessage(uint sourceId, string sourceName, uint targetId, string targetName, uint skillId, string skillName, float Duration, float? posX, float? posY, float? posZ, float? heading)
    {
        return ((FormattableString)$"{sourceId:X4}|{sourceName}|{skillId:X2}|{skillName}|{targetId:X4}|{targetName}|{Duration:0.000}|{posX:0.00}|{posY:0.00}|{posZ:0.00}|{heading:0.00}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatNetworkSignMessage(string markerEvent, uint markerId, uint sourceId, string sourceName, uint? targetId, string targetName)
    {
        return ((FormattableString)$"{markerEvent}|{markerId}|{sourceId:X4}|{sourceName}|{targetId:X4}|{targetName}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatNetworkWaymarkMessage(
        string markerEvent, uint markerId, uint sourceId, string sourceName, float? posX, float? posY, float? posZ)
    {
        return ((FormattableString)$"{markerEvent}|{markerId}|{sourceId:X4}|{sourceName}|{posX:0.00}|{posY:0.00}|{posZ:0.00}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatNetworkDoTMessage(uint targetId, string targetName, bool isHeal, uint buffId, uint amount, uint? targetCurrentHp, uint? targetMaxHp, uint? targetCurrentMp, uint? targetMaxMp, float? targetPosX, float? targetPosY, float? targetPosZ, float? targetHeading)
    {
        return FormattableStringFactory.Create("{0:X4}|{1}|{2}|{3:X1}|{4:X1}|", targetId, targetName, isHeal ? "HoT" : "DoT", buffId, amount).ToString(CultureInfo.InvariantCulture) + FormatCombatantProperties(targetCurrentHp, targetMaxHp, targetCurrentMp, targetMaxMp, null, targetPosX, targetPosY, targetPosZ, targetHeading);
    }

    public string FormatNetworkCancelMessage(uint targetId, string targetName, uint skillId, string skillName, bool cancelled, bool interrupted)
    {
        return FormattableStringFactory.Create("{0:X2}|{1}|{2:X2}|{3}|{4}{5}", targetId, targetName, skillId, skillName, cancelled ? "Cancelled" : "", interrupted ? "Interrupted" : "").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatNetworkDeathMessage(uint targetId, string targetName, uint sourceId, string sourceName)
    {
        return ((FormattableString)$"{targetId:X2}|{targetName}|{sourceId:X2}|{sourceName}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatNetworkTargetIconMessage(uint targetId, string targetName, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6)
    {
        return ((FormattableString)$"{targetId:X2}|{targetName}|{param1:X4}|{param2:X4}|{param3:X4}|{param4:X4}|{param5:X4}|{param6:X4}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatNetworkTargettableMessage(uint targetId, string targetName, uint sourceId, string sourceName, byte param)
    {
        return ((FormattableString)$"{targetId:X2}|{targetName}|{sourceId:X2}|{sourceName}|{param:X2}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatNetworkTetherMessage(uint targetId, string targetName, uint sourceId, string sourceName, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6)
    {
        return ((FormattableString)$"{targetId:X2}|{targetName}|{sourceId:X2}|{sourceName}|{param1:X4}|{param2:X4}|{param3:X4}|{param4:X4}|{param5:X4}|{param6:X4}").ToString(CultureInfo.InvariantCulture);
    }

    public string FormatEffectResultMessage(uint targetId, string targetName, uint sequence, uint currentHp, uint? maxHP, uint? currentMP, uint? maxMP, byte? damageShield, float? PosX, float? PosY, float? PosZ, float? Heading, params uint[] data)
    {
        var text = $"{targetId:X4}|{targetName}|{sequence:X8}|";
        text += FormatCombatantProperties(currentHp, maxHP, currentMP, maxMP, damageShield, PosX, PosY, PosZ, Heading);
        for (var i = 0; i < data.Length / 4; i++)
        {
            if (data[i * 4] != 0 || data[i * 4 + 1] != 0 || data[i * 4 + 2] != 0 || data[i * 4 + 3] != 0)
            {
                text = text + "|" + SimpleUIntFormat(data[i * 4]);
                text = text + "|" + SimpleUIntFormat(data[i * 4 + 1]);
                text = text + "|" + SimpleUIntFormat(data[i * 4 + 2]);
                text = text + "|" + SimpleUIntFormat(data[i * 4 + 3]);
            }
        }
        return text;
    }

    public string FormatStatusListMessage(uint targetId, string targetName, uint JobLevels, uint currentHp, uint maxHp, uint currentMp, uint maxMp, byte DamageShield, float? posX, float? posY, float? posZ, float? heading, params uint[] statusList)
    {
        var text = $"{targetId:X4}|{targetName}|{JobLevels:X8}|";
        text += FormatCombatantProperties(currentHp, maxHp, currentMp, maxMp, DamageShield, posX, posY, posZ, heading);
        var num = 0;
        for (var i = 0; i < statusList.Length; i++)
        {
            if (statusList[i] != 0)
            {
                num = i;
            }
        }
        for (var j = 0; j < statusList.Length / 3 && j * 3 <= num; j++)
        {
            text = text + "|" + SimpleUIntFormat(statusList[j * 3]);
            text = text + "|" + SimpleUIntFormat(statusList[j * 3 + 1]);
            text = text + "|" + SimpleUIntFormat(statusList[j * 3 + 2]);
        }
        return text;
    }

    public string FormatStatusListMessage3(uint targetId, string targetName, params uint[] statusList)
    {
        var text = $"{targetId:X4}|{targetName}";
        var num = 0;
        for (var i = 0; i < statusList.Length; i++)
        {
            if (statusList[i] != 0)
            {
                num = i;
            }
        }
        for (var j = 0; j < statusList.Length / 3 && j * 3 <= num; j++)
        {
            text = text + "|" + SimpleUIntFormat(statusList[j * 3]);
            text = text + "|" + SimpleUIntFormat(statusList[j * 3 + 1]);
            text = text + "|" + SimpleUIntFormat(statusList[j * 3 + 2]);
        }
        return text;
    }

    public string FormatUpdateHpMpTp(uint targetId, string targetName, uint currentHp, uint? maxHp, uint currentMp, uint? maxMp, float? PosX, float? PosY, float? PosZ, float? Heading)
    {
        return ((FormattableString)$"{targetId:X2}|{targetName}|").ToString(CultureInfo.InvariantCulture) + FormatCombatantProperties(currentHp, maxHp, currentMp, maxMp, null, PosX, PosY, PosZ, Heading);
    }

    public string SimpleUIntFormat(uint data)
    {
        if (data < 256)
        {
            if (data != 0)
            {
                return $"{data:X2}";
            }
            return "0";
        }
        if (data < 65536)
        {
            return $"{data:X4}";
        }
        if (data < 16777216)
        {
            return $"{data:X6}";
        }
        return $"{data:X8}";
    }

    public string FormatCombatantProperties(uint? currentHp, uint? maxHp, uint? currentMp, uint? maxMp, byte? damageShield, float? posX, float? posY, float? posZ, float? heading)
    {
        return ((FormattableString)$"{currentHp}|{maxHp}|{currentMp}|{maxMp}|{damageShield}||{posX:0.00}|{posY:0.00}|{posZ:0.00}|{heading:0.00}").ToString(CultureInfo.InvariantCulture);
    }
}
