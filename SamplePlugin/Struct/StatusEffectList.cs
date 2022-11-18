using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;
using Lumina.Excel.GeneratedSheets;

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
        public float RemainingTime;
        [FieldOffset(8)]
        public uint SourceID;

        public override bool Equals(object? obj)
        {
            if (obj is null or not NetStatus) return false;
            var tem = (NetStatus)obj;
            if (StatusID != tem.StatusID) return false;
            //if (StackCount != tem.StackCount) return false;
            //if (Param != tem.Param) return false;
            //if (RemainingTime >= tem.RemainingTime) return false;
            if (!SourceID.Equals(tem.SourceID)) return false;
            return true;
        }

        public override int GetHashCode()
        {

            return (int)(SourceID + (StatusID << 16));
        }

        public NetStatus(StatusEntry entry)
        {
            SourceID = entry.SourceActorID;
            Param = (byte)(entry.param >> 8);
            RemainingTime = entry.duration <0 ? -entry.duration : entry.duration;
            StackCount = (byte)(entry.param & 0xF);
            StatusID = entry.EffectID;

        }


    }
    //class NetStatusComparer : IEqualityComparer<NetStatus>
    //{
    //    public bool Equals(NetStatus x, NetStatus y)
    //    {

    //        if (x.StatusID != y.StatusID) return false;
    //        if (x.StackCount != y.StackCount) return false;
    //        if (x.Param != y.Param) return false;
    //        if (x.RemainingTime >= y.RemainingTime) return false;
    //        if (!x.SourceID.Equals(y.SourceID)) return false;
    //        return true;
    //    }

    //    // If Equals() returns true for a pair of objects
    //    // then GetHashCode() must return the same value for these objects.

    //    public int GetHashCode(NetStatus product)
    //    {
    //        return 0;
    //    }
    //}




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
        public byte DamageShield;
        public ushort Unknown1; // used to be TP
        public byte Unknown2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public NetStatus[] Effects;
    }
}
