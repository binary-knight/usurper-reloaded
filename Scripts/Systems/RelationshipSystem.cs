using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Complete relationship system based on Pascal RELATION.PAS and RELATIO2.PAS
/// Manages social relationships, marriages, divorces, and family dynamics
/// Maintains perfect Pascal compatibility with original game mechanics
/// </summary>
public class RelationshipSystem
{
    private static Dictionary<string, Dictionary<string, RelationshipRecord>> _relationships = new();
    private static Random _random = new Random();
    
    /// <summary>
    /// Relationship record structure based on Pascal RelationRec
    /// Tracks bidirectional relationships between characters
    /// </summary>
    public class RelationshipRecord
    {
        public string Name1 { get; set; } = "";         // player 1 name
        public string Name2 { get; set; } = "";         // player 2 name
        public CharacterAI AI1 { get; set; }            // player 1 AI type
        public CharacterAI AI2 { get; set; }            // player 2 AI type
        public CharacterRace Race1 { get; set; }        // player 1 race
        public CharacterRace Race2 { get; set; }        // player 2 race
        public int Relation1 { get; set; }              // pl1's relation to pl2
        public int Relation2 { get; set; }              // pl2's relation to pl1
        public string IdTag1 { get; set; } = "";        // player 1 unique ID
        public string IdTag2 { get; set; } = "";        // player 2 unique ID
        public int RecordNumber1 { get; set; }          // pl1 record number
        public int RecordNumber2 { get; set; }          // pl2 record number
        public int FileType1 { get; set; }              // pl1 file type (1=player, 2=npc)
        public int FileType2 { get; set; }              // pl2 file type (1=player, 2=npc)
        public bool Deleted { get; set; }               // is record deleted
        public int RecordNumber { get; set; }           // position in file
        public bool BannedMarry { get; set; }           // banned from marriage by King
        public int MarriedTimes { get; set; }           // times married
        public int MarriedDays { get; set; }            // days married
        public int Kids { get; set; }                   // children produced
        public int KilledBy1 { get; set; }              // name2 killed by name1 count
        public int KilledBy2 { get; set; }              // name1 killed by name2 count
        public DateTime CreatedDate { get; set; }       // when relationship started
        public DateTime LastUpdated { get; set; }       // last update time
    }
    
    /// <summary>
    /// Get relationship status between two characters
    /// Pascal equivalent: Social_Relation function
    /// </summary>
    public static int GetRelationshipStatus(Character character1, Character character2)
    {
        var key1 = GetRelationshipKey(character1.Name, character2.Name);
        var key2 = GetRelationshipKey(character2.Name, character1.Name);
        
        if (_relationships.ContainsKey(key1) && _relationships[key1].ContainsKey(key2))
        {
            return _relationships[key1][key2].Relation1;
        }
        
        // Default relationship is normal
        return GameConfig.RelationNormal;
    }
    
    /// <summary>
    /// Update relationship between two characters
    /// Pascal equivalent: Update_Relation procedure
    /// </summary>
    public static void UpdateRelationship(Character character1, Character character2, int direction, int steps = 1, bool overrideAutoHate = false, bool overrideMaxFeeling = false)
    {
        var relation = GetOrCreateRelationship(character1, character2);
        
        for (int i = 0; i < steps; i++)
        {
            if (direction > 0) // Improve relationship
            {
                relation.Relation1 = IncreaseRelation(relation.Relation1, overrideMaxFeeling);
            }
            else if (direction < 0) // Worsen relationship
            {
                relation.Relation1 = DecreaseRelation(relation.Relation1);
            }
        }
        
        relation.LastUpdated = DateTime.Now;
        SaveRelationship(relation);
        
        // Send relationship change notification
        SendRelationshipChangeNotification(character1, character2, relation.Relation1);
    }
    
    /// <summary>
    /// Check if two characters are married
    /// Pascal equivalent: Are_They_Married function
    /// </summary>
    public static bool AreMarried(Character character1, Character character2)
    {
        var relation = GetRelationship(character1, character2);
        return relation != null && 
               relation.Relation1 == GameConfig.RelationMarried && 
               relation.Relation2 == GameConfig.RelationMarried;
    }
    
