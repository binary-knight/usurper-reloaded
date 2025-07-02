using UsurperRemake.Utils;
using UsurperRemake.Systems;
using System;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Modern Daily System Manager - Flexible daily reset system for Steam single-player experience
/// Supports multiple daily cycle modes and integrates with comprehensive save system
/// </summary>
public class DailySystemManager
{
    private static DailySystemManager? instance;
    public static DailySystemManager Instance => instance ??= new DailySystemManager();
    
    private DateTime lastResetTime;
    private DateTime gameStartTime;
    private int currentDay = 1;
    private DailyCycleMode currentMode = DailyCycleMode.SessionBased;
    private MaintenanceSystem? maintenanceSystem;
    private TerminalUI? terminal;
    
    // Auto-save functionality
    private DateTime lastAutoSave;
    private TimeSpan autoSaveInterval = TimeSpan.FromMinutes(5);
    private bool autoSaveEnabled = true;
    
    public int CurrentDay => currentDay;
    public DailyCycleMode CurrentMode => currentMode;
    public DateTime LastResetTime => lastResetTime;
    public bool AutoSaveEnabled => autoSaveEnabled;
    
    public DailySystemManager()
    {
        gameStartTime = DateTime.Now;
        lastResetTime = DateTime.Now;
        lastAutoSave = DateTime.Now;
        
        // Initialize with terminal from GameEngine when available
        var gameEngine = GameEngine.Instance;
        terminal = gameEngine?.Terminal;
        
        if (terminal != null)
        {
            maintenanceSystem = new MaintenanceSystem(terminal);
        }
    }
    
    /// <summary>
    /// Set the daily cycle mode
    /// </summary>
    public void SetDailyCycleMode(DailyCycleMode mode)
    {
        if (currentMode != mode)
        {
            var oldMode = currentMode;
            currentMode = mode;
            
            terminal?.WriteLine($"Daily cycle mode changed from {oldMode} to {mode}", "bright_cyan");
            
            // Adjust reset time based on new mode
            AdjustResetTimeForMode();
        }
    }
    
    /// <summary>
    /// Check if a daily reset should occur based on current mode
    /// </summary>
    public bool ShouldPerformDailyReset()
    {
        var player = GameEngine.Instance?.CurrentPlayer;
        
        return currentMode switch
        {
            DailyCycleMode.SessionBased => player?.TurnsRemaining <= 0,
            DailyCycleMode.RealTime24Hour => DateTime.Now.Date > lastResetTime.Date,
            DailyCycleMode.Accelerated4Hour => DateTime.Now - lastResetTime >= TimeSpan.FromHours(4),
            DailyCycleMode.Accelerated8Hour => DateTime.Now - lastResetTime >= TimeSpan.FromHours(8),
            DailyCycleMode.Accelerated12Hour => DateTime.Now - lastResetTime >= TimeSpan.FromHours(12),
            DailyCycleMode.Endless => false, // Never reset in endless mode
            _ => false
        };
    }
    
    /// <summary>
    /// Check and run daily reset if needed (called periodically)
    /// </summary>
    public async Task CheckDailyReset()
    {
        if (ShouldPerformDailyReset())
        {
            await PerformDailyReset();
        }
        
        // Check for auto-save
        if (autoSaveEnabled && DateTime.Now - lastAutoSave >= autoSaveInterval)
        {
            await PerformAutoSave();
        }
    }
    
    /// <summary>
    /// Force daily reset to run immediately
    /// </summary>
    public async Task ForceDailyReset()
    {
        await PerformDailyReset(forced: true);
    }
    
