using UsurperRemake.Utils;
using System;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Daily System Manager - Pascal-compatible daily system controller
/// Integrates with MaintenanceSystem for complete Pascal MAINT.PAS functionality
/// </summary>
public class DailySystemManager
{
    private DateTime lastResetTime;
    private DateTime gameStartTime;
    private int currentDay = 1;
    private MaintenanceSystem maintenanceSystem;
    private TerminalUI terminal;
    
    public int CurrentDay => currentDay;
    
    public DailySystemManager()
    {
        gameStartTime = DateTime.Now;
        lastResetTime = DateTime.Now;
        
        // Initialize with terminal from GameEngine when available
        var gameEngine = GameEngine.Instance;
        terminal = gameEngine?.Terminal;
        
        if (terminal != null)
        {
            maintenanceSystem = new MaintenanceSystem(terminal);
        }
    }
    
    /// <summary>
    /// Check and run daily maintenance if needed
    /// Pascal: Auto maintenance check integration
    /// </summary>
    public async Task CheckDailyReset()
    {
        var timeSinceReset = DateTime.Now - lastResetTime;
        
        // Check if 24 hours have passed or if it's past midnight
        if (timeSinceReset.TotalHours >= 20 || IsPastMidnight())
        {
            await PerformDailyReset();
        }
    }
    
    /// <summary>
    /// Force daily maintenance to run
    /// Pascal: /HMAINT command functionality
    /// </summary>
    public async Task ForceDailyMaintenance()
    {
        if (maintenanceSystem != null)
        {
            await maintenanceSystem.RunDailyMaintenance(forced: true);
        }
        else
        {
            await PerformDailyReset();
        }
    }
    
    /// <summary>
    /// Run daily maintenance - alias for ForceDailyMaintenance for API compatibility
    /// </summary>
    public async Task RunDailyMaintenance()
    {
        await ForceDailyMaintenance();
    }
    
    private bool IsPastMidnight()
    {
        var now = DateTime.Now;
        var lastMidnight = now.Date;
        var lastResetDate = lastResetTime.Date;
        
        return lastMidnight > lastResetDate;
    }
    
    /// <summary>
    /// Perform daily reset - now integrates with full maintenance system
    /// Pascal: Complete daily maintenance integration
    /// </summary>
    private async Task PerformDailyReset()
    {
        currentDay++;
        lastResetTime = DateTime.Now;
        
        // Use MaintenanceSystem for complete Pascal-compatible maintenance
        if (maintenanceSystem != null)
        {
            var maintenanceRan = await maintenanceSystem.CheckAndRunMaintenance();
            
            if (!maintenanceRan)
            {
                // Run basic daily reset if maintenance wasn't needed
                await RunBasicDailyReset();
            }
        }
        else
        {
            // Fallback to basic daily reset
            await RunBasicDailyReset();
        }
        
        // Clean up old mail
        MailSystem.CleanupOldMail();
        
        GD.Print($"[DailySystem] Day {currentDay} reset completed at {DateTime.Now}");
    }
    
    /// <summary>
    /// Run basic daily reset when full maintenance isn't available
    /// </summary>
    private async Task RunBasicDailyReset()
    {
        if (terminal != null)
        {
            terminal.WriteLine("", "white");
            terminal.WriteLine("═══════════════════════════════════════", "bright_blue");
            terminal.WriteLine($"        DAY {currentDay} BEGINS!", "bright_yellow");
            terminal.WriteLine("═══════════════════════════════════════", "bright_blue");
            terminal.WriteLine("", "white");
        }
        
        // Reset player daily parameters
        var player = GameEngine.Instance?.CurrentPlayer;
        if (player != null)
        {
            // Basic daily resets (Pascal compatibility)
            player.TurnsRemaining = 250;
            HagglingEngine.ResetDailyHaggling(player);
            
            // Reset daily attempt counters
            player.Fights = GameConfig.DefaultDungeonFights;
            player.PFights = GameConfig.DefaultPlayerFights;
            player.TFights = GameConfig.DefaultTeamFights;
            player.Thiefs = GameConfig.DefaultThiefAttempts;
            player.Brawls = GameConfig.DefaultBrawls;
            player.Assa = GameConfig.DefaultAssassinAttempts;
            
            terminal?.WriteLine($"Your daily limits have been restored!", "bright_green");
        }
        
        // Process daily events
        await ProcessDailyEvents();
        
        // Process bank maintenance
        BankLocation.ProcessDailyMaintenance();
    }
    
