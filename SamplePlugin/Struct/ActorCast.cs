namespace DDD.Struct
{
    internal struct ActorCast
    {
        public ushort ActionID;
        public SkillType SkillType;
        public byte unknown;
        public uint unknown_1; // action id or mount id
        public float CastTime;
        public uint TargetID;
        public ushort Rotation;
        public ushort flag; // 1 = interruptible blinking cast bar
        public uint unknown_2;
        public ushort PosX;
        public ushort PosY;
        public ushort PosZ;
        public ushort unknown_3;
    }
    enum SkillType : byte
    {
        Normal = 0x1,
        ItemAction = 0x2,
        MountSkill = 0xD,
    };
};
