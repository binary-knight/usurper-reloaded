using System;
using System.Threading.Tasks;

/// <summary>
/// Castle System Architecture Validation
/// Simple test to verify the castle system structure and dependencies
/// </summary>
public class CastleSystemValidation
{
    /// <summary>
    /// Test basic King creation and management
    /// </summary>
    public static bool TestKingSystem()
    {
        try
        {
            // Test King creation
            var king = King.CreateNewKing("TestKing", CharacterAI.Human, CharacterSex.Male);
            
            if (king == null) return false;
            if (king.Name != "TestKing") return false;
            if (king.Treasury != GameConfig.DefaultRoyalTreasury) return false;
            
            // Test royal guard system
            var guardAdded = king.AddGuard("TestGuard", CharacterAI.Computer, CharacterSex.Female, 1500);
            if (!guardAdded) return false;
            if (king.Guards.Count != 1) return false;
            
            // Test daily calculations
            var expenses = king.CalculateDailyExpenses();
            if (expenses < 1500) return false; // At least guard salary + base costs
            
            // Test prison system
            king.ImprisonCharacter("TestPrisoner", 5, "Theft");
            if (!king.Prisoners.ContainsKey("TestPrisoner")) return false;
            
            Console.WriteLine("✅ King System: PASSED");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ King System: FAILED - {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Test Character integration with castle system
    /// </summary>
    public static bool TestCharacterIntegration()
    {
        try
        {
            // Test character creation
            var character = new Character();
            
            // Test king property
            character.King = true;
            if (!character.King) return false;
            
            // Test basic properties needed for castle
            character.Name2 = "TestPlayer";
            character.Level = 15;
            character.Gold = 10000;
            character.Chivalry = 500;
            character.Team = "";
            
            // Test castle eligibility
            bool canChallenge = character.Level >= GameConfig.MinLevelKing 
                               && !character.King 
                               && string.IsNullOrEmpty(character.Team);
            
            // Should be false since character is already king
            if (canChallenge) return false;
            
            // Test non-king character
            character.King = false;
            canChallenge = character.Level >= GameConfig.MinLevelKing 
                          && !character.King 
                          && string.IsNullOrEmpty(character.Team);
            
            if (!canChallenge) return false;
            
            Console.WriteLine("✅ Character Integration: PASSED");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Character Integration: FAILED - {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Test GameConfig castle constants
    /// </summary>
    public static bool TestGameConfigConstants()
    {
        try
        {
            // Test castle constants exist and are reasonable
            if (GameConfig.MaxRoyalGuards <= 0) return false;
            if (GameConfig.MinLevelKing <= 0) return false;
            if (GameConfig.DefaultRoyalTreasury <= 0) return false;
            if (GameConfig.BaseGuardSalary <= 0) return false;
            
            // Test tax alignment enum
            var taxAll = GameConfig.TaxAlignment.All;
            var taxGood = GameConfig.TaxAlignment.Good;
            var taxEvil = GameConfig.TaxAlignment.Evil;
            
            if ((int)taxAll != 0) return false;
            if ((int)taxGood != 1) return false;
            if ((int)taxEvil != 2) return false;
            
            Console.WriteLine("✅ GameConfig Constants: PASSED");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GameConfig Constants: FAILED - {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Test Location Manager integration
    /// </summary>
    public static bool TestLocationIntegration()
    {
        try
        {
            // Test that castle location exists in enum
            var castleLocation = GameLocation.Castle;
            
            // Test that location manager can be instantiated
            var locationManager = new LocationManager();
            
            // Test that castle location is accessible
            var hasLocation = locationManager.HasLocation(castleLocation);
            if (!hasLocation) return false;
            
            Console.WriteLine("✅ Location Integration: PASSED");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Location Integration: FAILED - {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Run all validation tests
    /// </summary>
    public static bool RunAllTests()
    {
        Console.WriteLine("🏰 Castle System Architecture Validation");
        Console.WriteLine("=========================================");
        
        bool allPassed = true;
        
        allPassed &= TestGameConfigConstants();
        allPassed &= TestCharacterIntegration();
        allPassed &= TestKingSystem();
        allPassed &= TestLocationIntegration();
        
        Console.WriteLine("=========================================");
        if (allPassed)
        {
            Console.WriteLine("🎉 ALL TESTS PASSED - Castle System Ready!");
        }
        else
        {
            Console.WriteLine("⚠️  SOME TESTS FAILED - Review Implementation");
        }
        
        return allPassed;
    }
}

/// <summary>
/// Simple console test runner for validation
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        CastleSystemValidation.RunAllTests();
    }
} 