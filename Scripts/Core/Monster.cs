using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Monster class based directly on Pascal MonsterRec structure from INIT.PAS
/// Maintains compatibility with original Usurper monster system
/// </summary>
public class Monster
{
    // From Pascal MonsterRec structure
    public string Name { get; set; } = "";              // name of creature
    public long WeapNr { get; set; }                    // weapon used, # points to weapon data file
    public long ArmNr { get; set; }                     // armor used, # points to armor data file
    public bool GrabWeap { get; set; }                  // can weapon be taken?
    public bool GrabArm { get; set; }                   // can armor be taken?
    public string Phrase { get; set; } = "";            // intro phrase from monster
    public int MagicRes { get; set; }                   // magic resistance
    public long Strength { get; set; }                  // strength
    public int Defence { get; set; }                    // defence
    public bool WUser { get; set; }                     // weapon user
    public bool AUser { get; set; }                     // armor user
    public long HP { get; set; }                        // hitpoints
    public long Punch { get; set; }                     // punch, temporary battle var
    public bool Poisoned { get; set; }                  // poisoned?, temporary battle var
    public string Weapon { get; set; } = "";            // name of weapon
    public string Armor { get; set; } = "";             // name of armor
    public bool Disease { get; set; }                   // infected by a disease?
    public int Target { get; set; }                     // target, temporary battle var
    public long WeapPow { get; set; }                   // weapon power
    public long ArmPow { get; set; }                    // armor power
    public int IQ { get; set; }                         // iq
    public int Evil { get; set; }                       // evil (0-100%)
    public byte MagicLevel { get; set; }                // magic level, higher = better magic
    public int Mana { get; set; }                       // mana left
    public int MaxMana { get; set; }                    // max mana
    
    // Monster spells (from Pascal: array[1..global_maxmspells] of boolean)
    public List<bool> Spell { get; set; }               // monster spells
    
    // Additional properties for enhanced functionality
    public int MonsterType { get; set; }                // Type/ID from monster data
    public int DungeonLevel { get; set; }               // What dungeon level this monster appears on
    public bool IsActive { get; set; } = false;        // Is this monster currently active
    public bool IsAlive => HP > 0;                     // Convenience property
    public string Location { get; set; } = "";         // Current location
    public DateTime LastAction { get; set; }           // When monster last acted
    
    // Combat state
    public bool InCombat { get; set; } = false;
    public string CombatTarget { get; set; } = "";
    public int CombatRound { get; set; } = 0;
    
    // Special monster flags
    public bool IsBoss { get; set; } = false;
    public bool IsUnique { get; set; } = false;
    public bool CanSpeak { get; set; } = false;         // From Pascal mon_talk setting
    
    // Additional properties for API compatibility
    public int Armour { get; set; }
    public int Special { get; set; }
    public int Undead { get; set; }
    
    // Additional properties for API compatibility
    public string WeaponName { get; set; } = "";
    public int WeaponId { get; set; }
    public string ArmorName { get; set; } = "";
    public int ArmorId { get; set; }
    public int Level { get; set; } = 1;
    
    // Combat properties for AdvancedCombatEngine
    public long WeaponPower { get; set; }       // Weapon power (settable)
    public long Gold { get; set; }              // Gold carried by monster
    public bool CanGrabWeapon { get; set; } = false;  // Can steal weapons
    public bool CanGrabArmor { get; set; } = false;   // Can steal armor
    
    // Additional missing properties for API compatibility
    public long Experience { get; set; }        // Experience points given when defeated
    public long ArmorPower { get; set; }        // Armor power (settable)
    public long MaxHP { get; set; }             // Maximum hit points
    
    // Simple status counters for combat effects
    public int PoisonRounds { get; set; } = 0;
    public int StunRounds { get; set; } = 0;
    public int WeakenRounds { get; set; } = 0;
    
    /// <summary>
    /// Constructor for creating a monster
    /// </summary>
    public Monster()
    {
        // Initialize spells list
        Spell = new List<bool>(new bool[GameConfig.MaxMSpells]);
        LastAction = DateTime.Now;
    }
    
    /// <summary>
    /// Create monster from Pascal data - matches Create_Monster procedure
    /// </summary>
    public static Monster CreateMonster(int nr, string name, long hps, long strength, long defence,
        string phrase, bool grabweap, bool grabarm, string weapon, string armor,
        bool poisoned, bool disease, long punch, long armpow, long weappow)
    {
        var monster = new Monster
        {
            MonsterType = nr,
            Name = name,
            HP = hps,
            Strength = strength,
            Defence = (int)defence,
            Phrase = phrase,
            GrabWeap = grabweap,
            GrabArm = grabarm,
            Weapon = weapon,
            Armor = armor,
            Poisoned = poisoned,
            Disease = disease,
            Punch = punch,
            ArmPow = armpow,
            WeapPow = weappow,
            IsActive = true
        };
        
        // Set derived properties
        monster.WUser = !string.IsNullOrEmpty(weapon);
        monster.AUser = !string.IsNullOrEmpty(armor);
        monster.CanSpeak = GameConfig.MonsterTalk; // From config
        
        // Generate other stats based on monster type
        monster.GenerateMonsterStats(nr);
        
        return monster;
    }
    
