using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Character class based directly on Pascal UserRec structure from INIT.PAS
/// This maintains perfect compatibility with the original game data
/// </summary>
public class Character
{
    // Basic character info - from Pascal UserRec
    public string Name1 { get; set; } = "";        // bbs/real name
    public string Name2 { get; set; } = "";        // game alias (this is the main name used)
    public CharacterAI AI { get; set; }             // (C)omputer or (H)uman
    public CharacterRace Race { get; set; }         // races
    public int Age { get; set; }                    // age
    public long Gold { get; set; }                  // gold in hand
    public long HP { get; set; }                    // hitpoints
    public long Experience { get; set; }            // experience
    public int Level { get; set; } = 1;
    public long BankGold { get; set; }              // gold in bank
    public long Chivalry { get; set; }              // chivalry
    public long Darkness { get; set; }              // darkness
    public int Fights { get; set; }                 // dungeon fights
    public long Strength { get; set; }              // strength
    public long Defence { get; set; }               // defence
    public long Healing { get; set; }               // healing potions
    public int MaxPotions => 20 + (Level - 1);      // max potions = 20 + (level - 1)
    public bool Allowed { get; set; }               // allowed to play
    public long MaxHP { get; set; }                 // max hitpoints
    public long LastOn { get; set; }                // laston, date
    public int AgePlus { get; set; }                // how soon before getting one year older
    public int DarkNr { get; set; }                 // dark deeds left
    public int ChivNr { get; set; }                 // good deeds left
    public int PFights { get; set; }                // player fights
    public bool King { get; set; }                  // king?
    public int Location { get; set; }               // offline location
    public string Team { get; set; } = "";          // team name
    public string TeamPW { get; set; } = "";        // team password
    public int TeamRec { get; set; }                // team record, days had town
    public int BGuard { get; set; }                 // type of guard
    public bool CTurf { get; set; }                 // is team in control of town
    public int GnollP { get; set; }                 // gnoll poison, temporary
    public int Mental { get; set; }                 // mental health
    public int Addict { get; set; }                 // drug addiction
    public bool WellWish { get; set; }              // has visited wishing well
    public int Height { get; set; }                 // height
    public int Weight { get; set; }                 // weight
    public int Eyes { get; set; }                   // eye color
    public int Hair { get; set; }                   // hair color
    public int Skin { get; set; }                   // skin color
    public CharacterSex Sex { get; set; }           // sex, male=1 female=2
    public long Mana { get; set; }                  // mana, spellcasters only
    public long MaxMana { get; set; }               // maxmana
    public long Stamina { get; set; }               // stamina
    public long Agility { get; set; }               // agility
    public long Charisma { get; set; }              // charisma
    public long Dexterity { get; set; }             // dexterity
    public long Wisdom { get; set; }                // wisdom
    public long Intelligence { get; set; }          // intelligence
    public long Constitution { get; set; }          // constitution  
    public long WeapPow { get; set; }               // weapon power
    public long ArmPow { get; set; }                // armor power
    
    // Disease status
    public bool Blind { get; set; }                 // blind?
    public bool Plague { get; set; }                // plague?
    public bool Smallpox { get; set; }              // smallpox?
    public bool Measles { get; set; }               // measles?
    public bool Leprosy { get; set; }               // leprosy?
    public int Mercy { get; set; }                  // mercy??
    
    // Inventory - array from Pascal
    public List<int> Item { get; set; } = new List<int>();             // inventory items (item IDs)
    public List<ObjType> ItemType { get; set; } = new List<ObjType>(); // type of items in inventory
    
    // Phrases used in different situations (6 phrases from Pascal)
    public List<string> Phrases { get; set; }       // phr array[1..6]
    /*
     * 1. what to say when being attacked
     * 2. what to say when you have defeated somebody
     * 3. what to say when you have been defeated
     * 4. what to say when you are begging for mercy
     * 5. what to say when you spare opponents life
     * 6. what to say when you don't spare opponents life
     */
    
    public bool AutoHeal { get; set; }              // autoheal in battle?
    public CharacterClass Class { get; set; }       // class
    public int Loyalty { get; set; }                // loyalty% (0-100)
    public int Haunt { get; set; }                  // how many demons haunt player
    public char Master { get; set; }                // level master player uses
    public int TFights { get; set; }                // team fights left
    public int Thiefs { get; set; }                 // thieveries left
    public int Brawls { get; set; }                 // brawls left
    public int Assa { get; set; }                   // assassinations left
    
    // Player description (4 lines from Pascal)
    public List<string> Description { get; set; }   // desc array[1..4]
    
