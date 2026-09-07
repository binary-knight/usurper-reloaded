using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using UsurperRemake;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// v1.2: companions and NPC teammates look after themselves. Below 40 percent HP the ability
/// picker reaches for a defensive or evasive ability first; below 35 percent with nothing left
/// they brace (half damage for the round) instead of attacking; Defend clears at round end.
/// </summary>
[Collection("SharedGameSingletons")]
public class TeammateDefenseTests
{
    private static Character Teammate(long hp, long maxHp = 100) => new Character
    {
        Name2 = "Mira", Class = CharacterClass.Cleric, Race = CharacterRace.Elf, HP = hp, MaxHP = maxHp,
    };

    private static ClassAbilitySystem.ClassAbility Ability(string id, ClassAbilitySystem.AbilityType type, string effect = "", int defense = 0, int level = 1) =>
        new() { Id = id, Name = id, Type = type, SpecialEffect = effect, DefenseBonus = defense, LevelRequired = level };

    [Fact]
    public void DefensiveSelection_KeepsShieldsSidestepsAndDefensiveBuffs_DropsAttacks()
    {
        var pool = new List<ClassAbilitySystem.ClassAbility>
        {
            Ability("power_strike", ClassAbilitySystem.AbilityType.Attack),
            Ability("shield_wall", ClassAbilitySystem.AbilityType.Defense),
            Ability("evasive_roll", ClassAbilitySystem.AbilityType.Utility, effect: "dodge_next"),
            Ability("smoke_bomb", ClassAbilitySystem.AbilityType.Utility, effect: "smoke"),
            Ability("war_cry", ClassAbilitySystem.AbilityType.Buff, defense: 0),
            Ability("stone_skin", ClassAbilitySystem.AbilityType.Buff, defense: 8),
            Ability("hex", ClassAbilitySystem.AbilityType.Debuff),
        };
        CombatEngine.SelectDefensiveAbilities(pool).Select(a => a.Id)
            .Should().BeEquivalentTo(new[] { "shield_wall", "evasive_roll", "smoke_bomb", "stone_skin" });
    }

    [Fact]
    public void Defend_TriggersBelowTheThreshold_OnceOnly()
    {
        var engine = new CombatEngine();
        var tm = Teammate(30);
        engine.TryTeammateDefend(tm).Should().BeTrue();
        tm.IsDefending.Should().BeTrue();
        tm.ActiveStatuses.Should().ContainKey(StatusEffect.Defending);
        engine.TryTeammateDefend(tm).Should().BeFalse("already bracing this round");
    }

    [Fact]
    public void Defend_DoesNotTrigger_WhenHealthy()
    {
        var engine = new CombatEngine();
        engine.TryTeammateDefend(Teammate(50)).Should().BeFalse("half health is not desperate");
        engine.TryTeammateDefend(Teammate(35)).Should().BeFalse("the threshold is strictly below 35 percent");
        engine.TryTeammateDefend(Teammate(34)).Should().BeTrue();
    }

    [Fact]
    public void Defend_ClearsAtRoundEnd()
    {
        var engine = new CombatEngine();
        var tm = Teammate(20);
        engine.TryTeammateDefend(tm).Should().BeTrue();
        var field = typeof(CombatEngine).GetField("currentTeammates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(engine, new List<Character> { tm });
        var owner = new Character { Name2 = "Hero", Class = CharacterClass.Warrior, Race = CharacterRace.Human, HP = 100, MaxHP = 100 };
        typeof(CombatEngine).GetMethod("ProcessEndOfRoundAbilityEffects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(engine, new object[] { owner });
        tm.IsDefending.Should().BeFalse();
        tm.ActiveStatuses.Should().NotContainKey(StatusEffect.Defending);
    }
}