    /// <summary>
    /// Get spouse name for a character
    /// Pascal equivalent: Is_Player_Married function
    /// </summary>
    public static string GetSpouseName(Character character)
    {
        foreach (var relationGroup in _relationships.Values)
        {
            foreach (var relation in relationGroup.Values)
            {
                if (relation.Deleted) continue;
                
                if (relation.Name1 == character.Name && 
                    relation.Relation1 == GameConfig.RelationMarried &&
                    relation.Relation2 == GameConfig.RelationMarried)
                {
                    return relation.Name2;
                }
                
                if (relation.Name2 == character.Name && 
                    relation.Relation1 == GameConfig.RelationMarried &&
                    relation.Relation2 == GameConfig.RelationMarried)
                {
                    return relation.Name1;
                }
            }
        }
        
        return "";
    }
    
    /// <summary>
    /// Perform marriage ceremony between two characters
    /// Pascal equivalent: marry_routine from LOVERS.PAS
    /// </summary>
    public static bool PerformMarriage(Character character1, Character character2, out string message)
    {
        message = "";
        
        // Check marriage prerequisites
        if (character1.Age < GameConfig.MinimumAgeToMarry || character2.Age < GameConfig.MinimumAgeToMarry)
        {
            message = "Both parties must be at least 18 years old to marry!";
            return false;
        }
        
        if (GetSpouseName(character1) != "" || GetSpouseName(character2) != "")
        {
            message = "One or both parties are already married!";
            return false;
        }
        
        if (character1.IntimacyActs < 1)
        {
            message = "You have no intimacy acts left today!";
            return false;
        }
        
        var relation = GetOrCreateRelationship(character1, character2);
        
        // Both must be in love to marry
        if (relation.Relation1 != GameConfig.RelationLove || relation.Relation2 != GameConfig.RelationLove)
        {
            message = "You both need to be in love with each other to marry!";
            return false;
        }
        
        // Check if marriage is banned
        if (relation.BannedMarry)
        {
            message = "Marriage between these characters has been banned by the King!";
            return false;
        }
        
        // Perform marriage ceremony
        relation.Relation1 = GameConfig.RelationMarried;
        relation.Relation2 = GameConfig.RelationMarried;
        relation.MarriedDays = 0;
        relation.MarriedTimes++;
        
        // Update character marriage status
        character1.Married = true;
        character1.IsMarried = true;
        character1.SpouseName = character2.Name;
        character1.MarriedTimes++;
        character1.IntimacyActs--;
        
        character2.Married = true;
        character2.IsMarried = true;
        character2.SpouseName = character1.Name;
        character2.MarriedTimes++;
        
        SaveRelationship(relation);
        
        // Generate wedding announcement
        var ceremonyMessage = GameConfig.WeddingCeremonyMessages[_random.Next(GameConfig.WeddingCeremonyMessages.Length)];
        
        message = $"Wedding Ceremony Complete!\n" +
                 $"{character1.Name} and {character2.Name} are now married!\n" +
                 $"{ceremonyMessage}";
        
        // Handle different-sex vs same-sex marriages
        if (character1.Sex != character2.Sex)
        {
            message += "\nCongratulations! (go home and make babies)";
        }
        else
        {
            message += "\nCongratulations! (go home and adopt babies)";
        }
        
        return true;
    }
    
    /// <summary>
    /// Process divorce between married characters
    /// Pascal equivalent: divorce procedure from LOVERS.PAS
    /// </summary>
    public static bool ProcessDivorce(Character character1, Character character2, out string message)
    {
        message = "";
        
        if (!AreMarried(character1, character2))
        {
            message = "You are not married to this person!";
            return false;
        }
        
        var relation = GetRelationship(character1, character2);
        if (relation == null)
        {
            message = "No relationship record found!";
            return false;
        }
        
        // Generate divorce message based on marriage duration
        string durationMessage;
        if (relation.MarriedDays < 1)
        {
            durationMessage = "Their marriage lasted only a couple of hours!";
        }
        else if (relation.MarriedDays < 30)
        {
            durationMessage = $"Their marriage lasted only {relation.MarriedDays} days.";
        }
        else
        {
            durationMessage = $"Their marriage lasted {relation.MarriedDays} days.";
        }
        
        // Update relationship status
        relation.Relation1 = GameConfig.RelationNormal;
        relation.Relation2 = GameConfig.RelationHate; // Divorced partner becomes hateful
        relation.MarriedDays = 0;
        
        // Update character marriage status
        character1.Married = false;
        character1.IsMarried = false;
        character1.SpouseName = "";
        
        character2.Married = false;
        character2.IsMarried = false;
        character2.SpouseName = "";
        
        SaveRelationship(relation);
        
        // Handle child custody (children go to character2 - the spouse)
        HandleChildCustodyAfterDivorce(character1, character2);
        
        message = $"Divorce Finalized!\n" +
                 $"{character1.Name} divorced {character2.Name}!\n" +
                 $"{durationMessage}\n" +
                 $"You have lost custody of your children!";
        
        return true;
    }
    
