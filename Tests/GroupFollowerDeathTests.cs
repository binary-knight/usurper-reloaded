using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using UsurperRemake;
using UsurperRemake.Server;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// Design item B: a grouped follower who dies in the leader's fight is only marked there;
/// their own session resolves the death. The mark persists so a disconnect cannot lose it.
/// </summary>
[Collection("SharedGameSingletons")]
public class GroupFollowerDeathTests
{
    private static Character Follower(string username = "beta") => new Character
    {
        Name1 = username, Name2 = "Beta", Class = CharacterClass.Warrior, Race = CharacterRace.Human, Level = 8,
        HP = 0, MaxHP = 80, BaseMaxHP = 80, Fame = 5, Resurrections = 3, MaxResurrections = 3,
        GroupPlayerUsername = username, IsAwaitingCombatInput = true,
    };

    [Fact]
    public void Mark_RecordsTheKiller_AndStopsWaitingForInput()
    {
        var f = Follower();
        GroupFollowerDeath.Mark(f, "Doom Engine");
        f.PendingGroupDeath.Should().Be("Doom Engine");
        f.IsAwaitingCombatInput.Should().BeFalse();
        f.HP.Should().Be(0, "the leader's fight must not heal or penalise the follower");
    }

    [Fact]
    public void Mark_WithNoKillerName_StillMarks()
    {
        var f = Follower();
        GroupFollowerDeath.Mark(f, "");
        f.PendingGroupDeath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Resolve_ClearsTheMark_CountsTheDeath_AndLeavesThePlayerAlive()
    {
        var f = Follower();
        f.PendingGroupDeath = "Doom Engine";
        var term = new TerminalEmulator(new System.IO.MemoryStream(), new System.IO.MemoryStream());
        bool alive = await GroupFollowerDeath.Resolve(f, term, "Doom Engine");
        alive.Should().BeTrue();
        f.PendingGroupDeath.Should().BeNull();
        f.PlaythroughDeaths.Should().Be(1);
        f.MDefeats.Should().Be(1);
        f.Fame.Should().Be(4);
        f.HP.Should().BeGreaterThan(0, "the follower leaves for the Temple alive");
    }

    [Fact]
    public void PendingGroupDeath_RoundTripsThroughTheSave()
    {
        var f = Follower();
        f.PendingGroupDeath = "Doom Engine";
        var serialize = typeof(SaveSystem).GetMethod("SerializePlayer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var data = (PlayerData)serialize.Invoke(SaveSystem.Instance, new object[] { f })!;
        var back = JsonSerializer.Deserialize<PlayerData>(JsonSerializer.Serialize(data))!;
        back.PendingGroupDeath.Should().Be("Doom Engine");
        var restore = typeof(GameEngine).GetMethod("RestorePlayerFromSaveData", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Character restored;
        try { restored = (Character)restore.Invoke(GameEngine.Instance, new object[] { back })!; }
        catch (TargetInvocationException ex) when (ex.InnerException != null) { throw ex.InnerException; }
        restored.PendingGroupDeath.Should().Be("Doom Engine");
        JsonSerializer.Deserialize<PlayerData>("{}")!.PendingGroupDeath.Should().BeNull("legacy saves carry no pending death");
    }
}
