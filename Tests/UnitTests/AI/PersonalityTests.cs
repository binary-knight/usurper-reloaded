using UsurperRemake.Utils;
using NUnit.Framework;
using UsurperRemake;
using UsurperRemake;
using FluentAssertions;
using Newtonsoft.Json;

namespace UsurperReborn.Tests.UnitTests.AI
{
    [TestFixture]
    public class PersonalityTests
    {
        private PersonalityTestData testData;
        private Random random;

        [SetUp]
        public void Setup()
        {
            // Load test data
            var testDataJson = File.ReadAllText("TestData/personality_test_cases.json");
            testData = JsonConvert.DeserializeObject<PersonalityTestData>(testDataJson);
            
            random = new Random(12345); // Fixed seed for consistent tests
        }

        [Test]
        [TestCase("thug", 0.7f, 1.0f, 0.6f, 1.0f)] // archetype, minAggression, maxAggression, minCourage, maxCourage
        [TestCase("merchant", 0.0f, 0.3f, 0.2f, 0.6f)]
        [TestCase("noble", 0.3f, 0.7f, 0.5f, 0.8f)]
        [TestCase("warrior", 0.6f, 0.9f, 0.8f, 1.0f)]
        [TestCase("scholar", 0.1f, 0.4f, 0.3f, 0.7f)]
        public void ValidatePersonalityGeneration(string archetype, float minAgg, float maxAgg, float minCour, float maxCour)
        {
            // Generate 100 personalities and verify they fall within expected ranges
            var personalities = new List<PersonalityProfile>();
            for (int i = 0; i < 100; i++)
            {
                personalities.Add(PersonalityProfile.GenerateRandom(archetype));
            }
            
            var avgAggression = personalities.Average(p => p.Aggression);
            var avgCourage = personalities.Average(p => p.Courage);
            
            personalities.Should().OnlyContain(p => p.Aggression >= minAgg && p.Aggression <= maxAgg,
                $"Aggression out of range for {archetype}");
            personalities.Should().OnlyContain(p => p.Courage >= minCour && p.Courage <= maxCour,
                $"Courage out of range for {archetype}");
        }

        [Test]
        public void ValidatePersonalityInfluencesDecisions()
        {
            var scenarios = new[]
            {
                new { 
                    Personality = new PersonalityProfile { Greed = 0.9f, Courage = 0.3f },
                    Situation = "low_health_treasure_room",
                    ExpectedAction = ActionType.Flee
                },
                new { 
                    Personality = new PersonalityProfile { Greed = 0.9f, Courage = 0.9f },
                    Situation = "low_health_treasure_room",
                    ExpectedAction = ActionType.ExploreDungeon
                },
                new { 
                    Personality = new PersonalityProfile { Vengefulness = 0.9f, Courage = 0.8f },
                    Situation = "encounter_killer",
                    ExpectedAction = ActionType.Attack
                }
            };
            
            foreach (var scenario in scenarios)
            {
                var npc = CreateNPCWithPersonality(scenario.Personality);
                var action = SimulateScenario(npc, scenario.Situation);
                
                // Allow for some flexibility in action types
                var validActions = GetValidActionsForScenario(scenario.Situation, scenario.ExpectedAction);
                validActions.Should().Contain(action.Type,
                    $"Personality didn't produce expected behavior in {scenario.Situation}");
            }
        }

        private List<ActionType> GetValidActionsForScenario(string situation, ActionType primary)
        {
            return situation switch
            {
                "low_health_treasure_room" when primary == ActionType.Flee => 
                    new List<ActionType> { ActionType.Flee, ActionType.SeekHealing, ActionType.DoNothing },
                "low_health_treasure_room" when primary == ActionType.ExploreDungeon => 
                    new List<ActionType> { ActionType.ExploreDungeon, ActionType.LootThenFlee, ActionType.SeekWealth },
                "encounter_killer" => 
                    new List<ActionType> { ActionType.Attack, ActionType.SeekCombat, ActionType.SeekRevenge },
                _ => new List<ActionType> { primary }
            };
        }

