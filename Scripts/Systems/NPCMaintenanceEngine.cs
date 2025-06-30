using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// NPC Maintenance Engine for automated NPC behaviors
/// Pascal NPCMAINT.PAS integration with existing AI systems
/// </summary>
public class NPCMaintenanceEngine : Node
{
    private EnhancedNPCBehaviorSystem behaviorSystem;
    private WorldSimulator worldSimulator;
    private LocationManager locationManager;
    private Random random = new Random();
    
    // Pascal maintenance settings
    private bool npcMaintEnabled = true;
    private bool npcShoppingEnabled = true;
    private bool npcBelieverSystemEnabled = true;
    private bool npcGangManagementEnabled = true;
    
    // Shopping AI data (Pascal NPCMAINT.PAS shopping logic)
    private Dictionary<string, NPCShoppingProfile> shoppingProfiles = new();
    private Dictionary<CharacterClass, ShopPreferences> classShopPreferences = new();
    
    public override void _Ready()
    {
        behaviorSystem = GetNode<EnhancedNPCBehaviorSystem>("/root/EnhancedNPCBehaviorSystem");
        worldSimulator = GetNode<WorldSimulator>("/root/WorldSimulator");
        locationManager = GetNode<LocationManager>("/root/LocationManager");
        
        InitializeShopPreferences();
    }
    
    /// <summary>
    /// Main maintenance cycle for NPCs
    /// </summary>
    public async Task RunMaintenanceCycle(List<NPC> npcs)
    {
        if (!npcMaintEnabled) return;
        
        GD.Print("[NPCMaint] Starting maintenance cycle");
        
        foreach (var npc in npcs.Where(n => n.IsAlive))
        {
            ProcessNPCMaintenance(npc);
        }
        
        ProcessGangMaintenance(npcs);
        
        GD.Print("[NPCMaint] Maintenance cycle completed");
    }
    
    /// <summary>
    /// Process individual NPC maintenance
    /// </summary>
    private void ProcessNPCMaintenance(NPC npc)
    {
        // Pascal NPC maintenance logic
        if (random.Next(3) == 0) // 33% chance
        {
            ProcessNPCShopping(npc);
        }
        
        if (random.Next(5) == 0) // 20% chance
        {
            ProcessNPCBelieverSystem(npc);
        }
    }
    
    /// <summary>
    /// Pascal shopping system for NPCs
    /// </summary>
    private void ProcessNPCShopping(NPC npc)
    {
        if (npc.Gold < 100) return;
        
        // Determine shopping goals based on class and personality
        var shoppingGoals = DetermineShoppingGoals(npc);
        
        foreach (var goal in shoppingGoals)
        {
            if (AttemptPurchase(npc, goal))
            {
                GD.Print($"[NPCMaint] {npc.Name} purchased {goal}");
                break; // One purchase per cycle
            }
        }
    }
    
    /// <summary>
    /// Pascal believer system
    /// </summary>
    private void ProcessNPCBelieverSystem(NPC npc)
    {
        if (string.IsNullOrEmpty(npc.God))
        {
            // Potential conversion
            if (random.Next(10) == 0)
            {
                AttemptFaithConversion(npc);
            }
        }
        else
        {
            // Existing believer actions
            ProcessExistingBeliever(npc);
        }
    }
    
    /// <summary>
    /// Gang maintenance from Pascal
    /// </summary>
    private void ProcessGangMaintenance(List<NPC> npcs)
    {
        var gangs = GetGangSizes(npcs);
        
        foreach (var gang in gangs)
        {
            if (gang.Value <= 3 && IsNPCOnlyGang(npcs, gang.Key))
            {
                if (random.Next(4) == 0)
                {
                    DissolveGang(gang.Key, npcs);
                }
                else
                {
                    RecruitGangMembers(gang.Key, npcs);
                }
            }
        }
    }
    
    private List<string> DetermineShoppingGoals(NPC npc)
    {
        var goals = new List<string>();
        
        switch (npc.Class)
        {
            case CharacterClass.Fighter:
                if (npc.WeaponPower < npc.Level * 20)
                    goals.Add("weapon");
                if (npc.ArmorClass < npc.Level * 15)
                    goals.Add("armor");
                break;
                
            case CharacterClass.Magician:
                if (npc.Mana < npc.MaxMana * 0.8f)
                    goals.Add("mana_potion");
                goals.Add("magic_item");
                break;
        }
        
        return goals;
    }
    
