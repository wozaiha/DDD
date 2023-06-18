using System;
using System.Runtime.InteropServices;
using uint8_t = System.Byte;
using uint16_t = System.UInt16;
using uint32_t = System.UInt32;

namespace DDD.Struct
{
    [StructLayout(LayoutKind.Explicit, Size = 0x30)]
    public unsafe struct Server_EnvironmentControl
    {
        [FieldOffset(0)]
        public UInt32 directorId;
        [FieldOffset(4)]
        public UInt32 State;
        [FieldOffset(8)]
        public UInt16 parm3;
        [FieldOffset(12)]
        public UInt16 parm4;
    }
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public unsafe struct FFXIVIpcObjectSpawn
    {
        [FieldOffset(0)]
        public uint8_t spawnIndex;
        [FieldOffset(1)]
        public uint8_t objKind;
        [FieldOffset(2)]
        public uint8_t flag;
        [FieldOffset(3)]
        public uint8_t InvisibilityGroup;
        [FieldOffset(4)]
        public uint32_t objId;
        [FieldOffset(8)]
        public uint32_t EntityId;
        [FieldOffset(12)]
        public uint32_t levelId;
        [FieldOffset(16)]
        public uint32_t ContentId;
        [FieldOffset(20)]
        public uint32_t OwnerId;
        [FieldOffset(24)]
        public uint32_t BindLayoutId;
        [FieldOffset(28)]
        public float scale;
        [FieldOffset(32)]
        public uint16_t SharedGroupTimelineState;
        [FieldOffset(34)]
        public uint16_t Dir;
        [FieldOffset(36)]
        public uint16_t FATE;
        [FieldOffset(38)]
        public uint16_t PermissionInvisibility;
        [FieldOffset(40)]
        public uint16_t Args2;
        [FieldOffset(42)]
        public uint16_t unknown28c;
        [FieldOffset(44)]
        public uint32_t housingLink;
        [FieldOffset(48)]
        public FFXIVARR_POSITION3 position;
        [FieldOffset(50)]
        public uint16_t unknown3C;
        [FieldOffset(52)]
        public uint16_t unknown3E;
    }
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct FFXIVARR_POSITION3
    {
        [FieldOffset(0)]
        public float x;
        [FieldOffset(4)]
        public float y;
        [FieldOffset(8)]
        public float z;
    }
};
