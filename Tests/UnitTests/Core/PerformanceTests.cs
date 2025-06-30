using UsurperRemake.Utils;
using NUnit.Framework;
using UsurperRemake;
using UsurperRemake;
using UsurperRemake;
using FluentAssertions;
using System.Diagnostics;

namespace UsurperReborn.Tests.UnitTests.Core
{
    [TestFixture]
    public class PerformanceTests
    {
        private Random random;

        [SetUp]
        public void Setup()
        {
            random = new Random(12345);
        }

        [Test]
        public void ValidateNPCDecisionPerformance()
        {
            var npcs = GenerateNPCs(100);
            var worldState = new WorldState
            {
                CurrentHour = 14,
                CurrentLocation = "town_square",
                InCombat = false
            };
            
            var stopwatch = Stopwatch.StartNew();
            
            foreach (var npc in npcs)
            {
                npc.Brain.DecideNextAction(worldState);
            }
            
            stopwatch.Stop();
            
            // 100 NPC decisions should complete in under 200ms
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(200,
                "NPC decision making should be fast enough for real-time gameplay");
        }

        [Test]
        public void ValidateMemorySystemPerformance()
        {
            var memory = new MemorySystem();
            
            // Add 1000 memories
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                memory.RecordEvent(new MemoryEvent
                {
                    Type = (MemoryEventType)(i % 10),
                    InvolvedCharacter = $"char{i % 50}",
                    Details = new Dictionary<string, object> { ["data"] = i },
                    Timestamp = DateTime.Now
                });
            }
            stopwatch.Stop();
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
                "Memory recording should be fast");
            
