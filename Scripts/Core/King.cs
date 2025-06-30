using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// King - Pascal-compatible royal court system
/// Based on KingRec from INIT.PAS and CASTLE.PAS
/// </summary>
public class King
{
    // Basic King Information (Pascal KingRec)
    public string Name { get; set; } = "";                    // King's name
    public CharacterAI AI { get; set; } = CharacterAI.Human;  // 'H' for human, 'N' for NPC
    public CharacterSex Sex { get; set; } = CharacterSex.Male; // King's gender
    
    // Royal Treasury (Pascal: treasury)
    public long Treasury { get; set; } = GameConfig.DefaultRoyalTreasury;
    
    // Tax System (Pascal: tax, taxalignment)
    public long TaxRate { get; set; } = 0;                     // Daily tax amount
    public GameConfig.TaxAlignment TaxAlignment { get; set; } = GameConfig.TaxAlignment.All;
    
    // Royal Guard System (Pascal: guard, guardpay, guardai, guardsex arrays)
    public List<RoyalGuard> Guards { get; set; } = new();
    
    // Royal Orders and Establishments (Pascal: various establishment controls)
    public Dictionary<string, bool> EstablishmentStatus { get; set; } = new();
    public string LastProclamation { get; set; } = "";
    public DateTime LastProclamationDate { get; set; } = DateTime.MinValue;
    
    // Royal Court State
    public bool IsActive { get; set; } = true;                 // Is there currently a king?
    public DateTime CoronationDate { get; set; } = DateTime.Now;
    public long TotalReign { get; set; } = 0;                  // Days as ruler
    
    // Prison System Integration
    public Dictionary<string, PrisonRecord> Prisoners { get; set; } = new();
    
    // Royal Orphanage (Pascal: royal orphanage system)
    public List<RoyalOrphan> Orphans { get; set; } = new();
    
    // Court Magic System
    public List<string> AvailableSpells { get; set; } = new();
    public long MagicBudget { get; set; } = 10000;
    
    public King()
    {
        InitializeDefaultEstablishments();
        InitializeDefaultSpells();
    }
    
    /// <summary>
    /// Initialize default establishment statuses
    /// </summary>
    private void InitializeDefaultEstablishments()
    {
        EstablishmentStatus["Inn"] = true;
        EstablishmentStatus["WeaponShop"] = true;
        EstablishmentStatus["ArmorShop"] = true;
        EstablishmentStatus["Bank"] = true;
        EstablishmentStatus["MagicShop"] = true;
        EstablishmentStatus["Healer"] = true;
        EstablishmentStatus["Marketplace"] = true;
        EstablishmentStatus["Church"] = true;
    }
    
    /// <summary>
    /// Initialize default court spells
    /// </summary>
    private void InitializeDefaultSpells()
    {
        AvailableSpells.Add("Royal Blessing");
        AvailableSpells.Add("Detect Evil");
        AvailableSpells.Add("Summon Guards");
        AvailableSpells.Add("Treasury Insight");
        AvailableSpells.Add("Divine Protection");
    }
    
    /// <summary>
    /// Get the royal title based on gender
    /// </summary>
    public string GetTitle()
    {
        return Sex == CharacterSex.Male ? "King" : "Queen";
    }
    
    /// <summary>
    /// Calculate daily expenses for the royal court
    /// </summary>
    public long CalculateDailyExpenses()
    {
        long expenses = 0;
        
        // Guard salaries
        foreach (var guard in Guards)
        {
            expenses += guard.DailySalary;
        }
        
        // Orphan care costs
        expenses += Orphans.Count * GameConfig.OrphanCareCost;
        
        // Base court maintenance
        expenses += 1000; // Base daily cost
        
        return expenses;
    }
    
    /// <summary>
    /// Calculate daily income (taxes)
    /// </summary>
    public long CalculateDailyIncome()
    {
        // TODO: Integrate with player database to calculate actual tax income
        // For now, return estimated income based on tax rate
        return TaxRate * 10; // Estimate based on 10 taxpayers
    }
    
