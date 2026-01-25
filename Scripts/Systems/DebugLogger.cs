using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Comprehensive debug logging system for tracking game events.
    /// Writes to debug.log file for post-mortem debugging when players report issues.
    /// </summary>
    public class DebugLogger
    {
        private static DebugLogger? _instance;
        public static DebugLogger Instance => _instance ??= new DebugLogger();

        public enum LogLevel
        {
            Debug,   // Verbose debugging info
            Info,    // Normal game events
            Warning, // Potential issues
            Error    // Actual errors
        }

        private readonly string logFilePath;
        private readonly string logDirectory;
        private readonly object writeLock = new();
        private readonly ConcurrentQueue<string> logQueue = new();
        private readonly Timer flushTimer;
        private const int MaxLogSizeBytes = 5 * 1024 * 1024; // 5MB max before rotation
        private const int MaxBackupFiles = 3;
        private bool isEnabled = true;
        private LogLevel minimumLevel = LogLevel.Info;

        // Session tracking
        private readonly string sessionId;
        private readonly DateTime sessionStart;

        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        public LogLevel MinimumLevel
        {
            get => minimumLevel;
            set => minimumLevel = value;
        }

        public DebugLogger()
        {
            _instance = this;
            sessionId = Guid.NewGuid().ToString("N")[..8];
            sessionStart = DateTime.Now;

            // Store logs in a logs subfolder
            logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logDirectory);
            logFilePath = Path.Combine(logDirectory, "debug.log");

            // Flush queue every 2 seconds
            flushTimer = new Timer(_ => FlushQueue(), null, 2000, 2000);

            // Log session start
            LogRaw($"\n{'=',-80}");
            LogRaw($"SESSION START: {sessionStart:yyyy-MM-dd HH:mm:ss} | ID: {sessionId}");
            LogRaw($"Version: {GameConfig.Version} | Platform: {Environment.OSVersion}");
            LogRaw($"{'=',-80}\n");
        }

        private void LogRaw(string message)
        {
            if (!isEnabled) return;
            logQueue.Enqueue(message);
        }

        public void Log(LogLevel level, string category, string message)
        {
            if (!isEnabled || level < minimumLevel) return;

            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var levelStr = level switch
            {
                LogLevel.Debug => "DBG",
                LogLevel.Info => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                _ => "???"
            };

            var logLine = $"[{timestamp}] [{levelStr}] [{category}] {message}";
            logQueue.Enqueue(logLine);

            // Also write errors to stderr for immediate visibility
            if (level == LogLevel.Error)
            {
                Console.Error.WriteLine(logLine);
            }
        }

        private void FlushQueue()
        {
            if (logQueue.IsEmpty) return;

            var sb = new StringBuilder();
            while (logQueue.TryDequeue(out var line))
            {
                sb.AppendLine(line);
            }

            if (sb.Length == 0) return;

            lock (writeLock)
            {
                try
                {
                    // Check for rotation
                    if (File.Exists(logFilePath))
                    {
                        var fileInfo = new FileInfo(logFilePath);
                        if (fileInfo.Length > MaxLogSizeBytes)
                        {
                            RotateLogs();
                        }
                    }

                    File.AppendAllText(logFilePath, sb.ToString());
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[DebugLogger] Failed to write log: {ex.Message}");
                }
            }
        }

        private void RotateLogs()
        {
            try
            {
                // Delete oldest backup
                var oldestBackup = Path.Combine(logDirectory, $"debug.{MaxBackupFiles}.log");
                if (File.Exists(oldestBackup))
                    File.Delete(oldestBackup);

                // Rotate existing backups
                for (int i = MaxBackupFiles - 1; i >= 1; i--)
                {
                    var current = Path.Combine(logDirectory, $"debug.{i}.log");
                    var next = Path.Combine(logDirectory, $"debug.{i + 1}.log");
                    if (File.Exists(current))
                        File.Move(current, next);
                }

                // Move current to .1
                var backup1 = Path.Combine(logDirectory, "debug.1.log");
                if (File.Exists(logFilePath))
                    File.Move(logFilePath, backup1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DebugLogger] Failed to rotate logs: {ex.Message}");
            }
        }

        public void Flush()
        {
            FlushQueue();
        }

        // ═══════════════════════════════════════════════════════════════
        // Convenience methods for common game events
        // ═══════════════════════════════════════════════════════════════

        public void LogDebug(string category, string message) => Log(LogLevel.Debug, category, message);
        public void LogInfo(string category, string message) => Log(LogLevel.Info, category, message);
        public void LogWarning(string category, string message) => Log(LogLevel.Warning, category, message);
        public void LogError(string category, string message) => Log(LogLevel.Error, category, message);

        // Save/Load events
        public void LogSave(string playerName, int level, long hp, long maxHp, long gold)
        {
            LogInfo("SAVE", $"Saving '{playerName}' - Level:{level} HP:{hp}/{maxHp} Gold:{gold}");
        }

        public void LogLoad(string playerName, int level, long hp, long maxHp, long gold)
        {
            LogInfo("LOAD", $"Loaded '{playerName}' - Level:{level} HP:{hp}/{maxHp} Gold:{gold}");
        }

        public void LogAutoSave(string playerName)
        {
            LogDebug("SAVE", $"AutoSave triggered for '{playerName}'");
        }

        // Combat events
        public void LogCombatStart(string playerName, int playerLevel, string[] enemies, int dungeonFloor)
        {
            var enemyList = string.Join(", ", enemies);
            LogInfo("COMBAT", $"Battle started - {playerName} (Lv{playerLevel}) vs [{enemyList}] on floor {dungeonFloor}");
        }

        public void LogCombatEnd(string outcome, long xpGained, long goldGained, int roundsPlayed)
        {
            LogInfo("COMBAT", $"Battle {outcome} - XP:{xpGained} Gold:{goldGained} Rounds:{roundsPlayed}");
        }

        public void LogCombatDamage(string attacker, string defender, long damage, bool isCritical)
        {
            var critStr = isCritical ? " (CRIT)" : "";
            LogDebug("COMBAT", $"{attacker} -> {defender}: {damage} damage{critStr}");
        }

        public void LogPlayerDeath(string playerName, string cause, int dungeonFloor)
        {
            LogWarning("DEATH", $"Player '{playerName}' died on floor {dungeonFloor} - Cause: {cause}");
        }

        // Character events
        public void LogLevelUp(string characterName, int oldLevel, int newLevel)
        {
            LogInfo("LEVEL", $"{characterName} leveled up: {oldLevel} -> {newLevel}");
        }

        public void LogStatChange(string characterName, string stat, long oldValue, long newValue, string reason)
        {
            LogDebug("STATS", $"{characterName} {stat}: {oldValue} -> {newValue} ({reason})");
        }

        public void LogEquip(string characterName, string itemName, string slot)
        {
            LogInfo("EQUIP", $"{characterName} equipped '{itemName}' in {slot}");
        }

        public void LogUnequip(string characterName, string itemName, string slot)
        {
            LogInfo("EQUIP", $"{characterName} unequipped '{itemName}' from {slot}");
        }

        // Economy events
        public void LogPurchase(string playerName, string itemName, long cost, long goldAfter)
        {
            LogInfo("SHOP", $"{playerName} bought '{itemName}' for {cost}g (Balance: {goldAfter}g)");
        }

        public void LogSale(string playerName, string itemName, long price, long goldAfter)
        {
            LogInfo("SHOP", $"{playerName} sold '{itemName}' for {price}g (Balance: {goldAfter}g)");
        }

        public void LogGoldChange(string playerName, long oldGold, long newGold, string reason)
        {
            var diff = newGold - oldGold;
            var sign = diff >= 0 ? "+" : "";
            LogDebug("GOLD", $"{playerName}: {oldGold} -> {newGold} ({sign}{diff}) - {reason}");
        }

        // Location events
        public void LogLocationChange(string playerName, string fromLocation, string toLocation)
        {
            LogDebug("LOCATION", $"{playerName} moved: {fromLocation} -> {toLocation}");
        }

        public void LogDungeonFloorChange(string playerName, int fromFloor, int toFloor)
        {
            LogInfo("DUNGEON", $"{playerName} floor change: {fromFloor} -> {toFloor}");
        }

        // Companion events
        public void LogCompanionRecruit(string companionName, int playerLevel)
        {
            LogInfo("COMPANION", $"Recruited {companionName} at player level {playerLevel}");
        }

        public void LogCompanionDeath(string companionName, string deathType)
        {
            LogWarning("COMPANION", $"{companionName} died permanently - Type: {deathType}");
        }

        public void LogCompanionLevelUp(string companionName, int oldLevel, int newLevel)
        {
            LogInfo("COMPANION", $"{companionName} leveled up: {oldLevel} -> {newLevel}");
        }

        // Party events
        public void LogPartyChange(string action, string memberName, int partySize)
        {
            LogInfo("PARTY", $"{action}: {memberName} (Party size: {partySize})");
        }

        // Quest/Story events
        public void LogQuestStart(string questName)
        {
            LogInfo("QUEST", $"Started: {questName}");
        }

        public void LogQuestComplete(string questName)
        {
            LogInfo("QUEST", $"Completed: {questName}");
        }

        public void LogAchievement(string achievementName)
        {
            LogInfo("ACHIEVE", $"Unlocked: {achievementName}");
        }

        public void LogStoryEvent(string eventDescription)
        {
            LogInfo("STORY", eventDescription);
        }

        // System events
        public void LogSystemError(string system, string error, string? stackTrace = null)
        {
            LogError(system, error);
            if (!string.IsNullOrEmpty(stackTrace))
            {
                LogError(system, $"Stack trace:\n{stackTrace}");
            }
        }

        public void LogGameStart(string playerName, bool isNewGame)
        {
            var type = isNewGame ? "New game" : "Continued game";
            LogInfo("GAME", $"{type} started for '{playerName}'");
        }

        public void LogGameExit(string playerName, string exitMethod)
        {
            LogInfo("GAME", $"Game exited for '{playerName}' via {exitMethod}");
            Flush(); // Ensure all logs are written before exit
        }

        // NPC events
        public void LogNPCInteraction(string npcName, string interactionType)
        {
            LogDebug("NPC", $"Interaction with {npcName}: {interactionType}");
        }

        public void LogNPCDeath(string npcName, string cause)
        {
            LogInfo("NPC", $"{npcName} died - Cause: {cause}");
        }

        public void LogNPCRespawn(string npcName)
        {
            LogDebug("NPC", $"{npcName} respawned");
        }

        // Daily system events
        public void LogDailyReset(int newDay)
        {
            LogInfo("DAILY", $"Day advanced to {newDay}");
        }

        // Boss events
        public void LogBossEncounter(string bossName, int floor)
        {
            LogInfo("BOSS", $"Encountered {bossName} on floor {floor}");
        }

        public void LogBossDefeated(string bossName, string outcome)
        {
            LogInfo("BOSS", $"{bossName} - Outcome: {outcome}");
        }

        // Seal events
        public void LogSealCollected(int sealNumber, int floor)
        {
            LogInfo("SEAL", $"Collected Seal #{sealNumber} on floor {floor}");
        }

        // Session end
        public void LogSessionEnd()
        {
            var duration = DateTime.Now - sessionStart;
            LogRaw($"\n{'=',-80}");
            LogRaw($"SESSION END: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogRaw($"Duration: {duration.Hours}h {duration.Minutes}m {duration.Seconds}s");
            LogRaw($"{'=',-80}\n");
            Flush();
        }
    }
}
