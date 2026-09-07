using System;
using System.Collections.Generic;
using System.Linq;

namespace UsurperRemake.Data;

/// <summary>
/// v1.2 (design item H2, issue #68): scalar tuning of built-in abilities and spells through
/// GameData/abilities.json and GameData/spells.json. Replace-by-key: an entry names an
/// existing ability id or class-and-level and overrides only the numbers it sets; nothing is
/// added or removed, so saves that store ids and levels stay valid. A file with any invalid
/// entry is rejected whole, with every problem logged, rather than half-applied.
/// </summary>
public class AbilityOverride
{
    public string Id { get; set; } = "";
    public int? Cooldown { get; set; }
    public int? StaminaCost { get; set; }
    public int? ManaCost { get; set; }
    public int? LevelRequired { get; set; }
    public int? BaseDamage { get; set; }
    public int? BaseHealing { get; set; }
    public int? DefenseBonus { get; set; }
    public int? AttackBonus { get; set; }
    public int? Duration { get; set; }
}

public class SpellOverride
{
    public CharacterClass Class { get; set; }
    public int Level { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? ManaCost { get; set; }
    public int? LevelRequired { get; set; }
    public string? MagicWords { get; set; }
}

public static class OverrideValidation
{
    public const int MaxCooldown = 20, MaxCost = 1000, MaxPower = 100_000, MaxDuration = 50, MaxLevel = 100;

    /// <summary>Problems with an abilities file; empty means valid.</summary>
    public static List<string> ValidateAbilities(IReadOnlyList<AbilityOverride> entries, Func<string, bool> idExists)
    {
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            string where = $"abilities[{i}] ({e.Id})";
            if (string.IsNullOrWhiteSpace(e.Id)) { errors.Add($"{where}: id is required"); continue; }
            if (!seen.Add(e.Id)) errors.Add($"{where}: duplicate id");
            if (!idExists(e.Id)) errors.Add($"{where}: no built-in ability has this id");
            Range(errors, where, "cooldown", e.Cooldown, 0, MaxCooldown);
            Range(errors, where, "staminaCost", e.StaminaCost, 0, MaxCost);
            Range(errors, where, "manaCost", e.ManaCost, 0, MaxCost);
            Range(errors, where, "levelRequired", e.LevelRequired, 1, MaxLevel);
            Range(errors, where, "baseDamage", e.BaseDamage, 0, MaxPower);
            Range(errors, where, "baseHealing", e.BaseHealing, 0, MaxPower);
            Range(errors, where, "defenseBonus", e.DefenseBonus, -MaxPower, MaxPower);
            Range(errors, where, "attackBonus", e.AttackBonus, -MaxPower, MaxPower);
            Range(errors, where, "duration", e.Duration, 0, MaxDuration);
        }
        return errors;
    }

    /// <summary>Problems with a spells file; empty means valid.</summary>
    public static List<string> ValidateSpells(IReadOnlyList<SpellOverride> entries, Func<CharacterClass, int, bool> spellExists)
    {
        var errors = new List<string>();
        var seen = new HashSet<(CharacterClass, int)>();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            string where = $"spells[{i}] ({e.Class} level {e.Level})";
            if (!Enum.IsDefined(typeof(CharacterClass), e.Class)) { errors.Add($"{where}: unknown class"); continue; }
            if (e.Level < 1 || e.Level > MaxLevel) { errors.Add($"{where}: level must be 1 to {MaxLevel}"); continue; }
            if (!seen.Add((e.Class, e.Level))) errors.Add($"{where}: duplicate class and level");
            if (!spellExists(e.Class, e.Level)) errors.Add($"{where}: no built-in spell at this class and level");
            if (e.Name != null && string.IsNullOrWhiteSpace(e.Name)) errors.Add($"{where}: name may not be blank");
            Range(errors, where, "manaCost", e.ManaCost, 1, MaxCost);
            Range(errors, where, "levelRequired", e.LevelRequired, 1, MaxLevel);
        }
        return errors;
    }

    private static void Range(List<string> errors, string where, string field, int? value, int min, int max)
    {
        if (value.HasValue && (value.Value < min || value.Value > max))
            errors.Add($"{where}: {field} must be {min} to {max}, was {value.Value}");
    }
}
