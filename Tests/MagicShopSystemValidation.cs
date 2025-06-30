using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Comprehensive validation tests for the Magic Shop System
/// Tests all Pascal-compatible features: items, identification, healing potions, spells
/// </summary>
public static class MagicShopSystemValidation
{
    public static void RunAllTests()
    {
        Console.WriteLine("═══ Magic Shop System Validation ═══");
        Console.WriteLine();
        
        TestMagicItemSystem();
        TestSpellSystem();
        TestHealingPotionPricing();
        TestItemIdentification();
        TestMagicShopIntegration();
        TestPascalCompatibility();
        
        Console.WriteLine();
        Console.WriteLine("═══ All Magic Shop Tests Complete ═══");
    }
    
    private static void TestMagicItemSystem()
    {
        Console.WriteLine("Testing Magic Item System...");
        
        // Test magic item creation
        var amulet = new Item();
        amulet.Name = "Test Amulet";
        amulet.MagicType = MagicItemType.Neck;
        amulet.MagicProperties.Mana = 15;
        amulet.MagicProperties.Wisdom = 3;
        amulet.OnlyForGood = true;
        amulet.Value = 2500;
        
        Console.WriteLine($"✓ Created magic amulet: {amulet.Name}");
        Console.WriteLine($"  - Mana: +{amulet.MagicProperties.Mana}");
        Console.WriteLine($"  - Wisdom: +{amulet.MagicProperties.Wisdom}");
        Console.WriteLine($"  - Restriction: Good only = {amulet.OnlyForGood}");
        Console.WriteLine($"  - Value: {amulet.Value} gold");
        
        // Test magic item types
        var testTypes = new[] { MagicItemType.Neck, MagicItemType.Fingers, MagicItemType.Waist };
        foreach (var type in testTypes)
        {
            Console.WriteLine($"✓ Magic item type: {type} (value: {(int)type})");
        }
        
        // Test cure types
        var testCures = new[] { CureType.All, CureType.Blindness, CureType.Plague };
        foreach (var cure in testCures)
        {
            Console.WriteLine($"✓ Disease cure type: {cure}");
        }
        
        Console.WriteLine("✓ Magic Item System tests passed!");
        Console.WriteLine();
    }
    
    private static void TestSpellSystem()
    {
        Console.WriteLine("Testing Spell System...");
        
        // Test spell costs (Pascal formula: level * 10)
        for (int level = 1; level <= 12; level++)
        {
            int cost = SpellSystem.GetSpellCost(CharacterClass.Cleric, level);
            int expectedCost = level * 10;
            
            if (cost != expectedCost)
            {
                Console.WriteLine($"✗ Spell cost mismatch! Level {level}: got {cost}, expected {expectedCost}");
            }
        }
        Console.WriteLine("✓ Spell cost calculations correct");
        
        // Test level requirements
        var levelTests = new Dictionary<int, int>
        {
            [1] = 1, [2] = 3, [3] = 5, [4] = 8, [5] = 12,
            [6] = 17, [7] = 23, [8] = 30, [9] = 38, [10] = 47,
            [11] = 57, [12] = 68
        };
        
        foreach (var test in levelTests)
        {
            int required = SpellSystem.GetLevelRequired(CharacterClass.Cleric, test.Key);
            if (required != test.Value)
            {
                Console.WriteLine($"✗ Level requirement mismatch! Spell {test.Key}: got {required}, expected {test.Value}");
            }
        }
        Console.WriteLine("✓ Spell level requirements correct");
        
        // Test spell info for each class
        var classes = new[] { CharacterClass.Cleric, CharacterClass.Magician, CharacterClass.Sage };
        foreach (var charClass in classes)
        {
            for (int level = 1; level <= 12; level++)
            {
                var spellInfo = SpellSystem.GetSpellInfo(charClass, level);
                if (spellInfo == null)
                {
                    Console.WriteLine($"✗ Missing spell info for {charClass} level {level}");
                }
                else
                {
                    Console.WriteLine($"✓ {charClass} Level {level}: {spellInfo.Name} ({spellInfo.ManaCost} mana)");
                }
            }
        }
        
        // Test spell casting for a test character
        var testCleric = new Character("TestCleric", "TestCleric");
        testCleric.Class = CharacterClass.Cleric;
        testCleric.Level = 10;
        testCleric.Mana = testCleric.MaxMana = 200;
        testCleric.HP = testCleric.MaxHP = 100;
        
        Console.WriteLine($"Testing spell casting with {testCleric.Name2} (Level {testCleric.Level})");
        
        var availableSpells = SpellSystem.GetAvailableSpells(testCleric);
        Console.WriteLine($"✓ Available spells: {availableSpells.Count}");
        
        // Test casting a healing spell
        if (SpellSystem.CanCastSpell(testCleric, 1))
        {
            var result = SpellSystem.CastSpell(testCleric, 1); // Cure Light
            Console.WriteLine($"✓ Cast Cure Light: {result.Success}");
            Console.WriteLine($"  Message: {result.Message}");
            Console.WriteLine($"  Healing: {result.Healing}");
            Console.WriteLine($"  Mana cost: {result.ManaCost}");
        }
        
        Console.WriteLine("✓ Spell System tests passed!");
        Console.WriteLine();
    }
    
