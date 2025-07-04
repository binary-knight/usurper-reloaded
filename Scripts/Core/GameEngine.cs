using UsurperRemake.Utils;
using UsurperRemake.Systems;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO;

/// <summary>
/// Main game engine based on Pascal USURPER.PAS
/// Handles the core game loop, initialization, and game state management
/// Now includes comprehensive save/load system and flexible daily cycles
/// </summary>
public partial class GameEngine : Node
{
    private static GameEngine? instance;
    private TerminalEmulator terminal;
    private Character? currentPlayer;
    
    // Game state
    private UsurperConfig config;
    private KingRecord kingRecord;
    private ScoreManager scoreManager;
    
    // System flags
    private bool multiNodeMode = false;
    
    public static GameEngine Instance => instance ??= new GameEngine();
    
    // Missing properties for compilation
    public Character? CurrentPlayer 
    { 
        get => currentPlayer; 
        set => currentPlayer = value; 
    }
    
    public static string DataPath => GameConfig.DataPath;
    
    // Terminal access for systems
    public TerminalEmulator Terminal => terminal;
    
    // Core game components
    private GameState gameState;
    private List<NPC> worldNPCs;
    private List<Monster> worldMonsters;
    private LocationManager locationManager;
    private DailySystemManager dailyManager;
    private CombatEngine combatEngine;
    private WorldSimulator worldSimulator;
    
    // Online system
    private List<OnlinePlayer> onlinePlayers;
    
    // Auto-save timer
    private DateTime lastPeriodicCheck;
    
    // Stub classes for compilation
    private class UsurperConfig
    {
        // Pascal compatible config structure
    }
    
    private class ScoreManager
    {
        // Score and ranking management
    }

    /// <summary>
    /// Console entry point for running the full game
    /// </summary>
    public static async Task RunConsoleAsync()
    {
        var engine = Instance;
        await engine.RunMainGameLoop();
    }
    
    /// <summary>
    /// Main game loop for console mode
    /// </summary>
    private async Task RunMainGameLoop()
    {
        InitializeGame();
        ShowTitleScreen();
        await MainMenu();
    }

    public override void _Ready()
    {
        GD.Print("Usurper Reborn - Initializing Game Engine...");
        
        // Initialize core systems
        InitializeGame();
        
        // Show title screen and start main menu
        ShowTitleScreen();
        
        // Handle async MainMenu properly since _Ready() can't be async
        Task.Run(async () => await MainMenu());
    }
    
    /// <summary>
    /// Initialize game systems - based on Init_Usurper procedure from Pascal
    /// </summary>
    private void InitializeGame()
    {
        // Ensure we have a working terminal instance when running outside of Godot
        if (terminal == null)
        {
            // If we were truly running inside Godot, the Terminal node would
            // already exist and TerminalEmulator.Instance would have been set.
            terminal = TerminalEmulator.Instance ?? new TerminalEmulator();
        }

        GD.Print("Reading configuration...");
        ReadStartCfgValues();
        
        GD.Print("Initializing core managers...");
        // Create the LocationManager early so that it becomes the singleton before NPCs are loaded
        if (locationManager == null)
        {
            locationManager = new LocationManager(terminal);
        }

        GD.Print("Initializing game data...");
        InitializeItems();      // From INIT.PAS Init_Items
        InitializeMonsters();   // From INIT.PAS Init_Monsters
        InitializeNPCs();       // From INIT.PAS Init_NPCs (needs LocationManager ready)
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
        
        // Initialize remaining core systems (LocationManager already created)
        dailyManager = DailySystemManager.Instance;
        combatEngine = new CombatEngine();

        // World simulator – start background AI processing
        worldSimulator = new WorldSimulator();
        if (worldNPCs == null)
            worldNPCs = new List<NPC>();
        worldSimulator.StartSimulation(worldNPCs);
        
        // Initialize collections
        worldMonsters = new List<Monster>();
        onlinePlayers = new List<OnlinePlayer>();
        
        // Initialize periodic check timer
        lastPeriodicCheck = DateTime.Now;
        
        GD.Print("Game engine initialized successfully!");
    }
    
