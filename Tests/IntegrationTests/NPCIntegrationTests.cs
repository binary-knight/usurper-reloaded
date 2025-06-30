using NUnit.Framework;
using UsurperReborn.Scripts.Core;
using UsurperReborn.Scripts.AI;
using UsurperReborn.Scripts.Locations;
using FluentAssertions;
using System.IO;

namespace UsurperReborn.Tests.IntegrationTests
{
    [TestFixture]
    public class NPCIntegrationTests
    {
        private GameEngine engine;
        private TestTerminal terminal;
        private WorldSimulator simulator;
        private List<NPC> testNPCs;

        [SetUp]
        public void Setup()
        {
            terminal = new TestTerminal();
            engine = new GameEngine(terminal);
            simulator = WorldSimulator.Instance;
            testNPCs = new List<NPC>();
        }

        [TearDown]
        public void TearDown()
        {
            simulator.Reset();
            testNPCs.Clear();
        }

        [Test]
        public async Task ValidatePlayerNPCInteraction()
        {
            // Create test player
            var player = CreateTestPlayer();
            engine.SetPlayer(player);
            
            // Create test NPC
            var npc = CreateFriendlyNPC("Seth Able");
            testNPCs.Add(npc);
            
            // Move to tavern location
            var tavern = new Tavern();
            tavern.NPCs.Add(npc);
            engine.SetCurrentLocation(tavern);
            
            // Simulate positive interaction
            npc.Brain.Memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.SharedDrink,
                InvolvedCharacter = player.Id,
                Timestamp = DateTime.Now
            });
            
            // Process the interaction
            npc.Brain.ProcessHourlyUpdate(new WorldState
            {
                NearbyCharacters = new[] { player },
                CurrentLocation = "tavern",
                CurrentHour = 20
            });
            
            // Verify interaction was recorded
            var memories = npc.Brain.Memory.GetMemoriesAbout(player.Id);
            memories.Should().NotBeEmpty("NPC should remember interacting with player");
            