    public int Poison { get; set; }                 // poison, adds to weapon
    
    // Spells (from Pascal: array[1..global_maxspells, 1..2] of boolean)
    public List<List<bool>> Spell { get; set; }     // spells [spell][known/mastered]
    
    // Close combat skills (from Pascal: array[1..global_maxcombat] of int)
    public List<int> Skill { get; set; }            // close combat skills
    
    public int Trains { get; set; }                 // training sessions
    
    // Equipment slots (item pointers from Pascal)
    public int LHand { get; set; }                  // item in left hand
    public int RHand { get; set; }                  // item in right hand
    public int Head { get; set; }                   // head
    public int Body { get; set; }                   // body
    public int Arms { get; set; }                   // arms
    public int LFinger { get; set; }                // left finger
    public int RFinger { get; set; }                // right finger
    public int Legs { get; set; }                   // legs
    public int Feet { get; set; }                   // feet
    public int Waist { get; set; }                  // waist
    public int Neck { get; set; }                   // neck
    public int Neck2 { get; set; }                  // neck2
    public int Face { get; set; }                   // face
    public int Shield { get; set; }                 // shield
    public int Hands { get; set; }                  // hands
    public int ABody { get; set; }                  // around body
    
    public bool Immortal { get; set; }              // never deleted for inactivity
    public string BattleCry { get; set; } = "";     // battle cry
    public int BGuardNr { get; set; }               // number of doorguards
    
    // Battle temporary flags
    public bool Casted { get; set; }                // used in battles
    public long Punch { get; set; }                 // player punch, temporary
    public long Absorb { get; set; }                // absorb punch, temporary
    public bool UsedItem { get; set; }              // has used item in battle
    public bool IsDefending { get; set; } = false;
    public bool IsRaging { get; set; } = false;        // Barbarian rage state
    public int SmiteChargesRemaining { get; set; } = 0; // Paladin daily smite uses left
    
    // Magical combat buffs
    public int MagicACBonus { get; set; } = 0;          // Flat AC bonus from spells like Shield/Prismatic Cage
    public int DamageAbsorptionPool { get; set; } = 0;  // Remaining damage Stoneskin can absorb

    // Cursed equipment flags
    public bool WeaponCursed { get; set; } = false;     // Weapon is cursed
    public bool ArmorCursed { get; set; } = false;      // Armor is cursed
    public bool ShieldCursed { get; set; } = false;     // Shield is cursed

    // NEW: Modern RPG Equipment System
    // Dictionary mapping each slot to equipment ID (0 = empty)
    public Dictionary<EquipmentSlot, int> EquippedItems { get; set; } = new();

    // Base stats (without equipment bonuses) - for recalculation
    public long BaseStrength { get; set; }
    public long BaseDexterity { get; set; }
    public long BaseConstitution { get; set; }
    public long BaseIntelligence { get; set; }
    public long BaseWisdom { get; set; }
    public long BaseCharisma { get; set; }
    public long BaseMaxHP { get; set; }
    public long BaseMaxMana { get; set; }
    public long BaseDefence { get; set; }
    public long BaseStamina { get; set; }
    public long BaseAgility { get; set; }

    // Weapon configuration detection
    public bool IsDualWielding =>
        EquippedItems.TryGetValue(EquipmentSlot.MainHand, out var mainId) && mainId > 0 &&
        EquippedItems.TryGetValue(EquipmentSlot.OffHand, out var offId) && offId > 0 &&
        EquipmentDatabase.GetById(mainId)?.Handedness == WeaponHandedness.OneHanded &&
        EquipmentDatabase.GetById(offId)?.Handedness == WeaponHandedness.OneHanded;

    public bool HasShieldEquipped =>
        EquippedItems.TryGetValue(EquipmentSlot.OffHand, out var offId) && offId > 0 &&
        EquipmentDatabase.GetById(offId)?.WeaponType == WeaponType.Shield ||
        EquipmentDatabase.GetById(offId)?.WeaponType == WeaponType.Buckler ||
        EquipmentDatabase.GetById(offId)?.WeaponType == WeaponType.TowerShield;

    public bool IsTwoHanding =>
        EquippedItems.TryGetValue(EquipmentSlot.MainHand, out var mainId) && mainId > 0 &&
        EquipmentDatabase.GetById(mainId)?.Handedness == WeaponHandedness.TwoHanded;

    /// <summary>
    /// Get the equipment in a specific slot
    /// </summary>
    public Equipment GetEquipment(EquipmentSlot slot)
    {
        if (EquippedItems.TryGetValue(slot, out var id) && id > 0)
            return EquipmentDatabase.GetById(id);
        return null;
    }

