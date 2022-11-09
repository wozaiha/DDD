using System.Collections.ObjectModel;

namespace DDD.Plugins;

public interface ILogFormat
{
    string FormatChangeZoneMessage(uint ZoneId, string ZoneName);

    string FormatChangeMapMessage(uint MapId, string Region, string Name, string SubName);

    string FormatChangePrimaryPlayerMessage(uint? PlayerID, string PlayerName);

    string FormatPlayerStatsMessage(ulong LocalContentId, uint JobID, uint Str, uint Dex, uint Vit, uint Intel, uint Mnd, uint Pie, uint Attack, uint DirectHit, uint Crit, uint AttackMagicPotency, uint HealMagicPotency, uint Det, uint SkillSpeed, uint SpellSpeed, uint Tenacity);

    string FormatCombatantMessage(uint CombatantID, uint OwnerID, string CombatantName, int JobID, int Level, uint WorldID, string WorldName, uint BNpcNameID, uint BNpcID, uint currentHp, uint maxHp, uint currentMp, uint maxMp, float PosX, float PosY, float PosZ, float Heading);

    string FormatPartyMessage(int partyCount, ReadOnlyCollection<uint> partyList);

    string FormatChatMessage(uint eventType, string player, string text);

    string FormatVersion();

    string FormatProcess(string clientMode, int? processId, bool isAdministrator, string gameVersion);

    string FormatNetworkBuffMessage(ushort BuffID, string buffName, float Duration, uint sourceId, string sourceName, uint TargetID, string TargetName, ushort BuffExtra, uint? TargetMaxHP, uint? sourceMaxHP);

    string FormatNetworkLimitBreakfMessage(byte maxLB, uint limitbreak);

    string FormatByteArray(byte[] data);

    string FormatUIntArray(params uint[] param);

    //string FormatMemorySettings(int ProcessID, string LogFileFolder, bool LogAllNetworkData, bool DisableCombatLog, string NetworkIP, bool UseWinPCap, bool UseSocketFilter);

    //string FormatParseSettings(bool DisableDamageShield, bool DisableCombinePets, Language LanguageID, ParseFilterMode ParseFilter, bool SimulateIndividualDoTCrits, bool ShowRealDoTTicks);

    string FormatNetworkAbilityMessage(uint sourceId, string sourceName, uint skillId, string skillName, uint targetId, string targetName, uint? sourceCurrentHp, uint? sourceMaxHp, uint? sourceCurrentMp, uint? sourceMaxMp, float? sourcePosX, float? sourcePosY, float? sourcePosZ, float? sourceHeading, uint? targetCurrentHp, uint? targetMaxHp, uint? targetCurrentMp, uint? targetMaxMp, float? targetPosX, float? targetPosY, float? targetPosZ, float? targetHeading, ulong effectData1, ulong effectData2, ulong effectData3, ulong effectData4, ulong effectData5, ulong effectData6, ulong effectData7, ulong effectData8, uint sequence, byte targetIndex, byte totalTargets);

    string FormatNetworkCastMessage(uint sourceId, string sourceName, uint targetId, string targetName, uint skillId, string skillName, float Duration, float? posX, float? posY, float? posZ, float? heading);

    string FormatNetworkSignMessage(string markerEvent, uint markerId, uint sourceId, string sourceName, uint? targetId, string targetName);

    string FormatNetworkWaymarkMessage(
        string markerEvent, uint markerId, uint sourceId, string sourceName, float? posX, float? posY, float? posZ);

    string FormatNetworkDoTMessage(uint targetId, string targetName, bool isHeal, uint buffId, uint amount, uint? targetCurrentHp, uint? targetMaxHp, uint? targetCurrentMp, uint? targetMaxMp, float? targetPosX, float? targetPosY, float? targetPosZ, float? targetHeading);

    string FormatNetworkCancelMessage(uint targetId, string targetName, uint skillId, string skillName, bool cancelled, bool interrupted);

    string FormatNetworkDeathMessage(uint targetId, string targetName, uint sourceId, string sourceName);

    string FormatNetworkTargetIconMessage(uint targetId, string targetName, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6);

    string FormatNetworkTargettableMessage(uint targetId, string targetName, uint sourceId, string sourceName, byte param);

    string FormatNetworkTetherMessage(uint targetId, string targetName, uint sourceId, string sourceName, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6);

    string FormatEffectResultMessage(uint targetId, string targetName, uint sequence, uint currentHp, uint? maxHP, uint? currentMP, uint? maxMP, byte? damageShield, float? PosX, float? PosY, float? PosZ, float? Heading, params uint[] data);

    string FormatStatusListMessage(uint targetId, string targetName, uint JobLevels, uint currentHp, uint maxHp, uint currentMp, uint maxMp, byte DamageShield, float? posX, float? posY, float? posZ, float? heading, params uint[] statusList);

    string FormatStatusListMessage3(uint targetId, string targetName, params uint[] statusList);

    string FormatUpdateHpMpTp(uint targetId, string targetName, uint currentHp, uint? maxHp, uint currentMp, uint? maxMp, float? PosX, float? PosY, float? PosZ, float? Heading);
}
