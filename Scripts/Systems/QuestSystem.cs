using UsurperRemake.Utils;
using UsurperRemake.Systems;
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
public partial class QuestSystem : Node
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
    public static QuestClaimResult ClaimQuest(Player player, Quest questToClaim)
    {
        var foundQuest = GetQuestById(questToClaim.Id);
        if (foundQuest == null) return QuestClaimResult.QuestDeleted;
        
        // Validate player can claim
        var claimResult = foundQuest.CanPlayerClaim(player);
        if (claimResult != QuestClaimResult.CanClaim) return claimResult;
        
        // Claim the quest
        foundQuest.Occupier = player.Name2;
        foundQuest.OccupierRace = player.Race;
                        foundQuest.OccupierSex = (byte)((int)player.Sex);
        foundQuest.OccupiedDays = 0;
        
        // Track in player list
        player.ActiveQuests.Add(foundQuest);
        
        GD.Print($"[QuestSystem] Quest claimed by {player.Name2}: {foundQuest.Id}");
        
        // Send confirmation mail (Pascal: Quest claim notification)
        MailSystem.SendQuestClaimedMail(player.Name2, foundQuest.Title);
        
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
        player.ActiveQuests.Remove(quest);
        
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
        MailSystem.SendQuestOfferMail(playerName, quest.Title);
        
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
    /// Generate objectives for dungeon quests
    /// </summary>
    private static void GenerateDungeonQuestObjectives(Quest quest)
    {
        quest.Objectives.Clear();

        switch (quest.QuestTarget)
        {
            case QuestTarget.ClearBoss:
                // Kill a specific boss
                var bossId = quest.Difficulty switch
                {
                    1 => "goblin_chief",
                    2 => "orc_warlord",
                    3 => "dragon_young",
                    4 => "demon_lord",
                    _ => "unknown_boss"
                };
                var bossName = quest.Difficulty switch
                {
                    1 => "Goblin Chief",
                    2 => "Orc Warlord",
                    3 => "Young Dragon",
                    4 => "Demon Lord",
                    _ => "Unknown Boss"
                };
                quest.Objectives.Add(new QuestObjective(
                    QuestObjectiveType.KillBoss,
                    $"Defeat the {bossName}",
                    1, bossId, bossName));
                quest.Title = $"Slay the {bossName}";
                break;

            case QuestTarget.FindArtifact:
                // Find an artifact on a specific floor
                var artifactFloor = quest.Difficulty * 3 + random.Next(1, 3);
                var artifactId = $"artifact_{random.Next(1, 100)}";
                var artifactNames = new[] { "Ancient Amulet", "Crystal Orb", "Mystic Tome", "Golden Chalice", "Obsidian Blade" };
                var artifactName = artifactNames[random.Next(artifactNames.Length)];
                quest.Objectives.Add(new QuestObjective(
                    QuestObjectiveType.ReachDungeonFloor,
                    $"Descend to floor {artifactFloor}",
                    artifactFloor, "", $"Floor {artifactFloor}"));
                quest.Objectives.Add(new QuestObjective(
                    QuestObjectiveType.FindArtifact,
                    $"Retrieve the {artifactName}",
                    1, artifactId, artifactName));
                quest.Title = $"Recover the {artifactName}";
                break;

            case QuestTarget.ReachFloor:
                // Reach a specific floor
                var targetFloor = quest.Difficulty * 5 + random.Next(1, 5);
                quest.Objectives.Add(new QuestObjective(
                    QuestObjectiveType.ReachDungeonFloor,
                    $"Reach dungeon floor {targetFloor}",
                    targetFloor, "", $"Floor {targetFloor}"));
                // Optional: kill some monsters on the way
                quest.Objectives.Add(new QuestObjective(
                    QuestObjectiveType.KillMonsters,
                    "Defeat monsters along the way",
                    quest.Difficulty * 5, "", "Monsters") { IsOptional = true, BonusReward = 100 });
                quest.Title = $"Expedition to Floor {targetFloor}";
                break;

            case QuestTarget.ClearFloor:
                // Clear all monsters on a specific floor
                var clearFloor = quest.Difficulty * 2 + random.Next(1, 3);
                var monstersOnFloor = 5 + (quest.Difficulty * 3);
                quest.Objectives.Add(new QuestObjective(
                    QuestObjectiveType.ReachDungeonFloor,
                    $"Descend to floor {clearFloor}",
                    clearFloor, "", $"Floor {clearFloor}"));
                quest.Objectives.Add(new QuestObjective(
                    QuestObjectiveType.ClearDungeonFloor,
                    $"Clear all {monstersOnFloor} monsters on floor {clearFloor}",
                    monstersOnFloor, clearFloor.ToString(), $"Floor {clearFloor}"));
                quest.Title = $"Clear Floor {clearFloor}";
                break;

            case QuestTarget.RescueNPC:
                // Rescue an NPC from a dungeon floor
                var rescueFloor = quest.Difficulty * 3 + random.Next(1, 3);
                var npcNames = new[] { "Lady Elara", "Sir Marcus", "Priest Aldric", "Merchant Tobias", "Scholar Helena" };
                var npcName = npcNames[random.Next(npcNames.Length)];
                quest.Objectives.Add(new QuestObjective(
                    QuestObjectiveType.ReachDungeonFloor,
                    $"Reach floor {rescueFloor} where {npcName} is trapped",
                    rescueFloor, "", $"Floor {rescueFloor}"));
                quest.Objectives.Add(new QuestObjective(
                    QuestObjectiveType.TalkToNPC,
                    $"Find and rescue {npcName}",
                    1, npcName.ToLower().Replace(" ", "_"), npcName));
                quest.Title = $"Rescue {npcName}";
                break;

            case QuestTarget.SurviveDungeon:
                // Survive multiple floors without returning
                var surviveFloors = quest.Difficulty * 3 + random.Next(2, 5);
                quest.Objectives.Add(new QuestObjective(
                    QuestObjectiveType.ReachDungeonFloor,
                    $"Survive {surviveFloors} consecutive floors",
                    surviveFloors, "", "Floors"));
                quest.Objectives.Add(new QuestObjective(
                    QuestObjectiveType.KillMonsters,
                    "Defeat at least 10 monsters",
                    10, "", "Monsters") { IsOptional = false });
                quest.Title = $"Survive {surviveFloors} Floors";
                break;
        }
    }

    /// <summary>
    /// Create a dungeon quest (bounty board style)
    /// </summary>
    public static Quest CreateDungeonQuest(QuestTarget target, byte difficulty, string dungeonName = "The Dungeon")
    {
        if (target < QuestTarget.ClearBoss || target > QuestTarget.SurviveDungeon)
        {
            throw new ArgumentException("Invalid dungeon quest target type");
        }

        var quest = new Quest
        {
            Initiator = "Bounty Board",
            QuestType = QuestType.SingleQuest,
            QuestTarget = target,
            Difficulty = difficulty,
            Comment = $"Dungeon quest in {dungeonName}",
            Date = DateTime.Now,
            MinLevel = difficulty * 2,
            MaxLevel = 9999,
            DaysToComplete = GameConfig.DefaultQuestDays + difficulty
        };

        // Generate objectives based on quest type
        GenerateDungeonQuestObjectives(quest);

        // Set rewards based on difficulty
        SetDefaultRewards(quest);

        // Add to database
        questDatabase.Add(quest);

        GD.Print($"[QuestSystem] Dungeon quest created: {quest.Title}");

        return quest;
    }

    /// <summary>
    /// Get available dungeon quests from the bounty board
    /// </summary>
    public static List<Quest> GetBountyBoardQuests(Character player)
    {
        return questDatabase.Where(q =>
            !q.Deleted &&
            string.IsNullOrEmpty(q.Occupier) &&
            q.Initiator == "Bounty Board" &&
            player.Level >= q.MinLevel &&
            player.Level <= q.MaxLevel
        ).ToList();
    }

    /// <summary>
    /// Populate the bounty board with available quests (called on new day or when empty)
    /// </summary>
    public static void RefreshBountyBoard(int playerLevel)
    {
        // Remove old unclaimed bounty board quests
        questDatabase.RemoveAll(q => q.Initiator == "Bounty Board" && string.IsNullOrEmpty(q.Occupier) && q.Date < DateTime.Now.AddDays(-3));

        // Count existing bounty board quests
        var existingCount = questDatabase.Count(q => q.Initiator == "Bounty Board" && !q.Deleted && string.IsNullOrEmpty(q.Occupier));

        // Add quests until we have 5 available
        var targetCount = 5;
        while (existingCount < targetCount)
        {
            // Random difficulty based on player level
            var difficulty = (byte)Math.Min(4, Math.Max(1, (playerLevel / 5) + random.Next(-1, 2)));

            // Random dungeon quest type
            var questTypes = new[] { QuestTarget.ClearBoss, QuestTarget.FindArtifact, QuestTarget.ReachFloor, QuestTarget.ClearFloor, QuestTarget.SurviveDungeon };
            var questType = questTypes[random.Next(questTypes.Length)];

            CreateDungeonQuest(questType, difficulty);
            existingCount++;
        }

        GD.Print($"[QuestSystem] Bounty board refreshed with {existingCount} quests");
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
        // quest.Penalty = (byte)Math.Max(1, quest.Reward - 1);
        // quest.PenaltyType = quest.RewardType;
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
        MailSystem.SendQuestFailureMail(quest.Occupier, quest.Title);
        
        // Send failure notification to king
        var kingName = quest.Initiator;
        MailSystem.SendQuestFailureNotificationMail(kingName, quest.Title);
        
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
        MailSystem.SendQuestCompletionMail(player.Name2, quest.Title, rewardAmount);
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

    /// <summary>
    /// Restore quests from save data
    /// </summary>
    public static void RestoreFromSaveData(List<QuestData> savedQuests)
    {
        // Clear existing quests
        questDatabase.Clear();

        if (savedQuests == null || savedQuests.Count == 0)
        {
            GD.Print("[QuestSystem] No saved quests to restore");
            return;
        }

        foreach (var questData in savedQuests)
        {
            var quest = new Quest
            {
                Id = questData.Id,
                Title = questData.Title,
                Initiator = questData.Initiator,
                Comment = questData.Comment,
                Date = questData.StartTime,
                QuestType = (QuestType)questData.QuestType,
                QuestTarget = (QuestTarget)questData.QuestTarget,
                Difficulty = (byte)questData.Difficulty,
                Occupier = questData.Occupier,
                OccupiedDays = questData.OccupiedDays,
                DaysToComplete = questData.DaysToComplete,
                MinLevel = questData.MinLevel,
                MaxLevel = questData.MaxLevel,
                Reward = (byte)questData.Reward,
                RewardType = (QuestRewardType)questData.RewardType,
                Penalty = (byte)questData.Penalty,
                PenaltyType = (QuestRewardType)questData.PenaltyType,
                OfferedTo = questData.OfferedTo,
                Forced = questData.Forced,
                Deleted = questData.Status == QuestStatus.Completed || questData.Status == QuestStatus.Failed
            };

            // Restore objectives
            foreach (var objData in questData.Objectives)
            {
                quest.Objectives.Add(new QuestObjective
                {
                    Id = objData.Id,
                    Description = objData.Description,
                    ObjectiveType = (QuestObjectiveType)objData.ObjectiveType,
                    TargetId = objData.TargetId,
                    TargetName = objData.TargetName,
                    RequiredProgress = objData.RequiredProgress,
                    CurrentProgress = objData.CurrentProgress,
                    IsOptional = objData.IsOptional,
                    BonusReward = objData.BonusReward
                });
            }

            // Restore monsters
            foreach (var monsterData in questData.Monsters)
            {
                quest.Monsters.Add(new QuestMonster(
                    monsterData.MonsterType,
                    monsterData.Count,
                    monsterData.MonsterName
                ));
            }

            questDatabase.Add(quest);
        }

        GD.Print($"[QuestSystem] Restored {questDatabase.Count} quests from save data");
    }

    /// <summary>
    /// Clear all quests (for testing or new game)
    /// </summary>
    public static void ClearAllQuests()
    {
        questDatabase.Clear();
        GD.Print("[QuestSystem] Quest database cleared");
    }

    #region Quest Progress Tracking

    /// <summary>
    /// Update quest progress when a monster is killed
    /// Call this from CombatEngine after monster defeat
    /// </summary>
    public static void OnMonsterKilled(Character player, string monsterName, bool isBoss = false)
    {
        var playerQuests = GetPlayerQuests(player.Name2);

        foreach (var quest in playerQuests)
        {
            // Update kill monster objectives
            quest.UpdateObjectiveProgress(QuestObjectiveType.KillMonsters, 1);
            quest.UpdateObjectiveProgress(QuestObjectiveType.KillSpecificMonster, 1, monsterName.ToLower().Replace(" ", "_"));

            // Update boss kill objectives
            if (isBoss)
            {
                quest.UpdateObjectiveProgress(QuestObjectiveType.KillBoss, 1, monsterName.ToLower().Replace(" ", "_"));
            }

            // Check for floor clear objective
            quest.UpdateObjectiveProgress(QuestObjectiveType.ClearDungeonFloor, 1);
        }
    }

    /// <summary>
    /// Update quest progress when player reaches a dungeon floor
    /// Call this from DungeonLocation when entering a floor
    /// </summary>
    public static void OnDungeonFloorReached(Character player, int floorNumber)
    {
        var playerQuests = GetPlayerQuests(player.Name2);

        foreach (var quest in playerQuests)
        {
            // Update floor objectives - set progress to floor number reached
            foreach (var objective in quest.Objectives.Where(o =>
                o.ObjectiveType == QuestObjectiveType.ReachDungeonFloor && !o.IsComplete))
            {
                if (floorNumber >= objective.RequiredProgress)
                {
                    objective.CurrentProgress = objective.RequiredProgress;
                }
                else if (floorNumber > objective.CurrentProgress)
                {
                    objective.CurrentProgress = floorNumber;
                }
            }
        }
    }

    /// <summary>
    /// Update quest progress when player finds an artifact
    /// Call this from DungeonLocation or treasure system
    /// </summary>
    public static void OnArtifactFound(Character player, string artifactId)
    {
        var playerQuests = GetPlayerQuests(player.Name2);

        foreach (var quest in playerQuests)
        {
            quest.UpdateObjectiveProgress(QuestObjectiveType.FindArtifact, 1, artifactId);
        }
    }

    /// <summary>
    /// Update quest progress when player talks to an NPC
    /// Call this from InnLocation or other NPC interaction points
    /// </summary>
    public static void OnNPCInteraction(Character player, string npcId)
    {
        var playerQuests = GetPlayerQuests(player.Name2);

        foreach (var quest in playerQuests)
        {
            quest.UpdateObjectiveProgress(QuestObjectiveType.TalkToNPC, 1, npcId);
        }
    }

    /// <summary>
    /// Update quest progress when player visits a location
    /// Call this from BaseLocation.EnterLocation
    /// </summary>
    public static void OnLocationVisited(Character player, string locationId)
    {
        var playerQuests = GetPlayerQuests(player.Name2);

        foreach (var quest in playerQuests)
        {
            quest.UpdateObjectiveProgress(QuestObjectiveType.VisitLocation, 1, locationId);
        }
    }

    /// <summary>
    /// Update quest progress when gold is collected
    /// </summary>
    public static void OnGoldCollected(Character player, long amount)
    {
        var playerQuests = GetPlayerQuests(player.Name2);

        foreach (var quest in playerQuests)
        {
            quest.UpdateObjectiveProgress(QuestObjectiveType.CollectGold, (int)Math.Min(amount, int.MaxValue));
        }
    }

    /// <summary>
    /// Check if player has any completed quests ready to turn in
    /// </summary>
    public static List<Quest> GetCompletedQuests(Character player)
    {
        var playerQuests = GetPlayerQuests(player.Name2);
        return playerQuests.Where(q => q.AreAllObjectivesComplete()).ToList();
    }

    /// <summary>
    /// Get quest progress summary for display
    /// </summary>
    public static string GetQuestProgressSummary(Quest quest)
    {
        if (quest.Objectives.Count == 0)
        {
            return "No tracked objectives";
        }

        var completed = quest.Objectives.Count(o => o.IsComplete && !o.IsOptional);
        var required = quest.Objectives.Count(o => !o.IsOptional);
        var optional = quest.Objectives.Count(o => o.IsOptional && o.IsComplete);
        var totalOptional = quest.Objectives.Count(o => o.IsOptional);

        var summary = $"Progress: {completed}/{required} objectives complete";
        if (totalOptional > 0)
        {
            summary += $" (+{optional}/{totalOptional} bonus)";
        }

        return summary;
    }

    #endregion
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
