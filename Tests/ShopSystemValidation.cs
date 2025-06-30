using UsurperRemake.Utils;
using Godot;
using System;
using System.Threading.Tasks;

/// <summary>
/// Shop System Validation - Pascal WEAPSHOP.PAS and ARMSHOP.PAS compatibility tests
/// </summary>
public class ShopSystemValidation
{
    public async Task<bool> RunValidationTests()
    {
        GD.Print("=== SHOP SYSTEM VALIDATION ===");
        
        // Test 1: Haggling Engine
        bool hagglingTest = TestHagglingEngine();
        GD.Print($"Haggling Engine: {(hagglingTest ? "PASS" : "FAIL")}");
        
        // Test 2: Item Manager Shop Methods
        bool itemManagerTest = TestItemManagerShopMethods();
        GD.Print($"Item Manager Shop Methods: {(itemManagerTest ? "PASS" : "FAIL")}");
        
        // Test 3: Location Integration
        bool locationTest = TestLocationIntegration();
        GD.Print($"Location Integration: {(locationTest ? "PASS" : "FAIL")}");
        
        bool allPassed = hagglingTest && itemManagerTest && locationTest;
        GD.Print($"=== SHOP VALIDATION {(allPassed ? "PASSED" : "FAILED")} ===");
        
        return allPassed;
    }
    
    private bool TestHagglingEngine()
    {
        try
        {
            var testPlayer = new Character
            {
                WeapHag = 3,
                ArmHag = 3,
                Charisma = 100,
                Race = CharacterRace.Troll
            };
            
            // Test haggling availability
            bool canHaggleWeapons = HagglingEngine.CanHaggle(testPlayer, HagglingEngine.ShopType.Weapon);
            bool canHaggleArmor = HagglingEngine.CanHaggle(testPlayer, HagglingEngine.ShopType.Armor);
            
            if (!canHaggleWeapons || !canHaggleArmor) return false;
            
            // Test race discount
            long discountedPrice = HagglingEngine.ApplyRaceDiscount(testPlayer, 1000);
            if (discountedPrice != 900) return false; // 10% troll discount
            
            // Test daily reset
            testPlayer.WeapHag = 0;
            testPlayer.ArmHag = 0;
            HagglingEngine.ResetDailyHaggling(testPlayer);
            
            return testPlayer.WeapHag == 3 && testPlayer.ArmHag == 3;
        }
        catch
        {
            return false;
        }
    }
    
    private bool TestItemManagerShopMethods()
    {
        try
        {
            // Test weapon retrieval
            var weapon = ItemManager.GetWeapon(1);
            if (weapon == null) return false;
            
            // Test armor retrieval
            var armor = ItemManager.GetArmor(1);
            if (armor == null) return false;
            
            // Test shop listings
            var shopWeapons = ItemManager.GetShopWeapons();
            var shopArmors = ItemManager.GetShopArmors();
            
            return shopWeapons.Count > 0 && shopArmors.Count > 0;
        }
        catch
        {
            return false;
        }
    }
    
    private bool TestLocationIntegration()
    {
        try
        {
            var weaponShop = new WeaponShopLocation();
            var armorShop = new ArmorShopLocation();
            
            return weaponShop != null && armorShop != null;
        }
        catch
        {
            return false;
        }
    }
} 
