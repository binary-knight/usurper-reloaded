using UsurperRemake.Utils;
using NUnit.Framework;
using UsurperRemake;
using UsurperRemake;
using FluentAssertions;
using System.Diagnostics;

namespace UsurperReborn.Tests.UnitTests.AI
{
    [TestFixture]
    public class MemorySystemTests
    {
        private MemorySystem memory;
        private NPC testNPC;
        
        [SetUp]
        public void Setup()
        {
            memory = new MemorySystem();
            testNPC = new NPC
            {
                Name = "Test NPC",
                Level = 5,
                MaxHP = 100,
                CurrentHP = 100,
                Gold = 500,
                Personality = new PersonalityProfile()
            };
            testNPC.Brain = new NPCBrain(testNPC, testNPC.Personality);
        }
        
        [Test]
        public void ValidateMemoryPersistence()
        {
            // Add important memory
            memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.WasAttacked,
                InvolvedCharacter = "player123",
                Details = new Dictionary<string, object> { ["damage"] = 50 },
                Timestamp = DateTime.Now
            });
            
            // Add 100 unimportant memories
            for (int i = 0; i < 100; i++)
            {
                memory.RecordEvent(new MemoryEvent
                {
                    Type = MemoryEventType.HeardRumor,
                    Details = new Dictionary<string, object> { ["rumor"] = $"rumor{i}" },
                    Timestamp = DateTime.Now
                });
            }
            
            // Important memory should still exist
            memory.RemembersBeingAttackedBy("player123").Should().BeTrue("Important attack memory should persist");
            
