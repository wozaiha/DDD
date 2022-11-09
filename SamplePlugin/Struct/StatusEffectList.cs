using System.Runtime.InteropServices;

namespace DDD.Struct
{
    unsafe struct FFXIVIpcStatusEffectList
    {
        byte classId;
        byte level1;
        ushort level;
        uint current_hp;
        uint max_hp;
        ushort current_mp;
        ushort max_mp;
        byte shieldPercentage;
        byte unknown1;
        ushort unknown2;
        fixed byte effect[16 * 30];
        uint padding;
    };

    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct NetStatus
    {
        [FieldOffset(0)]
        public ushort StatusID;
        [FieldOffset(2)]
        public byte StackCount;
        [FieldOffset(3)]
        public byte Param;
        [FieldOffset(4)]
        public uint RemainingTime;
        [FieldOffset(8)]
        public uint SourceID;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct StatusEffectList
    {
        public byte JobID;
        public byte Level1;
        public byte Level2;
        public byte Level3;
        public uint CurrentHP;
        public uint MaxHP;
        public ushort CurrentMP;
        public ushort MaxMP;
        public ushort Unknown1; // used to be TP
        public byte DamageShield;
        public byte Unknown2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public NetStatus[] Effects;
    }
};
