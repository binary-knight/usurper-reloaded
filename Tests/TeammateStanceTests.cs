using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using UsurperRemake;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// v1.1.3 council ruling 1: stances. Balanced is exactly PR #134; Aggressive and Cautious
/// bracket it; a Cautious ally never taunts; the wounded-target bonus is gone from ordinary
/// weighting; the +40 defending weight belongs to the player and Aggressive allies only;
/// Cautious weight is x0.70 before the floor.
/// </summary>
[Collection("SharedGameSingletons")]
public class TeammateStanceTests
{
    private static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

    [Fact]
    public void Balanced_IsExactlyWhatPr134Shipped()
    {
        var b = TeammateStances.PolicyFor(TeammateStance.Balanced);
        b.EmergencySelfPotion.Should().Be(GameConfig.TeammateEmergencySelfHealHpPercent).And.Be(0.30);
        b.DefensiveFirst.Should().Be(GameConfig.TeammateDefensivePriorityHpPercent).And.Be(0.40);
        b.Brace.Should().Be(GameConfig.TeammateDefendHpPercent).And.Be(0.35);
        b.PotionMostInjured.Should().Be(0.50);
        b.MayTaunt.Should().BeTrue();
        b.TargetWeightMultiplier.Should().Be(1.0);
    }

    [Theory]
    [InlineData(TeammateStance.Aggressive)]
    [InlineData(TeammateStance.Balanced)]
    [InlineData(TeammateStance.Cautious)]
    public void EveryColumn_KeepsTheOrdering(TeammateStance stance)
    {
        var p = TeammateStances.PolicyFor(stance);
        p.EmergencySelfPotion.Should().BeLessThan(p.Brace);
        p.Brace.Should().BeLessThan(p.DefensiveFirst);
        p.DefensiveFirst.Should().BeLessThan(p.PotionMostInjured);
    }

    [Fact]
    public void MissingUnknownOrExcluded_IsBalanced()
    {
        var owner = new Character { Name2 = "Hero" };
        var ally = new Character { Name2 = "Mira", ID = "npc-mira" };
        TeammateStances.Get(owner, ally).Should().Be(TeammateStance.Balanced);
        owner.TeammateStances[TeammateStances.KeyFor(ally)] = 99;
        TeammateStances.Get(owner, ally).Should().Be(TeammateStance.Balanced, "an unknown value is Balanced");
        TeammateStances.Set(owner, TeammateStances.KeyFor(ally), TeammateStance.Cautious);
        TeammateStances.Get(owner, ally).Should().Be(TeammateStance.Cautious);
        ally.IsMercenary = true;
        TeammateStances.Get(owner, ally).Should().Be(TeammateStance.Balanced, "mercenaries take no orders");
    }

    [Fact]
    public void Stances_RoundTripThroughTheSave()
    {
        var owner = new Character { Name1 = "hero", Name2 = "Hero", Class = CharacterClass.Warrior, Race = CharacterRace.Human, Level = 5, HP = 50, MaxHP = 50, BaseMaxHP = 50 };
        owner.TeammateStances["npc-mira"] = (int)TeammateStance.Cautious;
        var data = (PlayerData)typeof(SaveSystem).GetMethod("SerializePlayer", F)!.Invoke(SaveSystem.Instance, new object[] { owner })!;
        var back = JsonSerializer.Deserialize<PlayerData>(JsonSerializer.Serialize(data))!;
        back.TeammateStances.Should().ContainKey("npc-mira").WhoseValue.Should().Be((int)TeammateStance.Cautious);
        Character restored;
        try { restored = (Character)typeof(GameEngine).GetMethod("RestorePlayerFromSaveData", F)!.Invoke(GameEngine.Instance, new object[] { back })!; }
        catch (TargetInvocationException ex) when (ex.InnerException != null) { throw ex.InnerException; }
        restored.TeammateStances.Should().ContainKey("npc-mira");
        JsonSerializer.Deserialize<PlayerData>("{}")!.TeammateStances.Should().BeEmpty();
    }

    private static (CombatEngine engine, Character owner) Engine()
    {
        var owner = new Character { Name2 = "Hero", Class = CharacterClass.Warrior, Race = CharacterRace.Human, Level = 20, HP = 600, MaxHP = 600, ArmPow = 60 };
        var engine = new CombatEngine(new TerminalEmulator(new System.IO.MemoryStream(), new System.IO.MemoryStream()));
        typeof(CombatEngine).GetField("_combatOwner", F)!.SetValue(engine, owner);
        typeof(CombatEngine).GetField("currentPlayer", F)!.SetValue(engine, owner);
        return (engine, owner);
    }