            // Should have pruned some unimportant memories
            var allMemories = memory.GetAllMemories();
            allMemories.Count.Should().BeLessOrEqualTo(100, "Memory system should enforce limit");
        }
        
        [Test]
        public void ValidateMemoryDecay()
        {
            var oldAttack = new MemoryEvent
            {
                Type = MemoryEventType.WasAttacked,
                InvolvedCharacter = "oldEnemy",
                Timestamp = DateTime.Now.AddDays(-8), // 8 days old
                Details = new Dictionary<string, object> { ["damage"] = 30 }
            };
            
            memory.RecordEvent(oldAttack);
            
            // Should not remember attacks older than 7 days for emotional purposes
            memory.RemembersBeingAttackedBy("oldEnemy").Should().BeFalse("Should not remember old attacks emotionally");
            
            // But should still have the memory for historical purposes
            var historicalMemories = memory.GetMemoriesAbout("oldEnemy");
            historicalMemories.Should().NotBeEmpty("Should keep historical memory");
        }
        
        [Test]
        public void ValidateMemoryInfluencesRelationships()
        {
            var characterId = "testChar";
            
            // Positive interactions
            memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.WasHelped,
                InvolvedCharacter = characterId,
                Timestamp = DateTime.Now
            });
            
            memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.SharedDrink,
                InvolvedCharacter = characterId,
                Timestamp = DateTime.Now
            });
            
            var relationship = memory.GetRelationship(characterId);
            relationship.Friendship.Should().BeGreaterThan(20, "Positive interactions should increase friendship");
            relationship.Trust.Should().BeGreaterThan(20, "Positive interactions should increase trust");
            
            // Betrayal overwrites positive history
            memory.RecordEvent(new MemoryEvent
            {
                Type = MemoryEventType.WasBetrayed,
                InvolvedCharacter = characterId,
                Timestamp = DateTime.Now
            });
            
            relationship = memory.GetRelationship(characterId);
            relationship.Trust.Should().Be(-100, "Betrayal should destroy trust");
            relationship.Friendship.Should().BeLessThan(0, "Betrayal should damage friendship");
        }
        
        [Test]
        public void ValidateMemoryImportanceCalculation()
        {
            var importantEvents = new[]
            {
                new MemoryEvent { Type = MemoryEventType.WasAttacked, InvolvedCharacter = "enemy1" },
                new MemoryEvent { Type = MemoryEventType.WasBetrayed, InvolvedCharacter = "traitor1" },
                new MemoryEvent { Type = MemoryEventType.WasHelped, InvolvedCharacter = "helper1" }
            };
            
            var unimportantEvents = new[]
            {
                new MemoryEvent { Type = MemoryEventType.HeardRumor, Details = new Dictionary<string, object> { ["rumor"] = "test" } },
                new MemoryEvent { Type = MemoryEventType.SawPerson, InvolvedCharacter = "stranger1" },
                new MemoryEvent { Type = MemoryEventType.VisitedLocation, Details = new Dictionary<string, object> { ["location"] = "tavern" } }
            };
            
            foreach (var evt in importantEvents)
            {
                evt.Timestamp = DateTime.Now;
                memory.RecordEvent(evt);
            }
            
            foreach (var evt in unimportantEvents)
            {
                evt.Timestamp = DateTime.Now;
                memory.RecordEvent(evt);
            }
            
            // Add many more unimportant events to trigger pruning
            for (int i = 0; i < 200; i++)
            {
                memory.RecordEvent(new MemoryEvent
                {
                    Type = MemoryEventType.HeardRumor,
                    Details = new Dictionary<string, object> { ["rumor"] = $"filler{i}" },
                    Timestamp = DateTime.Now
                });
            }
            
            // Important memories should survive pruning
            memory.RemembersBeingAttackedBy("enemy1").Should().BeTrue("Important attack memory should survive");
            memory.GetRelationship("traitor1").Trust.Should().Be(-100, "Betrayal memory should survive");
            memory.GetRelationship("helper1").Friendship.Should().BeGreaterThan(0, "Helper memory should survive");
        }
        
        [Test]
        public void ValidateRelationshipCalculation()
        {
            var characterId = "relationshipTest";
            
            // Scenario: Betrayal by friend
            var events = new[]
            {
                new MemoryEvent { Type = MemoryEventType.SharedDrink, InvolvedCharacter = characterId },
                new MemoryEvent { Type = MemoryEventType.WasHelped, InvolvedCharacter = characterId },
                new MemoryEvent { Type = MemoryEventType.WasBetrayed, InvolvedCharacter = characterId }
            };
            
            foreach (var evt in events)
            {
                evt.Timestamp = DateTime.Now;
                memory.RecordEvent(evt);
            }
            
            var relationship = memory.GetRelationship(characterId);
            
            // Expected: friendship damaged, trust destroyed, hostility increased
            relationship.Friendship.Should().BeLessThan(0, "Betrayal should damage friendship despite past help");
            relationship.Trust.Should().Be(-100, "Betrayal should destroy trust completely");
            relationship.Hostility.Should().BeGreaterThan(50, "Betrayal should increase hostility significantly");
        }
        
        [Test]
        public void ValidateMemoryQuerying()
        {
            var character1 = "char1";
            var character2 = "char2";
            
            // Add various memories
            memory.RecordEvent(new MemoryEvent { Type = MemoryEventType.WasAttacked, InvolvedCharacter = character1, Timestamp = DateTime.Now });
            memory.RecordEvent(new MemoryEvent { Type = MemoryEventType.WasHelped, InvolvedCharacter = character1, Timestamp = DateTime.Now });
            memory.RecordEvent(new MemoryEvent { Type = MemoryEventType.SharedDrink, InvolvedCharacter = character2, Timestamp = DateTime.Now });
            
            // Test specific queries
            var char1Memories = memory.GetMemoriesAbout(character1);
            char1Memories.Should().HaveCount(2, "Should have 2 memories about character1");
            
            var char2Memories = memory.GetMemoriesAbout(character2);
            char2Memories.Should().HaveCount(1, "Should have 1 memory about character2");
            
            // Test attack memory query
            memory.RemembersBeingAttackedBy(character1).Should().BeTrue("Should remember being attacked by character1");
            memory.RemembersBeingAttackedBy(character2).Should().BeFalse("Should not remember being attacked by character2");
            
            // Test help memory query
            memory.RemembersBeingHelpedBy(character1).Should().BeTrue("Should remember being helped by character1");
            memory.RemembersBeingHelpedBy(character2).Should().BeFalse("Should not remember being helped by character2");
        }
        
        [Test]
        public void ValidateEnemyIdentification()
        {
            var enemy1 = "enemy1";
            var enemy2 = "enemy2";
            var friend1 = "friend1";
            
            // Create enemy relationships
            memory.RecordEvent(new MemoryEvent { Type = MemoryEventType.WasAttacked, InvolvedCharacter = enemy1, Timestamp = DateTime.Now });
            memory.RecordEvent(new MemoryEvent { Type = MemoryEventType.WasBetrayed, InvolvedCharacter = enemy2, Timestamp = DateTime.Now });
            memory.RecordEvent(new MemoryEvent { Type = MemoryEventType.WasHelped, InvolvedCharacter = friend1, Timestamp = DateTime.Now });
            
            var enemies = memory.GetEnemies();
            var allies = memory.GetAllies();
            
            enemies.Should().Contain(enemy1, "Attacker should be identified as enemy");
            enemies.Should().Contain(enemy2, "Betrayer should be identified as enemy");
            enemies.Should().NotContain(friend1, "Helper should not be identified as enemy");
            
            allies.Should().Contain(friend1, "Helper should be identified as ally");
            allies.Should().NotContain(enemy1, "Attacker should not be identified as ally");
        }
        
        [Test]
        public void ValidateMemoryEmotionalImpact()
        {
            var attacker = "attacker";
            var helper = "helper";
            
            // Record emotional events
            memory.RecordEvent(new MemoryEvent 
            { 
                Type = MemoryEventType.WasAttacked, 
                InvolvedCharacter = attacker, 
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object> { ["damage"] = 80 } // High damage
            });
            
            memory.RecordEvent(new MemoryEvent 
            { 
                Type = MemoryEventType.WasHelped, 
                InvolvedCharacter = helper, 
                Timestamp = DateTime.Now,
                Details = new Dictionary<string, object> { ["significance"] = "high" }
            });
            
            // Check emotional impact on relationships
            var attackerRel = memory.GetRelationship(attacker);
            var helperRel = memory.GetRelationship(helper);
            
            attackerRel.Hostility.Should().BeGreaterThan(50, "High damage attack should create strong hostility");
            attackerRel.Fear.Should().BeGreaterThan(30, "Attack should create fear");
            
            helperRel.Friendship.Should().BeGreaterThan(30, "Significant help should create strong friendship");
            helperRel.Trust.Should().BeGreaterThan(20, "Help should increase trust");
        }
        
        [Test]
        public void ValidateMemoryPerformance()
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Add 1000 memories
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
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Memory recording should be fast");
            
            // Query performance
            stopwatch.Restart();
            for (int i = 0; i < 100; i++)
            {
                memory.GetMemoriesAbout($"char{i % 50}");
                memory.GetRelationship($"char{i % 50}");
            }
            stopwatch.Stop();
            
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50, "Memory queries should be fast");
        }
        
        [Test]
        public void ValidateMemoryConsistency()
        {
            var characterId = "consistencyTest";
            
            // Record the same type of event multiple times
            for (int i = 0; i < 5; i++)
            {
                memory.RecordEvent(new MemoryEvent
                {
                    Type = MemoryEventType.SharedDrink,
                    InvolvedCharacter = characterId,
                    Timestamp = DateTime.Now.AddMinutes(i)
                });
            }
            
            var memories = memory.GetMemoriesAbout(characterId);
            var relationship = memory.GetRelationship(characterId);
            
            memories.Should().HaveCount(5, "Should record all drink-sharing events");
            relationship.Friendship.Should().BeGreaterThan(50, "Multiple positive interactions should build strong friendship");
            
            // Friendship should increase with each interaction but with diminishing returns
            var friendshipPerEvent = relationship.Friendship / 5;
            friendshipPerEvent.Should().BeInRange(10, 25, "Each drink should contribute reasonable friendship");
        }
        
        [Test]
        public void ValidateMemoryEventTypes()
        {
            var characterId = "eventTypeTest";
            var eventTypes = Enum.GetValues<MemoryEventType>();
            
            // Test each event type can be recorded and retrieved
            foreach (var eventType in eventTypes)
            {
                memory.RecordEvent(new MemoryEvent
                {
                    Type = eventType,
                    InvolvedCharacter = characterId,
                    Timestamp = DateTime.Now,
                    Details = new Dictionary<string, object> { ["test"] = eventType.ToString() }
                });
            }
            
            var memories = memory.GetMemoriesAbout(characterId);
            memories.Should().HaveCount(eventTypes.Length, "Should record all event types");
            
            // Verify each event type is present
            foreach (var eventType in eventTypes)
            {
                memories.Should().Contain(m => m.Event.Type == eventType, 
                    $"Should have memory of type {eventType}");
            }
        }
        
        [Test]
        public void ValidateMemoryThreadSafety()
        {
            var tasks = new List<Task>();
            var characterIds = Enumerable.Range(0, 10).Select(i => $"char{i}").ToArray();
            
            // Simulate concurrent memory operations
            for (int i = 0; i < 10; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        memory.RecordEvent(new MemoryEvent
                        {
                            Type = MemoryEventType.SawPerson,
                            InvolvedCharacter = characterIds[j % characterIds.Length],
                            Timestamp = DateTime.Now
                        });
                        
                        memory.GetMemoriesAbout(characterIds[j % characterIds.Length]);
                        memory.GetRelationship(characterIds[j % characterIds.Length]);
                    }
                }));
            }
            
            // Should complete without exceptions
            Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
            
            // Should have reasonable number of memories (some may be pruned)
            var totalMemories = memory.GetAllMemories().Count;
            totalMemories.Should().BeGreaterThan(100, "Should have recorded many memories");
        }
    }
} 
