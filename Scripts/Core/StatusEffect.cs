using System;

/// <summary>
/// Represents ongoing temporary effects applied to a character during combat.
/// Durations are tracked in rounds in Character.ActiveStatuses.
/// </summary>
public enum StatusEffect
{
    None,

    // Damage Over Time Effects
    Poisoned,      // 1d4 damage per round, can stack intensity
    Bleeding,      // 1d6 damage per round, physical wounds
    Burning,       // 2d4 damage per round, fire damage
    Frozen,        // 1d3 damage per round + 25% slower attacks
    Cursed,        // -2 to all stats, dark magic

    // Control Effects
    Stunned,       // Skip a turn completely
    Paralyzed,     // Skip turn, easier to hit (+25% accuracy against)
    Silenced,      // Cannot cast spells
    Blinded,       // -50% accuracy on attacks
    Confused,      // 25% chance to hit self or ally
    Feared,        // Cannot attack, may flee
    Charmed,       // Cannot attack charmer
    Sleeping,      // Skip turns until damaged

    // Buff Effects
    Blessed,       // +2 to attack and defense
    Raging,        // Barbarian rage – 2x damage, -4 defense
    Defending,     // 50% damage reduction
    PowerStance,   // -25% accuracy, +50% damage
    Hidden,        // Stealth until next attack, bonus damage
    Haste,         // Double attacks per round
    Regenerating,  // Heal 1d6 HP per round
    Shielded,      // Flat damage reduction per hit
    Empowered,     // +50% spell damage
    Protected,     // +4 AC bonus

    // Debuff Effects
    Weakened,      // -4 STR
    Slow,          // Half attacks per round
    Vulnerable,    // +25% damage taken
    Exhausted,     // -25% damage dealt
    Diseased,      // -1 HP per round, spreads on contact

    // Special Effects
    Blur,          // 20% chance for attacks to miss
    Stoneskin,     // Damage absorption pool
    Reflecting,    // Reflect 25% of damage back to attacker
    Lifesteal,     // Heal for 25% of damage dealt
    Berserk,       // Auto-attack nearest target, +50% damage
    Invulnerable   // Cannot take damage (very short duration)
}

/// <summary>
/// Status effect categories for grouping and UI display
/// </summary>
public enum StatusCategory
{
    DamageOverTime,
    Control,
    Buff,
    Debuff,
    Special
}

/// <summary>
/// Extension methods for StatusEffect
/// </summary>
public static class StatusEffectExtensions
{
    /// <summary>
    /// Get the category of a status effect
    /// </summary>
    public static StatusCategory GetCategory(this StatusEffect effect) => effect switch
    {
        StatusEffect.Poisoned or StatusEffect.Bleeding or StatusEffect.Burning or
        StatusEffect.Frozen or StatusEffect.Cursed or StatusEffect.Diseased
            => StatusCategory.DamageOverTime,

        StatusEffect.Stunned or StatusEffect.Paralyzed or StatusEffect.Silenced or
        StatusEffect.Blinded or StatusEffect.Confused or StatusEffect.Feared or
        StatusEffect.Charmed or StatusEffect.Sleeping
            => StatusCategory.Control,

        StatusEffect.Blessed or StatusEffect.Raging or StatusEffect.Defending or
        StatusEffect.PowerStance or StatusEffect.Hidden or StatusEffect.Haste or
        StatusEffect.Regenerating or StatusEffect.Shielded or StatusEffect.Empowered or
        StatusEffect.Protected
            => StatusCategory.Buff,

        StatusEffect.Weakened or StatusEffect.Slow or StatusEffect.Vulnerable or
        StatusEffect.Exhausted
            => StatusCategory.Debuff,

        _ => StatusCategory.Special
    };

    /// <summary>
    /// Check if this is a negative effect
    /// </summary>
    public static bool IsNegative(this StatusEffect effect) =>
        effect.GetCategory() == StatusCategory.DamageOverTime ||
        effect.GetCategory() == StatusCategory.Control ||
        effect.GetCategory() == StatusCategory.Debuff;

    /// <summary>
    /// Check if this effect prevents actions
    /// </summary>
    public static bool PreventsAction(this StatusEffect effect) => effect switch
    {
        StatusEffect.Stunned or StatusEffect.Paralyzed or StatusEffect.Sleeping or
        StatusEffect.Frozen => true,
        _ => false
    };

    /// <summary>
    /// Check if this effect prevents spell casting
    /// </summary>
    public static bool PreventsSpellcasting(this StatusEffect effect) => effect switch
    {
        StatusEffect.Silenced or StatusEffect.Stunned or StatusEffect.Paralyzed or
        StatusEffect.Sleeping or StatusEffect.Confused => true,
        _ => false
    };

