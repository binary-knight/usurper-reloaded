using System;
using System.Collections.Generic;

/// <summary>
/// Comprehensive statistics tracking system for player progress
/// Tracks combat stats, economic activity, exploration, and more
/// </summary>
public class PlayerStatistics
{
    // === COMBAT STATISTICS ===
    public long TotalMonstersKilled { get; set; }
    public long TotalMonstersEncountered { get; set; }
    public long TotalBossesKilled { get; set; }
    public long TotalUniquesKilled { get; set; }
    public long TotalPlayerKills { get; set; }      // PvP kills
    public long TotalPlayerDeaths { get; set; }     // Deaths to players
    public long TotalMonsterDeaths { get; set; }    // Deaths to monsters
    public long TotalCombatsWon { get; set; }
    public long TotalCombatsLost { get; set; }
    public long TotalCombatsFled { get; set; }
    public long TotalDamageDealt { get; set; }
    public long TotalDamageTaken { get; set; }
    public long HighestSingleHit { get; set; }
    public long TotalCriticalHits { get; set; }
    public long TotalSpellsCast { get; set; }
    public long TotalAbilitiesUsed { get; set; }

    // === ECONOMIC STATISTICS ===
    public long TotalGoldEarned { get; set; }       // All gold ever obtained
    public long TotalGoldSpent { get; set; }        // All gold ever spent
    public long TotalGoldFromMonsters { get; set; }
    public long TotalGoldFromQuests { get; set; }
    public long TotalGoldFromSelling { get; set; }
    public long TotalGoldFromGambling { get; set; }
    public long TotalGoldLostGambling { get; set; }
    public long TotalGoldStolen { get; set; }       // Via thievery
    public long TotalGoldLostToThieves { get; set; }
    public long HighestGoldHeld { get; set; }       // Peak gold at any point
    public long TotalItemsBought { get; set; }
    public long TotalItemsSold { get; set; }
    public long MostExpensivePurchase { get; set; }

    // === EXPERIENCE STATISTICS ===
    public long TotalExperienceEarned { get; set; }
    public long ExperienceFromMonsters { get; set; }
    public long ExperienceFromQuests { get; set; }
    public long ExperienceFromTraining { get; set; }
    public int HighestLevelReached { get; set; }
    public int TotalLevelUps { get; set; }

    // === EXPLORATION STATISTICS ===
    public int DeepestDungeonLevel { get; set; }
    public long TotalDungeonFloorsCovered { get; set; }
    public long TotalRoomsExplored { get; set; }
    public long TotalTrapsTriggered { get; set; }
    public long TotalTrapsDisarmed { get; set; }
    public long TotalSecretsFound { get; set; }
    public long TotalChestsOpened { get; set; }
    public Dictionary<string, int> LocationVisits { get; set; } = new();

    // === SOCIAL STATISTICS ===
    public long TotalNPCInteractions { get; set; }
    public long TotalConversations { get; set; }
    public long TotalGiftsGiven { get; set; }
    public long TotalFriendsGained { get; set; }
    public long TotalEnemiesMade { get; set; }
    public long TotalRomances { get; set; }
    public long TotalTeamBattles { get; set; }

    // === SURVIVAL STATISTICS ===
    public long TotalHealingPotionsUsed { get; set; }
    public long TotalManaPotionsUsed { get; set; }
    public long TotalHealthRestored { get; set; }
    public long TotalManaRestored { get; set; }
    public long TotalTimesResurrected { get; set; }
    public long TotalDiseasesContracted { get; set; }
    public long TotalDiseasesCured { get; set; }
    public long TotalPoisonings { get; set; }
    public long TotalCursesBroken { get; set; }

    // === TIME STATISTICS ===
    public DateTime CharacterCreated { get; set; } = DateTime.Now;
    public TimeSpan TotalPlayTime { get; set; } = TimeSpan.Zero;
    public int TotalDaysPlayed { get; set; }
    public int TotalSessionsPlayed { get; set; }
    public DateTime LastPlayed { get; set; } = DateTime.Now;
    public TimeSpan LongestSession { get; set; } = TimeSpan.Zero;
    public int CurrentStreak { get; set; }          // Consecutive days played
    public int LongestStreak { get; set; }

    // === ACHIEVEMENT TRIGGERS ===
    public int QuestsCompleted { get; set; }
    public int BountiesCompleted { get; set; }
    public int ChallengesCompleted { get; set; }
    public int TimesRuler { get; set; }
    public int DaysAsRuler { get; set; }

    // Session tracking (not saved, used for calculations)
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime SessionStart { get; set; } = DateTime.Now;

    /// <summary>
    /// Update session time when saving
    /// </summary>
    public void UpdateSessionTime()
    {
        var sessionDuration = DateTime.Now - SessionStart;
        TotalPlayTime += sessionDuration;

        if (sessionDuration > LongestSession)
            LongestSession = sessionDuration;

        LastPlayed = DateTime.Now;
        SessionStart = DateTime.Now; // Reset for next calculation
    }

    /// <summary>
    /// Track a new login session
    /// </summary>
    public void TrackNewSession()
    {
        TotalSessionsPlayed++;
        SessionStart = DateTime.Now;

        // Check for streak
        var daysSinceLastPlay = (DateTime.Now.Date - LastPlayed.Date).Days;
        if (daysSinceLastPlay == 1)
        {
            CurrentStreak++;
            if (CurrentStreak > LongestStreak)
                LongestStreak = CurrentStreak;
        }
        else if (daysSinceLastPlay > 1)
        {
            CurrentStreak = 1; // Reset streak
        }
        // If same day, streak doesn't change

        LastPlayed = DateTime.Now;
    }

