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

        // D20 Training System additions
        public string RollInfo { get; set; } = "";           // Roll details for display
        public float ProficiencyMultiplier { get; set; } = 1.0f;  // Effect power multiplier
        public bool SkillImproved { get; set; }              // Did skill level up?
        public string NewProficiencyLevel { get; set; } = ""; // New level name if improved
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
    /// Calculate mana cost for a given spell and caster.
    /// Uses StatEffectsSystem for Wisdom-based mana cost reduction.
    /// Formula: BaseCost * (1 - reduction%), minimum 1.
    /// </summary>
    public static int CalculateManaCost(SpellInfo spell, Character caster)
    {
        if (spell == null || caster == null) return 0;
        int baseCost = spell.Level * 5;

        // Apply Wisdom-based mana cost reduction from StatEffectsSystem
        int reductionPercent = StatEffectsSystem.GetManaCostReduction(caster.Wisdom);
        int cost = baseCost - (baseCost * reductionPercent / 100);

        return Math.Max(1, cost);
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
            // Use the actual LevelRequired from the spell definition, not the generic formula
            if (character.Level >= spell.LevelRequired)
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
        var random = new Random();

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

        // === D20 TRAINING SYSTEM INTEGRATION ===
        // Get spell skill ID for this spell level
        string skillId = TrainingSystem.GetSpellSkillId(caster.Class, spellLevel);
        var proficiency = TrainingSystem.GetSkillProficiency(caster, skillId);

        // Calculate DC based on spell level (higher level spells are harder)
        int baseDC = 8 + spellLevel;

        // Roll for spell success using training system
        var rollResult = TrainingSystem.RollAbilityCheck(caster, skillId, baseDC, random);

        // Store roll info in result message
        result.RollInfo = $"[Roll: {rollResult.NaturalRoll} + {rollResult.Modifier} = {rollResult.Total} vs DC {baseDC}]";

        // Deduct mana cost regardless of success (casting attempt uses mana)
        var manaCost = CalculateManaCost(spellInfo, caster);
        result.ManaCost = manaCost;
        caster.Mana -= manaCost;

        // Check for spell failure
        if (rollResult.IsCriticalFailure || !rollResult.Success)
        {
            result.Success = false;
            if (rollResult.IsCriticalFailure)
            {
                result.Message = $"{caster.Name2} fumbles the spell! The magic fizzles harmlessly.";
                result.SpecialEffect = "fizzle";
            }
            else
            {
                result.Message = $"{caster.Name2} utters '{spellInfo.MagicWords}'... but the spell fails!";
                result.SpecialEffect = "fail";
            }

            // Can still learn from failure
            if (TrainingSystem.TryImproveFromUse(caster, skillId, random))
            {
                var newLevel = TrainingSystem.GetSkillProficiency(caster, skillId);
                result.SkillImproved = true;
                result.NewProficiencyLevel = TrainingSystem.GetProficiencyName(newLevel);
            }

            return result;
        }

        result.Success = true;
        result.Message = $"{caster.Name2} utters '{spellInfo.MagicWords}'!";
        result.IsMultiTarget = spellInfo.IsMultiTarget;

        // Store proficiency multiplier for damage/healing scaling
        result.ProficiencyMultiplier = TrainingSystem.GetEffectMultiplier(proficiency);

        // Apply roll quality bonus (critical success = extra power)
        if (rollResult.IsCriticalSuccess)
        {
            result.ProficiencyMultiplier *= 1.5f; // 50% bonus on critical
            result.Message += " CRITICAL CAST!";
        }
        else if (rollResult.Total >= baseDC + 10)
        {
            result.ProficiencyMultiplier *= 1.25f; // 25% bonus on great roll
        }

        // Execute spell effects based on class and level
        ExecuteSpellEffect(caster, spellLevel, target, allTargets, result);

        // Chance to improve spell proficiency from use
        if (TrainingSystem.TryImproveFromUse(caster, skillId, random))
        {
            var newLevel = TrainingSystem.GetSkillProficiency(caster, skillId);
            result.SkillImproved = true;
            result.NewProficiencyLevel = TrainingSystem.GetProficiencyName(newLevel);
        }

        return result;
    }
    
    /// <summary>
    /// Execute specific spell effects (Pascal CAST.PAS implementation)
    /// Now with level-based scaling for damage and healing!
    /// </summary>
    private static void ExecuteSpellEffect(Character caster, int spellLevel, Character target, List<Character> allTargets, SpellResult result)
    {
        var random = new Random();

        // Get proficiency multiplier (stored in result by CastSpell)
        float profMult = result.ProficiencyMultiplier;

        switch (caster.Class)
        {
            case CharacterClass.Cleric:
                ExecuteClericSpell(caster, spellLevel, target, allTargets, result, random, profMult);
                break;
            case CharacterClass.Magician:
                ExecuteMagicianSpell(caster, spellLevel, target, allTargets, result, random, profMult);
                break;
            case CharacterClass.Sage:
                ExecuteSageSpell(caster, spellLevel, target, allTargets, result, random, profMult);
                break;
        }
    }

    /// <summary>
    /// Calculate level-scaled spell damage/healing
    /// Base damage is multiplied by a scaling factor based on caster level
    /// Level 1: 1.0x, Level 50: 2.5x, Level 100: 4.0x
    /// Now also applies proficiency multiplier from training system!
    /// Uses StatEffectsSystem for consistent stat-based bonuses.
    /// </summary>
    private static int ScaleSpellEffect(int baseEffect, Character caster, Random random, float proficiencyMult = 1.0f)
    {
        // Level scaling: starts at 1.0x, grows to 4.0x at level 100
        // Formula: 1.0 + (level * 0.03) gives 1.0 at level 0, 4.0 at level 100
        double levelMultiplier = 1.0 + (caster.Level * 0.03);

        // Use StatEffectsSystem for Intelligence-based spell damage multiplier
        double statBonus = StatEffectsSystem.GetSpellDamageMultiplier(caster.Intelligence);

        // Wisdom adds a smaller bonus for hybrid casters
        if (caster.Class == CharacterClass.Cleric || caster.Class == CharacterClass.Sage)
        {
            statBonus += (caster.Wisdom - 10) * 0.01; // +1% per Wisdom above 10
        }

        // Add some variance (±10%)
        double variance = 0.9 + (random.NextDouble() * 0.2);

        // Check for spell critical (from Intelligence)
        if (StatEffectsSystem.GetSpellCriticalChance(caster.Intelligence) > random.Next(100))
        {
            proficiencyMult *= 1.5f; // 50% bonus on spell crit
        }

        // Calculate final effect including proficiency bonus
        double scaledEffect = baseEffect * levelMultiplier * statBonus * variance * proficiencyMult;

        return Math.Max(1, (int)scaledEffect);
    }

    /// <summary>
    /// Calculate level-scaled healing (slightly lower scaling than damage)
    /// Level 1: 1.0x, Level 50: 2.0x, Level 100: 3.0x
    /// Now also applies proficiency multiplier from training system!
    /// Uses StatEffectsSystem for Wisdom-based healing bonus.
    /// </summary>
    private static int ScaleHealingEffect(int baseHealing, Character caster, Random random, float proficiencyMult = 1.0f)
    {
        // Healing scales a bit slower than damage
        double levelMultiplier = 1.0 + (caster.Level * 0.02);

        // Use StatEffectsSystem for Wisdom-based healing multiplier
        double wisdomBonus = StatEffectsSystem.GetHealingMultiplier(caster.Wisdom);

        double variance = 0.95 + (random.NextDouble() * 0.1);

        // Include proficiency bonus from training
        double scaledHealing = baseHealing * levelMultiplier * wisdomBonus * variance * proficiencyMult;

        return Math.Max(1, (int)scaledHealing);
    }
    
    /// <summary>
    /// Execute Cleric spells (healing and divine magic)
    /// ALL DAMAGE AND HEALING NOW SCALES WITH CASTER LEVEL AND PROFICIENCY!
    /// </summary>
    private static void ExecuteClericSpell(Character caster, int spellLevel, Character target, List<Character> allTargets, SpellResult result, Random random, float profMult = 1.0f)
    {
        switch (spellLevel)
        {
            case 1: // Cure Light - Base: 4-7 hp
                int baseHeal1 = 4 + random.Next(4);
                result.Healing = ScaleHealingEffect(baseHeal1, caster, random, profMult);
                result.Message += $" {caster.Name2} regains {result.Healing} hitpoints!";
                break;

            case 2: // Armor - Protection scales with level and proficiency
                int baseProtection2 = (int)((5 + (caster.Level / 10)) * profMult);
                result.ProtectionBonus = baseProtection2;
                result.Duration = 999; // Whole fight
                result.Message += $" {caster.Name2} feels protected! (+{result.ProtectionBonus} defense)";
                break;

            case 3: // Baptize Monster
                if (target != null)
                {
                    result.SpecialEffect = "convert";
                    result.Message += $" {target.Name2} may be converted to good!";
                }
                break;

            case 4: // Cure Critical - Base: 20-25 hp
                int baseHeal4 = 20 + random.Next(6);
                result.Healing = ScaleHealingEffect(baseHeal4, caster, random, profMult);
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

            case 6: // Holy Explosion - Base: 20-30 damage
                int baseDamage6 = 20 + random.Next(11);
                result.Damage = ScaleSpellEffect(baseDamage6, caster, random, profMult);
                result.IsMultiTarget = true;
                result.Message += $" A holy explosion deals {result.Damage} damage to all enemies!";
                break;

            case 7: // Invisibility - Protection scales with level and proficiency
                int baseProtection7 = (int)((20 + (caster.Level / 5)) * profMult);
                result.ProtectionBonus = baseProtection7;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "invisible";
                result.Message += $" {caster.Name2} becomes invisible! (+{result.ProtectionBonus} defense)";
                break;

            case 8: // Angel - Attack bonus scales with level and proficiency
                int baseAttack8 = (int)((100 + (caster.Level * 2)) * profMult);
                result.AttackBonus = baseAttack8;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "angel";
                result.Message += $" An Angel suddenly arrives with golden wings! (+{result.AttackBonus} attack)";
                break;

            case 9: // Call Lightning - Base: 80-89 damage
                int baseDamage9 = 80 + random.Next(10);
                result.Damage = ScaleSpellEffect(baseDamage9, caster, random, profMult);
                result.Message += $" Lightning strikes {target?.Name2 ?? "the target"} for {result.Damage} damage!";
                break;

            case 10: // Heal - Base: 200 hp
                result.Healing = ScaleHealingEffect(200, caster, random, profMult);
                result.Message += $" {caster.Name2} regains {result.Healing} hitpoints!";
                break;

            case 11: // Divination - Protection scales with level and proficiency
                int baseProtection11 = (int)((110 + random.Next(32) + (caster.Level / 2)) * profMult);
                result.ProtectionBonus = baseProtection11;
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
                result.Message += $" {caster.Name2} is blessed by divine intervention! (+{result.ProtectionBonus} defense)";
                break;

            case 12: // Gods Finger - Base: 200-300 damage (massive scaling)
                int baseDamage12 = 200 + random.Next(100);
                result.Damage = ScaleSpellEffect(baseDamage12, caster, random, profMult);
                result.IsMultiTarget = true;
                result.Message += $" The finger of God strikes down for {result.Damage} damage!";
                break;
        }
    }
    
    /// <summary>
    /// Execute Magician spells (elemental and arcane magic)
    /// ALL DAMAGE AND HEALING NOW SCALES WITH CASTER LEVEL AND PROFICIENCY!
    /// </summary>
    private static void ExecuteMagicianSpell(Character caster, int spellLevel, Character target, List<Character> allTargets, SpellResult result, Random random, float profMult = 1.0f)
    {
        switch (spellLevel)
        {
            case 1: // Magic Missile - Base: 4-7 damage
                int baseDamage1 = 4 + random.Next(4);
                result.Damage = ScaleSpellEffect(baseDamage1, caster, random, profMult);
                result.Message += $" Magic missiles strike {target?.Name2 ?? "the target"} for {result.Damage} damage!";
                break;

            case 2: // Shield - Protection scales with level and proficiency
                int baseProtection2 = (int)((8 + (caster.Level / 8)) * profMult);
                result.ProtectionBonus = baseProtection2;
                result.Duration = 999; // Whole fight
                result.Message += $" {caster.Name2} is surrounded by a steel aura! (+{result.ProtectionBonus} defense)";
                break;

            case 3: // Sleep
                if (target != null)
                {
                    result.SpecialEffect = "sleep";
                    result.Duration = (int)((random.Next(5) + 3 + (caster.Level / 20)) * profMult); // Duration scales
                    result.Message += $" {target.Name2} falls asleep!";
                }
                break;

            case 4: // Web
                if (target != null)
                {
                    result.SpecialEffect = "web";
                    result.Duration = (int)((random.Next(4) + 2 + (caster.Level / 20)) * profMult);
                    result.Message += $" A Magic Web surrounds {target.Name2}!";
                }
                break;

            case 5: // Haste - Attack bonus scales with level and proficiency
                int baseAttack5 = (int)((20 + (caster.Level / 4)) * profMult);
                result.AttackBonus = baseAttack5;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "haste";
                result.Message += $" {caster.Name2} is moving faster! (+{result.AttackBonus} attack)";
                break;

            case 6: // Power Hat - Base: 60-80 hp
                int baseHeal6 = 60 + random.Next(21);
                result.Healing = ScaleHealingEffect(baseHeal6, caster, random, profMult);
                int baseProtection6 = (int)((10 + random.Next(4) + (caster.Level / 10)) * profMult);
                result.ProtectionBonus = baseProtection6;
                result.Duration = 999; // Whole fight
                result.Message += $" {caster.Name2} regains {result.Healing} hitpoints! (+{result.ProtectionBonus} defense)";
                break;

            case 7: // Fireball - Base: 60-70 damage
                int baseDamage7 = 60 + random.Next(11);
                result.Damage = ScaleSpellEffect(baseDamage7, caster, random, profMult);
                result.Message += $" {target?.Name2 ?? "The target"} is engulfed by a Fireball for {result.Damage} damage!";
                break;

            case 8: // Fear
                if (target != null)
                {
                    result.SpecialEffect = "fear";
                    result.Duration = (int)((random.Next(6) + 2 + (caster.Level / 15)) * profMult);
                    result.Message += $" {target.Name2} is overwhelmed by fear!";
                }
                break;

            case 9: // Lightning Bolt - Base: 60-70 damage
                int baseDamage9 = 60 + random.Next(11);
                result.Damage = ScaleSpellEffect(baseDamage9, caster, random, profMult);
                result.Message += $" {target?.Name2 ?? "The target"} is struck by Lightning for {result.Damage} damage!";
                break;

            case 10: // Prismatic Cage - Protection scales with level and proficiency
                int baseProtection10 = (int)((20 + (caster.Level / 4)) * profMult);
                result.ProtectionBonus = baseProtection10;
                result.Duration = 999; // Whole fight
                result.Message += $" A prismatic cage protects {caster.Name2}! (+{result.ProtectionBonus} defense)";
                break;

            case 11: // Pillar of Fire - Base: 110-112 damage
                int baseDamage11 = 110 + random.Next(3);
                result.Damage = ScaleSpellEffect(baseDamage11, caster, random, profMult);
                result.Message += $" {target?.Name2 ?? "The target"} burns in a Pillar of Fire for {result.Damage} damage!";
                break;

            case 12: // Power word KILL - Base: 220-265 damage
                int baseDamage12 = 220 + random.Next(46);
                result.Damage = ScaleSpellEffect(baseDamage12, caster, random, profMult);
                result.Message += $" The POWER WORD KILL strikes for {result.Damage} damage!";
                break;

            case 13: // Summon Demon - Attack bonus scales with level and proficiency
                int baseAttack13 = (int)((80 + (caster.Level * 2)) * profMult);
                result.AttackBonus = baseAttack13;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "demon";
                result.Message += $" A demon is summoned! (+{result.AttackBonus} attack)";
                break;
        }
    }
    
    /// <summary>
    /// Execute Sage spells (mind and nature magic)
    /// ALL DAMAGE AND HEALING NOW SCALES WITH CASTER LEVEL AND PROFICIENCY!
    /// </summary>
    private static void ExecuteSageSpell(Character caster, int spellLevel, Character target, List<Character> allTargets, SpellResult result, Random random, float profMult = 1.0f)
    {
        switch (spellLevel)
        {
            case 1: // Fog of War - Protection scales with level and proficiency
                int baseProtection1 = (int)((3 + (caster.Level / 15)) * profMult);
                result.ProtectionBonus = baseProtection1;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "fog";
                result.Message += $" A bank of mist lowers over the battle! (+{result.ProtectionBonus} defense)";
                break;

            case 2: // Poison
                if (target != null)
                {
                    result.SpecialEffect = "poison";
                    result.Duration = (int)((random.Next(8) + 3 + (caster.Level / 15)) * profMult);
                    result.Message += $" {target.Name2} is poisoned!";
                }
                break;

            case 3: // Freeze
                if (target != null)
                {
                    result.SpecialEffect = "freeze";
                    result.Duration = (int)((random.Next(5) + 2 + (caster.Level / 20)) * profMult);
                    result.Message += $" {target.Name2} is frozen in place!";
                }
                break;

            case 4: // Duplicate - Protection scales with level and proficiency
                int baseProtection4 = (int)((10 + (caster.Level / 6)) * profMult);
                result.ProtectionBonus = baseProtection4;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "duplicate";
                result.Message += $" A magical duplicate appears! (+{result.ProtectionBonus} defense)";
                break;

            case 5: // Roast - Base: 50-65 damage
                int baseDamage5 = 50 + random.Next(16);
                result.Damage = ScaleSpellEffect(baseDamage5, caster, random, profMult);
                result.Message += $" {target?.Name2 ?? "The target"} is struck by hellfire for {result.Damage} damage!";
                break;

            case 6: // Hit Self - Base: 70-80 damage
                int baseDamage6 = 70 + random.Next(11);
                result.Damage = ScaleSpellEffect(baseDamage6, caster, random, profMult);
                result.Message += $" {target?.Name2 ?? "The target"} hurts themselves for {result.Damage} damage!";
                break;

            case 7: // Escape - Success chance scales with level
                result.SpecialEffect = "escape";
                result.Message += $" {caster.Name2} attempts to escape from battle!";
                break;

            case 8: // Giant - Attack bonus scales with level and proficiency
                int baseAttack8 = (int)((25 + (caster.Level / 3)) * profMult);
                result.AttackBonus = baseAttack8;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "giant";
                result.Message += $" {caster.Name2} transforms into a GIANT! (+{result.AttackBonus} attack)";
                break;

            case 9: // Steal - Gold stolen scales with level
                result.SpecialEffect = "steal";
                result.Message += $" {caster.Name2} attempts to steal gold from enemies!";
                break;

            case 10: // Energy Drain - Base: 130-141 damage
                int baseDamage10 = 130 + random.Next(12);
                result.Damage = ScaleSpellEffect(baseDamage10, caster, random, profMult);
                result.Message += $" {target?.Name2 ?? "The target"}'s energy is drained for {result.Damage} damage!";
                break;

            case 11: // Summon Demon - Now summons AND does damage, both scale
                int baseDamage11 = 100 + random.Next(21);
                result.Damage = ScaleSpellEffect(baseDamage11, caster, random, profMult);
                int baseAttack11 = (int)((50 + (caster.Level)) * profMult);
                result.AttackBonus = baseAttack11;
                result.Duration = 999; // Whole fight
                result.SpecialEffect = "demon";
                result.Message += $" A Servant-Demon deals {result.Damage} damage! (+{result.AttackBonus} attack)";
                break;

            case 12: // Death Kiss - Base: 150-250 damage (massive scaling)
                int baseDamage12 = 150 + random.Next(100);
                result.Damage = ScaleSpellEffect(baseDamage12, caster, random, profMult);
                result.Message += $" The DEATH KISS drains life for {result.Damage} damage!";
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
