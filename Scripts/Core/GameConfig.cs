using Godot;

/// <summary>
/// Game configuration constants extracted directly from Pascal INIT.PAS
/// These values must match exactly with the original Usurper game
/// </summary>
public static class GameConfig
{
    // From Pascal global_maxXX constants
    public const int MaxPlayers = 400;           // global_maxplayers
    public const int MaxTeamMembers = 5;         // global_maxteammembers
    public const int MaxAllows = 15;             // global_maxallows
    public const int MaxNod = 5;                 // global_maxnod
    public const int MaxMon = 17;                // global_maxmon (active monsters)
    public const int MaxMSpells = 6;             // global_maxmspells
    public const int MaxItem = 15;               // global_maxitem
    public const int MaxHittas = 450;            // global_maxhittas (dungeon objects)
    public const int MaxSpells = 12;             // global_maxspells
    public const int MaxCombat = 14;             // global_maxcombat
    public const int MaxClasses = 11;            // global_maxclasses
    public const int MaxRaces = 10;              // global_maxraces
    public const int MaxBarrelMasters = 15;      // global_maxbarrelmasters
    public const int MaxInput = 2000000000;      // global_maxinput
    public const int MaxMailLines = 15;          // global_maxmaillines
    public const int KingGuards = 5;             // global_KingGuards
    
    // Combat constants
    public const int CriticalHitChance = 5;      // 5% base critical hit chance
    public const float BackstabMultiplier = 3f;  // From MURDER.PAS
    public const float BerserkMultiplier = 2f;   // From FIGHT.PAS
    public const int MaxPoison = 100;            // maxpoison
    public const int MaxDarkness = 5;            // maxdarkness
    public const int MaxDrugs = 100;             // maxdrugs
    
    // Game limits
    public const int MaxLevel = 200;             // maxlevel
    public const int TurnsPerDay = 325;          // turns_per_day
    public const int MaxChildren = 8;            // maxchildren
    public const int MaxKingEdicts = 5;          // max_kingedicts
    public const int MaxHeals = 15;              // max healing potions
    
    // Item limits
    public const int MaxItems = 325;             // maxitems
    public const int MaxArmor = 17;              // maxarmor
    public const int MaxWeapons = 35;            // maxweapons
    
    // Location constants
    public const int MaxMonsters = 65;           // maxmonsters
    public const int MaxGuards = 15;             // maxguards
    public const int MaxLevels = 25;             // maxlevels (dungeon)
    
    // Special NPC marker
    public const string NpcMark = "*";           // global_npcmark
    
    // File paths (from Pascal constants)
    public const string DataDir = "DATA/";       // global_datadir
    public const string DocsDir = "DOCS/";       // global_docsdir
    public const string NodeDir = "NODE/";       // global_nodedir
    public const string ScoreDir = "SCORES/";    // global_scoredir
    public const string TextDir = "TEXT/";       // global_textdir
    
    // Key files
    public const string UsurperCfg = "USURPER.CFG";
    public const string TextDataFile = "USUTEXT.DAT";
    public const string UserFile = DataDir + "USERS.DAT";
    public const string NpcFile = DataDir + "NPCS.DAT";
    public const string MonsterFile = DataDir + "MONSTER.DAT";
    public const string LevelFile = DataDir + "LEVELS.DAT";
    public const string MailFile = DataDir + "MAIL.DAT";
    public const string ArmorFile = DataDir + "ARMOR.DAT";
    public const string WeaponFile = DataDir + "WEAPON.DAT";
    public const string BankSafeFile = DataDir + "BANKSAFE.DAT";
    public const string WantedFile = DataDir + "WANTED.DAT";
    public const string GuardsFile = DataDir + "GUARDS.DAT";
    public const string DateFile = DataDir + "DATE.DAT";
    public const string FameFile = DataDir + "FAME.DAT";
    public const string MarketFile = DataDir + "PLMARKET.DAT";
    public const string ChestFile = DataDir + "CHEST.DAT";
    public const string GodsFile = DataDir + "GODS.DAT";
    public const string KingFile = DataDir + "KING.DAT";
    public const string RelationFile = DataDir + "RELATION.DAT";
    public const string ChildrenFile = DataDir + "CHILDREN.DAT";
    
    // Display constants
    public const int ScreenLines = 25;           // global_screenlines
    
    // Money/currency settings
    public const string MoneyType = "gold";      // default money type
    public const string MoneyType2 = "coin";     // singular form
    public const string MoneyType3 = "coins";    // plural form
    
