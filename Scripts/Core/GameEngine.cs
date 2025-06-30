using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

/// <summary>
/// Main game engine based on Pascal USURPER.PAS
/// Handles the core game loop, initialization, and game state management
/// </summary>
public partial class GameEngine : Node
{
    private static GameEngine instance;
    public static GameEngine Instance => instance;
    
    // Core game components
    private GameState gameState;
    private Character currentPlayer;
    private List<NPC> worldNPCs;
    private List<Monster> worldMonsters;
    private TerminalEmulator terminal;
    private LocationManager locationManager;
    private SaveManager saveManager;
    private DailySystemManager dailyManager;
    private CombatEngine combatEngine;
    private WorldSimulator worldSimulator;
    
    // Game configuration
    private ConfigRecord config;
    private KingRecord kingRecord;
    
    // Online system
    private List<OnlinePlayer> onlinePlayers;
    private bool multiNodeMode;
    
    public override void _Ready()
    {
        instance = this;
        GD.Print("Usurper Reborn - Initializing Game Engine...");
        
        // Initialize core systems
        InitializeGame();
        
        // Show title screen and start main menu
        ShowTitleScreen();
        MainMenu();
    }
    
    /// <summary>
    /// Initialize game systems - based on Init_Usurper procedure from Pascal
    /// </summary>
    private void InitializeGame()
    {
        GD.Print("Reading configuration...");
        ReadStartCfgValues();
        
        GD.Print("Initializing game data...");
        InitializeItems();      // From INIT.PAS Init_Items
        InitializeMonsters();   // From INIT.PAS Init_Monsters
        InitializeNPCs();       // From INIT.PAS Init_NPCs
        InitializeLevels();     // From INIT.PAS Init_Levels
        InitializeGuards();     // From INIT.PAS Init_Guards
        
        GD.Print("Setting up game state...");
        gameState = new GameState
        {
            GameDate = 1,
            LastDayRun = 0,
            PlayersOnline = 0,
            MaintenanceRunning = false
        };
        
        // Initialize core systems
        terminal = GetNode<TerminalEmulator>("Terminal");
        locationManager = new LocationManager(terminal);
        saveManager = new SaveManager();
        dailyManager = new DailySystemManager();
        combatEngine = new CombatEngine();
        worldSimulator = new WorldSimulator();
        
        // Initialize collections
        worldNPCs = new List<NPC>();
        worldMonsters = new List<Monster>();
        onlinePlayers = new List<OnlinePlayer>();
        
        GD.Print("Game engine initialized successfully!");
    }
    
    /// <summary>
    /// Show title screen - displays USURPER.ANS from Pascal
    /// </summary>
    private void ShowTitleScreen()
    {
        terminal.ClearScreen();
        terminal.ShowANSIArt("USURPER");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("USURPER - The Dungeon of Death");
        terminal.WriteLine("Reborn Edition");
        terminal.WriteLine("");
        terminal.SetColor("gray");
        terminal.WriteLine("Original by Jakob Dangarden");
        terminal.WriteLine("Reborn by AI Assistant");
        terminal.WriteLine("");
        terminal.WriteLine("Press any key to continue...");
        terminal.WaitForKey();
    }
    
    /// <summary>
    /// Main menu - based on Town_Menu procedure from Pascal
    /// </summary>
    private void MainMenu()
    {
        bool done = false;
        
        while (!done)
        {
            terminal.ClearScreen();
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("USURPER - The Dungeon of Death");
            terminal.SetColor("yellow");
            terminal.WriteLine("═══════════════════════════════");
            terminal.WriteLine("");
            terminal.SetColor("white");
            
            var options = new List<MenuOption>
            {
                new MenuOption { Key = "E", Text = "Enter the Game", Action = async () => await EnterGame() },
                new MenuOption { Key = "I", Text = "Instructions", Action = async () => await ShowInstructions() },
                new MenuOption { Key = "L", Text = "List Players", Action = async () => await ListPlayers() },
                new MenuOption { Key = "T", Text = "Teams", Action = async () => await ShowTeams() },
                new MenuOption { Key = "N", Text = "Yesterday's News", Action = async () => await ShowNews() },
                new MenuOption { Key = "S", Text = "Game Settings", Action = async () => await ShowGameSettings() },
                new MenuOption { Key = "Q", Text = "Quit", Action = async () => { done = true; } }
            };
            
            var choice = await terminal.GetMenuChoice(options);
            if (choice >= 0 && choice < options.Count)
            {
                await options[choice].Action();
            }
        }
    }
    
    /// <summary>
    /// Enter the game - handles player login/creation
    /// </summary>
    private async Task EnterGame()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("Enter the Game");
        terminal.WriteLine("══════════════");
        terminal.WriteLine("");
        
