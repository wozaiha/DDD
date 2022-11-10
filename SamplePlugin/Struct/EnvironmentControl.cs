using System.Runtime.InteropServices;

namespace DDD.Struct
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Server_EnvironmentControl
    {
        public uint FeatureID; // seen 0x80xxxxxx, seems to be unique identifier of controlled feature
        public uint State; // typically hiword and loword both have one bit set; in disassembly this is actually 2 words
        public byte Index; // if feature has multiple elements, this is a 0-based index of element
        public byte u0; // padding?
        public ushort u1; // padding?
        public uint u2; // padding?
    }
};
