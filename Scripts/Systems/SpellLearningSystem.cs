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
            terminal.WriteLine($"Class: {player.Class} | Level: {player.Level} | Mana: {player.Mana}/{player.MaxMana}", "cyan");
            terminal.WriteLine("");
            terminal.WriteLine("Lvl  Req  Name                     Known  ManaCost  Description", "cyan");
            terminal.WriteLine("───────────────────────────────────────────────────────────────────────────────", "cyan");

            // Get ALL spells for this class, not just learned ones
            var allSpells = SpellSystem.GetAllSpellsForClass(player.Class).OrderBy(s => s.Level).ToList();

            foreach (var spell in allSpells)
            {
                int levelReq = SpellSystem.GetLevelRequired(player.Class, spell.Level);
                bool canLearn = player.Level >= levelReq;
                bool known = player.Spell != null &&
                            spell.Level - 1 >= 0 &&
                            spell.Level - 1 < player.Spell.Count &&
                            player.Spell[spell.Level - 1].Count > 0 &&
                            player.Spell[spell.Level - 1][0];
                int cost = SpellSystem.CalculateManaCost(spell, player);

                string color = known ? "bright_green" : (canLearn ? "white" : "dark_gray");
                string knownStr = known ? "YES" : (canLearn ? "---" : "   ");
                string levelMark = canLearn ? "✓" : " ";

                // Truncate description to fit
                string shortDesc = spell.Description.Length > 35 ? spell.Description.Substring(0, 32) + "..." : spell.Description;

                terminal.WriteLine($"{levelMark}{spell.Level,2}   {levelReq,2}   {spell.Name,-20}   {knownStr}    {cost,3}     {shortDesc}", color);
            }

            terminal.WriteLine("");
            terminal.WriteLine("✓ = You meet the level requirement to learn this spell", "gray");
            terminal.WriteLine("");
            terminal.WriteLine("Enter spell level number to learn/forget, or [X] to exit.", "yellow");
            var input = await terminal.GetInput("> ");
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Trim().ToUpper() == "X") break;
            if (!int.TryParse(input, out int lvl)) continue;

            var chosen = allSpells.FirstOrDefault(s => s.Level == lvl);
            if (chosen == null)
            {
                terminal.WriteLine("Invalid spell level!", "red");
                await Task.Delay(800);
                continue;
            }

            int reqLevel = SpellSystem.GetLevelRequired(player.Class, lvl);
            if (player.Level < reqLevel)
            {
                terminal.WriteLine($"You need to be level {reqLevel} to learn this spell!", "red");
                await Task.Delay(800);
                continue;
            }

            // Ensure Spell array is properly initialized
            while (player.Spell == null || player.Spell.Count <= lvl - 1)
            {
                if (player.Spell == null) player.Spell = new List<List<bool>>();
                player.Spell.Add(new List<bool> { false });
            }
            if (player.Spell[lvl - 1].Count == 0)
            {
                player.Spell[lvl - 1].Add(false);
            }

            bool already = player.Spell[lvl - 1][0];
            player.Spell[lvl - 1][0] = !already;
            terminal.WriteLine(already ? $"You forget {chosen.Name}." : $"You have learned {chosen.Name}!", "bright_green");
            await Task.Delay(1000);
        }
    }
} 