        var playerName = await terminal.GetInput("Enter your name: ");
        
        if (string.IsNullOrWhiteSpace(playerName))
        {
            terminal.WriteLine("Invalid name!", "red");
            await Task.Delay(2000);
            return;
        }
        
        // Try to load existing player
        currentPlayer = saveManager.LoadPlayer(playerName);
        
        if (currentPlayer == null)
        {
            // New player - create character
            currentPlayer = await CreateNewPlayer(playerName);
            if (currentPlayer == null)
            {
                return; // Player cancelled creation
            }
        }
        
        // Check if player is allowed to play
        if (!currentPlayer.Allowed)
        {
            terminal.WriteLine("You are not allowed to play today!", "red");
            await Task.Delay(2000);
            return;
        }
        
        // Check daily limits
        if (!CheckDailyLimits())
        {
            terminal.WriteLine($"You have used all your turns for today! ({currentPlayer.TurnsLeft} left)", "red");
            await Task.Delay(2000);
            return;
        }
        
        // Check if player is in prison
        if (currentPlayer.DaysInPrison > 0)
        {
            await HandlePrison();
            return;
        }
        
        // Check if player is dead
        if (!currentPlayer.IsAlive)
        {
            await HandleDeath();
            return;
        }
        
        // Enter main game using location system
        await locationManager.EnterLocation(GameLocation.MainStreet, currentPlayer);
    }
    
    /// <summary>
    /// Create new player using comprehensive character creation system
    /// Based on Pascal USERHUNC.PAS implementation
    /// </summary>
    private async Task<Character> CreateNewPlayer(string playerName)
    {
        // Create temporary character object with just the real name
        var tempCharacter = new Character
        {
            Name1 = playerName,
            Name2 = playerName, // Will be changed during creation
            AI = CharacterAI.Human,
            Allowed = false // Will be set to true after successful creation
        };
        
        try
        {
            // Use the CharacterCreationSystem for full Pascal-compatible creation
            var creationSystem = new CharacterCreationSystem(terminal);
            var newCharacter = await creationSystem.CreateNewCharacter(playerName);
            
            if (newCharacter == null)
            {
                // Character creation was aborted
                terminal.WriteLine("");
                terminal.WriteLine("Character creation was cancelled.", "yellow");
                terminal.WriteLine("You must create a character to play Usurper.", "white");
                
                var retry = await terminal.GetInputAsync("Would you like to try again? (Y/N): ");
                if (retry.ToUpper() == "Y")
                {
                    return await CreateNewPlayer(playerName); // Retry
                }
                
                return null; // User chose not to retry
            }
            
            // Character creation successful - save the new player
            saveManager.SavePlayer(newCharacter);
            
            terminal.WriteLine("");
            terminal.WriteLine("Character successfully saved to the realm!", "bright_green");
            await Task.Delay(1500);
            
            return newCharacter;
        }
        catch (OperationCanceledException)
        {
            terminal.WriteLine("Character creation aborted by user.", "red");
            return null;
        }
        catch (Exception ex)
        {
            terminal.WriteLine($"An error occurred during character creation: {ex.Message}", "red");
            GD.PrintErr($"Character creation error: {ex}");
            
            terminal.WriteLine("Please try again.", "yellow");
            var retry = await terminal.GetInputAsync("Would you like to try again? (Y/N): ");
            if (retry.ToUpper() == "Y")
            {
                return await CreateNewPlayer(playerName); // Retry
            }
            
            return null;
        }
    }
    
    /// <summary>
    /// Get current location description for compatibility
    /// </summary>
    public string GetCurrentLocationDescription()
    {
        return locationManager?.GetCurrentLocationDescription() ?? "Unknown location";
    }
    
    /// <summary>
    /// Update status line with player info
    /// </summary>
    private void UpdateStatusLine()
    {
        var statusText = $"[{currentPlayer.DisplayName}] " +
                        $"Level: {currentPlayer.Level} " +
                        $"HP: {currentPlayer.HP}/{currentPlayer.MaxHP} " +
                        $"Gold: {currentPlayer.Gold} " +
                        $"Turns: {currentPlayer.TurnsLeft}";
        
        terminal.SetStatusLine(statusText);
    }
    
    /// <summary>
    /// Get NPCs in a specific location (for location system compatibility)
    /// </summary>
    public List<NPC> GetNPCsInLocation(GameLocation locationId)
    {
        return locationManager?.GetNPCsInLocation(locationId) ?? new List<NPC>();
    }
    
    /// <summary>
    /// Add NPC to a specific location
    /// </summary>
    public void AddNPCToLocation(GameLocation locationId, NPC npc)
    {
        locationManager?.AddNPCToLocation(locationId, npc);
    }
    
    /// <summary>
    /// Get current player for location system
    /// </summary>
    public Character GetCurrentPlayer()
    {
        return currentPlayer;
    }
    
    /// <summary>
    /// Check daily limits - based on CHECK_ALLOWED from Pascal
    /// </summary>
    private bool CheckDailyLimits()
    {
        // Check if it's a new day
        if (dailyManager.IsNewDay())
        {
            dailyManager.RunDailyMaintenance(currentPlayer);
        }
        
        return currentPlayer.TurnsLeft > 0;
    }
    
    /// <summary>
    /// Handle player death - based on death handling from Pascal
    /// </summary>
    private async Task HandleDeath()
    {
        terminal.ClearScreen();
        terminal.ShowANSIArt("DEATH");
        terminal.SetColor("red");
        terminal.WriteLine("You are DEAD!");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("Your reign of terror has come to an end...");
        terminal.WriteLine("");
        
        // Check resurrections
        if (currentPlayer.Resurrections > 0)
        {
            terminal.WriteLine($"You have {currentPlayer.Resurrections} resurrections left.");
            var resurrect = await terminal.GetInput("Do you want to resurrect? (Y/N): ");
            
            if (resurrect.ToUpper() == "Y")
            {
                currentPlayer.Resurrections--;
                currentPlayer.HP = currentPlayer.MaxHP / 2;
                currentPlayer.Gold /= 2; // Lose half gold
                
                terminal.WriteLine("You have been resurrected!", "green");
                await Task.Delay(2000);
                return;
            }
        }
        
        // Player chooses to stay dead or out of resurrections
        terminal.WriteLine("Your adventure ends here...", "gray");
        await Task.Delay(3000);
        
        // Add to hall of fame if high level
        if (currentPlayer.Level >= 10)
        {
            // TODO: Add to fame file
        }
        
        // Delete character or mark as dead
        currentPlayer.Deleted = true;
        saveManager.SavePlayer(currentPlayer);
    }
    
    /// <summary>
    /// Handle prison - based on prison handling from Pascal
    /// </summary>
    private async Task HandlePrison()
    {
        terminal.ClearScreen();
        terminal.SetColor("gray");
        terminal.WriteLine("You are in PRISON!");
        terminal.WriteLine("═══════════════════");
        terminal.WriteLine("");
        terminal.WriteLine($"Days remaining: {currentPlayer.DaysInPrison}");
        terminal.WriteLine("");
        terminal.WriteLine("1. Wait it out");
        terminal.WriteLine("2. Attempt escape");
        terminal.WriteLine("3. Quit");
        
        var choice = await terminal.GetMenuChoice();
        
        switch (choice)
        {
            case 0: // Wait
                currentPlayer.DaysInPrison--;
                terminal.WriteLine("You wait patiently...", "gray");
                await Task.Delay(2000);
                break;
                
            case 1: // Escape
                if (currentPlayer.PrisonEscapes > 0)
                {
                    await AttemptPrisonEscape();
                }
                else
                {
                    terminal.WriteLine("You have no escape attempts left!", "red");
                    await Task.Delay(2000);
                }
                break;
                
            case 2: // Quit
                await QuitGame();
                break;
        }
    }
    
    /// <summary>
    /// Attempt prison escape
    /// </summary>
    private async Task AttemptPrisonEscape()
    {
        currentPlayer.PrisonEscapes--;
        
        terminal.WriteLine("You attempt to escape...", "yellow");
        await Task.Delay(1000);
        
        // Escape chance based on stats
        var escapeChance = (currentPlayer.Dexterity + currentPlayer.Agility) / 4;
        var roll = GD.RandRange(1, 100);
        
        if (roll <= escapeChance)
        {
            terminal.WriteLine("You successfully escape!", "green");
            currentPlayer.DaysInPrison = 0;
            currentPlayer.Location = 1; // Return to main street
        }
        else
        {
            terminal.WriteLine("You are caught trying to escape!", "red");
            currentPlayer.DaysInPrison += 2; // Extra penalty
        }
        
        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Quit game and save
    /// </summary>
    private async Task QuitGame()
    {
        terminal.WriteLine("Saving game...", "yellow");
        
        if (currentPlayer != null)
        {
            saveManager.SavePlayer(currentPlayer);
        }
        
        terminal.WriteLine("Goodbye!", "green");
        await Task.Delay(1000);
        
        GetTree().Quit();
    }
    
    // Helper methods
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
    
    /// <summary>
    /// Navigate to a specific location using the location manager
    /// </summary>
    public async Task<bool> NavigateToLocation(GameLocation destination)
    {
        return await locationManager.NavigateTo(destination, currentPlayer);
    }
    
    // Placeholder methods for game actions
    private async Task ShowInstructions() => await ShowInfoScreen("Instructions", "Game instructions will be here...");
    private async Task ListPlayers() => await ShowInfoScreen("Player List", "Player list will be here...");
    private async Task ShowTeams() => await ShowInfoScreen("Teams", "Team information will be here...");
    private async Task ShowNews() => await ShowInfoScreen("News", "Yesterday's news will be here...");
    private async Task ShowGameSettings() => await ShowInfoScreen("Game Settings", "Game settings will be here...");
    private async Task ShowStatus() => await ShowInfoScreen("Status", $"Player: {currentPlayer?.DisplayName}\nLevel: {currentPlayer?.Level}\nHP: {currentPlayer?.HP}/{currentPlayer?.MaxHP}");
    
    private async Task ShowInfoScreen(string title, string content)
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine(title);
        terminal.SetColor("cyan");
        terminal.WriteLine(new string('═', title.Length));
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine(content);
        terminal.WriteLine("");
        terminal.WriteLine("Press any key to continue...");
        terminal.WaitForKey();
    }
    
    // Placeholder initialization methods
    private void ReadStartCfgValues() { /* Load config from file */ }
    private void InitializeItems() { /* Load items from data */ }
    private void InitializeMonsters() { /* Load monsters from data */ }
    private void InitializeNPCs() { /* Load NPCs from data */ }
    private void InitializeLevels() { /* Load level data */ }
    private void InitializeGuards() { /* Load guard data */ }
    
    // Character creation helpers (now handled by CharacterCreationSystem)
    // These methods are kept for backwards compatibility but are no longer used
    private async Task<CharacterSex> SelectSex() => CharacterSex.Male; // Legacy - use CharacterCreationSystem
    private async Task<CharacterRace> SelectRace() => CharacterRace.Human; // Legacy - use CharacterCreationSystem
    private async Task<CharacterClass> SelectClass() => CharacterClass.Warrior; // Legacy - use CharacterCreationSystem
    private void ApplyRacialBonuses(Character character) { /* Legacy - handled by CharacterCreationSystem */ }
    private void ApplyClassBonuses(Character character) { /* Legacy - handled by CharacterCreationSystem */ }
    private void SetInitialEquipment(Character character) { /* Legacy - handled by CharacterCreationSystem */ }
    private async Task ShowCharacterSummary(Character character) { /* Legacy - handled by CharacterCreationSystem */ }

    /// <summary>
    /// Run magic shop system validation tests
    /// </summary>
    public static void TestMagicShopSystem()
    {
        try
        {
            GD.Print("═══ Running Magic Shop System Tests ═══");
            MagicShopSystemValidation.RunAllTests();
            GD.Print("═══ Magic Shop Tests Complete ═══");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Magic Shop Test Error: {ex.Message}");
        }
    }
}

