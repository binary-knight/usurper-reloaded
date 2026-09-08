using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using UsurperRemake;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// v1.1.4 ruling 1 and 3: the tick owns the schedule, the spawn, the window end, carry-over,
/// Rally, and the phase. The schedule is written into world_state so the test controls the hour.
/// </summary>
[Collection("SharedGameSingletons")]
public class WorldBossTickTests : IDisposable
{
    private readonly string _path;
    private readonly SqlSaveBackend _db;
    private readonly WorldBossSystem _sys = new WorldBossSystem();
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public WorldBossTickTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"usurper-wbt-{Guid.NewGuid():N}.db");
        _db = new SqlSaveBackend(_path);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_path); } catch { }
    }

    private void Exec(string sql, int id)
    {
        using var c = new SqliteConnection($"Data Source={_path};Pooling=true"); c.Open();
        using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
    }

    private async Task<WorldBossSchedule> Schedule() =>
        JsonSerializer.Deserialize<WorldBossSchedule>((await _db.LoadWorldState(WorldBossSystem.ScheduleKey))!, Json)!;

    private async Task ForceSpawnHour()
    {
        var s = await Schedule();
        s.SpawnUtc = DateTime.UtcNow.AddMinutes(-1);
        await _db.SaveWorldState(WorldBossSystem.ScheduleKey, JsonSerializer.Serialize(s, Json));
    }

    [Fact]
    public async Task DayOne_WritesTheNextEightPm_AndSpawnsNothingEarly()
    {
        await _sys.Tick(_db);
        var s = await Schedule();
        s.SpawnUtc.Should().BeAfter(DateTime.UtcNow).And.BeBefore(DateTime.UtcNow.AddHours(25));
        s.DefinitionId.Should().NotBeEmpty();
        s.BossLevel.Should().BeGreaterThanOrEqualTo(35, "the lowest boss, since a fresh database has no players");
        s.SpawnedBossId.Should().Be(0);
        (await _db.GetActiveWorldBoss()).Should().BeNull();
        _sys.Snapshot.Active.Should().BeFalse();
        _sys.Snapshot.NextSpawnUtc.Should().Be(s.SpawnUtc);
        _sys.TownLine().Should().NotBeNull().And.Contain(_sys.Snapshot.NextBossName);
        _sys.TownLine()!.Should().NotContain("{0}", "the countdown line is rendered");
    }

    [Fact]
    public async Task SpawnAtTheHour_WithdrawAtWindowEnd_ReturnRegenerated_LeaveAfterThreeNights()
    {
        await _sys.Tick(_db);
        await ForceSpawnHour();
        await _sys.Tick(_db);
        var boss = await _db.GetActiveWorldBoss();
        boss.Should().NotBeNull("the hour came");
        boss!.Nights.Should().Be(1);
        var def = UsurperRemake.Data.WorldBossDatabase.GetBossById(boss.DefinitionId)!;
        long scaledDef = def.BaseDefence + (boss.BossLevel - def.BaseLevel) * 2;
        boss.MaxHP.Should().Be(WorldBossMath.MaxHP(boss.BossLevel, scaledDef), "the kill budget, not the population");
        (await Schedule()).SpawnedBossId.Should().Be(boss.Id);
        _sys.Snapshot.Active.Should().BeTrue();
        _sys.TownLine().Should().Contain(def.Name);

        // night one ends with the boss wounded
        await _db.RecordWorldBossDamage(boss.Id, "Hero", boss.MaxHP / 2, boss.BossLevel);
        Exec("UPDATE world_bosses SET expires_at = datetime('now', '-1 minute') WHERE id = @id;", boss.Id);
        await _sys.Tick(_db);
        (await _db.GetWorldBossById(boss.Id))!.Status.Should().Be("withdrawn");
        var s = await Schedule();
        s.CarriedBossId.Should().Be(boss.Id, "the same boss returns");
        s.SpawnedBossId.Should().Be(0);
        s.SpawnUtc.Should().BeAfter(DateTime.UtcNow);
        _sys.Snapshot.Active.Should().BeFalse();
        _sys.Snapshot.NextBossName.Should().Be(def.Name);

        // night two: it comes back regenerated, and the same row carries on
        await ForceSpawnHour();
        await _sys.Tick(_db);
        var back = await _db.GetActiveWorldBoss();
        back!.Id.Should().Be(boss.Id);
        back.Nights.Should().Be(2);
        back.CurrentHP.Should().Be(boss.MaxHP - boss.MaxHP / 2 + (long)(boss.MaxHP * 0.20));

        // nights two and three end unkilled: it leaves, and the next schedule is a fresh pick
        Exec("UPDATE world_bosses SET expires_at = datetime('now', '-1 minute') WHERE id = @id;", boss.Id);
        await _sys.Tick(_db);
        await ForceSpawnHour();
        await _sys.Tick(_db);
        (await _db.GetActiveWorldBoss())!.Nights.Should().Be(3);
        Exec("UPDATE world_bosses SET expires_at = datetime('now', '-1 minute') WHERE id = @id;", boss.Id);
        await _sys.Tick(_db);
        (await _db.GetWorldBossById(boss.Id))!.Status.Should().Be("left");
        (await Schedule()).CarriedBossId.Should().Be(0, "a boss that left does not return");
    }

    [Fact]
    public async Task AWindowMissedWhileDown_IsNotSpawnedLate_ItRollsForward()
    {
        await _sys.Tick(_db);
        var s = await Schedule();
        s.SpawnUtc = DateTime.UtcNow.AddHours(-4);
        await _db.SaveWorldState(WorldBossSystem.ScheduleKey, JsonSerializer.Serialize(s, Json));
        await _sys.Tick(_db);
        (await _db.GetActiveWorldBoss()).Should().BeNull("every notice said one hour; no spawn at 2 AM");
        var next = await Schedule();
        next.SpawnUtc.Should().BeAfter(DateTime.UtcNow);
        next.SpawnedBossId.Should().Be(0);
        using var c = new SqliteConnection($"Data Source={_path};Pooling=true"); c.Open();
        using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT COUNT(*) FROM world_boss_events WHERE kind = 'missed';";
        Convert.ToInt32(cmd.ExecuteScalar()).Should().Be(1);
    }

    [Fact]
    public async Task LiveUpkeep_RaisesThePhaseOnce_AndRallyRegeneratesOnlyWhenIdle()
    {
        await _sys.Tick(_db);
        await ForceSpawnHour();
        await _sys.Tick(_db);
        var boss = (await _db.GetActiveWorldBoss())!;
        await _db.RecordWorldBossDamage(boss.Id, "Hero", (long)(boss.MaxHP * 0.40), boss.BossLevel);
        await _sys.Tick(_db);
        (await _db.GetWorldBossById(boss.Id))!.Phase.Should().Be(2, "60 percent is under the 65 line");
        long hpBefore = (await _db.GetWorldBossById(boss.Id))!.CurrentHP;
        await _sys.Tick(_db);
        (await _db.GetWorldBossById(boss.Id))!.CurrentHP.Should().Be(hpBefore, "hit a moment ago: no Rally");
        Exec("UPDATE world_bosses SET last_damaged_at = datetime('now', '-11 minutes'), window_started_at = datetime('now', '-12 minutes') WHERE id = @id;", boss.Id);
        await _sys.Tick(_db);
        var after = (await _db.GetWorldBossById(boss.Id))!;
        after.CurrentHP.Should().Be(hpBefore + Math.Max(1, (long)(boss.MaxHP * 0.005)));
        after.Phase.Should().Be(2, "phase never goes back");
    }
}
