using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Castle Location - Pascal-compatible royal court system
/// Based on CASTLE.PAS with complete King and commoner interfaces
/// </summary>
public class CastleLocation : BaseLocation
{
    private King currentKing = null;
    private bool playerIsKing = false;
    
    public CastleLocation() : base(
        GameLocation.Castle,
        "The Royal Castle",
        "You approach the magnificent royal castle, seat of power in the kingdom."
    ) { }
    
    protected override void SetupLocation()
    {
        base.SetupLocation();
        
        var currentPlayer = GetCurrentPlayer();
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
        // ASCII art header
        terminal.SetColor("yellow");
        terminal.WriteLine("╔══════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                 THE ROYAL CASTLE                    ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════╝");
        terminal.WriteLine("");
        
        // Royal greeting
        terminal.SetColor("cyan");
        terminal.WriteLine("You have entered the Great Hall. Upon your arrival you are");
        terminal.WriteLine("immediately surrounded by a flock of servants and advisors.");
        terminal.WriteLine("You greet your staff with a subtle nod.");
        terminal.WriteLine("");
        
        // Royal treasury status
        terminal.SetColor("green");
        terminal.Write("The Royal Purse");
        terminal.SetColor("white");
        terminal.Write(" has ");
        terminal.SetColor("yellow");
        terminal.Write($"{currentKing.Treasury:N0}");
        terminal.SetColor("white");
        terminal.WriteLine(" gold pieces.");
        terminal.WriteLine("");
        
        ShowRoyalMenu();
    }
    