    /// <summary>
    /// Perform daily reset based on current mode
    /// </summary>
    private async Task PerformDailyReset(bool forced = false)
    {
        var player = GameEngine.Instance?.CurrentPlayer;
        if (player == null) return;
        
        // Don't reset in endless mode unless forced
        if (currentMode == DailyCycleMode.Endless && !forced) return;
        
        // Increment day counter
        currentDay++;
        lastResetTime = DateTime.Now;
        
        // Display reset message
        await DisplayDailyResetMessage();
        
        // Use MaintenanceSystem for complete Pascal-compatible maintenance
        if (maintenanceSystem != null)
        {
            var maintenanceRan = await maintenanceSystem.CheckAndRunMaintenance(forced);
            
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
        
        // Process mode-specific resets
        await ProcessModeSpecificReset();
        
        // Clean up old mail
        MailSystem.CleanupOldMail();
        
        // Auto-save after reset
        if (autoSaveEnabled)
        {
            await SaveSystem.Instance.AutoSave(player);
        }
        
        GD.Print($"[DailySystem] Day {currentDay} reset completed at {DateTime.Now} (Mode: {currentMode})");
    }
    
    /// <summary>
    /// Display daily reset message based on mode
    /// </summary>
    private async Task DisplayDailyResetMessage()
    {
        if (terminal == null) return;
        
        terminal.WriteLine("", "white");
        terminal.WriteLine("═══════════════════════════════════════", "bright_blue");
        
        var message = currentMode switch
        {
            DailyCycleMode.SessionBased => $"        NEW SESSION BEGINS! (Day {currentDay})",
            DailyCycleMode.RealTime24Hour => $"        NEW DAY DAWNS! (Day {currentDay})",
            DailyCycleMode.Accelerated4Hour => $"        TIME ADVANCES! (Day {currentDay})",
            DailyCycleMode.Accelerated8Hour => $"        TIME ADVANCES! (Day {currentDay})",
            DailyCycleMode.Accelerated12Hour => $"        TIME ADVANCES! (Day {currentDay})",
            DailyCycleMode.Endless => $"        ENDLESS ADVENTURE CONTINUES! (Day {currentDay})",
            _ => $"        DAY {currentDay} BEGINS!"
        };
        
        terminal.WriteLine(message, "bright_yellow");
        terminal.WriteLine("═══════════════════════════════════════", "bright_blue");
        terminal.WriteLine("", "white");
        
        // Mode-specific flavor text
        var flavorText = currentMode switch
        {
            DailyCycleMode.SessionBased => "Your strength and resolve have been restored!",
            DailyCycleMode.RealTime24Hour => "The sun rises on a new day of adventure!",
            DailyCycleMode.Accelerated4Hour => "Time flows swiftly in this realm!",
            DailyCycleMode.Accelerated8Hour => "The hours pass quickly here!",
            DailyCycleMode.Accelerated12Hour => "Day and night cycle rapidly!",
            DailyCycleMode.Endless => "Time has no meaning in your endless quest!",
            _ => "A new day of adventure awaits!"
        };
        
        terminal.WriteLine(flavorText, "cyan");
        terminal.WriteLine("", "white");
        
        await Task.Delay(1000);
    }
    
    /// <summary>
    /// Run basic daily reset when full maintenance isn't available
    /// </summary>
    private async Task RunBasicDailyReset()
    {
        var player = GameEngine.Instance?.CurrentPlayer;
        if (player == null) return;
        
        // Reset daily parameters based on mode
        if (currentMode != DailyCycleMode.Endless)
        {
            // Restore turns based on mode
            var turnsToRestore = currentMode switch
            {
                DailyCycleMode.SessionBased => GameConfig.TurnsPerDay,
                DailyCycleMode.RealTime24Hour => GameConfig.TurnsPerDay,
                DailyCycleMode.Accelerated4Hour => GameConfig.TurnsPerDay / 6, // Reduced for faster cycles
                DailyCycleMode.Accelerated8Hour => GameConfig.TurnsPerDay / 3,
                DailyCycleMode.Accelerated12Hour => GameConfig.TurnsPerDay / 2,
                _ => GameConfig.TurnsPerDay
            };
            
            player.TurnsRemaining = turnsToRestore;
            
            // Reset daily attempt counters
            player.Fights = GameConfig.DefaultDungeonFights;
            player.PFights = GameConfig.DefaultPlayerFights;
            player.TFights = GameConfig.DefaultTeamFights;
            player.Thiefs = GameConfig.DefaultThiefAttempts;
            player.Brawls = GameConfig.DefaultBrawls;
            player.Assa = GameConfig.DefaultAssassinAttempts;
            
            // Reset class daily abilities
            player.IsRaging = false; // Rage refreshed each combat anyway, but ensure off
            if (player.Class == CharacterClass.Paladin)
            {
                var mods = player.GetClassCombatModifiers();
                player.SmiteChargesRemaining = mods.SmiteCharges;
            }
            else
            {
                player.SmiteChargesRemaining = 0;
            }
            
            // Reset haggling attempts
            HagglingEngine.ResetDailyHaggling(player);
            
            terminal?.WriteLine($"Your daily limits have been restored! ({turnsToRestore} turns)", "bright_green");
        }
        else
        {
            // In endless mode, just give a small turn boost if needed
            if (player.TurnsRemaining < 50)
            {
                player.TurnsRemaining += 25;
                terminal?.WriteLine("Your energy has been partially restored!", "bright_green");
            }
        }
        
        // Process daily events
        await ProcessDailyEvents();
        
        // Process bank maintenance
        BankLocation.ProcessDailyMaintenance();
    }
    
    /// <summary>
    /// Process mode-specific reset logic
    /// </summary>
    private async Task ProcessModeSpecificReset()
    {
        switch (currentMode)
        {
            case DailyCycleMode.SessionBased:
                await ProcessSessionBasedReset();
                break;
                
            case DailyCycleMode.RealTime24Hour:
                await ProcessRealTimeReset();
                break;
                
            case DailyCycleMode.Accelerated4Hour:
            case DailyCycleMode.Accelerated8Hour:
            case DailyCycleMode.Accelerated12Hour:
                await ProcessAcceleratedReset();
                break;
                
            case DailyCycleMode.Endless:
                await ProcessEndlessReset();
                break;
        }
    }
    
    private async Task ProcessSessionBasedReset()
    {
        terminal?.WriteLine("Session-based reset: Ready for a new adventure session!", "bright_cyan");
        
        // Process NPCs during player absence (minimal)
        await ProcessNPCsDuringAbsence(TimeSpan.FromHours(1)); // Assume 1 hour offline
    }
    
    private async Task ProcessRealTimeReset()
    {
        var timeSinceLastReset = DateTime.Now - lastResetTime;
        terminal?.WriteLine($"Real-time reset: {timeSinceLastReset.TotalHours:F1} hours have passed!", "bright_cyan");
        
        // Process NPCs during real-time absence
        await ProcessNPCsDuringAbsence(timeSinceLastReset);
        
        // Process world events that occurred during absence
        await ProcessWorldEventsDuringAbsence(timeSinceLastReset);
    }
    
    private async Task ProcessAcceleratedReset()
    {
        var cycleName = currentMode switch
        {
            DailyCycleMode.Accelerated4Hour => "4-hour",
            DailyCycleMode.Accelerated8Hour => "8-hour",
            DailyCycleMode.Accelerated12Hour => "12-hour",
            _ => "accelerated"
        };
        
        terminal?.WriteLine($"Accelerated reset: {cycleName} cycle completed!", "bright_cyan");
        
        // Process accelerated world simulation
        var simulatedTime = currentMode switch
        {
            DailyCycleMode.Accelerated4Hour => TimeSpan.FromHours(4),
            DailyCycleMode.Accelerated8Hour => TimeSpan.FromHours(8),
            DailyCycleMode.Accelerated12Hour => TimeSpan.FromHours(12),
            _ => TimeSpan.FromHours(6)
        };
        
        await ProcessNPCsDuringAbsence(simulatedTime);
    }
    
    private async Task ProcessEndlessReset()
    {
        terminal?.WriteLine("Endless mode: Time flows differently here...", "bright_magenta");
        
        // In endless mode, still process some world simulation but less frequently
        if (currentDay % 7 == 0) // Weekly world updates
        {
            await ProcessNPCsDuringAbsence(TimeSpan.FromDays(1));
        }
    }
    
    /// <summary>
    /// Process NPC activities during player absence
    /// </summary>
    private async Task ProcessNPCsDuringAbsence(TimeSpan timeSpan)
    {
        // Note: EnhancedNPCSystem doesn't have an Instance property or ProcessTimePassage method
        // In a full implementation, this would process NPC activities during player absence
        terminal?.WriteLine($"NPCs have been active during your absence ({timeSpan.TotalHours:F1} hours simulated)", "yellow");
    }
    
    /// <summary>
    /// Process world events during player absence
    /// </summary>
    private async Task ProcessWorldEventsDuringAbsence(TimeSpan timeSpan)
    {
        // Note: WorldSimulator doesn't have an Instance property or SimulateTimePassage method
        // In a full implementation, this would simulate world events during player absence
        terminal?.WriteLine("World events have unfolded in your absence!", "yellow");
    }
    
    /// <summary>
    /// Perform auto-save
    /// </summary>
    private async Task PerformAutoSave()
    {
        var player = GameEngine.Instance?.CurrentPlayer;
        if (player != null)
        {
            var success = await SaveSystem.Instance.AutoSave(player);
            if (success)
            {
                lastAutoSave = DateTime.Now;
                terminal?.WriteLine("Auto-saved", "gray");
            }
        }
    }
    
    /// <summary>
    /// Adjust reset time when mode changes
    /// </summary>
    private void AdjustResetTimeForMode()
    {
        // Adjust the last reset time to prevent immediate reset when changing modes
        lastResetTime = currentMode switch
        {
            DailyCycleMode.SessionBased => DateTime.Now, // Reset immediately available
            DailyCycleMode.RealTime24Hour => DateTime.Now.Date, // Next reset at midnight
            DailyCycleMode.Accelerated4Hour => DateTime.Now, // Start new cycle
            DailyCycleMode.Accelerated8Hour => DateTime.Now,
            DailyCycleMode.Accelerated12Hour => DateTime.Now,
            DailyCycleMode.Endless => DateTime.Now.AddDays(1), // Delay next reset
            _ => DateTime.Now
        };
    }
    
    /// <summary>
    /// Load daily system state from save data
    /// </summary>
    public void LoadFromSaveData(SaveGameData saveData)
    {
        currentDay = saveData.CurrentDay;
        lastResetTime = saveData.LastDailyReset;
        currentMode = saveData.DailyCycleMode;
        autoSaveEnabled = saveData.Settings.AutoSaveEnabled;
        autoSaveInterval = saveData.Settings.AutoSaveInterval;
        
        GD.Print($"Daily system loaded: Day {currentDay}, Mode {currentMode}");
    }
    
    /// <summary>
    /// Configure auto-save settings
    /// </summary>
    public void ConfigureAutoSave(bool enabled, TimeSpan interval)
    {
        autoSaveEnabled = enabled;
        autoSaveInterval = interval;
        
        terminal?.WriteLine($"Auto-save {(enabled ? "enabled" : "disabled")}" + 
                          (enabled ? $" (every {interval.TotalMinutes} minutes)" : ""), "cyan");
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
            "You find a small pouch of gold on the ground!",
            "A mysterious stranger shares a healing potion with you!",
            "You feel particularly energetic today!",
            "A sage shares words of wisdom with you.",
            "You have a moment of clarity about your goals."
        };
        
        var randomEvent = randomEvents[GD.RandRange(0, randomEvents.Length - 1)];
        terminal?.WriteLine($"Random Event: {randomEvent}", "bright_cyan");
        
        if (terminal != null)
        {
            await terminal.PressAnyKey("Press any key to continue...");
        }
    }
    
