using System.Runtime.InteropServices;

namespace DDD.Struct
{
    public unsafe struct FFXIVIpcPlayerStats
    {
        // order comes from baseparam order column
        public uint strength;
        public uint dexterity;
        public uint vitality;
        public uint intelligence;
        public uint mind;
        public uint piety;
        public uint hp;
        public uint mp;
        public uint tp;
        public uint gp;
        public uint cp;
        public uint delay;
        public uint tenacity;
        public uint attackPower;
        public uint defense;
        public uint directHitRate;
        public uint evasion;
        public uint magicDefense;
        public uint criticalHit;
        public uint attackMagicPotency;
        public uint healingMagicPotency;
        public uint elementalBonus;
        public uint determination;
        public uint skillSpeed;
        public uint spellSpeed;
        public uint haste;
        public uint craftsmanship;
        public uint control;
        public uint gathering;
        public uint perception;

        // todo: what is here?
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26 * 4)]
        public byte[] unknown;
    };

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct PlayerStruct64
    {
        [FieldOffset(88)]
        public ulong LocalContentId;

        [FieldOffset(106)]
        public byte Job;

        [FieldOffset(368)]
        public uint Str;

        [FieldOffset(372)]
        public uint Dex;

        [FieldOffset(376)]
        public uint Vit;

        [FieldOffset(380)]
        public uint Int;

        [FieldOffset(384)]
        public uint Mnd;

        [FieldOffset(388)]
        public uint Pie;

        [FieldOffset(440)]
        public uint Tenacity;

        [FieldOffset(444)]
        public uint Attack;

        [FieldOffset(452)]
        public uint DirectHit;

        [FieldOffset(472)]
        public uint Crit;

        [FieldOffset(496)]
        public uint AttackMagicPotency;

        [FieldOffset(500)]
        public uint HealMagicPotency;

        [FieldOffset(540)]
        public uint Det;

        [FieldOffset(544)]
        public uint SkillSpeed;

        [FieldOffset(548)]
        public uint SpellSpeed;

        public override bool Equals(object? obj)
        {
            if (obj is not PlayerStruct64 player) return false;
            var eq = true;
            eq &= LocalContentId == player.LocalContentId;
            eq &= Job == player.Job;
            eq &= Str == player.Str;
            eq &= Dex == player.Dex;
            eq &= Vit == player.Vit;
            eq &= Int == player.Int;
            eq &= Mnd == player.Mnd;
            eq &= Pie == player.Pie;
            eq &= Tenacity == player.Tenacity;
            eq &= Attack == player.Attack;
            eq &= DirectHit == player.DirectHit;
            eq &= Crit == player.Crit;
            eq &= AttackMagicPotency == player.AttackMagicPotency;
            eq &= HealMagicPotency == player.HealMagicPotency;
            eq &= Det == player.Det;
            eq &= SkillSpeed == player.SkillSpeed;
            eq &= SpellSpeed == player.SpellSpeed;
            return eq;
        }
    }
};