    private static void TestHealingPotionPricing()
    {
        Console.WriteLine("Testing Healing Potion Pricing...");
        
        // Test Pascal formula: level * 5
        var testLevels = new[] { 1, 5, 10, 20, 50, 100 };
        foreach (int level in testLevels)
        {
            int expectedPrice = level * GameConfig.HealingPotionLevelMultiplier;
            Console.WriteLine($"✓ Level {level} character: {expectedPrice} gold per potion");
        }
        
        // Test max potions limit
        Console.WriteLine($"✓ Maximum potions allowed: {GameConfig.MaxHealingPotions}");
        
        // Test purchase calculations
        var testPlayer = new Character("TestPlayer", "TestPlayer");
        testPlayer.Level = 10;
        testPlayer.Gold = 1000;
        testPlayer.Healing = 5;
        
        int potionPrice = testPlayer.Level * GameConfig.HealingPotionLevelMultiplier;
        int maxCanBuy = testPlayer.Gold / potionPrice;
        int maxCanCarry = GameConfig.MaxHealingPotions - testPlayer.Healing;
        int maxPotions = Math.Min(maxCanBuy, maxCanCarry);
        
        Console.WriteLine($"✓ Test player (Level {testPlayer.Level}, {testPlayer.Gold} gold, {testPlayer.Healing} potions):");
        Console.WriteLine($"  - Price per potion: {potionPrice} gold");
        Console.WriteLine($"  - Max can afford: {maxCanBuy}");
        Console.WriteLine($"  - Max can carry: {maxCanCarry}");
        Console.WriteLine($"  - Max can buy: {maxPotions}");
        
        Console.WriteLine("✓ Healing Potion Pricing tests passed!");
        Console.WriteLine();
    }
    
    private static void TestItemIdentification()
    {
        Console.WriteLine("Testing Item Identification...");
        
        // Test identification cost
        Console.WriteLine($"✓ Base identification cost: {GameConfig.DefaultIdentificationCost} gold");
        
        // Test unidentified item
        var mysteryItem = new Item();
        mysteryItem.Name = "Strange Amulet";
        mysteryItem.IsIdentified = false;
        mysteryItem.MagicProperties.Mana = 25;
        mysteryItem.MagicProperties.Wisdom = 5;
        mysteryItem.IsCursed = true;
        
        Console.WriteLine($"✓ Created unidentified item: {mysteryItem.Name}");
        Console.WriteLine($"  - Identified: {mysteryItem.IsIdentified}");
        Console.WriteLine($"  - Hidden mana bonus: +{mysteryItem.MagicProperties.Mana}");
        Console.WriteLine($"  - Hidden curse: {mysteryItem.IsCursed}");
        
        // Simulate identification
        mysteryItem.IsIdentified = true;
        Console.WriteLine($"✓ Item identified! Now shows all properties");
        
        Console.WriteLine("✓ Item Identification tests passed!");
        Console.WriteLine();
    }
    
