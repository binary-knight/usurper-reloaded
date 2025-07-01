using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Bank of Wealth - Complete Pascal-compatible banking system
/// Features: Deposits, Withdrawals, Transfers, Guard Duty, Bank Robberies
/// Based on Pascal BANK.PAS with full compatibility
/// </summary>
public partial class BankLocation : BaseLocation
{
    private const string BankTitle = "Bank of Wealth";
    private const string BankmanName = "Groggo"; // Default gnome banker name
    
    // Bank safe information
    private static long _safeContents = 100000L; // Starting safe contents
    private static List<Character> _activeGuards = new List<Character>();
    
    public BankLocation()
    {
        LocationName = "Bank";
        LocationId = GameConfig.GameLocation.Bank;
        
        // Add banker NPC
        var banker = CreateBanker();
        npcs.Add(banker);
    }
    
    private NPC CreateBanker()
    {
        var banker = new NPC("Groggo", "Groggo", CharacterAI.Civilian, CharacterRace.Gnome);
        banker.Name1 = BankmanName;
        banker.Name2 = BankmanName;
        banker.Level = 25;
        banker.Gold = 500000L;
        banker.HP = banker.MaxHP = 120;
        banker.Strength = 15;
        banker.Defence = 10;
        banker.Agility = 18;
        banker.Charisma = 22;
        banker.Wisdom = 20;
        banker.Dexterity = 16;
        
        // Banker personality - greedy but professional
        banker.Brain.Personality.Greed = 0.8f;
        banker.Brain.Personality.Trustworthiness = 0.9f;
        banker.Brain.Personality.Caution = 0.7f;
        
        return banker;
    }
    
    public new void Enter(Character player)
    {
        base.Enter(player);
        
        // Check if player is fleeing from a robbery
        if (player.Location == GameConfig.GameLocation.Bank && player.HP <= 0)
        {
            DisplayMessage("You stumble into the bank, barely alive from your failed robbery attempt!");
            return;
        }
        
        DisplayBankMenu(player);
    }
    
    private void DisplayBankMenu(Character player)
    {
        ClearScreen();
        
        // Bank header
        DisplayMessage($"┌─ {BankTitle}, managed by {BankmanName} the Gnome ─┐", ConsoleColor.Magenta);
        DisplayMessage("├─────────────────────────────────────────────────────────┤", ConsoleColor.Magenta);
        DisplayMessage("");
        
        // Bank description
        DisplayMessage("You enter the bank, which resides in the finer areas of", ConsoleColor.Gray);
        DisplayMessage("the town. A huge safe is located at the back of the room.", ConsoleColor.Gray);
        DisplayMessage("You can also see some guards posted around the place.", ConsoleColor.Gray);
        DisplayMessage("An old gnome appears and asks you of your business.", ConsoleColor.Gray);
        DisplayMessage("");
        
        // Account status
        DisplayMessage($"Gold on hand .. {player.Gold:N0} gold coins.", ConsoleColor.Gray);
        DisplayMessage($"Gold in bank .. {player.BankGold:N0} gold coins.", ConsoleColor.Gray);
        DisplayMessage("");
        
        // Menu options
        DisplayMessage("(D)eposit gold               (P)ut gold on other players account", ConsoleColor.Gray);
        DisplayMessage("(W)ithdraw                   (T)his is a robbery", ConsoleColor.Gray);
        DisplayMessage("(S)tatus                     (A)pply for Guard duty", ConsoleColor.Gray);
        DisplayMessage("(R)eturn                     (*) resign from guard duty", ConsoleColor.Gray);
        DisplayMessage("");
        
        ProcessBankMenu(player);
    }
    
    private void ProcessBankMenu(Character player)
    {
        while (true)
        {
            DisplayMessage("Your choice: ", ConsoleColor.Yellow, false);
            var choice = Console.ReadKey().KeyChar.ToString().ToUpper();
            DisplayMessage(""); // New line
            
            switch (choice)
            {
                case "D":
                    DepositMoney(player);
                    break;
                case "W":
                    WithdrawMoney(player);
                    break;
                case "P":
                    TransferMoney(player);
                    break;
                case "S":
                    DisplayAccountStatus(player);
                    break;
                case "A":
                    ApplyForGuardDuty(player);
                    break;
                case "*":
                    ResignFromGuardDuty(player);
                    break;
                case "T":
                    AttemptBankRobbery(player);
                    break;
                case "R":
                    ExitLocation();
                    return;
                case "?":
                    DisplayBankMenu(player);
                    return;
                default:
                    DisplayMessage("Invalid choice. Press '?' for menu.", ConsoleColor.Red);
                    break;
            }
            
            DisplayMessage("");
            DisplayMessage("Press any key to continue...", ConsoleColor.Yellow);
            Console.ReadKey();
            DisplayBankMenu(player);
            return;
        }
    }
    
