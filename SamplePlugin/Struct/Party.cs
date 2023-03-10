using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DDD.Struct
{
    [StructLayout(LayoutKind.Explicit, Size = 0xDD8)]
    internal struct Party
    { 
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        [FieldOffset(0x0)]public MemberEntry[] Member;
        [FieldOffset(8 * 440)]ulong PartyID;
        [FieldOffset(8 * 440 + 0x8)] ulong ChatChannel;
        [FieldOffset(8 * 440 + 0x10)] byte LeaderIndex;
        [FieldOffset(8 * 440 + 0x18)] public  byte PartyCount;
        //byte __padding1;
        //byte __padding2;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 30 * 0xC)]
        //private byte[] effects;
    };
    [StructLayout(LayoutKind.Explicit, Size = 440)]
    internal struct MemberEntry
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
