namespace SamplePlugin.Struct
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
        fixed byte effect[16*30];
        uint padding;
    };
};
