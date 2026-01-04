using System;

/// <summary>
/// Difficulty modes for the game
/// Affects XP rates, gold drops, monster damage, and other game balance factors
/// </summary>
public enum DifficultyMode
{
    Easy = 0,       // Casual players - more XP, more gold, less damage taken
    Normal = 1,     // Default balanced experience
    Hard = 2,       // Challenge mode - less XP, less gold, more damage taken
    Nightmare = 3   // Extreme challenge - heavily reduced rewards, brutal combat
}

/// <summary>
/// Difficulty system that provides modifiers based on selected difficulty
/// </summary>
public static class DifficultySystem
{
    /// <summary>
    /// Current game difficulty (defaults to Normal)
    /// This is set per-character during character creation
    /// </summary>
    public static DifficultyMode CurrentDifficulty { get; set; } = DifficultyMode.Normal;

    /// <summary>
    /// Get display name for a difficulty mode
    /// </summary>
    public static string GetDisplayName(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Easy => "Easy",
        DifficultyMode.Normal => "Normal",
        DifficultyMode.Hard => "Hard",
        DifficultyMode.Nightmare => "Nightmare",
        _ => "Normal"
    };

    /// <summary>
    /// Get description for a difficulty mode
    /// </summary>
    public static string GetDescription(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Easy => "Relaxed gameplay. +50% XP, +50% Gold, -25% monster damage.",
        DifficultyMode.Normal => "Balanced challenge. Standard rewards and combat.",
        DifficultyMode.Hard => "For veterans. -25% XP, -25% Gold, +25% monster damage.",
        DifficultyMode.Nightmare => "Brutal challenge. -50% XP, -50% Gold, +50% monster damage, no fleeing.",
        _ => "Balanced challenge."
    };

    /// <summary>
    /// Get the color to display this difficulty in
    /// </summary>
    public static string GetColor(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Easy => "bright_green",
        DifficultyMode.Normal => "bright_cyan",
        DifficultyMode.Hard => "bright_yellow",
        DifficultyMode.Nightmare => "bright_red",
        _ => "white"
    };

    /// <summary>
    /// Experience multiplier based on difficulty
    /// </summary>
    public static float GetExperienceMultiplier(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Easy => 1.5f,      // +50% XP
        DifficultyMode.Normal => 1.0f,    // Standard
        DifficultyMode.Hard => 0.75f,     // -25% XP
        DifficultyMode.Nightmare => 0.5f, // -50% XP
        _ => 1.0f
    };

    /// <summary>
    /// Gold drop multiplier based on difficulty
    /// </summary>
    public static float GetGoldMultiplier(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Easy => 1.5f,      // +50% Gold
        DifficultyMode.Normal => 1.0f,    // Standard
        DifficultyMode.Hard => 0.75f,     // -25% Gold
        DifficultyMode.Nightmare => 0.5f, // -50% Gold
        _ => 1.0f
    };

    /// <summary>
    /// Monster damage multiplier (damage taken by player)
    /// </summary>
    public static float GetMonsterDamageMultiplier(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Easy => 0.75f,     // -25% damage taken
        DifficultyMode.Normal => 1.0f,    // Standard
        DifficultyMode.Hard => 1.25f,     // +25% damage taken
        DifficultyMode.Nightmare => 1.5f, // +50% damage taken
        _ => 1.0f
    };

    /// <summary>
    /// Player damage multiplier (damage dealt by player)
    /// </summary>
    public static float GetPlayerDamageMultiplier(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Easy => 1.15f,     // +15% damage dealt
        DifficultyMode.Normal => 1.0f,    // Standard
        DifficultyMode.Hard => 1.0f,      // Standard (challenge is in taking more, not dealing less)
        DifficultyMode.Nightmare => 0.9f, // -10% damage dealt
        _ => 1.0f
    };

    /// <summary>
    /// Whether the player can flee from combat
    /// </summary>
    public static bool CanFlee(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Nightmare => false, // No fleeing in Nightmare
        _ => true
    };

    /// <summary>
    /// Healing potion effectiveness multiplier
    /// </summary>
    public static float GetHealingMultiplier(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Easy => 1.25f,     // +25% healing
        DifficultyMode.Normal => 1.0f,    // Standard
        DifficultyMode.Hard => 1.0f,      // Standard
        DifficultyMode.Nightmare => 0.75f,// -25% healing
        _ => 1.0f
    };

