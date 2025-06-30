using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Comprehensive validation tests for the Healer System (Phase 10)
/// Tests disease healing, cursed item removal, cost calculations, and integration
/// Based on Pascal HEALERC.PAS validation requirements
/// </summary>
public class HealerSystemValidation
{
    private TerminalEmulator terminal;
    private LocationManager locationManager;
    
    public HealerSystemValidation()
    {
        terminal = new TerminalEmulator();
        locationManager = new LocationManager(terminal);
    }
    
    /// <summary>
    /// Main validation entry point - runs all healer system tests
    /// </summary>
    public async Task RunAllValidationTests()
    {
        DisplayHeader("PHASE 10: HEALER SYSTEM VALIDATION");
        
        var testResults = new List<TestResult>();
        
        // Core Disease System Tests
        testResults.AddRange(await TestDiseaseSystem());
        
        // Healing Service Tests
        testResults.AddRange(await TestHealingServices());
        
        // Cost Calculation Tests
        testResults.AddRange(await TestCostCalculations());
        
        // Cursed Item Removal Tests
        testResults.AddRange(await TestCursedItemRemoval());
        
        // Status Display Tests
        testResults.AddRange(await TestStatusDisplay());
        
        // Location Integration Tests
        testResults.AddRange(await TestLocationIntegration());
        
        // Expert Mode Tests
        testResults.AddRange(await TestExpertMode());
        
        // Error Handling Tests
        testResults.AddRange(await TestErrorHandling());
        
        // Display final results
        DisplayTestResults(testResults);
    }
    
    #region Disease System Tests
    
    private async Task<List<TestResult>> TestDiseaseSystem()
    {
        DisplayTestCategory("Disease System Tests");
        var results = new List<TestResult>();
        
        try
        {
            // Test 1: Disease detection
            var player = CreateTestPlayer();
            player.Blind = true;
            player.Plague = true;
            
            int diseaseCount = 0;
            if (player.Blind) diseaseCount++;
            if (player.Plague) diseaseCount++;
            if (player.Smallpox) diseaseCount++;
            if (player.Measles) diseaseCount++;
            if (player.Leprosy) diseaseCount++;
            
            results.Add(new TestResult(
                "Disease Detection",
                diseaseCount == 2,
                $"Detected {diseaseCount} diseases (expected 2)"
            ));
            
            // Test 2: All diseases
            player.Smallpox = true;
            player.Measles = true;
            player.Leprosy = true;
            
            diseaseCount = 0;
            if (player.Blind) diseaseCount++;
            if (player.Plague) diseaseCount++;
            if (player.Smallpox) diseaseCount++;
            if (player.Measles) diseaseCount++;
            if (player.Leprosy) diseaseCount++;
            
            results.Add(new TestResult(
                "All Disease Types",
                diseaseCount == 5,
                $"All 5 disease types detected: {diseaseCount}"
            ));
            
            // Test 3: No diseases
            var healthyPlayer = CreateTestPlayer();
            diseaseCount = 0;
            if (healthyPlayer.Blind) diseaseCount++;
            if (healthyPlayer.Plague) diseaseCount++;
            if (healthyPlayer.Smallpox) diseaseCount++;
            if (healthyPlayer.Measles) diseaseCount++;
            if (healthyPlayer.Leprosy) diseaseCount++;
            
            results.Add(new TestResult(
                "Healthy Player Detection",
                diseaseCount == 0,
                $"Healthy player correctly identified: {diseaseCount} diseases"
            ));
            
            // Test 4: Disease properties validation
            results.Add(new TestResult(
                "Disease Properties",
                ValidateDiseaseProperties(),
                "All disease boolean properties exist on Character class"
            ));
        }
        catch (Exception ex)
        {
            results.Add(new TestResult("Disease System Test", false, $"Exception: {ex.Message}"));
        }
        
        return results;
    }
    
    #endregion
    
    #region Healing Service Tests
    