    /// <summary>
    /// Equip an item to the appropriate slot
    /// </summary>
    public bool EquipItem(Equipment item, out string message)
    {
        message = "";

        if (item == null)
        {
            message = "No item to equip";
            return false;
        }

        // Check if character can equip this item
        if (!item.CanEquip(this, out string reason))
        {
            message = reason;
            return false;
        }

        // Handle two-handed weapons - must unequip off-hand
        if (item.Handedness == WeaponHandedness.TwoHanded)
        {
            if (EquippedItems.TryGetValue(EquipmentSlot.OffHand, out var offId) && offId > 0)
            {
                UnequipSlot(EquipmentSlot.OffHand);
                message = "Unequipped off-hand to use two-handed weapon. ";
            }
        }

        // Handle shields/off-hand - must unequip if using 2H weapon
        if (item.Slot == EquipmentSlot.OffHand && IsTwoHanding)
        {
            message = "Cannot equip off-hand item while using two-handed weapon";
            return false;
        }

        // Unequip current item in slot if any
        var slot = item.Slot;

        // For weapons, determine the correct slot
        if (item.Handedness == WeaponHandedness.OneHanded || item.Handedness == WeaponHandedness.TwoHanded)
            slot = EquipmentSlot.MainHand;
        else if (item.Handedness == WeaponHandedness.OffHandOnly)
            slot = EquipmentSlot.OffHand;

        UnequipSlot(slot);

        // Equip the new item
        EquippedItems[slot] = item.Id;

        // Apply stats
        item.ApplyToCharacter(this);

        message += $"Equipped {item.Name}";
        return true;
    }

    /// <summary>
    /// Unequip item from a specific slot
    /// </summary>
    public Equipment UnequipSlot(EquipmentSlot slot)
    {
        if (!EquippedItems.TryGetValue(slot, out var id) || id == 0)
            return null;

        var item = EquipmentDatabase.GetById(id);
        if (item != null)
        {
            // Check if cursed
            if (item.IsCursed)
                return null; // Can't unequip cursed items

            // Remove stats
            item.RemoveFromCharacter(this);
        }

        EquippedItems[slot] = 0;
        return item;
    }

    /// <summary>
    /// Recalculate all stats from base values plus equipment bonuses
    /// </summary>
    public void RecalculateStats()
    {
        // Start from base values
        Strength = BaseStrength;
        Dexterity = BaseDexterity;
        Constitution = BaseConstitution;
        Intelligence = BaseIntelligence;
        Wisdom = BaseWisdom;
        Charisma = BaseCharisma;
        MaxHP = BaseMaxHP;
        MaxMana = BaseMaxMana;
        Defence = BaseDefence;
        Stamina = BaseStamina;
        Agility = BaseAgility;
        WeapPow = 0;
        ArmPow = 0;

        // Add bonuses from all equipped items
        foreach (var kvp in EquippedItems)
        {
            if (kvp.Value <= 0) continue;
            var item = EquipmentDatabase.GetById(kvp.Value);
            item?.ApplyToCharacter(this);
        }

        // Keep current HP/Mana within bounds
        HP = Math.Min(HP, MaxHP);
        Mana = Math.Min(Mana, MaxMana);
    }

    /// <summary>
    /// Initialize base stats from current values (call when creating character or loading old save)
    /// </summary>
    public void InitializeBaseStats()
    {
        BaseStrength = Strength;
        BaseDexterity = Dexterity;
        BaseConstitution = Constitution;
        BaseIntelligence = Intelligence;
        BaseWisdom = Wisdom;
        BaseCharisma = Charisma;
        BaseMaxHP = MaxHP;
        BaseMaxMana = MaxMana;
        BaseDefence = Defence;
        BaseStamina = Stamina;
        BaseAgility = Agility;
    }

    /// <summary>
    /// Get total equipment value (for sell price calculation)
    /// </summary>
    public long GetTotalEquipmentValue()
    {
        long total = 0;
        foreach (var kvp in EquippedItems)
        {
            if (kvp.Value <= 0) continue;
            var item = EquipmentDatabase.GetById(kvp.Value);
            if (item != null) total += item.Value;
        }
        return total;
    }

