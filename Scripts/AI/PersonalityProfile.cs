using UsurperRemake.Utils;
using Godot;
using System.Collections.Generic;

public class PersonalityProfile
{
    // Core traits (0.0 to 1.0)
    public float Aggression { get; set; }      // Likelihood to start fights
    public float Greed { get; set; }           // Focus on wealth accumulation
    public float Courage { get; set; }         // Willingness to take risks
    public float Loyalty { get; set; }         // Stays with gangs/friends
    public float Vengefulness { get; set; }    // Seeks revenge for wrongs
    public float Impulsiveness { get; set; }   // Makes quick decisions
    public float Sociability { get; set; }     // Seeks interaction
    public float Ambition { get; set; }        // Desires power/status
    
    // Behavioral modifiers
    public CombatStyle PreferredCombatStyle { get; set; }
    public List<string> Fears { get; set; } = new List<string>();
    public List<string> Desires { get; set; } = new List<string>();
    public string Archetype { get; set; }
    
    public static PersonalityProfile GenerateRandom(string npcArchetype)
    {
        var profile = new PersonalityProfile();
        profile.Archetype = npcArchetype;
        
        switch (npcArchetype.ToLower())
        {
            case "thug":
            case "brawler":
                profile.Aggression = GD.Randf() * 0.3f + 0.7f;  // 0.7-1.0
                profile.Courage = GD.Randf() * 0.4f + 0.6f;     // 0.6-1.0
                profile.Greed = GD.Randf() * 0.5f + 0.3f;       // 0.3-0.8
                profile.Loyalty = GD.Randf() * 0.3f + 0.2f;     // 0.2-0.5
                profile.Vengefulness = GD.Randf() * 0.3f + 0.6f; // 0.6-0.9
                profile.Impulsiveness = GD.Randf() * 0.4f + 0.5f; // 0.5-0.9
                profile.Sociability = GD.Randf() * 0.4f + 0.3f; // 0.3-0.7
                profile.Ambition = GD.Randf() * 0.5f + 0.2f;    // 0.2-0.7
                profile.PreferredCombatStyle = CombatStyle.Aggressive;
                profile.Fears.Add("stronger_opponents");
                profile.Fears.Add("law_enforcement");
                profile.Desires.Add("dominance");
                profile.Desires.Add("respect");
                break;
                
            case "merchant":
            case "trader":
                profile.Aggression = GD.Randf() * 0.3f;         // 0.0-0.3
                profile.Greed = GD.Randf() * 0.3f + 0.7f;      // 0.7-1.0
                profile.Courage = GD.Randf() * 0.4f + 0.2f;     // 0.2-0.6
                profile.Loyalty = GD.Randf() * 0.4f + 0.4f;     // 0.4-0.8
                profile.Vengefulness = GD.Randf() * 0.3f + 0.2f; // 0.2-0.5
                profile.Impulsiveness = GD.Randf() * 0.3f + 0.1f; // 0.1-0.4
                profile.Sociability = GD.Randf() * 0.3f + 0.6f; // 0.6-0.9
                profile.Ambition = GD.Randf() * 0.3f + 0.5f;    // 0.5-0.8
                profile.PreferredCombatStyle = CombatStyle.Defensive;
                profile.Fears.Add("poverty");
                profile.Fears.Add("violence");
                profile.Fears.Add("theft");
                profile.Desires.Add("wealth");
                profile.Desires.Add("security");
                profile.Desires.Add("reputation");
                break;
                
            case "noble":
            case "aristocrat":
                profile.Ambition = GD.Randf() * 0.2f + 0.8f;    // 0.8-1.0
                profile.Vengefulness = GD.Randf() * 0.4f + 0.5f;// 0.5-0.9
                profile.Loyalty = GD.Randf() * 0.3f + 0.4f;     // 0.4-0.7
                profile.Courage = GD.Randf() * 0.3f + 0.5f;     // 0.5-0.8
                profile.Sociability = GD.Randf() * 0.3f + 0.4f; // 0.4-0.7
                profile.Greed = GD.Randf() * 0.4f + 0.4f;       // 0.4-0.8
                profile.Aggression = GD.Randf() * 0.4f + 0.3f;  // 0.3-0.7
                profile.Impulsiveness = GD.Randf() * 0.3f + 0.2f; // 0.2-0.5
                profile.PreferredCombatStyle = CombatStyle.Tactical;
                profile.Fears.Add("disgrace");
                profile.Fears.Add("poverty");
                profile.Desires.Add("power");
                profile.Desires.Add("respect");
                profile.Desires.Add("influence");
                break;
                
            case "guard":
            case "soldier":
                profile.Loyalty = GD.Randf() * 0.2f + 0.7f;     // 0.7-0.9
                profile.Courage = GD.Randf() * 0.3f + 0.6f;     // 0.6-0.9
                profile.Aggression = GD.Randf() * 0.3f + 0.4f;  // 0.4-0.7
                profile.Ambition = GD.Randf() * 0.4f + 0.3f;    // 0.3-0.7
                profile.Vengefulness = GD.Randf() * 0.3f + 0.4f; // 0.4-0.7
                profile.Impulsiveness = GD.Randf() * 0.3f + 0.2f; // 0.2-0.5
                profile.Sociability = GD.Randf() * 0.4f + 0.3f; // 0.3-0.7
                profile.Greed = GD.Randf() * 0.4f + 0.2f;       // 0.2-0.6
                profile.PreferredCombatStyle = CombatStyle.Balanced;
                profile.Fears.Add("dereliction");
                profile.Fears.Add("dishonor");
                profile.Desires.Add("order");
                profile.Desires.Add("justice");
                profile.Desires.Add("duty");
                break;
                
            case "priest":
            case "cleric":
                profile.Loyalty = GD.Randf() * 0.3f + 0.6f;     // 0.6-0.9
                profile.Courage = GD.Randf() * 0.4f + 0.4f;     // 0.4-0.8
                profile.Aggression = GD.Randf() * 0.2f;         // 0.0-0.2
                profile.Ambition = GD.Randf() * 0.3f + 0.3f;    // 0.3-0.6
                profile.Vengefulness = GD.Randf() * 0.2f;       // 0.0-0.2
                profile.Impulsiveness = GD.Randf() * 0.3f + 0.1f; // 0.1-0.4
                profile.Sociability = GD.Randf() * 0.3f + 0.6f; // 0.6-0.9
                profile.Greed = GD.Randf() * 0.3f;              // 0.0-0.3
                profile.PreferredCombatStyle = CombatStyle.Defensive;
                profile.Fears.Add("sin");
                profile.Fears.Add("corruption");
                profile.Desires.Add("peace");
                profile.Desires.Add("salvation");
                profile.Desires.Add("knowledge");
                break;
                
            case "mystic":
            case "mage":
                profile.Ambition = GD.Randf() * 0.3f + 0.6f;    // 0.6-0.9
                profile.Courage = GD.Randf() * 0.5f + 0.3f;     // 0.3-0.8
                profile.Aggression = GD.Randf() * 0.4f + 0.2f;  // 0.2-0.6
                profile.Loyalty = GD.Randf() * 0.4f + 0.3f;     // 0.3-0.7
                profile.Vengefulness = GD.Randf() * 0.4f + 0.4f; // 0.4-0.8
                profile.Impulsiveness = GD.Randf() * 0.3f + 0.2f; // 0.2-0.5
                profile.Sociability = GD.Randf() * 0.5f + 0.2f; // 0.2-0.7
                profile.Greed = GD.Randf() * 0.4f + 0.3f;       // 0.3-0.7
                profile.PreferredCombatStyle = CombatStyle.Tactical;
                profile.Fears.Add("ignorance");
                profile.Fears.Add("powerlessness");
                profile.Desires.Add("knowledge");
                profile.Desires.Add("power");
                profile.Desires.Add("secrets");
                break;
                
            case "craftsman":
            case "artisan":
                profile.Loyalty = GD.Randf() * 0.3f + 0.5f;     // 0.5-0.8
                profile.Courage = GD.Randf() * 0.4f + 0.4f;     // 0.4-0.8
                profile.Aggression = GD.Randf() * 0.3f + 0.2f;  // 0.2-0.5
                profile.Ambition = GD.Randf() * 0.4f + 0.3f;    // 0.3-0.7
                profile.Vengefulness = GD.Randf() * 0.3f + 0.3f; // 0.3-0.6
                profile.Impulsiveness = GD.Randf() * 0.3f + 0.2f; // 0.2-0.5
                profile.Sociability = GD.Randf() * 0.4f + 0.4f; // 0.4-0.8
                profile.Greed = GD.Randf() * 0.4f + 0.4f;       // 0.4-0.8
                profile.PreferredCombatStyle = CombatStyle.Balanced;
                profile.Fears.Add("mediocrity");
                profile.Fears.Add("poverty");
                profile.Desires.Add("mastery");
                profile.Desires.Add("recognition");
                profile.Desires.Add("quality");
                break;
                
            default: // commoner
                // Balanced random stats with slight variation
                profile.Aggression = GD.Randf() * 0.6f + 0.2f;  // 0.2-0.8
                profile.Greed = GD.Randf() * 0.6f + 0.2f;       // 0.2-0.8
                profile.Courage = GD.Randf() * 0.6f + 0.2f;     // 0.2-0.8
                profile.Loyalty = GD.Randf() * 0.6f + 0.2f;     // 0.2-0.8
                profile.Vengefulness = GD.Randf() * 0.6f + 0.2f; // 0.2-0.8
                profile.Impulsiveness = GD.Randf() * 0.8f + 0.1f; // 0.1-0.9
                profile.Sociability = GD.Randf() * 0.6f + 0.2f; // 0.2-0.8
                profile.Ambition = GD.Randf() * 0.6f + 0.2f;    // 0.2-0.8
                profile.PreferredCombatStyle = (CombatStyle)GD.RandRange(0, 3);
                profile.Fears.Add("death");
                profile.Fears.Add("poverty");
                profile.Desires.Add("survival");
                profile.Desires.Add("security");
                break;
        }
        
        return profile;
    }
    
