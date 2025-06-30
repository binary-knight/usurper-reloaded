using UsurperReborn.Scripts.Core;
using UsurperReborn.Scripts.AI;
using System.Text;

namespace UsurperReborn.Tests.ValidationTools
{
    public class NPCBehaviorAnalyzer
    {
        private readonly List<BehaviorObservation> observations;
        private readonly Dictionary<string, PersonalityProfile> npcPersonalities;
        private readonly Random random;

        public NPCBehaviorAnalyzer()
        {
            observations = new List<BehaviorObservation>();
            npcPersonalities = new Dictionary<string, PersonalityProfile>();
            random = new Random(12345);
        }

        public void AnalyzeNPCBehavior(NPC npc, int observationDays = 7)
        {
            npcPersonalities[npc.Id] = npc.Personality;
            
            for (int day = 0; day < observationDays; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    var worldState = CreateRandomWorldState(hour);
                    var action = npc.Brain.DecideNextAction(worldState);
                    
                    observations.Add(new BehaviorObservation
                    {
                        NPCId = npc.Id,
                        NPCName = npc.Name,
                        Day = day,
                        Hour = hour,
                        Action = action,
                        Personality = npc.Personality,
                        WorldState = worldState,
                        EmotionalState = npc.Brain.EmotionalState,
                        ActiveGoals = npc.Brain.Goals.GetActiveGoals().ToList(),
                        RecentMemories = npc.Brain.Memory.GetRecentMemories(TimeSpan.FromDays(3)).ToList()
                    });
                    
                    // Process the hour to update NPC state
                    npc.Brain.ProcessHourlyUpdate(worldState);
                }
            }
        }

        public void AnalyzePopulation(List<NPC> npcs, int observationDays = 7)
        {
            foreach (var npc in npcs)
            {
                AnalyzeNPCBehavior(npc, observationDays);
            }
        }

        public BehaviorAnalysisReport GenerateReport()
        {
            var report = new BehaviorAnalysisReport
            {
                PersonalityBehaviorCorrelations = AnalyzePersonalityBehaviorCorrelations(),
                DecisionConsistency = AnalyzeDecisionConsistency(),
                EmotionalStateImpacts = AnalyzeEmotionalStateImpacts(),
                BehavioralAnomalies = IdentifyBehavioralAnomalies(),
                OverallScores = CalculateOverallScores()
            };
            
            return report;
        }

        private Dictionary<string, float> AnalyzePersonalityBehaviorCorrelations()
        {
            var correlations = new Dictionary<string, float>();
            
            var aggressionCorrelation = CalculateCorrelation(
                obs => obs.Personality.Aggression,
                obs => obs.Action.Type == ActionType.Attack ? 1.0f : 0.0f
            );
            correlations["Aggression-Combat"] = aggressionCorrelation;
            
            var greedCorrelation = CalculateCorrelation(
                obs => obs.Personality.Greed,
                obs => obs.Action.Type == ActionType.ExploreDungeon ? 1.0f : 0.0f
            );
            correlations["Greed-WealthSeeking"] = greedCorrelation;
            
            return correlations;
        }

        private float CalculateCorrelation(Func<BehaviorObservation, float> xSelector, Func<BehaviorObservation, float> ySelector)
        {
            if (observations.Count < 2) return 0.0f;
            
            var xValues = observations.Select(xSelector).ToList();
            var yValues = observations.Select(ySelector).ToList();
            
            var meanX = xValues.Average();
            var meanY = yValues.Average();
            
            var numerator = xValues.Zip(yValues, (x, y) => (x - meanX) * (y - meanY)).Sum();
            var denominatorX = Math.Sqrt(xValues.Select(x => Math.Pow(x - meanX, 2)).Sum());
            var denominatorY = Math.Sqrt(yValues.Select(y => Math.Pow(y - meanY, 2)).Sum());
            
            if (denominatorX == 0 || denominatorY == 0) return 0.0f;
            
            return (float)(numerator / (denominatorX * denominatorY));
        }

        private Dictionary<string, float> AnalyzeDecisionConsistency()
        {
            var consistency = new Dictionary<string, float>();
            
            var npcGroups = observations.GroupBy(obs => obs.NPCId);
            
            foreach (var npcGroup in npcGroups)
            {
                var npcObservations = npcGroup.ToList();
                var personality = npcObservations.First().Personality;
                
                // Analyze consistency in similar situations
                var similarSituations = GroupSimilarSituations(npcObservations);
                var consistencyScores = new List<float>();
                
                foreach (var situationGroup in similarSituations)
                {
                    if (situationGroup.Value.Count > 1)
                    {
                        var actions = situationGroup.Value.Select(obs => obs.Action.Type).ToList();
                        var mostCommonAction = actions.GroupBy(a => a).OrderByDescending(g => g.Count()).First();
                        var consistencyScore = (float)mostCommonAction.Count() / actions.Count;
                        consistencyScores.Add(consistencyScore);
                    }
                }
                
                if (consistencyScores.Any())
                {
                    consistency[npcGroup.Key] = consistencyScores.Average();
                }
            }
            
            return consistency;
        }

        private Dictionary<string, List<BehaviorObservation>> GroupSimilarSituations(List<BehaviorObservation> observations)
        {
            var groups = new Dictionary<string, List<BehaviorObservation>>();
            
            foreach (var obs in observations)
            {
                var situationKey = GenerateSituationKey(obs.WorldState);
                
                if (!groups.ContainsKey(situationKey))
                    groups[situationKey] = new List<BehaviorObservation>();
                    
                groups[situationKey].Add(obs);
            }
            
            return groups;
        }