    /// <summary>
    /// Get equipment summary for display
    /// </summary>
    public string GetEquipmentSummary()
    {
        var lines = new List<string>();

        void AddSlot(string label, EquipmentSlot slot)
        {
            var item = GetEquipment(slot);
            lines.Add($"{label}: {item?.Name ?? "None"}");
        }

        AddSlot("Main Hand", EquipmentSlot.MainHand);
        AddSlot("Off Hand", EquipmentSlot.OffHand);
        AddSlot("Head", EquipmentSlot.Head);
        AddSlot("Body", EquipmentSlot.Body);
        AddSlot("Arms", EquipmentSlot.Arms);
        AddSlot("Hands", EquipmentSlot.Hands);
        AddSlot("Legs", EquipmentSlot.Legs);
        AddSlot("Feet", EquipmentSlot.Feet);
        AddSlot("Waist", EquipmentSlot.Waist);
        AddSlot("Cloak", EquipmentSlot.Cloak);
        AddSlot("Neck", EquipmentSlot.Neck);
        AddSlot("Neck 2", EquipmentSlot.Neck2);
        AddSlot("Face", EquipmentSlot.Face);
        AddSlot("Left Ring", EquipmentSlot.LFinger);
        AddSlot("Right Ring", EquipmentSlot.RFinger);

        return string.Join("\n", lines);
    }

    // Kill statistics
    public long MKills { get; set; }                // monster kills
    public long MDefeats { get; set; }              // monster defeats
    public long PKills { get; set; }                // player kills
    public long PDefeats { get; set; }              // player defeats
    
    // New for version 0.08+
    public long Interest { get; set; }              // accumulated bank interest
    public long AliveBonus { get; set; }            // staying alive bonus
    public bool Expert { get; set; }                // expert menus ON/OFF
    public int MaxTime { get; set; }                // max minutes per session
    public byte Ear { get; set; }                   // internode message handling
    public char CastIn { get; set; }                // casting flag
    public int Weapon { get; set; }                 // OLD mode weapon
    public int Armor { get; set; }                  // OLD mode armor
    public int APow { get; set; }                   // OLD mode armor power
    public int WPow { get; set; }                   // OLD mode weapon power
    public byte DisRes { get; set; }                // disease resistance
    public bool AMember { get; set; }               // alchemist society member
    
    // Medals (from Pascal: array[1..20] of boolean)
    public List<bool> Medal { get; set; }           // medals earned
    
    public bool BankGuard { get; set; }             // bank guard?
    public long BankWage { get; set; }              // salary from bank
    public byte WeapHag { get; set; } = 3;          // weapon shop haggling attempts left
    public byte ArmHag { get; set; } = 3;           // armor shop haggling attempts left
    public int RecNr { get; set; }                  // file record number
    public bool AutoMenu { get; set; }              // auto draw menus
    
    // New for version 0.14+
    public int Quests { get; set; }                 // completed missions/quests
    public bool Deleted { get; set; }               // is record deleted
    public string God { get; set; } = "";           // worshipped god name
    public long RoyQuests { get; set; }             // royal quests accomplished
    
    // New for version 0.17+
    public long RoyTaxPaid { get; set; }            // royal taxes paid
    public byte Wrestlings { get; set; }            // wrestling matches left
    public byte DrinksLeft { get; set; }            // drinks left today
    public byte DaysInPrison { get; set; }          // days left in prison
    
    // New for version 0.18+
    public byte UmanBearTries { get; set; }         // bear taming attempts
    public byte Massage { get; set; }               // massages today
    public byte GymSessions { get; set; }           // gym sessions left
    public byte GymOwner { get; set; }              // gym controller
    public byte GymCard { get; set; }               // free gym card
    public int RoyQuestsToday { get; set; }         // royal quests today
    public byte KingVotePoll { get; set; }          // days since king vote
    public byte KingLastVote { get; set; }          // last vote value
    
    // Marriage and family
    public bool Married { get; set; }               // is married?
    public int Kids { get; set; }                   // number of children
    public int IntimacyActs { get; set; }           // intimacy acts left today
    public byte Pregnancy { get; set; }             // pregnancy days (0=not pregnant)
    public string FatherID { get; set; } = "";      // father's unique ID
    public byte AutoHate { get; set; }              // auto-worsen relations
    public string ID { get; set; } = "";            // unique player ID
    public bool TaxRelief { get; set; }             // free from tax
    
    public int MarriedTimes { get; set; }           // marriage counter
    public int BardSongsLeft { get; set; }          // bard songs left
    public byte PrisonEscapes { get; set; }         // escape attempts allowed
    public byte FileType { get; set; }              // file type (1=player, 2=npc)
    public int Resurrections { get; set; }          // resurrections left
    
    // New for version 0.20+
    public int PickPocketAttempts { get; set; }     // pick pocket attempts
    public int BankRobberyAttempts { get; set; }    // bank robbery attempts
    
