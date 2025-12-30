using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central equipment database containing all weapons, armor, shields, and accessories
/// Organized by slot type with full progression from basic to legendary items
/// </summary>
public static class EquipmentDatabase
{
    private static Dictionary<int, Equipment> _allEquipment = new();
    private static bool _initialized = false;

    // ID ranges for different equipment types
    private const int OneHandedWeaponStart = 1000;
    private const int TwoHandedWeaponStart = 2000;
    private const int ShieldStart = 3000;
    private const int HeadArmorStart = 4000;
    private const int BodyArmorStart = 5000;
    private const int ArmsArmorStart = 6000;
    private const int HandsArmorStart = 7000;
    private const int LegsArmorStart = 8000;
    private const int FeetArmorStart = 9000;
    private const int WaistArmorStart = 10000;
    private const int FaceArmorStart = 11000;
    private const int CloakStart = 12000;
    private const int NeckAccessoryStart = 13000;
    private const int RingStart = 14000;

    /// <summary>
    /// Initialize the equipment database - call once at game start
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        _allEquipment.Clear();

        InitializeOneHandedWeapons();
        InitializeTwoHandedWeapons();
        InitializeShields();
        InitializeHeadArmor();
        InitializeBodyArmor();
        InitializeArmsArmor();
        InitializeHandsArmor();
        InitializeLegsArmor();
        InitializeFeetArmor();
        InitializeWaistArmor();
        InitializeFaceArmor();
        InitializeCloaks();
        InitializeNeckAccessories();
        InitializeRings();

