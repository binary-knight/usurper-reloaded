using UsurperRemake.Utils;
using System;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Quest System Validation Tests
/// Comprehensive testing of quest creation, claiming, completion, and Pascal compatibility
/// </summary>
public class QuestSystemValidation
{
    private static TerminalEmulator terminal;
    private static int testsRun = 0;
    private static int testsPassed = 0;
    
    /// <summary>
    /// Run all quest system validation tests
    /// </summary>
    public static async Task RunAllTests(TerminalEmulator term)
    {
        terminal = term;
        testsRun = 0;
        testsPassed = 0;
        
        terminal.WriteLine("═══════════════════════════════════════", "bright_cyan");
        terminal.WriteLine("     QUEST SYSTEM VALIDATION TESTS", "bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════", "bright_cyan");
        terminal.WriteLine("", "white");
        
        // Core Quest System Tests
        await TestQuestCreation();
        await TestQuestClaiming();
        await TestQuestCompletion();
        await TestQuestRewards();
        
        // Quest Data Tests
        await TestQuestDataStructure();
        await TestQuestTargetTypes();
        await TestQuestDifficulty();
        
        // Quest Validation Tests
        await TestQuestValidation();
        await TestQuestLimits();
        await TestQuestFailure();
        
        // Integration Tests
        await TestQuestMaintenance();
        await TestQuestLocations();
        
        // Pascal Compatibility Tests
        await TestPascalCompatibility();
        
        // Display final results
        terminal.WriteLine("", "white");
        terminal.WriteLine("═══════════════════════════════════════", "bright_cyan");
        terminal.WriteLine($"Quest System Tests: {testsPassed}/{testsRun} Passed", 
                         testsPassed == testsRun ? "bright_green" : "bright_red");
        terminal.WriteLine("═══════════════════════════════════════", "bright_cyan");
        
        if (testsPassed == testsRun)
        {
            terminal.WriteLine("✅ All quest system tests PASSED!", "bright_green");
            terminal.WriteLine("Quest system is ready for production.", "bright_green");
        }
        else
        {
            terminal.WriteLine("❌ Some quest system tests FAILED!", "bright_red");
            terminal.WriteLine("Quest system needs attention before production.", "bright_red");
        }
    }
    
