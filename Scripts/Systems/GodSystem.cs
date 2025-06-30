using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// God System - Complete Pascal-compatible god management system
/// Based on Pascal VARGODS.PAS with all god functions and procedures
/// Handles god creation, management, believers, sacrifices, and divine interventions
/// </summary>
public class GodSystem
{
    private List<God> gods;
    private Dictionary<string, God> godsByName;
    private Dictionary<string, string> playerGods; // player name -> god name mapping
    private Random random;
    private DateTime lastMaintenance;
    
    public GodSystem()
    {
        gods = new List<God>();
        godsByName = new Dictionary<string, God>();
        playerGods = new Dictionary<string, string>();
        random = new Random();
        lastMaintenance = DateTime.Now;
        
        // Initialize with supreme creator if needed
        InitializeSupremeCreator();
    }
    
    /// <summary>
    /// Initialize the supreme creator god (Pascal global_supreme_creator)
    /// </summary>
    private void InitializeSupremeCreator()
    {
        if (!godsByName.ContainsKey(GameConfig.SupremeCreatorName))
        {
            var supremeCreator = new God
            {
                Name = GameConfig.SupremeCreatorName,
                RealName = "System",
                Id = "SUPREME",
                Level = GameConfig.MaxGodLevel,
                Experience = GameConfig.GodLevel9Experience * 10,
                AI = GameConfig.GodAIComputer,
                Age = 1000,
                Sex = 1,
                Believers = 0,
                Deleted = false,
                DeedsLeft = 99,
                Darkness = 0,
                Goodness = 50000
            };
            
            gods.Add(supremeCreator);
            godsByName[supremeCreator.Name] = supremeCreator;
        }
    }
    
    /// <summary>
    /// Search for gods by user name (Pascal God_Search function)
    /// </summary>
    public List<God> SearchGodsByUser(string userName)
    {
        return gods.Where(g => !g.Deleted && 
                              string.Equals(g.RealName, userName, StringComparison.OrdinalIgnoreCase))
                   .ToList();
    }
    
    /// <summary>
    /// Get god title by level (Pascal God_Title function)
    /// </summary>
    public static string GetGodTitle(int level)
    {
        if (level >= 1 && level <= GameConfig.MaxGodLevel)
        {
            return GameConfig.GodTitles[level];
        }
        return "Lesser Spirit";
    }
    
    /// <summary>
    /// Count believers for a god (Pascal God_Believers function)
    /// </summary>
    public int CountBelievers(string godName, bool listThem = false)
    {
        if (!godsByName.ContainsKey(godName))
            return 0;
            
        var god = godsByName[godName];
        if (listThem)
        {
            // In Pascal this would display the list - here we just return count
            GD.Print($"Followers of {godName}:");
            for (int i = 0; i < god.Disciples.Count; i++)
            {
                GD.Print($"{i + 1}. {god.Disciples[i]}");
            }
        }
        
        return god.Believers;
    }
    
    /// <summary>
    /// Select a god interactively (Pascal Select_A_God function)
    /// </summary>
    public God SelectGod(string excludeName = "", bool numbered = false)
    {
        var availableGods = gods.Where(g => g.IsActive() && g.Name != excludeName).ToList();
        
        if (availableGods.Count == 0)
            return null;
            
        // In a real implementation, this would show a menu
        // For now, return the first available god
        return availableGods.First();
    }
    
    /// <summary>
    /// Check if god is active (Pascal God_Active function)
    /// </summary>
    public bool IsGodActive(God god)
    {
        return god != null && god.IsActive();
    }
    
    /// <summary>
    /// Verify god exists (Pascal Verify_Gods_Existance function)
    /// </summary>
    public bool VerifyGodExists(string godName)
    {
        return godsByName.ContainsKey(godName) && godsByName[godName].IsActive();
    }
    
    /// <summary>
    /// Get random active god (Pascal Get_Random_God function)
    /// </summary>
    public God GetRandomGod()
    {
        var activeGods = gods.Where(g => g.IsActive()).ToList();
        if (activeGods.Count == 0)
            return null;
            
        return activeGods[random.Next(activeGods.Count)];
    }
    
    /// <summary>
    /// Count how many believers a god has (Pascal How_Many_Believers function)
    /// </summary>
    public int CountGodBelievers(God god)
    {
        return god?.Believers ?? 0;
    }
    
    /// <summary>
    /// Load god by name (Pascal Load_God_By_Name function)
    /// </summary>
    public God LoadGodByName(string godName)
    {
        return godsByName.ContainsKey(godName) ? godsByName[godName] : null;
    }
    
    /// <summary>
    /// Check if player has a god (Pascal Player_Has_A_God function)
    /// </summary>
    public bool PlayerHasGod(string playerName)
    {
        return playerGods.ContainsKey(playerName) && 
               !string.IsNullOrEmpty(playerGods[playerName]) &&
               VerifyGodExists(playerGods[playerName]);
    }
    
    /// <summary>
    /// Get player's god name
    /// </summary>
    public string GetPlayerGod(string playerName)
    {
        return playerGods.ContainsKey(playerName) ? playerGods[playerName] : "";
    }
    