    /// <summary>
    /// Generate additional monster stats based on type
    /// </summary>
    private void GenerateMonsterStats(int monsterType)
    {
        // Set IQ based on monster type
        IQ = monsterType switch
        {
            <= 5 => GD.RandRange(20, 40),      // Low-level monsters
            <= 10 => GD.RandRange(40, 60),     // Mid-level monsters
            <= 15 => GD.RandRange(60, 80),     // High-level monsters
            _ => GD.RandRange(80, 100)          // Boss monsters
        };
        
        // Set evil rating
        Evil = monsterType switch
        {
            <= 3 => GD.RandRange(20, 50),      // Neutral creatures
            <= 8 => GD.RandRange(50, 80),      // Somewhat evil
            _ => GD.RandRange(80, 100)          // Very evil
        };
        
        // Set magic resistance
        MagicRes = monsterType / 2 + GD.RandRange(0, 20);
        
        // Set magic level and mana for spell-casting monsters
        if (monsterType >= 8)
        {
            MagicLevel = (byte)GD.RandRange(1, 6);
            MaxMana = monsterType * 5 + GD.RandRange(10, 30);
            Mana = MaxMana;
            
            // Give some spells to magical monsters
            for (int i = 0; i < Math.Min((int)MagicLevel, GameConfig.MaxMSpells); i++)
            {
                if (GD.Randf() < 0.6f) // 60% chance per spell
                {
                    Spell[i] = true;
                }
            }
        }
        
        // Set dungeon level appearance
        DungeonLevel = Math.Max(1, (monsterType / 3) + GD.RandRange(-1, 2));
        
        // Check for boss status
        if (monsterType >= 15 || Name.ToLower().Contains("boss") || Name.ToLower().Contains("lord"))
        {
            IsBoss = true;
            HP = (long)(HP * 1.5f);  // Bosses get more HP
            Strength = (long)(Strength * 1.2f);
        }
        
        // Check for unique status
        if (Name.ToLower().Contains("death knight") || Name.ToLower().Contains("supreme"))
        {
            IsUnique = true;
            IsBoss = true;
        }
    }
    
    /// <summary>
    /// Get monster's intro phrase (Pascal compatible)
    /// </summary>
    public string GetIntroPhrase()
    {
        if (!CanSpeak || string.IsNullOrEmpty(Phrase))
        {
            return ""; // Silent monsters
        }
        
        // Special handling for unique monsters
        if (Name.ToLower().Contains("seth able"))
        {
            var phrases = new[]
            {
                "You lookin' at me funny?!",
                "*hiccup* Want to fight?",
                "I can take anyone in this place!"
            };
            return phrases[GD.RandRange(0, phrases.Length - 1)];
        }
        
        return Phrase;
    }
    
    /// <summary>
    /// Calculate monster's total attack power (Pascal compatible)
    /// </summary>
    public long GetAttackPower()
    {
        long attack = Strength + WeapPow + Punch;
        
        // Add bonus for boss monsters
        if (IsBoss)
        {
            attack = (long)(attack * 1.3f);
        }
        
        // Add poison damage if poisoned
        if (Poisoned)
        {
            attack += 5;
        }
        
        return Math.Max(1, attack);
    }
    
    /// <summary>
    /// Calculate monster's total defense power (Pascal compatible)
    /// </summary>
    public long GetDefensePower()
    {
        long defense = Defence + ArmPow;
        
        // Add bonus for boss monsters
        if (IsBoss)
        {
            defense = (long)(defense * 1.2f);
        }
        
        return Math.Max(0, defense);
    }
    
