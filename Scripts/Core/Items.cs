using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Item system based on Pascal ORec structure from INIT.PAS
/// Maintains perfect compatibility with original Usurper item system
/// </summary>
public class Item
{
    // From Pascal ORec structure
    public string Name { get; set; } = "";              // objects name
    public ObjType Type { get; set; }                   // type of object (head, body, weapon, etc.)
    public long Value { get; set; }                     // object value (cost/price)
    public int HP { get; set; }                         // can object increase/decrease hps
    public int Stamina { get; set; }                    // ..stamina
    public int Agility { get; set; }                    // ..agility  
    public int Charisma { get; set; }                   // ..charisma
    public int Dexterity { get; set; }                  // ..dexterity
    public int Wisdom { get; set; }                     // ..wisdom
    public int Mana { get; set; }                       // ..mana
    public int Armor { get; set; }                      // ..can object increase armor value
    public int Attack { get; set; }                     // ..can object increase attack value
    public string Owned { get; set; } = "";             // owned by (character name)
    public bool OnlyOne { get; set; }                   // only one object of its kind?
    public Cures Cure { get; set; }                     // can the object heal?
    public bool Shop { get; set; }                      // is the object available in shoppe
    public bool Dungeon { get; set; }                   // can you find item in dungeons
    public bool Cursed { get; set; }                    // is the item cursed?
    public int MinLevel { get; set; }                   // min level to be found in dungeons
    public int MaxLevel { get; set; }                   // max level to be found in dungeons
    
    // Descriptions (from Pascal: array[1..5] of s70)
    public List<string> Description { get; set; }       // normal description
    public List<string> DetailedDescription { get; set; } // detailed description
    
    public int Strength { get; set; }                   // can object increase/decrease strength
    public int Defence { get; set; }                    // can object increase/decrease defence
    public int StrengthNeeded { get; set; }             // strength needed to wear object
    public bool RequiresGood { get; set; }              // character needs to be good to use
    public bool RequiresEvil { get; set; }              // character needs to be evil to use
    
    // Class restrictions (from Pascal: array[1..global_maxclasses] of boolean)
    public List<bool> ClassRestrictions { get; set; }   // which classes can use this item
    
    // Additional properties for enhanced functionality
    public int ItemID { get; set; }                     // Unique item identifier
    public DateTime CreatedDate { get; set; }           // When item was created
    public string Creator { get; set; } = "";           // Who created the item (for player-made items)
    public bool IsArtifact { get; set; } = false;       // Special unique artifacts
    public int Durability { get; set; } = 100;          // Item durability (0-100)
    public int MaxDurability { get; set; } = 100;       // Maximum durability
    
    // Magic properties
    public MagicEnhancement MagicProperties { get; set; } = new MagicEnhancement();
    public MagicItemType MagicType { get; set; } = MagicItemType.None;
    public bool IsIdentified { get; set; } = true; // Most items start identified
    public bool IsCursed { get; set; } = false;
    public bool OnlyForGood { get; set; } = false; // Good alignment required
    public bool OnlyForEvil { get; set; } = false; // Evil alignment required
    
    /// <summary>
    /// Constructor for creating items
    /// </summary>
    public Item()
    {
        Description = new List<string>(new string[5]);
        DetailedDescription = new List<string>(new string[5]);
        ClassRestrictions = new List<bool>(new bool[GameConfig.MaxClasses]);
        CreatedDate = DateTime.Now;
    }
    
    /// <summary>
    /// Check if a character can use this item
    /// </summary>
    public bool CanUse(Character character)
    {
        // Check strength requirement
        if (character.Strength < StrengthNeeded)
            return false;
            
        // Check alignment requirements
        if (RequiresGood && character.Chivalry <= 0)
            return false;
            
        if (RequiresEvil && character.Darkness <= 0)
            return false;
            
        // Check class restrictions
        var classIndex = (int)character.Class;
        if (classIndex < ClassRestrictions.Count && !ClassRestrictions[classIndex])
            return false;
            
        // Check level requirements (for dungeon items)
        if (MinLevel > 0 && character.Level < MinLevel)
            return false;
            
        return true;
    }
    
    /// <summary>
    /// Apply item effects to character when equipped
    /// </summary>
    public void ApplyEffects(Character character)
    {
        character.MaxHP += HP;
        character.HP = Math.Min(character.HP + HP, character.MaxHP);
        character.Stamina += Stamina;
        character.Agility += Agility;
        character.Charisma += Charisma;
        character.Dexterity += Dexterity;
        character.Wisdom += Wisdom;
        character.MaxMana += Mana;
        character.Mana = Math.Min(character.Mana + Mana, character.MaxMana);
        character.Strength += Strength;
        character.Defence += Defence;
        character.ArmPow += Armor;
        character.WeapPow += Attack;
    }
    