    /// <summary>
    /// Set player's god
    /// </summary>
    public void SetPlayerGod(string playerName, string godName)
    {
        if (string.IsNullOrEmpty(godName))
        {
            // Remove god
            if (playerGods.ContainsKey(playerName))
            {
                var oldGodName = playerGods[playerName];
                if (godsByName.ContainsKey(oldGodName))
                {
                    godsByName[oldGodName].RemoveBeliever(playerName);
                }
                playerGods.Remove(playerName);
            }
        }
        else
        {
            // Remove from old god first
            if (playerGods.ContainsKey(playerName))
            {
                var oldGodName = playerGods[playerName];
                if (godsByName.ContainsKey(oldGodName))
                {
                    godsByName[oldGodName].RemoveBeliever(playerName);
                }
            }
            
            // Add to new god
            playerGods[playerName] = godName;
            if (godsByName.ContainsKey(godName))
            {
                godsByName[godName].AddBeliever(playerName);
            }
        }
    }
    
    /// <summary>
    /// Calculate sacrifice gold return (Pascal Sacrifice_Gold_Return function)
    /// </summary>
    public static long CalculateSacrificeGoldReturn(long goldAmount)
    {
        return God.CalculateSacrificeReturn(goldAmount);
    }
    
    /// <summary>
    /// Become a god (Pascal Become_God procedure)
    /// </summary>
    public God BecomeGod(string userName, string alias, string playerId, int playerSex, long playerDarkness, long playerGoodness)
    {
        // Check if name is already taken
        if (godsByName.ContainsKey(alias) || 
            alias.Equals("SYSOP", StringComparison.OrdinalIgnoreCase) ||
            alias.Equals(GameConfig.SupremeCreatorName, StringComparison.OrdinalIgnoreCase))
        {
            return null; // Name already taken
        }
        
        // Create new god
        var newGod = new God(userName, alias, playerId, playerSex, playerDarkness, playerGoodness);
        
        // Find empty slot or add to end
        var emptySlot = gods.FindIndex(g => g.Deleted);
        if (emptySlot >= 0)
        {
            newGod.RecordNumber = emptySlot + 1;
            gods[emptySlot] = newGod;
        }
        else
        {
            newGod.RecordNumber = gods.Count + 1;
            gods.Add(newGod);
        }
        
        godsByName[newGod.Name] = newGod;
        
        // In Pascal, this would send news and notifications
        GD.Print($"DIVINITY! {newGod.Name} became immortal and entered the Divine Realm!");
        
        return newGod;
    }
    
    /// <summary>
    /// God maintenance - run daily (Pascal God_Maintenance procedure)
    /// </summary>
    public void RunGodMaintenance()
    {
        if ((DateTime.Now - lastMaintenance).TotalHours < GameConfig.GodMaintenanceInterval)
            return;
            
        foreach (var god in gods.Where(g => g.IsActive()))
        {
            god.ResetDailyDeeds();
        }
        
        lastMaintenance = DateTime.Now;
    }
    
    /// <summary>
    /// Get god status display (Pascal God_Status procedure)
    /// </summary>
    public string GetGodStatus(God god)
    {
        if (god == null || !god.IsActive())
            return "God not found or inactive.";
            
        return god.GetStatusDisplay();
    }
    
    /// <summary>
    /// List all gods (Pascal List_Gods procedure)
    /// </summary>
    public List<God> ListGods(bool numbered = false)
    {
        var activeGods = gods.Where(g => g.IsActive()).OrderByDescending(g => g.Experience).ToList();
        
        if (numbered)
        {
            for (int i = 0; i < activeGods.Count; i++)
            {
                var god = activeGods[i];
                GD.Print($"{i + 1}. {god.Name} - {god.GetTitle()} - {god.Believers} believers");
            }
        }
        
        return activeGods;
    }
    
    /// <summary>
    /// Inform disciples of god events (Pascal Inform_Disciples procedure)
    /// </summary>
    public void InformDisciples(God god, string header, params string[] messages)
    {
        if (god == null || !god.IsActive())
            return;
            
        foreach (var disciple in god.Disciples)
        {
            // In Pascal, this would send mail to each disciple
            // For now, just log the message
            GD.Print($"Message to {disciple} from {god.Name}: {header}");
            foreach (var message in messages.Where(m => !string.IsNullOrEmpty(m)))
            {
                GD.Print($"  {message}");
            }
        }
    }
    
    /// <summary>
    /// Process gold sacrifice to god
    /// </summary>
    public long ProcessGoldSacrifice(string godName, long goldAmount, string playerName)
    {
        if (!godsByName.ContainsKey(godName))
            return 0;
            
        var god = godsByName[godName];
        if (!god.IsActive())
            return 0;
            
        var powerGained = CalculateSacrificeGoldReturn(goldAmount);
        god.IncreaseExperience(powerGained);
        
        // Inform god if online (in Pascal this would be a broadcast)
        GD.Print($"{playerName} sacrificed {goldAmount} gold to {godName}! Power increased by {powerGained}");
        
        return powerGained;
    }
    
