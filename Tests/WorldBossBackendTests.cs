using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using UsurperRemake;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// v1.1.4: the world boss rows. Every shared write is one guarded UPDATE whose row count is the
/// signal (DOCS/WORLD_BOSS_PLAN.md, "Invariants"). Real SQLite in a temp file per test.
/// </summary>
[Collection("SharedGameSingletons")]
public class WorldBossBackendTests : IDisposable
{
    private readonly string _path;
    private readonly SqlSaveBackend _db;

    public WorldBossBackendTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"usurper-wb-{Guid.NewGuid():N}.db");
        _db = new SqlSaveBackend(_path);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_path); } catch { }
    }

    private Task<int> Spawn(long hp = 1000, int hours = 3) =>
        _db.SpawnScheduledWorldBoss("Test Boss", 40, hp, hours, "{}", "abyssal_leviathan", DateTime.UtcNow, 40, 2);

    private void Exec(string sql, int id)
    {
        using var c = new SqliteConnection($"Data Source={_path};Pooling=true"); c.Open();
        using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
    }

    [Fact]
    public async Task Damage_IsCreditedOnlyToAnActiveRow_AndClampedToRemainingHp()
    {
        int id = await Spawn(hp: 100);
        var (rem, kill, applied) = await _db.RecordWorldBossDamage(id, "Hero", 60, 40);
        (rem, kill, applied).Should().Be((40L, false, 60L));
        (rem, kill, applied) = await _db.RecordWorldBossDamage(id, "Hero", 999, 40);
        applied.Should().Be(40, "overkill is not credited");
        kill.Should().BeTrue();
        rem.Should().Be(0);
        (rem, kill, applied) = await _db.RecordWorldBossDamage(id, "Late", 50, 40);
        (kill, applied).Should().Be((false, 0L), "a defeated row credits nothing");
        var board = await _db.GetWorldBossDamageLeaderboard(id, 10);
        board.Should().ContainSingle().Which.DamageDealt.Should().Be(100);
        board[0].PlayerLevel.Should().Be(40);
        board[0].NightDamage.Should().Be(100);
        (await _db.GetWorldBossById(id))!.Status.Should().Be("defeated");
    }

    [Fact]
    public async Task ExpiredWindow_CreditsNothing_UntilTheTickWithdrawsAndReactivates()
    {
        int id = await Spawn(hp: 1000);
        await _db.RecordWorldBossDamage(id, "Hero", 300, 40);
        Exec("UPDATE world_bosses SET expires_at = datetime('now', '-1 minute') WHERE id = @id;", id);
        (await _db.RecordWorldBossDamage(id, "Hero", 100, 40)).applied.Should().Be(0, "outside the window");
        (await _db.GetActiveWorldBoss()).Should().BeNull("the UI query hides a passed window");
        (await _db.GetActiveWorldBossAnyTime())!.Id.Should().Be(id, "the tick still sees it");

        (await _db.WithdrawWorldBoss(id)).Should().BeTrue();
        (await _db.WithdrawWorldBoss(id)).Should().BeFalse("once");
        var w = await _db.GetWithdrawnWorldBoss();
        w!.CurrentHP.Should().Be(700);

        (await _db.ReactivateWorldBoss(id, 0.20, 3)).Should().BeTrue();
        (await _db.ReactivateWorldBoss(id, 0.20, 3)).Should().BeFalse("once per night");
        var back = await _db.GetActiveWorldBoss();
        back!.CurrentHP.Should().Be(900, "20 percent of max regenerated");
        back.Nights.Should().Be(2);
        var board = await _db.GetWorldBossDamageLeaderboard(id, 10);
        board[0].DamageDealt.Should().Be(300, "the boss keeps its wounds across nights");
        board[0].NightDamage.Should().Be(0, "night damage starts over");

        (await _db.MarkWorldBossLeft(id)).Should().BeFalse("only a withdrawn boss leaves");
        (await _db.WithdrawWorldBoss(id)).Should().BeTrue();
        (await _db.MarkWorldBossLeft(id)).Should().BeTrue();
        (await _db.GetWorldBossById(id))!.Status.Should().Be("left");
    }

    [Fact]
    public async Task Rally_RegeneratesOnlyWhenIdle_AndPhaseOnlyRises()
    {
        int id = await Spawn(hp: 1000);
        (await _db.RallyRegenWorldBoss(id, 50, 10)).Should().BeFalse("never hit: nothing to regenerate");
        await _db.RecordWorldBossDamage(id, "Hero", 400, 40);
        (await _db.RallyRegenWorldBoss(id, 50, 10)).Should().BeFalse("hit a moment ago");
        Exec("UPDATE world_bosses SET last_damaged_at = datetime('now', '-11 minutes') WHERE id = @id;", id);
        (await _db.RallyRegenWorldBoss(id, 50, 10)).Should().BeTrue();
        (await _db.GetWorldBossById(id))!.CurrentHP.Should().Be(650);
        (await _db.RallyRegenWorldBoss(id, 5000, 10)).Should().BeTrue();
        (await _db.GetWorldBossById(id))!.CurrentHP.Should().Be(1000, "never above max");
        (await _db.RallyRegenWorldBoss(id, 50, 10)).Should().BeFalse("full");

        (await _db.RaiseWorldBossPhase(id, 2)).Should().BeTrue();
        (await _db.RaiseWorldBossPhase(id, 2)).Should().BeFalse("already there");
        (await _db.RaiseWorldBossPhase(id, 1)).Should().BeFalse("never back");
        (await _db.GetWorldBossById(id))!.Phase.Should().Be(2);

        (await _db.HealWorldBoss(id, 10)).Should().BeTrue();
        (await _db.GetWorldBossById(id))!.CurrentHP.Should().Be(1000);
    }

    [Fact]
    public async Task NightPay_AndDelivery_AreSingleGuardedWrites()
    {
        int id = await Spawn();
        await _db.RecordWorldBossDamage(id, "Hero", 10, 40);
        (await _db.ClaimWorldBossNightPay(id, "hero", 1)).Should().BeTrue();
        (await _db.ClaimWorldBossNightPay(id, "HERO", 1)).Should().BeFalse("paid once");
        (await _db.ClaimWorldBossNightPay(id, "hero", 2)).Should().BeTrue("a different night");
        (await _db.ClaimWorldBossNightPay(id, "nobody", 1)).Should().BeFalse("no row, no pay");

        var reward = new WorldBossReward { BossId = id, BossName = "Test Boss", PlayerName = "Hero", Night = 1, Kind = "kill", Xp = 100, Gold = 50, Fame = 15, Score = 0.5 };
        (await _db.InsertWorldBossReward(reward)).Should().BeTrue();
        (await _db.InsertWorldBossReward(reward)).Should().BeFalse("a second settle writes nothing new");
        var pending = _db.GetUndeliveredWorldBossRewards("hero");
        pending.Should().ContainSingle().Which.Xp.Should().Be(100);
        (await _db.MarkWorldBossRewardDelivered(pending[0].Id)).Should().BeTrue();
        (await _db.MarkWorldBossRewardDelivered(pending[0].Id)).Should().BeFalse("delivered once");
        _db.GetUndeliveredWorldBossRewards("hero").Should().BeEmpty();

        (await _db.MarkWorldBossSettled(id)).Should().BeTrue();
        (await _db.MarkWorldBossSettled(id)).Should().BeFalse();
    }

    [Fact]
    public async Task Sessions_Cooldowns_AndEngagedCount()
    {
        int id = await Spawn();
        _db.GetWorldBossCooldownSeconds(id, "hero").Should().Be(0);
        await _db.RecordWorldBossSession(id, "Hero", 40, 12, fell: true, cooldownSeconds: 300);
        _db.GetWorldBossCooldownSeconds(id, "hero").Should().BeInRange(290, 300);
        await _db.RecordWorldBossSession(id, "Hero", 40, 5, fell: false, cooldownSeconds: 0);
        _db.GetWorldBossCooldownSeconds(id, "hero").Should().Be(0);
        _db.GetWorldBossEngagedCount(id, 2).Should().Be(0, "sessions without damage are not engaged");
        await _db.RecordWorldBossDamage(id, "Hero", 10, 40);
        await _db.RecordWorldBossDamage(id, "Mira", 10, 30);
        _db.GetWorldBossEngagedCount(id, 2).Should().Be(2);
        var board = await _db.GetWorldBossDamageLeaderboard(id, 10);
        board.First(e => e.PlayerName == "hero").Sessions.Should().Be(2);
        board.First(e => e.PlayerName == "hero").Rounds.Should().Be(17);
        (await _db.UpdateWorldBossPeakEngaged(id, 2)).Should().BeTrue();
        (await _db.UpdateWorldBossPeakEngaged(id, 1)).Should().BeFalse("peak only rises");
        _db.GetUnsettledWorldBossIds().Should().BeEmpty("active bosses are not settled");
        _db.LogWorldBossEvent(id, "spawn", "", "test");
        _db.GetActivePlayersMedianLevel(7, fallback: 33).Should().Be(33, "no players in a fresh database");
    }
}
