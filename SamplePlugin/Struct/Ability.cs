using System.Runtime.InteropServices;

namespace DDD.Struct
{
    
    public struct Header
    {
        public uint animationTargetId;

        public uint unknown;

        public uint actionId;

        public uint globalSequence;

        public float animationLockTime;

        public uint SomeTargetID;

        public ushort hiddenAnimation;

        public ushort rotation;

        public ushort actionAnimationId;

        public byte variation;

        public byte effectDisplayType;

        public byte unknown20;

        public byte effectCount;

        public ushort padding21;

        
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x8)]
    public struct EffectEntry //0x8
    {
        [FieldOffset(0x0)] public byte type;
        [FieldOffset(0x1)] public byte param1;
        [FieldOffset(0x2)] public byte param2;
        [FieldOffset(0x3)] public byte param3;
        [FieldOffset(0x4)] public byte param4;
        [FieldOffset(0x5)] public byte param5;
        [FieldOffset(0x6)] public ushort param0;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x6)]
    public struct Ender
    {
        [FieldOffset(0x0)] public ushort padding1;
        [FieldOffset(0x2)] public uint padding2;
    }


    public unsafe struct Ability1
    {
        public Header Header;
        public fixed ulong Effects[1 * 8];
        public Ender ender;
        public fixed ulong targetId[1];
    }

    public unsafe struct Ability8
    {
        public Header header;
        public fixed ulong enrty[8 * 8];
        public Ender ender;
        public fixed ulong targetId[8];
    }

    public unsafe struct Ability16
    {
        public Header header;
        public fixed ulong enrty[16 * 8];
        public Ender ender;
        public fixed ulong targetId[16];
    }

    public unsafe struct Ability24
    {
        public Header header;
        public fixed ulong enrty[24 * 8];
        public Ender ender;
        public fixed ulong targetId[24];
    }

    public unsafe struct Ability32
    {
        public Header header;
        public fixed ulong enrty[32 * 8];
        public Ender ender;
        public fixed ulong targetId[32];
    }

    public unsafe struct EffectsEntry
    {
        public fixed ulong entry[32 * 8];
    }

    public unsafe struct TargetsEntry
    {
        public fixed ulong entry[32];
    }
}