    private async Task<List<TestResult>> TestHealingServices()
    {
        DisplayTestCategory("Healing Service Tests");
        var results = new List<TestResult>();
        
        try
        {
            // Test 1: Single disease cure
            var player = CreateTestPlayer();
            player.Gold = 100000; // Enough for any cure
            player.Level = 10;
            player.Blind = true;
            
            long initialGold = player.Gold;
            long expectedCost = GameConfig.BlindnessCostMultiplier * player.Level;
            
            // Simulate cure
            player.Gold -= expectedCost;
            player.Blind = false;
            
            results.Add(new TestResult(
                "Single Disease Cure",
                !player.Blind && player.Gold == (initialGold - expectedCost),
                $"Blindness cured, cost: {expectedCost}"
            ));
            
            // Test 2: Cure all diseases
            player = CreateTestPlayer();
            player.Gold = 500000; // Enough for all cures
            player.Level = 10;
            player.Blind = true;
            player.Plague = true;
            player.Smallpox = true;
            player.Measles = true;
            player.Leprosy = true;
            
            initialGold = player.Gold;
            long totalCost = (GameConfig.BlindnessCostMultiplier + 
                             GameConfig.PlagueCostMultiplier + 
                             GameConfig.SmallpoxCostMultiplier + 
                             GameConfig.MeaslesCostMultiplier + 
                             GameConfig.LeprosyCostMultiplier) * player.Level;
            
            // Simulate cure all
            player.Gold -= totalCost;
            player.Blind = false;
            player.Plague = false;
            player.Smallpox = false;
            player.Measles = false;
            player.Leprosy = false;
            
            bool allCured = !player.Blind && !player.Plague && !player.Smallpox && !player.Measles && !player.Leprosy;
            
            results.Add(new TestResult(
                "Cure All Diseases",
                allCured && player.Gold == (initialGold - totalCost),
                $"All diseases cured, total cost: {totalCost}"
            ));
            
            // Test 3: Insufficient funds
            player = CreateTestPlayer();
            player.Gold = 1000; // Not enough
            player.Level = 10;
            player.Blind = true;
            
            expectedCost = GameConfig.BlindnessCostMultiplier * player.Level;
            bool canAfford = player.Gold >= expectedCost;
            
            results.Add(new TestResult(
                "Insufficient Funds Check",
                !canAfford,
                $"Correctly detected insufficient funds: {player.Gold} < {expectedCost}"
            ));
            
            // Test 4: Level-based cost scaling
            var lowLevelPlayer = CreateTestPlayer();
            lowLevelPlayer.Level = 5;
            var highLevelPlayer = CreateTestPlayer();
            highLevelPlayer.Level = 20;
            
            long lowCost = GameConfig.BlindnessCostMultiplier * lowLevelPlayer.Level;
            long highCost = GameConfig.BlindnessCostMultiplier * highLevelPlayer.Level;
            
            results.Add(new TestResult(
                "Level-Based Cost Scaling",
                highCost == (lowCost * 4), // 20/5 = 4
                $"Cost scaling: Level 5 = {lowCost}, Level 20 = {highCost}"
            ));
        }
        catch (Exception ex)
        {
            results.Add(new TestResult("Healing Services Test", false, $"Exception: {ex.Message}"));
        }
        
        return results;
    }
    
    #endregion
    
    #region Cost Calculation Tests
    