    /// <summary>
    /// Display castle exterior for non-monarchs
    /// </summary>
    private void DisplayCastleExterior()
    {
        // ASCII art header
        terminal.SetColor("red");
        terminal.WriteLine("╔══════════════════════════════════════════════════════╗");
        terminal.WriteLine("║              OUTSIDE THE ROYAL CASTLE               ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════╝");
        terminal.WriteLine("");
        
        // Approach description
        terminal.SetColor("gray");
        terminal.WriteLine("You journey the winding road up to the gates of the Castle.");
        terminal.WriteLine("Massive stone walls tower above you, and royal guards");
        terminal.WriteLine("watch your approach with keen interest.");
        terminal.WriteLine("");
        
        // King status
        if (currentKing != null && currentKing.IsActive)
        {
            terminal.SetColor("white");
            terminal.Write($"The mighty {currentKing.GetTitle()} ");
            terminal.SetColor("cyan");
            terminal.Write(currentKing.Name);
            terminal.SetColor("white");
            terminal.WriteLine(" rules from within these walls.");
            terminal.WriteLine("");
        }
        else
        {
            terminal.SetColor("red");
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
        terminal.SetColor("white");
        terminal.WriteLine("(P)rison Cells      (O)rders            (1) Royal Mail");
        terminal.WriteLine("(G)o to Sleep       (C)heck Security    (H)istory of Monarchs");
        terminal.WriteLine("(A)bdicate          (M)agic             (F)iscal Matters");
        terminal.WriteLine("(S)tatus            (Q)uests            (T)he Royal Orphanage");
        terminal.WriteLine("(R)eturn to Town");
        terminal.WriteLine("");
    }
    
    /// <summary>
    /// Show castle exterior menu for commoners
    /// </summary>
    private void ShowCastleExteriorMenu()
    {
        terminal.SetColor("white");
        terminal.WriteLine("(T)he Royal Guard   (P)rison            (D)onate to Royal Purse");
        
        if (CanChallengeThrone())
        {
            terminal.WriteLine("(I)nfiltrate Castle (Challenge for Throne)");
        }
        else if (currentKing == null || !currentKing.IsActive)
        {
            terminal.WriteLine("(C)laim Empty Throne");
        }
        
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
                terminal.WriteLine("Invalid choice! Try again.", "red");
                await Task.Delay(1500);
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
                    terminal.WriteLine("You are not worthy to challenge for the throne!", "red");
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
                    terminal.WriteLine("The throne is already occupied!", "red");
                    await Task.Delay(2000);
                }
                return false;
                
            case "R":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;
                
            default:
                terminal.WriteLine("Invalid choice! Try again.", "red");
                await Task.Delay(1500);
                return false;
        }
    }
    
    /// <summary>
    /// Royal Orders - Pascal royal office system
    /// </summary>
    private async Task RoyalOrders()
    {
        terminal.ClearScreen();
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ ROYAL OFFICE ═══");
        terminal.WriteLine("");
        
        terminal.SetColor("white");
        terminal.Write("You enter the ");
        terminal.SetColor("green");
        terminal.Write("Royal Office");
        terminal.SetColor("white");
        terminal.WriteLine(".");
        terminal.WriteLine("");
        
        // Show treasury status
        terminal.SetColor("cyan");
        terminal.Write("Issue Orders, The Royal Purse");
        if (currentKing.Treasury > 0)
        {
            terminal.SetColor("white");
            terminal.Write(" has ");
            terminal.SetColor("yellow");
            terminal.Write($"{currentKing.Treasury:N0}");
            terminal.SetColor("white");
            terminal.WriteLine(" gold pieces.");
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine(" is empty!");
        }
        terminal.WriteLine("");
        
        bool done = false;
        while (!done)
        {
            // Orders menu
            terminal.SetColor("white");
            terminal.WriteLine("(E)stablishments    (P)roclamation     (M)atrimonial Decisions");
            terminal.WriteLine("(L)evel Masters     (R)eturn");
            terminal.WriteLine("");
            
            var orderChoice = await terminal.GetInput("Orders: ");
            
            switch (orderChoice.ToUpper())
            {
                case "E":
                    await ManageEstablishments();
                    break;
                case "P":
                    await RoyalProclamation();
                    break;
                case "M":
                    await MatrimonialDecisions();
                    break;
                case "L":
                    await ManageLevelMasters();
                    break;
                case "R":
                    done = true;
                    break;
                default:
                    terminal.WriteLine("Invalid choice!", "red");
                    await Task.Delay(1000);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Fiscal matters - tax and treasury management
    /// </summary>
    private async Task FiscalMatters()
    {
        terminal.ClearScreen();
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ FISCAL MATTERS ═══");
        terminal.WriteLine("");
        
        terminal.SetColor("white");
        terminal.WriteLine($"Royal Treasury: {currentKing.Treasury:N0} gold");
        terminal.WriteLine($"Current Tax Rate: {currentKing.TaxRate} gold per citizen");
        terminal.WriteLine($"Tax Alignment: {currentKing.TaxAlignment}");
        terminal.WriteLine("");
        
        terminal.WriteLine("Daily Income: " + currentKing.CalculateDailyIncome());
        terminal.WriteLine("Daily Expenses: " + currentKing.CalculateDailyExpenses());
        terminal.WriteLine("");
        
        bool done = false;
        while (!done)
        {
            terminal.WriteLine("(T)ax Policy        (B)udget Review     (G)uard Salaries");
            terminal.WriteLine("(D)onations         (R)eturn");
            terminal.WriteLine("");
            
            var choice = await terminal.GetInput("Fiscal Decision: ");
            
            switch (choice.ToUpper())
            {
                case "T":
                    await SetTaxPolicy();
                    break;
                case "B":
                    await ReviewBudget();
                    break;
                case "G":
                    await ManageGuardSalaries();
                    break;
                case "D":
                    await ViewDonations();
                    break;
                case "R":
                    done = true;
                    break;
                default:
                    terminal.WriteLine("Invalid choice!", "red");
                    await Task.Delay(1000);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Abdication process - Pascal abdication mechanics
    /// </summary>
    private async Task<bool> AttemptAbdication()
    {
        terminal.ClearScreen();
        terminal.SetColor("red");
        terminal.WriteLine("═══ ABDICATION ═══");
        terminal.WriteLine("");
        
        terminal.SetColor("white");
        terminal.WriteLine("Are you sure you want to abdicate the throne?");
        terminal.WriteLine("This action cannot be undone!");
        terminal.WriteLine("");
        terminal.WriteLine("You will lose all royal privileges and the kingdom");
        terminal.WriteLine("will be thrown into chaos until a new ruler emerges.");
        terminal.WriteLine("");
        
        var confirm = await terminal.GetInput("Abdicate the throne? (yes/no): ");
        
        if (confirm.ToLower() == "yes")
        {
            await ProcessAbdication();
            return true; // Exit castle
        }
        else
        {
            terminal.WriteLine("Phew! Thank you!", "green");
            await Task.Delay(2000);
            return false;
        }
    }
    
    /// <summary>
    /// Process the abdication
    /// </summary>
    private async Task ProcessAbdication()
    {
        terminal.WriteLine("");
        terminal.SetColor("red");
        terminal.WriteLine("You pack your few personal belongings and leave your Crown");
        terminal.WriteLine("to the royal treasurer. You dress yourself in simple clothes");
        terminal.WriteLine("and walk out from the Castle, never to return (?).");
        terminal.WriteLine("");
        
        var currentPlayer = GetCurrentPlayer();
        currentPlayer.King = false;
        
        // Remove king from power
        currentKing.IsActive = false;
        currentKing = null;
        
        terminal.SetColor("yellow");
        terminal.WriteLine($"The {(currentPlayer.Sex == CharacterSex.Male ? "King" : "Queen")} has ABDICATED!");
        terminal.WriteLine("The land is in disarray! Who will claim the Throne?");
        
        await Task.Delay(3000);
    }
    
    /// <summary>
    /// Check if player can challenge the throne
    /// </summary>
    private bool CanChallengeThrone()
    {
        var currentPlayer = GetCurrentPlayer();
        
        if (currentPlayer.Level < GameConfig.MinLevelKing)
            return false;
            
        if (currentPlayer.King)
            return false;
            
        if (!string.IsNullOrEmpty(currentPlayer.Team))
            return false;
            
        return true;
    }
    
    /// <summary>
    /// Challenge the current monarch for the throne
    /// </summary>
    private async Task<bool> ChallengeThrone()
    {
        terminal.ClearScreen();
        terminal.SetColor("red");
        terminal.WriteLine("═══ THRONE CHALLENGE ═══");
        terminal.WriteLine("");
        
        terminal.WriteLine("You have chosen to challenge for the throne!");
        terminal.WriteLine("This will result in mortal combat with the current ruler.");
        terminal.WriteLine("");
        
        var confirm = await terminal.GetInput("Do you wish to proceed? (Y/N): ");
        
        if (confirm.ToUpper() != "Y")
        {
            terminal.WriteLine("You reconsider and step back.", "gray");
            await Task.Delay(2000);
            return false;
        }
        
        // TODO: Implement throne challenge combat
        terminal.WriteLine("Throne challenge combat not yet implemented.", "yellow");
        await Task.Delay(2000);
        return false;
    }
    
    /// <summary>
    /// Claim an empty throne
    /// </summary>
    private async Task<bool> ClaimEmptyThrone()
    {
        terminal.ClearScreen();
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ CLAIMING THE THRONE ═══");
        terminal.WriteLine("");
        
        var currentPlayer = GetCurrentPlayer();
        
        terminal.WriteLine("The Castle seems to be in disarray!");
        terminal.WriteLine("No King or Queen is to be found anywhere. People are just");
        terminal.WriteLine("running around in a disorganized manner.");
        terminal.WriteLine("");
        
        var title = currentPlayer.Sex == CharacterSex.Male ? "KING" : "QUEEN";
        var confirm = await terminal.GetInput($"Proclaim yourself {title}? (Y/N): ");
        
        if (confirm.ToUpper() != "Y")
        {
            terminal.WriteLine("You decide not to claim the throne.", "gray");
            await Task.Delay(2000);
            return false;
        }
        
        // Crown the new monarch
        currentPlayer.King = true;
        currentKing = King.CreateNewKing(currentPlayer.DisplayName, CharacterAI.Human, currentPlayer.Sex);
        playerIsKing = true;
        
        terminal.WriteLine("");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("Congratulations, The Castle is Yours!");
        terminal.WriteLine($"The InterRegnum is over, long live the {title}!");
        terminal.WriteLine("");
        
        await Task.Delay(3000);
        return false; // Stay in castle as new king
    }
    
    /// <summary>
    /// Donate to the royal purse
    /// </summary>
    private async Task DonateToRoyalPurse()
    {
        var currentPlayer = GetCurrentPlayer();
        
        if (currentKing == null || !currentKing.IsActive)
        {
            terminal.WriteLine("There is no monarch to receive your donation!", "red");
            await Task.Delay(2000);
            return;
        }
        
        terminal.WriteLine($"The Royal Purse currently contains {currentKing.Treasury:N0} gold.", "white");
        var input = await terminal.GetInput("How much gold do you wish to donate? ");
        
        if (long.TryParse(input, out long amount))
        {
            if (amount <= 0)
            {
                terminal.WriteLine("Invalid amount!", "red");
            }
            else if (amount > currentPlayer.Gold)
            {
                terminal.WriteLine("You don't have that much gold!", "red");
            }
            else
            {
                currentPlayer.Gold -= amount;
                currentKing.Treasury += amount;
                currentPlayer.Chivalry += (int)(amount / 100); // Gain chivalry for donations
                
                terminal.WriteLine($"You donate {amount} gold to the Royal Purse.", "green");
                terminal.WriteLine("Your chivalry increases for this noble deed!", "bright_green");
            }
        }
        else
        {
            terminal.WriteLine("Invalid amount!", "red");
        }
        
        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Load king data (placeholder for save/load system)
    /// </summary>
    private void LoadKingData()
    {
        // TODO: Implement proper save/load system
        // For now, create a default king if none exists and player is king
        if (currentKing == null && playerIsKing)
        {
            var currentPlayer = GetCurrentPlayer();
            currentKing = King.CreateNewKing(currentPlayer.DisplayName, CharacterAI.Human, currentPlayer.Sex);
        }
    }
    
    // Placeholder methods for royal functions (to be implemented in future phases)
    private async Task ManagePrisonCells()
    {
        terminal.WriteLine("Prison cell management not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task ReadRoyalMail()
    {
        terminal.WriteLine("Royal mail system not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task RoyalSleep()
    {
        terminal.WriteLine("You rest in the royal chambers and recover fully.", "green");
        var player = GetCurrentPlayer();
        player.HP = player.MaxHP;
        await Task.Delay(2000);
    }
    
    private async Task CheckSecurity()
    {
        terminal.WriteLine($"Royal Guard Report: {currentKing.Guards.Count} guards active", "cyan");
        terminal.WriteLine("Security check not yet fully implemented.", "gray");
        await Task.Delay(2000);
    }
    
    private async Task ShowMonarchHistory()
    {
        terminal.WriteLine("History of Monarchs not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task CourtMagician()
    {
        terminal.WriteLine("Court magician services not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task RoyalQuests()
    {
        terminal.WriteLine("Royal quest system not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task RoyalOrphanage()
    {
        terminal.WriteLine($"Royal Orphanage: {currentKing.Orphans.Count} orphans under royal care", "cyan");
        terminal.WriteLine("Orphanage management not yet fully implemented.", "gray");
        await Task.Delay(2000);
    }
    
    private async Task ManageEstablishments()
    {
        terminal.WriteLine("Establishment management not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task RoyalProclamation()
    {
        terminal.WriteLine("Royal proclamation system not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task MatrimonialDecisions()
    {
        terminal.WriteLine("Matrimonial decision system not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task ManageLevelMasters()
    {
        terminal.WriteLine("Level master management not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task SetTaxPolicy()
    {
        terminal.WriteLine("Tax policy management not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task ReviewBudget()
    {
        terminal.WriteLine("Budget review not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task ManageGuardSalaries()
    {
        terminal.WriteLine("Guard salary management not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task ViewDonations()
    {
        terminal.WriteLine("Donation tracking not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task ViewRoyalGuard()
    {
        terminal.WriteLine("Royal Guard viewing not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task VisitPrison()
    {
        terminal.WriteLine("Prison visiting not yet implemented.", "gray");
        await Task.Delay(1500);
    }
} 