    /// <summary>
    /// Get display color for the status effect
    /// </summary>
    public static string GetDisplayColor(this StatusEffect effect) => effect.GetCategory() switch
    {
        StatusCategory.DamageOverTime => "red",
        StatusCategory.Control => "magenta",
        StatusCategory.Buff => "bright_green",
        StatusCategory.Debuff => "yellow",
        StatusCategory.Special => "bright_cyan",
        _ => "white"
    };

    /// <summary>
    /// Get short display name (3-4 chars) for status bar
    /// </summary>
    public static string GetShortName(this StatusEffect effect) => effect switch
    {
        StatusEffect.Poisoned => "PSN",
        StatusEffect.Bleeding => "BLD",
        StatusEffect.Burning => "BRN",
        StatusEffect.Frozen => "FRZ",
        StatusEffect.Cursed => "CRS",
        StatusEffect.Stunned => "STN",
        StatusEffect.Paralyzed => "PAR",
        StatusEffect.Silenced => "SIL",
        StatusEffect.Blinded => "BLN",
        StatusEffect.Confused => "CNF",
        StatusEffect.Feared => "FER",
        StatusEffect.Charmed => "CHM",
        StatusEffect.Sleeping => "SLP",
        StatusEffect.Blessed => "BLS",
        StatusEffect.Raging => "RAG",
        StatusEffect.Defending => "DEF",
        StatusEffect.PowerStance => "PWR",
        StatusEffect.Hidden => "HID",
        StatusEffect.Haste => "HST",
        StatusEffect.Regenerating => "RGN",
        StatusEffect.Shielded => "SHD",
        StatusEffect.Empowered => "EMP",
        StatusEffect.Protected => "PRT",
        StatusEffect.Weakened => "WEK",
        StatusEffect.Slow => "SLO",
        StatusEffect.Vulnerable => "VUL",
        StatusEffect.Exhausted => "EXH",
        StatusEffect.Diseased => "DIS",
        StatusEffect.Blur => "BLR",
        StatusEffect.Stoneskin => "STN",
        StatusEffect.Reflecting => "RFL",
        StatusEffect.Lifesteal => "LFS",
        StatusEffect.Berserk => "BSK",
        StatusEffect.Invulnerable => "INV",
        _ => "???"
    };

    /// <summary>
    /// Get description of what this effect does
    /// </summary>
    public static string GetDescription(this StatusEffect effect) => effect switch
    {
        StatusEffect.Poisoned => "Taking poison damage each round",
        StatusEffect.Bleeding => "Bleeding, taking damage each round",
        StatusEffect.Burning => "On fire! Taking heavy fire damage",
        StatusEffect.Frozen => "Frozen, slowed and taking cold damage",
        StatusEffect.Cursed => "Cursed, all stats reduced",
        StatusEffect.Stunned => "Stunned, cannot act",
        StatusEffect.Paralyzed => "Paralyzed, cannot move or act",
        StatusEffect.Silenced => "Silenced, cannot cast spells",
        StatusEffect.Blinded => "Blinded, accuracy greatly reduced",
        StatusEffect.Confused => "Confused, may attack randomly",
        StatusEffect.Feared => "Terrified, cannot attack",
        StatusEffect.Charmed => "Charmed, cannot attack the caster",
        StatusEffect.Sleeping => "Asleep, will wake when damaged",
        StatusEffect.Blessed => "Blessed, +2 to attack and defense",
        StatusEffect.Raging => "Raging! Double damage but reduced defense",
        StatusEffect.Defending => "Defending, damage reduced by 50%",
        StatusEffect.PowerStance => "Power stance, +50% damage but -25% accuracy",
        StatusEffect.Hidden => "Hidden in shadows, next attack deals bonus damage",
        StatusEffect.Haste => "Hasted, attacking twice as fast",
        StatusEffect.Regenerating => "Regenerating health each round",
        StatusEffect.Shielded => "Magical shield absorbing damage",
        StatusEffect.Empowered => "Empowered, spell damage increased",
        StatusEffect.Protected => "Protected, +4 armor class",
        StatusEffect.Weakened => "Weakened, strength reduced",
        StatusEffect.Slow => "Slowed, attacking half as often",
        StatusEffect.Vulnerable => "Vulnerable, taking extra damage",
        StatusEffect.Exhausted => "Exhausted, dealing less damage",
        StatusEffect.Diseased => "Diseased, slowly losing health",
        StatusEffect.Blur => "Blurred, 20% chance to avoid attacks",
        StatusEffect.Stoneskin => "Stoneskin, absorbing damage",
        StatusEffect.Reflecting => "Reflecting damage back to attackers",
        StatusEffect.Lifesteal => "Leeching life from attacks",
        StatusEffect.Berserk => "Berserk! Attacking wildly",
        StatusEffect.Invulnerable => "Invulnerable to all damage",
        _ => "Unknown effect"
    };
} 