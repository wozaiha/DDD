using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DDD.Struct
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class Party
    { 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public MemberEntry[] Member;
        ulong PartyID;
        ulong ChatChannel;
        byte LeaderIndex;
        public byte PartyCount;
        byte __padding1;
        byte __padding2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30 * 0xC)]
        private byte[] effects;
    };
    [StructLayout(LayoutKind.Explicit, Size = 440)]
    internal class MemberEntry
    {
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        //byte[] Name;
        //ulong CharaId;
        [FieldOffset(32+8)]public uint EntityId;
        //uint ParentEntityId;
        //byte Valid;
        //byte ClassJob;
        //byte Sex;
        //byte Role;
        //byte Lv;
        //byte LvSync;
        //byte ObjType;
        //byte BuddyCommand;
        //uint Hp;
        //uint HpMax;
        //ushort Mp;
        //ushort MpMax;
        //ushort Tp;
        //ushort TerritoryType;
        //uint PetEntityId;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 30 * 0xC)]
        //private byte[] effects;

    }
}