    /// <summary>
    /// Generate personality profile for specific archetype - alias for GenerateRandom for API compatibility
    /// </summary>
    public static PersonalityProfile GenerateForArchetype(string archetype)
    {
        return GenerateRandom(archetype);
    }
    
    public float GetCompatibility(PersonalityProfile other)
    {
        // Calculate compatibility based on trait differences
        var aggressionDiff = Math.Abs(Aggression - other.Aggression);
        var loyaltyDiff = Math.Abs(Loyalty - other.Loyalty);
        var sociabilityDiff = Math.Abs(Sociability - other.Sociability);
        var ambitionDiff = Math.Abs(Ambition - other.Ambition);
        
        // Similar people are more compatible
        var traitCompatibility = 1.0f - (aggressionDiff + loyaltyDiff + sociabilityDiff + ambitionDiff) / 4.0f;
        
        // Special compatibility bonuses/penalties
        var archetypeBonus = GetArchetypeCompatibility(other.Archetype);
        
        return Math.Max(0.0f, Math.Min(1.0f, traitCompatibility + archetypeBonus));
    }
    
    private float GetArchetypeCompatibility(string otherArchetype)
    {
        // Define archetype relationships
        var compatibilityMatrix = new Dictionary<string, Dictionary<string, float>>
        {
            ["thug"] = new Dictionary<string, float>
            {
                ["thug"] = 0.2f,
                ["merchant"] = -0.3f,
                ["guard"] = -0.5f,
                ["priest"] = -0.4f,
                ["noble"] = -0.2f,
                ["commoner"] = 0.1f
            },
            ["merchant"] = new Dictionary<string, float>
            {
                ["merchant"] = 0.3f,
                ["thug"] = -0.3f,
                ["guard"] = 0.1f,
                ["priest"] = 0.1f,
                ["noble"] = 0.2f,
                ["craftsman"] = 0.3f
            },
            ["guard"] = new Dictionary<string, float>
            {
                ["guard"] = 0.4f,
                ["thug"] = -0.5f,
                ["priest"] = 0.3f,
                ["noble"] = 0.2f,
                ["merchant"] = 0.1f
            },
            ["priest"] = new Dictionary<string, float>
            {
                ["priest"] = 0.4f,
                ["thug"] = -0.4f,
                ["guard"] = 0.3f,
                ["noble"] = 0.1f,
                ["commoner"] = 0.2f
            }
        };
        
        if (compatibilityMatrix.ContainsKey(Archetype.ToLower()) &&
            compatibilityMatrix[Archetype.ToLower()].ContainsKey(otherArchetype.ToLower()))
        {
            return compatibilityMatrix[Archetype.ToLower()][otherArchetype.ToLower()];
        }
        
        return 0.0f;
    }
    
