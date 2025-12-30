using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Comprehensive save/load system for Usurper Reloaded
    /// Supports multiple daily cycle modes and complete world state persistence
    /// </summary>
    public class SaveSystem
    {
        private static SaveSystem? instance;
        public static SaveSystem Instance => instance ??= new SaveSystem();
        
        private readonly string saveDirectory;
        private readonly JsonSerializerOptions jsonOptions;
        
        public SaveSystem()
        {
            // Use Godot's user data directory for cross-platform compatibility
            saveDirectory = Path.Combine(GetUserDataPath(), "saves");
            
            // Ensure save directory exists
            Directory.CreateDirectory(saveDirectory);
            
            // Configure JSON serialization
            jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IncludeFields = true
            };
        }
        
        /// <summary>
        /// Save complete game state including player, world, and NPCs
        /// </summary>
        public async Task<bool> SaveGame(string playerName, Character player)
        {
            try
            {
                var saveData = new SaveGameData
                {
                    Version = GameConfig.SaveVersion,
                    SaveTime = DateTime.Now,
                    LastDailyReset = DailySystemManager.Instance.LastResetTime,
                    CurrentDay = DailySystemManager.Instance.CurrentDay,
                    DailyCycleMode = DailySystemManager.Instance.CurrentMode,
                    Player = SerializePlayer(player),
                    NPCs = await SerializeNPCs(),
                    WorldState = SerializeWorldState(),
                    Settings = SerializeDailySettings()
                };
                
                var fileName = GetSaveFileName(playerName);
                var filePath = Path.Combine(saveDirectory, fileName);
                var json = JsonSerializer.Serialize(saveData, jsonOptions);
                
                await File.WriteAllTextAsync(filePath, json);
                
                GD.Print($"Game saved successfully: {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to save game: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load complete game state
        /// </summary>
        public async Task<SaveGameData?> LoadGame(string playerName)
        {
            try
            {
                var fileName = GetSaveFileName(playerName);
                var filePath = Path.Combine(saveDirectory, fileName);
                
                if (!File.Exists(filePath))
                {
                    return null;
                }
                
                var json = await File.ReadAllTextAsync(filePath);
                var saveData = JsonSerializer.Deserialize<SaveGameData>(json, jsonOptions);
                
                if (saveData == null)
                {
                    GD.PrintErr("Failed to deserialize save data");
                    return null;
                }
                
                // Validate save version compatibility
                if (saveData.Version < GameConfig.MinSaveVersion)
                {
                    GD.PrintErr($"Save file version {saveData.Version} is too old (minimum: {GameConfig.MinSaveVersion})");
                    return null;
                }
                
                GD.Print($"Game loaded successfully: {fileName}");
                return saveData;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to load game: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Check if a save file exists for the player
        /// </summary>
        public bool SaveExists(string playerName)
        {
            var fileName = GetSaveFileName(playerName);
            var filePath = Path.Combine(saveDirectory, fileName);
            return File.Exists(filePath);
        }
        
        /// <summary>
        /// Delete a save file
        /// </summary>
        public bool DeleteSave(string playerName)
        {
            try
            {
                var fileName = GetSaveFileName(playerName);
                var filePath = Path.Combine(saveDirectory, fileName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    GD.Print($"Save file deleted: {fileName}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to delete save: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get list of all save files
        /// </summary>
        public List<SaveInfo> GetAllSaves()
        {
            var saves = new List<SaveInfo>();
            
            try
            {
                var files = Directory.GetFiles(saveDirectory, "*.json");
                
                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var saveData = JsonSerializer.Deserialize<SaveGameData>(json, jsonOptions);
                        
                        if (saveData?.Player != null)
                        {
                            saves.Add(new SaveInfo
                            {
                                PlayerName = saveData.Player.Name2 ?? saveData.Player.Name1,
                                SaveTime = saveData.SaveTime,
                                Level = saveData.Player.Level,
                                CurrentDay = saveData.CurrentDay,
                                TurnsRemaining = saveData.Player.TurnsRemaining,
                                FileName = Path.GetFileName(file)
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"Failed to read save file {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to enumerate save files: {ex.Message}");
            }
            
            return saves;
        }
        
        /// <summary>
        /// Auto-save the current game state with rotation (keeps 5 most recent autosaves)
        /// </summary>
        public async Task<bool> AutoSave(Character player)
        {
            if (player == null) return false;

            var playerName = player.Name2 ?? player.Name1;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var autosaveName = $"{playerName}_autosave_{timestamp}";

            // Save the new autosave
            var success = await SaveGame(autosaveName, player);

            if (success)
            {
                // Rotate autosaves - keep only the 5 most recent
                RotateAutosaves(playerName);
            }

            return success;
        }

        /// <summary>
        /// Rotate autosaves to keep only the 5 most recent
        /// </summary>
        private void RotateAutosaves(string playerName)
        {
            try
            {
                // Get all autosaves for this player
                var autosavePattern = $"{string.Join("_", playerName.Split(Path.GetInvalidFileNameChars()))}_autosave_*.json";
                var autosaveFiles = Directory.GetFiles(saveDirectory, autosavePattern);

                // Sort by creation time, newest first
                var sortedFiles = autosaveFiles
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                // Delete all but the 5 most recent
                for (int i = 5; i < sortedFiles.Count; i++)
                {
                    sortedFiles[i].Delete();
                    GD.Print($"Deleted old autosave: {sortedFiles[i].Name}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to rotate autosaves: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all saves for a specific player (including autosaves)
        /// </summary>
        public List<SaveInfo> GetPlayerSaves(string playerName)
        {
            var saves = new List<SaveInfo>();
            var sanitizedName = string.Join("_", playerName.Split(Path.GetInvalidFileNameChars()));

            try
            {
                // Get all saves for this player (manual saves and autosaves)
                var pattern = $"{sanitizedName}*.json";
                var files = Directory.GetFiles(saveDirectory, pattern);

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var saveData = JsonSerializer.Deserialize<SaveGameData>(json, jsonOptions);

                        if (saveData?.Player != null)
                        {
                            var fileName = Path.GetFileName(file);
                            var isAutosave = fileName.Contains("_autosave_");

                            saves.Add(new SaveInfo
                            {
                                PlayerName = saveData.Player.Name2 ?? saveData.Player.Name1,
                                SaveTime = saveData.SaveTime,
                                Level = saveData.Player.Level,
                                CurrentDay = saveData.CurrentDay,
                                TurnsRemaining = saveData.Player.TurnsRemaining,
                                FileName = fileName,
                                IsAutosave = isAutosave,
                                SaveType = isAutosave ? "Autosave" : "Manual Save"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"Failed to read save file {file}: {ex.Message}");
                    }
                }

                // Sort by save time, newest first
                saves = saves.OrderByDescending(s => s.SaveTime).ToList();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to get player saves: {ex.Message}");
            }

            return saves;
        }

        /// <summary>
        /// Get the most recent save for a player (autosave or manual)
        /// </summary>
        public SaveInfo? GetMostRecentSave(string playerName)
        {
            var saves = GetPlayerSaves(playerName);
            return saves.FirstOrDefault();
        }

        /// <summary>
        /// Load a save by filename
        /// </summary>
        public async Task<SaveGameData?> LoadSaveByFileName(string fileName)
        {
            try
            {
                var filePath = Path.Combine(saveDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var saveData = JsonSerializer.Deserialize<SaveGameData>(json, jsonOptions);

                if (saveData == null)
                {
                    GD.PrintErr("Failed to deserialize save data");
                    return null;
                }

                // Validate save version compatibility
                if (saveData.Version < GameConfig.MinSaveVersion)
                {
                    GD.PrintErr($"Save file version {saveData.Version} is too old (minimum: {GameConfig.MinSaveVersion})");
                    return null;
                }

                GD.Print($"Game loaded successfully: {fileName}");
                return saveData;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to load game: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get list of all unique player names that have saves
        /// </summary>
        public List<string> GetAllPlayerNames()
        {
            var playerNames = new HashSet<string>();

            try
            {
                var files = Directory.GetFiles(saveDirectory, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var saveData = JsonSerializer.Deserialize<SaveGameData>(json, jsonOptions);

                        if (saveData?.Player != null)
                        {
                            var playerName = saveData.Player.Name2 ?? saveData.Player.Name1;
                            if (!string.IsNullOrWhiteSpace(playerName))
                            {
                                playerNames.Add(playerName);
                            }
                        }
                    }
                    catch
                    {
                        // Skip invalid save files
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to enumerate player names: {ex.Message}");
            }

            return playerNames.OrderBy(n => n).ToList();
        }
        
        /// <summary>
        /// Create backup of existing save before overwriting
        /// </summary>
        public void CreateBackup(string playerName)
        {
            try
            {
                var fileName = GetSaveFileName(playerName);
                var filePath = Path.Combine(saveDirectory, fileName);
                
                if (File.Exists(filePath))
                {
                    var backupPath = Path.Combine(saveDirectory, $"{Path.GetFileNameWithoutExtension(fileName)}_backup.json");
                    File.Copy(filePath, backupPath, true);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to create backup: {ex.Message}");
            }
        }
        
        private string GetSaveFileName(string playerName)
        {
            // Sanitize player name for file system
            var sanitized = string.Join("_", playerName.Split(Path.GetInvalidFileNameChars()));
            return $"{sanitized}.json";
        }
        
        private PlayerData SerializePlayer(Character player)
        {
            return new PlayerData
            {
                // Basic info
                Name1 = player.Name1,
                Name2 = player.Name2,
                RealName = (player as Player)?.RealName ?? player.Name1,
                
                // Core stats
                Level = player.Level,
                Experience = player.Experience,
                HP = player.HP,
                MaxHP = player.MaxHP,
                Gold = player.Gold,
                BankGold = player.BankGold,
                
                // Attributes
                Strength = player.Strength,
                Defence = player.Defence,
                Stamina = player.Stamina,
                Agility = player.Agility,
                Charisma = player.Charisma,
                Dexterity = player.Dexterity,
                Wisdom = player.Wisdom,
                Intelligence = player.Intelligence,
                Constitution = player.Constitution,
                Mana = player.Mana,
                MaxMana = player.MaxMana,

                // Equipment and items (CRITICAL FIXES)
                Healing = player.Healing,     // POTIONS
                WeapPow = player.WeapPow,     // WEAPON POWER
                ArmPow = player.ArmPow,       // ARMOR POWER
                
                // Character details
                Race = player.Race,
                Class = player.Class,
                Sex = (char)((int)player.Sex),
                Age = player.Age,
                
                // Game state
                CurrentLocation = player.Location.ToString(),
                TurnsRemaining = player.TurnsRemaining,
                DaysInPrison = player.DaysInPrison,
                
                // Daily limits
                Fights = player.Fights,
                PFights = player.PFights,
                TFights = player.TFights,
                Thiefs = player.Thiefs,
                Brawls = player.Brawls,
                Assa = player.Assa,
                
                // Items and equipment
                Items = player.Item?.ToArray() ?? new int[0],
                ItemTypes = player.ItemType?.Select(t => (int)t).ToArray() ?? new int[0],

                // NEW: Modern RPG Equipment System
                EquippedItems = player.EquippedItems?.ToDictionary(
                    kvp => (int)kvp.Key,
                    kvp => kvp.Value
                ) ?? new Dictionary<int, int>(),

                // Base stats
                BaseStrength = player.BaseStrength,
                BaseDexterity = player.BaseDexterity,
                BaseConstitution = player.BaseConstitution,
                BaseIntelligence = player.BaseIntelligence,
                BaseWisdom = player.BaseWisdom,
                BaseCharisma = player.BaseCharisma,
                BaseMaxHP = player.BaseMaxHP,
                BaseMaxMana = player.BaseMaxMana,
                BaseDefence = player.BaseDefence,
                BaseStamina = player.BaseStamina,
                BaseAgility = player.BaseAgility,

                // Social/Team
                Team = player.Team,
                TeamPassword = player.TeamPW,
                IsTeamLeader = player.CTurf,
                TeamRec = player.TeamRec,
                BGuard = player.BGuard,

                // Status
                Chivalry = player.Chivalry,
                Darkness = player.Darkness,
                Mental = player.Mental,
                Poison = player.Poison,
                GnollP = player.GnollP,
                Addict = player.Addict,
                Mercy = player.Mercy,

                // Disease status
                Blind = player.Blind,
                Plague = player.Plague,
                Smallpox = player.Smallpox,
                Measles = player.Measles,
                Leprosy = player.Leprosy,

                // Character settings
                AutoHeal = player.AutoHeal,
                Loyalty = player.Loyalty,
                Haunt = player.Haunt,
                Master = player.Master,
                WellWish = player.WellWish,

                // Physical appearance
                Height = player.Height,
                Weight = player.Weight,
                Eyes = player.Eyes,
                Hair = player.Hair,
                Skin = player.Skin,

                // Character flavor text
                Phrases = player.Phrases?.ToList() ?? new List<string>(),
                Description = player.Description?.ToList() ?? new List<string>(),
                
                // Relationships
                Relationships = SerializeRelationships(player),
                
                // Quests
                ActiveQuests = SerializeActiveQuests(player),
                
                // Achievements (for Player type)
                Achievements = (player as Player)?.Achievements ?? new Dictionary<string, bool>(),
                
                // Timestamps
                LastLogin = (player as Player)?.LastLogin ?? DateTime.Now,
                AccountCreated = (player as Player)?.AccountCreated ?? DateTime.Now
            };
        }
        
        private async Task<List<NPCData>> SerializeNPCs()
        {
            var npcData = new List<NPCData>();
            
            // Get all NPCs from the world - use GameEngine or WorldSimulator to access NPCs
            var gameEngine = GameEngine.Instance;
            if (gameEngine?.CurrentPlayer != null)
            {
                // For now, we'll create a placeholder implementation
                // In a full implementation, we'd get NPCs from the world state
                var worldNPCs = GetWorldNPCs(); // Helper method to get NPCs
                
                foreach (var npc in worldNPCs)
                {
                    npcData.Add(new NPCData
                    {
                        Id = npc.Id ?? Guid.NewGuid().ToString(),
                        Name = npc.Name2 ?? npc.Name1,
                        Level = npc.Level,
                        HP = npc.HP,
                        MaxHP = npc.MaxHP,
                        Location = npc.Location.ToString(),
                        
                        // AI state
                        PersonalityProfile = SerializePersonality(npc.Brain?.Personality),
                        Memories = SerializeMemories(npc.Brain?.Memory),
                        CurrentGoals = SerializeGoals(npc.Brain?.Goals),
                        EmotionalState = SerializeEmotionalState(npc.Brain?.Emotions),
                        
                        // Relationships
                        Relationships = SerializeNPCRelationships(npc),
                        
                        // Inventory
                        Gold = npc.Gold,
                        Items = npc.Item?.ToArray() ?? new int[0]
                    });
                }
            }
            
            return npcData;
        }
        
        /// <summary>
        /// Helper method to get NPCs from the world
        /// This would be implemented to access the actual NPC collection
        /// </summary>
        private List<NPC> GetWorldNPCs()
        {
            // For now, return empty list - in full implementation, this would
            // access the world's NPC collection through WorldSimulator or similar
            return new List<NPC>();
        }
        
        private WorldStateData SerializeWorldState()
        {
            return new WorldStateData
            {
                // Economic state
                BankInterestRate = GameConfig.DefaultBankInterest,
                TownPotValue = GameConfig.DefaultTownPot,
                
                // Political state
                CurrentRuler = GameEngine.Instance?.CurrentPlayer?.King == true ? 
                              GameEngine.Instance.CurrentPlayer.Name2 : null,
                
                // World events
                ActiveEvents = SerializeActiveEvents(),
                
                // Shop inventories
                ShopInventories = SerializeShopInventories(),
                
                // News and history
                RecentNews = SerializeRecentNews(),
                
                // God system state
                GodStates = SerializeGodStates()
            };
        }
        
        private DailySettings SerializeDailySettings()
        {
            return new DailySettings
            {
                Mode = DailySystemManager.Instance.CurrentMode,
                LastResetTime = DailySystemManager.Instance.LastResetTime,
                AutoSaveEnabled = true,
                AutoSaveInterval = TimeSpan.FromMinutes(5)
            };
        }
        
        // Helper methods for serialization
        private Dictionary<string, float> SerializeRelationships(Character player)
        {
            // This would integrate with the relationship system
            return new Dictionary<string, float>();
        }
        
        private List<QuestData> SerializeActiveQuests(Character player)
        {
            // This would integrate with the quest system
            return new List<QuestData>();
        }
        
        private PersonalityData? SerializePersonality(PersonalityProfile? profile)
        {
            if (profile == null) return null;
            
            return new PersonalityData
            {
                Aggression = profile.Aggression,
                Loyalty = profile.Loyalty,
                Intelligence = profile.Intelligence,
                Greed = profile.Greed,
                Compassion = profile.Sociability, // Use Sociability as Compassion
                Courage = profile.Courage,
                Honesty = profile.Trustworthiness, // Use Trustworthiness as Honesty
                Ambition = profile.Ambition
            };
        }
        
        private List<MemoryData> SerializeMemories(MemorySystem? memory)
        {
            // This would serialize NPC memories
            return new List<MemoryData>();
        }
        
        private List<GoalData> SerializeGoals(GoalSystem? goals)
        {
            // This would serialize NPC goals
            return new List<GoalData>();
        }
        
        private EmotionalStateData? SerializeEmotionalState(EmotionalState? state)
        {
            if (state == null) return null;
            
            return new EmotionalStateData
            {
                Happiness = state.GetEmotionIntensity(EmotionType.Joy),
                Anger = state.GetEmotionIntensity(EmotionType.Anger),
                Fear = state.GetEmotionIntensity(EmotionType.Fear),
                Trust = state.GetEmotionIntensity(EmotionType.Gratitude) // Use Gratitude as Trust
            };
        }
        
        private Dictionary<string, float> SerializeNPCRelationships(NPC npc)
        {
            // This would serialize NPC relationships
            return new Dictionary<string, float>();
        }
        
        private List<WorldEventData> SerializeActiveEvents()
        {
            // This would serialize active world events
            return new List<WorldEventData>();
        }
        
        private Dictionary<string, ShopInventoryData> SerializeShopInventories()
        {
            // This would serialize shop inventories
            return new Dictionary<string, ShopInventoryData>();
        }
        
        private List<NewsEntryData> SerializeRecentNews()
        {
            // This would serialize recent news
            return new List<NewsEntryData>();
        }
        
        private Dictionary<string, GodStateData> SerializeGodStates()
        {
            // This would serialize god states
            return new Dictionary<string, GodStateData>();
        }
        
        /// <summary>
        /// Get cross-platform user data directory
        /// </summary>
        private string GetUserDataPath()
        {
            // For console mode, use platform-specific directories
            var appName = "UsurperReloaded";
            
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), appName);
            }
            else if (System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var home = System.Environment.GetEnvironmentVariable("HOME");
                return Path.Combine(home ?? "/tmp", ".local", "share", appName);
            }
            else
            {
                // Mac or other - use application support
                var home = System.Environment.GetEnvironmentVariable("HOME");
                return Path.Combine(home ?? "/tmp", "Library", "Application Support", appName);
            }
        }
    }
} 