    /// <summary>
    /// Take damage (Pascal compatible)
    /// </summary>
    public void TakeDamage(long damage)
    {
        var actualDamage = Math.Max(1, damage - GetDefensePower());
        HP = Math.Max(0, HP - actualDamage);
        
        if (HP <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Monster dies
    /// </summary>
    private void Die()
    {
        IsActive = false;
        InCombat = false;
        CombatTarget = "";
    }
    
    /// <summary>
    /// Cast spell if monster has mana and spells
    /// </summary>
    public MonsterSpellResult CastSpell(Character target)
    {
        if (Mana <= 0 || MagicLevel == 0)
        {
            return new MonsterSpellResult { Success = false, Message = "No mana or magic ability" };
        }
        
        // Find available spells
        var availableSpells = new List<int>();
        for (int i = 0; i < Spell.Count; i++)
        {
            if (Spell[i])
            {
                availableSpells.Add(i);
            }
        }
        
        if (availableSpells.Count == 0)
        {
            return new MonsterSpellResult { Success = false, Message = "No spells known" };
        }
        
        // Cast random spell
        var spellIndex = availableSpells[GD.RandRange(0, availableSpells.Count - 1)];
        var spellCost = 5 + (spellIndex * 2);
        
        if (Mana < spellCost)
        {
            return new MonsterSpellResult { Success = false, Message = "Insufficient mana" };
        }
        
        Mana -= spellCost;
        return CastSpellByIndex(spellIndex, target);
    }
    
    /// <summary>
    /// Cast specific spell by index
    /// </summary>
    private MonsterSpellResult CastSpellByIndex(int spellIndex, Character target)
    {
        var result = new MonsterSpellResult { Success = true };
        
        switch (spellIndex)
        {
            case 0: // Heal
                var healAmount = MagicLevel * 10 + GD.RandRange(5, 15);
                HP = Math.Min((int)HP + (int)healAmount, (int)GetMaxHP());
                result.Message = $"{Name} heals for {healAmount} points!";
                break;
                
            case 1: // Magic missile
                var damage = MagicLevel * 8 + GD.RandRange(3, 12);
                result.Damage = damage;
                result.Message = $"{Name} casts magic missile for {damage} damage!";
                break;
                
            case 2: // Poison
                result.SpecialEffect = "poison";
                result.Message = $"{Name} casts a poison spell!";
                break;
                
            case 3: // Weakness
                result.SpecialEffect = "weakness";
                result.Message = $"{Name} casts a weakness spell!";
                break;
                
            case 4: // Fear
                result.SpecialEffect = "fear";
                result.Message = $"{Name} casts a fear spell!";
                break;
                
            case 5: // Death spell
                if (MagicLevel >= 5 && GD.Randf() < 0.1f) // 10% chance, high level only
                {
                    result.Damage = target.HP; // Instant death
                    result.Message = $"{Name} casts DEATH! You feel your life force drain away!";
                }
                else
                {
                    result.Damage = MagicLevel * 15;
                    result.Message = $"{Name} casts a death spell for {result.Damage} damage!";
                }
                break;
                
            default:
                result.Success = false;
                result.Message = "Unknown spell";
                break;
        }
        
        return result;
    }
    
    /// <summary>
    /// Get maximum HP for this monster type
    /// </summary>
    private long GetMaxHP()
    {
        // Calculate based on monster type and stats
        return Strength * 2 + (MonsterType * 10);
    }
    
    /// <summary>
    /// Monster AI decision making (simplified)
    /// </summary>
    public MonsterAction DecideAction(Character target)
    {
        if (!IsAlive)
        {
            return new MonsterAction { Type = MonsterActionType.Death };
        }
        
        // If low on HP and can heal, try to heal
        if (HP < GetMaxHP() / 3 && Mana >= 5 && Spell[0] && GD.Randf() < 0.7f)
        {
            return new MonsterAction { Type = MonsterActionType.CastSpell, SpellIndex = 0 };
        }
        
        // If has offensive spells and mana, sometimes cast spells
        if (Mana >= 10 && MagicLevel > 0 && GD.Randf() < 0.3f)
        {
            for (int i = 1; i < Spell.Count; i++)
            {
                if (Spell[i])
                {
                    return new MonsterAction { Type = MonsterActionType.CastSpell, SpellIndex = i };
                }
            }
        }
        
        // Otherwise, attack
        return new MonsterAction { Type = MonsterActionType.Attack };
    }
    
    /// <summary>
    /// Get experience reward for defeating this monster
    /// </summary>
    public long GetExperienceReward()
    {
        long baseExp = (Strength + Defence + WeapPow + ArmPow) / 4;
        baseExp += MonsterType * 10;
        
        if (IsBoss)
        {
            baseExp *= 3;
        }
        
        if (IsUnique)
        {
            baseExp *= 5;
        }
        
        return Math.Max(10, baseExp);
    }
    
    /// <summary>
    /// Get gold reward for defeating this monster
    /// </summary>
    public long GetGoldReward()
    {
        long baseGold = MonsterType * 5 + GD.RandRange(1, 20);
        
        if (IsBoss)
        {
            baseGold *= 2;
        }
        
        if (IsUnique)
        {
            baseGold *= 4;
        }
        
        return Math.Max(1, baseGold);
    }
    
    /// <summary>
    /// Get display information for terminal
    /// </summary>
    public string GetDisplayInfo()
    {
        var status = "";
        if (IsBoss) status += " [BOSS]";
        if (IsUnique) status += " [UNIQUE]";
        if (Poisoned) status += " [POISONED]";
        if (Disease) status += " [DISEASED]";
        
        return $"{Name} (Level {MonsterType}) - HP: {HP}{status}";
    }
    
    /// <summary>
    /// Reset monster for reuse (Pascal monster recycling)
    /// </summary>
    public void Reset()
    {
        IsActive = false;
        InCombat = false;
        CombatTarget = "";
        CombatRound = 0;
        Poisoned = false;
        Disease = false;
        Target = 0;
        Punch = 0;
        Mana = MaxMana;
        LastAction = DateTime.Now;
    }
}

/// <summary>
/// Result of a monster spell cast
/// </summary>
public class MonsterSpellResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public long Damage { get; set; }
    public string SpecialEffect { get; set; } = "";
}

/// <summary>
/// Monster action for AI
/// </summary>
public class MonsterAction
{
    public MonsterActionType Type { get; set; }
    public int SpellIndex { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>
/// Types of actions a monster can take
/// </summary>
public enum MonsterActionType
{
    Attack,
    CastSpell,
    Defend,
    Flee,
    Death
} 