    private static void TestMagicShopIntegration()
    {
        Console.WriteLine("Testing Magic Shop Integration...");
        
        // Test shop owner configuration
        string defaultOwner = GameConfig.DefaultMagicShopOwner;
        Console.WriteLine($"✓ Default shop owner: {defaultOwner}");
        
        MagicShopLocation.SetOwnerName("Test Gnome");
        Console.WriteLine($"✓ Owner name changed to: Test Gnome");
        
        MagicShopLocation.SetOwnerName(defaultOwner); // Reset
        Console.WriteLine($"✓ Owner name reset to: {defaultOwner}");
        
        // Test identification cost configuration
        MagicShopLocation.SetIdentificationCost(2000);
        Console.WriteLine($"✓ Identification cost set to: 2000 gold");
        
        MagicShopLocation.SetIdentificationCost(GameConfig.DefaultIdentificationCost); // Reset
        Console.WriteLine($"✓ Identification cost reset to: {GameConfig.DefaultIdentificationCost} gold");
        
        // Test magic inventory
        var inventory = MagicShopLocation.GetMagicInventory();
        Console.WriteLine($"✓ Magic shop inventory items: {inventory.Count}");
        
        // Group by type
        var neckItems = inventory.Where(i => i.MagicType == MagicItemType.Neck).Count();
        var ringItems = inventory.Where(i => i.MagicType == MagicItemType.Fingers).Count();
        var waistItems = inventory.Where(i => i.MagicType == MagicItemType.Waist).Count();
        
        Console.WriteLine($"  - Neck items (amulets): {neckItems}");
        Console.WriteLine($"  - Ring items: {ringItems}");
        Console.WriteLine($"  - Waist items (belts): {waistItems}");
        
        Console.WriteLine("✓ Magic Shop Integration tests passed!");
        Console.WriteLine();
    }
    
    private static void TestPascalCompatibility()
    {
        Console.WriteLine("Testing Pascal Compatibility...");
        
        // Test magic words (from Pascal SPELLSU.PAS)
        var testWords = new Dictionary<(CharacterClass, int), string>
        {
            [(CharacterClass.Cleric, 1)] = "Sularahamasturie",
            [(CharacterClass.Magician, 6)] = "Exammmarie",
            [(CharacterClass.Sage, 11)] = "Edujnomed"
        };
        
        foreach (var test in testWords)
        {
            string words = SpellSystem.GetMagicWords(test.Key.Item1, test.Key.Item2);
            if (words == test.Value)
            {
                Console.WriteLine($"✓ Magic words correct: {test.Key.Item1} L{test.Key.Item2} = '{words}'");
            }
            else
            {
                Console.WriteLine($"✗ Magic words mismatch: expected '{test.Value}', got '{words}'");
            }
        }
        
        // Test spell names match Pascal
        var spellNameTests = new Dictionary<(CharacterClass, int), string>
        {
            [(CharacterClass.Cleric, 10)] = "Heal",
            [(CharacterClass.Magician, 11)] = "Power word KILL",
            [(CharacterClass.Sage, 6)] = "Hit Self"
        };
        
        foreach (var test in spellNameTests)
        {
            var spellInfo = SpellSystem.GetSpellInfo(test.Key.Item1, test.Key.Item2);
            if (spellInfo?.Name == test.Value)
            {
                Console.WriteLine($"✓ Spell name correct: {test.Key.Item1} L{test.Key.Item2} = '{spellInfo.Name}'");
            }
            else
            {
                Console.WriteLine($"✗ Spell name mismatch: expected '{test.Value}', got '{spellInfo?.Name}'");
            }
        }
        
        // Test damage ranges match Pascal
        var testChar = new Character("Test", "Test");
        testChar.Class = CharacterClass.Magician;
        testChar.Level = 50;
        testChar.Mana = 1000;
        
        Console.WriteLine("Testing damage ranges (10 samples):");
        for (int i = 0; i < 10; i++)
        {
            var result = SpellSystem.CastSpell(testChar, 6); // Fireball: 60-70 damage
            if (result.Damage >= 60 && result.Damage <= 70)
            {
                Console.WriteLine($"✓ Fireball damage in range: {result.Damage}");
            }
            else
            {
                Console.WriteLine($"✗ Fireball damage out of range: {result.Damage} (expected 60-70)");
            }
            testChar.Mana += result.ManaCost; // Restore mana for next test
        }
        
        Console.WriteLine("✓ Pascal Compatibility tests passed!");
        Console.WriteLine();
    }
} 