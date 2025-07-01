using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UsurperRemake;

/// <summary>
/// NPC Action types for behavior system
/// </summary>
public enum NPCActionType
{
    Idle,
    MoveTo,
    Combat,
    Shop,
    Socialize,
    Rest,
    Patrol,
    Quest,
    Move,
    Talk,
    Fight,
    Trade,
    Work
}

/// <summary>
/// NPC class that extends Character with AI behavior
/// Based on Pascal NPC system with enhanced AI features
/// </summary>
public partial class NPC : Character
{
    // NPC-specific properties
    public NPCBrain Brain { get; set; }
    public PersonalityProfile Personality { get; set; }
    public MemorySystem Memory { get; set; }
    public RelationshipManager Relationships { get; set; }
    public EmotionalState EmotionalState { get; set; }
    public GoalSystem Goals { get; set; }
    
    // NPC behavior settings
    public string Archetype { get; set; } = "citizen";        // thug, merchant, guard, priest, etc.
    public string CurrentLocation { get; set; } = "main_street";
    public bool IsSpecialNPC { get; set; } = false;           // Special scripted NPCs like Seth Able
    public string SpecialScript { get; set; } = "";           // Name of special behavior script
    
    // Daily behavior tracking
    public DateTime LastInteraction { get; set; }
    public int InteractionsToday { get; set; }
    public string LastSpokenTo { get; set; } = "";
    
