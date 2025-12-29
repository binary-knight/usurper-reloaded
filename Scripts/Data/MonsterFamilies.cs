using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Monster family system - defines monster types with tier progressions
/// Monsters scale from level 1-100 with distinct families and variations
/// </summary>
public static class MonsterFamilies
{
    /// <summary>
    /// Monster family definition with tier progression
    /// </summary>
    public class MonsterFamily
    {
        public string FamilyName { get; set; } = "";
        public string Description { get; set; } = "";
        public string BaseColor { get; set; } = "white";
        public List<MonsterTier> Tiers { get; set; } = new();
        public string AttackType { get; set; } = "physical"; // physical, magic, poison, fire, etc.
    }

    /// <summary>
    /// Individual tier within a monster family
    /// </summary>
    public class MonsterTier
    {
        public string Name { get; set; } = "";
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public string Color { get; set; } = "white";
        public float PowerMultiplier { get; set; } = 1.0f;
        public List<string> SpecialAbilities { get; set; } = new();
    }

    /// <summary>
    /// All monster families in the game
    /// </summary>
    public static readonly List<MonsterFamily> AllFamilies = new()
    {
        // GOBLINOID FAMILY
        new MonsterFamily
        {
            FamilyName = "Goblinoid",
            Description = "Small, cunning creatures that grow in power and organization",
            BaseColor = "green",
            AttackType = "physical",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Goblin", MinLevel = 1, MaxLevel = 10, Color = "green", PowerMultiplier = 0.8f },
                new() { Name = "Hobgoblin", MinLevel = 11, MaxLevel = 25, Color = "yellow", PowerMultiplier = 1.0f },
                new() { Name = "Goblin Champion", MinLevel = 26, MaxLevel = 50, Color = "bright_yellow", PowerMultiplier = 1.3f, SpecialAbilities = new() { "CriticalStrike" } },
                new() { Name = "Goblin Warlord", MinLevel = 51, MaxLevel = 75, Color = "bright_red", PowerMultiplier = 1.6f, SpecialAbilities = new() { "CriticalStrike", "Rally" } },
                new() { Name = "Goblin King", MinLevel = 76, MaxLevel = 100, Color = "magenta", PowerMultiplier = 2.0f, SpecialAbilities = new() { "CriticalStrike", "Rally", "CommandArmy" } }
            }
        },