    /// <summary>
    /// Process daily royal court activities
    /// </summary>
    public void ProcessDailyActivities()
    {
        var expenses = CalculateDailyExpenses();
        var income = CalculateDailyIncome();
        
        Treasury += income - expenses;
        
        // Ensure treasury doesn't go negative
        if (Treasury < 0)
        {
            Treasury = 0;
        }
        
        TotalReign++;
        
        // Process prisoner time served
        var prisonersToRelease = new List<string>();
        foreach (var prisoner in Prisoners)
        {
            prisoner.Value.DaysServed++;
            if (prisoner.Value.DaysServed >= prisoner.Value.Sentence)
            {
                prisonersToRelease.Add(prisoner.Key);
            }
        }
        
        // Release prisoners who have served their time
        foreach (var prisonerId in prisonersToRelease)
        {
            Prisoners.Remove(prisonerId);
        }
    }
    
    /// <summary>
    /// Add a guard to the royal guard
    /// </summary>
    public bool AddGuard(string guardName, CharacterAI ai, CharacterSex sex, long salary)
    {
        if (Guards.Count >= GameConfig.MaxRoyalGuards)
            return false;
            
        if (Treasury < GameConfig.GuardRecruitmentCost)
            return false;
            
        var guard = new RoyalGuard
        {
            Name = guardName,
            AI = ai,
            Sex = sex,
            DailySalary = salary,
            RecruitmentDate = DateTime.Now
        };
        
        Guards.Add(guard);
        Treasury -= GameConfig.GuardRecruitmentCost;
        
        return true;
    }
    
    /// <summary>
    /// Remove a guard from the royal guard
    /// </summary>
    public bool RemoveGuard(string guardName)
    {
        var guard = Guards.Find(g => g.Name == guardName);
        if (guard != null)
        {
            Guards.Remove(guard);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Imprison a character
    /// </summary>
    public void ImprisonCharacter(string characterName, int sentence, string crime)
    {
        var prisonRecord = new PrisonRecord
        {
            CharacterName = characterName,
            Crime = crime,
            Sentence = sentence,
            DaysServed = 0,
            ImprisonmentDate = DateTime.Now
        };
        
        Prisoners[characterName] = prisonRecord;
    }
    
    /// <summary>
    /// Release a character from prison (pardon or bail)
    /// </summary>
    public bool ReleaseCharacter(string characterName)
    {
        return Prisoners.Remove(characterName);
    }
    
    /// <summary>
    /// Create a new king (abdication or succession)
    /// </summary>
    public static King CreateNewKing(string name, CharacterAI ai, CharacterSex sex)
    {
        var king = new King
        {
            Name = name,
            AI = ai,
            Sex = sex,
            Treasury = GameConfig.DefaultRoyalTreasury,
            TaxRate = 0,
            TaxAlignment = GameConfig.TaxAlignment.All,
            CoronationDate = DateTime.Now,
            TotalReign = 0
        };
        
        return king;
    }
}

/// <summary>
/// Royal Guard record
/// </summary>
public class RoyalGuard
{
    public string Name { get; set; } = "";
    public CharacterAI AI { get; set; } = CharacterAI.Human;
    public CharacterSex Sex { get; set; } = CharacterSex.Male;
    public long DailySalary { get; set; } = GameConfig.BaseGuardSalary;
    public DateTime RecruitmentDate { get; set; } = DateTime.Now;
    public int Loyalty { get; set; } = 100;           // 0-100, affects guard performance
    public bool IsActive { get; set; } = true;        // Can be temporarily deactivated
}

/// <summary>
/// Prison record for royal justice system
/// </summary>
public class PrisonRecord
{
    public string CharacterName { get; set; } = "";
    public string Crime { get; set; } = "";
    public int Sentence { get; set; } = 1;            // Days in prison
    public int DaysServed { get; set; } = 0;
    public DateTime ImprisonmentDate { get; set; } = DateTime.Now;
    public long BailAmount { get; set; } = 0;         // 0 = no bail allowed
}

/// <summary>
/// Royal orphan under crown protection
/// </summary>
public class RoyalOrphan
{
    public string Name { get; set; } = "";
    public int Age { get; set; } = 10;
    public CharacterSex Sex { get; set; } = CharacterSex.Male;
    public DateTime ArrivalDate { get; set; } = DateTime.Now;
    public string BackgroundStory { get; set; } = "";
    public int Happiness { get; set; } = 50;          // 0-100, affects kingdom morale
} 