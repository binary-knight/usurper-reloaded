using UsurperRemake.Utils;
using UsurperRemake.Systems;
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

        while (!exitLocation && currentPlayer.IsAlive) // No turn limit - continuous gameplay
        {
            // Autosave BEFORE displaying location (save stable state)
            // This ensures we don't save during quit/exit actions
            if (currentPlayer != null)
            {
                await SaveSystem.Instance.AutoSave(currentPlayer);
            }

            // Display location
            DisplayLocation();

            // Get user choice
            var choice = await GetUserChoice();

            // Process choice
            exitLocation = await ProcessChoice(choice);

            // Increment turn count (drives world simulation)
            if (currentPlayer != null && !string.IsNullOrWhiteSpace(choice))
            {
                currentPlayer.TurnCount++;

                // Run world simulation every 5 turns
                if (currentPlayer.TurnCount % 5 == 0)
                {
                    await RunWorldSimulationTick();
                }
            }
        }
    }

    /// <summary>
    /// Run a tick of world simulation (NPCs act, world events, etc.)
    /// </summary>
    private async Task RunWorldSimulationTick()
    {
        // Run game engine's periodic update for world simulation
        var gameEngine = GameEngine.Instance;
        if (gameEngine != null)
        {
            await gameEngine.PeriodicUpdate();
        }
    }
    
    /// <summary>
    /// Display the location screen
    /// </summary>
    protected virtual void DisplayLocation()
    {
        terminal.ClearScreen();

        // Breadcrumb navigation
        ShowBreadcrumb();

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
    /// Show breadcrumb navigation at top of screen
    /// </summary>
    protected virtual void ShowBreadcrumb()
    {
        terminal.SetColor("gray");
        terminal.Write("Location: ");
        terminal.SetColor("bright_cyan");

        // Build breadcrumb path based on current location
        string breadcrumb = GetBreadcrumbPath();
        terminal.WriteLine(breadcrumb);
        terminal.WriteLine("");
    }

    /// <summary>
    /// Get breadcrumb path for current location
    /// </summary>
    protected virtual string GetBreadcrumbPath()
    {
        // Default: just show location name
        // Subclasses can override for more complex paths (e.g., "Main Street → Dungeons → Level 3")
        switch (LocationId)
        {
            case GameLocation.MainStreet:
                return "Main Street";
            case GameLocation.Home:
                return "Anchor Road → Your Home";
            case GameLocation.AnchorRoad:
                return "Main Street → Anchor Road";
            case GameLocation.WeaponShop:
                return "Main Street → Weapon Shop";
            case GameLocation.ArmorShop:
                return "Main Street → Armor Shop";
            case GameLocation.MagicShop:
                return "Main Street → Magic Shop";
            case GameLocation.TheInn:
                return "Main Street → The Inn";
            case GameLocation.DarkAlley:
                return "Main Street → Dark Alley";
            case GameLocation.Church:
                return "Main Street → Church";
            case GameLocation.Bank:
                return "Main Street → Bank";
            case GameLocation.Castle:
                return "Main Street → Royal Castle";
            case GameLocation.Prison:
                return "Anchor Road → Outside Prison";
            default:
                return Name ?? "Unknown";
        }
    }

    /// <summary>
    /// Show status line at bottom
    /// </summary>
    protected virtual void ShowStatusLine()
    {
        terminal.SetColor("gray");
        terminal.Write("HP: ");
        terminal.SetColor("red");
        terminal.Write($"{currentPlayer.HP}");
        terminal.SetColor("gray");
        terminal.Write("/");
        terminal.SetColor("red");
        terminal.Write($"{currentPlayer.MaxHP}");

        terminal.SetColor("gray");
        terminal.Write(" | Gold: ");
        terminal.SetColor("yellow");
        terminal.Write($"{currentPlayer.Gold}");

        if (currentPlayer.MaxMana > 0)
        {
            terminal.SetColor("gray");
            terminal.Write(" | Mana: ");
            terminal.SetColor("blue");
            terminal.Write($"{currentPlayer.Mana}");
            terminal.SetColor("gray");
            terminal.Write("/");
            terminal.SetColor("blue");
            terminal.Write($"{currentPlayer.MaxMana}");
        }

        terminal.SetColor("gray");
        terminal.Write(" | Level: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.Level}");

        terminal.SetColor("gray");
        terminal.Write(" | Turn: ");
        terminal.SetColor("white");
        terminal.WriteLine($"{currentPlayer.TurnCount}");
        terminal.WriteLine("");

        // Quick command bar
        ShowQuickCommandBar();
    }

    /// <summary>
    /// Show quick command bar with common keyboard shortcuts
    /// </summary>
    protected virtual void ShowQuickCommandBar()
    {
        terminal.SetColor("darkgray");
        terminal.Write("─────────────────────────────────────────────────────────────────────────────");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.Write("Quick Commands: ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("cyan");
        terminal.Write("S");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("tatus  ");

        if (LocationId != GameLocation.MainStreet)
        {
            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("cyan");
            terminal.Write("Q");
            terminal.SetColor("darkgray");
            terminal.Write("]");
            terminal.SetColor("white");
            terminal.Write("uick Return  ");
        }

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("cyan");
        terminal.Write("?");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("Help");

        terminal.WriteLine("");
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
                terminal.SetColor("red");
                terminal.WriteLine($"Invalid choice: '{choice}'");
                terminal.SetColor("gray");
                terminal.Write("Try: ");
                terminal.SetColor("cyan");
                terminal.Write("[S]");
                terminal.SetColor("gray");
                terminal.Write("tatus");

                if (LocationId != GameLocation.MainStreet)
                {
                    terminal.Write(", ");
                    terminal.SetColor("cyan");
                    terminal.Write("[Q]");
                    terminal.SetColor("gray");
                    terminal.Write("uick Return");
                }

                terminal.Write(", or ");
                terminal.SetColor("cyan");
                terminal.Write("[?]");
                terminal.SetColor("gray");
                terminal.WriteLine(" for help");
                await Task.Delay(2000);
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
    /// Show player status - Comprehensive character information display
    /// </summary>
    protected virtual async Task ShowStatus()
    {
        terminal.ClearScreen();

        // Header
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                           CHARACTER STATUS                                  ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Basic Info
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ BASIC INFORMATION ═══");
        terminal.SetColor("white");
        terminal.Write("Name: ");
        terminal.SetColor("bright_white");
        terminal.WriteLine(currentPlayer.DisplayName);

        terminal.SetColor("white");
        terminal.Write("Class: ");
        terminal.SetColor("bright_green");
        terminal.Write($"{currentPlayer.Class}");
        terminal.SetColor("white");
        terminal.Write("  |  Race: ");
        terminal.SetColor("bright_green");
        terminal.Write($"{currentPlayer.Race}");
        terminal.SetColor("white");
        terminal.Write("  |  Sex: ");
        terminal.SetColor("bright_green");
        terminal.WriteLine($"{(currentPlayer.Sex == CharacterSex.Male ? "Male" : "Female")}");

        terminal.SetColor("white");
        terminal.Write("Age: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.Age}");
        terminal.SetColor("white");
        terminal.Write("  |  Height: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.Height}cm");
        terminal.SetColor("white");
        terminal.Write("  |  Weight: ");
        terminal.SetColor("cyan");
        terminal.WriteLine($"{currentPlayer.Weight}kg");
        terminal.WriteLine("");

        // Level & Experience
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ LEVEL & EXPERIENCE ═══");
        terminal.SetColor("white");
        terminal.Write("Current Level: ");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine($"{currentPlayer.Level}");

        terminal.SetColor("white");
        terminal.Write("Experience: ");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine($"{currentPlayer.Experience:N0}");

        // Calculate XP needed for next level
        long nextLevelXP = GetExperienceForLevel(currentPlayer.Level + 1);
        long xpNeeded = nextLevelXP - currentPlayer.Experience;

        terminal.SetColor("white");
        terminal.Write("XP to Next Level: ");
        terminal.SetColor("bright_magenta");
        terminal.Write($"{xpNeeded:N0}");
        terminal.SetColor("gray");
        terminal.WriteLine($" (Need {nextLevelXP:N0} total)");
        terminal.WriteLine("");

        // Combat Stats
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ COMBAT STATISTICS ═══");
        terminal.SetColor("white");
        terminal.Write("HP: ");
        terminal.SetColor("bright_red");
        terminal.Write($"{currentPlayer.HP}");
        terminal.SetColor("white");
        terminal.Write("/");
        terminal.SetColor("red");
        terminal.WriteLine($"{currentPlayer.MaxHP}");

        if (currentPlayer.MaxMana > 0)
        {
            terminal.SetColor("white");
            terminal.Write("Mana: ");
            terminal.SetColor("bright_blue");
            terminal.Write($"{currentPlayer.Mana}");
            terminal.SetColor("white");
            terminal.Write("/");
            terminal.SetColor("blue");
            terminal.WriteLine($"{currentPlayer.MaxMana}");
        }

        terminal.SetColor("white");
        terminal.Write("Strength: ");
        terminal.SetColor("bright_green");
        terminal.Write($"{currentPlayer.Strength}");
        terminal.SetColor("white");
        terminal.Write("  |  Defence: ");
        terminal.SetColor("bright_green");
        terminal.Write($"{currentPlayer.Defence}");
        terminal.SetColor("white");
        terminal.Write("  |  Agility: ");
        terminal.SetColor("bright_green");
        terminal.WriteLine($"{currentPlayer.Agility}");

        terminal.SetColor("white");
        terminal.Write("Dexterity: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.Dexterity}");
        terminal.SetColor("white");
        terminal.Write("  |  Stamina: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.Stamina}");
        terminal.SetColor("white");
        terminal.Write("  |  Wisdom: ");
        terminal.SetColor("cyan");
        terminal.WriteLine($"{currentPlayer.Wisdom}");

        terminal.SetColor("white");
        terminal.Write("Intelligence: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.Intelligence}");
        terminal.SetColor("white");
        terminal.Write("  |  Charisma: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.Charisma}");
        terminal.SetColor("white");
        terminal.Write("  |  Constitution: ");
        terminal.SetColor("cyan");
        terminal.WriteLine($"{currentPlayer.Constitution}");
        terminal.WriteLine("");

        // Pagination - Page 1 break
        terminal.SetColor("gray");
        terminal.Write("Press Space to continue...");
        await terminal.GetInput("");
        terminal.WriteLine("");

        // Equipment
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ EQUIPMENT ═══");

        // Weapon
        terminal.SetColor("white");
        terminal.Write("Weapon: ");
        if (currentPlayer.Weapon > 0)
        {
            var weapon = ItemManager.GetClassicWeapon(currentPlayer.Weapon);
            if (weapon != null)
            {
                terminal.SetColor("bright_yellow");
                terminal.Write($"{weapon.Name}");
                terminal.SetColor("white");
                terminal.Write(" (Power: ");
                terminal.SetColor("yellow");
                terminal.Write($"{weapon.Power}");
                terminal.SetColor("white");
                terminal.WriteLine(")");
            }
            else
            {
                terminal.SetColor("gray");
                terminal.WriteLine($"Unknown weapon #{currentPlayer.Weapon}");
            }
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("None (Bare Hands)");
        }

        // Armor
        terminal.SetColor("white");
        terminal.Write("Armor: ");
        if (currentPlayer.Armor > 0)
        {
            var armor = ItemManager.GetClassicArmor(currentPlayer.Armor);
            if (armor != null)
            {
                terminal.SetColor("bright_cyan");
                terminal.Write($"{armor.Name}");
                terminal.SetColor("white");
                terminal.Write(" (AC: ");
                terminal.SetColor("cyan");
                terminal.Write($"{armor.Power}");
                terminal.SetColor("white");
                terminal.WriteLine(")");
            }
            else
            {
                terminal.SetColor("gray");
                terminal.WriteLine($"Unknown armor #{currentPlayer.Armor}");
            }
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("None (Unarmored)");
        }

        // Show active buffs if any
        if (currentPlayer.MagicACBonus > 0 || currentPlayer.DamageAbsorptionPool > 0 ||
            currentPlayer.IsRaging || currentPlayer.SmiteChargesRemaining > 0)
        {
            terminal.WriteLine("");
            terminal.SetColor("bright_magenta");
            terminal.WriteLine("Active Effects:");

            if (currentPlayer.MagicACBonus > 0)
            {
                terminal.SetColor("magenta");
                terminal.WriteLine($"  • Magic AC Bonus: +{currentPlayer.MagicACBonus}");
            }
            if (currentPlayer.DamageAbsorptionPool > 0)
            {
                terminal.SetColor("magenta");
                terminal.WriteLine($"  • Stoneskin: {currentPlayer.DamageAbsorptionPool} damage absorption");
            }
            if (currentPlayer.IsRaging)
            {
                terminal.SetColor("bright_red");
                terminal.WriteLine("  • RAGING! (+Strength, +HP, -AC)");
            }
            if (currentPlayer.SmiteChargesRemaining > 0)
            {
                terminal.SetColor("yellow");
                terminal.WriteLine($"  • Smite Evil: {currentPlayer.SmiteChargesRemaining} charges");
            }
        }
        terminal.WriteLine("");

        // Wealth
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ WEALTH ═══");
        terminal.SetColor("white");
        terminal.Write("Gold on Hand: ");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine($"{currentPlayer.Gold:N0}");

        terminal.SetColor("white");
        terminal.Write("Gold in Bank: ");
        terminal.SetColor("yellow");
        terminal.WriteLine($"{currentPlayer.BankGold:N0}");

        terminal.SetColor("white");
        terminal.Write("Total Wealth: ");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine($"{(currentPlayer.Gold + currentPlayer.BankGold):N0}");
        terminal.WriteLine("");

        // Pagination - Page 2 break
        terminal.SetColor("gray");
        terminal.Write("Press Space to continue...");
        await terminal.GetInput("");
        terminal.WriteLine("");

        // Relationships
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ RELATIONSHIPS ═══");
        terminal.SetColor("white");
        terminal.Write("Marital Status: ");
        if (currentPlayer.Married || currentPlayer.IsMarried)
        {
            terminal.SetColor("bright_magenta");
            terminal.Write("Married");
            if (!string.IsNullOrEmpty(currentPlayer.SpouseName))
            {
                terminal.SetColor("white");
                terminal.Write(" to ");
                terminal.SetColor("magenta");
                terminal.Write(currentPlayer.SpouseName);
            }
            terminal.WriteLine("");

            terminal.SetColor("white");
            terminal.Write("Children: ");
            terminal.SetColor("cyan");
            terminal.WriteLine($"{currentPlayer.Kids}");

            if (currentPlayer.Pregnancy > 0)
            {
                terminal.SetColor("white");
                terminal.Write("Pregnancy: ");
                terminal.SetColor("bright_cyan");
                terminal.WriteLine($"{currentPlayer.Pregnancy} days");
            }
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("Single");
        }

        terminal.SetColor("white");
        terminal.Write("Team: ");
        if (!string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine(currentPlayer.Team);
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("None");
        }
        terminal.WriteLine("");

        // Alignment & Reputation
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ ALIGNMENT & REPUTATION ═══");
        terminal.SetColor("white");
        terminal.Write("Chivalry: ");
        terminal.SetColor("bright_green");
        terminal.Write($"{currentPlayer.Chivalry}");
        terminal.SetColor("white");
        terminal.Write("  |  Darkness: ");
        terminal.SetColor("red");
        terminal.WriteLine($"{currentPlayer.Darkness}");

        terminal.SetColor("white");
        terminal.Write("Loyalty: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.Loyalty}%");
        terminal.SetColor("white");
        terminal.Write("  |  Mental Health: ");
        terminal.SetColor(currentPlayer.Mental >= 50 ? "green" : "red");
        terminal.WriteLine($"{currentPlayer.Mental}");

        if (currentPlayer.King)
        {
            terminal.SetColor("bright_yellow");
            terminal.WriteLine("👑 REIGNING MONARCH 👑");
        }
        terminal.WriteLine("");

        // Game Progress
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ GAME PROGRESS ═══");
        terminal.SetColor("white");
        terminal.Write("Turn Count: ");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine($"{currentPlayer.TurnCount}");

        terminal.SetColor("white");
        terminal.Write("Dungeon Fights: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.Fights}");
        terminal.SetColor("white");
        terminal.Write("  |  Player Fights: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.PFights}");
        terminal.SetColor("white");
        terminal.Write("  |  Team Fights: ");
        terminal.SetColor("cyan");
        terminal.WriteLine($"{currentPlayer.TFights}");

        terminal.SetColor("white");
        terminal.Write("Good Deeds: ");
        terminal.SetColor("green");
        terminal.Write($"{currentPlayer.ChivNr}");
        terminal.SetColor("white");
        terminal.Write("  |  Dark Deeds: ");
        terminal.SetColor("red");
        terminal.WriteLine($"{currentPlayer.DarkNr}");

        terminal.SetColor("white");
        terminal.Write("Thieveries: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.Thiefs}");
        terminal.SetColor("white");
        terminal.Write("  |  Assassinations: ");
        terminal.SetColor("cyan");
        terminal.Write($"{currentPlayer.Assa}");
        terminal.SetColor("white");
        terminal.Write("  |  Brawls: ");
        terminal.SetColor("cyan");
        terminal.WriteLine($"{currentPlayer.Brawls}");
        terminal.WriteLine("");

        // Pagination - Page 3 break
        terminal.SetColor("gray");
        terminal.Write("Press Space to continue...");
        await terminal.GetInput("");
        terminal.WriteLine("");

        // Battle Record
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ BATTLE RECORD ═══");
        terminal.SetColor("white");
        terminal.Write("Monster Kills: ");
        terminal.SetColor("bright_green");
        terminal.Write($"{currentPlayer.MKills}");
        terminal.SetColor("white");
        terminal.Write("  |  Monster Defeats: ");
        terminal.SetColor("red");
        terminal.WriteLine($"{currentPlayer.MDefeats}");

        terminal.SetColor("white");
        terminal.Write("Player Kills: ");
        terminal.SetColor("bright_yellow");
        terminal.Write($"{currentPlayer.PKills}");
        terminal.SetColor("white");
        terminal.Write("  |  Player Defeats: ");
        terminal.SetColor("red");
        terminal.WriteLine($"{currentPlayer.PDefeats}");

        // Calculate win rate
        long totalMonsterBattles = currentPlayer.MKills + currentPlayer.MDefeats;
        long totalPlayerBattles = currentPlayer.PKills + currentPlayer.PDefeats;

        if (totalMonsterBattles > 0)
        {
            double monsterWinRate = (double)currentPlayer.MKills / totalMonsterBattles * 100;
            terminal.SetColor("white");
            terminal.Write("Monster Win Rate: ");
            terminal.SetColor("cyan");
            terminal.WriteLine($"{monsterWinRate:F1}%");
        }

        if (totalPlayerBattles > 0)
        {
            double playerWinRate = (double)currentPlayer.PKills / totalPlayerBattles * 100;
            terminal.SetColor("white");
            terminal.Write("PvP Win Rate: ");
            terminal.SetColor("cyan");
            terminal.WriteLine($"{playerWinRate:F1}%");
        }
        terminal.WriteLine("");

        // Diseases & Afflictions
        if (currentPlayer.Blind || currentPlayer.Plague || currentPlayer.Smallpox ||
            currentPlayer.Measles || currentPlayer.Leprosy || currentPlayer.Poison > 0 ||
            currentPlayer.Addict > 0 || currentPlayer.Haunt > 0)
        {
            terminal.SetColor("bright_red");
            terminal.WriteLine("═══ AFFLICTIONS ═══");

            if (currentPlayer.Blind)
            {
                terminal.SetColor("red");
                terminal.WriteLine("  • Blind");
            }
            if (currentPlayer.Plague)
            {
                terminal.SetColor("red");
                terminal.WriteLine("  • Plague");
            }
            if (currentPlayer.Smallpox)
            {
                terminal.SetColor("red");
                terminal.WriteLine("  • Smallpox");
            }
            if (currentPlayer.Measles)
            {
                terminal.SetColor("red");
                terminal.WriteLine("  • Measles");
            }
            if (currentPlayer.Leprosy)
            {
                terminal.SetColor("red");
                terminal.WriteLine("  • Leprosy");
            }
            if (currentPlayer.Poison > 0)
            {
                terminal.SetColor("red");
                terminal.WriteLine($"  • Poisoned (Level {currentPlayer.Poison})");
            }
            if (currentPlayer.Addict > 0)
            {
                terminal.SetColor("red");
                terminal.WriteLine($"  • Addicted (Level {currentPlayer.Addict})");
            }
            if (currentPlayer.Haunt > 0)
            {
                terminal.SetColor("red");
                terminal.WriteLine($"  • Haunted by {currentPlayer.Haunt} demon(s)");
            }
            terminal.WriteLine("");
        }

        // Footer
        terminal.SetColor("gray");
        terminal.WriteLine("────────────────────────────────────────────────────────────────────────────────");

        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Calculate experience required for a given level (cumulative)
    /// </summary>
    private static long GetExperienceForLevel(int level)
    {
        if (level <= 1) return 0;
        long exp = 0;
        for (int i = 2; i <= level; i++)
        {
            exp += (long)(Math.Pow(i, 2.5) * 100);
        }
        return exp;
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

    // Convenience constructor for legacy classes that only provide name and skip description
    protected BaseLocation(GameLocation locationId, string name) : this(locationId, name, "")
    {
    }

    // Legacy constructor where parameters were (string name, GameLocation id)
    protected BaseLocation(string name, GameLocation locationId) : this(locationId, name, "")
    {
    }

    // Legacy constructor that passed only a name (defaults to NoWhere)
    protected BaseLocation(string name) : this(GameLocation.NoWhere, name, "")
    {
    }

    // Some pre-refactor code refers to LocationName instead of Name
    public string LocationName
    {
        get => Name;
        set => Name = value;
    }

    // ShortDescription used by some legacy locations
    public string ShortDescription { get; set; } = string.Empty;

    // Pascal fields expected by Prison/Temple legacy code
    public string LocationDescription { get; set; } = string.Empty;
    public HashSet<CharacterClass> AllowedClasses { get; set; } = new();
    public int LevelRequirement { get; set; } = 1;

    // Legacy single-parameter Enter wrapper
    public virtual async Task Enter(Character player)
    {
        await EnterLocation(player, TerminalEmulator.Instance ?? new TerminalEmulator());
    }

    // Legacy OnEnter hook – alias of DisplayLocation for now
    public virtual void OnEnter(Character player)
    {
        // For now simply display location header
        DisplayLocation();
    }

    // Allow derived locations to add menu options without maintaining their own list
    protected List<(string Key, string Text)> LegacyMenuOptions { get; } = new();

    public void AddMenuOption(string key, string text)
    {
        LegacyMenuOptions.Add((key, text));
    }

    // Stub for ShowLocationMenu used by some locations
    protected virtual void ShowLocationMenu()
    {
        // Basic menu display if terminal available
        if (terminal == null || LegacyMenuOptions.Count == 0) return;
        terminal.Clear();
        terminal.WriteLine($"{LocationName} Menu:");
        foreach (var (Key, Text) in LegacyMenuOptions)
        {
            terminal.WriteLine($"({Key}) {Text}");
        }
    }

    // Placeholder Ready method for Godot-style initialization
    public virtual void _Ready()
    {
        // No-op for standalone build
    }

    // Expose CurrentPlayer as Player for legacy code while still maintaining Character
    public Player? CurrentPlayer { get; protected set; }

    // Convenience GetNode wrapper (delegates to global helper)
    protected T GetNode<T>(string path) where T : class, new() => UsurperRemake.Utils.GodotHelpers.GetNode<T>(path);

    // Legacy exit helper used by some derived locations
    protected virtual async Task Exit(Player player)
    {
        // Simply break out by returning
        await Task.CompletedTask;
    }

    // Parameterless constructor retained for serialization or manual instantiation
    protected BaseLocation()
    {
        LocationId = GameLocation.NoWhere;
        Name = string.Empty;
        Description = string.Empty;
    }

    // Legacy helper referenced by some shop locations
    protected void ExitLocation()
    {
        // simply break – actual navigation handled by LocationManager
    }
} 
