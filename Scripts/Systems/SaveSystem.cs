using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using UsurperRemake.BBS;

namespace UsurperRemake.Systems
{
    /// <summary>
    /// Comprehensive save/load system for Usurper Reloaded
    /// Supports multiple daily cycle modes and complete world state persistence
    /// Supports BBS door mode with per-BBS save isolation
    /// </summary>
    public class SaveSystem
    {
        private static SaveSystem? instance;
        public static SaveSystem Instance => instance ??= new SaveSystem();

        private readonly string baseSaveDirectory;
        private readonly JsonSerializerOptions jsonOptions;

        /// <summary>
        /// Get the active save directory (includes BBS namespace if in door mode)
        /// </summary>
        public string saveDirectory
        {
            get
            {
                var bbsNamespace = DoorMode.GetSaveNamespace();
                if (!string.IsNullOrEmpty(bbsNamespace))
                {
                    var bbsDir = Path.Combine(baseSaveDirectory, bbsNamespace);
                    Directory.CreateDirectory(bbsDir);
                    return bbsDir;
                }
                return baseSaveDirectory;
            }
        }

        public SaveSystem()
        {
            // Use Godot's user data directory for cross-platform compatibility
            baseSaveDirectory = Path.Combine(GetUserDataPath(), "saves");

            // Ensure base save directory exists
            Directory.CreateDirectory(baseSaveDirectory);

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
                // Create backup of existing save before overwriting
                CreateBackup(playerName);

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
                    Settings = SerializeDailySettings(),
                    StorySystems = SerializeStorySystems()
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
                Difficulty = player.Difficulty,
                
                // Game state
                CurrentLocation = player.Location.ToString(),
                TurnsRemaining = player.TurnsRemaining,
                DaysInPrison = player.DaysInPrison,
                CellDoorOpen = player.CellDoorOpen,
                RescuedBy = player.RescuedBy ?? "",
                PrisonEscapes = player.PrisonEscapes,

                // Daily limits
                Fights = player.Fights,
                PFights = player.PFights,
                TFights = player.TFights,
                Thiefs = player.Thiefs,
                Brawls = player.Brawls,
                Assa = player.Assa,
                DarkNr = player.DarkNr,
                ChivNr = player.ChivNr,
                
                // Items and equipment
                Items = player.Item?.ToArray() ?? new int[0],
                ItemTypes = player.ItemType?.Select(t => (int)t).ToArray() ?? new int[0],

                // NEW: Modern RPG Equipment System
                EquippedItems = player.EquippedItems?.ToDictionary(
                    kvp => (int)kvp.Key,
                    kvp => kvp.Value
                ) ?? new Dictionary<int, int>(),

                // Curse status for equipped items
                WeaponCursed = player.WeaponCursed,
                ArmorCursed = player.ArmorCursed,
                ShieldCursed = player.ShieldCursed,

                // Player inventory (dungeon loot items)
                Inventory = player.Inventory?.Select(item => new InventoryItemData
                {
                    Name = item.Name,
                    Value = item.Value,
                    Type = item.Type,
                    Attack = item.Attack,
                    Armor = item.Armor,
                    Strength = item.Strength,
                    Dexterity = item.Dexterity,
                    Wisdom = item.Wisdom,
                    Defence = item.Defence,
                    HP = item.HP,
                    Mana = item.Mana,
                    Charisma = item.Charisma,
                    MinLevel = item.MinLevel,
                    IsCursed = item.IsCursed,
                    Cursed = item.Cursed,
                    Shop = item.Shop,
                    Dungeon = item.Dungeon,
                    Description = item.Description?.ToList() ?? new List<string>()
                }).ToList() ?? new List<InventoryItemData>(),

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

                // Active status effects (convert enum keys to int)
                ActiveStatuses = player.ActiveStatuses?.ToDictionary(
                    kvp => (int)kvp.Key,
                    kvp => kvp.Value
                ) ?? new Dictionary<int, int>(),

                GnollP = player.GnollP,
                Addict = player.Addict,
                SteroidDays = player.SteroidDays,
                DrugEffectDays = player.DrugEffectDays,
                ActiveDrug = (int)player.ActiveDrug,
                Mercy = player.Mercy,

                // Disease status
                Blind = player.Blind,
                Plague = player.Plague,
                Smallpox = player.Smallpox,
                Measles = player.Measles,
                Leprosy = player.Leprosy,
                LoversBane = player.LoversBane,

                // Combat statistics (kill/death counts)
                MKills = (int)player.MKills,
                MDefeats = (int)player.MDefeats,
                PKills = (int)player.PKills,
                PDefeats = (int)player.PDefeats,

                // Character settings
                AutoHeal = player.AutoHeal,
                CombatSpeed = player.CombatSpeed,
                SkipIntimateScenes = player.SkipIntimateScenes,
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

                // Romance Tracker Data
                RomanceData = RomanceTracker.Instance.ToSaveData(),

                // Quests
                ActiveQuests = SerializeActiveQuests(player),
                
                // Achievements (for Player type)
                Achievements = (player as Player)?.Achievements ?? new Dictionary<string, bool>(),

                // Learned combat abilities
                LearnedAbilities = player.LearnedAbilities?.ToList() ?? new List<string>(),

                // Training system
                Trains = player.Trains,
                TrainingPoints = player.TrainingPoints,
                SkillProficiencies = player.SkillProficiencies?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (int)kvp.Value) ?? new Dictionary<string, int>(),
                SkillTrainingProgress = player.SkillTrainingProgress ?? new Dictionary<string, int>(),

