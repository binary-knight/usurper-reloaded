using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Main Street location - central hub of the game
/// Based on Pascal main_menu procedure from GAMEC.PAS
/// </summary>
public class MainStreetLocation : BaseLocation
{
    public MainStreetLocation() : base(
        GameLocation.MainStreet,
        "Main Street",
        "You are standing on the main street of town. The bustling center of all activity."
    ) { }
    
    protected override void SetupLocation()
    {
        // Pascal-compatible exits from ONLINE.PAS onloc_mainstreet case
        PossibleExits = new List<GameLocation>
        {
            GameLocation.TheInn,       // loc1
            GameLocation.Church,       // loc2  
            GameLocation.Darkness,     // loc3
            GameLocation.Master,       // loc4
            GameLocation.MagicShop,    // loc5
            GameLocation.Dungeons,     // loc6
            GameLocation.WeaponShop,   // loc7
            GameLocation.ArmorShop,    // loc8
            GameLocation.Bank,         // loc9
            GameLocation.Marketplace,  // loc10
            GameLocation.DarkAlley,    // loc11
            GameLocation.ReportRoom,   // loc12
            GameLocation.Healer,       // loc13
            GameLocation.AnchorRoad    // loc14
        };
        
        // Location actions based on Pascal main menu
        LocationActions = new List<string>
        {
            "Status",              // (S)tatus
            "Good Deeds",          // (G)ood Deeds
            "Evil Deeds",          // (E)vil Deeds  
            "News",                // (N)ews
            "List Characters",     // (L)ist Characters
            "Fame",                // (F)ame
            "Relations",           // (R)elations
            "Suicide",             // (*) Suicide
            "Who is Online?",      // (Ctrl+W)
            "Send Message",        // (Ctrl+T)  
            "Send Stuff"           // (Ctrl+S)
        };
    }
    
    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        
        // ASCII art header (simplified version)
        terminal.SetColor("bright_blue");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                              MAIN STREET                                    ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");
        
        // Location description with time/weather
        terminal.SetColor("white");
        terminal.WriteLine($"You are standing on the main street of {GetTownName()}.");
        terminal.WriteLine($"The {GetTimeOfDay()} air is {GetWeather()}.");
        terminal.WriteLine("");
        
        // Show NPCs in location
        ShowNPCsInLocation();
        
        // Main Street menu (Pascal-style layout)
        ShowMainStreetMenu();
        
