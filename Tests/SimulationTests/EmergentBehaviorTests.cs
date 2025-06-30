using NUnit.Framework;
using UsurperReborn.Scripts.Core;
using UsurperReborn.Scripts.AI;
using FluentAssertions;
using System.Diagnostics;

namespace UsurperReborn.Tests.SimulationTests
{
    [TestFixture]
    public class EmergentBehaviorTests
    {
        private WorldSimulator simulator;
        private List<NPC> testNPCs;
        private Random random;
        
        [SetUp]
        public void Setup()
        {
            simulator = WorldSimulator.Instance;
            random = new Random(12345); // Fixed seed for consistent tests
            testNPCs = new List<NPC>();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Reset simulator state
            simulator.Reset();
        }
        
        [Test]
        public void ValidateGangFormation()
        {
            // Create NPCs with compatible personalities for gang formation
            var leader = CreateNPC("Gang Leader", new PersonalityProfile 
            { 
                Ambition = 0.9f, 
                Loyalty = 0.7f,
                Sociability = 0.8f,
                Aggression = 0.6f,
                Courage = 0.8f,
                Greed = 0.5f,
                Vengefulness = 0.5f,
                Impulsiveness = 0.4f
            });
            
            var followers = new List<NPC>();
            for (int i = 0; i < 5; i++)
            {
                var follower = CreateNPC($"Follower{i}", new PersonalityProfile
                {
                    Loyalty = 0.8f,
                    Ambition = 0.3f,
                    Sociability = 0.7f,
                    Aggression = 0.5f,
                    Courage = 0.6f,
                    Greed = 0.4f,
                    Vengefulness = 0.4f,
                    Impulsiveness = 0.5f
                });
                
                // Place them in the same location to increase interaction
                follower.Location = "tavern";
                
                // Create some positive initial relationships
                follower.Brain.Memory.RecordEvent(new MemoryEvent
                {
                    Type = MemoryEventType.WasHelped,
                    InvolvedCharacter = leader.Id,
                    Timestamp = DateTime.Now.AddDays(-1)
                });
                
                followers.Add(follower);
            }
            
            leader.Location = "tavern";
            
            var allNPCs = new List<NPC> { leader };
            allNPCs.AddRange(followers);
            
            simulator.Initialize(allNPCs);
            
            // Simulate for 7 game days to allow gang formation
            for (int day = 0; day < 7; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    simulator.SimulateHour();
                }
            }
            
            // Check if gang formed
            leader.IsGangLeader.Should().BeTrue("Ambitious, loyal, social NPC should become gang leader");
            
            var joinedFollowers = followers.Count(f => f.Gang?.Leader == leader);
            joinedFollowers.Should().BeGreaterThan(2, "At least 3 followers should have joined the gang");
            
            // Gang should have a name
            if (leader.Gang != null)
            {
                leader.Gang.Name.Should().NotBeNullOrEmpty("Gang should have a name");
                leader.Gang.Members.Should().Contain(leader, "Leader should be in their own gang");
            }
        }
        
