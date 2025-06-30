using Godot;
using System;
using System.Collections.Generic;

public class Character
{
    public string Id { get; set; }
    public string Name { get; set; }
    public CharacterClass Class { get; set; }
    public int Level { get; set; } = 1;
    public long Experience { get; set; } = 0;
    
    // Core Stats
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Constitution { get; set; }
    public int Intelligence { get; set; }
    public int Wisdom { get; set; }
    public int Charisma { get; set; }
    
    // Health and Mana
    public int CurrentHP { get; set; }
    public int MaxHP { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    
    // Resources
    public int Gold { get; set; }
    public int TurnsRemaining { get; set; }
    
    // Location and Status
    public string CurrentLocation { get; set; } = "town_square";
    public bool IsAlive => CurrentHP > 0;
    public bool IsRuler { get; set; } = false;
    
    // Equipment
    public Equipment EquippedWeapon { get; set; }
    public Equipment EquippedArmor { get; set; }
    public Equipment EquippedShield { get; set; }
    public List<Equipment> Inventory { get; set; } = new List<Equipment>();
    
    // Active Effects
    public Dictionary<string, StatusEffect> ActiveEffects { get; set; } = new Dictionary<string, StatusEffect>();
    
    // Gang/Team
    public string GangId { get; set; }
    public List<string> GangMembers { get; set; } = new List<string>();
    
    public Character()
    {
        Id = Guid.NewGuid().ToString();
    }
    
    public Character(string name, CharacterClass charClass) : this()
    {
        Name = name;
        Class = charClass;
        InitializeStats();
    }
    
    private void InitializeStats()
    {
        var classData = CharacterDataManager.GetClassData(Class);
        if (classData != null)
        {
            Strength = classData.BaseStats["strength"];
            Dexterity = classData.BaseStats["dexterity"];
            Constitution = classData.BaseStats["constitution"];
            Intelligence = classData.BaseStats["intelligence"];
            Wisdom = classData.BaseStats["wisdom"];
            Charisma = classData.BaseStats["charisma"];
            
            MaxHP = classData.BaseHP;
            CurrentHP = MaxHP;
            MaxMana = classData.BaseMana;
            CurrentMana = MaxMana;
        }
    }
    
    public void GainExperience(long exp)
    {
        Experience += exp;
        CheckLevelUp();
    }
    
    private void CheckLevelUp()
    {
        var expTable = CharacterDataManager.GetExperienceTable();
        while (Level < expTable.Length - 1 && Experience >= expTable[Level])
        {
            LevelUp();
        }
    }
    
    private void LevelUp()
    {
        Level++;
        var classData = CharacterDataManager.GetClassData(Class);
        
        if (classData != null)
        {
            // Increase stats
            Strength += classData.StatProgression["strength"];
            Dexterity += classData.StatProgression["dexterity"];
            Constitution += classData.StatProgression["constitution"];
            Intelligence += classData.StatProgression["intelligence"];
            Wisdom += classData.StatProgression["wisdom"];
            Charisma += classData.StatProgression["charisma"];
            
            // Increase HP and Mana
            var hpGain = classData.HPPerLevel + GetConstitutionBonus();
            var manaGain = classData.ManaPerLevel + GetIntelligenceBonus();
            
            MaxHP += hpGain;
            CurrentHP += hpGain; // Full heal on level up
            MaxMana += manaGain;
            CurrentMana += manaGain;
        }
        
        // Trigger level up event
        OnLevelUp();
    }
    
    protected virtual void OnLevelUp()
    {
        // Override in derived classes for specific behavior
    }
    
    public int GetAttackPower()
    {
        var baseDamage = GetStrengthBonus();
        var weaponDamage = EquippedWeapon?.Damage ?? 1;
        return baseDamage + weaponDamage;
    }
    
    public int GetArmorClass()
    {
        var baseAC = 10 + GetDexterityBonus();
        var armorBonus = EquippedArmor?.ArmorClass ?? 0;
        var shieldBonus = EquippedShield?.ArmorClass ?? 0;
        return baseAC + armorBonus + shieldBonus;
    }
    
    public int GetStrengthBonus() => (Strength - 10) / 2;
    public int GetDexterityBonus() => (Dexterity - 10) / 2;
    public int GetConstitutionBonus() => (Constitution - 10) / 2;
    public int GetIntelligenceBonus() => (Intelligence - 10) / 2;
    public int GetWisdomBonus() => (Wisdom - 10) / 2;
    public int GetCharismaBonus() => (Charisma - 10) / 2;
    
    public void AddEffect(string effectName, int durationMinutes, Dictionary<string, object> parameters = null)
    {
        var effect = new StatusEffect
        {
            Name = effectName,
            Duration = durationMinutes,
            StartTime = DateTime.Now,
            Parameters = parameters ?? new Dictionary<string, object>()
        };
        
        ActiveEffects[effectName] = effect;
    }
    
    public void RemoveEffect(string effectName)
    {
        ActiveEffects.Remove(effectName);
    }
    
    public bool HasEffect(string effectName)
    {
        return ActiveEffects.ContainsKey(effectName) && !ActiveEffects[effectName].IsExpired();
    }
    
    public void UpdateEffects()
    {
        var expiredEffects = new List<string>();
        
        foreach (var kvp in ActiveEffects)
        {
            if (kvp.Value.IsExpired())
            {
                expiredEffects.Add(kvp.Key);
            }
        }
        
        foreach (var effectName in expiredEffects)
        {
            ActiveEffects.Remove(effectName);
        }
    }
    
    public void TakeDamage(int damage)
    {
        CurrentHP = Math.Max(0, CurrentHP - damage);
    }
    
    public void Heal(int amount)
    {
        CurrentHP = Math.Min(MaxHP, CurrentHP + amount);
    }
    
    public bool CanAfford(int cost)
    {
        return Gold >= cost;
    }
    
    public bool SpendGold(int amount)
    {
        if (CanAfford(amount))
        {
            Gold -= amount;
            return true;
        }
        return false;
    }
    
    public void GainGold(int amount)
    {
        Gold += amount;
    }
    
    public bool HasTurns()
    {
        return TurnsRemaining > 0;
    }
    
    public void SpendTurn()
    {
        if (TurnsRemaining > 0)
            TurnsRemaining--;
    }
    
    public virtual string GetDisplayInfo()
    {
        return $"{Name} (Level {Level} {Class})";
    }
}

public enum CharacterClass
{
    Warrior,
    Thief,
    Cleric,
    Mage
}

public class StatusEffect
{
    public string Name { get; set; }
    public int Duration { get; set; } // in minutes
    public DateTime StartTime { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    
    public bool IsExpired()
    {
        return DateTime.Now.Subtract(StartTime).TotalMinutes >= Duration;
    }
    
    public int RemainingMinutes()
    {
        var elapsed = DateTime.Now.Subtract(StartTime).TotalMinutes;
        return Math.Max(0, Duration - (int)elapsed);
    }
}

public class Equipment
{
    public string Id { get; set; }
    public string Name { get; set; }
    public EquipmentType Type { get; set; }
    public int Damage { get; set; } // For weapons
    public int ArmorClass { get; set; } // For armor/shields
    public int Value { get; set; }
    public Dictionary<string, int> StatBonuses { get; set; } = new Dictionary<string, int>();
    public string Description { get; set; }
}

public enum EquipmentType
{
    Weapon,
    Armor,
    Shield,
    Accessory
} 