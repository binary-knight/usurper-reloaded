using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Enhanced monster generation system
/// Creates monsters with scalable stats for levels 1-100
/// Integrates with MonsterFamilies for variety and color
/// </summary>
public static class MonsterGenerator
{
    /// <summary>
    /// Generate a monster for a specific dungeon level
    /// Uses monster families and balanced stat scaling
    /// </summary>
    public static Monster GenerateMonster(int dungeonLevel, bool isBoss = false, Random random = null)
    {
        random ??= new Random();

        // Get appropriate monster family and tier for this level
        var (family, tier) = MonsterFamilies.GetMonsterForLevel(dungeonLevel, random);

        // Calculate base stats using smooth scaling formulas
        var stats = CalculateMonsterStats(dungeonLevel, tier.PowerMultiplier, isBoss);

        // Create the monster
        var monster = Monster.CreateMonster(
            nr: dungeonLevel,
            name: tier.Name,
            hps: stats.HP,
            strength: stats.Strength,
            defence: stats.Defence,
            phrase: GetMonsterPhrase(family, tier),
            grabweap: random.NextDouble() < 0.15, // 15% chance to disarm
            grabarm: false,
            weapon: GetWeapon(dungeonLevel, random),
            armor: GetArmor(dungeonLevel, random),
            poisoned: false,
            disease: false,
            punch: stats.Punch,
            armpow: stats.ArmorPower,
            weappow: stats.WeaponPower
        );

        // Store family and tier info for combat messages
        monster.FamilyName = family.FamilyName;
        monster.TierName = tier.Name;
        monster.MonsterColor = tier.Color;
        monster.AttackType = family.AttackType;
        monster.Level = dungeonLevel;
        monster.IsBoss = isBoss;

        // Add special abilities
        foreach (var ability in tier.SpecialAbilities)
        {
            monster.SpecialAbilities.Add(ability);
        }

        return monster;
    }

    /// <summary>
    /// Monster stats calculated for a given level
    /// </summary>
    private class MonsterStats
    {
        public long HP { get; set; }
        public long Strength { get; set; }
        public long Defence { get; set; }
        public long Punch { get; set; }
        public long WeaponPower { get; set; }
        public long ArmorPower { get; set; }
    }

    /// <summary>
    /// Calculate balanced stats for a monster
    /// Formulas designed to scale smoothly from level 1 to 100
    /// Reduced exponential scaling to make combat more manageable
    /// </summary>
    private static MonsterStats CalculateMonsterStats(int level, float powerMultiplier, bool isBoss)
    {
        // Base multipliers for boss monsters
        float bossMultiplier = isBoss ? 1.8f : 1.0f;
        float totalMultiplier = powerMultiplier * bossMultiplier;

        // Gentler exponential scaling with linear base
        // Reduced exponents from 1.35/1.25/1.2 to 1.2/1.15/1.1
        // This makes monsters scale more linearly with level

        // HP scales moderately (reduced from 1.35 to 1.2)
        long baseHP = (long)((40 * level) + Math.Pow(level, 1.2) * 15);
        long hp = (long)(baseHP * totalMultiplier);

        // Strength scales gently (reduced from 1.25 to 1.15)
        long baseStrength = (long)((4 * level) + Math.Pow(level, 1.15) * 2);
        long strength = (long)(baseStrength * totalMultiplier);

        // Defence scales minimally (reduced from 1.2 to 1.1)
        long baseDefence = (long)((2 * level) + Math.Pow(level, 1.1) * 1.5);
        long defence = (long)(baseDefence * totalMultiplier * 0.7f);

        // Punch (natural attack bonus) - reduced scaling
        long basePunch = (long)((1.5 * level) + Math.Pow(level, 1.1) * 1);
        long punch = (long)(basePunch * totalMultiplier);

        // Weapon power - reduced scaling
        long baseWeaponPower = (long)((3 * level) + Math.Pow(level, 1.15) * 1.5);
        long weaponPower = (long)(baseWeaponPower * totalMultiplier);

        // Armor power - reduced to keep combat reasonable
        long baseArmorPower = (long)((2 * level) + Math.Pow(level, 1.1) * 1.5);
        long armorPower = (long)(baseArmorPower * totalMultiplier * 0.6f);

        return new MonsterStats
        {
            HP = Math.Max(15, hp),
            Strength = Math.Max(3, strength),
            Defence = Math.Max(0, defence),
            Punch = Math.Max(1, punch),
            WeaponPower = Math.Max(1, weaponPower),
            ArmorPower = Math.Max(0, armorPower)
        };
    }

