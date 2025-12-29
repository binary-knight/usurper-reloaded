using System;
using System.Collections.Generic;

/// <summary>
/// Integration tests for combat spell casting system
/// Verifies that spells work correctly during combat encounters
/// </summary>
public static class CombatSpellIntegrationTest
{
    public static void RunAllTests()
    {
        Console.WriteLine("═══ Combat Spell Integration Tests ═══");
        Console.WriteLine();

        TestClericSpells();
        TestMagicianSpells();
        TestSageSpells();
        TestManaCosts();
        TestSpellEffects();
        TestLevelRequirements();

        Console.WriteLine();
        Console.WriteLine("═══ All Combat Spell Tests Passed! ═══");
    }

    private static void TestClericSpells()
    {
        Console.WriteLine("Testing Cleric Spells in Combat...");

        // Create a level 100 cleric with lots of mana
        var cleric = CreateTestCharacter(CharacterClass.Cleric, 100, 10000);
        var monster = CreateTestMonster("Test Orc", 100, 50);

        // Test healing spell (Cure Light - Level 1)
        var spell1 = SpellSystem.GetSpellInfo(CharacterClass.Cleric, 1);
        AssertNotNull(spell1, "Cure Light spell should exist");
        AssertEqual(spell1.Name, "Cure Light", "Spell name should match");
        AssertEqual(spell1.SpellType, "Heal", "Should be healing spell");

        // Test damage spell (Holy Explosion - Level 6)
        var spell6 = SpellSystem.GetSpellInfo(CharacterClass.Cleric, 6);
        AssertNotNull(spell6, "Holy Explosion spell should exist");
        AssertEqual(spell6.SpellType, "Attack", "Should be attack spell");
        AssertTrue(spell6.IsMultiTarget, "Holy Explosion should be multi-target");

        // Test buff spell (Armor - Level 2)
        var spell2 = SpellSystem.GetSpellInfo(CharacterClass.Cleric, 2);
        AssertNotNull(spell2, "Armor spell should exist");
        AssertEqual(spell2.SpellType, "Buff", "Should be buff spell");

        // Test ultimate spell (Gods Finger - Level 12)
        var spell12 = SpellSystem.GetSpellInfo(CharacterClass.Cleric, 12);
        AssertNotNull(spell12, "Gods Finger spell should exist");
        AssertEqual(spell12.SpellType, "Attack", "Should be attack spell");

        Console.WriteLine("✓ All Cleric spells verified");
        Console.WriteLine();
    }

    private static void TestMagicianSpells()
    {
        Console.WriteLine("Testing Magician Spells in Combat...");

        var magician = CreateTestCharacter(CharacterClass.Magician, 100, 10000);

        // Test attack spells
        var spells = new[] { 1, 7, 9, 11, 12 }; // Magic Missile, Fireball, Lightning, Pillar, Kill
        foreach (var level in spells)
        {
            var spell = SpellSystem.GetSpellInfo(CharacterClass.Magician, level);
            AssertNotNull(spell, $"Magician spell level {level} should exist");
            Console.WriteLine($"  ✓ {spell.Name} - {spell.Description.Substring(0, Math.Min(50, spell.Description.Length))}...");
        }

        // Test buff spells
        var buffSpells = new[] { 2, 5, 10 }; // Shield, Haste, Prismatic Cage
        foreach (var level in buffSpells)
        {
            var spell = SpellSystem.GetSpellInfo(CharacterClass.Magician, level);
            AssertEqual(spell.SpellType, "Buff", $"Magician spell {spell.Name} should be Buff type");
        }

        // Test debuff spells
        var debuffSpells = new[] { 3, 4, 8 }; // Sleep, Web, Fear
        foreach (var level in debuffSpells)
        {
            var spell = SpellSystem.GetSpellInfo(CharacterClass.Magician, level);
            AssertEqual(spell.SpellType, "Debuff", $"Magician spell {spell.Name} should be Debuff type");
        }

        // Test Haste spell (Level 5)
        var haste = SpellSystem.GetSpellInfo(CharacterClass.Magician, 5);
        AssertNotNull(haste, "Haste spell should exist");
        AssertEqual(haste.Name, "Haste", "Spell name should be Haste");
        AssertEqual(haste.MagicWords, "Quicksilvarie", "Magic words should match");

        Console.WriteLine("✓ All Magician spells verified");
        Console.WriteLine();
    }

