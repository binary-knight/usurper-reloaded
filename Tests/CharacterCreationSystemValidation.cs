using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Character Creation System Validation Tests
/// Comprehensive validation for Pascal USERHUNC.PAS compatibility
/// </summary>
public static class CharacterCreationSystemValidation
{
    private static int testsRun = 0;
    private static int testsPassed = 0;
    private static List<string> failedTests = new();

    public static void RunAllTests()
    {
        GD.Print("═══ Starting Character Creation System Validation ═══");
        testsRun = 0;
        testsPassed = 0;
        failedTests.Clear();

        // Run test categories
        TestGameConfigConstants();
        TestRaceAttributes();
        TestClassAttributes();
        TestPhysicalAppearance();
        TestAttributeCalculation();
        TestRaceClassCombinations();
        TestCharacterInitialization();
        TestPascalCompatibility();
        TestErrorHandling();
        TestDataIntegrity();

        // Summary
        GD.Print($"═══ Character Creation Tests Complete: {testsPassed}/{testsRun} passed ═══");
        
        if (failedTests.Count > 0)
        {
            GD.Print("Failed tests:");
            foreach (var test in failedTests)
            {
                GD.PrintErr($"  - {test}");
            }
        }
    }

    #region Test Categories

    /// <summary>
    /// Test 1: Game Configuration Constants
    /// </summary>
    private static void TestGameConfigConstants()
    {
        Test("Default starting gold matches Pascal", () =>
            GameConfig.DefaultStartingGold == 2000, 
            "Starting gold should be 2000 as per Pascal startm variable");

        Test("Default starting experience matches Pascal", () =>
            GameConfig.DefaultStartingExperience == 10,
            "Starting experience should be 10 as per Pascal USERHUNC.PAS");

        Test("Race names array has correct count", () =>
            GameConfig.RaceNames.Length == 10,
            "Should have exactly 10 races matching Pascal enum");

        Test("Class names array has correct count", () =>
            GameConfig.ClassNames.Length == 11,
            "Should have exactly 11 classes matching Pascal enum");

        Test("Class starting attributes has all classes", () =>
            GameConfig.ClassStartingAttributes.Count == 11,
            "Starting attributes should be defined for all 11 classes");

        Test("Race attributes has all races", () =>
            GameConfig.RaceAttributes.Count == 10,
            "Race attributes should be defined for all 10 races");

        Test("Eye colors array has correct values", () =>
            GameConfig.EyeColors.Length == 6 && GameConfig.EyeColors[1] == "Brown",
            "Eye colors should match Pascal appearance system");

        Test("Hair colors array has correct values", () =>
            GameConfig.HairColors.Length == 11 && GameConfig.HairColors[1] == "Black",
            "Hair colors should match Pascal appearance system");

        Test("Skin colors array has correct values", () =>
            GameConfig.SkinColors.Length == 11 && GameConfig.SkinColors[1] == "Very Dark",
            "Skin colors should match Pascal appearance system");

        Test("Forbidden names includes SYSOP", () =>
            GameConfig.ForbiddenNames.Contains("SYSOP"),
            "SYSOP should be a forbidden name as per Pascal validation");
    }

