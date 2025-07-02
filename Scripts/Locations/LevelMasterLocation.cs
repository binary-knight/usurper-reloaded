using Godot;
using System;
using System.Threading.Tasks;

namespace UsurperRemake.Locations;

/// <summary>
/// The Level Master – lets players advance in level when they have enough experience.
/// Simplified but compatible with original Usurper mechanics (LEVMAST.PAS).
/// Supports:
///  • Level raise (loops until no more raises possible)
///  • Crystal Ball placeholder (future party inspection)
///  • Help team member placeholder
///  • Return to main street
/// </summary>
public class LevelMasterLocation : BaseLocation
{
    private static readonly string[] Masters =
    {
        "Akrappa",
        "Singuman",
        "Ishana",
        "Dzarrgo",
        "Agni",
        "Apollonia",
        "Sachmez",
        "Umilak",
        "Asanga",
        "Gregorius"
    };

    private readonly string currentMaster;

    public LevelMasterLocation() : base(GameLocation.Master, "Level Master's Hut",
        "An aura of wisdom permeates the dim chamber. Scrolls litter the floor while a robed sage waits behind a low table.")
    {
        // Pick a deterministic master based on hash of day so players get some variety
        var index = DateTime.Now.Day % Masters.Length;
        currentMaster = Masters[index];
    }

    protected override void SetupLocation()
    {
        PossibleExits.Add(GameLocation.MainStreet);
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("╔════════════════════════════════════════════╗");
        terminal.WriteLine($"║  {currentMaster.ToUpper().PadLeft((42 + currentMaster.Length) / 2).PadRight(42)}║");
        terminal.WriteLine("╚════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine($"\"Greetings, seeker of power,\" whispers {currentMaster}. \"What brings you today?\"");
        terminal.WriteLine("");

        terminal.SetColor("yellow");
        terminal.WriteLine("Services:");
        terminal.SetColor("green");
        terminal.WriteLine("(L) Level Raise – advance if you have earned enough experience");
        terminal.WriteLine("(C) Crystal Ball – scry information about other players (coming soon)");
        terminal.WriteLine("(H) Help Team Member – assist a teammate in levelling (coming soon)");
        terminal.WriteLine("(S) Status – view your statistics");
        terminal.WriteLine("(R) Return to Main Street");
        terminal.WriteLine("");

        ShowStatusLine();
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        switch (choice.ToUpperInvariant())
        {
            case "L":
                await AttemptLevelRaise();
                return false;
            case "C":
                terminal.WriteLine("The crystal ball is cloudy today… (feature not yet implemented)", "gray");
                await Task.Delay(1500);
                return false;
            case "H":
                terminal.WriteLine("Your master smiles sadly: 'I cannot aid your allies yet.' (feature not implemented)", "gray");
                await Task.Delay(1500);
                return false;
            case "R":
            case "Q":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;
            default:
                return await base.ProcessChoice(choice);
        }
    }

    #region Level Raise Logic

    private async Task AttemptLevelRaise()
    {
        int levelsRaised = 0;
        while (currentPlayer.Level < GameConfig.MaxLevel &&
               currentPlayer.Experience >= GetExperienceForLevel(currentPlayer.Level + 1))
        {
            // Raise one level
            int newLevel = currentPlayer.Level + 1;
            ApplyStatIncreases();
            currentPlayer.RaiseLevel(newLevel);
            levelsRaised++;
        }

        if (levelsRaised == 0)
        {
            long needed = GetExperienceForLevel(currentPlayer.Level + 1) - currentPlayer.Experience;
            terminal.WriteLine($"You still need {needed:N0} experience to reach level {currentPlayer.Level + 1}.", "yellow");
            await Task.Delay(2000);
        }
        else
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine($"You advance {levelsRaised} level{(levelsRaised > 1 ? "s" : string.Empty)}! You are now level {currentPlayer.Level}.");
            terminal.WriteLine("Your body and mind feel stronger.");
            await Task.Delay(2500);
        }
    }

    /// <summary>
    /// Very simple stat progression – subject to balancing later.
    /// </summary>
    private void ApplyStatIncreases()
    {
        currentPlayer.MaxHP += 10;
        currentPlayer.HP = currentPlayer.MaxHP;
        currentPlayer.Strength += 2;
        currentPlayer.Defence += 2;
        currentPlayer.Stamina += 1;
        currentPlayer.Agility += 1;
        currentPlayer.Wisdom += 1;
        currentPlayer.Intelligence += 1;
    }

    /// <summary>
    /// Experience required to have <paramref name="level"/> (cumulative), compatible with NPC formula.
    /// </summary>
    private static long GetExperienceForLevel(int level)
    {
        if (level <= 1) return 0;
        long exp = 0;
        for (int i = 2; i <= level; i++)
        {
            exp += (long)(Math.Pow(i, 2.5) * 100);
        }
        return exp;
    }

    #endregion
} 