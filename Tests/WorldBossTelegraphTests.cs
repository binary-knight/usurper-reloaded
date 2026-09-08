using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using UsurperRemake;
using UsurperRemake.Data;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// v1.1.5 (milestone B, DOCS/WORLD_BOSS_PLAN.md ruling 2): the tick owns the telegraphs, players
/// answer them by writing their own column, a channel breaks on a shared counter that never exceeds
/// its need, a landed telegraph is applied to a player once per seq across sessions, focus is
/// last-writer-wins by design, and the round screen fits in twenty rows.
/// </summary>
[Collection("SharedGameSingletons")]
public class WorldBossTelegraphTests : IDisposable
{
    private static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly string _path;
    private readonly SqlSaveBackend _db;
    private readonly WorldBossSystem _sys = new WorldBossSystem();

    public WorldBossTelegraphTests()
    {
        _path = Path.Combine(Path.GetTempPath(), $"usurper-wbt-{Guid.NewGuid():N}.db");
        _db = new SqlSaveBackend(_path);
        _sys.SaveHook = _ => Task.FromResult(true);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(_path); } catch { }
    }

    private sealed class ScriptedStream : Stream
    {
        private readonly byte[] _data; private int _pos;
        public ScriptedStream(string script) { _data = Encoding.UTF8.GetBytes(script); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_pos >= _data.Length) return 0;
            int n = Math.Min(count, _data.Length - _pos); Array.Copy(_data, _pos, buffer, offset, n); _pos += n; return n;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => Task.FromResult(Read(buffer, offset, count));
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
        public override long Length => _data.Length; public override long Position { get => _pos; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long o, SeekOrigin s) => throw new NotSupportedException();
        public override void SetLength(long v) => throw new NotSupportedException();
        public override void Write(byte[] b, int o, int c) => throw new NotSupportedException();
    }

    private static readonly System.Text.RegularExpressions.Regex Ansi = new("\u001b\\[[0-9;]*[A-Za-z]");

    private static Character Hero(string name, int level, long hp) => new Character
    {
        Name1 = name, Name2 = name, Class = CharacterClass.Warrior, Race = CharacterRace.Human, Level = level,
        HP = hp, MaxHP = hp, Strength = 130, WeapPow = 50, Defence = 100, ArmPow = 40, Dexterity = 30, Mana = 0, MaxMana = 0,
    };

    /// <summary>A boss whose basic attack is a scratch (strength 1), so HP assertions read the telegraphs.</summary>
    private async Task<WorldBossInfo> Spawn(string defId = "abyssal_leviathan", long hp = 200_000, int level = 40, long strength = 1)
    {
        var def = WorldBossDatabase.GetBossById(defId)!;
        var data = new WorldBossRuntimeData { DefinitionId = def.Id, CurrentPhase = 1, ScaledLevel = level, ScaledStrength = strength, ScaledDefence = 0, ScaledAgility = 80, AttacksPerRound = 2 };
        int id = await _db.SpawnScheduledWorldBoss(def.Name, level, hp, 3, System.Text.Json.JsonSerializer.Serialize(data), def.Id, DateTime.UtcNow, level, 1);
        return (await _db.GetWorldBossById(id))!;
    }

    private async Task<string> Fight(Character hero, WorldBossInfo boss, string script)
    {
        var output = new MemoryStream();
        var term = new TerminalEmulator(new ScriptedStream(script), output);
        var m = typeof(WorldBossSystem).GetMethod("RunWorldBossCombat", F)!;
        await (Task)m.Invoke(_sys, new object[] { hero, term, _db, boss })!;
        term.StreamWriterInternal!.Flush();
        return Ansi.Replace(Encoding.UTF8.GetString(output.ToArray()), "");
    }

    private Task Upkeep(WorldBossInfo boss, int engaged) =>
        (Task)typeof(WorldBossSystem).GetMethod("TelegraphUpkeep", F)!.Invoke(_sys, new object[] { _db, boss, engaged })!;

    private void Sql(string sql, int bossId)
    {
        using var c = new SqliteConnection($"Data Source={_path};Pooling=true"); c.Open();
        using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.Parameters.AddWithValue("@id", bossId); cmd.ExecuteNonQuery();
    }

    /// <summary>Tests re-enter the same hero at once; live play waits out the retreat cooldown.</summary>
    private void ClearCooldown(int bossId) => Sql("UPDATE world_boss_damage SET cooldown_until = NULL WHERE boss_id = @id;", bossId);

    private void AgeLanding(int bossId) => Sql("UPDATE world_bosses SET telegraph_lands_at = datetime('now', '-5 seconds') WHERE id = @id;", bossId);

    private async Task<WorldBossInfo> Row(int id) => (await _db.GetWorldBossById(id))!;

    // ───────────────────────────── the tick ─────────────────────────────

    [Fact]
    public async Task TheTick_IssuesFromTheCycle_OnlyWhileSomeoneIsEngaged_AndOnlyFromThePreviousSeq()
    {
        var boss = await Spawn();
        await Upkeep(boss, engaged: 0);
        (await Row(boss.Id)).TelegraphSeq.Should().Be(0, "nothing is issued to an empty field");

        await Upkeep(boss, engaged: 1);
        var r = await Row(boss.Id);
        r.TelegraphSeq.Should().Be(1);
        r.TelegraphId.Should().Be("Tidal Surge", "the first ability of phase 1, in definition order");
        r.InterruptsNeeded.Should().Be(0, "a strike is personal");
        r.TelegraphLive.Should().BeTrue();
        r.TelegraphLandsAt.Should().BeAfter(DateTime.UtcNow.AddSeconds(GameConfig.WorldBossTelegraphLandSeconds - 10));

        await Upkeep(r, engaged: 3);
        (await Row(boss.Id)).TelegraphSeq.Should().Be(1, "nothing new while one is live");
        (await _db.IssueWorldBossTelegraph(boss.Id, "Frost Bolt", 5, 60, 0)).Should().BeFalse("seq must follow the previous one");
        (await _db.IssueWorldBossTelegraph(boss.Id, "Frost Bolt", 2, 60, 0)).Should().BeFalse("not while seq 1 is live either: the guard is the previous seq, and the tick resolves before issuing");
    }

    [Fact]
    public async Task TheTick_ResolvesALandedStrikeOnce_ThenIssuesTheNextAfterTheGap()
    {
        var boss = await Spawn();
        await Upkeep(boss, 1);
        AgeLanding(boss.Id);
        var live = await Row(boss.Id);
        await Upkeep(live, 1);
        var r = await Row(boss.Id);
        r.LastResolvedSeq.Should().Be(1);
        r.LastResolvedOutcome.Should().Be("landed");
        r.TelegraphLive.Should().BeFalse();
        r.Staggered.Should().BeFalse("a strike never staggers");
        _db.GetResolvedWorldBossTelegraphs(boss.Id, 0).Should().ContainSingle(t => t.Seq == 1 && t.Id == "Tidal Surge" && t.Kind == "strike" && t.Outcome == "landed");
        (await _db.ResolveWorldBossTelegraph(boss.Id, 1, "broken", 60)).Should().BeFalse("resolution is once per seq");

        await Upkeep(r, 1);
        (await Row(boss.Id)).TelegraphSeq.Should().Be(1, "the gap after the landing has not passed");
        Sql("UPDATE world_bosses SET telegraph_lands_at = datetime('now', '-100 seconds') WHERE id = @id;", boss.Id);
        await Upkeep(await Row(boss.Id), 1);
        var next = await Row(boss.Id);
        next.TelegraphSeq.Should().Be(2);
        next.TelegraphId.Should().Be("Frost Bolt", "the cycle continues from the last telegraph");
    }

    [Fact]
    public async Task AChannel_NeedsMinTwoAndEngaged_BreaksWhenMet_AndStaggersTheBoss()
    {
        var boss = await Spawn();
        // Whirlpool is the first channel in the Leviathan's phase 2 cycle: walk the cycle to it
        Sql("UPDATE world_bosses SET phase = 2, telegraph_id = 'Frost Bolt', telegraph_seq = 2, last_resolved_seq = 2, telegraph_lands_at = datetime('now', '-100 seconds') WHERE id = @id;", boss.Id);
        await Upkeep(await Row(boss.Id), engaged: 5);
        var r = await Row(boss.Id);
        r.TelegraphId.Should().Be("Whirlpool");
        r.InterruptsNeeded.Should().Be(2, "min(2, engaged)");

        (await _db.TryInterruptWorldBoss(boss.Id, 3)).Should().BeTrue();
        (await _db.TryInterruptWorldBoss(boss.Id, 3)).Should().BeTrue();
        (await _db.TryInterruptWorldBoss(boss.Id, 3)).Should().BeFalse("the need is met");
        (await _db.TryInterruptWorldBoss(boss.Id, 2)).Should().BeFalse("a stale seq counts for nothing");

        AgeLanding(boss.Id);
        await Upkeep(await Row(boss.Id), 5);
        r = await Row(boss.Id);
        r.LastResolvedOutcome.Should().Be("broken");
        r.Staggered.Should().BeTrue();
        r.StaggerUntil.Should().BeAfter(DateTime.UtcNow.AddSeconds(GameConfig.WorldBossStaggerSeconds - 10));
        WorldBossMath.RoundCap(boss.MaxHP, true).Should().Be((long)(WorldBossMath.RoundCap(boss.MaxHP) * 1.5), "a staggered boss takes half again per round");
    }

    [Fact]
    public async Task AChannelShortOfInterrupts_Lands_AndAHealChannelHealsThePoolOnceInTheTick()
    {
        var boss = await Spawn("shadowlord_malachar", hp: 100_000);
        await _db.RecordWorldBossDamage(boss.Id, "someone", 50_000, 40);
        // Dark Pact is the Shadow Lord's phase 2 heal channel
        Sql("UPDATE world_bosses SET phase = 2, telegraph_id = 'Fear', telegraph_seq = 1, last_resolved_seq = 1, telegraph_lands_at = datetime('now', '-100 seconds') WHERE id = @id;", boss.Id);
        await Upkeep(await Row(boss.Id), engaged: 1);
        var r = await Row(boss.Id);
        r.TelegraphId.Should().Be("Dark Pact");
        r.InterruptsNeeded.Should().Be(1, "alone, one interrupt breaks it");

        (await _db.TryInterruptWorldBoss(boss.Id, 2)).Should().BeTrue();
        AgeLanding(boss.Id);
        await Upkeep(await Row(boss.Id), 1);
        (await Row(boss.Id)).LastResolvedOutcome.Should().Be("broken");
        (await Row(boss.Id)).CurrentHP.Should().Be(50_000, "a broken heal heals nothing");

        // the next one, unanswered: Soul Drain is a strike; walk to the heal again by hand
        Sql("UPDATE world_bosses SET telegraph_id = 'Fear', telegraph_seq = 9, last_resolved_seq = 9, telegraph_lands_at = datetime('now', '-100 seconds'), stagger_until = NULL WHERE id = @id;", boss.Id);
        await Upkeep(await Row(boss.Id), 1);
        AgeLanding(boss.Id);
        await Upkeep(await Row(boss.Id), 1);
        r = await Row(boss.Id);
        r.LastResolvedOutcome.Should().Be("landed");
        r.CurrentHP.Should().Be(53_000, "three percent of max HP, once");
        await Upkeep(r, 1);
        (await Row(boss.Id)).CurrentHP.Should().Be(53_000, "a second tick does not heal again");
    }

    [Fact]
    public async Task TheInterruptCounter_NeverExceedsTheNeed_UnderConcurrentAnswers()
    {
        var boss = await Spawn();
        (await _db.IssueWorldBossTelegraph(boss.Id, "Whirlpool", 1, 60, 2)).Should().BeTrue();
        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => _db.TryInterruptWorldBoss(boss.Id, 1))));
        results.Count(x => x).Should().Be(2);
        (await Row(boss.Id)).InterruptsDone.Should().Be(2);
    }

    // ───────────────────────────── the loop ─────────────────────────────

    [Fact]
    public async Task ABracedStrike_CostsTheRound_AndLandsForATenth()
    {
        var boss = await Spawn();
        await Upkeep(boss, 1);
        var hero = Hero("Bracer", 40, 5000);
        var text = await Fight(hero, boss, "B\nR\n\n");
        text.Should().Contain(Loc.Get("world_boss.menu_brace"));
        text.Should().Contain(Loc.Get("world_boss.you_brace", "Tidal Surge"));
        var mine = _db.GetWorldBossPlayerTelegraphState(boss.Id, "bracer");
        mine.AnsweredSeq.Should().Be(1);
        mine.AnswerKind.Should().Be("brace");
        mine.EngagedSinceSeq.Should().Be(1, "entered while seq 1 was live");
        (await _db.GetWorldBossDamageLeaderboard(boss.Id, 5)).Should().BeEmpty("bracing dealt no damage; the row exists but the board lists hits");

        AgeLanding(boss.Id);
        await Upkeep(await Row(boss.Id), 1);
        ClearCooldown(boss.Id);
        long before = hero.HP;
        text = await Fight(hero, boss, "A\nR\n\n");
        text.Should().Contain(Loc.Get("world_boss.strike_lands_braced", "Tidal Surge", "500", (before - 500).ToString(), "5000"));
        (before - hero.HP).Should().BeInRange(500, 510, "a tenth of max HP plus two scratches");
        _db.GetWorldBossPlayerTelegraphState(boss.Id, "bracer").LastResolvedSeq.Should().Be(1);

        ClearCooldown(boss.Id);
        long again = hero.HP;
        text = await Fight(hero, boss, "A\nR\n\n");
        text.Should().NotContain("lands", "once per seq, across sessions");
        (again - hero.HP).Should().BeInRange(0, 10);
    }

    [Fact]
    public async Task AnUnansweredStrike_LandsForThreeTenths_ButNoActionStoppingStatus()
    {
        var boss = await Spawn();
        // Frost Bolt freezes; frozen players cannot answer the next one, so the landing skips it
        (await _db.IssueWorldBossTelegraph(boss.Id, "Frost Bolt", 1, 60, 0)).Should().BeTrue();
        var hero = Hero("Idler", 40, 5000);
        await Fight(hero, boss, "A\nR\n\n");
        AgeLanding(boss.Id);
        await Upkeep(await Row(boss.Id), 1);
        ClearCooldown(boss.Id);
        long before = hero.HP;
        var text = await Fight(hero, boss, "A\nR\n\n");
        text.Should().Contain(Loc.Get("world_boss.strike_lands_full", "Frost Bolt", "1,500", (before - 1500).ToString(), "5000"));
        (before - hero.HP).Should().BeInRange(1500, 1510);
        hero.ActiveStatuses.Keys.Should().NotContain(StatusEffect.Frozen);
        (await _db.GetWorldBossDamageLeaderboard(boss.Id, 5)).Single().Rounds.Should().Be(4, "two rounds each session: no round was eaten by a freeze");
    }

    [Fact]
    public async Task AReturningPlayer_DoesNotTakeWhatLandedWhileTheyWereAway()
    {
        var boss = await Spawn();
        var hero = Hero("Walker", 40, 5000);
        await Fight(hero, boss, "A\nR\n\n");
        // two strikes issue and land while Walker is gone
        (await _db.IssueWorldBossTelegraph(boss.Id, "Tidal Surge", 1, 60, 0)).Should().BeTrue();
        AgeLanding(boss.Id); await Upkeep(await Row(boss.Id), 1);
        (await _db.IssueWorldBossTelegraph(boss.Id, "Frost Bolt", 2, 60, 0)).Should().BeTrue();
        AgeLanding(boss.Id); await Upkeep(await Row(boss.Id), 1);
        (await Row(boss.Id)).LastResolvedSeq.Should().Be(2);

        ClearCooldown(boss.Id);
        long before = hero.HP;
        var text = await Fight(hero, boss, "A\nR\n\n");
        text.Should().NotContain("lands");
        (before - hero.HP).Should().BeInRange(0, 10, "nothing was live when Walker left; what issued during the absence is not theirs");
        _db.GetWorldBossPlayerTelegraphState(boss.Id, "walker").EngagedSinceSeq.Should().Be(3);

        // but the one live when they leave follows them back, and only that one
        (await _db.IssueWorldBossTelegraph(boss.Id, "Tidal Surge", 3, 60, 0)).Should().BeTrue();
        ClearCooldown(boss.Id);
        await Fight(hero, boss, "A\nR\n\n");
        _db.GetWorldBossPlayerTelegraphState(boss.Id, "walker").EngagedUntilSeq.Should().Be(3);
        AgeLanding(boss.Id); await Upkeep(await Row(boss.Id), 1);
        (await _db.IssueWorldBossTelegraph(boss.Id, "Frost Bolt", 4, 60, 0)).Should().BeTrue();
        AgeLanding(boss.Id); await Upkeep(await Row(boss.Id), 1);
        ClearCooldown(boss.Id);
        before = hero.HP;
        text = await Fight(hero, boss, "A\nR\n\n");
        text.Should().Contain("Tidal Surge lands").And.NotContain("Frost Bolt lands");
        (before - hero.HP).Should().BeInRange(1500, 1510, "one landing, unanswered");
    }

    [Fact]
    public async Task Interrupt_ThroughTheLoop_CountsOnce_AndTheBrokenChannelIsShownNext()
    {
        var boss = await Spawn();
        (await _db.IssueWorldBossTelegraph(boss.Id, "Whirlpool", 1, 60, 1)).Should().BeTrue();
        var hero = Hero("Breaker", 40, 5000);
        var text = await Fight(hero, boss, "T\nT\nR\n\n");
        text.Should().Contain(Loc.Get("world_boss.menu_interrupt"));
        text.Should().Contain(Loc.Get("world_boss.you_interrupt", "Whirlpool", 1, 1));
        text.Should().Contain(Loc.Get("world_boss.already_answered"), "the second T is refused by the player's own row");
        (await Row(boss.Id)).InterruptsDone.Should().Be(1);

        AgeLanding(boss.Id);
        await Upkeep(await Row(boss.Id), 1);
        ClearCooldown(boss.Id);
        long before = hero.HP;
        text = await Fight(hero, boss, "A\nR\n\n");
        text.Should().Contain(Loc.Get("world_boss.channel_broken", "The Abyssal Leviathan", "Whirlpool"));
        text.Should().Contain(Loc.Get("world_boss.staggered_header", 0).Split(' ')[0], "the header shows the stagger");
        (before - hero.HP).Should().BeInRange(0, 10, "a broken channel lands nothing");
    }

    [Fact]
    public async Task AnUninterruptedChannel_LandsOnEveryoneEngaged_HalvedForTheBraced()
    {
        var boss = await Spawn();
        (await _db.IssueWorldBossTelegraph(boss.Id, "Whirlpool", 1, 60, 2)).Should().BeTrue();
        var braced = Hero("Braced", 40, 5000);
        var bare = Hero("Bare", 40, 5000);
        await Fight(braced, boss, "B\nR\n\n");
        await Fight(bare, boss, "A\nR\n\n");
        AgeLanding(boss.Id);
        await Upkeep(await Row(boss.Id), 2);
        (await Row(boss.Id)).LastResolvedOutcome.Should().Be("landed");
        ClearCooldown(boss.Id);
        long b0 = braced.HP, n0 = bare.HP;
        await Fight(braced, boss, "A\nR\n\n");
        await Fight(bare, boss, "A\nR\n\n");
        (b0 - braced.HP).Should().BeInRange(750, 760, "fifteen percent");
        (n0 - bare.HP).Should().BeInRange(1500, 1510, "thirty percent");
    }

    // ───────────────────────────── focus ─────────────────────────────

    [Fact]
    public async Task Focus_FollowsWindowDamage_UnlessAChallengeHoldsIt_LastWriterWins()
    {
        var boss = await Spawn();
        await _db.RecordWorldBossDamage(boss.Id, "small", 100, 40, "Small");
        await _db.RecordWorldBossDamage(boss.Id, "big", 900, 40, "Big");
        (await _db.RefreshWorldBossFocus(boss.Id, 60, 2)).Should().BeNull("the window has not closed");
        Sql("UPDATE world_bosses SET window_started_at = datetime('now', '-100 seconds') WHERE id = @id;", boss.Id);
        (await _db.RefreshWorldBossFocus(boss.Id, 60, 2)).Should().Be("big");
        var board = await _db.GetWorldBossDamageLeaderboard(boss.Id, 5);
        board.Should().OnlyContain(e => e.DamageDealt > 0, "total damage keeps");
        using (var c = new SqliteConnection($"Data Source={_path};Pooling=true"))
        {
            c.Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT SUM(window_damage) FROM world_boss_damage WHERE boss_id = @id;"; cmd.Parameters.AddWithValue("@id", boss.Id);
            Convert.ToInt64(cmd.ExecuteScalar()).Should().Be(0, "the window starts over");
        }

        // Challenge takes it and holds it through the next window; a second challenge is refused
        (await _db.TryChallengeWorldBoss(boss.Id, "small", 60)).Should().BeTrue();
        (await _db.TryChallengeWorldBoss(boss.Id, "big", 60)).Should().BeFalse("held");
        await _db.RecordWorldBossDamage(boss.Id, "big", 900, 40, "Big");
        Sql("UPDATE world_bosses SET window_started_at = datetime('now', '-100 seconds') WHERE id = @id;", boss.Id);
        (await _db.RefreshWorldBossFocus(boss.Id, 60, 2)).Should().Be("small", "the hold outlasts the window");
        // Focus is last-writer-wins by design: when the hold lapses, the next window's top damage takes it back
        Sql("UPDATE world_bosses SET focus_until = datetime('now', '-1 seconds'), window_started_at = datetime('now', '-100 seconds') WHERE id = @id;", boss.Id);
        await _db.RecordWorldBossDamage(boss.Id, "big", 900, 40, "Big");
        (await _db.RefreshWorldBossFocus(boss.Id, 60, 2)).Should().Be("big");
    }

    [Fact]
    public async Task Alone_YouAreAlwaysFocused_AndTheBasicAttackCarriesTheMultiplier()
    {
        var boss = await Spawn(strength: 1000);
        var hero = Hero("Solo", 40, 100_000);
        hero.Defence = 0; hero.ArmPow = 0;
        var text = await Fight(hero, boss, "D\nR\n\n");
        text.Should().Contain(Loc.Get("world_boss.focus_you"));
        text.Should().Contain(Loc.Get("world_boss.fighting_alone"));
        // strength 1000, defence 0 (defend doubles nothing): raw 1000 x 0.7..1.3 x 1.5 focus
        var m = System.Text.RegularExpressions.Regex.Match(text, @"bears down on you for ([\d,]+)");
        m.Success.Should().BeTrue();
        long dmg = long.Parse(m.Groups[1].Value.Replace(",", ""));
        dmg.Should().BeInRange(1050, 1950);
    }

    // ───────────────────────────── the screen ─────────────────────────────

    [Fact]
    public async Task ARound_FitsInTwentyRows_WithATelegraphAndAStatus()
    {
        var boss = await Spawn();
        (await _db.IssueWorldBossTelegraph(boss.Id, "Whirlpool", 1, 60, 2)).Should().BeTrue();
        foreach (var name in new[] { "ann", "bob", "cal", "dee", "eve", "fay", "gus" })
            await _db.RecordWorldBossDamage(boss.Id, name, 10, 40, name.ToUpperInvariant());
        var hero = Hero("Reader", 40, 5000);
        hero.ApplyStatus(StatusEffect.Slow, 3);
        var text = await Fight(hero, boss, "A\nR\n\n");
        var lines = text.Split('\n');
        int r1 = Array.FindIndex(lines, l => l.Contains(Loc.Get("world_boss.round", 1)));
        int r2 = Array.FindIndex(lines, l => l.Contains(Loc.Get("world_boss.round", 2)));
        r1.Should().BeGreaterThan(0); r2.Should().BeGreaterThan(r1);
        (r2 - r1).Should().BeLessThan(20, $"one round is the header, your line, the roster, the telegraph, the menu and the results:\n{string.Join("\n", lines[r1..r2])}");
        text.Should().Contain(Loc.Get("world_boss.and_more", 2), "five names and a count");
        text.Should().Contain(Loc.Get("world_boss.telegraph_channel", "The Abyssal Leviathan", "Whirlpool", 0, 2, "").TrimEnd());
        text.Should().NotContain("╔", "the seven-row box is gone");
    }
}