    // Game version info
    public const string WebAddress = "http://www.usurper.info";
    public const string LevelRaiseText = "(you are eligible for a level raise!)";
    
    // Color constants (from Pascal)
    public const byte HpColor = 12;              // global_hpcol
    public const byte TalkColor = 13;            // global_talkcol
    public const byte TeamColor = 3;             // global_teamcol
    public const byte PlayerColor = 10;          // global_plycol
    public const byte GodColor = 10;             // global_godcol
    public const byte KingColor = 10;            // global_kingcol
    public const byte KidColor = 10;             // global_kidcol
    public const byte MonsterColor = 9;          // global_moncol
    public const byte ItemColor = 11;            // global_itemcol
    public const byte BashColor = 3;             // global_bashcol
    public const byte RelationColor = 6;         // global_relationcol
    
    // Online system constants
    public const int OnlineMaxWaits = 4500;      // global_online_maxwaits
    public const int OnlineMaxWaitsBigLoop = 50000; // global_online_maxwaits_bigloop
    public const string OnLocal = "Loc";         // global_onlocal
    
    // Special character constants
    public const char ReturnKey = '\r';          // #13
    public const char EscapeKey = '\x1b';        // #27
    public const char DeleteKey = '\b';          // #8
    public const char MaxInputKey = '>';         // MaxInput_key
    
    // Deleted player names
    public const string DelName1 = "EMPTY";      // global_delname1
    public const string DelName2 = "EMPTY";      // global_delname2
    
    // ANSI control character
    public const char AnsiControlChar = '`';     // acc
    
    // Game state flags (initialized as per Pascal)
    public static bool UBeta = false;            // global_ubeta
    public static bool UTest = false;            // global_utest
    public static bool Multi = false;            // global_multi
    public static bool UShare = true;            // global_ushare
    public static bool Ansi = false;             // global_ansi
    public static bool Registered = false;       // global_registered
    public static bool MaintRunning = false;     // global_maintrunning
    public static bool CarrierDropped = false;   // global_carrierdropped
    public static bool CheckCarrier = false;     // global_checkcarrier
    
    // Color values
    public static byte CForeground = 2;          // global_cfor
    public static byte CBackground = 0;          // global_cback
    
    // Dungeon level (affects XP calculation)
    public static int DungeonLevel = 3;          // global_dungeonlevel
    
    // Fake players
    public static byte FakePlayers = 0;          // global_fakeplayers
    
    // Supreme being equipment flags
    public static bool SupremeLantern = false;   // global_s_lantern
    public static bool SupremeSword = false;     // global_s_sword
    public static bool SupremeBStaff = false;    // global_s_bstaff
    public static bool SupremeWStaff = false;    // global_s_wstaff
    
    // God activity flag
    public static bool GodActive = false;        // Global_GodActive
    
    // Special game state flags
    public static bool PlayerInSteroids = false; // global_PlayerInSteroids
    public static bool PlayerInFight = false;    // global_PlayerInFight
    public static bool Begged = false;           // global_begged
    public static bool NoBeg = true;             // global_nobeg
    public static bool Escape = true;            // global_escape
    public static bool Killed = false;          // global_killed
    public static bool IceMap = false;           // global_icemap
    public static bool MonsterInit = false;      // global_monsterinit
    public static bool OneMin = false;           // global_onemin
    public static bool TwoMin = false;           // global_twomin
    
    // Maintenance text color
    public const int MaintTxtColor = 10;         // global_mainttxt
    
    // Auto probe location
    public static Places AutoProbe = Places.NoWhere; // global_auto_probe

    // Castle and Royal Court Constants
    public const int MaxRoyalGuards = 20;
    public const int MaxMoatGuards = 100;
    public const int MinLevelKing = 10;              // Minimum level to challenge for throne
    public const long DefaultRoyalTreasury = 50000;  // Starting royal treasury
    public const float DonationTaxRate = 0.1f;       // Tax rate on donations to royal purse
    
    // Royal Tax Alignment Types (Pascal: taxalignment)
    public enum TaxAlignment
    {
        All = 0,        // Everyone must pay
        Good = 1,       // Only good characters pay
        Evil = 2        // Only evil characters pay
    }
    
    // Royal Guard System
    public const long BaseGuardSalary = 1000;        // Base daily salary for guards
    public const int GuardRecruitmentCost = 5000;    // Cost to recruit a guard
    
