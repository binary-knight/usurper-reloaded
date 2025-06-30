using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class NPCBrain
{
    private NPC owner;
    private PersonalityProfile personality;
    private MemorySystem memory;
    private GoalSystem goals;
    private RelationshipManager relationships;
    private EmotionalState emotions;
    
    private DateTime lastDecisionTime = DateTime.Now;
    private const int DECISION_COOLDOWN_MINUTES = 15;
    
    public NPC Owner => owner;
    public PersonalityProfile Personality => personality;
    public MemorySystem Memory => memory;
    public GoalSystem Goals => goals;
    public RelationshipManager Relationships => relationships;
    public EmotionalState Emotions => emotions;
    
    public NPCBrain(NPC npc, PersonalityProfile profile)
    {
        owner = npc;
        personality = profile;
        memory = new MemorySystem();
        goals = new GoalSystem(personality);
        relationships = new RelationshipManager();
        emotions = new EmotionalState();
        
        InitializeGoals();
        GD.Print($"[AI] Created brain for {npc.Name} ({profile.Archetype})");
    }
    
    private void InitializeGoals()
    {
        // Add basic goals based on personality and archetype
        switch (personality.Archetype.ToLower())
        {
            case "thug":
                goals.AddGoal(new Goal("Dominate Others", GoalType.Social, 0.8f));
                goals.AddGoal(new Goal("Gain Strength", GoalType.Personal, 0.7f));
                goals.AddGoal(new Goal("Find Enemies", GoalType.Social, 0.6f));
                break;
                
            case "merchant":
                goals.AddGoal(new Goal("Accumulate Wealth", GoalType.Economic, 0.9f));
                goals.AddGoal(new Goal("Build Trade Network", GoalType.Social, 0.7f));
                goals.AddGoal(new Goal("Secure Trade Routes", GoalType.Economic, 0.6f));
                break;
                
            case "guard":
                goals.AddGoal(new Goal("Maintain Order", GoalType.Social, 0.8f));
                goals.AddGoal(new Goal("Protect Citizens", GoalType.Social, 0.7f));
                goals.AddGoal(new Goal("Improve Skills", GoalType.Personal, 0.5f));
                break;
                
            case "priest":
                goals.AddGoal(new Goal("Help Others", GoalType.Social, 0.8f));
                goals.AddGoal(new Goal("Spread Faith", GoalType.Social, 0.7f));
                goals.AddGoal(new Goal("Gain Wisdom", GoalType.Personal, 0.6f));
                break;
                
            case "noble":
                goals.AddGoal(new Goal("Gain Political Power", GoalType.Social, 0.9f));
                goals.AddGoal(new Goal("Increase Influence", GoalType.Social, 0.8f));
                goals.AddGoal(new Goal("Maintain Status", GoalType.Personal, 0.7f));
                break;
                
            default:
                goals.AddGoal(new Goal("Survive", GoalType.Personal, 0.6f));
                goals.AddGoal(new Goal("Improve Life", GoalType.Personal, 0.5f));
                break;
        }
        
        // Add personality-driven goals
        if (personality.Greed > 0.7f)
        {
            goals.AddGoal(new Goal("Become Wealthy", GoalType.Economic, personality.Greed));
        }
        
        if (personality.Ambition > 0.8f)
        {
            goals.AddGoal(new Goal("Gain Power", GoalType.Social, personality.Ambition));
        }
        
        if (personality.Vengefulness > 0.7f)
        {
            goals.AddGoal(new Goal("Seek Revenge", GoalType.Social, personality.Vengefulness));
        }
    }
    
    public NPCAction DecideNextAction(WorldState world)
    {
        // Only make decisions periodically
        var timeSinceLastDecision = DateTime.Now - lastDecisionTime;
        if (timeSinceLastDecision.TotalMinutes < DECISION_COOLDOWN_MINUTES)
        {
            return new NPCAction { Type = ActionType.Continue };
        }
        
        lastDecisionTime = DateTime.Now;
        
        // Update emotional state based on recent events
        emotions.Update(memory.GetRecentEvents());
        
        // Decay old memories
        memory.DecayMemories();
        
        // Re-evaluate goals based on current situation
        goals.UpdateGoals(owner, world, memory, emotions);
        
        // Get current priority goal
        var currentGoal = goals.GetPriorityGoal();
        if (currentGoal == null)
        {
            return new NPCAction { Type = ActionType.Idle };
        }
        
        // Generate possible actions based on current goal
        var possibleActions = GenerateActions(currentGoal, world);
        
        // Score each action based on personality and current state
        var bestAction = SelectBestAction(possibleActions);
        
        // Record the decision
        memory.RecordEvent(new MemoryEvent
        {
            Type = MemoryType.PersonalAchievement,
            Description = $"Decided to {bestAction.Type} for goal: {currentGoal.Name}",
            Importance = 0.3f,
            Location = owner.CurrentLocation
        });
        
        return bestAction;
    }
    
    private List<NPCAction> GenerateActions(Goal goal, WorldState world)
    {
        var actions = new List<NPCAction>();
        
        switch (goal.Type)
        {
            case GoalType.Economic:
                actions.AddRange(GenerateEconomicActions(world));
                break;
                
            case GoalType.Social:
                actions.AddRange(GenerateSocialActions(world));
                break;
                
            case GoalType.Personal:
                actions.AddRange(GeneratePersonalActions(world));
                break;
                
            case GoalType.Combat:
                actions.AddRange(GenerateCombatActions(world));
                break;
        }
        
        // Always add basic actions
        actions.Add(new NPCAction { Type = ActionType.Idle, Priority = 0.1f });
        actions.Add(new NPCAction { Type = ActionType.Explore, Priority = 0.3f });
        
        return actions;
    }
    
    private List<NPCAction> GenerateEconomicActions(WorldState world)
    {
        var actions = new List<NPCAction>();
        
        if (personality.Greed > 0.5f)
        {
            actions.Add(new NPCAction 
            { 
                Type = ActionType.Trade, 
                Priority = personality.Greed * 0.8f,
                Target = FindTradePartner(world)
            });
            
            if (personality.GetDecisionWeight("steal") > 0.6f)
            {
                actions.Add(new NPCAction 
                { 
                    Type = ActionType.Steal, 
                    Priority = personality.GetDecisionWeight("steal"),
                    Target = FindStealTarget(world)
                });
            }
        }
        
        return actions;
    }
    
    private List<NPCAction> GenerateSocialActions(WorldState world)
    {
        var actions = new List<NPCAction>();
        
        if (personality.Sociability > 0.6f)
        {
            actions.Add(new NPCAction 
            { 
                Type = ActionType.Socialize, 
                Priority = personality.Sociability * 0.7f,
                Target = FindSocialTarget(world)
            });
        }
        
        if (personality.IsLikelyToJoinGang() && owner.GangId == null)
        {
            actions.Add(new NPCAction 
            { 
                Type = ActionType.JoinGang, 
                Priority = 0.8f,
                Target = FindGangToJoin(world)
            });
        }
        
        if (personality.IsLikelyToSeekRevenge())
        {
            var enemy = FindRevengTarget();
            if (enemy != null)
            {
                actions.Add(new NPCAction 
                { 
                    Type = ActionType.SeekRevenge, 
                    Priority = personality.Vengefulness,
                    Target = enemy
                });
            }
        }
        
        return actions;
    }
    
    private List<NPCAction> GeneratePersonalActions(WorldState world)
    {
        var actions = new List<NPCAction>();
        
        if (owner.CurrentHP < owner.MaxHP * 0.5f)
        {
            actions.Add(new NPCAction 
            { 
                Type = ActionType.Rest, 
                Priority = 0.9f 
            });
        }
        
        if (personality.Ambition > 0.7f)
        {
            actions.Add(new NPCAction 
            { 
                Type = ActionType.Train, 
                Priority = personality.Ambition * 0.6f 
            });
        }
        
        return actions;
    }
    
    private List<NPCAction> GenerateCombatActions(WorldState world)
    {
        var actions = new List<NPCAction>();
        
        if (personality.Aggression > 0.6f)
        {
            var target = FindCombatTarget(world);
            if (target != null)
            {
                actions.Add(new NPCAction 
                { 
                    Type = ActionType.Attack, 
                    Priority = personality.Aggression,
                    Target = target
                });
            }
        }
        
        return actions;
    }
    
    private NPCAction SelectBestAction(List<NPCAction> actions)
    {
        if (!actions.Any())
        {
            return new NPCAction { Type = ActionType.Idle };
        }
        
        // Modify priorities based on emotional state
        foreach (var action in actions)
        {
            action.Priority *= emotions.GetActionModifier(action.Type);
        }
        
        // Add randomness based on impulsiveness
        if (personality.Impulsiveness > 0.7f && actions.Count > 1)
        {
            // Sometimes pick a random action instead of the best one
            if (GD.Randf() < personality.Impulsiveness * 0.3f)
            {
                return actions[GD.RandRange(0, actions.Count - 1)];
            }
        }
        
        // Select the highest priority action
        return actions.OrderByDescending(a => a.Priority).First();
    }
    
    private string FindTradePartner(WorldState world)
    {
        // Find NPCs in the same location who are merchants or friendly
        return world.GetNPCsInLocation(owner.CurrentLocation)
            .Where(npc => npc.Id != owner.Id && npc.Archetype == "merchant")
            .FirstOrDefault()?.Id;
    }
    
    private string FindStealTarget(WorldState world)
    {
        // Find wealthy NPCs to steal from
        return world.GetNPCsInLocation(owner.CurrentLocation)
            .Where(npc => npc.Id != owner.Id && npc.Gold > 100 && !owner.IsAllyOf(npc.Id))
            .OrderByDescending(npc => npc.Gold)
            .FirstOrDefault()?.Id;
    }
    
    private string FindSocialTarget(WorldState world)
    {
        // Find compatible NPCs to socialize with
        return world.GetNPCsInLocation(owner.CurrentLocation)
            .Where(npc => npc.Id != owner.Id && !owner.IsEnemyOf(npc.Id))
            .FirstOrDefault()?.Id;
    }
    
    private string FindGangToJoin(WorldState world)
    {
        // Find gang leaders to approach
        return world.GetNPCsInLocation(owner.CurrentLocation)
            .Where(npc => npc.Id != owner.Id && npc.GangMembers.Any())
            .FirstOrDefault()?.Id;
    }
    
    private string FindRevengTarget()
    {
        // Find enemies from memory
        return memory.GetMemoriesOfType(MemoryType.Attacked)
            .Where(m => m.InvolvedCharacter != null)
            .OrderByDescending(m => m.Importance)
            .FirstOrDefault()?.InvolvedCharacter;
    }
    
    private string FindCombatTarget(WorldState world)
    {
        // Find enemies or potential targets based on personality
        var npcsHere = world.GetNPCsInLocation(owner.CurrentLocation);
        
        // Prioritize known enemies
        var enemy = npcsHere.FirstOrDefault(npc => owner.IsEnemyOf(npc.Id));
        if (enemy != null) return enemy.Id;
        
        // For aggressive NPCs, find weaker targets
        if (personality.Aggression > 0.8f)
        {
            return npcsHere
                .Where(npc => npc.Id != owner.Id && npc.Level < owner.Level)
                .FirstOrDefault()?.Id;
        }
        
        return null;
    }
    
    public void RecordInteraction(Character other, InteractionType type, Dictionary<string, object> details = null)
    {
        var memoryType = ConvertInteractionToMemoryType(type);
        var importance = CalculateInteractionImportance(type);
        
        var memoryEvent = new MemoryEvent
        {
            Type = memoryType,
            InvolvedCharacter = other.Id,
            Location = owner.CurrentLocation,
            Details = details ?? new Dictionary<string, object>(),
            Importance = importance,
            Description = $"{type} with {other.Name}"
        };
        
        memory.RecordEvent(memoryEvent);
        relationships.UpdateRelationship(other.Id, memoryEvent);
        
        // Update emotional state for significant events
        if (type == InteractionType.Attacked || type == InteractionType.Betrayed)
        {
            emotions.AddEmotion(EmotionType.Anger, 0.8f, 120); // 2 hours of anger
        }
        else if (type == InteractionType.Helped || type == InteractionType.Defended)
        {
            emotions.AddEmotion(EmotionType.Gratitude, 0.6f, 180); // 3 hours of gratitude
        }
    }
    
    private MemoryType ConvertInteractionToMemoryType(InteractionType interaction)
    {
        return interaction switch
        {
            InteractionType.Attacked => MemoryType.Attacked,
            InteractionType.Betrayed => MemoryType.Betrayed,
            InteractionType.Helped => MemoryType.Helped,
            InteractionType.Defended => MemoryType.Defended,
            InteractionType.Traded => MemoryType.Traded,
            InteractionType.SharedDrink => MemoryType.SharedDrink,
            InteractionType.SharedItem => MemoryType.SharedItem,
            InteractionType.Defeated => MemoryType.Defeated,
            InteractionType.Threatened => MemoryType.Threatened,
            InteractionType.Insulted => MemoryType.Insulted,
            InteractionType.Complimented => MemoryType.Complimented,
            _ => MemoryType.Miscellaneous
        };
    }
    
    private float CalculateInteractionImportance(InteractionType type)
    {
        return type switch
        {
            InteractionType.Attacked => 0.9f,
            InteractionType.Betrayed => 1.0f,
            InteractionType.Helped => 0.7f,
            InteractionType.Defended => 0.8f,
            InteractionType.Defeated => 0.8f,
            InteractionType.Threatened => 0.6f,
            InteractionType.Traded => 0.3f,
            InteractionType.SharedDrink => 0.2f,
            InteractionType.SharedItem => 0.4f,
            InteractionType.Complimented => 0.2f,
            InteractionType.Insulted => 0.4f,
            _ => 0.3f
        };
    }
    
    public void OnNPCLevelUp()
    {
        memory.RecordEvent(new MemoryEvent
        {
            Type = MemoryType.PersonalAchievement,
            Description = $"Reached level {owner.Level}",
            Importance = 0.8f,
            Location = owner.CurrentLocation
        });
        
        // Level up might change goals
        goals.OnLevelUp(owner.Level);
        
        // Boost confidence emotion
        emotions.AddEmotion(EmotionType.Confidence, 0.7f, 300); // 5 hours
    }
    
    public string GetBrainSummary()
    {
        var summary = $"=== {owner.Name} AI Brain ===\n";
        summary += $"Personality: {personality}\n";
        summary += $"Current Goal: {goals.GetPriorityGoal()?.Name ?? "None"}\n";
        summary += $"Active Emotions: {emotions.GetActiveEmotions().Count}\n";
        summary += memory.GetMemorySummary();
        
        return summary;
    }
}