    private bool AttemptPurchase(NPC npc, string goal)
    {
        // Simplified purchase logic
        var cost = random.Next(50, 500);
        if (npc.Gold >= cost)
        {
            npc.Gold -= cost;
            return true;
        }
        return false;
    }
    
    private void AttemptFaithConversion(NPC npc)
    {
        var availableGods = new[] { "Nosferatu", "Darkcloak", "Druid" };
        if (availableGods.Length > 0)
        {
            npc.God = availableGods[random.Next(availableGods.Length)];
            GD.Print($"[NPCMaint] {npc.Name} converted to {npc.God}");
        }
    }
    
    private void ProcessExistingBeliever(NPC npc)
    {
        // Pascal believer actions
        if (random.Next(10) == 0)
        {
            GD.Print($"[NPCMaint] {npc.Name} performed religious act");
        }
    }
    
    private Dictionary<string, int> GetGangSizes(List<NPC> npcs)
    {
        return npcs.Where(n => !string.IsNullOrEmpty(n.Team))
                  .GroupBy(n => n.Team)
                  .ToDictionary(g => g.Key, g => g.Count());
    }
    
    private bool IsNPCOnlyGang(List<NPC> npcs, string gangName)
    {
        var members = npcs.Where(n => n.Team == gangName);
        return members.Any() && members.All(n => n.AI == CharacterAI.Computer);
    }
    
    private void DissolveGang(string gangName, List<NPC> npcs)
    {
        GD.Print($"[NPCMaint] Dissolving gang: {gangName}");
        
        foreach (var member in npcs.Where(n => n.Team == gangName))
        {
            member.Team = "";
            member.ControlsTurf = false;
        }
    }
    
    private void RecruitGangMembers(string gangName, List<NPC> npcs)
    {
        var availableNPCs = npcs.Where(n => 
            string.IsNullOrEmpty(n.Team) && 
            !n.King && 
            n.IsAlive).ToList();
        
        foreach (var candidate in availableNPCs.Take(2))
        {
            if (random.Next(3) == 0)
            {
                candidate.Team = gangName;
                GD.Print($"[NPCMaint] {candidate.Name} recruited to {gangName}");
            }
        }
    }
    
    #region Pascal Shopping AI System
    
    /// <summary>
    /// Enhanced NPC shopping system - Pascal NPCMAINT.PAS Npc_Buy function
    /// </summary>
    private async Task ProcessNPCShopping(List<NPC> npcs)
    {
        foreach (var npc in npcs.Where(n => n.IsAlive && ShouldNPCShop(n)))
        {
            await ProcessIndividualNPCShopping(npc);
        }
    }
    
    /// <summary>
    /// Process individual NPC shopping - Pascal Ok_To_Buy logic
    /// </summary>
    private async Task ProcessIndividualNPCShopping(NPC npc)
    {
        var profile = GetOrCreateShoppingProfile(npc);
        var preferences = GetShopPreferences(npc.Class);
        
        // Determine shopping goals based on Pascal logic
        var shoppingGoals = DetermineShoppingGoals(npc, profile, preferences);
        
        foreach (var goal in shoppingGoals)
        {
            var success = await AttemptPurchase(npc, goal);
            if (success)
            {
                profile.LastSuccessfulPurchase = DateTime.Now;
                profile.PurchaseHistory.Add(new PurchaseRecord
                {
                    ItemType = goal.ItemType,
                    Cost = goal.EstimatedCost,
                    Timestamp = DateTime.Now,
                    Location = npc.CurrentLocation
                });
                
                // Update NPC memory about the purchase
                npc.Memory.AddMemory($"I bought a {goal.ItemType} for {goal.EstimatedCost} gold", 
                    "purchase", DateTime.Now);
            }
        }
    }
    