    /// <summary>
    /// Test quest creation functionality
    /// </summary>
    private static async Task TestQuestCreation()
    {
        terminal.WriteLine("Testing Quest Creation...", "yellow");
        
        // Create test king
        var king = TestHelpers.CreateTestKing();
        king.QuestsLeft = 5;
        
        try
        {
            // Test basic quest creation
            var quest = QuestSystem.CreateQuest(king, QuestTarget.Monster, 2, "Test monster quest");
            
            Assert(quest != null, "Quest should be created");
            Assert(!string.IsNullOrEmpty(quest.Id), "Quest should have valid ID");
            Assert(quest.Initiator == king.Name2, "Quest initiator should match king");
            Assert(quest.QuestTarget == QuestTarget.Monster, "Quest target should match");
            Assert(quest.Difficulty == 2, "Quest difficulty should match");
            Assert(quest.Comment == "Test monster quest", "Quest comment should match");
            Assert(king.QuestsLeft == 4, "King's quest count should decrease");
            
            terminal.WriteLine("✅ Basic quest creation", "green");
            
            // Test quest with monsters
            Assert(quest.Monsters.Count > 0, "Monster quest should have monsters");
            terminal.WriteLine("✅ Monster generation for quest", "green");
            
            // Test different quest types
            var assassinQuest = QuestSystem.CreateQuest(king, QuestTarget.Assassin, 1, "Test assassination");
            Assert(assassinQuest.QuestTarget == QuestTarget.Assassin, "Assassination quest type");
            terminal.WriteLine("✅ Different quest types", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest creation failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test quest claiming functionality
    /// </summary>
    private static async Task TestQuestClaiming()
    {
        terminal.WriteLine("Testing Quest Claiming...", "yellow");
        
        try
        {
            // Create test data
            var king = TestHelpers.CreateTestKing();
            king.QuestsLeft = 5;
            var player = TestHelpers.CreateTestPlayer();
            
            // Create a quest
            var quest = QuestSystem.CreateQuest(king, QuestTarget.Monster, 2, "Test quest");
            
            // Test successful claiming
            var result = QuestSystem.ClaimQuest(player, quest.Id);
            Assert(result, "Player should be able to claim quest");
            Assert(quest.Occupier == player.Name2, "Quest should be occupied by player");
            Assert(quest.OccupierRace == player.Race, "Quest should record player race");
            Assert(quest.OccupiedDays == 0, "Quest should start with 0 days");
            
            terminal.WriteLine("✅ Successful quest claiming", "green");
            
            // Test claiming already claimed quest
            var player2 = TestHelpers.CreateTestPlayer();
            player2.Name2 = "TestPlayer2";
            var result2 = QuestSystem.ClaimQuest(player2, quest.Id);
            Assert(!result2, "Should not be able to claim already claimed quest");
            
            terminal.WriteLine("✅ Cannot claim already claimed quest", "green");
            
            // Test king cannot claim quest
            var result3 = QuestSystem.ClaimQuest(king, quest.Id);
            Assert(!result3, "King should not be able to claim quest");
            
            terminal.WriteLine("✅ King cannot claim quests", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest claiming failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test quest completion functionality
    /// </summary>
    private static async Task TestQuestCompletion()
    {
        terminal.WriteLine("Testing Quest Completion...", "yellow");
        
        try
        {
            // Create test data
            var king = TestHelpers.CreateTestKing();
            king.QuestsLeft = 5;
            var player = TestHelpers.CreateTestPlayer();
            var testTerminal = new TerminalUI();
            
            // Create and claim quest
            var quest = QuestSystem.CreateQuest(king, QuestTarget.Monster, 2, "Test completion quest");
            QuestSystem.ClaimQuest(player, quest.Id);
            
            // Set up player for completion (mock monster kills)
            player.MKills = 100;  // Ensure enough kills for quest
            
            var initialExp = player.Experience;
            var initialQuests = player.RoyQuests;
            
            // Test quest completion
            var result = QuestSystem.CompleteQuest(player, quest.Id, testTerminal);
            Assert(result, "Quest should complete successfully");
            Assert(quest.Deleted, "Completed quest should be marked as deleted");
            Assert(string.IsNullOrEmpty(quest.Occupier), "Completed quest should have no occupier");
            Assert(player.RoyQuests == initialQuests + 1, "Player quest count should increase");
            
            terminal.WriteLine("✅ Successful quest completion", "green");
            
            // Test cannot complete other player's quest
            var player2 = TestHelpers.CreateTestPlayer();
            player2.Name2 = "TestPlayer2";
            var quest2 = QuestSystem.CreateQuest(king, QuestTarget.Monster, 1, "Another quest");
            QuestSystem.ClaimQuest(player, quest2.Id);
            
            var result2 = QuestSystem.CompleteQuest(player2, quest2.Id, testTerminal);
            Assert(!result2, "Should not complete other player's quest");
            
            terminal.WriteLine("✅ Cannot complete other player's quest", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest completion failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test quest reward calculations
    /// </summary>
    private static async Task TestQuestRewards()
    {
        terminal.WriteLine("Testing Quest Rewards...", "yellow");
        
        try
        {
            var quest = new Quest
            {
                Reward = 2,  // Medium reward
                RewardType = QuestRewardType.Experience
            };
            
            // Test experience reward calculation
            var expReward = quest.CalculateReward(10);  // Level 10 player
            Assert(expReward == 5000, $"Experience reward should be 5000, got {expReward}");  // 10 * 500
            
            terminal.WriteLine("✅ Experience reward calculation", "green");
            
            // Test gold reward
            quest.RewardType = QuestRewardType.Money;
            var goldReward = quest.CalculateReward(10);
            Assert(goldReward == 51000, $"Gold reward should be 51000, got {goldReward}");  // 10 * 5100
            
            terminal.WriteLine("✅ Gold reward calculation", "green");
            
            // Test potion reward
            quest.RewardType = QuestRewardType.Potions;
            var potionReward = quest.CalculateReward(10);
            Assert(potionReward == 100, $"Potion reward should be 100, got {potionReward}");
            
            terminal.WriteLine("✅ Potion reward calculation", "green");
            
            // Test alignment rewards
            quest.RewardType = QuestRewardType.Chivalry;
            var chivalryReward = quest.CalculateReward(10);
            Assert(chivalryReward == 75, $"Chivalry reward should be 75, got {chivalryReward}");
            
            terminal.WriteLine("✅ Alignment reward calculation", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest rewards failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test quest data structure
    /// </summary>
    private static async Task TestQuestDataStructure()
    {
        terminal.WriteLine("Testing Quest Data Structure...", "yellow");
        
        try
        {
            var quest = new Quest();
            
            // Test default values
            Assert(!string.IsNullOrEmpty(quest.Id), "Quest should have auto-generated ID");
            Assert(!quest.Deleted, "Quest should not be deleted by default");
            Assert(quest.DaysToComplete == 7, "Quest should have 7 days by default");
            Assert(quest.Monsters != null, "Quest should have monsters list");
            Assert(quest.MinLevel == 1, "Quest should have min level 1");
            Assert(quest.MaxLevel == 9999, "Quest should have max level 9999");
            
            terminal.WriteLine("✅ Default quest values", "green");
            
            // Test status properties
            Assert(quest.IsAvailable, "Empty quest should be available");
            Assert(!quest.IsActive, "Empty quest should not be active");
            
            quest.Occupier = "TestPlayer";
            Assert(!quest.IsAvailable, "Occupied quest should not be available");
            Assert(quest.IsActive, "Occupied quest should be active");
            
            terminal.WriteLine("✅ Quest status properties", "green");
            
            // Test difficulty strings
            quest.Difficulty = 1;
            Assert(quest.GetDifficultyString() == "Easy", "Difficulty 1 should be Easy");
            quest.Difficulty = 4;
            Assert(quest.GetDifficultyString() == "Extreme", "Difficulty 4 should be Extreme");
            
            terminal.WriteLine("✅ Difficulty descriptions", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest data structure failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test quest target types
    /// </summary>
    private static async Task TestQuestTargetTypes()
    {
        terminal.WriteLine("Testing Quest Target Types...", "yellow");
        
        try
        {
            // Test all quest target descriptions
            var monsterQuest = new Quest { QuestTarget = QuestTarget.Monster };
            Assert(monsterQuest.GetTargetDescription() == "Slay Monsters", "Monster quest description");
            
            var assassinQuest = new Quest { QuestTarget = QuestTarget.Assassin };
            Assert(assassinQuest.GetTargetDescription() == "Assassination Mission", "Assassin quest description");
            
            var seduceQuest = new Quest { QuestTarget = QuestTarget.Seduce };
            Assert(seduceQuest.GetTargetDescription() == "Seduction Mission", "Seduce quest description");
            
            var territoryQuest = new Quest { QuestTarget = QuestTarget.ClaimTown };
            Assert(territoryQuest.GetTargetDescription() == "Claim Territory", "Territory quest description");
            
            var gangQuest = new Quest { QuestTarget = QuestTarget.GangWar };
            Assert(gangQuest.GetTargetDescription() == "Gang War Participation", "Gang war quest description");
            
            terminal.WriteLine("✅ All quest target types", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest target types failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test quest difficulty levels
    /// </summary>
    private static async Task TestQuestDifficulty()
    {
        terminal.WriteLine("Testing Quest Difficulty...", "yellow");
        
        try
        {
            var king = TestHelpers.CreateTestKing();
            king.QuestsLeft = 10;
            
            // Test different difficulty levels generate appropriate monsters
            for (byte diff = 1; diff <= 4; diff++)
            {
                var quest = QuestSystem.CreateQuest(king, QuestTarget.Monster, diff, $"Difficulty {diff} test");
                
                if (quest.QuestTarget == QuestTarget.Monster)
                {
                    Assert(quest.Monsters.Count >= 1, $"Difficulty {diff} should have monsters");
                    Assert(quest.Monsters.Count <= diff, $"Difficulty {diff} should have at most {diff} monster types");
                    
                    // Check monster counts increase with difficulty
                    var totalMonsters = 0;
                    foreach (var monster in quest.Monsters)
                    {
                        totalMonsters += monster.Count;
                    }
                    
                    Assert(totalMonsters >= diff * 2, $"Difficulty {diff} should have adequate monster count");
                }
            }
            
            terminal.WriteLine("✅ Difficulty-based monster generation", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest difficulty failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test quest validation rules
    /// </summary>
    private static async Task TestQuestValidation()
    {
        terminal.WriteLine("Testing Quest Validation...", "yellow");
        
        try
        {
            var king = TestHelpers.CreateTestKing();
            var player = TestHelpers.CreateTestPlayer();
            king.QuestsLeft = 5;
            
            // Create quest with level restrictions
            var quest = QuestSystem.CreateQuest(king, QuestTarget.Monster, 2, "Level restricted quest");
            quest.MinLevel = 5;
            quest.MaxLevel = 15;
            
            // Test level too low
            player.Level = 3;
            var result1 = QuestSystem.ClaimQuest(player, quest.Id);
            Assert(!result1, "Should not claim quest with level too low");
            
            // Test level too high
            player.Level = 20;
            var result2 = QuestSystem.ClaimQuest(player, quest.Id);
            Assert(!result2, "Should not claim quest with level too high");
            
            // Test valid level
            player.Level = 10;
            var result3 = QuestSystem.ClaimQuest(player, quest.Id);
            Assert(result3, "Should claim quest with valid level");
            
            terminal.WriteLine("✅ Level restriction validation", "green");
            
            // Test cannot claim own quest
            var selfQuest = QuestSystem.CreateQuest(player, QuestTarget.Monster, 1, "Self quest");
            var result4 = QuestSystem.ClaimQuest(player, selfQuest.Id);
            Assert(!result4, "Should not claim own quest");
            
            terminal.WriteLine("✅ Self-quest restriction", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest validation failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test quest limits and restrictions
    /// </summary>
    private static async Task TestQuestLimits()
    {
        terminal.WriteLine("Testing Quest Limits...", "yellow");
        
        try
        {
            var king = TestHelpers.CreateTestKing();
            
            // Test king quest limit
            king.QuestsLeft = 1;
            var quest1 = QuestSystem.CreateQuest(king, QuestTarget.Monster, 1, "First quest");
            Assert(king.QuestsLeft == 0, "King should have no quests left");
            
            try
            {
                var quest2 = QuestSystem.CreateQuest(king, QuestTarget.Monster, 1, "Second quest");
                TestFailed("Should not create quest when limit reached");
            }
            catch (InvalidOperationException)
            {
                terminal.WriteLine("✅ King quest limit enforcement", "green");
            }
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest limits failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test quest failure handling
    /// </summary>
    private static async Task TestQuestFailure()
    {
        terminal.WriteLine("Testing Quest Failure...", "yellow");
        
        try
        {
            var king = TestHelpers.CreateTestKing();
            var player = TestHelpers.CreateTestPlayer();
            king.QuestsLeft = 5;
            
            // Create and claim quest
            var quest = QuestSystem.CreateQuest(king, QuestTarget.Monster, 1, "Failure test quest");
            QuestSystem.ClaimQuest(player, quest.Id);
            
            // Simulate time passing
            quest.OccupiedDays = 8;  // More than default 7 days
            
            // Test quest is expired
            Assert(quest.DaysRemaining <= 0, "Quest should be expired");
            
            terminal.WriteLine("✅ Quest expiration detection", "green");
            
            // Test daily maintenance handles failure
            QuestSystem.ProcessDailyQuestMaintenance();
            Assert(quest.Deleted, "Expired quest should be deleted");
            Assert(string.IsNullOrEmpty(quest.Occupier), "Failed quest should have no occupier");
            
            terminal.WriteLine("✅ Quest failure processing", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest failure failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test quest maintenance functionality
    /// </summary>
    private static async Task TestQuestMaintenance()
    {
        terminal.WriteLine("Testing Quest Maintenance...", "yellow");
        
        try
        {
            // Test daily maintenance runs without errors
            QuestSystem.ProcessDailyQuestMaintenance();
            terminal.WriteLine("✅ Daily maintenance execution", "green");
            
            // Test quest retrieval functions
            var allQuests = QuestSystem.GetAllQuests();
            Assert(allQuests != null, "Should return quest list");
            
            var playerQuests = QuestSystem.GetPlayerQuests("NonExistentPlayer");
            Assert(playerQuests != null, "Should return empty list for non-existent player");
            Assert(playerQuests.Count == 0, "Should return empty list for non-existent player");
            
            terminal.WriteLine("✅ Quest retrieval functions", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest maintenance failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test quest location functionality
    /// </summary>
    private static async Task TestQuestLocations()
    {
        terminal.WriteLine("Testing Quest Locations...", "yellow");
        
        try
        {
            var testTerminal = new TerminalEmulator();
            
            // Test Quest Hall can be created
            var questHall = new QuestHallLocation(testTerminal);
            Assert(questHall != null, "Quest Hall should be created");
            
            // Test Royal Quest location can be created
            var royalQuest = new RoyalQuestLocation(testTerminal);
            Assert(royalQuest != null, "Royal Quest location should be created");
            
            terminal.WriteLine("✅ Location creation", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Quest locations failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Test Pascal compatibility
    /// </summary>
    private static async Task TestPascalCompatibility()
    {
        terminal.WriteLine("Testing Pascal Compatibility...", "yellow");
        
        try
        {
            // Test Pascal enum values
            Assert((int)QuestType.SingleQuest == 0, "SingleQuest should be 0");
            Assert((int)QuestType.TeamQuest == 1, "TeamQuest should be 1");
            
            Assert((int)QuestTarget.Monster == 0, "Monster should be 0");
            Assert((int)QuestTarget.Assassin == 1, "Assassin should be 1");
            Assert((int)QuestTarget.Seduce == 2, "Seduce should be 2");
            Assert((int)QuestTarget.ClaimTown == 3, "ClaimTown should be 3");
            Assert((int)QuestTarget.GangWar == 4, "GangWar should be 4");
            
            Assert((int)QuestRewardType.Nothing == 0, "Nothing should be 0");
            Assert((int)QuestRewardType.Experience == 1, "Experience should be 1");
            Assert((int)QuestRewardType.Money == 2, "Money should be 2");
            Assert((int)QuestRewardType.Potions == 3, "Potions should be 3");
            Assert((int)QuestRewardType.Darkness == 4, "Darkness should be 4");
            Assert((int)QuestRewardType.Chivalry == 5, "Chivalry should be 5");
            
            terminal.WriteLine("✅ Pascal enum compatibility", "green");
            
            // Test Pascal reward calculations match exactly
            var quest = new Quest { Reward = 2, RewardType = QuestRewardType.Experience };
            var reward = quest.CalculateReward(10);
            Assert(reward == 5000, "Pascal exp calculation: level * 500");
            
            quest.RewardType = QuestRewardType.Money;
            reward = quest.CalculateReward(10);
            Assert(reward == 51000, "Pascal gold calculation: level * 5100");
            
            terminal.WriteLine("✅ Pascal reward calculations", "green");
            
        }
        catch (Exception ex)
        {
            TestFailed($"Pascal compatibility failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Assert helper with test counting
    /// </summary>
    private static void Assert(bool condition, string message)
    {
        testsRun++;
        if (condition)
        {
            testsPassed++;
        }
        else
        {
            terminal.WriteLine($"❌ ASSERTION FAILED: {message}", "red");
            throw new Exception($"Assertion failed: {message}");
        }
    }
    
    /// <summary>
    /// Handle test failure
    /// </summary>
    private static void TestFailed(string message)
    {
        terminal.WriteLine($"❌ TEST FAILED: {message}", "red");
    }
}

/// <summary>
/// Test helper methods for quest system testing
/// </summary>
public static class TestHelpers
{
    public static Character CreateTestKing()
    {
        var king = new Character
        {
            Name2 = "TestKing",
            King = true,
            Level = 20,
            QuestsLeft = 5
        };
        return king;
    }
    
    public static Character CreateTestPlayer()
    {
        var player = new Character
        {
            Name2 = "TestPlayer",
            King = false,
            Level = 10,
            Race = CharacterRace.Human,
            Sex = 1,
            RoyQuests = 0,
            RoyQuestsToday = 0,
            MKills = 0,
            Experience = 1000
        };
        return player;
    }
} 
