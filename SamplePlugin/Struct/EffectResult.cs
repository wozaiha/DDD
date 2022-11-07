using System.Runtime.InteropServices;

namespace SamplePlugin.Struct
{
   unsafe struct FFXIVIpcEffectResult
   {
       private uint unknown0;
        public uint globalSequence;
        public uint actor_id;
        public uint current_hp;
        public uint max_hp;
        public ushort current_mp;
        public byte unknown1;
        public byte classId;
        public byte shieldPercentage;
        public byte entryCount;
        public ushort unknown2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public StatusEntry[] statusEntries;

    };
    struct StatusEntry
    {
        public byte index; // which position do i display this
        byte unknown3;
        public ushort id;
        public ushort param;
        ushort unknown4; // Sort this out (old right half of power/param property)
        public float duration;
        public uint sourceActorId;
    }
};