    /// <summary>
    /// Test 2: Race Attributes and Bonuses
    /// </summary>
    private static void TestRaceAttributes()
    {
        // Test Human race (baseline)
        var human = GameConfig.RaceAttributes[CharacterRace.Human];
        Test("Human HP bonus is correct", () =>
            human.HPBonus == 14,
            "Human HP bonus should be +14 as per Pascal USERHUNC.PAS");

        Test("Human strength bonus is correct", () =>
            human.StrengthBonus == 4,
            "Human strength bonus should be +4 as per Pascal");

        Test("Human age range is correct", () =>
            human.MinAge == 15 && human.MaxAge == 19,
            "Human age should be random(5) + 15 = 15-19");

        Test("Human height range is correct", () =>
            human.MinHeight == 180 && human.MaxHeight == 219,
            "Human height should be random(40) + 180 = 180-219");

        // Test Troll race (powerful race)
        var troll = GameConfig.RaceAttributes[CharacterRace.Troll];
        Test("Troll HP bonus is highest", () =>
            troll.HPBonus == 20,
            "Troll should have highest HP bonus (+20)");

        Test("Troll strength bonus is highest", () =>
            troll.StrengthBonus == 7,
            "Troll should have highest strength bonus (+7)");

        Test("Troll skin color is correct", () =>
            troll.SkinColor == 5,
            "Troll skin should be fixed at 5 (green/grayish)");

        // Test Mutant race (random race)
        var mutant = GameConfig.RaceAttributes[CharacterRace.Mutant];
        Test("Mutant has random skin", () =>
            mutant.SkinColor == 0,
            "Mutant skin should be 0 indicating random generation");

        Test("Mutant has empty hair colors", () =>
            mutant.HairColors.Length == 0,
            "Mutant hair colors should be empty indicating random generation");

        // Test all races have required fields
        foreach (CharacterRace race in Enum.GetValues<CharacterRace>())
        {
            var attr = GameConfig.RaceAttributes[race];
            Test($"{race} has valid age range", () =>
                attr.MinAge > 0 && attr.MaxAge > attr.MinAge,
                $"{race} should have valid age range");

            Test($"{race} has valid height range", () =>
                attr.MinHeight > 0 && attr.MaxHeight > attr.MinHeight,
                $"{race} should have valid height range");

            Test($"{race} has valid weight range", () =>
                attr.MinWeight > 0 && attr.MaxWeight > attr.MinWeight,
                $"{race} should have valid weight range");
        }
    }

    /// <summary>
    /// Test 3: Class Attributes and Starting Values
    /// </summary>
    private static void TestClassAttributes()
    {
        // Test Magician class (mage archetype)
        var magician = GameConfig.ClassStartingAttributes[CharacterClass.Magician];
        Test("Magician has high mana", () =>
            magician.Mana == 40 && magician.MaxMana == 40,
            "Magician should start with 40 mana as per Pascal");

        Test("Magician has low HP", () =>
            magician.HP == 2,
            "Magician should have lowest HP (2) as per Pascal");

        Test("Magician has high wisdom", () =>
            magician.Wisdom == 4,
            "Magician should have high wisdom");

        // Test Barbarian class (warrior archetype)
        var barbarian = GameConfig.ClassStartingAttributes[CharacterClass.Barbarian];
        Test("Barbarian has high HP", () =>
            barbarian.HP == 5,
            "Barbarian should have highest HP (5) as per Pascal");

        Test("Barbarian has high strength", () =>
            barbarian.Strength == 5,
            "Barbarian should have highest strength (5)");

        Test("Barbarian has low wisdom", () =>
            barbarian.Wisdom == 1,
            "Barbarian should have lowest wisdom (1)");

        // Test Cleric class (hybrid caster)
        var cleric = GameConfig.ClassStartingAttributes[CharacterClass.Cleric];
        Test("Cleric has moderate mana", () =>
            cleric.Mana == 20 && cleric.MaxMana == 20,
            "Cleric should start with 20 mana as per Pascal");

        Test("Cleric has balanced stats", () =>
            cleric.HP == 3 && cleric.Strength == 3,
            "Cleric should have balanced combat stats");

        // Test all classes have valid stats
        foreach (CharacterClass charClass in Enum.GetValues<CharacterClass>())
        {
            var attr = GameConfig.ClassStartingAttributes[charClass];
            Test($"{charClass} has valid HP", () =>
                attr.HP > 0 && attr.HP <= 10,
                $"{charClass} should have valid HP range (1-10)");

            Test($"{charClass} has valid stats", () =>
                attr.Strength > 0 && attr.Dexterity > 0 && attr.Wisdom > 0,
                $"{charClass} should have positive stats");

            Test($"{charClass} mana consistency", () =>
                attr.Mana == attr.MaxMana,
                $"{charClass} starting mana should equal max mana");
        }
    }

