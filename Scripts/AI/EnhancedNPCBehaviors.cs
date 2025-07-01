using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Enhanced NPC Behaviors - Phase 21 Implementation
/// Pascal-compatible NPC behaviors that extend the existing AI system
/// </summary>
public static class EnhancedNPCBehaviors
{
    private static Random random = new Random();
    
    /// <summary>
    /// Enhanced inventory management - Pascal NPC_CHEC.PAS Check_Inventory
    /// </summary>
    public static int CheckNPCInventory(NPC npc, int itemId = 0, bool shout = false)
    {
        if (GameConfig.ClassicMode) return 0;
        
        var result = 0; // Pascal return codes: 0=not touched, 1=equipped, 2=swapped
        
        if (itemId > 0)
        {
            // NPC examines new item (Pascal shout logic)
            if (shout)
            {
                GD.Print($"{npc.Name2} looks at the new item.");
            }
            
            // Determine if NPC should use this item
            result = ProcessItemDecision(npc, itemId, shout);
        }
        else
        {
            // Reinventory all items (Pascal onr = 0 logic)
            ReinventoryAllItems(npc);
        }
        
        return result;
    }
    
    /// <summary>
    /// NPC shopping AI - Pascal NPCMAINT.PAS Npc_Buy function
    /// </summary>
    public static bool ProcessNPCShopping(NPC npc)
    {
        // Pascal shopping conditions
        if (npc.Gold < 100) return false;
        if (npc.HP < npc.MaxHP * 0.3f) return false; // Too injured to shop
        
        var shoppingGoals = DetermineShoppingNeeds(npc);
        if (shoppingGoals.Count == 0) return false;
        
        foreach (var goal in shoppingGoals)
        {
            if (AttemptPurchase(npc, goal))
            {
                RecordPurchaseInMemory(npc, goal);
                return true; // One purchase per shopping session
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Gang management - Pascal NPCMAINT.PAS gang logic
    /// </summary>
    public static void ProcessGangMaintenance(List<NPC> npcs)
    {
        var gangAnalysis = AnalyzeGangs(npcs);
        
        foreach (var gang in gangAnalysis)
        {
            if (gang.Value.IsNPCOnly && gang.Value.Size <= 3)
            {
                if (random.Next(4) == 0) // 25% chance to dissolve
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
    
    /// <summary>
    /// NPC believer system - Pascal NPCMAINT.PAS NPC_Believer
    /// </summary>
    public static void ProcessBelieverSystem(NPC npc)
    {
        if (GameConfig.NPCBelievers == 0) return;
        if (random.Next(3) != 0) return; // Only 33% processed per cycle
        
        if (string.IsNullOrEmpty(npc.God))
        {
            // Potential conversion based on personality
            var conversionChance = CalculateConversionChance(npc);
            if (random.NextDouble() < conversionChance)
            {
                ConvertNPCToFaith(npc);
            }
        }
        else
        {
            // Existing believer actions
            ProcessBelieverActions(npc);
        }
    }
    
    /// <summary>
    /// Automated gang warfare - Pascal AUTOGANG.PAS Auto_Gangwar
    /// </summary>
    public static GangWarResult ConductAutoGangWar(string gang1, string gang2, List<NPC> npcs)
    {
        var team1 = LoadGangMembers(gang1, npcs);
        var team2 = LoadGangMembers(gang2, npcs);
        
        if (team1.Count == 0 || team2.Count == 0)
        {
            return new GangWarResult { Outcome = "No Contest" };
        }
        
        // Generate news header (Pascal Gang_War_Header)
        var header = GetGangWarHeader();
        GenerateGangWarNews(header, gang1, gang2);
        
        // Conduct battles
        var result = new GangWarResult { Gang1 = gang1, Gang2 = gang2 };
        ConductGangBattles(team1, team2, result);
        
        return result;
    }
    
    /// <summary>
    /// Enhanced relationship processing - Pascal RELATIO2.PAS
    /// </summary>
    public static void ProcessNPCRelationships(NPC npc, List<NPC> allNPCs)
    {
        // Marriage system
        if (string.IsNullOrEmpty(npc.Married) && npc.Level >= 5)
        {
            if (random.Next(50) == 0) // 2% chance per cycle
            {
                AttemptNPCMarriage(npc, allNPCs);
            }
        }
        
        // Friendship development
        ProcessFriendshipDevelopment(npc, allNPCs);
        
        // Enemy relationship tracking
        ProcessEnemyRelationships(npc);
    }
    
    #region Private Helper Methods
    
    private static int ProcessItemDecision(NPC npc, int itemId, bool shout)
    {
        // Simplified item evaluation (Pascal objekt_test logic)
        var currentValue = GetCurrentEquipmentValue(npc);
        var newItemValue = EstimateItemValue(itemId);
        
        if (newItemValue > currentValue * 1.2f) // 20% better
        {
            if (shout)
            {
                GD.Print($"{npc.Name2} starts to use the new item instead.");
            }
            
            // Send mail notification (Pascal Inform_By_Mail)
            SendItemNotificationMail(npc, itemId);
            
            return 2; // Swapped equipment
        }
        else if (newItemValue > currentValue * 1.1f) // 10% better
        {
            if (shout)
            {
                GD.Print($"{npc.Name2} starts to use the new item.");
            }
            
            SendItemNotificationMail(npc, itemId);
            return 1; // Equipped new item
        }
        
        return 0; // No change
    }
    
    private static void ReinventoryAllItems(NPC npc)
    {
        // Pascal reinventory logic - check all items
        npc.Memory?.AddMemory("I reorganized my equipment", "inventory", DateTime.Now);
        
        // Simple optimization
        OptimizeNPCEquipment(npc);
    }
    
    private static List<ShoppingGoal> DetermineShoppingNeeds(NPC npc)
    {
        var goals = new List<ShoppingGoal>();
        
        // Pascal Ok_To_Buy logic based on class
        switch (npc.Class)
        {
            case CharacterClass.Warrior:
                if (npc.WeaponPower < npc.Level * 20)
                    goals.Add(new ShoppingGoal { Type = "weapon", Priority = 0.8f });
                if (npc.ArmorClass < npc.Level * 15)
                    goals.Add(new ShoppingGoal { Type = "armor", Priority = 0.7f });
                break;
                
            case CharacterClass.Magician:
                if (npc.Mana < npc.MaxMana * 0.7f)
                    goals.Add(new ShoppingGoal { Type = "mana_potion", Priority = 0.9f });
                goals.Add(new ShoppingGoal { Type = "magic_item", Priority = 0.6f });
                break;
                
            case CharacterClass.Paladin:
                if (npc.HP < npc.MaxHP * 0.8f)
                    goals.Add(new ShoppingGoal { Type = "healing_potion", Priority = 0.8f });
                break;
        }
        
        return goals.OrderByDescending(g => g.Priority).ToList();
    }
    
    private static bool AttemptPurchase(NPC npc, ShoppingGoal goal)
    {
        var cost = CalculateItemCost(goal.Type, npc.Level);
        
        if (npc.Gold >= cost)
        {
            npc.Gold -= cost;
            return true;
        }
        
        return false;
    }
    
    private static void RecordPurchaseInMemory(NPC npc, ShoppingGoal goal)
    {
        npc.Memory?.AddMemory($"I bought a {goal.Type}", "purchase", DateTime.Now);
    }
    
    private static Dictionary<string, GangInfo> AnalyzeGangs(List<NPC> npcs)
    {
        return npcs.Where(n => !string.IsNullOrEmpty(n.Team))
                  .GroupBy(n => n.Team)
                  .ToDictionary(g => g.Key, g => new GangInfo
                  {
                      Size = g.Count(),
                      IsNPCOnly = g.All(n => n.AI == CharacterAI.Computer)
                  });
    }
    
    private static void DissolveGang(string gangName, List<NPC> npcs)
    {
        // Pascal Remove_Gang procedure
        GD.Print($"Removing NPC team: {gangName}");
        
        foreach (var member in npcs.Where(n => n.Team == gangName))
        {
            member.Team = "";
            member.ControlsTurf = false;
            member.TeamPassword = "";
            member.GymOwner = 0;
        }
        
        // Generate news
        var newsSystem = GetNode<NewsSystem>("/root/NewsSystem");
        newsSystem?.Newsy($"{GameConfig.TeamColor}{gangName}{GameConfig.NewsColorDefault} ceased to exist!", false, GameConfig.NewsCategory.General);
    }
    
    private static void RecruitGangMembers(string gangName, List<NPC> npcs)
    {
        var availableNPCs = npcs.Where(n => 
            string.IsNullOrEmpty(n.Team) && 
            !n.King && 
            n.IsAlive).Take(3).ToList();
        
        foreach (var candidate in availableNPCs)
        {
            if (random.Next(3) == 0) // 33% recruitment chance
            {
                candidate.Team = gangName;
                
                // Copy team settings from existing member
                var existingMember = npcs.FirstOrDefault(n => n.Team == gangName);
                if (existingMember != null)
                {
                    candidate.TeamPassword = existingMember.TeamPassword;
                    candidate.ControlsTurf = existingMember.ControlsTurf;
                }
                
                // Generate news
                var newsSystem = GetNode<NewsSystem>("/root/NewsSystem");
                newsSystem?.Newsy($"{GameConfig.NewsColorPlayer}{candidate.Name2}{GameConfig.NewsColorDefault} has been recruited to {GameConfig.TeamColor}{gangName}{GameConfig.NewsColorDefault}", true, GameConfig.NewsCategory.General);
            }
        }
    }
    
    private static double CalculateConversionChance(NPC npc)
    {
        // Base chance modified by personality
        var baseChance = 0.05; // 5%
        
        // Personality modifiers
        if (npc.Personality.Sociability > 0.7f)
            baseChance *= 1.5;
            
        if (npc.Personality.Ambition > 0.8f)
            baseChance *= 0.7; // Ambitious NPCs less likely to convert
            
        return baseChance;
    }
    
    private static void ConvertNPCToFaith(NPC npc)
    {
        var availableGods = new[] { "Nosferatu", "Darkcloak", "Druid", "Seth Able" };
        npc.God = availableGods[random.Next(availableGods.Length)];
        
        npc.Memory?.AddMemory($"I found faith in {npc.God}", "faith", DateTime.Now);
        npc.EmotionalState?.AdjustMood("spiritual", 0.3f);
        
        GD.Print($"[Faith] {npc.Name2} converted to {npc.God}");
    }
    
    private static void ProcessBelieverActions(NPC npc)
    {
        var actions = new[] { "pray", "make offering", "seek guidance", "preach" };
        var action = actions[random.Next(actions.Length)];
        
        npc.Memory?.AddMemory($"I {action} to {npc.God}", "faith", DateTime.Now);
        
        // Faith actions can affect mood and goals
        npc.EmotionalState?.AdjustMood("spiritual", 0.1f);
        
        if (random.Next(10) == 0) // 10% chance to add faith-based goal
        {
            npc.Goals?.AddGoal(new Goal($"Serve {npc.God}", GoalType.Social, 0.6f));
        }
    }
    
    private static List<NPC> LoadGangMembers(string gangName, List<NPC> npcs)
    {
        return npcs.Where(n => n.Team == gangName && n.IsAlive).ToList();
    }
    
    private static string GetGangWarHeader()
    {
        var headers = new[] { "Gang War!", "Team Bash!", "Team War!", "Turf War!", "Gang Fight!", "Rival Gangs Clash!" };
        return headers[random.Next(headers.Length)];
    }
    
    private static void GenerateGangWarNews(string header, string gang1, string gang2)
    {
        var newsSystem = GetNode<NewsSystem>("/root/NewsSystem");
        newsSystem?.Newsy(false, header,
            $"{GameConfig.TeamColor}{gang1}{GameConfig.NewsColorDefault} challenged {GameConfig.TeamColor}{gang2}{GameConfig.NewsColorDefault}");
    }
    
    private static void ConductGangBattles(List<NPC> team1, List<NPC> team2, GangWarResult result)
    {
        // Pascal computer vs computer battles
        for (int i = 0; i < Math.Min(team1.Count, team2.Count); i++)
        {
            var fighter1 = team1[i];
            var fighter2 = team2[i];
            
            var battle = ConductSingleBattle(fighter1, fighter2);
            result.Battles.Add(battle);
        }
        
        // Determine overall winner
        var team1Wins = result.Battles.Count(b => b.Winner == 1);
        var team2Wins = result.Battles.Count(b => b.Winner == 2);
        
        result.Outcome = team1Wins > team2Wins ? $"{result.Gang1} Victory" : 
                        team2Wins > team1Wins ? $"{result.Gang2} Victory" : "Draw";
    }
    
    private static BattleResult ConductSingleBattle(NPC fighter1, NPC fighter2)
    {
        // Simplified battle logic
        var power1 = fighter1.Level + fighter1.WeaponPower + random.Next(20);
        var power2 = fighter2.Level + fighter2.WeaponPower + random.Next(20);
        
        var winner = power1 > power2 ? 1 : 2;
        var loser = winner == 1 ? fighter2 : fighter1;
        
        // Reduce loser HP
        loser.HP = Math.Max(1, loser.HP - random.Next(20, 50));
        
        return new BattleResult
        {
            Fighter1 = fighter1.Name2,
            Fighter2 = fighter2.Name2,
            Winner = winner,
            Rounds = random.Next(1, 5)
        };
    }
    
    // Additional helper methods
    private static int GetCurrentEquipmentValue(NPC npc) => npc.WeaponPower + npc.ArmorClass;
    private static int EstimateItemValue(int itemId) => itemId * 10; // Placeholder
    private static void SendItemNotificationMail(NPC npc, int itemId) { /* Mail implementation */ }
    private static void OptimizeNPCEquipment(NPC npc) { /* Equipment optimization */ }
    private static int CalculateItemCost(string itemType, int level) => level * 50 + random.Next(25, 100);
    private static void AttemptNPCMarriage(NPC npc, List<NPC> candidates) { /* Marriage logic */ }
    private static void ProcessFriendshipDevelopment(NPC npc, List<NPC> others) { /* Friendship logic */ }
    private static void ProcessEnemyRelationships(NPC npc) { /* Enemy tracking */ }
    
    private static T GetNode<T>(string path) where T : Node
    {
        return Engine.GetMainLoop().GetNode<T>(path);
    }
    
    #endregion
    
    #region Data Classes
    
    public class ShoppingGoal
    {
        public string Type { get; set; }
        public float Priority { get; set; }
    }
    
    public class GangInfo
    {
        public int Size { get; set; }
        public bool IsNPCOnly { get; set; }
    }
    
    public class GangWarResult
    {
        public string Gang1 { get; set; }
        public string Gang2 { get; set; }
        public string Outcome { get; set; }
        public List<BattleResult> Battles { get; set; } = new();
    }
    
    public class BattleResult
    {
        public string Fighter1 { get; set; }
        public string Fighter2 { get; set; }
        public int Winner { get; set; } // 1 or 2
        public int Rounds { get; set; }
    }
    
    #endregion
} 
