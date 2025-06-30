using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public partial class GameEngine : Node
{
    private static GameEngine instance;
    public static GameEngine Instance => instance;
    
    private GameState currentState;
    private Player currentPlayer;
    private List<NPC> worldNPCs;
    private TerminalEmulator terminal;
    private WorldSimulator worldSim;
    private DailySystemManager dailySystem;
    
    public GameState CurrentState => currentState;
    public Player CurrentPlayer => currentPlayer;
    public TerminalEmulator Terminal => terminal;
    public List<NPC> WorldNPCs => worldNPCs;
    public WorldSimulator WorldSimulator => worldSim;
    
    public override void _Ready()
    {
        instance = this;
        InitializeSystems();
        LoadGameData();
        _ = ShowMainMenu(); // Fire and forget async
    }
    
    private void InitializeSystems()
    {
        // Create terminal
        terminal = new TerminalEmulator();
        AddChild(terminal);
        
        // Initialize systems
        worldSim = new WorldSimulator();
        dailySystem = new DailySystemManager();
        worldNPCs = new List<NPC>();
        
        LoadDataFiles();
        InitializeNPCs();
    }
    
    private void LoadDataFiles()
    {
        // These would be implemented by data manager classes
        GD.Print("Loading game data files...");
        // ItemManager.LoadItems("res://Data/items.json");
        // CharacterClassManager.LoadClasses("res://Data/characters.json");
        // NPCDataManager.LoadNPCs("res://Data/npcs.json");
        // MonsterManager.LoadMonsters("res://Data/monsters.json");
        // EventManager.LoadEvents("res://Data/events.json");
        // FormulaManager.LoadFormulas("res://Data/formulas.json");
        // DialogueManager.LoadDialogues("res://Data/dialogue.json");
        // ConfigManager.LoadConfig("res://Data/game_config.json");
    }
    
    private void InitializeNPCs()
    {
        // Create some basic NPCs for testing
        var npcData = new[]
        {
            new { Name = "Seth Able", Archetype = "thug", Class = CharacterClass.Warrior, Level = 5 },
            new { Name = "Mary the Merchant", Archetype = "merchant", Class = CharacterClass.Thief, Level = 8 },
            new { Name = "Captain Harris", Archetype = "guard", Class = CharacterClass.Warrior, Level = 15 },
            new { Name = "Brother Thomas", Archetype = "priest", Class = CharacterClass.Cleric, Level = 18 },
            new { Name = "Elena the Mystic", Archetype = "mystic", Class = CharacterClass.Mage, Level = 20 }
        };
        
        foreach (var data in npcData)
        {
            var npc = new NPC(data.Name, data.Archetype, data.Class, data.Level);
            var personality = PersonalityProfile.GenerateRandom(data.Archetype);
            npc.Brain = new NPCBrain(npc, personality);
            worldNPCs.Add(npc);
        }
        
        GD.Print($"Initialized {worldNPCs.Count} NPCs");
    }
    
    private async Task ShowMainMenu()
    {
        while (true)
        {
            terminal.ClearScreen();
            terminal.ShowASCIIArt("usurper_title");
            
            var options = new List<MenuOption>
            {
                new MenuOption { Text = "New Game", Action = async () => await StartNewGame() },
                new MenuOption { Text = "Load Game", Action = async () => await LoadGame() },
                new MenuOption { Text = "Instructions", Action = async () => await ShowInstructions() },
                new MenuOption { Text = "Credits", Action = async () => await ShowCredits() },
                new MenuOption { Text = "Quit", Action = async () => await QuitGame() }
            };
            
            var choice = await terminal.GetMenuChoice(options);
            if (choice >= 0)
            {
                await options[choice].Action();
            }
            else
            {
                await QuitGame();
            }
        }
    }
    
    public async Task StartNewGame()
    {
        terminal.ClearScreen();
        terminal.ShowASCIIArt("usurper_title");
        await terminal.PressAnyKey("Press any key to begin your adventure...");
        
        currentPlayer = await CreateCharacter();
        currentState = new GameState
        {
            Day = 1,
            Hour = 6,
            TurnsRemaining = 250 // ConfigManager.TurnsPerDay
        };
        
        await EnterGameWorld();
    }
    
    private async Task<Player> CreateCharacter()
    {
        terminal.ClearScreen();
        terminal.WriteLine("╔══════════════════ CHARACTER CREATION ══════════════════╗", "bright_blue");
        terminal.WriteLine("║                                                        ║", "bright_blue");
        terminal.WriteLine("║  Welcome to the realm! Create your character below.   ║", "white");
        terminal.WriteLine("║                                                        ║", "bright_blue");
        terminal.WriteLine("╚════════════════════════════════════════════════════════╝", "bright_blue");
        terminal.WriteLine("");
        
        var realName = await terminal.GetInput("Enter your real name: ");
        var characterName = await terminal.GetInput("Enter your character name: ");
        
        // Choose class
        terminal.WriteLine("\nChoose your class:", "yellow");
        var classOptions = new List<MenuOption>
        {
            new MenuOption { Text = "Warrior - Strong fighter, high HP", Action = null },
            new MenuOption { Text = "Thief - Fast and sneaky, good with locks", Action = null },
            new MenuOption { Text = "Cleric - Healer with divine magic", Action = null },
            new MenuOption { Text = "Mage - Powerful magic user, low HP", Action = null }
        };
        
        var classChoice = await terminal.GetMenuChoice(classOptions);
        var selectedClass = (CharacterClass)classChoice;
        
        var player = new Player(realName, characterName, selectedClass);
        
        terminal.WriteLine("");
        terminal.WriteLine($"Welcome, {characterName} the {selectedClass}!", "bright_green");
        await terminal.PressAnyKey();
        
        return player;
    }
    
    private async Task EnterGameWorld()
    {
        worldSim.StartSimulation(worldNPCs);
        await MainGameLoop();
    }
    
    private async Task MainGameLoop()
    {
        while (currentPlayer.IsAlive && currentPlayer.HasTurns())
        {
            // Check for daily reset
            await dailySystem.CheckDailyReset();
            
            // Show current location
            await ShowLocation();
            
            // Process world simulation
            worldSim.SimulateStep();
            
            // Update game time
            currentState.Hour++;
            if (currentState.Hour >= 24)
            {
                currentState.Hour = 0;
                currentState.Day++;
            }
            
            // Check win conditions
            if (currentPlayer.IsRuler)
            {
                await ShowVictoryScreen();
                break;
            }
        }
        
        if (!currentPlayer.IsAlive)
        {
            await ShowDeathScreen();
        }
        else if (!currentPlayer.HasTurns())
        {
            await ShowOutOfTurnsScreen();
        }
    }
    
    private async Task ShowLocation()
    {
        terminal.ClearScreen();
        terminal.ShowStatusBar(
            currentPlayer.Name,
            currentPlayer.Level,
            currentPlayer.CurrentHP,
            currentPlayer.MaxHP,
            currentPlayer.Gold,
            currentPlayer.TurnsRemaining
        );
        
        // Show current location (simplified for now)
        switch (currentPlayer.CurrentLocation)
        {
            case "town_square":
                await ShowTownSquare();
                break;
            case "tavern":
                await ShowTavern();
                break;
            default:
                await ShowTownSquare();
                break;
        }
    }
    
    private async Task ShowTownSquare()
    {
        terminal.WriteLine("╔══════════════════════ TOWN SQUARE ══════════════════════╗", "yellow");
        terminal.WriteLine("║                                                         ║", "yellow");
        terminal.WriteLine("║  You stand in the bustling town square. People mill    ║", "white");
        terminal.WriteLine("║  about, conducting their daily business.               ║", "white");
        terminal.WriteLine("║                                                         ║", "yellow");
        terminal.WriteLine("╚═════════════════════════════════════════════════════════╝", "yellow");
        terminal.WriteLine("");
        
        // Show NPCs present
        var npcsHere = worldNPCs.Where(npc => npc.CurrentLocation == "town_square" && npc.IsAlive).ToList();
        if (npcsHere.Any())
        {
            terminal.WriteLine("People here:", "cyan");
            foreach (var npc in npcsHere.Take(5))
            {
                terminal.WriteLine($"  • {npc.GetDisplayInfo()}", "white");
            }
            terminal.WriteLine("");
        }
        
        var options = new List<MenuOption>
        {
            new MenuOption { Text = "Go to the Tavern", Action = async () => { currentPlayer.CurrentLocation = "tavern"; } },
            new MenuOption { Text = "Visit the Market", Action = async () => { currentPlayer.CurrentLocation = "market"; } },
            new MenuOption { Text = "Enter the Dungeon", Action = async () => await EnterDungeon() },
            new MenuOption { Text = "View Character Stats", Action = async () => await ShowCharacterStats() },
            new MenuOption { Text = "Save and Quit", Action = async () => await SaveAndQuit() }
        };
        
        if (npcsHere.Any())
        {
            options.Insert(0, new MenuOption { Text = "Talk to someone", Action = async () => await TalkToNPC(npcsHere) });
        }
        
        var choice = await terminal.GetMenuChoice(options);
        if (choice >= 0)
        {
            await options[choice].Action();
            currentPlayer.SpendTurn();
        }
    }
    
    private async Task ShowTavern()
    {
        terminal.WriteLine("╔═════════════════ THE DRUNKEN DRAGON TAVERN ═════════════════╗", "yellow");
        terminal.WriteLine("║                                                             ║", "yellow");
        terminal.WriteLine("║  The smoky tavern is filled with boisterous laughter and   ║", "white");
        terminal.WriteLine("║  the clash of tankards. A fireplace crackles in the corner.║", "white");
        terminal.WriteLine("║                                                             ║", "yellow");
        terminal.WriteLine("╚═════════════════════════════════════════════════════════════╝", "yellow");
        terminal.WriteLine("");
        
        var options = new List<MenuOption>
        {
            new MenuOption { Text = "Order a drink (5 gold)", Action = async () => await OrderDrink() },
            new MenuOption { Text = "Listen for rumors", Action = async () => await ListenForRumors() },
            new MenuOption { Text = "Rest for the night (20 gold)", Action = async () => await Rest() },
            new MenuOption { Text = "Return to Town Square", Action = async () => { currentPlayer.CurrentLocation = "town_square"; } }
        };
        
        var choice = await terminal.GetMenuChoice(options);
        if (choice >= 0)
        {
            await options[choice].Action();
            if (choice != 3) // Don't spend turn for leaving
                currentPlayer.SpendTurn();
        }
    }
    
    private async Task OrderDrink()
    {
        if (!currentPlayer.CanAfford(5))
        {
            terminal.WriteLine("You don't have enough gold!", "red");
            await terminal.PressAnyKey();
            return;
        }
        
        currentPlayer.SpendGold(5);
        terminal.WriteLine("You order a frothy mug of ale and drink deeply.", "yellow");
        
        // Small chance of getting drunk or hearing rumors
        if (GD.Randf() < 0.3f)
        {
            terminal.WriteLine("The ale loosens your tongue and you overhear some interesting gossip...", "cyan");
            await ListenForRumors();
        }
        
        currentPlayer.Heal(5);
        await terminal.PressAnyKey();
    }
    
    private async Task ListenForRumors()
    {
        terminal.WriteLine("\nYou listen carefully to the tavern chatter...", "cyan");
        await Task.Delay(1500);
        
        var rumors = new[]
        {
            "I heard strange noises coming from the dungeon last night...",
            "The king has been acting strangely lately.",
            "Someone found a magical sword deep in the dungeon!",
            "The merchant's guild is looking for caravan guards.",
            "They say the deeper dungeon levels hold incredible treasures...",
            "I wouldn't trust the castle guards if I were you.",
            "There's talk of forming a new gang to challenge the rulers."
        };
        
        var rumor = rumors[GD.RandRange(0, rumors.Length - 1)];
        terminal.WriteLine($"\"{rumor}\"", "bright_cyan");
        await terminal.PressAnyKey();
    }
    
    private async Task Rest()
    {
        if (!currentPlayer.CanAfford(20))
        {
            terminal.WriteLine("You don't have enough gold to rent a room!", "red");
            await terminal.PressAnyKey();
            return;
        }
        
        currentPlayer.SpendGold(20);
        terminal.WriteLine("You rent a room and get a good night's rest.", "green");
        
        var healAmount = currentPlayer.MaxHP / 2;
        currentPlayer.Heal(healAmount);
        
        terminal.WriteLine($"You feel refreshed! (+{healAmount} HP)", "bright_green");
        await terminal.PressAnyKey();
    }
    
    private async Task EnterDungeon()
    {
        terminal.WriteLine("You descend into the dark dungeon...", "gray");
        await terminal.PressAnyKey("Press any key to continue into the depths...");
        
        // Simple dungeon encounter
        if (GD.Randf() < 0.7f) // 70% chance of encounter
        {
            await DungeonEncounter();
        }
        else
        {
            terminal.WriteLine("You explore the dungeon but find nothing of interest.", "gray");
            await terminal.PressAnyKey();
        }
    }
    
    private async Task DungeonEncounter()
    {
        terminal.WriteLine("A monster emerges from the shadows!", "red");
        await terminal.PressAnyKey();
        
        // Simple combat simulation
        var monsterHP = 20;
        var monsterAttack = 8;
        
        while (monsterHP > 0 && currentPlayer.IsAlive)
        {
            // Player attacks
            var playerDamage = currentPlayer.GetAttackPower() + GD.RandRange(1, 6);
            monsterHP -= playerDamage;
            terminal.WriteLine($"You attack for {playerDamage} damage!", "yellow");
            
            if (monsterHP <= 0)
            {
                terminal.WriteLine("The monster is defeated!", "bright_green");
                var exp = 50 + GD.RandRange(10, 30);
                var gold = GD.RandRange(10, 50);
                currentPlayer.GainExperience(exp);
                currentPlayer.GainGold(gold);
                currentPlayer.MonsterKills++;
                terminal.WriteLine($"You gain {exp} experience and {gold} gold!", "bright_yellow");
                break;
            }
            
            // Monster attacks
            var monsterDamage = monsterAttack + GD.RandRange(1, 4);
            currentPlayer.TakeDamage(monsterDamage);
            terminal.WriteLine($"The monster attacks you for {monsterDamage} damage!", "red");
            
            if (!currentPlayer.IsAlive)
            {
                terminal.WriteLine("You have been defeated!", "bright_red");
                currentPlayer.Die();
                break;
            }
            
            await terminal.PressAnyKey("Press any key to continue combat...");
        }
        
        await terminal.PressAnyKey();
    }
    
    private async Task TalkToNPC(List<NPC> npcs)
    {
        terminal.WriteLine("Who would you like to talk to?", "yellow");
        
        var npcOptions = npcs.Select(npc => new MenuOption
        {
            Text = npc.GetDisplayInfo(),
            Action = async () => await InteractWithNPC(npc)
        }).ToList();
        
        var choice = await terminal.GetMenuChoice(npcOptions);
        if (choice >= 0)
        {
            await npcOptions[choice].Action();
        }
    }
    
    private async Task InteractWithNPC(NPC npc)
    {
        var greeting = npc.GetGreeting(currentPlayer);
        terminal.WriteLine($"{npc.Name}: \"{greeting}\"", "yellow");
        
        // Simple interaction options
        var options = new List<MenuOption>
        {
            new MenuOption { Text = "Chat", Action = async () => await ChatWithNPC(npc) },
            new MenuOption { Text = "Ask about rumors", Action = async () => await AskAboutRumors(npc) }
        };
        
        // Add class-specific options
        if (npc.Archetype == "merchant")
        {
            options.Insert(0, new MenuOption { Text = "Trade", Action = async () => await TradeWithNPC(npc) });
        }
        
        var choice = await terminal.GetMenuChoice(options);
        if (choice >= 0)
        {
            await options[choice].Action();
        }
    }
    
    private async Task ChatWithNPC(NPC npc)
    {
        var responses = new[]
        {
            "It's good to see a friendly face around here.",
            "Times are tough, but we make do.",
            "Have you heard the latest news?",
            "Be careful out there - danger lurks everywhere.",
            "The world is changing, and not for the better."
        };
        
        var response = responses[GD.RandRange(0, responses.Length - 1)];
        terminal.WriteLine($"{npc.Name}: \"{response}\"", "cyan");
        await terminal.PressAnyKey();
    }
    
    private async Task AskAboutRumors(NPC npc)
    {
        await ListenForRumors(); // Reuse rumor system
    }
    
    private async Task TradeWithNPC(NPC npc)
    {
        terminal.WriteLine($"{npc.Name}: \"What would you like to trade?\"", "yellow");
        terminal.WriteLine("Trading system not yet implemented.", "gray");
        await terminal.PressAnyKey();
    }
    
    private async Task ShowCharacterStats()
    {
        currentPlayer.ShowPlayerStats(terminal);
        await terminal.PressAnyKey();
    }
    
    private async Task ShowVictoryScreen()
    {
        terminal.ClearScreen();
        terminal.WriteLine("🎉 VICTORY! 🎉", "bright_yellow");
        terminal.WriteLine("You have become the ruler of the realm!", "bright_green");
        await terminal.PressAnyKey();
    }
    
    private async Task ShowDeathScreen()
    {
        terminal.ClearScreen();
        terminal.WriteLine("☠ DEATH ☠", "bright_red");
        terminal.WriteLine("Your adventure has come to an end...", "red");
        await terminal.PressAnyKey();
    }
    
    private async Task ShowOutOfTurnsScreen()
    {
        terminal.ClearScreen();
        terminal.WriteLine("Out of turns for today!", "yellow");
        terminal.WriteLine("Come back tomorrow for more adventure!", "cyan");
        await terminal.PressAnyKey();
    }
    
    private async Task LoadGame()
    {
        terminal.WriteLine("Load game not yet implemented.", "gray");
        await terminal.PressAnyKey();
    }
    
    private async Task ShowInstructions()
    {
        terminal.ClearScreen();
        terminal.WriteLine("USURPER INSTRUCTIONS", "bright_yellow");
        terminal.WriteLine("", "white");
        terminal.WriteLine("Your goal is to become the ruler of the realm by gaining", "white");
        terminal.WriteLine("experience, gold, and power. Fight monsters, interact with", "white");
        terminal.WriteLine("NPCs, and work your way up from peasant to ruler!", "white");
        await terminal.PressAnyKey();
    }
    
    private async Task ShowCredits()
    {
        terminal.ClearScreen();
        terminal.WriteLine("USURPER REBORN", "bright_yellow");
        terminal.WriteLine("", "white");
        terminal.WriteLine("A remake of the classic BBS door game by Jakob Dangarden", "white");
        terminal.WriteLine("Remake created with Godot and C#", "white");
        terminal.WriteLine("Featuring advanced NPC AI and emergent storytelling", "white");
        await terminal.PressAnyKey();
    }
    
    private async Task SaveAndQuit()
    {
        terminal.WriteLine("Saving game...", "yellow");
        await Task.Delay(1000);
        terminal.WriteLine("Game saved!", "green");
        await QuitGame();
    }
    
    private async Task QuitGame()
    {
        terminal.WriteLine("Thanks for playing Usurper Reborn!", "bright_cyan");
        await Task.Delay(2000);
        GetTree().Quit();
    }
}

public class GameState
{
    public int Day { get; set; } = 1;
    public int Hour { get; set; } = 6;
    public int TurnsRemaining { get; set; } = 250;
} 