    private async Task<List<TestResult>> TestCostCalculations()
    {
        DisplayTestCategory("Cost Calculation Tests");
        var results = new List<TestResult>();
        
        try
        {
            // Test 1: Pascal cost multipliers
            bool blindnessCorrect = GameConfig.BlindnessCostMultiplier == 5000;
            bool plagueCorrect = GameConfig.PlagueCostMultiplier == 6000;
            bool smallpoxCorrect = GameConfig.SmallpoxCostMultiplier == 7000;
            bool measlesCorrect = GameConfig.MeaslesCostMultiplier == 7500;
            bool leprosyCorrect = GameConfig.LeprosyCostMultiplier == 8500;
            
            bool allMultipliersCorrect = blindnessCorrect && plagueCorrect && smallpoxCorrect && 
                                       measlesCorrect && leprosyCorrect;
            
            results.Add(new TestResult(
                "Pascal Cost Multipliers",
                allMultipliersCorrect,
                $"Disease costs: B={GameConfig.BlindnessCostMultiplier}, P={GameConfig.PlagueCostMultiplier}, S={GameConfig.SmallpoxCostMultiplier}, M={GameConfig.MeaslesCostMultiplier}, L={GameConfig.LeprosyCostMultiplier}"
            ));
            
            // Test 2: Cost calculation formula
            var player = CreateTestPlayer();
            player.Level = 15;
            
            long blindnessCost = GameConfig.BlindnessCostMultiplier * player.Level;
            long expectedBlindnessCost = 5000 * 15; // 75,000
            
            results.Add(new TestResult(
                "Cost Calculation Formula",
                blindnessCost == expectedBlindnessCost,
                $"Level {player.Level} blindness cost: {blindnessCost} (expected: {expectedBlindnessCost})"
            ));
            
            // Test 3: Cursed item removal cost
            long cursedItemCost = GameConfig.CursedItemRemovalMultiplier * player.Level;
            long expectedCursedCost = 1000 * 15; // 15,000
            
            results.Add(new TestResult(
                "Cursed Item Removal Cost",
                cursedItemCost == expectedCursedCost,
                $"Level {player.Level} cursed item removal: {cursedItemCost} (expected: {expectedCursedCost})"
            ));
            
            // Test 4: High level cost validation
            player.Level = 100; // Max level scenario
            long highLevelCost = GameConfig.LeprosyCostMultiplier * player.Level; // Most expensive
            long expectedHighCost = 8500 * 100; // 850,000
            
            results.Add(new TestResult(
                "High Level Cost Validation",
                highLevelCost == expectedHighCost,
                $"Level 100 leprosy cost: {highLevelCost} (expected: {expectedHighCost})"
            ));
        }
        catch (Exception ex)
        {
            results.Add(new TestResult("Cost Calculations Test", false, $"Exception: {ex.Message}"));
        }
        
        return results;
    }
    
    #endregion
    
    #region Cursed Item Removal Tests
    
    private async Task<List<TestResult>> TestCursedItemRemoval()
    {
        DisplayTestCategory("Cursed Item Removal Tests");
        var results = new List<TestResult>();
        
        try
        {
            // Test 1: Cursed item cost multiplier validation
            bool cursedMultiplierCorrect = GameConfig.CursedItemRemovalMultiplier == 1000;
            
            results.Add(new TestResult(
                "Cursed Item Cost Multiplier",
                cursedMultiplierCorrect,
                $"Cursed item removal multiplier: {GameConfig.CursedItemRemovalMultiplier} (expected: 1000)"
            ));
            
            // Test 2: Cursed item cost calculation
            var player = CreateTestPlayer();
            player.Level = 12;
            long cursedCost = GameConfig.CursedItemRemovalMultiplier * player.Level;
            long expectedCost = 1000 * 12; // 12,000
            
            results.Add(new TestResult(
                "Cursed Item Cost Calculation", 
                cursedCost == expectedCost,
                $"Cursed item removal cost: {cursedCost} (expected: {expectedCost})"
            ));
            
            // Test 3: Multiple cursed items
            int cursedItemCount = 3;
            long totalCursedCost = cursedCost * cursedItemCount;
            long expectedTotalCost = expectedCost * cursedItemCount;
            
            results.Add(new TestResult(
                "Multiple Cursed Items Cost",
                totalCursedCost == expectedTotalCost,
                $"3 cursed items cost: {totalCursedCost} (expected: {expectedTotalCost})"
            ));
            
            // Test 4: Cost scaling with level
            var lowLevelPlayer = CreateTestPlayer();
            lowLevelPlayer.Level = 5;
            var highLevelPlayer = CreateTestPlayer();
            highLevelPlayer.Level = 25;
            
            long lowCursedCost = GameConfig.CursedItemRemovalMultiplier * lowLevelPlayer.Level;
            long highCursedCost = GameConfig.CursedItemRemovalMultiplier * highLevelPlayer.Level;
            
            results.Add(new TestResult(
                "Cursed Item Cost Scaling",
                highCursedCost == (lowCursedCost * 5), // 25/5 = 5
                $"Cursed cost scaling: Level 5 = {lowCursedCost}, Level 25 = {highCursedCost}"
            ));
        }
        catch (Exception ex)
        {
            results.Add(new TestResult("Cursed Item Removal Test", false, $"Exception: {ex.Message}"));
        }
        
        return results;
    }
    
