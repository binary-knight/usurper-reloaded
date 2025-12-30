using Godot;
using System;
using System.Threading.Tasks;

namespace UsurperRemake.Locations;

/// <summary>
/// The Level Master – lets players advance in level when they have enough experience.
/// Features three alignment-based masters with hidden stat bonuses:
///  • Good Master: Grants bonus Wisdom and Charisma
///  • Neutral Master: Grants bonus Intelligence and Dexterity
///  • Evil Master: Grants bonus Strength and Constitution
///
/// Each class also receives stat bonuses appropriate to their role:
///  • Magic classes (Magician, Cleric, Sage, Alchemist): +Intelligence, +Wisdom, +Mana
///  • Warrior classes (Warrior, Barbarian, Paladin): +Strength, +Constitution, +HP
///  • Agile classes (Assassin, Ranger, Jester, Bard): +Dexterity, +Agility, +Stamina
/// </summary>
public class LevelMasterLocation : BaseLocation
{
    // The three alignment-based masters
    private static readonly MasterInfo GoodMaster = new(
        "Seraphina the Radiant",
        "A celestial figure surrounded by soft golden light",
        "bright_yellow",
        PlayerAlignment.Good
    );

    private static readonly MasterInfo NeutralMaster = new(
        "Zharkon the Grey",
        "A mysterious sage wrapped in grey robes, eyes glowing with arcane knowledge",
        "gray",
        PlayerAlignment.Neutral
    );

    private static readonly MasterInfo EvilMaster = new(
        "Malachar the Dark",
        "A shadowy figure emanating an aura of dark power",
        "bright_red",
        PlayerAlignment.Evil
    );

    private MasterInfo currentMaster;
    private PlayerAlignment playerAlignment;

    public LevelMasterLocation() : base(GameLocation.Master, "Level Master's Sanctum",
        "A mystical chamber where warriors seek to transcend their limits.")
    {
    }

    protected override void SetupLocation()
    {
        PossibleExits.Add(GameLocation.MainStreet);
    }

    public override async Task EnterLocation(Character player, TerminalEmulator term)
    {
        // Determine player alignment before entering
        playerAlignment = DetermineAlignment(player);
        currentMaster = GetMasterForAlignment(playerAlignment);

        await base.EnterLocation(player, term);
    }

    /// <summary>
    /// Determines player alignment based on Chivalry/Darkness ratio
    /// </summary>
    private static PlayerAlignment DetermineAlignment(Character player)
    {
        long chivalry = player.Chivalry;
        long darkness = player.Darkness;

        // If both are zero or very close, default to neutral
        if (chivalry == 0 && darkness == 0)
            return PlayerAlignment.Neutral;

        // Calculate the ratio
        double total = chivalry + darkness;
        if (total == 0)
            return PlayerAlignment.Neutral;

        double chivalryRatio = chivalry / total;
        double darknessRatio = darkness / total;

        // If within 20% of each other, consider neutral
        if (Math.Abs(chivalryRatio - darknessRatio) < 0.2)
            return PlayerAlignment.Neutral;

        return chivalryRatio > darknessRatio ? PlayerAlignment.Good : PlayerAlignment.Evil;
    }

