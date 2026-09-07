using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using UsurperRemake;
using UsurperRemake.Data;
using UsurperRemake.Systems;
using Xunit;

namespace UsurperReborn.Tests;

/// <summary>
/// Design item H2 (issue #68): GameData/abilities.json and spells.json tune built-in numbers
/// by key. A file with any invalid entry is rejected whole; a valid one changes only the
/// fields it sets.
/// </summary>
[Collection("SharedGameSingletons")]
public class ModdableOverridesTests
{
    [Fact]
    public void Templates_CoverEveryBuiltIn_AndValidateClean()
    {
        var abilities = ClassAbilitySystem.ExportOverrideTemplate();
        var spells = SpellSystem.ExportOverrideTemplate();
        abilities.Count.Should().BeGreaterThan(100);
        spells.Count.Should().BeGreaterThan(50);
        abilities.Should().OnlyContain(a => ClassAbilitySystem.HasAbility(a.Id));
        spells.Should().OnlyContain(s => SpellSystem.HasSpell(s.Class, s.Level));
        OverrideValidation.ValidateAbilities(abilities, ClassAbilitySystem.HasAbility).Should().BeEmpty();
        OverrideValidation.ValidateSpells(spells, SpellSystem.HasSpell).Should().BeEmpty();
    }

    [Fact]
    public void Templates_RoundTripThroughTheLoaderJsonOptions()
    {
        var abilities = ClassAbilitySystem.ExportOverrideTemplate();
        var json = JsonSerializer.Serialize(abilities, GameDataLoader.JsonOptions);
        json.Should().Contain("\"cooldown\"", "the loader writes camelCase");
        var back = JsonSerializer.Deserialize<List<AbilityOverride>>(json, GameDataLoader.JsonOptions)!;
        back.Select(a => (a.Id, a.Cooldown)).Should().Equal(abilities.Select(a => (a.Id, a.Cooldown)));

        var spells = SpellSystem.ExportOverrideTemplate();
        var back2 = JsonSerializer.Deserialize<List<SpellOverride>>(JsonSerializer.Serialize(spells, GameDataLoader.JsonOptions), GameDataLoader.JsonOptions)!;
        back2.Select(s => (s.Class, s.Level, s.ManaCost)).Should().Equal(spells.Select(s => (s.Class, s.Level, s.ManaCost)));
    }

    [Fact]
    public void Validation_RejectsUnknownIds_Duplicates_AndOutOfRange()
    {
        string real = ClassAbilitySystem.ExportOverrideTemplate()[0].Id;
        var entries = new List<AbilityOverride>
        {
            new() { Id = "no_such_ability", Cooldown = 1 },
            new() { Id = real, Cooldown = 99 },
            new() { Id = real, StaminaCost = -1 },
            new() { Id = "" },
        };
        var errors = OverrideValidation.ValidateAbilities(entries, ClassAbilitySystem.HasAbility);
        errors.Should().Contain(e => e.Contains("no built-in ability"));
        errors.Should().Contain(e => e.Contains("cooldown must be 0 to 20"));
        errors.Should().Contain(e => e.Contains("duplicate id"));
        errors.Should().Contain(e => e.Contains("staminaCost must be"));
        errors.Should().Contain(e => e.Contains("id is required"));

        var spellErrors = OverrideValidation.ValidateSpells(new List<SpellOverride>
        {
            new() { Class = CharacterClass.Cleric, Level = 1, ManaCost = 0 },
            new() { Class = CharacterClass.Cleric, Level = 1, Name = "  " },
            new() { Class = CharacterClass.Cleric, Level = 999 },
        }, SpellSystem.HasSpell);
        spellErrors.Should().Contain(e => e.Contains("manaCost must be 1 to"));
        spellErrors.Should().Contain(e => e.Contains("duplicate class and level"));
        spellErrors.Should().Contain(e => e.Contains("name may not be blank"));
        spellErrors.Should().Contain(e => e.Contains("level must be 1 to 100"));
    }

    [Fact]
    public void ApplyOverrides_ChangesOnlyTheFieldsSet_AndOnlyTheNamedKeys()
    {
        var template = ClassAbilitySystem.ExportOverrideTemplate();
        var target = ClassAbilitySystem.GetAbility(template[0].Id)!;
        var other = ClassAbilitySystem.GetAbility(template[1].Id)!;
        int oldCooldown = target.Cooldown, oldStamina = target.StaminaCost, otherCooldown = other.Cooldown;
        try
        {
            int applied = ClassAbilitySystem.ApplyOverrides(new[] { new AbilityOverride { Id = target.Id, Cooldown = 9 } });
            applied.Should().Be(1);
            target.Cooldown.Should().Be(9);
            target.StaminaCost.Should().Be(oldStamina, "unset fields keep the built-in value");
            other.Cooldown.Should().Be(otherCooldown, "unnamed abilities are untouched");
        }
        finally { target.Cooldown = oldCooldown; }

        var spell = SpellSystem.GetSpellInfo(CharacterClass.Cleric, 1);
        int oldMana = spell.ManaCost; string oldName = spell.Name;
        try
        {
            SpellSystem.ApplyOverrides(new[] { new SpellOverride { Class = CharacterClass.Cleric, Level = 1, ManaCost = 7 } }).Should().Be(1);
            SpellSystem.GetSpellInfo(CharacterClass.Cleric, 1).ManaCost.Should().Be(7);
            SpellSystem.GetSpellInfo(CharacterClass.Cleric, 1).Name.Should().Be(oldName);
        }
        finally { spell.ManaCost = oldMana; }
    }
}