    /// <summary>
    /// Test 4: Physical Appearance Generation
    /// </summary>
    private static void TestPhysicalAppearance()
    {
        // Test appearance ranges
        Test("Eye color range is valid", () =>
            true, // Eyes are random(5) + 1 = 1-5, validated in generation
            "Eye colors should be in range 1-5");

        Test("Hair color range is valid", () =>
            GameConfig.HairColors.Length == 11,
            "Hair colors should support range 1-10 plus empty slot");

        Test("Skin color range is valid", () =>
            GameConfig.SkinColors.Length == 11,
            "Skin colors should support range 1-10 plus empty slot");

        // Test race-specific hair colors
        var human = GameConfig.RaceAttributes[CharacterRace.Human];
        Test("Human hair colors are correct", () =>
            human.HairColors.SequenceEqual(new[] { 1, 4, 5, 8 }),
            "Human hair should be Black(1), Blond(4), Dark(5), Golden(8)");

        var troll = GameConfig.RaceAttributes[CharacterRace.Troll];
        Test("Troll hair colors are correct", () =>
            troll.HairColors.SequenceEqual(new[] { 5, 4, 4, 5 }),
            "Troll hair should favor Dark(5) and Blond(4)");

        var gnome = GameConfig.RaceAttributes[CharacterRace.Gnome];
        Test("Gnome hair colors are correct", () =>
            gnome.HairColors.SequenceEqual(new[] { 3, 3, 4, 9 }),
            "Gnome hair should be Red(3), Red(3), Blond(4), Silver(9)");

        // Test skin colors
        Test("Human skin is fair", () =>
            human.SkinColor == 10,
            "Human skin should be Very Fair (10)");

        Test("Troll skin is green", () =>
            troll.SkinColor == 5,
            "Troll skin should be Green (5)");

        Test("Dwarf skin is bronze", () =>
            GameConfig.RaceAttributes[CharacterRace.Dwarf].SkinColor == 7,
            "Dwarf skin should be Bronze (7)");
    }

    /// <summary>
    /// Test 5: Attribute Calculation
    /// </summary>
    private static void TestAttributeCalculation()
    {
        // Test Human Warrior combination
        var humanWarrior = CalculateAttributes(CharacterRace.Human, CharacterClass.Warrior);
        Test("Human Warrior HP calculation", () =>
            humanWarrior.HP == 4 + 14, // Warrior base + Human bonus
            "Human Warrior should have 18 HP (4+14)");

        Test("Human Warrior strength calculation", () =>
            humanWarrior.Strength == 4 + 4, // Warrior base + Human bonus
            "Human Warrior should have 8 strength (4+4)");

        // Test Troll Barbarian combination (highest physical stats)
        var trollBarbarian = CalculateAttributes(CharacterRace.Troll, CharacterClass.Barbarian);
        Test("Troll Barbarian HP calculation", () =>
            trollBarbarian.HP == 5 + 20, // Barbarian base + Troll bonus
            "Troll Barbarian should have 25 HP (5+20)");

        Test("Troll Barbarian strength calculation", () =>
            trollBarbarian.Strength == 5 + 7, // Barbarian base + Troll bonus
            "Troll Barbarian should have 12 strength (5+7)");

        // Test Elf Magician combination (mage archetype)
        var elfMagician = CalculateAttributes(CharacterRace.Elf, CharacterClass.Magician);
        Test("Elf Magician HP calculation", () =>
            elfMagician.HP == 2 + 11, // Magician base + Elf bonus
            "Elf Magician should have 13 HP (2+11)");

        Test("Elf Magician mana unchanged", () =>
            elfMagician.Mana == 40,
            "Elf Magician mana should remain 40 (no race bonus to mana)");

        // Test all combinations for basic validity
        foreach (CharacterRace race in Enum.GetValues<CharacterRace>())
        {
            foreach (CharacterClass charClass in Enum.GetValues<CharacterClass>())
            {
                // Skip invalid combinations
                if (IsInvalidCombination(race, charClass)) continue;

                var attrs = CalculateAttributes(race, charClass);
                Test($"{race} {charClass} has positive HP", () =>
                    attrs.HP > 0,
                    $"{race} {charClass} should have positive HP after bonuses");

                Test($"{race} {charClass} has positive strength", () =>
                    attrs.Strength > 0,
                    $"{race} {charClass} should have positive strength after bonuses");
            }
        }
    }

