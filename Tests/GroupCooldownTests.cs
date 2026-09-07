using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using UsurperRemake;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// Design item A: a grouped follower acts through the owner's action paths, so ability
/// cooldowns must be resolved per actor. The owner keeps the engine's own dictionary;
/// every other actor gets a dictionary keyed by username (grouped player) or display
/// name (NPC teammate).
/// </summary>
[Collection("SharedGameSingletons")]
public class GroupCooldownTests
{
    private static Character Warrior(string name, string? username = null) => new Character
    {
        Name2 = name, Class = CharacterClass.Warrior, Race = CharacterRace.Human, Level = 10,
        HP = 100, MaxHP = 100, GroupPlayerUsername = username,
    };

    private static (CombatEngine engine, Dictionary<string, int> ownerDict) Engine(Character owner)
    {
        var engine = new CombatEngine();
        typeof(CombatEngine).GetField("_combatOwner", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(engine, owner);
        var ownerDict = (Dictionary<string, int>)typeof(CombatEngine).GetField("abilityCooldowns", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(engine)!;
        return (engine, ownerDict);
    }

    [Fact]
    public void Owner_ResolvesToTheEngineDictionary()
    {
        var owner = Warrior("Alpha");
        var (engine, ownerDict) = Engine(owner);
        engine.CooldownsFor(owner).Should().BeSameAs(ownerDict);
    }

    [Fact]
    public void Follower_OfTheSameClass_HasItsOwnCooldowns()
    {
        var owner = Warrior("Alpha");
        var follower = Warrior("Beta", username: "beta");
        var (engine, ownerDict) = Engine(owner);

        ownerDict["power_strike"] = 3;
        var followerDict = engine.CooldownsFor(follower);

        followerDict.Should().NotBeSameAs(ownerDict);
        followerDict.Should().NotContainKey("power_strike", "the owner's cooldown must not block the follower");
        followerDict["power_strike"] = 2;
        ownerDict["power_strike"].Should().Be(3, "the follower's cooldown must not touch the owner's");
        engine.CooldownsFor(follower).Should().BeSameAs(followerDict, "the follower's dictionary is stable across turns");
    }

    [Fact]
    public void Followers_WithTheSameDisplayName_AreKeyedByUsername()
    {
        var owner = Warrior("Alpha");
        var (engine, _) = Engine(owner);
        var one = Warrior("Twin", username: "twin1");
        var two = Warrior("Twin", username: "twin2");
        engine.CooldownsFor(one).Should().NotBeSameAs(engine.CooldownsFor(two));
    }

    [Fact]
    public void NoOwnerSet_FallsBackToTheEngineDictionary()
    {
        var engine = new CombatEngine();
        var ownerDict = (Dictionary<string, int>)typeof(CombatEngine).GetField("abilityCooldowns", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(engine)!;
        engine.CooldownsFor(Warrior("Anyone")).Should().BeSameAs(ownerDict);
    }
}
