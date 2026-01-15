using UsurperRemake.Utils;
using UsurperRemake.Systems;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Castle Location - Pascal-compatible royal court system
/// Based on CASTLE.PAS with complete King and commoner interfaces
/// </summary>
public class CastleLocation : BaseLocation
{
    private static King currentKing = null;
    private static List<MonarchRecord> monarchHistory = new();
    private static List<RoyalMailMessage> royalMail = new();
    private bool playerIsKing = false;
    private Random random = new Random();

    public CastleLocation() : base(
        GameLocation.Castle,
        "The Royal Castle",
        "You approach the magnificent royal castle, seat of power in the kingdom."
    ) { }

    protected override void SetupLocation()
    {
        base.SetupLocation();

        playerIsKing = currentPlayer?.King ?? false;

        // Load or create king data
        LoadKingData();
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();

        if (playerIsKing)
        {
            DisplayRoyalCastleInterior();
        }
        else
        {
            DisplayCastleExterior();
        }
    }

    /// <summary>
    /// Display castle interior for the reigning monarch
    /// </summary>
    private void DisplayRoyalCastleInterior()
    {
        // Header - standardized format
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔═════════════════════════════════════════════════════════════════════════════╗");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("║                            THE ROYAL CASTLE                                 ║");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╚═════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Royal greeting
        terminal.SetColor("cyan");
        terminal.WriteLine("You have entered the Great Hall. Upon your arrival you are");
        terminal.WriteLine("immediately surrounded by a flock of servants and advisors.");
        terminal.SetColor("white");
        terminal.WriteLine("You greet your staff with a subtle nod.");
        terminal.WriteLine("");

        // Royal treasury status
        terminal.SetColor("bright_green");
        terminal.Write("The Royal Purse");
        terminal.SetColor("white");
        terminal.Write(" has ");
        terminal.SetColor("bright_yellow");
        terminal.Write($"{currentKing.Treasury:N0}");
        terminal.SetColor("white");
        terminal.WriteLine(" gold pieces.");

        // Guard status
        terminal.SetColor("cyan");
        terminal.Write("Royal Guards: ");
        terminal.SetColor("white");
        terminal.WriteLine($"{currentKing.Guards.Count}/{GameConfig.MaxRoyalGuards}");

        // Prisoners
        terminal.SetColor("red");
        terminal.Write("Prisoners: ");
        terminal.SetColor("white");
        terminal.WriteLine($"{currentKing.Prisoners.Count}");

        // Unread mail
        int unreadMail = royalMail.Count(m => !m.IsRead);
        if (unreadMail > 0)
        {
            terminal.SetColor("bright_magenta");
            terminal.WriteLine($"You have {unreadMail} unread messages!");
        }
        terminal.WriteLine("");

        ShowRoyalMenu();
    }

    /// <summary>
    /// Display castle exterior for non-monarchs
    /// </summary>
    private void DisplayCastleExterior()
    {
        // Header - standardized format
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔═════════════════════════════════════════════════════════════════════════════╗");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("║                        OUTSIDE THE ROYAL CASTLE                             ║");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╚═════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Approach description
        terminal.SetColor("white");
        terminal.WriteLine("You journey the winding road up to the gates of the Castle.");
        terminal.WriteLine("Massive stone walls tower above you, and royal guards");
        terminal.WriteLine("watch your approach with keen interest.");
        terminal.WriteLine("");

        // King status
        if (currentKing != null && currentKing.IsActive)
        {
            terminal.SetColor("white");
            terminal.Write("The mighty ");
            terminal.SetColor("bright_yellow");
            terminal.Write($"{currentKing.GetTitle()} {currentKing.Name}");
            terminal.SetColor("white");
            terminal.WriteLine(" rules from within these walls.");
            terminal.SetColor("cyan");
            terminal.WriteLine($"Reign: {currentKing.TotalReign} days | Treasury: {currentKing.Treasury:N0} gold");
            terminal.WriteLine("");
        }
        else
        {
            terminal.SetColor("bright_red");
            terminal.WriteLine("The kingdom appears to be in disarray!");
            terminal.WriteLine("No monarch sits upon the throne...");
            terminal.WriteLine("");
        }

        ShowCastleExteriorMenu();
    }

    /// <summary>
    /// Show royal castle menu for the king
    /// </summary>
    private void ShowRoyalMenu()
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("Royal Commands:");
        terminal.WriteLine("");