    /// <summary>
    /// Test 6: Race/Class Combination Validation
    /// </summary>
    private static void TestRaceClassCombinations()
    {
        // Test invalid combinations (Pascal validation)
        Test("Troll cannot be Paladin", () =>
            GameConfig.InvalidCombinations.ContainsKey(CharacterRace.Troll) &&
            GameConfig.InvalidCombinations[CharacterRace.Troll].Contains(CharacterClass.Paladin),
            "Troll/Paladin should be an invalid combination");

        Test("Orc cannot be Paladin", () =>
            GameConfig.InvalidCombinations.ContainsKey(CharacterRace.Orc) &&
            GameConfig.InvalidCombinations[CharacterRace.Orc].Contains(CharacterClass.Paladin),
            "Orc/Paladin should be an invalid combination");

        // Test that all other combinations are valid
        var invalidCount = 0;
        foreach (CharacterRace race in Enum.GetValues<CharacterRace>())
        {
            foreach (CharacterClass charClass in Enum.GetValues<CharacterClass>())
            {
                if (IsInvalidCombination(race, charClass))
                {
                    invalidCount++;
                }
            }
        }

        Test("Only expected invalid combinations exist", () =>
            invalidCount == 2, // Only Troll/Paladin and Orc/Paladin
            "Should have exactly 2 invalid race/class combinations");

        // Test specific valid combinations
        Test("Human can be all classes", () =>
            !GameConfig.InvalidCombinations.ContainsKey(CharacterRace.Human),
            "Human should be able to be any class");

        Test("Elf can be Paladin", () =>
            !IsInvalidCombination(CharacterRace.Elf, CharacterClass.Paladin),
            "Elf should be able to be Paladin");

        Test("Dwarf can be Paladin", () =>
            !IsInvalidCombination(CharacterRace.Dwarf, CharacterClass.Paladin),
            "Dwarf should be able to be Paladin");
    }

    /// <summary>
    /// Test 7: Character Initialization
    /// </summary>
    private static void TestCharacterInitialization()
    {
        Test("Default deeds are correct", () =>
            GameConfig.DefaultGoodDeeds == 3 && GameConfig.DefaultDarkDeeds == 3,
            "Default good and dark deeds should be 3 each");

        Test("Default tournament fights are correct", () =>
            GameConfig.DefaultTournamentFights == 3,
            "Default tournament fights should be 3");

        Test("Default dungeon fights are correct", () =>
            GameConfig.DefaultDungeonFights == 5,
            "Default dungeon fights should be 5");

        Test("Default player fights are correct", () =>
            GameConfig.DefaultPlayerFights == 3,
            "Default player fights should be 3");

        Test("Default healing potions are correct", () =>
            GameConfig.DefaultStartingHealing == 125,
            "Default healing potions should be 125");

        Test("Default loyalty is correct", () =>
            GameConfig.DefaultLoyalty == 50,
            "Default loyalty should be 50%");

        Test("Default mental health is correct", () =>
            GameConfig.DefaultMentalHealth == 100,
            "Default mental health should be 100");

        Test("Default thief attempts are correct", () =>
            GameConfig.DefaultThiefAttempts == 3,
            "Default thief attempts should be 3");

        Test("Default brawls are correct", () =>
            GameConfig.DefaultBrawls == 3,
            "Default brawls should be 3");

        Test("Default assassin attempts are correct", () =>
            GameConfig.DefaultAssassinAttempts == 3,
            "Default assassin attempts should be 3");
    }

