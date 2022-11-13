using System.Runtime.InteropServices;

namespace DDD.Struct
{
    unsafe struct FFXIVIpcEffectResult
    {
        public uint Unknown1;

        public uint RelatedActionSequence;

        public uint ActorID;

        public uint CurrentHP;

        public uint MaxHP;

        public ushort CurrentMP;

        public ushort Unknown3;

        public byte DamageShield;

        public byte EffectCount;

        public ushort Unknown6;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public StatusEntry[] Effects;

    };
    public struct StatusEntry
    {
        public byte EffectIndex;

        public byte unknown1;

        public ushort EffectID;

        public ushort param;

        public ushort unknown3;

        public float duration;

        public uint SourceActorID;
    }
};