    /// <summary>
    /// Determine if NPC should shop - Pascal shopping conditions
    /// </summary>
    private bool ShouldNPCShop(NPC npc)
    {
        // Pascal conditions for NPC shopping
        if (npc.Gold < 100) return false; // Need minimum gold
        if (npc.HP < npc.MaxHP * 0.3f) return false; // Too injured
        if (npc.IsInConversation) return false; // Busy
        
        // Check if in a location with shops
        var shoppingLocations = new[] { "main_street", "weapon_shop", "armor_shop", "magic_shop" };
        if (!shoppingLocations.Contains(npc.CurrentLocation)) return false;
        
        // Personality-based shopping frequency
        var profile = GetOrCreateShoppingProfile(npc);
        var timeSinceLastShopping = DateTime.Now - profile.LastShoppingAttempt;
        
        var shoppingFrequency = npc.Personality.Greed * 24f; // Hours between shopping attempts
        return timeSinceLastShopping.TotalHours >= shoppingFrequency;
    }
    
    /// <summary>
    /// Pascal equipment evaluation - Ok_To_Buy function logic
    /// </summary>
    private List<ShoppingGoal> DetermineShoppingGoals(NPC npc, NPCShoppingProfile profile, ShopPreferences preferences)
    {
        var goals = new List<ShoppingGoal>();
        
        // Check each equipment slot
        if (ShouldUpgradeWeapon(npc, preferences))
        {
            goals.Add(new ShoppingGoal
            {
                ItemType = ObjType.Weapon,
                MinValue = CalculateMinAcceptableValue(npc.WeaponPower),
                MaxCost = CalculateMaxAffordableCost(npc.Gold, 0.3f),
                Priority = GetWeaponPriority(npc)
            });
        }
        
        if (ShouldUpgradeArmor(npc, preferences))
        {
            goals.Add(new ShoppingGoal
            {
                ItemType = ObjType.Abody,
                MinValue = CalculateMinAcceptableValue(npc.ArmorClass),
                MaxCost = CalculateMaxAffordableCost(npc.Gold, 0.4f),
                Priority = GetArmorPriority(npc)
            });
        }
        
        // Pascal healing potion logic
        if (ShouldBuyHealingPotions(npc))
        {
            goals.Add(new ShoppingGoal
            {
                ItemType = ObjType.Potion,
                MinValue = npc.Level * 20, // Healing amount
                MaxCost = npc.Level * 50,  // Level-based pricing
                Priority = GetHealingPriority(npc)
            });
        }
        
        return goals.OrderByDescending(g => g.Priority).ToList();
    }
    
    #endregion
    
    #region Advanced Gang Management
    
    /// <summary>
    /// Enhanced gang management beyond basic Pascal logic
    /// </summary>
    private async Task ProcessAdvancedGangManagement(List<NPC> npcs)
    {
        // Gang formation for lone NPCs
        await ProcessGangFormation(npcs);
        
        // Gang rivalry development
        await ProcessGangRivalries(npcs);
        
        // Gang territory management
        await ProcessGangTerritories(npcs);
        
        // Gang loyalty and betrayal
        await ProcessGangLoyalty(npcs);
    }
    
    /// <summary>
    /// Gang formation for compatible NPCs
    /// </summary>
    private async Task ProcessGangFormation(List<NPC> npcs)
    {
        var loneNPCs = npcs.Where(n => string.IsNullOrEmpty(n.Team) && 
                                      n.Personality.IsLikelyToJoinGang() &&
                                      n.Level >= 5).ToList();
        
        foreach (var potential in loneNPCs)
        {
            if (random.Next(20) == 0) // 5% chance per cycle
            {
                await AttemptGangFormation(potential, loneNPCs);
            }
        }
    }
    
    /// <summary>
    /// Gang rivalry system
    /// </summary>
    private async Task ProcessGangRivalries(List<NPC> npcs)
    {
        var gangs = npcs.Where(n => !string.IsNullOrEmpty(n.Team))
                       .GroupBy(n => n.Team)
                       .Where(g => g.Count() >= 2)
                       .ToList();
        
        for (int i = 0; i < gangs.Count - 1; i++)
        {
            for (int j = i + 1; j < gangs.Count; j++)
            {
                await CheckGangRivalry(gangs[i].Key, gangs[j].Key, npcs);
            }
        }
    }
    
