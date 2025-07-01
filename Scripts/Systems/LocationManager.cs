using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

/// <summary>
/// Location Manager - handles all location-related functionality
/// Based on Pascal location system from ONLINE.PAS
/// </summary>
public class LocationManager
{
    private Dictionary<GameLocation, BaseLocation> locations;
    private GameLocation currentLocationId;
    private BaseLocation currentLocation;
    private TerminalEmulator terminal;
    private Character currentPlayer;
    
    // Pascal-compatible navigation table (from ONLINE.PAS)
    private Dictionary<GameLocation, List<GameLocation>> navigationTable;
    
    private static LocationManager? _instance;
    public static LocationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Create a default terminal for head-less scenarios
                _instance = new LocationManager(new TerminalEmulator());
            }
            return _instance;
        }
    }

    // Ensure that any manually created manager becomes the global instance
    private void RegisterAsSingleton()
    {
        if (_instance == null)
            _instance = this;
    }

    public LocationManager() : this(new TerminalEmulator())
    {
    }

    public LocationManager(TerminalEmulator term)
    {
        terminal = term;
        locations = new Dictionary<GameLocation, BaseLocation>();
        InitializeLocations();
        InitializeNavigationTable();
        currentLocationId = GameLocation.MainStreet;
        RegisterAsSingleton();
    }
    
    /// <summary>
    /// Initialize all game locations
    /// </summary>
    private void InitializeLocations()
    {
        // Core locations
        locations[GameLocation.MainStreet] = new MainStreetLocation();
        locations[GameLocation.TheInn] = new InnLocation();
        locations[GameLocation.Church] = new PlaceholderLocation(GameLocation.Church, "Church", "A peaceful place of worship and healing.");
        locations[GameLocation.Dungeons] = new DungeonLocation();
        locations[GameLocation.Bank] = new BankLocation();
        locations[GameLocation.WeaponShop] = new WeaponShopLocation();
        locations[GameLocation.ArmorShop] = new ArmorShopLocation();
        locations[GameLocation.Marketplace] = new PlaceholderLocation(GameLocation.Marketplace, "Marketplace", "A bustling center of trade and commerce.");
        locations[GameLocation.Castle] = new CastleLocation();
        locations[GameLocation.Healer] = new HealerLocation();
        locations[GameLocation.MagicShop] = new MagicShopLocation();
        locations[GameLocation.Master] = new PlaceholderLocation(GameLocation.Master, "Level Master", "An old sage who can help you advance in power.");
        locations[GameLocation.DarkAlley] = new PlaceholderLocation(GameLocation.DarkAlley, "Dark Alley", "A shadowy place where questionable deals are made.");
        locations[GameLocation.AnchorRoad] = new PlaceholderLocation(GameLocation.AnchorRoad, "Anchor Road", "The gateway to challenges and adventures.");
        locations[GameLocation.TeamCorner] = new PlaceholderLocation(GameLocation.TeamCorner, "Team Corner", "Where groups gather to plan their strategies.");
        locations[GameLocation.Recruit] = new PlaceholderLocation(GameLocation.Recruit, "Hall of Recruitment", "Seek allies for your quests here.");
        locations[GameLocation.Dormitory] = new PlaceholderLocation(GameLocation.Dormitory, "Dormitory", "A place to rest and recover from your adventures.");
        locations[GameLocation.Temple] = new TempleLocation();
        locations[GameLocation.Home] = new PlaceholderLocation(GameLocation.Home, "Your Home", "Your personal dwelling and sanctuary.");
        locations[GameLocation.Prison] = new PrisonLocation();
        locations[GameLocation.PrisonWalk] = new PlaceholderLocation(GameLocation.PrisonWalk, "Outside Prison", "Walk around the prison grounds.");
        
        // Phase 11: Prison System
        locations[GameLocation.Prisoner] = new PrisonLocation();
        
        // Phase 12: Relationship System  
        locations[GameLocation.LoveCorner] = new LoveCornerLocation();
        
        GD.Print($"[LocationManager] Initialized {locations.Count} locations");
    }
    
    /// <summary>
    /// Initialize Pascal-compatible navigation table
    /// Exact match with ONLINE.PAS navigation case statements
    /// </summary>
    private void InitializeNavigationTable()
    {
        navigationTable = new Dictionary<GameLocation, List<GameLocation>>();
        
        // From Pascal ONLINE.PAS case statements
        navigationTable[GameLocation.MainStreet] = new List<GameLocation>
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
        
        navigationTable[GameLocation.TheInn] = new List<GameLocation>
        {
            GameLocation.MainStreet,   // loc1
            GameLocation.TeamCorner,   // loc2
            GameLocation.Recruit       // loc3
        };
        
        navigationTable[GameLocation.TeamCorner] = new List<GameLocation>
        {
            GameLocation.TheInn       // loc1
        };
        
        navigationTable[GameLocation.Recruit] = new List<GameLocation>
        {
            GameLocation.TheInn       // loc1
        };
        
        navigationTable[GameLocation.Church] = new List<GameLocation>
        {
            GameLocation.MainStreet   // loc1
        };
        
        navigationTable[GameLocation.Dungeons] = new List<GameLocation>
        {
            GameLocation.MainStreet   // loc1
        };
        
        navigationTable[GameLocation.WeaponShop] = new List<GameLocation>
        {
            GameLocation.MainStreet   // loc1
        };
        
        navigationTable[GameLocation.ArmorShop] = new List<GameLocation>
        {
            GameLocation.MainStreet   // loc1
        };
        
        navigationTable[GameLocation.Bank] = new List<GameLocation>
        {
            GameLocation.MainStreet   // loc1
        };
        
        navigationTable[GameLocation.Marketplace] = new List<GameLocation>
        {
            GameLocation.MainStreet,  // loc1
            GameLocation.FoodStore,   // loc2
            // Additional exits will be added as locations are implemented
        };
        
        navigationTable[GameLocation.AnchorRoad] = new List<GameLocation>
        {
            GameLocation.MainStreet,    // loc1
            GameLocation.Dormitory,     // loc2
            GameLocation.BountyRoom,    // loc3
            GameLocation.QuestHall,     // loc4
            GameLocation.Temple,        // loc6
            GameLocation.LoveStreet,    // loc7
            GameLocation.OutsideCastle  // loc8
        };
        
        // Add more navigation entries as needed
        // This follows the exact Pascal pattern from ONLINE.PAS
    }
    
    /// <summary>
    /// Enter a location with the current player
    /// </summary>
    public async Task EnterLocation(GameLocation locationId, Character player)
    {
        currentPlayer = player;
        
        if (!locations.ContainsKey(locationId))
        {
            GD.PrintErr($"[LocationManager] Location {locationId} not found!");
            return;
        }
        
        // Update current location
        var previousLocation = currentLocationId;
        currentLocationId = locationId;
        currentLocation = locations[locationId];
        
        // Update player location (Pascal compatibility)
        currentPlayer.Location = (int)locationId;
        
        GD.Print($"[LocationManager] Player {player.DisplayName} entering {BaseLocation.GetLocationName(locationId)}");
        
        // Enter the location
        try
        {
            await currentLocation.EnterLocation(player, terminal);
        }
        catch (LocationExitException ex)
        {
            // Handle location change request
            if (ex.DestinationLocation != GameLocation.NoWhere)
            {
                await EnterLocation(ex.DestinationLocation, player);
            }
            else
            {
                // NoWhere means quit game
                terminal.WriteLine("Returning to main menu...", "yellow");
                await Task.Delay(1000);
            }
        }
    }
    
    /// <summary>
    /// Navigate from current location to destination
    /// Validates navigation according to Pascal rules
    /// </summary>
    public async Task<bool> NavigateTo(GameLocation destination, Character player)
    {
        // Check if navigation is allowed
        if (!CanNavigateTo(currentLocationId, destination))
        {
            terminal.WriteLine($"You cannot go to {BaseLocation.GetLocationName(destination)} from here!", "red");
            await Task.Delay(1500);
            return false;
        }
        
        // Perform navigation
        await EnterLocation(destination, player);
        return true;
    }
    
    /// <summary>
    /// Check if navigation is allowed according to Pascal rules
    /// </summary>
    public bool CanNavigateTo(GameLocation from, GameLocation to)
    {
        if (!navigationTable.ContainsKey(from))
            return false;
            
        return navigationTable[from].Contains(to);
    }
    
    /// <summary>
    /// Get available exits from current location
    /// </summary>
    public List<GameLocation> GetAvailableExits(GameLocation from)
    {
        if (navigationTable.ContainsKey(from))
            return new List<GameLocation>(navigationTable[from]);
            
        return new List<GameLocation>();
    }
    
    /// <summary>
    /// Get current location description for online system (Pascal compatible)
    /// </summary>
    public string GetCurrentLocationDescription()
    {
        return currentLocation?.GetLocationDescription() ?? "Unknown location";
    }
    
    /// <summary>
    /// Add NPC to a specific location
    /// </summary>
    public void AddNPCToLocation(GameLocation locationId, NPC npc)
    {
        if (locations.ContainsKey(locationId))
        {
            locations[locationId].AddNPC(npc);
        }
    }
    
    /// <summary>
    /// Remove NPC from a specific location
    /// </summary>
    public void RemoveNPCFromLocation(GameLocation locationId, NPC npc)
    {
        if (locations.ContainsKey(locationId))
        {
            locations[locationId].RemoveNPC(npc);
        }
    }
    
    /// <summary>
    /// Get NPCs in a specific location
    /// </summary>
    public List<NPC> GetNPCsInLocation(GameLocation locationId)
    {
        if (locations.ContainsKey(locationId))
        {
            return new List<NPC>(locations[locationId].LocationNPCs);
        }
        return new List<NPC>();
    }
    
    /// <summary>
    /// Get current location ID
    /// </summary>
    public GameLocation GetCurrentLocation()
    {
        return currentLocationId;
    }

    // Utility accessor used heavily by legacy code
    public BaseLocation? GetLocation(GameLocation id)
    {
        return locations.TryGetValue(id, out var loc) ? loc : null;
    }

    /// <summary>
    /// Legacy ChangeLocation overloads. Many older classes call these synchronous helpers.
    /// They simply enqueue a navigation request that will be executed on the calling thread.
    /// In this stub we just switch currentLocationId and return completed tasks so that the game can compile.
    /// </summary>
    public async Task ChangeLocation(Character player, string locationClassName)
    {
        // Very naive implementation – look for first location whose class name matches.
        var match = locations.Values.FirstOrDefault(l => l.GetType().Name.Equals(locationClassName, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            await EnterLocation(match.LocationId, player);
        }
        else
        {
            terminal.WriteLine($"Unknown location '{locationClassName}'. Returning to Main Street.", "red");
            await EnterLocation(GameLocation.MainStreet, player);
        }
    }

    public async Task ChangeLocation(Character player, GameLocation locationId)
    {
        await EnterLocation(locationId, player);
    }

    // Non-player overload kept for backwards compatibility
    public void ChangeLocation(string locationClassName)
    {
        var dummy = new Player(); // Placeholder – caller didn't provide player reference in legacy code
        ChangeLocation(dummy, locationClassName).Wait();
    }
}