    // NPC state flags
    public bool IsAwake { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
    public bool IsInConversation { get; set; } = false;
    public bool IsHostile { get; set; } = false;
    
    // Pascal compatibility flags
    public bool CanInteract => IsAwake && IsAvailable && !IsInConversation;
    public string Location => CurrentLocation;  // Pascal compatibility
    public bool IsNPC => true;                  // For compatibility checks
    
    /// <summary>
    /// Alias for King property for compatibility with GoalSystem
    /// </summary>
    public bool IsRuler => King;
    
    // Additional properties for world simulation
    public List<string> KnownCharacters { get; set; } = new();
    public List<string> Enemies { get; set; } = new();
    public List<string> GangMembers { get; set; } = new();
    public string GangId { get; set; } = "";
    public bool ControlsTurf { get; set; }
    
    // Missing properties for API compatibility
    public string Id { get; set; } = Guid.NewGuid().ToString();  // Unique identifier
    public string TeamPassword { get; set; } = "";               // Team password for joining
    
    /// <summary>
    /// Constructor for creating a new NPC
    /// </summary>
    public NPC(string name, string archetype, CharacterClass characterClass, int level = 1)
    {
        // Set basic character properties
        Name1 = name;
        Name2 = name;
        AI = CharacterAI.Computer;
        Class = characterClass;
        Level = level;
        Archetype = archetype;
        
        // Generate appropriate stats for the archetype and level
        GenerateStatsForArchetype(archetype, level);
        
        // Initialize NPC systems
        InitializeNPCSystems();
        
        // Set initial equipment and location based on archetype
        SetArchetypeDefaults();
    }
    
    /// <summary>
    /// Initialize NPC AI systems
    /// </summary>
    private void InitializeNPCSystems()
    {
        // Create personality based on archetype
        Personality = PersonalityProfile.GenerateForArchetype(Archetype);
        
        // Initialize memory system
        Memory = new MemorySystem(this);
        
        // Initialize relationship manager
        Relationships = new RelationshipManager(this);
        
        // Initialize emotional state
        EmotionalState = new EmotionalState();
        
        // Initialize goal system
        Goals = new GoalSystem(this, Personality);
        
        // Create brain with all systems
        Brain = new NPCBrain();
        
        // Check if this is a special Pascal NPC
        CheckForSpecialNPC();
    }
    
    /// <summary>
    /// Check if this NPC has special scripted behavior from Pascal
    /// </summary>
    private void CheckForSpecialNPC()
    {
        var specialNPCs = new Dictionary<string, string>
        {
            {"Seth Able", "drunk_fighter"},
            {"Dungeon Guard", "dungeon_guardian"},
            {"King's Guard", "castle_guard"},
            {"Death Knight", "dungeon_boss"},
            {"Bob", "tavern_keeper"},
            {"Bishop", "church_leader"},
            {"Gossip Monger", "love_corner_keeper"},
            {"Gym Masseur", "gym_worker"}
        };
        
        if (specialNPCs.ContainsKey(Name2))
        {
            IsSpecialNPC = true;
            SpecialScript = specialNPCs[Name2];
        }
    }
    
    /// <summary>
    /// Generate stats appropriate for archetype and level
    /// </summary>
    private void GenerateStatsForArchetype(string archetype, int level)
    {
        // Base stats
        var baseStats = GetBaseStatsForArchetype(archetype);
        
        // Apply level scaling
        Strength = baseStats.Strength + (level * 2);
        Defence = baseStats.Defence + (level * 2);
        Dexterity = baseStats.Dexterity + (level * 2);
        Agility = baseStats.Agility + (level * 2);
        Charisma = baseStats.Charisma + level;
        Wisdom = baseStats.Wisdom + level;
        Stamina = baseStats.Stamina + (level * 3);
        
        // Calculate HP and Mana
        MaxHP = 20 + (level * 8) + (Stamina / 2);
        HP = MaxHP;
        MaxMana = 10 + (level * 2) + (Wisdom / 3);
        Mana = MaxMana;
        
        // Set gold based on archetype and level
        Gold = GetBaseGold(archetype) * level;
        
        // Set experience
        Experience = GetExperienceForLevel(level);
    }
    
    /// <summary>
    /// Get base stats for different archetypes
    /// </summary>
    private (int Strength, int Defence, int Dexterity, int Agility, int Charisma, int Wisdom, int Stamina) GetBaseStatsForArchetype(string archetype)
    {
        return archetype.ToLower() switch
        {
            "thug" or "fighter" => (15, 12, 10, 8, 6, 7, 14),
            "guard" => (14, 15, 8, 7, 9, 8, 16),
            "merchant" => (8, 10, 12, 10, 15, 12, 10),
            "priest" or "cleric" => (9, 11, 8, 7, 12, 16, 12),
            "mystic" or "mage" => (7, 8, 10, 9, 11, 18, 8),
            "assassin" or "thief" => (11, 9, 16, 15, 10, 12, 11),
            "noble" => (10, 12, 11, 10, 16, 14, 12),
            "citizen" => (10, 10, 10, 10, 10, 10, 10),
            _ => (10, 10, 10, 10, 10, 10, 10)
        };
    }
    
    /// <summary>
    /// Get base gold for archetype
    /// </summary>
    private long GetBaseGold(string archetype)
    {
        return archetype.ToLower() switch
        {
            "noble" => 500,
            "merchant" => 300,
            "priest" => 200,
            "guard" => 150,
            "citizen" => 100,
            "thug" => 50,
            _ => 75
        };
    }
    
    /// <summary>
    /// Set archetype-specific defaults
    /// </summary>
    private void SetArchetypeDefaults()
    {
        switch (Archetype.ToLower())
        {
            case "thug":
                CurrentLocation = "tavern";
                Race = (CharacterRace)GD.RandRange(0, 9); // Random race
                Sex = (CharacterSex)GD.RandRange(1, 2);
                IsHostile = GD.Randf() < 0.3f; // 30% chance of being hostile
                break;
                
            case "guard":
                CurrentLocation = GD.RandRange(0, 2) == 0 ? "castle" : "main_street";
                Race = CharacterRace.Human; // Guards are usually human
                Sex = (CharacterSex)GD.RandRange(1, 2);
                break;
                
            case "merchant":
                CurrentLocation = "market";
                Race = CharacterRace.Human;
                Sex = (CharacterSex)GD.RandRange(1, 2);
                break;
                
            case "priest":
                CurrentLocation = "chapel";
                Race = CharacterRace.Human;
                Sex = (CharacterSex)GD.RandRange(1, 2);
                Chivalry = Level * 10; // Priests start with chivalry
                break;
                
            case "mystic":
                CurrentLocation = "magic_shop";
                Race = CharacterRace.Elf;
                Sex = (CharacterSex)GD.RandRange(1, 2);
                break;
                
            default:
                CurrentLocation = "main_street";
                Race = (CharacterRace)GD.RandRange(0, 9);
                Sex = (CharacterSex)GD.RandRange(1, 2);
                break;
        }
        
        // Set age based on level and archetype
        Age = 18 + Level + GD.RandRange(-5, 10);
        if (Age < 16) Age = 16;
        if (Age > 80) Age = 80;
    }
    
    /// <summary>
    /// Get experience needed for a specific level (Pascal compatible)
    /// </summary>
    private long GetExperienceForLevel(int level)
    {
        if (level <= 1) return 0;
        
        // Experience formula compatible with Pascal
        long exp = 0;
        for (int i = 2; i <= level; i++)
        {
            exp += (long)(Math.Pow(i, 2.5) * 100);
        }
        return exp;
    }
    
    /// <summary>
    /// Main NPC update method - called by world simulator
    /// Enhanced with Pascal-compatible behaviors
    /// </summary>
    public void UpdateNPC(float deltaTime)
    {
        // Update emotional state
        EmotionalState.Update(deltaTime);
        
        // Process goals and decide actions
        var action = Brain.DecideNextAction(GetWorldState());
        
        // Execute the action
        ExecuteAction(action);
        
        // Update memory with recent events
        Memory.ProcessNewMemories();
        
        // Update relationships
        Relationships.UpdateRelationships();
        
        // Enhanced NPC behaviors (Phase 21)
        ProcessEnhancedBehaviors(deltaTime);
    }
    
    /// <summary>
    /// Enhanced NPC behaviors - Pascal NPC_CHEC.PAS and NPCMAINT.PAS integration
    /// </summary>
    private void ProcessEnhancedBehaviors(float deltaTime)
    {
        var random = new Random();
        
        // Inventory management (Pascal NPC_CHEC.PAS logic)
        if (random.Next(100) < 5) // 5% chance per update
        {
            ProcessInventoryCheck();
        }
        
        // Shopping behavior (Pascal NPCMAINT.PAS shopping)
        if (random.Next(200) < 3 && Gold > 100) // 1.5% chance if has gold
        {
            ProcessShoppingBehavior();
        }
        
        // Gang/team management
        if (random.Next(300) < 2) // 0.67% chance
        {
            ProcessGangBehavior();
        }
        
        // Faith/believer system
        if (random.Next(400) < 2) // 0.5% chance
        {
            ProcessBelieverBehavior();
        }
    }
    
    /// <summary>
    /// Pascal NPC_CHEC.PAS inventory management
    /// </summary>
    private void ProcessInventoryCheck()
    {
        // Re-evaluate all equipped items
        Memory.AddMemory("I checked my equipment", "inventory", DateTime.Now);
        
        // Simple equipment optimization
        if (WeaponPower < Level * 15)
        {
            Goals.AddGoal(new Goal("Find Better Weapon", GoalType.Economic, 0.7f));
        }
        
        if (ArmorClass < Level * 10)
        {
            Goals.AddGoal(new Goal("Find Better Armor", GoalType.Economic, 0.6f));
        }
    }
    
    /// <summary>
    /// Pascal NPCMAINT.PAS shopping behavior
    /// </summary>
    private void ProcessShoppingBehavior()
    {
        var shoppingLocations = new[] { "main_street", "weapon_shop", "armor_shop", "magic_shop" };
        
        if (shoppingLocations.Contains(CurrentLocation))
        {
            // Determine what to buy based on class and needs
            var shoppingGoal = DetermineShoppingGoal();
            if (shoppingGoal != null)
            {
                Goals.AddGoal(shoppingGoal);
                Memory.AddMemory($"I need to buy {shoppingGoal.Name}", "shopping", DateTime.Now);
            }
        }
    }
    
    /// <summary>
    /// Gang behavior from Pascal NPCMAINT.PAS
    /// </summary>
    private void ProcessGangBehavior()
    {
        var random = new Random();
        
        if (string.IsNullOrEmpty(Team))
        {
            // Not in a gang - consider joining one
            if (Personality.IsLikelyToJoinGang() && random.Next(10) == 0)
            {
                Goals.AddGoal(new Goal("Join Gang", GoalType.Social, 0.8f));
                Memory.AddMemory("I should look for a gang to join", "social", DateTime.Now);
            }
        }
        else
        {
            // In a gang - gang loyalty actions
            if (random.Next(5) == 0)
            {
                Memory.AddMemory($"I'm loyal to {Team}", "gang", DateTime.Now);
                Goals.AddGoal(new Goal("Support Gang", GoalType.Social, 0.7f));
            }
        }
    }
    
    /// <summary>
    /// Pascal NPCMAINT.PAS believer system
    /// </summary>
    private void ProcessBelieverBehavior()
    {
        var random = new Random();
        
        if (string.IsNullOrEmpty(God))
        {
            // Potential conversion
            if (random.Next(20) == 0)
            {
                var availableGods = new[] { "Nosferatu", "Darkcloak", "Druid" };
                God = availableGods[random.Next(availableGods.Length)];
                Memory.AddMemory($"I found faith in {God}", "faith", DateTime.Now);
                EmotionalState.AdjustMood("spiritual", 0.3f);
            }
        }
        else
        {
            // Existing believer - perform faith actions
            if (random.Next(10) == 0)
            {
                var actions = new[] { "pray", "make offering", "seek guidance" };
                var action = actions[random.Next(actions.Length)];
                Memory.AddMemory($"I {action} to {God}", "faith", DateTime.Now);
                
                // Faith actions affect mood
                EmotionalState.AdjustMood("spiritual", 0.1f);
            }
        }
    }
    
    /// <summary>
    /// Determine shopping goal based on class and personality
    /// </summary>
    private Goal DetermineShoppingGoal()
    {
        switch (Class)
        {
            case CharacterClass.Warrior:
                if (WeaponPower < Level * 20)
                    return new Goal("Buy Better Weapon", GoalType.Economic, Personality.Greed);
                if (ArmorClass < Level * 15)
                    return new Goal("Buy Better Armor", GoalType.Economic, Personality.Greed * 0.8f);
                break;
                
            case CharacterClass.Magician:
                if (Mana < MaxMana * 0.7f)
                    return new Goal("Buy Mana Potions", GoalType.Economic, 0.6f);
                break;
                
            case CharacterClass.Paladin:
                if (HP < MaxHP * 0.8f)
                    return new Goal("Buy Healing Potions", GoalType.Economic, 0.7f);
                break;
        }
        
        return null;
    }
    
    /// <summary>
    /// Execute an NPC action
    /// </summary>
    private void ExecuteAction(NPCAction action)
    {
        switch ((NPCActionType)action.Type)
        {
            case NPCActionType.Move:
                Move(action.Target);
                break;
                
            case NPCActionType.Talk:
                // Handled by conversation system
                break;
                
            case NPCActionType.Fight:
                InitiateFight(action.Target);
                break;
                
            case NPCActionType.Trade:
                // Handled by trading system
                break;
                
            case NPCActionType.Rest:
                Rest();
                break;
                
            case NPCActionType.Work:
                Work();
                break;
                
            case NPCActionType.Socialize:
                Socialize();
                break;
                
            case NPCActionType.Idle:
                // Do nothing
                break;
        }
    }
    
    /// <summary>
    /// Move to a new location
    /// </summary>
    private void Move(string newLocation)
    {
        if (!string.IsNullOrEmpty(newLocation) && newLocation != CurrentLocation)
        {
            CurrentLocation = newLocation;
            Memory.AddMemory($"I moved to {newLocation}", "movement", DateTime.Now);
        }
    }
    
    /// <summary>
    /// Initiate combat with target
    /// </summary>
    private void InitiateFight(string target)
    {
        // This would trigger the combat system
        Memory.AddMemory($"I fought with {target}", "combat", DateTime.Now);
    }
    
    /// <summary>
    /// Rest and recover
    /// </summary>
    private void Rest()
    {
        HP = Math.Min(HP + 10, MaxHP);
        Mana = Math.Min(Mana + 5, MaxMana);
    }
    
    /// <summary>
    /// Work at job (archetype-specific)
    /// </summary>
    private void Work()
    {
        switch (Archetype.ToLower())
        {
            case "merchant":
                Gold += GD.RandRange(5, 20);
                break;
                
            case "guard":
                // Guards patrol
                var locations = new[] { "main_street", "castle", "market" };
                CurrentLocation = locations[GD.RandRange(0, locations.Length - 1)];
                break;
                
            case "priest":
                // Priests help others
                Chivalry += 1;
                break;
        }
    }
    
    /// <summary>
    /// Socialize with other NPCs/players
    /// </summary>
    private void Socialize()
    {
        // This would interact with other characters in the same location
        EmotionalState.AdjustMood("social", 0.1f);
    }
    
    /// <summary>
    /// Get current world state for decision making
    /// </summary>
    private WorldState GetWorldState()
    {
        return new WorldState
        {
            CurrentLocation = CurrentLocation,
            TimeOfDay = DateTime.Now.Hour,
            PlayersInArea = GetPlayersInLocation(CurrentLocation),
            NPCsInArea = GetNPCsInLocation(CurrentLocation),
            DangerLevel = CalculateDangerLevel()
        };
    }
    
    /// <summary>
    /// Get greeting for a player (Pascal compatible)
    /// </summary>
    public string GetGreeting(Character player)
    {
        if (IsSpecialNPC)
        {
            return GetSpecialGreeting(player);
        }
        
        // Use personality and relationship to generate greeting
        var relationship = Relationships.GetRelationshipWith(player.Name2);
        return Brain.GenerateGreeting(player, relationship);
    }
    
    /// <summary>
    /// Get special greeting for special NPCs (Pascal behavior)
    /// </summary>
    private string GetSpecialGreeting(Character player)
    {
        return SpecialScript switch
        {
            "drunk_fighter" => GetSethAbleGreeting(player),
            "dungeon_guardian" => "The dungeon is dangerous! Many never return...",
            "castle_guard" => "Halt! State your business in the castle!",
            "tavern_keeper" => "Welcome to my establishment! What can I get you?",
            _ => "Hello there, traveler."
        };
    }
    
    /// <summary>
    /// Seth Able specific greetings (from Pascal)
    /// </summary>
    private string GetSethAbleGreeting(Character player)
    {
        var greetings = new[]
        {
            "You lookin' at me funny?!",
            "*hiccup* Want to fight?",
            "I can take anyone in this place!",
            "*burp* You think you're tough?",
            "Another pretty boy... pfft!"
        };
        
        return greetings[GD.RandRange(0, greetings.Length - 1)];
    }
    
    /// <summary>
    /// Get display info for terminal
    /// </summary>
    public string GetDisplayInfo()
    {
        var marker = IsNPC ? GameConfig.NpcMark : "";
        var status = IsHostile ? " (Hostile)" : "";
        return $"{marker}{DisplayName} [{Archetype}, Level {Level}]{status}";
    }
    
    // Helper methods for world state
    private List<string> GetPlayersInLocation(string location)
    {
        // This would query the game engine for players in the location
        return new List<string>();
    }
    
    private List<string> GetNPCsInLocation(string location)
    {
        // This would query the game engine for NPCs in the location
        return new List<string>();
    }
    
    private float CalculateDangerLevel()
    {
        // Calculate danger based on location, time, etc.
        return CurrentLocation switch
        {
            "dungeon" => 0.8f,
            "tavern" => 0.3f,
            "main_street" => 0.1f,
            "castle" => 0.2f,
            _ => 0.1f
        };
    }
    
    // Missing methods for API compatibility
    public void UpdateLocation(string newLocation)
    {
        CurrentLocation = newLocation;
    }
    
    public bool CanAfford(long cost)
    {
        return Gold >= cost;
    }
    
    public void SpendGold(long amount)
    {
        Gold = Math.Max(0, Gold - amount);
    }
    
    public void GainGold(long amount)
    {
        Gold += amount;
    }
    
    public void AddRelationship(string characterId, int relationValue)
    {
        // Implementation for relationship tracking
    }
    
    public void AddRelationship(string characterId, UsurperRemake.RelationshipType relationType)
    {
        AddRelationship(characterId, (int)relationType);
    }
    
    public long GetAttackPower()
    {
        return WeaponPower + (Strength / 2);
    }
    
    public long GetArmorClass()
    {
        return ArmorClass + (Defence / 3);
    }
    
    public void TakeDamage(long damage)
    {
        HP = Math.Max(0, HP - damage);
    }
    
    public void SetState(object state)
    {
        // Implementation for NPC state management
    }
    
    public void Heal(long amount)
    {
        HP = Math.Min(MaxHP, HP + amount);
    }
    
    public void GainExperience(long exp)
    {
        Experience += exp;
    }
    
    public void ChangeActivity(object activity)
    {
        // Implementation for activity changes
    }
    
    // Overload expected by WorldSimulator (Activity type, urgency/intensity)
    public void ChangeActivity(UsurperRemake.Activity activity, int intensity)
    {
        // Stub: no behaviour yet
    }
    
    public void ChangeActivity(UsurperRemake.Activity activity, string description)
    {
        // Stub for compatibility, no actual behaviour now
    }
} 