    #endregion
    
    #region Enhanced Believer System
    
    /// <summary>
    /// Enhanced believer system beyond Pascal NPC_Believer
    /// </summary>
    private async Task ProcessEnhancedBelieverSystem(List<NPC> npcs)
    {
        foreach (var npc in npcs.Where(n => n.IsAlive))
        {
            await ProcessNPCFaithJourney(npc);
        }
    }
    
    /// <summary>
    /// Individual NPC faith development
    /// </summary>
    private async Task ProcessNPCFaithJourney(NPC npc)
    {
        // Faith based on personality and experiences
        var faithInclination = CalculateFaithInclination(npc);
        
        if (string.IsNullOrEmpty(npc.God))
        {
            // Potential conversion
            if (random.NextDouble() < faithInclination / 100.0)
            {
                await AttemptFaithConversion(npc);
            }
        }
        else
        {
            // Existing believer actions
            await ProcessBelieverActions(npc);
        }
    }
    
    #endregion
    
    #region Equipment Maintenance
    
    /// <summary>
    /// Advanced equipment maintenance and upgrading
    /// </summary>
    private async Task ProcessNPCEquipmentMaintenance(List<NPC> npcs)
    {
        foreach (var npc in npcs.Where(n => n.IsAlive))
        {
            await ProcessIndividualEquipmentMaintenance(npc);
        }
    }
    
    /// <summary>
    /// Individual NPC equipment optimization
    /// </summary>
    private async Task ProcessIndividualEquipmentMaintenance(NPC npc)
    {
        // Re-inventory all items (Pascal reinventory logic)
        await behaviorSystem.CheckNPCInventory(npc, 0, ObjType.Head, false, 0);
        
        // Smart equipment swapping
        await OptimizeNPCEquipment(npc);
        
        // Repair damaged equipment
        await RepairNPCEquipment(npc);
    }
    
    #endregion
    
    #region Relationship Maintenance
    