        [Test]
        public void ValidateBehavioralCorrelations()
        {
            var correlations = testData.PersonalityBehaviorCorrelations;
            
            foreach (var correlation in correlations)
            {
                ValidateHighTraitBehaviors(correlation);
                ValidateLowTraitBehaviors(correlation);
            }
        }

        private void ValidateHighTraitBehaviors(dynamic correlation)
        {
            string traitName = correlation.trait;
            var highBehaviors = ((IEnumerable<dynamic>)correlation.high_value_behaviors).Cast<string>().ToList();
            
            // Create NPC with high trait value
            var personality = new PersonalityProfile();
            SetTraitValue(personality, traitName, 0.9f);
            var npc = CreateNPCWithPersonality(personality);
            
            // Simulate various scenarios and track behaviors
            var observedBehaviors = SimulateNPCBehaviorsForWeek(npc);
            
            // Should exhibit high-trait behaviors more frequently
            foreach (var expectedBehavior in highBehaviors)
            {
                var behaviorCount = CountBehavior(observedBehaviors, expectedBehavior);
                behaviorCount.Should().BeGreaterThan(0, 
                    $"NPC with high {traitName} should exhibit '{expectedBehavior}' behavior");
            }
        }

        private void ValidateLowTraitBehaviors(dynamic correlation)
        {
            string traitName = correlation.trait;
            var lowBehaviors = ((IEnumerable<dynamic>)correlation.low_value_behaviors).Cast<string>().ToList();
            
            // Create NPC with low trait value
            var personality = new PersonalityProfile();
            SetTraitValue(personality, traitName, 0.1f);
            var npc = CreateNPCWithPersonality(personality);
            
            // Simulate various scenarios and track behaviors
            var observedBehaviors = SimulateNPCBehaviorsForWeek(npc);
            
            // Should exhibit low-trait behaviors more frequently
            foreach (var expectedBehavior in lowBehaviors)
            {
                var behaviorCount = CountBehavior(observedBehaviors, expectedBehavior);
                behaviorCount.Should().BeGreaterThan(0, 
                    $"NPC with low {traitName} should exhibit '{expectedBehavior}' behavior");
            }
        }

        [Test]
        public void ValidatePersonalityCombinations()
        {
            var combinations = testData.PersonalityCombinations;
            
            foreach (var combo in combinations)
            {
                var personality = CreatePersonalityFromDynamic(combo.traits);
                var npc = CreateNPCWithPersonality(personality);
                
                // Simulate behaviors for this personality combination
                var behaviors = SimulateNPCBehaviorsForWeek(npc);
                
                var expectedBehaviors = ((IEnumerable<dynamic>)combo.expected_behaviors).Cast<string>().ToList();
                
                foreach (var expectedBehavior in expectedBehaviors)
                {
                    var behaviorCount = CountBehavior(behaviors, expectedBehavior);
                    behaviorCount.Should().BeGreaterThan(0,
                        $"Personality combo '{combo.name}' should exhibit '{expectedBehavior}' behavior");
                }
            }
        }

        [Test]
        public void ValidatePersonalityStability()
        {
            var personality = PersonalityProfile.GenerateRandom("warrior");
            var npc = CreateNPCWithPersonality(personality);
            
            var initialAggression = personality.Aggression;
            var initialCourage = personality.Courage;
            
            // Simulate activity for several days
            for (int i = 0; i < 100; i++)
            {
                var worldState = new WorldState { CurrentHour = i % 24 };
                npc.Brain.ProcessHourlyUpdate(worldState);
            }
            
            // Personality should remain stable
            personality.Aggression.Should().Be(initialAggression, "Aggression should remain stable");
            personality.Courage.Should().Be(initialCourage, "Courage should remain stable");
        }

        [Test]
        public void ValidatePersonalityDistribution()
        {
            var population = new List<PersonalityProfile>();
            var archetypes = new[] { "thug", "merchant", "noble", "warrior", "scholar" };
            
            for (int i = 0; i < 500; i++)
            {
                var archetype = archetypes[i % archetypes.Length];
                population.Add(PersonalityProfile.GenerateRandom(archetype));
            }
            
            // Check for good trait distribution
            var aggressionValues = population.Select(p => p.Aggression).ToList();
            var courageValues = population.Select(p => p.Courage).ToList();
            
            var aggressionStdDev = CalculateStandardDeviation(aggressionValues);
            var courageStdDev = CalculateStandardDeviation(courageValues);
            
            aggressionStdDev.Should().BeGreaterThan(0.15f, "Aggression should have good variation");
            courageStdDev.Should().BeGreaterThan(0.15f, "Courage should have good variation");
        }