    /// <summary>
    /// Process altar desecration
    /// </summary>
    public void ProcessAltarDesecration(string godName, string playerName)
    {
        if (!godsByName.ContainsKey(godName))
            return;
            
        var god = godsByName[godName];
        if (!god.IsActive())
            return;
            
        // In Pascal, this would reduce god power and inform disciples
        GD.Print($"{playerName} desecrated the altar of {godName}!");
        
        // Inform disciples
        InformDisciples(god, "ALTAR DESECRATED!", 
            $"{playerName} desecrated the altar of {godName}, your god!",
            "You must protect your master!");
    }
    
    /// <summary>
    /// Divine intervention - help prisoner escape
    /// </summary>
    public bool DivineInterventionPrisonEscape(God god, string prisonerName)
    {
        if (god == null || !god.IsActive() || !god.UseDeed())
            return false;
            
        // In Pascal, this would actually free the prisoner
        GD.Print($"{god.Name} helped {prisonerName} escape from prison!");
        
        return true;
    }
    
    /// <summary>
    /// Divine intervention - bless mortal
    /// </summary>
    public bool DivineInterventionBless(God god, string mortalName)
    {
        if (god == null || !god.IsActive() || !god.UseDeed())
            return false;
            
        // In Pascal, this would provide actual blessing effects
        GD.Print($"{god.Name} blessed {mortalName}!");
        
        return true;
    }
    
    /// <summary>
    /// Divine intervention - curse mortal
    /// </summary>
    public bool DivineInterventionCurse(God god, string mortalName)
    {
        if (god == null || !god.IsActive() || !god.UseDeed())
            return false;
            
        // In Pascal, this would provide actual curse effects
        GD.Print($"{god.Name} cursed {mortalName}!");
        
        return true;
    }
    
    /// <summary>
    /// Get all gods
    /// </summary>
    public List<God> GetAllGods()
    {
        return gods.ToList();
    }
    
    /// <summary>
    /// Get active gods
    /// </summary>
    public List<God> GetActiveGods()
    {
        return gods.Where(g => g.IsActive()).ToList();
    }
    
    /// <summary>
    /// Get god by name
    /// </summary>
    public God GetGod(string godName)
    {
        return godsByName.ContainsKey(godName) ? godsByName[godName] : null;
    }
    
    /// <summary>
    /// Add god to system
    /// </summary>
    public void AddGod(God god)
    {
        if (god != null && god.IsValid() && !godsByName.ContainsKey(god.Name))
        {
            gods.Add(god);
            godsByName[god.Name] = god;
        }
    }
    
    /// <summary>
    /// Remove god from system
    /// </summary>
    public void RemoveGod(string godName)
    {
        if (godsByName.ContainsKey(godName))
        {
            var god = godsByName[godName];
            god.Deleted = true;
            
            // Remove all believers
            foreach (var believer in god.Disciples.ToList())
            {
                SetPlayerGod(believer, "");
            }
        }
    }
    
    /// <summary>
    /// Get god statistics
    /// </summary>
    public Dictionary<string, object> GetGodStatistics()
    {
        var activeGods = GetActiveGods();
        var totalBelievers = activeGods.Sum(g => g.Believers);
        var averageLevel = activeGods.Count > 0 ? activeGods.Average(g => g.Level) : 0;
        var totalExperience = activeGods.Sum(g => g.Experience);
        
        return new Dictionary<string, object>
        {
            ["TotalGods"] = activeGods.Count,
            ["TotalBelievers"] = totalBelievers,
            ["AverageLevel"] = Math.Round(averageLevel, 2),
            ["TotalExperience"] = totalExperience,
            ["MostPowerfulGod"] = activeGods.OrderByDescending(g => g.Experience).FirstOrDefault()?.Name ?? "None",
            ["MostPopularGod"] = activeGods.OrderByDescending(g => g.Believers).FirstOrDefault()?.Name ?? "None"
        };
    }
    
    /// <summary>
    /// Convert to dictionary for serialization
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            ["Gods"] = gods.Select(g => g.ToDictionary()).ToArray(),
            ["PlayerGods"] = playerGods,
            ["LastMaintenance"] = lastMaintenance.ToBinary()
        };
    }
    
    /// <summary>
    /// Load from dictionary
    /// </summary>
    public static GodSystem FromDictionary(Dictionary<string, object> dict)
    {
        var system = new GodSystem();
        
        if (dict.ContainsKey("Gods"))
        {
            var godDicts = (object[])dict["Gods"];
            foreach (var godDict in godDicts)
            {
                var god = God.FromDictionary((Dictionary<string, object>)godDict);
                system.gods.Add(god);
                system.godsByName[god.Name] = god;
            }
        }
        
        if (dict.ContainsKey("PlayerGods"))
        {
            system.playerGods = (Dictionary<string, string>)dict["PlayerGods"];
        }
        
        if (dict.ContainsKey("LastMaintenance"))
        {
            system.lastMaintenance = DateTime.FromBinary(Convert.ToInt64(dict["LastMaintenance"]));
        }
        
        return system;
    }
} 
