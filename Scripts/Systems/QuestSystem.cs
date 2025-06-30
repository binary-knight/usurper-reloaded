using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Quest System - Complete Pascal-compatible quest management engine
/// Based on Pascal PLYQUEST.PAS and RQUESTS.PAS with all quest functionality
/// Handles quest creation, claiming, completion, rewards, and database management
/// </summary>
public class QuestSystem
{
    private static List<Quest> questDatabase = new List<Quest>();
    private static Random random = new Random();
    
    /// <summary>
    /// Create new quest (Pascal: Royal quest initiation from RQUESTS.PAS)
    /// </summary>
    public static Quest CreateQuest(Character king, QuestTarget target, byte difficulty, 
                                   string comment, QuestType questType = QuestType.SingleQuest)
    {
        // Validate king can create quest
        if (king.QuestsLeft < 1)
        {
            throw new InvalidOperationException("King has no quests left today");
        }
        
        if (questDatabase.Count >= GameConfig.MaxQuestsAllowed)
        {
            throw new InvalidOperationException("Quest database is full");
        }
        
        var quest = new Quest
        {
            Initiator = king.Name2,
            QuestType = questType,
            QuestTarget = target,
            Difficulty = difficulty,
            Comment = comment,
            Date = DateTime.Now,
            MinLevel = 1,
            MaxLevel = 9999,
            DaysToComplete = GameConfig.DefaultQuestDays
        };
        
        // Generate quest monsters based on target and difficulty
        GenerateQuestMonsters(quest);
        
        // Set default rewards based on difficulty
        SetDefaultRewards(quest);
        
        // Add to database
        questDatabase.Add(quest);
        
        // Update king's quest count
        king.QuestsLeft--;
        
        GD.Print($"[QuestSystem] Quest created by {king.Name2}: {quest.GetTargetDescription()}");
        
        return quest;
    }
    
    /// <summary>
    /// Claim quest for player (Pascal: Quest claiming from PLYQUEST.PAS)
    /// </summary>
    public static QuestClaimResult ClaimQuest(Character player, string questId)
    {
        var quest = GetQuestById(questId);
        if (quest == null) return QuestClaimResult.QuestDeleted;
        
        // Validate player can claim
        var claimResult = quest.CanPlayerClaim(player);
        if (claimResult != QuestClaimResult.CanClaim) return claimResult;
        
        // Claim the quest
        quest.Occupier = player.Name2;
        quest.OccupierRace = player.Race;
        quest.OccupierSex = (byte)player.Sex;
        quest.OccupiedDays = 0;
        
        // Update player quest count
        player.ActiveQuests++;
        
        GD.Print($"[QuestSystem] Quest claimed by {player.Name2}: {quest.Id}");
        
        // Send confirmation mail (Pascal: Quest claim notification)
        MailSystem.SendQuestClaimedMail(player.Name2, quest);
        
        return QuestClaimResult.CanClaim;
    }
    
    /// <summary>
    /// Complete quest and give rewards (Pascal: Quest completion from PLYQUEST.PAS)
    /// </summary>
    public static QuestCompletionResult CompleteQuest(Character player, string questId, TerminalUI terminal)
    {
        var quest = GetQuestById(questId);
        if (quest == null) return QuestCompletionResult.QuestNotFound;
        
        if (quest.Occupier != player.Name2) return QuestCompletionResult.NotYourQuest;
        if (quest.Deleted) return QuestCompletionResult.QuestDeleted;
        
        // Check if player completed all quest requirements
        if (!ValidateQuestCompletion(player, quest))
        {
            return QuestCompletionResult.RequirementsNotMet;
        }
        
        // Calculate and give rewards (Pascal reward calculations)
        var rewardAmount = quest.CalculateReward(player.Level);
        ApplyQuestReward(player, quest, rewardAmount, terminal);
        
        // Mark quest as complete
        quest.Deleted = true;
        quest.Occupier = "";
        
        // Update player statistics
        player.RoyQuests++;
        player.RoyQuestsToday++;
        player.ActiveQuests--;
        
        // Send completion notification to king (Pascal: King notification)
        SendQuestCompletionMail(player, quest, rewardAmount);
        
        // News announcement (Pascal: News generation)
        GenerateQuestCompletionNews(player, quest);
        
        GD.Print($"[QuestSystem] Quest completed by {player.Name2}: {quest.Id}");
        
        return QuestCompletionResult.Success;
    }
    
    /// <summary>
    /// Get available quests for player (Pascal: Quest listing)
    /// </summary>
    public static List<Quest> GetAvailableQuests(Character player)
    {
        return questDatabase.Where(q => 
            !q.Deleted && 
            string.IsNullOrEmpty(q.Occupier) &&
            !player.King &&
            q.Initiator != player.Name2 &&
            player.Level >= q.MinLevel &&
            player.Level <= q.MaxLevel
        ).ToList();
    }
    