        [Test]
        public void ValidatePersonalityInfluencesGoalPriorities()
        {
            // Greedy NPCs should prioritize wealth goals
            var greedyNPC = CreateNPCWithPersonality(new PersonalityProfile { Greed = 0.9f });
            greedyNPC.Gold = 100; // Low gold to trigger wealth goal
            
            var worldState = new WorldState();
            greedyNPC.Brain.ProcessHourlyUpdate(worldState);
            
            var activeGoals = greedyNPC.Brain.Goals.GetActiveGoals();
            activeGoals.Should().Contain(g => g.Type == GoalType.AccumulateWealth,
                "Greedy NPCs should have wealth accumulation goals");
            
            // Vengeful NPCs should prioritize revenge goals when they have enemies
            var vengefulNPC = CreateNPCWithPersonality(new PersonalityProfile { Vengefulness = 0.9f });
            vengefulNPC.Brain.Memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.WasAttacked,
                InvolvedCharacter = "enemy123",
                Timestamp = DateTime.Now.AddDays(-1)
            });
            
            vengefulNPC.Brain.ProcessHourlyUpdate(worldState);
            
            var revengeGoals = vengefulNPC.Brain.Goals.GetActiveGoals()
                .Where(g => g.Type == GoalType.GetRevenge);
            revengeGoals.Should().NotBeEmpty("Vengeful NPCs should pursue revenge goals");
        }

        // Helper methods

        private PersonalityProfile CreatePersonalityFromDynamic(dynamic traits)
        {
            var personality = new PersonalityProfile();
            
            if (traits.aggression != null) personality.Aggression = (float)traits.aggression;
            if (traits.courage != null) personality.Courage = (float)traits.courage;
            if (traits.greed != null) personality.Greed = (float)traits.greed;
            if (traits.loyalty != null) personality.Loyalty = (float)traits.loyalty;
            if (traits.vengefulness != null) personality.Vengefulness = (float)traits.vengefulness;
            if (traits.impulsiveness != null) personality.Impulsiveness = (float)traits.impulsiveness;
            if (traits.sociability != null) personality.Sociability = (float)traits.sociability;
            if (traits.ambition != null) personality.Ambition = (float)traits.ambition;
            
            return personality;
        }

        private NPC CreateNPCWithPersonality(PersonalityProfile personality)
        {
            var npc = new NPC
            {
                Name = "Test NPC",
                Level = 5,
                MaxHP = 100,
                CurrentHP = 100,
                Strength = 40,
                Defense = 25,
                Gold = 500,
                Personality = personality,
                Location = "town_square"
            };
            
            npc.Brain = new NPCBrain(npc, personality);
            return npc;
        }

        private NPCAction SimulateScenario(NPC npc, string scenario)
        {
            var worldState = scenario switch
            {
                "low_health_treasure_room" => new WorldState
                {
                    CurrentLocation = "dungeon",
                    CurrentHour = 14,
                    TreasurePresent = true
                },
                "encounter_killer" => new WorldState
                {
                    CurrentLocation = "town_square",
                    CurrentHour = 14,
                    NearbyCharacters = new[] { CreateEnemyCharacter(npc) }
                },
                _ => new WorldState { CurrentHour = 14 }
            };
            
            // Set health for low health scenarios
            if (scenario.Contains("low_health"))
            {
                npc.CurrentHP = npc.MaxHP * 0.25f;
            }
            
            return npc.Brain.DecideNextAction(worldState);
        }

        private Character CreateEnemyCharacter(NPC npc)
        {
            var enemy = new Character
            {
                Name = "Enemy",
                Level = npc.Level + 2,
                CurrentHP = 100,
                MaxHP = 100
            };
            
            // Record that this enemy attacked the NPC
            npc.Brain.Memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.WasAttacked,
                InvolvedCharacter = enemy.Id,
                Timestamp = DateTime.Now.AddDays(-1)
            });
            
            return enemy;
        }

        private List<string> SimulateNPCBehaviorsForWeek(NPC npc)
        {
            var behaviors = new List<string>();
            
            for (int day = 0; day < 7; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    var worldState = new WorldState 
                    { 
                        CurrentHour = hour,
                        NearbyCharacters = GenerateRandomNearbyCharacters()
                    };
                    
                    var action = npc.Brain.DecideNextAction(worldState);
                    behaviors.Add(MapActionToBehavior(action.Type, npc.Personality));
                    
                    // Process the hour
                    npc.Brain.ProcessHourlyUpdate(worldState);
                }
            }
            
            return behaviors;
        }

        private Character[] GenerateRandomNearbyCharacters()
        {
            var count = random.Next(0, 4); // 0-3 nearby characters
            var characters = new Character[count];
            
            for (int i = 0; i < count; i++)
            {
                characters[i] = new Character
                {
                    Name = $"Random NPC {i}",
                    Level = random.Next(3, 8),
                    CurrentHP = 80,
                    MaxHP = 100
                };
            }
            
            return characters;
        }

        private string MapActionToBehavior(ActionType action, PersonalityProfile personality)
        {
            return action switch
            {
                ActionType.Attack => "initiates_combat_more_often",
                ActionType.Flee => "flees_from_combat",
                ActionType.SeekCombat => "initiates_combat_more_often",
                ActionType.ExploreDungeon => "spends_more_time_in_dungeons",
                ActionType.Socialize => "spends_time_in_taverns",
                ActionType.FormGang => "forms_gangs_quickly",
                ActionType.SeekRevenge => "actively_hunts_enemies",
                ActionType.AccumulateWealth => "prioritizes_wealth_accumulation",
                ActionType.CallForHelp => "seeks_peaceful_solutions",
                _ => "other_behavior"
            };
        }

        private int CountBehavior(List<string> behaviors, string targetBehavior)
        {
            return behaviors.Count(b => b == targetBehavior);
        }

        private void SetTraitValue(PersonalityProfile personality, string traitName, float value)
        {
            switch (traitName.ToLower())
            {
                case "aggression": personality.Aggression = value; break;
                case "courage": personality.Courage = value; break;
                case "greed": personality.Greed = value; break;
                case "loyalty": personality.Loyalty = value; break;
                case "vengefulness": personality.Vengefulness = value; break;
                case "impulsiveness": personality.Impulsiveness = value; break;
                case "sociability": personality.Sociability = value; break;
                case "ambition": personality.Ambition = value; break;
            }
        }

        private float CalculateStandardDeviation(List<float> values)
        {
            var mean = values.Average();
            var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
            return (float)Math.Sqrt(variance);
        }
    }

    // Data classes for test data deserialization
    public class PersonalityTestData
    {
        public Dictionary<string, Dictionary<string, TraitRange>> ArchetypePersonalityRanges { get; set; }
        public List<PersonalityBehaviorCorrelation> PersonalityBehaviorCorrelations { get; set; }
        public List<PersonalityCombination> PersonalityCombinations { get; set; }
    }

    public class TraitRange
    {
        public float Min { get; set; }
        public float Max { get; set; }
    }

    public class PersonalityBehaviorCorrelation
    {
        public string Trait { get; set; }
        public List<string> HighValueBehaviors { get; set; }
        public List<string> LowValueBehaviors { get; set; }
    }

    public class PersonalityCombination
    {
        public string Name { get; set; }
        public Dictionary<string, float> Traits { get; set; }
        public List<string> ExpectedBehaviors { get; set; }
    }

    public class BehaviorScenarios
    {
        public List<PersonalityDecisionScenario> PersonalityDecisionScenarios { get; set; }
    }

    public class PersonalityDecisionScenario
    {
        public string ScenarioName { get; set; }
        public string Description { get; set; }
        public dynamic WorldState { get; set; }
        public List<PersonalityTest> PersonalityTests { get; set; }
    }

    public class PersonalityTest
    {
        public dynamic Personality { get; set; }
        public string ExpectedAction { get; set; }
        public string Reasoning { get; set; }
    }
} 
