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
        public MemberEntry[] members;
        ulong partyId;
        ulong channelId;
        public byte leaderIndex;
        public byte partySize;
        public ushort padding1;
        public uint padding2;
};
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class MemberEntry
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] name;
        public ulong contentId;
        public uint charaId;
        public uint u1;
        public uint u2;
        public uint hp;
        public uint maxHp;
        public ushort mp;
        public ushort maxMp;
        public ushort u3;
        public ushort zoneId;
        public byte gposeSelectable;
        public byte classId;
        public byte u5;
        public byte level;
        public byte isLevelSync;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30 * 0xC)]
        private byte[] effects;
    }
}
