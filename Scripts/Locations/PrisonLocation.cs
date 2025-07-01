using UsurperRemake.Utils;
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

/// <summary>
/// Prison Location - The prisoner's perspective inside the Royal Prison
/// Based on PRISONC.PAS from the original Usurper Pascal implementation
/// Provides escape attempts, prisoner communication, and royal justice system
/// </summary>
public partial class PrisonLocation : BaseLocation
{
    private readonly GameEngine gameEngine;
    private readonly TerminalEmulator terminal;
    private bool refreshMenu = true;
    
    public PrisonLocation(GameEngine engine, TerminalEmulator term) : base("prison")
    {
        gameEngine = engine;
        terminal = term;
        SetLocationProperties();
    }
    
    // Add parameterless constructor for compatibility
    public PrisonLocation() : base("prison")
    {
        gameEngine = GameEngine.Instance;
        terminal = GameEngine.Instance.Terminal;
        SetLocationProperties();
    }
    
    private void SetLocationProperties()
    {
        LocationId = (int)GameLocation.Prison;
        LocationName = GameConfig.DefaultPrisonName;
        LocationDescription = "You are locked in a cold, damp prison cell";
        AllowedClasses = new HashSet<CharacterClass>(); // All classes allowed
        LevelRequirement = 1;
        
        // Add all character classes to allowed set
        foreach (CharacterClass charClass in System.Enum.GetValues<CharacterClass>())
        {
            AllowedClasses.Add(charClass);
        }
    }
    
    public new async Task<bool> EnterLocation(Character player)
    {
        if (player == null) return false;
        
        // Check if player is actually imprisoned
        if (player.DaysInPrison <= 0)
        {
            await terminal.WriteLineAsync("You are not imprisoned! Returning to Main Street.");
            await Task.Delay(1000);
            return false;
        }
        
        refreshMenu = true;
        await ShowPrisonInterface(player);
        return true;
    }
    
    private async Task ShowPrisonInterface(Character player)
    {
        char choice = '?';
        
        while (choice != 'Q')
        {
            // Update location status if needed
            await UpdatePrisonStatus(player);
            
            // Check if player can walk out (cell door open)
            if (await CanOpenCellDoor(player))
            {
                await HandleCellDoorOpen(player);
                return; // Exit prison
            }
            
            // Show who else is here if enabled
            if (ShouldShowOthersHere(player))
            {
                await ShowOthersHere(player);
            }
            
            // Display menu
            await DisplayPrisonMenu(player, true, true);
            
            // Get user input
            choice = await terminal.GetCharAsync();
            choice = char.ToUpper(choice);
            
            // Process user choice
            await ProcessPrisonChoice(player, choice);
        }
        
        // Quit prison (log out)
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("You cover yourself with some hay and try to get some sleep.");
        await terminal.WriteLineAsync("It will be a long and cold night with the rats...");
        await terminal.WriteLineAsync();
    }
    
    private async Task UpdatePrisonStatus(Character player)
    {
        // This would typically update the online player location
        // For now, just ensure the location is set correctly
        refreshMenu = true;
    }
    
    private async Task<bool> CanOpenCellDoor(Character player)
    {
        // In Pascal, this checks if onliner.location == onloc_prisonerop
        // For this implementation, we'll check if player has been rescued
        // This would be set by another player breaking them out
        return false; // TODO: Implement rescue mechanism
    }
    
    private async Task HandleCellDoorOpen(Character player)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("You walk out of your cell.");
        await terminal.WriteLineAsync("You are free!");
        
        // Reset player state
        player.HP = player.MaxHP;
        player.DaysInPrison = 0;
        
