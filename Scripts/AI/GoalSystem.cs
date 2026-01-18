using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class GoalSystem
{
    private List<Goal> goals = new List<Goal>();
    private PersonalityProfile personality;

    // Public accessor for serialization
    public List<Goal> AllGoals => goals;

    public GoalSystem(PersonalityProfile profile)
    {
        personality = profile;
    }
    
    public void AddGoal(Goal goal)
    {
        goals.Add(goal);
        // GD.Print($"[Goals] Added goal: {goal.Name} (Priority: {goal.Priority:F2})");
    }
    
    public void RemoveGoal(string goalName)
    {
        goals.RemoveAll(g => g.Name == goalName);
    }
    
    public Goal GetPriorityGoal()
    {
        return goals
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.GetEffectivePriority())
            .FirstOrDefault();
    }
    
    public List<Goal> GetActiveGoals()
    {
        return goals.Where(g => g.IsActive).OrderByDescending(g => g.Priority).ToList();
    }
    
    public void UpdateGoals(NPC owner, WorldState world, MemorySystem memory, EmotionalState emotions)
    {
        // Decay goal priorities over time
        foreach (var goal in goals)
        {
            goal.Priority *= 0.995f; // Slow decay
            
            // Check if goal should be completed or abandoned
            if (IsGoalCompleted(goal, owner, world))
            {
                goal.Complete();
                // GD.Print($"[Goals] {owner.Name} completed goal: {goal.Name}");
            }
            else if (goal.Priority < 0.1f)
            {
                goal.IsActive = false;
                // GD.Print($"[Goals] {owner.Name} abandoned goal: {goal.Name}");
            }
        }
        
        // Add new goals based on current situation
        GenerateNewGoals(owner, world, memory, emotions);
        
        // Adjust priorities based on personality and emotions
        AdjustGoalPriorities(emotions);
    }
    
    private bool IsGoalCompleted(Goal goal, NPC owner, WorldState world)
    {
        return goal.Type switch
        {
            GoalType.Economic when goal.Name.Contains("Wealthy") => owner.Gold >= 10000,
            GoalType.Social when goal.Name.Contains("Power") => owner.King,
            GoalType.Personal when goal.Name.Contains("Strength") => owner.Level >= 20,
            GoalType.Social when goal.Name.Contains("Gang") => !string.IsNullOrEmpty(owner.GangId),
            _ => false
        };
    }
    
    private void GenerateNewGoals(NPC owner, WorldState world, MemorySystem memory, EmotionalState emotions)
    {
        // Generate revenge goals based on memories
        var attackMemories = memory.GetMemoriesOfType(MemoryType.Attacked)
            .Where(m => m.IsRecent(168)) // Within a week
            .ToList();
            
        foreach (var attackMemory in attackMemories.Take(2)) // Limit revenge goals
        {
            var revengeName = $"Revenge against {attackMemory.InvolvedCharacter}";
            if (!goals.Any(g => g.Name == revengeName))
            {
                var revengeGoal = new Goal(revengeName, GoalType.Social, personality.Vengefulness);
                revengeGoal.TargetCharacter = attackMemory.InvolvedCharacter;
                AddGoal(revengeGoal);
            }
        }
        
        // Generate social goals based on loneliness
        if (personality.Sociability > 0.6f && owner.KnownCharacters.Count < 3)
        {
            if (!goals.Any(g => g.Name.Contains("Make Friends")))
            {
                AddGoal(new Goal("Make Friends", GoalType.Social, personality.Sociability * 0.8f));
            }
        }
        
        // Generate economic goals based on poverty
        if (owner.Gold < 100 && personality.Greed > 0.5f)
        {
            if (!goals.Any(g => g.Name.Contains("Earn Money")))
            {
                AddGoal(new Goal("Earn Money", GoalType.Economic, personality.Greed));
            }
        }
        
        // Generate survival goals based on health
        if (owner.CurrentHP < owner.MaxHP * 0.3f)
        {
            if (!goals.Any(g => g.Name.Contains("Heal")))
            {
                AddGoal(new Goal("Heal Wounds", GoalType.Personal, 0.9f));
            }
        }
        
        // Generate power goals for ambitious NPCs
        if (personality.Ambition > 0.8f && owner.Level >= 10)
        {
            if (!goals.Any(g => g.Name.Contains("Become Ruler")))
            {
                AddGoal(new Goal("Become Ruler", GoalType.Social, personality.Ambition));
            }
        }
    }
    
    private void AdjustGoalPriorities(EmotionalState emotions)
    {
        var activeEmotions = emotions.GetActiveEmotions();
        
        foreach (var goal in goals.Where(g => g.IsActive))
        {
            var emotionModifier = 1.0f;
            
            // Emotion-based goal priority adjustments
            foreach (var emotion in activeEmotions)
            {
                emotionModifier *= emotion.Key switch
                {
                    EmotionType.Anger when goal.Type == GoalType.Social && goal.Name.Contains("Revenge") => 1.5f,
                    EmotionType.Fear when goal.Type == GoalType.Personal => 1.3f,
                    EmotionType.Greed when goal.Type == GoalType.Economic => 1.4f,
                    EmotionType.Confidence when goal.Type == GoalType.Social && goal.Name.Contains("Power") => 1.2f,
                    EmotionType.Loneliness when goal.Type == GoalType.Social && goal.Name.Contains("Friends") => 1.6f,
                    _ => 1.0f
                };
            }
            
            goal.EmotionModifier = emotionModifier;
        }
    }
    
    public void OnLevelUp(int newLevel)
    {
        // Boost ambition-related goals on level up
        foreach (var goal in goals.Where(g => g.Type == GoalType.Personal || g.Type == GoalType.Social))
        {
            goal.Priority += 0.1f;
        }
        
        // Add new high-level goals
        if (newLevel >= 15 && personality.Ambition > 0.7f)
        {
            if (!goals.Any(g => g.Name.Contains("Elite Status")))
            {
                AddGoal(new Goal("Achieve Elite Status", GoalType.Social, 0.8f));
            }
        }
    }
    
    public string GetGoalsSummary()
    {
        var activeGoals = GetActiveGoals();
        if (!activeGoals.Any())
        {
            return "No active goals";
        }
        
        var summary = "Active Goals:\n";
        foreach (var goal in activeGoals.Take(3))
        {
            summary += $"  - {goal.Name} (Priority: {goal.GetEffectivePriority():F2})\n";
        }
        
        return summary.TrimEnd();
    }
}

