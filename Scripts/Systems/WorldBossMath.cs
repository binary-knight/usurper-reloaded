using System;
using System.Collections.Generic;
using System.Linq;
using UsurperRemake.Data;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// v1.1.4: the world boss arithmetic, pure so the tests need no database. DOCS/WORLD_BOSS_PLAN.md
    /// rulings 3 and 4 and council decision 5. Every constant is in GameConfig.
    /// </summary>
    public static class WorldBossMath
    {
        /// <summary>At-level monster strength on the raw level (MonsterGenerator's formula without its deep-floor soft cap).</summary>
        public static double N(int level) => 2.0 * Math.Max(1, level) + 1.5 * Math.Pow(Math.Max(1, level), 1.05);

        /// <summary>One-sided: the boss meets a player below its level at theirs; at or above, they fight it raw.</summary>
        public static double Ratio(int playerLevel, int bossLevel) =>
            playerLevel >= bossLevel ? 1.0 : N(playerLevel) / N(bossLevel);

        /// <summary>A reference at-level fighter's damage over one window, against the boss's scaled defence.</summary>
        public static long PerPlayerBudget(int bossLevel, long scaledDefence)
        {
            double perRound = 10 + 3.0 * bossLevel + Math.Min(82, bossLevel) - scaledDefence / 2.0;
            perRound = Math.Max(1, perRound);
            return (long)Math.Round(perRound * GameConfig.WorldBossAbilityFactor * GameConfig.WorldBossRoundsPerWindow);
        }

        public static long MaxHP(int bossLevel, long scaledDefence) =>
            GameConfig.WorldBossKillBudgetPlayers * PerPlayerBudget(bossLevel, scaledDefence);

        public static long RoundCap(long maxHp, bool staggered = false) =>
            Math.Max(1, (long)(maxHp * GameConfig.WorldBossPerRoundCapPercent * (staggered ? 1.5 : 1.0)));

        /// <summary>Native damage becomes applied damage: divided by r, then capped.</summary>
        public static long Applied(long native, double r, long cap) =>
            Math.Min(cap, Math.Max(0, (long)Math.Round(native / Math.Max(0.01, r))));

        public static long UnavoidableCap(long playerMaxHp) =>
            Math.Max(1, (long)(playerMaxHp * GameConfig.WorldBossUnavoidableCapPercent));

        public static double Score(long applied, long budget) =>
            budget <= 0 ? 0 : Math.Min(1.0, (double)Math.Max(0, applied) / budget);

        public static bool Qualified(double score) => score >= GameConfig.WorldBossQualifyScore;

        public static long HourXP(int level) => (long)(GameConfig.WorldBossXPPerHourFactor * Math.Pow(Math.Max(1, level), 1.5));

        public static long NextLevelCost(int level) =>
            Math.Max(1, GameConfig.GetExperienceForLevel(level + 1) - GameConfig.GetExperienceForLevel(level));

        public static double TogetherBonus(int contributors) =>
            Math.Min(GameConfig.WorldBossTogetherBonusCap, 1.0 + GameConfig.WorldBossTogetherBonusPerAlly * Math.Max(0, contributors - 1));

        private static double EffortFactor(double score) => 0.25 + 0.75 * Math.Clamp(score, 0, 1);

        /// <summary>The kill reward at the player's own level, capped at half the next level.</summary>
        public static long KillXP(int playerLevel, double score, int contributors)
        {
            double raw = HourXP(playerLevel) * EffortFactor(score) * TogetherBonus(contributors);
            double cap = NextLevelCost(playerLevel) * GameConfig.WorldBossXPCapOfNextLevel;
            return (long)Math.Round(Math.Min(raw, cap));
        }

        public static long KillGold(int playerLevel, double score, int contributors) =>
            (long)Math.Round(GameConfig.WorldBossGoldFactor * Math.Pow(Math.Max(1, playerLevel), 1.5) * EffortFactor(score) * TogetherBonus(contributors));

        public static long WithdrawalXP(int playerLevel, double nightScore, int nightContributors) =>
            (long)Math.Round(KillXP(playerLevel, nightScore, nightContributors) * GameConfig.WorldBossWithdrawalPayFraction);

        public static long WithdrawalGold(int playerLevel, double nightScore, int nightContributors) =>
            (long)Math.Round(KillGold(playerLevel, nightScore, nightContributors) * GameConfig.WorldBossWithdrawalPayFraction);

        /// <summary>Items by score, not rank; Legendary only for the MVP of a real crowd.</summary>
        public static LootGenerator.ItemRarity TierFor(double score, bool mvp, int humans)
        {
            if (mvp && humans >= GameConfig.WorldBossMvpLegendaryMinHumans) return LootGenerator.ItemRarity.Legendary;
            if (score >= 0.75) return LootGenerator.ItemRarity.Epic;
            if (score >= 0.5) return LootGenerator.ItemRarity.Rare;
            if (score >= 0.25) return LootGenerator.ItemRarity.Uncommon;
            return LootGenerator.ItemRarity.Common;
        }

        /// <summary>Bosses within reach of the cohort; the lowest when none is.</summary>
        public static WorldBossDefinition PickBoss(int medianLevel, IReadOnlyList<WorldBossDefinition> all, Random rng)
        {
            var eligible = all.Where(b => b.BaseLevel <= medianLevel + GameConfig.WorldBossPickLevelSlack).ToList();
            if (eligible.Count == 0) return all.OrderBy(b => b.BaseLevel).First();
            return eligible[rng.Next(eligible.Count)];
        }

        public static int BossLevelFor(WorldBossDefinition def, int medianLevel) => Math.Max(def.BaseLevel, medianLevel);

        private static TimeZoneInfo Eastern()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
        }

        /// <summary>The spawn hour on the reset boundary's Eastern day plus daysAhead, as UTC; DST-safe.</summary>
        public static DateTime SpawnUtcFor(DateTime resetBoundaryUtc, int daysAhead)
        {
            var tz = Eastern();
            var eastern = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(resetBoundaryUtc, DateTimeKind.Utc), tz);
            var spawnEastern = eastern.Date.AddDays(daysAhead).AddHours(GameConfig.WorldBossSpawnHourEastern);
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(spawnEastern, DateTimeKind.Unspecified), tz);
        }

        /// <summary>The next spawn at or after now: today's if it has not passed, else tomorrow's.</summary>
        public static DateTime NextSpawnUtc(DateTime nowUtc)
        {
            var boundary = DailySystemManager.GetCurrentResetBoundary();
            var today = SpawnUtcFor(boundary, 0);
            return today > nowUtc ? today : SpawnUtcFor(boundary, 1);
        }
    }

    /// <summary>The schedule row in world_state (key world_boss_schedule), written at the reset and read by the tick.</summary>
    public class WorldBossSchedule
    {
        public string DefinitionId { get; set; } = "";
        public int BossLevel { get; set; }
        public DateTime SpawnUtc { get; set; }
        public int WindowHours { get; set; } = GameConfig.WorldBossWindowHours;
        public int MedianLevel { get; set; }
        public int CarriedBossId { get; set; }
        public bool NoticedAtReset { get; set; }
        public bool NoticedHourBefore { get; set; }
        public int SpawnedBossId { get; set; }
    }
}
