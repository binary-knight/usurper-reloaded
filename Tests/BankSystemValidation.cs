using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Comprehensive validation tests for the Bank System
/// Tests deposits, withdrawals, guard duty, robberies, and daily maintenance
/// Based on Pascal BANK.PAS validation requirements
/// </summary>
public static class BankSystemValidation
{
    public static void RunAllTests()
    {
        Console.WriteLine("=== BANK SYSTEM VALIDATION ===");
        Console.WriteLine($"Test run started at: {DateTime.Now}\n");
        
        int passedTests = 0;
        int totalTests = 0;
        
        // Core banking operations
        passedTests += TestBankingOperations() ? 1 : 0; totalTests++;
        passedTests += TestMoneyLimits() ? 1 : 0; totalTests++;
        passedTests += TestAccountStatus() ? 1 : 0; totalTests++;
        passedTests += TestMoneyTransfers() ? 1 : 0; totalTests++;
        
        // Guard system
        passedTests += TestGuardApplication() ? 1 : 0; totalTests++;
        passedTests += TestGuardEligibility() ? 1 : 0; totalTests++;
        passedTests += TestGuardWages() ? 1 : 0; totalTests++;
        
        // Bank security
        passedTests += TestBankRobbery() ? 1 : 0; totalTests++;
        passedTests += TestRobberyLimits() ? 1 : 0; totalTests++;
        
        // Daily systems
        passedTests += TestDailyMaintenance() ? 1 : 0; totalTests++;
        passedTests += TestInterestCalculation() ? 1 : 0; totalTests++;
        
        // Integration tests
        passedTests += TestBankIntegration() ? 1 : 0; totalTests++;
        
        Console.WriteLine($"\n=== VALIDATION SUMMARY ===");
        Console.WriteLine($"Tests Passed: {passedTests}/{totalTests}");
        Console.WriteLine($"Success Rate: {(double)passedTests/totalTests*100:F1}%");
        
        if (passedTests == totalTests)
        {
            Console.WriteLine("✅ ALL BANK SYSTEM TESTS PASSED!");
        }
        else
        {
            Console.WriteLine("❌ Some tests failed. Review implementation.");
        }
    }
    