            var relationship = npc.Brain.Memory.GetRelationship(player.Id);
            relationship.Friendship.Should().BeGreaterThan(0, "Positive interaction should increase friendship");
        }

        [Test]
        public async Task ValidateHostileNPCBehavior()
        {
            var player = CreateTestPlayer();
            engine.SetPlayer(player);
            
            var hostileNPC = CreateHostileNPC("Angry Thug");
            testNPCs.Add(hostileNPC);
            
            // Record that player attacked the NPC
            hostileNPC.Brain.Memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.WasAttacked,
                InvolvedCharacter = player.Id,
                Timestamp = DateTime.Now.AddHours(-2),
                Details = new Dictionary<string, object> { ["damage"] = 40 }
            });
            
            // Process hourly update
            hostileNPC.Brain.ProcessHourlyUpdate(new WorldState
            {
                NearbyCharacters = new[] { player },
                CurrentLocation = "town_square",
                CurrentHour = 14
            });
            
            // NPC should remember being attacked and be hostile
            hostileNPC.Brain.Memory.RemembersBeingAttackedBy(player.Id).Should().BeTrue();
            
            var relationship = hostileNPC.Brain.Memory.GetRelationship(player.Id);
            relationship.Hostility.Should().BeGreaterThan(30, "Being attacked should create hostility");
            
            // NPC should have revenge goal
            var revengeGoal = hostileNPC.Brain.Goals.GetActiveGoals()
                .FirstOrDefault(g => g.Type == GoalType.GetRevenge && g.Target == player.Id);
            revengeGoal.Should().NotBeNull("NPC should seek revenge after being attacked");
        }

        [Test]
        public async Task ValidateNPCPersistence()
        {
            var player = CreateTestPlayer();
            engine.SetPlayer(player);
            
            // Create memorable interaction
            var npc = CreateTestNPC("Guard Captain", new PersonalityProfile { Loyalty = 0.8f });
            testNPCs.Add(npc);
            
            // Create positive relationship
            npc.Brain.Memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.WasHelped,
                InvolvedCharacter = player.Id,
                Timestamp = DateTime.Now.AddDays(-2),
                Details = new Dictionary<string, object> { ["significance"] = "high" }
            });
            
            var relationshipBefore = npc.Brain.Memory.GetRelationship(player.Id);
            var memoriesBefore = npc.Brain.Memory.GetMemoriesAbout(player.Id).Count;
            
            // Simulate save/load cycle
            var saveData = SerializeNPCState(npc);
            var restoredNPC = DeserializeNPCState(saveData);
            
            // Verify NPC remembers after restoration
            var relationshipAfter = restoredNPC.Brain.Memory.GetRelationship(player.Id);
            var memoriesAfter = restoredNPC.Brain.Memory.GetMemoriesAbout(player.Id).Count;
            
            relationshipAfter.Friendship.Should().Be(relationshipBefore.Friendship, "Friendship should persist");
            relationshipAfter.Trust.Should().Be(relationshipBefore.Trust, "Trust should persist");
            memoriesAfter.Should().Be(memoriesBefore, "Memory count should persist");
        }

        [Test]
        public void ValidateWorldSimulationIntegration()
        {
            // Create diverse population
            var population = CreateDiversePopulation(10);
            simulator.Initialize(population);
            
            // Simulate a few days of activity
            var events = new List<WorldEvent>();
            
            for (int day = 0; day < 3; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    simulator.SimulateHour();
                    var hourlyEvents = simulator.GetRecentEvents(1);
                    events.AddRange(hourlyEvents);
                }
            }
            
            // Verify events occurred
            events.Should().NotBeEmpty("Simulation should generate events");
            
            // Verify NPCs developed relationships
            var npcWithRelationships = population.Count(npc =>
                npc.Brain.Memory.GetAllMemories().Any(m => !string.IsNullOrEmpty(m.Event.InvolvedCharacter)));
            
            npcWithRelationships.Should().BeGreaterThan(2, "Multiple NPCs should develop relationships");
        }

        [Test]
        public void ValidateLocationSpecificBehavior()
        {
            // Create NPCs with different personalities
            var socialNPC = CreateTestNPC("Social Butterfly", new PersonalityProfile
            {
                Sociability = 0.9f,
                Greed = 0.3f,
                Aggression = 0.2f
            });
            
            var greedyNPC = CreateTestNPC("Greedy Adventurer", new PersonalityProfile
            {
                Greed = 0.9f,
                Sociability = 0.3f,
                Courage = 0.8f
            });
            
            var aggressiveNPC = CreateTestNPC("Town Bully", new PersonalityProfile
            {
                Aggression = 0.9f,
                Sociability = 0.4f,
                Impulsiveness = 0.8f
            });
            
            testNPCs.AddRange(new[] { socialNPC, greedyNPC, aggressiveNPC });
            
            // Test tavern behavior
            var tavernState = new WorldState
            {
                CurrentLocation = "tavern",
                CurrentHour = 20, // Evening
                NearbyCharacters = new[] { socialNPC, greedyNPC }
            };
            
            var socialAction = socialNPC.Brain.DecideNextAction(tavernState);
            var greedyAction = greedyNPC.Brain.DecideNextAction(tavernState);
            
            // Social NPC should prefer social activities in tavern
            socialAction.Type.Should().BeOneOf(ActionType.Socialize, ActionType.VisitTavern, ActionType.DoNothing);
            
            // Test dungeon behavior
            var dungeonState = new WorldState
            {
                CurrentLocation = "dungeon",
                CurrentHour = 14,
                TreasurePresent = true
            };
            
            greedyAction = greedyNPC.Brain.DecideNextAction(dungeonState);
            
            // Greedy NPC should prefer exploration in dungeon
            greedyAction.Type.Should().BeOneOf(ActionType.ExploreDungeon, ActionType.SeekWealth);
        }

        [Test]
        public void ValidateEmergentGangFormation()
        {
            // Create potential gang leader
            var leader = CreateTestNPC("Gang Leader", new PersonalityProfile
            {
                Ambition = 0.9f,
                Loyalty = 0.7f,
                Sociability = 0.8f,
                Aggression = 0.6f
            });
            
            // Create potential followers
            var followers = new List<NPC>();
            for (int i = 0; i < 3; i++)
            {
                var follower = CreateTestNPC($"Follower {i + 1}", new PersonalityProfile
                {
                    Loyalty = 0.8f,
                    Ambition = 0.3f,
                    Sociability = 0.7f
                });
                
                // Create positive relationship with leader
                follower.Brain.Memory.RecordEvent(new MemoryEvent
                {
                    Type = MemoryEventType.WasHelped,
                    InvolvedCharacter = leader.Id,
                    Timestamp = DateTime.Now.AddDays(-1)
                });
                
                followers.Add(follower);
            }
            
            var allNPCs = new List<NPC> { leader };
            allNPCs.AddRange(followers);
            testNPCs.AddRange(allNPCs);
            
            simulator.Initialize(allNPCs);
            
            // Simulate for several days
            for (int day = 0; day < 7; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    simulator.SimulateHour();
                }
            }
            
            // Check if gang formation occurred
            leader.IsGangLeader.Should().BeTrue("Ambitious, social NPC should form gang");
            
            var gangMembers = followers.Count(f => f.Gang?.Leader == leader);
            gangMembers.Should().BeGreaterThan(0, "At least 1 follower should join the gang");
        }

        [Test]
        public void ValidateEconomicBehaviorIntegration()
        {
            // Create NPCs with different economic focuses
            var merchant = CreateTestNPC("Wealthy Merchant", new PersonalityProfile
            {
                Greed = 0.9f,
                Ambition = 0.8f,
                Aggression = 0.2f
            });
            merchant.Gold = 5000;
            
            var pauper = CreateTestNPC("Poor Adventurer", new PersonalityProfile
            {
                Greed = 0.7f,
                Courage = 0.8f,
                Aggression = 0.6f
            });
            pauper.Gold = 50;
            
            testNPCs.AddRange(new[] { merchant, pauper });
            simulator.Initialize(testNPCs);
            
            // Simulate economic activity
            for (int day = 0; day < 14; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    simulator.SimulateHour();
                }
            }
            
            // Verify economic goals
            var merchantGoals = merchant.Brain.Goals.GetActiveGoals();
            var pauperGoals = pauper.Brain.Goals.GetActiveGoals();
            
            // Wealthy merchant might pursue power goals
            if (merchant.Gold > 10000)
            {
                merchantGoals.Should().Contain(g => g.Type == GoalType.BecomeRuler || g.Type == GoalType.GainInfluence,
                    "Wealthy NPCs should pursue power goals");
            }
            
            // Poor NPC should pursue wealth
            pauperGoals.Should().Contain(g => g.Type == GoalType.AccumulateWealth,
                "Poor NPCs should pursue wealth goals");
        }

        [Test]
        public void ValidateRevengeChainIntegration()
        {
            // Create vengeful NPCs
            var attacker = CreateTestNPC("Initial Attacker", new PersonalityProfile
            {
                Aggression = 0.8f,
                Vengefulness = 0.6f,
                Impulsiveness = 0.7f
            });
            
            var victim = CreateTestNPC("Vengeful Victim", new PersonalityProfile
            {
                Vengefulness = 0.9f,
                Courage = 0.7f,
                Aggression = 0.6f
            });
            
            testNPCs.AddRange(new[] { attacker, victim });
            
            // Simulate initial attack
            victim.Brain.Memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.WasAttacked,
                InvolvedCharacter = attacker.Id,
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object> { ["damage"] = 60 }
            });
            
            victim.Brain.Memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.LostTo,
                InvolvedCharacter = attacker.Id,
                Timestamp = DateTime.Now
            });
            
            simulator.Initialize(testNPCs);
            
            // Process the attack consequences
            victim.Brain.ProcessHourlyUpdate(new WorldState());
            
            // Victim should develop revenge goal
            var revengeGoal = victim.Brain.Goals.GetActiveGoals()
                .FirstOrDefault(g => g.Type == GoalType.GetRevenge && g.Target == attacker.Id);
            revengeGoal.Should().NotBeNull("Victim should develop revenge goal");
            
            // Simulate until revenge attempt
            var revengeAttempted = false;
            for (int i = 0; i < 168 && !revengeAttempted; i++) // 1 week max
            {
                simulator.SimulateHour();
                
                // Check if revenge was attempted
                var attackerMemories = attacker.Brain.Memory.GetMemoriesAbout(victim.Id);
                revengeAttempted = attackerMemories.Any(m => m.Event.Type == MemoryEventType.WasAttacked);
            }
            
            revengeAttempted.Should().BeTrue("Vengeful NPC should eventually attempt revenge");
        }

        // Helper methods

        private Character CreateTestPlayer()
        {
            return new Character
            {
                Name = "Test Player",
                Level = 5,
                MaxHP = 120,
                CurrentHP = 120,
                Strength = 50,
                Defense = 30,
                Gold = 2000,
                Experience = 750
            };
        }

        private NPC CreateTestNPC(string name, PersonalityProfile personality)
        {
            var npc = new NPC
            {
                Name = name,
                Level = 5,
                MaxHP = 100,
                CurrentHP = 100,
                Strength = 40,
                Defense = 25,
                Gold = 1000,
                Personality = personality,
                Location = "town_square"
            };
            
            npc.Brain = new NPCBrain(npc, personality);
            return npc;
        }

        private NPC CreateFriendlyNPC(string name)
        {
            return CreateTestNPC(name, new PersonalityProfile
            {
                Sociability = 0.8f,
                Loyalty = 0.7f,
                Aggression = 0.2f,
                Greed = 0.4f,
                Courage = 0.6f,
                Vengefulness = 0.3f,
                Impulsiveness = 0.4f,
                Ambition = 0.5f
            });
        }

        private NPC CreateHostileNPC(string name)
        {
            return CreateTestNPC(name, new PersonalityProfile
            {
                Aggression = 0.9f,
                Vengefulness = 0.8f,
                Impulsiveness = 0.7f,
                Courage = 0.7f,
                Sociability = 0.2f,
                Loyalty = 0.3f,
                Greed = 0.6f,
                Ambition = 0.5f
            });
        }

        private List<NPC> CreateDiversePopulation(int count)
        {
            var population = new List<NPC>();
            var archetypes = new[] { "thug", "merchant", "noble", "warrior", "scholar" };
            var random = new Random(12345);
            
            for (int i = 0; i < count; i++)
            {
                var archetype = archetypes[i % archetypes.Length];
                var personality = PersonalityProfile.GenerateRandom(archetype);
                var npc = CreateTestNPC($"NPC_{i}", personality);
                
                npc.Level = random.Next(3, 8);
                npc.Gold = random.Next(500, 3000);
                
                population.Add(npc);
            }
            
            return population;
        }

        private string SerializeNPCState(NPC npc)
        {
            // Simple serialization for testing
            var state = new
            {
                Id = npc.Id,
                Name = npc.Name,
                Level = npc.Level,
                Gold = npc.Gold,
                Personality = npc.Personality,
                Memories = npc.Brain.Memory.GetAllMemories(),
                Goals = npc.Brain.Goals.GetActiveGoals(),
                EmotionalState = npc.Brain.EmotionalState
            };
            
            return System.Text.Json.JsonSerializer.Serialize(state);
        }

        private NPC DeserializeNPCState(string saveData)
        {
            // Simple deserialization for testing
            using var doc = System.Text.Json.JsonDocument.Parse(saveData);
            var root = doc.RootElement;
            
            var npc = new NPC
            {
                Id = root.GetProperty("Id").GetString(),
                Name = root.GetProperty("Name").GetString(),
                Level = root.GetProperty("Level").GetInt32(),
                Gold = root.GetProperty("Gold").GetInt32(),
                MaxHP = 100,
                CurrentHP = 100
            };
            
            // This would need proper deserialization in real implementation
            npc.Personality = new PersonalityProfile(); // Simplified
            npc.Brain = new NPCBrain(npc, npc.Personality);
            
            return npc;
        }
    }

    // Test terminal implementation
    public class TestTerminal : ITerminal
    {
        private readonly Queue<string> inputQueue = new();
        private readonly List<string> outputLog = new();

        public void QueueInput(string input)
        {
            inputQueue.Enqueue(input);
        }

        public List<string> GetOutputLog()
        {
            return new List<string>(outputLog);
        }

        public void Write(string text)
        {
            outputLog.Add(text);
        }

        public void WriteLine(string text)
        {
            outputLog.Add(text + Environment.NewLine);
        }

        public string ReadLine()
        {
            return inputQueue.Count > 0 ? inputQueue.Dequeue() : "";
        }

        public void Clear()
        {
            outputLog.Clear();
        }

        public void WaitForKey()
        {
            // Do nothing in tests
        }
    }
} 