    /// <summary>
    /// Advanced relationship maintenance
    /// </summary>
    private async Task ProcessNPCRelationshipMaintenance(List<NPC> npcs)
    {
        // Marriage system
        await behaviorSystem.ProcessNPCMarriageSystem(npcs.Cast<Character>().ToList());
        
        // Child management
        await behaviorSystem.ProcessChildManagement(npcs.Cast<Character>().ToList());
        
        // Friendship development
        await ProcessNPCFriendships(npcs);
        
        // Enemy relationship tracking
        await ProcessNPCEnemies(npcs);
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Initialize shop preferences by class
    /// </summary>
    private void InitializeShopPreferences()
    {
        classShopPreferences[CharacterClass.Fighter] = new ShopPreferences
        {
            WeaponPriority = 0.9f,
            ArmorPriority = 0.8f,
            PotionPriority = 0.6f,
            PreferredWeaponTypes = new[] { ObjType.Weapon },
            PreferredArmorTypes = new[] { ObjType.Abody, ObjType.Head }
        };
        
        classShopPreferences[CharacterClass.Magician] = new ShopPreferences
        {
            WeaponPriority = 0.4f,
            ArmorPriority = 0.6f,
            PotionPriority = 0.9f,
            PreferredWeaponTypes = new[] { ObjType.Weapon },
            PreferredArmorTypes = new[] { ObjType.Head, ObjType.Neck }
        };
        
        // Add more class preferences...
    }
    
    /// <summary>
    /// Get or create shopping profile for NPC
    /// </summary>
    private NPCShoppingProfile GetOrCreateShoppingProfile(NPC npc)
    {
        if (!shoppingProfiles.ContainsKey(npc.Name2))
        {
            shoppingProfiles[npc.Name2] = new NPCShoppingProfile
            {
                NPCName = npc.Name2,
                ShoppingStyle = DetermineShoppingStyle(npc.Personality),
                LastShoppingAttempt = DateTime.MinValue,
                PurchaseHistory = new List<PurchaseRecord>()
            };
        }
        return shoppingProfiles[npc.Name2];
    }
    
    /// <summary>
    /// Determine shopping style based on personality
    /// </summary>
    private ShoppingStyle DetermineShoppingStyle(PersonalityProfile personality)
    {
        if (personality.Greed > 0.8f) return ShoppingStyle.Aggressive;
        if (personality.Impulsiveness > 0.7f) return ShoppingStyle.Impulsive;
        if (personality.Greed < 0.3f) return ShoppingStyle.Conservative;
        return ShoppingStyle.Balanced;
    }
    
    /// <summary>
    /// Get shop preferences for character class
    /// </summary>
    private ShopPreferences GetShopPreferences(CharacterClass charClass)
    {
        return classShopPreferences.GetValueOrDefault(charClass, 
            new ShopPreferences { WeaponPriority = 0.5f, ArmorPriority = 0.5f, PotionPriority = 0.5f });
    }
    
    // Placeholder implementation methods
    private bool ShouldUpgradeWeapon(NPC npc, ShopPreferences prefs) { return npc.Gold > 500 && random.Next(10) == 0; }
    private bool ShouldUpgradeArmor(NPC npc, ShopPreferences prefs) { return npc.Gold > 400 && random.Next(12) == 0; }
    private bool ShouldBuyHealingPotions(NPC npc) { return npc.HP < npc.MaxHP * 0.7f && npc.Gold > 100; }
    private int CalculateMinAcceptableValue(int currentValue) { return (int)(currentValue * 1.2f); }
    private long CalculateMaxAffordableCost(long gold, float percentage) { return (long)(gold * percentage); }
    private float GetWeaponPriority(NPC npc) { return npc.Personality.Aggression * 0.8f + 0.2f; }
    private float GetArmorPriority(NPC npc) { return (1.0f - npc.Personality.Courage) * 0.6f + 0.4f; }
    private float GetHealingPriority(NPC npc) { return (1.0f - (float)npc.HP / npc.MaxHP) * 0.9f; }
    private async Task<bool> AttemptPurchase(NPC npc, ShoppingGoal goal) { return random.Next(3) == 0; }
    private async Task AttemptGangFormation(NPC leader, List<NPC> candidates) { }
    private async Task CheckGangRivalry(string gang1, string gang2, List<NPC> npcs) { }
    private float CalculateFaithInclination(NPC npc) { return npc.Personality.Sociability * 50f; }
    private async Task AttemptFaithConversion(NPC npc) { }
    private async Task ProcessBelieverActions(NPC npc) { }
    private async Task OptimizeNPCEquipment(NPC npc) { }
    private async Task RepairNPCEquipment(NPC npc) { }
    private async Task ProcessNPCFriendships(List<NPC> npcs) { }
    private async Task ProcessNPCEnemies(List<NPC> npcs) { }
    
    #endregion
    
    #region Data Structures
    
    public class NPCShoppingProfile
    {
        public string NPCName { get; set; }
        public ShoppingStyle ShoppingStyle { get; set; }
        public DateTime LastShoppingAttempt { get; set; }
        public DateTime LastSuccessfulPurchase { get; set; }
        public List<PurchaseRecord> PurchaseHistory { get; set; } = new();
        public Dictionary<ObjType, float> ItemTypePreferences { get; set; } = new();
    }
    
    public class ShopPreferences
    {
        public float WeaponPriority { get; set; }
        public float ArmorPriority { get; set; }
        public float PotionPriority { get; set; }
        public ObjType[] PreferredWeaponTypes { get; set; } = Array.Empty<ObjType>();
        public ObjType[] PreferredArmorTypes { get; set; } = Array.Empty<ObjType>();
    }
    
    public class ShoppingGoal
    {
        public ObjType ItemType { get; set; }
        public int MinValue { get; set; }
        public long MaxCost { get; set; }
        public long EstimatedCost { get; set; }
        public float Priority { get; set; }
    }
    
    public class PurchaseRecord
    {
        public ObjType ItemType { get; set; }
        public long Cost { get; set; }
        public DateTime Timestamp { get; set; }
        public string Location { get; set; }
    }
    
    public enum ShoppingStyle
    {
        Conservative,
        Balanced,
        Aggressive,
        Impulsive
    }
    
    #endregion
} 
