using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Base location class for all game locations
/// Based on Pascal location system from ONLINE.PAS
/// </summary>
public abstract class BaseLocation
{
    public GameLocation LocationId { get; protected set; }
    public string Name { get; protected set; } = "";
    public string Description { get; protected set; } = "";
    public List<GameLocation> PossibleExits { get; protected set; } = new();
    public List<NPC> LocationNPCs { get; protected set; } = new();
    public List<string> LocationActions { get; protected set; } = new();
    
    // Pascal compatibility
    public bool RefreshRequired { get; set; } = true;
    
    protected TerminalEmulator terminal;
    protected Character currentPlayer;
    
    public BaseLocation(GameLocation locationId, string name, string description)
    {
        LocationId = locationId;
        Name = name;
        Description = description;
        SetupLocation();
    }
    
    /// <summary>
    /// Setup location-specific data (exits, NPCs, actions)
    /// </summary>
    protected virtual void SetupLocation()
    {
        // Override in derived classes
    }
    
    /// <summary>
    /// Enter the location - main entry point
    /// </summary>
    public virtual async Task EnterLocation(Character player, TerminalEmulator term)
    {
        currentPlayer = player;
        terminal = term;
        
        // Update player location
        player.Location = (int)LocationId;
        
        // Main location loop
        await LocationLoop();
    }
    
    /// <summary>
    /// Main location loop - handles display and user input
    /// </summary>
    protected virtual async Task LocationLoop()
    {
        bool exitLocation = false;
        
        while (!exitLocation && currentPlayer.IsAlive && currentPlayer.TurnsLeft > 0)
        {
            // Display location
            DisplayLocation();
            
            // Get user choice
            var choice = await GetUserChoice();
            
            // Process choice
            exitLocation = await ProcessChoice(choice);
        }
    }
    
    /// <summary>
    /// Display the location screen
    /// </summary>
    protected virtual void DisplayLocation()
    {
        terminal.ClearScreen();
        
        // Location header
        terminal.SetColor("bright_yellow");
        terminal.WriteLine(Name);
        terminal.SetColor("yellow");
        terminal.WriteLine(new string('═', Name.Length));
        terminal.WriteLine("");
        
        // Location description
        terminal.SetColor("white");
        terminal.WriteLine(Description);
        terminal.WriteLine("");
        
        // Show NPCs in location
        ShowNPCsInLocation();
        
        // Show available actions
        ShowLocationActions();
        
        // Show exits
        ShowExits();
        
        // Status line
        ShowStatusLine();
    }
    
    /// <summary>
    /// Show NPCs in this location
    /// </summary>
    protected virtual void ShowNPCsInLocation()
    {
        if (LocationNPCs.Count > 0)
        {
            terminal.SetColor("bright_cyan");
            terminal.WriteLine("People here:");
            
            foreach (var npc in LocationNPCs)
            {
                if (npc.IsAvailable && npc.CanInteract)
                {
                    terminal.SetColor("cyan");
                    terminal.WriteLine($"  {npc.GetDisplayInfo()}");
                }
            }
            terminal.WriteLine("");
        }
    }
    
    /// <summary>
    /// Show location-specific actions
    /// </summary>
    protected virtual void ShowLocationActions()
    {
        if (LocationActions.Count > 0)
        {
            terminal.SetColor("white");
            terminal.WriteLine("Available actions:");
            
            for (int i = 0; i < LocationActions.Count; i++)
            {
                terminal.WriteLine($"  {i + 1}. {LocationActions[i]}");
            }
            terminal.WriteLine("");
        }
    }
    
    /// <summary>
    /// Show available exits (Pascal-compatible)
    /// </summary>
    protected virtual void ShowExits()
    {
        if (PossibleExits.Count > 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("Exits:");
            
            foreach (var exit in PossibleExits)
            {
                var exitName = GetLocationName(exit);
                var exitKey = GetLocationKey(exit);
                terminal.WriteLine($"  ({exitKey}) {exitName}");
            }
            terminal.WriteLine("");
        }
    }
    
    /// <summary>
    /// Show status line at bottom
    /// </summary>
    protected virtual void ShowStatusLine()
    {
        terminal.SetColor("gray");
        terminal.WriteLine($"HP: {currentPlayer.HP}/{currentPlayer.MaxHP} | " +
                          $"Gold: {currentPlayer.Gold} | " +
                          $"Level: {currentPlayer.Level} | " +
                          $"Turns: {currentPlayer.TurnsLeft}");
        terminal.WriteLine("");
    }
    
    /// <summary>
    /// Get user choice
    /// </summary>
    protected virtual async Task<string> GetUserChoice()
    {
        terminal.SetColor("bright_white");
        return await terminal.GetInput("Your choice: ");
    }
    