        [Test]
        public void ValidateRevengeChains()
        {
            var npc1 = CreateVengefulNPC("Vengeful1");
            var npc2 = CreateVengefulNPC("Vengeful2");
            var npc3 = CreateNeutralNPC("Bystander");
            
            var npcs = new List<NPC> { npc1, npc2, npc3 };
            simulator.Initialize(npcs);
            
            // Manually trigger initial attack (NPC1 attacks NPC2)
            npc2.Brain.Memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.WasAttacked,
                InvolvedCharacter = npc1.Id,
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object> { ["damage"] = 50 }
            });
            
            npc2.Brain.Memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.LostTo,
                InvolvedCharacter = npc1.Id,
                Timestamp = DateTime.Now
            });
            
            // NPC2 should develop revenge goal
            npc2.Brain.ProcessHourlyUpdate(new WorldState());
            
            var revengeGoal = npc2.Brain.Goals.GetActiveGoals()
                .FirstOrDefault(g => g.Type == GoalType.GetRevenge && g.Target == npc1.Id);
            revengeGoal.Should().NotBeNull("Vengeful NPC should develop revenge goal after being attacked");
            
            // Simulate until revenge attempt happens
            var revengeAttempted = false;
            for (int i = 0; i < 168 && !revengeAttempted; i++) // 1 week max
            {
                simulator.SimulateHour();
                
                // Check if NPC2 has attempted revenge
                var memories = npc1.Brain.Memory.GetMemoriesAbout(npc2.Id);
                revengeAttempted = memories.Any(m => m.Event.Type == MemoryEventType.WasAttacked);
            }
            
            revengeAttempted.Should().BeTrue("Vengeful NPC should eventually attempt revenge");
        }
        
        [Test]
        public void ValidateEconomicCompetition()
        {
            // Create merchant NPCs with varying intelligence/greed
            var merchants = new List<NPC>();
            for (int i = 0; i < 5; i++)
            {
                var merchant = CreateNPC($"Merchant{i}", new PersonalityProfile
                {
                    Greed = 0.8f + (i * 0.05f), // Increasing greed
                    Aggression = 0.2f,
                    Ambition = 0.7f + (i * 0.05f), // Increasing ambition
                    Sociability = 0.6f,
                    Courage = 0.5f,
                    Loyalty = 0.5f,
                    Vengefulness = 0.3f,
                    Impulsiveness = 0.3f
                });
                
                merchant.Gold = 1000; // Start with equal wealth
                merchant.Location = "town_square";
                merchants.Add(merchant);
            }
            
            simulator.Initialize(merchants);
            
            // Track wealth over time
            var wealthTracking = new Dictionary<string, List<int>>();
            
            for (int day = 0; day < 14; day++) // 2 weeks simulation
            {
                foreach (var merchant in merchants)
                {
                    if (!wealthTracking.ContainsKey(merchant.Id))
                        wealthTracking[merchant.Id] = new List<int>();
                        
                    wealthTracking[merchant.Id].Add(merchant.Gold);
                }
                
                // Simulate full day
                for (int hour = 0; hour < 24; hour++)
                {
                    simulator.SimulateHour();
                }
            }
            
            // Verify wealth inequality emerged
            var finalWealth = merchants.Select(m => m.Gold).ToList();
            var wealthStdDev = CalculateStandardDeviation(finalWealth.Select(w => (float)w).ToList());
            
            wealthStdDev.Should().BeGreaterThan(500f, "Economic competition should create wealth inequality");
            
            // Most ambitious/greedy merchants should generally be wealthier
            var topMerchant = merchants.OrderByDescending(m => m.Personality.Greed + m.Personality.Ambition).First();
            var avgWealth = merchants.Average(m => m.Gold);
            
            topMerchant.Gold.Should().BeGreaterThan(avgWealth * 1.1, "Most greedy/ambitious merchants should accumulate more wealth");
        }
        
        [Test]
        public void ValidateSocialNetworkFormation()
        {
            // Create NPCs with varying sociability
            var socialNPCs = new List<NPC>();
            for (int i = 0; i < 10; i++)
            {
                var npc = CreateNPC($"Social{i}", new PersonalityProfile
                {
                    Sociability = 0.7f + (i * 0.03f), // Increasing sociability
                    Loyalty = 0.6f,
                    Aggression = 0.3f,
                    Greed = 0.4f,
                    Courage = 0.5f,
                    Ambition = 0.5f,
                    Vengefulness = 0.3f,
                    Impulsiveness = 0.4f
                });
                
                npc.Location = "tavern"; // Social location
                socialNPCs.Add(npc);
            }
            
            simulator.Initialize(socialNPCs);
            
            // Simulate social interactions
            for (int day = 0; day < 10; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    simulator.SimulateHour();
                }
            }
            
            // Verify social connections formed
            var totalRelationships = 0;
            var positiveRelationships = 0;
            
            foreach (var npc in socialNPCs)
            {
                var memories = npc.Brain.Memory.GetAllMemories();
                var socialMemories = memories.Where(m => 
                    m.Event.Type == MemoryEventType.SharedDrink ||
                    m.Event.Type == MemoryEventType.WasHelped ||
                    m.Event.Type == MemoryEventType.SocializedWith);
                
                totalRelationships += socialMemories.Count();
                
                foreach (var memory in socialMemories)
                {
                    if (!string.IsNullOrEmpty(memory.Event.InvolvedCharacter))
                    {
                        var relationship = npc.Brain.Memory.GetRelationship(memory.Event.InvolvedCharacter);
                        if (relationship.Friendship > 20)
                            positiveRelationships++;
                    }
                }
            }
            
            totalRelationships.Should().BeGreaterThan(20, "Social NPCs should form many social connections");
            positiveRelationships.Should().BeGreaterThan(10, "Many social connections should be positive");
        }
        
        [Test]
        public void ValidateConflictEscalation()
        {
            // Create aggressive NPCs prone to conflict
            var aggressiveNPCs = new List<NPC>();
            for (int i = 0; i < 6; i++)
            {
                var npc = CreateNPC($"Aggressive{i}", new PersonalityProfile
                {
                    Aggression = 0.8f + (i * 0.03f),
                    Vengefulness = 0.7f + (i * 0.04f),
                    Courage = 0.7f,
                    Impulsiveness = 0.8f,
                    Loyalty = 0.3f, // Low loyalty = more likely to betray
                    Sociability = 0.4f,
                    Greed = 0.6f,
                    Ambition = 0.7f
                });
                
                npc.Location = "town_square";
                aggressiveNPCs.Add(npc);
            }
            
            simulator.Initialize(aggressiveNPCs);
            
            // Simulate for extended period to allow conflicts to develop
            var combatEvents = new List<WorldEvent>();
            
            for (int day = 0; day < 21; day++) // 3 weeks
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    simulator.SimulateHour();
                    var dailyEvents = simulator.GetRecentEvents(1);
                    combatEvents.AddRange(dailyEvents.Where(e => e.Type == EventType.Combat));
                }
            }
            
            // Should have multiple combat events
            combatEvents.Should().NotBeEmpty("Aggressive NPCs should engage in combat");
            combatEvents.Count.Should().BeGreaterThan(5, "Multiple conflicts should occur over time");
            
            // Should have revenge-motivated conflicts
            var revengeEvents = combatEvents.Where(e => 
                e.Details.ContainsKey("motivation") && 
                e.Details["motivation"].ToString() == "revenge");
            revengeEvents.Should().NotBeEmpty("Some conflicts should be revenge-motivated");
        }
        
        [Test]
        public void ValidateLeadershipEmergence()
        {
            // Create population with varying ambition and leadership qualities
            var population = new List<NPC>();
            
            // Add potential leaders
            for (int i = 0; i < 3; i++)
            {
                var leader = CreateNPC($"Leader{i}", new PersonalityProfile
                {
                    Ambition = 0.9f,
                    Loyalty = 0.6f,
                    Sociability = 0.8f,
                    Courage = 0.8f,
                    Aggression = 0.5f + (i * 0.1f), // Varying aggression
                    Greed = 0.6f,
                    Vengefulness = 0.5f,
                    Impulsiveness = 0.3f
                });
                
                leader.Level = 8 + i; // Higher levels
                leader.Gold = 3000 + (i * 1000); // More resources
                population.Add(leader);
            }
            
            // Add followers
            for (int i = 0; i < 12; i++)
            {
                var follower = CreateNPC($"Follower{i}", new PersonalityProfile
                {
                    Ambition = 0.3f + (i * 0.02f),
                    Loyalty = 0.7f + (i * 0.02f),
                    Sociability = 0.6f,
                    Courage = 0.5f,
                    Aggression = 0.4f,
                    Greed = 0.5f,
                    Vengefulness = 0.4f,
                    Impulsiveness = 0.5f
                });
                
                follower.Level = 3 + (i / 3); // Lower levels
                population.Add(follower);
            }
            
            simulator.Initialize(population);
            
            // Simulate for a month to allow leadership to emerge
            for (int day = 0; day < 30; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    simulator.SimulateHour();
                }
            }
            
            // Check for leadership emergence
            var leaders = population.Where(npc => npc.IsGangLeader).ToList();
            leaders.Should().NotBeEmpty("Leadership should emerge from ambitious NPCs");
            
            var totalGangMembers = population.Count(npc => npc.Gang != null);
            totalGangMembers.Should().BeGreaterThan(5, "Multiple NPCs should join gangs");
            
            // Most successful leader should have highest combination of ambition and resources
            if (leaders.Any())
            {
                var topLeader = leaders.OrderByDescending(l => l.Gang?.Members.Count ?? 0).First();
                topLeader.Personality.Ambition.Should().BeGreaterThan(0.8f, "Top leader should be highly ambitious");
            }
        }
        
        [Test]
        public void ValidateBetrayalAndConsequences()
        {
            // Create scenario prone to betrayal
            var loyalist = CreateNPC("Loyal NPC", new PersonalityProfile
            {
                Loyalty = 0.9f,
                Trust = 0.8f,
                Greed = 0.3f,
                Ambition = 0.4f,
                Aggression = 0.3f,
                Courage = 0.6f,
                Sociability = 0.7f,
                Vengefulness = 0.6f,
                Impulsiveness = 0.2f
            });
            
            var opportunist = CreateNPC("Opportunist NPC", new PersonalityProfile
            {
                Loyalty = 0.2f, // Very low loyalty
                Greed = 0.9f,
                Ambition = 0.8f,
                Aggression = 0.5f,
                Courage = 0.7f,
                Sociability = 0.5f,
                Vengefulness = 0.4f,
                Impulsiveness = 0.7f
            });
            
            // Build initial friendship
            loyalist.Brain.Memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.WasHelped,
                InvolvedCharacter = opportunist.Id,
                Timestamp = DateTime.Now.AddDays(-5)
            });
            
            opportunist.Brain.Memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.SharedDrink,
                InvolvedCharacter = loyalist.Id,
                Timestamp = DateTime.Now.AddDays(-3)
            });
            
            // Give opportunist a tempting opportunity (make loyalist wealthy)
            loyalist.Gold = 5000;
            opportunist.Gold = 100;
            
            var npcs = new List<NPC> { loyalist, opportunist };
            simulator.Initialize(npcs);
            
            // Simulate until betrayal occurs or time limit reached
            var betrayalOccurred = false;
            
            for (int day = 0; day < 14 && !betrayalOccurred; day++) // 2 weeks max
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    simulator.SimulateHour();
                    
                    // Check for betrayal
                    var loyalistMemories = loyalist.Brain.Memory.GetMemoriesAbout(opportunist.Id);
                    betrayalOccurred = loyalistMemories.Any(m => m.Event.Type == MemoryEventType.WasBetrayed);
                }
            }
            
            if (betrayalOccurred)
            {
                // Verify consequences of betrayal
                var relationship = loyalist.Brain.Memory.GetRelationship(opportunist.Id);
                relationship.Trust.Should().Be(-100, "Betrayal should destroy trust");
                relationship.Hostility.Should().BeGreaterThan(50, "Betrayal should create hostility");
                
                // Loyalist should seek revenge if vengeful enough
                if (loyalist.Personality.Vengefulness > 0.5f)
                {
                    var revengeGoal = loyalist.Brain.Goals.GetActiveGoals()
                        .FirstOrDefault(g => g.Type == GoalType.GetRevenge && g.Target == opportunist.Id);
                    revengeGoal.Should().NotBeNull("Vengeful NPC should seek revenge after betrayal");
                }
            }
        }
        
        [Test]
        public void ValidatePopulationStability()
        {
            var population = GenerateBalancedPopulation(20);
            simulator.Initialize(population);
            
            var initialPopulation = simulator.GetAliveNPCs().Count;
            
            // Simulate for 30 days
            for (int day = 0; day < 30; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    simulator.SimulateHour();
                }
                
                // Handle daily respawns (simulating new arrivals)
                if (day % 7 == 0) // Weekly respawns
                {
                    simulator.HandleDailyRespawns();
                }
            }
            
            var finalPopulation = simulator.GetAliveNPCs().Count;
            
            // Population should remain relatively stable
            finalPopulation.Should().BeInRange((int)(initialPopulation * 0.7), (int)(initialPopulation * 1.3),
                "Population should remain relatively stable over time");
        }
        
        // Helper methods
        
        private List<NPC> GenerateBalancedPopulation(int count)
        {
            var population = new List<NPC>();
            var archetypes = new[] { "thug", "merchant", "noble", "warrior", "scholar" };
            
            for (int i = 0; i < count; i++)
            {
                var archetype = archetypes[i % archetypes.Length];
                var personality = PersonalityProfile.GenerateRandom(archetype);
                
                var npc = CreateNPC($"NPC{i}", personality);
                npc.Level = random.Next(3, 8);
                npc.Gold = random.Next(500, 2000);
                npc.Location = GetRandomLocation();
                
                population.Add(npc);
            }
            
            return population;
        }
        
        private string GetRandomLocation()
        {
            var locations = new[] { "town_square", "tavern", "inn", "market", "dungeon_entrance" };
            return locations[random.Next(locations.Length)];
        }
        
        private NPC CreateNPC(string name, PersonalityProfile personality)
        {
            var npc = new NPC
            {
                Name = name,
                Level = 5,
                MaxHP = 100,
                CurrentHP = 100,
                Strength = 40 + random.Next(-10, 11),
                Defense = 25 + random.Next(-5, 6),
                Gold = 1000,
                Personality = personality,
                Location = "town_square"
            };
            
            npc.Brain = new NPCBrain(npc, personality);
            testNPCs.Add(npc);
            return npc;
        }
        
        private NPC CreateVengefulNPC(string name)
        {
            return CreateNPC(name, new PersonalityProfile
            {
                Vengefulness = 0.9f,
                Courage = 0.7f,
                Aggression = 0.8f,
                Loyalty = 0.5f,
                Greed = 0.4f,
                Ambition = 0.6f,
                Sociability = 0.4f,
                Impulsiveness = 0.6f
            });
        }
        
        private NPC CreateNeutralNPC(string name)
        {
            return CreateNPC(name, new PersonalityProfile
            {
                Vengefulness = 0.3f,
                Courage = 0.5f,
                Aggression = 0.3f,
                Loyalty = 0.6f,
                Greed = 0.5f,
                Ambition = 0.4f,
                Sociability = 0.6f,
                Impulsiveness = 0.4f
            });
        }
        
        private float CalculateStandardDeviation(List<float> values)
        {
            var mean = values.Average();
            var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
            return (float)Math.Sqrt(variance);
        }
    }
} 