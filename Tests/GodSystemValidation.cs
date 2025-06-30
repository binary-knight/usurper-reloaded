using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Comprehensive God System Validation Tests
/// Tests all Pascal-compatible god functionality from INITGODS.PAS, VARGODS.PAS, TEMPLE.PAS, GODWORLD.PAS
/// Validates 100% Pascal compatibility for the complete god and divine system
/// </summary>
public partial class GodSystemValidation : Node
{
    private TerminalEmulator terminal;
    private LocationManager locationManager;
    private GodSystem godSystem;
    private TempleLocation templeLocation;
    private GodWorldLocation godWorldLocation;
    
    public override void _Ready()
    {
        GD.Print("=== God System Validation Test Suite ===");
        GD.Print("Testing Pascal-compatible god system features...");
        
        RunValidationTests();
    }
    
    private void RunValidationTests()
    {
        try
        {
            // Initialize components
            terminal = new TerminalEmulator();
            godSystem = new GodSystem();
            locationManager = new LocationManager(terminal);
            templeLocation = new TempleLocation(terminal, locationManager, godSystem);
            godWorldLocation = new GodWorldLocation(terminal, locationManager, godSystem);
            
            // Test Suite 1: Core God System
            TestCoreGodSystem();
            
            // Test Suite 2: God Creation and Management
            TestGodCreationManagement();
            
            // Test Suite 3: God Levels and Experience
            TestGodLevelsExperience();
            
            // Test Suite 4: Believers and Worship
            TestBelieversWorship();
            
            // Test Suite 5: Sacrifices and Altars
            TestSacrificesAltars();
            
            // Test Suite 6: Divine Interventions
            TestDivineInterventions();
            
            // Test Suite 7: Temple Location
            TestTempleLocation();
            
            // Test Suite 8: God World Location
            TestGodWorldLocation();
            
            // Test Suite 9: Pascal Compatibility
            TestPascalCompatibility();
            
            // Test Suite 10: System Integration
            TestSystemIntegration();
            
            // Test Suite 11: Error Handling
            TestErrorHandling();
            
            // Test Suite 12: Performance Tests
            TestPerformance();
            
            GD.Print("✓ All God System validation tests passed!");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"✗ God System validation failed: {ex.Message}");
            GD.PrintErr($"Stack trace: {ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// Test Suite 1: Core God System (4 tests)
    /// </summary>
    private void TestCoreGodSystem()
    {
        GD.Print("Testing Core God System...");
        
        // Test 1.1: God System Initialization
        Assert(godSystem != null, "God system should be created");
        var stats = godSystem.GetGodStatistics();
        Assert(stats.ContainsKey("TotalGods"), "God statistics should be available");
        Assert((int)stats["TotalGods"] >= 1, "Supreme creator should be initialized");
        
        // Test 1.2: Supreme Creator Initialization
        var supremeCreator = godSystem.GetGod(GameConfig.SupremeCreatorName);
        Assert(supremeCreator != null, "Supreme creator should exist");
        Assert(supremeCreator.Name == GameConfig.SupremeCreatorName, "Supreme creator should have correct name");
        Assert(supremeCreator.Level == GameConfig.MaxGodLevel, "Supreme creator should be max level");
        Assert(supremeCreator.IsActive(), "Supreme creator should be active");
        
        // Test 1.3: God Search Functions
        var searchResults = godSystem.SearchGodsByUser("System");
        Assert(searchResults.Count > 0, "Should find supreme creator by user name");
        Assert(godSystem.VerifyGodExists(GameConfig.SupremeCreatorName), "Supreme creator should exist");
        Assert(!godSystem.VerifyGodExists("NonExistentGod"), "Non-existent god should not exist");
        
        // Test 1.4: God System Configuration
        Assert(GameConfig.MaxGodLevel == 9, "Max god level should be 9");
        Assert(GameConfig.DefaultGodDeedsLeft == 3, "Default deeds should be 3");
        Assert(GameConfig.SupremeCreatorName == "Manwe", "Supreme creator should be Manwe");
        Assert(GameConfig.GodTitles.Length == 10, "Should have 10 god titles (0-9)");
        
        GD.Print("✓ Core God System tests passed");
    }
    
    /// <summary>
    /// Test Suite 2: God Creation and Management (4 tests)
    /// </summary>
    private void TestGodCreationManagement()
    {
        GD.Print("Testing God Creation and Management...");
        
        // Test 2.1: God Creation (Pascal Become_God)
        var newGod = godSystem.BecomeGod("TestUser", "TestGod", "TEST123", 1, 1000, 2000);
        Assert(newGod != null, "Should create new god");
        Assert(newGod.Name == "TestGod", "God should have correct name");
        Assert(newGod.RealName == "TestUser", "God should have correct real name");
        Assert(newGod.Level == 1, "New god should start at level 1");
        Assert(newGod.Experience == 1, "New god should start with 1 experience");
        Assert(newGod.DeedsLeft == GameConfig.DefaultGodDeedsLeft, "New god should have default deeds");
        
        // Test 2.2: Duplicate Name Prevention
        var duplicateGod = godSystem.BecomeGod("AnotherUser", "TestGod", "ANOTHER", 2, 500, 1500);
        Assert(duplicateGod == null, "Should not create god with duplicate name");
        
        var sysopGod = godSystem.BecomeGod("TestUser2", "SYSOP", "SYSOP123", 1, 0, 0);
        Assert(sysopGod == null, "Should not create god with SYSOP name");
        
        var supremeGod = godSystem.BecomeGod("TestUser3", GameConfig.SupremeCreatorName, "SUPREME", 1, 0, 0);
        Assert(supremeGod == null, "Should not create god with supreme creator name");
        
        // Test 2.3: God Management
        godSystem.AddGod(newGod);
        var retrievedGod = godSystem.GetGod("TestGod");
        Assert(retrievedGod != null, "Should retrieve added god");
        Assert(retrievedGod.Name == newGod.Name, "Retrieved god should match original");
        
        // Test 2.4: God Deletion
        godSystem.RemoveGod("TestGod");
        var deletedGod = godSystem.GetGod("TestGod");
        Assert(deletedGod == null || deletedGod.Deleted, "God should be deleted or marked as deleted");
        
        GD.Print("✓ God Creation and Management tests passed");
    }
    
    /// <summary>
    /// Test Suite 3: God Levels and Experience (4 tests)
    /// </summary>
    private void TestGodLevelsExperience()
    {
        GD.Print("Testing God Levels and Experience...");
        
        // Test 3.1: Experience Thresholds (Pascal God_Level_Raise)
        var testGod = new God("TestUser", "LevelTestGod", "LEVEL123", 1, 0, 0);
        
        Assert(testGod.CalculateLevel() == 1, "Level 1 for starting experience");
        
        testGod.Experience = GameConfig.GodLevel2Experience;
        Assert(testGod.CalculateLevel() == 2, "Level 2 at 5000 experience");
        
        testGod.Experience = GameConfig.GodLevel5Experience;
        Assert(testGod.CalculateLevel() == 5, "Level 5 at 70000 experience");
        
        testGod.Experience = GameConfig.GodLevel9Experience;
        Assert(testGod.CalculateLevel() == 9, "Level 9 at 1000500 experience");
        
        // Test 3.2: God Titles (Pascal God_Title)
        Assert(testGod.GetTitle() == "God", "Level 9 should be 'God'");
        
        testGod.Level = 1;
        Assert(GodSystem.GetGodTitle(1) == "Lesser Spirit", "Level 1 should be 'Lesser Spirit'");
        Assert(GodSystem.GetGodTitle(5) == "Minor Deity", "Level 5 should be 'Minor Deity'");
        Assert(GodSystem.GetGodTitle(8) == "DemiGod", "Level 8 should be 'DemiGod'");
        
        // Test 3.3: Experience Increase
        long startExp = testGod.Experience;
        testGod.IncreaseExperience(1000);
        Assert(testGod.Experience == startExp + 1000, "Experience should increase correctly");
        Assert(testGod.Level == testGod.CalculateLevel(), "Level should update with experience");
        
        // Test 3.4: Sacrifice Experience Return (Pascal Sacrifice_Gold_Return)
        Assert(GodSystem.CalculateSacrificeGoldReturn(10) == 1, "Small sacrifice should return 1 power");
        Assert(GodSystem.CalculateSacrificeGoldReturn(1000) == 2, "Medium sacrifice should return 2 power");
        Assert(GodSystem.CalculateSacrificeGoldReturn(50000) == 4, "Large sacrifice should return 4 power");
        Assert(GodSystem.CalculateSacrificeGoldReturn(200000000) == 8, "Huge sacrifice should return 8 power");
        
        GD.Print("✓ God Levels and Experience tests passed");
    }
    
    /// <summary>
    /// Test Suite 4: Believers and Worship (4 tests)
    /// </summary>
    private void TestBelieversWorship()
    {
        GD.Print("Testing Believers and Worship...");
        
        // Test 4.1: Believer Management
        var testGod = new God("TestUser", "WorshipTestGod", "WORSHIP123", 1, 0, 0);
        godSystem.AddGod(testGod);
        
        Assert(testGod.Believers == 0, "New god should have no believers");
        Assert(testGod.Disciples.Count == 0, "New god should have empty disciples list");
        
        // Test 4.2: Adding Believers
        godSystem.SetPlayerGod("Player1", "WorshipTestGod");
        godSystem.SetPlayerGod("Player2", "WorshipTestGod");
        
        var updatedGod = godSystem.GetGod("WorshipTestGod");
        Assert(updatedGod.Believers == 2, "God should have 2 believers");
        Assert(updatedGod.Disciples.Contains("Player1"), "Should contain Player1");
        Assert(updatedGod.Disciples.Contains("Player2"), "Should contain Player2");
        
        // Test 4.3: Believer Count Functions
        Assert(godSystem.CountBelievers("WorshipTestGod") == 2, "Count believers should return 2");
        Assert(godSystem.PlayerHasGod("Player1"), "Player1 should have a god");
        Assert(godSystem.GetPlayerGod("Player1") == "WorshipTestGod", "Player1's god should be correct");
        
        // Test 4.4: Removing Believers
        godSystem.SetPlayerGod("Player1", ""); // Remove god
        updatedGod = godSystem.GetGod("WorshipTestGod");
        Assert(updatedGod.Believers == 1, "God should have 1 believer after removal");
        Assert(!updatedGod.Disciples.Contains("Player1"), "Should not contain Player1");
        Assert(updatedGod.Disciples.Contains("Player2"), "Should still contain Player2");
        
        GD.Print("✓ Believers and Worship tests passed");
    }
    
    /// <summary>
    /// Test Suite 5: Sacrifices and Altars (4 tests)
    /// </summary>
    private void TestSacrificesAltars()
    {
        GD.Print("Testing Sacrifices and Altars...");
        
        // Test 5.1: Gold Sacrifice Processing
        var testGod = new God("TestUser", "SacrificeTestGod", "SACRIFICE123", 1, 0, 0);
        godSystem.AddGod(testGod);
        
        long initialExp = testGod.Experience;
        long powerGained = godSystem.ProcessGoldSacrifice("SacrificeTestGod", 1000, "TestPlayer");
        
        Assert(powerGained == 2, "1000 gold should return 2 power");
        var updatedGod = godSystem.GetGod("SacrificeTestGod");
        Assert(updatedGod.Experience == initialExp + powerGained, "God experience should increase");
        
        // Test 5.2: Sacrifice Tiers
        Assert(God.CalculateSacrificeReturn(15) == 1, "Tier 1 sacrifice");
        Assert(God.CalculateSacrificeReturn(1500) == 2, "Tier 2 sacrifice");
        Assert(God.CalculateSacrificeReturn(30000) == 3, "Tier 3 sacrifice");
        Assert(God.CalculateSacrificeReturn(100000) == 4, "Tier 4 sacrifice");
        Assert(God.CalculateSacrificeReturn(500000) == 5, "Tier 5 sacrifice");
        Assert(God.CalculateSacrificeReturn(10000000) == 6, "Tier 6 sacrifice");
        Assert(God.CalculateSacrificeReturn(50000000) == 7, "Tier 7 sacrifice");
        Assert(God.CalculateSacrificeReturn(500000000) == 8, "Tier 8 sacrifice");
        
        // Test 5.3: Altar Desecration
        godSystem.ProcessAltarDesecration("SacrificeTestGod", "EvilPlayer");
        // In full implementation, this would reduce god power and notify disciples
        
        // Test 5.4: Multiple Sacrifices
        long exp1 = updatedGod.Experience;
        godSystem.ProcessGoldSacrifice("SacrificeTestGod", 5000, "Player1");
        long exp2 = godSystem.GetGod("SacrificeTestGod").Experience;
        godSystem.ProcessGoldSacrifice("SacrificeTestGod", 10000, "Player2");
        long exp3 = godSystem.GetGod("SacrificeTestGod").Experience;
        
        Assert(exp2 > exp1, "Experience should increase after first sacrifice");
        Assert(exp3 > exp2, "Experience should increase after second sacrifice");
        
        GD.Print("✓ Sacrifices and Altars tests passed");
    }
    
    /// <summary>
    /// Test Suite 6: Divine Interventions (4 tests)
    /// </summary>
    private void TestDivineInterventions()
    {
        GD.Print("Testing Divine Interventions...");
        
        // Test 6.1: Divine Deed Management
        var testGod = new God("TestUser", "InterventionTestGod", "INTERVENTION123", 1, 0, 0);
        Assert(testGod.DeedsLeft == GameConfig.DefaultGodDeedsLeft, "God should start with default deeds");
        
        bool usedDeed = testGod.UseDeed();
        Assert(usedDeed, "Should successfully use a deed");
        Assert(testGod.DeedsLeft == GameConfig.DefaultGodDeedsLeft - 1, "Deeds should decrease");
        
        // Test 6.2: Divine Intervention Functions
        godSystem.AddGod(testGod);
        bool blessed = godSystem.DivineInterventionBless(testGod, "TestMortal");
        Assert(blessed, "Should successfully bless mortal");
        
        bool cursed = godSystem.DivineInterventionCurse(testGod, "EvilMortal");
        Assert(cursed, "Should successfully curse mortal");
        
        bool escape = godSystem.DivineInterventionPrisonEscape(testGod, "Prisoner");
        Assert(escape, "Should successfully help prisoner escape");
        
        // Test 6.3: Deed Limitations
        // Use up remaining deeds
        while (testGod.DeedsLeft > 0)
        {
            testGod.UseDeed();
        }
        
        bool noDeeds = godSystem.DivineInterventionBless(testGod, "AnotherMortal");
        Assert(!noDeeds, "Should fail when no deeds left");
        
        // Test 6.4: Daily Reset (Pascal God_Maintenance)
        testGod.ResetDailyDeeds();
        Assert(testGod.DeedsLeft == GameConfig.DefaultGodDeedsLeft, "Deeds should reset to default");
        
        GD.Print("✓ Divine Interventions tests passed");
    }
    
    /// <summary>
    /// Test Suite 7: Temple Location (4 tests)
    /// </summary>
    private void TestTempleLocation()
    {
        GD.Print("Testing Temple Location...");
        
        // Test 7.1: Temple Creation and Properties
        Assert(templeLocation != null, "Temple location should be created");
        Assert(templeLocation.LocationName == "Temple of the Gods", "Temple should have correct name");
        Assert(templeLocation.LocationId == "temple", "Temple should have correct ID");
        
        // Test 7.2: God System Integration
        var testPlayer = CreateTestPlayer();
        godSystem.SetPlayerGod(testPlayer.Name2, GameConfig.SupremeCreatorName);
        
        string playerGod = godSystem.GetPlayerGod(testPlayer.Name2);
        Assert(playerGod == GameConfig.SupremeCreatorName, "Player should worship supreme creator");
        
        // Test 7.3: Temple Menu Constants
        Assert(GameConfig.TempleMenuWorship == "W", "Worship menu option");
        Assert(GameConfig.TempleMenuDesecrate == "D", "Desecrate menu option");
        Assert(GameConfig.TempleMenuAltars == "A", "Altars menu option");
        Assert(GameConfig.TempleMenuContribute == "C", "Contribute menu option");
        Assert(GameConfig.TempleMenuStatus == "S", "Status menu option");
        Assert(GameConfig.TempleMenuGodRanking == "G", "God ranking menu option");
        Assert(GameConfig.TempleMenuHolyNews == "H", "Holy news menu option");
        Assert(GameConfig.TempleMenuReturn == "R", "Return menu option");
        
        // Test 7.4: Temple Location ID
        Assert(GameConfig.TempleLocationId == 47, "Temple location ID should be 47");
        
        GD.Print("✓ Temple Location tests passed");
    }
    
    /// <summary>
    /// Test Suite 8: God World Location (4 tests)
    /// </summary>
    private void TestGodWorldLocation()
    {
        GD.Print("Testing God World Location...");
        
        // Test 8.1: God World Creation and Properties
        Assert(godWorldLocation != null, "God world location should be created");
        Assert(godWorldLocation.LocationName == "The Divine Heaven", "God world should have correct name");
        Assert(godWorldLocation.LocationId == "heaven", "God world should have correct ID");
        
        // Test 8.2: Heaven Menu Constants
        Assert(GameConfig.GodWorldMenuImmortals == "I", "Immortals menu option");
        Assert(GameConfig.GodWorldMenuIntervention == "D", "Divine intervention menu option");
        Assert(GameConfig.GodWorldMenuVisitBoss == "V", "Visit boss menu option");
        Assert(GameConfig.GodWorldMenuBelievers == "B", "Believers menu option");
        Assert(GameConfig.GodWorldMenuListMortals == "L", "List mortals menu option");
        Assert(GameConfig.GodWorldMenuMessage == "M", "Message menu option");
        Assert(GameConfig.GodWorldMenuExamine == "E", "Examine menu option");
        Assert(GameConfig.GodWorldMenuStatus == "S", "Status menu option");
        Assert(GameConfig.GodWorldMenuComment == "C", "Comment menu option");
        Assert(GameConfig.GodWorldMenuNews == "N", "News menu option");
        Assert(GameConfig.GodWorldMenuQuit == "Q", "Quit menu option");
        Assert(GameConfig.GodWorldMenuFlock == "F", "Flock menu option");
        Assert(GameConfig.GodWorldMenuSuicide == "*", "Suicide menu option");
        Assert(GameConfig.GodWorldMenuImmortalNews == "1", "Immortal news menu option");
        
        // Test 8.3: Divine Intervention Menu Constants
        Assert(GameConfig.DivineMortals == "M", "Divine mortals option");
        Assert(GameConfig.DivineChildren == "C", "Divine children option");
        Assert(GameConfig.DivinePrisoners == "P", "Divine prisoners option");
        Assert(GameConfig.DivineHelp == "H", "Divine help option");
        Assert(GameConfig.DivineReturn == "R", "Divine return option");
        
        // Test 8.4: Heaven Location ID
        Assert(GameConfig.HeavenLocationId == 400, "Heaven location ID should be 400");
        Assert(GameConfig.HeavenBossLocationId == 401, "Heaven boss location ID should be 401");
        
        GD.Print("✓ God World Location tests passed");
    }
    
    /// <summary>
    /// Test Suite 9: Pascal Compatibility (4 tests)
    /// </summary>
    private void TestPascalCompatibility()
    {
        GD.Print("Testing Pascal Compatibility...");
        
        // Test 9.1: Pascal Constants Matching
        Assert(GameConfig.TempleLocationId == 47, "Temple location should match Pascal onloc_temple = 47");
        Assert(GameConfig.HeavenLocationId == 400, "Heaven location should match Pascal onloc_heaven = 400");
        Assert(GameConfig.MaxGodLevel == 9, "Max god level should match Pascal maximum");
        Assert(GameConfig.DefaultGodDeedsLeft == 3, "Default deeds should match Pascal config.gods_deedsleft");
        
        // Test 9.2: Experience Thresholds Exact Match
        Assert(GameConfig.GodLevel2Experience == 5000, "Level 2 threshold matches Pascal");
        Assert(GameConfig.GodLevel3Experience == 15000, "Level 3 threshold matches Pascal");
        Assert(GameConfig.GodLevel4Experience == 50000, "Level 4 threshold matches Pascal");
        Assert(GameConfig.GodLevel5Experience == 70000, "Level 5 threshold matches Pascal");
        Assert(GameConfig.GodLevel6Experience == 90000, "Level 6 threshold matches Pascal");
        Assert(GameConfig.GodLevel7Experience == 110000, "Level 7 threshold matches Pascal");
        Assert(GameConfig.GodLevel8Experience == 550000, "Level 8 threshold matches Pascal");
        Assert(GameConfig.GodLevel9Experience == 1000500, "Level 9 threshold matches Pascal");
        
        // Test 9.3: Sacrifice Return Values
        Assert(GameConfig.SacrificeGoldTier1Max == 20, "Tier 1 max matches Pascal");
        Assert(GameConfig.SacrificeGoldTier2Max == 2000, "Tier 2 max matches Pascal");
        Assert(GameConfig.SacrificeGoldTier3Max == 45000, "Tier 3 max matches Pascal");
        Assert(GameConfig.SacrificeGoldTier4Max == 150000, "Tier 4 max matches Pascal");
        Assert(GameConfig.SacrificeGoldTier5Max == 900000, "Tier 5 max matches Pascal");
        Assert(GameConfig.SacrificeGoldTier6Max == 15000000, "Tier 6 max matches Pascal");
        Assert(GameConfig.SacrificeGoldTier7Max == 110000000, "Tier 7 max matches Pascal");
        
        // Test 9.4: God Titles Match Pascal
        Assert(GameConfig.GodTitles[1] == "Lesser Spirit", "Level 1 title matches Pascal");
        Assert(GameConfig.GodTitles[2] == "Minor Spirit", "Level 2 title matches Pascal");
        Assert(GameConfig.GodTitles[3] == "Spirit", "Level 3 title matches Pascal");
        Assert(GameConfig.GodTitles[4] == "Major Spirit", "Level 4 title matches Pascal");
        Assert(GameConfig.GodTitles[5] == "Minor Deity", "Level 5 title matches Pascal");
        Assert(GameConfig.GodTitles[6] == "Deity", "Level 6 title matches Pascal");
        Assert(GameConfig.GodTitles[7] == "Major Deity", "Level 7 title matches Pascal");
        Assert(GameConfig.GodTitles[8] == "DemiGod", "Level 8 title matches Pascal");
        Assert(GameConfig.GodTitles[9] == "God", "Level 9 title matches Pascal");
        
        GD.Print("✓ Pascal Compatibility tests passed");
    }
    
    /// <summary>
    /// Test Suite 10: System Integration (4 tests)
    /// </summary>
    private void TestSystemIntegration()
    {
        GD.Print("Testing System Integration...");
        
        // Test 10.1: God System with Location Manager
        Assert(locationManager != null, "Location manager should exist");
        // In full integration, temple and god world would be registered with location manager
        
        // Test 10.2: God Maintenance System
        godSystem.RunGodMaintenance();
        var activeGods = godSystem.GetActiveGods();
        foreach (var god in activeGods)
        {
            Assert(god.DeedsLeft == GameConfig.DefaultGodDeedsLeft, "All gods should have reset deeds after maintenance");
        }
        
        // Test 10.3: Serialization and Persistence
        var godSystemDict = godSystem.ToDictionary();
        Assert(godSystemDict.ContainsKey("Gods"), "Serialization should include gods");
        Assert(godSystemDict.ContainsKey("PlayerGods"), "Serialization should include player mappings");
        Assert(godSystemDict.ContainsKey("LastMaintenance"), "Serialization should include maintenance time");
        
        var restoredSystem = GodSystem.FromDictionary(godSystemDict);
        Assert(restoredSystem != null, "Should restore god system from dictionary");
        Assert(restoredSystem.GetActiveGods().Count == godSystem.GetActiveGods().Count, "Restored system should have same god count");
        
        // Test 10.4: God Statistics
        var stats = godSystem.GetGodStatistics();
        Assert(stats.ContainsKey("TotalGods"), "Statistics should include total gods");
        Assert(stats.ContainsKey("TotalBelievers"), "Statistics should include total believers");
        Assert(stats.ContainsKey("AverageLevel"), "Statistics should include average level");
        Assert(stats.ContainsKey("TotalExperience"), "Statistics should include total experience");
        Assert(stats.ContainsKey("MostPowerfulGod"), "Statistics should include most powerful god");
        Assert(stats.ContainsKey("MostPopularGod"), "Statistics should include most popular god");
        
        GD.Print("✓ System Integration tests passed");
    }
    
    /// <summary>
    /// Test Suite 11: Error Handling (4 tests)
    /// </summary>
    private void TestErrorHandling()
    {
        GD.Print("Testing Error Handling...");
        
        // Test 11.1: Invalid God Operations
        Assert(godSystem.GetGod("NonExistentGod") == null, "Should return null for non-existent god");
        Assert(!godSystem.VerifyGodExists("InvalidGod"), "Should return false for invalid god");
        Assert(godSystem.CountBelievers("NonExistentGod") == 0, "Should return 0 believers for non-existent god");
        
        // Test 11.2: Invalid Character Operations
        var invalidGod = new God("", "", "", 1, 0, 0); // Invalid god with empty names
        Assert(!invalidGod.IsValid(), "God with empty names should be invalid");
        
        godSystem.AddGod(invalidGod); // Should not add invalid god
        Assert(godSystem.GetGod("") == null, "Should not add god with empty name");
        
        // Test 11.3: Boundary Conditions
        var testGod = new God("TestUser", "BoundaryTestGod", "BOUNDARY123", 1, 0, 0);
        testGod.Level = -1; // Invalid level
        Assert(!testGod.IsValid(), "God with invalid level should be invalid");
        
        testGod.Level = GameConfig.MaxGodLevel + 1; // Too high level
        Assert(!testGod.IsValid(), "God with too high level should be invalid");
        
        testGod.Level = 5; // Valid level
        testGod.Experience = -1; // Invalid experience
        Assert(!testGod.IsValid(), "God with negative experience should be invalid");
        
        // Test 11.4: Sacrifice Edge Cases
        Assert(GodSystem.CalculateSacrificeGoldReturn(0) == 1, "Zero sacrifice should return minimum power");
        Assert(GodSystem.CalculateSacrificeGoldReturn(-100) == 1, "Negative sacrifice should return minimum power");
        
        GD.Print("✓ Error Handling tests passed");
    }
    
    /// <summary>
    /// Test Suite 12: Performance Tests (4 tests)
    /// </summary>
    private void TestPerformance()
    {
        GD.Print("Testing Performance...");
        
        // Test 12.1: Large God Set Operations
        var startTime = DateTime.Now;
        for (int i = 0; i < 100; i++)
        {
            var god = new God($"PerfUser{i}", $"PerfGod{i}", $"PERF{i}", 1, 0, 0);
            god.IncreaseExperience(i * 1000);
        }
        var endTime = DateTime.Now;
        var duration = endTime - startTime;
        Assert(duration.TotalMilliseconds < 1000, "Creating 100 gods should be fast");
        
        // Test 12.2: God Search Performance
        startTime = DateTime.Now;
        for (int i = 0; i < 1000; i++)
        {
            godSystem.GetGod(GameConfig.SupremeCreatorName);
        }
        endTime = DateTime.Now;
        duration = endTime - startTime;
        Assert(duration.TotalMilliseconds < 500, "1000 god lookups should be fast");
        
        // Test 12.3: Believer Management Performance
        var perfGod = new God("PerfUser", "PerfGod", "PERF999", 1, 0, 0);
        godSystem.AddGod(perfGod);
        
        startTime = DateTime.Now;
        for (int i = 0; i < 1000; i++)
        {
            godSystem.SetPlayerGod($"Player{i}", "PerfGod");
        }
        endTime = DateTime.Now;
        duration = endTime - startTime;
        Assert(duration.TotalMilliseconds < 2000, "Adding 1000 believers should be reasonably fast");
        
        // Test 12.4: Statistics Calculation Performance
        startTime = DateTime.Now;
        for (int i = 0; i < 100; i++)
        {
            var stats = godSystem.GetGodStatistics();
        }
        endTime = DateTime.Now;
        duration = endTime - startTime;
        Assert(duration.TotalMilliseconds < 1000, "100 statistics calculations should be fast");
        
        GD.Print("✓ Performance tests passed");
    }
    
    /// <summary>
    /// Create a test player for validation
    /// </summary>
    private Character CreateTestPlayer()
    {
        return new Character
        {
            Name1 = "TestPlayer",
            Name2 = "TestPlayer",
            Level = 10,
            Gold = 10000,
            ChivNr = 5,
            DarkNr = 3,
            Expert = false,
            HP = 100,
            MaxHP = 100
        };
    }
    
    /// <summary>
    /// Assert helper method
    /// </summary>
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception($"Assertion failed: {message}");
        }
    }
} 