/// <summary>
/// Exception thrown when a location wants to change to another location
/// </summary>
public class LocationExitException : Exception
{
    public GameLocation DestinationLocation { get; }
    
    public LocationExitException(GameLocation destination) : base($"Exiting to {destination}")
    {
        DestinationLocation = destination;
    }
}

/// <summary>
/// Placeholder location for locations not yet implemented
/// </summary>
public class PlaceholderLocation : BaseLocation
{
    public PlaceholderLocation(GameLocation locationId, string name, string description) 
        : base(locationId, name, description)
    {
    }
    
    protected virtual void SetupLocation()
    {
        // Basic exits - most locations can return to main street
        PossibleExits = new List<GameLocation>
        {
            GameLocation.MainStreet
        };
        
        LocationActions = new List<string>
        {
            "This location is not yet implemented",
            "Return to Main Street"
        };
    }
    
    protected virtual async Task<bool> ProcessChoice(string choice)
    {
        var upperChoice = choice.ToUpper().Trim();
        
        switch (upperChoice)
        {
            case "M":
            case "1":
            case "2":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;
                
            case "S":
                await ShowStatus();
                return false;
                
            default:
                terminal.WriteLine("This location is not yet implemented. Press M to return to Main Street.", "yellow");
                await Task.Delay(2000);
                return false;
        }
    }
    
    protected virtual void DisplayLocation()
    {
        terminal.ClearScreen();
        
        // Location header
        terminal.SetColor("bright_yellow");
        terminal.WriteLine(Name);
        terminal.SetColor("yellow");
        terminal.WriteLine(new string('═', Name.Length));
        terminal.WriteLine("");
        
        // Description
        terminal.SetColor("white");
        terminal.WriteLine(Description);
        terminal.WriteLine("");
        
        // Implementation notice
        terminal.SetColor("gray");
        terminal.WriteLine("This location is not yet fully implemented.");
        terminal.WriteLine("More features will be added in future updates.");
        terminal.WriteLine("");
        
        // Simple navigation
        terminal.SetColor("yellow");
        terminal.WriteLine("Navigation:");
        terminal.WriteLine("(M) Return to Main Street");
        terminal.WriteLine("(S) Status");
        terminal.WriteLine("");
        
        // Status line
        ShowStatusLine();
    }
} 
