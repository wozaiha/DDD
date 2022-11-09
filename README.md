# SamplePlugin

Simple example plugin for Dalamud.

This is not designed to be the simplest possible example, but it is also not designed to cover everything you might want to do. For more detailed questions, come ask in [the Discord](https://discord.gg/3NMcUV5).

## Main Points

    ChatLog = 0,先不管
    
    Territory = 1,done,但是ACT资源文件翻译和本地有区别是什么鬼
    
    ChangePrimaryPlayer = 2,done
    
    AddCombatant = 3, done,100ms比较一次
    
    RemoveCombatant = 4,done
    
    PartyList = 11,done,拿partylist 每帧比较
    
    PlayerStats = 12,
    
    StartsCasting = 20,done
    
    ActionEffect = 21,done
    
    AOEActionEffect = 22,done
    
    CancelAction = 23,done
    
    DoTHoT = 24,done
    
    Death = 25,done
    
    StatusAdd = 26, done
    
    TargetIcon = 27,done
    
    WaymarkMarker = 28,done
    
    SignMarker = 29,done
    
    StatusRemove = 30,done
    
    Gauge = 31,done
    
    Director = 33,done
    
    NameToggle = 34, done
    
    Tether = 35,done(取消连线ACT没写)
    
    LimitBreak = 36, 
    
    EffectResult = 37,NetworkActionSync,
    
    StatusList = 38,
    
    UpdateHp = 39, 
    
    ChangeMap = 40,done
    
    SystemLogMessage = 41,done
    
    StatusList3 = 42,
//以下不管
    Settings = 249,
    Process = 250,
    Debug = 251,
    PacketDump = 252,
    Version = 253,
    Error = 254