    // Religious and Divine Properties (Pascal UserRec fields)
    public bool IsMarried { get; set; } = false;       // Marriage status
    public string SpouseName { get; set; } = "";       // Name of spouse
    public int MarriageAttempts { get; set; } = 0;     // Daily marriage attempts used
    public bool BannedFromChurch { get; set; } = false; // Banned from religious services
    public DateTime LastResurrection { get; set; } = DateTime.MinValue; // Last time resurrected
    public int ResurrectionsUsed { get; set; } = 0;    // Total resurrections used
    public int MaxResurrections { get; set; } = 3;     // Maximum resurrections allowed
    
    // Divine favor and religious standing  
    public int DivineBlessing { get; set; } = 0;       // Divine blessing duration (days)
    public bool HasHolyWater { get; set; } = false;    // Carrying holy water
    public DateTime LastConfession { get; set; } = DateTime.MinValue; // Last confession
    public int SacrificesMade { get; set; } = 0;       // Total sacrifices to gods
    
    // Church-related statistics
    public long ChurchDonations { get; set; } = 0;     // Total amount donated to church
    public int BlessingsReceived { get; set; } = 0;    // Number of blessings received
    public int HealingsReceived { get; set; } = 0;     // Number of healings received
    
    // Additional compatibility properties
    public int QuestsLeft { get; set; } = 5;
    public List<Quest> ActiveQuests { get; set; } = new();
    public int DrinkslLeft { get; set; } = 5;
    public long WeaponPower { get; set; }
    public long ArmorClass { get; set; }
    public int WantedLvl { get; set; } = 0;  // Wanted level for crime tracking
    
    // Missing inventory system
    public List<Item> Inventory { get; set; } = new();
    
    // Current values (convenience properties)
    public long CurrentHP 
    { 
        get => HP; 
        set => HP = value; 
    }
    
    public long CurrentMana 
    { 
        get => Mana; 
        set => Mana = value; 
    }
    
    // Additional properties for API compatibility
    // TurnCount - counts UP from 0, drives world simulation (single-player persistent system)
    public int TurnCount { get; set; } = 0;

    // Legacy properties for compatibility (no longer used for limiting gameplay)
    private int? _manualTurnsRemaining;
    public int TurnsRemaining
    {
        get => _manualTurnsRemaining ?? TurnCount; // Now returns turn count for save compatibility
        set => _manualTurnsRemaining = value;
    }
    public int PrisonsLeft { get; set; } = 0; // Prison sentences remaining
    public int ExecuteLeft { get; set; } = 0; // Execution attempts remaining
    public int MarryActions { get; set; } = 0; // Marriage actions remaining
    public int WolfFeed { get; set; } = 0; // Wolf feeding actions
    public int RoyalAdoptions { get; set; } = 0; // Royal adoption actions
    public int DaysInPower { get; set; } = 0; // Days as king/ruler
    public int Fame { get; set; } = 0; // Fame/reputation level
    
    public DateTime LastLogin { get; set; }
    
    // Generic status effects (duration in rounds)
    public Dictionary<StatusEffect, int> ActiveStatuses { get; set; } = new();

    public bool HasStatus(StatusEffect s) => ActiveStatuses.ContainsKey(s);

    public void ApplyStatus(StatusEffect status, int duration)
    {
        if (status == StatusEffect.None) return;
        ActiveStatuses[status] = duration;
    }

