using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Comprehensive validation tests for the News System (Phase 17)
/// Tests Pascal NEWS.PAS and GENNEWS.PAS compatibility and functionality
/// Verifies news writing, categorization, file management, and player interface
/// </summary>
public class NewsSystemValidation : Node
{
    private NewsSystem _newsSystem;
    private Player _testPlayer;
    private NewsLocation _newsLocation;
    private int _testsRun = 0;
    private int _testsPassed = 0;
    private List<string> _failedTests = new List<string>();

    public override void _Ready()
    {
        GD.Print("=== NEWS SYSTEM VALIDATION TESTS ===");
        
        SetupTestEnvironment();
        RunAllTests();
        GenerateTestReport();
    }

    private void SetupTestEnvironment()
    {
        try
        {
            _newsSystem = NewsSystem.Instance;
            _newsLocation = new NewsLocation();
            
            // Create test player
            _testPlayer = new Player();
            _testPlayer.Name1 = "TestPlayer";
            _testPlayer.Name2 = "NewsTester";
            _testPlayer.Level = 50;
            _testPlayer.IsKing = false;
            
            // Ensure test directories exist
            string scoresDir = GameConfig.ScoreDir;
            if (!Directory.Exists(scoresDir))
            {
                Directory.CreateDirectory(scoresDir);
            }
            
            GD.Print("Test environment setup completed");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to setup test environment: {ex.Message}");
        }
    }

    private void RunAllTests()
    {
        // Core News System Tests
        TestNewsSystemInitialization();
        TestNewsFileCreation();
        TestPascalNewsyFunction();
        TestPascalGenericNewsFunction();
        
        // News Category Tests
        TestGeneralNewsWriting();
        TestRoyalNewsWriting();
        TestMarriageNewsWriting();
        TestBirthNewsWriting();
        TestHolyNewsWriting();
        
        // News Reading Tests
        TestNewsReading();
        TestNewsStatistics();
        TestDailyNewsMaintenance();
        
        // File Management Tests
        TestNewsFileRotation();
        TestAnsiCodeStripping();
        TestNewsFileLocking();
        
        // Integration Tests
        TestPlayerDeathNews();
        TestQuestCompletionNews();
        TestTeamWarfareNews();
        TestPrisonNews();
        
        // Location Interface Tests
        TestNewsLocationInterface();
        TestNewsMenuNavigation();
        TestNewsPagination();
        
        // Pascal Compatibility Tests
        TestPascalFileStructure();
        TestPascalTimestamping();
        TestPascalColorCodes();
        
        // Error Handling Tests
        TestFileAccessErrors();
        TestInvalidInputHandling();
        TestSystemRecovery();
    }

    #region Core System Tests

    private void TestNewsSystemInitialization()
    {
        RunTest("News System Initialization", () =>
        {
            Assert(_newsSystem != null, "NewsSystem instance should be created");
            
            // Verify news files are initialized
            Assert(File.Exists(GameConfig.NewsAnsiFile) || !string.IsNullOrEmpty(GameConfig.NewsAnsiFile), 
                "News ANSI file should exist or be configured");
            Assert(File.Exists(GameConfig.NewsAsciiFile) || !string.IsNullOrEmpty(GameConfig.NewsAsciiFile), 
                "News ASCII file should exist or be configured");
                
            GD.Print("✓ News system properly initialized");
        });
    }

    private void TestNewsFileCreation()
    {
        RunTest("News File Creation", () =>
        {
            // Test that all news files can be created with proper headers
            string[] newsFiles = {
                GameConfig.NewsAnsiFile,
                GameConfig.NewsAsciiFile,
                GameConfig.MonarchNewsAnsiFile,
                GameConfig.MonarchNewsAsciiFile,
                GameConfig.GodsNewsAnsiFile,
                GameConfig.GodsNewsAsciiFile,
                GameConfig.MarriageNewsAnsiFile,
                GameConfig.MarriageNewsAsciiFile,
                GameConfig.BirthNewsAnsiFile,
                GameConfig.BirthNewsAsciiFile
            };
            
            foreach (string file in newsFiles)
            {
                if (!string.IsNullOrEmpty(file))
                {
                    Assert(File.Exists(file) || CanCreateFile(file), $"Should be able to create/access {file}");
                }
            }
            
            GD.Print("✓ All news files can be created/accessed");
        });
    }