            // Query performance
            stopwatch.Restart();
            for (int i = 0; i < 100; i++)
            {
                memory.GetMemoriesAbout($"char{i % 50}");
                memory.GetRelationship($"char{i % 50}");
            }
            stopwatch.Stop();
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
                "Memory queries should be fast");
        }

        [Test]
        public void ValidateWorldSimulationScaling()
        {
            var testCounts = new[] { 10, 25, 50 };
            var timings = new Dictionary<int, long>();
            
            foreach (var count in testCounts)
            {
                var simulator = WorldSimulator.Instance;
                simulator.Reset();
                simulator.Initialize(GenerateNPCs(count));
                
                var stopwatch = Stopwatch.StartNew();
                simulator.SimulateHour();
                stopwatch.Stop();
                
                timings[count] = stopwatch.ElapsedMilliseconds;
                
                // Each individual simulation should be reasonable
                stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, 
                    $"Simulation with {count} NPCs should complete quickly");
            }
            
            // Verify roughly linear scaling (not exponential)
            var scalingFactor = (double)timings[50] / timings[10];
            scalingFactor.Should().BeLessThan(10, 
                "Simulation should scale reasonably with population size");
        }

        [Test]
        public void ValidateCombatPerformance()
        {
            var combatEngine = new CombatEngine();
            var combats = new List<CombatResult>();
            
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 200; i++)
            {
                var npc1 = GenerateRandomNPC($"Fighter{i}A");
                var npc2 = GenerateRandomNPC($"Fighter{i}B");
                
                combats.Add(combatEngine.ExecuteCombat(npc1, npc2));
            }
            
            stopwatch.Stop();
            
            // Should complete 200 combats in under 3 seconds
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000,
                "Combat system should handle many combats efficiently");
            
            // Verify all combats completed
            combats.Should().HaveCount(200);
            combats.Should().OnlyContain(c => c.Winner != null, "All combats should have a winner");
        }

        [Test]
        public void ValidateGoalSystemPerformance()
        {
            var npcs = GenerateNPCs(50);
            var worldState = new WorldState { CurrentHour = 14 };
            
            var stopwatch = Stopwatch.StartNew();
            
            foreach (var npc in npcs)
            {
                // Process goal updates
                npc.Brain.Goals.UpdateGoals(npc, worldState, npc.Brain.Memory, npc.Brain.EmotionalState);
                
                // Get priority goal
                var priorityGoal = npc.Brain.Goals.GetPriorityGoal();
                
                // Get all active goals
                var activeGoals = npc.Brain.Goals.GetActiveGoals();
            }
            
            stopwatch.Stop();
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
                "Goal system processing should be fast for 50 NPCs");
        }

        [Test]
        public void ValidateRelationshipCalculationPerformance()
        {
            var memory = new MemorySystem();
            
            // Add many relationship-affecting events
            for (int i = 0; i < 500; i++)
            {
                memory.RecordEvent(new MemoryEvent
                {
                    Type = new MemoryEventType[]
                    {
                        MemoryEventType.WasHelped,
                        MemoryEventType.WasAttacked,
                        MemoryEventType.SharedDrink,
                        MemoryEventType.WasBetrayed
                    }[i % 4],
                    InvolvedCharacter = $"char{i % 20}",
                    Timestamp = DateTime.Now
                });
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            // Calculate relationships for all characters
            for (int i = 0; i < 20; i++)
            {
                var relationship = memory.GetRelationship($"char{i}");
                var enemies = memory.GetEnemies();
                var allies = memory.GetAllies();
            }
            
            stopwatch.Stop();
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
                "Relationship calculations should be efficient");
        }

        [Test]
        public void ValidateLongRunningSimulationStability()
        {
            var simulator = WorldSimulator.Instance;
            simulator.Reset();
            
            var npcs = GenerateNPCs(20);
            simulator.Initialize(npcs);
            
            var initialMemoryUsage = GC.GetTotalMemory(true);
            
            // Simulate 7 days (168 hours)
            var stopwatch = Stopwatch.StartNew();
            for (int hour = 0; hour < 168; hour++)
            {
                simulator.SimulateHour();
                
                // Check memory usage periodically
                if (hour % 24 == 0)
                {
                    var currentMemory = GC.GetTotalMemory(false);
                    var memoryIncrease = currentMemory - initialMemoryUsage;
                    
                    // Memory shouldn't grow excessively (allow 10MB growth)
                    memoryIncrease.Should().BeLessThan(10 * 1024 * 1024,
                        $"Memory usage should remain stable at hour {hour}");
                }
            }
            stopwatch.Stop();
            
            // Total simulation time should be reasonable
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000,
                "7-day simulation should complete in under 30 seconds");
            
            // NPCs should still be functional
            var aliveNPCs = simulator.GetAliveNPCs();
            aliveNPCs.Should().NotBeEmpty("Some NPCs should still be alive");
            
            foreach (var npc in aliveNPCs.Take(5))
            {
                var action = npc.Brain.DecideNextAction(new WorldState { CurrentHour = 12 });
                action.Should().NotBeNull("NPCs should still be making decisions");
            }
        }

        [Test]
        public void ValidateConcurrentNPCOperations()
        {
            var npcs = GenerateNPCs(20);
            var worldState = new WorldState { CurrentHour = 14 };
            
            var stopwatch = Stopwatch.StartNew();
            
            // Simulate concurrent NPC processing
            Parallel.ForEach(npcs, npc =>
            {
                for (int i = 0; i < 50; i++)
                {
                    npc.Brain.DecideNextAction(worldState);
                    npc.Brain.ProcessHourlyUpdate(worldState);
                    
                    // Add some memory events
                    npc.Brain.Memory.RecordEvent(new MemoryEvent
                    {
                        Type = MemoryEventType.SawPerson,
                        InvolvedCharacter = $"stranger{i}",
                        Timestamp = DateTime.Now
                    });
                }
            });
            
            stopwatch.Stop();
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
                "Concurrent NPC operations should complete quickly");
            
            // Verify NPCs are still in valid state
            foreach (var npc in npcs)
            {
                npc.Brain.Should().NotBeNull("NPC brain should remain valid");
                var memories = npc.Brain.Memory.GetAllMemories();
                memories.Should().NotBeEmpty("NPCs should have recorded memories");
            }
        }

        // Helper methods
        private List<NPC> GenerateNPCs(int count)
        {
            var npcs = new List<NPC>();
            var archetypes = new[] { "thug", "merchant", "noble", "warrior", "scholar" };
            
            for (int i = 0; i < count; i++)
            {
                var archetype = archetypes[i % archetypes.Length];
                var personality = PersonalityProfile.GenerateRandom(archetype);
                
                var npc = new NPC
                {
                    Name = $"NPC_{i}",
                    Level = random.Next(3, 8),
                    MaxHP = 100,
                    CurrentHP = random.Next(50, 100),
                    Strength = random.Next(30, 60),
                    Defense = random.Next(20, 40),
                    Gold = random.Next(100, 2000),
                    Personality = personality,
                    Location = "town_square"
                };
                
                npc.Brain = new NPCBrain(npc, personality);
                npcs.Add(npc);
            }
            
            return npcs;
        }

        private NPC GenerateRandomNPC(string name)
        {
            var personality = new PersonalityProfile
            {
                Aggression = (float)random.NextDouble(),
                Greed = (float)random.NextDouble(),
                Courage = (float)random.NextDouble(),
                Loyalty = (float)random.NextDouble(),
                Vengefulness = (float)random.NextDouble(),
                Impulsiveness = (float)random.NextDouble(),
                Sociability = (float)random.NextDouble(),
                Ambition = (float)random.NextDouble()
            };
            
            var npc = new NPC
            {
                Name = name,
                Level = random.Next(3, 8),
                MaxHP = 100,
                CurrentHP = random.Next(50, 100),
                Strength = random.Next(30, 60),
                Defense = random.Next(20, 40),
                Gold = random.Next(100, 1000),
                Personality = personality,
                Location = "town_square"
            };
            
            npc.Brain = new NPCBrain(npc, personality);
            return npc;
        }
    }
} 
