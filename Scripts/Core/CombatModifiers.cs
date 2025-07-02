using System;

/// <summary>
/// Encapsulates optional bonuses or abilities a character gains from their class during combat.
/// All properties are optional and default to the neutral value (0 / false / 1.0 etc.).
/// </summary>
public class CombatModifiers
{
    public int AttackBonus { get; set; } = 0;          // Flat bonus to hit/damage rolls (Pascal: adds to punch)
    public int ExtraAttacks { get; set; } = 0;         // Additional attacks per round

    // Assassin
    public float BackstabMultiplier { get; set; } = 1.0f;  // Multiplier applied when performing Backstab
    public int PoisonChance { get; set; } = 0;             // % chance to poison target on hit

    // Barbarian
    public int DamageReduction { get; set; } = 0;      // Flat damage reduction each hit
    public bool RageAvailable { get; set; } = false;   // Can toggle rage state

    // Paladin
    public int SmiteCharges { get; set; } = 0;         // Daily smite uses
    public int AuraBonus { get; set; } = 0;            // Party-wide bonus to hit/defence

    // Ranger
    public int RangedBonus { get; set; } = 0;          // Bonus when doing ranged attacks
    public bool Tracking { get; set; } = false;        // Can follow fleeing enemies
} 