                // Spells and skills
                Spells = player.Spell ?? new List<List<bool>>(),
                Skills = player.Skill ?? new List<int>(),

                // Legacy equipment slots
                LHand = player.LHand,
                RHand = player.RHand,
                Head = player.Head,
                Body = player.Body,
                Arms = player.Arms,
                LFinger = player.LFinger,
                RFinger = player.RFinger,
                Legs = player.Legs,
                Feet = player.Feet,
                Waist = player.Waist,
                Neck = player.Neck,
                Neck2 = player.Neck2,
                Face = player.Face,
                Shield = player.Shield,
                Hands = player.Hands,
                ABody = player.ABody,

                // Combat flags
                Immortal = player.Immortal,
                BattleCry = player.BattleCry ?? "",
                BGuardNr = player.BGuardNr,

                // Timestamps
                LastLogin = (player as Player)?.LastLogin ?? DateTime.Now,
                AccountCreated = (player as Player)?.AccountCreated ?? DateTime.Now,

                // Gym cooldown timers
                LastStrengthTraining = player.LastStrengthTraining,
                LastDexterityTraining = player.LastDexterityTraining,
                LastTugOfWar = player.LastTugOfWar,
                LastWrestling = player.LastWrestling,

                // Player statistics - update session time before saving
                Statistics = UpdateAndGetStatistics(player),

                // Player achievements
                AchievementsData = SerializeAchievements(player),

                // Home Upgrade System (Gold Sinks)
                HomeLevel = player.HomeLevel,
                ChestLevel = player.ChestLevel,
                TrainingRoomLevel = player.TrainingRoomLevel,
                GardenLevel = player.GardenLevel,
                HasTrophyRoom = player.HasTrophyRoom,
                HasTeleportCircle = player.HasTeleportCircle,
                HasLegendaryArmory = player.HasLegendaryArmory,
                HasVitalityFountain = player.HasVitalityFountain,
                PermanentDamageBonus = player.PermanentDamageBonus,
                PermanentDefenseBonus = player.PermanentDefenseBonus,
                BonusMaxHP = player.BonusMaxHP,