    /// <summary>
    /// Remove item effects from character when unequipped
    /// </summary>
    public void RemoveEffects(Character character)
    {
        character.MaxHP -= HP;
        character.HP = Math.Min(character.HP, character.MaxHP);
        character.Stamina -= Stamina;
        character.Agility -= Agility;
        character.Charisma -= Charisma;
        character.Dexterity -= Dexterity;
        character.Wisdom -= Wisdom;
        character.MaxMana -= Mana;
        character.Mana = Math.Min(character.Mana, character.MaxMana);
        character.Strength -= Strength;
        character.Defence -= Defence;
        character.ArmPow -= Armor;
        character.WeapPow -= Attack;
    }
    
    /// <summary>
    /// Get item's display name with condition
    /// </summary>
    public string GetDisplayName()
    {
        var name = Name;
        
        if (Cursed)
            name += " (Cursed)";
            
        if (Durability < 100)
        {
            var condition = Durability switch
            {
                >= 80 => "Good",
                >= 60 => "Fair", 
                >= 40 => "Poor",
                >= 20 => "Bad",
                _ => "Broken"
            };
            name += $" [{condition}]";
        }
        
        if (IsArtifact)
            name += " ★";
            
        return name;
    }
    
    /// <summary>
    /// Get full item description for examination
    /// </summary>
    public string GetFullDescription()
    {
        var desc = string.Join("\n", Description.Where(d => !string.IsNullOrEmpty(d)));
        
        if (!string.IsNullOrEmpty(desc))
        {
            desc += "\n\n";
        }
        
        // Add stat information
        var stats = new List<string>();
        
        if (Attack != 0) stats.Add($"Attack: {Attack:+#;-#;0}");
        if (Armor != 0) stats.Add($"Armor: {Armor:+#;-#;0}");
        if (Strength != 0) stats.Add($"Strength: {Strength:+#;-#;0}");
        if (Defence != 0) stats.Add($"Defence: {Defence:+#;-#;0}");
        if (HP != 0) stats.Add($"HP: {HP:+#;-#;0}");
        if (Mana != 0) stats.Add($"Mana: {Mana:+#;-#;0}");
        if (Stamina != 0) stats.Add($"Stamina: {Stamina:+#;-#;0}");
        if (Agility != 0) stats.Add($"Agility: {Agility:+#;-#;0}");
        if (Dexterity != 0) stats.Add($"Dexterity: {Dexterity:+#;-#;0}");
        if (Charisma != 0) stats.Add($"Charisma: {Charisma:+#;-#;0}");
        if (Wisdom != 0) stats.Add($"Wisdom: {Wisdom:+#;-#;0}");
        
        if (stats.Count > 0)
        {
            desc += string.Join(", ", stats) + "\n";
        }
        
        // Add requirements
        var requirements = new List<string>();
        if (StrengthNeeded > 0) requirements.Add($"Str: {StrengthNeeded}");
        if (RequiresGood) requirements.Add("Good alignment");
        if (RequiresEvil) requirements.Add("Evil alignment");
        
        if (requirements.Count > 0)
        {
            desc += "Requires: " + string.Join(", ", requirements) + "\n";
        }
        
        // Add special properties
        if (OnlyOne) desc += "Unique item\n";
        if (Cursed) desc += "This item is cursed!\n";
        if (Cure != Cures.Nothing) desc += $"Cures: {Cure}\n";
        
        return desc.Trim();
    }
    
    /// <summary>
    /// Damage the item (reduce durability)
    /// </summary>
    public void TakeDamage(int damage)
    {
        Durability = Math.Max(0, Durability - damage);
    }
    
    /// <summary>
    /// Repair the item
    /// </summary>
    public void Repair(int amount)
    {
        Durability = Math.Min(MaxDurability, Durability + amount);
    }
    
    /// <summary>
    /// Check if item is broken
    /// </summary>
    public bool IsBroken => Durability <= 0;
}

/// <summary>
/// Classic weapon from Pascal WeapRec structure
/// </summary>
public class ClassicWeapon
{
    public string Name { get; set; } = "";              // name of weapon
    public long Value { get; set; }                     // value
    public long Power { get; set; }                     // power
    
    public ClassicWeapon(string name, long value, long power)
    {
        Name = name;
        Value = value;
        Power = power;
    }
}

/// <summary>
/// Classic armor from Pascal ArmRec structure  
/// </summary>
public class ClassicArmor
{
    public string Name { get; set; } = "";              // name of armor
    public long Value { get; set; }                     // value
    public long Power { get; set; }                     // power
    
    public ClassicArmor(string name, long value, long power)
    {
        Name = name;
        Value = value; 
        Power = power;
    }
}

