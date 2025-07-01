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
        
        // Magic properties
        public MagicProperties MagicProperties { get; set; } = new();
        
        // Restrictions
        public bool OnlyForGood { get; set; }        // good alignment only
        public bool OnlyForEvil { get; set; }        // evil alignment only
        public bool IsCursed { get; set; }           // cursed item
        
        public long ArmPow { get; set; }             // armor power
        public long WeapPow { get; set; }            // weapon power  
        public string Phrase { get; set; } = "";    // special phrase for the item
        public bool Cursed { get; set; }             // cursed item?
        
        // Missing properties for compilation
        public int StrengthRequired { get; set; }    // Strength requirement
        
        // Missing methods for compilation
        public Item Clone()
        {
            return new Item
            {
                Nr = this.Nr,
                Name = this.Name,
                Cost = this.Cost,
                Value = this.Value,
                Type = this.Type,
                MagicType = this.MagicType,
                IsShopItem = this.IsShopItem,
                IsIdentified = this.IsIdentified,
                Strength = this.Strength,
                Defence = this.Defence,
                Attack = this.Attack,
                Dexterity = this.Dexterity,
                Wisdom = this.Wisdom,
                OnlyForGood = this.OnlyForGood,
                OnlyForEvil = this.OnlyForEvil,
                IsCursed = this.IsCursed,
                ArmPow = this.ArmPow,
                WeapPow = this.WeapPow,
                Phrase = this.Phrase,
                Cursed = this.Cursed,
                StrengthRequired = this.StrengthRequired
            };
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