    /// <summary>
    /// Get weapon for monster based on level
    /// </summary>
    private static string GetWeapon(int level, Random random)
    {
        return level switch
        {
            <= 10 => random.Next(4) switch
            {
                0 => "Rusty Dagger",
                1 => "Wooden Club",
                2 => "Short Sword",
                _ => "Crude Axe"
            },
            <= 25 => random.Next(4) switch
            {
                0 => "Iron Sword",
                1 => "Battle Axe",
                2 => "Mace",
                _ => "Spear"
            },
            <= 50 => random.Next(4) switch
            {
                0 => "Steel Longsword",
                1 => "War Hammer",
                2 => "Halberd",
                _ => "Greatsword"
            },
            <= 75 => random.Next(4) switch
            {
                0 => "Enchanted Blade",
                1 => "Mythril Axe",
                2 => "Flaming Sword",
                3 => "Dragonbone Mace"
            },
            _ => random.Next(5) switch
            {
                0 => "Legendary Sword",
                1 => "Vorpal Blade",
                2 => "Soul Reaver",
                3 => "Doom Bringer",
                _ => "Ancient Artifact Weapon"
            }
        };
    }

    /// <summary>
    /// Get armor for monster based on level
    /// </summary>
    private static string GetArmor(int level, Random random)
    {
        return level switch
        {
            <= 10 => random.Next(4) switch
            {
                0 => "Leather Armor",
                1 => "Tattered Robes",
                2 => "Hide Armor",
                _ => "None"
            },
            <= 25 => random.Next(4) switch
            {
                0 => "Chain Mail",
                1 => "Studded Leather",
                2 => "Scale Mail",
                _ => "Ring Mail"
            },
            <= 50 => random.Next(4) switch
            {
                0 => "Plate Armor",
                1 => "Splint Mail",
                2 => "Banded Mail",
                _ => "Full Plate"
            },
            <= 75 => random.Next(4) switch
            {
                0 => "Enchanted Plate",
                1 => "Mythril Armor",
                2 => "Dragonscale Mail",
                3 => "Elven Chain"
            },
            _ => random.Next(5) switch
            {
                0 => "Legendary Armor",
                1 => "Adamantine Plate",
                2 => "Divine Vestments",
                3 => "Demon Plate",
                _ => "Ancient Artifact Armor"
            }
        };
    }

