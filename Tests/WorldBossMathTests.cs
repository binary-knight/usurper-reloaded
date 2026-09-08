using System;
using System.Linq;
using FluentAssertions;
using UsurperRemake;
using UsurperRemake.Data;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// v1.1.4 (DOCS/WORLD_BOSS_PLAN.md rulings 3 and 4, council decision 5): the world boss arithmetic
/// with the plan's pinned numbers.
/// </summary>
public class WorldBossMathTests
{
    [Fact]
    public void KillBudget_MatchesThePlan()
    {
        // Malachar 35 / def 90, Leviathan 40 / def 100, Horror 80 / def 160
        WorldBossMath.PerPlayerBudget(35, 90).Should().Be(14175);
        WorldBossMath.MaxHP(35, 90).Should().Be(42525);
        WorldBossMath.PerPlayerBudget(40, 100).Should().Be(16200);
        WorldBossMath.MaxHP(40, 100).Should().Be(48600);
        WorldBossMath.PerPlayerBudget(80, 160).Should().Be(33750);
        WorldBossMath.MaxHP(80, 160).Should().Be(101250);
        WorldBossMath.RoundCap(48600).Should().Be(291);
        WorldBossMath.RoundCap(101250).Should().Be(607);
        WorldBossMath.RoundCap(48600, staggered: true).Should().Be(437);
    }

    [Fact]
    public void Ratio_IsOneSided_WithThePinnedValues()
    {
        WorldBossMath.Ratio(15, 40).Should().BeApproximately(0.37, 0.01);
        WorldBossMath.Ratio(40, 40).Should().Be(1.0, "at level fights raw");
        WorldBossMath.Ratio(80, 40).Should().Be(1.0, "one-sided: above level fights raw under the cap");
        // N on the raw level, not MonsterGenerator's soft-capped one
        (WorldBossMath.N(80) / WorldBossMath.N(40)).Should().BeApproximately(2.03, 0.01);
        // a level 15 who hits like a level 15 lands the level-40 reference's wound
        WorldBossMath.Applied(52, WorldBossMath.Ratio(15, 40), WorldBossMath.RoundCap(48600)).Should().BeInRange(138, 146);
        // the cap bounds a capped caster
        WorldBossMath.Applied(5700, 1.0, WorldBossMath.RoundCap(101250)).Should().Be(607);
        WorldBossMath.Applied(0, 0.37, 100).Should().Be(0);
    }

    [Fact]
    public void Score_AndQualification()
    {
        WorldBossMath.Score(16200, 16200).Should().Be(1.0);
        WorldBossMath.Score(40000, 16200).Should().Be(1.0, "capped");
        WorldBossMath.Score(1620, 16200).Should().BeApproximately(0.1, 1e-9);
        WorldBossMath.Qualified(0.1).Should().BeTrue();
        WorldBossMath.Qualified(0.099).Should().BeFalse();
        WorldBossMath.Score(10, 0).Should().Be(0);
    }

    [Theory]
    [InlineData(15, 6400)]
    [InlineData(40, 42025)]
    [InlineData(80, 321994)]
    public void KillXP_IsAnHourInTheDungeon_CappedAtHalfALevel(int level, long expected)
    {
        WorldBossMath.KillXP(level, 1.0, 1).Should().BeCloseTo(expected, 2);
    }

    [Fact]
    public void Rewards_ScaleWithEffortAndCompany()
    {
        WorldBossMath.KillXP(80, 0, 1).Should().Be((long)Math.Round(WorldBossMath.HourXP(80) * 0.25));
        WorldBossMath.TogetherBonus(1).Should().Be(1.0);
        WorldBossMath.TogetherBonus(3).Should().BeApproximately(1.2, 1e-9);
        WorldBossMath.TogetherBonus(20).Should().Be(1.5, "capped");
        WorldBossMath.KillGold(40, 1.0, 1).Should().Be(37947);
        WorldBossMath.WithdrawalXP(80, 1.0, 1).Should().Be((long)Math.Round(321994 * 0.25));
    }

    [Fact]
    public void Tiers_ByScoreNotRank()
    {
        WorldBossMath.TierFor(0.8, false, 1).Should().Be(LootGenerator.ItemRarity.Epic);
        WorldBossMath.TierFor(0.5, false, 1).Should().Be(LootGenerator.ItemRarity.Rare);
        WorldBossMath.TierFor(0.25, false, 1).Should().Be(LootGenerator.ItemRarity.Uncommon);
        WorldBossMath.TierFor(0.1, false, 1).Should().Be(LootGenerator.ItemRarity.Common);
        WorldBossMath.TierFor(1.0, true, 2).Should().Be(LootGenerator.ItemRarity.Epic, "the MVP of two is not Legendary");
        WorldBossMath.TierFor(1.0, true, 3).Should().Be(LootGenerator.ItemRarity.Legendary);
    }

    [Fact]
    public void Pick_StaysWithinReachOfTheCohort()
    {
        var all = WorldBossDatabase.GetAllBosses();
        var rng = new Random(7);
        WorldBossMath.PickBoss(15, all, rng).BaseLevel.Should().Be(35, "nothing within ten of 15: the lowest boss");
        for (int i = 0; i < 50; i++)
            WorldBossMath.PickBoss(45, all, rng).BaseLevel.Should().BeLessThanOrEqualTo(55);
        for (int i = 0; i < 50; i++)
            WorldBossMath.PickBoss(75, all, rng).BaseLevel.Should().BeLessThanOrEqualTo(85);
        WorldBossMath.BossLevelFor(all.First(b => b.BaseLevel == 35), 50).Should().Be(50);
        WorldBossMath.BossLevelFor(all.First(b => b.BaseLevel == 35), 20).Should().Be(35);
    }

    [Fact]
    public void SpawnTime_IsEightPmEasternOnTheBoundaryDay_DstSafe()
    {
        // 2026-03-07 19:00 EST = 2026-03-08 00:00 UTC; the next day crosses into EDT
        var boundary = new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc);
        WorldBossMath.SpawnUtcFor(boundary, 0).Should().Be(new DateTime(2026, 3, 8, 1, 0, 0, DateTimeKind.Utc), "8 PM EST is 01:00 UTC");
        WorldBossMath.SpawnUtcFor(boundary, 1).Should().Be(new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc), "8 PM EDT is 00:00 UTC");
    }
}