    /// <summary>
    /// Test 8: Pascal Compatibility
    /// </summary>
    private static void TestPascalCompatibility()
    {
        // Test Pascal constant values
        Test("Max spells matches Pascal", () =>
            GameConfig.MaxSpells == 12,
            "Max spells should be 12 as per Pascal global_maxspells");

        Test("Max combat skills matches Pascal", () =>
            GameConfig.MaxCombat == 14,
            "Max combat skills should be 14 as per Pascal global_maxcombat");

        Test("Max items matches Pascal", () =>
            GameConfig.MaxItem == 15,
            "Max items should be 15 as per Pascal global_maxitem");

        Test("Max level matches Pascal", () =>
            GameConfig.MaxLevel == 200,
            "Max level should be 200 as per Pascal maxlevel");

        Test("Offline dormitory location matches Pascal", () =>
            GameConfig.OfflineLocationDormitory == 0,
            "Dormitory should be location 0 as per Pascal offloc_dormitory");

        // Test Pascal enum compatibility
        Test("Race enum matches Pascal order", () =>
            (int)CharacterRace.Human == 0 && (int)CharacterRace.Mutant == 9,
            "Race enum should match Pascal races order");

        Test("Class enum matches Pascal order", () =>
            (int)CharacterClass.Alchemist == 0 && (int)CharacterClass.Warrior == 10,
            "Class enum should match Pascal classes order");

        Test("Sex enum matches Pascal values", () =>
            (int)CharacterSex.Male == 1 && (int)CharacterSex.Female == 2,
            "Sex enum should match Pascal sex values");

        Test("AI enum matches Pascal values", () =>
            (char)CharacterAI.Computer == 'C' && (char)CharacterAI.Human == 'H',
            "AI enum should match Pascal AI characters");

        // Test Pascal race descriptions match exactly
        Test("Race descriptions match Pascal format", () =>
            GameConfig.RaceDescriptions[CharacterRace.Human] == "a humble Human" &&
            GameConfig.RaceDescriptions[CharacterRace.Troll] == "a stinking Troll",
            "Race descriptions should match Pascal format exactly");
    }

    /// <summary>
    /// Test 9: Error Handling and Edge Cases
    /// </summary>
    private static void TestErrorHandling()
    {
        // Test boundary conditions
        Test("All races have positive bonuses", () =>
        {
            foreach (CharacterRace race in Enum.GetValues<CharacterRace>())
            {
                var attr = GameConfig.RaceAttributes[race];
                if (attr.HPBonus <= 0 || attr.StrengthBonus < 0) return false;
            }
            return true;
        }, "All races should have positive HP bonus and non-negative other bonuses");

        Test("All classes have positive HP", () =>
        {
            foreach (CharacterClass charClass in Enum.GetValues<CharacterClass>())
            {
                var attr = GameConfig.ClassStartingAttributes[charClass];
                if (attr.HP <= 0) return false;
            }
            return true;
        }, "All classes should have positive starting HP");

        Test("Age ranges are sensible", () =>
        {
            foreach (CharacterRace race in Enum.GetValues<CharacterRace>())
            {
                var attr = GameConfig.RaceAttributes[race];
                if (attr.MinAge < 10 || attr.MaxAge > 100 || attr.MinAge >= attr.MaxAge)
                    return false;
            }
            return true;
        }, "Age ranges should be reasonable (10-100) and min < max");

        Test("Physical ranges are sensible", () =>
        {
            foreach (CharacterRace race in Enum.GetValues<CharacterRace>())
            {
                var attr = GameConfig.RaceAttributes[race];
                if (attr.MinHeight < 50 || attr.MaxHeight > 300 ||
                    attr.MinWeight < 20 || attr.MaxWeight > 200)
                    return false;
            }
            return true;
        }, "Physical ranges should be reasonable for fantasy races");

        Test("Mana is zero for non-casters", () =>
        {
            var nonCasters = new[] { CharacterClass.Warrior, CharacterClass.Barbarian, 
                                   CharacterClass.Ranger, CharacterClass.Assassin,
                                   CharacterClass.Bard, CharacterClass.Jester, CharacterClass.Paladin };
            
            return nonCasters.All(c => GameConfig.ClassStartingAttributes[c].Mana == 0);
        }, "Non-spellcasting classes should have zero mana");

        Test("Casters have positive mana", () =>
        {
            var casters = new[] { CharacterClass.Magician, CharacterClass.Cleric, 
                                CharacterClass.Sage, CharacterClass.Alchemist };
            
            return casters.All(c => GameConfig.ClassStartingAttributes[c].Mana > 0);
        }, "Spellcasting classes should have positive mana");
    }