    /// <summary>
    /// Record a monster kill
    /// </summary>
    public void RecordMonsterKill(long xpGained, long goldGained, bool isBoss, bool isUnique)
    {
        TotalMonstersKilled++;
        TotalCombatsWon++;

        if (isBoss) TotalBossesKilled++;
        if (isUnique) TotalUniquesKilled++;

        TotalExperienceEarned += xpGained;
        ExperienceFromMonsters += xpGained;
        TotalGoldEarned += goldGained;
        TotalGoldFromMonsters += goldGained;
    }

    /// <summary>
    /// Record damage dealt
    /// </summary>
    public void RecordDamageDealt(long damage, bool isCritical)
    {
        TotalDamageDealt += damage;
        if (damage > HighestSingleHit)
            HighestSingleHit = damage;
        if (isCritical)
            TotalCriticalHits++;
    }

    /// <summary>
    /// Record damage taken
    /// </summary>
    public void RecordDamageTaken(long damage)
    {
        TotalDamageTaken += damage;
    }

    /// <summary>
    /// Record player death
    /// </summary>
    public void RecordDeath(bool toPlayer)
    {
        TotalCombatsLost++;
        if (toPlayer)
            TotalPlayerDeaths++;
        else
            TotalMonsterDeaths++;
    }

    /// <summary>
    /// Record gold changes
    /// </summary>
    public void RecordGoldChange(long currentGold)
    {
        if (currentGold > HighestGoldHeld)
            HighestGoldHeld = currentGold;
    }

    /// <summary>
    /// Record gold spent on a purchase
    /// </summary>
    public void RecordPurchase(long amount)
    {
        TotalGoldSpent += amount;
        TotalItemsBought++;
        if (amount > MostExpensivePurchase)
            MostExpensivePurchase = amount;
    }

    /// <summary>
    /// Record gold spent (non-purchase, like services, healing, etc.)
    /// </summary>
    public void RecordGoldSpent(long amount)
    {
        TotalGoldSpent += amount;
    }

    /// <summary>
    /// Record item sold
    /// </summary>
    public void RecordSale(long amount)
    {
        TotalGoldEarned += amount;
        TotalGoldFromSelling += amount;
        TotalItemsSold++;
    }

    /// <summary>
    /// Record level up
    /// </summary>
    public void RecordLevelUp(int newLevel)
    {
        TotalLevelUps++;
        if (newLevel > HighestLevelReached)
            HighestLevelReached = newLevel;
    }

    /// <summary>
    /// Record dungeon exploration
    /// </summary>
    public void RecordDungeonLevel(int level)
    {
        TotalDungeonFloorsCovered++;
        if (level > DeepestDungeonLevel)
            DeepestDungeonLevel = level;
    }

    /// <summary>
    /// Record location visit
    /// </summary>
    public void RecordLocationVisit(string locationName)
    {
        if (!LocationVisits.ContainsKey(locationName))
            LocationVisits[locationName] = 0;
        LocationVisits[locationName]++;
    }

    /// <summary>
    /// Record a PvP kill (killing another player/NPC)
    /// </summary>
    public void RecordPlayerKill()
    {
        TotalPlayerKills++;
        TotalCombatsWon++;
    }

    /// <summary>
    /// Record a quest completion
    /// </summary>
    public void RecordQuestComplete()
    {
        QuestsCompleted++;
    }

    /// <summary>
    /// Record a bounty completion
    /// </summary>
    public void RecordBountyComplete()
    {
        BountiesCompleted++;
    }

    /// <summary>
    /// Calculate combat win rate
    /// </summary>
    public double GetCombatWinRate()
    {
        long totalCombats = TotalCombatsWon + TotalCombatsLost + TotalCombatsFled;
        if (totalCombats == 0) return 0;
        return (double)TotalCombatsWon / totalCombats * 100;
    }

    /// <summary>
    /// Calculate average damage per hit
    /// </summary>
    public double GetAverageDamagePerHit()
    {
        if (TotalMonstersKilled == 0) return 0;
        return (double)TotalDamageDealt / TotalMonstersKilled;
    }

    /// <summary>
    /// Get formatted play time string
    /// </summary>
    public string GetFormattedPlayTime()
    {
        UpdateSessionTime();

        if (TotalPlayTime.TotalHours >= 1)
            return $"{(int)TotalPlayTime.TotalHours}h {TotalPlayTime.Minutes}m";
        else
            return $"{TotalPlayTime.Minutes}m {TotalPlayTime.Seconds}s";
    }
}

/// <summary>
/// Static statistics manager for the current player
/// </summary>
public static class StatisticsManager
{
    private static PlayerStatistics? _current;

    /// <summary>
    /// Get or create statistics for current player
    /// </summary>
    public static PlayerStatistics Current
    {
        get => _current ??= new PlayerStatistics();
        set => _current = value;
    }

    /// <summary>
    /// Initialize statistics for a new character
    /// </summary>
    public static void InitializeNew()
    {
        _current = new PlayerStatistics
        {
            CharacterCreated = DateTime.Now,
            SessionStart = DateTime.Now,
            TotalSessionsPlayed = 1
        };
    }

    /// <summary>
    /// Load statistics from save data
    /// </summary>
    public static void LoadFromSaveData(PlayerStatistics? stats)
    {
        if (stats != null)
        {
            _current = stats;
            _current.TrackNewSession();
        }
        else
        {
            InitializeNew();
        }
    }

    /// <summary>
    /// Reset statistics (for testing or new game+)
    /// </summary>
    public static void Reset()
    {
        _current = null;
    }
}