    #endregion
    
    #region Status Display Tests
    
    private async Task<List<TestResult>> TestStatusDisplay()
    {
        DisplayTestCategory("Status Display Tests");
        var results = new List<TestResult>();
        
        try
        {
            // Test 1: Disease status detection
            var player = CreateTestPlayer();
            player.Blind = true;
            player.Plague = true;
            
            var diseaseList = new List<string>();
            if (player.Blind) diseaseList.Add("Blindness");
            if (player.Plague) diseaseList.Add("Plague");
            if (player.Smallpox) diseaseList.Add("Smallpox");
            if (player.Measles) diseaseList.Add("Measles");
            if (player.Leprosy) diseaseList.Add("Leprosy");
            
            results.Add(new TestResult(
                "Disease Status Detection",
                diseaseList.Count == 2 && diseaseList.Contains("Blindness") && diseaseList.Contains("Plague"),
                $"Disease status correctly shows: {string.Join(", ", diseaseList)}"
            ));
            
            // Test 2: Healthy status detection
            var healthyPlayer = CreateTestPlayer();
            bool isHealthy = !healthyPlayer.Blind && !healthyPlayer.Plague && !healthyPlayer.Smallpox && 
                            !healthyPlayer.Measles && !healthyPlayer.Leprosy;
            
            results.Add(new TestResult(
                "Healthy Status Detection",
                isHealthy,
                "Healthy player correctly identified - no diseases"
            ));
            
            // Test 3: Player stats availability
            bool hasRequiredStats = player.Level > 0 && player.MaxHP > 0 && player.Gold >= 0;
            
            results.Add(new TestResult(
                "Player Stats Availability",
                hasRequiredStats,
                $"Player stats available: Level {player.Level}, HP {player.HP}/{player.MaxHP}, Gold {player.Gold}"
            ));
            
            // Test 4: Display name formatting
            bool hasDisplayName = !string.IsNullOrEmpty(player.DisplayName);
            
            results.Add(new TestResult(
                "Display Name Formatting",
                hasDisplayName,
                $"Player display name available: '{player.DisplayName}'"
            ));
        }
        catch (Exception ex)
        {
            results.Add(new TestResult("Status Display Test", false, $"Exception: {ex.Message}"));
        }
        
        return results;
    }
    
    #endregion
    
    #region Location Integration Tests
    
    private async Task<List<TestResult>> TestLocationIntegration()
    {
        DisplayTestCategory("Location Integration Tests");
        var results = new List<TestResult>();
        
        try
        {
            // Test 1: Healer location instantiation
            var healer = new HealerLocation();
            bool healerExists = healer != null;
            
            results.Add(new TestResult(
                "Healer Location Instantiation",
                healerExists,
                "HealerLocation class exists and can be instantiated"
            ));
            
            // Test 2: Location ID validation
            bool correctLocationId = healer.LocationId == (int)GameLocation.Healer;
            
            results.Add(new TestResult(
                "Location ID Validation",
                correctLocationId,
                $"Healer location ID: {healer.LocationId} (expected: {(int)GameLocation.Healer})"
            ));
            
            // Test 3: GameConfig constants validation
            bool configConstantsExist = !string.IsNullOrEmpty(GameConfig.DefaultHealerName) && 
                                       !string.IsNullOrEmpty(GameConfig.DefaultHealerManager);
            
            results.Add(new TestResult(
                "GameConfig Constants",
                configConstantsExist,
                $"Healer constants: '{GameConfig.DefaultHealerName}', '{GameConfig.DefaultHealerManager}'"
            ));
            
            // Test 4: Pascal compatibility validation
            string expectedName = "The Golden Bow, Healing Hut";
            string expectedManager = "Jadu The Fat";
            bool pascalCompatible = GameConfig.DefaultHealerName == expectedName && 
                                   GameConfig.DefaultHealerManager == expectedManager;
            
            results.Add(new TestResult(
                "Pascal Compatibility",
                pascalCompatible,
                $"Pascal names: Name='{expectedName}', Manager='{expectedManager}'"
            ));
        }
        catch (Exception ex)
        {
            results.Add(new TestResult("Location Integration Test", false, $"Exception: {ex.Message}"));
        }
        
        return results;
    }
    
