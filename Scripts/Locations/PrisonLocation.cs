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
        LocationId = GameLocation.Prison;
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
            // Navigate player to Main Street properly
            throw new LocationExitException(GameLocation.MainStreet);
        }

        refreshMenu = true;
        await ShowPrisonInterface(player);
        return true;
    }
    
    private async Task ShowPrisonInterface(Character player)
    {
        char choice = '?';
        bool exitPrison = false;

        while (!exitPrison)
        {
            // Check if sentence is served (days ran out)
            if (player.DaysInPrison <= 0)
            {
                await terminal.WriteLineAsync();
                await terminal.WriteColorLineAsync("The guards open your cell door.", TerminalEmulator.ColorGreen);
                await terminal.WriteLineAsync("\"Your sentence has been served. You are free to go.\"");
                await terminal.WriteColorLineAsync("You are FREE!", TerminalEmulator.ColorGreen);
                player.CellDoorOpen = false;
                player.RescuedBy = "";
                player.HP = Math.Max(player.HP, player.MaxHP / 2); // Restore some health
                await Task.Delay(1500);
                throw new LocationExitException(GameLocation.MainStreet);
            }

            // Update location status if needed
            await UpdatePrisonStatus(player);

            // Check if player can walk out (cell door open by rescue)
            if (await CanOpenCellDoor(player))
            {
                await HandleCellDoorOpen(player);
                throw new LocationExitException(GameLocation.MainStreet);
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

            // Process user choice - returns true if player escaped/freed
            exitPrison = await ProcessPrisonChoice(player, choice);
        }
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
        await Task.CompletedTask;
        return player.CellDoorOpen;
    }
    
    private async Task HandleCellDoorOpen(Character player)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteColorLineAsync("The cell door swings open!", TerminalEmulator.ColorGreen);
        await terminal.WriteLineAsync();

        if (!string.IsNullOrEmpty(player.RescuedBy))
        {
            await terminal.WriteColorAsync(player.RescuedBy, TerminalEmulator.ColorCyan);
            await terminal.WriteLineAsync(" broke you out of prison!");
            await terminal.WriteLineAsync("You owe them your freedom!");
        }
        else
        {
            await terminal.WriteLineAsync("Someone has unlocked your cell!");
        }

        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("You walk out of your cell.");
        await terminal.WriteColorLineAsync("You are FREE!", TerminalEmulator.ColorGreen);

        // Reset player state
        player.HP = player.MaxHP;
        player.DaysInPrison = 0;
        player.CellDoorOpen = false;
        player.RescuedBy = "";

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
                if (refreshMenu)
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
                await terminal.WriteAsync("Royal Prison (W,M,N,D,O,S,E,A,Q,?) :");
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
        await terminal.WriteLineAsync("(O)pen cell door            (E)scape!");
        await terminal.WriteLineAsync("(S)tatus                    (A)ctivities - Exercise!");
        await terminal.WriteLineAsync("(Q)uit");
    }
    
    private async Task<bool> ProcessPrisonChoice(Character player, char choice)
    {
        switch (choice)
        {
            case '?':
                await HandleMenuDisplay(player);
                return false;
            case 'S':
                await HandleStatusDisplay(player);
                return false;
            case 'Q':
                return await HandleQuitConfirmation(player);
            case 'O':
                await HandleOpenCellDoor(player);
                return false;
            case 'D':
                await HandleDemandRelease(player);
                return false;
            case 'E':
                return await HandleEscapeAttempt(player);
            case 'W':
                await HandleListPrisoners(player);
                return false;
            case 'A':
                await HandleActivities(player);
                return false;
            default:
                // Invalid choice, do nothing
                return false;
        }
    }

    /// <summary>
    /// Handle prison activity selection - allows prisoners to build stats
    /// </summary>
    private async Task HandleActivities(Character player)
    {
        await terminal.ClearScreenAsync();
        await terminal.WriteColorLineAsync("═══════════════════════════════════════", TerminalEmulator.ColorCyan);
        await terminal.WriteColorLineAsync("           PRISON ACTIVITIES           ", TerminalEmulator.ColorCyan);
        await terminal.WriteColorLineAsync("═══════════════════════════════════════", TerminalEmulator.ColorCyan);
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("While imprisoned, you can pass the time");
        await terminal.WriteLineAsync("with activities that improve your body and mind.");
        await terminal.WriteLineAsync();

        var activities = PrisonActivitySystem.Instance.GetAvailableActivities();
        int i = 1;
        foreach (var activity in activities)
        {
            var info = PrisonActivitySystem.ActivityInfo[activity];
            await terminal.WriteColorAsync($"({i}) ", TerminalEmulator.ColorYellow);
            await terminal.WriteAsync($"{info.Name,-15} ");
            await terminal.WriteColorAsync($"{info.Effect}", TerminalEmulator.ColorGreen);
            await terminal.WriteLineAsync();
            await terminal.WriteColorLineAsync($"    {info.Description}", TerminalEmulator.ColorDarkGray);
            i++;
        }

        await terminal.WriteLineAsync();
        await terminal.WriteAsync("Choose activity (0 to cancel): ");
        string input = await terminal.ReadLineAsync();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= activities.Count)
        {
            var selectedActivity = activities[choice - 1];
            string result = await PrisonActivitySystem.Instance.PerformActivity(player, selectedActivity);

            await terminal.WriteLineAsync();
            await terminal.WriteColorLineAsync(result, TerminalEmulator.ColorGreen);
            await terminal.WriteLineAsync();
            await terminal.WriteAsync("Press any key to continue...");
            await terminal.GetCharAsync();
        }

        refreshMenu = true;
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
    
    private async Task<bool> HandleQuitConfirmation(Character player)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync();

        bool confirmed = await terminal.ConfirmAsync("QUIT game and rest for the night", false);
        if (!confirmed)
        {
            // Don't quit, continue prison loop
            return false;
        }

        // Player is logging out - display sleep message
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("You cover yourself with some hay and try to get some sleep.");
        await terminal.WriteLineAsync("It will be a long and cold night with the rats...");
        await terminal.WriteLineAsync();
        await Task.Delay(1500);

        // Save and quit - throw game exit exception
        throw new GameExitException("Player logging out from prison");
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
    
    private async Task<bool> HandleEscapeAttempt(Character player)
    {
        await terminal.WriteLineAsync();

        if (player.PrisonEscapes < 1)
        {
            await terminal.WriteLineAsync();
            await terminal.WriteColorLineAsync("You have no escape attempts left! Try again tomorrow.", TerminalEmulator.ColorRed);
            await Task.Delay(1000);
            return false;
        }

        await terminal.WriteLineAsync();
        bool confirmed = await terminal.ConfirmAsync("Jail-Break", true);

        if (!confirmed)
        {
            return false;
        }

        // Use escape attempt
        player.PrisonEscapes--;

        await terminal.WriteLineAsync();
        await Task.Delay(GameConfig.PrisonEscapeDelay);

        // Escape chance based on dexterity and level (better than 50/50)
        var random = new System.Random();
        int escapeChance = 40 + (int)(player.Dexterity / 3) + (player.Level / 2);
        escapeChance = Math.Clamp(escapeChance, 30, 80); // 30-80% chance
        bool success = random.Next(100) < escapeChance;

        if (!success)
        {
            await terminal.WriteColorLineAsync("You failed!", TerminalEmulator.ColorRed);

            // Generate news about failed escape
            NewsSystem.Instance.Newsy(true, $"{player.DisplayName} failed to escape from the Royal Prison!");

            await terminal.WriteLineAsync("The guards heard your escape attempt!");
            await terminal.WriteLineAsync("Your sentence has been extended by 1 day!");
            player.DaysInPrison++;
            await Task.Delay(1500);
            return false;
        }
        else
        {
            await terminal.WriteColorLineAsync("Success! You are FREE!", TerminalEmulator.ColorGreen);

            // Generate news about successful escape
            NewsSystem.Instance.Newsy(true, $"{player.DisplayName} has escaped from the Royal Prison!");

            await terminal.WriteLineAsync();
            await Task.Delay(1000);

            // Free the player
            player.HP = player.MaxHP;
            player.DaysInPrison = 0;
            player.CellDoorOpen = false;

            await terminal.WriteLineAsync("You have successfully escaped from prison!");
            await terminal.WriteLineAsync("You are now free to return to your adventures!");
            await Task.Delay(1500);

            // Navigate to Main Street
            throw new LocationExitException(GameLocation.MainStreet);
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
        await Task.CompletedTask;

        // Get NPC prisoners from the NPCSpawnSystem
        var npcPrisoners = UsurperRemake.Systems.NPCSpawnSystem.Instance.GetPrisoners();
        foreach (var npc in npcPrisoners)
        {
            prisoners.Add(npc);
        }

        // Could also add player prisoners here if multiplayer is enabled

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