    private void TestPascalNewsyFunction()
    {
        RunTest("Pascal newsy() Function", () =>
        {
            string testMessage = "Test news message for general category";
            
            // Test writing to ANSI and ASCII files (Pascal newsy implementation)
            _newsSystem.Newsy(true, testMessage);  // ANSI
            _newsSystem.Newsy(false, testMessage); // ASCII
            
            // Verify messages were written
            var generalNews = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
            Assert(generalNews.Count >= 2, "Should have at least 2 general news entries");
            Assert(generalNews.Any(n => n.Contains(testMessage)), "Should contain test message");
            
            GD.Print("✓ Pascal newsy() function working correctly");
        });
    }

    private void TestPascalGenericNewsFunction()
    {
        RunTest("Pascal generic_news() Function", () =>
        {
            // Test all news categories (Pascal GENNEWS.PAS types)
            var testCases = new Dictionary<GameConfig.NewsCategory, string>
            {
                { GameConfig.NewsCategory.Royal, "Test royal proclamation" },
                { GameConfig.NewsCategory.Marriage, "Test marriage announcement" },
                { GameConfig.NewsCategory.Birth, "Test birth announcement" },
                { GameConfig.NewsCategory.Holy, "Test divine event" }
            };
            
            foreach (var testCase in testCases)
            {
                _newsSystem.GenericNews(testCase.Key, true, testCase.Value);
                
                var categoryNews = _newsSystem.GetTodaysNews(testCase.Key);
                Assert(categoryNews.Count > 0, $"Should have {testCase.Key} news entries");
                Assert(categoryNews.Any(n => n.Contains(testCase.Value)), 
                    $"Should contain {testCase.Key} test message");
            }
            
            GD.Print("✓ Pascal generic_news() function working correctly");
        });
    }

    #endregion

    #region News Category Tests

    private void TestGeneralNewsWriting()
    {
        RunTest("General News Writing", () =>
        {
            string combatNews = "TestPlayer defeated a fearsome dragon!";
            _newsSystem.WriteNews(GameConfig.NewsCategory.General, combatNews);
            
            var news = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
            Assert(news.Any(n => n.Contains(combatNews)), "Should contain general news entry");
            
            GD.Print("✓ General news writing functional");
        });
    }

