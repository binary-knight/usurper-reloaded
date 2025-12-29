using UsurperRemake.Utils;
using UsurperRemake.Systems;
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
            GameLocation.AnchorRoad,   // loc14
            GameLocation.Home          // loc15
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
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔═════════════════════════════════════════════════════════════════════════════╗");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("║                          -= MAIN STREET =-                                  ║");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╠═════════════════════════════════════════════════════════════════════════════╣");
        terminal.WriteLine("");

        // Row 1 - Primary locations
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_green");
        terminal.Write("D");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("ungeons     ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_green");
        terminal.Write("I");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("nn          ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_green");
        terminal.Write("T");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("emple       ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_green");
        terminal.Write("O");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("ld Church");

        // Row 2 - Shops
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_yellow");
        terminal.Write("W");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("eapon Shop  ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("A");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("rmor Shop   ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_yellow");
        terminal.Write("M");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("agic Shop");

        // Row 3 - Services
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_cyan");
        terminal.Write("B");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("ank         ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("1");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write(" Healer     ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("J");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine(" Marketplace");

        // Row 4 - Important locations
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_magenta");
        terminal.Write("K");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write(" Castle     ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_magenta");
        terminal.Write("H");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("ome         ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_magenta");
        terminal.Write("C");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("hallenges");

        terminal.WriteLine("");

        // Row 5 - Information
        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("cyan");
        terminal.Write("S");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("tatus       ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("cyan");
        terminal.Write("N");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("ews         ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("cyan");
        terminal.Write("F");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("ame");

        terminal.WriteLine("");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╚═════════════════════════════════════════════════════════════════════════════╝");
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
                var newsLocation = new NewsLocation();
                newsLocation.HandlePlayerEntry(currentPlayer as Player ?? new Player());
                return true;
                
            case "Z":
                await NavigateToTeamCorner();
                return true;
                
            case "L":
                await ListCharacters();
                return false;
                
            case "T":
                terminal.WriteLine("You enter the Temple of the Gods...", "cyan");
                await Task.Delay(1500);
                throw new LocationExitException(GameLocation.Temple);
                
            case "X":
                await NavigateToLocation(GameLocation.DarkAlley); // Extra shops
                return true;
                
            case "J":
                await NavigateToLocation(GameLocation.Marketplace);
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
                throw new LocationExitException(GameLocation.DarkAlley);
                
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
                
            case "2":
                await SendStuff();
                return false;
                
            case "3":
                await ListCharacters();
                return false;
                
            case "SETTINGS":
            case "CONFIG":
                await ShowSettingsMenu();
                return false;
                
            case "MAIL":
            case "CTRL+M":
            case "!M":
                await ShowMail();
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
    
    private async Task NavigateToTeamCorner()
    {
        terminal.WriteLine("You head to the Adventurers Team Corner...", "yellow");
        await Task.Delay(1000);
        
        // Navigate to TeamCornerLocation
        await NavigateToLocation(GameLocation.TeamCorner);
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
    
    /// <summary>
    /// Show settings and save management menu
    /// </summary>
    private async Task ShowSettingsMenu()
    {
        bool exitSettings = false;
        
        while (!exitSettings)
        {
            terminal.ClearScreen();
            terminal.SetColor("bright_cyan");
            terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            terminal.WriteLine("║                            SETTINGS & SAVE OPTIONS                          ║");
            terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
            terminal.WriteLine("");
            
            var dailyManager = DailySystemManager.Instance;
            var currentMode = dailyManager.CurrentMode;
            
            terminal.SetColor("white");
            terminal.WriteLine("Current Settings:");
            terminal.WriteLine($"  Daily Cycle Mode: {GetDailyCycleModeDescription(currentMode)}", "yellow");
            terminal.WriteLine($"  Auto-save: {(dailyManager.AutoSaveEnabled ? "Enabled" : "Disabled")}", "yellow");
            terminal.WriteLine($"  Current Day: {dailyManager.CurrentDay}", "yellow");
            terminal.WriteLine("");
            
            terminal.WriteLine("Options:");
            terminal.WriteLine("1. Change Daily Cycle Mode");
            terminal.WriteLine("2. Configure Auto-save Settings");
            terminal.WriteLine("3. Save Game Now");
            terminal.WriteLine("4. Load Different Save");
            terminal.WriteLine("5. Delete Save Files");
            terminal.WriteLine("6. View Save File Information");
            terminal.WriteLine("7. Force Daily Reset");
            terminal.WriteLine("8. Back to Main Street");
            terminal.WriteLine("");
            
            var choice = await terminal.GetInput("Enter your choice (1-8): ");
            
            switch (choice)
            {
                case "1":
                    await ChangeDailyCycleMode();
                    break;
                    
                case "2":
                    await ConfigureAutoSave();
                    break;
                    
                case "3":
                    await SaveGameNow();
                    break;
                    
                case "4":
                    await LoadDifferentSave();
                    break;
                    
                case "5":
                    await DeleteSaveFiles();
                    break;
                    
                case "6":
                    await ViewSaveFileInfo();
                    break;
                    
                case "7":
                    await ForceDailyReset();
                    break;
                    
                case "8":
                    exitSettings = true;
                    break;
                    
                default:
                    terminal.WriteLine("Invalid choice!", "red");
                    await Task.Delay(1000);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Change daily cycle mode
    /// </summary>
    private async Task ChangeDailyCycleMode()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("         DAILY CYCLE MODES");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("");
        
        terminal.SetColor("white");
        terminal.WriteLine("Available modes:");
        terminal.WriteLine("");
        
        terminal.WriteLine("1. Session-Based (Default)", "yellow");
        terminal.WriteLine("   • New day starts when you run out of turns or choose to rest");
        terminal.WriteLine("   • Perfect for casual play sessions");
        terminal.WriteLine("");
        
        terminal.WriteLine("2. Real-Time (24 hours)", "yellow");
        terminal.WriteLine("   • Classic BBS-style daily reset at midnight");
        terminal.WriteLine("   • NPCs continue to act while you're away");
        terminal.WriteLine("");
        
        terminal.WriteLine("3. Accelerated (4 hours)", "yellow");
        terminal.WriteLine("   • New day every 4 real hours");
        terminal.WriteLine("   • Faster progression for active players");
        terminal.WriteLine("");
        
        terminal.WriteLine("4. Accelerated (8 hours)", "yellow");
        terminal.WriteLine("   • New day every 8 real hours");
        terminal.WriteLine("   • Balanced progression");
        terminal.WriteLine("");
        
        terminal.WriteLine("5. Accelerated (12 hours)", "yellow");
        terminal.WriteLine("   • New day every 12 real hours");
        terminal.WriteLine("   • Slower but steady progression");
        terminal.WriteLine("");
        
        terminal.WriteLine("6. Endless", "yellow");
        terminal.WriteLine("   • No turn limits, play as long as you want");
        terminal.WriteLine("   • Perfect for exploration and experimentation");
        terminal.WriteLine("");
        
        var choice = await terminal.GetInput("Select mode (1-6) or 0 to cancel: ");
        
        var newMode = choice switch
        {
            "1" => DailyCycleMode.SessionBased,
            "2" => DailyCycleMode.RealTime24Hour,
            "3" => DailyCycleMode.Accelerated4Hour,
            "4" => DailyCycleMode.Accelerated8Hour,
            "5" => DailyCycleMode.Accelerated12Hour,
            "6" => DailyCycleMode.Endless,
            _ => (DailyCycleMode?)null
        };
        
        if (newMode.HasValue)
        {
            var dailyManager = DailySystemManager.Instance;
            dailyManager.SetDailyCycleMode(newMode.Value);
            
            terminal.WriteLine($"Daily cycle mode changed to: {GetDailyCycleModeDescription(newMode.Value)}", "green");
            
            // Save the change
            await GameEngine.Instance.SaveCurrentGame();
        }
        else if (choice != "0")
        {
            terminal.WriteLine("Invalid choice!", "red");
        }
        
        await terminal.PressAnyKey("Press any key to continue...");
    }
    
    /// <summary>
    /// Configure auto-save settings
    /// </summary>
    private async Task ConfigureAutoSave()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("         AUTO-SAVE SETTINGS");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("");
        
        var dailyManager = DailySystemManager.Instance;
        
        terminal.SetColor("white");
        terminal.WriteLine($"Current auto-save: {(dailyManager.AutoSaveEnabled ? "Enabled" : "Disabled")}");
        terminal.WriteLine("");
        
        terminal.WriteLine("1. Enable auto-save");
        terminal.WriteLine("2. Disable auto-save");
        terminal.WriteLine("3. Change auto-save interval");
        terminal.WriteLine("4. Back");
        terminal.WriteLine("");
        
        var choice = await terminal.GetInput("Enter your choice (1-4): ");
        
        switch (choice)
        {
            case "1":
                dailyManager.ConfigureAutoSave(true, TimeSpan.FromMinutes(5));
                terminal.WriteLine("Auto-save enabled (every 5 minutes)", "green");
                break;
                
            case "2":
                dailyManager.ConfigureAutoSave(false, TimeSpan.FromMinutes(5));
                terminal.WriteLine("Auto-save disabled", "yellow");
                break;
                
            case "3":
                terminal.WriteLine("Enter auto-save interval in minutes (1-60): ");
                var intervalInput = await terminal.GetInput("");
                if (int.TryParse(intervalInput, out var minutes) && minutes >= 1 && minutes <= 60)
                {
                    dailyManager.ConfigureAutoSave(true, TimeSpan.FromMinutes(minutes));
                    terminal.WriteLine($"Auto-save interval set to {minutes} minutes", "green");
                }
                else
                {
                    terminal.WriteLine("Invalid interval!", "red");
                }
                break;
                
            case "4":
                return;
                
            default:
                terminal.WriteLine("Invalid choice!", "red");
                break;
        }
        
        await terminal.PressAnyKey("Press any key to continue...");
    }
    
    /// <summary>
    /// Save game now
    /// </summary>
    private async Task SaveGameNow()
    {
        await GameEngine.Instance.SaveCurrentGame();
        await terminal.PressAnyKey("Press any key to continue...");
    }
    
    /// <summary>
    /// Load different save file
    /// </summary>
    private async Task LoadDifferentSave()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("         LOAD DIFFERENT SAVE");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("");
        
        var saves = SaveSystem.Instance.GetAllSaves();
        
        if (saves.Count == 0)
        {
            terminal.WriteLine("No save files found!", "red");
            await terminal.PressAnyKey("Press any key to continue...");
            return;
        }
        
        terminal.SetColor("white");
        terminal.WriteLine("Available save files:");
        terminal.WriteLine("");
        
        for (int i = 0; i < saves.Count; i++)
        {
            var save = saves[i];
            terminal.WriteLine($"{i + 1}. {save.PlayerName} (Level {save.Level}, Day {save.CurrentDay}, {save.TurnsRemaining} turns)");
            terminal.WriteLine($"   Saved: {save.SaveTime:yyyy-MM-dd HH:mm:ss}");
            terminal.WriteLine("");
        }
        
        var choice = await terminal.GetInput($"Select save file (1-{saves.Count}) or 0 to cancel: ");
        
        if (int.TryParse(choice, out var index) && index >= 1 && index <= saves.Count)
        {
            var selectedSave = saves[index - 1];
            terminal.WriteLine($"Loading {selectedSave.PlayerName}...", "yellow");
            
            // This would require restarting the game with the new save
            terminal.WriteLine("Note: Loading a different save requires restarting the game.", "cyan");
            terminal.WriteLine("Please exit and restart, then enter the character name to load.", "cyan");
        }
        else if (choice != "0")
        {
            terminal.WriteLine("Invalid choice!", "red");
        }
        
        await terminal.PressAnyKey("Press any key to continue...");
    }
    
    /// <summary>
    /// Delete save files
    /// </summary>
    private async Task DeleteSaveFiles()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_red");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("         DELETE SAVE FILES");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("");
        
        terminal.SetColor("red");
        terminal.WriteLine("WARNING: This action cannot be undone!");
        terminal.WriteLine("");
        
        var saves = SaveSystem.Instance.GetAllSaves();
        
        if (saves.Count == 0)
        {
            terminal.WriteLine("No save files found!", "yellow");
            await terminal.PressAnyKey("Press any key to continue...");
            return;
        }
        
        terminal.SetColor("white");
        terminal.WriteLine("Available save files:");
        terminal.WriteLine("");
        
        for (int i = 0; i < saves.Count; i++)
        {
            var save = saves[i];
            terminal.WriteLine($"{i + 1}. {save.PlayerName} (Level {save.Level}, Day {save.CurrentDay})");
        }
        
        terminal.WriteLine("");
        var choice = await terminal.GetInput($"Select save file to delete (1-{saves.Count}) or 0 to cancel: ");
        
        if (int.TryParse(choice, out var index) && index >= 1 && index <= saves.Count)
        {
            var selectedSave = saves[index - 1];
            
            terminal.WriteLine("");
            var confirm = await terminal.GetInput($"Are you sure you want to delete '{selectedSave.PlayerName}'? Type 'DELETE' to confirm: ");
            
            if (confirm == "DELETE")
            {
                var success = SaveSystem.Instance.DeleteSave(selectedSave.PlayerName);
                if (success)
                {
                    terminal.WriteLine("Save file deleted successfully!", "green");
                }
                else
                {
                    terminal.WriteLine("Failed to delete save file!", "red");
                }
            }
            else
            {
                terminal.WriteLine("Deletion cancelled.", "yellow");
            }
        }
        else if (choice != "0")
        {
            terminal.WriteLine("Invalid choice!", "red");
        }
        
        await terminal.PressAnyKey("Press any key to continue...");
    }
    
    /// <summary>
    /// View save file information
    /// </summary>
    private async Task ViewSaveFileInfo()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("         SAVE FILE INFORMATION");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("");
        
        var saves = SaveSystem.Instance.GetAllSaves();
        
        if (saves.Count == 0)
        {
            terminal.WriteLine("No save files found!", "red");
            await terminal.PressAnyKey("Press any key to continue...");
            return;
        }
        
        terminal.SetColor("white");
        foreach (var save in saves)
        {
            terminal.WriteLine($"Character: {save.PlayerName}", "yellow");
            terminal.WriteLine($"Level: {save.Level}");
            terminal.WriteLine($"Current Day: {save.CurrentDay}");
            terminal.WriteLine($"Turns Remaining: {save.TurnsRemaining}");
            terminal.WriteLine($"Last Saved: {save.SaveTime:yyyy-MM-dd HH:mm:ss}");
            terminal.WriteLine($"File: {save.FileName}");
            terminal.WriteLine("");
        }
        
        await terminal.PressAnyKey("Press any key to continue...");
    }
    
    /// <summary>
    /// Force daily reset
    /// </summary>
    private async Task ForceDailyReset()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("         FORCE DAILY RESET");
        terminal.WriteLine("═══════════════════════════════════════");
        terminal.WriteLine("");
        
        terminal.SetColor("white");
        terminal.WriteLine("This will immediately trigger a daily reset, restoring your");
        terminal.WriteLine("daily limits and advancing the game day.");
        terminal.WriteLine("");
        
        var confirm = await terminal.GetInput("Are you sure? (yes/no): ");
        
        if (confirm.ToLower() == "yes")
        {
            var dailyManager = DailySystemManager.Instance;
            await dailyManager.ForceDailyReset();
            
            terminal.WriteLine("Daily reset completed!", "green");
        }
        else
        {
            terminal.WriteLine("Reset cancelled.", "yellow");
        }
        
        await terminal.PressAnyKey("Press any key to continue...");
    }
    
    /// <summary>
    /// Get description for daily cycle mode
    /// </summary>
    private string GetDailyCycleModeDescription(DailyCycleMode mode)
    {
        return mode switch
        {
            DailyCycleMode.SessionBased => "Session-Based (resets when turns depleted)",
            DailyCycleMode.RealTime24Hour => "Real-Time 24 Hour (resets at midnight)",
            DailyCycleMode.Accelerated4Hour => "Accelerated 4 Hour (resets every 4 hours)",
            DailyCycleMode.Accelerated8Hour => "Accelerated 8 Hour (resets every 8 hours)", 
            DailyCycleMode.Accelerated12Hour => "Accelerated 12 Hour (resets every 12 hours)",
            DailyCycleMode.Endless => "Endless (no turn limits)",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Show player's mailbox using the MailSystem.
    /// </summary>
    private async Task ShowMail()
    {
        terminal.WriteLine("Checking your mailbox...", "cyan");
        await MailSystem.ReadPlayerMail(currentPlayer.Name2, terminal);
        terminal.WriteLine("Press ENTER to return to Main Street.", "gray");
        await terminal.GetInput("");
    }
} 