    /// <summary>
    /// Get flavor text phrase for monster
    /// </summary>
    private static string GetMonsterPhrase(MonsterFamilies.MonsterFamily family, MonsterFamilies.MonsterTier tier)
    {
        // Family-specific battle cries and phrases
        return family.FamilyName switch
        {
            "Goblinoid" => tier.Name switch
            {
                "Goblin" => "Grrr! Me smash you!",
                "Hobgoblin" => "You die now, weakling!",
                "Goblin Champion" => "For the tribe! Attack!",
                "Goblin Warlord" => "I'll mount your head on my wall!",
                "Goblin King" => "Kneel before your king, or perish!",
                _ => "Prepare to die!"
            },
            "Undead" => "The living shall join the dead...",
            "Orcish" => tier.Name switch
            {
                "Orc" => "Blood and skulls!",
                "Orc Warrior" => "Die, human scum!",
                "Orc Berserker" => "BLOOD! BLOOD! BLOOD!",
                "Orc Chieftain" => "I'll tear you limb from limb!",
                "Orc Warlord" => "Your skull will join my throne!",
                _ => "For the Horde!"
            },
            "Draconic" => tier.Name switch
            {
                "Kobold" => "You no scare kobold!",
                "Drake" => "*Roars and breathes fire*",
                "Wyvern" => "You shall burn!",
                "Young Dragon" => "I am fire incarnate!",
                "Ancient Dragon" => "Mortals... such fleeting things.",
                _ => "*Roars*"
            },
            "Demonic" => "Your soul is mine!",
            "Giant" => "Fe Fi Fo Fum!",
            "Beast" => "*Snarls and growls*",
            "Elemental" => "Burn! BURN!",
            "Aberration" => "*Wet squelching sounds*",
            "Insectoid" => "*Chittering and hissing*",
            "Construct" => "*Mechanical grinding noises*",
            "Fey" => "Let's play a game...",
            "Aquatic" => "*Gurgling roar*",
            "Celestial" => "You have been judged and found wanting.",
            "Shadow" => "Embrace the darkness...",
            _ => "Prepare for battle!"
        };
    }

    /// <summary>
    /// Generate a group of monsters for an encounter
    /// Group size and composition based on dungeon level
    /// </summary>
    public static List<Monster> GenerateMonsterGroup(int dungeonLevel, Random random = null)
    {
        random ??= new Random();
        var monsters = new List<Monster>();

        // 10% chance for boss encounter (single powerful monster)
        if (random.NextDouble() < 0.10)
        {
            monsters.Add(GenerateMonster(dungeonLevel, isBoss: true, random));
            return monsters;
        }

        // Regular encounter - 1 to 5 monsters
        // Higher levels tend toward more monsters
        int groupSize = dungeonLevel switch
        {
            <= 10 => random.Next(1, 3),   // 1-2 monsters early game
            <= 30 => random.Next(1, 4),   // 1-3 monsters mid-early
            <= 60 => random.Next(2, 5),   // 2-4 monsters mid-late
            _ => random.Next(3, 6)        // 3-5 monsters late game
        };

        // Generate monsters (usually same family for thematic encounters)
        bool sameFamily = random.NextDouble() < 0.7; // 70% chance for same family

        if (sameFamily)
        {
            // Pick one family and tier
            var (family, tier) = MonsterFamilies.GetMonsterForLevel(dungeonLevel, random);

            for (int i = 0; i < groupSize; i++)
            {
                var stats = CalculateMonsterStats(dungeonLevel, tier.PowerMultiplier, false);

                var monster = Monster.CreateMonster(
                    nr: dungeonLevel,
                    name: tier.Name,
                    hps: stats.HP,
                    strength: stats.Strength,
                    defence: stats.Defence,
                    phrase: GetMonsterPhrase(family, tier),
                    grabweap: random.NextDouble() < 0.15,
                    grabarm: false,
                    weapon: GetWeapon(dungeonLevel, random),
                    armor: GetArmor(dungeonLevel, random),
                    poisoned: false,
                    disease: false,
                    punch: stats.Punch,
                    armpow: stats.ArmorPower,
                    weappow: stats.WeaponPower
                );

                monster.FamilyName = family.FamilyName;
                monster.TierName = tier.Name;
                monster.MonsterColor = tier.Color;
                monster.AttackType = family.AttackType;
                monster.Level = dungeonLevel;

                foreach (var ability in tier.SpecialAbilities)
                {
                    monster.SpecialAbilities.Add(ability);
                }

                monsters.Add(monster);
            }
        }
        else
        {
            // Mixed family encounter
            for (int i = 0; i < groupSize; i++)
            {
                monsters.Add(GenerateMonster(dungeonLevel, false, random));
            }
        }

        return monsters;
    }
}