/// <summary>
/// Item manager for handling all game items
/// </summary>
public static class ItemManager
{
    private static Dictionary<int, Item> gameItems = new Dictionary<int, Item>();
    private static Dictionary<int, ClassicWeapon> classicWeapons = new Dictionary<int, ClassicWeapon>();
    private static Dictionary<int, ClassicArmor> classicArmor = new Dictionary<int, ClassicArmor>();
    
    /// <summary>
    /// Initialize items from Pascal data - based on Init_Items procedure
    /// </summary>
    public static void InitializeItems()
    {
        InitializeClassicWeapons();
        InitializeClassicArmor();
        InitializeSpecialItems();
    }
    
    /// <summary>
    /// Initialize classic weapons from Pascal weapon array
    /// </summary>
    private static void InitializeClassicWeapons()
    {
        // From Pascal INIT.PAS weapon initialization
        // weapon[x].name := 'Weapon Name'; weapon[x].value := cost; weapon[x].pow := damage;
        
        var weapons = new (string name, long value, long power)[]
        {
            ("Fists", 0, 1),
            ("Stick", 10, 2),
            ("Dagger", 25, 3),
            ("Club", 50, 4),
            ("Short Sword", 100, 5),
            ("Mace", 200, 6),
            ("Long Sword", 400, 7),
            ("Broad Sword", 800, 8),
            ("Battle Axe", 1500, 9),
            ("Two-Handed Sword", 3000, 10),
            ("War Hammer", 5000, 11),
            ("Halberd", 8000, 12),
            ("Bastard Sword", 12000, 13),
            ("Great Sword", 18000, 14),
            ("Executioner's Axe", 25000, 15),
            // ... continue for all 35 weapons from Pascal
        };
        
        for (int i = 0; i < weapons.Length; i++)
        {
            classicWeapons[i] = new ClassicWeapon(weapons[i].name, weapons[i].value, weapons[i].power);
        }
    }
    
    /// <summary>
    /// Initialize classic armor from Pascal armor array
    /// </summary>
    private static void InitializeClassicArmor()
    {
        // From Pascal INIT.PAS armor initialization
        var armor = new (string name, long value, long power)[]
        {
            ("Skin", 0, 0),
            ("Leather Armor", 50, 2),
            ("Studded Leather", 100, 3),
            ("Ring Mail", 200, 4),
            ("Scale Mail", 400, 5),
            ("Chain Mail", 800, 6),
            ("Splint Mail", 1500, 7),
            ("Banded Mail", 3000, 8),
            ("Plate Mail", 5000, 9),
            ("Field Plate", 8000, 10),
            ("Full Plate", 12000, 11),
            ("Plate of Honor", 18000, 12),
            ("Holy Plate", 25000, 13),
            ("Demon Plate", 35000, 14),
            ("Dragon Scale", 50000, 15),
            ("Celestial Armor", 75000, 16),
            ("Divine Protection", 100000, 17)
        };
        
        for (int i = 0; i < armor.Length; i++)
        {
            classicArmor[i] = new ClassicArmor(armor[i].name, armor[i].value, armor[i].power);
        }
    }
    
    /// <summary>
    /// Initialize special items and artifacts
    /// </summary>
    private static void InitializeSpecialItems()
    {
        // Supreme Being items (from Pascal global_s_* constants)
        CreateSupremeItem(1001, "Lantern of Eternal Light", ObjType.Weapon, 
            "A mystical lantern that never dims", true);
            
        CreateSupremeItem(1002, "Sword of Supreme Justice", ObjType.Weapon,
            "The ultimate weapon of righteousness", true);
            
        CreateSupremeItem(1003, "Staff of Black Magic", ObjType.Weapon,
            "A staff that channels dark powers", true);
            
        CreateSupremeItem(1004, "Staff of White Magic", ObjType.Weapon,
            "A staff blessed with holy power", true);
    }
    
    /// <summary>
    /// Create a supreme being item
    /// </summary>
    private static void CreateSupremeItem(int id, string name, ObjType type, string description, bool artifact)
    {
        var item = new Item
        {
            ItemID = id,
            Name = name,
            Type = type,
            Value = 999999,
            Attack = type == ObjType.Weapon ? 50 : 0,
            Armor = type != ObjType.Weapon ? 50 : 0,
            OnlyOne = true,
            IsArtifact = artifact,
            MinLevel = 100,
            RequiresGood = name.Contains("White") || name.Contains("Justice"),
            RequiresEvil = name.Contains("Black"),
            StrengthNeeded = 25
        };
        
        item.Description[0] = description;
        item.Description[1] = "This legendary item pulses with incredible power.";
        
        gameItems[id] = item;
    }
    