        // Return to dormitory
        await Task.Delay(GameConfig.PrisonCellOpenDelay);
    }
    
    private bool ShouldShowOthersHere(Character player)
    {
        // In Pascal: if player.ear = global_ear_all
        // For now, always show others
        return true;
    }
    
    private async Task ShowOthersHere(Character player)
    {
        // TODO: Implement showing other players in prison
        // This would list other online prisoners
    }
    
    private async Task DisplayPrisonMenu(Character player, bool force, bool isShort)
    {
        if (isShort)
        {
            if (!player.Expert)
            {
                if (refreshMenu && player.AutoMenu)
                {
                    refreshMenu = false;
                    await ShowPrisonMenuFull();
                }
                
                await terminal.WriteLineAsync();
                await terminal.WriteAsync("Royal Prison (");
                await terminal.WriteColorAsync("?", TerminalEmulator.ColorYellow);
                await terminal.WriteAsync(" for menu) :");
            }
            else
            {
                await terminal.WriteLineAsync();
                await terminal.WriteAsync("Royal Prison (W,M,N,D,O,S,E,Q,?) :");
            }
        }
        else
        {
            if (!player.Expert || force)
            {
                await ShowPrisonMenuFull();
            }
        }
    }
    
    private async Task ShowPrisonMenuFull()
    {
        await terminal.ClearScreenAsync();
        await terminal.WriteLineAsync();
        
        // Prison header
        await terminal.WriteColorLineAsync("IIIIIIIIIIIIIIIIIIIIIIII", TerminalEmulator.ColorCyan);
        await terminal.WriteColorLineAsync("III The Royal Prison III", TerminalEmulator.ColorCyan);
        await terminal.WriteColorLineAsync("IIIIIIIIIIIIIIIIIIIIIIII", TerminalEmulator.ColorCyan);
        await terminal.WriteLineAsync();
        
        // Prison atmosphere description
        await terminal.WriteLineAsync("You wake up cold and aching.");
        await terminal.WriteLineAsync("Horrifying screams from the torture-chamber nearby make You");
        await terminal.WriteLineAsync("shudder with fear.");
        await terminal.WriteLineAsync("The Sheriff and his henchmen can be heard chatting in the");
        await terminal.WriteLineAsync("corridor outside.");
        await terminal.WriteLineAsync();
        
        // Menu options
        await terminal.WriteLineAsync("(W)ho else is here          (D)emand to be released!");
        await terminal.WriteLineAsync("(M)essage                   (N)ew mail");
        await terminal.WriteLineAsync("(O)pen cell door            (E)scape!");
        await terminal.WriteLineAsync("(S)tatus");
        await terminal.WriteLineAsync("(Q)uit");
    }
    
    private async Task ProcessPrisonChoice(Character player, char choice)
    {
        switch (choice)
        {
            case '?':
                await HandleMenuDisplay(player);
                break;
            case 'S':
                await HandleStatusDisplay(player);
                break;
            case 'Q':
                await HandleQuitConfirmation(player);
                break;
            case 'M':
                await HandleSendMessage(player);
                break;
            case 'N':
                await HandleCheckMail(player);
                break;
            case 'O':
                await HandleOpenCellDoor(player);
                break;
            case 'D':
                await HandleDemandRelease(player);
                break;
            case 'E':
                await HandleEscapeAttempt(player);
                break;
            case 'W':
                await HandleListPrisoners(player);
                break;
            default:
                // Invalid choice, do nothing
                break;
        }
    }
    
    private async Task HandleMenuDisplay(Character player)
    {
        if (player.Expert)
            await DisplayPrisonMenu(player, true, false);
        else
            await DisplayPrisonMenu(player, false, false);
    }
    
    private async Task HandleStatusDisplay(Character player)
    {
        await ShowCharacterStatus(player);
    }
    
    private async Task ShowCharacterStatus(Character player)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("=== PRISONER STATUS ===");
        await terminal.WriteLineAsync($"Name: {player.DisplayName}");
        await terminal.WriteLineAsync($"Level: {player.Level}");
        await terminal.WriteLineAsync($"Health: {player.HP}/{player.MaxHP}");
        await terminal.WriteLineAsync($"Days remaining in prison: {player.DaysInPrison}");
        await terminal.WriteLineAsync($"Escape attempts left: {player.PrisonEscapes}");
        
        if (player.DaysInPrison == 1)
            await terminal.WriteLineAsync("You will likely be released tomorrow.");
        else
            await terminal.WriteLineAsync($"You have {player.DaysInPrison} days left to serve.");
            
        await terminal.WriteLineAsync();
        await terminal.WriteAsync("Press any key to continue...");
        await terminal.GetCharAsync();
    }
    
    private async Task HandleQuitConfirmation(Character player)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync();
        
        bool confirmed = await terminal.ConfirmAsync("QUIT game", false);
        if (!confirmed)
        {
            // Don't quit, continue prison loop
            return;
        }
        // If confirmed, the main loop will exit
    }
    
    private async Task HandleSendMessage(Character player)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("MESSAGE SYSTEM");
        await terminal.WriteLineAsync("==============");
        await terminal.WriteLineAsync("The message system is not yet implemented.");
        await terminal.WriteLineAsync("You'll have to wait for a future update to send messages from prison.");
        await terminal.WriteLineAsync();
        await terminal.WriteAsync("Press any key to continue...");
        await terminal.GetCharAsync();
    }
    
    private async Task HandleCheckMail(Character player)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync();
        await terminal.WriteColorLineAsync("Let's see if you have mail waiting ...", TerminalEmulator.ColorYellow);
        await terminal.WriteLineAsync();
        
        await Task.Delay(1000);
        
        // TODO: Implement mail system
        await terminal.WriteLineAsync("The mail system is not yet implemented.");
        await terminal.WriteLineAsync("You'll have to wait for future updates to receive mail in prison.");
        await terminal.WriteLineAsync();
        await terminal.WriteAsync("Press any key to continue...");
        await terminal.GetCharAsync();
    }
    
    private async Task HandleOpenCellDoor(Character player)
    {
        await terminal.WriteLineAsync();
        
        // Check if cell door can be opened (player was rescued)
        if (await CanOpenCellDoor(player))
        {
            await HandleCellDoorOpen(player);
        }
        else
        {
            await terminal.WriteLineAsync();
            await terminal.WriteColorLineAsync("You try to open the Iron door, but it's impossible.", TerminalEmulator.ColorRed);
            await terminal.WriteColorLineAsync("You are trapped in here! Perhaps you should try to escape.", TerminalEmulator.ColorRed);
            await Task.Delay(1000);
        }
    }
    
    private async Task HandleDemandRelease(Character player)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync();
        await terminal.WriteColorAsync("You clear your throat : ", TerminalEmulator.ColorWhite);
        await terminal.WriteColorLineAsync("Let me out of here please....!", TerminalEmulator.ColorCyan);
        
        await Task.Delay(GameConfig.PrisonGuardResponseDelay);
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("After a moment you hear a dark voice cry out :");
        
        // Random guard response (Pascal: case random(5))
        var random = new System.Random();
        string response = random.Next(5) switch
        {
            0 => GameConfig.PrisonDemandResponse1,
            1 => GameConfig.PrisonDemandResponse2,
            2 => GameConfig.PrisonDemandResponse3,
            3 => GameConfig.PrisonDemandResponse4,
            _ => GameConfig.PrisonDemandResponse5
        };
        
        await terminal.WriteColorLineAsync(response, TerminalEmulator.ColorMagenta);
        await terminal.WriteLineAsync("(You will probably be released tomorrow)");
    }
    
    private async Task HandleEscapeAttempt(Character player)
    {
        await terminal.WriteLineAsync();
        
        if (player.PrisonEscapes < 1)
        {
            await terminal.WriteLineAsync();
            await terminal.WriteColorLineAsync("You have no escape attempts left! Try again tomorrow.", TerminalEmulator.ColorRed);
            await Task.Delay(1000);
            return;
        }
        
        await terminal.WriteLineAsync();
        bool confirmed = await terminal.ConfirmAsync("Jail-Break", true);
        
        if (!confirmed)
        {
            return;
        }
        
        // Use escape attempt
        player.PrisonEscapes--;
        
        await terminal.WriteLineAsync();
        await Task.Delay(GameConfig.PrisonEscapeDelay);
        
        // 50% chance of success (Pascal: x := random(2))
        var random = new System.Random();
        bool success = random.Next(2) == 1;
        
        if (!success)
        {
            await terminal.WriteColorLineAsync("You failed!", TerminalEmulator.ColorRed);
            
            // TODO: Add news system integration
            await terminal.WriteLineAsync("The guards heard your escape attempt!");
            await Task.Delay(1000);
        }
        else
        {
            await terminal.WriteColorLineAsync("Success! You are FREE!", TerminalEmulator.ColorGreen);
            
            // TODO: Add news system integration
            // TODO: Inform king of escape
            // TODO: Inform other nodes
            
            await terminal.WriteLineAsync();
            await Task.Delay(1000);
            
            // Free the player
            player.HP = player.MaxHP;
            player.DaysInPrison = 0;
            
            await terminal.WriteLineAsync("You have successfully escaped from prison!");
            await terminal.WriteLineAsync("You are now free to return to your adventures!");
            
            return; // Exit prison
        }
    }
    
    private async Task HandleListPrisoners(Character player)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync();
        await terminal.WriteColorLineAsync("Prisoners", TerminalEmulator.ColorWhite);
        await terminal.WriteColorLineAsync("=========", TerminalEmulator.ColorWhite);
        
        // List other prisoners
        var prisoners = await GetOtherPrisoners(player);
        
        if (prisoners.Count == 0)
        {
            await terminal.WriteColorLineAsync("You are the only prisoner here right now!", TerminalEmulator.ColorCyan);
        }
        else
        {
            foreach (var prisoner in prisoners)
            {
                await ShowPrisonerInfo(prisoner);
            }
        }
        
        // Show player's remaining time
        await terminal.WriteLineAsync();
        string dayStr = player.DaysInPrison == 1 ? "day" : "days";
        await terminal.WriteLineAsync($"You have {player.DaysInPrison} {dayStr} left in prison.");
        
        await terminal.WriteLineAsync();
        await terminal.WriteAsync("Press any key to continue...");
        await terminal.GetCharAsync();
    }
    
    private async Task<List<Character>> GetOtherPrisoners(Character currentPlayer)
    {
        var prisoners = new List<Character>();
        
        // TODO: This would query the actual player/NPC database
        // For now, return empty list as placeholder
        
        return prisoners;
    }
    
    private async Task ShowPrisonerInfo(Character prisoner)
    {
        await terminal.WriteColorAsync(prisoner.DisplayName, TerminalEmulator.ColorCyan);
        await terminal.WriteAsync($" the {GetRaceDisplay(prisoner.Race)}");
        
        // Show if online/offline/dead
        if (await IsPlayerOnline(prisoner))
        {
            await terminal.WriteColorAsync(" (awake)", TerminalEmulator.ColorGreen);
        }
        else if (prisoner.HP < 1)
        {
            await terminal.WriteColorAsync(" (dead)", TerminalEmulator.ColorRed);
        }
        else
        {
            await terminal.WriteAsync(" (sleeping)");
        }
        
        // Show days left
        int daysLeft = prisoner.DaysInPrison > 0 ? prisoner.DaysInPrison : 1;
        string dayStr = daysLeft == 1 ? "day" : "days";
        await terminal.WriteLineAsync($" ({daysLeft} {dayStr} left)");
    }
    
    private string GetRaceDisplay(CharacterRace race)
    {
        return race.ToString();
    }
    
    private async Task<bool> IsPlayerOnline(Character player)
    {
        // TODO: Implement online player checking
        return false;
    }
    
    public new async Task<List<string>> GetLocationCommands(Character player)
    {
        var commands = new List<string>
        {
            "? - Show menu",
            "W - Who else is here",
            "D - Demand to be released",
            "M - Send message",
            "N - Check new mail",
            "O - Try to open cell door",
            "E - Attempt escape",
            "S - Show status", 
            "Q - Quit game"
        };
        
        return commands;
    }
    
    public new async Task<bool> CanEnterLocation(Character player)
    {
        // Can only enter if actually imprisoned
        return player.DaysInPrison > 0;
    }
    
    public new async Task<string> GetLocationStatus(Character player)
    {
        int daysLeft = player.DaysInPrison;
        string dayStr = daysLeft == 1 ? "day" : "days";
        return $"Imprisoned - {daysLeft} {dayStr} remaining, {player.PrisonEscapes} escape attempts left";
    }
} 
