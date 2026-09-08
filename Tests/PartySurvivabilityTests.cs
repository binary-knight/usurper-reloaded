using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using UsurperRemake;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// v1.1.3 council rulings 4 and 5: the shared potion belt, the one-personal-potion rule, the
/// fight summary counters, the potion-statistics attribution fix, the pre-fight low-HP test,
/// the floor guard threshold, and the grouped-player Defend weight the supervisor flagged.
/// </summary>
[Collection("SharedGameSingletons")]
public class PartySurvivabilityTests
{
    private static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

    private static (CombatEngine engine, Character owner) Engine(int ownerPotions = 10)
    {
        var owner = new Character { Name2 = "Hero", Class = CharacterClass.Warrior, Race = CharacterRace.Human, Level = 20, HP = 600, MaxHP = 600, ArmPow = 60, Healing = ownerPotions };
        var engine = new CombatEngine(new TerminalEmulator(new MemoryStream(), new MemoryStream()));
        typeof(CombatEngine).GetField("_combatOwner", F)!.SetValue(engine, owner);
        typeof(CombatEngine).GetField("currentPlayer", F)!.SetValue(engine, owner);
        return (engine, owner);
    }

    private static Character Ally(int hp, int maxHp, int potions) =>
        new Character { Name2 = "Mira", ID = "npc-mira", Class = CharacterClass.Cleric, Race = CharacterRace.Elf, Level = 10, HP = hp, MaxHP = maxHp, Healing = potions };

    private static Task<bool> Heal(CombatEngine engine, Character ally, Character owner)
    {
        var result = new CombatResult { Player = owner, Monsters = new List<Monster>(), Teammates = new List<Character> { ally }, CombatLog = new List<string>() };
        var method = typeof(CombatEngine).GetMethod("TryTeammateHealAction", F)!;
        return (Task<bool>)method.Invoke(engine, new object[] { ally, new List<Character> { owner, ally }, result })!;
    }

    [Fact]
    public async Task Belt_OffByDefault_ThenTwoPerFight_NeverBelowThree()
    {
        var (engine, owner) = Engine(ownerPotions: 5);
        var ally = Ally(10, 100, potions: 0);
        new Character().SharedPotionBelt.Should().BeFalse("off by default");
        (await Heal(engine, ally, owner)).Should().BeFalse("belt off: nothing to drink");
        owner.Healing.Should().Be(5);

        owner.SharedPotionBelt = true;
        (await Heal(engine, ally, owner)).Should().BeTrue("first loan");
        owner.Healing.Should().Be(4);
        owner.PotionCooldownRounds.Should().Be(0, "the ally drank, not the player");
        ally.HP = 10;
        (await Heal(engine, ally, owner)).Should().BeTrue("second loan");
        owner.Healing.Should().Be(3);
        ally.HP = 10;
        (await Heal(engine, ally, owner)).Should().BeFalse("three is the reserve, and two is the per-fight cap");
        owner.Healing.Should().Be(3);
        owner.Statistics.TotalHealingPotionsUsed.Should().Be(0, "an ally's potion is not the player's statistic");
        var stats = engine.StatsFor(ally);
        stats.PotionsDrunk.Should().Be(2);
        stats.PotionsFromPlayer.Should().Be(2);
    }

    [Fact]
    public async Task Belt_CapResetsPerFight_AndOwnPotionsComeFirst()
    {
        var (engine, owner) = Engine(ownerPotions: 10);
        owner.SharedPotionBelt = true;
        var ally = Ally(10, 100, potions: 1);
        (await Heal(engine, ally, owner)).Should().BeTrue();
        ally.Healing.Should().Be(0, "own stock first");
        owner.Healing.Should().Be(10);
        ally.HP = 10;
        (await Heal(engine, ally, owner)).Should().BeTrue();
        ally.HP = 10;
        (await Heal(engine, ally, owner)).Should().BeTrue();
        ally.HP = 10;
        (await Heal(engine, ally, owner)).Should().BeFalse("two loans this fight");
        owner.Healing.Should().Be(8);
        typeof(CombatEngine).GetField("_borrowedThisFight", F)!.SetValue(engine, 0); // what a new fight does
        (await Heal(engine, ally, owner)).Should().BeTrue("a new fight, a new cap");
        owner.Healing.Should().Be(7);
    }

    [Fact]
    public async Task OnePersonalPotion_IsKeptUnlessThePlayerIsCritical()
    {
        var (engine, owner) = Engine();
        var ally = Ally(100, 100, potions: 1);
        owner.HP = 240; // 40 percent: the most injured, above the 30 percent line
        (await Heal(engine, ally, owner)).Should().BeFalse("one potion is kept for the ally's own emergency");
        ally.Healing.Should().Be(1);
        owner.HP = 120; // 20 percent
        (await Heal(engine, ally, owner)).Should().BeTrue("the player is about to die");
        ally.Healing.Should().Be(0);

        var (engine2, owner2) = Engine();
        var generous = Ally(100, 100, potions: 2);
        owner2.HP = 240;
        (await Heal(engine2, generous, owner2)).Should().BeTrue("with two potions the ally shares");
        owner2.Statistics.TotalHealingPotionsUsed.Should().Be(0);
    }

