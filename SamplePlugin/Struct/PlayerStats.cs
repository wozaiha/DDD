namespace SamplePlugin.Struct
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
        fixed  uint unknown[26];
    };
};