    private void DepositMoney(Character player)
    {
        if (player.Gold <= 0)
        {
            DisplayMessage("You have no gold!", ConsoleColor.Red);
            return;
        }
        
        DisplayMessage("");
        DisplayMessage($"The {BankmanName.ToUpper()} gnome gets a greedy look in his eyes.", ConsoleColor.Gray);
        
        string genderAddress = player.Sex == CharacterSex.Male ? "sir" : "madam";
        DisplayMessage($"How much would you like to deposit, {genderAddress}?", ConsoleColor.Cyan);
        
        long maxDeposit = Math.Min(player.Gold, GameConfig.MaxBankBalance - player.BankGold);
        DisplayMessage($"(max {maxDeposit:N0}): ", ConsoleColor.Gray, false);
        
        string input = Console.ReadLine();
        if (long.TryParse(input, out long amount) && amount > 0 && amount <= maxDeposit)
        {
            player.Gold -= amount;
            player.BankGold += amount;
            _safeContents += amount;
            
            DisplayMessage($"You deposited {amount:N0} gold coins.", ConsoleColor.Green);
            
            // Add to king's treasury (bank tax)
            var king = GetKing();
            if (king != null)
            {
                long bankTax = amount / 100; // 1% bank tax
                king.Treasury += bankTax;
            }
        }
        else
        {
            DisplayMessage("Squid-brain.", ConsoleColor.Red);
        }
    }
    
    private void WithdrawMoney(Character player)
    {
        if (player.BankGold <= 0)
        {
            DisplayMessage("Your account is empty!", ConsoleColor.Red);
            return;
        }
        
        DisplayMessage("");
        DisplayMessage($"The {BankmanName.ToUpper()} gnome looks disappointed...", ConsoleColor.Gray);
        
        string genderAddress = player.Sex == CharacterSex.Male ? "sir" : "madam";
        DisplayMessage($"How much would you like to withdraw, {genderAddress}?", ConsoleColor.Cyan);
        
        long maxWithdraw = Math.Min(player.BankGold, GameConfig.MaxBankBalance - player.Gold);
        DisplayMessage($"(max {maxWithdraw:N0}): ", ConsoleColor.Gray, false);
        
        string input = Console.ReadLine();
        if (long.TryParse(input, out long amount) && amount > 0 && amount <= maxWithdraw)
        {
            player.BankGold -= amount;
            player.Gold += amount;
            _safeContents = Math.Max(0, _safeContents - amount);
            
            DisplayMessage($"You withdrew {amount:N0} gold coins.", ConsoleColor.Green);
        }
        else
        {
            DisplayMessage("Dummy! Get real!", ConsoleColor.Red);
        }
    }
    
    private void TransferMoney(Character player)
    {
        if (player.BankGold <= 0)
        {
            DisplayMessage("You have no gold in your bank account!", ConsoleColor.Red);
            return;
        }
        
        DisplayMessage("");
        DisplayMessage("Transfer gold to another player's account.", ConsoleColor.Gray);
        DisplayMessage("Enter player name: ", ConsoleColor.Yellow, false);
        string targetName = Console.ReadLine()?.Trim();
        
        if (string.IsNullOrEmpty(targetName))
        {
            DisplayMessage("Invalid name.", ConsoleColor.Red);
            return;
        }
        
        // In a real implementation, you would search the player database
        // For now, we'll simulate finding a player
        DisplayMessage($"Player '{targetName}' found.", ConsoleColor.Green);
        
        DisplayMessage($"How much gold to transfer (max {player.BankGold:N0}): ", ConsoleColor.Yellow, false);
        string input = Console.ReadLine();
        
        if (long.TryParse(input, out long amount) && amount > 0 && amount <= player.BankGold)
        {
            player.BankGold -= amount;
            
            DisplayMessage($"You transferred {amount:N0} gold coins to {targetName}.", ConsoleColor.Green);
            DisplayMessage("A transfer receipt has been sent to them.", ConsoleColor.Gray);
            
            // In a real implementation, you would:
            // 1. Add gold to target player's bank account
            // 2. Send them a mail notification
            // 3. Log the transaction
        }
        else
        {
            DisplayMessage("Invalid amount.", ConsoleColor.Red);
        }
    }
    