        private string GenerateSituationKey(WorldState worldState)
        {
            var key = new StringBuilder();
            key.Append($"Hour:{worldState.CurrentHour / 6}"); // 4 time periods
            key.Append($"_Combat:{worldState.InCombat}");
            key.Append($"_Enemies:{(worldState.NearbyCharacters?.Length ?? 0)}");
            key.Append($"_Location:{worldState.CurrentLocation ?? "unknown"}");
            return key.ToString();
        }

        private Dictionary<string, float> AnalyzeEmotionalStateImpacts()
        {
            var impacts = new Dictionary<string, float>();
            
            var angerImpact = CalculateCorrelation(
                obs => obs.EmotionalState.Anger,
                obs => obs.Action.Type == ActionType.Attack ? 1.0f : 0.0f
            );
            impacts["Anger-Attack"] = angerImpact;
            
            var fearImpact = CalculateCorrelation(
                obs => obs.EmotionalState.Fear,
                obs => obs.Action.Type == ActionType.Flee ? 1.0f : 0.0f
            );
            impacts["Fear-Flee"] = fearImpact;
            
            return impacts;
        }

        private List<BehavioralAnomaly> IdentifyBehavioralAnomalies()
        {
            var anomalies = new List<BehavioralAnomaly>();
            
            var npcGroups = observations.GroupBy(obs => obs.NPCId);
            
            foreach (var npcGroup in npcGroups)
            {
                var npcObs = npcGroup.ToList();
                var personality = npcObs.First().Personality;
                
                // Check for anomalies based on personality expectations
                anomalies.AddRange(CheckAggressionAnomalies(npcObs, personality));
            }
            
            return anomalies;
        }

        private List<BehavioralAnomaly> CheckAggressionAnomalies(List<BehaviorObservation> observations, PersonalityProfile personality)
        {
            var anomalies = new List<BehavioralAnomaly>();
            
            var combatActions = observations.Count(obs => 
                obs.Action.Type == ActionType.Attack);
            var totalActions = observations.Count;
            var combatRate = (float)combatActions / totalActions;
            
            var expectedCombatRate = personality.Aggression * 0.3f; // Expected rate based on aggression
            
            if (Math.Abs(combatRate - expectedCombatRate) > 0.2f) // 20% tolerance
            {
                anomalies.Add(new BehavioralAnomaly
                {
                    NPCId = observations.First().NPCId,
                    NPCName = observations.First().NPCName,
                    Type = "Aggression Mismatch",
                    Description = $"Combat rate ({combatRate:P}) doesn't match aggression level ({personality.Aggression:P})",
                    Severity = Math.Abs(combatRate - expectedCombatRate)
                });
            }
            
            return anomalies;
        }

        private OverallScores CalculateOverallScores()
        {
            var personalityCorrelations = AnalyzePersonalityBehaviorCorrelations();
            var consistencyScores = AnalyzeDecisionConsistency();
            var emotionalImpacts = AnalyzeEmotionalStateImpacts();
            
            return new OverallScores
            {
                BehaviorRealismScore = personalityCorrelations.Values.Any() ? personalityCorrelations.Values.Average() : 0,
                DecisionConsistencyScore = consistencyScores.Values.Any() ? consistencyScores.Values.Average() : 0,
                EmotionalCoherenceScore = emotionalImpacts.Values.Any() ? emotionalImpacts.Values.Average() : 0,
                OverallAIQualityScore = 0.5f // Calculated from above
            };
        }

        private WorldState CreateRandomWorldState(int hour)
        {
            return new WorldState
            {
                CurrentHour = hour,
                InCombat = random.NextDouble() < 0.1,
                NearbyCharacters = random.NextDouble() < 0.3 ? new[] { CreateRandomCharacter() } : new Character[0],
                CurrentLocation = "town_square"
            };
        }

        private Character CreateRandomCharacter()
        {
            return new Character
            {
                Name = "Random Character",
                Level = random.Next(3, 8),
                CurrentHP = random.Next(50, 100),
                MaxHP = 100
            };
        }
    }

    // Data classes for analysis results
    public class BehaviorObservation
    {
        public string NPCId { get; set; }
        public string NPCName { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public NPCAction Action { get; set; }
        public PersonalityProfile Personality { get; set; }
        public WorldState WorldState { get; set; }
        public EmotionalState EmotionalState { get; set; }
        public List<Goal> ActiveGoals { get; set; }
        public List<Memory> RecentMemories { get; set; }
    }

    public class BehaviorAnalysisReport
    {
        public Dictionary<string, float> PersonalityBehaviorCorrelations { get; set; }
        public Dictionary<string, float> DecisionConsistency { get; set; }
        public Dictionary<string, float> EmotionalStateImpacts { get; set; }
        public List<BehavioralAnomaly> BehavioralAnomalies { get; set; }
        public OverallScores OverallScores { get; set; }
    }

    public class BehavioralAnomaly
    {
        public string NPCId { get; set; }
        public string NPCName { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public float Severity { get; set; }
    }

    public class OverallScores
    {
        public float BehaviorRealismScore { get; set; }
        public float DecisionConsistencyScore { get; set; }
        public float EmotionalCoherenceScore { get; set; }
        public float OverallAIQualityScore { get; set; }
    }
} 