    public string GetTimeStatus()
    {
        var uptime = DateTime.Now - gameStartTime;
        var modeText = currentMode switch
        {
            DailyCycleMode.SessionBased => "Session",
            DailyCycleMode.RealTime24Hour => "Real-time",
            DailyCycleMode.Accelerated4Hour => "Fast (4h)",
            DailyCycleMode.Accelerated8Hour => "Fast (8h)",
            DailyCycleMode.Accelerated12Hour => "Fast (12h)",
            DailyCycleMode.Endless => "Endless",
            _ => "Unknown"
        };
        
        return $"Day {currentDay} | Mode: {modeText} | Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
    }
    
    public bool IsNewDay()
    {
        var timeSinceReset = DateTime.Now - lastResetTime;
        return timeSinceReset.TotalMinutes < 5; // New day if reset was less than 5 minutes ago
    }
    
    public TimeSpan GetTimeUntilNextReset()
    {
        return currentMode switch
        {
            DailyCycleMode.SessionBased => TimeSpan.Zero, // Available when turns run out
            DailyCycleMode.RealTime24Hour => DateTime.Now.Date.AddDays(1) - DateTime.Now,
            DailyCycleMode.Accelerated4Hour => lastResetTime.AddHours(4) - DateTime.Now,
            DailyCycleMode.Accelerated8Hour => lastResetTime.AddHours(8) - DateTime.Now,
            DailyCycleMode.Accelerated12Hour => lastResetTime.AddHours(12) - DateTime.Now,
            DailyCycleMode.Endless => TimeSpan.MaxValue, // Never resets
            _ => TimeSpan.Zero
        };
    }
}

// Simple config manager placeholder
public static partial class ConfigManager
{
    public static void LoadConfig()
    {
        // This would normally load from JSON and set static properties
        // For now, the GameConfig class already has default values
    }

    // Generic accessor placeholder so that legacy calls compile
    public static T GetConfig<T>(string key) => default!;
} 