        // UNDEAD FAMILY
        new MonsterFamily
        {
            FamilyName = "Undead",
            Description = "Cursed beings risen from death, immune to fear and pain",
            BaseColor = "gray",
            AttackType = "necrotic",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Zombie", MinLevel = 1, MaxLevel = 15, Color = "gray", PowerMultiplier = 0.9f },
                new() { Name = "Ghoul", MinLevel = 16, MaxLevel = 30, Color = "cyan", PowerMultiplier = 1.1f, SpecialAbilities = new() { "Paralyze" } },
                new() { Name = "Wight", MinLevel = 31, MaxLevel = 50, Color = "blue", PowerMultiplier = 1.4f, SpecialAbilities = new() { "LifeDrain" } },
                new() { Name = "Wraith", MinLevel = 51, MaxLevel = 75, Color = "bright_blue", PowerMultiplier = 1.7f, SpecialAbilities = new() { "LifeDrain", "Incorporeal" } },
                new() { Name = "Lich", MinLevel = 76, MaxLevel = 100, Color = "bright_magenta", PowerMultiplier = 2.2f, SpecialAbilities = new() { "LifeDrain", "Spellcasting", "Phylactery" } }
            }
        },

        // ORCISH FAMILY
        new MonsterFamily
        {
            FamilyName = "Orcish",
            Description = "Brutal warriors that live for battle and glory",
            BaseColor = "red",
            AttackType = "physical",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Orc", MinLevel = 5, MaxLevel = 20, Color = "red", PowerMultiplier = 1.0f },
                new() { Name = "Orc Warrior", MinLevel = 21, MaxLevel = 40, Color = "bright_red", PowerMultiplier = 1.3f, SpecialAbilities = new() { "Rage" } },
                new() { Name = "Orc Berserker", MinLevel = 41, MaxLevel = 60, Color = "bright_red", PowerMultiplier = 1.6f, SpecialAbilities = new() { "Rage", "Frenzy" } },
                new() { Name = "Orc Chieftain", MinLevel = 61, MaxLevel = 80, Color = "magenta", PowerMultiplier = 1.9f, SpecialAbilities = new() { "Rage", "Frenzy", "Warcry" } },
                new() { Name = "Orc Warlord", MinLevel = 81, MaxLevel = 100, Color = "bright_magenta", PowerMultiplier = 2.3f, SpecialAbilities = new() { "Rage", "Frenzy", "Warcry", "Cleave" } }
            }
        },

        // DRACONIC FAMILY
        new MonsterFamily
        {
            FamilyName = "Draconic",
            Description = "Ancient reptilian creatures of immense power",
            BaseColor = "bright_red",
            AttackType = "fire",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Kobold", MinLevel = 1, MaxLevel = 12, Color = "yellow", PowerMultiplier = 0.7f },
                new() { Name = "Drake", MinLevel = 13, MaxLevel = 30, Color = "bright_yellow", PowerMultiplier = 1.2f, SpecialAbilities = new() { "FireBreath" } },
                new() { Name = "Wyvern", MinLevel = 31, MaxLevel = 55, Color = "bright_red", PowerMultiplier = 1.5f, SpecialAbilities = new() { "FireBreath", "Flight" } },
                new() { Name = "Young Dragon", MinLevel = 56, MaxLevel = 80, Color = "bright_red", PowerMultiplier = 1.9f, SpecialAbilities = new() { "FireBreath", "Flight", "DragonFear" } },
                new() { Name = "Ancient Dragon", MinLevel = 81, MaxLevel = 100, Color = "bright_magenta", PowerMultiplier = 2.5f, SpecialAbilities = new() { "FireBreath", "Flight", "DragonFear", "AncientMagic" } }
            }
        },

        // DEMONIC FAMILY
        new MonsterFamily
        {
            FamilyName = "Demonic",
            Description = "Fiends from the lower planes, masters of corruption",
            BaseColor = "red",
            AttackType = "fire",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Imp", MinLevel = 8, MaxLevel = 20, Color = "red", PowerMultiplier = 0.9f, SpecialAbilities = new() { "Invisibility" } },
                new() { Name = "Demon", MinLevel = 21, MaxLevel = 40, Color = "bright_red", PowerMultiplier = 1.3f, SpecialAbilities = new() { "Teleport" } },
                new() { Name = "Greater Demon", MinLevel = 41, MaxLevel = 65, Color = "magenta", PowerMultiplier = 1.7f, SpecialAbilities = new() { "Teleport", "Hellfire" } },
                new() { Name = "Demon Lord", MinLevel = 66, MaxLevel = 85, Color = "bright_magenta", PowerMultiplier = 2.1f, SpecialAbilities = new() { "Teleport", "Hellfire", "Corruption" } },
                new() { Name = "Archfiend", MinLevel = 86, MaxLevel = 100, Color = "bright_magenta", PowerMultiplier = 2.6f, SpecialAbilities = new() { "Teleport", "Hellfire", "Corruption", "Dominate" } }
            }
        },

        // GIANT FAMILY
        new MonsterFamily
        {
            FamilyName = "Giant",
            Description = "Massive humanoids of incredible strength",
            BaseColor = "bright_white",
            AttackType = "physical",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Ogre", MinLevel = 10, MaxLevel = 25, Color = "white", PowerMultiplier = 1.2f },
                new() { Name = "Troll", MinLevel = 26, MaxLevel = 45, Color = "green", PowerMultiplier = 1.5f, SpecialAbilities = new() { "Regeneration" } },
                new() { Name = "Hill Giant", MinLevel = 46, MaxLevel = 65, Color = "bright_white", PowerMultiplier = 1.8f, SpecialAbilities = new() { "Boulder" } },
                new() { Name = "Stone Giant", MinLevel = 66, MaxLevel = 85, Color = "gray", PowerMultiplier = 2.2f, SpecialAbilities = new() { "Boulder", "Stoneskin" } },
                new() { Name = "Titan", MinLevel = 86, MaxLevel = 100, Color = "bright_yellow", PowerMultiplier = 2.7f, SpecialAbilities = new() { "Lightning", "Earthquake", "Stoneskin" } }
            }
        },

        // BEAST FAMILY
        new MonsterFamily
        {
            FamilyName = "Beast",
            Description = "Savage predators driven by primal hunger",
            BaseColor = "yellow",
            AttackType = "physical",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Wolf", MinLevel = 1, MaxLevel = 15, Color = "white", PowerMultiplier = 0.8f, SpecialAbilities = new() { "PackTactics" } },
                new() { Name = "Dire Wolf", MinLevel = 16, MaxLevel = 35, Color = "gray", PowerMultiplier = 1.1f, SpecialAbilities = new() { "PackTactics", "Bite" } },
                new() { Name = "Werewolf", MinLevel = 36, MaxLevel = 60, Color = "bright_white", PowerMultiplier = 1.5f, SpecialAbilities = new() { "PackTactics", "Lycanthropy", "Regeneration" } },
                new() { Name = "Alpha Werewolf", MinLevel = 61, MaxLevel = 80, Color = "bright_yellow", PowerMultiplier = 1.9f, SpecialAbilities = new() { "PackTactics", "Lycanthropy", "Regeneration", "Howl" } },
                new() { Name = "Fenrir", MinLevel = 81, MaxLevel = 100, Color = "bright_cyan", PowerMultiplier = 2.4f, SpecialAbilities = new() { "Devour", "Moonlight", "Regeneration" } }
            }
        },

        // ELEMENTAL FAMILY
        new MonsterFamily
        {
            FamilyName = "Elemental",
            Description = "Living manifestations of primal fire",
            BaseColor = "bright_red",
            AttackType = "fire",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Spark", MinLevel = 5, MaxLevel = 18, Color = "yellow", PowerMultiplier = 0.7f },
                new() { Name = "Fire Elemental", MinLevel = 19, MaxLevel = 38, Color = "bright_yellow", PowerMultiplier = 1.2f, SpecialAbilities = new() { "Burn" } },
                new() { Name = "Inferno", MinLevel = 39, MaxLevel = 60, Color = "bright_red", PowerMultiplier = 1.6f, SpecialAbilities = new() { "Burn", "Immolate" } },
                new() { Name = "Fire Lord", MinLevel = 61, MaxLevel = 85, Color = "bright_red", PowerMultiplier = 2.0f, SpecialAbilities = new() { "Burn", "Immolate", "Fireball" } },
                new() { Name = "Phoenix", MinLevel = 86, MaxLevel = 100, Color = "bright_magenta", PowerMultiplier = 2.5f, SpecialAbilities = new() { "Rebirth", "Immolate", "Inferno" } }
            }
        },

        // ABERRATION FAMILY
        new MonsterFamily
        {
            FamilyName = "Aberration",
            Description = "Amorphous horrors from beyond reality",
            BaseColor = "green",
            AttackType = "acid",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Ooze", MinLevel = 3, MaxLevel = 15, Color = "green", PowerMultiplier = 0.6f, SpecialAbilities = new() { "Corrosion" } },
                new() { Name = "Slime", MinLevel = 16, MaxLevel = 32, Color = "bright_green", PowerMultiplier = 1.0f, SpecialAbilities = new() { "Corrosion", "Split" } },
                new() { Name = "Gelatinous Cube", MinLevel = 33, MaxLevel = 55, Color = "cyan", PowerMultiplier = 1.4f, SpecialAbilities = new() { "Engulf", "Corrosion" } },
                new() { Name = "Elder Ooze", MinLevel = 56, MaxLevel = 80, Color = "bright_cyan", PowerMultiplier = 1.8f, SpecialAbilities = new() { "Engulf", "Corrosion", "Absorb" } },
                new() { Name = "Shoggoth", MinLevel = 81, MaxLevel = 100, Color = "bright_magenta", PowerMultiplier = 2.3f, SpecialAbilities = new() { "Engulf", "ShapeShift", "Madness" } }
            }
        },

        // INSECTOID FAMILY
        new MonsterFamily
        {
            FamilyName = "Insectoid",
            Description = "Chitinous horrors from the deep places",
            BaseColor = "white",
            AttackType = "poison",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Giant Spider", MinLevel = 5, MaxLevel = 18, Color = "white", PowerMultiplier = 0.9f, SpecialAbilities = new() { "WebTrap" } },
                new() { Name = "Phase Spider", MinLevel = 19, MaxLevel = 38, Color = "gray", PowerMultiplier = 1.2f, SpecialAbilities = new() { "WebTrap", "PhaseShift" } },
                new() { Name = "Spider Queen", MinLevel = 39, MaxLevel = 62, Color = "bright_white", PowerMultiplier = 1.6f, SpecialAbilities = new() { "WebTrap", "Poison", "SummonSpiders" } },
                new() { Name = "Arachnid Horror", MinLevel = 63, MaxLevel = 85, Color = "bright_red", PowerMultiplier = 2.0f, SpecialAbilities = new() { "DeadlyVenom", "WebTrap", "SummonSpiders" } },
                new() { Name = "Broodmother", MinLevel = 86, MaxLevel = 100, Color = "bright_magenta", PowerMultiplier = 2.4f, SpecialAbilities = new() { "DeadlyVenom", "Swarm", "Cocoon" } }
            }
        },

        // CONSTRUCT FAMILY
        new MonsterFamily
        {
            FamilyName = "Construct",
            Description = "Animated objects and golems, relentless and unfeeling",
            BaseColor = "gray",
            AttackType = "physical",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Animated Armor", MinLevel = 8, MaxLevel = 22, Color = "white", PowerMultiplier = 1.0f },
                new() { Name = "Golem", MinLevel = 23, MaxLevel = 45, Color = "gray", PowerMultiplier = 1.4f, SpecialAbilities = new() { "ImmuneMagic" } },
                new() { Name = "Iron Golem", MinLevel = 46, MaxLevel = 68, Color = "bright_white", PowerMultiplier = 1.8f, SpecialAbilities = new() { "ImmuneMagic", "PoisonGas" } },
                new() { Name = "Adamantine Golem", MinLevel = 69, MaxLevel = 88, Color = "bright_cyan", PowerMultiplier = 2.2f, SpecialAbilities = new() { "ImmuneMagic", "Indestructible" } },
                new() { Name = "Ancient Construct", MinLevel = 89, MaxLevel = 100, Color = "bright_yellow", PowerMultiplier = 2.6f, SpecialAbilities = new() { "ImmuneMagic", "SelfRepair", "Overload" } }
            }
        },

        // FEY FAMILY
        new MonsterFamily
        {
            FamilyName = "Fey",
            Description = "Magical beings from the feywild, tricksters and nature guardians",
            BaseColor = "green",
            AttackType = "magic",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Pixie", MinLevel = 1, MaxLevel = 15, Color = "cyan", PowerMultiplier = 0.6f, SpecialAbilities = new() { "Invisibility" } },
                new() { Name = "Sprite", MinLevel = 16, MaxLevel = 32, Color = "bright_cyan", PowerMultiplier = 0.9f, SpecialAbilities = new() { "Invisibility", "Sleep" } },
                new() { Name = "Dryad", MinLevel = 33, MaxLevel = 55, Color = "green", PowerMultiplier = 1.3f, SpecialAbilities = new() { "TreeMeld", "Charm" } },
                new() { Name = "Treant", MinLevel = 56, MaxLevel = 80, Color = "bright_green", PowerMultiplier = 1.8f, SpecialAbilities = new() { "AnimateTrees", "RootEntangle" } },
                new() { Name = "Archfey", MinLevel = 81, MaxLevel = 100, Color = "bright_magenta", PowerMultiplier = 2.3f, SpecialAbilities = new() { "Dominate", "TimeStop", "WildShape" } }
            }
        },

        // AQUATIC FAMILY
        new MonsterFamily
        {
            FamilyName = "Aquatic",
            Description = "Terrors from the deep ocean",
            BaseColor = "blue",
            AttackType = "physical",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Merrow", MinLevel = 12, MaxLevel = 28, Color = "blue", PowerMultiplier = 1.0f },
                new() { Name = "Sea Troll", MinLevel = 29, MaxLevel = 50, Color = "bright_blue", PowerMultiplier = 1.4f, SpecialAbilities = new() { "Regeneration" } },
                new() { Name = "Kraken Spawn", MinLevel = 51, MaxLevel = 72, Color = "bright_cyan", PowerMultiplier = 1.7f, SpecialAbilities = new() { "TentacleGrab", "InkCloud" } },
                new() { Name = "Leviathan", MinLevel = 73, MaxLevel = 90, Color = "bright_blue", PowerMultiplier = 2.1f, SpecialAbilities = new() { "TentacleGrab", "Devour", "Whirlpool" } },
                new() { Name = "Elder Kraken", MinLevel = 91, MaxLevel = 100, Color = "bright_magenta", PowerMultiplier = 2.5f, SpecialAbilities = new() { "TentacleGrab", "Devour", "TidalWave" } }
            }
        },

        // CELESTIAL FAMILY
        new MonsterFamily
        {
            FamilyName = "Celestial",
            Description = "Divine warriors from the upper planes",
            BaseColor = "bright_white",
            AttackType = "holy",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Angel", MinLevel = 20, MaxLevel = 38, Color = "bright_white", PowerMultiplier = 1.2f, SpecialAbilities = new() { "Heal", "Flight" } },
                new() { Name = "Archangel", MinLevel = 39, MaxLevel = 58, Color = "bright_yellow", PowerMultiplier = 1.6f, SpecialAbilities = new() { "Heal", "Flight", "HolySmite" } },
                new() { Name = "Seraph", MinLevel = 59, MaxLevel = 78, Color = "bright_yellow", PowerMultiplier = 2.0f, SpecialAbilities = new() { "Heal", "Flight", "HolySmite", "Purify" } },
                new() { Name = "Throne", MinLevel = 79, MaxLevel = 92, Color = "bright_cyan", PowerMultiplier = 2.4f, SpecialAbilities = new() { "Flight", "DivineJudgment", "Sanctuary" } },
                new() { Name = "Empyrean", MinLevel = 93, MaxLevel = 100, Color = "bright_magenta", PowerMultiplier = 2.8f, SpecialAbilities = new() { "Flight", "DivineJudgment", "Resurrection" } }
            }
        },

        // SHADOW FAMILY
        new MonsterFamily
        {
            FamilyName = "Shadow",
            Description = "Dark entities from the plane of shadow",
            BaseColor = "gray",
            AttackType = "necrotic",
            Tiers = new List<MonsterTier>
            {
                new() { Name = "Shadow", MinLevel = 15, MaxLevel = 32, Color = "gray", PowerMultiplier = 1.0f, SpecialAbilities = new() { "Incorporeal" } },
                new() { Name = "Shade", MinLevel = 33, MaxLevel = 52, Color = "blue", PowerMultiplier = 1.3f, SpecialAbilities = new() { "Incorporeal", "StrengthDrain" } },
                new() { Name = "Nightshade", MinLevel = 53, MaxLevel = 72, Color = "bright_blue", PowerMultiplier = 1.7f, SpecialAbilities = new() { "Incorporeal", "StrengthDrain", "Terror" } },
                new() { Name = "Umbral Horror", MinLevel = 73, MaxLevel = 90, Color = "magenta", PowerMultiplier = 2.1f, SpecialAbilities = new() { "Incorporeal", "Possess", "Nightmare" } },
                new() { Name = "Void Entity", MinLevel = 91, MaxLevel = 100, Color = "bright_magenta", PowerMultiplier = 2.6f, SpecialAbilities = new() { "Incorporeal", "DevourSoul", "RealityBreak" } }
            }
        }
    };

    /// <summary>
    /// Get appropriate monster tier for a given level
    /// </summary>
    public static (MonsterFamily family, MonsterTier tier) GetMonsterForLevel(int level, Random random = null)
    {
        random ??= new Random();

        // Get all families that have tiers for this level
        var suitableFamilies = AllFamilies
            .Where(f => f.Tiers.Any(t => level >= t.MinLevel && level <= t.MaxLevel))
            .ToList();

        if (suitableFamilies.Count == 0)
        {
            // Fallback to goblin
            var goblinFamily = AllFamilies.First(f => f.FamilyName == "Goblinoid");
            var fallbackTier = goblinFamily.Tiers.First();
            return (goblinFamily, fallbackTier);
        }

        // Pick random family
        var family = suitableFamilies[random.Next(suitableFamilies.Count)];

        // Get appropriate tier for this level
        var tier = family.Tiers.FirstOrDefault(t => level >= t.MinLevel && level <= t.MaxLevel)
                   ?? family.Tiers.Last();

        return (family, tier);
    }

    /// <summary>
    /// Get monster name with color
    /// </summary>
    public static string GetColoredMonsterName(string name, string color)
    {
        return $"[{color}]{name}[/]";
    }
}
