using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using UsurperRemake;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// Design item F: relationships cool with neglect measured in days the player was present,
/// never in wall-clock absence. Non-spouses cool one step per NeglectStepDays down to Normal;
/// spouses stay married but LoveLevel worsens after a grace period; any positive contact
/// resets the clock.
/// </summary>
[Collection("SharedGameSingletons")]
public class RelationshipNeglectTests : IDisposable
{
    private readonly Player _player;
    private readonly Character _npc;

    public RelationshipNeglectTests()
    {
        RelationshipSystem.Instance.Reset();
        _player = new Player { Name2 = "Hero", ID = "player-hero", Class = CharacterClass.Warrior, Race = CharacterRace.Human };
        _npc = new Character { Name2 = "Mira", ID = "npc-mira", Class = CharacterClass.Cleric, Race = CharacterRace.Elf };
    }

    public void Dispose() => RelationshipSystem.Instance.Reset();

    private RelationshipSystem.RelationshipRecord Record() => RelationshipSystem.GetOrCreateRelationship(_player, _npc);

    private int NpcFeeling()
    {
        var r = Record();
        return r.Name1 == _player.Name ? r.Relation2 : r.Relation1;
    }

    private void SetNpcFeeling(int value)
    {
        var r = Record();
        if (r.Name1 == _player.Name) r.Relation2 = value; else r.Relation1 = value;
    }

    [Fact]
    public void Neglect_CoolsOneStepPerWeek_DownToNormal_NeverBelow()
    {
        SetNpcFeeling(GameConfig.RelationFriendship); // 40
        _player.PresentDays = 6;
        RelationshipSystem.ProcessNeglect(_player);
        NpcFeeling().Should().Be(GameConfig.RelationFriendship, "six days is inside the first week");

        _player.PresentDays = 7;
        RelationshipSystem.ProcessNeglect(_player);
        NpcFeeling().Should().Be(GameConfig.RelationTrust, "one step at seven days");

        for (int day = 8; day <= 40; day++) { _player.PresentDays = day; RelationshipSystem.ProcessNeglect(_player); }
        NpcFeeling().Should().Be(GameConfig.RelationNormal, "neglect stops at Normal and never turns hostile");
    }

    [Fact]
    public void PositiveContact_StampsTheDay_AndResetsTheClock()
    {
        SetNpcFeeling(GameConfig.RelationFriendship);
        _player.PresentDays = 30;
        RelationshipSystem.UpdateRelationship(_player, _npc, +1);
        Record().LastPlayerContactDay.Should().Be(30);
        RelationshipSystem.GetNeglectDays(_player, _npc).Should().Be(0);
        _player.PresentDays = 37;
        RelationshipSystem.GetNeglectDays(_player, _npc).Should().Be(7);
    }

    [Fact]
    public void AFreshlyCreatedCharacter_WithHumanAI_StampsContactToo()
    {
        // Character creation builds a plain Character (not a Player) until the first reload.
        var fresh = new Character { Name2 = "Newborn", ID = "player-new", AI = CharacterAI.Human, Class = CharacterClass.Warrior, Race = CharacterRace.Human, PresentDays = 4 };
        RelationshipSystem.UpdateRelationship(fresh, _npc, +1);
        RelationshipSystem.GetOrCreateRelationship(fresh, _npc).LastPlayerContactDay.Should().Be(4);
    }

    [Fact]
    public void ContactAtTheDailyCap_StillCounts()
    {
        SetNpcFeeling(GameConfig.RelationFriendship);
        _player.PresentDays = 3;
        for (int i = 0; i < GameConfig.MaxDailyRelationshipGain + 2; i++)
            RelationshipSystem.UpdateRelationship(_player, _npc, +1);
        _player.PresentDays = 9;
        RelationshipSystem.UpdateRelationship(_player, _npc, +1); // capped for today, still contact
        Record().LastPlayerContactDay.Should().Be(9);
    }

    [Fact]
    public void Spouse_StaysMarried_ButLoveLevelCreepsAfterTheGrace()
    {
        var r = Record();
        r.Relation1 = GameConfig.RelationMarried; r.Relation2 = GameConfig.RelationMarried;
        var romance = RomanceTracker.Instance;
        romance.Spouses.Clear();
        romance.Spouses.Add(new Spouse { NPCId = _npc.ID!, NPCName = _npc.Name, LoveLevel = 20 });
        try
        {
            for (int day = 1; day <= 28; day++) { _player.PresentDays = day; RelationshipSystem.ProcessNeglect(_player); }
            romance.Spouses[0].LoveLevel.Should().Be(35, "no creep at 7 (grace), then +5 at 14, 21, 28");
            RelationshipSystem.AreMarried(_player, _npc).Should().BeTrue("neglect never divorces by itself");
            RelationshipSystem.UpdateRelationship(_player, _npc, +1);
            romance.Spouses[0].LoveLevel.Should().Be(30, "making up after a long silence gives one step back");
        }
        finally { romance.Spouses.Clear(); }
    }

    [Fact]
    public void LastContactDay_RoundTripsThroughExportAndImport()
    {
        SetNpcFeeling(GameConfig.RelationFriendship);
        Record().LastPlayerContactDay = 12;
        var exported = RelationshipSystem.ExportAllRelationships();
        exported.Should().ContainSingle(e => e.Name1 == _player.Name || e.Name2 == _player.Name)
            .Which.LastPlayerContactDay.Should().Be(12);
        RelationshipSystem.ImportAllRelationships(exported);
        Record().LastPlayerContactDay.Should().Be(12);
    }

    [Fact]
    public void PresentDays_AdvancesOnlyInRunBasicDailyReset()
    {
        // Absence must add nothing: the catch-up path that replays missed days must not touch
        // PresentDays, and the live reset must. Checked at the source so a refactor cannot move it.
        string src = File.ReadAllText(Path.Combine(RepoRoot(), "Scripts", "Systems", "DailySystemManager.cs"));
        Body(src, "RunBasicDailyReset").Should().Contain("PresentDays++");
        Body(src, "RunCatchUpDailyReset").Should().NotContain("PresentDays");
        Body(src, "RunCatchUpDailyReset").Should().NotContain("RunBasicDailyReset(");
    }

    private static string Body(string src, string method)
    {
        var m = Regex.Match(src, @"(?:Task|void)\s+" + Regex.Escape(method) + @"\s*\(");
        m.Success.Should().BeTrue($"{method} must be defined");
        int start = m.Index;
        int brace = src.IndexOf('{', start); int depth = 0;
        for (int i = brace; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}' && --depth == 0) return src.Substring(brace, i - brace + 1);
        }
        throw new InvalidOperationException("unbalanced braces");
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "usurper-reloaded.csproj"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("repo root");
    }
}