    /// <summary>
    /// Shop price multiplier (higher = more expensive)
    /// </summary>
    public static float GetShopPriceMultiplier(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Easy => 0.85f,     // -15% shop prices
        DifficultyMode.Normal => 1.0f,    // Standard
        DifficultyMode.Hard => 1.15f,     // +15% shop prices
        DifficultyMode.Nightmare => 1.25f,// +25% shop prices
        _ => 1.0f
    };

    /// <summary>
    /// Disease/poison duration multiplier
    /// </summary>
    public static float GetAfflictionDurationMultiplier(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Easy => 0.5f,      // Half duration
        DifficultyMode.Normal => 1.0f,    // Standard
        DifficultyMode.Hard => 1.25f,     // +25% duration
        DifficultyMode.Nightmare => 1.5f, // +50% duration
        _ => 1.0f
    };

    /// <summary>
    /// Death penalty multiplier (XP/gold loss on death)
    /// </summary>
    public static float GetDeathPenaltyMultiplier(DifficultyMode mode) => mode switch
    {
        DifficultyMode.Easy => 0.5f,      // Half penalties
        DifficultyMode.Normal => 1.0f,    // Standard
        DifficultyMode.Hard => 1.5f,      // +50% penalties
        DifficultyMode.Nightmare => 2.0f, // Double penalties
        _ => 1.0f
    };

    // Convenience methods using CurrentDifficulty

    /// <summary>
    /// Get experience multiplier for current difficulty
    /// </summary>
    public static float GetExperienceMultiplier() => GetExperienceMultiplier(CurrentDifficulty);

    /// <summary>
    /// Get gold multiplier for current difficulty
    /// </summary>
    public static float GetGoldMultiplier() => GetGoldMultiplier(CurrentDifficulty);

    /// <summary>
    /// Get monster damage multiplier for current difficulty
    /// </summary>
    public static float GetMonsterDamageMultiplier() => GetMonsterDamageMultiplier(CurrentDifficulty);

    /// <summary>
    /// Get player damage multiplier for current difficulty
    /// </summary>
    public static float GetPlayerDamageMultiplier() => GetPlayerDamageMultiplier(CurrentDifficulty);

    /// <summary>
    /// Check if player can flee for current difficulty
    /// </summary>
    public static bool CanFlee() => CanFlee(CurrentDifficulty);

    /// <summary>
    /// Get healing multiplier for current difficulty
    /// </summary>
    public static float GetHealingMultiplier() => GetHealingMultiplier(CurrentDifficulty);

    /// <summary>
    /// Get shop price multiplier for current difficulty
    /// </summary>
    public static float GetShopPriceMultiplier() => GetShopPriceMultiplier(CurrentDifficulty);

    /// <summary>
    /// Get affliction duration multiplier for current difficulty
    /// </summary>
    public static float GetAfflictionDurationMultiplier() => GetAfflictionDurationMultiplier(CurrentDifficulty);

    /// <summary>
    /// Get death penalty multiplier for current difficulty
    /// </summary>
    public static float GetDeathPenaltyMultiplier() => GetDeathPenaltyMultiplier(CurrentDifficulty);

    /// <summary>
    /// Apply experience multiplier to a base XP value
    /// </summary>
    public static long ApplyExperienceMultiplier(long baseXP)
    {
        return (long)(baseXP * GetExperienceMultiplier());
    }

    /// <summary>
    /// Apply gold multiplier to a base gold value
    /// </summary>
    public static long ApplyGoldMultiplier(long baseGold)
    {
        return (long)(baseGold * GetGoldMultiplier());
    }

    /// <summary>
    /// Apply monster damage multiplier to damage
    /// </summary>
    public static long ApplyMonsterDamageMultiplier(long baseDamage)
    {
        return (long)(baseDamage * GetMonsterDamageMultiplier());
    }

    /// <summary>
    /// Apply player damage multiplier to damage
    /// </summary>
    public static long ApplyPlayerDamageMultiplier(long baseDamage)
    {
        return (long)(baseDamage * GetPlayerDamageMultiplier());
    }

    /// <summary>
    /// Apply healing multiplier to healing amount
    /// </summary>
    public static long ApplyHealingMultiplier(long baseHealing)
    {
        return (long)(baseHealing * GetHealingMultiplier());
    }

    /// <summary>
    /// Apply shop price multiplier to a price
    /// </summary>
    public static long ApplyShopPriceMultiplier(long basePrice)
    {
        return (long)(basePrice * GetShopPriceMultiplier());
    }
}
