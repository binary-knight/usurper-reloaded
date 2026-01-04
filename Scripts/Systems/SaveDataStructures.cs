using System;
using System.Collections.Generic;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Main save game data structure
    /// </summary>
    public class SaveGameData
    {
        public int Version { get; set; }
        public DateTime SaveTime { get; set; }
        public DateTime LastDailyReset { get; set; }
        public int CurrentDay { get; set; }
        public DailyCycleMode DailyCycleMode { get; set; }
        public PlayerData Player { get; set; } = new();
        public List<NPCData> NPCs { get; set; } = new();
        public WorldStateData WorldState { get; set; } = new();
        public DailySettings Settings { get; set; } = new();

        // Story systems
        public StorySystemsData StorySystems { get; set; } = new();
    }

    /// <summary>
    /// Data for all story/narrative systems
    /// </summary>
    public class StorySystemsData
    {
        // Ocean Philosophy
        public int AwakeningLevel { get; set; }
        public List<int> CollectedFragments { get; set; } = new();
        public List<int> ExperiencedMoments { get; set; } = new();

        // Seven Seals
        public List<int> CollectedSeals { get; set; } = new();

        // Companions
        public List<CompanionSaveInfo> Companions { get; set; } = new();
        public List<int> ActiveCompanionIds { get; set; } = new();
        public List<CompanionDeathInfo> FallenCompanions { get; set; } = new();

        // Grief state
        public int GriefStage { get; set; }
        public int GriefDaysRemaining { get; set; }
        public string GriefCompanionName { get; set; } = "";

        // Story progression flags
        public Dictionary<string, bool> StoryFlags { get; set; } = new();
        public int CurrentCycle { get; set; } = 1;
    }

    public class CompanionSaveInfo
    {
        public int Id { get; set; }
        public bool IsRecruited { get; set; }
        public bool IsActive { get; set; }
        public bool IsDead { get; set; }
        public int LoyaltyLevel { get; set; }
        public int TrustLevel { get; set; }
        public int RomanceLevel { get; set; }
        public bool PersonalQuestStarted { get; set; }
        public bool PersonalQuestCompleted { get; set; }
        public int RecruitedDay { get; set; }
    }

    public class CompanionDeathInfo
    {
        public int CompanionId { get; set; }
        public int DeathType { get; set; }
        public string Circumstance { get; set; } = "";
        public string LastWords { get; set; } = "";
        public int DeathDay { get; set; }
    }

    /// <summary>
    /// Player data for save system
    /// </summary>
    public class PlayerData
    {
        // Basic info
        public string Name1 { get; set; } = "";
        public string Name2 { get; set; } = "";
        public string RealName { get; set; } = "";
        
        // Core stats
        public int Level { get; set; }
        public long Experience { get; set; }
        public long HP { get; set; }
        public long MaxHP { get; set; }
        public long Gold { get; set; }
        public long BankGold { get; set; }
        
        // Attributes
        public long Strength { get; set; }
        public long Defence { get; set; }
        public long Stamina { get; set; }
        public long Agility { get; set; }
        public long Charisma { get; set; }
        public long Dexterity { get; set; }
        public long Wisdom { get; set; }
        public long Intelligence { get; set; }
        public long Constitution { get; set; }
        public long Mana { get; set; }
        public long MaxMana { get; set; }

        // Equipment and items
        public long Healing { get; set; }  // CRITICAL: Healing potions count
        public long WeapPow { get; set; }  // CRITICAL: Weapon power
        public long ArmPow { get; set; }   // CRITICAL: Armor power
        
        // Character details
        public CharacterRace Race { get; set; }
        public CharacterClass Class { get; set; }
        public char Sex { get; set; }
        public int Age { get; set; }
        public DifficultyMode Difficulty { get; set; } = DifficultyMode.Normal;
        
        // Game state
        public string CurrentLocation { get; set; } = "";
        public int TurnsRemaining { get; set; }
        public int DaysInPrison { get; set; }
        public bool CellDoorOpen { get; set; }  // Has been rescued
        public string RescuedBy { get; set; } = "";  // Name of rescuer
        public int PrisonEscapes { get; set; }  // Escape attempts remaining
        
        // Daily limits
        public int Fights { get; set; }
        public int PFights { get; set; }
        public int TFights { get; set; }
        public int Thiefs { get; set; }
        public int Brawls { get; set; }
        public int Assa { get; set; }
        public int DarkNr { get; set; }  // Dark deeds remaining today
        public int ChivNr { get; set; }  // Chivalry deeds remaining today
        
        // Items and equipment
        public int[] Items { get; set; } = new int[0];
        public int[] ItemTypes { get; set; } = new int[0];

        // NEW: Modern RPG Equipment System
        public Dictionary<int, int> EquippedItems { get; set; } = new(); // EquipmentSlot -> Equipment ID

        // Curse status for equipped items
        public bool WeaponCursed { get; set; }
        public bool ArmorCursed { get; set; }
        public bool ShieldCursed { get; set; }

        // Player inventory (dungeon loot, etc.)
        public List<InventoryItemData> Inventory { get; set; } = new();

        // Base stats (without equipment bonuses)
        public long BaseStrength { get; set; }
        public long BaseDexterity { get; set; }
        public long BaseConstitution { get; set; }
        public long BaseIntelligence { get; set; }
        public long BaseWisdom { get; set; }
        public long BaseCharisma { get; set; }
        public long BaseMaxHP { get; set; }
        public long BaseMaxMana { get; set; }
        public long BaseDefence { get; set; }
        public long BaseStamina { get; set; }
        public long BaseAgility { get; set; }

        // Social/Team
        public string Team { get; set; } = "";
        public string TeamPassword { get; set; } = "";
        public bool IsTeamLeader { get; set; }
        public int TeamRec { get; set; }  // Team record, days had town
        public int BGuard { get; set; }   // Type of guard
        
        // Status
        public long Chivalry { get; set; }
        public long Darkness { get; set; }
        public int Mental { get; set; }
        public int Poison { get; set; }
        public int GnollP { get; set; }  // Gnoll poison (temporary)
        public int Addict { get; set; }  // Drug addiction level
        public int SteroidDays { get; set; }  // Days remaining on steroids
        public int DrugEffectDays { get; set; }  // Days remaining on drug effects
        public int ActiveDrug { get; set; }  // Currently active drug type (DrugType enum)
        public int Mercy { get; set; }   // Mercy counter

        // Disease status
        public bool Blind { get; set; }
        public bool Plague { get; set; }
        public bool Smallpox { get; set; }
        public bool Measles { get; set; }
        public bool Leprosy { get; set; }
        public bool LoversBane { get; set; }  // STD from Love Street

        // Character settings
        public bool AutoHeal { get; set; }  // Auto-heal in battle
        public CombatSpeed CombatSpeed { get; set; } = CombatSpeed.Normal;  // Combat text speed
        public int Loyalty { get; set; }    // Loyalty percentage (0-100)
        public int Haunt { get; set; }      // How many demons haunt player
        public char Master { get; set; }    // Level master player uses
        public bool WellWish { get; set; }  // Has visited wishing well

        // Physical appearance
        public int Height { get; set; }
        public int Weight { get; set; }
        public int Eyes { get; set; }
        public int Hair { get; set; }
        public int Skin { get; set; }

        // Character flavor text
        public List<string> Phrases { get; set; } = new();      // Combat phrases (6 phrases)
        public List<string> Description { get; set; } = new();  // Character description (4 lines)
        
        // Relationships
        public Dictionary<string, float> Relationships { get; set; } = new();

        // Romance Tracker Data
        public RomanceTrackerData RomanceData { get; set; } = new();

        // Quests
        public List<QuestData> ActiveQuests { get; set; } = new();
        
        // Achievements
        public Dictionary<string, bool> Achievements { get; set; } = new();

        // Learned combat abilities (non-caster classes)
        public List<string> LearnedAbilities { get; set; } = new();

        // Training system
        public int Trains { get; set; }  // Training sessions available
        public int TrainingPoints { get; set; }
        public Dictionary<string, int> SkillProficiencies { get; set; } = new();  // Skill name -> proficiency level
        public Dictionary<string, int> SkillTrainingProgress { get; set; } = new();  // Skill name -> progress

        // Spells array: [spellIndex][0=known, 1=mastered]
        public List<List<bool>> Spells { get; set; } = new();

        // Combat skills array
        public List<int> Skills { get; set; } = new();

        // Legacy equipment slots (for backwards compatibility)
        public int LHand { get; set; }
        public int RHand { get; set; }
        public int Head { get; set; }
        public int Body { get; set; }
        public int Arms { get; set; }
        public int LFinger { get; set; }
        public int RFinger { get; set; }
        public int Legs { get; set; }
        public int Feet { get; set; }
        public int Waist { get; set; }
        public int Neck { get; set; }
        public int Neck2 { get; set; }
        public int Face { get; set; }
        public int Shield { get; set; }
        public int Hands { get; set; }
        public int ABody { get; set; }

        // Combat flags
        public bool Immortal { get; set; }
        public string BattleCry { get; set; } = "";
        public int BGuardNr { get; set; }  // Number of door guards

        // Kill statistics
        public int MKills { get; set; }   // Monster kills
        public int MDefeats { get; set; } // Monster defeats
        public int PKills { get; set; }   // Player kills
        public int PDefeats { get; set; } // Player defeats

        // Timestamps
        public DateTime LastLogin { get; set; }
        public DateTime AccountCreated { get; set; }

        // Note: Gym removed - these fields kept for save compatibility but unused
        public DateTime LastStrengthTraining { get; set; }
        public DateTime LastDexterityTraining { get; set; }
        public DateTime LastTugOfWar { get; set; }
        public DateTime LastWrestling { get; set; }

        // Player statistics
        public PlayerStatistics? Statistics { get; set; }

        // Player achievements
        public PlayerAchievementsData? AchievementsData { get; set; }
    }

    /// <summary>
    /// Player achievements data for save system
    /// </summary>
    public class PlayerAchievementsData
    {
        public HashSet<string> UnlockedAchievements { get; set; } = new();
        public Dictionary<string, DateTime> UnlockDates { get; set; } = new();
    }

    /// <summary>
    /// NPC data for save system
    /// </summary>
    public class NPCData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public long HP { get; set; }
        public long MaxHP { get; set; }
        public string Location { get; set; } = "";
        public long Gold { get; set; }
        public int[] Items { get; set; } = new int[0];

        // Character stats
        public long Experience { get; set; }
        public long Strength { get; set; }
        public long Defence { get; set; }
        public long Agility { get; set; }
        public long Dexterity { get; set; }
        public long Mana { get; set; }
        public long MaxMana { get; set; }
        public long WeapPow { get; set; }
        public long ArmPow { get; set; }

        // Class and race
        public CharacterClass Class { get; set; }
        public CharacterRace Race { get; set; }
        public char Sex { get; set; }

        // Team and political status
        public string Team { get; set; } = "";
        public bool IsTeamLeader { get; set; }
        public bool IsKing { get; set; }

        // Alignment
        public long Chivalry { get; set; }
        public long Darkness { get; set; }

        // AI state
        public PersonalityData? PersonalityProfile { get; set; }
        public List<MemoryData> Memories { get; set; } = new();
        public List<GoalData> CurrentGoals { get; set; } = new();
        public EmotionalStateData? EmotionalState { get; set; }

        // Relationships
        public Dictionary<string, float> Relationships { get; set; } = new();

        // Marketplace inventory - items NPC has to sell
        public List<MarketItemData> MarketInventory { get; set; } = new();
    }

    /// <summary>
    /// Marketplace item data for serialization
    /// </summary>
    public class MarketItemData
    {
        public string ItemName { get; set; } = "";
        public long ItemValue { get; set; }
        public global::ObjType ItemType { get; set; }
        public int Attack { get; set; }
        public int Armor { get; set; }
        public int Strength { get; set; }
        public int Defence { get; set; }
        public bool IsCursed { get; set; }
    }

    /// <summary>
    /// Player inventory item data for serialization (dungeon loot, etc.)
    /// </summary>
    public class InventoryItemData
    {
        public string Name { get; set; } = "";
        public long Value { get; set; }
        public global::ObjType Type { get; set; }
        public int Attack { get; set; }
        public int Armor { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Wisdom { get; set; }
        public int Defence { get; set; }
        public int HP { get; set; }
        public int Mana { get; set; }
        public int Charisma { get; set; }
        public int MinLevel { get; set; }
        public bool IsCursed { get; set; }
        // Cursed is now an alias for IsCursed for backwards compatibility
        public bool Cursed
        {
            get => IsCursed;
            set => IsCursed = value;
        }
        public bool Shop { get; set; }
        public bool Dungeon { get; set; }
        public List<string> Description { get; set; } = new();
    }

    /// <summary>
    /// Marketplace listing data for persistence
    /// </summary>
    public class MarketListingData
    {
        public MarketItemData Item { get; set; } = new();
        public string Seller { get; set; } = "";
        public bool IsNPCSeller { get; set; }
        public string SellerNPCId { get; set; } = "";
        public long Price { get; set; }
        public DateTime Posted { get; set; }
    }

    /// <summary>
    /// World state data
    /// </summary>
    public class WorldStateData
    {
        // Economic state
        public int BankInterestRate { get; set; }
        public int TownPotValue { get; set; }

        // Political state
        public string? CurrentRuler { get; set; }

        // World events
        public List<WorldEventData> ActiveEvents { get; set; } = new();

        // Active quests
        public List<QuestData> ActiveQuests { get; set; } = new();

        // Shop inventories
        public Dictionary<string, ShopInventoryData> ShopInventories { get; set; } = new();

        // News and history
        public List<NewsEntryData> RecentNews { get; set; } = new();

        // God system state
        public Dictionary<string, GodStateData> GodStates { get; set; } = new();

        // Marketplace listings
        public List<MarketListingData> MarketplaceListings { get; set; } = new();
    }

    /// <summary>
    /// Daily cycle settings
    /// </summary>
    public class DailySettings
    {
        public DailyCycleMode Mode { get; set; }
        public DateTime LastResetTime { get; set; }
        public bool AutoSaveEnabled { get; set; }
        public TimeSpan AutoSaveInterval { get; set; }
    }

    /// <summary>
    /// Save file information for UI
    /// </summary>
    public class SaveInfo
    {
        public string PlayerName { get; set; } = "";
        public DateTime SaveTime { get; set; }
        public int Level { get; set; }
        public int CurrentDay { get; set; }
        public int TurnsRemaining { get; set; }
        public string FileName { get; set; } = "";
        public bool IsAutosave { get; set; }
        public string SaveType { get; set; } = "Manual Save";
    }

    /// <summary>
    /// Quest data for save system - matches Quest class structure
    /// </summary>
    public class QuestData
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Initiator { get; set; } = "";
        public string Comment { get; set; } = "";
        public QuestStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public int QuestType { get; set; }
        public int QuestTarget { get; set; }
        public int Difficulty { get; set; }
        public string Occupier { get; set; } = "";
        public int OccupiedDays { get; set; }
        public int DaysToComplete { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public int Reward { get; set; }
        public int RewardType { get; set; }
        public int Penalty { get; set; }
        public int PenaltyType { get; set; }
        public string OfferedTo { get; set; } = "";
        public bool Forced { get; set; }
        public List<QuestObjectiveData> Objectives { get; set; } = new();
        public List<QuestMonsterData> Monsters { get; set; } = new();
    }

    /// <summary>
    /// Quest objective data for save system
    /// </summary>
    public class QuestObjectiveData
    {
        public string Id { get; set; } = "";
        public string Description { get; set; } = "";
        public int ObjectiveType { get; set; }
        public string TargetId { get; set; } = "";
        public string TargetName { get; set; } = "";
        public int RequiredProgress { get; set; }
        public int CurrentProgress { get; set; }
        public bool IsOptional { get; set; }
        public int BonusReward { get; set; }
    }

    /// <summary>
    /// Quest monster data for save system
    /// </summary>
    public class QuestMonsterData
    {
        public int MonsterType { get; set; }
        public int Count { get; set; }
        public string MonsterName { get; set; } = "";
    }

    /// <summary>
    /// Personality data for NPCs
    /// </summary>
    public class PersonalityData
    {
        public float Aggression { get; set; }
        public float Loyalty { get; set; }
        public float Intelligence { get; set; }
        public float Greed { get; set; }
        public float Compassion { get; set; }
        public float Courage { get; set; }
        public float Honesty { get; set; }
        public float Ambition { get; set; }
    }

    /// <summary>
    /// Memory data for NPCs
    /// </summary>
    public class MemoryData
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";  // String representation of MemoryType enum
        public string Description { get; set; } = "";
        public string InvolvedCharacter { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public float Importance { get; set; }
        public float EmotionalImpact { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// Goal data for NPCs
    /// </summary>
    public class GoalData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";  // String representation of GoalType enum
        public float Priority { get; set; }
        public float Progress { get; set; }
        public bool IsActive { get; set; }
        public float TargetValue { get; set; }
        public float CurrentValue { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? CompletionTime { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Emotional state data for NPCs
    /// </summary>
    public class EmotionalStateData
    {
        public float Happiness { get; set; }
        public float Anger { get; set; }
        public float Fear { get; set; }
        public float Trust { get; set; }
    }

    /// <summary>
    /// World event data
    /// </summary>
    public class WorldEventData
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<string> AffectedLocations { get; set; } = new();
        public List<string> AffectedNPCs { get; set; } = new();
    }

    /// <summary>
    /// Shop inventory data
    /// </summary>
    public class ShopInventoryData
    {
        public string ShopId { get; set; } = "";
        public DateTime LastRestock { get; set; }
        public Dictionary<string, ShopItemData> Items { get; set; } = new();
        public float PriceModifier { get; set; } = 1.0f;
    }

    /// <summary>
    /// Shop item data
    /// </summary>
    public class ShopItemData
    {
        public string ItemId { get; set; } = "";
        public int Quantity { get; set; }
        public int Price { get; set; }
        public DateTime LastSold { get; set; }
    }

    /// <summary>
    /// News entry data
    /// </summary>
    public class NewsEntryData
    {
        public string Id { get; set; } = "";
        public string Category { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Author { get; set; } = "";
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// God state data
    /// </summary>
    public class GodStateData
    {
        public string GodId { get; set; } = "";
        public string Name { get; set; } = "";
        public long Power { get; set; }
        public int Followers { get; set; }
        public DateTime LastActivity { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new();
    }

    /// <summary>
    /// Daily cycle modes
    /// </summary>
    public enum DailyCycleMode
    {
        SessionBased,    // New day when turns depleted
        RealTime24Hour,  // Classic midnight reset
        Accelerated4Hour,
        Accelerated8Hour,
        Accelerated12Hour,
        Endless         // No turn limits
    }

    /// <summary>
    /// Quest status enumeration
    /// </summary>
    public enum QuestStatus
    {
        Active,
        Completed,
        Failed,
        Abandoned
    }

    /// <summary>
    /// Memory event types for NPCs
    /// </summary>
    public enum MemoryEventType
    {
        FirstMeeting,
        WasHelped,
        WasHarmed,
        WitnessedCombat,
        ReceivedGift,
        WasRobbed,
        SawDeath,
        SharedMeal,
        Conversation,
        Trade,
        Romance,
        Betrayal,
        Alliance,
        Rivalry
    }

    /// <summary>
    /// Goal types for NPCs
    /// </summary>
    public enum GoalType
    {
        Survival,
        Wealth,
        Power,
        Love,
        Revenge,
        Knowledge,
        Protection,
        Exploration,
        Social,
        Religious
    }

    /// <summary>
    /// Goal status enumeration
    /// </summary>
    public enum GoalStatus
    {
        Active,
        Completed,
        Failed,
        Paused,
        Abandoned
    }

    /// <summary>
    /// Romance tracker data for save system
    /// </summary>
    public class RomanceTrackerData
    {
        public List<LoverData> CurrentLovers { get; set; } = new();
        public List<SpouseData> Spouses { get; set; } = new();
        public List<string> FriendsWithBenefits { get; set; } = new();
        public List<string> Exes { get; set; } = new();
        public List<IntimateEncounterData> EncounterHistory { get; set; } = new();
        public Dictionary<string, int> JealousyLevels { get; set; } = new();
        public Dictionary<string, int> AgreedStructures { get; set; } = new(); // RelationshipStructure enum as int
        public List<CuckoldArrangementData> CuckArrangements { get; set; } = new();
        public List<PolyNetworkData> PolyNetworks { get; set; } = new();
        public List<ConversationStateData> ConversationStates { get; set; } = new(); // Flirt progress per NPC
    }

    /// <summary>
    /// Lover data for save system
    /// </summary>
    public class LoverData
    {
        public string NPCId { get; set; } = "";
        public string NPCName { get; set; } = "";
        public int LoveLevel { get; set; }
        public bool IsExclusive { get; set; }
        public bool KnowsAboutOthers { get; set; }
        public List<string> MetamorsList { get; set; } = new();
        public DateTime RelationshipStart { get; set; }
        public DateTime? LastIntimateDate { get; set; }
    }

    /// <summary>
    /// Spouse data for save system
    /// </summary>
    public class SpouseData
    {
        public string NPCId { get; set; } = "";
        public string NPCName { get; set; } = "";
        public DateTime MarriedDate { get; set; }
        public int LoveLevel { get; set; }
        public bool AcceptsPolyamory { get; set; }
        public bool KnowsAboutOthers { get; set; }
        public int Children { get; set; }
        public DateTime? LastIntimateDate { get; set; }
    }

    /// <summary>
    /// Intimate encounter data for save system
    /// </summary>
    public class IntimateEncounterData
    {
        public DateTime Date { get; set; }
        public string Location { get; set; } = "";
        public List<string> PartnerIds { get; set; } = new();
        public List<string> PartnerNames { get; set; } = new();
        public int Type { get; set; } // EncounterType enum as int
        public int Mood { get; set; } // IntimacyMood enum as int
        public bool IsFirstTime { get; set; }
        public List<string> WatcherIds { get; set; } = new();
        public string Notes { get; set; } = "";
    }

    /// <summary>
    /// Cuckold arrangement data for save system
    /// </summary>
    public class CuckoldArrangementData
    {
        public string PrimaryPartnerId { get; set; } = "";
        public string PrimaryPartnerName { get; set; } = "";
        public string ThirdPartyId { get; set; } = "";
        public string ThirdPartyName { get; set; } = "";
        public bool PlayerIsWatching { get; set; }
        public DateTime ConsentedDate { get; set; }
        public int EncounterCount { get; set; }
    }

    /// <summary>
    /// Poly network data for save system
    /// </summary>
    public class PolyNetworkData
    {
        public string NetworkName { get; set; } = "";
        public List<string> MemberIds { get; set; } = new();
        public List<string> MemberNames { get; set; } = new();
        public DateTime EstablishedDate { get; set; }
    }

    /// <summary>
    /// Child data for save system
    /// </summary>
    public class ChildData
    {
        public string Name { get; set; } = "";
        public string Mother { get; set; } = "";
        public string Father { get; set; } = "";
        public string MotherID { get; set; } = "";
        public string FatherID { get; set; } = "";
        public string OriginalMother { get; set; } = "";
        public string OriginalFather { get; set; } = "";
        public int Sex { get; set; } // CharacterSex enum as int
        public int Age { get; set; }
        public DateTime BirthDate { get; set; }
        public bool Named { get; set; }
        public int Location { get; set; }
        public int Health { get; set; }
        public int Soul { get; set; }
        public bool MotherAccess { get; set; }
        public bool FatherAccess { get; set; }
        public bool Kidnapped { get; set; }
        public string KidnapperName { get; set; } = "";
        public long RansomDemanded { get; set; }
        public string CursedByGod { get; set; } = "";
        public int Royal { get; set; }
    }

    /// <summary>
    /// Conversation state data for save system (tracks flirt progress per NPC)
    /// </summary>
    public class ConversationStateData
    {
        public string NPCId { get; set; } = "";
        public int FlirtSuccessCount { get; set; }
        public bool LastFlirtWasPositive { get; set; }
        public int TotalConversations { get; set; }
        public int PersonalQuestionsAsked { get; set; }
        public bool HasConfessed { get; set; }
        public bool ConfessionAccepted { get; set; }
        public List<string> TopicsDiscussed { get; set; } = new();
        public DateTime LastConversationDate { get; set; }
    }
} 