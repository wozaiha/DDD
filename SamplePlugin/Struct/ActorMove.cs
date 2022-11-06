namespace SamplePlugin.Struct
{
    struct FFXIVIpcActorMove
    {
        /* 0000 */
        byte headRotation;
        /* 0001 */
        byte rotation;
        /* 0002 */
        byte animationType;
        /* 0003 */
        byte animationState;
        /* 0004 */
        byte animationSpeed;
        /* 0005 */
        byte unknownRotation;
        /* 0006 */
        ushort posX;
        /* 0008 */
        ushort posY;
        /* 000a */
        ushort posZ;
        /* 000C */
        uint unknown_12;
    };
};