    /// <summary>
    /// Gets the appropriate master for the player's alignment
    /// </summary>
    private static MasterInfo GetMasterForAlignment(PlayerAlignment alignment)
    {
        return alignment switch
        {
            PlayerAlignment.Good => GoodMaster,
            PlayerAlignment.Evil => EvilMaster,
            _ => NeutralMaster
        };
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        terminal.SetColor(currentMaster.Color);
        terminal.WriteLine("╔════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine($"║  {currentMaster.Name.ToUpper().PadLeft((76 + currentMaster.Name.Length) / 2).PadRight(76)}║");
        terminal.WriteLine("╚════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine(currentMaster.Description);
        terminal.WriteLine("");

        // Show alignment-specific greeting
        terminal.SetColor(currentMaster.Color);
        switch (playerAlignment)
        {
            case PlayerAlignment.Good:
                terminal.WriteLine($"\"Welcome, noble {currentPlayer.DisplayName}. Your light shines brightly.\"");
                break;
            case PlayerAlignment.Evil:
                terminal.WriteLine($"\"Ah, {currentPlayer.DisplayName}... The darkness within you grows stronger.\"");
                break;
            default:
                terminal.WriteLine($"\"Greetings, seeker of balance. What brings you here, {currentPlayer.DisplayName}?\"");
                break;
        }
        terminal.WriteLine("");

        // Show XP status
        DisplayExperienceStatus();

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

    /// <summary>
    /// Display the player's experience status
    /// </summary>
    private void DisplayExperienceStatus()
    {
        terminal.SetColor("cyan");
        terminal.WriteLine($"Your Experience: {currentPlayer.Experience:N0}");

        if (currentPlayer.Level >= GameConfig.MaxLevel)
        {
            terminal.SetColor("bright_magenta");
            terminal.WriteLine("You have reached the pinnacle of mortal power!");
        }
        else
        {
            long nextLevelXP = GetExperienceForLevel(currentPlayer.Level + 1);
            long xpNeeded = nextLevelXP - currentPlayer.Experience;

            if (xpNeeded <= 0)
            {
                terminal.SetColor("bright_green");
                terminal.WriteLine($"★ You are ready to advance to level {currentPlayer.Level + 1}! ★");
            }
            else
            {
                terminal.SetColor("white");
                terminal.WriteLine($"Experience needed for level {currentPlayer.Level + 1}: {xpNeeded:N0}");
            }
        }
        terminal.WriteLine("");
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
        int startLevel = currentPlayer.Level;

        while (currentPlayer.Level < GameConfig.MaxLevel &&
               currentPlayer.Experience >= GetExperienceForLevel(currentPlayer.Level + 1))
        {
            // Raise one level
            int newLevel = currentPlayer.Level + 1;

            // Apply class-based stat increases
            ApplyClassBasedStatIncreases();

            // Apply hidden master bonuses
            ApplyMasterBonuses();

            currentPlayer.RaiseLevel(newLevel);
            levelsRaised++;
        }

        if (levelsRaised == 0)
        {
            long needed = GetExperienceForLevel(currentPlayer.Level + 1) - currentPlayer.Experience;
            terminal.SetColor("yellow");
            terminal.WriteLine($"You still need {needed:N0} experience to reach level {currentPlayer.Level + 1}.");
            await Task.Delay(2000);
        }
        else
        {
            // Full HP and Mana restore on level up
            currentPlayer.HP = currentPlayer.MaxHP;
            currentPlayer.Mana = currentPlayer.MaxMana;

            // Display level up celebration
            await DisplayLevelUpCelebration(levelsRaised, startLevel);
        }
    }

    /// <summary>
    /// Displays an elaborate level up celebration message
    /// </summary>
    private async Task DisplayLevelUpCelebration(int levelsRaised, int startLevel)
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("");
        terminal.WriteLine("  ╔══════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("  ║                         ★ LEVEL UP! ★                                ║");
        terminal.WriteLine("  ╚══════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("bright_green");
        if (levelsRaised == 1)
        {
            terminal.WriteLine($"  You have advanced to level {currentPlayer.Level}!");
        }
        else
        {
            terminal.WriteLine($"  You have advanced {levelsRaised} levels! ({startLevel} → {currentPlayer.Level})");
        }
        terminal.WriteLine("");

        terminal.SetColor(currentMaster.Color);
        switch (playerAlignment)
        {
            case PlayerAlignment.Good:
                terminal.WriteLine($"  {currentMaster.Name} places a gentle hand on your shoulder:");
                terminal.WriteLine("  \"The light within you grows ever stronger. Use it wisely.\"");
                break;
            case PlayerAlignment.Evil:
                terminal.WriteLine($"  {currentMaster.Name}'s eyes gleam with dark approval:");
                terminal.WriteLine("  \"Yes... embrace your power. The weak will tremble before you.\"");
                break;
            default:
                terminal.WriteLine($"  {currentMaster.Name} nods with quiet respect:");
                terminal.WriteLine("  \"Balance in all things. You walk the true path.\"");
                break;
        }
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("  Your body and mind surge with newfound power!");
        terminal.WriteLine("  Your health and mana have been fully restored.");
        terminal.WriteLine("");

        await terminal.PressAnyKey("  Press any key to continue...");
    }

    /// <summary>
    /// Apply class-based stat increases based on character class type
    /// </summary>
    private void ApplyClassBasedStatIncreases()
    {
        // Base stats that everyone gets
        currentPlayer.BaseMaxHP += 5;
        currentPlayer.BaseDefence += 1;
        currentPlayer.BaseStamina += 1;

        // Class-specific bonuses
        switch (currentPlayer.Class)
        {
            // Magic classes - focus on Intelligence, Wisdom, Mana
            case CharacterClass.Magician:
                currentPlayer.BaseIntelligence += 4;
                currentPlayer.BaseWisdom += 3;
                currentPlayer.BaseMaxMana += 15;
                currentPlayer.BaseMaxHP += 3;
                currentPlayer.BaseStrength += 1;
                currentPlayer.BaseConstitution += 1;
                break;

            case CharacterClass.Cleric:
                currentPlayer.BaseWisdom += 4;
                currentPlayer.BaseIntelligence += 2;
                currentPlayer.BaseMaxMana += 12;
                currentPlayer.BaseMaxHP += 6;
                currentPlayer.BaseStrength += 2;
                currentPlayer.BaseConstitution += 2;
                break;

            case CharacterClass.Sage:
                currentPlayer.BaseIntelligence += 5;
                currentPlayer.BaseWisdom += 4;
                currentPlayer.BaseMaxMana += 18;
                currentPlayer.BaseMaxHP += 2;
                currentPlayer.BaseStrength += 1;
                break;

            case CharacterClass.Alchemist:
                currentPlayer.BaseIntelligence += 3;
                currentPlayer.BaseWisdom += 2;
                currentPlayer.BaseDexterity += 2;
                currentPlayer.BaseMaxMana += 10;
                currentPlayer.BaseMaxHP += 5;
                currentPlayer.BaseConstitution += 2;
                break;

            // Warrior classes - focus on Strength, Constitution, HP
            case CharacterClass.Warrior:
                currentPlayer.BaseStrength += 4;
                currentPlayer.BaseConstitution += 3;
                currentPlayer.BaseMaxHP += 12;
                currentPlayer.BaseDexterity += 2;
                currentPlayer.BaseDefence += 2;
                break;

            case CharacterClass.Barbarian:
                currentPlayer.BaseStrength += 5;
                currentPlayer.BaseConstitution += 4;
                currentPlayer.BaseMaxHP += 15;
                currentPlayer.BaseStamina += 2;
                break;

            case CharacterClass.Paladin:
                currentPlayer.BaseStrength += 3;
                currentPlayer.BaseConstitution += 3;
                currentPlayer.BaseWisdom += 2;
                currentPlayer.BaseCharisma += 2;
                currentPlayer.BaseMaxHP += 10;
                currentPlayer.BaseMaxMana += 5;
                currentPlayer.BaseDefence += 1;
                break;

            // Agile classes - focus on Dexterity, Agility, Stamina
            case CharacterClass.Assassin:
                currentPlayer.BaseDexterity += 4;
                currentPlayer.BaseAgility += 3;
                currentPlayer.BaseStrength += 2;
                currentPlayer.BaseMaxHP += 6;
                currentPlayer.BaseStamina += 2;
                break;

            case CharacterClass.Ranger:
                currentPlayer.BaseDexterity += 3;
                currentPlayer.BaseAgility += 3;
                currentPlayer.BaseStrength += 2;
                currentPlayer.BaseWisdom += 1;
                currentPlayer.BaseMaxHP += 8;
                currentPlayer.BaseStamina += 2;
                break;

            case CharacterClass.Jester:
                currentPlayer.BaseDexterity += 3;
                currentPlayer.BaseAgility += 3;
                currentPlayer.BaseCharisma += 3;
                currentPlayer.BaseMaxHP += 5;
                currentPlayer.BaseStamina += 2;
                break;

            case CharacterClass.Bard:
                currentPlayer.BaseCharisma += 4;
                currentPlayer.BaseDexterity += 2;
                currentPlayer.BaseAgility += 2;
                currentPlayer.BaseIntelligence += 2;
                currentPlayer.BaseMaxHP += 5;
                currentPlayer.BaseMaxMana += 5;
                break;

            default:
                // Fallback for any undefined class
                currentPlayer.BaseStrength += 2;
                currentPlayer.BaseConstitution += 2;
                currentPlayer.BaseMaxHP += 8;
                break;
        }

        // Recalculate all stats from base values
        currentPlayer.RecalculateStats();
    }

    /// <summary>
    /// Apply hidden master-specific bonuses based on alignment
    /// These are not shown to the player but provide small advantages
    /// </summary>
    private void ApplyMasterBonuses()
    {
        switch (playerAlignment)
        {
            case PlayerAlignment.Good:
                // Good master grants bonus Wisdom and Charisma
                currentPlayer.BaseWisdom += 1;
                currentPlayer.BaseCharisma += 1;
                // Small healing boost
                currentPlayer.BaseMaxHP += 2;
                break;

            case PlayerAlignment.Evil:
                // Evil master grants bonus Strength and Constitution
                currentPlayer.BaseStrength += 1;
                currentPlayer.BaseConstitution += 1;
                // Small damage boost
                currentPlayer.BaseMaxHP += 1;
                break;

            case PlayerAlignment.Neutral:
                // Neutral master grants bonus Intelligence and Dexterity
                currentPlayer.BaseIntelligence += 1;
                currentPlayer.BaseDexterity += 1;
                // Small mana boost
                currentPlayer.BaseMaxMana += 3;
                break;
        }

        // Recalculate after master bonuses
        currentPlayer.RecalculateStats();
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
            // Gentler curve: level^1.8 * 50 instead of level^2.5 * 100
            exp += (long)(Math.Pow(i, 1.8) * 50);
        }
        return exp;
    }

    #endregion
}

/// <summary>
/// Represents a level master's information
/// </summary>
public record MasterInfo(string Name, string Description, string Color, PlayerAlignment Alignment);

/// <summary>
/// Player alignment for determining which master to use
/// </summary>
public enum PlayerAlignment
{
    Good,
    Neutral,
    Evil
}
