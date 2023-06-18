namespace DDD.Struct
{
    internal unsafe struct FFXIVIpcPlaceFieldMarkerPreset
    {
        /*! which fieldmarks to show */
        public FieldMarkerStatus status;
        /*! A coordinates would be (float)Xints[0]/1000.0, (float)Yints[0]/1000.0, (float)Zints[0]/1000.0 */
        public fixed uint Xints[8];
        public fixed uint Yints[8];
        public fixed uint Zints[8];
    };

    /**
     * Structural representation of the packet sent by the server
     * to place/remove a field marker
     */
    unsafe struct FFXIVIpcPlaceFieldMarker
    {
        public FieldMarkerId markerId;
        public byte status;
        public fixed byte pad[2];
        public int Xint;
        public int Yint;
        public int Zint;
    };

    enum FieldMarkerStatus : uint
    {
        A = 0x1,
        B = 0x2,
        C = 0x4,
        D = 0x8,
        One = 0x10,
        Two = 0x20,
        Three = 0x40,
        Four = 0x80
    };

    enum FieldMarkerId : byte
    {
        A,
        B,
        C,
        D,
        One,
        Two,
        Three,
        Four
    };
};
