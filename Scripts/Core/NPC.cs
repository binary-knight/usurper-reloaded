using Godot;
using System;
using System.Collections.Generic;

public class NPC : Character
{
    public NPCBrain Brain { get; set; }
    public string Archetype { get; set; }
    public string Background { get; set; }
    public List<string> SpecialTraits { get; set; } = new List<string>();
    public Dictionary<string, List<string>> DialogueOptions { get; set; } = new Dictionary<string, List<string>>();
    
    // NPC-specific properties
    public Activity CurrentActivity { get; set; } = Activity.Idle;
    public DateTime LastActionTime { get; set; } = DateTime.Now;
    public string LastKnownLocation { get; set; }
    public int ActionPoints { get; set; } = 10; // For AI decision making
    
    // Relationships and social connections
    public List<string> KnownCharacters { get; set; } = new List<string>();
    public List<string> Enemies { get; set; } = new List<string>();
    public List<string> Allies { get; set; } = new List<string>();
    
    // NPC state tracking
    public NPCState CurrentState { get; set; } = NPCState.Active;
    public DateTime StateChangeTime { get; set; } = DateTime.Now;
    public Dictionary<string, object> StateData { get; set; } = new Dictionary<string, object>();
    
    public NPC() : base()
    {
        // NPCs start with some basic equipment and gold
        Gold = GD.RandRange(50, 300);
        TurnsRemaining = int.MaxValue; // NPCs don't have turn limitations
    }
    
    public NPC(string name, string archetype, CharacterClass charClass, int level = 1) : base(name, charClass)
    {
        Archetype = archetype;
        Level = level;
        
        // Set level-appropriate experience
        if (level > 1)
        {
            var expTable = CharacterDataManager.GetExperienceTable();
            if (level < expTable.Length)
            {
                Experience = expTable[level - 1];
            }
        }
        
        // Generate starting resources based on archetype and level
        GenerateStartingResources();
        
        // NPCs don't have turn limitations
        TurnsRemaining = int.MaxValue;
        
        InitializeFromArchetype();
    }
    
    private void InitializeFromArchetype()
    {
        switch (Archetype.ToLower())
        {
            case "thug":
            case "brawler":
                SpecialTraits.AddRange(new[] { "violent", "intimidating", "reckless" });
                CurrentActivity = Activity.Looking;
                break;
                
            case "merchant":
            case "trader":
                SpecialTraits.AddRange(new[] { "greedy", "talkative", "observant" });
                CurrentActivity = Activity.Trading;
                break;
                
            case "guard":
            case "soldier":
                SpecialTraits.AddRange(new[] { "lawful", "vigilant", "dutiful" });
                CurrentActivity = Activity.Patrolling;
                break;
                
            case "priest":
            case "cleric":
                SpecialTraits.AddRange(new[] { "holy", "peaceful", "wise" });
                CurrentActivity = Activity.Praying;
                break;
                
            case "noble":
            case "aristocrat":
                SpecialTraits.AddRange(new[] { "arrogant", "wealthy", "influential" });
                CurrentActivity = Activity.Socializing;
                break;
                
            case "mystic":
            case "mage":
                SpecialTraits.AddRange(new[] { "mysterious", "intelligent", "powerful" });
                CurrentActivity = Activity.Studying;
                break;
                
            case "craftsman":
            case "artisan":
                SpecialTraits.AddRange(new[] { "skilled", "hardworking", "precise" });
                CurrentActivity = Activity.Working;
                break;
                
            default:
                SpecialTraits.AddRange(new[] { "ordinary", "cautious" });
                CurrentActivity = Activity.Idle;
                break;
        }
    }
    
