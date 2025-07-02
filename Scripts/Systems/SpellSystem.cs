using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Complete Pascal-compatible spell system
/// Handles all spell mechanics from SPELLSU.PAS and CAST.PAS
/// Spells are learned automatically by class and level
/// </summary>
public static class SpellSystem
{
    /// <summary>
    /// Spell casting result
    /// </summary>
    public class SpellResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int Damage { get; set; }
        public int Healing { get; set; }
        public string SpecialEffect { get; set; } = "";
        public int ProtectionBonus { get; set; }
        public int AttackBonus { get; set; }
        public bool IsMultiTarget { get; set; }
        public int Duration { get; set; } // Combat rounds
        public int ManaCost { get; set; }
    }
    
    /// <summary>
    /// Spell information
    /// </summary>
    public class SpellInfo
    {
        public int Level { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int ManaCost { get; set; }
        public int LevelRequired { get; set; }
        public string MagicWords { get; set; } = "";
        public bool IsMultiTarget { get; set; }
        public string SpellType { get; set; } = ""; // Attack, Heal, Buff, Debuff
        
        public SpellInfo(int level, string name, string description, int manaCost, int levelRequired, string magicWords, bool isMultiTarget = false, string spellType = "")
        {
            Level = level;
            Name = name;
            Description = description;
            ManaCost = manaCost;
            LevelRequired = levelRequired;
            MagicWords = magicWords;
            IsMultiTarget = isMultiTarget;
            SpellType = spellType;
        }
    }
    
    // Pascal spell data - exact recreation from SPELLSU.PAS
    private static readonly Dictionary<CharacterClass, Dictionary<int, SpellInfo>> SpellBook = new()
    {
        [CharacterClass.Cleric] = new Dictionary<int, SpellInfo>
        {
            [1] = new SpellInfo(1, "Cure Light", "This spell is good for small wounds only. Effect: caster regains 4-7 hps. Duration: 1 turn.", 10, 1, "Sularahamasturie", false, "Heal"),
            [2] = new SpellInfo(2, "Armor", "Provides magical protection to the caster. Protection: +5. Duration: whole fight.", 20, 2, "Exmaddurie", false, "Buff"),
            [3] = new SpellInfo(3, "Baptize Monster", "Attempts to convert monster to good alignment. May cause monster to flee or become friendly.", 30, 3, "Exdelusiarie", false, "Debuff"),
            [4] = new SpellInfo(4, "Cure Critical", "More powerful healing spell. Effect: caster regains 20-25 hps, increases chivalry. Duration: 1 turn.", 40, 4, "Aduexusmarie", false, "Heal"),
            [5] = new SpellInfo(5, "Disease", "Inflicts a random disease on the target. Effect: random disease. Duration: 1 turn.", 50, 5, "Exdavenusmarie", false, "Debuff"),
            [6] = new SpellInfo(6, "Holy Explosion", "A ball of holy energy which bursts into darts. Damage: 20-30 hps. Duration: 1 turn.", 60, 6, "Adrealitarieum", true, "Attack"),
            [7] = new SpellInfo(7, "Invisibility", "Makes the caster invisible to enemies. Effect: greatly increased protection. Duration: whole fight.", 70, 7, "Exsuamarie", false, "Buff"),
            [8] = new SpellInfo(8, "Angel", "Calls an angel to aid in battle. Damage: 100 hps. Duration: whole fight.", 80, 8, "Admoriasumumarie", false, "Summon"),
            [9] = new SpellInfo(9, "Call Lightning", "Sends a lightning bolt through the target. Damage: 80-89 hps. Duration: 1 turn.", 90, 9, "Exdahabarie", false, "Attack"),
            [10] = new SpellInfo(10, "Heal", "The most powerful healing spell. Effect: caster regains 200 hps. Duration: 1 turn.", 100, 10, "Exmasumarie", false, "Heal"),
            [11] = new SpellInfo(11, "Divination", "Divine intervention. Causes caster to become mini-God. Protection: +110-141, increased goodness. Duration: random.", 110, 11, "Exadmassumumarie", false, "Buff"),
            [12] = new SpellInfo(12, "Gods Finger", "Ultimate divine attack. Massive damage to all enemies. Damage: varies greatly. Duration: 1 turn.", 120, 12, "Umbarakahstahx", true, "Attack")
        },
        
        [CharacterClass.Magician] = new Dictionary<int, SpellInfo>
        {
            [1] = new SpellInfo(1, "Magic Missile", "Sends steel arrows towards target. Few armor can resist it. Damage: 4-7 hps. Duration: 1 turn.", 10, 1, "Exmamarie", false, "Attack"),
            [2] = new SpellInfo(2, "Shield", "Creates magical shield around caster. Protection: +8. Duration: whole fight.", 20, 2, "Exmasumarie", false, "Buff"),
            [3] = new SpellInfo(3, "Sleep", "Puts target to sleep, preventing action. Duration: varies. Effect: target cannot act.", 30, 3, "Exdamarie", false, "Debuff"),
            [4] = new SpellInfo(4, "Web", "Catches target in magic web. Effect: target cannot move or attack. Duration: varies.", 40, 4, "Exmasesamamarie", false, "Debuff"),
            [5] = new SpellInfo(5, "Haste", "Doubles the caster's speed, granting extra attacks each round.", 50, 5, "Quicksilvarie", false, "Buff"),
            [6] = new SpellInfo(6, "Power Hat", "Regenerates hitpoints and provides protection. Effect: regains 60-80 hps + 10-13 protection every turn. Duration: whole fight.", 55, 6, "Excadammarie", false, "Heal"),
            [7] = new SpellInfo(7, "Fireball", "Target is swallowed by ball of fire. Even if it burns out quickly, impact is great. Damage: 60-70 hps. Duration: 1 turn.", 60, 7, "Exammmarie", false, "Attack"),
            [8] = new SpellInfo(8, "Fear", "Causes overwhelming fear in target. Target may flee or fight with reduced effectiveness. Duration: varies.", 70, 8, "Examasumarie", false, "Debuff"),
            [9] = new SpellInfo(9, "Lightning Bolt", "Stolen from Clerics guild. Comes down silently causing respectable damage. Damage: 60-70 hps. Duration: 1 turn.", 80, 9, "Exmasesaexmarie", false, "Attack"),
            [10] = new SpellInfo(10, "Prismatic Cage", "Lowers magic cage over caster, increasing protection. Protection: +20. Duration: whole fight.", 90, 10, "Exmasummasumarie", false, "Buff"),
            [11] = new SpellInfo(11, "Pillar of Fire", "Fire that sticks like glue to skin. Penetrates all armor. Damage: 110-112 hps. Duration: 1 turn.", 100, 11, "Exdammasumarie", false, "Attack"),
            [12] = new SpellInfo(12, "Power word KILL", "Drives spirit from flesh, shuts down body and mind. Works well on animals. Damage: 220-265 hps. Duration: 1 turn.", 110, 12, "Mattravidduzzievh", false, "Attack"),
            [13] = new SpellInfo(13, "Summon Demon", "Calls demon from hell to serve in battle. The demon will attack enemies until combat ends. Duration: whole fight.", 120, 13, "Excadexsumarie", false, "Summon")
        },
        
        [CharacterClass.Sage] = new Dictionary<int, SpellInfo>
        {
            [1] = new SpellInfo(1, "Fog of War", "Bank of mist lowers over battle. Only caster can see clearly. Protection: +3. Duration: whole fight.", 10, 1, "Exadmasaxmarie", false, "Buff"),
            [2] = new SpellInfo(2, "Poison", "Poisons the target with magical toxins. Effect: target takes damage over time. Duration: varies.", 20, 2, "Exadlimmarie", false, "Debuff"),
            [3] = new SpellInfo(3, "Freeze", "Freezes target in place with ice magic. Effect: target cannot move. Duration: varies.", 30, 3, "Excadaliemarie", false, "Debuff"),
            [4] = new SpellInfo(4, "Duplicate", "Creates magical duplicate of caster to confuse enemies. Effect: reduces chance to be hit. Duration: whole fight.", 40, 4, "Exmassesumarie", false, "Buff"),
            [5] = new SpellInfo(5, "Roast", "Sends bolt of fire towards target. Penetrates everything to bare skin. Damage: 50-65 hps. Duration: 1 turn.", 50, 5, "Exdamseaxmarie", false, "Attack"),
            [6] = new SpellInfo(6, "Hit Self", "Mind boggling spell. Target loses control and tries to commit suicide. Damage: 70-80 hps. Duration: 1 turn.", 60, 6, "Exadliemasumarie", false, "Attack"),
            [7] = new SpellInfo(7, "Escape", "Allows instant escape from battle. Success rate depends on caster level. Effect: ends combat immediately.", 70, 7, "Exemarie", false, "Escape"),
            [8] = new SpellInfo(8, "Giant", "Transforms caster into giant with club. Adds to attack capabilities. Effect: +25 damage. Duration: whole fight.", 80, 8, "Excadmassumarie", false, "Buff"),
            [9] = new SpellInfo(9, "Steal", "Robs enemies of their gold. Effect: gets random amount of enemy's gold. Duration: 1 turn.", 90, 9, "Exmasumieladmarie", false, "Special"),
            [10] = new SpellInfo(10, "Energy Drain", "Forces victim to gather psi-energy into heart where it is zapped. Damage: 130-141 hps. Duration: 1 turn.", 100, 10, "Examdammasaxmarie", false, "Attack"),
            [11] = new SpellInfo(11, "Summon Demon", "Calls demon from higher regions of hell. Usually a normal Servant-Demon. Damage: 100-120 hps. Duration: whole fight.", 110, 11, "Edujnomed", false, "Summon"),
            [12] = new SpellInfo(12, "Death Kiss", "Ultimate death magic. Target's life force is drained directly. Damage: extremely high. Duration: 1 turn.", 120, 12, "Exmasdamliemasumarie", false, "Attack")
        }
    };
    
    /// <summary>
    /// Calculate mana cost for a given spell and caster, using D&D-style wisdom discount.
    /// Formula: (level * 5) – ((Wisdom – 10) / 2).  Minimum 1.
    /// Mirrors the design spec for Usurper Reloaded.
    /// </summary>
    public static int CalculateManaCost(SpellInfo spell, Character caster)
    {
        if (spell == null || caster == null) return 0;
        long baseCost = spell.Level * 5;
        long wisdomMod = (caster.Wisdom - 10) / 2;
        long costLong = baseCost - wisdomMod;
        int cost = (int)Math.Max(1, costLong);
        return cost;
    }
    
    /// <summary>
    /// Get spell cost for character class and spell level
    /// </summary>
    public static int GetSpellCost(CharacterClass characterClass, int spellLevel)
    {
        if (spellLevel < 1 || spellLevel > GameConfig.MaxSpellLevel)
            return 0;
            
        return GameConfig.BaseSpellManaCost + ((spellLevel - 1) * GameConfig.ManaPerSpellLevel);
    }
    
    /// <summary>
    /// Get level requirement for spell (Pascal formula)
    /// </summary>
    public static int GetLevelRequired(CharacterClass characterClass, int spellLevel)
    {
        // Basic formula from Pascal: spell level requirements
        return spellLevel switch
        {
            1 => 1,   // Level 1 spells at character level 1
            2 => 3,   // Level 2 spells at character level 3
            3 => 5,   // Level 3 spells at character level 5
            4 => 8,   // Level 4 spells at character level 8
            5 => 12,  // Level 5 spells at character level 12
            6 => 17,  // Level 6 spells at character level 17
            7 => 23,  // Level 7 spells at character level 23
            8 => 30,  // Level 8 spells at character level 30
            9 => 38,  // Level 9 spells at character level 38
            10 => 47, // Level 10 spells at character level 47
            11 => 57, // Level 11 spells at character level 57
            12 => 68, // Level 12 spells at character level 68
            _ => 100
        };
    }
    
    /// <summary>
    /// Get spell information for character class and spell level
    /// </summary>
    public static SpellInfo GetSpellInfo(CharacterClass characterClass, int spellLevel)
    {
        if (SpellBook.TryGetValue(characterClass, out var classSpells) &&
            classSpells.TryGetValue(spellLevel, out var spellInfo))
        {
            return spellInfo;
        }
        
        return null;
    }
    
    /// <summary>
    /// Get all available spells for character
    /// </summary>
    public static List<SpellInfo> GetAvailableSpells(Character character)
    {
        var availableSpells = new List<SpellInfo>();
        
        if (!SpellBook.TryGetValue(character.Class, out var classSpells))
            return availableSpells;
            
        foreach (var spell in classSpells.Values)
        {
            if (character.Level >= GetLevelRequired(character.Class, spell.Level))
            {
                availableSpells.Add(spell);
            }
        }
        
        return availableSpells;
    }
    
    /// <summary>
    /// Check if character can cast specific spell
    /// </summary>
    public static bool CanCastSpell(Character character, int spellLevel)
    {
        if (spellLevel < 1 || spellLevel > GameConfig.MaxSpellLevel)
            return false;
            
        // Must be magic-using class
        if (character.Class != CharacterClass.Cleric &&
            character.Class != CharacterClass.Magician &&
            character.Class != CharacterClass.Sage)
            return false;
            
        // Level requirement
        if (character.Level < GetLevelRequired(character.Class, spellLevel))
            return false;
            
        var info = GetSpellInfo(character.Class, spellLevel);
        if (info == null) return false;

        var manaCost = CalculateManaCost(info, character);
        return character.Mana >= manaCost;
    }
    
    /// <summary>
    /// Cast spell - main spell casting function (Pascal CAST.PAS recreation)
    /// </summary>
    public static SpellResult CastSpell(Character caster, int spellLevel, Character target = null, List<Character> allTargets = null)
    {
        var result = new SpellResult();
        
        if (!CanCastSpell(caster, spellLevel))
        {
            result.Success = false;
            result.Message = "Cannot cast this spell!";
            return result;
        }
        
        var spellInfo = GetSpellInfo(caster.Class, spellLevel);
        if (spellInfo == null)
        {
            result.Success = false;
            result.Message = "Unknown spell!";
            return result;
        }
        
        // Deduct mana cost using dynamic calculation
        var manaCost = CalculateManaCost(spellInfo, caster);
        result.ManaCost = manaCost;
        caster.Mana -= manaCost;
        
        result.Success = true;
        result.Message = $"{caster.Name2} utters '{spellInfo.MagicWords}'!";
        result.IsMultiTarget = spellInfo.IsMultiTarget;
        
        // Execute spell effects based on class and level
        ExecuteSpellEffect(caster, spellLevel, target, allTargets, result);
        
        return result;
    }
    
    /// <summary>
    /// Execute specific spell effects (Pascal CAST.PAS implementation)
    /// </summary>
    private static void ExecuteSpellEffect(Character caster, int spellLevel, Character target, List<Character> allTargets, SpellResult result)
    {
        var random = new Random();
        
        switch (caster.Class)
        {
            case CharacterClass.Cleric:
                ExecuteClericSpell(caster, spellLevel, target, allTargets, result, random);
                break;
            case CharacterClass.Magician:
                ExecuteMagicianSpell(caster, spellLevel, target, allTargets, result, random);
                break;
            case CharacterClass.Sage:
                ExecuteSageSpell(caster, spellLevel, target, allTargets, result, random);
                break;
        }
    }
    
    /// <summary>
    /// Execute Cleric spells (healing and divine magic)
    /// </summary>
    private static void ExecuteClericSpell(Character caster, int spellLevel, Character target, List<Character> allTargets, SpellResult result, Random random)
    {
        switch (spellLevel)
        {
            case 1: // Cure Light
                result.Healing = 4 + random.Next(4); // 4-7 hp
                result.Message += $" {caster.Name2} regains {result.Healing} hitpoints!";
                break;
                
            case 2: // Armor
                result.ProtectionBonus = 5;
                result.Duration = 999; // Whole fight
                result.Message += $" {caster.Name2} feels protected!";
                break;
                
            case 3: // Baptize Monster
                if (target != null)
                {
                    result.SpecialEffect = "convert";
                    result.Message += $" {target.Name2} may be converted to good!";
                }
                break;
                
            case 4: // Cure Critical
                result.Healing = 20 + random.Next(6); // 20-25 hp
                if (caster.Darkness > 0)
                {
                    caster.Darkness = Math.Max(0, caster.Darkness - 15);
                }
                else
                {
                    caster.Chivalry += 15;
                }
                result.Message += $" {caster.Name2} feels stronger and regains {result.Healing} hitpoints!";
                break;
                
            case 5: // Disease
                if (target != null)
                {
                    result.SpecialEffect = "disease";
                    result.Message += $" A disease strikes {target.Name2}!";
                }
                break;
                
            case 6: // Holy Explosion
                result.Damage = 20 + random.Next(11); // 20-30 damage
                result.IsMultiTarget = true;
                result.Message += $" A magic bomb explodes causing {result.Damage} damage!";
                break;
                
            case 7: // Invisibility
                result.ProtectionBonus = 20;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "invisible";
                result.Message += $" {caster.Name2} becomes invisible!";
                break;
                
            case 8: // Angel
                result.AttackBonus = 100;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "angel";
                result.Message += " An Angel suddenly arrives with golden wings!";
                break;
                
            case 9: // Call Lightning
                result.Damage = 80 + random.Next(10); // 80-89 damage
                result.Message += $" Lightning strikes {target?.Name2 ?? "the target"} for {result.Damage} damage!";
                break;
                
            case 10: // Heal
                result.Healing = 200;
                result.Message += $" {caster.Name2} regains {result.Healing} hitpoints!";
                break;
                
            case 11: // Divination
                result.ProtectionBonus = 110 + random.Next(32); // 110-141 protection
                result.Duration = random.Next(10) + 5; // Random duration
                result.SpecialEffect = "divination";
                if (caster.Darkness > 0)
                {
                    caster.Darkness = Math.Max(0, caster.Darkness - 50);
                }
                else
                {
                    caster.Chivalry += 50;
                }
                result.Message += $" {caster.Name2} is blessed by divine intervention!";
                break;
                
            case 12: // Gods Finger
                result.Damage = 200 + random.Next(100); // Massive damage
                result.IsMultiTarget = true;
                result.Message += " The finger of God strikes down upon the enemies!";
                break;
        }
    }
    
    /// <summary>
    /// Execute Magician spells (elemental and arcane magic)
    /// </summary>
    private static void ExecuteMagicianSpell(Character caster, int spellLevel, Character target, List<Character> allTargets, SpellResult result, Random random)
    {
        switch (spellLevel)
        {
            case 1: // Magic Missile
                result.Damage = 4 + random.Next(4); // 4-7 damage
                result.Message += $" Magic missiles strike {target?.Name2 ?? "the target"} for {result.Damage} damage!";
                break;
                
            case 2: // Shield
                result.ProtectionBonus = 8;
                result.Duration = 999; // Whole fight
                result.Message += $" {caster.Name2} is surrounded by a steel aura!";
                break;
                
            case 3: // Sleep
                if (target != null)
                {
                    result.SpecialEffect = "sleep";
                    result.Duration = random.Next(5) + 3;
                    result.Message += $" {target.Name2} falls asleep!";
                }
                break;
                
            case 4: // Web
                if (target != null)
                {
                    result.SpecialEffect = "web";
                    result.Duration = random.Next(4) + 2;
                    result.Message += $" A Magic Web surrounds {target.Name2}!";
                }
                break;
                
            case 5: // Haste
                result.AttackBonus = 20;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "haste";
                result.Message += $" {caster.Name2} is moving faster!";
                break;
                
            case 6: // Power Hat
                result.Healing = 60 + random.Next(21); // 60-80 hp
                result.ProtectionBonus = 10 + random.Next(4); // 10-13 protection per turn
                result.Duration = 999; // Whole fight
                result.Message += $" {caster.Name2} regains {result.Healing} hitpoints and feels protected!";
                break;
                
            case 7: // Fireball
                result.Damage = 60 + random.Next(11); // 60-70 damage
                result.Message += $" {target?.Name2 ?? "The target"} is engulfed by a Fireball for {result.Damage} damage!";
                break;
                
            case 8: // Fear
                if (target != null)
                {
                    result.SpecialEffect = "fear";
                    result.Duration = random.Next(6) + 2;
                    result.Message += $" {target.Name2} is overwhelmed by fear!";
                }
                break;
                
            case 9: // Lightning Bolt
                result.Damage = 60 + random.Next(11); // 60-70 damage
                result.Message += $" {target?.Name2 ?? "The target"} is struck by Lightning for {result.Damage} damage!";
                break;
                
            case 10: // Prismatic Cage
                result.ProtectionBonus = 20;
                result.Duration = 999; // Whole fight
                result.Message += $" A prismatic cage lowers over {caster.Name2}!";
                break;
                
            case 11: // Pillar of Fire
                result.Damage = 110 + random.Next(3); // 110-112 damage
                result.Message += $" {target?.Name2 ?? "The target"} is severely burned in a Pillar of Fire for {result.Damage} damage!";
                break;
                
            case 12: // Power word KILL
                result.Damage = 220 + random.Next(46); // 220-265 damage
                result.Message += $" The POWER WORD KILL strikes {target?.Name2 ?? "the target"} for {result.Damage} damage!";
                break;
                
            case 13: // Summon Demon
                result.AttackBonus = 80;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "demon";
                result.Message += " A demon is summoned from hell to serve in battle!";
                break;
        }
    }
    
    /// <summary>
    /// Execute Sage spells (mind and nature magic)
    /// </summary>
    private static void ExecuteSageSpell(Character caster, int spellLevel, Character target, List<Character> allTargets, SpellResult result, Random random)
    {
        switch (spellLevel)
        {
            case 1: // Fog of War
                result.ProtectionBonus = 3;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "fog";
                result.Message += " A bank of mist lowers over the battle!";
                break;
                
            case 2: // Poison
                if (target != null)
                {
                    result.SpecialEffect = "poison";
                    result.Duration = random.Next(8) + 3;
                    result.Message += $" {target.Name2} is poisoned!";
                }
                break;
                
            case 3: // Freeze
                if (target != null)
                {
                    result.SpecialEffect = "freeze";
                    result.Duration = random.Next(5) + 2;
                    result.Message += $" {target.Name2} is frozen in place!";
                }
                break;
                
            case 4: // Duplicate
                result.ProtectionBonus = 10;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "duplicate";
                result.Message += $" A magical duplicate of {caster.Name2} appears!";
                break;
                
            case 5: // Roast
                result.Damage = 50 + random.Next(16); // 50-65 damage
                result.Message += $" {target?.Name2 ?? "The target"} is struck by hellfire for {result.Damage} damage!";
                break;
                
            case 6: // Hit Self
                result.Damage = 70 + random.Next(11); // 70-80 damage
                result.Message += $" {target?.Name2 ?? "The target"} is filled with agony and tries to hurt themselves for {result.Damage} damage!";
                break;
                
            case 7: // Escape
                result.SpecialEffect = "escape";
                result.Message += $" {caster.Name2} attempts to escape from battle!";
                break;
                
            case 8: // Giant
                result.AttackBonus = 25;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "giant";
                result.Message += $" {caster.Name2} transforms into a GIANT!";
                break;
                
            case 9: // Steal
                result.SpecialEffect = "steal";
                result.Message += $" {caster.Name2} attempts to steal gold from enemies!";
                break;
                
            case 10: // Energy Drain
                result.Damage = 130 + random.Next(12); // 130-141 damage
                result.Message += $" {target?.Name2 ?? "The target"}'s energy is drained for {result.Damage} damage!";
                break;
                
            case 11: // Summon Demon
                result.Damage = 100 + random.Next(21); // 100-120 damage
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "demon";
                result.Message += " A Servant-Demon is summoned from hell!";
                break;
                
            case 12: // Death Kiss
                result.Damage = 150 + random.Next(100); // Massive damage
                result.Message += $" The DEATH KISS drains {target?.Name2 ?? "the target"}'s life force for {result.Damage} damage!";
                break;
        }
    }
    
    /// <summary>
    /// Get magic words for spell (Pascal SPELLSU.PAS recreation)
    /// </summary>
    public static string GetMagicWords(CharacterClass characterClass, int spellLevel)
    {
        var spellInfo = GetSpellInfo(characterClass, spellLevel);
        return spellInfo?.MagicWords ?? "Abracadabra";
    }
    
    /// <summary>
    /// Check if character has any spells available
    /// </summary>
    public static bool HasSpells(Character character)
    {
        return character.Class == CharacterClass.Cleric ||
               character.Class == CharacterClass.Magician ||
               character.Class == CharacterClass.Sage;
    }
    
    /// <summary>
    /// Get highest spell level character can cast
    /// </summary>
    public static int GetMaxSpellLevel(Character character)
    {
        if (!HasSpells(character))
            return 0;
            
        for (int level = GameConfig.MaxSpellLevel; level >= 1; level--)
        {
            if (character.Level >= GetLevelRequired(character.Class, level))
            {
                return level;
            }
        }
        
        return 0;
    }
} 