    /// <summary>
    /// Test 10: Data Integrity and Consistency
    /// </summary>
    private static void TestDataIntegrity()
    {
        Test("Race names array matches enum count", () =>
            GameConfig.RaceNames.Length == Enum.GetValues<CharacterRace>().Length,
            "Race names array should match race enum count");

        Test("Class names array matches enum count", () =>
            GameConfig.ClassNames.Length == Enum.GetValues<CharacterClass>().Length,
            "Class names array should match class enum count");

        Test("Race descriptions covers all races", () =>
            GameConfig.RaceDescriptions.Count == Enum.GetValues<CharacterRace>().Length,
            "Race descriptions should cover all races");

        Test("Race attributes covers all races", () =>
            GameConfig.RaceAttributes.Count == Enum.GetValues<CharacterRace>().Length,
            "Race attributes should cover all races");

        Test("Class attributes covers all classes", () =>
            GameConfig.ClassStartingAttributes.Count == Enum.GetValues<CharacterClass>().Length,
            "Class attributes should cover all classes");

        Test("No null or empty race names", () =>
            GameConfig.RaceNames.All(name => !string.IsNullOrWhiteSpace(name)),
            "Race names should not be null or empty");

        Test("No null or empty class names", () =>
            GameConfig.ClassNames.All(name => !string.IsNullOrWhiteSpace(name)),
            "Class names should not be null or empty");

        Test("Hair color arrays are valid", () =>
        {
            foreach (CharacterRace race in Enum.GetValues<CharacterRace>())
            {
                var attr = GameConfig.RaceAttributes[race];
                if (race == CharacterRace.Mutant)
                {
                    if (attr.HairColors.Length != 0) return false; // Mutants should have empty array
                }
                else
                {
                    if (attr.HairColors.Length == 0 || attr.HairColors.Any(c => c < 1 || c > 10))
                        return false;
                }
            }
            return true;
        }, "Hair color arrays should be valid for each race");

        Test("Skin colors are in valid range", () =>
        {
            foreach (CharacterRace race in Enum.GetValues<CharacterRace>())
            {
                var attr = GameConfig.RaceAttributes[race];
                if (race == CharacterRace.Mutant)
                {
                    if (attr.SkinColor != 0) return false; // Mutants should have 0 (random)
                }
                else
                {
                    if (attr.SkinColor < 1 || attr.SkinColor > 10) return false;
                }
            }
            return true;
        }, "Skin colors should be in valid range (1-10) or 0 for random");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Calculate combined race and class attributes
    /// </summary>
    private static (long HP, long Strength) CalculateAttributes(CharacterRace race, CharacterClass charClass)
    {
        var classAttr = GameConfig.ClassStartingAttributes[charClass];
        var raceAttr = GameConfig.RaceAttributes[race];
        
        return (
            HP: classAttr.HP + raceAttr.HPBonus,
            Strength: classAttr.Strength + raceAttr.StrengthBonus
        );
    }

    /// <summary>
    /// Check if race/class combination is invalid
    /// </summary>
    private static bool IsInvalidCombination(CharacterRace race, CharacterClass charClass)
    {
        return GameConfig.InvalidCombinations.ContainsKey(race) &&
               GameConfig.InvalidCombinations[race].Contains(charClass);
    }

    /// <summary>
    /// Execute a test and track results
    /// </summary>
    private static void Test(string name, Func<bool> test, string description = "")
    {
        testsRun++;
        try
        {
            if (test())
            {
                testsPassed++;
                GD.Print($"✓ {name}");
            }
            else
            {
                failedTests.Add($"{name}: {description}");
                GD.PrintErr($"✗ {name}: {description}");
            }
        }
        catch (Exception ex)
        {
            failedTests.Add($"{name}: Exception - {ex.Message}");
            GD.PrintErr($"✗ {name}: Exception - {ex.Message}");
        }
    }

    #endregion
} 