    private void GenerateStartingResources()
    {
        var baseGold = Level * 50;
        var variation = GD.RandRange(-20, 50);
        Gold = Math.Max(10, baseGold + variation);
        
        // Merchants start with more gold
        if (Archetype.ToLower() == "merchant" || Archetype.ToLower() == "trader")
        {
            Gold *= 3;
        }
        
        // Nobles start with even more
        if (Archetype.ToLower() == "noble" || Archetype.ToLower() == "aristocrat")
        {
            Gold *= 5;
        }
        
        // Guards and soldiers have steady income
        if (Archetype.ToLower() == "guard" || Archetype.ToLower() == "soldier")
        {
            Gold += 200;
        }
    }
    
    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        
        // NPC-specific level up behavior
        Brain?.OnNPCLevelUp();
        
        // Update activity based on new capabilities
        if (Level % 5 == 0)
        {
            // Every 5 levels, NPCs might change their primary activity
            ConsiderActivityChange();
        }
        
        // Gain more gold and possibly better equipment
        var goldGain = Level * 25 + GD.RandRange(10, 50);
        Gold += goldGain;
        
        // Notify other NPCs of this character's growth
        Brain?.Memory?.RecordEvent(new MemoryEvent
        {
            Type = MemoryType.PersonalAchievement,
            Description = $"Reached level {Level}",
            Importance = 0.6f,
            Location = CurrentLocation
        });
    }
    
    private void ConsiderActivityChange()
    {
        if (Brain == null) return;
        
        var personality = Brain.Personality;
        var currentGoals = Brain.Goals.GetActiveGoals();
        
        // Ambitious NPCs might change activities more often
        if (personality.Ambition > 0.7f && GD.Randf() < 0.3f)
        {
            var newActivity = GetActivityForAmbition();
            if (newActivity != CurrentActivity)
            {
                ChangeActivity(newActivity, "Seeking new opportunities");
            }
        }
    }
    
    private Activity GetActivityForAmbition()
    {
        return Archetype.ToLower() switch
        {
            "thug" => Activity.Hunting, // Hunt for stronger opponents
            "merchant" => Activity.Trading,
            "guard" => Activity.Patrolling,
            "noble" => Activity.Scheming,
            "mage" => Activity.Studying,
            _ => Activity.Exploring
        };
    }
    
    public void ChangeActivity(Activity newActivity, string reason = "")
    {
        var oldActivity = CurrentActivity;
        CurrentActivity = newActivity;
        LastActionTime = DateTime.Now;
        
        Brain?.Memory?.RecordEvent(new MemoryEvent
        {
            Type = MemoryType.ActivityChange,
            Description = $"Changed from {oldActivity} to {newActivity}. {reason}",
            Importance = 0.3f,
            Location = CurrentLocation
        });
    }
    
    public void SetState(NPCState newState, Dictionary<string, object> stateData = null)
    {
        CurrentState = newState;
        StateChangeTime = DateTime.Now;
        StateData = stateData ?? new Dictionary<string, object>();
        
        // Log state change
        Brain?.Memory?.RecordEvent(new MemoryEvent
        {
            Type = MemoryType.StateChange,
            Description = $"State changed to {newState}",
            Importance = 0.4f,
            Location = CurrentLocation
        });
    }
    
    public void UpdateLocation(string newLocation)
    {
        if (newLocation != CurrentLocation)
        {
            LastKnownLocation = CurrentLocation;
            CurrentLocation = newLocation;
            
            Brain?.Memory?.RecordEvent(new MemoryEvent
            {
                Type = MemoryType.LocationChange,
                Description = $"Moved to {newLocation}",
                Importance = 0.2f,
                Location = newLocation
            });
        }
    }
    
    public void AddRelationship(string characterId, RelationshipType type)
    {
        Brain?.Relationships?.SetRelationship(characterId, type);
        
        if (!KnownCharacters.Contains(characterId))
        {
            KnownCharacters.Add(characterId);
        }
        
        switch (type)
        {
            case RelationshipType.Enemy:
            case RelationshipType.Nemesis:
                if (!Enemies.Contains(characterId))
                    Enemies.Add(characterId);
                Allies.Remove(characterId);
                break;
                
            case RelationshipType.Brother:
            case RelationshipType.CloseFriend:
            case RelationshipType.Friend:
                if (!Allies.Contains(characterId))
                    Allies.Add(characterId);
                Enemies.Remove(characterId);
                break;
        }
    }
    
    public bool IsEnemyOf(string characterId)
    {
        return Enemies.Contains(characterId);
    }
    
    public bool IsAllyOf(string characterId)
    {
        return Allies.Contains(characterId);
    }
    
    public bool KnowsCharacter(string characterId)
    {
        return KnownCharacters.Contains(characterId);
    }
    
    public string GetGreeting(Character other)
    {
        if (Brain?.Relationships != null)
        {
            var relationship = Brain.Relationships.GetRelationship(other.Id);
            var relationshipType = relationship?.GetRelationshipType() ?? RelationshipType.Stranger;
            
            return GetGreetingByRelationship(relationshipType, other);
        }
        
        return GetDefaultGreeting(other);
    }
    
    private string GetGreetingByRelationship(RelationshipType relationship, Character other)
    {
        var greetings = relationship switch
        {
            RelationshipType.Brother => new[] { "Brother! It's good to see you!", "My trusted ally!", "Welcome, my friend!" },
            RelationshipType.CloseFriend => new[] { "Good to see you, friend!", "How are you doing?", "Welcome!" },
            RelationshipType.Friend => new[] { "Hello there!", "Good day!", "Nice to see you again." },
            RelationshipType.Enemy => new[] { "You...", "What do you want?", "I don't have time for you." },
            RelationshipType.Nemesis => new[] { "YOU! How dare you show your face here!", "Get away from me!", "I should kill you where you stand!" },
            RelationshipType.Feared => new[] { "P-please don't hurt me...", "I-I haven't done anything!", "Have mercy..." },
            _ => GetDefaultGreeting(other)
        };
        
        if (greetings.Length > 0)
        {
            return greetings[GD.RandRange(0, greetings.Length - 1)];
        }
        
        return GetDefaultGreeting(other);
    }
    
    private string GetDefaultGreeting(Character other)
    {
        if (DialogueOptions.ContainsKey("greeting") && DialogueOptions["greeting"].Count > 0)
        {
            var greetings = DialogueOptions["greeting"];
            return greetings[GD.RandRange(0, greetings.Count - 1)];
        }
        
        // Fallback greetings based on archetype
        return Archetype.ToLower() switch
        {
            "thug" => "What do you want?",
            "merchant" => "Welcome! Looking to buy or sell?",
            "guard" => "State your business.",
            "priest" => "May the light guide you.",
            "noble" => "Greetings, commoner.",
            "mage" => "Interesting... what brings you here?",
            _ => "Hello."
        };
    }
    
    public override string GetDisplayInfo()
    {
        var activity = CurrentActivity switch
        {
            Activity.Idle => "idling",
            Activity.Trading => "trading",
            Activity.Fighting => "fighting",
            Activity.Drinking => "drinking",
            Activity.Talking => "talking",
            Activity.Resting => "resting",
            Activity.Patrolling => "patrolling",
            Activity.Studying => "studying",
            Activity.Praying => "praying",
            Activity.Working => "working",
            Activity.Scheming => "scheming",
            Activity.Hunting => "hunting",
            Activity.Exploring => "exploring",
            Activity.Looking => "looking around",
            Activity.Socializing => "socializing",
            _ => "doing something"
        };
        
        return $"{Name} (Level {Level} {Class}) - {activity}";
    }
}

public enum Activity
{
    Idle,
    Trading,
    Fighting,
    Drinking,
    Talking,
    Resting,
    Patrolling,
    Studying,
    Praying,
    Working,
    Scheming,
    Hunting,
    Exploring,
    Looking,
    Socializing
}

public enum NPCState
{
    Active,      // Normal operation
    Busy,        // Engaged in important task
    Hostile,     // In combat or aggressive
    Friendly,    // Open to interaction
    Suspicious,  // Wary of others
    Afraid,      // Scared or intimidated
    Unconscious, // Knocked out or sleeping
    Dead         // Permanently dead
} 