    private static int Weight(CombatEngine engine, Character c) =>
        (int)typeof(CombatEngine).GetMethod("GetTargetWeight", F)!.Invoke(engine, new object[] { c })!;

    [Fact]
    public void WoundedBracedCasterAlly_IsNoLongerTheLikeliestTargetInTheRoom()
    {
        // Cleric 80, light armour -20, small max HP -20 = 40. Before: +25 wounded +40 defend = 105,
        // against the tank player's 210 and a healthy Warrior ally's 210.
        var (engine, owner) = Engine();
        var ally = new Character { Name2 = "Mira", ID = "npc-mira", Class = CharacterClass.Cleric, Race = CharacterRace.Elf, HP = 20, MaxHP = 90, ArmPow = 10, IsDefending = true };
        Weight(engine, ally).Should().Be(40, "no wounded bonus, and a Balanced brace adds nothing");
        Weight(engine, owner).Should().Be(210, "the player's weight is unchanged when not defending");
        owner.IsDefending = true;
        Weight(engine, owner).Should().Be(250, "the player's own Defend still pulls hits");
    }

    [Fact]
    public void AggressiveBrace_KeepsThePlus40_CautiousIsSeventyPercent()
    {
        var (engine, owner) = Engine();
        var ally = new Character { Name2 = "Aldric", ID = "npc-aldric", Class = CharacterClass.Paladin, Race = CharacterRace.Human, HP = 100, MaxHP = 100, ArmPow = 60, IsDefending = true };
        Weight(engine, ally).Should().Be(210, "Balanced: 180 + 30 armour, no defending bonus");
        TeammateStances.Set(owner, TeammateStances.KeyFor(ally), TeammateStance.Aggressive);
        Weight(engine, ally).Should().Be(250, "Aggressive keeps the +40 when defending");
        ally.IsDefending = false;
        TeammateStances.Set(owner, TeammateStances.KeyFor(ally), TeammateStance.Cautious);
        Weight(engine, ally).Should().Be(147, "Cautious is x0.70 of 210, applied before the floor");
    }

    [Fact]
    public async System.Threading.Tasks.Task CautiousTank_NeverTaunts()
    {
        var (engine, owner) = Engine();
        var tank = new Character { Name2 = "Aldric", ID = "npc-aldric", Class = CharacterClass.Paladin, Race = CharacterRace.Human, Level = 20, HP = 100, MaxHP = 100, CurrentCombatStamina = 100 };
        tank.EquippedItems[EquipmentSlot.OffHand] = EquipmentDatabase.GetShields().First().Id;
        typeof(CombatEngine).GetField("currentTeammates", F)!.SetValue(engine, new List<Character> { tank });
        var method = typeof(CombatEngine).GetMethod("TryTeammateClassAbility", F)!;
        var cooldowns = (Dictionary<string, Dictionary<string, int>>)typeof(CombatEngine).GetField("teammateCooldowns", F)!.GetValue(engine)!;
        async System.Threading.Tasks.Task<bool> Taunted()
        {
            var monsters = new List<Monster> { new Monster { Name = "Ogre", Level = 15, HP = 400, MaxHP = 400, Strength = 30 } };
            var result = new CombatResult { Player = owner, Monsters = monsters, Teammates = new List<Character> { tank }, CombatLog = new List<string>() };
            bool any = false;
            for (int round = 0; round < 8 && !any; round++)
            {
                tank.CurrentCombatStamina = 100; cooldowns.Clear();
                foreach (var m in monsters) { m.TauntedBy = null; m.TauntRoundsLeft = 0; }
                await (System.Threading.Tasks.Task<bool>)method.Invoke(engine, new object[] { tank, monsters, result })!;
                any = cooldowns.Values.SelectMany(d => d.Keys).Contains("thundering_roar") || monsters.Any(m => !string.IsNullOrEmpty(m.TauntedBy));
            }
            return any;
        }
        (await Taunted()).Should().BeTrue("control: a Balanced tank with nothing taunted taunts");
        TeammateStances.Set(owner, TeammateStances.KeyFor(tank), TeammateStance.Cautious);
        (await Taunted()).Should().BeFalse("a Cautious tank never starts a taunt");
    }

    [Fact]
    public void Brace_UsesTheStanceThreshold()
    {
        var (engine, owner) = Engine();
        var ally = new Character { Name2 = "Mira", ID = "npc-mira", Class = CharacterClass.Cleric, Race = CharacterRace.Elf, HP = 45, MaxHP = 100 };
        engine.TryTeammateDefend(ally).Should().BeFalse("Balanced braces below 35");
        TeammateStances.Set(owner, TeammateStances.KeyFor(ally), TeammateStance.Cautious);
        engine.TryTeammateDefend(ally).Should().BeTrue("Cautious braces below 50");
    }
}