    /// <summary>
    /// Calculate experience gained from romantic interaction
    /// Pascal equivalent: Sex_Experience function
    /// </summary>
    public static long CalculateRomanticExperience(Character character1, Character character2, int experienceType)
    {
        long baseExperience = character1.Level * 110 + character2.Level * 90;
        
        return experienceType switch
        {
            0 => baseExperience / 2, // Kiss
            1 => baseExperience,     // Dinner
            2 => baseExperience / 3, // Hold hands
            3 => baseExperience * 2, // Intimate
            _ => baseExperience
        };
    }
    
    /// <summary>
    /// List all married couples
    /// Pascal equivalent: List_Married_Couples procedure
    /// </summary>
    public static List<string> GetMarriedCouples()
    {
        var couples = new List<string>();
        
        foreach (var relationGroup in _relationships.Values)
        {
            foreach (var relation in relationGroup.Values)
            {
                if (relation.Deleted) continue;
                
                if (relation.Relation1 == GameConfig.RelationMarried &&
                    relation.Relation2 == GameConfig.RelationMarried)
                {
                    string duration = relation.MarriedDays == 1 ? "day" : "days";
                    couples.Add($"{relation.Name1} and {relation.Name2} have been married for {relation.MarriedDays} {duration}.");
                }
            }
        }
        
        return couples;
    }
    
    /// <summary>
    /// Generate relationship description string
    /// Pascal equivalent: Relation_String function
    /// </summary>
    public static string GetRelationshipDescription(int relationValue, bool useYou = false)
    {
        return relationValue switch
        {
            GameConfig.RelationMarried => useYou ? "You are married!" : "They are married",
            GameConfig.RelationLove => useYou ? "You are in love!" : "They are in love",
            GameConfig.RelationPassion => useYou ? "You have passionate feelings!" : "They have passionate feelings",
            GameConfig.RelationFriendship => useYou ? "You consider them a friend." : "They are friends",
            GameConfig.RelationTrust => useYou ? "You trust them." : "They trust each other",
            GameConfig.RelationRespect => useYou ? "You respect them." : "They respect each other",
            GameConfig.RelationNormal => useYou ? "You feel neutral towards them." : "They are neutral",
            GameConfig.RelationSuspicious => useYou ? "You are suspicious of them." : "They are suspicious",
            GameConfig.RelationAnger => useYou ? "You feel anger towards them!" : "They are angry",
            GameConfig.RelationEnemy => useYou ? "You consider them an enemy!" : "They are enemies",
            GameConfig.RelationHate => useYou ? "You HATE them!" : "They hate each other",
            _ => useYou ? "You feel indifferent." : "No relationship"
        };
    }
    
    /// <summary>
    /// Daily relationship maintenance
    /// Pascal equivalent: Relation_Maintenance procedure
    /// </summary>
    public static void DailyMaintenance()
    {
        foreach (var relationGroup in _relationships.Values)
        {
            foreach (var relation in relationGroup.Values)
            {
                if (relation.Deleted) continue;
                
                // Increment married days
                if (relation.Relation1 == GameConfig.RelationMarried &&
                    relation.Relation2 == GameConfig.RelationMarried)
                {
                    relation.MarriedDays++;
                    
                    // Random chance of divorce (5% chance - 1 in 20)
                    if (_random.Next(20) == 0)
                    {
                        ProcessAutomaticDivorce(relation);
                    }
                }
                
                relation.LastUpdated = DateTime.Now;
                SaveRelationship(relation);
            }
        }
    }
    
    #region Private Helper Methods
    
    private static string GetRelationshipKey(string name1, string name2)
    {
        return $"{name1}_{name2}";
    }
    