        // Status line
        ShowStatusLine();
    }
    
    /// <summary>
    /// Show the classic Main Street menu layout
    /// </summary>
    private void ShowMainStreetMenu()
    {
        terminal.SetColor("yellow");
        terminal.WriteLine("Main street actions:");
        terminal.WriteLine("");
        
        terminal.WriteLine("┌─────────────────────────────────────────────────────────────────────────────┐", "cyan");
        terminal.WriteLine("│                              -= MAIN STREET =-                            │", "cyan");
        terminal.WriteLine("│                                                                             │", "cyan");
        terminal.WriteLine("│  (S)tatus          (D)ungeons         (B)ank             (I)nn            │", "white");
        terminal.WriteLine("│  (C)hallenges      (A)rmor Shop       (W)eapon Shop      (M)agic Shop     │", "white");
        terminal.WriteLine("│  (T)emple          (N)ews             (F)ame             (R)elations      │", "white");
        terminal.WriteLine("│                                                                             │", "cyan");
        terminal.WriteLine("│  (L)ist Characters  (T)he Marketplace  (X)tra Shops  (Q)uit Game  (9) Combat Test │", "white");
        terminal.WriteLine("│                                                                             │", "cyan");
        terminal.WriteLine("│  (G)ood Deeds       (E)vil Deeds        (V)isit Master  (*) Suicide  (R)elations │", "white");
        terminal.WriteLine("│                                                                             │", "cyan");
        terminal.WriteLine("│  (K) Castle  (P) Prison  (O) Church  (Y) Dark Alley  (Ctrl+W) Who is Online?  (Ctrl+T) Send message │", "white");
        terminal.WriteLine("│                                                                             │", "cyan");
        terminal.WriteLine("│  (1) Healing Hut  (Q)uit Game  (2) Send stuff  (Ctrl+S) Send stuff  (3) List Characters │", "white");
        terminal.WriteLine("│                                                                             │", "cyan");
        terminal.WriteLine("└─────────────────────────────────────────────────────────────────────────────┘", "cyan");
        terminal.WriteLine("");
        
        // Navigation shortcuts
        terminal.SetColor("cyan");
        terminal.WriteLine("Quick Navigation:");
        terminal.WriteLine("(K) Castle  (P) Prison  (O) Church  (Y) Dark Alley");
        terminal.WriteLine("");
    }
    
    protected override async Task<bool> ProcessChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(choice))
            return false;
            
        var upperChoice = choice.ToUpper().Trim();
        
        // Handle Main Street specific commands first
        switch (upperChoice)
        {
            case "S":
                await ShowStatus();
                return false;
                
            case "D":
                await NavigateToLocation(GameLocation.Dungeons);
                return true;
                
            case "B":
                await NavigateToLocation(GameLocation.Bank);
                return true;
                
            case "I":
                await NavigateToLocation(GameLocation.TheInn);
                return true;
                
            case "C":
                await NavigateToLocation(GameLocation.AnchorRoad); // Challenges
                return true;
                
            case "A":
                await NavigateToLocation(GameLocation.ArmorShop);
                return true;
                
            case "W":
                await NavigateToLocation(GameLocation.WeaponShop);
                return true;
                
            case "H":
                await NavigateToLocation(GameLocation.Home);
                return true;
                
            case "F":
                await ShowFame();
                return false;
                
            case "1":
                await NavigateToLocation(GameLocation.Healer);
                return true;
                
            case "Q":
                await QuitGame();
                return true;
                
            case "G":
                await ShowGoodDeeds();
                return false;
                
            case "E":
                await ShowEvilDeeds();
                return false;
                
            case "V":
                await NavigateToLocation(GameLocation.Master);
                return true;
                
            case "M":
                await NavigateToLocation(GameLocation.MagicShop);
                return true;
                
            case "N":
                await ShowNews();
                return false;
                
            case "L":
                await ListCharacters();
                return false;
                
            case "T":
                terminal.WriteLine("You enter the Temple of the Gods...", "cyan");
                await Task.Delay(1500);
                throw new LocationChangeException("temple");
                
            case "X":
                await NavigateToLocation(GameLocation.DarkAlley); // Extra shops
                return true;
                
            case "R":
                await ShowRelations();
                return false;
                
            case "*":
                await AttemptSuicide();
                return false;
            
            case "9":
                await TestCombat();
                return false;
            
            // Quick navigation
            case "K":
                await NavigateToLocation(GameLocation.Castle);
                return true;
                
            case "P":
                await NavigateToLocation(GameLocation.Prison);
                return true;
                
            case "O":
                await NavigateToLocation(GameLocation.Church);
                return true;
                
            case "Y":
                terminal.WriteLine("You head to the Dark Alley...", "gray");
                await Task.Delay(1500);
                throw new LocationChangeException("dark_alley");
                
            // Global commands
            case "CTRL+W":
            case "!W":
                await ShowWhoIsOnline();
                return false;
                
            case "CTRL+T":
            case "!T":
                await SendMessage();
                return false;
                
            case "CTRL+S":
            case "!S":
                await SendStuff();
                return false;
                
            case "?":
                // Menu is always shown
                return false;
                
            default:
                terminal.WriteLine("Invalid choice! Type ? for help.", "red");
                await Task.Delay(1500);
                return false;
        }
    }
    
    // Main Street specific action implementations
    private async Task ShowGoodDeeds()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_white");
        terminal.WriteLine("Good Deeds");
        terminal.WriteLine("==========");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine($"Your Chivalry: {currentPlayer.Chivalry}");
        terminal.WriteLine($"Good deeds left today: {currentPlayer.ChivNr}");
        terminal.WriteLine("");
        
        if (currentPlayer.ChivNr > 0)
        {
            terminal.WriteLine("Available good deeds:");
            terminal.WriteLine("1. Give gold to the poor");
            terminal.WriteLine("2. Help at the temple");
            terminal.WriteLine("3. Volunteer at orphanage");
            terminal.WriteLine("");
            
            var choice = await terminal.GetInput("Choose a deed (1-3, 0 to cancel): ");
            await ProcessGoodDeed(choice);
        }
        else
        {
            terminal.WriteLine("You have done enough good for today.", "yellow");
        }
        
        await terminal.PressAnyKey();
    }
    
    private async Task ShowEvilDeeds()
    {
        terminal.ClearScreen();
        terminal.SetColor("red");
        terminal.WriteLine("Evil Deeds");
        terminal.WriteLine("==========");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine($"Your Darkness: {currentPlayer.Darkness}");
        terminal.WriteLine($"Dark deeds left today: {currentPlayer.DarkNr}");
        terminal.WriteLine("");
        
        if (currentPlayer.DarkNr > 0)
        {
            terminal.WriteLine("Available dark deeds:");
            terminal.WriteLine("1. Rob from the poor");
            terminal.WriteLine("2. Vandalize property");
            terminal.WriteLine("3. Spread malicious rumors");
            terminal.WriteLine("");
            
            var choice = await terminal.GetInput("Choose a deed (1-3, 0 to cancel): ");
            await ProcessEvilDeed(choice);
        }
        else
        {
            terminal.WriteLine("You have caused enough trouble for today.", "yellow");
        }
        
        await terminal.PressAnyKey();
    }
    
    private async Task ShowNews()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("Yesterday's News");
        terminal.WriteLine("===============");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("• A new challenger has emerged in the dungeons!");
        terminal.WriteLine("• The King announced new tax rates for the kingdom.");
        terminal.WriteLine("• Strange lights were seen over the Dark Alley last night.");
        terminal.WriteLine("• The Inn is serving a new ale that boosts strength!");
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    private async Task ShowFame()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("Hall of Fame");
        terminal.WriteLine("============");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("Top Players:");
        terminal.WriteLine("1. King Arthur - Level 45 - Paladin");
        terminal.WriteLine("2. Morgana - Level 42 - Magician");  
        terminal.WriteLine("3. Lancelot - Level 40 - Warrior");
        terminal.WriteLine("");
        terminal.WriteLine($"Your Rank: #{GetPlayerRank()} - Level {currentPlayer.Level}");
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    private async Task ListCharacters()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_green");
        terminal.WriteLine("Character List");
        terminal.WriteLine("==============");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("Active Players:");
        terminal.WriteLine($"• {currentPlayer.DisplayName} (You) - Level {currentPlayer.Level} {currentPlayer.Class}");
        terminal.WriteLine("• Sir Galahad - Level 38 Paladin");
        terminal.WriteLine("• Dark Wizard - Level 35 Magician");
        terminal.WriteLine("• Swift Arrow - Level 33 Ranger");
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    private async Task ShowRelations()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("Relations");
        terminal.WriteLine("=========");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine($"Married: {(currentPlayer.Married ? "Yes" : "No")}");
        terminal.WriteLine($"Children: {currentPlayer.Kids}");
        terminal.WriteLine($"Team: {(string.IsNullOrEmpty(currentPlayer.Team) ? "None" : currentPlayer.Team)}");
        terminal.WriteLine("");
        
        if (currentPlayer.Married)
        {
            terminal.WriteLine("Family options:");
            terminal.WriteLine("1. Visit home");
            terminal.WriteLine("2. Check on children");
        }
        else
        {
            terminal.WriteLine("You are single. Visit Love Street to find romance!");
        }
        
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    private async Task AttemptSuicide()
    {
        terminal.SetColor("red");
        terminal.WriteLine("Are you sure you want to end your character's life?", "red");
        var confirm = await terminal.GetInput("Type YES to confirm: ");
        
        if (confirm.ToUpper() == "YES")
        {
            terminal.WriteLine("Your character takes their own life...", "red");
            currentPlayer.HP = 0;
            await Task.Delay(2000);
            terminal.WriteLine("You are dead. Game over.", "gray");
            await Task.Delay(3000);
        }
        else
        {
            terminal.WriteLine("You decide life is worth living after all.", "green");
            await Task.Delay(1500);
        }
    }
    
    private async Task ShowWhoIsOnline()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("Who's Online");
        terminal.WriteLine("============");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine($"• {currentPlayer.DisplayName} - Main Street (You)");
        terminal.WriteLine("• Seth Able - The Inn (Drunk)");
        terminal.WriteLine("• Royal Guard - Castle (On Duty)");
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    private async Task SendMessage()
    {
        terminal.WriteLine("Message system not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task SendStuff()
    {
        terminal.WriteLine("Item transfer system not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    private async Task QuitGame()
    {
        terminal.SetColor("yellow");
        terminal.WriteLine("Thanks for playing Usurper Reborn!");
        terminal.WriteLine("Your progress has been saved.");
        await Task.Delay(2000);
        
        // Signal game should quit
        throw new LocationExitException(GameLocation.NoWhere);
    }
    
    // Helper methods
    private string GetTownName()
    {
        return "Usurper"; // Could be configurable
    }
    
    private string GetTimeOfDay()
    {
        var hour = DateTime.Now.Hour;
        return hour switch
        {
            >= 6 and < 12 => "morning",
            >= 12 and < 18 => "afternoon", 
            >= 18 and < 22 => "evening",
            _ => "night"
        };
    }
    
    private string GetWeather()
    {
        var weather = new[] { "clear", "cloudy", "misty", "cool", "warm", "breezy" };
        return weather[GD.RandRange(0, weather.Length - 1)];
    }
    
    private int GetPlayerRank()
    {
        // Simplified ranking system
        return Math.Max(1, 20 - (int)(currentPlayer.Level / 2));
    }
    
    private async Task ProcessGoodDeed(string choice)
    {
        if (int.TryParse(choice, out int deed) && deed >= 1 && deed <= 3)
        {
            currentPlayer.ChivNr--;
            currentPlayer.Chivalry += 10;
            
            var deedName = deed switch
            {
                1 => "giving gold to the poor",
                2 => "helping at the temple",
                3 => "volunteering at the orphanage",
                _ => "performing a good deed"
            };
            
            terminal.WriteLine($"You gain chivalry by {deedName}!", "green");
            await Task.Delay(1500);
        }
    }
    
    private async Task ProcessEvilDeed(string choice)
    {
        if (int.TryParse(choice, out int deed) && deed >= 1 && deed <= 3)
        {
            currentPlayer.DarkNr--;
            currentPlayer.Darkness += 10;
            
            var deedName = deed switch
            {
                1 => "robbing from the poor",
                2 => "vandalizing property", 
                3 => "spreading malicious rumors",
                _ => "performing an evil deed"
            };
            
            terminal.WriteLine($"Your dark soul grows by {deedName}!", "red");
            await Task.Delay(1500);
        }
    }
    
    /// <summary>
    /// Test combat system (DEBUG)
    /// </summary>
    private async Task TestCombat()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_red");
        terminal.WriteLine("=== COMBAT TEST ===");
        terminal.WriteLine("");
        
        // Create a test monster (Street Thug)
        var testMonster = Monster.CreateMonster(
            nr: 1,
            name: "Street Thug",
            hps: 50,
            strength: 15,
            defence: 8,
            phrase: "Give me your gold!",
            grabweap: true,
            grabarm: false,
            weapon: "Rusty Knife",
            armor: "Torn Clothes",
            poisoned: false,
            disease: false,
            punch: 12,
            armpow: 2,
            weappow: 8
        );
        
        terminal.WriteLine("A street thug jumps out and blocks your path!");
        terminal.WriteLine($"The {testMonster.Name} brandishes a {testMonster.Weapon}!");
        terminal.WriteLine("");
        
        var confirm = await terminal.GetInput("Fight the thug? (Y/N): ");
        
        if (confirm.ToUpper() == "Y")
        {
            // Initialize combat engine
            var combatEngine = new CombatEngine(terminal);
            
            // Execute combat
            var result = await combatEngine.PlayerVsMonster(currentPlayer, testMonster);
            
            // Display result summary
            terminal.ClearScreen();
            terminal.SetColor("bright_cyan");
            terminal.WriteLine("=== COMBAT SUMMARY ===");
            terminal.WriteLine("");
            
            foreach (var logEntry in result.CombatLog)
            {
                terminal.WriteLine($"• {logEntry}");
            }
            
            terminal.WriteLine("");
            terminal.SetColor("white");
            terminal.WriteLine($"Final Outcome: {result.Outcome}");
            
            if (result.Outcome == CombatOutcome.Victory)
            {
                terminal.WriteLine("The thug flees into the shadows!", "green");
            }
            else if (result.Outcome == CombatOutcome.PlayerEscaped)
            {
                terminal.WriteLine("You slip away from the dangerous encounter.", "yellow");
            }
        }
        else
        {
            terminal.WriteLine("You wisely avoid the confrontation.", "green");
        }
        
        await terminal.PressAnyKey();
    }
} 