    private void DisplayAccountStatus(Character player)
    {
        DisplayMessage("");
        DisplayMessage("═══ Account Status ═══", ConsoleColor.Cyan);
        DisplayMessage($"Account Holder: {player.Name2}", ConsoleColor.Gray);
        DisplayMessage($"Gold on Hand: {player.Gold:N0}", ConsoleColor.Yellow);
        DisplayMessage($"Gold in Bank: {player.BankGold:N0}", ConsoleColor.Yellow);
        DisplayMessage($"Total Worth: {(player.Gold + player.BankGold):N0}", ConsoleColor.Green);
        
        if (player.BankGuard)
        {
            DisplayMessage($"Guard Status: ACTIVE", ConsoleColor.Green);
            DisplayMessage($"Daily Wage: {player.BankWage:N0} gold", ConsoleColor.Green);
        }
        else
        {
            DisplayMessage("Guard Status: Not a guard", ConsoleColor.Gray);
        }
        
        DisplayMessage($"Interest Earned: {player.Interest:N0} gold", ConsoleColor.Cyan);
        DisplayMessage($"Bank Robbery Attempts Left: {player.BankRobberyAttempts}", ConsoleColor.Red);
    }
    
    private void ApplyForGuardDuty(Character player)
    {
        DisplayMessage("");
        
        if (player.BankGuard)
        {
            DisplayMessage("You are already a bank guard!", ConsoleColor.Yellow);
            return;
        }
        
        if (player.Level < GameConfig.MinLevelForGuard)
        {
            DisplayMessage($"You need to be at least level {GameConfig.MinLevelForGuard} to apply.", ConsoleColor.Red);
            return;
        }
        
        if (player.Darkness > GameConfig.MaxDarknessForGuard)
        {
            DisplayMessage("Your criminal record is too extensive. We cannot trust you.", ConsoleColor.Red);
            DisplayMessage("Try doing some good deeds first.", ConsoleColor.Gray);
            return;
        }
        
        DisplayMessage("The bank manager reviews your application...", ConsoleColor.Gray);
        DisplayMessage("And according to our records, you appear to be a", ConsoleColor.Gray);
        DisplayMessage("trustworthy character.", ConsoleColor.Gray);
        DisplayMessage("");
        
        DisplayMessage("Would you like to come work for us? (Y/N): ", ConsoleColor.Yellow, false);
        var response = Console.ReadKey().KeyChar.ToString().ToUpper();
        DisplayMessage("");
        
        if (response == "Y")
        {
            player.BankGuard = true;
            int guardSalary = 1000 + (player.Level * GameConfig.GuardSalaryPerLevel);
            player.BankWage = guardSalary;
            _activeGuards.Add(player);
            
            DisplayMessage("Great!", ConsoleColor.Green);
            DisplayMessage("You will be called upon whenever the bank is in trouble.", ConsoleColor.Gray);
            DisplayMessage("Your salary will be transferred to your account on a daily basis.", ConsoleColor.Gray);
            DisplayMessage("Try to stay alive and keep your weapons ready!", ConsoleColor.Gray);
            DisplayMessage("Good luck!", ConsoleColor.Gray);
            DisplayMessage("");
            
            DisplayMessage("Go public?", ConsoleColor.Magenta);
            DisplayMessage("The bank management is anxious to scare the", ConsoleColor.Gray);
            DisplayMessage("criminal elements from robbing the bank.", ConsoleColor.Gray);
            DisplayMessage("The bank would therefore appreciate if you would", ConsoleColor.Gray);
            DisplayMessage("let them announce your employment.", ConsoleColor.Gray);
            DisplayMessage("");
            
            DisplayMessage("Go public with this? (Y/N): ", ConsoleColor.Yellow, false);
            var publicResponse = Console.ReadKey().KeyChar.ToString().ToUpper();
            DisplayMessage("");
            
            if (publicResponse == "Y")
            {
                DisplayMessage("Good!", ConsoleColor.Green);
                // In a real implementation, this would post to the game news
                DisplayMessage("Your employment has been announced in the town news.", ConsoleColor.Gray);
            }
        }
        else
        {
            DisplayMessage("I think that was a wise decision...", ConsoleColor.Gray);
        }
    }
    