/// <summary>
/// Menu option for terminal menus
/// </summary>
public class MenuOption
{
    public string Key { get; set; } = "";
    public string Text { get; set; } = "";
    public Func<Task> Action { get; set; } = async () => { };
}

/// <summary>
/// Game state tracking
/// </summary>
public class GameState
{
    public int GameDate { get; set; }
    public int LastDayRun { get; set; }
    public int PlayersOnline { get; set; }
    public bool MaintenanceRunning { get; set; }
}

/// <summary>
/// Online player tracking
/// </summary>
public class OnlinePlayer
{
    public string Name { get; set; } = "";
    public string Node { get; set; } = "";
    public DateTime Arrived { get; set; }
    public string Location { get; set; } = "";
    public bool IsNPC { get; set; }
}

/// <summary>
/// Config record based on Pascal ConfigRecord
/// </summary>
public class ConfigRecord
{
    public bool MarkNPCs { get; set; } = true;
    public int LevelDiff { get; set; } = 5;
    public bool FastPlay { get; set; } = false;
    public string Anchor { get; set; } = "Anchor road";
    public bool SimulNode { get; set; } = false;
    public bool AutoMaint { get; set; } = true;
    // Add more config fields as needed
}

/// <summary>
/// King record based on Pascal KingRec
/// </summary>
public class KingRecord
{
    public string Name { get; set; } = "";
    public CharacterAI AI { get; set; } = CharacterAI.Computer;
    public CharacterSex Sex { get; set; } = CharacterSex.Male;
    public long DaysInPower { get; set; } = 0;
    public byte Tax { get; set; } = 10;
    public long Treasury { get; set; } = 50000;
    // Add more king fields as needed
} 