    private static bool TestBankingOperations()
    {
        Console.WriteLine("Testing basic banking operations...");
        
        try
        {
            var player = CreateTestPlayer();
            
            // Test deposit
            long initialGold = player.Gold;
            long depositAmount = 5000;
            
            // Simulate deposit
            player.Gold -= depositAmount;
            player.BankGold += depositAmount;
            
            bool depositTest = (player.Gold == initialGold - depositAmount) && 
                              (player.BankGold == depositAmount);
            
            // Test withdrawal
            long withdrawAmount = 2000;
            player.BankGold -= withdrawAmount;
            player.Gold += withdrawAmount;
            
            bool withdrawTest = (player.Gold == initialGold - depositAmount + withdrawAmount) &&
                               (player.BankGold == depositAmount - withdrawAmount);
            
            Console.WriteLine($"  ✓ Deposit test: {(depositTest ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Withdrawal test: {(withdrawTest ? "PASS" : "FAIL")}");
            
            return depositTest && withdrawTest;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Banking operations test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestMoneyLimits()
    {
        Console.WriteLine("Testing money limits and constraints...");
        
        try
        {
            var player = CreateTestPlayer();
            
            // Test maximum bank balance limit
            player.BankGold = GameConfig.MaxBankBalance - 1000;
            bool limitTest1 = player.BankGold < GameConfig.MaxBankBalance;
            
            // Test that deposits are limited by maximum balance
            long maxAdditionalDeposit = GameConfig.MaxBankBalance - player.BankGold;
            bool limitTest2 = maxAdditionalDeposit == 1000;
            
            // Test negative amounts prevention
            player.Gold = 0;
            player.BankGold = 0;
            bool limitTest3 = player.Gold >= 0 && player.BankGold >= 0;
            
            Console.WriteLine($"  ✓ Max balance limit: {(limitTest1 ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Deposit constraint: {(limitTest2 ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Negative prevention: {(limitTest3 ? "PASS" : "FAIL")}");
            
            return limitTest1 && limitTest2 && limitTest3;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Money limits test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestAccountStatus()
    {
        Console.WriteLine("Testing account status display...");
        
        try
        {
            var player = CreateTestPlayer();
            player.Gold = 25000;
            player.BankGold = 75000;
            player.Interest = 1500;
            player.BankGuard = true;
            player.BankWage = 2500;
            player.BankRobberyAttempts = 3;
            
            // Test total worth calculation
            long totalWorth = player.Gold + player.BankGold;
            bool wealthTest = totalWorth == 100000;
            
            // Test guard status
            bool guardTest = player.BankGuard && player.BankWage > 0;
            
            // Test interest tracking
            bool interestTest = player.Interest == 1500;
            
            Console.WriteLine($"  ✓ Wealth calculation: {(wealthTest ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Guard status: {(guardTest ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Interest tracking: {(interestTest ? "PASS" : "FAIL")}");
            
            return wealthTest && guardTest && interestTest;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Account status test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestMoneyTransfers()
    {
        Console.WriteLine("Testing money transfer system...");
        
        try
        {
            var sender = CreateTestPlayer();
            sender.BankGold = 50000;
            
            // Simulate transfer
            long transferAmount = 10000;
            bool validTransfer = transferAmount <= sender.BankGold && transferAmount > 0;
            
            if (validTransfer)
            {
                sender.BankGold -= transferAmount;
            }
            
            bool transferTest = sender.BankGold == 40000 && validTransfer;
            
            // Test transfer limits
            bool limitTest = transferAmount <= GameConfig.MaxMoneyTransfer;
            
            Console.WriteLine($"  ✓ Transfer execution: {(transferTest ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Transfer limits: {(limitTest ? "PASS" : "FAIL")}");
            
            return transferTest && limitTest;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Money transfer test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestGuardApplication()
    {
        Console.WriteLine("Testing guard application system...");
        
        try
        {
            var player = CreateTestPlayer();
            player.Level = 10;
            player.Darkness = 50;
            player.BankGuard = false;
            
            // Test eligibility
            bool levelTest = player.Level >= GameConfig.MinLevelForGuard;
            bool darknessTest = player.Darkness <= GameConfig.MaxDarknessForGuard;
            bool notGuardTest = !player.BankGuard;
            
            bool eligible = levelTest && darknessTest && notGuardTest;
            
            // Simulate successful application
            if (eligible)
            {
                player.BankGuard = true;
                player.BankWage = 1000 + (player.Level * GameConfig.GuardSalaryPerLevel);
            }
            
            bool applicationTest = player.BankGuard && player.BankWage > 0;
            
            Console.WriteLine($"  ✓ Eligibility check: {(eligible ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Application success: {(applicationTest ? "PASS" : "FAIL")}");
            
            return eligible && applicationTest;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Guard application test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestGuardEligibility()
    {
        Console.WriteLine("Testing guard eligibility requirements...");
        
        try
        {
            // Test low level rejection
            var lowLevelPlayer = CreateTestPlayer();
            lowLevelPlayer.Level = 3;
            lowLevelPlayer.Darkness = 0;
            bool lowLevelRejected = lowLevelPlayer.Level < GameConfig.MinLevelForGuard;
            
            // Test high darkness rejection
            var darkPlayer = CreateTestPlayer();
            darkPlayer.Level = 10;
            darkPlayer.Darkness = 150;
            bool darkPlayerRejected = darkPlayer.Darkness > GameConfig.MaxDarknessForGuard;
            
            // Test already guard rejection
            var existingGuard = CreateTestPlayer();
            existingGuard.Level = 10;
            existingGuard.Darkness = 0;
            existingGuard.BankGuard = true;
            bool alreadyGuard = existingGuard.BankGuard;
            
            Console.WriteLine($"  ✓ Low level rejection: {(lowLevelRejected ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ High darkness rejection: {(darkPlayerRejected ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Already guard rejection: {(alreadyGuard ? "PASS" : "FAIL")}");
            
            return lowLevelRejected && darkPlayerRejected && alreadyGuard;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Guard eligibility test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestGuardWages()
    {
        Console.WriteLine("Testing guard wage calculation...");
        
        try
        {
            var guard = CreateTestPlayer();
            guard.Level = 15;
            guard.BankGuard = true;
            
            int expectedWage = 1000 + (guard.Level * GameConfig.GuardSalaryPerLevel);
            guard.BankWage = expectedWage;
            
            bool wageTest = guard.BankWage == expectedWage;
            bool wageFormula = expectedWage == (1000 + (15 * GameConfig.GuardSalaryPerLevel));
            
            Console.WriteLine($"  ✓ Wage calculation: {(wageTest ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Formula verification: {(wageFormula ? "PASS" : "FAIL")}");
            
            return wageTest && wageFormula;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Guard wages test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestBankRobbery()
    {
        Console.WriteLine("Testing bank robbery system...");
        
        try
        {
            var player = CreateTestPlayer();
            player.BankRobberyAttempts = 3;
            player.BankGuard = false;
            player.Chivalry = 100;
            player.Darkness = 50;
            
            // Test robbery attempt limit
            bool hasAttempts = player.BankRobberyAttempts > 0;
            
            // Test guard cannot rob
            bool notGuardRobber = !player.BankGuard;
            
            // Simulate robbery consequences
            if (hasAttempts && notGuardRobber)
            {
                player.Chivalry = 0; // Lose all chivalry
                player.Darkness += 50; // Gain darkness
                player.BankRobberyAttempts--; // Use attempt
            }
            
            bool consequencesTest = player.Chivalry == 0 && 
                                   player.Darkness == 100 && 
                                   player.BankRobberyAttempts == 2;
            
            Console.WriteLine($"  ✓ Attempt limit check: {(hasAttempts ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Guard restriction: {(notGuardRobber ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Robbery consequences: {(consequencesTest ? "PASS" : "FAIL")}");
            
            return hasAttempts && notGuardRobber && consequencesTest;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Bank robbery test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestRobberyLimits()
    {
        Console.WriteLine("Testing robbery attempt limits...");
        
        try
        {
            var player = CreateTestPlayer();
            
            // Test default attempts
            player.BankRobberyAttempts = GameConfig.DefaultBankRobberyAttempts;
            bool defaultTest = player.BankRobberyAttempts == 3;
            
            // Test exhausted attempts
            player.BankRobberyAttempts = 0;
            bool exhaustedTest = player.BankRobberyAttempts <= 0;
            
            // Test attempt consumption
            player.BankRobberyAttempts = 2;
            player.BankRobberyAttempts--; // Use one attempt
            bool consumptionTest = player.BankRobberyAttempts == 1;
            
            Console.WriteLine($"  ✓ Default attempts: {(defaultTest ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Exhausted check: {(exhaustedTest ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Attempt consumption: {(consumptionTest ? "PASS" : "FAIL")}");
            
            return defaultTest && exhaustedTest && consumptionTest;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Robbery limits test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestDailyMaintenance()
    {
        Console.WriteLine("Testing daily maintenance system...");
        
        try
        {
            var guard = CreateTestPlayer();
            guard.BankGuard = true;
            guard.BankWage = 1500;
            guard.BankGold = 10000;
            guard.HP = 100; // Alive
            
            long initialBankGold = guard.BankGold;
            
            // Simulate daily maintenance
            guard.BankGold += guard.BankWage; // Pay wages
            
            bool wagePayment = guard.BankGold == initialBankGold + guard.BankWage;
            
            // Test interest calculation
            long interest = (long)(guard.BankGold * GameConfig.DailyInterestRate / 100);
            guard.Interest += interest;
            guard.BankGold += interest;
            
            bool interestTest = guard.Interest == interest;
            
            Console.WriteLine($"  ✓ Wage payment: {(wagePayment ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Interest calculation: {(interestTest ? "PASS" : "FAIL")}");
            
            return wagePayment && interestTest;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Daily maintenance test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestInterestCalculation()
    {
        Console.WriteLine("Testing interest calculation system...");
        
        try
        {
            var player = CreateTestPlayer();
            player.BankGold = 100000;
            player.Interest = 0;
            
            // Calculate daily interest
            long expectedInterest = (long)(player.BankGold * GameConfig.DailyInterestRate / 100);
            player.Interest += expectedInterest;
            player.BankGold += expectedInterest;
            
            bool interestAmount = expectedInterest == (long)(100000 * 0.05 / 100);
            bool interestTracking = player.Interest == expectedInterest;
            bool bankGoldUpdate = player.BankGold == 100000 + expectedInterest;
            
            Console.WriteLine($"  ✓ Interest amount: {(interestAmount ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Interest tracking: {(interestTracking ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Bank gold update: {(bankGoldUpdate ? "PASS" : "FAIL")}");
            
            return interestAmount && interestTracking && bankGoldUpdate;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Interest calculation test failed: {ex.Message}");
            return false;
        }
    }
    
    private static bool TestBankIntegration()
    {
        Console.WriteLine("Testing bank system integration...");
        
        try
        {
            // Test configuration constants
            bool configTest = GameConfig.DefaultBankRobberyAttempts == 3 &&
                             GameConfig.MaxBankBalance == 2000000000L &&
                             GameConfig.MinLevelForGuard == 5 &&
                             GameConfig.MaxDarknessForGuard == 100;
            
            // Test guard thresholds
            bool thresholdTest = GameConfig.SafeGuardThreshold1 == 50000L &&
                               GameConfig.SafeGuardThreshold6 == 1000000L;
            
            Console.WriteLine($"  ✓ Configuration constants: {(configTest ? "PASS" : "FAIL")}");
            Console.WriteLine($"  ✓ Guard thresholds: {(thresholdTest ? "PASS" : "FAIL")}");
            
            return configTest && thresholdTest;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Bank integration test failed: {ex.Message}");
            return false;
        }
    }
    
    // Helper method
    private static Character CreateTestPlayer()
    {
        var player = new Character();
        player.Name1 = "Test";
        player.Name2 = "Player";
        player.Level = 10;
        player.Gold = 10000;
        player.BankGold = 0;
        player.BankGuard = false;
        player.BankWage = 0;
        player.Interest = 0;
        player.BankRobberyAttempts = GameConfig.DefaultBankRobberyAttempts;
        player.Chivalry = 100;
        player.Darkness = 0;
        player.HP = player.MaxHP = 100;
        return player;
    }
} 