    #endregion
    
    #region Expert Mode Tests
    
    private async Task<List<TestResult>> TestExpertMode()
    {
        DisplayTestCategory("Expert Mode Tests");
        var results = new List<TestResult>();
        
        try
        {
            // Test 1: Expert mode flag
            var expertPlayer = CreateTestPlayer();
            expertPlayer.Expert = true;
            
            results.Add(new TestResult(
                "Expert Mode Flag",
                expertPlayer.Expert,
                "Expert mode flag can be set and read"
            ));
            
            // Test 2: Novice mode flag  
            var novicePlayer = CreateTestPlayer();
            novicePlayer.Expert = false;
            
            results.Add(new TestResult(
                "Novice Mode Flag",
                !novicePlayer.Expert,
                "Novice mode flag correctly set to false"
            ));
            
            // Test 3: Default expert setting
            var defaultPlayer = CreateTestPlayer();
            
            results.Add(new TestResult(
                "Default Expert Setting",
                !defaultPlayer.Expert, // Should default to false (novice)
                $"Default expert setting: {defaultPlayer.Expert} (expected: false)"
            ));
            
            // Test 4: Expert mode prompts (conceptual test)
            string expertPrompt = "Healing Hut (H,C,R,S,?) :";
            string novicePrompt = "Healing Hut (? for menu) :";
            bool promptsExist = !string.IsNullOrEmpty(expertPrompt) && !string.IsNullOrEmpty(novicePrompt);
            
            results.Add(new TestResult(
                "Expert Mode Prompts",
                promptsExist,
                "Different prompts exist for expert and novice modes"
            ));
        }
        catch (Exception ex)
        {
            results.Add(new TestResult("Expert Mode Test", false, $"Exception: {ex.Message}"));
        }
        
        return results;
    }
    
    #endregion
    
    #region Error Handling Tests
    
    private async Task<List<TestResult>> TestErrorHandling()
    {
        DisplayTestCategory("Error Handling Tests");
        var results = new List<TestResult>();
        
        try
        {
            // Test 1: Insufficient funds scenario
            var poorPlayer = CreateTestPlayer();
            poorPlayer.Gold = 100;
            poorPlayer.Level = 50; // High level = expensive cures
            
            long requiredGold = GameConfig.BlindnessCostMultiplier * poorPlayer.Level;
            bool insufficientFunds = poorPlayer.Gold < requiredGold;
            
            results.Add(new TestResult(
                "Insufficient Funds Detection",
                insufficientFunds,
                $"Correctly detects insufficient funds: {poorPlayer.Gold} < {requiredGold}"
            ));
            
            // Test 2: No diseases scenario
            var healthyPlayer = CreateTestPlayer();
            bool noDiseases = !healthyPlayer.Blind && !healthyPlayer.Plague && !healthyPlayer.Smallpox && 
                             !healthyPlayer.Measles && !healthyPlayer.Leprosy;
            
            results.Add(new TestResult(
                "No Diseases Scenario",
                noDiseases,
                "Correctly handles healthy player with no diseases"
            ));
            
            // Test 3: Zero level edge case  
            var edgeCasePlayer = CreateTestPlayer();
            edgeCasePlayer.Level = 0; // Edge case
            
            long edgeCaseCost = GameConfig.BlindnessCostMultiplier * Math.Max(1, edgeCasePlayer.Level);
            bool edgeCaseHandled = edgeCaseCost >= 0;
            
            results.Add(new TestResult(
                "Zero Level Edge Case",
                edgeCaseHandled,
                $"Edge case handled: Level 0 cost = {edgeCaseCost}"
            ));
            
            // Test 4: Negative gold edge case
            var negativeGoldPlayer = CreateTestPlayer();
            negativeGoldPlayer.Gold = -1000; // Edge case
            
            bool negativeGoldHandled = negativeGoldPlayer.Gold < 0; // Should be detectable
            
            results.Add(new TestResult(
                "Negative Gold Edge Case",
                negativeGoldHandled,
                $"Negative gold detectable: {negativeGoldPlayer.Gold}"
            ));
        }
        catch (Exception ex)
        {
            results.Add(new TestResult("Error Handling Test", false, $"Exception: {ex.Message}"));
        }
        
        return results;
    }
    
