using UsurperRemake.Utils;
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

/// <summary>
/// Prison Walk Location - Players can attempt to free prisoners from outside
/// Based on PRISONF.PAS from the original Usurper Pascal implementation
/// Provides prison breaking mechanics, guard combat, and prisoner liberation
/// </summary>
public partial class PrisonWalkLocation : BaseLocation
{
    private readonly GameEngine gameEngine;
    private readonly TerminalEmulator terminal;
    private bool refreshMenu = true;
    
    public PrisonWalkLocation(GameEngine engine, TerminalEmulator term) : base("prisonwalk")
    {
        gameEngine = engine ?? throw new System.ArgumentNullException(nameof(engine));
        terminal = term ?? throw new System.ArgumentNullException(nameof(term));
        
        SetLocationProperties();
    }
    
    // Add parameterless constructor for compatibility
    public PrisonWalkLocation() : base("prison_walk")
    {
        gameEngine = GameEngine.Instance;
        terminal = GameEngine.Instance.Terminal;
        SetLocationProperties();
    }
    
    private void SetLocationProperties()
    {
        LocationId = (int)GameLocation.Prison;
        LocationName = "Prison Walking Area";
        LocationDescription = "A small courtyard where prisoners can exercise";
        AllowedClasses = new HashSet<CharacterClass>();
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
        
        // Cannot enter if player is imprisoned
        if (player.DaysInPrison > 0)
        {
            await terminal.WriteLineAsync("You cannot visit the prison while you are imprisoned!");
            await terminal.WriteLineAsync("You must serve your sentence first.");
            await Task.Delay(1000);
            return false;
        }
        
        refreshMenu = true;
        await ShowPrisonWalkInterface(player);
        return true;
    }
    
    private async Task ShowPrisonWalkInterface(Character player)
    {
        char choice = '?';
        
        while (choice != 'R')
        {
            // Update location status if needed
            await UpdateLocationStatus(player);
            
            // Display menu
            await DisplayPrisonWalkMenu(player, true, true);
            
            // Get user input
            choice = await terminal.GetCharAsync();
            choice = char.ToUpper(choice);
            
            // Process user choice
            await ProcessPrisonWalkChoice(player, choice);
        }
        
        // Return message
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("You leave the depressing prison grounds.");
        await terminal.WriteLineAsync();
    }
    
    private async Task UpdateLocationStatus(Character player)
    {
        // This would typically update the online player location
        // For now, just ensure the location is set correctly
        refreshMenu = true;
    }
    
    private async Task DisplayPrisonWalkMenu(Character player, bool force, bool isShort)
    {
        if (isShort)
        {
            if (!player.Expert)
            {
                if (refreshMenu && player.AutoMenu)
                {
                    refreshMenu = false;
                    await ShowPrisonWalkMenuFull();
                }
                
                await terminal.WriteLineAsync();
                await terminal.WriteAsync("Prison walk (");
                await terminal.WriteColorAsync("?", TerminalEmulator.ColorYellow);
                await terminal.WriteAsync(" for menu) :");
            }
            else
            {
                await terminal.WriteLineAsync();
                await terminal.WriteAsync("Prison walk (P,F,S,R,?) :");
            }
        }
        else
        {
            if (!player.Expert || force)
            {
                await ShowPrisonWalkMenuFull();
            }
        }
    }
    
    private async Task ShowPrisonWalkMenuFull()
    {
        await terminal.ClearScreenAsync();
        await terminal.WriteLineAsync();
        
        await terminal.WriteColorLineAsync("Outside the Royal Prison", TerminalEmulator.ColorWhite);
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("You walk along side the long stretch of cells.");
        await terminal.WriteLineAsync("From the dark pits You can hear the screams from the tortured souls");
        await terminal.WriteLineAsync("deep in the dungeons. The torture masters must be having a great time.");
        await terminal.WriteLineAsync();
        
        // Menu options
        await terminal.WriteLineAsync("(P)risoners");
        await terminal.WriteLineAsync("(F)ree a prisoner");
        await terminal.WriteLineAsync("(S)tatus");
        await terminal.WriteLineAsync("(R)eturn");
    }
    
    private async Task ProcessPrisonWalkChoice(Character player, char choice)
    {
        switch (choice)
        {
            case '?':
                await HandleMenuDisplay(player);
                break;
            case 'S':
                await HandleStatusDisplay(player);
                break;
            case 'P':
                await HandleListPrisoners(player);
                break;
            case 'F':
                await HandleFreePrisoner(player);
                break;
            case 'R':
                // Return - handled by main loop
                break;
            default:
                // Invalid choice, do nothing
                break;
        }
    }
    
    private async Task HandleMenuDisplay(Character player)
    {
        if (player.Expert)
            await DisplayPrisonWalkMenu(player, true, false);
        else
            await DisplayPrisonWalkMenu(player, false, false);
    }
    