    [Fact]
    public async Task AllyPotion_IsNotThePlayersStatistic()
    {
        var (engine, owner) = Engine();
        var ally = Ally(10, 100, potions: 3);
        (await Heal(engine, ally, owner)).Should().BeTrue("emergency self-potion");
        ally.Healing.Should().Be(2);
        owner.Statistics.TotalHealingPotionsUsed.Should().Be(0, "it used to be recorded against the player");
        engine.StatsFor(ally).PotionsDrunk.Should().Be(1);
        engine.StatsFor(ally).PotionsFromPlayer.Should().Be(0);
    }

    [Fact]
    public void FightSummary_CountsHitsAndTargeting_AndSurvivesDeath()
    {
        var (engine, owner) = Engine();
        var ally = Ally(50, 100, potions: 0);
        engine.StatsFor(ally).Targeted++;
        engine.RecordAllyHit(ally, 30);
        engine.RecordAllyHit(ally, 20);
        ally.HP = 0;
        var s = engine.StatsFor(ally);
        s.Targeted.Should().Be(1);
        s.HitsLanded.Should().Be(2);
        s.HpLost.Should().Be(50);
        var output = new MemoryStream();
        var terminal = new TerminalEmulator(new MemoryStream(), output);
        typeof(CombatEngine).GetField("terminal", F)!.SetValue(engine, terminal);
        typeof(CombatEngine).GetField("currentTeammates", F)!.SetValue(engine, new List<Character>());
        engine.PrintPartyFightSummary(new CombatResult { Player = owner, CombatLog = new List<string>() });
        terminal.StreamWriterInternal!.Flush();
        var text = System.Text.Encoding.UTF8.GetString(output.ToArray());
        text.Should().Contain("Mira").And.Contain("0/100", "the dead ally is still reported");
    }

    [Fact]
    public void BossChannel_CountsAsAHit_NotAsTargeting_AndNeverOverkill()
    {
        var (engine, owner) = Engine();
        var sturdy = Ally(100, 100, potions: 0);
        var frail = Ally(5, 100, potions: 0);
        var boss = new Monster { Name = "Old One", Level = 30, HP = 1000, MaxHP = 1000, Strength = 20, IsBoss = true, IsChanneling = true, ChannelingRoundsLeft = 1, ChannelingAbilityName = "Doom" };
        var result = new CombatResult { Player = owner, Monsters = new List<Monster> { boss }, Teammates = new List<Character> { sturdy, frail }, CombatLog = new List<string>() };
        typeof(CombatEngine).GetMethod("ProcessBossChannel", F)!.Invoke(engine, new object[] { boss, owner, result });
        sturdy.HP.Should().BeLessThan(100, "the channel landed");
        var s = engine.StatsFor(sturdy);
        s.HitsLanded.Should().Be(1);
        s.HpLost.Should().Be(100 - sturdy.HP);
        s.Targeted.Should().Be(0, "an area effect is not a targeting choice");
        frail.HP.Should().Be(0);
        engine.StatsFor(frail).HpLost.Should().Be(5, "overkill is not HP lost");
    }

    [Fact]
    public void GroupedPlayerDefend_KeepsThePlus40_AndNoMultiplier()
    {
        var (engine, owner) = Engine();
        var grouped = new Character { Name2 = "Bran", Class = CharacterClass.Paladin, Race = CharacterRace.Human, HP = 100, MaxHP = 100, ArmPow = 60, IsDefending = true,
            RemoteTerminal = new TerminalEmulator(new MemoryStream(), new MemoryStream()) };
        grouped.IsGroupedPlayer.Should().BeTrue();
        int weight = (int)typeof(CombatEngine).GetMethod("GetTargetWeight", F)!.Invoke(engine, new object[] { grouped })!;
        weight.Should().Be(250, "180 + 30 armour + 40 for a player's own Defend");
    }

    [Fact]
    public void FloorGuardAndLowHp_Thresholds()
    {
        var player = new Character { Level = 30 };
        DungeonLocation.IsOutleveled(player, new Character { Level = 20 }).Should().BeFalse("ten levels is fine");
        DungeonLocation.IsOutleveled(player, new Character { Level = 19 }).Should().BeTrue("eleven is the line");
        var low = Ally(29, 100, 0);
        var fine = Ally(30, 100, 0);
        var grouped = Ally(5, 100, 0); grouped.RemoteTerminal = new TerminalEmulator(new MemoryStream(), new MemoryStream());
        DungeonLocation.LowHealthAllies(new[] { low, fine, grouped }).Should().ContainSingle().Which.Should().BeSameAs(low);
    }

    [Fact]
    public void Belt_RoundTripsThroughTheSave()
    {
        var owner = new Character { Name1 = "hero", Name2 = "Hero", Class = CharacterClass.Warrior, Race = CharacterRace.Human, Level = 5, HP = 50, MaxHP = 50, BaseMaxHP = 50, SharedPotionBelt = true };
        var data = (PlayerData)typeof(SaveSystem).GetMethod("SerializePlayer", F)!.Invoke(SaveSystem.Instance, new object[] { owner })!;
        var back = JsonSerializer.Deserialize<PlayerData>(JsonSerializer.Serialize(data))!;
        back.SharedPotionBelt.Should().BeTrue();
        Character restored;
        try { restored = (Character)typeof(GameEngine).GetMethod("RestorePlayerFromSaveData", F)!.Invoke(GameEngine.Instance, new object[] { back })!; }
        catch (TargetInvocationException ex) when (ex.InnerException != null) { throw ex.InnerException; }
        restored.SharedPotionBelt.Should().BeTrue();
        JsonSerializer.Deserialize<PlayerData>("{}")!.SharedPotionBelt.Should().BeFalse("old saves: off");
    }
}