        _initialized = true;
    }

    #region Lookup Methods

    /// <summary>
    /// Get equipment by ID
    /// </summary>
    public static Equipment GetById(int id)
    {
        EnsureInitialized();
        return _allEquipment.TryGetValue(id, out var equip) ? equip : null;
    }

    /// <summary>
    /// Get all equipment for a specific slot
    /// </summary>
    public static List<Equipment> GetBySlot(EquipmentSlot slot)
    {
        EnsureInitialized();
        return _allEquipment.Values
            .Where(e => e.Slot == slot)
            .OrderBy(e => e.Value)
            .ToList();
    }

    /// <summary>
    /// Get all weapons of a specific handedness
    /// </summary>
    public static List<Equipment> GetWeaponsByHandedness(WeaponHandedness handedness)
    {
        EnsureInitialized();
        return _allEquipment.Values
            .Where(e => e.Handedness == handedness)
            .OrderBy(e => e.Value)
            .ToList();
    }

    /// <summary>
    /// Get all one-handed weapons
    /// </summary>
    public static List<Equipment> GetOneHandedWeapons() =>
        GetWeaponsByHandedness(WeaponHandedness.OneHanded);

    /// <summary>
    /// Get all two-handed weapons
    /// </summary>
    public static List<Equipment> GetTwoHandedWeapons() =>
        GetWeaponsByHandedness(WeaponHandedness.TwoHanded);

    /// <summary>
    /// Get all shields
    /// </summary>
    public static List<Equipment> GetShields()
    {
        EnsureInitialized();
        return _allEquipment.Values
            .Where(e => e.WeaponType == WeaponType.Shield)
            .OrderBy(e => e.Value)
            .ToList();
    }

    /// <summary>
    /// Get all armor for shop display (all slots)
    /// </summary>
    public static List<Equipment> GetAllArmor()
    {
        EnsureInitialized();
        return _allEquipment.Values
            .Where(e => e.Slot.IsArmorSlot())
            .OrderBy(e => e.Slot)
            .ThenBy(e => e.Value)
            .ToList();
    }

    /// <summary>
    /// Get all accessories (rings, amulets)
    /// </summary>
    public static List<Equipment> GetAccessories()
    {
        EnsureInitialized();
        return _allEquipment.Values
            .Where(e => e.Slot.IsAccessorySlot())
            .OrderBy(e => e.Value)
            .ToList();
    }

    /// <summary>
    /// Find best equipment by power/AC within budget
    /// </summary>
    public static Equipment GetBestAffordable(EquipmentSlot slot, long maxGold)
    {
        EnsureInitialized();
        return _allEquipment.Values
            .Where(e => e.Slot == slot && e.Value <= maxGold)
            .OrderByDescending(e => e.ArmorClass + e.WeaponPower + e.ShieldBonus)
            .FirstOrDefault();
    }

    /// <summary>
    /// Find equipment matching a power value (for save migration)
    /// </summary>
    public static Equipment FindByPower(long power, EquipmentSlot slot)
    {
        EnsureInitialized();
        return _allEquipment.Values
            .Where(e => e.Slot == slot)
            .OrderBy(e => Math.Abs((e.WeaponPower + e.ArmorClass + e.ShieldBonus) - power))
            .FirstOrDefault();
    }

    private static void EnsureInitialized()
    {
        if (!_initialized) Initialize();
    }

    private static void AddEquipment(Equipment equip)
    {
        _allEquipment[equip.Id] = equip;
    }

    #endregion

    #region One-Handed Weapons (30 items)

    private static void InitializeOneHandedWeapons()
    {
        int id = OneHandedWeaponStart;

        // Daggers (Rogue weapons - fast, crit bonus)
        AddEquipment(new Equipment { Id = id++, Name = "Rusty Dagger", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Dagger, WeaponPower = 2, Value = 15, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Iron Dagger", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Dagger, WeaponPower = 4, Value = 50, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Steel Dagger", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Dagger, WeaponPower = 6, Value = 150, Rarity = EquipmentRarity.Uncommon, CriticalChanceBonus = 5 });
        AddEquipment(new Equipment { Id = id++, Name = "Assassin's Blade", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Dagger, WeaponPower = 10, Value = 800, Rarity = EquipmentRarity.Rare, CriticalChanceBonus = 10, CriticalDamageBonus = 25 });
        AddEquipment(new Equipment { Id = id++, Name = "Shadowfang", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Dagger, WeaponPower = 15, Value = 5000, Rarity = EquipmentRarity.Epic, CriticalChanceBonus = 15, CriticalDamageBonus = 50, PoisonDamage = 5 });

        // Swords (Versatile, balanced)
        AddEquipment(new Equipment { Id = id++, Name = "Rusty Sword", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Sword, WeaponPower = 3, Value = 25, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Short Sword", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Sword, WeaponPower = 5, Value = 100, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Long Sword", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Sword, WeaponPower = 8, Value = 400, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Broad Sword", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Sword, WeaponPower = 10, Value = 800, Rarity = EquipmentRarity.Uncommon });
        AddEquipment(new Equipment { Id = id++, Name = "Bastard Sword", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Sword, WeaponPower = 13, Value = 2000, Rarity = EquipmentRarity.Uncommon });
        AddEquipment(new Equipment { Id = id++, Name = "Knight's Blade", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Sword, WeaponPower = 16, Value = 5000, Rarity = EquipmentRarity.Rare, StrengthBonus = 2 });
        AddEquipment(new Equipment { Id = id++, Name = "Paladin's Sword", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Sword, WeaponPower = 20, Value = 15000, Rarity = EquipmentRarity.Epic, StrengthBonus = 3, RequiresGood = true });
        AddEquipment(new Equipment { Id = id++, Name = "Demon Blade", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Sword, WeaponPower = 22, Value = 18000, Rarity = EquipmentRarity.Epic, StrengthBonus = 4, RequiresEvil = true, LifeSteal = 5 });
        AddEquipment(new Equipment { Id = id++, Name = "Excalibur", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Sword, WeaponPower = 30, Value = 100000, Rarity = EquipmentRarity.Legendary, StrengthBonus = 5, CharismaBonus = 5, RequiresGood = true, IsUnique = true });

        // Axes (High damage)
        AddEquipment(new Equipment { Id = id++, Name = "Hand Axe", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Axe, WeaponPower = 6, Value = 75, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "War Axe", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Axe, WeaponPower = 10, Value = 400, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Battle Axe", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Axe, WeaponPower = 14, Value = 1500, Rarity = EquipmentRarity.Uncommon });
        AddEquipment(new Equipment { Id = id++, Name = "Berserker Axe", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Axe, WeaponPower = 18, Value = 6000, Rarity = EquipmentRarity.Rare, StrengthBonus = 3, CriticalDamageBonus = 20 });
        AddEquipment(new Equipment { Id = id++, Name = "Executioner's Axe", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Axe, WeaponPower = 25, Value = 25000, Rarity = EquipmentRarity.Epic, StrengthBonus = 5, CriticalDamageBonus = 50 });

        // Maces (Armor piercing)
        AddEquipment(new Equipment { Id = id++, Name = "Wooden Club", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Mace, WeaponPower = 4, Value = 30, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Iron Mace", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Mace, WeaponPower = 7, Value = 200, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Flanged Mace", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Mace, WeaponPower = 11, Value = 800, Rarity = EquipmentRarity.Uncommon });
        AddEquipment(new Equipment { Id = id++, Name = "Holy Mace", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Mace, WeaponPower = 15, Value = 4000, Rarity = EquipmentRarity.Rare, WisdomBonus = 3, RequiresGood = true });
        AddEquipment(new Equipment { Id = id++, Name = "Skull Crusher", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Mace, WeaponPower = 20, Value = 12000, Rarity = EquipmentRarity.Epic, StrengthBonus = 4 });

        // Rapiers (Dex-based, fast)
        AddEquipment(new Equipment { Id = id++, Name = "Fencing Foil", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Rapier, WeaponPower = 4, Value = 80, Rarity = EquipmentRarity.Common, DexterityBonus = 1 });
        AddEquipment(new Equipment { Id = id++, Name = "Rapier", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Rapier, WeaponPower = 7, Value = 350, Rarity = EquipmentRarity.Common, DexterityBonus = 2 });
        AddEquipment(new Equipment { Id = id++, Name = "Duelist's Rapier", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Rapier, WeaponPower = 11, Value = 1200, Rarity = EquipmentRarity.Uncommon, DexterityBonus = 3, CriticalChanceBonus = 5 });
        AddEquipment(new Equipment { Id = id++, Name = "Estoc", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Rapier, WeaponPower = 15, Value = 5000, Rarity = EquipmentRarity.Rare, DexterityBonus = 4, CriticalChanceBonus = 10 });
        AddEquipment(new Equipment { Id = id++, Name = "Dancer's Blade", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.OneHanded, WeaponType = WeaponType.Rapier, WeaponPower = 18, Value = 20000, Rarity = EquipmentRarity.Epic, DexterityBonus = 6, AgilityBonus = 5, CriticalChanceBonus = 15 });
    }

    #endregion

    #region Two-Handed Weapons (20 items)

    private static void InitializeTwoHandedWeapons()
    {
        int id = TwoHandedWeaponStart;

        // Greatswords
        AddEquipment(new Equipment { Id = id++, Name = "Wooden Greatsword", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Greatsword, WeaponPower = 8, Value = 100, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Iron Greatsword", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Greatsword, WeaponPower = 12, Value = 500, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Steel Greatsword", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Greatsword, WeaponPower = 16, Value = 1500, Rarity = EquipmentRarity.Uncommon });
        AddEquipment(new Equipment { Id = id++, Name = "Claymore", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Greatsword, WeaponPower = 22, Value = 5000, Rarity = EquipmentRarity.Rare, StrengthBonus = 3 });
        AddEquipment(new Equipment { Id = id++, Name = "Zweihander", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Greatsword, WeaponPower = 28, Value = 15000, Rarity = EquipmentRarity.Epic, StrengthBonus = 5 });
        AddEquipment(new Equipment { Id = id++, Name = "Dragonslayer", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Greatsword, WeaponPower = 40, Value = 150000, Rarity = EquipmentRarity.Legendary, StrengthBonus = 8, MaxHPBonus = 50, IsUnique = true });

        // Great Axes
        AddEquipment(new Equipment { Id = id++, Name = "Woodcutter's Axe", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Greataxe, WeaponPower = 10, Value = 150, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Greataxe", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Greataxe, WeaponPower = 18, Value = 2000, Rarity = EquipmentRarity.Uncommon, StrengthBonus = 2 });
        AddEquipment(new Equipment { Id = id++, Name = "Barbarian's Greataxe", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Greataxe, WeaponPower = 26, Value = 8000, Rarity = EquipmentRarity.Rare, StrengthBonus = 4, CriticalDamageBonus = 30 });
        AddEquipment(new Equipment { Id = id++, Name = "Demon Cleaver", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Greataxe, WeaponPower = 35, Value = 50000, Rarity = EquipmentRarity.Epic, StrengthBonus = 6, LifeSteal = 10, RequiresEvil = true });

        // Staves (Magic weapons)
        AddEquipment(new Equipment { Id = id++, Name = "Wooden Staff", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Staff, WeaponPower = 5, Value = 50, Rarity = EquipmentRarity.Common, MaxManaBonus = 10 });
        AddEquipment(new Equipment { Id = id++, Name = "Oak Staff", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Staff, WeaponPower = 8, Value = 300, Rarity = EquipmentRarity.Common, MaxManaBonus = 20, WisdomBonus = 1 });
        AddEquipment(new Equipment { Id = id++, Name = "Mage's Staff", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Staff, WeaponPower = 12, Value = 1000, Rarity = EquipmentRarity.Uncommon, MaxManaBonus = 40, WisdomBonus = 2, IntelligenceBonus = 2 });
        AddEquipment(new Equipment { Id = id++, Name = "Archmage Staff", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Staff, WeaponPower = 18, Value = 6000, Rarity = EquipmentRarity.Rare, MaxManaBonus = 80, WisdomBonus = 4, IntelligenceBonus = 4 });
        AddEquipment(new Equipment { Id = id++, Name = "Staff of Power", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Staff, WeaponPower = 25, Value = 30000, Rarity = EquipmentRarity.Epic, MaxManaBonus = 150, WisdomBonus = 6, IntelligenceBonus = 6, MagicResistance = 10 });
        AddEquipment(new Equipment { Id = id++, Name = "Staff of the Cosmos", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Staff, WeaponPower = 35, Value = 200000, Rarity = EquipmentRarity.Legendary, MaxManaBonus = 300, WisdomBonus = 10, IntelligenceBonus = 10, MagicResistance = 25, IsUnique = true });

        // Polearms
        AddEquipment(new Equipment { Id = id++, Name = "Spear", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Polearm, WeaponPower = 9, Value = 200, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Halberd", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Polearm, WeaponPower = 15, Value = 1200, Rarity = EquipmentRarity.Uncommon, StrengthBonus = 1, DexterityBonus = 1 });
        AddEquipment(new Equipment { Id = id++, Name = "Glaive", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Polearm, WeaponPower = 20, Value = 4000, Rarity = EquipmentRarity.Rare, StrengthBonus = 2, DexterityBonus = 2 });
        AddEquipment(new Equipment { Id = id++, Name = "Dragon Lance", Slot = EquipmentSlot.MainHand, Handedness = WeaponHandedness.TwoHanded, WeaponType = WeaponType.Polearm, WeaponPower = 32, Value = 80000, Rarity = EquipmentRarity.Epic, StrengthBonus = 5, DexterityBonus = 5, CriticalChanceBonus = 10 });
    }

    #endregion

    #region Shields (15 items)

    private static void InitializeShields()
    {
        int id = ShieldStart;

        AddEquipment(new Equipment { Id = id++, Name = "Wooden Buckler", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Buckler, ShieldBonus = 2, BlockChance = 10, Value = 25, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Leather Shield", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 4, BlockChance = 12, Value = 75, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Wooden Shield", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 5, BlockChance = 15, Value = 150, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Iron Shield", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 7, BlockChance = 18, Value = 400, Rarity = EquipmentRarity.Common });
        AddEquipment(new Equipment { Id = id++, Name = "Steel Shield", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 10, BlockChance = 20, Value = 1000, Rarity = EquipmentRarity.Uncommon });
        AddEquipment(new Equipment { Id = id++, Name = "Knight's Shield", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 13, BlockChance = 22, Value = 2500, Rarity = EquipmentRarity.Uncommon, DefenceBonus = 2 });
        AddEquipment(new Equipment { Id = id++, Name = "Heater Shield", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 16, BlockChance = 25, Value = 5000, Rarity = EquipmentRarity.Rare, DefenceBonus = 3 });
        AddEquipment(new Equipment { Id = id++, Name = "Tower Shield", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.TowerShield, ShieldBonus = 22, BlockChance = 30, Value = 12000, Rarity = EquipmentRarity.Rare, DefenceBonus = 5, StrengthRequired = 15 });
        AddEquipment(new Equipment { Id = id++, Name = "Paladin's Shield", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 18, BlockChance = 25, Value = 15000, Rarity = EquipmentRarity.Epic, DefenceBonus = 4, MagicResistance = 10, RequiresGood = true });
        AddEquipment(new Equipment { Id = id++, Name = "Demon Shield", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 20, BlockChance = 25, Value = 18000, Rarity = EquipmentRarity.Epic, DefenceBonus = 5, LifeSteal = 3, RequiresEvil = true });
        AddEquipment(new Equipment { Id = id++, Name = "Dragon Scale Shield", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 25, BlockChance = 28, Value = 50000, Rarity = EquipmentRarity.Epic, DefenceBonus = 6, MagicResistance = 15 });
        AddEquipment(new Equipment { Id = id++, Name = "Aegis", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 30, BlockChance = 35, Value = 120000, Rarity = EquipmentRarity.Legendary, DefenceBonus = 8, MagicResistance = 25, MaxHPBonus = 30, IsUnique = true });
        AddEquipment(new Equipment { Id = id++, Name = "Wall of Faith", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.TowerShield, ShieldBonus = 35, BlockChance = 40, Value = 200000, Rarity = EquipmentRarity.Legendary, DefenceBonus = 10, MagicResistance = 30, RequiresGood = true, IsUnique = true });
        AddEquipment(new Equipment { Id = id++, Name = "Bulwark of Shadows", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 28, BlockChance = 30, Value = 180000, Rarity = EquipmentRarity.Legendary, DefenceBonus = 8, LifeSteal = 8, RequiresEvil = true, IsUnique = true });
        AddEquipment(new Equipment { Id = id++, Name = "Shield of the Ancients", Slot = EquipmentSlot.OffHand, Handedness = WeaponHandedness.OffHandOnly, WeaponType = WeaponType.Shield, ShieldBonus = 40, BlockChance = 45, Value = 500000, Rarity = EquipmentRarity.Artifact, DefenceBonus = 12, MagicResistance = 40, MaxHPBonus = 100, IsUnique = true });
    }

    #endregion

    #region Head Armor (20 items)

    private static void InitializeHeadArmor()
    {
        int id = HeadArmorStart;

        AddEquipment(Equipment.CreateArmor(id++, "Cloth Cap", EquipmentSlot.Head, ArmorType.Cloth, 1, 15));
        AddEquipment(Equipment.CreateArmor(id++, "Leather Hood", EquipmentSlot.Head, ArmorType.Leather, 2, 50));
        AddEquipment(Equipment.CreateArmor(id++, "Leather Cap", EquipmentSlot.Head, ArmorType.Leather, 3, 100));
        AddEquipment(Equipment.CreateArmor(id++, "Studded Leather Cap", EquipmentSlot.Head, ArmorType.Leather, 4, 200));
        AddEquipment(Equipment.CreateArmor(id++, "Chain Coif", EquipmentSlot.Head, ArmorType.Chain, 5, 400, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Iron Helm", EquipmentSlot.Head, ArmorType.Plate, 6, 750, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Steel Helm", EquipmentSlot.Head, ArmorType.Plate, 8, 1500, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Great Helm", EquipmentSlot.Head, ArmorType.Plate, 10, 3000, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Knight's Helm", EquipmentSlot.Head, ArmorType.Plate, 12, 6000, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Barbute", EquipmentSlot.Head, ArmorType.Plate, 14, 12000, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Crusader's Helm", EquipmentSlot.Head, ArmorType.Plate, 16, 25000, EquipmentRarity.Epic).WithWisdom(2).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Demon Visage", EquipmentSlot.Head, ArmorType.Plate, 17, 28000, EquipmentRarity.Epic).WithStrength(3).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Mage's Hood", EquipmentSlot.Head, ArmorType.Cloth, 6, 8000, EquipmentRarity.Rare).WithMaxMana(30).WithIntelligence(3));
        AddEquipment(Equipment.CreateArmor(id++, "Archmage Crown", EquipmentSlot.Head, ArmorType.Magic, 10, 40000, EquipmentRarity.Epic).WithMaxMana(80).WithIntelligence(5).WithWisdom(5));
        AddEquipment(Equipment.CreateArmor(id++, "Dragon Helm", EquipmentSlot.Head, ArmorType.Plate, 20, 60000, EquipmentRarity.Epic).WithStrength(4).WithMagicResist(15));
        AddEquipment(Equipment.CreateArmor(id++, "Crown of Valor", EquipmentSlot.Head, ArmorType.Plate, 22, 100000, EquipmentRarity.Legendary).WithStrength(5).WithCharisma(5).WithMaxHP(30));
        AddEquipment(Equipment.CreateArmor(id++, "Crown of Shadows", EquipmentSlot.Head, ArmorType.Magic, 18, 90000, EquipmentRarity.Legendary).WithDexterity(6).WithCritChance(10).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Divine Halo", EquipmentSlot.Head, ArmorType.Magic, 20, 150000, EquipmentRarity.Legendary).WithWisdom(8).WithMagicResist(25).RequiresGoodAlignment().AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Crown of the Ancients", EquipmentSlot.Head, ArmorType.Artifact, 25, 300000, EquipmentRarity.Artifact).WithStrength(5).WithIntelligence(5).WithWisdom(5).WithMaxHP(50).WithMaxMana(50).AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Helm of the Titans", EquipmentSlot.Head, ArmorType.Artifact, 30, 500000, EquipmentRarity.Artifact).WithStrength(10).WithConstitution(10).WithMaxHP(100).AsUnique());
    }

    #endregion

    #region Body Armor (50 items - keeping existing progression)

    private static void InitializeBodyArmor()
    {
        int id = BodyArmorStart;

        // Basic armors (AC 0-5)
        AddEquipment(Equipment.CreateArmor(id++, "Bare Skin", EquipmentSlot.Body, ArmorType.Cloth, 0, 0));
        AddEquipment(Equipment.CreateArmor(id++, "Cloth Rags", EquipmentSlot.Body, ArmorType.Cloth, 1, 25));
        AddEquipment(Equipment.CreateArmor(id++, "Padded Cloth", EquipmentSlot.Body, ArmorType.Cloth, 2, 50));
        AddEquipment(Equipment.CreateArmor(id++, "Leather Tunic", EquipmentSlot.Body, ArmorType.Leather, 3, 100));
        AddEquipment(Equipment.CreateArmor(id++, "Hardened Leather", EquipmentSlot.Body, ArmorType.Leather, 4, 200));
        AddEquipment(Equipment.CreateArmor(id++, "Studded Leather", EquipmentSlot.Body, ArmorType.Leather, 5, 400));

        // Light mail (AC 6-10)
        AddEquipment(Equipment.CreateArmor(id++, "Ring Mail", EquipmentSlot.Body, ArmorType.Chain, 6, 750));
        AddEquipment(Equipment.CreateArmor(id++, "Scale Mail", EquipmentSlot.Body, ArmorType.Scale, 7, 1200));
        AddEquipment(Equipment.CreateArmor(id++, "Chain Shirt", EquipmentSlot.Body, ArmorType.Chain, 8, 2000, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Chain Mail", EquipmentSlot.Body, ArmorType.Chain, 9, 3200, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Reinforced Chain", EquipmentSlot.Body, ArmorType.Chain, 10, 5000, EquipmentRarity.Uncommon));

        // Medium armor (AC 11-15)
        AddEquipment(Equipment.CreateArmor(id++, "Splint Mail", EquipmentSlot.Body, ArmorType.Scale, 11, 7500, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Banded Mail", EquipmentSlot.Body, ArmorType.Scale, 12, 11000, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Bronze Plate", EquipmentSlot.Body, ArmorType.Plate, 13, 16000, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Steel Plate", EquipmentSlot.Body, ArmorType.Plate, 14, 22000, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Plate Mail", EquipmentSlot.Body, ArmorType.Plate, 15, 30000, EquipmentRarity.Rare));

        // Heavy armor (AC 16-20)
        AddEquipment(Equipment.CreateArmor(id++, "Field Plate", EquipmentSlot.Body, ArmorType.Plate, 16, 40000, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Full Plate", EquipmentSlot.Body, ArmorType.Plate, 17, 52000, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Master Plate", EquipmentSlot.Body, ArmorType.Plate, 18, 66000, EquipmentRarity.Epic));
        AddEquipment(Equipment.CreateArmor(id++, "Knight's Plate", EquipmentSlot.Body, ArmorType.Plate, 19, 82000, EquipmentRarity.Epic));
        AddEquipment(Equipment.CreateArmor(id++, "Royal Plate", EquipmentSlot.Body, ArmorType.Plate, 20, 100000, EquipmentRarity.Epic));

        // Enchanted armor (AC 21-25)
        AddEquipment(Equipment.CreateArmor(id++, "Plate of Valor", EquipmentSlot.Body, ArmorType.Magic, 21, 125000, EquipmentRarity.Epic).WithStrength(2));
        AddEquipment(Equipment.CreateArmor(id++, "Blessed Plate", EquipmentSlot.Body, ArmorType.Magic, 22, 155000, EquipmentRarity.Epic).WithWisdom(2).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Plate of Honor", EquipmentSlot.Body, ArmorType.Magic, 23, 190000, EquipmentRarity.Epic).WithCharisma(3));
        AddEquipment(Equipment.CreateArmor(id++, "Sacred Plate", EquipmentSlot.Body, ArmorType.Magic, 24, 230000, EquipmentRarity.Epic).WithMaxHP(30).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Holy Plate", EquipmentSlot.Body, ArmorType.Magic, 25, 275000, EquipmentRarity.Epic).WithMagicResist(15).RequiresGoodAlignment());

        // Dark armors (AC 26-30)
        AddEquipment(Equipment.CreateArmor(id++, "Shadow Plate", EquipmentSlot.Body, ArmorType.Magic, 26, 330000, EquipmentRarity.Epic).WithDexterity(3).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Demon Plate", EquipmentSlot.Body, ArmorType.Magic, 27, 390000, EquipmentRarity.Epic).WithStrength(4).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Cursed Plate", EquipmentSlot.Body, ArmorType.Magic, 28, 460000, EquipmentRarity.Epic).WithLifeSteal(5).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Infernal Plate", EquipmentSlot.Body, ArmorType.Magic, 29, 540000, EquipmentRarity.Legendary).WithStrength(5).WithLifeSteal(8).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Abyssal Armor", EquipmentSlot.Body, ArmorType.Magic, 30, 630000, EquipmentRarity.Legendary).WithStrength(6).WithMaxHP(50).RequiresEvilAlignment());

        // Dragon armors (AC 31-35)
        AddEquipment(Equipment.CreateArmor(id++, "Dragon Scale", EquipmentSlot.Body, ArmorType.Magic, 31, 730000, EquipmentRarity.Legendary).WithMagicResist(20));
        AddEquipment(Equipment.CreateArmor(id++, "Wyrm Scale", EquipmentSlot.Body, ArmorType.Magic, 32, 840000, EquipmentRarity.Legendary).WithMagicResist(22).WithMaxHP(40));
        AddEquipment(Equipment.CreateArmor(id++, "Ancient Dragon Hide", EquipmentSlot.Body, ArmorType.Magic, 33, 960000, EquipmentRarity.Legendary).WithMagicResist(25).WithStrength(5));
        AddEquipment(Equipment.CreateArmor(id++, "Great Wyrm Armor", EquipmentSlot.Body, ArmorType.Magic, 34, 1100000, EquipmentRarity.Legendary).WithMagicResist(28).WithMaxHP(60));
        AddEquipment(Equipment.CreateArmor(id++, "Elder Dragon Plate", EquipmentSlot.Body, ArmorType.Magic, 35, 1250000, EquipmentRarity.Legendary).WithMagicResist(30).WithStrength(6).WithMaxHP(80));

        // Celestial armors (AC 36-40)
        AddEquipment(Equipment.CreateArmor(id++, "Celestial Armor", EquipmentSlot.Body, ArmorType.Artifact, 36, 1420000, EquipmentRarity.Legendary).WithWisdom(5).WithMaxMana(50).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Heavenly Plate", EquipmentSlot.Body, ArmorType.Artifact, 37, 1600000, EquipmentRarity.Legendary).WithWisdom(6).WithMagicResist(30).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Angelic Armor", EquipmentSlot.Body, ArmorType.Artifact, 38, 1800000, EquipmentRarity.Legendary).WithWisdom(7).WithMaxHP(80).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Seraphim Plate", EquipmentSlot.Body, ArmorType.Artifact, 39, 2050000, EquipmentRarity.Artifact).WithWisdom(8).WithMaxMana(100).WithMagicResist(35).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Divine Protection", EquipmentSlot.Body, ArmorType.Artifact, 40, 2300000, EquipmentRarity.Artifact).WithWisdom(10).WithMaxHP(100).WithMagicResist(40).RequiresGoodAlignment().AsUnique());

        // Legendary armors (AC 41-45)
        AddEquipment(Equipment.CreateArmor(id++, "Titan Armor", EquipmentSlot.Body, ArmorType.Artifact, 41, 2600000, EquipmentRarity.Artifact).WithStrength(8).WithConstitution(8).WithMaxHP(100));
        AddEquipment(Equipment.CreateArmor(id++, "Godforged Plate", EquipmentSlot.Body, ArmorType.Artifact, 42, 2950000, EquipmentRarity.Artifact).WithStrength(10).WithMaxHP(120));
        AddEquipment(Equipment.CreateArmor(id++, "Eternal Guardian", EquipmentSlot.Body, ArmorType.Artifact, 43, 3350000, EquipmentRarity.Artifact).WithConstitution(10).WithMaxHP(150).WithMagicResist(40));
        AddEquipment(Equipment.CreateArmor(id++, "Mythril Armor", EquipmentSlot.Body, ArmorType.Artifact, 44, 3800000, EquipmentRarity.Artifact).WithDexterity(8).WithAgility(10).WithMaxHP(80));
        AddEquipment(Equipment.CreateArmor(id++, "Adamantine Plate", EquipmentSlot.Body, ArmorType.Artifact, 45, 4300000, EquipmentRarity.Artifact).WithStrength(12).WithMaxHP(150).WithDefence(10));

        // Ultimate armors (AC 46-50)
        AddEquipment(Equipment.CreateArmor(id++, "Supreme Protection", EquipmentSlot.Body, ArmorType.Artifact, 46, 4900000, EquipmentRarity.Artifact).WithStrength(10).WithConstitution(10).WithMaxHP(200));
        AddEquipment(Equipment.CreateArmor(id++, "Armor of the Ancients", EquipmentSlot.Body, ArmorType.Artifact, 47, 5600000, EquipmentRarity.Artifact).WithStrength(12).WithWisdom(12).WithMaxHP(200).WithMaxMana(100).AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Immortal's Shell", EquipmentSlot.Body, ArmorType.Artifact, 48, 6400000, EquipmentRarity.Artifact).WithConstitution(15).WithMaxHP(300).WithMagicResist(50).AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Armor of Eternity", EquipmentSlot.Body, ArmorType.Artifact, 49, 7300000, EquipmentRarity.Artifact).WithStrength(15).WithConstitution(15).WithMaxHP(350).AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Ultimate Defense", EquipmentSlot.Body, ArmorType.Artifact, 50, 8500000, EquipmentRarity.Artifact).WithStrength(20).WithConstitution(20).WithMaxHP(500).WithMagicResist(50).WithDefence(20).AsUnique());
    }

    #endregion

    #region Arms Armor (20 items)

    private static void InitializeArmsArmor()
    {
        int id = ArmsArmorStart;

        AddEquipment(Equipment.CreateArmor(id++, "Cloth Wraps", EquipmentSlot.Arms, ArmorType.Cloth, 1, 10));
        AddEquipment(Equipment.CreateArmor(id++, "Leather Bracers", EquipmentSlot.Arms, ArmorType.Leather, 2, 40));
        AddEquipment(Equipment.CreateArmor(id++, "Studded Bracers", EquipmentSlot.Arms, ArmorType.Leather, 3, 100));
        AddEquipment(Equipment.CreateArmor(id++, "Chain Sleeves", EquipmentSlot.Arms, ArmorType.Chain, 4, 250));
        AddEquipment(Equipment.CreateArmor(id++, "Iron Vambraces", EquipmentSlot.Arms, ArmorType.Plate, 5, 500, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Steel Vambraces", EquipmentSlot.Arms, ArmorType.Plate, 6, 1000, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Knight's Vambraces", EquipmentSlot.Arms, ArmorType.Plate, 8, 2500, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Blessed Bracers", EquipmentSlot.Arms, ArmorType.Magic, 9, 5000, EquipmentRarity.Rare).WithStrength(2).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Dark Vambraces", EquipmentSlot.Arms, ArmorType.Magic, 9, 5000, EquipmentRarity.Rare).WithStrength(3).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Mage's Sleeves", EquipmentSlot.Arms, ArmorType.Cloth, 4, 3000, EquipmentRarity.Rare).WithMaxMana(20).WithIntelligence(2));
        AddEquipment(Equipment.CreateArmor(id++, "Berserker Bracers", EquipmentSlot.Arms, ArmorType.Plate, 10, 8000, EquipmentRarity.Epic).WithStrength(4).WithCritDamage(15));
        AddEquipment(Equipment.CreateArmor(id++, "Dragon Scale Arms", EquipmentSlot.Arms, ArmorType.Magic, 12, 15000, EquipmentRarity.Epic).WithMagicResist(10));
        AddEquipment(Equipment.CreateArmor(id++, "Celestial Bracers", EquipmentSlot.Arms, ArmorType.Magic, 14, 30000, EquipmentRarity.Epic).WithWisdom(4).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Demon Arms", EquipmentSlot.Arms, ArmorType.Magic, 14, 32000, EquipmentRarity.Epic).WithStrength(5).WithLifeSteal(3).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Vambraces of Power", EquipmentSlot.Arms, ArmorType.Magic, 16, 50000, EquipmentRarity.Legendary).WithStrength(6).WithMaxHP(20));
        AddEquipment(Equipment.CreateArmor(id++, "Archmage Sleeves", EquipmentSlot.Arms, ArmorType.Magic, 12, 45000, EquipmentRarity.Legendary).WithMaxMana(60).WithIntelligence(5).WithWisdom(3));
        AddEquipment(Equipment.CreateArmor(id++, "Arms of the Titan", EquipmentSlot.Arms, ArmorType.Artifact, 20, 100000, EquipmentRarity.Legendary).WithStrength(8).WithMaxHP(40));
        AddEquipment(Equipment.CreateArmor(id++, "Arms of Eternity", EquipmentSlot.Arms, ArmorType.Artifact, 22, 150000, EquipmentRarity.Artifact).WithStrength(10).WithConstitution(5).WithMaxHP(60));
        AddEquipment(Equipment.CreateArmor(id++, "Godforged Vambraces", EquipmentSlot.Arms, ArmorType.Artifact, 25, 250000, EquipmentRarity.Artifact).WithStrength(12).WithMaxHP(80).AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Titan's Arms", EquipmentSlot.Arms, ArmorType.Artifact, 28, 400000, EquipmentRarity.Artifact).WithStrength(15).WithConstitution(10).WithMaxHP(100).AsUnique());
    }

    #endregion

    #region Hands Armor (20 items)

    private static void InitializeHandsArmor()
    {
        int id = HandsArmorStart;

        AddEquipment(Equipment.CreateArmor(id++, "Cloth Gloves", EquipmentSlot.Hands, ArmorType.Cloth, 1, 10));
        AddEquipment(Equipment.CreateArmor(id++, "Leather Gloves", EquipmentSlot.Hands, ArmorType.Leather, 2, 35));
        AddEquipment(Equipment.CreateArmor(id++, "Studded Gloves", EquipmentSlot.Hands, ArmorType.Leather, 3, 80));
        AddEquipment(Equipment.CreateArmor(id++, "Chain Gauntlets", EquipmentSlot.Hands, ArmorType.Chain, 4, 200));
        AddEquipment(Equipment.CreateArmor(id++, "Iron Gauntlets", EquipmentSlot.Hands, ArmorType.Plate, 5, 400, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Steel Gauntlets", EquipmentSlot.Hands, ArmorType.Plate, 6, 800, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Knight's Gauntlets", EquipmentSlot.Hands, ArmorType.Plate, 8, 2000, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Thief's Gloves", EquipmentSlot.Hands, ArmorType.Leather, 4, 1500, EquipmentRarity.Rare).WithDexterity(3).WithCritChance(5));
        AddEquipment(Equipment.CreateArmor(id++, "Mage's Gloves", EquipmentSlot.Hands, ArmorType.Cloth, 3, 2500, EquipmentRarity.Rare).WithMaxMana(15).WithIntelligence(2));
        AddEquipment(Equipment.CreateArmor(id++, "Blessed Gauntlets", EquipmentSlot.Hands, ArmorType.Magic, 9, 4000, EquipmentRarity.Rare).WithStrength(2).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Spiked Gauntlets", EquipmentSlot.Hands, ArmorType.Plate, 8, 5000, EquipmentRarity.Epic).WithCritDamage(10));
        AddEquipment(Equipment.CreateArmor(id++, "Dragon Gauntlets", EquipmentSlot.Hands, ArmorType.Magic, 12, 12000, EquipmentRarity.Epic).WithMagicResist(8).WithStrength(3));
        AddEquipment(Equipment.CreateArmor(id++, "Shadow Gloves", EquipmentSlot.Hands, ArmorType.Magic, 10, 15000, EquipmentRarity.Epic).WithDexterity(5).WithCritChance(10).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Celestial Gauntlets", EquipmentSlot.Hands, ArmorType.Magic, 14, 25000, EquipmentRarity.Legendary).WithWisdom(4).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Demon Gauntlets", EquipmentSlot.Hands, ArmorType.Magic, 14, 28000, EquipmentRarity.Legendary).WithStrength(5).WithLifeSteal(2).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Archmage Gloves", EquipmentSlot.Hands, ArmorType.Magic, 10, 40000, EquipmentRarity.Legendary).WithMaxMana(50).WithIntelligence(5).WithWisdom(3));
        AddEquipment(Equipment.CreateArmor(id++, "Gauntlets of Power", EquipmentSlot.Hands, ArmorType.Magic, 18, 70000, EquipmentRarity.Legendary).WithStrength(6));
        AddEquipment(Equipment.CreateArmor(id++, "Titan's Grasp", EquipmentSlot.Hands, ArmorType.Artifact, 22, 150000, EquipmentRarity.Artifact).WithStrength(10).WithMaxHP(30));
        AddEquipment(Equipment.CreateArmor(id++, "Godforged Gauntlets", EquipmentSlot.Hands, ArmorType.Artifact, 25, 250000, EquipmentRarity.Artifact).WithStrength(12).AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Hands of Eternity", EquipmentSlot.Hands, ArmorType.Artifact, 28, 400000, EquipmentRarity.Artifact).WithStrength(15).WithDexterity(10).AsUnique());
    }

    #endregion

    #region Legs Armor (20 items)

    private static void InitializeLegsArmor()
    {
        int id = LegsArmorStart;

        AddEquipment(Equipment.CreateArmor(id++, "Cloth Pants", EquipmentSlot.Legs, ArmorType.Cloth, 1, 12));
        AddEquipment(Equipment.CreateArmor(id++, "Leather Leggings", EquipmentSlot.Legs, ArmorType.Leather, 2, 45));
        AddEquipment(Equipment.CreateArmor(id++, "Studded Leggings", EquipmentSlot.Legs, ArmorType.Leather, 3, 100));
        AddEquipment(Equipment.CreateArmor(id++, "Chain Leggings", EquipmentSlot.Legs, ArmorType.Chain, 4, 250));
        AddEquipment(Equipment.CreateArmor(id++, "Iron Greaves", EquipmentSlot.Legs, ArmorType.Plate, 5, 500, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Steel Greaves", EquipmentSlot.Legs, ArmorType.Plate, 6, 1000, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Knight's Greaves", EquipmentSlot.Legs, ArmorType.Plate, 8, 2500, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Blessed Greaves", EquipmentSlot.Legs, ArmorType.Magic, 9, 5000, EquipmentRarity.Rare).WithAgility(2).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Mage's Pants", EquipmentSlot.Legs, ArmorType.Cloth, 4, 3000, EquipmentRarity.Rare).WithMaxMana(20).WithIntelligence(2));
        AddEquipment(Equipment.CreateArmor(id++, "Berserker Greaves", EquipmentSlot.Legs, ArmorType.Plate, 10, 8000, EquipmentRarity.Epic).WithStrength(3).WithAgility(3));
        AddEquipment(Equipment.CreateArmor(id++, "Dragon Greaves", EquipmentSlot.Legs, ArmorType.Magic, 12, 15000, EquipmentRarity.Epic).WithMagicResist(10));
        AddEquipment(Equipment.CreateArmor(id++, "Shadow Leggings", EquipmentSlot.Legs, ArmorType.Magic, 10, 14000, EquipmentRarity.Epic).WithDexterity(4).WithAgility(4).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Celestial Greaves", EquipmentSlot.Legs, ArmorType.Magic, 14, 30000, EquipmentRarity.Legendary).WithWisdom(4).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Demon Greaves", EquipmentSlot.Legs, ArmorType.Magic, 14, 32000, EquipmentRarity.Legendary).WithStrength(5).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Archmage Leggings", EquipmentSlot.Legs, ArmorType.Magic, 12, 45000, EquipmentRarity.Legendary).WithMaxMana(60).WithIntelligence(5));
        AddEquipment(Equipment.CreateArmor(id++, "Greaves of Swiftness", EquipmentSlot.Legs, ArmorType.Magic, 14, 50000, EquipmentRarity.Legendary).WithDexterity(6).WithAgility(8));
        AddEquipment(Equipment.CreateArmor(id++, "Titan's Stride", EquipmentSlot.Legs, ArmorType.Artifact, 20, 100000, EquipmentRarity.Legendary).WithStrength(7).WithMaxHP(40));
        AddEquipment(Equipment.CreateArmor(id++, "Eternal Greaves", EquipmentSlot.Legs, ArmorType.Artifact, 22, 150000, EquipmentRarity.Artifact).WithStrength(10).WithAgility(8).WithMaxHP(60));
        AddEquipment(Equipment.CreateArmor(id++, "Godforged Greaves", EquipmentSlot.Legs, ArmorType.Artifact, 25, 250000, EquipmentRarity.Artifact).WithStrength(12).WithMaxHP(80).AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Legs of the Colossus", EquipmentSlot.Legs, ArmorType.Artifact, 28, 400000, EquipmentRarity.Artifact).WithStrength(15).WithConstitution(10).WithMaxHP(100).AsUnique());
    }

    #endregion

    #region Feet Armor (20 items)

    private static void InitializeFeetArmor()
    {
        int id = FeetArmorStart;

        AddEquipment(Equipment.CreateArmor(id++, "Sandals", EquipmentSlot.Feet, ArmorType.Cloth, 0, 5));
        AddEquipment(Equipment.CreateArmor(id++, "Cloth Shoes", EquipmentSlot.Feet, ArmorType.Cloth, 1, 15));
        AddEquipment(Equipment.CreateArmor(id++, "Leather Boots", EquipmentSlot.Feet, ArmorType.Leather, 2, 50));
        AddEquipment(Equipment.CreateArmor(id++, "Studded Boots", EquipmentSlot.Feet, ArmorType.Leather, 3, 120));
        AddEquipment(Equipment.CreateArmor(id++, "Chain Boots", EquipmentSlot.Feet, ArmorType.Chain, 4, 300));
        AddEquipment(Equipment.CreateArmor(id++, "Iron Boots", EquipmentSlot.Feet, ArmorType.Plate, 5, 600, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Steel Boots", EquipmentSlot.Feet, ArmorType.Plate, 6, 1200, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Knight's Boots", EquipmentSlot.Feet, ArmorType.Plate, 8, 3000, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Boots of Speed", EquipmentSlot.Feet, ArmorType.Leather, 4, 4000, EquipmentRarity.Rare).WithAgility(5).WithDexterity(2));
        AddEquipment(Equipment.CreateArmor(id++, "Mage's Slippers", EquipmentSlot.Feet, ArmorType.Cloth, 3, 3500, EquipmentRarity.Rare).WithMaxMana(20).WithIntelligence(2));
        AddEquipment(Equipment.CreateArmor(id++, "Dragon Boots", EquipmentSlot.Feet, ArmorType.Magic, 12, 15000, EquipmentRarity.Epic).WithMagicResist(10));
        AddEquipment(Equipment.CreateArmor(id++, "Shadow Steps", EquipmentSlot.Feet, ArmorType.Magic, 10, 18000, EquipmentRarity.Epic).WithDexterity(5).WithAgility(6).WithCritChance(5).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Celestial Boots", EquipmentSlot.Feet, ArmorType.Magic, 14, 35000, EquipmentRarity.Legendary).WithWisdom(4).WithAgility(4).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Demon Treads", EquipmentSlot.Feet, ArmorType.Magic, 14, 38000, EquipmentRarity.Legendary).WithStrength(5).WithAgility(4).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Boots of the Wind", EquipmentSlot.Feet, ArmorType.Magic, 12, 50000, EquipmentRarity.Legendary).WithAgility(10).WithDexterity(5));
        AddEquipment(Equipment.CreateArmor(id++, "Archmage Slippers", EquipmentSlot.Feet, ArmorType.Magic, 10, 55000, EquipmentRarity.Legendary).WithMaxMana(60).WithIntelligence(5));
        AddEquipment(Equipment.CreateArmor(id++, "Titan's Boots", EquipmentSlot.Feet, ArmorType.Artifact, 20, 120000, EquipmentRarity.Legendary).WithStrength(8).WithMaxHP(40));
        AddEquipment(Equipment.CreateArmor(id++, "Eternal Boots", EquipmentSlot.Feet, ArmorType.Artifact, 22, 180000, EquipmentRarity.Artifact).WithStrength(10).WithAgility(8).WithMaxHP(50));
        AddEquipment(Equipment.CreateArmor(id++, "Godforged Sabatons", EquipmentSlot.Feet, ArmorType.Artifact, 25, 280000, EquipmentRarity.Artifact).WithStrength(12).WithAgility(10).AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Boots of the Gods", EquipmentSlot.Feet, ArmorType.Artifact, 28, 450000, EquipmentRarity.Artifact).WithStrength(15).WithDexterity(10).WithAgility(15).AsUnique());
    }

    #endregion

    #region Waist Armor (15 items)

    private static void InitializeWaistArmor()
    {
        int id = WaistArmorStart;

        AddEquipment(Equipment.CreateArmor(id++, "Rope Belt", EquipmentSlot.Waist, ArmorType.Cloth, 0, 5));
        AddEquipment(Equipment.CreateArmor(id++, "Leather Belt", EquipmentSlot.Waist, ArmorType.Leather, 1, 30));
        AddEquipment(Equipment.CreateArmor(id++, "Studded Belt", EquipmentSlot.Waist, ArmorType.Leather, 2, 80));
        AddEquipment(Equipment.CreateArmor(id++, "Chain Belt", EquipmentSlot.Waist, ArmorType.Chain, 3, 200));
        AddEquipment(Equipment.CreateArmor(id++, "Warrior's Belt", EquipmentSlot.Waist, ArmorType.Leather, 4, 500, EquipmentRarity.Uncommon).WithStrength(2));
        AddEquipment(Equipment.CreateArmor(id++, "Belt of Dexterity", EquipmentSlot.Waist, ArmorType.Leather, 3, 800, EquipmentRarity.Rare).WithDexterity(4));
        AddEquipment(Equipment.CreateArmor(id++, "Belt of Strength", EquipmentSlot.Waist, ArmorType.Leather, 4, 1200, EquipmentRarity.Rare).WithStrength(4));
        AddEquipment(Equipment.CreateArmor(id++, "Mage's Sash", EquipmentSlot.Waist, ArmorType.Cloth, 2, 1500, EquipmentRarity.Rare).WithMaxMana(30).WithIntelligence(2));
        AddEquipment(Equipment.CreateArmor(id++, "Girdle of Giants", EquipmentSlot.Waist, ArmorType.Magic, 5, 5000, EquipmentRarity.Epic).WithStrength(6).WithConstitution(3));
        AddEquipment(Equipment.CreateArmor(id++, "Belt of the Archmage", EquipmentSlot.Waist, ArmorType.Magic, 4, 8000, EquipmentRarity.Epic).WithMaxMana(60).WithIntelligence(4).WithWisdom(4));
        AddEquipment(Equipment.CreateArmor(id++, "Celestial Sash", EquipmentSlot.Waist, ArmorType.Magic, 6, 15000, EquipmentRarity.Legendary).WithWisdom(5).WithMaxHP(30).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Belt of Shadows", EquipmentSlot.Waist, ArmorType.Magic, 6, 18000, EquipmentRarity.Legendary).WithDexterity(6).WithCritChance(8).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Titan's Girdle", EquipmentSlot.Waist, ArmorType.Artifact, 8, 50000, EquipmentRarity.Legendary).WithStrength(10).WithMaxHP(50));
        AddEquipment(Equipment.CreateArmor(id++, "Girdle of Power", EquipmentSlot.Waist, ArmorType.Artifact, 10, 100000, EquipmentRarity.Artifact).WithStrength(12).WithConstitution(8).WithMaxHP(80).AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Divine Sash", EquipmentSlot.Waist, ArmorType.Artifact, 12, 200000, EquipmentRarity.Artifact).WithStrength(15).WithWisdom(10).WithMaxHP(100).WithMaxMana(50).AsUnique());
    }

    #endregion

    #region Face Armor (15 items)

    private static void InitializeFaceArmor()
    {
        int id = FaceArmorStart;

        AddEquipment(Equipment.CreateArmor(id++, "Cloth Mask", EquipmentSlot.Face, ArmorType.Cloth, 1, 20));
        AddEquipment(Equipment.CreateArmor(id++, "Leather Mask", EquipmentSlot.Face, ArmorType.Leather, 2, 60));
        AddEquipment(Equipment.CreateArmor(id++, "Iron Visor", EquipmentSlot.Face, ArmorType.Plate, 3, 150));
        AddEquipment(Equipment.CreateArmor(id++, "Steel Visor", EquipmentSlot.Face, ArmorType.Plate, 4, 350, EquipmentRarity.Uncommon));
        AddEquipment(Equipment.CreateArmor(id++, "Battle Mask", EquipmentSlot.Face, ArmorType.Plate, 5, 700, EquipmentRarity.Uncommon).WithStrength(1));
        AddEquipment(Equipment.CreateArmor(id++, "Thief's Mask", EquipmentSlot.Face, ArmorType.Leather, 3, 1000, EquipmentRarity.Rare).WithDexterity(3).WithCritChance(3));
        AddEquipment(Equipment.CreateArmor(id++, "Knight's Visor", EquipmentSlot.Face, ArmorType.Plate, 7, 2500, EquipmentRarity.Rare));
        AddEquipment(Equipment.CreateArmor(id++, "Mage's Cowl", EquipmentSlot.Face, ArmorType.Cloth, 4, 3000, EquipmentRarity.Rare).WithMaxMana(25).WithIntelligence(3));
        AddEquipment(Equipment.CreateArmor(id++, "Dragon Mask", EquipmentSlot.Face, ArmorType.Magic, 10, 12000, EquipmentRarity.Epic).WithMagicResist(12).WithStrength(3));
        AddEquipment(Equipment.CreateArmor(id++, "Shadow Mask", EquipmentSlot.Face, ArmorType.Magic, 8, 15000, EquipmentRarity.Epic).WithDexterity(5).WithCritChance(8).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Celestial Visage", EquipmentSlot.Face, ArmorType.Magic, 12, 30000, EquipmentRarity.Legendary).WithWisdom(5).WithCharisma(5).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Demon's Visage", EquipmentSlot.Face, ArmorType.Magic, 12, 35000, EquipmentRarity.Legendary).WithStrength(6).WithLifeSteal(3).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Mask of Terror", EquipmentSlot.Face, ArmorType.Magic, 14, 60000, EquipmentRarity.Legendary).WithStrength(5).WithCharisma(-3));
        AddEquipment(Equipment.CreateArmor(id++, "Mask of the Ancients", EquipmentSlot.Face, ArmorType.Artifact, 18, 120000, EquipmentRarity.Artifact).WithIntelligence(8).WithWisdom(8).WithMaxMana(50).AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Visage of the Gods", EquipmentSlot.Face, ArmorType.Artifact, 20, 250000, EquipmentRarity.Artifact).WithStrength(10).WithWisdom(10).WithCharisma(10).AsUnique());
    }

    #endregion

    #region Cloaks (15 items)

    private static void InitializeCloaks()
    {
        int id = CloakStart;

        AddEquipment(Equipment.CreateArmor(id++, "Torn Cloak", EquipmentSlot.Cloak, ArmorType.Cloth, 1, 15));
        AddEquipment(Equipment.CreateArmor(id++, "Wool Cloak", EquipmentSlot.Cloak, ArmorType.Cloth, 2, 40));
        AddEquipment(Equipment.CreateArmor(id++, "Leather Cloak", EquipmentSlot.Cloak, ArmorType.Leather, 3, 100));
        AddEquipment(Equipment.CreateArmor(id++, "Silk Cloak", EquipmentSlot.Cloak, ArmorType.Cloth, 4, 300, EquipmentRarity.Uncommon).WithCharisma(2));
        AddEquipment(Equipment.CreateArmor(id++, "Traveler's Cloak", EquipmentSlot.Cloak, ArmorType.Leather, 5, 600, EquipmentRarity.Uncommon).WithStamina(10));
        AddEquipment(Equipment.CreateArmor(id++, "Mage's Robe", EquipmentSlot.Cloak, ArmorType.Cloth, 4, 1500, EquipmentRarity.Rare).WithMaxMana(30).WithIntelligence(3));
        AddEquipment(Equipment.CreateArmor(id++, "Cloak of Protection", EquipmentSlot.Cloak, ArmorType.Magic, 6, 3000, EquipmentRarity.Rare).WithDefence(3).WithMagicResist(5));
        AddEquipment(Equipment.CreateArmor(id++, "Cloak of Shadows", EquipmentSlot.Cloak, ArmorType.Magic, 5, 5000, EquipmentRarity.Epic).WithDexterity(4).WithCritChance(5));
        AddEquipment(Equipment.CreateArmor(id++, "Dragon Scale Cloak", EquipmentSlot.Cloak, ArmorType.Magic, 10, 15000, EquipmentRarity.Epic).WithMagicResist(15));
        AddEquipment(Equipment.CreateArmor(id++, "Celestial Mantle", EquipmentSlot.Cloak, ArmorType.Magic, 12, 35000, EquipmentRarity.Legendary).WithWisdom(5).WithMagicResist(15).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Cloak of the Demon", EquipmentSlot.Cloak, ArmorType.Magic, 12, 40000, EquipmentRarity.Legendary).WithStrength(5).WithLifeSteal(5).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateArmor(id++, "Archmage Robes", EquipmentSlot.Cloak, ArmorType.Magic, 10, 50000, EquipmentRarity.Legendary).WithMaxMana(100).WithIntelligence(8).WithWisdom(5));
        AddEquipment(Equipment.CreateArmor(id++, "Cloak of Invisibility", EquipmentSlot.Cloak, ArmorType.Magic, 8, 80000, EquipmentRarity.Legendary).WithDexterity(8).WithAgility(8).WithCritChance(15).AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Divine Mantle", EquipmentSlot.Cloak, ArmorType.Artifact, 18, 150000, EquipmentRarity.Artifact).WithWisdom(10).WithMagicResist(30).WithMaxHP(50).RequiresGoodAlignment().AsUnique());
        AddEquipment(Equipment.CreateArmor(id++, "Cloak of the Cosmos", EquipmentSlot.Cloak, ArmorType.Artifact, 20, 300000, EquipmentRarity.Artifact).WithIntelligence(12).WithWisdom(12).WithMaxMana(150).WithMagicResist(35).AsUnique());
    }

    #endregion

    #region Neck Accessories (20 items)

    private static void InitializeNeckAccessories()
    {
        int id = NeckAccessoryStart;

        AddEquipment(Equipment.CreateAccessory(id++, "Wooden Pendant", EquipmentSlot.Neck, 30).WithMaxHP(5));
        AddEquipment(Equipment.CreateAccessory(id++, "Copper Necklace", EquipmentSlot.Neck, 80).WithCharisma(1));
        AddEquipment(Equipment.CreateAccessory(id++, "Silver Amulet", EquipmentSlot.Neck, 200, EquipmentRarity.Uncommon).WithWisdom(1).WithMaxMana(10));
        AddEquipment(Equipment.CreateAccessory(id++, "Gold Amulet", EquipmentSlot.Neck, 500, EquipmentRarity.Uncommon).WithCharisma(2).WithMaxHP(10));
        AddEquipment(Equipment.CreateAccessory(id++, "Amulet of Wisdom", EquipmentSlot.Neck, 1500, EquipmentRarity.Rare).WithWisdom(4).WithMaxMana(30));
        AddEquipment(Equipment.CreateAccessory(id++, "Amulet of Strength", EquipmentSlot.Neck, 1500, EquipmentRarity.Rare).WithStrength(4));
        AddEquipment(Equipment.CreateAccessory(id++, "Pendant of Protection", EquipmentSlot.Neck, 2000, EquipmentRarity.Rare).WithDefence(3).WithMagicResist(5));
        AddEquipment(Equipment.CreateAccessory(id++, "Holy Symbol", EquipmentSlot.Neck, 3000, EquipmentRarity.Rare).WithWisdom(5).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateAccessory(id++, "Dark Medallion", EquipmentSlot.Neck, 3500, EquipmentRarity.Rare).WithStrength(5).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateAccessory(id++, "Crystal Necklace", EquipmentSlot.Neck, 5000, EquipmentRarity.Epic).WithMaxMana(50).WithIntelligence(3));
        AddEquipment(Equipment.CreateAccessory(id++, "Amulet of the Dragon", EquipmentSlot.Neck, 12000, EquipmentRarity.Epic).WithMagicResist(15).WithMaxHP(30));
        AddEquipment(Equipment.CreateAccessory(id++, "Necklace of Vitality", EquipmentSlot.Neck, 15000, EquipmentRarity.Epic).WithConstitution(5).WithMaxHP(50));
        AddEquipment(Equipment.CreateAccessory(id++, "Amulet of the Archmage", EquipmentSlot.Neck, 25000, EquipmentRarity.Epic).WithMaxMana(100).WithIntelligence(6).WithWisdom(4));
        AddEquipment(Equipment.CreateAccessory(id++, "Celestial Pendant", EquipmentSlot.Neck, 40000, EquipmentRarity.Legendary).WithWisdom(8).WithMagicResist(20).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateAccessory(id++, "Amulet of Shadows", EquipmentSlot.Neck, 45000, EquipmentRarity.Legendary).WithDexterity(8).WithCritChance(10).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateAccessory(id++, "Titan's Pendant", EquipmentSlot.Neck, 60000, EquipmentRarity.Legendary).WithStrength(10).WithMaxHP(60));
        AddEquipment(Equipment.CreateAccessory(id++, "Amulet of Power", EquipmentSlot.Neck, 100000, EquipmentRarity.Legendary).WithStrength(8).WithIntelligence(8).WithWisdom(8).AsUnique());
        AddEquipment(Equipment.CreateAccessory(id++, "Divine Amulet", EquipmentSlot.Neck, 150000, EquipmentRarity.Artifact).WithWisdom(12).WithMagicResist(30).WithMaxHP(80).RequiresGoodAlignment().AsUnique());
        AddEquipment(Equipment.CreateAccessory(id++, "Amulet of the Void", EquipmentSlot.Neck, 180000, EquipmentRarity.Artifact).WithStrength(12).WithLifeSteal(10).WithMagicResist(25).RequiresEvilAlignment().AsUnique());
        AddEquipment(Equipment.CreateAccessory(id++, "Amulet of Eternity", EquipmentSlot.Neck, 300000, EquipmentRarity.Artifact).WithStrength(10).WithConstitution(10).WithWisdom(10).WithMaxHP(100).WithMaxMana(100).AsUnique());
    }

    #endregion

    #region Rings (20 items)

    private static void InitializeRings()
    {
        int id = RingStart;

        AddEquipment(Equipment.CreateAccessory(id++, "Copper Ring", EquipmentSlot.LFinger, 25));
        AddEquipment(Equipment.CreateAccessory(id++, "Silver Ring", EquipmentSlot.LFinger, 100, EquipmentRarity.Uncommon).WithCharisma(1));
        AddEquipment(Equipment.CreateAccessory(id++, "Gold Ring", EquipmentSlot.LFinger, 300, EquipmentRarity.Uncommon).WithCharisma(2));
        AddEquipment(Equipment.CreateAccessory(id++, "Ring of Dexterity", EquipmentSlot.LFinger, 800, EquipmentRarity.Rare).WithDexterity(3));
        AddEquipment(Equipment.CreateAccessory(id++, "Ring of Strength", EquipmentSlot.LFinger, 800, EquipmentRarity.Rare).WithStrength(3));
        AddEquipment(Equipment.CreateAccessory(id++, "Mana Ring", EquipmentSlot.LFinger, 1000, EquipmentRarity.Rare).WithMaxMana(25));
        AddEquipment(Equipment.CreateAccessory(id++, "Ring of Protection", EquipmentSlot.LFinger, 1500, EquipmentRarity.Rare).WithDefence(2).WithMagicResist(5));
        AddEquipment(Equipment.CreateAccessory(id++, "Ring of Vitality", EquipmentSlot.LFinger, 2000, EquipmentRarity.Rare).WithMaxHP(25).WithConstitution(2));
        AddEquipment(Equipment.CreateAccessory(id++, "Sage's Ring", EquipmentSlot.LFinger, 3000, EquipmentRarity.Epic).WithIntelligence(4).WithWisdom(4));
        AddEquipment(Equipment.CreateAccessory(id++, "Ring of Shadows", EquipmentSlot.LFinger, 4000, EquipmentRarity.Epic).WithDexterity(5).WithCritChance(5));
        AddEquipment(Equipment.CreateAccessory(id++, "Warrior's Band", EquipmentSlot.LFinger, 5000, EquipmentRarity.Epic).WithStrength(5).WithMaxHP(20));
        AddEquipment(Equipment.CreateAccessory(id++, "Ring of the Archmage", EquipmentSlot.LFinger, 10000, EquipmentRarity.Epic).WithMaxMana(50).WithIntelligence(5));
        AddEquipment(Equipment.CreateAccessory(id++, "Master's Ring", EquipmentSlot.LFinger, 15000, EquipmentRarity.Legendary).WithStrength(6).WithDexterity(6).WithWisdom(6));
        AddEquipment(Equipment.CreateAccessory(id++, "Ring of Power", EquipmentSlot.LFinger, 25000, EquipmentRarity.Legendary).WithStrength(8).WithMaxHP(40));
        AddEquipment(Equipment.CreateAccessory(id++, "Ring of the Dragon", EquipmentSlot.LFinger, 40000, EquipmentRarity.Legendary).WithMagicResist(15).WithMaxHP(50));
        AddEquipment(Equipment.CreateAccessory(id++, "Celestial Band", EquipmentSlot.LFinger, 60000, EquipmentRarity.Legendary).WithWisdom(8).WithMagicResist(15).RequiresGoodAlignment());
        AddEquipment(Equipment.CreateAccessory(id++, "Ring of Darkness", EquipmentSlot.LFinger, 65000, EquipmentRarity.Legendary).WithStrength(8).WithLifeSteal(5).RequiresEvilAlignment());
        AddEquipment(Equipment.CreateAccessory(id++, "Titan's Band", EquipmentSlot.LFinger, 100000, EquipmentRarity.Artifact).WithStrength(10).WithConstitution(8).WithMaxHP(80).AsUnique());
        AddEquipment(Equipment.CreateAccessory(id++, "Ring of the Gods", EquipmentSlot.LFinger, 200000, EquipmentRarity.Artifact).WithStrength(8).WithDexterity(8).WithWisdom(8).WithIntelligence(8).AsUnique());
        AddEquipment(Equipment.CreateAccessory(id++, "Ring of Eternity", EquipmentSlot.LFinger, 350000, EquipmentRarity.Artifact).WithStrength(12).WithConstitution(12).WithMaxHP(100).WithMaxMana(50).WithLifeSteal(5).AsUnique());
    }

    #endregion
}
