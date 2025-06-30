using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Quest Record - Pascal-compatible quest structure based on QuestRec
/// </summary>
public class Quest
{
    // Primary Quest Data (Pascal QuestRec fields)
    public string Id { get; set; } = "";
    public string Initiator { get; set; } = "";
    public DateTime Date { get; set; }
    public QuestType QuestType { get; set; }
    public QuestTarget QuestTarget { get; set; }
    public byte Difficulty { get; set; }
    public bool Deleted { get; set; } = false;
    public string Comment { get; set; } = "";
    
    // Quest Occupancy
    public string Occupier { get; set; } = "";
    public CharacterRace OccupierRace { get; set; }
    public byte OccupierSex { get; set; }
    public int OccupiedDays { get; set; } = 0;
    public int DaysToComplete { get; set; }
    
    // Quest Requirements
    public int MinLevel { get; set; } = 1;
    public int MaxLevel { get; set; } = 9999;
    
    // Quest Rewards
    public byte Reward { get; set; } = 0;
    public QuestRewardType RewardType { get; set; }
    
    // Quest Monsters
    public List<QuestMonster> Monsters { get; set; } = new();
    
    // Status Properties
    public bool IsActive => !Deleted && !string.IsNullOrEmpty(Occupier);
    public bool IsAvailable => !Deleted && string.IsNullOrEmpty(Occupier);
    public int DaysRemaining => Math.Max(0, DaysToComplete - OccupiedDays);
    
    public Quest()
    {
        Id = $"Q{DateTime.Now:yyyyMMddHHmmss}{GD.Randi() % 1000:D3}";
        Date = DateTime.Now;
        DaysToComplete = 7; // Default 7 days
        Monsters = new List<QuestMonster>();
    }
    
    /// <summary>
    /// Get quest difficulty description
    /// </summary>
    public string GetDifficultyString()
    {
        return Difficulty switch
        {
            1 => "Easy",
            2 => "Medium", 
            3 => "Hard",
            4 => "Extreme",
            _ => "Unknown"
        };
    }
    
    /// <summary>
    /// Get quest target description
    /// </summary>
    public string GetTargetDescription()
    {
        return QuestTarget switch
        {
            QuestTarget.Monster => "Slay Monsters",
            QuestTarget.Assassin => "Assassination Mission",
            QuestTarget.Seduce => "Seduction Mission",
            QuestTarget.ClaimTown => "Claim Territory",
            QuestTarget.GangWar => "Gang War Participation",
            _ => "Unknown Mission"
        };
    }
    
    /// <summary>
    /// Calculate reward amount based on player level
    /// </summary>
    public long CalculateReward(int playerLevel)
    {
        if (Reward == 0) return 0;
        
        return RewardType switch
        {
            QuestRewardType.Experience => Reward switch
            {
                1 => playerLevel * 100,   // Low exp
                2 => playerLevel * 500,   // Medium exp
                3 => playerLevel * 1000,  // High exp
                _ => 0
            },
            QuestRewardType.Money => Reward switch
            {
                1 => playerLevel * 1100,  // Low gold
                2 => playerLevel * 5100,  // Medium gold
                3 => playerLevel * 11000, // High gold
                _ => 0
            },
            QuestRewardType.Potions => Reward switch
            {
                1 => 50,   // Low potions
                2 => 100,  // Medium potions
                3 => 200,  // High potions
                _ => 0
            },
            QuestRewardType.Darkness or QuestRewardType.Chivalry => Reward switch
            {
                1 => 25,   // Low points
                2 => 75,   // Medium points
                3 => 110,  // High points
                _ => 0
            },
            _ => 0
        };
    }
    
    /// <summary>
    /// Get full quest display information (Pascal quest display)
    /// </summary>
    public string GetDisplayInfo()
    {
        var status = IsActive ? $"Claimed by {Occupier}" : "Available";
        var timeInfo = IsActive ? $"{DaysRemaining} days left" : $"{DaysToComplete} days to complete";
        
        return $"{GetTargetDescription()} | {GetDifficultyString()} | {GetRewardDescription()} | {status} | {timeInfo}";
    }
}

/// <summary>
/// Quest Monster data
/// </summary>
public class QuestMonster
{
    public int MonsterType { get; set; }
    public int Count { get; set; }
    public string MonsterName { get; set; } = "";
    
    public QuestMonster(int type, int count, string name = "")
    {
        MonsterType = type;
        Count = count;
        MonsterName = name;
    }
}

/// <summary>
/// Quest Types - Pascal QuestTypes enumeration
/// </summary>
public enum QuestType
{
    SingleQuest = 0,
    TeamQuest = 1
}

/// <summary>
/// Quest Targets - Pascal QuestTargets enumeration  
/// </summary>
public enum QuestTarget
{
    Monster = 0,
    Assassin = 1,
    Seduce = 2,
    ClaimTown = 3,
    GangWar = 4
}

/// <summary>
/// Quest Reward Types - Pascal QRewardTypes enumeration
/// </summary>
public enum QuestRewardType
{
    Nothing = 0,
    Experience = 1,
    Money = 2,
    Potions = 3,
    Darkness = 4,
    Chivalry = 5
}

/// <summary>
/// Quest Claim Results - Validation results for quest claiming
/// </summary>
public enum QuestClaimResult
{
    CanClaim = 0,                                                // Player can claim quest
    QuestDeleted = 1,                                            // Quest no longer exists
    AlreadyClaimed = 2,                                          // Quest already claimed
    RoyalsNotAllowed = 3,                                        // Royals cannot take quests
    OwnQuest = 4,                                                // Cannot take own quest
    LevelTooLow = 5,                                             // Player level too low
    LevelTooHigh = 6,                                            // Player level too high
    DailyLimitReached = 7                                        // Daily quest limit reached
} 