    /// <summary>
    /// Tick status durations and apply per-round effects (poison damage, etc.).
    /// Should be called once per combat round.
    /// Returns a list of status effect messages to display.
    /// </summary>
    public List<(string message, string color)> ProcessStatusEffects()
    {
        var messages = new List<(string message, string color)>();
        if (ActiveStatuses.Count == 0) return messages;

        var toRemove = new List<StatusEffect>();
        var rnd = new Random();

        foreach (var kvp in ActiveStatuses.ToList())
        {
            int dmg = 0;
            switch (kvp.Key)
            {
                case StatusEffect.Poisoned:
                    dmg = rnd.Next(1, 5); // 1d4
                    HP = Math.Max(0, HP - dmg);
                    messages.Add(($"{DisplayName} takes {dmg} poison damage!", "green"));
                    break;

                case StatusEffect.Bleeding:
                    dmg = rnd.Next(1, 7); // 1d6
                    HP = Math.Max(0, HP - dmg);
                    messages.Add(($"{DisplayName} bleeds for {dmg} damage!", "red"));
                    break;

                case StatusEffect.Burning:
                    dmg = rnd.Next(2, 9); // 2d4
                    HP = Math.Max(0, HP - dmg);
                    messages.Add(($"{DisplayName} burns for {dmg} fire damage!", "bright_red"));
                    break;

                case StatusEffect.Frozen:
                    dmg = rnd.Next(1, 4); // 1d3
                    HP = Math.Max(0, HP - dmg);
                    messages.Add(($"{DisplayName} takes {dmg} cold damage from the frost!", "bright_cyan"));
                    break;

                case StatusEffect.Cursed:
                    dmg = rnd.Next(1, 3); // 1d2
                    HP = Math.Max(0, HP - dmg);
                    messages.Add(($"{DisplayName} suffers {dmg} curse damage!", "magenta"));
                    break;

                case StatusEffect.Diseased:
                    HP = Math.Max(0, HP - 1);
                    messages.Add(($"{DisplayName} suffers from disease! (-1 HP)", "yellow"));
                    break;

                case StatusEffect.Regenerating:
                    var heal = rnd.Next(1, 7); // 1d6
                    HP = Math.Min(HP + heal, MaxHP);
                    messages.Add(($"{DisplayName} regenerates {heal} HP!", "bright_green"));
                    break;

                case StatusEffect.Reflecting:
                    // Handled during damage calculation, just remind
                    break;

                case StatusEffect.Lifesteal:
                    // Handled during damage calculation
                    break;
            }

            // Decrement duration (some effects like Stoneskin don't expire by time)
            if (kvp.Key != StatusEffect.Stoneskin && kvp.Key != StatusEffect.Shielded)
            {
                ActiveStatuses[kvp.Key] = kvp.Value - 1;
                if (ActiveStatuses[kvp.Key] <= 0)
                    toRemove.Add(kvp.Key);
            }
        }

        foreach (var s in toRemove)
        {
            ActiveStatuses.Remove(s);
            string effectName = s.GetShortName();

            switch (s)
            {
                case StatusEffect.Blessed:
                case StatusEffect.Defending:
                case StatusEffect.Protected:
                    MagicACBonus = 0;
                    messages.Add(($"{DisplayName}'s {effectName} effect fades.", "gray"));
                    break;
                case StatusEffect.Stoneskin:
                    DamageAbsorptionPool = 0;
                    messages.Add(($"{DisplayName}'s stoneskin crumbles away.", "gray"));
                    break;
                case StatusEffect.Raging:
                    IsRaging = false;
                    messages.Add(($"{DisplayName}'s rage subsides.", "gray"));
                    break;
                case StatusEffect.Haste:
                    messages.Add(($"{DisplayName} slows to normal speed.", "gray"));
                    break;
                case StatusEffect.Slow:
                    messages.Add(($"{DisplayName} can move normally again.", "gray"));
                    break;
                case StatusEffect.Stunned:
                case StatusEffect.Paralyzed:
                    messages.Add(($"{DisplayName} recovers and can act again!", "white"));
                    break;
                case StatusEffect.Silenced:
                    messages.Add(($"{DisplayName} can cast spells again.", "bright_cyan"));
                    break;
                case StatusEffect.Blinded:
                    messages.Add(($"{DisplayName}'s vision clears.", "white"));
                    break;
                case StatusEffect.Sleeping:
                    messages.Add(($"{DisplayName} wakes up!", "white"));
                    break;
                case StatusEffect.Poisoned:
                case StatusEffect.Bleeding:
                case StatusEffect.Burning:
                case StatusEffect.Frozen:
                case StatusEffect.Cursed:
                case StatusEffect.Diseased:
                    messages.Add(($"{DisplayName} is no longer {s.ToString().ToLower()}.", "gray"));
                    break;
                default:
                    messages.Add(($"{DisplayName}'s {effectName} wears off.", "gray"));
                    break;
            }
        }

        return messages;
    }

    /// <summary>
    /// Check if the character can take actions this turn
    /// </summary>
    public bool CanAct()
    {
        foreach (var status in ActiveStatuses.Keys)
        {
            if (status.PreventsAction())
                return false;
        }
        return true;
    }

    /// <summary>
    /// Check if the character can cast spells
    /// </summary>
    public bool CanCastSpells()
    {
        foreach (var status in ActiveStatuses.Keys)
        {
            if (status.PreventsSpellcasting())
                return false;
        }
        return true;
    }

    /// <summary>
    /// Get accuracy modifier from status effects
    /// </summary>
    public float GetAccuracyModifier()
    {
        float modifier = 1.0f;
        if (HasStatus(StatusEffect.Blinded)) modifier *= 0.5f;
        if (HasStatus(StatusEffect.PowerStance)) modifier *= 0.75f;
        if (HasStatus(StatusEffect.Frozen)) modifier *= 0.75f;
        return modifier;
    }