        // Row 1
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_yellow");
        terminal.Write("P");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("rison Cells       ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("O");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("rders             ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("1");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine(" Royal Mail");

        // Row 2
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_green");
        terminal.Write("G");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("o to Sleep        ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_green");
        terminal.Write("C");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("heck Security     ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("H");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("istory of Monarchs");

        // Row 3
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_red");
        terminal.Write("A");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("bdicate           ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_magenta");
        terminal.Write("M");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("agic              ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("F");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("iscal Matters");

        // Row 4
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_cyan");
        terminal.Write("S");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("tatus             ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_magenta");
        terminal.Write("Q");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("uests             ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("T");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("he Royal Orphanage");

        terminal.WriteLine("");

        // Navigation
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_red");
        terminal.Write("R");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("eturn to Town");
        terminal.WriteLine("");
    }

    /// <summary>
    /// Show castle exterior menu for commoners
    /// </summary>
    private void ShowCastleExteriorMenu()
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("Options:");
        terminal.WriteLine("");

        // Row 1
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_yellow");
        terminal.Write("T");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("he Royal Guard    ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("P");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("rison             ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_green");
        terminal.Write("D");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("onate to Royal Purse");

        // Row 2
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_cyan");
        terminal.Write("H");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("istory of Monarchs ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_magenta");
        terminal.Write("S");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("eek Audience");

        // Throne options - always show one of these
        if (currentKing != null && currentKing.IsActive)
        {
            // There is a king - show infiltrate option
            terminal.SetColor("darkgray");
            terminal.Write(" [");
            terminal.SetColor("bright_red");
            terminal.Write("I");
            terminal.SetColor("darkgray");
            terminal.Write("]");

            if (CanChallengeThrone())
            {
                terminal.SetColor("bright_red");
                terminal.WriteLine("nfiltrate Castle (Challenge for Throne)");
            }
            else if (currentPlayer.Level < GameConfig.MinLevelKing)
            {
                terminal.SetColor("gray");
                terminal.WriteLine($"nfiltrate Castle (Requires Level {GameConfig.MinLevelKing})");
            }
            else
            {
                terminal.SetColor("gray");
                terminal.WriteLine("nfiltrate Castle (Not Available)");
            }
        }
        else
        {
            // No king - show claim throne option
            terminal.SetColor("darkgray");
            terminal.Write(" [");
            terminal.SetColor("bright_yellow");
            terminal.Write("C");
            terminal.SetColor("darkgray");
            terminal.Write("]");

            if (currentPlayer.Level >= GameConfig.MinLevelKing)
            {
                terminal.SetColor("bright_yellow");
                terminal.WriteLine("laim Empty Throne");
            }
            else
            {
                terminal.SetColor("gray");
                terminal.WriteLine($"laim Empty Throne (Requires Level {GameConfig.MinLevelKing})");
            }
        }

        terminal.WriteLine("");

        // Navigation
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_red");
        terminal.Write("R");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("eturn to Town");
        terminal.WriteLine("");
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        // Handle global quick commands first
        var (handled, shouldExit) = await TryProcessGlobalCommand(choice);
        if (handled) return shouldExit;

        if (string.IsNullOrWhiteSpace(choice))
            return false;

        var upperChoice = choice.ToUpper().Trim();

        if (playerIsKing)
        {
            return await ProcessRoyalChoice(upperChoice);
        }
        else
        {
            return await ProcessCommonerChoice(upperChoice);
        }
    }

    /// <summary>
    /// Process choices for the reigning monarch
    /// </summary>
    private async Task<bool> ProcessRoyalChoice(string choice)
    {
        switch (choice)
        {
            case "P":
                await ManagePrisonCells();
                return false;

            case "O":
                await RoyalOrders();
                return false;

            case "1":
                await ReadRoyalMail();
                return false;

            case "G":
                await RoyalSleep();
                return false;

            case "C":
                await CheckSecurity();
                return false;

            case "H":
                await ShowMonarchHistory();
                return false;

            case "A":
                return await AttemptAbdication();

            case "M":
                await CourtMagician();
                return false;

            case "F":
                await FiscalMatters();
                return false;

            case "S":
                await ShowStatus();
                return false;

            case "Q":
                await RoyalQuests();
                return false;

            case "T":
                await RoyalOrphanage();
                return false;

            case "R":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;

            default:
                terminal.SetColor("red");
                terminal.WriteLine("Invalid choice! Try again.");
                await Task.Delay(1000);
                return false;
        }
    }

    /// <summary>
    /// Process choices for commoners outside the castle
    /// </summary>
    private async Task<bool> ProcessCommonerChoice(string choice)
    {
        switch (choice)
        {
            case "T":
                await ViewRoyalGuard();
                return false;

            case "P":
                await VisitPrison();
                return false;

            case "D":
                await DonateToRoyalPurse();
                return false;

            case "H":
                await ShowMonarchHistory();
                return false;

            case "S":
                await SeekAudience();
                return false;

            case "I":
                if (CanChallengeThrone())
                {
                    return await ChallengeThrone();
                }
                else
                {
                    terminal.SetColor("red");
                    terminal.WriteLine("You are not worthy to challenge for the throne!");
                    await Task.Delay(2000);
                }
                return false;

            case "C":
                if (currentKing == null || !currentKing.IsActive)
                {
                    return await ClaimEmptyThrone();
                }
                else
                {
                    terminal.SetColor("red");
                    terminal.WriteLine("The throne is already occupied!");
                    await Task.Delay(2000);
                }
                return false;

            case "R":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;

            default:
                terminal.SetColor("red");
                terminal.WriteLine("Invalid choice! Try again.");
                await Task.Delay(1000);
                return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ROYAL GUARD SYSTEM
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task ViewRoyalGuard()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                           THE ROYAL GUARD                                   ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        if (currentKing == null || !currentKing.IsActive)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("With no monarch on the throne, the Royal Guard has disbanded.");
            terminal.WriteLine("The castle stands largely undefended...");
        }
        else if (currentKing.Guards.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"{currentKing.GetTitle()} {currentKing.Name} has no Royal Guards!");
            terminal.WriteLine("The castle relies only on its walls for defense.");
        }
        else
        {
            terminal.SetColor("white");
            terminal.WriteLine($"The Royal Guard of {currentKing.GetTitle()} {currentKing.Name}:");
            terminal.WriteLine("");

            terminal.SetColor("cyan");
            terminal.WriteLine($"{"#",-3} {"Name",-20} {"Rank",-15} {"Loyalty",-10}");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 50));

            int i = 1;
            foreach (var guard in currentKing.Guards)
            {
                string loyaltyColor = guard.Loyalty > 80 ? "bright_green" :
                                     guard.Loyalty > 50 ? "yellow" : "red";

                terminal.SetColor("white");
                terminal.Write($"{i,-3} {guard.Name,-20} ");
                terminal.SetColor("cyan");
                terminal.Write($"{"Guard",-15} ");
                terminal.SetColor(loyaltyColor);
                terminal.WriteLine($"{guard.Loyalty}%");
                i++;
            }

            terminal.WriteLine("");
            terminal.SetColor("white");
            terminal.WriteLine($"Total Guards: {currentKing.Guards.Count}/{GameConfig.MaxRoyalGuards}");
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRISON SYSTEM
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task VisitPrison()
    {
        terminal.ClearScreen();
        terminal.SetColor("red");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                           THE ROYAL PRISON                                  ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine("You peer through the iron bars at the gloomy prison cells.");
        terminal.WriteLine("The smell of damp stone fills your nostrils...");
        terminal.WriteLine("");

        if (currentKing == null || currentKing.Prisoners.Count == 0)
        {
            terminal.SetColor("white");
            terminal.WriteLine("The prison cells are empty.");
        }
        else
        {
            terminal.SetColor("cyan");
            terminal.WriteLine($"{"Name",-20} {"Crime",-20} {"Days Left",-10}");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 55));

            foreach (var prisoner in currentKing.Prisoners)
            {
                int daysLeft = Math.Max(0, prisoner.Value.Sentence - prisoner.Value.DaysServed);
                terminal.SetColor("white");
                terminal.WriteLine($"{prisoner.Value.CharacterName,-20} {prisoner.Value.Crime,-20} {daysLeft,-10}");
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    private async Task ManagePrisonCells()
    {
        bool done = false;
        while (!done)
        {
            terminal.ClearScreen();
            terminal.SetColor("bright_red");
            terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            terminal.WriteLine("║                        PRISON CELL MANAGEMENT                               ║");
            terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
            terminal.WriteLine("");

            if (currentKing.Prisoners.Count == 0)
            {
                terminal.SetColor("gray");
                terminal.WriteLine("The dungeons are empty. Justice reigns in your kingdom!");
                terminal.WriteLine("");
            }
            else
            {
                terminal.SetColor("cyan");
                terminal.WriteLine($"{"#",-3} {"Name",-18} {"Crime",-18} {"Sentence",-10} {"Served",-8} {"Bail",-10}");
                terminal.SetColor("darkgray");
                terminal.WriteLine(new string('─', 75));

                int i = 1;
                foreach (var prisoner in currentKing.Prisoners)
                {
                    var p = prisoner.Value;
                    string bailStr = p.BailAmount > 0 ? $"{p.BailAmount:N0}g" : "None";
                    terminal.SetColor("white");
                    terminal.WriteLine($"{i,-3} {p.CharacterName,-18} {p.Crime,-18} {p.Sentence,-10} {p.DaysServed,-8} {bailStr,-10}");
                    i++;
                }
                terminal.WriteLine("");
            }

            terminal.SetColor("cyan");
            terminal.WriteLine("Commands:");
            terminal.SetColor("white");
            terminal.WriteLine("(I)mprison someone  (P)ardon prisoner  (E)xecute prisoner");
            terminal.WriteLine("(S)et bail amount   (R)eturn");
            terminal.WriteLine("");

            terminal.SetColor("cyan");
            terminal.Write("Your decree: ");
            terminal.SetColor("white");
            string input = await terminal.ReadLineAsync();

            if (string.IsNullOrEmpty(input)) continue;

            switch (char.ToUpper(input[0]))
            {
                case 'I':
                    await ImprisonSomeone();
                    break;
                case 'P':
                    await PardonPrisoner();
                    break;
                case 'E':
                    await ExecutePrisoner();
                    break;
                case 'S':
                    await SetBailAmount();
                    break;
                case 'R':
                    done = true;
                    break;
            }
        }
    }

    private async Task ImprisonSomeone()
    {
        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Name of the criminal: ");
        terminal.SetColor("white");
        string name = await terminal.ReadLineAsync();

        if (string.IsNullOrEmpty(name)) return;

        terminal.SetColor("cyan");
        terminal.Write("Crime committed: ");
        terminal.SetColor("white");
        string crime = await terminal.ReadLineAsync();

        if (string.IsNullOrEmpty(crime)) crime = "General Misconduct";

        terminal.SetColor("cyan");
        terminal.Write("Sentence (days): ");
        terminal.SetColor("white");
        string sentenceStr = await terminal.ReadLineAsync();

        int sentence = 7;
        if (int.TryParse(sentenceStr, out int s) && s > 0)
            sentence = Math.Min(s, 365);

        currentKing.ImprisonCharacter(name, sentence, crime);

        terminal.SetColor("bright_green");
        terminal.WriteLine($"{name} has been imprisoned for {sentence} days!");
        NewsSystem.Instance.Newsy(true, $"{currentKing.GetTitle()} {currentKing.Name} imprisoned {name} for {crime}!");

        await Task.Delay(2000);
    }

    private async Task PardonPrisoner()
    {
        if (currentKing.Prisoners.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("There are no prisoners to pardon.");
            await Task.Delay(1500);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Name to pardon: ");
        terminal.SetColor("white");
        string name = await terminal.ReadLineAsync();

        if (currentKing.ReleaseCharacter(name))
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine($"{name} has been pardoned and released!");
            NewsSystem.Instance.Newsy(true, $"{currentKing.GetTitle()} {currentKing.Name} pardoned {name}!");
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("That person is not in the dungeon.");
        }

        await Task.Delay(2000);
    }

    private async Task ExecutePrisoner()
    {
        if (currentKing.Prisoners.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("There are no prisoners to execute.");
            await Task.Delay(1500);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("bright_red");
        terminal.WriteLine("WARNING: Executions greatly increase your Darkness!");
        terminal.SetColor("cyan");
        terminal.Write("Name to execute: ");
        terminal.SetColor("white");
        string name = await terminal.ReadLineAsync();

        if (currentKing.Prisoners.ContainsKey(name))
        {
            terminal.SetColor("cyan");
            terminal.Write("Are you SURE? (Y/N): ");
            terminal.SetColor("white");
            string confirm = await terminal.ReadLineAsync();

            if (confirm?.ToUpper() == "Y")
            {
                currentKing.Prisoners.Remove(name);
                currentPlayer.Darkness += 100;

                terminal.SetColor("red");
                terminal.WriteLine($"{name} has been executed!");
                terminal.WriteLine("Your darkness increases significantly...");
                NewsSystem.Instance.Newsy(true, $"{currentKing.GetTitle()} {currentKing.Name} executed {name}!");
            }
            else
            {
                terminal.SetColor("gray");
                terminal.WriteLine("Execution cancelled.");
            }
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("That person is not in the dungeon.");
        }

        await Task.Delay(2000);
    }

    private async Task SetBailAmount()
    {
        if (currentKing.Prisoners.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("There are no prisoners.");
            await Task.Delay(1500);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Name of prisoner: ");
        terminal.SetColor("white");
        string name = await terminal.ReadLineAsync();

        if (currentKing.Prisoners.ContainsKey(name))
        {
            terminal.SetColor("cyan");
            terminal.Write("Bail amount (0 for no bail): ");
            terminal.SetColor("white");
            string amountStr = await terminal.ReadLineAsync();

            if (long.TryParse(amountStr, out long amount) && amount >= 0)
            {
                currentKing.Prisoners[name].BailAmount = amount;
                terminal.SetColor("bright_green");
                if (amount > 0)
                    terminal.WriteLine($"Bail set to {amount:N0} gold for {name}.");
                else
                    terminal.WriteLine($"No bail allowed for {name}.");
            }
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("That person is not in the dungeon.");
        }

        await Task.Delay(2000);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ROYAL MAIL SYSTEM
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task ReadRoyalMail()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                             ROYAL MAIL                                      ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Generate some random mail if there's none
        if (royalMail.Count == 0)
        {
            GenerateRandomMail();
        }

        if (royalMail.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("Your inbox is empty.");
        }
        else
        {
            terminal.SetColor("cyan");
            terminal.WriteLine($"{"#",-3} {"From",-20} {"Subject",-35} {"Status",-10}");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 70));

            for (int i = 0; i < royalMail.Count; i++)
            {
                var mail = royalMail[i];
                string status = mail.IsRead ? "Read" : "NEW";
                string statusColor = mail.IsRead ? "gray" : "bright_yellow";

                terminal.SetColor("white");
                terminal.Write($"{i + 1,-3} {mail.Sender,-20} {mail.Subject,-35} ");
                terminal.SetColor(statusColor);
                terminal.WriteLine(status);
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Read message # (0 to return): ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= royalMail.Count)
        {
            var mail = royalMail[choice - 1];
            mail.IsRead = true;

            terminal.ClearScreen();
            terminal.SetColor("bright_cyan");
            terminal.WriteLine($"From: {mail.Sender}");
            terminal.WriteLine($"Subject: {mail.Subject}");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 60));
            terminal.SetColor("white");
            terminal.WriteLine("");
            terminal.WriteLine(mail.Body);
            terminal.WriteLine("");

            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
        }
    }

    private void GenerateRandomMail()
    {
        string[] senders = { "Royal Advisor", "Castle Steward", "High Priest", "Guild Master", "Merchant Lord", "Town Elder" };
        string[] subjects = { "Treasury Report", "Kingdom Affairs", "Request for Audience", "Trade Proposal", "Security Concerns", "Festival Planning" };
        string[] bodies = {
            "Your Majesty,\n\nThe treasury report for this quarter shows steady growth.\nOur financial situation remains stable.\n\nYour humble servant.",
            "Your Highness,\n\nThe people speak highly of your rule.\nMorale in the kingdom is good.\n\nLong live the Crown!",
            "Most Noble Sovereign,\n\nA delegation from a distant land requests an audience.\nThey bring gifts and proposals of trade.\n\nAwaiting your decision.",
            "My Liege,\n\nThe castle walls require maintenance.\nI recommend allocating funds for repairs.\n\nYour faithful steward.",
            "Your Royal Majesty,\n\nAll is well in the realm.\nThe guards report no incidents.\n\nMay your reign be long and prosperous."
        };

        // Add 1-3 random messages
        int numMessages = random.Next(1, 4);
        for (int i = 0; i < numMessages; i++)
        {
            royalMail.Add(new RoyalMailMessage
            {
                Sender = senders[random.Next(senders.Length)],
                Subject = subjects[random.Next(subjects.Length)],
                Body = bodies[random.Next(bodies.Length)],
                IsRead = false,
                Date = DateTime.Now.AddDays(-random.Next(1, 7))
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ROYAL SLEEP
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task RoyalSleep()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_blue");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                          THE ROYAL CHAMBERS                                 ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("You retire to your opulent royal chambers.");
        terminal.WriteLine("Servants draw a warm bath and prepare your bed.");
        terminal.WriteLine("");

        await Task.Delay(1500);

        // Full heal
        currentPlayer.HP = currentPlayer.MaxHP;
        currentPlayer.Mana = currentPlayer.MaxMana;

        // Process daily activities
        currentKing.ProcessDailyActivities();

        terminal.SetColor("bright_green");
        terminal.WriteLine("You rest peacefully through the night...");
        terminal.WriteLine("");
        terminal.WriteLine($"HP restored to {currentPlayer.MaxHP}!");
        terminal.WriteLine($"Mana restored to {currentPlayer.MaxMana}!");
        terminal.WriteLine("");

        // Show daily report
        terminal.SetColor("cyan");
        terminal.WriteLine("=== Daily Kingdom Report ===");
        terminal.SetColor("white");
        terminal.WriteLine($"Daily Income: {currentKing.CalculateDailyIncome():N0} gold");
        terminal.WriteLine($"Daily Expenses: {currentKing.CalculateDailyExpenses():N0} gold");
        terminal.WriteLine($"Treasury Balance: {currentKing.Treasury:N0} gold");
        terminal.WriteLine($"Days of Reign: {currentKing.TotalReign}");

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SECURITY CHECK
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task CheckSecurity()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                          SECURITY REPORT                                    ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // NPC Guard summary
        terminal.SetColor("cyan");
        terminal.WriteLine("=== Royal Guard Status ===");
        terminal.SetColor("white");
        terminal.WriteLine($"NPC Guards: {currentKing.Guards.Count}/{King.MaxNPCGuards}");

        if (currentKing.Guards.Count > 0)
        {
            int avgLoyalty = (int)currentKing.Guards.Average(g => g.Loyalty);
            string loyaltyStatus = avgLoyalty > 80 ? "Excellent" : avgLoyalty > 60 ? "Good" : avgLoyalty > 40 ? "Fair" : "Poor";
            terminal.WriteLine($"Average Loyalty: {avgLoyalty}% ({loyaltyStatus})");
            terminal.WriteLine($"Daily Guard Costs: {currentKing.Guards.Sum(g => g.DailySalary):N0} gold");
            terminal.WriteLine("");

            // Individual guard listing
            terminal.SetColor("cyan");
            terminal.WriteLine($"{"Name",-20} {"Loyalty",-12} {"Salary",-12} {"Status",-10}");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 55));

            foreach (var guard in currentKing.Guards)
            {
                string loyaltyColor = guard.Loyalty > 80 ? "bright_green" :
                                     guard.Loyalty > 50 ? "yellow" : "red";
                string status = guard.IsActive ? "Active" : "Inactive";

                terminal.SetColor("white");
                terminal.Write($"{guard.Name,-20} ");
                terminal.SetColor(loyaltyColor);
                terminal.Write($"{guard.Loyalty}%{"",-9} ");
                terminal.SetColor("white");
                terminal.WriteLine($"{guard.DailySalary,-12} {status,-10}");
            }
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("No NPC guards currently employed.");
        }

        terminal.WriteLine("");

        // Monster Guard summary
        terminal.SetColor("bright_red");
        terminal.WriteLine("=== Monster Guards ===");
        terminal.SetColor("white");
        terminal.WriteLine($"Monster Guards: {currentKing.MonsterGuards.Count}/{King.MaxMonsterGuards}");

        if (currentKing.MonsterGuards.Count > 0)
        {
            terminal.WriteLine($"Daily Feeding Costs: {currentKing.MonsterGuards.Sum(m => m.DailyFeedingCost):N0} gold");
            terminal.WriteLine("");

            terminal.SetColor("cyan");
            terminal.WriteLine($"{"Name",-20} {"Level",-8} {"HP",-12} {"Strength",-10}");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 55));

            foreach (var monster in currentKing.MonsterGuards)
            {
                terminal.SetColor("red");
                terminal.WriteLine($"{monster.Name,-20} {monster.Level,-8} {monster.HP}/{monster.MaxHP,-8} {monster.Strength,-10}");
            }
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("No monster guards currently protecting the castle.");
        }

        terminal.WriteLine("");

        // Security assessment
        terminal.SetColor("cyan");
        terminal.WriteLine("=== Security Assessment ===");
        int securityLevel = CalculateSecurityLevel();
        string securityColor = securityLevel > 80 ? "bright_green" : securityLevel > 50 ? "yellow" : "red";
        terminal.SetColor(securityColor);
        terminal.WriteLine($"Overall Security: {securityLevel}%");
        terminal.SetColor("white");
        terminal.WriteLine($"Total Guards: {currentKing.TotalGuardCount} (Challengers fight monsters first, then NPC guards)");

        if (securityLevel < 50)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Your castle is vulnerable to attack!");
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("Guard Commands:");
        terminal.SetColor("white");
        terminal.WriteLine("(H)ire NPC guard    (M)onster guard    (F)ire guard");
        terminal.WriteLine("(P)ay bonus         (D)ismiss monster  (R)eturn");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.Write("Command: ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (!string.IsNullOrEmpty(input))
        {
            switch (char.ToUpper(input[0]))
            {
                case 'H':
                    await HireGuard();
                    break;
                case 'M':
                    await HireMonsterGuard();
                    break;
                case 'F':
                    await FireGuard();
                    break;
                case 'D':
                    await DismissMonsterGuard();
                    break;
                case 'P':
                    await PayGuardBonus();
                    break;
            }
        }
    }

    private int CalculateSecurityLevel()
    {
        int totalGuards = currentKing.TotalGuardCount;
        if (totalGuards == 0) return 10;

        // NPC guards contribute based on loyalty and count
        int npcContribution = 0;
        if (currentKing.Guards.Count > 0)
        {
            int avgLoyalty = (int)currentKing.Guards.Average(g => g.Loyalty);
            npcContribution = (currentKing.Guards.Count * 10) + (avgLoyalty / 5);
        }

        // Monster guards contribute based on level and count
        int monsterContribution = 0;
        if (currentKing.MonsterGuards.Count > 0)
        {
            int avgLevel = (int)currentKing.MonsterGuards.Average(m => m.Level);
            monsterContribution = (currentKing.MonsterGuards.Count * 8) + (avgLevel / 2);
        }

        return Math.Min(100, npcContribution + monsterContribution + 10);
    }

    private async Task HireMonsterGuard()
    {
        if (currentKing.MonsterGuards.Count >= King.MaxMonsterGuards)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You already have the maximum number of monster guards!");
            await Task.Delay(2000);
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor("bright_red");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                      MONSTER GUARD MARKET                                   ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("The Beast Master presents fearsome creatures for your protection.");
        terminal.WriteLine($"Treasury: {currentKing.Treasury:N0} gold");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine($"{"#",-3} {"Name",-20} {"Level",-8} {"Cost",-12} {"Feeding/Day",-12}");
        terminal.SetColor("darkgray");
        terminal.WriteLine(new string('─', 60));

        int i = 1;
        foreach (var (name, level, cost) in MonsterGuardTypes.AvailableMonsters)
        {
            long actualCost = cost + (currentKing.MonsterGuards.Count * 500);
            long feedingCost = 50 + (level * 10);

            terminal.SetColor("white");
            terminal.WriteLine($"{i,-3} {name,-20} {level,-8} {actualCost:N0,-12} {feedingCost:N0,-12}");
            i++;
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Purchase which monster? (0 to cancel): ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= MonsterGuardTypes.AvailableMonsters.Length)
        {
            var (name, level, cost) = MonsterGuardTypes.AvailableMonsters[choice - 1];

            if (currentKing.AddMonsterGuard(name, level, cost))
            {
                terminal.SetColor("bright_green");
                terminal.WriteLine($"A {name} has been added to your castle defenses!");
                NewsSystem.Instance.Newsy(true, $"{currentKing.GetTitle()} {currentKing.Name} acquired a fearsome {name} to guard the castle!");
            }
            else
            {
                terminal.SetColor("red");
                terminal.WriteLine("Insufficient funds!");
            }
        }

        await Task.Delay(2000);
    }

    private async Task DismissMonsterGuard()
    {
        if (currentKing.MonsterGuards.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You have no monster guards to dismiss.");
            await Task.Delay(2000);
            return;
        }

        terminal.SetColor("cyan");
        terminal.Write("Name of monster to dismiss: ");
        terminal.SetColor("white");
        string name = await terminal.ReadLineAsync();

        if (currentKing.RemoveMonsterGuard(name))
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"The {name} has been released from service.");
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("No monster by that name found.");
        }

        await Task.Delay(2000);
    }

    private async Task HireGuard()
    {
        if (currentKing.Guards.Count >= King.MaxNPCGuards)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You already have the maximum number of guards!");
            await Task.Delay(2000);
            return;
        }

        if (currentKing.Treasury < GameConfig.GuardRecruitmentCost)
        {
            terminal.SetColor("red");
            terminal.WriteLine($"Insufficient funds! Need {GameConfig.GuardRecruitmentCost:N0} gold.");
            await Task.Delay(2000);
            return;
        }

        // Generate random guard name
        string[] firstNames = { "Sir Marcus", "Sir Gerald", "Lady Helena", "Sir Roland", "Dame Elise", "Sir Bartholomew", "Lady Catherine", "Sir Edmund" };
        string guardName = firstNames[random.Next(firstNames.Length)];

        terminal.SetColor("cyan");
        terminal.Write($"Hire {guardName}? Cost: {GameConfig.GuardRecruitmentCost:N0} gold (Y/N): ");
        terminal.SetColor("white");
        string confirm = await terminal.ReadLineAsync();

        if (confirm?.ToUpper() == "Y")
        {
            CharacterSex sex = guardName.StartsWith("Lady") || guardName.StartsWith("Dame") ? CharacterSex.Female : CharacterSex.Male;
            if (currentKing.AddGuard(guardName, CharacterAI.Computer, sex, GameConfig.BaseGuardSalary))
            {
                terminal.SetColor("bright_green");
                terminal.WriteLine($"{guardName} has joined the Royal Guard!");
            }
        }

        await Task.Delay(2000);
    }

    private async Task FireGuard()
    {
        if (currentKing.Guards.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You have no guards to dismiss.");
            await Task.Delay(2000);
            return;
        }

        terminal.SetColor("cyan");
        terminal.Write("Name of guard to dismiss: ");
        terminal.SetColor("white");
        string name = await terminal.ReadLineAsync();

        if (currentKing.RemoveGuard(name))
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"{name} has been dismissed from the Royal Guard.");
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("No guard by that name found.");
        }

        await Task.Delay(2000);
    }

    private async Task PayGuardBonus()
    {
        if (currentKing.Guards.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You have no guards to pay.");
            await Task.Delay(2000);
            return;
        }

        terminal.SetColor("cyan");
        terminal.Write("Bonus amount per guard: ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (long.TryParse(input, out long bonus) && bonus > 0)
        {
            long totalCost = bonus * currentKing.Guards.Count;
            if (totalCost > currentKing.Treasury)
            {
                terminal.SetColor("red");
                terminal.WriteLine("Insufficient funds in treasury!");
            }
            else
            {
                currentKing.Treasury -= totalCost;
                foreach (var guard in currentKing.Guards)
                {
                    guard.Loyalty = Math.Min(100, guard.Loyalty + (int)(bonus / 100));
                }
                terminal.SetColor("bright_green");
                terminal.WriteLine($"Paid {bonus:N0} gold bonus to each guard!");
                terminal.WriteLine("Guard loyalty has increased!");
            }
        }

        await Task.Delay(2000);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // MONARCH HISTORY
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task ShowMonarchHistory()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                         HISTORY OF MONARCHS                                 ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        if (monarchHistory.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("The history books are empty. Your reign may be the first!");
            terminal.WriteLine("");
        }
        else
        {
            terminal.SetColor("cyan");
            terminal.WriteLine($"{"#",-3} {"Name",-25} {"Title",-8} {"Reign",-12} {"End",-15}");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 65));

            int i = 1;
            foreach (var monarch in monarchHistory.OrderByDescending(m => m.CoronationDate))
            {
                terminal.SetColor("white");
                terminal.WriteLine($"{i,-3} {monarch.Name,-25} {monarch.Title,-8} {monarch.DaysReigned,-12} {monarch.EndReason,-15}");
                i++;
            }
            terminal.WriteLine("");
        }

        // Current monarch
        if (currentKing != null && currentKing.IsActive)
        {
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("=== CURRENT MONARCH ===");
            terminal.SetColor("white");
            terminal.WriteLine($"{currentKing.GetTitle()} {currentKing.Name}");
            terminal.WriteLine($"Reign: {currentKing.TotalReign} days");
            terminal.WriteLine($"Coronation: {currentKing.CoronationDate:d}");
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // COURT MAGICIAN
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task CourtMagician()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                          THE COURT MAGICIAN                                 ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("An elderly wizard in flowing robes approaches you.");
        terminal.SetColor("white");
        terminal.WriteLine("\"Greetings, Your Majesty. How may I serve the Crown?\"");
        terminal.WriteLine("");

        terminal.SetColor("yellow");
        terminal.WriteLine($"Magic Budget: {currentKing.MagicBudget:N0} gold");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("Available Services:");
        terminal.SetColor("white");
        terminal.WriteLine("(B)less the Kingdom   - 1,000 gold - Increase kingdom morale");
        terminal.WriteLine("(D)etect Threats      - 500 gold   - Reveal potential dangers");
        terminal.WriteLine("(P)rotection Spell    - 2,000 gold - Boost castle defenses");
        terminal.WriteLine("(S)cry on Enemy       - 1,500 gold - Learn about rivals");
        terminal.WriteLine("(R)eturn");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.Write("Your wish: ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (!string.IsNullOrEmpty(input))
        {
            switch (char.ToUpper(input[0]))
            {
                case 'B':
                    await CastBlessKingdom();
                    break;
                case 'D':
                    await CastDetectThreats();
                    break;
                case 'P':
                    await CastProtection();
                    break;
                case 'S':
                    await CastScry();
                    break;
            }
        }
    }

    private async Task CastBlessKingdom()
    {
        if (currentKing.MagicBudget < 1000)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Insufficient magic budget!");
            await Task.Delay(2000);
            return;
        }

        currentKing.MagicBudget -= 1000;
        currentPlayer.Chivalry += 25;

        terminal.SetColor("bright_yellow");
        terminal.WriteLine("");
        terminal.WriteLine("The wizard raises his staff and incants ancient words...");
        await Task.Delay(1500);
        terminal.SetColor("bright_green");
        terminal.WriteLine("A golden light spreads across the kingdom!");
        terminal.WriteLine("The people feel blessed! Your chivalry increases!");

        NewsSystem.Instance.Newsy(true, $"{currentKing.GetTitle()} {currentKing.Name} blessed the kingdom with powerful magic!");

        await Task.Delay(2500);
    }

    private async Task CastDetectThreats()
    {
        if (currentKing.MagicBudget < 500)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Insufficient magic budget!");
            await Task.Delay(2000);
            return;
        }

        currentKing.MagicBudget -= 500;

        terminal.SetColor("bright_blue");
        terminal.WriteLine("");
        terminal.WriteLine("The wizard gazes into a crystal ball...");
        await Task.Delay(1500);

        // Get potential threats (high darkness NPCs)
        var threats = NPCSpawnSystem.Instance.ActiveNPCs
            .Where(n => n.IsAlive && n.Darkness > 300)
            .Take(3)
            .ToList();

        if (threats.Count == 0)
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine("\"I sense no significant threats to your realm, Your Majesty.\"");
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("\"I sense darkness in these individuals...\"");
            terminal.WriteLine("");
            foreach (var threat in threats)
            {
                terminal.SetColor("red");
                terminal.WriteLine($"  - {threat.Name} (Level {threat.Level}) - Darkness: {threat.Darkness}");
            }
        }

        await Task.Delay(3000);
    }

    private async Task CastProtection()
    {
        if (currentKing.MagicBudget < 2000)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Insufficient magic budget!");
            await Task.Delay(2000);
            return;
        }

        currentKing.MagicBudget -= 2000;

        // Boost all guards
        foreach (var guard in currentKing.Guards)
        {
            guard.Loyalty = Math.Min(100, guard.Loyalty + 10);
        }

        terminal.SetColor("bright_cyan");
        terminal.WriteLine("");
        terminal.WriteLine("The wizard weaves protective enchantments...");
        await Task.Delay(1500);
        terminal.SetColor("bright_green");
        terminal.WriteLine("The castle glows with magical protection!");
        terminal.WriteLine("Guard loyalty increased!");

        await Task.Delay(2500);
    }

    private async Task CastScry()
    {
        if (currentKing.MagicBudget < 1500)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Insufficient magic budget!");
            await Task.Delay(2000);
            return;
        }

        currentKing.MagicBudget -= 1500;

        terminal.SetColor("bright_blue");
        terminal.WriteLine("");
        terminal.WriteLine("The wizard peers through the mists of time...");
        await Task.Delay(1500);

        // Show info about a random powerful NPC
        var targets = NPCSpawnSystem.Instance.ActiveNPCs
            .Where(n => n.IsAlive && n.Level > 5)
            .ToList();

        if (targets.Count > 0)
        {
            var target = targets[random.Next(targets.Count)];
            terminal.SetColor("cyan");
            terminal.WriteLine($"\"I see {target.Name}...\"");
            terminal.SetColor("white");
            terminal.WriteLine($"  Level: {target.Level}");
            terminal.WriteLine($"  Location: {target.CurrentLocation}");
            terminal.WriteLine($"  Team: {(string.IsNullOrEmpty(target.Team) ? "None" : target.Team)}");
            terminal.WriteLine($"  Gold: ~{target.Gold:N0}");
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("\"The mists reveal nothing of note, Your Majesty.\"");
        }

        await Task.Delay(3000);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FISCAL MATTERS
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task FiscalMatters()
    {
        bool done = false;
        while (!done)
        {
            terminal.ClearScreen();
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            terminal.WriteLine("║                           FISCAL MATTERS                                    ║");
            terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
            terminal.WriteLine("");

            terminal.SetColor("white");
            terminal.WriteLine($"Royal Treasury: {currentKing.Treasury:N0} gold");
            terminal.WriteLine($"Current Tax Rate: {currentKing.TaxRate} gold per citizen");
            terminal.WriteLine($"Tax Alignment: {currentKing.TaxAlignment}");
            terminal.WriteLine("");

            terminal.SetColor("cyan");
            terminal.WriteLine("=== Financial Summary ===");
            terminal.SetColor("bright_green");
            terminal.WriteLine($"Daily Income:    {currentKing.CalculateDailyIncome():N0} gold");
            terminal.SetColor("red");
            terminal.WriteLine($"Daily Expenses:  {currentKing.CalculateDailyExpenses():N0} gold");
            terminal.SetColor("white");
            long netIncome = currentKing.CalculateDailyIncome() - currentKing.CalculateDailyExpenses();
            string netColor = netIncome >= 0 ? "bright_green" : "red";
            terminal.SetColor(netColor);
            terminal.WriteLine($"Net Daily:       {netIncome:N0} gold");
            terminal.WriteLine("");

            terminal.SetColor("cyan");
            terminal.WriteLine("Options:");
            terminal.SetColor("white");
            terminal.WriteLine("(T)ax Policy        (B)udget Details    (W)ithdraw from Treasury");
            terminal.WriteLine("(D)eposit to Treasury                   (R)eturn");
            terminal.WriteLine("");

            terminal.SetColor("cyan");
            terminal.Write("Decision: ");
            terminal.SetColor("white");
            string input = await terminal.ReadLineAsync();

            if (string.IsNullOrEmpty(input)) continue;

            switch (char.ToUpper(input[0]))
            {
                case 'T':
                    await SetTaxPolicy();
                    break;
                case 'B':
                    await ShowBudgetDetails();
                    break;
                case 'W':
                    await WithdrawFromTreasury();
                    break;
                case 'D':
                    await DepositToTreasury();
                    break;
                case 'R':
                    done = true;
                    break;
            }
        }
    }

    private async Task SetTaxPolicy()
    {
        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("Tax Alignments:");
        terminal.SetColor("white");
        terminal.WriteLine("1. All citizens");
        terminal.WriteLine("2. Good-aligned only");
        terminal.WriteLine("3. Evil-aligned only");
        terminal.WriteLine("4. Neutrals only");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.Write("New tax alignment (1-4): ");
        terminal.SetColor("white");
        string alignInput = await terminal.ReadLineAsync();

        if (int.TryParse(alignInput, out int alignChoice) && alignChoice >= 1 && alignChoice <= 4)
        {
            currentKing.TaxAlignment = alignChoice switch
            {
                1 => GameConfig.TaxAlignment.All,
                2 => GameConfig.TaxAlignment.Good,
                3 => GameConfig.TaxAlignment.Evil,
                4 => GameConfig.TaxAlignment.All, // No Neutral enum, use All
                _ => GameConfig.TaxAlignment.All
            };
        }

        terminal.SetColor("cyan");
        terminal.Write("New tax rate (gold per citizen): ");
        terminal.SetColor("white");
        string rateInput = await terminal.ReadLineAsync();

        if (long.TryParse(rateInput, out long rate) && rate >= 0)
        {
            currentKing.TaxRate = Math.Min(rate, 1000); // Cap at 1000
            terminal.SetColor("bright_green");
            terminal.WriteLine($"Tax rate set to {currentKing.TaxRate} gold.");

            if (rate > 100)
            {
                terminal.SetColor("yellow");
                terminal.WriteLine("Warning: High taxes may cause unrest!");
            }
        }

        await Task.Delay(2000);
    }

    private async Task ShowBudgetDetails()
    {
        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("=== Expense Breakdown ===");
        terminal.SetColor("white");

        long guardCosts = currentKing.Guards.Sum(g => g.DailySalary);
        long orphanCosts = currentKing.Orphans.Count * GameConfig.OrphanCareCost;
        long baseCosts = 1000;

        terminal.WriteLine($"Guard Salaries:    {guardCosts:N0} gold ({currentKing.Guards.Count} guards)");
        terminal.WriteLine($"Orphanage Costs:   {orphanCosts:N0} gold ({currentKing.Orphans.Count} orphans)");
        terminal.WriteLine($"Castle Maintenance: {baseCosts:N0} gold");
        terminal.SetColor("darkgray");
        terminal.WriteLine(new string('─', 40));
        terminal.SetColor("white");
        terminal.WriteLine($"Total Daily:       {guardCosts + orphanCosts + baseCosts:N0} gold");

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    private async Task WithdrawFromTreasury()
    {
        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write($"Withdraw how much? (Max: {currentKing.Treasury:N0}): ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (long.TryParse(input, out long amount) && amount > 0)
        {
            if (amount > currentKing.Treasury)
            {
                terminal.SetColor("red");
                terminal.WriteLine("The treasury doesn't have that much!");
            }
            else
            {
                currentKing.Treasury -= amount;
                currentPlayer.Gold += amount;
                terminal.SetColor("bright_green");
                terminal.WriteLine($"Withdrew {amount:N0} gold from the treasury.");
            }
        }

        await Task.Delay(2000);
    }

    private async Task DepositToTreasury()
    {
        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write($"Deposit how much? (You have: {currentPlayer.Gold:N0}): ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (long.TryParse(input, out long amount) && amount > 0)
        {
            if (amount > currentPlayer.Gold)
            {
                terminal.SetColor("red");
                terminal.WriteLine("You don't have that much gold!");
            }
            else
            {
                currentPlayer.Gold -= amount;
                currentKing.Treasury += amount;
                terminal.SetColor("bright_green");
                terminal.WriteLine($"Deposited {amount:N0} gold to the treasury.");
            }
        }

        await Task.Delay(2000);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ROYAL ORDERS
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task RoyalOrders()
    {
        bool done = false;
        while (!done)
        {
            terminal.ClearScreen();
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            terminal.WriteLine("║                             ROYAL ORDERS                                    ║");
            terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
            terminal.WriteLine("");

            terminal.SetColor("white");
            terminal.WriteLine("Issue decrees and manage the kingdom.");
            terminal.WriteLine("");

            if (!string.IsNullOrEmpty(currentKing.LastProclamation))
            {
                terminal.SetColor("cyan");
                terminal.WriteLine("Last Proclamation:");
                terminal.SetColor("gray");
                terminal.WriteLine($"\"{currentKing.LastProclamation}\"");
                terminal.WriteLine("");
            }

            terminal.SetColor("cyan");
            terminal.WriteLine("Commands:");
            terminal.SetColor("white");
            terminal.WriteLine("(E)stablishments    (P)roclamation     (B)ounty on someone");
            terminal.WriteLine("(L)evel Masters     (R)eturn");
            terminal.WriteLine("");

            terminal.SetColor("cyan");
            terminal.Write("Orders: ");
            terminal.SetColor("white");
            string input = await terminal.ReadLineAsync();

            if (string.IsNullOrEmpty(input)) continue;

            switch (char.ToUpper(input[0]))
            {
                case 'E':
                    await ManageEstablishments();
                    break;
                case 'P':
                    await IssueProclamation();
                    break;
                case 'B':
                    await PlaceBounty();
                    break;
                case 'L':
                    await ManageLevelMasters();
                    break;
                case 'R':
                    done = true;
                    break;
            }
        }
    }

    private async Task ManageEstablishments()
    {
        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("=== Establishment Status ===");
        terminal.WriteLine("");

        int i = 1;
        var establishments = currentKing.EstablishmentStatus.ToList();
        foreach (var est in establishments)
        {
            string status = est.Value ? "OPEN" : "CLOSED";
            string statusColor = est.Value ? "bright_green" : "red";

            terminal.SetColor("white");
            terminal.Write($"{i}. {est.Key,-20} ");
            terminal.SetColor(statusColor);
            terminal.WriteLine(status);
            i++;
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Toggle which establishment? (0 to return): ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= establishments.Count)
        {
            var key = establishments[choice - 1].Key;
            currentKing.EstablishmentStatus[key] = !currentKing.EstablishmentStatus[key];

            string newStatus = currentKing.EstablishmentStatus[key] ? "opened" : "closed";
            terminal.SetColor("bright_green");
            terminal.WriteLine($"The {key} is now {newStatus}!");

            NewsSystem.Instance.Newsy(true, $"{currentKing.GetTitle()} {currentKing.Name} has {newStatus} the {key}!");

            await Task.Delay(2000);
        }
    }

    private async Task IssueProclamation()
    {
        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("Issue a Royal Proclamation to all citizens.");
        terminal.Write("Your decree: ");
        terminal.SetColor("white");
        string proclamation = await terminal.ReadLineAsync();

        if (!string.IsNullOrEmpty(proclamation))
        {
            currentKing.LastProclamation = proclamation;
            currentKing.LastProclamationDate = DateTime.Now;

            terminal.SetColor("bright_yellow");
            terminal.WriteLine("");
            terminal.WriteLine("HEAR YE, HEAR YE!");
            terminal.SetColor("white");
            terminal.WriteLine($"By royal decree of {currentKing.GetTitle()} {currentKing.Name}:");
            terminal.WriteLine($"\"{proclamation}\"");

            NewsSystem.Instance.Newsy(true, $"Royal Proclamation: \"{proclamation}\" - {currentKing.GetTitle()} {currentKing.Name}");

            await Task.Delay(3000);
        }
    }

    private async Task PlaceBounty()
    {
        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Name of the criminal: ");
        terminal.SetColor("white");
        string name = await terminal.ReadLineAsync();

        if (string.IsNullOrEmpty(name)) return;

        terminal.SetColor("cyan");
        terminal.Write("Bounty amount: ");
        terminal.SetColor("white");
        string amountStr = await terminal.ReadLineAsync();

        if (long.TryParse(amountStr, out long amount) && amount > 0)
        {
            if (amount > currentKing.Treasury)
            {
                terminal.SetColor("red");
                terminal.WriteLine("Insufficient funds in treasury!");
            }
            else
            {
                currentKing.Treasury -= amount;
                terminal.SetColor("bright_red");
                terminal.WriteLine($"A bounty of {amount:N0} gold has been placed on {name}!");

                NewsSystem.Instance.Newsy(true, $"BOUNTY: {amount:N0} gold on {name} by order of {currentKing.GetTitle()} {currentKing.Name}!");
            }
        }

        await Task.Delay(2500);
    }

    private async Task ManageLevelMasters()
    {
        terminal.WriteLine("");
        terminal.SetColor("gray");
        terminal.WriteLine("Level Master appointments are handled automatically.");
        terminal.WriteLine("Masters train adventurers to advance in their classes.");
        await Task.Delay(2000);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ROYAL ORPHANAGE
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task RoyalOrphanage()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                          THE ROYAL ORPHANAGE                                ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("You visit the children under royal protection.");
        terminal.WriteLine($"Daily care cost: {GameConfig.OrphanCareCost} gold per child");
        terminal.WriteLine("");

        if (currentKing.Orphans.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("The orphanage is empty.");
        }
        else
        {
            terminal.SetColor("cyan");
            terminal.WriteLine($"{"Name",-20} {"Age",-6} {"Happiness",-12}");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 40));

            foreach (var orphan in currentKing.Orphans)
            {
                string happyColor = orphan.Happiness > 70 ? "bright_green" :
                                   orphan.Happiness > 40 ? "yellow" : "red";

                terminal.SetColor("white");
                terminal.Write($"{orphan.Name,-20} {orphan.Age,-6} ");
                terminal.SetColor(happyColor);
                terminal.WriteLine($"{orphan.Happiness}%");
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("Commands:");
        terminal.SetColor("white");
        terminal.WriteLine("(A)dopt new orphan   (G)ive gifts (increase happiness)   (R)eturn");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.Write("Action: ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (!string.IsNullOrEmpty(input))
        {
            switch (char.ToUpper(input[0]))
            {
                case 'A':
                    await AdoptOrphan();
                    break;
                case 'G':
                    await GiveGiftsToOrphans();
                    break;
            }
        }
    }

    private async Task AdoptOrphan()
    {
        string[] names = { "Tommy", "Sarah", "Billy", "Emma", "Jack", "Lily", "Oliver", "Sophie" };
        string name = names[random.Next(names.Length)];

        var orphan = new RoyalOrphan
        {
            Name = name,
            Age = random.Next(5, 15),
            Sex = random.Next(2) == 0 ? CharacterSex.Male : CharacterSex.Female,
            Happiness = 50,
            BackgroundStory = "Found wandering the streets."
        };

        currentKing.Orphans.Add(orphan);

        terminal.SetColor("bright_green");
        terminal.WriteLine($"{name}, age {orphan.Age}, has been taken into the Royal Orphanage.");
        terminal.WriteLine("Your compassion increases your standing with the people!");

        currentPlayer.Chivalry += 15;

        await Task.Delay(2500);
    }

    private async Task GiveGiftsToOrphans()
    {
        if (currentKing.Orphans.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("There are no orphans to gift.");
            await Task.Delay(1500);
            return;
        }

        terminal.SetColor("cyan");
        terminal.Write("Gift amount (gold): ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (long.TryParse(input, out long amount) && amount > 0)
        {
            if (amount > currentKing.Treasury)
            {
                terminal.SetColor("red");
                terminal.WriteLine("Insufficient funds in treasury!");
            }
            else
            {
                currentKing.Treasury -= amount;
                int happinessBoost = (int)Math.Min(30, amount / 100);

                foreach (var orphan in currentKing.Orphans)
                {
                    orphan.Happiness = Math.Min(100, orphan.Happiness + happinessBoost);
                }

                terminal.SetColor("bright_green");
                terminal.WriteLine("The children are delighted with your generosity!");
                terminal.WriteLine($"Orphan happiness increased by {happinessBoost}%!");

                currentPlayer.Chivalry += (int)(amount / 200);
            }
        }

        await Task.Delay(2500);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ROYAL QUESTS
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task RoyalQuests()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_red");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                         ROYAL BOUNTY BOARD                                  ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("The King's personal bounty board displays wanted criminals and threats.");
        terminal.WriteLine("");

        // Get bounties posted by the King
        var bounties = QuestSystem.GetKingBounties();

        if (bounties.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("No bounties have been posted by the Crown.");
            terminal.WriteLine("");
            terminal.SetColor("yellow");
            terminal.WriteLine("The realm is at peace... for now.");
        }
        else
        {
            terminal.SetColor("bright_red");
            terminal.WriteLine("WANTED:");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 60));

            foreach (var bounty in bounties)
            {
                terminal.SetColor("red");
                terminal.Write("  ▪ ");
                terminal.SetColor("white");
                terminal.WriteLine(bounty.Title ?? bounty.Comment ?? "Unknown Target");
                terminal.SetColor("yellow");
                terminal.WriteLine($"    Reward: {bounty.GetRewardDescription()}");
                terminal.SetColor("gray");
                terminal.WriteLine($"    {bounty.Comment}");
                terminal.WriteLine("");
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("Visit the Quest Hall on Main Street to claim bounties.");
        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // THRONE MECHANICS
    // ═══════════════════════════════════════════════════════════════════════════

    private bool CanChallengeThrone()
    {
        if (currentPlayer.Level < GameConfig.MinLevelKing)
            return false;

        if (currentPlayer.King)
            return false;

        if (currentKing == null || !currentKing.IsActive)
            return false;

        return true;
    }

    private async Task<bool> ChallengeThrone()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_red");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                          THRONE CHALLENGE                                   ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // RULE: King cannot be on a team - must leave team first
        if (!string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("To challenge for the throne, you must abandon your team.");
            terminal.WriteLine($"You are currently a member of '{currentPlayer.Team}'.");
            terminal.WriteLine("");
            terminal.SetColor("cyan");
            terminal.Write("Leave your team to pursue the crown? (Y/N): ");
            terminal.SetColor("white");
            string leaveConfirm = await terminal.ReadLineAsync();

            if (leaveConfirm?.ToUpper() != "Y")
            {
                terminal.SetColor("gray");
                terminal.WriteLine("You decide to remain loyal to your team.");
                await Task.Delay(2000);
                return false;
            }

            // Force leave team
            string oldTeam = currentPlayer.Team;
            CityControlSystem.Instance.ForceLeaveTeam(currentPlayer);
            terminal.SetColor("yellow");
            terminal.WriteLine($"You have left '{oldTeam}' to pursue the crown.");
            terminal.WriteLine("");
        }

        terminal.SetColor("white");
        terminal.WriteLine("You have chosen to challenge for the throne!");
        terminal.WriteLine($"You must defeat {currentKing.GetTitle()} {currentKing.Name} in combat.");
        terminal.WriteLine("");

        // Show monster guard warning
        if (currentKing.MonsterGuards.Count > 0)
        {
            terminal.SetColor("bright_red");
            terminal.WriteLine($"WARNING: You must first defeat {currentKing.MonsterGuards.Count} Monster Guards!");
        }

        // Show NPC guard warning
        if (currentKing.Guards.Count > 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"WARNING: You must also defeat {currentKing.Guards.Count} Royal Guards!");
        }

        if (currentKing.TotalGuardCount > 0)
        {
            terminal.WriteLine("");
        }

        terminal.SetColor("cyan");
        terminal.Write("Do you wish to proceed? (Y/N): ");
        terminal.SetColor("white");
        string confirm = await terminal.ReadLineAsync();

        if (confirm?.ToUpper() != "Y")
        {
            terminal.SetColor("gray");
            terminal.WriteLine("You reconsider and step back.");
            await Task.Delay(2000);
            return false;
        }

        long runningPlayerHP = currentPlayer.HP;

        // PHASE 1: Fight through monster guards first
        foreach (var monster in currentKing.MonsterGuards.ToList())
        {
            terminal.ClearScreen();
            terminal.SetColor("bright_red");
            terminal.WriteLine($"=== Fighting Monster Guard: {monster.Name} (Level {monster.Level}) ===");
            terminal.WriteLine("");

            long monsterHP = monster.HP;

            while (monsterHP > 0 && runningPlayerHP > 0)
            {
                // Player attacks
                long playerDamage = Math.Max(1, currentPlayer.Strength + currentPlayer.WeapPow - monster.Defence);
                playerDamage += random.Next(1, (int)Math.Max(2, currentPlayer.WeapPow / 3));
                monsterHP -= playerDamage;

                terminal.SetColor("bright_green");
                terminal.WriteLine($"You strike the {monster.Name} for {playerDamage} damage! (Monster HP: {Math.Max(0, monsterHP)})");

                if (monsterHP <= 0) break;

                // Monster attacks
                long monsterDamage = Math.Max(1, monster.Strength + monster.WeapPow - currentPlayer.Defence - currentPlayer.ArmPow);
                monsterDamage += random.Next(1, (int)Math.Max(2, monster.WeapPow / 3));
                runningPlayerHP -= monsterDamage;

                terminal.SetColor("red");
                terminal.WriteLine($"The {monster.Name} claws you for {monsterDamage} damage! (Your HP: {Math.Max(0, runningPlayerHP)})");

                await Task.Delay(300);
            }

            if (runningPlayerHP <= 0)
            {
                terminal.SetColor("red");
                terminal.WriteLine($"You were slain by the {monster.Name}!");
                currentPlayer.HP = 1;
                terminal.WriteLine("Your challenge has failed. You barely escape with your life.");
                await Task.Delay(2500);
                return false;
            }
            else
            {
                terminal.SetColor("bright_green");
                terminal.WriteLine($"You defeated the {monster.Name}!");
                currentKing.MonsterGuards.Remove(monster);
                NewsSystem.Instance?.Newsy(true, $"{currentPlayer.DisplayName} slew the monster guard {monster.Name}!");
            }

            await Task.Delay(1500);
        }

        // PHASE 2: Fight through NPC guards
        foreach (var guard in currentKing.Guards.ToList())
        {
            terminal.ClearScreen();
            terminal.SetColor("bright_red");
            terminal.WriteLine($"=== Fighting Royal Guard: {guard.Name} ===");
            terminal.WriteLine("");

            // Simple guard combat
            int guardStr = 50 + random.Next(50);
            int guardHP = 200 + random.Next(200);

            while (guardHP > 0 && runningPlayerHP > 0)
            {
                // Player attacks
                long playerDamage = Math.Max(1, currentPlayer.Strength + currentPlayer.WeapPow - guardStr / 5);
                guardHP -= (int)playerDamage;

                terminal.SetColor("bright_green");
                terminal.WriteLine($"You strike {guard.Name} for {playerDamage} damage! (Guard HP: {Math.Max(0, guardHP)})");

                if (guardHP <= 0) break;

                // Guard attacks
                long guardDamage = Math.Max(1, guardStr - currentPlayer.Defence);
                runningPlayerHP -= guardDamage;

                terminal.SetColor("red");
                terminal.WriteLine($"{guard.Name} strikes you for {guardDamage} damage! (Your HP: {Math.Max(0, runningPlayerHP)})");

                await Task.Delay(300);
            }

            if (runningPlayerHP <= 0)
            {
                terminal.SetColor("red");
                terminal.WriteLine($"You were defeated by {guard.Name}!");
                currentPlayer.HP = 1;
                terminal.WriteLine("Your challenge has failed. You barely escape with your life.");
                await Task.Delay(2500);
                return false;
            }
            else
            {
                terminal.SetColor("bright_green");
                terminal.WriteLine($"You defeated {guard.Name}!");
                currentKing.Guards.Remove(guard);
                NewsSystem.Instance?.Newsy(true, $"{currentPlayer.DisplayName} defeated guard {guard.Name}!");
            }

            await Task.Delay(1500);
        }

        // Update player HP from the guard battles
        currentPlayer.HP = runningPlayerHP;

        // Fight the king (simulated as powerful NPC)
        terminal.ClearScreen();
        terminal.SetColor("bright_red");
        terminal.WriteLine($"=== Final Battle: {currentKing.GetTitle()} {currentKing.Name} ===");
        terminal.WriteLine("");

        int kingStr = 100 + random.Next(100);
        int kingHP = 500 + random.Next(500);
        long finalPlayerHP = currentPlayer.HP;

        int round = 0;
        while (kingHP > 0 && finalPlayerHP > 0)
        {
            round++;
            terminal.SetColor("cyan");
            terminal.WriteLine($"--- Round {round} ---");

            // Player attacks
            long playerDmg = Math.Max(1, currentPlayer.Strength + currentPlayer.WeapPow - kingStr / 4);
            playerDmg += random.Next((int)currentPlayer.WeapPow / 2);
            kingHP -= (int)playerDmg;

            terminal.SetColor("bright_green");
            terminal.WriteLine($"You strike for {playerDmg} damage! (King HP: {Math.Max(0, kingHP)})");

            if (kingHP <= 0) break;

            // King attacks
            long kingDmg = Math.Max(1, kingStr - currentPlayer.Defence);
            kingDmg += random.Next(20);
            finalPlayerHP -= kingDmg;

            terminal.SetColor("red");
            terminal.WriteLine($"{currentKing.Name} strikes for {kingDmg} damage! (Your HP: {Math.Max(0, finalPlayerHP)})");

            await Task.Delay(500);
        }

        currentPlayer.HP = Math.Max(1, finalPlayerHP);

        if (kingHP <= 0)
        {
            // Player wins!
            terminal.WriteLine("");
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("════════════════════════════════════════════════");
            terminal.WriteLine("              VICTORY!");
            terminal.WriteLine("════════════════════════════════════════════════");
            terminal.WriteLine($"You have defeated {currentKing.GetTitle()} {currentKing.Name}!");
            terminal.WriteLine("The throne is yours!");

            // Record old monarch
            monarchHistory.Add(new MonarchRecord
            {
                Name = currentKing.Name,
                Title = currentKing.GetTitle(),
                DaysReigned = (int)currentKing.TotalReign,
                CoronationDate = currentKing.CoronationDate,
                EndReason = $"Defeated by {currentPlayer.DisplayName}"
            });

            // Crown new monarch
            currentPlayer.King = true;
            currentKing = King.CreateNewKing(currentPlayer.DisplayName, CharacterAI.Human, currentPlayer.Sex);
            playerIsKing = true;

            currentPlayer.PKills++;

            // Track archetype - Major Ruler moment
            UsurperRemake.Systems.ArchetypeTracker.Instance.RecordBecameKing();

            NewsSystem.Instance.Newsy(true, $"{currentPlayer.DisplayName} has seized the throne! Long live the new {currentKing.GetTitle()}!");

            await Task.Delay(4000);
            return false; // Stay in castle as new king
        }
        else
        {
            // Player lost
            terminal.WriteLine("");
            terminal.SetColor("red");
            terminal.WriteLine("You have been defeated!");
            terminal.WriteLine("The guards drag you from the castle...");

            await Task.Delay(3000);
            return true; // Exit castle
        }
    }

    private async Task<bool> ClaimEmptyThrone()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                         CLAIMING THE THRONE                                 ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Check level requirement
        if (currentPlayer.Level < GameConfig.MinLevelKing)
        {
            terminal.SetColor("red");
            terminal.WriteLine($"You must be at least level {GameConfig.MinLevelKing} to claim the throne!");
            terminal.WriteLine($"Your current level: {currentPlayer.Level}");
            await Task.Delay(2500);
            return false;
        }

        terminal.SetColor("white");
        terminal.WriteLine("The Castle seems to be in disarray!");
        terminal.WriteLine("No King or Queen is to be found anywhere. People are just");
        terminal.WriteLine("running around in a disorganized manner.");
        terminal.WriteLine("");

        // RULE: King cannot be on a team - must leave team first
        if (!string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("To claim the throne, you must abandon your team.");
            terminal.WriteLine($"You are currently a member of '{currentPlayer.Team}'.");
            terminal.WriteLine("");
            terminal.SetColor("cyan");
            terminal.Write("Leave your team to claim the crown? (Y/N): ");
            terminal.SetColor("white");
            string leaveConfirm = await terminal.ReadLineAsync();

            if (leaveConfirm?.ToUpper() != "Y")
            {
                terminal.SetColor("gray");
                terminal.WriteLine("You decide to remain loyal to your team.");
                await Task.Delay(2000);
                return false;
            }

            // Force leave team
            string oldTeam = currentPlayer.Team;
            CityControlSystem.Instance.ForceLeaveTeam(currentPlayer);
            terminal.SetColor("yellow");
            terminal.WriteLine($"You have left '{oldTeam}' to claim the crown.");
            terminal.WriteLine("");
        }

        var title = currentPlayer.Sex == CharacterSex.Male ? "KING" : "QUEEN";
        terminal.SetColor("cyan");
        terminal.Write($"Proclaim yourself {title}? (Y/N): ");
        terminal.SetColor("white");
        string confirm = await terminal.ReadLineAsync();

        if (confirm?.ToUpper() != "Y")
        {
            terminal.SetColor("gray");
            terminal.WriteLine("You decide not to claim the throne.");
            await Task.Delay(2000);
            return false;
        }

        // Crown the new monarch
        currentPlayer.King = true;
        currentKing = King.CreateNewKing(currentPlayer.DisplayName, CharacterAI.Human, currentPlayer.Sex);
        playerIsKing = true;

        // Track archetype - Major Ruler moment
        UsurperRemake.Systems.ArchetypeTracker.Instance.RecordBecameKing();

        terminal.WriteLine("");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("        Congratulations, The Castle is Yours!");
        terminal.WriteLine($"  The InterRegnum is over, long live the {title}!");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        NewsSystem.Instance.Newsy(true, $"{currentPlayer.DisplayName} has claimed the empty throne! Long live the {title}!");

        await Task.Delay(4000);
        return false; // Stay in castle as new king
    }

    private async Task<bool> AttemptAbdication()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_red");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                            ABDICATION                                       ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("Are you sure you want to abdicate the throne?");
        terminal.WriteLine("This action cannot be undone!");
        terminal.WriteLine("");
        terminal.SetColor("yellow");
        terminal.WriteLine("You will lose all royal privileges and the kingdom");
        terminal.WriteLine("will be thrown into chaos until a new ruler emerges.");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.Write("Abdicate the throne? (type 'yes' to confirm): ");
        terminal.SetColor("white");
        string confirm = await terminal.ReadLineAsync();

        if (confirm?.ToLower() == "yes")
        {
            // Record old monarch
            monarchHistory.Add(new MonarchRecord
            {
                Name = currentKing.Name,
                Title = currentKing.GetTitle(),
                DaysReigned = (int)currentKing.TotalReign,
                CoronationDate = currentKing.CoronationDate,
                EndReason = "Abdicated"
            });

            terminal.WriteLine("");
            terminal.SetColor("gray");
            terminal.WriteLine("You pack your few personal belongings and leave your Crown");
            terminal.WriteLine("to the royal treasurer. You dress yourself in simple clothes");
            terminal.WriteLine("and walk out from the Castle, never to return (?).");
            terminal.WriteLine("");

            currentPlayer.King = false;
            currentKing.IsActive = false;
            currentKing = null;
            playerIsKing = false;

            terminal.SetColor("bright_yellow");
            terminal.WriteLine($"The {(currentPlayer.Sex == CharacterSex.Male ? "King" : "Queen")} has ABDICATED!");
            terminal.SetColor("red");
            terminal.WriteLine("The land is in disarray! Who will claim the Throne?");

            NewsSystem.Instance.Newsy(true, $"{currentPlayer.DisplayName} has abdicated the throne! The kingdom is in chaos!");

            await Task.Delay(4000);
            return true; // Exit castle
        }
        else
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine("Phew! The kingdom breathes a sigh of relief.");
            await Task.Delay(2000);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DONATION
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task SeekAudience()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                           SEEK AN AUDIENCE                                  ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        if (currentKing == null || !currentKing.IsActive)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("With no monarch on the throne, there is no one to grant you an audience.");
            terminal.WriteLine("The throne room stands empty and echoing...");
            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        // Display monarch info
        terminal.SetColor("bright_yellow");
        terminal.WriteLine($"{currentKing.GetTitle()} {currentKing.Name} sits upon the throne.");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine($"Reign: {currentKing.TotalReign} days");
        terminal.WriteLine($"Royal Treasury: {currentKing.Treasury:N0} gold");
        terminal.WriteLine($"Royal Guards: {currentKing.Guards.Count}");
        terminal.WriteLine("");

        // Check if player has good standing
        long chivalry = currentPlayer.Chivalry;
        long fame = currentPlayer.Fame;
        long reputation = (chivalry + fame) / 2;

        if (reputation < 50)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("The guards look at you skeptically.");
            terminal.WriteLine($"\"Your reputation precedes you, {currentPlayer.DisplayName}. The {currentKing.GetTitle()}");
            terminal.WriteLine("is not receiving visitors of... your standing today.\"");
            terminal.WriteLine("");
            terminal.SetColor("gray");
            terminal.WriteLine("(Increase your Chivalry and Fame to gain an audience)");
        }
        else if (reputation < 100)
        {
            terminal.SetColor("cyan");
            terminal.WriteLine("The royal herald announces your presence.");
            terminal.WriteLine($"\"The {currentKing.GetTitle()} acknowledges your service to the realm.\"");
            terminal.WriteLine("");
            terminal.SetColor("white");
            terminal.WriteLine($"{currentKing.GetTitle()} {currentKing.Name} nods in your direction.");
            terminal.WriteLine("\"Continue your good works, and perhaps we shall speak in time.\"");
        }
        else
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine("The guards bow respectfully as you approach the throne.");
            terminal.WriteLine($"\"Welcome, honored {currentPlayer.DisplayName}!\"");
            terminal.WriteLine("");
            terminal.SetColor("bright_yellow");
            terminal.WriteLine($"{currentKing.GetTitle()} {currentKing.Name} rises to greet you warmly.");
            terminal.WriteLine("\"A hero of the realm! Your deeds have not gone unnoticed.\"");
            terminal.WriteLine("");

            // High reputation bonus
            if (new Random().NextDouble() < 0.3)
            {
                int goldGift = new Random().Next(100, 500) * currentPlayer.Level;
                currentPlayer.Gold += goldGift;
                terminal.SetColor("bright_cyan");
                terminal.WriteLine($"The {currentKing.GetTitle()} bestows a gift of {goldGift:N0} gold upon you!");
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    private async Task DonateToRoyalPurse()
    {
        if (currentKing == null || !currentKing.IsActive)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("There is no monarch to receive your donation!");
            await Task.Delay(2000);
            return;
        }

        terminal.SetColor("white");
        terminal.WriteLine($"The Royal Purse currently contains {currentKing.Treasury:N0} gold.");
        terminal.WriteLine($"You have {currentPlayer.Gold:N0} gold.");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.Write("How much gold do you wish to donate? ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (long.TryParse(input, out long amount))
        {
            if (amount <= 0)
            {
                terminal.SetColor("gray");
                terminal.WriteLine("Donation cancelled.");
            }
            else if (amount > currentPlayer.Gold)
            {
                terminal.SetColor("red");
                terminal.WriteLine("You don't have that much gold!");
            }
            else
            {
                currentPlayer.Gold -= amount;
                currentKing.Treasury += amount;
                int chivalryGain = (int)Math.Min(50, amount / 100);
                currentPlayer.Chivalry += chivalryGain;

                terminal.SetColor("bright_green");
                terminal.WriteLine($"You donate {amount:N0} gold to the Royal Purse.");
                terminal.WriteLine($"Your chivalry increases by {chivalryGain} for this noble deed!");
            }
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("Invalid amount.");
        }

        await Task.Delay(2500);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UTILITY
    // ═══════════════════════════════════════════════════════════════════════════

    private void LoadKingData()
    {
        // If player is king but no king data exists, create it
        if (currentKing == null && playerIsKing)
        {
            currentKing = King.CreateNewKing(currentPlayer.DisplayName, CharacterAI.Human, currentPlayer.Sex);
            return;
        }

        // If no king exists at all, trigger NPC succession
        if (currentKing == null || !currentKing.IsActive)
        {
            TriggerNPCSuccession();
        }
    }

    /// <summary>
    /// When throne is empty, find the strongest/most worthy NPC to claim it
    /// </summary>
    private void TriggerNPCSuccession()
    {
        var npcs = NPCSpawnSystem.Instance.ActiveNPCs;
        if (npcs == null || npcs.Count == 0)
        {
            GD.Print("[Castle] No NPCs available for succession - throne remains empty");
            return;
        }

        // Find the most worthy NPC based on level, alignment (good preferred), and class
        var candidates = npcs
            .Where(npc => npc.IsAlive && npc.Level >= 10) // Minimum level 10 to be king
            .OrderByDescending(npc => CalculateSuccessionScore(npc))
            .ToList();

        if (candidates.Count == 0)
        {
            GD.Print("[Castle] No worthy NPCs found for succession - throne remains empty");
            return;
        }

        // Crown the highest scoring NPC
        var newMonarch = candidates.First();
        CrownNPC(newMonarch);

        // Announce succession
        NewsSystem.Instance.Newsy(true, $"{newMonarch.DisplayName} has claimed the throne and been crowned {(newMonarch.Sex == CharacterSex.Male ? "King" : "Queen")}!");
    }

    /// <summary>
    /// Calculate an NPC's worthiness to become monarch
    /// </summary>
    private int CalculateSuccessionScore(NPC npc)
    {
        int score = npc.Level * 10;

        // Paladins and Clerics are more worthy
        if (npc.Class == CharacterClass.Paladin) score += 50;
        if (npc.Class == CharacterClass.Cleric) score += 30;
        if (npc.Class == CharacterClass.Warrior) score += 20;

        // Higher charisma = better leader
        score += (int)(npc.Charisma / 2);

        // Good alignment preferred (Chivalry - Darkness)
        long alignment = npc.Chivalry - npc.Darkness;
        if (alignment > 0) score += (int)Math.Min(alignment, 100);

        // Wealth helps
        score += (int)(npc.Gold / 10000);

        return score;
    }

    /// <summary>
    /// Crown an NPC as the new monarch
    /// </summary>
    private void CrownNPC(NPC npc)
    {
        currentKing = new King
        {
            Name = npc.DisplayName,
            AI = CharacterAI.Computer,
            Sex = npc.Sex,
            IsActive = true,
            CoronationDate = DateTime.Now,
            Treasury = Math.Max(10000, npc.Gold / 2), // NPC donates half their gold to treasury
            TotalReign = 0
        };

        // Record the coronation
        monarchHistory.Add(new MonarchRecord
        {
            Name = npc.DisplayName,
            Title = npc.Sex == CharacterSex.Male ? "King" : "Queen",
            CoronationDate = DateTime.Now,
            DaysReigned = 0,
            EndReason = ""
        });

        GD.Print($"[Castle] {npc.DisplayName} has been crowned {(npc.Sex == CharacterSex.Male ? "King" : "Queen")}!");
    }

    /// <summary>
    /// Get the current king (for external access)
    /// </summary>
    public static King GetCurrentKing() => currentKing;

    /// <summary>
    /// Set the king (for save/load)
    /// </summary>
    public static void SetKing(King king) => currentKing = king;

    /// <summary>
    /// Set an NPC as the current king (for save restoration)
    /// </summary>
    public static void SetCurrentKing(NPC npc)
    {
        if (npc == null)
        {
            currentKing = null;
            return;
        }

        currentKing = new King
        {
            Name = npc.Name,
            AI = CharacterAI.Computer,
            Sex = npc.Sex,
            IsActive = true,
            CoronationDate = DateTime.Now
        };

        GD.Print($"[Castle] {npc.Name} has been restored as monarch");
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// SUPPORTING CLASSES
// ═══════════════════════════════════════════════════════════════════════════

public class RoyalMailMessage
{
    public string Sender { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsRead { get; set; } = false;
    public DateTime Date { get; set; } = DateTime.Now;
}

public class MonarchRecord
{
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public int DaysReigned { get; set; } = 0;
    public DateTime CoronationDate { get; set; } = DateTime.Now;
    public string EndReason { get; set; } = "";
}