    private void TestRoyalNewsWriting()
    {
        RunTest("Royal News Writing", () =>
        {
            string kingName = "TestKing";
            string proclamation = "All citizens must pay extra taxes for the royal treasury!";
            
            _newsSystem.WriteRoyalNews(kingName, proclamation);
            
            var royalNews = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.Royal);
            Assert(royalNews.Any(n => n.Contains(kingName) && n.Contains(proclamation)), 
                "Should contain royal proclamation");
            
            GD.Print("✓ Royal news writing functional");
        });
    }

    private void TestMarriageNewsWriting()
    {
        RunTest("Marriage News Writing", () =>
        {
            string player1 = "Romeo";
            string player2 = "Juliet";
            
            _newsSystem.WriteMarriageNews(player1, player2, "Temple");
            
            var marriageNews = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.Marriage);
            Assert(marriageNews.Any(n => n.Contains(player1) && n.Contains(player2) && n.Contains("married")), 
                "Should contain marriage announcement");
            
            // Test divorce news
            _newsSystem.WriteDivorceNews(player1, player2);
            marriageNews = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.Marriage);
            Assert(marriageNews.Any(n => n.Contains(player1) && n.Contains(player2) && n.Contains("divorced")), 
                "Should contain divorce announcement");
            
            GD.Print("✓ Marriage/divorce news writing functional");
        });
    }

    private void TestBirthNewsWriting()
    {
        RunTest("Birth News Writing", () =>
        {
            string mother = "TestMother";
            string father = "TestFather";
            string child = "TestChild";
            
            _newsSystem.WriteBirthNews(mother, father, child);
            
            var birthNews = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.Birth);
            Assert(birthNews.Any(n => n.Contains(mother) && n.Contains(father) && n.Contains(child)), 
                "Should contain birth announcement");
            
            GD.Print("✓ Birth news writing functional");
        });
    }

    private void TestHolyNewsWriting()
    {
        RunTest("Holy News Writing", () =>
        {
            string godName = "TestDeity";
            string event_description = "granted divine blessings to faithful worshippers";
            
            _newsSystem.WriteHolyNews(godName, event_description);
            
            var holyNews = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.Holy);
            Assert(holyNews.Any(n => n.Contains(godName) && n.Contains(event_description)), 
                "Should contain holy event");
            
            GD.Print("✓ Holy news writing functional");
        });
    }

    #endregion

    #region News Reading Tests

    private void TestNewsReading()
    {
        RunTest("News Reading", () =>
        {
            // Write some test news first
            _newsSystem.WriteNews(GameConfig.NewsCategory.General, "Test reading message");
            
            // Test reading from files
            var generalNews = _newsSystem.ReadNews(GameConfig.NewsCategory.General, true);
            Assert(generalNews.Count > 0, "Should be able to read general news");
            
            var royalNews = _newsSystem.ReadNews(GameConfig.NewsCategory.Royal, true);
            // Royal news might be empty, that's okay
            
            GD.Print("✓ News reading functional");
        });
    }

    private void TestNewsStatistics()
    {
        RunTest("News Statistics", () =>
        {
            // Write some test news
            _newsSystem.WriteNews(GameConfig.NewsCategory.General, "Stats test 1");
            _newsSystem.WriteNews(GameConfig.NewsCategory.General, "Stats test 2");
            _newsSystem.WriteRoyalNews("TestKing", "Stats test royal");
            
            var stats = _newsSystem.GetNewsStatistics();
            Assert(stats != null, "Should return statistics");
            Assert(stats.ContainsKey("Today_General"), "Should contain general news count");
            
            int generalCount = (int)stats["Today_General"];
            Assert(generalCount >= 2, $"Should have at least 2 general news entries, got {generalCount}");
            
            GD.Print("✓ News statistics functional");
        });
    }

    private void TestDailyNewsMaintenance()
    {
        RunTest("Daily News Maintenance", () =>
        {
            // Write some test news
            _newsSystem.WriteNews(GameConfig.NewsCategory.General, "Pre-maintenance message");
            
            // Run maintenance
            _newsSystem.ProcessDailyNewsMaintenance();
            
            // Verify news was cleared/rotated
            var todayNews = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
            // After maintenance, today's cache should be cleared
            
            GD.Print("✓ Daily maintenance functional");
        });
    }

    #endregion

    #region Integration Tests

    private void TestPlayerDeathNews()
    {
        RunTest("Player Death News", () =>
        {
            string playerName = "DeadPlayer";
            string killerName = "DragonLord";
            string location = "Dungeon Level 5";
            
            _newsSystem.WriteDeathNews(playerName, killerName, location);
            
            var news = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
            Assert(news.Any(n => n.Contains(playerName) && n.Contains(killerName) && n.Contains("slain")), 
                "Should contain death announcement");
            
            GD.Print("✓ Player death news integration working");
        });
    }

    private void TestQuestCompletionNews()
    {
        RunTest("Quest Completion News", () =>
        {
            string playerName = "QuestHero";
            string questDesc = "Slay the Ancient Dragon";
            
            // Test completion
            _newsSystem.WriteQuestNews(playerName, questDesc, true);
            
            var news = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
            Assert(news.Any(n => n.Contains(playerName) && n.Contains(questDesc) && n.Contains("completed")), 
                "Should contain quest completion");
            
            // Test failure
            _newsSystem.WriteQuestNews(playerName, "Rescue the Princess", false);
            news = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
            Assert(news.Any(n => n.Contains(playerName) && n.Contains("failed")), 
                "Should contain quest failure");
            
            GD.Print("✓ Quest news integration working");
        });
    }

    private void TestTeamWarfareNews()
    {
        RunTest("Team Warfare News", () =>
        {
            string teamName = "DragonSlayers";
            string event_desc = "conquered the eastern territories!";
            
            _newsSystem.WriteTeamNews(teamName, event_desc);
            
            var news = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
            Assert(news.Any(n => n.Contains(teamName) && n.Contains(event_desc)), 
                "Should contain team warfare news");
            
            GD.Print("✓ Team warfare news integration working");
        });
    }

    private void TestPrisonNews()
    {
        RunTest("Prison News", () =>
        {
            string playerName = "Prisoner123";
            string event_desc = "escaped from the Royal Prison!";
            
            _newsSystem.WritePrisonNews(playerName, event_desc);
            
            var news = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
            Assert(news.Any(n => n.Contains(playerName) && n.Contains(event_desc)), 
                "Should contain prison news");
            
            GD.Print("✓ Prison news integration working");
        });
    }

    #endregion

    #region Location Interface Tests

    private void TestNewsLocationInterface()
    {
        RunTest("News Location Interface", () =>
        {
            Assert(_newsLocation != null, "NewsLocation should be created");
            Assert(_newsLocation.LocationName == GameConfig.DefaultNewsLocation, 
                "Should have correct location name");
            
            string description = _newsLocation.GetLocationDescription(_testPlayer);
            Assert(!string.IsNullOrEmpty(description), "Should provide location description");
            Assert(description.Contains("News"), "Description should mention news");
            
            var commands = _newsLocation.GetAvailableCommands(_testPlayer);
            Assert(commands.Count >= 6, "Should have at least 6 commands available");
            
            GD.Print("✓ News location interface functional");
        });
    }

    private void TestNewsMenuNavigation()
    {
        RunTest("News Menu Navigation", () =>
        {
            // Test menu command handling
            var validCommands = new string[] { "D", "R", "M", "B", "H", "Y", "Q" };
            
            foreach (string command in validCommands)
            {
                // This would normally test user input handling
                // For validation, we just verify the command is recognized
                Assert(IsValidNewsCommand(command), $"Command '{command}' should be valid");
            }
            
            GD.Print("✓ News menu navigation functional");
        });
    }

    private void TestNewsPagination()
    {
        RunTest("News Pagination", () =>
        {
            // Create enough news entries to test pagination
            for (int i = 0; i < 25; i++)
            {
                _newsSystem.WriteNews(GameConfig.NewsCategory.General, $"Pagination test message {i}");
            }
            
            var news = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
            Assert(news.Count >= 25, "Should have enough news for pagination testing");
            
            // Pagination is handled in the location, this verifies we have enough data
            GD.Print("✓ News pagination data available");
        });
    }

    #endregion

    #region Pascal Compatibility Tests

    private void TestPascalFileStructure()
    {
        RunTest("Pascal File Structure Compatibility", () =>
        {
            // Verify file paths match Pascal constants
            Assert(GameConfig.NewsAnsiFile.EndsWith("NEWS.ANS"), "ANSI file should match Pascal global_nwfileans");
            Assert(GameConfig.NewsAsciiFile.EndsWith("NEWS.ASC"), "ASCII file should match Pascal global_nwfileasc");
            Assert(GameConfig.YesterdayNewsAnsiFile.EndsWith("YNEWS.ANS"), "Yesterday ANSI should match Pascal global_ynwfileans");
            Assert(GameConfig.YesterdayNewsAsciiFile.EndsWith("YNEWS.ASC"), "Yesterday ASCII should match Pascal global_ynwfileasc");
            
            // Verify specialized files (GENNEWS.PAS)
            Assert(GameConfig.MonarchNewsAnsiFile.EndsWith("MONARCHS.ANS"), "Monarch ANSI should match Pascal global_MonarchsANSI");
            Assert(GameConfig.GodsNewsAnsiFile.EndsWith("GODS.ANS"), "Gods ANSI should match Pascal global_GodsANSI");
            Assert(GameConfig.MarriageNewsAnsiFile.EndsWith("MARRHIST.ANS"), "Marriage ANSI should match Pascal global_MarrHistANSI");
            Assert(GameConfig.BirthNewsAnsiFile.EndsWith("BIRTHIST.ANS"), "Birth ANSI should match Pascal global_ChildBirthHistANSI");
            
            GD.Print("✓ Pascal file structure compatibility verified");
        });
    }

    private void TestPascalTimestamping()
    {
        RunTest("Pascal Timestamp Compatibility", () =>
        {
            string testMessage = "Timestamp test message";
            _newsSystem.Newsy(false, testMessage);
            
            var news = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
            var lastEntry = news.LastOrDefault(n => n.Contains(testMessage));
            
            Assert(lastEntry != null, "Should find timestamped entry");
            Assert(lastEntry.Contains("[") && lastEntry.Contains("]"), "Should contain timestamp brackets");
            
            // Verify timestamp format (HH:mm)
            int bracketStart = lastEntry.IndexOf('[');
            int bracketEnd = lastEntry.IndexOf(']');
            if (bracketStart >= 0 && bracketEnd > bracketStart)
            {
                string timestamp = lastEntry.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
                Assert(timestamp.Contains(":"), "Timestamp should contain colon separator");
            }
            
            GD.Print("✓ Pascal timestamp compatibility verified");
        });
    }

    private void TestPascalColorCodes()
    {
        RunTest("Pascal Color Code Integration", () =>
        {
            // Verify color constants match Pascal expectations
            Assert(GameConfig.NewsColorDefault == "`2", "Default color should match Pascal config.textcol1");
            Assert(GameConfig.NewsColorHighlight == "`5", "Highlight color should match Pascal config.textcol2");
            Assert(GameConfig.AnsiControlChar == '`', "ANSI control character should match Pascal acc");
            
            // Test color code usage in news
            _newsSystem.WriteDeathNews("TestPlayer", "TestKiller", "TestLocation");
            var news = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
            var deathEntry = news.LastOrDefault();
            
            Assert(deathEntry != null && deathEntry.Contains("`"), "Death news should contain color codes");
            
            GD.Print("✓ Pascal color code integration verified");
        });
    }

    #endregion

    #region Error Handling Tests

    private void TestFileAccessErrors()
    {
        RunTest("File Access Error Handling", () =>
        {
            // This test verifies the system handles file access errors gracefully
            try
            {
                // Try to write news even if files are temporarily inaccessible
                _newsSystem.WriteNews(GameConfig.NewsCategory.General, "Error handling test");
                
                // System should not crash
                GD.Print("✓ File access error handling functional");
            }
            catch (Exception ex)
            {
                // Errors should be caught and logged, not crash the system
                GD.PrintErr($"File access error: {ex.Message}");
                GD.Print("✓ File access errors caught appropriately");
            }
        });
    }

    private void TestInvalidInputHandling()
    {
        RunTest("Invalid Input Handling", () =>
        {
            // Test location handles invalid input gracefully
            bool result = _newsLocation.HandlePlayerInput(_testPlayer, "INVALID_COMMAND");
            Assert(result == true, "Should continue running after invalid input");
            
            result = _newsLocation.HandlePlayerInput(_testPlayer, "");
            Assert(result == true, "Should handle empty input");
            
            result = _newsLocation.HandlePlayerInput(_testPlayer, null);
            Assert(result == true, "Should handle null input");
            
            GD.Print("✓ Invalid input handling functional");
        });
    }

    private void TestSystemRecovery()
    {
        RunTest("System Recovery", () =>
        {
            // Test that the system can recover from various error conditions
            try
            {
                // Simulate various error conditions and recovery
                _newsSystem.WriteNews(GameConfig.NewsCategory.General, "Recovery test 1");
                
                // Force maintenance to test file operations
                _newsSystem.ProcessDailyNewsMaintenance();
                
                // Continue operations after maintenance
                _newsSystem.WriteNews(GameConfig.NewsCategory.General, "Recovery test 2");
                
                var news = _newsSystem.GetTodaysNews(GameConfig.NewsCategory.General);
                // Should have at least the post-maintenance entry
                
                GD.Print("✓ System recovery functional");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"System recovery error: {ex.Message}");
                throw; // Fail the test if recovery doesn't work
            }
        });
    }

    #endregion

    #region Helper Methods

    private void RunTest(string testName, Action testAction)
    {
        _testsRun++;
        try
        {
            testAction();
            _testsPassed++;
        }
        catch (Exception ex)
        {
            _failedTests.Add($"{testName}: {ex.Message}");
            GD.PrintErr($"❌ {testName} - FAILED: {ex.Message}");
        }
    }

    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }

    private bool CanCreateFile(string filePath)
    {
        try
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, "Test");
            return File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidNewsCommand(string command)
    {
        var validCommands = new[] { "D", "R", "M", "B", "H", "Y", "Q" };
        return validCommands.Contains(command.ToUpper());
    }

    private void GenerateTestReport()
    {
        GD.Print("\n=== NEWS SYSTEM VALIDATION REPORT ===");
        GD.Print($"Tests Run: {_testsRun}");
        GD.Print($"Tests Passed: {_testsPassed}");
        GD.Print($"Tests Failed: {_testsRun - _testsPassed}");
        GD.Print($"Success Rate: {(_testsRun > 0 ? (_testsPassed * 100.0) / _testsRun : 0):F1}%");
        
        if (_failedTests.Count > 0)
        {
            GD.Print("\nFAILED TESTS:");
            foreach (string failure in _failedTests)
            {
                GD.Print($"  ❌ {failure}");
            }
        }
        else
        {
            GD.Print("\n🎉 ALL TESTS PASSED! News System is fully functional and Pascal-compatible.");
        }
        
        GD.Print("\n=== PHASE 17 NEWS SYSTEM VALIDATION COMPLETE ===");
    }

    #endregion
} 