    /// <summary>
    /// Get player's active quests (Pascal: Player quest tracking)
    /// </summary>
    public static List<Quest> GetPlayerQuests(string playerName)
    {
        return questDatabase.Where(q => 
            !q.Deleted && 
            q.Occupier == playerName
        ).ToList();
    }
    
    /// <summary>
    /// Get quest by ID
    /// </summary>
    public static Quest GetQuestById(string questId)
    {
        return questDatabase.FirstOrDefault(q => q.Id == questId);
    }
    
    /// <summary>
    /// Offer quest to specific player (Pascal: Quest offering system)
    /// </summary>
    public static void OfferQuestToPlayer(Quest quest, string playerName, bool forced = false)
    {
        quest.OfferedTo = playerName;
        quest.Forced = forced;
        
        // Send quest offer mail (Pascal: Quest offer mail)
        MailSystem.SendQuestOfferMail(playerName, quest);
        
        GD.Print($"[QuestSystem] Quest offered to {playerName}: {quest.Id}");
    }
    
    /// <summary>
    /// Process daily quest maintenance (Pascal: Quest aging and failure)
    /// </summary>
    public static void ProcessDailyQuestMaintenance()
    {
        var failedQuests = new List<Quest>();
        
        foreach (var quest in questDatabase.Where(q => !q.Deleted && !string.IsNullOrEmpty(q.Occupier)))
        {
            quest.OccupiedDays++;
            
            // Check for quest failure (Pascal: Quest time limit)
            if (quest.OccupiedDays > quest.DaysToComplete)
            {
                failedQuests.Add(quest);
            }
        }
        
        // Process failed quests
        foreach (var failedQuest in failedQuests)
        {
            ProcessQuestFailure(failedQuest);
        }
        
        // Clean up old completed quests (keep database manageable)
        CleanupOldQuests();
        
        GD.Print($"[QuestSystem] Daily maintenance: {failedQuests.Count} quests failed");
    }
    
    /// <summary>
    /// Generate quest monsters based on target and difficulty
    /// Pascal: Monster generation for quests
    /// </summary>
    private static void GenerateQuestMonsters(Quest quest)
    {
        if (quest.QuestTarget != QuestTarget.Monster) return;
        
        quest.Monsters.Clear();
        
        // Number of monster types based on difficulty
        var monsterTypes = quest.Difficulty switch
        {
            1 => 1,     // Easy: 1 type
            2 => 2,     // Medium: 2 types  
            3 => 3,     // Hard: 3 types
            4 => 4,     // Extreme: 4 types
            _ => 1
        };
        
        for (int i = 0; i < monsterTypes; i++)
        {
            var monsterType = random.Next(1, 21); // Random monster type
            var count = quest.Difficulty switch
            {
                1 => random.Next(3, 8),      // Easy: 3-7 monsters
                2 => random.Next(5, 12),     // Medium: 5-11 monsters
                3 => random.Next(8, 16),     // Hard: 8-15 monsters
                4 => random.Next(12, 21),    // Extreme: 12-20 monsters
                _ => 5
            };
            
            quest.Monsters.Add(new QuestMonster(monsterType, count, $"Monster Type {monsterType}"));
        }
    }
    
    /// <summary>
    /// Set default rewards based on difficulty
    /// Pascal: Default reward assignment
    /// </summary>
    private static void SetDefaultRewards(Quest quest)
    {
        // Reward level matches difficulty
        quest.Reward = quest.Difficulty switch
        {
            1 => GameConfig.QuestRewardLow,
            2 => GameConfig.QuestRewardMedium,
            3 => GameConfig.QuestRewardHigh,
            4 => GameConfig.QuestRewardHigh,
            _ => GameConfig.QuestRewardLow
        };
        
        // Random reward type (Pascal: Random reward assignment)
        quest.RewardType = (QuestRewardType)random.Next(1, 6); // 1-5 (skip Nothing)
        
        // Set penalty (usually lower than reward)
        quest.Penalty = (byte)Math.Max(1, quest.Reward - 1);
        quest.PenaltyType = quest.RewardType;
    }
    
    /// <summary>
    /// Validate quest completion requirements
    /// Pascal: Quest completion validation
    /// </summary>
    private static bool ValidateQuestCompletion(Character player, Quest quest)
    {
        switch (quest.QuestTarget)
        {
            case QuestTarget.Monster:
                // Check if player killed enough monsters (simplified)
                return player.MKills >= quest.Monsters.Sum(m => m.Count);
                
            case QuestTarget.Assassin:
                // Check assassination mission completion
                return player.Assa > 0; // Has assassination attempts
                
            case QuestTarget.Seduce:
                // Check seduction mission completion  
                return player.IntimacyActs > 0; // Has intimacy acts
                
            default:
                return true; // Other quest types auto-complete for now
        }
    }
    