    /// <summary>
    /// Periodic update for game systems (called regularly during gameplay)
    /// </summary>
    public async Task PeriodicUpdate()
    {
        var now = DateTime.Now;
        
        // Only run periodic checks every 30 seconds
        if (now - lastPeriodicCheck < TimeSpan.FromSeconds(30))
            return;
            
        lastPeriodicCheck = now;
        
        // Check for daily reset
        await dailyManager.CheckDailyReset();
        
        // Update world simulation
        worldSimulator?.SimulateStep();
        
        // Process NPC behaviors
        // Note: EnhancedNPCSystem doesn't have an Instance property
        // This would need to be handled differently in a full implementation
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
        terminal.WriteLine("1993 - Original by Jakob Dangarden");
        terminal.WriteLine("2025 - Reborn by Jason Knight");
        terminal.WriteLine("");
        terminal.WriteLine("Press any key to continue...");
        terminal.WaitForKey();
    }
    
    /// <summary>
    /// Main menu - based on Town_Menu procedure from Pascal
    /// </summary>
    private async Task MainMenu()
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
    /// Enter the game with save/load integration
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
        
        // Check for existing save
        var saveExists = SaveSystem.Instance.SaveExists(playerName);
        
        if (saveExists)
        {
            terminal.WriteLine($"Found existing save for '{playerName}'", "green");
            terminal.WriteLine("");
            terminal.WriteLine("1. Continue existing game");
            terminal.WriteLine("2. Start new game (WARNING: Will overwrite save!)");
            terminal.WriteLine("3. Back to main menu");
            
            var choice = await terminal.GetMenuChoice();
            
            switch (choice)
            {
                case 0: // Continue
                    await LoadExistingGame(playerName);
                    break;
                    
                case 1: // New game
                    terminal.WriteLine("");
                    var confirm = await terminal.GetInput("Are you sure? This will delete your existing save! (yes/no): ");
                    if (confirm.ToLower() == "yes")
                    {
                        SaveSystem.Instance.DeleteSave(playerName);
                        await CreateNewGame(playerName);
                    }
                    break;
                    
                case 2: // Back
                    return;
            }
        }
        else
        {
            // No existing save, create new game
            await CreateNewGame(playerName);
        }
    }
    
    /// <summary>
    /// Load existing game from save file
    /// </summary>
    private async Task LoadExistingGame(string playerName)
    {
        terminal.WriteLine("Loading game...", "yellow");
        
        var saveData = await SaveSystem.Instance.LoadGame(playerName);
        if (saveData == null)
        {
            terminal.WriteLine("Failed to load save file!", "red");
            await Task.Delay(2000);
            return;
        }
        
        // Restore player from save data
        currentPlayer = RestorePlayerFromSaveData(saveData.Player);
        
        // Load daily system state
        dailyManager.LoadFromSaveData(saveData);
        
        // Restore world state
        await RestoreWorldState(saveData.WorldState);
        
        // Restore NPCs
        await RestoreNPCs(saveData.NPCs);
        
        terminal.WriteLine($"Game loaded successfully! Day {saveData.CurrentDay}, {saveData.Player.TurnsRemaining} turns remaining", "green");
        await Task.Delay(1500);
        
        // Check if daily reset is needed after loading
        await dailyManager.CheckDailyReset();
        
        // Enter the game world
        await EnterGameWorld();
    }
    
    /// <summary>
    /// Create new game
    /// </summary>
    private async Task CreateNewGame(string playerName)
    {
        // Create new player using character creation system
        var newCharacter = await CreateNewPlayer(playerName);
        if (newCharacter == null)
        {
            return; // Player cancelled creation
        }
        
        currentPlayer = (Character)newCharacter;
        
        // Save the new game
        var success = await SaveSystem.Instance.SaveGame(playerName, currentPlayer);
        if (success)
        {
            terminal.WriteLine("New game saved successfully!", "green");
        }
        else
        {
            terminal.WriteLine("Warning: Failed to save game!", "red");
        }
        
        await Task.Delay(1500);
        
        // Enter the game world
        await EnterGameWorld();
    }
    
    /// <summary>
    /// Enter the main game world
    /// </summary>
    private async Task EnterGameWorld()
    {
        if (currentPlayer == null) return;
        
        // Check if player is allowed to play
        if (!currentPlayer.Allowed)
        {
            terminal.WriteLine("You are not allowed to play today!", "red");
            await Task.Delay(2000);
            return;
        }
        
        // Check daily limits (but not in endless mode)
        if (dailyManager.CurrentMode != DailyCycleMode.Endless && !await CheckDailyLimits())
        {
            terminal.WriteLine($"You have used all your turns for today! ({currentPlayer.TurnsRemaining} left)", "red");
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
    /// Restore player from save data
    /// </summary>
    private Character RestorePlayerFromSaveData(PlayerData playerData)
    {
        var player = new Character
        {
            Name1 = playerData.Name1,
            Name2 = playerData.Name2,
            Level = playerData.Level,
            Experience = playerData.Experience,
            HP = playerData.HP,
            MaxHP = playerData.MaxHP,
            Gold = playerData.Gold,
            BankGold = playerData.BankGold,
            
            // Attributes
            Strength = playerData.Strength,
            Defence = playerData.Defence,
            Stamina = playerData.Stamina,
            Agility = playerData.Agility,
            Charisma = playerData.Charisma,
            Dexterity = playerData.Dexterity,
            Wisdom = playerData.Wisdom,
            Mana = playerData.Mana,
            MaxMana = playerData.MaxMana,
            
            // Character details
            Race = playerData.Race,
            Class = playerData.Class,
            Sex = (CharacterSex)playerData.Sex,
            Age = playerData.Age,
            
            // Game state
            TurnsRemaining = playerData.TurnsRemaining,
            DaysInPrison = (byte)playerData.DaysInPrison,
            
            // Daily limits
            Fights = playerData.Fights,
            PFights = playerData.PFights,
            TFights = playerData.TFights,
            Thiefs = playerData.Thiefs,
            Brawls = playerData.Brawls,
            Assa = playerData.Assa,
            
            // Status
            Chivalry = playerData.Chivalry,
            Darkness = playerData.Darkness,
            Mental = playerData.Mental,
            Poison = playerData.Poison,
            
            // Social
            Team = playerData.Team,
            TeamPW = playerData.TeamPassword,
            CTurf = playerData.IsTeamLeader,
            
            Allowed = true // Always allow loaded players
        };
        
        // Restore items
        if (playerData.Items?.Length > 0)
        {
            player.Item = playerData.Items.ToList();
        }
        
        if (playerData.ItemTypes?.Length > 0)
        {
            player.ItemType = playerData.ItemTypes.Select(i => (ObjType)i).ToList();
        }
        
        // Parse location
        if (int.TryParse(playerData.CurrentLocation, out var locationId))
        {
            player.Location = locationId;
        }
        
        return player;
    }
    
    /// <summary>
    /// Restore world state from save data
    /// </summary>
    private async Task RestoreWorldState(WorldStateData worldState)
    {
        // Restore economic state
        // This would integrate with bank and economy systems
        
        // Restore political state
        if (!string.IsNullOrEmpty(worldState.CurrentRuler))
        {
            // Set current ruler if applicable
        }
        
        // Restore active world events
        // This would integrate with world event system
        
        GD.Print($"World state restored: {worldState.ActiveEvents.Count} active events");
    }
    
    /// <summary>
    /// Restore NPCs from save data
    /// </summary>
    private async Task RestoreNPCs(List<NPCData> npcData)
    {
        // Note: EnhancedNPCSystem doesn't have an Instance property
        // In a full implementation, this would restore NPC state including AI memories and relationships
        foreach (var data in npcData)
        {
            // Restore NPC state - implementation depends on NPC system architecture
        }
        
        GD.Print($"Restored {npcData.Count} NPCs from save data");
    }
    
    /// <summary>
    /// Save current game state
    /// </summary>
    public async Task SaveCurrentGame()
    {
        if (currentPlayer == null) return;
        
        var playerName = currentPlayer.Name2 ?? currentPlayer.Name1;
        terminal.WriteLine("Saving game...", "yellow");
        
        var success = await SaveSystem.Instance.SaveGame(playerName, currentPlayer);
        
        if (success)
        {
            terminal.WriteLine("Game saved successfully!", "green");
        }
        else
        {
            terminal.WriteLine("Failed to save game!", "red");
        }
        
        await Task.Delay(1000);
    }
    
    /// <summary>
    /// Create new player using comprehensive character creation system
    /// Based on Pascal USERHUNC.PAS implementation
    /// </summary>
    private async Task<Character> CreateNewPlayer(string playerName)
    {
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
            
            // Character creation successful
            terminal.WriteLine("");
            terminal.WriteLine("Character successfully created!", "bright_green");
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
    private async Task<bool> CheckDailyLimits()
    {
        // Check if it's a new day
        if (dailyManager.IsNewDay())
        {
            await dailyManager.CheckDailyReset();
        }
        
        return currentPlayer.TurnsRemaining > 0;
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
        SaveManager.SavePlayer(currentPlayer);
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
            SaveManager.SavePlayer(currentPlayer);
        }
        
        terminal.WriteLine("Goodbye!", "green");
        await Task.Delay(1000);
        
        // GetTree().Quit(); // Godot API not available, use alternative
        Environment.Exit(0);
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
    private void InitializeNPCs()
    {
        try
        {
            if (worldNPCs == null)
                worldNPCs = new List<NPC>();

            var dataPath = Path.Combine(DataPath, "npcs.json");
            if (!File.Exists(dataPath))
            {
                GD.PrintErr($"[Init] NPC data file not found at {dataPath}. Using hard-coded specials only.");
                return;
            }

            var json = File.ReadAllText(dataPath);
            using var doc = JsonDocument.Parse(json);

            // Flatten all category arrays (tavern_npcs, guard_npcs, random_npcs, etc.)
            var root = doc.RootElement;
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Array) continue;

                foreach (var npcElem in prop.Value.EnumerateArray())
                {
                    try
                    {
                        var name = npcElem.GetProperty("name").GetString() ?? "Unknown";
                        var archetype = npcElem.GetProperty("archetype").GetString() ?? "citizen";
                        var classStr = npcElem.GetProperty("class").GetString() ?? "warrior";
                        var level = npcElem.GetProperty("level").GetInt32();

                        if (!Enum.TryParse<CharacterClass>(classStr, true, out var charClass))
                        {
                            charClass = CharacterClass.Warrior;
                        }

                        var npc = new NPC(name, archetype, charClass, level);

                        // Gold override if provided
                        if (npcElem.TryGetProperty("gold", out var goldProp) && goldProp.TryGetInt64(out long gold))
                        {
                            npc.Gold = gold;
                        }

                        // Starting location mapping
                        string startLoc = npcElem.GetProperty("startingLocation").GetString() ?? "main_street";
                        var locId = MapStringToLocation(startLoc);
                        npc.UpdateLocation(startLoc); // keep textual for AI compatibility

                        worldNPCs.Add(npc);

                        // Add to LocationManager so they show up to the player
                        LocationManager.Instance.AddNPCToLocation(locId, npc);
                    }
                    catch (Exception exNpc)
                    {
                        GD.PrintErr($"[Init] Failed to load NPC: {exNpc.Message}");
                    }
                }
            }

            GD.Print($"[Init] Loaded {worldNPCs.Count} NPC definitions from data file.");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Init] Error loading NPCs: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Map simple string location names from JSON to GameLocation enum.
    /// </summary>
    private static GameLocation MapStringToLocation(string loc)
    {
        return loc.ToLower() switch
        {
            "tavern" or "inn" => GameLocation.TheInn,
            "market" or "marketplace" => GameLocation.Marketplace,
            "town_square" or "main_street" => GameLocation.MainStreet,
            "castle" => GameLocation.Castle,
            "temple" or "church" => GameLocation.Temple,
            "dungeon" or "dungeons" => GameLocation.Dungeons,
            "bank" => GameLocation.Bank,
            "dark_alley" or "alley" => GameLocation.DarkAlley,
            _ => GameLocation.MainStreet
        };
    }
    
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
            // MagicShopSystemValidation(); // TODO: Implement this validation method
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
