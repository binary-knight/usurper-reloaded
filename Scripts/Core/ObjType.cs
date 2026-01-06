namespace UsurperRemake
{
    /// <summary>
    /// Item category types - for general categorization of items
    /// Note: This is distinct from the global ObjType enum in Character.cs
    /// which defines equipment slot types for the Pascal-compatible system
    /// </summary>
    public enum ItemCategory
    {
        None = 0,
        Weapon = 1,      // weapon
        Armor = 2,       // armor
        Potion = 3,      // healing potion
        Magic = 4,       // magic item
        Special = 5,     // special item
        Gold = 6,        // gold/money
        Food = 7,        // food item
        Drink = 8,       // beverage
        Key = 9,         // key item
        Scroll = 10      // scroll/spell item
    }
} 