    private async Task HandleStatusDisplay(Character player)
    {
        await ShowCharacterStatus(player);
    }
    
    private async Task ShowCharacterStatus(Character player)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("=== CHARACTER STATUS ===");
        await terminal.WriteLineAsync($"Name: {player.DisplayName}");
        await terminal.WriteLineAsync($"Level: {player.Level}");
        await terminal.WriteLineAsync($"Health: {player.HP}/{player.MaxHP}");
        await terminal.WriteLineAsync($"Gold: {player.Gold:N0}");
        await terminal.WriteLineAsync($"Experience: {player.Experience:N0}");
        await terminal.WriteLineAsync($"Chivalry: {player.Chivalry:N0}");
        await terminal.WriteLineAsync($"Darkness: {player.Darkness:N0}");
        await terminal.WriteLineAsync();
        await terminal.WriteAsync("Press any key to continue...");
        await terminal.GetCharAsync();
    }
    
    private async Task HandleListPrisoners(Character player)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync("You examine the Dungeons.");
        await terminal.WriteLineAsync();
        
        await ListAllPrisoners();
    }
    
    private async Task ListAllPrisoners()
    {
        await terminal.WriteColorLineAsync("Current Prisoners", TerminalEmulator.ColorWhite);
        await terminal.WriteColorLineAsync("=================", TerminalEmulator.ColorWhite);
        
        // Get list of all prisoners
        var prisoners = await GetAllPrisoners();
        
        if (prisoners.Count == 0)
        {
            await terminal.WriteColorLineAsync("The Cells are empty! (how boring!)", TerminalEmulator.ColorCyan);
        }
        else
        {
            int count = 0;
            foreach (var prisoner in prisoners)
            {
                await ShowPrisonerInfo(prisoner);
                count++;
                
                // Pause for long lists
                if (count % 10 == 0)
                {
                    bool continueList = await terminal.ConfirmAsync("Continue search", true);
                    if (!continueList) break;
                }
            }
            
            await terminal.WriteLineAsync();
            await terminal.WriteLineAsync($"There is a total of {prisoners.Count:N0} prisoners.");
        }
        
        await terminal.WriteLineAsync();
        await terminal.WriteAsync("Press any key to continue...");
        await terminal.GetCharAsync();
    }
    
    private async Task<List<Character>> GetAllPrisoners()
    {
        var prisoners = new List<Character>();
        
        // TODO: This would query the actual player/NPC database for characters with DaysInPrison > 0
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
    
    private async Task HandleFreePrisoner(Character player)
    {
        // Check if someone else is already attempting a prison break
        if (await IsPrisonBreakInProgress())
        {
            await terminal.WriteLineAsync();
            await terminal.WriteLineAsync();
            await terminal.WriteColorLineAsync("Sorry, the Prison is being infiltrated right now!", TerminalEmulator.ColorRed);
            await terminal.WriteColorLineAsync("There would be too big a risk to break in!", TerminalEmulator.ColorRed);
            await Task.Delay(1000);
            return;
        }
        
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync();
        await terminal.WriteColorLineAsync("You prepare to break in!", TerminalEmulator.ColorRed);
        await terminal.WriteLineAsync("Don't get caught! You will be jailed instantly if the guards");
        await terminal.WriteLineAsync("get you!");
        await terminal.WriteLineAsync();
        
        // Get prisoner name to free
        await terminal.WriteLineAsync("Who do you wanna set free?");
        await terminal.WriteAsync(":");
        string prisonerName = await terminal.GetStringAsync(30);
        
        if (string.IsNullOrWhiteSpace(prisonerName))
        {
            await terminal.WriteLineAsync("No prisoner name entered.");
            return;
        }
        
        // Search for prisoner
        var prisoner = await FindPrisoner(prisonerName);
        
        if (prisoner == null)
        {
            await terminal.WriteLineAsync();
            await terminal.WriteLineAsync($"Could not find prisoner '{prisonerName}' in the prison.");
            return;
        }
        
        // Confirm prisoner selection
        bool confirmed = await terminal.ConfirmAsync($"Free {prisoner.DisplayName}", false);
        if (!confirmed)
        {
            return;
        }
        
        // Attempt prison break
        await AttemptPrisonBreak(player, prisoner);
    }
    
    private async Task<bool> IsPrisonBreakInProgress()
    {
        // TODO: Check if any other player is currently on location onloc_prisonbreak
        // For now, always return false
        return false;
    }
    
    private async Task<Character> FindPrisoner(string searchName)
    {
        // TODO: Search through player and NPC databases for matching prisoner
        // For now, return null (no prisoners found)
        return null;
    }
    
    private async Task AttemptPrisonBreak(Character player, Character prisoner)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteLineAsync($"You attempt to break {prisoner.DisplayName} out of prison!");
        await terminal.WriteLineAsync();
        
        // Set location to prison break (for other systems to detect)
        // TODO: Set player location to onloc_prisonbreak
        
        // Gather prison guards for battle
        var guards = await GatherPrisonGuards();
        
        if (guards.Count == 0)
        {
            await terminal.WriteLineAsync("No guards responded! The prison break succeeds easily!");
            await FreePrisonerSuccessfully(player, prisoner);
            return;
        }
        
        await terminal.WriteLineAsync($"{guards.Count} prison guards respond to the alarm!");
        await terminal.WriteLineAsync();
        
        // Battle with guards
        bool combatResult = await BattlePrisonGuards(player, guards);
        
        if (combatResult)
        {
            // Player won - successful prison break
            await FreePrisonerSuccessfully(player, prisoner);
        }
        else
        {
            // Player lost or surrendered - get imprisoned
            await HandlePrisonBreakFailure(player, prisoner);
        }
    }
    
    private async Task<List<Character>> GatherPrisonGuards()
    {
        var guards = new List<Character>();
        
        // TODO: Implement gathering of royal guards for combat
        // This would check the king's guard roster and create opponents
        // For now, return empty list
        
        return guards;
    }
    
    private async Task<bool> BattlePrisonGuards(Character player, List<Character> guards)
    {
        await terminal.WriteLineAsync("=== PRISON GUARD BATTLE ===");
        await terminal.WriteLineAsync("You must fight the prison guards to free the prisoner!");
        await terminal.WriteLineAsync();
        
        // TODO: Implement actual combat system integration
        // For now, simulate with random chance
        var random = new System.Random();
        bool playerWins = random.Next(2) == 1; // 50% chance
        
        if (playerWins)
        {
            await terminal.WriteColorLineAsync("You defeat the prison guards!", TerminalEmulator.ColorGreen);
            return true;
        }
        else
        {
            await terminal.WriteColorLineAsync("The prison guards overpower you!", TerminalEmulator.ColorRed);
            
            // Check if player surrenders
            bool surrender = await terminal.ConfirmAsync("Surrender to avoid injury", true);
            
            if (surrender)
            {
                await terminal.WriteColorLineAsync("YOU COWARD!", TerminalEmulator.ColorRed);
                await terminal.WriteColorLineAsync("You are dragged to a Cell where you await your sentence.", TerminalEmulator.ColorRed);
            }
            
            return false;
        }
    }
    
    private async Task FreePrisonerSuccessfully(Character player, Character prisoner)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteColorLineAsync("SUCCESS! The prisoner has been freed!", TerminalEmulator.ColorGreen);
        await terminal.WriteLineAsync();
        
        // Free the prisoner
        prisoner.DaysInPrison = 0;
        prisoner.HP = prisoner.MaxHP; // Restore health
        
        // TODO: Save prisoner data
        // TODO: Add news entry about escape
        // TODO: Inform king about escape
        // TODO: Send mail to freed prisoner
        
        await terminal.WriteLineAsync($"{prisoner.DisplayName} is now free!");
        await terminal.WriteLineAsync("The prisoner thanks you and disappears into the night.");
        
        await Task.Delay(2000);
    }
    
    private async Task HandlePrisonBreakFailure(Character player, Character prisoner)
    {
        await terminal.WriteLineAsync();
        await terminal.WriteColorLineAsync("PRISON BREAK FAILED!", TerminalEmulator.ColorRed);
        await terminal.WriteLineAsync();
        
        // Player gets imprisoned
        player.DaysInPrison = (byte)(GameConfig.DefaultPrisonSentence + GameConfig.PrisonBreakPenalty);
        player.HP = 0; // Beaten up by guards
        
        await terminal.WriteLineAsync("You are beaten unconscious and thrown into a cell!");
        await terminal.WriteLineAsync($"You are sentenced to {player.DaysInPrison} days in prison.");
        await terminal.WriteLineAsync();
        
        // TODO: Add news entry about failed prison break
        // TODO: Inform other players
        
        await terminal.WriteLineAsync("You will wake up in your cell tomorrow...");
        await Task.Delay(2000);
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
            "P - List prisoners",
            "F - Attempt to free a prisoner",
            "S - Show status",
            "R - Return to Main Street"
        };
        
        return commands;
    }
    
    public new async Task<bool> CanEnterLocation(Character player)
    {
        // Cannot enter if player is imprisoned
        return player.DaysInPrison <= 0;
    }
    
    public new async Task<string> GetLocationStatus(Character player)
    {
        var prisoners = await GetAllPrisoners();
        return $"Outside the Royal Prison - {prisoners.Count} prisoners currently held";
    }
} 