    private static void TestSageSpells()
    {
        Console.WriteLine("Testing Sage Spells in Combat...");

        var sage = CreateTestCharacter(CharacterClass.Sage, 100, 10000);

        // Test all sage spells exist
        for (int i = 1; i <= 12; i++)
        {
            var spell = SpellSystem.GetSpellInfo(CharacterClass.Sage, i);
            AssertNotNull(spell, $"Sage spell level {i} should exist");
        }

        // Test Escape spell (Level 7)
        var escape = SpellSystem.GetSpellInfo(CharacterClass.Sage, 7);
        AssertNotNull(escape, "Escape spell should exist");
        AssertEqual(escape.Name, "Escape", "Spell name should be Escape");
        AssertEqual(escape.SpellType, "Escape", "Should be Escape type");

        // Test Giant spell (Level 8)
        var giant = SpellSystem.GetSpellInfo(CharacterClass.Sage, 8);
        AssertNotNull(giant, "Giant spell should exist");
        AssertEqual(giant.SpellType, "Buff", "Giant should be buff spell");

        // Test Steal spell (Level 9)
        var steal = SpellSystem.GetSpellInfo(CharacterClass.Sage, 9);
        AssertNotNull(steal, "Steal spell should exist");
        AssertEqual(steal.SpellType, "Special", "Steal should be special type");

        Console.WriteLine("✓ All Sage spells verified");
        Console.WriteLine();
    }

    private static void TestManaCosts()
    {
        Console.WriteLine("Testing Mana Cost Calculations...");

        // Create character with specific wisdom
        var wizard = CreateTestCharacter(CharacterClass.Magician, 50, 5000);
        wizard.Wisdom = 20; // High wisdom should reduce cost

        var spell = SpellSystem.GetSpellInfo(CharacterClass.Magician, 1);
        var manaCost = SpellSystem.CalculateManaCost(spell, wizard);

        // Formula: (level * 5) - ((Wisdom - 10) / 2)
        // For level 1: (1 * 5) - ((20 - 10) / 2) = 5 - 5 = 0, but minimum 1
        AssertTrue(manaCost >= 1, "Mana cost should have minimum of 1");

        Console.WriteLine($"  ✓ Level 1 spell with 20 wisdom costs {manaCost} mana");

        // Test low wisdom character
        var lowWis = CreateTestCharacter(CharacterClass.Cleric, 50, 5000);
        lowWis.Wisdom = 8; // Low wisdom increases cost

        var healSpell = SpellSystem.GetSpellInfo(CharacterClass.Cleric, 5);
        var highCost = SpellSystem.CalculateManaCost(healSpell, lowWis);

        // Formula: (5 * 5) - ((8 - 10) / 2) = 25 - (-1) = 26
        AssertTrue(highCost > 20, "Low wisdom should increase mana cost");

        Console.WriteLine($"  ✓ Level 5 spell with 8 wisdom costs {highCost} mana");

        Console.WriteLine("✓ Mana cost calculations correct");
        Console.WriteLine();
    }

    private static void TestSpellEffects()
    {
        Console.WriteLine("Testing Spell Effect Application...");

        var caster = CreateTestCharacter(CharacterClass.Cleric, 50, 5000);
        caster.HP = 50; // Wounded
        caster.MaxHP = 100;
        caster.Mana = 1000;

        var target = CreateTestMonster("Test Goblin", 80, 30);

        // Test healing spell
        long oldHP = caster.HP;
        var healResult = SpellSystem.CastSpell(caster, 1, target); // Cure Light
        AssertTrue(healResult.Success, "Spell cast should succeed");
        AssertTrue(healResult.Healing > 0, "Healing spell should heal");
        AssertTrue(healResult.Healing >= 4 && healResult.Healing <= 7, "Cure Light should heal 4-7 HP");

        Console.WriteLine($"  ✓ Cure Light healed {healResult.Healing} HP");

        // Test damage spell
        var damageResult = SpellSystem.CastSpell(caster, 6, target); // Holy Explosion
        AssertTrue(damageResult.Success, "Damage spell should succeed");
        AssertTrue(damageResult.Damage > 0, "Damage spell should deal damage");
        AssertTrue(damageResult.Damage >= 20 && damageResult.Damage <= 30, "Holy Explosion should deal 20-30 damage");
        AssertTrue(damageResult.IsMultiTarget, "Holy Explosion should be multi-target");

        Console.WriteLine($"  ✓ Holy Explosion dealt {damageResult.Damage} damage");

        // Test buff spell
        var buffResult = SpellSystem.CastSpell(caster, 2, target); // Armor
        AssertTrue(buffResult.Success, "Buff spell should succeed");
        AssertTrue(buffResult.ProtectionBonus > 0, "Buff should provide protection");
        AssertEqual(buffResult.ProtectionBonus, 5, "Armor should give +5 protection");
        AssertEqual(buffResult.Duration, 999, "Armor should last whole fight");

        Console.WriteLine($"  ✓ Armor granted +{buffResult.ProtectionBonus} AC for {buffResult.Duration} rounds");

        // Verify mana was deducted
        AssertTrue(caster.Mana < 1000, "Casting spells should consume mana");

        Console.WriteLine("✓ All spell effects working correctly");
        Console.WriteLine();
    }