    private async Task ProcessDailyEvents()
    {
        var terminal = GameEngine.Instance?.Terminal;
        
        // Daily news and events
        var events = new[]
        {
            "The town crier announces the daily news.",
            "Merchants restock their wares for the new day.",
            "Guards begin their morning patrol routes.",
            "The tavern keeper opens for another day of business.",
            "Adventurers gather in the town square.",
            "The sun rises over the kingdom.",
            "Birds chirp as the day begins.",
            "The church bells ring to mark the new day."
        };
        
        if (terminal != null && GD.Randf() < 0.7f) // 70% chance for daily flavor text
        {
            var randomEvent = events[GD.RandRange(0, events.Length - 1)];
            terminal.WriteLine($"Morning News: {randomEvent}", "cyan");
            terminal.WriteLine("", "white");
        }
        
        // Special events based on day number
        if (currentDay % 7 == 0) // Weekly events
        {
            await ProcessWeeklyEvent();
        }
        
        if (currentDay % 30 == 0) // Monthly events
        {
            await ProcessMonthlyEvent();
        }
        
        // Check for random special events
        if (GD.Randf() < 0.15f) // 15% chance
        {
            await ProcessRandomDailyEvent();
        }
    }
    
    private async Task ProcessWeeklyEvent()
    {
        var terminal = GameEngine.Instance?.Terminal;
        var weeklyEvents = new[]
        {
            "Market Day: All merchants have special deals!",
            "Tournament Day: The arena hosts special competitions!",
            "Festival Day: The town celebrates with reduced prices!",
            "Guard Day: Extra security patrols the streets.",
            "Scholar Day: The library offers wisdom to all seekers."
        };
        
        var eventText = weeklyEvents[GD.RandRange(0, weeklyEvents.Length - 1)];
        terminal?.WriteLine($"Weekly Event: {eventText}", "bright_magenta");
        terminal?.WriteLine("", "white");
    }
    
    private async Task ProcessMonthlyEvent()
    {
        var terminal = GameEngine.Instance?.Terminal;
        var monthlyEvents = new[]
        {
            "Royal Decree: The king announces new policies!",
            "Grand Festival: The entire kingdom celebrates!",
            "Dungeon Awakening: Ancient evils stir in the depths!",
            "Merchant Caravan: Exotic goods arrive from distant lands!",
            "Mystical Convergence: Magic flows stronger today!"
        };
        
        var eventText = monthlyEvents[GD.RandRange(0, monthlyEvents.Length - 1)];
        terminal?.WriteLine($"MONTHLY EVENT: {eventText}", "bright_red");
        terminal?.WriteLine("", "white");
    }
    
    private async Task ProcessRandomDailyEvent()
    {
        var terminal = GameEngine.Instance?.Terminal;
        var player = GameEngine.Instance?.CurrentPlayer;
        
        var randomEvents = new[]
        {
            new { Text = "You find a small pouch of gold on the ground!", Action = () => player?.GainGold(GD.RandRange(10, 50)) },
            new { Text = "A mysterious stranger gives you a health potion!", Action = () => player?.Heal(GD.RandRange(10, 25)) },
            new { Text = "You feel particularly energetic today!", Action = () => player?.AddEffect("energy", 60) },
            new { Text = "A sage shares words of wisdom with you.", Action = () => player?.GainExperience(GD.RandRange(25, 75)) },
            new { Text = "You have a moment of clarity about your goals.", Action = () => { /* Boost to motivation */ } }
        };
        
        var randomEvent = randomEvents[GD.RandRange(0, randomEvents.Length - 1)];
        terminal?.WriteLine($"Random Event: {randomEvent.Text}", "bright_cyan");
        randomEvent.Action?.Invoke();
        
        if (terminal != null)
        {
            await terminal.PressAnyKey("Press any key to continue...");
        }
    }
    
    public string GetTimeStatus()
    {
        var uptime = DateTime.Now - gameStartTime;
        return $"Day {currentDay} | Game Time: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
    }
    
    public bool IsNewDay()
    {
        var timeSinceReset = DateTime.Now - lastResetTime;
        return timeSinceReset.TotalMinutes < 5; // New day if reset was less than 5 minutes ago
    }
    
    public TimeSpan GetTimeUntilNextReset()
    {
        var nextReset = lastResetTime.AddHours(20); // Minimum 20 hours between resets
        var now = DateTime.Now;
        
        if (nextReset <= now)
        {
            return TimeSpan.Zero;
        }
        
        return nextReset - now;
    }
}

// Simple config manager placeholder
public static class ConfigManager
{
    public static void LoadConfig()
    {
        // This would normally load from JSON and set static properties
        // For now, the GameConfig class already has default values
    }

    // Generic accessor placeholder so that legacy calls compile
    public static T GetConfig<T>(string key) => default!;
} 
