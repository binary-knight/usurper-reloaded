using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UsurperRemake.Utils;

/// <summary>
/// Class Ability System - Manages combat abilities for all classes
/// Spell classes get spells, Non-spell classes get unique combat abilities
/// All classes can learn abilities appropriate to their archetype
/// </summary>
public static class ClassAbilitySystem
{
    /// <summary>
    /// Represents a combat ability that can be used in battle
    /// </summary>
    public class ClassAbility
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int LevelRequired { get; set; }
        public int StaminaCost { get; set; }
        public int Cooldown { get; set; } // Combat rounds before can use again
        public AbilityType Type { get; set; }
        public CharacterClass[] AvailableToClasses { get; set; } = Array.Empty<CharacterClass>();

        // Effect values
        public int BaseDamage { get; set; }
        public int BaseHealing { get; set; }
        public int DefenseBonus { get; set; }
        public int AttackBonus { get; set; }
        public int Duration { get; set; } // Combat rounds
        public string SpecialEffect { get; set; } = "";
    }

    public enum AbilityType
    {
        Attack,      // Direct damage
        Defense,     // Defensive stance/buff
        Utility,     // Escape, steal, etc.
        Buff,        // Self or ally buff
        Debuff,      // Enemy debuff
        Heal         // Self-healing
    }

    /// <summary>
    /// All available class abilities
    /// </summary>
    private static readonly Dictionary<string, ClassAbility> AllAbilities = new()
    {
        // ============ WARRIOR ABILITIES ============
        ["power_strike"] = new ClassAbility
        {
            Id = "power_strike",
            Name = "Power Strike",
            Description = "A devastating two-handed blow that deals massive damage.",
            LevelRequired = 5,
            StaminaCost = 20,
            Cooldown = 0,
            Type = AbilityType.Attack,
            BaseDamage = 50,
            AvailableToClasses = new[] { CharacterClass.Warrior, CharacterClass.Barbarian, CharacterClass.Paladin }
        },
        ["shield_wall"] = new ClassAbility
        {
            Id = "shield_wall",
            Name = "Shield Wall",
            Description = "Raise your shield high, greatly increasing defense for several rounds.",
            LevelRequired = 10,
            StaminaCost = 25,
            Cooldown = 3,
            Type = AbilityType.Defense,
            DefenseBonus = 30,
            Duration = 3,
            AvailableToClasses = new[] { CharacterClass.Warrior, CharacterClass.Paladin }
        },
        ["battle_cry"] = new ClassAbility
        {
            Id = "battle_cry",
            Name = "Battle Cry",
            Description = "A thunderous war cry that boosts your attack power.",
            LevelRequired = 15,
            StaminaCost = 30,
            Cooldown = 4,
            Type = AbilityType.Buff,
            AttackBonus = 40,
            Duration = 4,
            AvailableToClasses = new[] { CharacterClass.Warrior, CharacterClass.Barbarian }
        },
        ["execute"] = new ClassAbility
        {
            Id = "execute",
            Name = "Execute",
            Description = "A finishing blow that deals extra damage to wounded enemies.",
            LevelRequired = 25,
            StaminaCost = 40,
            Cooldown = 2,
            Type = AbilityType.Attack,
            BaseDamage = 100,
            SpecialEffect = "execute", // Double damage if target below 30% HP
            AvailableToClasses = new[] { CharacterClass.Warrior, CharacterClass.Barbarian, CharacterClass.Assassin }
        },
        ["last_stand"] = new ClassAbility
        {
            Id = "last_stand",
            Name = "Last Stand",
            Description = "When near death, channel your remaining strength into a powerful counterattack.",
            LevelRequired = 40,
            StaminaCost = 50,
            Cooldown = 5,
            Type = AbilityType.Attack,
            BaseDamage = 150,
            SpecialEffect = "last_stand", // Extra damage when HP low
            AvailableToClasses = new[] { CharacterClass.Warrior, CharacterClass.Barbarian, CharacterClass.Paladin }
        },
        ["whirlwind"] = new ClassAbility
        {
            Id = "whirlwind",
            Name = "Whirlwind",
            Description = "Spin with weapon extended, striking all nearby enemies.",
            LevelRequired = 50,
            StaminaCost = 60,
            Cooldown = 3,
            Type = AbilityType.Attack,
            BaseDamage = 80,
            SpecialEffect = "aoe", // Hits all monsters in group
            AvailableToClasses = new[] { CharacterClass.Warrior, CharacterClass.Barbarian }
        },

        // ============ BARBARIAN ABILITIES ============
        ["rage"] = new ClassAbility
        {
            Id = "rage",
            Name = "Berserker Rage",
            Description = "Enter a blood rage, greatly increasing damage but lowering defense.",
            LevelRequired = 8,
            StaminaCost = 35,
            Cooldown = 5,
            Type = AbilityType.Buff,
            AttackBonus = 60,
            DefenseBonus = -20,
            Duration = 5,
            SpecialEffect = "rage",
            AvailableToClasses = new[] { CharacterClass.Barbarian }
        },
        ["reckless_attack"] = new ClassAbility
        {
            Id = "reckless_attack",
            Name = "Reckless Attack",
            Description = "Throw caution to the wind for a devastating but risky attack.",
            LevelRequired = 12,
            StaminaCost = 25,
            Cooldown = 1,
            Type = AbilityType.Attack,
            BaseDamage = 80,
            SpecialEffect = "reckless", // High damage, enemy gets bonus on next attack
            AvailableToClasses = new[] { CharacterClass.Barbarian }
        },
        ["intimidate"] = new ClassAbility
        {
            Id = "intimidate",
            Name = "Intimidating Roar",
            Description = "A terrifying roar that may cause enemies to flee or fight with reduced effectiveness.",
            LevelRequired = 20,
            StaminaCost = 30,
            Cooldown = 4,
            Type = AbilityType.Debuff,
            SpecialEffect = "fear",
            Duration = 3,
            AvailableToClasses = new[] { CharacterClass.Barbarian }
        },
        ["bloodlust"] = new ClassAbility
        {
            Id = "bloodlust",
            Name = "Bloodlust",
            Description = "Each kill fuels your fury, healing you and increasing damage.",
            LevelRequired = 35,
            StaminaCost = 40,
            Cooldown = 6,
            Type = AbilityType.Buff,
            BaseHealing = 30,
            AttackBonus = 25,
            Duration = 999,
            SpecialEffect = "bloodlust", // Heals on kill
            AvailableToClasses = new[] { CharacterClass.Barbarian }
        },

        // ============ PALADIN ABILITIES ============
        ["lay_on_hands"] = new ClassAbility
        {
            Id = "lay_on_hands",
            Name = "Lay on Hands",
            Description = "Channel divine power to heal your wounds.",
            LevelRequired = 5,
            StaminaCost = 30,
            Cooldown = 4,
            Type = AbilityType.Heal,
            BaseHealing = 50,
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },
        ["divine_smite"] = new ClassAbility
        {
            Id = "divine_smite",
            Name = "Divine Smite",
            Description = "Channel holy energy through your weapon for extra damage. Extra effective against undead.",
            LevelRequired = 10,
            StaminaCost = 35,
            Cooldown = 2,
            Type = AbilityType.Attack,
            BaseDamage = 70,
            SpecialEffect = "holy", // Double damage vs undead
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },
        ["aura_of_protection"] = new ClassAbility
        {
            Id = "aura_of_protection",
            Name = "Aura of Protection",
            Description = "Project a protective aura that increases your defense significantly.",
            LevelRequired = 18,
            StaminaCost = 40,
            Cooldown = 5,
            Type = AbilityType.Defense,
            DefenseBonus = 50,
            Duration = 4,
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },
        ["holy_avenger"] = new ClassAbility
        {
            Id = "holy_avenger",
            Name = "Holy Avenger",
            Description = "Call upon divine wrath to smite evil with righteous fury.",
            LevelRequired = 30,
            StaminaCost = 60,
            Cooldown = 5,
            Type = AbilityType.Attack,
            BaseDamage = 120,
            SpecialEffect = "holy_avenger", // Massive damage vs evil/undead/demon
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },

        // ============ ASSASSIN ABILITIES ============
        ["backstab"] = new ClassAbility
        {
            Id = "backstab",
            Name = "Backstab",
            Description = "Strike from the shadows for critical damage.",
            LevelRequired = 5,
            StaminaCost = 25,
            Cooldown = 1,
            Type = AbilityType.Attack,
            BaseDamage = 60,
            SpecialEffect = "critical", // High crit chance
            AvailableToClasses = new[] { CharacterClass.Assassin }
        },
        ["poison_blade"] = new ClassAbility
        {
            Id = "poison_blade",
            Name = "Poison Blade",
            Description = "Coat your weapon with deadly poison that deals damage over time.",
            LevelRequired = 10,
            StaminaCost = 30,
            Cooldown = 3,
            Type = AbilityType.Attack,
            BaseDamage = 30,
            SpecialEffect = "poison",
            Duration = 5,
            AvailableToClasses = new[] { CharacterClass.Assassin }
        },
        ["shadow_step"] = new ClassAbility
        {
            Id = "shadow_step",
            Name = "Shadow Step",
            Description = "Disappear into shadows, becoming nearly impossible to hit.",
            LevelRequired = 15,
            StaminaCost = 35,
            Cooldown = 4,
            Type = AbilityType.Defense,
            DefenseBonus = 60,
            Duration = 2,
            SpecialEffect = "evasion",
            AvailableToClasses = new[] { CharacterClass.Assassin }
        },
        ["death_mark"] = new ClassAbility
        {
            Id = "death_mark",
            Name = "Death Mark",
            Description = "Mark a target for death, causing all your attacks to deal bonus damage.",
            LevelRequired = 25,
            StaminaCost = 45,
            Cooldown = 5,
            Type = AbilityType.Debuff,
            AttackBonus = 50,
            Duration = 4,
            SpecialEffect = "marked",
            AvailableToClasses = new[] { CharacterClass.Assassin }
        },
        ["assassinate"] = new ClassAbility
        {
            Id = "assassinate",
            Name = "Assassinate",
            Description = "A lethal strike aimed at vital points. Can instantly kill weakened enemies.",
            LevelRequired = 40,
            StaminaCost = 70,
            Cooldown = 6,
            Type = AbilityType.Attack,
            BaseDamage = 200,
            SpecialEffect = "instant_kill", // Chance to instant kill if target below 20% HP
            AvailableToClasses = new[] { CharacterClass.Assassin }
        },

        // ============ RANGER ABILITIES ============
        ["precise_shot"] = new ClassAbility
        {
            Id = "precise_shot",
            Name = "Precise Shot",
            Description = "Take careful aim for a guaranteed hit with bonus damage.",
            LevelRequired = 5,
            StaminaCost = 20,
            Cooldown = 1,
            Type = AbilityType.Attack,
            BaseDamage = 45,
            SpecialEffect = "guaranteed_hit",
            AvailableToClasses = new[] { CharacterClass.Ranger }
        },
        ["hunters_mark"] = new ClassAbility
        {
            Id = "hunters_mark",
            Name = "Hunter's Mark",
            Description = "Mark your prey, increasing all damage dealt to them.",
            LevelRequired = 8,
            StaminaCost = 25,
            Cooldown = 4,
            Type = AbilityType.Debuff,
            AttackBonus = 30,
            Duration = 5,
            SpecialEffect = "marked",
            AvailableToClasses = new[] { CharacterClass.Ranger }
        },
        ["evasive_roll"] = new ClassAbility
        {
            Id = "evasive_roll",
            Name = "Evasive Roll",
            Description = "Roll away from danger, avoiding the next attack entirely.",
            LevelRequired = 12,
            StaminaCost = 30,
            Cooldown = 3,
            Type = AbilityType.Defense,
            DefenseBonus = 100,
            Duration = 1,
            SpecialEffect = "dodge_next",
            AvailableToClasses = new[] { CharacterClass.Ranger, CharacterClass.Assassin }
        },
        ["volley"] = new ClassAbility
        {
            Id = "volley",
            Name = "Volley",
            Description = "Fire multiple arrows at all enemies in rapid succession.",
            LevelRequired = 25,
            StaminaCost = 50,
            Cooldown = 4,
            Type = AbilityType.Attack,
            BaseDamage = 50,
            SpecialEffect = "aoe",
            AvailableToClasses = new[] { CharacterClass.Ranger }
        },
        ["natures_blessing"] = new ClassAbility
        {
            Id = "natures_blessing",
            Name = "Nature's Blessing",
            Description = "Call upon nature spirits to heal your wounds.",
            LevelRequired = 20,
            StaminaCost = 40,
            Cooldown = 5,
            Type = AbilityType.Heal,
            BaseHealing = 80,
            AvailableToClasses = new[] { CharacterClass.Ranger }
        },

        // ============ JESTER/BARD ABILITIES ============
        ["mock"] = new ClassAbility
        {
            Id = "mock",
            Name = "Vicious Mockery",
            Description = "Unleash scathing insults that distract and damage enemies.",
            LevelRequired = 5,
            StaminaCost = 15,
            Cooldown = 1,
            Type = AbilityType.Attack,
            BaseDamage = 30,
            SpecialEffect = "distract", // Enemy has reduced accuracy
            AvailableToClasses = new[] { CharacterClass.Jester, CharacterClass.Bard }
        },
        ["inspiring_tune"] = new ClassAbility
        {
            Id = "inspiring_tune",
            Name = "Inspiring Tune",
            Description = "Play an inspiring melody that boosts your attack and defense.",
            LevelRequired = 10,
            StaminaCost = 30,
            Cooldown = 4,
            Type = AbilityType.Buff,
            AttackBonus = 20,
            DefenseBonus = 20,
            Duration = 4,
            AvailableToClasses = new[] { CharacterClass.Bard }
        },
        ["disappearing_act"] = new ClassAbility
        {
            Id = "disappearing_act",
            Name = "Disappearing Act",
            Description = "Perform a magical trick to escape combat entirely.",
            LevelRequired = 15,
            StaminaCost = 40,
            Cooldown = 6,
            Type = AbilityType.Utility,
            SpecialEffect = "escape",
            AvailableToClasses = new[] { CharacterClass.Jester }
        },
        ["charm"] = new ClassAbility
        {
            Id = "charm",
            Name = "Charming Performance",
            Description = "Use your charisma to confuse and beguile your enemy.",
            LevelRequired = 20,
            StaminaCost = 35,
            Cooldown = 4,
            Type = AbilityType.Debuff,
            SpecialEffect = "charm",
            Duration = 3,
            AvailableToClasses = new[] { CharacterClass.Jester, CharacterClass.Bard }
        },
        ["song_of_rest"] = new ClassAbility
        {
            Id = "song_of_rest",
            Name = "Song of Rest",
            Description = "Play a soothing melody that heals your wounds.",
            LevelRequired = 12,
            StaminaCost = 35,
            Cooldown = 5,
            Type = AbilityType.Heal,
            BaseHealing = 60,
            AvailableToClasses = new[] { CharacterClass.Bard }
        },

        // ============ ALCHEMIST ABILITIES ============
        ["throw_bomb"] = new ClassAbility
        {
            Id = "throw_bomb",
            Name = "Throw Bomb",
            Description = "Hurl an explosive concoction at your enemies.",
            LevelRequired = 5,
            StaminaCost = 25,
            Cooldown = 2,
            Type = AbilityType.Attack,
            BaseDamage = 55,
            SpecialEffect = "fire",
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["healing_elixir"] = new ClassAbility
        {
            Id = "healing_elixir",
            Name = "Healing Elixir",
            Description = "Quickly drink a healing potion you've prepared.",
            LevelRequired = 8,
            StaminaCost = 20,
            Cooldown = 3,
            Type = AbilityType.Heal,
            BaseHealing = 70,
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["acid_splash"] = new ClassAbility
        {
            Id = "acid_splash",
            Name = "Acid Splash",
            Description = "Throw a vial of acid that melts through armor.",
            LevelRequired = 15,
            StaminaCost = 35,
            Cooldown = 3,
            Type = AbilityType.Attack,
            BaseDamage = 40,
            SpecialEffect = "armor_pierce", // Ignores defense
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["smoke_bomb"] = new ClassAbility
        {
            Id = "smoke_bomb",
            Name = "Smoke Bomb",
            Description = "Create a cloud of smoke to escape or confuse enemies.",
            LevelRequired = 12,
            StaminaCost = 30,
            Cooldown = 4,
            Type = AbilityType.Utility,
            DefenseBonus = 40,
            Duration = 2,
            SpecialEffect = "smoke",
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["mutagen"] = new ClassAbility
        {
            Id = "mutagen",
            Name = "Mutagen",
            Description = "Drink a mutagen that temporarily enhances your physical abilities.",
            LevelRequired = 25,
            StaminaCost = 50,
            Cooldown = 6,
            Type = AbilityType.Buff,
            AttackBonus = 35,
            DefenseBonus = 25,
            Duration = 5,
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },

        // ============ UNIVERSAL ABILITIES (Available to all) ============
        ["second_wind"] = new ClassAbility
        {
            Id = "second_wind",
            Name = "Second Wind",
            Description = "Catch your breath and recover some health.",
            LevelRequired = 1,
            StaminaCost = 30,
            Cooldown = 5,
            Type = AbilityType.Heal,
            BaseHealing = 30,
            AvailableToClasses = new[] {
                CharacterClass.Warrior, CharacterClass.Barbarian, CharacterClass.Paladin,
                CharacterClass.Assassin, CharacterClass.Ranger, CharacterClass.Jester,
                CharacterClass.Bard, CharacterClass.Alchemist
            }
        },
        ["focus"] = new ClassAbility
        {
            Id = "focus",
            Name = "Focus",
            Description = "Concentrate deeply, increasing the accuracy of your next attack.",
            LevelRequired = 3,
            StaminaCost = 15,
            Cooldown = 2,
            Type = AbilityType.Buff,
            AttackBonus = 25,
            Duration = 1,
            AvailableToClasses = new[] {
                CharacterClass.Warrior, CharacterClass.Barbarian, CharacterClass.Paladin,
                CharacterClass.Assassin, CharacterClass.Ranger, CharacterClass.Jester,
                CharacterClass.Bard, CharacterClass.Alchemist
            }
        }
    };

    /// <summary>
    /// Get all abilities available to a specific class
    /// </summary>
    public static List<ClassAbility> GetClassAbilities(CharacterClass characterClass)
    {
        return AllAbilities.Values
            .Where(a => a.AvailableToClasses.Contains(characterClass))
            .OrderBy(a => a.LevelRequired)
            .ToList();
    }

    /// <summary>
    /// Get abilities that a character can currently use (meets level requirement)
    /// </summary>
    public static List<ClassAbility> GetAvailableAbilities(Character character)
    {
        return AllAbilities.Values
            .Where(a => a.AvailableToClasses.Contains(character.Class) &&
                       character.Level >= a.LevelRequired)
            .OrderBy(a => a.LevelRequired)
            .ToList();
    }

    /// <summary>
    /// Get ability by ID
    /// </summary>
    public static ClassAbility? GetAbility(string abilityId)
    {
        return AllAbilities.TryGetValue(abilityId, out var ability) ? ability : null;
    }

    /// <summary>
    /// Check if character can use an ability (has stamina, not on cooldown)
    /// </summary>
    public static bool CanUseAbility(Character character, string abilityId, Dictionary<string, int> cooldowns)
    {
        var ability = GetAbility(abilityId);
        if (ability == null) return false;

        // Check class
        if (!ability.AvailableToClasses.Contains(character.Class)) return false;

        // Check level
        if (character.Level < ability.LevelRequired) return false;

        // Check stamina
        if (character.Stamina < ability.StaminaCost) return false;

        // Check cooldown
        if (cooldowns.TryGetValue(abilityId, out int remainingCooldown) && remainingCooldown > 0)
            return false;

        return true;
    }

    /// <summary>
    /// Use an ability and return the result
    /// </summary>
    public static ClassAbilityResult UseAbility(Character user, string abilityId, Random? random = null)
    {
        random ??= new Random();
        var ability = GetAbility(abilityId);
        var result = new ClassAbilityResult();

        if (ability == null)
        {
            result.Success = false;
            result.Message = "Unknown ability!";
            return result;
        }

        // Deduct stamina
        user.Stamina -= ability.StaminaCost;

        result.Success = true;
        result.AbilityUsed = ability;
        result.CooldownApplied = ability.Cooldown;

        // Calculate scaled effect values
        double levelScale = 1.0 + (user.Level * 0.02); // 2% per level

        // Stat scaling based on ability type
        double statScale = 1.0;
        if (ability.Type == AbilityType.Attack)
        {
            statScale += user.Strength * 0.003; // Strength for attacks
        }
        else if (ability.Type == AbilityType.Heal)
        {
            statScale += user.Constitution * 0.003; // Con for healing
        }
        else if (ability.Type == AbilityType.Defense)
        {
            statScale += user.Constitution * 0.002;
        }

        double totalScale = levelScale * statScale;

        // Apply effects
        if (ability.BaseDamage > 0)
        {
            result.Damage = (int)(ability.BaseDamage * totalScale * (0.9 + random.NextDouble() * 0.2));
        }

        if (ability.BaseHealing > 0)
        {
            result.Healing = (int)(ability.BaseHealing * totalScale * (0.9 + random.NextDouble() * 0.2));
        }

        if (ability.AttackBonus > 0)
        {
            result.AttackBonus = (int)(ability.AttackBonus * levelScale);
        }

        if (ability.DefenseBonus != 0) // Can be negative for rage
        {
            result.DefenseBonus = (int)(ability.DefenseBonus * levelScale);
        }

        result.Duration = ability.Duration;
        result.SpecialEffect = ability.SpecialEffect;

        // Generate message
        result.Message = $"{user.Name2} uses {ability.Name}!";

        return result;
    }

    /// <summary>
    /// Check if a class is a spellcaster (uses SpellSystem instead)
    /// </summary>
    public static bool IsSpellcaster(CharacterClass characterClass)
    {
        return characterClass == CharacterClass.Cleric ||
               characterClass == CharacterClass.Magician ||
               characterClass == CharacterClass.Sage;
    }

    /// <summary>
    /// Display the ability learning menu at the Level Master
    /// </summary>
    public static async Task ShowAbilityLearningMenu(Character player, TerminalEmulator terminal)
    {
        if (IsSpellcaster(player.Class))
        {
            // Spellcasters go to spell learning instead
            await SpellLearningSystem.ShowSpellLearningMenu(player, terminal);
            return;
        }

        while (true)
        {
            terminal.ClearScreen();
            terminal.WriteLine("═══ COMBAT ABILITIES ═══", "bright_yellow");
            terminal.WriteLine($"Class: {player.Class} | Level: {player.Level} | Stamina: {player.Stamina}", "cyan");
            terminal.WriteLine("");
            terminal.WriteLine("Lvl  Name                     Stamina  Description", "cyan");
            terminal.WriteLine("───────────────────────────────────────────────────────────────", "cyan");

            var classAbilities = GetClassAbilities(player.Class);
            int index = 1;

            foreach (var ability in classAbilities)
            {
                string levelMark = player.Level >= ability.LevelRequired ? "✓" : " ";
                string color = player.Level >= ability.LevelRequired ? "green" : "dark_gray";

                terminal.WriteLine($"{levelMark} {ability.LevelRequired,2}  {ability.Name,-24} {ability.StaminaCost,3}     {ability.Description}", color);
                index++;
            }

            terminal.WriteLine("");
            terminal.WriteLine("All abilities are automatically available when you reach the required level.", "yellow");
            terminal.WriteLine("Use them in combat with the (A)bility command.", "yellow");
            terminal.WriteLine("");
            terminal.WriteLine("Press any key to return...", "gray");

            await terminal.WaitForKey();
            break;
        }
    }
}

/// <summary>
/// Result of using a class ability (distinct from monster AbilityResult)
/// </summary>
public class ClassAbilityResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public ClassAbilitySystem.ClassAbility? AbilityUsed { get; set; }
    public int Damage { get; set; }
    public int Healing { get; set; }
    public int AttackBonus { get; set; }
    public int DefenseBonus { get; set; }
    public int Duration { get; set; }
    public string SpecialEffect { get; set; } = "";
    public int CooldownApplied { get; set; }
}