    /// <summary>
    /// Process user choice - returns true if should exit location
    /// </summary>
    protected virtual async Task<bool> ProcessChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(choice))
            return false;
            
        var upperChoice = choice.ToUpper().Trim();
        
        // Check for exits first
        foreach (var exit in PossibleExits)
        {
            if (upperChoice == GetLocationKey(exit))
            {
                await NavigateToLocation(exit);
                return true;
            }
        }
        
        // Check for numbered actions
        if (int.TryParse(upperChoice, out int actionIndex))
        {
            if (actionIndex > 0 && actionIndex <= LocationActions.Count)
            {
                await ExecuteLocationAction(actionIndex - 1);
                return false;
            }
        }
        
        // Check for special commands
        switch (upperChoice)
        {
            case "S":
                await ShowStatus();
                break;
            case "?":
                // Help/menu already shown
                break;
            case "Q":
                if (LocationId != GameLocation.MainStreet)
                {
                    await NavigateToLocation(GameLocation.MainStreet);
                    return true;
                }
                break;
            default:
                terminal.WriteLine("Invalid choice!", "red");
                await Task.Delay(1000);
                break;
        }
        
        return false;
    }
    
    /// <summary>
    /// Execute a location-specific action
    /// </summary>
    protected virtual async Task ExecuteLocationAction(int actionIndex)
    {
        // Override in derived classes
        terminal.WriteLine("This action is not yet implemented.", "gray");
        await Task.Delay(1500);
    }
    
    /// <summary>
    /// Navigate to another location
    /// </summary>
    protected virtual async Task NavigateToLocation(GameLocation destination)
    {
        terminal.WriteLine($"Heading to {GetLocationName(destination)}...", "yellow");
        await Task.Delay(1000);
        
        // Throw exception to signal location change
        throw new LocationExitException(destination);
    }
    
    /// <summary>
    /// Show player status
    /// </summary>
    protected virtual async Task ShowStatus()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("Player Status");
        terminal.WriteLine("=============");
        terminal.WriteLine("");
        
        terminal.SetColor("white");
        terminal.WriteLine($"Name: {currentPlayer.DisplayName}");
        terminal.WriteLine($"Level: {currentPlayer.Level}");
        terminal.WriteLine($"Class: {currentPlayer.Class}");
        terminal.WriteLine($"Race: {currentPlayer.Race}");
        terminal.WriteLine($"HP: {currentPlayer.HP}/{currentPlayer.MaxHP}");
        terminal.WriteLine($"Experience: {currentPlayer.Experience}");
        terminal.WriteLine($"Gold: {currentPlayer.Gold}");
        terminal.WriteLine($"Bank: {currentPlayer.BankGold}");
        terminal.WriteLine($"Strength: {currentPlayer.Strength}");
        terminal.WriteLine($"Defence: {currentPlayer.Defence}");
        terminal.WriteLine($"Turns Left: {currentPlayer.TurnsLeft}");
        
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    /// <summary>
    /// Get location name for display
    /// </summary>
    public static string GetLocationName(GameLocation location)
    {
        return location switch
        {
            GameLocation.MainStreet => "Main Street",
            GameLocation.TheInn => "The Inn",
            GameLocation.DarkAlley => "Dark Alley",
            GameLocation.Church => "Church",
            GameLocation.WeaponShop => "Weapon Shop",
            GameLocation.ArmorShop => "Armor Shop",
            GameLocation.Bank => "Bank",
            GameLocation.Marketplace => "Marketplace",
            GameLocation.Dungeons => "Dungeons",
            GameLocation.Castle => "Royal Castle",
            GameLocation.Dormitory => "Dormitory",
            GameLocation.AnchorRoad => "Anchor Road",
            GameLocation.Temple => "Temple",
            GameLocation.BobsBeer => "Bob's Beer",
            GameLocation.Healer => "Healer",
            GameLocation.MagicShop => "Magic Shop",
            GameLocation.Master => "Level Master",
            _ => location.ToString()
        };
    }
    
    /// <summary>
    /// Get location key for navigation
    /// </summary>
    public static string GetLocationKey(GameLocation location)
    {
        return location switch
        {
            GameLocation.MainStreet => "M",
            GameLocation.TheInn => "I",
            GameLocation.DarkAlley => "D",
            GameLocation.Church => "C",
            GameLocation.WeaponShop => "W",
            GameLocation.ArmorShop => "A",
            GameLocation.Bank => "B",
            GameLocation.Marketplace => "K",
            GameLocation.Dungeons => "U",
            GameLocation.Castle => "S",
            GameLocation.Dormitory => "O",
            GameLocation.AnchorRoad => "R",
            GameLocation.Temple => "T",
            GameLocation.BobsBeer => "H",
            GameLocation.Healer => "E",
            GameLocation.MagicShop => "G",
            GameLocation.Master => "L",
            _ => "?"
        };
    }
    
    /// <summary>
    /// Add NPC to this location
    /// </summary>
    public virtual void AddNPC(NPC npc)
    {
        if (!LocationNPCs.Contains(npc))
        {
            LocationNPCs.Add(npc);
            npc.CurrentLocation = LocationId.ToString().ToLower();
        }
    }
    
    /// <summary>
    /// Remove NPC from this location
    /// </summary>
    public virtual void RemoveNPC(NPC npc)
    {
        LocationNPCs.Remove(npc);
    }
    
    /// <summary>
    /// Get location description for online system (Pascal compatible)
    /// </summary>
    public virtual string GetLocationDescription()
    {
        return LocationId switch
        {
            GameLocation.MainStreet => "Main street",
            GameLocation.TheInn => "Inn",
            GameLocation.DarkAlley => "outside the Shady Shops",
            GameLocation.Church => "Church",
            GameLocation.Dungeons => "Dungeons",
            GameLocation.WeaponShop => "Weapon shop",
            GameLocation.Master => "level master",
            GameLocation.MagicShop => "Magic shop",
            GameLocation.ArmorShop => "Armor shop",
            GameLocation.Bank => "Bank",
            GameLocation.Healer => "Healer",
            GameLocation.Marketplace => "Market Place",
            GameLocation.Dormitory => "Dormitory",
            GameLocation.AnchorRoad => "Anchor road",
            GameLocation.BobsBeer => "Bobs Beer",
            GameLocation.Castle => "Royal Castle",
            GameLocation.Prison => "Royal Prison",
            GameLocation.Temple => "Holy Temple",
            _ => Name
        };
    }
} 