    private static void TestLevelRequirements()
    {
        Console.WriteLine("Testing Spell Level Requirements...");

        // Test low-level character
        var lowLevel = CreateTestCharacter(CharacterClass.Magician, 1, 1000);
        lowLevel.Mana = 1000;

        // Should be able to cast level 1 spells
        AssertTrue(SpellSystem.CanCastSpell(lowLevel, 1), "Level 1 character should cast level 1 spells");

        // Should NOT be able to cast high-level spells
        AssertFalse(SpellSystem.CanCastSpell(lowLevel, 12), "Level 1 character should NOT cast level 12 spells");

        Console.WriteLine("  ✓ Level 1 character can cast level 1 spells");
        Console.WriteLine("  ✓ Level 1 character cannot cast level 12 spells");

        // Test mid-level character
        var midLevel = CreateTestCharacter(CharacterClass.Cleric, 30, 1000);
        midLevel.Mana = 1000;

        AssertTrue(SpellSystem.CanCastSpell(midLevel, 8), "Level 30 character should cast level 8 spells");
        AssertFalse(SpellSystem.CanCastSpell(midLevel, 11), "Level 30 character should NOT cast level 11 spells");

        Console.WriteLine("  ✓ Level 30 character can cast level 8 spells");
        Console.WriteLine("  ✓ Level 30 character cannot cast level 11 spells");

        // Test high-level character
        var highLevel = CreateTestCharacter(CharacterClass.Sage, 100, 1000);
        highLevel.Mana = 10000;

        AssertTrue(SpellSystem.CanCastSpell(highLevel, 12), "Level 100 character should cast all spells");

        Console.WriteLine("  ✓ Level 100 character can cast all spells");

        // Test non-spellcasting class
        var warrior = CreateTestCharacter(CharacterClass.Warrior, 50, 1000);
        warrior.Mana = 1000;

        AssertFalse(SpellSystem.CanCastSpell(warrior, 1), "Warrior should not be able to cast spells");

        Console.WriteLine("  ✓ Warriors cannot cast spells");

        Console.WriteLine("✓ Level requirements working correctly");
        Console.WriteLine();
    }

    // Helper methods
    private static Character CreateTestCharacter(CharacterClass charClass, int level, long mana)
    {
        var character = new Character
        {
            Name2 = "Test Character",
            Class = charClass,
            Level = level,
            Mana = mana,
            MaxMana = mana,
            HP = 100,
            MaxHP = 100,
            Strength = 50,
            Defence = 30,
            Wisdom = 15,
            Intelligence = 15,
            Gold = 10000,
            AI = CharacterAI.Human
        };
        return character;
    }

    private static Monster CreateTestMonster(string name, long hp, long strength)
    {
        var monster = new Monster
        {
            Name = name,
            HP = hp,
            MaxHP = hp,
            Strength = strength,
            Level = 10,
            Gold = 100,
            Experience = 50
        };
        return monster;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }

    private static void AssertFalse(bool condition, string message)
    {
        if (condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }

    private static void AssertEqual<T>(T actual, T expected, string message)
    {
        if (!actual.Equals(expected))
        {
            throw new Exception($"Assertion failed: {message}. Expected: {expected}, Got: {actual}");
        }
    }

    private static void AssertNotNull(object obj, string message)
    {
        if (obj == null)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }
}
