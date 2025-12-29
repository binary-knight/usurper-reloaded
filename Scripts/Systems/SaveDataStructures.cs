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
        
        // Game state
        public string CurrentLocation { get; set; } = "";
        public int TurnsRemaining { get; set; }
        public int DaysInPrison { get; set; }
        
        // Daily limits
        public int Fights { get; set; }
        public int PFights { get; set; }
        public int TFights { get; set; }
        public int Thiefs { get; set; }
        public int Brawls { get; set; }
        public int Assa { get; set; }
        
        // Items and equipment
        public int[] Items { get; set; } = new int[0];
        public int[] ItemTypes { get; set; } = new int[0];
        
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
        public int Mercy { get; set; }   // Mercy counter

        // Disease status
        public bool Blind { get; set; }
        public bool Plague { get; set; }
        public bool Smallpox { get; set; }
        public bool Measles { get; set; }
        public bool Leprosy { get; set; }

        // Character settings
        public bool AutoHeal { get; set; }  // Auto-heal in battle
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
        
        // Quests
        public List<QuestData> ActiveQuests { get; set; } = new();
        
        // Achievements
        public Dictionary<string, bool> Achievements { get; set; } = new();
        
        // Timestamps
        public DateTime LastLogin { get; set; }
        public DateTime AccountCreated { get; set; }
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
        
        // AI state
        public PersonalityData? PersonalityProfile { get; set; }
        public List<MemoryData> Memories { get; set; } = new();
        public List<GoalData> CurrentGoals { get; set; } = new();
        public EmotionalStateData? EmotionalState { get; set; }
        
        // Relationships
        public Dictionary<string, float> Relationships { get; set; } = new();
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
        
        // Shop inventories
        public Dictionary<string, ShopInventoryData> ShopInventories { get; set; } = new();
        
        // News and history
        public List<NewsEntryData> RecentNews { get; set; } = new();
        
        // God system state
        public Dictionary<string, GodStateData> GodStates { get; set; } = new();
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
    /// Quest data for save system
    /// </summary>
    public class QuestData
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public QuestStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? CompletionTime { get; set; }
        public Dictionary<string, object> Progress { get; set; } = new();
        public List<string> Objectives { get; set; } = new();
        public Dictionary<string, bool> CompletedObjectives { get; set; } = new();
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
        public MemoryEventType Type { get; set; }
        public string InvolvedCharacter { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public float Importance { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// Goal data for NPCs
    /// </summary>
    public class GoalData
    {
        public string Id { get; set; } = "";
        public GoalType Type { get; set; }
        public float Priority { get; set; }
        public GoalStatus Status { get; set; }
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
} 