using System;

/// <summary>
/// Represents ongoing temporary effects applied to a character during combat.
/// Durations are tracked in rounds in Character.ActiveStatuses.
/// </summary>
public enum StatusEffect
{
    None,
    Poisoned,      // 1d4 damage per round
    Blessed,       // +2 to all rolls
    Stunned,       // Skip a turn
    Raging,        // Barbarian rage – handled by IsRaging flag too
    Defending,     // Temporary defend boost
    PowerStance,   // -25% hit, +50% damage (Power Attack)
    Weakened,      // -4 STR
    Hidden,        // From Hide action: grants stealth until next attack
    Haste,         // Double attacks
    Slow,          // Half attacks
    Blur,          // 20% chance to cause incoming attacks to miss (Fog/Duplicate effects)
    Stoneskin      // Absorption shield that soaks incoming damage until depleted
} 