    /// <summary>
    /// Get damage dealt modifier from status effects
    /// </summary>
    public float GetDamageDealtModifier()
    {
        float modifier = 1.0f;
        if (HasStatus(StatusEffect.Raging) || IsRaging) modifier *= 2.0f;
        if (HasStatus(StatusEffect.PowerStance)) modifier *= 1.5f;
        if (HasStatus(StatusEffect.Berserk)) modifier *= 1.5f;
        if (HasStatus(StatusEffect.Exhausted)) modifier *= 0.75f;
        if (HasStatus(StatusEffect.Empowered)) modifier *= 1.5f; // For spells
        if (HasStatus(StatusEffect.Hidden)) modifier *= 1.5f; // Stealth bonus
        return modifier;
    }

    /// <summary>
    /// Get damage taken modifier from status effects
    /// </summary>
    public float GetDamageTakenModifier()
    {
        float modifier = 1.0f;
        if (HasStatus(StatusEffect.Defending)) modifier *= 0.5f;
        if (HasStatus(StatusEffect.Vulnerable)) modifier *= 1.25f;
        if (HasStatus(StatusEffect.Invulnerable)) modifier = 0f;
        return modifier;
    }

    /// <summary>
    /// Get number of attacks this round based on status effects
    /// </summary>
    public int GetAttackCountModifier(int baseAttacks)
    {
        int attacks = baseAttacks;
        if (HasStatus(StatusEffect.Haste)) attacks *= 2;
        if (HasStatus(StatusEffect.Slow)) attacks = Math.Max(1, attacks / 2);
        if (HasStatus(StatusEffect.Frozen)) attacks = Math.Max(1, attacks / 2);
        return attacks;
    }

    /// <summary>
    /// Remove a status effect
    /// </summary>
    public void RemoveStatus(StatusEffect effect)
    {
        if (ActiveStatuses.ContainsKey(effect))
        {
            ActiveStatuses.Remove(effect);

            // Clean up associated state
            switch (effect)
            {
                case StatusEffect.Raging:
                    IsRaging = false;
                    break;
                case StatusEffect.Stoneskin:
                    DamageAbsorptionPool = 0;
                    break;
                case StatusEffect.Blessed:
                case StatusEffect.Defending:
                case StatusEffect.Protected:
                    MagicACBonus = 0;
                    break;
            }
        }
    }

    /// <summary>
    /// Clear all status effects (e.g., after combat)
    /// </summary>
    public void ClearAllStatuses()
    {
        ActiveStatuses.Clear();
        IsRaging = false;
        IsDefending = false;
        DamageAbsorptionPool = 0;
        MagicACBonus = 0;
    }

    /// <summary>
    /// Get a formatted string of active status effects for display
    /// </summary>
    public string GetStatusDisplayString()
    {
        if (ActiveStatuses.Count == 0) return "";

        var parts = new List<string>();
        foreach (var kvp in ActiveStatuses)
        {
            string shortName = kvp.Key.GetShortName();
            parts.Add($"{shortName}({kvp.Value})");
        }
        return string.Join(" ", parts);
    }
    
    // Constructor to initialize lists
    public Character()
    {
        // Initialize lists based on Pascal array sizes
        Item = new List<int>(new int[GameConfig.MaxItem]);
        ItemType = new List<ObjType>(new ObjType[GameConfig.MaxItem]);
        Phrases = new List<string>(new string[6]);
        Description = new List<string>(new string[4]);
        
        // Initialize spells array [maxspells][2]
        Spell = new List<List<bool>>();
        for (int i = 0; i < GameConfig.MaxSpells; i++)
        {
            Spell.Add(new List<bool> { false, false });
        }
        
        // Initialize combat skills
        Skill = new List<int>(new int[GameConfig.MaxCombat]);
        
        // Initialize medals
        Medal = new List<bool>(new bool[20]);
    }
    
    // Helper properties for commonly used calculations
    public bool IsAlive => HP > 0;
    public bool IsPlayer => AI == CharacterAI.Human;
    public bool IsNPC => AI == CharacterAI.Computer;
    public string DisplayName => !string.IsNullOrEmpty(Name2) ? Name2 : Name1;

    // TurnsLeft - now just returns TurnCount for backward compatibility (no limits in single-player)
    public int TurnsLeft => TurnCount;
    
    // Combat-related properties
    public long WeaponValue => WeapPow;
    public long ArmorValue => ArmPow;
    public string WeaponName => GetEquippedItemName(RHand); // Right hand weapon
    public string ArmorName => GetEquippedItemName(Body);   // Body armor
    