                // Recurring Duelist Rival
                RecurringDuelist = SerializeRecurringDuelist(player)
            };
        }

        /// <summary>
        /// Serialize recurring duelist data for saving
        /// </summary>
        private DuelistData? SerializeRecurringDuelist(Character player)
        {
            string playerId = player.ID ?? player.Name;
            var duelistData = DungeonLocation.GetRecurringDuelist(playerId);
            if (duelistData.HasValue)
            {
                var duelist = duelistData.Value;
                return new DuelistData
                {
                    Name = duelist.Name,
                    Weapon = duelist.Weapon,
                    Level = duelist.Level,
                    TimesEncountered = duelist.TimesEncountered,
                    PlayerWins = duelist.PlayerWins,
                    PlayerLosses = duelist.PlayerLosses,
                    WasInsulted = duelist.WasInsulted,
                    IsDead = duelist.IsDead
                };
            }
            return null;
        }

        /// <summary>
        /// Serialize player achievements for saving
        /// </summary>
        private PlayerAchievementsData SerializeAchievements(Character player)
        {
            return new PlayerAchievementsData
            {
                UnlockedAchievements = new HashSet<string>(player.Achievements.UnlockedAchievements),
                UnlockDates = new Dictionary<string, DateTime>(player.Achievements.UnlockDates)
            };
        }

        /// <summary>
        /// Update statistics session time and return for saving
        /// </summary>
        private PlayerStatistics UpdateAndGetStatistics(Character player)
        {
            player.Statistics.UpdateSessionTime();
            return player.Statistics;
        }
        
        private async Task<List<NPCData>> SerializeNPCs()
        {
            var npcData = new List<NPCData>();

            // Get all NPCs from NPCSpawnSystem
            var worldNPCs = GetWorldNPCs();

            // Get current king for reference
            var currentKing = global::CastleLocation.GetCurrentKing();

            foreach (var npc in worldNPCs)
            {
                npcData.Add(new NPCData
                {
                    Id = npc.Id ?? Guid.NewGuid().ToString(),
                    CharacterID = npc.ID ?? "",  // Save the Character.ID property (used by RomanceTracker)
                    Name = npc.Name2 ?? npc.Name1,
                    Archetype = npc.Archetype ?? "citizen",
                    Level = npc.Level,
                    HP = npc.HP,
                    MaxHP = npc.MaxHP,
                    Location = npc.CurrentLocation ?? npc.Location.ToString(),

                    // Character stats
                    Experience = npc.Experience,
                    Strength = npc.Strength,
                    Defence = npc.Defence,
                    Agility = npc.Agility,
                    Dexterity = npc.Dexterity,
                    Mana = npc.Mana,
                    MaxMana = npc.MaxMana,
                    WeapPow = npc.WeapPow,
                    ArmPow = npc.ArmPow,

                    // Class and race
                    Class = npc.Class,
                    Race = npc.Race,
                    Sex = (char)npc.Sex,

                    // Team and political status - CRITICAL for persistence
                    Team = npc.Team ?? "",
                    IsTeamLeader = npc.CTurf,
                    IsKing = currentKing != null && currentKing.Name == npc.Name,

                    // Death status - permanent death tracking
                    IsDead = npc.IsDead,

                    // Alignment
                    Chivalry = npc.Chivalry,
                    Darkness = npc.Darkness,

                    // AI state
                    PersonalityProfile = SerializePersonality(npc.Brain?.Personality),
                    Memories = SerializeMemories(npc.Brain?.Memory),
                    CurrentGoals = SerializeGoals(npc.Brain?.Goals),
                    EmotionalState = SerializeEmotionalState(npc.Brain?.Emotions),

                    // Relationships
                    Relationships = SerializeNPCRelationships(npc),

                    // Inventory
                    Gold = npc.Gold,
                    Items = npc.Item?.ToArray() ?? new int[0],

                    // Market inventory for NPC trading
                    MarketInventory = npc.MarketInventory?.Select(item => new MarketItemData
                    {
                        ItemName = item.Name,
                        ItemValue = item.Value,
                        ItemType = item.Type,
                        Attack = item.Attack,
                        Armor = item.Armor,
                        Strength = item.Strength,
                        Defence = item.Defence,
                        IsCursed = item.IsCursed
                    }).ToList() ?? new List<MarketItemData>()
                });
            }

            await Task.CompletedTask;
            return npcData;
        }
        
        /// <summary>
        /// Helper method to get NPCs from the world
        /// </summary>
        private List<NPC> GetWorldNPCs()
        {
            // Get all active NPCs from NPCSpawnSystem
            return NPCSpawnSystem.Instance?.ActiveNPCs ?? new List<NPC>();
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

                // Active quests
                ActiveQuests = SerializeActiveQuests(GameEngine.Instance?.CurrentPlayer),

                // Shop inventories
                ShopInventories = SerializeShopInventories(),

                // News and history
                RecentNews = SerializeRecentNews(),

                // God system state
                GodStates = SerializeGodStates(),

                // Marketplace listings
                MarketplaceListings = MarketplaceSystem.Instance.ToSaveData()
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
        
        private List<QuestData> SerializeActiveQuests(Character? player)
        {
            var questDataList = new List<QuestData>();

            // Get all active quests from the quest system
            var allQuests = QuestSystem.GetAllQuests(includeCompleted: false);

            foreach (var quest in allQuests)
            {
                var questData = new QuestData
                {
                    Id = quest.Id,
                    Title = quest.Title,
                    Initiator = quest.Initiator,
                    Comment = quest.Comment,
                    Status = quest.Deleted ? QuestStatus.Completed :
                             string.IsNullOrEmpty(quest.Occupier) ? QuestStatus.Active : QuestStatus.Active,
                    StartTime = quest.Date,
                    QuestType = (int)quest.QuestType,
                    QuestTarget = (int)quest.QuestTarget,
                    Difficulty = quest.Difficulty,
                    Occupier = quest.Occupier,
                    OccupiedDays = quest.OccupiedDays,
                    DaysToComplete = quest.DaysToComplete,
                    MinLevel = quest.MinLevel,
                    MaxLevel = quest.MaxLevel,
                    Reward = quest.Reward,
                    RewardType = (int)quest.RewardType,
                    Penalty = quest.Penalty,
                    PenaltyType = (int)quest.PenaltyType,
                    OfferedTo = quest.OfferedTo,
                    Forced = quest.Forced,
                    Objectives = new List<QuestObjectiveData>(),
                    Monsters = new List<QuestMonsterData>()
                };

                // Serialize objectives
                foreach (var objective in quest.Objectives)
                {
                    questData.Objectives.Add(new QuestObjectiveData
                    {
                        Id = objective.Id,
                        Description = objective.Description,
                        ObjectiveType = (int)objective.ObjectiveType,
                        TargetId = objective.TargetId,
                        TargetName = objective.TargetName,
                        RequiredProgress = objective.RequiredProgress,
                        CurrentProgress = objective.CurrentProgress,
                        IsOptional = objective.IsOptional,
                        BonusReward = objective.BonusReward
                    });
                }

                // Serialize monsters
                foreach (var monster in quest.Monsters)
                {
                    questData.Monsters.Add(new QuestMonsterData
                    {
                        MonsterType = monster.MonsterType,
                        Count = monster.Count,
                        MonsterName = monster.MonsterName
                    });
                }

                questDataList.Add(questData);
            }

            GD.Print($"[SaveSystem] Serialized {questDataList.Count} active quests");
            return questDataList;
        }
        
        private PersonalityData? SerializePersonality(PersonalityProfile? profile)
        {
            if (profile == null) return null;

            return new PersonalityData
            {
                // Core traits
                Aggression = profile.Aggression,
                Loyalty = profile.Loyalty,
                Intelligence = profile.Intelligence,
                Greed = profile.Greed,
                Compassion = profile.Sociability, // Use Sociability as Compassion
                Courage = profile.Courage,
                Honesty = profile.Trustworthiness, // Use Trustworthiness as Honesty
                Ambition = profile.Ambition,
                Vengefulness = profile.Vengefulness,
                Impulsiveness = profile.Impulsiveness,
                Caution = profile.Caution,
                Mysticism = profile.Mysticism,
                Patience = profile.Patience,

                // Romance/Intimacy traits
                Gender = profile.Gender,
                Orientation = profile.Orientation,
                IntimateStyle = profile.IntimateStyle,
                RelationshipPref = profile.RelationshipPref,
                Romanticism = profile.Romanticism,
                Sensuality = profile.Sensuality,
                Jealousy = profile.Jealousy,
                Commitment = profile.Commitment,
                Adventurousness = profile.Adventurousness,
                Exhibitionism = profile.Exhibitionism,
                Voyeurism = profile.Voyeurism,
                Flirtatiousness = profile.Flirtatiousness,
                Passion = profile.Passion,
                Tenderness = profile.Tenderness
            };
        }
        
        private List<MemoryData> SerializeMemories(MemorySystem? memory)
        {
            if (memory == null) return new List<MemoryData>();

            return memory.AllMemories.Select(m => new MemoryData
            {
                Type = m.Type.ToString(),
                Description = m.Description,
                InvolvedCharacter = m.InvolvedCharacter ?? "",
                Importance = m.Importance,
                EmotionalImpact = m.EmotionalImpact,
                Timestamp = m.Timestamp
            }).ToList();
        }

        private List<GoalData> SerializeGoals(GoalSystem? goals)
        {
            if (goals == null) return new List<GoalData>();

            return goals.AllGoals.Select(g => new GoalData
            {
                Name = g.Name,
                Type = g.Type.ToString(),
                Priority = g.Priority,
                Progress = g.Progress,
                IsActive = g.IsActive,
                TargetValue = g.TargetValue,
                CurrentValue = g.CurrentValue
            }).ToList();
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
            var eventDataList = new List<WorldEventData>();
            var activeEvents = WorldEventSystem.Instance.GetActiveEvents();

            foreach (var evt in activeEvents)
            {
                var eventData = new WorldEventData
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = evt.Type.ToString(),
                    Title = evt.Title,
                    Description = evt.Description,
                    StartTime = DateTime.Now.AddDays(-evt.StartDay),
                    EndTime = DateTime.Now.AddDays(evt.DaysRemaining),
                    Parameters = new Dictionary<string, object>
                    {
                        ["DaysRemaining"] = evt.DaysRemaining,
                        ["StartDay"] = evt.StartDay
                    }
                };

                // Add effect parameters
                foreach (var effect in evt.Effects)
                {
                    eventData.Parameters[$"Effect_{effect.Key}"] = effect.Value;
                }

                eventDataList.Add(eventData);
            }

            // Also save global modifier state
            if (eventDataList.Count > 0 || WorldEventSystem.Instance.PlaguActive ||
                WorldEventSystem.Instance.WarActive || WorldEventSystem.Instance.FestivalActive)
            {
                var stateData = new WorldEventData
                {
                    Id = "GLOBAL_STATE",
                    Type = "GlobalState",
                    Title = "World State",
                    Description = WorldEventSystem.Instance.CurrentKingDecree,
                    Parameters = new Dictionary<string, object>
                    {
                        ["PlaguActive"] = WorldEventSystem.Instance.PlaguActive,
                        ["WarActive"] = WorldEventSystem.Instance.WarActive,
                        ["FestivalActive"] = WorldEventSystem.Instance.FestivalActive,
                        ["GlobalPriceModifier"] = WorldEventSystem.Instance.GlobalPriceModifier,
                        ["GlobalXPModifier"] = WorldEventSystem.Instance.GlobalXPModifier,
                        ["GlobalGoldModifier"] = WorldEventSystem.Instance.GlobalGoldModifier,
                        ["GlobalStatModifier"] = WorldEventSystem.Instance.GlobalStatModifier
                    }
                };
                eventDataList.Add(stateData);
            }

            return eventDataList;
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

        /// <summary>
        /// Serialize all story systems state
        /// Note: Uses reflection to safely access properties that may or may not exist
        /// </summary>
        private StorySystemsData SerializeStorySystems()
        {
            var data = new StorySystemsData();

            // Ocean Philosophy - save awakening level and collected fragments
            try
            {
                var ocean = OceanPhilosophySystem.Instance;
                data.AwakeningLevel = ocean.AwakeningLevel;
                data.CollectedFragments = ocean.CollectedFragments.Select(f => (int)f).ToList();
                data.ExperiencedMoments = ocean.ExperiencedMoments.Select(m => (int)m).ToList();
            }
            catch { /* System not initialized */ }

            // Grief System - save current stage
            try
            {
                var grief = GriefSystem.Instance;
                data.GriefStage = (int)grief.CurrentStage;
            }
            catch { /* System not initialized */ }

            // Story Progression - save cycle count, seals, and story flags
            try
            {
                var story = StoryProgressionSystem.Instance;
                data.CurrentCycle = story.CurrentCycle;
                data.CollectedSeals = story.CollectedSeals.Select(s => (int)s).ToList();
                data.StoryFlags = new Dictionary<string, bool>(story.StoryFlags);
            }
            catch { /* System not initialized */ }

            // God System - save player worship data
            try
            {
                var godSystem = UsurperRemake.GodSystemSingleton.Instance;
                var godData = godSystem.ToDictionary();
                if (godData.ContainsKey("PlayerGods") && godData["PlayerGods"] is Dictionary<string, string> playerGodDict)
                {
                    data.PlayerGods = new Dictionary<string, string>(playerGodDict);
                }
            }
            catch { /* System not initialized */ }

            // Companion System - save companion states
            try
            {
                var companionData = CompanionSystem.Instance.Serialize();

                // Convert CompanionSaveData to CompanionSaveInfo for storage
                data.Companions = companionData.CompanionStates.Select(c => new CompanionSaveInfo
                {
                    Id = (int)c.Id,
                    IsRecruited = c.IsRecruited,
                    IsActive = c.IsActive,
                    IsDead = c.IsDead,
                    LoyaltyLevel = c.LoyaltyLevel,
                    TrustLevel = c.TrustLevel,
                    RomanceLevel = c.RomanceLevel,
                    PersonalQuestStarted = c.PersonalQuestStarted,
                    PersonalQuestCompleted = c.PersonalQuestCompleted,
                    RecruitedDay = c.RecruitedDay
                }).ToList();

                data.ActiveCompanionIds = companionData.ActiveCompanions.Select(c => (int)c).ToList();
                GD.Print($"[SaveSystem] Saving {data.ActiveCompanionIds.Count} active companions: [{string.Join(", ", data.ActiveCompanionIds)}]");

                data.FallenCompanions = companionData.FallenCompanions.Select(d => new CompanionDeathInfo
                {
                    CompanionId = (int)d.CompanionId,
                    DeathType = (int)d.Type,
                    Circumstance = d.Circumstance,
                    LastWords = d.LastWords,
                    DeathDay = d.DeathDay
                }).ToList();
            }
            catch { /* Companion system not initialized */ }

            // Dungeon Party NPCs - save NPC teammates (spouses, team members, lovers)
            try
            {
                data.DungeonPartyNPCIds = GameEngine.Instance?.DungeonPartyNPCIds?.ToList() ?? new List<string>();
                if (data.DungeonPartyNPCIds.Count > 0)
                {
                    GD.Print($"[SaveSystem] Saving {data.DungeonPartyNPCIds.Count} dungeon party NPCs: [{string.Join(", ", data.DungeonPartyNPCIds)}]");
                }
            }
            catch { /* GameEngine not initialized */ }

            // Family System - save children
            try
            {
                data.Children = FamilySystem.Instance.SerializeChildren();
                if (data.Children.Count > 0)
                {
                    GD.Print($"[SaveSystem] Saved {data.Children.Count} children");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SaveSystem] Failed to save children: {ex.Message}");
            }

            // Archetype Tracker - save Jungian archetype scores
            try
            {
                data.ArchetypeTracker = ArchetypeTracker.Instance.Serialize();
                GD.Print($"[SaveSystem] Saved archetype tracker data");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SaveSystem] Failed to save archetype tracker: {ex.Message}");
            }

            return data;
        }

        /// <summary>
        /// Restore story systems state from save data
        /// Note: Restoration is best-effort - systems may not support all restore operations
        /// </summary>
        public void RestoreStorySystems(StorySystemsData? data)
        {
            if (data == null) return;

            // Ocean Philosophy - restore fragments one by one
            try
            {
                var ocean = OceanPhilosophySystem.Instance;
                foreach (var fragmentInt in data.CollectedFragments)
                {
                    var fragment = (WaveFragment)fragmentInt;
                    if (!ocean.CollectedFragments.Contains(fragment))
                    {
                        ocean.CollectFragment(fragment);
                    }
                }
                foreach (var momentInt in data.ExperiencedMoments)
                {
                    var moment = (AwakeningMoment)momentInt;
                    if (!ocean.ExperiencedMoments.Contains(moment))
                    {
                        ocean.ExperienceMoment(moment);
                    }
                }
            }
            catch { /* System not available */ }

            // Story Progression - restore seals and story flags
            try
            {
                var story = StoryProgressionSystem.Instance;

                // Restore collected seals
                foreach (var sealInt in data.CollectedSeals)
                {
                    var seal = (UsurperRemake.Systems.SealType)sealInt;
                    if (!story.CollectedSeals.Contains(seal))
                    {
                        story.CollectSeal(seal);
                    }
                }

                // Restore story flags
                foreach (var kvp in data.StoryFlags)
                {
                    story.SetStoryFlag(kvp.Key, kvp.Value);
                }

                // Restore cycle count
                story.CurrentCycle = data.CurrentCycle;
            }
            catch { /* System not available */ }

            // God System - restore player worship data
            try
            {
                var godSystem = UsurperRemake.GodSystemSingleton.Instance;
                foreach (var kvp in data.PlayerGods)
                {
                    godSystem.SetPlayerGod(kvp.Key, kvp.Value);
                }
            }
            catch { /* System not available */ }

            // Companion System - restore companion states
            try
            {
                // Restore if we have companion states OR active companion IDs
                bool hasCompanionData = (data.Companions != null && data.Companions.Count > 0);
                bool hasActiveCompanions = (data.ActiveCompanionIds != null && data.ActiveCompanionIds.Count > 0);

                if (hasCompanionData || hasActiveCompanions)
                {
                    // Convert CompanionSaveInfo back to CompanionSystemData format
                    var companionSystemData = new CompanionSystemData
                    {
                        CompanionStates = data.Companions?.Select(c => new CompanionSaveData
                        {
                            Id = (CompanionId)c.Id,
                            IsRecruited = c.IsRecruited,
                            IsActive = c.IsActive,
                            IsDead = c.IsDead,
                            LoyaltyLevel = c.LoyaltyLevel,
                            TrustLevel = c.TrustLevel,
                            RomanceLevel = c.RomanceLevel,
                            PersonalQuestStarted = c.PersonalQuestStarted,
                            PersonalQuestCompleted = c.PersonalQuestCompleted,
                            RecruitedDay = c.RecruitedDay
                        }).ToList() ?? new List<CompanionSaveData>(),

                        ActiveCompanions = data.ActiveCompanionIds?.Select(id => (CompanionId)id).ToList() ?? new List<CompanionId>(),

                        FallenCompanions = data.FallenCompanions?.Select(d => new CompanionDeath
                        {
                            CompanionId = (CompanionId)d.CompanionId,
                            Type = (DeathType)d.DeathType,
                            Circumstance = d.Circumstance,
                            LastWords = d.LastWords,
                            DeathDay = d.DeathDay
                        }).ToList() ?? new List<CompanionDeath>()
                    };

                    CompanionSystem.Instance.Deserialize(companionSystemData);
                    GD.Print($"[SaveSystem] Restored {companionSystemData.ActiveCompanions.Count} active companions");
                }
            }
            catch { /* Companion system not available */ }

            // Dungeon Party NPCs - restore NPC teammates
            try
            {
                if (data.DungeonPartyNPCIds != null && data.DungeonPartyNPCIds.Count > 0)
                {
                    GameEngine.Instance?.SetDungeonPartyNPCs(data.DungeonPartyNPCIds);
                    GD.Print($"[SaveSystem] Restored {data.DungeonPartyNPCIds.Count} dungeon party NPCs");
                }
            }
            catch { /* GameEngine not available */ }

            // Family System - restore children
            try
            {
                if (data.Children != null && data.Children.Count > 0)
                {
                    FamilySystem.Instance.DeserializeChildren(data.Children);
                    GD.Print($"[SaveSystem] Restored {data.Children.Count} children from save");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SaveSystem] Failed to restore children: {ex.Message}");
            }

            // Archetype Tracker - restore Jungian archetype scores
            try
            {
                if (data.ArchetypeTracker != null)
                {
                    ArchetypeTracker.Instance.Deserialize(data.ArchetypeTracker);
                    GD.Print($"[SaveSystem] Restored archetype tracker data");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[SaveSystem] Failed to restore archetype tracker: {ex.Message}");
            }
        }
    }
} 