public class Goal
{
    public string Name { get; set; }
    public GoalType Type { get; set; }
    public float Priority { get; set; }
    public float EmotionModifier { get; set; } = 1.0f;
    public bool IsActive { get; set; } = true;
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedTime { get; set; } = DateTime.Now;
    public string TargetCharacter { get; set; } // For revenge goals
    public string TargetLocation { get; set; } // For location-based goals
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    // Progress tracking for serialization
    public float Progress { get; set; } = 0f;
    public float TargetValue { get; set; } = 1f;
    public float CurrentValue { get; set; } = 0f;
    
    public Goal(string name, GoalType type, float priority)
    {
        Name = name;
        Type = type;
        Priority = Math.Max(0.0f, Math.Min(1.0f, priority));
    }
    
    public float GetEffectivePriority()
    {
        var timeFactor = 1.0f;
        var age = DateTime.Now - CreatedTime;
        
        // Some goals become more urgent over time
        if (Type == GoalType.Personal)
        {
            timeFactor = 1.0f + (float)(age.TotalHours * 0.01f); // Gradually increase
        }
        
        return Priority * EmotionModifier * timeFactor;
    }
    
    public void Complete()
    {
        IsCompleted = true;
        IsActive = false;
    }
    
    public bool IsUrgent()
    {
        return GetEffectivePriority() > 0.8f;
    }
    
    public override string ToString()
    {
        var status = IsCompleted ? "[DONE]" : IsActive ? "[ACTIVE]" : "[INACTIVE]";
        return $"{status} {Name} ({Type}) - Priority: {GetEffectivePriority():F2}";
    }
}

public enum GoalType
{
    Personal,   // Self-improvement, survival, health
    Social,     // Relationships, reputation, power
    Economic,   // Wealth, trade, resources
    Combat,     // Fighting, revenge, dominance
    Exploration // Discovery, adventure, knowledge
} 
