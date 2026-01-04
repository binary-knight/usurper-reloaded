using System;
using System.Collections.Generic;

namespace UsurperRemake
{
    /// <summary>
    /// Item class - Pascal compatible item system
    /// Based on USERHUNC.PAS item record structure
    /// </summary>
    public class Item
    {
        public int Nr { get; set; }                  // item number
        public string Name { get; set; } = "";       // item name
        public long Cost { get; set; }               // cost to buy
        public long Value { get; set; }              // base value
        public ObjType Type { get; set; }            // item type
        public MagicItemType MagicType { get; set; } // magic item type
        public bool IsShopItem { get; set; }         // available in shops
        public bool IsIdentified { get; set; } = true; // identified status
        
        // Base stats
        public int Strength { get; set; }            // strength bonus
        public int Defence { get; set; }             // defense bonus
        public int Attack { get; set; }              // attack bonus
        public int Dexterity { get; set; }           // dexterity bonus
        public int Wisdom { get; set; }              // wisdom bonus
        public int Armor { get; set; }               // armor value (for armor items)
        public int HP { get; set; }                  // HP bonus
        public int Mana { get; set; }                // Mana bonus
        public int Charisma { get; set; }            // Charisma bonus
        public int Constitution { get; set; }        // Constitution bonus
        public int Intelligence { get; set; }        // Intelligence bonus
        public int Agility { get; set; }             // Agility bonus
        public int MinLevel { get; set; }            // Minimum level required to use
        
        // Magic properties
        public MagicProperties MagicProperties { get; set; } = new();
        
        // Restrictions
        public bool OnlyForGood { get; set; }        // good alignment only
        public bool OnlyForEvil { get; set; }        // evil alignment only
        public bool IsCursed { get; set; }           // cursed item

        public long ArmPow { get; set; }             // armor power
        public long WeapPow { get; set; }            // weapon power
        public string Phrase { get; set; } = "";    // special phrase for the item

        // Backwards compatibility alias for IsCursed
        public bool Cursed
        {
            get => IsCursed;
            set => IsCursed = value;
        }
        
        // Compatibility member – some shop code expects this
        public long StrengthRequired { get; set; } = 0;

        // Source tracking (for loot system)
        public bool Shop { get; set; }               // Item from shop
        public bool Dungeon { get; set; }            // Item from dungeon loot

        // Description for special effects
        public List<string> Description { get; set; } = new();

        // Simple shallow clone required by shop/location code
        public Item Clone()
        {
            var clone = (Item)MemberwiseClone();
            // Deep clone the lists
            clone.Description = new List<string>(Description);
            clone.MagicProperties = new MagicProperties
            {
                Mana = MagicProperties.Mana,
                Wisdom = MagicProperties.Wisdom,
                Dexterity = MagicProperties.Dexterity,
                MagicResistance = MagicProperties.MagicResistance,
                DiseaseImmunity = MagicProperties.DiseaseImmunity
            };
            return clone;
        }
    }
    
    /// <summary>
    /// Magic item types from Pascal
    /// </summary>
    public enum MagicItemType
    {
        None = 0,
        Neck = 10,    // Amulets, necklaces
        Fingers = 5,  // Rings
        Waist = 9     // Belts, girdles
    }
    
    /// <summary>
    /// Magic properties container
    /// </summary>
    public class MagicProperties
    {
        public int Mana { get; set; }
        public int Wisdom { get; set; }
        public int Dexterity { get; set; }
        public int MagicResistance { get; set; }
        public CureType DiseaseImmunity { get; set; } = CureType.None;
    }
    
    /// <summary>
    /// Disease cure types
    /// </summary>
    public enum CureType
    {
        None = 0,
        All = 1,
        Blindness = 2,
        Plague = 3,
        Smallpox = 4,
        Measles = 5,
        Leprosy = 6
    }
} 