// Supporting classes and enums
public class NPCAction
{
    public ActionType Type { get; set; }
    public float Priority { get; set; } = 0.5f;
    public string Target { get; set; } // Target character ID
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}

public enum ActionType
{
    Idle,
    Continue,
    Explore,
    Trade,
    Socialize,
    Attack,
    Flee,
    Rest,
    Train,
    Steal,
    JoinGang,
    LeaveGang,
    SeekRevenge,
    Help,
    Betray,
    
    // Additional actions for testing compatibility
    SeekHealing,
    DoNothing,
    ExploreDungeon,
    LootThenFlee,
    SeekWealth,
    SeekCombat,
    FormGang,
    AccumulateWealth,
    CallForHelp,
    VisitTavern
}

public enum InteractionType
{
    Attacked,
    Betrayed,
    Helped,
    Defended,
    Traded,
    SharedDrink,
    SharedItem,
    Defeated,
    Threatened,
    Insulted,
    Complimented,
    Challenged,
    Intimidated
}

public class WorldState
{
    private List<NPC> npcs;
    
    // Properties for test compatibility
    public int CurrentHour { get; set; }
    public string CurrentLocation { get; set; }
    public bool InCombat { get; set; }
    public Character[] NearbyCharacters { get; set; }
    
    public WorldState(List<NPC> worldNPCs = null)
    {
        npcs = worldNPCs ?? new List<NPC>();
    }
    
    public List<NPC> GetNPCsInLocation(string location)
    {
        return npcs.Where(npc => npc.CurrentLocation == location && npc.IsAlive).ToList();
    }
    
    public NPC GetNPCById(string id)
    {
        return npcs.FirstOrDefault(npc => npc.Id == id);
    }
} 