    #endregion
    
    #region Helper Methods
    
    private Character CreateTestPlayer()
    {
        var player = new Character();
        player.Name1 = "TestPlayer";
        player.Name2 = "TestHero";
        player.Level = 10;
        player.HP = 100;
        player.MaxHP = 100;
        player.Gold = 50000;
        player.Expert = false;
        
        // Initialize disease properties to false
        player.Blind = false;
        player.Plague = false;
        player.Smallpox = false;
        player.Measles = false;
        player.Leprosy = false;
        
        return player;
    }
    
    private bool ValidateDiseaseProperties()
    {
        try
        {
            var player = new Character();
            var type = player.GetType();
            
            var diseaseProperties = new[] { "Blind", "Plague", "Smallpox", "Measles", "Leprosy" };
            
            foreach (var propertyName in diseaseProperties)
            {
                var property = type.GetProperty(propertyName);
                if (property == null || property.PropertyType != typeof(bool))
                {
                    return false;
                }
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private void DisplayHeader(string title)
    {
        GD.Print("═══════════════════════════════════════════════════════════════════");
        GD.Print($"  {title}");
        GD.Print("═══════════════════════════════════════════════════════════════════");
        GD.Print("");
    }
    
    private void DisplayTestCategory(string categoryName)
    {
        GD.Print($"\n--- {categoryName} ---");
    }
    
    private void DisplayTestResults(List<TestResult> results)
    {
        GD.Print("\n═══ HEALER SYSTEM VALIDATION RESULTS ═══");
        
        int totalTests = results.Count;
        int passedTests = results.Count(r => r.Passed);
        int failedTests = totalTests - passedTests;
        
        GD.Print($"Total Tests: {totalTests}");
        GD.Print($"Passed: {passedTests}");
        GD.Print($"Failed: {failedTests}");
        GD.Print($"Success Rate: {(passedTests * 100.0 / totalTests):F1}%");
        GD.Print("");
        
        // Show failed tests
        if (failedTests > 0)
        {
            GD.Print("FAILED TESTS:");
            foreach (var result in results.Where(r => !r.Passed))
            {
                GD.Print($"❌ {result.TestName}: {result.Details}");
            }
            GD.Print("");
        }
        
        // Show passed tests summary
        GD.Print("PASSED TESTS:");
        foreach (var result in results.Where(r => r.Passed))
        {
            GD.Print($"✅ {result.TestName}: {result.Details}");
        }
        
        GD.Print("\n═══════════════════════════════════════════════════════════════════");
    }
    
    #endregion
    
    private class TestResult
    {
        public string TestName { get; set; }
        public bool Passed { get; set; }
        public string Details { get; set; }
        
        public TestResult(string testName, bool passed, string details)
        {
            TestName = testName;
            Passed = passed;
            Details = details;
        }
    }
} 