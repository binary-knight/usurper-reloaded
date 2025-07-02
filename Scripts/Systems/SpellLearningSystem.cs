using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UsurperRemake.Utils;

/// <summary>
/// Simple spell-learning interface that lets players study spells outside combat.
/// Intended to be invoked from Mage Guild or Library locations.
/// </summary>
public static class SpellLearningSystem
{
    /// <summary>
    /// Interactive console UI that shows all learnable spells and lets the player learn them.
    /// </summary>
    public static async Task ShowSpellLearningMenu(Character player, TerminalEmulator terminal)
    {
        if (terminal == null || player == null) return;
        if (player.Class != CharacterClass.Cleric && player.Class != CharacterClass.Magician && player.Class != CharacterClass.Sage)
        {
            terminal.WriteLine("Only magical professions can learn spells here.", "red");
            await Task.Delay(1000);
            return;
        }

        while (true)
        {
            terminal.ClearScreen();
            terminal.WriteLine("═══ SPELL LIBRARY ═══", "bright_magenta");
            terminal.WriteLine("Level  Name                     Known  ManaCost", "cyan");
            terminal.WriteLine("───────────────────────────────────────────────", "cyan");

            var available = SpellSystem.GetAvailableSpells(player).OrderBy(s => s.Level).ToList();
            foreach (var spell in available)
            {
                bool known = player.Spell[spell.Level - 1][0];
                int cost = SpellSystem.CalculateManaCost(spell, player);
                terminal.WriteLine($" {spell.Level,2}    {spell.Name,-20}   {(known ? "YES" : "no ")}    {cost}");
            }
            terminal.WriteLine("");
            terminal.WriteLine("Enter level number to learn/forget, or X to exit.", "yellow");
            var input = await terminal.GetInput("> ");
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Trim().ToUpper() == "X") break;
            if (!int.TryParse(input, out int lvl)) continue;

            var chosen = available.FirstOrDefault(s => s.Level == lvl);
            if (chosen == null) continue;

            bool already = player.Spell[lvl - 1][0];
            player.Spell[lvl - 1][0] = !already;
            terminal.WriteLine(already ? "You forget the spell." : "You memorise the spell!", "green");
            await Task.Delay(800);
        }
    }
} 