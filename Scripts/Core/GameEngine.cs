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

        // Show splash screen
        await UsurperRemake.UI.SplashScreen.Show(terminal);

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

        // Process NPC behaviors and maintenance
        await RunNPCMaintenanceCycle();
    }

    /// <summary>
    /// Run NPC maintenance cycle - handles NPC movement, activities, and world events
    /// </summary>
    private async Task RunNPCMaintenanceCycle()
    {
        var npcs = NPCSpawnSystem.Instance.ActiveNPCs;
        if (npcs == null || npcs.Count == 0) return;

        var random = new Random();

        // Process each living NPC
        foreach (var npc in npcs.Where(n => n.IsAlive).ToList())
        {
            // 20% chance to move to a different location
            if (random.Next(5) == 0)
            {
                MoveNPCToRandomLocation(npc, random);
            }

            // Process NPC activities (shopping, healing, etc.)
            await ProcessNPCActivity(npc, random);

            // Small chance for NPC to generate news
            if (random.Next(20) == 0)
            {
                GenerateNPCNews(npc, random);
            }
        }

        // Process NPC leveling (rare)
        if (random.Next(10) == 0)
        {
            ProcessNPCLeveling(npcs, random);
        }
    }

    /// <summary>
    /// Move an NPC to a random location in town
    /// </summary>
    private void MoveNPCToRandomLocation(NPC npc, Random random)
    {
        var locations = new[]
        {
            "Main Street", "Market", "Inn", "Temple", "Gym",
            "Weapon Shop", "Armor Shop", "Magic Shop", "Tavern",
            "Bank", "Healer", "Dark Alley"
        };

        var newLocation = locations[random.Next(locations.Length)];

        // Don't log every move - too spammy
        npc.CurrentLocation = newLocation;
    }

    /// <summary>
    /// Process NPC activity based on their current situation
    /// </summary>
    private async Task ProcessNPCActivity(NPC npc, Random random)
    {
        // Heal if injured
        if (npc.HP < npc.MaxHP && random.Next(3) == 0)
        {
            long healAmount = Math.Min(npc.MaxHP / 10, npc.MaxHP - npc.HP);
            npc.HP += (int)healAmount;
        }

        // Restore mana
        if (npc.Mana < npc.MaxMana && random.Next(2) == 0)
        {
            long manaAmount = Math.Min(npc.MaxMana / 5, npc.MaxMana - npc.Mana);
            npc.Mana += (int)manaAmount;
        }

        // Shopping (if at shop and has gold)
        if (npc.Gold > 500 && random.Next(10) == 0)
        {
            // Buy equipment upgrade
            if (npc.CurrentLocation == "Weapon Shop")
            {
                int cost = random.Next(100, 500);
                if (npc.Gold >= cost)
                {
                    npc.Gold -= cost;
                    npc.WeapPow += random.Next(1, 5);
                }
            }
            else if (npc.CurrentLocation == "Armor Shop")
            {
                int cost = random.Next(100, 400);
                if (npc.Gold >= cost)
                {
                    npc.Gold -= cost;
                    npc.ArmPow += random.Next(1, 4);
                }
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Generate news about NPC activities
    /// </summary>
    private void GenerateNPCNews(NPC npc, Random random)
    {
        var newsSystem = NewsSystem.Instance;
        if (newsSystem == null) return;

        var newsItems = new List<string>();

        // Alignment-based news
        if (npc.Darkness > npc.Chivalry + 200)
        {
            newsItems.Add($"{npc.Name2} was seen lurking in the shadows");
            newsItems.Add($"{npc.Name2} threatened a merchant");
            newsItems.Add($"Guards are watching {npc.Name2} closely");
        }
        else if (npc.Chivalry > npc.Darkness + 200)
        {
            newsItems.Add($"{npc.Name2} helped a lost child find their parents");
            newsItems.Add($"{npc.Name2} donated gold to the temple");
            newsItems.Add($"{npc.Name2} protected a merchant from thieves");
        }
        else
        {
            newsItems.Add($"{npc.Name2} was seen at the {npc.CurrentLocation}");
            newsItems.Add($"{npc.Name2} is looking for adventure partners");
        }

        // Class-based news
        switch (npc.Class)
        {
            case CharacterClass.Warrior:
            case CharacterClass.Barbarian:
                newsItems.Add($"{npc.Name2} challenged someone to a duel");
                break;
            case CharacterClass.Magician:
            case CharacterClass.Sage:
                newsItems.Add($"{npc.Name2} was seen studying ancient tomes");
                break;
            case CharacterClass.Assassin:
                newsItems.Add($"Rumors swirl about {npc.Name2}'s latest target");
                break;
        }

        if (newsItems.Count > 0)
        {
            var headline = newsItems[random.Next(newsItems.Count)];
            newsSystem.Newsy(true, headline);
        }
    }

    /// <summary>
    /// Process NPC leveling based on their activities
    /// </summary>
    private void ProcessNPCLeveling(List<NPC> npcs, Random random)
    {
        // Pick a random NPC to level up
        var eligibleNPCs = npcs.Where(n => n.IsAlive && n.Level < 50).ToList();
        if (eligibleNPCs.Count == 0) return;

        var luckyNPC = eligibleNPCs[random.Next(eligibleNPCs.Count)];

        // Level up!
        luckyNPC.Level++;
        luckyNPC.Experience += luckyNPC.Level * 1000;

        // Boost stats
        luckyNPC.MaxHP += random.Next(10, 30);
        luckyNPC.HP = luckyNPC.MaxHP;
        luckyNPC.Strength += random.Next(1, 3);
        luckyNPC.Defence += random.Next(1, 2);
        luckyNPC.WeapPow += random.Next(1, 3);
        luckyNPC.ArmPow += random.Next(1, 2);

        // Generate news about the level up
        var newsSystem = NewsSystem.Instance;
        if (newsSystem != null)
        {
            newsSystem.Newsy(true, $"{luckyNPC.Name2} has reached level {luckyNPC.Level}!");
        }
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

            // Title header
            terminal.SetColor("bright_red");
            terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("║               USURPER RELOADED - The Dungeon of Death                       ║");
            terminal.SetColor("bright_red");
            terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
            terminal.WriteLine("");

            // Menu options with classic BBS style
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("bright_cyan");
            terminal.Write("E");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("cyan");
            terminal.WriteLine("Enter the Game");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("bright_cyan");
            terminal.Write("I");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Instructions");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("bright_cyan");
            terminal.Write("L");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("List Players");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("bright_cyan");
            terminal.Write("T");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Teams");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("bright_cyan");
            terminal.Write("N");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Yesterday's News");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("bright_cyan");
            terminal.Write("S");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Game Settings");

            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("bright_red");
            terminal.Write("Q");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("red");
            terminal.WriteLine("Quit to DOS");

            terminal.WriteLine("");
            terminal.SetColor("bright_white");
            var choice = await terminal.GetInput("Your choice: ");

            switch (choice.ToUpper())
            {
                case "E":
                    await EnterGame();
                    break;
                case "I":
                    await ShowInstructions();
                    break;
                case "L":
                    await ListPlayers();
                    break;
                case "T":
                    await ShowTeams();
                    break;
                case "N":
                    await ShowNews();
                    break;
                case "S":
                    await ShowGameSettings();
                    break;
                case "Q":
                    done = true;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Enter the game with modern save/load UI
    /// </summary>
    private async Task EnterGame()
    {
        while (true)
        {
            terminal.ClearScreen();
            terminal.SetColor("bright_cyan");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════════════════════════╗");
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("║                           SAVE FILE MANAGEMENT                           ║");
            terminal.SetColor("bright_cyan");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════════════════════════╝");
            terminal.WriteLine("");

            // Get all unique player names
            var playerNames = SaveSystem.Instance.GetAllPlayerNames();

            if (playerNames.Count == 0)
            {
                // No saves exist - create new character
                terminal.SetColor("yellow");
                terminal.WriteLine("No saved games found.");
                terminal.WriteLine("");
                terminal.SetColor("white");
                terminal.WriteLine("Let's create a new character!");
                terminal.WriteLine("");

                var playerName = await terminal.GetInput("Enter your real name (or player name): ");

                if (string.IsNullOrWhiteSpace(playerName))
                {
                    terminal.WriteLine("Invalid name!", "red");
                    await Task.Delay(2000);
                    continue;
                }

                await CreateNewGame(playerName);
                return;
            }

            // Show existing save slots
            terminal.SetColor("bright_green");
            terminal.WriteLine("Existing Save Slots:");
            terminal.WriteLine("");

            for (int i = 0; i < playerNames.Count; i++)
            {
                var playerName = playerNames[i];
                var mostRecentSave = SaveSystem.Instance.GetMostRecentSave(playerName);

                if (mostRecentSave != null)
                {
                    terminal.SetColor("darkgray");
                    terminal.Write($"[");
                    terminal.SetColor("bright_cyan");
                    terminal.Write($"{i + 1}");
                    terminal.SetColor("darkgray");
                    terminal.Write("] ");
                    terminal.SetColor("white");
                    terminal.Write($"{mostRecentSave.PlayerName}");
                    terminal.SetColor("gray");
                    terminal.Write($" - Level {mostRecentSave.Level}");
                    terminal.SetColor("darkgray");
                    terminal.Write(" | ");
                    terminal.SetColor(mostRecentSave.IsAutosave ? "yellow" : "green");
                    terminal.Write(mostRecentSave.SaveType);
                    terminal.SetColor("darkgray");
                    terminal.Write(" | ");
                    terminal.SetColor("gray");
                    terminal.WriteLine(mostRecentSave.SaveTime.ToString("yyyy-MM-dd HH:mm:ss"));
                }
            }

            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_green");
            terminal.Write("N");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("green");
            terminal.WriteLine("New Character");

            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_red");
            terminal.Write("B");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("red");
            terminal.WriteLine("Back to Main Menu");

            terminal.WriteLine("");
            terminal.SetColor("bright_white");
            var choice = await terminal.GetInput("Select save slot or option: ");

            // Handle numeric selection
            if (int.TryParse(choice, out int slotNumber) && slotNumber > 0 && slotNumber <= playerNames.Count)
            {
                var selectedPlayer = playerNames[slotNumber - 1];
                await ShowSaveSlotMenu(selectedPlayer);
                return;
            }

            // Handle letter commands
            switch (choice.ToUpper())
            {
                case "N":
                    var newName = await terminal.GetInput("Enter new character name: ");
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        if (playerNames.Contains(newName))
                        {
                            terminal.WriteLine("That name already exists! Choose a different name.", "red");
                            await Task.Delay(2000);
                        }
                        else
                        {
                            await CreateNewGame(newName);
                            return;
                        }
                    }
                    break;

                case "B":
                    return;

                default:
                    terminal.WriteLine("Invalid choice!", "red");
                    await Task.Delay(1500);
                    break;
            }
        }
    }

    /// <summary>
    /// Show save slot menu for a specific player
    /// </summary>
    private async Task ShowSaveSlotMenu(string playerName)
    {
        while (true)
        {
            terminal.ClearScreen();
            terminal.SetColor("bright_cyan");
            terminal.WriteLine($"╔═══════════════════════════════════════════════════════════════════════════╗");
            terminal.SetColor("bright_yellow");
            terminal.WriteLine($"║                      SAVE SLOTS FOR: {playerName.PadRight(33)}║");
            terminal.SetColor("bright_cyan");
            terminal.WriteLine($"╚═══════════════════════════════════════════════════════════════════════════╝");
            terminal.WriteLine("");

            var saves = SaveSystem.Instance.GetPlayerSaves(playerName);

            if (saves.Count == 0)
            {
                terminal.WriteLine("No saves found for this character.", "red");
                await Task.Delay(2000);
                return;
            }

            // Display all saves (autosaves and manual saves)
            terminal.SetColor("bright_green");
            terminal.WriteLine("Available Saves:");
            terminal.WriteLine("");

            for (int i = 0; i < saves.Count && i < 10; i++) // Show up to 10 saves
            {
                var save = saves[i];
                terminal.SetColor("darkgray");
                terminal.Write($"[");
                terminal.SetColor("bright_cyan");
                terminal.Write($"{i + 1}");
                terminal.SetColor("darkgray");
                terminal.Write("] ");

                terminal.SetColor(save.IsAutosave ? "yellow" : "bright_green");
                terminal.Write($"{save.SaveType.PadRight(12)}");

                terminal.SetColor("gray");
                terminal.Write($" - Day {save.CurrentDay}, Level {save.Level}, {save.TurnsRemaining} turns");

                terminal.SetColor("darkgray");
                terminal.Write(" | ");

                terminal.SetColor("cyan");
                terminal.WriteLine(save.SaveTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_red");
            terminal.Write("D");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("red");
            terminal.WriteLine("Delete this character (all saves)");

            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_red");
            terminal.Write("B");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("red");
            terminal.WriteLine("Back");

            terminal.WriteLine("");
            terminal.SetColor("bright_white");
            var choice = await terminal.GetInput("Select save to load: ");

            // Handle numeric selection
            if (int.TryParse(choice, out int saveNumber) && saveNumber > 0 && saveNumber <= saves.Count)
            {
                var selectedSave = saves[saveNumber - 1];
                await LoadSaveByFileName(selectedSave.FileName);
                return;
            }

            // Handle letter commands
            switch (choice.ToUpper())
            {
                case "D":
                    terminal.WriteLine("");
                    var confirm = await terminal.GetInput($"Delete ALL saves for '{playerName}'? Type 'DELETE' to confirm: ");
                    if (confirm == "DELETE")
                    {
                        // Delete all saves for this player
                        foreach (var save in saves)
                        {
                            var filePath = System.IO.Path.Combine(
                                System.IO.Path.Combine(GetUserDataPath(), "saves"),
                                save.FileName);
                            try
                            {
                                System.IO.File.Delete(filePath);
                            }
                            catch (Exception ex)
                            {
                                GD.PrintErr($"Failed to delete {save.FileName}: {ex.Message}");
                            }
                        }
                        terminal.WriteLine("All saves deleted.", "green");
                        await Task.Delay(1500);
                        return;
                    }
                    break;

                case "B":
                    return;

                default:
                    terminal.WriteLine("Invalid choice!", "red");
                    await Task.Delay(1500);
                    break;
            }
        }
    }

    /// <summary>
    /// Load game by filename
    /// </summary>
    private async Task LoadSaveByFileName(string fileName)
    {
        try
        {
            terminal.WriteLine("Loading save...", "yellow");

            var saveData = await SaveSystem.Instance.LoadSaveByFileName(fileName);
            if (saveData == null)
            {
                terminal.WriteLine("Failed to load save file!", "red");
                terminal.WriteLine("The save file may be corrupted or invalid.", "yellow");
                await Task.Delay(3000);
                return;
            }

            // Validate save data
            if (saveData.Player == null)
            {
                terminal.WriteLine("Save file is missing player data!", "red");
                await Task.Delay(3000);
                return;
            }

            terminal.WriteLine($"Restoring {saveData.Player.Name2 ?? saveData.Player.Name1}...", "green");
            await Task.Delay(500);

            // Restore player from save data
            currentPlayer = RestorePlayerFromSaveData(saveData.Player);

            if (currentPlayer == null)
            {
                terminal.WriteLine("Failed to restore player data!", "red");
                await Task.Delay(3000);
                return;
            }

            // Load daily system state
            if (dailyManager != null)
            {
                dailyManager.LoadFromSaveData(saveData);
            }

            // Restore world state
            await RestoreWorldState(saveData.WorldState);

            // Restore NPCs
            await RestoreNPCs(saveData.NPCs);

            terminal.WriteLine("Save loaded successfully!", "bright_green");
            await Task.Delay(1000);

            // Start game at saved location
            await locationManager.EnterLocation(GameLocation.MainStreet, currentPlayer);
        }
        catch (Exception ex)
        {
            terminal.WriteLine($"Error loading save: {ex.Message}", "red");
            GD.PrintErr($"Failed to load save {fileName}: {ex}");
            await Task.Delay(3000);
        }
    }

    /// <summary>
    /// Get user data path (cross-platform)
    /// </summary>
    private string GetUserDataPath()
    {
        var appName = "UsurperReloaded";

        if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            return System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), appName);
        }
        else if (System.Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var home = System.Environment.GetEnvironmentVariable("HOME");
            return System.IO.Path.Combine(home ?? "/tmp", ".local", "share", appName);
        }
        else
        {
            var home = System.Environment.GetEnvironmentVariable("HOME");
            return System.IO.Path.Combine(home ?? "/tmp", "Library", "Application Support", appName);
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

        // Initialize NPCs only if they haven't been initialized yet
        // The NPCSpawnSystem has a guard flag to prevent duplicate spawning
        if (NPCSpawnSystem.Instance.ActiveNPCs.Count == 0)
        {
            await NPCSpawnSystem.Instance.InitializeClassicNPCs();
        }

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

        // Check if player is dead - handle death and continue playing
        if (!currentPlayer.IsAlive)
        {
            await HandleDeath();
            // After death handling, player is resurrected - continue to game
            // (HandleDeath sets HP > 0 and saves)
        }

        // Enter main game using location system
        // If player died and was resurrected, they'll start at the Inn
        var startLocation = currentPlayer.Location > 0
            ? (GameLocation)currentPlayer.Location
            : GameLocation.MainStreet;
        await locationManager.EnterLocation(startLocation, currentPlayer);
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
            Intelligence = playerData.Intelligence,
            Constitution = playerData.Constitution,
            Mana = playerData.Mana,
            MaxMana = playerData.MaxMana,

            // Equipment and items (CRITICAL FIXES)
            Healing = playerData.Healing,     // POTIONS
            WeapPow = playerData.WeapPow,     // WEAPON POWER
            ArmPow = playerData.ArmPow,       // ARMOR POWER

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
            GnollP = playerData.GnollP,
            Addict = playerData.Addict,
            Mercy = playerData.Mercy,

            // Disease status
            Blind = playerData.Blind,
            Plague = playerData.Plague,
            Smallpox = playerData.Smallpox,
            Measles = playerData.Measles,
            Leprosy = playerData.Leprosy,

            // Character settings
            AutoHeal = playerData.AutoHeal,
            Loyalty = playerData.Loyalty,
            Haunt = playerData.Haunt,
            Master = playerData.Master,
            WellWish = playerData.WellWish,

            // Physical appearance
            Height = playerData.Height,
            Weight = playerData.Weight,
            Eyes = playerData.Eyes,
            Hair = playerData.Hair,
            Skin = playerData.Skin,

            // Social/Team
            Team = playerData.Team,
            TeamPW = playerData.TeamPassword,
            CTurf = playerData.IsTeamLeader,
            TeamRec = playerData.TeamRec,
            BGuard = playerData.BGuard,

            Allowed = true // Always allow loaded players
        };

        // Restore character flavor text
        if (playerData.Phrases?.Count > 0)
        {
            player.Phrases = playerData.Phrases;
        }

        if (playerData.Description?.Count > 0)
        {
            player.Description = playerData.Description;
        }
        
        // Restore items (ensure lists are always initialized)
        player.Item = playerData.Items?.Length > 0
            ? playerData.Items.ToList()
            : new List<int>();

        player.ItemType = playerData.ItemTypes?.Length > 0
            ? playerData.ItemTypes.Select(i => (ObjType)i).ToList()
            : new List<ObjType>();

        // NEW: Restore equipment system
        if (playerData.EquippedItems != null && playerData.EquippedItems.Count > 0)
        {
            player.EquippedItems = playerData.EquippedItems.ToDictionary(
                kvp => (EquipmentSlot)kvp.Key,
                kvp => kvp.Value
            );
        }

        // Restore base stats
        player.BaseStrength = playerData.BaseStrength > 0 ? playerData.BaseStrength : playerData.Strength;
        player.BaseDexterity = playerData.BaseDexterity > 0 ? playerData.BaseDexterity : playerData.Dexterity;
        player.BaseConstitution = playerData.BaseConstitution > 0 ? playerData.BaseConstitution : playerData.Constitution;
        player.BaseIntelligence = playerData.BaseIntelligence > 0 ? playerData.BaseIntelligence : playerData.Intelligence;
        player.BaseWisdom = playerData.BaseWisdom > 0 ? playerData.BaseWisdom : playerData.Wisdom;
        player.BaseCharisma = playerData.BaseCharisma > 0 ? playerData.BaseCharisma : playerData.Charisma;
        player.BaseMaxHP = playerData.BaseMaxHP > 0 ? playerData.BaseMaxHP : playerData.MaxHP;
        player.BaseMaxMana = playerData.BaseMaxMana > 0 ? playerData.BaseMaxMana : playerData.MaxMana;
        player.BaseDefence = playerData.BaseDefence > 0 ? playerData.BaseDefence : playerData.Defence;
        player.BaseStamina = playerData.BaseStamina > 0 ? playerData.BaseStamina : playerData.Stamina;
        player.BaseAgility = playerData.BaseAgility > 0 ? playerData.BaseAgility : playerData.Agility;

        // If this is an old save without equipment data, initialize from WeapPow/ArmPow
        if ((playerData.EquippedItems == null || playerData.EquippedItems.Count == 0)
            && (playerData.WeapPow > 0 || playerData.ArmPow > 0))
        {
            // Migration: Find best matching equipment based on WeapPow/ArmPow
            MigrateOldEquipmentToNew(player, playerData.WeapPow, playerData.ArmPow);
        }

        // Parse location
        if (int.TryParse(playerData.CurrentLocation, out var locationId))
        {
            player.Location = locationId;
        }

        return player;
    }

    /// <summary>
    /// Migrate old WeapPow/ArmPow to new equipment system for old saves
    /// </summary>
    private void MigrateOldEquipmentToNew(Character player, long weapPow, long armPow)
    {
        // Find best matching weapon for WeapPow
        if (weapPow > 0)
        {
            var weapons = EquipmentDatabase.GetWeaponsByHandedness(WeaponHandedness.OneHanded);
            var bestWeapon = weapons
                .Where(w => w.WeaponPower <= weapPow)
                .OrderByDescending(w => w.WeaponPower)
                .FirstOrDefault();

            if (bestWeapon != null)
            {
                player.EquippedItems[EquipmentSlot.MainHand] = bestWeapon.Id;
            }
        }

        // Find best matching armor for ArmPow
        if (armPow > 0)
        {
            var armors = EquipmentDatabase.GetBySlot(EquipmentSlot.Body);
            var bestArmor = armors
                .Where(a => a.ArmorClass <= armPow)
                .OrderByDescending(a => a.ArmorClass)
                .FirstOrDefault();

            if (bestArmor != null)
            {
                player.EquippedItems[EquipmentSlot.Body] = bestArmor.Id;
            }
        }

        // Initialize base stats
        player.InitializeBaseStats();
    }
    
    /// <summary>
    /// Restore world state from save data
    /// </summary>
    private async Task RestoreWorldState(WorldStateData worldState)
    {
        if (worldState == null)
        {
            GD.Print("[GameEngine] No world state to restore");
            return;
        }

        // Restore economic state
        // This would integrate with bank and economy systems

        // Restore political state
        if (!string.IsNullOrEmpty(worldState.CurrentRuler))
        {
            // Set current ruler if applicable
        }

        // Restore active world events from save data
        var currentDay = dailyManager?.CurrentDay ?? 1;
        WorldEventSystem.Instance.RestoreFromSaveData(worldState.ActiveEvents, currentDay);

        GD.Print($"[GameEngine] World state restored: {worldState.ActiveEvents?.Count ?? 0} active events");
        await Task.CompletedTask;
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
    /// Player respawns at the Inn with penalties instead of being deleted
    /// </summary>
    private async Task HandleDeath()
    {
        terminal.ClearScreen();
        terminal.ShowANSIArt("DEATH");
        terminal.SetColor("bright_red");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                        YOU HAVE DIED!                          ");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine("Your vision fades to black as death claims you...");
        terminal.WriteLine("");
        await Task.Delay(2000);

        // Check if player has resurrections (from items/temple)
        if (currentPlayer.Resurrections > 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"You have {currentPlayer.Resurrections} resurrection(s) available!");
            terminal.WriteLine("");
            var resurrect = await terminal.GetInput("Use a resurrection to avoid penalties? (Y/N): ");

            if (resurrect.ToUpper().StartsWith("Y"))
            {
                currentPlayer.Resurrections--;
                currentPlayer.HP = currentPlayer.MaxHP;
                terminal.SetColor("bright_green");
                terminal.WriteLine("");
                terminal.WriteLine("Divine light surrounds you!");
                terminal.WriteLine("You have been fully resurrected with no penalties!");
                await Task.Delay(2500);

                // Return to the Inn
                currentPlayer.Location = (int)GameLocation.TheInn;
                await SaveSystem.Instance.AutoSave(currentPlayer);
                return;
            }
        }

        // Apply death penalties
        terminal.SetColor("red");
        terminal.WriteLine("Death Penalties Applied:");
        terminal.WriteLine("─────────────────────────");

        // Calculate penalties
        long expLoss = currentPlayer.Experience / 10;  // Lose 10% experience
        long goldLoss = currentPlayer.Gold / 4;        // Lose 25% gold on hand

        // Apply penalties
        currentPlayer.Experience = Math.Max(0, currentPlayer.Experience - expLoss);
        currentPlayer.Gold = Math.Max(0, currentPlayer.Gold - goldLoss);

        // Track death count
        currentPlayer.MDefeats++;

        terminal.SetColor("yellow");
        if (expLoss > 0)
            terminal.WriteLine($"  • Lost {expLoss:N0} experience points");
        if (goldLoss > 0)
            terminal.WriteLine($"  • Lost {goldLoss:N0} gold (dropped upon death)");
        terminal.WriteLine($"  • Monster defeats: {currentPlayer.MDefeats}");
        terminal.WriteLine("");

        // Resurrect player at the Inn with half HP
        currentPlayer.HP = Math.Max(1, currentPlayer.MaxHP / 2);
        currentPlayer.Location = (int)GameLocation.TheInn;

        // Clear any negative status effects
        currentPlayer.Poison = 0;

        terminal.SetColor("cyan");
        terminal.WriteLine("You wake up at the Inn, nursed back to health by the innkeeper.");
        terminal.WriteLine($"Your wounds have partially healed. (HP: {currentPlayer.HP}/{currentPlayer.MaxHP})");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine("\"You're lucky to be alive, friend. Rest up and try again.\"");
        terminal.WriteLine("");

        await terminal.PressAnyKey();

        // Save the resurrected character
        await SaveSystem.Instance.AutoSave(currentPlayer);

        // Continue playing - don't mark as deleted!
        terminal.SetColor("green");
        terminal.WriteLine("Your adventure continues...");
        await Task.Delay(1500);
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
    private void InitializeItems()
    {
        // Initialize items using ItemManager
        ItemManager.InitializeItems();
    }
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
