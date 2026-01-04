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
    /// All available class abilities - expanded to ~10 per class spread across levels 1-100
    /// Themed around game lore: Maelketh (War), Old Gods, and class fantasy
    /// </summary>
    private static readonly Dictionary<string, ClassAbility> AllAbilities = new()
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // WARRIOR ABILITIES - Masters of martial combat, disciples of Maelketh
        // ═══════════════════════════════════════════════════════════════════════════════
        ["power_strike"] = new ClassAbility
        {
            Id = "power_strike",
            Name = "Power Strike",
            Description = "A devastating two-handed blow that deals massive damage.",
            LevelRequired = 1,
            StaminaCost = 15,
            Cooldown = 0,
            Type = AbilityType.Attack,
            BaseDamage = 30,
            AvailableToClasses = new[] { CharacterClass.Warrior, CharacterClass.Barbarian, CharacterClass.Paladin }
        },
        ["shield_wall"] = new ClassAbility
        {
            Id = "shield_wall",
            Name = "Shield Wall",
            Description = "Raise your shield high, greatly increasing defense.",
            LevelRequired = 8,
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
            LevelRequired = 16,
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
            LevelRequired = 28,
            StaminaCost = 40,
            Cooldown = 2,
            Type = AbilityType.Attack,
            BaseDamage = 100,
            SpecialEffect = "execute",
            AvailableToClasses = new[] { CharacterClass.Warrior, CharacterClass.Barbarian, CharacterClass.Assassin }
        },
        ["last_stand"] = new ClassAbility
        {
            Id = "last_stand",
            Name = "Last Stand",
            Description = "When near death, channel your remaining strength into a counterattack.",
            LevelRequired = 40,
            StaminaCost = 50,
            Cooldown = 5,
            Type = AbilityType.Attack,
            BaseDamage = 150,
            SpecialEffect = "last_stand",
            AvailableToClasses = new[] { CharacterClass.Warrior, CharacterClass.Barbarian, CharacterClass.Paladin }
        },
        ["whirlwind"] = new ClassAbility
        {
            Id = "whirlwind",
            Name = "Whirlwind",
            Description = "Spin with weapon extended, striking all nearby enemies.",
            LevelRequired = 55,
            StaminaCost = 60,
            Cooldown = 3,
            Type = AbilityType.Attack,
            BaseDamage = 80,
            SpecialEffect = "aoe",
            AvailableToClasses = new[] { CharacterClass.Warrior, CharacterClass.Barbarian }
        },
        ["maelketh_fury"] = new ClassAbility
        {
            Id = "maelketh_fury",
            Name = "Maelketh's Fury",
            Description = "Channel the War God's rage for devastating strikes.",
            LevelRequired = 70,
            StaminaCost = 70,
            Cooldown = 5,
            Type = AbilityType.Attack,
            BaseDamage = 200,
            AttackBonus = 50,
            Duration = 3,
            SpecialEffect = "fury",
            AvailableToClasses = new[] { CharacterClass.Warrior }
        },
        ["iron_fortress"] = new ClassAbility
        {
            Id = "iron_fortress",
            Name = "Iron Fortress",
            Description = "Become an immovable bastion of steel.",
            LevelRequired = 85,
            StaminaCost = 80,
            Cooldown = 6,
            Type = AbilityType.Defense,
            DefenseBonus = 100,
            Duration = 4,
            AvailableToClasses = new[] { CharacterClass.Warrior }
        },
        ["champion_strike"] = new ClassAbility
        {
            Id = "champion_strike",
            Name = "Champion's Strike",
            Description = "The ultimate warrior technique, perfected through countless battles.",
            LevelRequired = 100,
            StaminaCost = 100,
            Cooldown = 6,
            Type = AbilityType.Attack,
            BaseDamage = 350,
            SpecialEffect = "champion",
            AvailableToClasses = new[] { CharacterClass.Warrior }
        },

        // ═══════════════════════════════════════════════════════════════════════════════
        // BARBARIAN ABILITIES - Primal fury, berserker rage
        // ═══════════════════════════════════════════════════════════════════════════════
        ["rage"] = new ClassAbility
        {
            Id = "rage",
            Name = "Berserker Rage",
            Description = "Enter a blood rage, greatly increasing damage but lowering defense.",
            LevelRequired = 5,
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
            SpecialEffect = "reckless",
            AvailableToClasses = new[] { CharacterClass.Barbarian }
        },
        ["intimidate"] = new ClassAbility
        {
            Id = "intimidate",
            Name = "Intimidating Roar",
            Description = "A terrifying roar that weakens enemies.",
            LevelRequired = 24,
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
            LevelRequired = 36,
            StaminaCost = 40,
            Cooldown = 6,
            Type = AbilityType.Buff,
            BaseHealing = 30,
            AttackBonus = 25,
            Duration = 999,
            SpecialEffect = "bloodlust",
            AvailableToClasses = new[] { CharacterClass.Barbarian }
        },
        ["frenzy"] = new ClassAbility
        {
            Id = "frenzy",
            Name = "Frenzy",
            Description = "Attack in a wild frenzy, striking multiple times.",
            LevelRequired = 48,
            StaminaCost = 55,
            Cooldown = 4,
            Type = AbilityType.Attack,
            BaseDamage = 60,
            SpecialEffect = "multi_hit",
            AvailableToClasses = new[] { CharacterClass.Barbarian }
        },
        ["primal_scream"] = new ClassAbility
        {
            Id = "primal_scream",
            Name = "Primal Scream",
            Description = "A scream from the depths of your soul that damages all enemies.",
            LevelRequired = 60,
            StaminaCost = 65,
            Cooldown = 5,
            Type = AbilityType.Attack,
            BaseDamage = 120,
            SpecialEffect = "aoe",
            AvailableToClasses = new[] { CharacterClass.Barbarian }
        },
        ["unstoppable"] = new ClassAbility
        {
            Id = "unstoppable",
            Name = "Unstoppable",
            Description = "Nothing can stop your rampage. Immune to status effects.",
            LevelRequired = 75,
            StaminaCost = 70,
            Cooldown = 6,
            Type = AbilityType.Buff,
            DefenseBonus = 50,
            Duration = 4,
            SpecialEffect = "immunity",
            AvailableToClasses = new[] { CharacterClass.Barbarian }
        },
        ["avatar_of_destruction"] = new ClassAbility
        {
            Id = "avatar_of_destruction",
            Name = "Avatar of Destruction",
            Description = "Become a living embodiment of primal destruction.",
            LevelRequired = 90,
            StaminaCost = 90,
            Cooldown = 7,
            Type = AbilityType.Buff,
            AttackBonus = 100,
            BaseDamage = 200,
            Duration = 5,
            SpecialEffect = "avatar",
            AvailableToClasses = new[] { CharacterClass.Barbarian }
        },

        // ═══════════════════════════════════════════════════════════════════════════════
        // PALADIN ABILITIES - Holy warriors, servants of light
        // ═══════════════════════════════════════════════════════════════════════════════
        ["lay_on_hands"] = new ClassAbility
        {
            Id = "lay_on_hands",
            Name = "Lay on Hands",
            Description = "Channel divine power to heal your wounds.",
            LevelRequired = 1,
            StaminaCost = 25,
            Cooldown = 4,
            Type = AbilityType.Heal,
            BaseHealing = 40,
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },
        ["divine_smite"] = new ClassAbility
        {
            Id = "divine_smite",
            Name = "Divine Smite",
            Description = "Channel holy energy through your weapon. Extra vs undead.",
            LevelRequired = 10,
            StaminaCost = 35,
            Cooldown = 2,
            Type = AbilityType.Attack,
            BaseDamage = 70,
            SpecialEffect = "holy",
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },
        ["aura_of_protection"] = new ClassAbility
        {
            Id = "aura_of_protection",
            Name = "Aura of Protection",
            Description = "Project a protective aura that increases defense.",
            LevelRequired = 20,
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
            Description = "Call upon divine wrath to smite evil.",
            LevelRequired = 32,
            StaminaCost = 55,
            Cooldown = 5,
            Type = AbilityType.Attack,
            BaseDamage = 120,
            SpecialEffect = "holy_avenger",
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },
        ["cleansing_light"] = new ClassAbility
        {
            Id = "cleansing_light",
            Name = "Cleansing Light",
            Description = "Purge corruption and heal wounds with holy light.",
            LevelRequired = 44,
            StaminaCost = 50,
            Cooldown = 5,
            Type = AbilityType.Heal,
            BaseHealing = 100,
            SpecialEffect = "cleanse",
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },
        ["divine_shield"] = new ClassAbility
        {
            Id = "divine_shield",
            Name = "Divine Shield",
            Description = "Become invulnerable for a short time.",
            LevelRequired = 56,
            StaminaCost = 60,
            Cooldown = 7,
            Type = AbilityType.Defense,
            DefenseBonus = 200,
            Duration = 2,
            SpecialEffect = "invulnerable",
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },
        ["aurelion_blessing"] = new ClassAbility
        {
            Id = "aurelion_blessing",
            Name = "Aurelion's Blessing",
            Description = "The Sun God's light empowers and heals you.",
            LevelRequired = 68,
            StaminaCost = 70,
            Cooldown = 6,
            Type = AbilityType.Buff,
            BaseHealing = 150,
            AttackBonus = 40,
            DefenseBonus = 40,
            Duration = 4,
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },
        ["judgment_day"] = new ClassAbility
        {
            Id = "judgment_day",
            Name = "Judgment Day",
            Description = "Call down divine judgment on all enemies.",
            LevelRequired = 80,
            StaminaCost = 80,
            Cooldown = 6,
            Type = AbilityType.Attack,
            BaseDamage = 180,
            SpecialEffect = "aoe_holy",
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },
        ["avatar_of_light"] = new ClassAbility
        {
            Id = "avatar_of_light",
            Name = "Avatar of Light",
            Description = "Become a vessel of pure divine energy.",
            LevelRequired = 95,
            StaminaCost = 100,
            Cooldown = 7,
            Type = AbilityType.Buff,
            AttackBonus = 80,
            DefenseBonus = 80,
            BaseHealing = 200,
            Duration = 5,
            SpecialEffect = "avatar_light",
            AvailableToClasses = new[] { CharacterClass.Paladin }
        },

        // ═══════════════════════════════════════════════════════════════════════════════
        // ASSASSIN ABILITIES - Shadow killers, servants of Noctura
        // ═══════════════════════════════════════════════════════════════════════════════
        ["backstab"] = new ClassAbility
        {
            Id = "backstab",
            Name = "Backstab",
            Description = "Strike from the shadows for critical damage.",
            LevelRequired = 1,
            StaminaCost = 20,
            Cooldown = 1,
            Type = AbilityType.Attack,
            BaseDamage = 50,
            SpecialEffect = "critical",
            AvailableToClasses = new[] { CharacterClass.Assassin }
        },
        ["poison_blade"] = new ClassAbility
        {
            Id = "poison_blade",
            Name = "Poison Blade",
            Description = "Coat your weapon with deadly poison.",
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
            LevelRequired = 18,
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
            Description = "Mark a target for death, increasing damage dealt.",
            LevelRequired = 28,
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
            Description = "A lethal strike. Can instantly kill weakened enemies.",
            LevelRequired = 42,
            StaminaCost = 70,
            Cooldown = 6,
            Type = AbilityType.Attack,
            BaseDamage = 200,
            SpecialEffect = "instant_kill",
            AvailableToClasses = new[] { CharacterClass.Assassin }
        },
        ["vanish"] = new ClassAbility
        {
            Id = "vanish",
            Name = "Vanish",
            Description = "Completely disappear, resetting combat advantage.",
            LevelRequired = 52,
            StaminaCost = 50,
            Cooldown = 5,
            Type = AbilityType.Utility,
            DefenseBonus = 100,
            Duration = 1,
            SpecialEffect = "vanish",
            AvailableToClasses = new[] { CharacterClass.Assassin }
        },
        ["noctura_embrace"] = new ClassAbility
        {
            Id = "noctura_embrace",
            Name = "Noctura's Embrace",
            Description = "The Shadow Goddess cloaks you in darkness.",
            LevelRequired = 65,
            StaminaCost = 60,
            Cooldown = 6,
            Type = AbilityType.Buff,
            DefenseBonus = 80,
            AttackBonus = 60,
            Duration = 4,
            SpecialEffect = "shadow",
            AvailableToClasses = new[] { CharacterClass.Assassin }
        },
        ["blade_dance"] = new ClassAbility
        {
            Id = "blade_dance",
            Name = "Blade Dance",
            Description = "A flurry of deadly strikes hitting all enemies.",
            LevelRequired = 78,
            StaminaCost = 75,
            Cooldown = 5,
            Type = AbilityType.Attack,
            BaseDamage = 150,
            SpecialEffect = "aoe",
            AvailableToClasses = new[] { CharacterClass.Assassin }
        },
        ["death_blossom"] = new ClassAbility
        {
            Id = "death_blossom",
            Name = "Death Blossom",
            Description = "The ultimate assassination technique. Lethal to all.",
            LevelRequired = 92,
            StaminaCost = 90,
            Cooldown = 7,
            Type = AbilityType.Attack,
            BaseDamage = 300,
            SpecialEffect = "execute_all",
            AvailableToClasses = new[] { CharacterClass.Assassin }
        },

        // ═══════════════════════════════════════════════════════════════════════════════
        // RANGER ABILITIES - Masters of bow and nature
        // ═══════════════════════════════════════════════════════════════════════════════
        ["precise_shot"] = new ClassAbility
        {
            Id = "precise_shot",
            Name = "Precise Shot",
            Description = "Take careful aim for a guaranteed hit.",
            LevelRequired = 1,
            StaminaCost = 15,
            Cooldown = 1,
            Type = AbilityType.Attack,
            BaseDamage = 35,
            SpecialEffect = "guaranteed_hit",
            AvailableToClasses = new[] { CharacterClass.Ranger }
        },
        ["hunters_mark"] = new ClassAbility
        {
            Id = "hunters_mark",
            Name = "Hunter's Mark",
            Description = "Mark your prey, increasing damage dealt.",
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
            Description = "Roll away from danger, avoiding the next attack.",
            LevelRequired = 16,
            StaminaCost = 30,
            Cooldown = 3,
            Type = AbilityType.Defense,
            DefenseBonus = 100,
            Duration = 1,
            SpecialEffect = "dodge_next",
            AvailableToClasses = new[] { CharacterClass.Ranger, CharacterClass.Assassin }
        },
        ["natures_blessing"] = new ClassAbility
        {
            Id = "natures_blessing",
            Name = "Nature's Blessing",
            Description = "Call upon nature spirits to heal your wounds.",
            LevelRequired = 24,
            StaminaCost = 40,
            Cooldown = 5,
            Type = AbilityType.Heal,
            BaseHealing = 80,
            AvailableToClasses = new[] { CharacterClass.Ranger }
        },
        ["volley"] = new ClassAbility
        {
            Id = "volley",
            Name = "Volley",
            Description = "Fire multiple arrows at all enemies.",
            LevelRequired = 36,
            StaminaCost = 50,
            Cooldown = 4,
            Type = AbilityType.Attack,
            BaseDamage = 50,
            SpecialEffect = "aoe",
            AvailableToClasses = new[] { CharacterClass.Ranger }
        },
        ["camouflage"] = new ClassAbility
        {
            Id = "camouflage",
            Name = "Camouflage",
            Description = "Blend with surroundings, greatly increasing evasion.",
            LevelRequired = 48,
            StaminaCost = 45,
            Cooldown = 5,
            Type = AbilityType.Defense,
            DefenseBonus = 70,
            Duration = 3,
            SpecialEffect = "stealth",
            AvailableToClasses = new[] { CharacterClass.Ranger }
        },
        ["terravok_call"] = new ClassAbility
        {
            Id = "terravok_call",
            Name = "Terravok's Call",
            Description = "The Earth God empowers your connection to nature.",
            LevelRequired = 60,
            StaminaCost = 60,
            Cooldown = 6,
            Type = AbilityType.Buff,
            AttackBonus = 50,
            BaseHealing = 100,
            Duration = 4,
            AvailableToClasses = new[] { CharacterClass.Ranger }
        },
        ["arrow_storm"] = new ClassAbility
        {
            Id = "arrow_storm",
            Name = "Arrow Storm",
            Description = "Rain arrows upon all enemies with devastating effect.",
            LevelRequired = 75,
            StaminaCost = 70,
            Cooldown = 5,
            Type = AbilityType.Attack,
            BaseDamage = 140,
            SpecialEffect = "aoe",
            AvailableToClasses = new[] { CharacterClass.Ranger }
        },
        ["legendary_shot"] = new ClassAbility
        {
            Id = "legendary_shot",
            Name = "Legendary Shot",
            Description = "A shot that will be remembered in songs forever.",
            LevelRequired = 88,
            StaminaCost = 80,
            Cooldown = 6,
            Type = AbilityType.Attack,
            BaseDamage = 280,
            SpecialEffect = "legendary",
            AvailableToClasses = new[] { CharacterClass.Ranger }
        },

        // ═══════════════════════════════════════════════════════════════════════════════
        // JESTER/BARD ABILITIES - Tricksters and performers
        // ═══════════════════════════════════════════════════════════════════════════════
        ["mock"] = new ClassAbility
        {
            Id = "mock",
            Name = "Vicious Mockery",
            Description = "Scathing insults that distract and damage.",
            LevelRequired = 1,
            StaminaCost = 10,
            Cooldown = 1,
            Type = AbilityType.Attack,
            BaseDamage = 25,
            SpecialEffect = "distract",
            AvailableToClasses = new[] { CharacterClass.Jester, CharacterClass.Bard }
        },
        ["inspiring_tune"] = new ClassAbility
        {
            Id = "inspiring_tune",
            Name = "Inspiring Tune",
            Description = "Play an inspiring melody that boosts stats.",
            LevelRequired = 10,
            StaminaCost = 30,
            Cooldown = 4,
            Type = AbilityType.Buff,
            AttackBonus = 20,
            DefenseBonus = 20,
            Duration = 4,
            AvailableToClasses = new[] { CharacterClass.Bard }
        },
        ["song_of_rest"] = new ClassAbility
        {
            Id = "song_of_rest",
            Name = "Song of Rest",
            Description = "A soothing melody that heals wounds.",
            LevelRequired = 18,
            StaminaCost = 35,
            Cooldown = 5,
            Type = AbilityType.Heal,
            BaseHealing = 60,
            AvailableToClasses = new[] { CharacterClass.Bard }
        },
        ["charm"] = new ClassAbility
        {
            Id = "charm",
            Name = "Charming Performance",
            Description = "Use charisma to confuse your enemy.",
            LevelRequired = 26,
            StaminaCost = 35,
            Cooldown = 4,
            Type = AbilityType.Debuff,
            SpecialEffect = "charm",
            Duration = 3,
            AvailableToClasses = new[] { CharacterClass.Jester, CharacterClass.Bard }
        },
        ["disappearing_act"] = new ClassAbility
        {
            Id = "disappearing_act",
            Name = "Disappearing Act",
            Description = "Perform a magical trick to escape combat.",
            LevelRequired = 34,
            StaminaCost = 40,
            Cooldown = 6,
            Type = AbilityType.Utility,
            SpecialEffect = "escape",
            AvailableToClasses = new[] { CharacterClass.Jester }
        },
        ["deadly_joke"] = new ClassAbility
        {
            Id = "deadly_joke",
            Name = "Deadly Joke",
            Description = "A joke so bad it causes physical pain.",
            LevelRequired = 46,
            StaminaCost = 45,
            Cooldown = 3,
            Type = AbilityType.Attack,
            BaseDamage = 90,
            SpecialEffect = "confusion",
            AvailableToClasses = new[] { CharacterClass.Jester }
        },
        ["veloura_serenade"] = new ClassAbility
        {
            Id = "veloura_serenade",
            Name = "Veloura's Serenade",
            Description = "Channel the lost Goddess of Love through song.",
            LevelRequired = 58,
            StaminaCost = 55,
            Cooldown = 6,
            Type = AbilityType.Buff,
            BaseHealing = 100,
            AttackBonus = 30,
            DefenseBonus = 30,
            Duration = 4,
            AvailableToClasses = new[] { CharacterClass.Bard }
        },
        ["grand_finale"] = new ClassAbility
        {
            Id = "grand_finale",
            Name = "Grand Finale",
            Description = "The ultimate performance that devastates all foes.",
            LevelRequired = 72,
            StaminaCost = 70,
            Cooldown = 6,
            Type = AbilityType.Attack,
            BaseDamage = 160,
            SpecialEffect = "aoe",
            AvailableToClasses = new[] { CharacterClass.Bard, CharacterClass.Jester }
        },
        ["legend_incarnate"] = new ClassAbility
        {
            Id = "legend_incarnate",
            Name = "Legend Incarnate",
            Description = "Become the legend you've always sung about.",
            LevelRequired = 85,
            StaminaCost = 80,
            Cooldown = 7,
            Type = AbilityType.Buff,
            AttackBonus = 70,
            DefenseBonus = 70,
            Duration = 5,
            SpecialEffect = "legend",
            AvailableToClasses = new[] { CharacterClass.Bard }
        },

        // ═══════════════════════════════════════════════════════════════════════════════
        // ALCHEMIST ABILITIES - Masters of potions and explosives
        // ═══════════════════════════════════════════════════════════════════════════════
        ["throw_bomb"] = new ClassAbility
        {
            Id = "throw_bomb",
            Name = "Throw Bomb",
            Description = "Hurl an explosive concoction at enemies.",
            LevelRequired = 1,
            StaminaCost = 20,
            Cooldown = 2,
            Type = AbilityType.Attack,
            BaseDamage = 40,
            SpecialEffect = "fire",
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["healing_elixir"] = new ClassAbility
        {
            Id = "healing_elixir",
            Name = "Healing Elixir",
            Description = "Drink a prepared healing potion.",
            LevelRequired = 8,
            StaminaCost = 20,
            Cooldown = 3,
            Type = AbilityType.Heal,
            BaseHealing = 55,
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["smoke_bomb"] = new ClassAbility
        {
            Id = "smoke_bomb",
            Name = "Smoke Bomb",
            Description = "Create smoke to confuse enemies.",
            LevelRequired = 16,
            StaminaCost = 30,
            Cooldown = 4,
            Type = AbilityType.Utility,
            DefenseBonus = 40,
            Duration = 2,
            SpecialEffect = "smoke",
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["acid_splash"] = new ClassAbility
        {
            Id = "acid_splash",
            Name = "Acid Splash",
            Description = "Throw acid that melts through armor.",
            LevelRequired = 24,
            StaminaCost = 35,
            Cooldown = 3,
            Type = AbilityType.Attack,
            BaseDamage = 60,
            SpecialEffect = "armor_pierce",
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["mutagen"] = new ClassAbility
        {
            Id = "mutagen",
            Name = "Mutagen",
            Description = "Drink a mutagen that enhances physical abilities.",
            LevelRequired = 36,
            StaminaCost = 50,
            Cooldown = 6,
            Type = AbilityType.Buff,
            AttackBonus = 35,
            DefenseBonus = 25,
            Duration = 5,
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["frost_bomb"] = new ClassAbility
        {
            Id = "frost_bomb",
            Name = "Frost Bomb",
            Description = "A bomb that freezes enemies solid.",
            LevelRequired = 48,
            StaminaCost = 45,
            Cooldown = 4,
            Type = AbilityType.Attack,
            BaseDamage = 80,
            SpecialEffect = "freeze",
            Duration = 2,
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["greater_elixir"] = new ClassAbility
        {
            Id = "greater_elixir",
            Name = "Greater Elixir",
            Description = "A masterwork healing potion.",
            LevelRequired = 60,
            StaminaCost = 45,
            Cooldown = 5,
            Type = AbilityType.Heal,
            BaseHealing = 150,
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["philosophers_bomb"] = new ClassAbility
        {
            Id = "philosophers_bomb",
            Name = "Philosopher's Bomb",
            Description = "An alchemical masterpiece that devastates all.",
            LevelRequired = 74,
            StaminaCost = 70,
            Cooldown = 5,
            Type = AbilityType.Attack,
            BaseDamage = 180,
            SpecialEffect = "aoe",
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },
        ["transmutation"] = new ClassAbility
        {
            Id = "transmutation",
            Name = "Transmutation",
            Description = "The ultimate alchemical transformation.",
            LevelRequired = 88,
            StaminaCost = 85,
            Cooldown = 7,
            Type = AbilityType.Buff,
            AttackBonus = 80,
            DefenseBonus = 60,
            BaseHealing = 100,
            Duration = 5,
            SpecialEffect = "transmute",
            AvailableToClasses = new[] { CharacterClass.Alchemist }
        },

        // ═══════════════════════════════════════════════════════════════════════════════
        // UNIVERSAL ABILITIES - Available to all non-spellcaster classes
        // ═══════════════════════════════════════════════════════════════════════════════
        ["second_wind"] = new ClassAbility
        {
            Id = "second_wind",
            Name = "Second Wind",
            Description = "Catch your breath and recover health.",
            LevelRequired = 1,
            StaminaCost = 25,
            Cooldown = 5,
            Type = AbilityType.Heal,
            BaseHealing = 25,
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
            Description = "Concentrate to increase accuracy.",
            LevelRequired = 5,
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
        },
        ["rally"] = new ClassAbility
        {
            Id = "rally",
            Name = "Rally",
            Description = "Steel your resolve, recovering health and stamina.",
            LevelRequired = 30,
            StaminaCost = 40,
            Cooldown = 6,
            Type = AbilityType.Heal,
            BaseHealing = 70,
            AvailableToClasses = new[] {
                CharacterClass.Warrior, CharacterClass.Barbarian, CharacterClass.Paladin,
                CharacterClass.Assassin, CharacterClass.Ranger, CharacterClass.Jester,
                CharacterClass.Bard, CharacterClass.Alchemist
            }
        },
        ["desperate_strike"] = new ClassAbility
        {
            Id = "desperate_strike",
            Name = "Desperate Strike",
            Description = "A powerful attack when all seems lost.",
            LevelRequired = 50,
            StaminaCost = 55,
            Cooldown = 4,
            Type = AbilityType.Attack,
            BaseDamage = 130,
            SpecialEffect = "desperate",
            AvailableToClasses = new[] {
                CharacterClass.Warrior, CharacterClass.Barbarian, CharacterClass.Paladin,
                CharacterClass.Assassin, CharacterClass.Ranger, CharacterClass.Jester,
                CharacterClass.Bard, CharacterClass.Alchemist
            }
        },
        ["iron_will"] = new ClassAbility
        {
            Id = "iron_will",
            Name = "Iron Will",
            Description = "Your will becomes unbreakable. Resist all effects.",
            LevelRequired = 70,
            StaminaCost = 60,
            Cooldown = 6,
            Type = AbilityType.Buff,
            DefenseBonus = 60,
            Duration = 3,
            SpecialEffect = "resist_all",
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
    /// Also updates the character's LearnedAbilities set for tracking
    /// </summary>
    public static List<ClassAbility> GetAvailableAbilities(Character character)
    {
        var available = AllAbilities.Values
            .Where(a => a.AvailableToClasses.Contains(character.Class) &&
                       character.Level >= a.LevelRequired)
            .OrderBy(a => a.LevelRequired)
            .ToList();

        // Update LearnedAbilities tracking
        foreach (var ability in available)
        {
            if (!character.LearnedAbilities.Contains(ability.Id))
            {
                character.LearnedAbilities.Add(ability.Id);
            }
        }

        return available;
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

        // Check combat stamina (CurrentCombatStamina is used during combat)
        if (character.CurrentCombatStamina < ability.StaminaCost) return false;

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

        // Deduct combat stamina
        user.CurrentCombatStamina -= ability.StaminaCost;

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