    public bool IsLikelyToJoinGang()
    {
        // Gang joining likelihood based on personality
        var gangScore = (Loyalty * 0.3f) + (Sociability * 0.3f) + (Ambition * 0.2f) + (Courage * 0.2f);
        return gangScore > 0.6f && Aggression > 0.3f;
    }
    
    public bool IsLikelyToBetrray()
    {
        // Betrayal likelihood
        var betrayalScore = (1.0f - Loyalty) * 0.4f + Greed * 0.3f + Ambition * 0.2f + Impulsiveness * 0.1f;
        return betrayalScore > 0.7f;
    }
    
    public bool IsLikelyToSeekRevenge()
    {
        return Vengefulness > 0.6f && (Aggression > 0.4f || Ambition > 0.5f);
    }
    
    public float GetDecisionWeight(string actionType)
    {
        return actionType.ToLower() switch
        {
            "attack" => Aggression * 0.7f + Courage * 0.3f,
            "flee" => (1.0f - Courage) * 0.6f + (1.0f - Aggression) * 0.4f,
            "negotiate" => Sociability * 0.5f + (1.0f - Aggression) * 0.3f + Charisma * 0.2f,
            "steal" => Greed * 0.6f + (1.0f - Loyalty) * 0.4f,
            "help" => Loyalty * 0.4f + Sociability * 0.3f + (1.0f - Greed) * 0.3f,
            "betray" => IsLikelyToBetrray() ? 0.8f : 0.1f,
            "revenge" => IsLikelyToSeekRevenge() ? 0.9f : 0.2f,
            "join_gang" => IsLikelyToJoinGang() ? 0.7f : 0.2f,
            "trade" => Greed * 0.4f + Sociability * 0.3f + (1.0f - Aggression) * 0.3f,
            "explore" => Courage * 0.5f + Ambition * 0.3f + (1.0f - Fear) * 0.2f,
            _ => 0.5f
        };
    }
    
    private float Charisma => Sociability; // Simplified for now
    private float Fear => Fears.Count * 0.1f; // Simple fear calculation
    
    public override string ToString()
    {
        return $"{Archetype}: Agg={Aggression:F2}, Greed={Greed:F2}, Courage={Courage:F2}, " +
               $"Loyalty={Loyalty:F2}, Vengeful={Vengefulness:F2}, Social={Sociability:F2}";
    }
}

public enum CombatStyle
{
    Aggressive,    // All-out attack
    Defensive,     // Focus on defense and counters
    Tactical,      // Strategic, uses abilities
    Balanced       // Adaptable approach
} 