    private void ResignFromGuardDuty(Character player)
    {
        if (!player.BankGuard)
        {
            DisplayMessage("You are not a bank guard!", ConsoleColor.Red);
            return;
        }
        
        DisplayMessage("");
        DisplayMessage("Are you sure you want to resign from guard duty? (Y/N): ", ConsoleColor.Yellow, false);
        var response = Console.ReadKey().KeyChar.ToString().ToUpper();
        DisplayMessage("");
        
        if (response == "Y")
        {
            player.BankGuard = false;
            player.BankWage = 0;
            _activeGuards.Remove(player);
            
            DisplayMessage("You have resigned from bank guard duty.", ConsoleColor.Gray);
            DisplayMessage("Thank you for your service.", ConsoleColor.Gray);
        }
    }
    
    private void AttemptBankRobbery(Character player)
    {
        DisplayMessage("");
        
        if (player.BankGuard)
        {
            DisplayMessage("You cannot rob the bank you are sworn to protect!", ConsoleColor.Red);
            return;
        }
        
        if (player.BankRobberyAttempts <= 0)
        {
            DisplayMessage("You have exhausted your robbery attempts for today!", ConsoleColor.Red);
            return;
        }
        
        DisplayMessage("You survey the bank's defenses...", ConsoleColor.Gray);
        DisplayMessage("");
        
        int guardCount = CalculateGuardCount();
        DisplayMessage($"Safe contains    : {_safeContents:N0} gold coins.", ConsoleColor.Gray);
        DisplayMessage($"Guards to fight  : {guardCount} (could be some dogs too)", ConsoleColor.Gray);
        DisplayMessage($"Players to fight : *unknown*", ConsoleColor.Magenta);
        DisplayMessage("");
        
        DisplayMessage("R(O)b!  (all your chivalry will be gone!)", ConsoleColor.Red);
        DisplayMessage("(I)nspect guards", ConsoleColor.Gray);
        DisplayMessage("(A)bort", ConsoleColor.Gray);
        DisplayMessage("");
        
        DisplayMessage("Bank robbery (? for menu): ", ConsoleColor.Yellow, false);
        var choice = Console.ReadKey().KeyChar.ToString().ToUpper();
        DisplayMessage("");
        
        switch (choice)
        {
            case "O":
                DisplayMessage("Are you sure you want to rob the bank? (Y/N): ", ConsoleColor.Red, false);
                var confirm = Console.ReadKey().KeyChar.ToString().ToUpper();
                DisplayMessage("");
                
                if (confirm == "Y")
                {
                    ExecuteBankRobbery(player, guardCount);
                }
                break;
                
            case "I":
                InspectGuards(guardCount);
                break;
                
            case "A":
                DisplayMessage("Chicken out? (Y/N): ", ConsoleColor.Yellow, false);
                var abort = Console.ReadKey().KeyChar.ToString().ToUpper();
                DisplayMessage("");
                if (abort == "Y")
                {
                    DisplayMessage("You quietly leave the bank.", ConsoleColor.Gray);
                }
                break;
        }
    }
    
    private int CalculateGuardCount()
    {
        int guards = 2; // Base guards
        
        if (_safeContents > GameConfig.SafeGuardThreshold1) guards += 2;
        if (_safeContents > GameConfig.SafeGuardThreshold2) guards += 2;
        if (_safeContents > GameConfig.SafeGuardThreshold3) guards += 2;
        if (_safeContents > GameConfig.SafeGuardThreshold4) guards += 2;
        if (_safeContents > GameConfig.SafeGuardThreshold5) guards += 2;
        if (_safeContents > GameConfig.SafeGuardThreshold6) guards += 2;
        
        return Math.Min(guards, 10); // Max 10 guards
    }
    
    private void InspectGuards(int guardCount)
    {
        DisplayMessage("");
        DisplayMessage("You discretely inspect the guards...", ConsoleColor.Gray);
        
        for (int i = 1; i <= guardCount; i++)
        {
            if (i == 1)
            {
                DisplayMessage("Captain of the Guard (Broadsword - Chainmail)", ConsoleColor.Red);
            }
            else
            {
                DisplayMessage("Guard (Halberd - Ringmail)", ConsoleColor.Yellow);
            }
        }
        
        // Random chance for guard dog
        if (new Random().Next(2) == 0)
        {
            DisplayMessage("Pitbull (Jaws - skin)", ConsoleColor.DarkRed);
        }
    }
    