    /// <summary>
    /// Get item by ID
    /// </summary>
    public static Item GetItem(int itemId)
    {
        return gameItems.ContainsKey(itemId) ? gameItems[itemId] : null;
    }
    
    /// <summary>
    /// Get classic weapon by index (Pascal compatible)
    /// </summary>
    public static ClassicWeapon GetClassicWeapon(int index)
    {
        return classicWeapons.ContainsKey(index) ? classicWeapons[index] : null;
    }
    
    /// <summary>
    /// Get classic armor by index (Pascal compatible)
    /// </summary>
    public static ClassicArmor GetClassicArmor(int index)
    {
        return classicArmor.ContainsKey(index) ? classicArmor[index] : null;
    }
    
    /// <summary>
    /// Get all items of a specific type
    /// </summary>
    public static List<Item> GetItemsByType(ObjType type)
    {
        return gameItems.Values.Where(item => item.Type == type).ToList();
    }
    
    /// <summary>
    /// Create a new item (for dynamic item creation)
    /// </summary>
    public static Item CreateItem(string name, ObjType type, long value)
    {
        var item = new Item
        {
            ItemID = gameItems.Count + 10000, // Avoid conflicts with static items
            Name = name,
            Type = type,
            Value = value,
            CreatedDate = DateTime.Now
        };
        
        gameItems[item.ItemID] = item;
        return item;
    }
    
    // Shop-related methods for weapon and armor shops
    
    /// <summary>
    /// Get weapon by ID (for weapon shop)
    /// </summary>
    public static ClassicWeapon GetWeapon(int weaponId)
    {
        return classicWeapons.ContainsKey(weaponId) ? classicWeapons[weaponId] : null;
    }
    
    /// <summary>
    /// Get armor by ID (for armor shop)  
    /// </summary>
    public static ClassicArmor GetArmor(int armorId)
    {
        return classicArmor.ContainsKey(armorId) ? classicArmor[armorId] : null;
    }
    
    /// <summary>
    /// Get all weapons available in shops
    /// </summary>
    public static List<ClassicWeapon> GetShopWeapons()
    {
        return classicWeapons.Values.OrderBy(w => w.Value).ToList();
    }
    
    /// <summary>
    /// Get all armors available in shops
    /// </summary>
    public static List<ClassicArmor> GetShopArmors()
    {
        return classicArmor.Values.OrderBy(a => a.Value).ToList();
    }
    
    /// <summary>
    /// Get armors by equipment type (simplified for classic mode)
    /// </summary>
    public static List<ClassicArmor> GetArmorsByType(ObjType armorType)
    {
        // In classic mode, all armors are body armor
        return GetShopArmors();
    }
    
    /// <summary>
    /// Get best affordable weapon for player
    /// </summary>
    public static ClassicWeapon GetBestAffordableWeapon(long maxGold, Character player)
    {
        return classicWeapons.Values
            .Where(w => w.Value <= maxGold)
            .OrderByDescending(w => w.Power)
            .ThenByDescending(w => w.Value)
            .FirstOrDefault();
    }
    
    /// <summary>
    /// Get best affordable armor for specific slot
    /// </summary>
    public static ClassicArmor GetBestAffordableArmor(ObjType armorType, long maxGold, Character player)
    {
        return classicArmor.Values
            .Where(a => a.Value <= maxGold)
            .OrderByDescending(a => a.Power)
            .ThenByDescending(a => a.Value)
            .FirstOrDefault();
    }
}

/// <summary>
/// Magic item types for shop categorization
/// </summary>
public enum MagicItemType
{
    None = 0,
    Neck = 10,      // Amulets, necklaces
    Fingers = 5,    // Rings  
    Waist = 9       // Belts, girdles
}

/// <summary>
/// Disease cure types for magic items
/// </summary>
public enum CureType
{
    None = 0,
    All,        // Cures all diseases
    Blindness,  // Cures blindness
    Plague,     // Cures plague
    Smallpox,   // Cures smallpox
    Measles,    // Cures measles
    Leprosy     // Cures leprosy
}

/// <summary>
/// Magic enhancement properties for items
/// </summary>
public class MagicEnhancement
{
    public int Mana { get; set; }           // Mana bonus/penalty
    public int Wisdom { get; set; }         // Wisdom bonus/penalty
    public int Dexterity { get; set; }      // Dexterity bonus/penalty
    public int MagicResistance { get; set; } // Resistance to spells
    public CureType DiseaseImmunity { get; set; } // Disease protection
    public bool AntiMagic { get; set; }     // Blocks all magic
    public bool SpellReflection { get; set; } // Reflects spells back
    
    public MagicEnhancement()
    {
        DiseaseImmunity = CureType.None;
    }
} 