    // Prison System (integrated with Castle)
    public const int MaxPrisonEscapeAttempts = 3;    // Daily escape attempts
    public const long PrisonBailMultiplier = 1000;   // Level * multiplier = bail cost
    
    // Royal Orphanage
    public const int MaxRoyalOrphans = 50;           // Maximum orphans in royal care
    public const long OrphanCareCost = 100;          // Daily cost per orphan
    
    // Court Magician
    public const long MagicSpellBaseCost = 500;      // Base cost for royal magic
    public const int MaxRoyalSpells = 10;            // Max spells available to king
}

/// <summary>
/// Game locations for auto-probe system (from Pascal)
/// </summary>
public enum Places
{
    NoWhere,
    MainStreet,
    Slottet,        // Castle
    Inn,
    Dormy,          // Dormitory
    Prison,
    UmanCave,
    AtHome
}

/// <summary>
/// Pascal location constants - exact match with CMS.PAS onloc_ constants
/// </summary>
public enum GameLocation
{
    NoWhere = 0,
    MainStreet = 1,      // onloc_mainstreet
    TheInn = 2,          // onloc_theinn  
    DarkAlley = 3,       // onloc_darkalley (outside the shady shops)
    Church = 4,          // onloc_church
    WeaponShop = 5,      // onloc_weaponshop
    Master = 6,          // onloc_master (level master)
    MagicShop = 7,       // onloc_magicshop
    Dungeons = 8,        // onloc_dungeons
    DeathMaze = 9,       // onloc_deathmaze
    MadMage = 17,        // onloc_madmage (groggo's shop)
    ArmorShop = 18,      // onloc_armorshop
    Bank = 19,           // onloc_bank
    ReportRoom = 20,     // onloc_reportroom
    Healer = 21,         // onloc_healer
    Marketplace = 22,    // onloc_marketplace
    FoodStore = 23,      // onloc_foodstore
    PlayerMarket = 24,   // onloc_plymarket
    Recruit = 25,        // onloc_recruit (hall of recruitment)
    Dormitory = 26,      // onloc_dormitory
    AnchorRoad = 27,     // onloc_anchorroad
    Orbs = 28,           // onloc_orbs (orbs bar)
    BobsBeer = 31,       // onloc_bobsbeer (Bob's Beer Hut)
    Alchemist = 32,      // onloc_alchemist
    Steroids = 33,       // onloc_steroids (Lizard's Training Center)
    Drugs = 34,          // onloc_drugs
    Darkness = 35,       // onloc_darkness
    Whores = 36,         // onloc_whores
    Gigolos = 38,        // onloc_gigolos
    OutsideInn = 39,     // onloc_outsideinn
    TeamCorner = 41,     // onloc_teamcorner
    Temple = 47,         // onloc_temple (altar of the gods)
    BountyRoom = 44,     // onloc_bountyroom
    QuestHall = 75,      // onloc_questhall
    
    // Castle locations
    Castle = 70,         // onloc_castle (royal castle)
    RoyalMail = 71,      // onloc_royalmail
    CourtMage = 72,      // onloc_courtmage
    WarChamber = 73,     // onloc_warchamber
    QuestMaster = 74,    // onloc_questmaster
    RoyalOrphanage = 77, // onloc_royorphanag
    GuardOffice = 80,    // onloc_guardoffice
    OutsideCastle = 81,  // onloc_outcastle
    
    // Prison locations
    Prison = 90,         // onloc_prison
    Prisoner = 91,       // onloc_prisoner (in cell)
    PrisonerOpen = 92,   // onloc_prisonerop (cell door open)
    PrisonerExecution = 93, // onloc_prisonerex
    PrisonWalk = 94,     // onloc_prisonwalk (outside prison)
    PrisonBreak = 95,    // onloc_prisonbreak
    ChestLoot = 96,      // onloc_chestloot
    
    // Relationship locations
    LoveStreet = 200,    // onloc_lovestreet
    Home = 201,          // onloc_home
    Nursery = 202,       // onloc_nursery
    Kidnapper = 203,     // onloc_kidnapper
    GiftShop = 204,      // onloc_giftshop
    
    // Special locations
    IceCaves = 300,      // onloc_icecaves
    Heaven = 400,        // onloc_heaven
    HeavenBoss = 401,    // onloc_heaven_boss
    
    Closed = 30000       // onloc_closed (for fake players)
} 