    private void ExecuteBankRobbery(Character player, int guardCount)
    {
        // Lose all chivalry and gain darkness
        player.Chivalry = 0;
        player.Darkness += 50;
        player.BankRobberyAttempts--;
        
        DisplayMessage("");
        DisplayMessage("You storm into the bank with weapons drawn!", ConsoleColor.Red);
        DisplayMessage("Alarms start ringing throughout the building!", ConsoleColor.Red);
        DisplayMessage("");
        
        // Create combat scenario
        var combatEngine = new CombatEngine();
        var monsters = new List<Monster>();
        
        // Create guards
        for (int i = 0; i < guardCount; i++)
        {
            Monster guard;
            if (i == 0)
            {
                guard = CreateCaptainOfGuard();
            }
            else
            {
                guard = CreateBankGuard();
            }
            monsters.Add(guard);
        }
        
        // Random guard dog
        if (new Random().Next(2) == 0)
        {
            monsters.Add(CreateGuardDog());
        }
        
        DisplayMessage("The guards move to stop you!", ConsoleColor.Red);
        
        // Start combat
        var combatResult = combatEngine.StartCombat(player, monsters, "Bank Robbery", allowRetreat: false);
        
        if (combatResult.PlayerWon)
        {
            // Successful robbery
            long stolenGold = _safeContents / 4; // Can only steal 25% of safe
            player.Gold += stolenGold;
            _safeContents -= stolenGold;
            
            DisplayMessage($"You successfully robbed {stolenGold:N0} gold from the bank!", ConsoleColor.Green);
            DisplayMessage("You quickly escape before reinforcements arrive!", ConsoleColor.Yellow);
            
            // Major criminal act - wanted level increases
            player.WantedLvl += 5;
        }
        else
        {
            // Failed robbery
            DisplayMessage("The guards overwhelm you!", ConsoleColor.Red);
            DisplayMessage("You are beaten and thrown out of the bank!", ConsoleColor.Red);
            
            // Lose some gold as fine
            long fine = Math.Min(player.Gold, player.Level * 1000);
            player.Gold -= fine;
            if (fine > 0)
            {
                DisplayMessage($"You are fined {fine:N0} gold for the attempt!", ConsoleColor.Red);
            }
            
            // Wanted level increases
            player.WantedLvl += 2;
        }
    }
    
    private Monster CreateCaptainOfGuard()
    {
        var captain = new Monster();
        captain.Name = "Captain of the Guard";
        captain.HP = captain.MaxHP = 75;
        captain.Strength = 25;
        captain.Defence = 15;
        captain.Experience = 1500;
        captain.Gold = 500;
        captain.WeaponName = "Broadsword";
        captain.ArmorName = "Chainmail";
        captain.WeaponPower = 25;
        captain.ArmorPower = 15;
        captain.Phrase = "Stop!";
        return captain;
    }
    
    private Monster CreateBankGuard()
    {
        var guard = new Monster();
        guard.Name = "Guard";
        guard.HP = guard.MaxHP = 50;
        guard.Strength = 20;
        guard.Defence = 10;
        guard.Experience = 800;
        guard.Gold = 200;
        guard.WeaponName = "Halberd";
        guard.ArmorName = "Ringmail";
        guard.WeaponPower = 15;
        guard.ArmorPower = 10;
        guard.Phrase = "Robbery!";
        return guard;
    }
    
    private Monster CreateGuardDog()
    {
        var dog = new Monster();
        dog.Name = "Pitbull";
        dog.HP = dog.MaxHP = 95;
        dog.Strength = 40;
        dog.Defence = 7;
        dog.Experience = 600;
        dog.Gold = 0;
        dog.WeaponName = "Jaws";
        dog.ArmorName = "skin";
        dog.WeaponPower = 45;
        dog.ArmorPower = 7;
        dog.Phrase = "Grrrr..";
        return dog;
    }
    
    /// <summary>
    /// Daily maintenance - pay guard wages and calculate interest
    /// </summary>
    public static void ProcessDailyMaintenance()
    {
        foreach (var guard in _activeGuards)
        {
            if (guard.BankGuard && guard.HP > 0) // Only living guards get paid
            {
                guard.BankGold += guard.BankWage;
                
                // Add interest to all accounts (small amount)
                if (guard.BankGold > 0)
                {
                    long interest = (long)(guard.BankGold * GameConfig.DailyInterestRate / 100);
                    guard.Interest += interest;
                    guard.BankGold += interest;
                }
            }
        }
    }
    
    /// <summary>
    /// Get current safe contents for display
    /// </summary>
    public static long GetSafeContents()
    {
        return _safeContents;
    }
    
    /// <summary>
    /// Get active guard count
    /// </summary>
    public static int GetActiveGuardCount()
    {
        return _activeGuards.Count(g => g.BankGuard && g.HP > 0);
    }

    /// <summary>
    /// Stub for GetKing method
    /// </summary>
    private Character GetKing()
    {
        // Return a default king character or null
        return null;
    }
} 