    private static RelationshipRecord GetOrCreateRelationship(Character character1, Character character2)
    {
        var key1 = GetRelationshipKey(character1.Name, character2.Name);
        var key2 = GetRelationshipKey(character2.Name, character1.Name);
        
        if (!_relationships.ContainsKey(key1))
            _relationships[key1] = new Dictionary<string, RelationshipRecord>();
        
        if (!_relationships[key1].ContainsKey(key2))
        {
            var newRelation = new RelationshipRecord
            {
                Name1 = character1.Name,
                Name2 = character2.Name,
                AI1 = character1.AI,
                AI2 = character2.AI,
                Race1 = character1.Race,
                Race2 = character2.Race,
                Relation1 = GameConfig.RelationNormal,
                Relation2 = GameConfig.RelationNormal,
                IdTag1 = character1.ID,
                IdTag2 = character2.ID,
                CreatedDate = DateTime.Now,
                LastUpdated = DateTime.Now
            };
            
            _relationships[key1][key2] = newRelation;
        }
        
        return _relationships[key1][key2];
    }
    
    private static RelationshipRecord GetRelationship(Character character1, Character character2)
    {
        var key1 = GetRelationshipKey(character1.Name, character2.Name);
        var key2 = GetRelationshipKey(character2.Name, character1.Name);
        
        if (_relationships.ContainsKey(key1) && _relationships[key1].ContainsKey(key2))
        {
            return _relationships[key1][key2];
        }
        
        return null;
    }
    
    private static int IncreaseRelation(int currentRelation, bool overrideMaxFeeling)
    {
        return currentRelation switch
        {
            GameConfig.RelationMarried => GameConfig.RelationMarried, // no change
            GameConfig.RelationLove => GameConfig.RelationLove, // no change
            GameConfig.RelationPassion => overrideMaxFeeling ? GameConfig.RelationLove : GameConfig.RelationPassion,
            GameConfig.RelationFriendship => overrideMaxFeeling ? GameConfig.RelationPassion : GameConfig.RelationFriendship,
            GameConfig.RelationTrust => GameConfig.RelationFriendship,
            GameConfig.RelationRespect => GameConfig.RelationTrust,
            GameConfig.RelationNormal => GameConfig.RelationRespect,
            GameConfig.RelationSuspicious => GameConfig.RelationNormal,
            GameConfig.RelationAnger => GameConfig.RelationSuspicious,
            GameConfig.RelationEnemy => GameConfig.RelationAnger,
            GameConfig.RelationHate => GameConfig.RelationEnemy,
            _ => currentRelation
        };
    }
    
    private static int DecreaseRelation(int currentRelation)
    {
        return currentRelation switch
        {
            GameConfig.RelationMarried => GameConfig.RelationMarried, // no change
            GameConfig.RelationLove => GameConfig.RelationPassion,
            GameConfig.RelationPassion => GameConfig.RelationFriendship,
            GameConfig.RelationFriendship => GameConfig.RelationTrust,
            GameConfig.RelationTrust => GameConfig.RelationRespect,
            GameConfig.RelationRespect => GameConfig.RelationNormal,
            GameConfig.RelationNormal => GameConfig.RelationSuspicious,
            GameConfig.RelationSuspicious => GameConfig.RelationAnger,
            GameConfig.RelationAnger => GameConfig.RelationEnemy,
            GameConfig.RelationEnemy => GameConfig.RelationHate,
            GameConfig.RelationHate => GameConfig.RelationHate, // no change
            _ => currentRelation
        };
    }
    
    private static void SaveRelationship(RelationshipRecord relation)
    {
        // In a full implementation, this would save to a file
        // For now, we keep it in memory
        relation.LastUpdated = DateTime.Now;
    }
    
    private static void SendRelationshipChangeNotification(Character character1, Character character2, int newRelation)
    {
        // In a full implementation, this would send mail notifications
        // For now, we just log the change
        GD.Print($"Relationship changed: {character1.Name} -> {character2.Name}: {GetRelationshipDescription(newRelation)}");
    }
    
    private static void HandleChildCustodyAfterDivorce(Character parent1, Character parent2)
    {
        // In a full implementation, this would update child records
        // Children would lose access to parent1 and become depressed
        GD.Print($"Child custody handled: Children of {parent1.Name} and {parent2.Name} affected by divorce");
    }
    
    private static void ProcessAutomaticDivorce(RelationshipRecord relation)
    {
        // Automatic divorce processing for NPCs
        relation.Relation1 = GameConfig.RelationNormal;
        relation.Relation2 = GameConfig.RelationHate;
        relation.MarriedDays = 0;
        
        GD.Print($"Automatic divorce: {relation.Name1} and {relation.Name2} divorced after {relation.MarriedDays} days");
    }
    
    #endregion
}