    // Status properties
    public bool Poisoned => Poison > 0;
    public int PoisonLevel => Poison;
    public bool OnSteroids => false; // TODO: Implement steroid system
    public int DrugDays => 0; // TODO: Implement drug system
    
    // Social properties
    public string TeamName => Team;
    public bool IsTeamLeader => CTurf;
    public int Children => Kids;
    
    /// <summary>
    /// Compatibility property that maps to CTurf for API consistency
    /// </summary>
    public bool ControlsTurf 
    { 
        get => CTurf; 
        set => CTurf = value; 
    }
    
    /// <summary>
    /// Compatibility property that maps to TeamPW for API consistency
    /// </summary>
    public string TeamPassword 
    { 
        get => TeamPW; 
        set => TeamPW = value; 
    }
    
    /// <summary>
    /// Compatibility property that maps to TeamRec for API consistency
    /// </summary>
    public int TeamRecord 
    { 
        get => TeamRec; 
        set => TeamRec = value; 
    }
    
    private string GetEquippedItemName(int itemId)
    {
        if (itemId == 0) return "None";
        // TODO: Implement item lookup from game data
        return $"Item{itemId}";
    }
    
    // Pascal-compatible string access for names
    public string Name => Name2; // Main game name
    public string RealName => Name1; // BBS name
    public string KingName => King ? DisplayName : "";
    
    public DateTime Created { get; set; } = DateTime.Now;

    // Alias American spelling used by some systems
    public long Defense
    {
        get => Defence;
        set => Defence = value;
    }

    // Simplified thievery skill placeholder
    public long Thievery { get; set; }

    // Simple level-up event hook for UI/system code expecting it
    public event Action<Character>? OnLevelUp;

    public void RaiseLevel(int newLevel)
    {
        if (newLevel > Level)
        {
            Level = newLevel;
            OnLevelUp?.Invoke(this);
        }
    }

    /// <summary>
    /// Returns a CombatModifiers object describing bonuses and abilities granted by this character's class.
    /// The numbers largely mirror classic Usurper balance but are open to tuning.
    /// </summary>
    public CombatModifiers GetClassCombatModifiers()
    {
        return Class switch
        {
            CharacterClass.Warrior => new CombatModifiers { AttackBonus = Level / 5, ExtraAttacks = Level / 10 },
            CharacterClass.Assassin => new CombatModifiers { BackstabMultiplier = 3.0f, PoisonChance = 25 },
            CharacterClass.Barbarian => new CombatModifiers { DamageReduction = 2, RageAvailable = true },
            CharacterClass.Paladin => new CombatModifiers { SmiteCharges = 1 + Level / 10, AuraBonus = 2 },
            CharacterClass.Ranger => new CombatModifiers { RangedBonus = 4, Tracking = true },
            _ => new CombatModifiers()
        };
    }
}

/// <summary>
/// Character AI type from Pascal
/// </summary>
public enum CharacterAI
{
    Computer = 'C',
    Human = 'H',
    Civilian = 'N'
}

/// <summary>
/// Character sex from Pascal (1=male, 2=female)
/// </summary>
public enum CharacterSex
{
    Male = 1,
    Female = 2
}

/// <summary>
/// Character races from Pascal races enum
/// </summary>
public enum CharacterRace
{
    Human,      // change RATING.PAS and VARIOUS.PAS when changing # of races
    Hobbit,
    Elf,
    HalfElf,
    Dwarf,
    Troll,
    Orc,
    Gnome,
    Gnoll,
    Mutant
}

/// <summary>
/// Character classes from Pascal classes enum
/// </summary>
public enum CharacterClass
{
    Alchemist,  // change RATING.PAS and VARIOUS.PAS when changing # of classes
    Assassin,
    Barbarian,  // no special ability
    Bard,       // no special ability
    Cleric,
    Jester,     // no special ability
    Magician,
    Paladin,
    Ranger,     // no special ability
    Sage,
    Warrior     // no special ability
}

/// <summary>
/// Object types from Pascal ObjType enum
/// </summary>
public enum ObjType
{
    Head = 1,
    Body = 2,
    Arms = 3,
    Hands = 4,
    Fingers = 5,
    Legs = 6,
    Feet = 7,
    Waist = 8,
    Neck = 9,
    Face = 10,
    Shield = 11,
    Food = 12,
    Drink = 13,
    Weapon = 14,
    Abody = 15,  // around body
    Magic = 16,
    Potion = 17
}

/// <summary>
/// Disease types from Pascal Cures enum
/// </summary>
public enum Cures
{
    Nothing,
    All,
    Blindness,
    Plague,
    Smallpox,
    Measles,
    Leprosy
} 