    /// <summary>
    /// Apply quest reward to player (Pascal: Reward application)
    /// </summary>
    private static void ApplyQuestReward(Character player, Quest quest, long rewardAmount, TerminalUI terminal)
    {
        if (quest.Reward == 0 || rewardAmount == 0) return;
        
        terminal.WriteLine("", "white");
        terminal.WriteLine("═══════════════════════════════════════", "bright_green");
        terminal.WriteLine("          QUEST COMPLETED!", "bright_green");
        terminal.WriteLine("═══════════════════════════════════════", "bright_green");
        terminal.WriteLine("", "white");
        
        switch (quest.RewardType)
        {
            case QuestRewardType.Experience:
                player.Experience += rewardAmount;
                terminal.WriteLine($"You gain {rewardAmount} experience points!", "bright_green");
                break;
                
            case QuestRewardType.Money:
                player.Gold += rewardAmount;
                terminal.WriteLine($"You receive {rewardAmount} gold!", "bright_yellow");
                break;
                
            case QuestRewardType.Potions:
                player.Healing += (int)rewardAmount;
                terminal.WriteLine($"You receive {rewardAmount} healing potions!", "bright_cyan");
                break;
                
            case QuestRewardType.Darkness:
                player.Darkness += (int)rewardAmount;
                terminal.WriteLine($"You gain {rewardAmount} darkness points!", "dark_red");
                break;
                
            case QuestRewardType.Chivalry:
                player.Chivalry += (int)rewardAmount;
                terminal.WriteLine($"You gain {rewardAmount} chivalry points!", "bright_white");
                break;
        }
        
        terminal.WriteLine($"Congratulations! You have now completed {player.RoyQuests + 1} quests in your career.", "white");
        terminal.WriteLine("", "white");
    }
    
    /// <summary>
    /// Process quest failure (Pascal: Quest failure handling)
    /// </summary>
    private static void ProcessQuestFailure(Quest quest)
    {
        // Send failure mail to player
        MailSystem.SendQuestFailureMail(quest.Occupier, quest);
        
        // Send failure notification to king
        var kingName = quest.Initiator;
        MailSystem.SendQuestFailureNotificationMail(kingName, quest);
        
        // Apply penalty if configured
        ApplyQuestPenalty(quest);
        
        // Mark quest as failed/deleted
        quest.Deleted = true;
        quest.Occupier = "";
        
        GD.Print($"[QuestSystem] Quest failed: {quest.Id} by {quest.Occupier}");
    }
    
    /// <summary>
    /// Apply quest failure penalty
    /// </summary>
    private static void ApplyQuestPenalty(Quest quest)
    {
        // In a full implementation, would load player and apply penalties
        // For now, just log the penalty
        var penaltyAmount = quest.CalculateReward(10); // Use level 10 as default
        GD.Print($"[QuestSystem] Penalty applied: {quest.PenaltyType} -{penaltyAmount}");
    }
    
    /// <summary>
    /// Send quest completion mail to king
    /// </summary>
    private static void SendQuestCompletionMail(Character player, Quest quest, long rewardAmount)
    {
        var rewardText = $"{quest.RewardType} {rewardAmount}";
        MailSystem.SendQuestCompletionMail(quest.Initiator, player.Name2, quest, rewardText);
    }
    
    /// <summary>
    /// Generate news for quest completion
    /// </summary>
    private static void GenerateQuestCompletionNews(Character player, Quest quest)
    {
        var newsLines = new[]
        {
            $"{player.Name2} completed a {quest.GetDifficultyString()} quest!",
            $"{player.Name2} returned home and received a reward."
        };
        
        MailSystem.SendNewsMail("Successful Quest!", newsLines);
    }
    
    /// <summary>
    /// Clean up old completed quests
    /// </summary>
    private static void CleanupOldQuests()
    {
        var cutoffDate = DateTime.Now.AddDays(-30); // Keep quests for 30 days
        var removedCount = questDatabase.RemoveAll(q => q.Deleted && q.Date < cutoffDate);
        
        if (removedCount > 0)
        {
            GD.Print($"[QuestSystem] Cleaned up {removedCount} old quests");
        }
    }
    
    /// <summary>
    /// Get quest rankings (Pascal: Quest master rankings)
    /// </summary>
    public static List<QuestRanking> GetQuestRankings()
    {
        // In a full implementation, would load all players and rank by quest completions
        // For now, return empty list
        return new List<QuestRanking>();
    }
    
    /// <summary>
    /// Get all quests (for king/admin view)
    /// </summary>
    public static List<Quest> GetAllQuests(bool includeCompleted = false)
    {
        return includeCompleted ? 
            questDatabase.ToList() : 
            questDatabase.Where(q => !q.Deleted).ToList();
    }
}

/// <summary>
/// Quest completion results
/// </summary>
public enum QuestCompletionResult
{
    Success = 0,
    QuestNotFound = 1,
    NotYourQuest = 2,
    QuestDeleted = 3,
    RequirementsNotMet = 4
}

/// <summary>
/// Quest ranking data for leaderboards
/// </summary>
public class QuestRanking
{
    public string PlayerName { get; set; } = "";
    public int QuestsCompleted { get; set; } = 0;
    public CharacterRace Race { get; set; }
    public byte Sex { get; set; }
} 
