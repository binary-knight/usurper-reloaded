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
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                         THE ROYAL CASTLE                                    ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
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
        terminal.SetColor("bright_red");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                       OUTSIDE THE ROYAL CASTLE                              ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
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
        terminal.SetColor("white");
        terminal.WriteLine("(P)rison Cells      (O)rders            (1) Royal Mail");
        terminal.WriteLine("(G)o to Sleep       (C)heck Security    (H)istory of Monarchs");
        terminal.WriteLine("(A)bdicate          (M)agic             (F)iscal Matters");
        terminal.WriteLine("(S)tatus            (Q)uests            (T)he Royal Orphanage");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("(R)eturn to Town");
        terminal.WriteLine("");
    }

    /// <summary>
    /// Show castle exterior menu for commoners
    /// </summary>
    private void ShowCastleExteriorMenu()
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("Options:");
        terminal.SetColor("white");
        terminal.WriteLine("(T)he Royal Guard   (P)rison            (D)onate to Royal Purse");

        if (CanChallengeThrone())
        {
            terminal.SetColor("bright_red");
            terminal.WriteLine("(I)nfiltrate Castle (Challenge for Throne)");
        }
        else if (currentKing == null || !currentKing.IsActive)
        {
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("(C)laim Empty Throne");
        }

        terminal.SetColor("bright_cyan");
        terminal.WriteLine("(R)eturn to Town");
        terminal.WriteLine("");
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
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

        // Guard summary
        terminal.SetColor("cyan");
        terminal.WriteLine("=== Royal Guard Status ===");
        terminal.SetColor("white");
        terminal.WriteLine($"Total Guards: {currentKing.Guards.Count}/{GameConfig.MaxRoyalGuards}");

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
            terminal.SetColor("red");
            terminal.WriteLine("WARNING: No guards protecting the castle!");
        }

        terminal.WriteLine("");

        // Security assessment
        terminal.SetColor("cyan");
        terminal.WriteLine("=== Security Assessment ===");
        int securityLevel = CalculateSecurityLevel();
        string securityColor = securityLevel > 80 ? "bright_green" : securityLevel > 50 ? "yellow" : "red";
        terminal.SetColor(securityColor);
        terminal.WriteLine($"Overall Security: {securityLevel}%");

        if (securityLevel < 50)
        {
            terminal.SetColor("red");
            terminal.WriteLine("Your castle is vulnerable to attack!");
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("Guard Commands:");
        terminal.SetColor("white");
        terminal.WriteLine("(H)ire guard  (F)ire guard  (P)ay bonus  (R)eturn");
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
                case 'F':
                    await FireGuard();
                    break;
                case 'P':
                    await PayGuardBonus();
                    break;
            }
        }
    }

    private int CalculateSecurityLevel()
    {
        if (currentKing.Guards.Count == 0) return 10;

        int baseLevel = (currentKing.Guards.Count * 100) / GameConfig.MaxRoyalGuards;
        int loyaltyBonus = (int)currentKing.Guards.Average(g => g.Loyalty) / 2;

        return Math.Min(100, baseLevel + loyaltyBonus);
    }

    private async Task HireGuard()
    {
        if (currentKing.Guards.Count >= GameConfig.MaxRoyalGuards)
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
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                            ROYAL QUESTS                                     ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("As monarch, you can commission quests for adventurers.");
        terminal.WriteLine("");

        // Show available quests from QuestSystem
        var quests = QuestSystem.GetAvailableQuests(currentPlayer);

        if (quests.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("No active quests at this time.");
        }
        else
        {
            terminal.SetColor("cyan");
            terminal.WriteLine("Active Royal Quests:");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 50));

            foreach (var quest in quests.Take(5))
            {
                terminal.SetColor("white");
                terminal.WriteLine($"  - {quest.Title}");
                terminal.SetColor("gray");
                terminal.WriteLine($"    {quest.GetRewardDescription()}");
            }
        }

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

        terminal.SetColor("white");
        terminal.WriteLine("You have chosen to challenge for the throne!");
        terminal.WriteLine($"You must defeat {currentKing.GetTitle()} {currentKing.Name} in combat.");
        terminal.WriteLine("");

        // Show king's guard warning
        if (currentKing.Guards.Count > 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"WARNING: You must first defeat {currentKing.Guards.Count} Royal Guards!");
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

        // Fight through guards
        bool defeatedGuards = true;
        foreach (var guard in currentKing.Guards.ToList())
        {
            terminal.ClearScreen();
            terminal.SetColor("bright_red");
            terminal.WriteLine($"=== Fighting {guard.Name} ===");
            terminal.WriteLine("");

            // Simple guard combat
            int guardStr = 50 + random.Next(50);
            int guardHP = 200 + random.Next(200);
            long playerHP = currentPlayer.HP;

            while (guardHP > 0 && playerHP > 0)
            {
                // Player attacks
                long playerDamage = Math.Max(1, currentPlayer.Strength + currentPlayer.WeapPow - guardStr / 5);
                guardHP -= (int)playerDamage;

                if (guardHP <= 0) break;

                // Guard attacks
                long guardDamage = Math.Max(1, guardStr - currentPlayer.Defence);
                playerHP -= guardDamage;
            }

            if (playerHP <= 0)
            {
                defeatedGuards = false;
                terminal.SetColor("red");
                terminal.WriteLine($"You were defeated by {guard.Name}!");
                currentPlayer.HP = 1;
                break;
            }
            else
            {
                terminal.SetColor("bright_green");
                terminal.WriteLine($"You defeated {guard.Name}!");
                currentKing.Guards.Remove(guard);
            }

            await Task.Delay(1500);
        }

        if (!defeatedGuards)
        {
            terminal.WriteLine("Your challenge has failed. You barely escape with your life.");
            await Task.Delay(2500);
            return false;
        }

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

        terminal.SetColor("white");
        terminal.WriteLine("The Castle seems to be in disarray!");
        terminal.WriteLine("No King or Queen is to be found anywhere. People are just");
        terminal.WriteLine("running around in a disorganized manner.");
        terminal.WriteLine("");

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
        }
    }

    /// <summary>
    /// Get the current king (for external access)
    /// </summary>
    public static King GetCurrentKing() => currentKing;

    /// <summary>
    /// Set the king (for save/load)
    /// </summary>
    public static void SetKing(King king) => currentKing = king;
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
