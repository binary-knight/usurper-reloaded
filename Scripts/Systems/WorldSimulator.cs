using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class WorldSimulator
{
    private List<NPC> npcs;
    private bool isRunning = false;
    private Random random = new Random();

    private const float SIMULATION_INTERVAL = 60.0f; // seconds between simulation steps
    private const int MAX_TEAM_SIZE = 5; // Maximum members per team (from Pascal)

    // Team name generators for NPC-formed teams
    private static readonly string[] TeamNamePrefixes = new[]
    {
        "The", "Dark", "Iron", "Shadow", "Blood", "Storm", "Night", "Steel",
        "Crimson", "Black", "Silver", "Golden", "Savage", "Wild", "Grim"
    };

    private static readonly string[] TeamNameSuffixes = new[]
    {
        "Warriors", "Raiders", "Blades", "Wolves", "Dragons", "Vipers", "Hawks",
        "Reapers", "Knights", "Legion", "Fang", "Claws", "Guard", "Syndicate", "Brotherhood"
    };

    // Location names that match actual game locations
    private static readonly string[] GameLocations = new[]
    {
        "Main Street", "Dungeon", "Weapon Shop", "Armor Shop", "Magic Shop",
        "Healer", "Inn", "Temple", "Gym", "Tavern", "Market", "Castle"
    };
    
    public void StartSimulation(List<NPC> worldNPCs)
    {
        npcs = worldNPCs;
        isRunning = true;
        
        // Start a background task to periodically run simulation steps. This works even when
        // running head-less outside the Godot scene tree.
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            GD.Print($"[WorldSim] Background world simulation running – {npcs.Count} NPCs");
            while (isRunning)
            {
                try
                {
                    SimulateStep();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[WorldSim] Simulation error: {ex.Message}");
                }
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(SIMULATION_INTERVAL));
            }
        });
    }
    
    public void StopSimulation()
    {
        isRunning = false;
        GD.Print("[WorldSim] Simulation stopped");
    }
    
    public void SimulateStep()
    {
        if (!isRunning || npcs == null) return;

        var worldState = new WorldState(npcs);

        // Process each NPC's AI
        foreach (var npc in npcs.Where(n => n.IsAlive && n.Brain != null))
        {
            try
            {
                var action = npc.Brain.DecideNextAction(worldState);
                ExecuteNPCAction(npc, action, worldState);

                // NPCs have a chance to do additional activities each tick
                ProcessNPCActivities(npc, worldState);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[WorldSim] Error processing NPC {npc.Name}: {ex.Message}");
            }
        }

        // Process world events
        ProcessWorldEvents();

        // Update relationships and social dynamics
        UpdateSocialDynamics();
    }

    /// <summary>
    /// Process additional NPC activities like dungeon runs, shopping, training
    /// </summary>
    private void ProcessNPCActivities(NPC npc, WorldState world)
    {
        // 15% chance per tick to do something interesting
        if (random.NextDouble() > 0.15) return;

        // Weight activities based on NPC state
        var activities = new List<(string action, double weight)>();

        // Dungeon exploration - if HP is good and level appropriate
        if (npc.HP > npc.MaxHP * 0.7 && npc.Level >= 1)
        {
            activities.Add(("dungeon", 0.25));
        }

        // Shopping - if has gold
        if (npc.Gold > 100)
        {
            activities.Add(("shop", 0.20));
        }

        // Training at gym
        if (npc.Gold > 50)
        {
            activities.Add(("train", 0.15));
        }

        // Visit level master if eligible
        long expForNextLevel = GetExperienceForLevel(npc.Level + 1);
        if (npc.Experience >= expForNextLevel && npc.Level < 100)
        {
            activities.Add(("levelup", 0.30));
        }

        // Heal if wounded
        if (npc.HP < npc.MaxHP * 0.5)
        {
            activities.Add(("heal", 0.35));
        }

        // Socialize/move around
        activities.Add(("move", 0.10));

        // Team activities
        if (string.IsNullOrEmpty(npc.Team))
        {
            // Not in a team - consider joining or forming one
            if (npc.Brain?.Personality?.IsLikelyToJoinGang() == true)
            {
                activities.Add(("team_recruit", 0.15)); // Try to form/join a team
            }
        }
        else
        {
            // In a team - team activities
            if (npc.HP > npc.MaxHP * 0.6)
            {
                activities.Add(("team_dungeon", 0.20)); // Team dungeon run
            }
            activities.Add(("team_recruit", 0.10)); // Recruit more members
        }

        if (activities.Count == 0) return;

        // Weighted random selection
        double totalWeight = activities.Sum(a => a.weight);
        double roll = random.NextDouble() * totalWeight;
        double cumulative = 0;

        string selectedAction = "move";
        foreach (var (action, weight) in activities)
        {
            cumulative += weight;
            if (roll <= cumulative)
            {
                selectedAction = action;
                break;
            }
        }

        switch (selectedAction)
        {
            case "dungeon":
                NPCExploreDungeon(npc);
                break;
            case "shop":
                NPCGoShopping(npc);
                break;
            case "train":
                NPCTrainAtGym(npc);
                break;
            case "levelup":
                NPCVisitMaster(npc);
                break;
            case "heal":
                NPCVisitHealer(npc);
                break;
            case "move":
                MoveNPCToRandomLocation(npc);
                break;
            case "team_recruit":
                NPCTeamRecruitment(npc);
                break;
            case "team_dungeon":
                NPCTeamDungeonRun(npc);
                break;
        }
    }

    /// <summary>
    /// NPC attempts to form or join a team, or recruit members
    /// </summary>
    private void NPCTeamRecruitment(NPC npc)
    {
        if (string.IsNullOrEmpty(npc.Team))
        {
            // Try to join an existing team or form a new one
            NPCTryJoinOrFormTeam(npc);
        }
        else
        {
            // Already in a team - try to recruit others
            NPCTryRecruitForTeam(npc);
        }
    }

    /// <summary>
    /// NPC tries to join an existing team or form a new one
    /// </summary>
    private void NPCTryJoinOrFormTeam(NPC npc)
    {
        // Look for existing teams at this location to join
        var teamsAtLocation = npcs
            .Where(n => n.IsAlive && !string.IsNullOrEmpty(n.Team) && n.CurrentLocation == npc.CurrentLocation)
            .GroupBy(n => n.Team)
            .Where(g => g.Count() < MAX_TEAM_SIZE)
            .ToList();

        if (teamsAtLocation.Any() && random.NextDouble() < 0.6)
        {
            // Try to join an existing team
            var teamGroup = teamsAtLocation[random.Next(teamsAtLocation.Count)];
            var teamLeader = teamGroup.FirstOrDefault();
            if (teamLeader != null)
            {
                // Check compatibility with team leader
                var compatibility = npc.Brain?.Personality?.GetCompatibility(teamLeader.Brain?.Personality) ?? 0.5f;
                if (compatibility > 0.4f)
                {
                    // Join the team!
                    npc.Team = teamLeader.Team;
                    npc.TeamPW = teamLeader.TeamPW;
                    npc.CTurf = teamLeader.CTurf;

                    NewsSystem.Instance.Newsy(true, $"{npc.Name} joined the team '{npc.Team}'!");
                    GD.Print($"[WorldSim] {npc.Name} joined team '{npc.Team}'");
                    return;
                }
            }
        }

        // Form a new team if we have compatible NPCs nearby
        var nearbyUnteamed = npcs
            .Where(n => n.IsAlive &&
                   string.IsNullOrEmpty(n.Team) &&
                   n.CurrentLocation == npc.CurrentLocation &&
                   n.Id != npc.Id &&
                   n.Brain?.Personality?.IsLikelyToJoinGang() == true)
            .ToList();

        if (nearbyUnteamed.Count >= 1 && random.NextDouble() < 0.3)
        {
            // Form a new team
            var teamName = GenerateTeamName();
            var teamPassword = Guid.NewGuid().ToString().Substring(0, 8);

            npc.Team = teamName;
            npc.TeamPW = teamPassword;
            npc.CTurf = false;
            npc.TeamRec = 0;

            // Add first recruit
            var recruit = nearbyUnteamed[random.Next(nearbyUnteamed.Count)];
            var compatibility = npc.Brain?.Personality?.GetCompatibility(recruit.Brain?.Personality) ?? 0.5f;
            if (compatibility > 0.35f)
            {
                recruit.Team = teamName;
                recruit.TeamPW = teamPassword;
                recruit.CTurf = false;

                NewsSystem.Instance.Newsy(true, $"{npc.Name} formed a new team called '{teamName}' with {recruit.Name}!");
                GD.Print($"[WorldSim] {npc.Name} formed team '{teamName}' with {recruit.Name}");
            }
        }
    }

    /// <summary>
    /// NPC tries to recruit others into their team
    /// </summary>
    private void NPCTryRecruitForTeam(NPC npc)
    {
        if (string.IsNullOrEmpty(npc.Team)) return;

        // Check current team size
        var teamSize = npcs.Count(n => n.Team == npc.Team && n.IsAlive);
        if (teamSize >= MAX_TEAM_SIZE) return;

        // Find candidates at this location
        var candidates = npcs
            .Where(n => n.IsAlive &&
                   string.IsNullOrEmpty(n.Team) &&
                   n.CurrentLocation == npc.CurrentLocation &&
                   n.Id != npc.Id)
            .ToList();

        if (candidates.Count == 0) return;

        var candidate = candidates[random.Next(candidates.Count)];
        var compatibility = npc.Brain?.Personality?.GetCompatibility(candidate.Brain?.Personality) ?? 0.5f;

        // Base recruitment chance on compatibility, charisma, and candidate's gang-joining tendency
        float recruitChance = compatibility * 0.5f;
        recruitChance += (npc.Charisma / 100f) * 0.2f;
        if (candidate.Brain?.Personality?.IsLikelyToJoinGang() == true)
        {
            recruitChance += 0.2f;
        }

        if (random.NextDouble() < recruitChance)
        {
            candidate.Team = npc.Team;
            candidate.TeamPW = npc.TeamPW;
            candidate.CTurf = npc.CTurf;

            if (random.NextDouble() < 0.3) // 30% chance to announce
            {
                NewsSystem.Instance.Newsy(true, $"{npc.Name} recruited {candidate.Name} into '{npc.Team}'!");
            }
            GD.Print($"[WorldSim] {npc.Name} recruited {candidate.Name} into team '{npc.Team}'");
        }
    }

    /// <summary>
    /// Generate a random team name
    /// </summary>
    private string GenerateTeamName()
    {
        var prefix = TeamNamePrefixes[random.Next(TeamNamePrefixes.Length)];
        var suffix = TeamNameSuffixes[random.Next(TeamNameSuffixes.Length)];
        return $"{prefix} {suffix}";
    }

    /// <summary>
    /// NPC does a team dungeon run with teammates
    /// </summary>
    private void NPCTeamDungeonRun(NPC npc)
    {
        if (string.IsNullOrEmpty(npc.Team)) return;

        // Get all alive team members
        var teamMembers = npcs
            .Where(n => n.Team == npc.Team && n.IsAlive && n.HP > n.MaxHP * 0.5)
            .ToList();

        if (teamMembers.Count < 2)
        {
            // Not enough healthy teammates, do solo dungeon run
            NPCExploreDungeon(npc);
            return;
        }

        // Move team to dungeon
        foreach (var member in teamMembers)
        {
            member.UpdateLocation("Dungeon");
        }

        // Determine dungeon level based on average team level
        int avgLevel = (int)teamMembers.Average(m => m.Level);
        int dungeonLevel = Math.Max(1, avgLevel + random.Next(-2, 4));
        dungeonLevel = Math.Min(dungeonLevel, 100);

        // Generate monster group (teams fight groups of monsters)
        int monsterCount = Math.Min(teamMembers.Count, random.Next(2, 5));
        var monsters = new List<Monster>();
        for (int i = 0; i < monsterCount; i++)
        {
            monsters.Add(MonsterGenerator.GenerateMonster(dungeonLevel));
        }

        // Team combat simulation
        bool teamWon = SimulateTeamVsMonsterCombat(teamMembers, monsters, out long totalExp, out long totalGold);

        if (teamWon)
        {
            // Distribute rewards evenly among surviving team members
            var survivors = teamMembers.Where(m => m.IsAlive).ToList();
            if (survivors.Count > 0)
            {
                long expShare = totalExp / survivors.Count;
                long goldShare = totalGold / survivors.Count;

                // Bonus for team play
                expShare = (long)(expShare * 1.15); // 15% team XP bonus

                foreach (var member in survivors)
                {
                    member.GainExperience(expShare);
                    member.GainGold(goldShare);
                }

                // Generate news for notable victories
                if (random.NextDouble() < 0.15 || monsters.Any(m => m.IsBoss))
                {
                    NewsSystem.Instance.Newsy(true, $"Team '{npc.Team}' conquered dungeon level {dungeonLevel}, defeating {monsterCount} monsters!");
                }

                GD.Print($"[WorldSim] Team '{npc.Team}' won! {survivors.Count} survivors shared {totalExp} XP and {totalGold} gold");
            }
        }
        else
        {
            // Team lost - check for deaths
            var dead = teamMembers.Where(m => !m.IsAlive).ToList();
            if (dead.Any())
            {
                foreach (var deadMember in dead)
                {
                    NewsSystem.Instance.WriteDeathNews(deadMember.Name, monsters.First().Name, "the Dungeon");
                }
                GD.Print($"[WorldSim] Team '{npc.Team}' was defeated! {dead.Count} members died");
            }

            // Survivors flee
            foreach (var survivor in teamMembers.Where(m => m.IsAlive))
            {
                survivor.UpdateLocation("Main Street");
            }
        }
    }

    /// <summary>
    /// Simulate team vs monster group combat
    /// </summary>
    private bool SimulateTeamVsMonsterCombat(List<NPC> team, List<Monster> monsters, out long totalExp, out long totalGold)
    {
        totalExp = 0;
        totalGold = 0;
        int rounds = 0;
        const int maxRounds = 25;

        while (team.Any(m => m.IsAlive) && monsters.Any(m => m.IsAlive) && rounds < maxRounds)
        {
            rounds++;

            // Team attacks monsters
            foreach (var member in team.Where(m => m.IsAlive))
            {
                var target = monsters.Where(m => m.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault();
                if (target == null) break;

                // Attack calculation with team coordination bonus
                long damage = Math.Max(1, member.Strength + member.WeapPow - target.Defence);
                damage += random.Next(1, (int)Math.Max(2, member.WeapPow / 3));
                damage = (long)(damage * 1.1); // 10% team coordination bonus

                target.HP -= damage;

                if (!target.IsAlive)
                {
                    totalExp += target.GetExperienceReward();
                    totalGold += target.GetGoldReward();
                }
            }

            // Monsters attack team
            foreach (var monster in monsters.Where(m => m.IsAlive))
            {
                var target = team.Where(m => m.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault();
                if (target == null) break;

                // Monster attack - slightly reduced against teams (they help each other)
                long damage = Math.Max(1, monster.Strength + monster.WeapPow - target.Defence - target.ArmPow);
                damage += random.Next(1, (int)Math.Max(2, monster.WeapPow / 3));
                damage = (long)(damage * 0.85); // 15% damage reduction due to team support

                target.TakeDamage(damage);
            }
        }

        return team.Any(m => m.IsAlive) && !monsters.Any(m => m.IsAlive);
    }

    /// <summary>
    /// NPC explores the dungeon and fights monsters
    /// </summary>
    private void NPCExploreDungeon(NPC npc)
    {
        npc.UpdateLocation("Dungeon");

        // Determine dungeon level based on NPC level (can go slightly above their level)
        int dungeonLevel = Math.Max(1, npc.Level + random.Next(-2, 3));
        dungeonLevel = Math.Min(dungeonLevel, 100);

        // Generate a monster
        var monster = MonsterGenerator.GenerateMonster(dungeonLevel);

        // Simulate combat
        int rounds = 0;
        bool npcWon = false;

        while (npc.IsAlive && monster.IsAlive && rounds < 20)
        {
            rounds++;

            // NPC attacks
            long npcDamage = Math.Max(1, npc.Strength + npc.WeapPow - monster.Defence);
            npcDamage += random.Next(1, (int)Math.Max(1, npc.WeapPow / 2));
            monster.HP -= npcDamage;

            if (!monster.IsAlive)
            {
                npcWon = true;
                break;
            }

            // Monster attacks
            long monsterDamage = Math.Max(1, monster.Strength + monster.WeapPow - npc.Defence);
            monsterDamage += random.Next(1, (int)Math.Max(1, monster.WeapPow / 2));
            npc.TakeDamage(monsterDamage);
        }

        if (npcWon)
        {
            // NPC wins - gain XP and gold
            long expGain = monster.GetExperienceReward();
            long goldGain = monster.GetGoldReward();

            npc.GainExperience(expGain);
            npc.GainGold(goldGain);

            // Generate news for notable victories
            if (monster.IsBoss || monster.Level >= npc.Level + 5 || random.NextDouble() < 0.1)
            {
                string newsMsg = monster.IsBoss
                    ? $"{npc.Name} defeated the mighty {monster.Name} in the dungeon depths!"
                    : $"{npc.Name} slew a {monster.Name} (Lv{monster.Level}) and earned {goldGain} gold.";
                NewsSystem.Instance.Newsy(true, newsMsg);
            }

            GD.Print($"[WorldSim] {npc.Name} defeated {monster.Name}, gained {expGain} XP and {goldGain} gold");
        }
        else if (!npc.IsAlive)
        {
            // NPC died - generate death news
            NewsSystem.Instance.WriteDeathNews(npc.Name, monster.Name, "the Dungeon");
            GD.Print($"[WorldSim] {npc.Name} was slain by {monster.Name} in the dungeon!");
        }
        else
        {
            // Fled or timeout
            npc.UpdateLocation("Main Street");
            GD.Print($"[WorldSim] {npc.Name} fled from {monster.Name}");
        }
    }

    /// <summary>
    /// NPC goes shopping for equipment
    /// </summary>
    private void NPCGoShopping(NPC npc)
    {
        // Decide what to buy based on current gear
        bool boughtSomething = false;
        string itemBought = "";

        // Try to upgrade weapon (50% of the time)
        if (random.NextDouble() < 0.5)
        {
            npc.UpdateLocation("Weapon Shop");
            int weaponCost = (int)(npc.Level * 50 + random.Next(50, 200));
            if (npc.Gold >= weaponCost)
            {
                npc.SpendGold(weaponCost);
                int powerGain = random.Next(2, 6);
                npc.WeapPow += powerGain;
                boughtSomething = true;
                itemBought = $"a new weapon (+{powerGain} power)";
            }
        }
        else
        {
            // Try to upgrade armor
            npc.UpdateLocation("Armor Shop");
            int armorCost = (int)(npc.Level * 40 + random.Next(40, 180));
            if (npc.Gold >= armorCost)
            {
                npc.SpendGold(armorCost);
                int defenseGain = random.Next(2, 5);
                npc.ArmPow += defenseGain;
                boughtSomething = true;
                itemBought = $"new armor (+{defenseGain} defense)";
            }
        }

        if (boughtSomething && random.NextDouble() < 0.15)
        {
            NewsSystem.Instance.Newsy(true, $"{npc.Name} purchased {itemBought} from the shop.");
        }

        GD.Print($"[WorldSim] {npc.Name} went shopping" + (boughtSomething ? $" and bought {itemBought}" : " but couldn't afford anything"));
    }

    /// <summary>
    /// NPC trains at the gym
    /// </summary>
    private void NPCTrainAtGym(NPC npc)
    {
        npc.UpdateLocation("Gym");

        int trainingCost = npc.Level * 10 + 50;
        if (npc.Gold >= trainingCost)
        {
            npc.SpendGold(trainingCost);

            // Random stat increase
            int statChoice = random.Next(4);
            string statName = "";
            switch (statChoice)
            {
                case 0:
                    npc.Strength++;
                    statName = "Strength";
                    break;
                case 1:
                    npc.Defence++;
                    statName = "Defence";
                    break;
                case 2:
                    npc.Agility++;
                    statName = "Agility";
                    break;
                case 3:
                    npc.MaxHP += 5;
                    npc.HP = Math.Min(npc.HP + 5, npc.MaxHP);
                    statName = "Vitality";
                    break;
            }

            GD.Print($"[WorldSim] {npc.Name} trained at the Gym and gained {statName}");

            // Occasionally newsworthy
            if (random.NextDouble() < 0.05)
            {
                NewsSystem.Instance.Newsy(true, $"{npc.Name} has been training hard at the Gym!");
            }
        }
    }

    /// <summary>
    /// NPC visits their master to level up
    /// </summary>
    private void NPCVisitMaster(NPC npc)
    {
        long expNeeded = GetExperienceForLevel(npc.Level + 1);
        if (npc.Experience >= expNeeded && npc.Level < 100)
        {
            npc.Level++;

            // Stat gains on level up
            npc.MaxHP += 10 + random.Next(5, 15);
            npc.HP = npc.MaxHP;
            npc.Strength += random.Next(1, 3);
            npc.Defence += random.Next(1, 2);

            // This is always newsworthy!
            NewsSystem.Instance.Newsy(true, $"{npc.Name} has achieved Level {npc.Level}!");
            GD.Print($"[WorldSim] {npc.Name} leveled up to {npc.Level}!");
        }
    }

    /// <summary>
    /// NPC visits the healer
    /// </summary>
    private void NPCVisitHealer(NPC npc)
    {
        npc.UpdateLocation("Healer");

        long healCost = (npc.MaxHP - npc.HP) * 2;
        if (npc.Gold >= healCost && healCost > 0)
        {
            npc.SpendGold(healCost);
            npc.HP = npc.MaxHP;
            GD.Print($"[WorldSim] {npc.Name} healed at the Healer for {healCost} gold");
        }
        else if (npc.HP < npc.MaxHP)
        {
            // Partial heal
            long canAfford = npc.Gold / 2;
            long hpToHeal = canAfford / 2;
            if (hpToHeal > 0)
            {
                npc.SpendGold(canAfford);
                npc.HP = Math.Min(npc.HP + hpToHeal, npc.MaxHP);
                GD.Print($"[WorldSim] {npc.Name} partially healed at the Healer");
            }
        }
    }

    /// <summary>
    /// Calculate XP needed for a level (matches player formula)
    /// </summary>
    private static long GetExperienceForLevel(int level)
    {
        if (level <= 1) return 0;
        long exp = 0;
        for (int i = 2; i <= level; i++)
        {
            exp += (long)(Math.Pow(i, 1.8) * 50);
        }
        return exp;
    }
    
    private void ExecuteNPCAction(NPC npc, NPCAction action, WorldState world)
    {
        switch (action.Type)
        {
            case ActionType.Idle:
                // NPC does nothing this turn
                break;
                
            case ActionType.Explore:
                MoveNPCToRandomLocation(npc);
                break;
                
            case ActionType.Trade:
                ExecuteTrade(npc, action.Target, world);
                break;
                
            case ActionType.Socialize:
                ExecuteSocialize(npc, action.Target, world);
                break;
                
            case ActionType.Attack:
                ExecuteAttack(npc, action.Target, world);
                break;
                
            case ActionType.Rest:
                ExecuteRest(npc);
                break;
                
            case ActionType.Train:
                ExecuteTrain(npc);
                break;
                
            case ActionType.JoinGang:
                ExecuteJoinGang(npc, action.Target, world);
                break;
                
            case ActionType.SeekRevenge:
                ExecuteSeekRevenge(npc, action.Target, world);
                break;
        }
    }
    
    private void MoveNPCToRandomLocation(NPC npc)
    {
        var newLocation = GameLocations[random.Next(GameLocations.Length)];

        if (newLocation != npc.CurrentLocation)
        {
            npc.UpdateLocation(newLocation);
            GD.Print($"[WorldSim] {npc.Name} moved to {newLocation}");
        }
    }
    
    private void ExecuteTrade(NPC npc, string targetId, WorldState world)
    {
        if (string.IsNullOrEmpty(targetId)) return;
        
        var target = world.GetNPCById(targetId);
        if (target == null || target.CurrentLocation != npc.CurrentLocation) return;
        
        // Simple trade simulation
        var tradeAmount = GD.RandRange(10, 100);
        if (npc.CanAfford(tradeAmount) && target.CanAfford(tradeAmount))
        {
            // Exchange some gold (simplified)
            npc.SpendGold(tradeAmount / 2);
            target.GainGold(tradeAmount / 2);
            
            // Record the interaction
            npc.Brain?.RecordInteraction(target, InteractionType.Traded);
            target.Brain?.RecordInteraction(npc, InteractionType.Traded);
            
            GD.Print($"[WorldSim] {npc.Name} traded with {target.Name}");
        }
    }
    
    private void ExecuteSocialize(NPC npc, string targetId, WorldState world)
    {
        if (string.IsNullOrEmpty(targetId)) return;
        
        var target = world.GetNPCById(targetId);
        if (target == null || target.CurrentLocation != npc.CurrentLocation) return;
        
        // Check compatibility for relationship building
        var compatibility = npc.Brain.Personality.GetCompatibility(target.Brain?.Personality);
        
        if (compatibility > 0.6f)
        {
            // Positive interaction
            npc.Brain?.RecordInteraction(target, InteractionType.Complimented);
            target.Brain?.RecordInteraction(npc, InteractionType.Complimented);
            
            // Chance to become friends or allies
            if (GD.Randf() < compatibility * 0.5f)
            {
                npc.AddRelationship(target.Id, 0);
                target.AddRelationship(npc.Id, 0);
                GD.Print($"[WorldSim] {npc.Name} and {target.Name} became friends");
            }
        }
        else if (compatibility < 0.3f)
        {
            // Negative interaction
            npc.Brain?.RecordInteraction(target, InteractionType.Insulted);
            target.Brain?.RecordInteraction(npc, InteractionType.Insulted);
            
            GD.Print($"[WorldSim] {npc.Name} had a negative interaction with {target.Name}");
        }
    }
    
    private void ExecuteAttack(NPC npc, string targetId, WorldState world)
    {
        if (string.IsNullOrEmpty(targetId)) return;
        
        var target = world.GetNPCById(targetId);
        if (target == null || target.CurrentLocation != npc.CurrentLocation || !target.IsAlive) return;
        
        // Simple combat simulation
        var attackerPower = npc.GetAttackPower() + GD.RandRange(1, 10);
        var defenderAC = target.GetArmorClass();
        
        if (attackerPower > defenderAC)
        {
            var damage = Math.Max(1, attackerPower - defenderAC);
            target.TakeDamage(damage);
            
            // Record the attack
            npc.Brain?.RecordInteraction(target, InteractionType.Attacked);
            target.Brain?.RecordInteraction(npc, InteractionType.Attacked);
            
            // Update relationships
            npc.AddRelationship(target.Id, 0);
            target.AddRelationship(npc.Id, 0);
            
            if (!target.IsAlive)
            {
                target.SetState(NPCState.Dead);
                npc.Brain?.Memory?.RecordEvent(new MemoryEvent
                {
                    Type = MemoryType.SawDeath,
                    InvolvedCharacter = target.Id,
                    Description = $"Killed {target.Name} in combat",
                    Importance = 0.9f,
                    Location = npc.CurrentLocation
                });

                // Generate news about the killing!
                NewsSystem.Instance.WriteDeathNews(target.Name, npc.Name, npc.CurrentLocation ?? "unknown");

                GD.Print($"[WorldSim] {npc.Name} killed {target.Name}!");
            }
            else
            {
                GD.Print($"[WorldSim] {npc.Name} attacked {target.Name} for {damage} damage");
            }
        }
        else
        {
            GD.Print($"[WorldSim] {npc.Name} attacked {target.Name} but missed");
        }
    }
    
    private void ExecuteRest(NPC npc)
    {
        var healAmount = npc.MaxHP / 4;
        npc.Heal(healAmount);
        npc.ChangeActivity(Activity.Resting, "Recovering health");
        
        GD.Print($"[WorldSim] {npc.Name} rested and healed {healAmount} HP");
    }
    
    private void ExecuteTrain(NPC npc)
    {
        // Small chance to gain experience from training
        if (GD.Randf() < 0.3f)
        {
            var expGain = GD.RandRange(10, 30);
            npc.GainExperience(expGain);
            GD.Print($"[WorldSim] {npc.Name} trained and gained {expGain} experience");
        }
        
        npc.ChangeActivity(Activity.Working, "Training and improving skills");
    }
    
    private void ExecuteJoinGang(NPC npc, string targetId, WorldState world)
    {
        if (string.IsNullOrEmpty(targetId) || npc.GangId != null) return;
        
        var gangLeader = world.GetNPCById(targetId);
        if (gangLeader == null || gangLeader.CurrentLocation != npc.CurrentLocation) return;
        
        // Check if gang leader accepts the NPC
        var compatibility = npc.Brain.Personality.GetCompatibility(gangLeader.Brain?.Personality);
        
        if (compatibility > 0.5f && gangLeader.GangMembers.Count < 6) // Max gang size
        {
            npc.GangId = gangLeader.Id;
            gangLeader.GangMembers.Add(npc.Id);
            
            npc.AddRelationship(gangLeader.Id, 0);
            gangLeader.AddRelationship(npc.Id, 0);
            
            npc.Brain?.Memory?.RecordEvent(new MemoryEvent
            {
                Type = MemoryType.JoinedGang,
                InvolvedCharacter = gangLeader.Id,
                Description = $"Joined {gangLeader.Name}'s gang",
                Importance = 0.8f,
                Location = npc.CurrentLocation
            });
            
            GD.Print($"[WorldSim] {npc.Name} joined {gangLeader.Name}'s gang");
        }
    }
    
    private void ExecuteSeekRevenge(NPC npc, string targetId, WorldState world)
    {
        if (string.IsNullOrEmpty(targetId)) return;
        
        var target = world.GetNPCById(targetId);
        if (target != null && target.CurrentLocation == npc.CurrentLocation)
        {
            // If target is found, attack them
            ExecuteAttack(npc, targetId, world);
        }
        else
        {
            // Move around looking for the target
            MoveNPCToRandomLocation(npc);
            npc.ChangeActivity(Activity.Hunting, $"Seeking revenge against {target?.Name ?? "enemy"}");
        }
    }
    
    private void ProcessWorldEvents()
    {
        // Random world events that can affect NPCs
        if (GD.Randf() < 0.05f) // 5% chance per simulation step
        {
            GenerateRandomEvent();
        }
    }
    
    private void GenerateRandomEvent()
    {
        var events = new[]
        {
            "A merchant caravan arrives in town",
            "Strange noises are heard from the dungeon",
            "The king makes a royal decree",
            "A festival begins in the town square",
            "Bandits are spotted near the roads",
            "A mysterious stranger appears",
            "The weather turns stormy",
            "A new shop opens in the market"
        };
        
        var randomEvent = events[GD.RandRange(0, events.Length - 1)];
        GD.Print($"[WorldSim] World Event: {randomEvent}");
        
        // Record event in all NPC memories with low importance
        foreach (var npc in npcs.Where(n => n.IsAlive && n.Brain != null))
        {
            npc.Brain.Memory?.RecordEvent(new MemoryEvent
            {
                Type = MemoryType.WitnessedEvent,
                Description = $"Witnessed: {randomEvent}",
                Importance = 0.2f,
                Location = npc.CurrentLocation
            });
        }
    }
    
    private void UpdateSocialDynamics()
    {
        // Check for gang betrayals
        CheckGangBetrayals();

        // Check for new gang formations
        CheckGangFormations();

        // Process rival relationships
        ProcessRivalries();

        // Process team dynamics
        ProcessTeamDynamics();
    }

    /// <summary>
    /// Process team-related dynamics: betrayals, team wars, turf control
    /// </summary>
    private void ProcessTeamDynamics()
    {
        // Check for team betrayals (members leaving)
        CheckTeamBetrayals();

        // Check for team vs team conflicts
        CheckTeamWars();

        // Update turf control
        UpdateTurfControl();
    }

    /// <summary>
    /// Check for NPCs leaving their teams
    /// </summary>
    private void CheckTeamBetrayals()
    {
        var teamMembers = npcs.Where(n => !string.IsNullOrEmpty(n.Team) && n.IsAlive).ToList();

        foreach (var member in teamMembers)
        {
            // Low loyalty or betrayal-prone personality
            bool likelyToLeave = member.Brain?.Personality?.IsLikelyToBetrray() == true ||
                                 member.Loyalty < 30;

            if (likelyToLeave && random.NextDouble() < 0.01) // 1% chance per tick
            {
                string oldTeam = member.Team;

                // Leave the team
                member.Team = "";
                member.TeamPW = "";
                member.CTurf = false;
                member.TeamRec = 0;

                NewsSystem.Instance.Newsy(true, $"{member.Name} abandoned '{oldTeam}'!");
                GD.Print($"[WorldSim] {member.Name} left team '{oldTeam}'");

                // Check if team is now empty
                var remainingMembers = npcs.Count(n => n.Team == oldTeam && n.IsAlive);
                if (remainingMembers == 0)
                {
                    NewsSystem.Instance.Newsy(true, $"The team '{oldTeam}' has been disbanded!");
                }
            }
        }
    }

    /// <summary>
    /// Check for team vs team conflicts
    /// </summary>
    private void CheckTeamWars()
    {
        // Get all active teams
        var teams = npcs
            .Where(n => !string.IsNullOrEmpty(n.Team) && n.IsAlive)
            .GroupBy(n => n.Team)
            .Where(g => g.Count() >= 2) // Only teams with 2+ members
            .ToList();

        if (teams.Count < 2) return;

        // Small chance for team war
        if (random.NextDouble() > 0.02) return; // 2% chance per tick

        // Pick two random teams at the same location
        var team1Group = teams[random.Next(teams.Count)];
        var team1Location = team1Group.First().CurrentLocation;

        var teamsAtSameLocation = teams
            .Where(t => t.Key != team1Group.Key && t.Any(n => n.CurrentLocation == team1Location))
            .ToList();

        if (!teamsAtSameLocation.Any()) return;

        var team2Group = teamsAtSameLocation[random.Next(teamsAtSameLocation.Count)];

        // Team war!
        var team1 = team1Group.Where(n => n.CurrentLocation == team1Location && n.IsAlive).ToList();
        var team2 = team2Group.Where(n => n.CurrentLocation == team1Location && n.IsAlive).ToList();

        if (team1.Count == 0 || team2.Count == 0) return;

        string team1Name = team1Group.Key;
        string team2Name = team2Group.Key;

        NewsSystem.Instance.Newsy(true, $"Team War! '{team1Name}' clashes with '{team2Name}' at {team1Location}!");
        GD.Print($"[WorldSim] Team war between '{team1Name}' and '{team2Name}'");

        // Simulate team battle
        bool team1Won = SimulateTeamVsTeamCombat(team1, team2);

        if (team1Won)
        {
            NewsSystem.Instance.Newsy(true, $"'{team1Name}' emerged victorious against '{team2Name}'!");
        }
        else
        {
            NewsSystem.Instance.Newsy(true, $"'{team2Name}' emerged victorious against '{team1Name}'!");
        }
    }

    /// <summary>
    /// Simulate team vs team combat
    /// </summary>
    private bool SimulateTeamVsTeamCombat(List<NPC> team1, List<NPC> team2)
    {
        int rounds = 0;
        const int maxRounds = 15;

        while (team1.Any(m => m.IsAlive) && team2.Any(m => m.IsAlive) && rounds < maxRounds)
        {
            rounds++;

            // Team 1 attacks team 2
            foreach (var attacker in team1.Where(m => m.IsAlive))
            {
                var target = team2.Where(m => m.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault();
                if (target == null) break;

                long damage = Math.Max(1, attacker.Strength + attacker.WeapPow - target.Defence - target.ArmPow);
                damage += random.Next(1, (int)Math.Max(2, attacker.WeapPow / 4));
                target.TakeDamage(damage);

                if (!target.IsAlive)
                {
                    NewsSystem.Instance.WriteDeathNews(target.Name, attacker.Name, target.CurrentLocation ?? "battle");
                }
            }

            // Team 2 attacks team 1
            foreach (var attacker in team2.Where(m => m.IsAlive))
            {
                var target = team1.Where(m => m.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault();
                if (target == null) break;

                long damage = Math.Max(1, attacker.Strength + attacker.WeapPow - target.Defence - target.ArmPow);
                damage += random.Next(1, (int)Math.Max(2, attacker.WeapPow / 4));
                target.TakeDamage(damage);

                if (!target.IsAlive)
                {
                    NewsSystem.Instance.WriteDeathNews(target.Name, attacker.Name, target.CurrentLocation ?? "battle");
                }
            }
        }

        // Determine winner by survivors
        int team1Alive = team1.Count(m => m.IsAlive);
        int team2Alive = team2.Count(m => m.IsAlive);

        if (team1Alive > team2Alive) return true;
        if (team2Alive > team1Alive) return false;

        // Tiebreaker: total remaining HP
        long team1HP = team1.Where(m => m.IsAlive).Sum(m => m.HP);
        long team2HP = team2.Where(m => m.IsAlive).Sum(m => m.HP);

        return team1HP >= team2HP;
    }

    /// <summary>
    /// Update turf control - strongest team can claim turf
    /// </summary>
    private void UpdateTurfControl()
    {
        // Get current turf controller
        var currentController = npcs.FirstOrDefault(n => n.CTurf && !string.IsNullOrEmpty(n.Team));

        // Get all teams with their power
        var teams = npcs
            .Where(n => !string.IsNullOrEmpty(n.Team) && n.IsAlive)
            .GroupBy(n => n.Team)
            .Select(g => new
            {
                TeamName = g.Key,
                Power = g.Sum(m => m.Level + m.Strength + m.Defence),
                Members = g.ToList(),
                HasTurf = g.Any(m => m.CTurf)
            })
            .OrderByDescending(t => t.Power)
            .ToList();

        if (teams.Count == 0) return;

        var strongestTeam = teams.First();

        // If no one controls turf and there's a strong team, small chance to claim it
        if (currentController == null && strongestTeam.Power > 100 && random.NextDouble() < 0.005)
        {
            foreach (var member in strongestTeam.Members)
            {
                member.CTurf = true;
                member.TeamRec = 0;
            }

            NewsSystem.Instance.Newsy(true, $"'{strongestTeam.TeamName}' has taken control of the town!");
            GD.Print($"[WorldSim] Team '{strongestTeam.TeamName}' took control of turf");
        }
    }
    
    private void CheckGangBetrayals()
    {
        var gangMembers = npcs.Where(npc => !string.IsNullOrEmpty(npc.GangId)).ToList();
        
        foreach (var member in gangMembers)
        {
            if (member.Brain?.Personality?.IsLikelyToBetrray() == true && GD.Randf() < 0.02f) // 2% chance
            {
                var gangLeader = npcs.FirstOrDefault(npc => npc.Id == member.GangId);
                if (gangLeader != null)
                {
                    // Betray the gang
                    member.GangId = null;
                    gangLeader.GangMembers.Remove(member.Id);
                    
                    member.AddRelationship(gangLeader.Id, 0);
                    gangLeader.AddRelationship(member.Id, 0);
                    
                    member.Brain?.RecordInteraction(gangLeader, InteractionType.Betrayed);
                    gangLeader.Brain?.RecordInteraction(member, InteractionType.Betrayed);
                    
                    GD.Print($"[WorldSim] {member.Name} betrayed {gangLeader.Name}'s gang!");
                }
            }
        }
    }
    
    private void CheckGangFormations()
    {
        var potentialLeaders = npcs.Where(npc => 
            npc.IsAlive && 
            string.IsNullOrEmpty(npc.GangId) && 
            npc.GangMembers.Count == 0 &&
            npc.Brain?.Personality?.IsLikelyToJoinGang() == true &&
            npc.Brain.Personality.Ambition > 0.7f).ToList();
        
        foreach (var leader in potentialLeaders)
        {
            if (GD.Randf() < 0.01f) // 1% chance to form new gang
            {
                // Look for potential gang members in the same location
                var sameLocation = npcs.Where(npc => 
                    npc.CurrentLocation == leader.CurrentLocation &&
                    npc.Id != leader.Id &&
                    string.IsNullOrEmpty(npc.GangId) &&
                    npc.Brain?.Personality?.IsLikelyToJoinGang() == true).ToList();
                
                if (sameLocation.Count >= 2)
                {
                    var newMember = sameLocation[GD.RandRange(0, sameLocation.Count - 1)];
                    newMember.GangId = leader.Id;
                    leader.GangMembers.Add(newMember.Id);
                    
                    GD.Print($"[WorldSim] {leader.Name} formed a new gang with {newMember.Name}");
                }
            }
        }
    }
    
    private void ProcessRivalries()
    {
        // Check for escalating conflicts between enemies
        var enemies = npcs.Where(npc => npc.Enemies.Count > 0).ToList();
        
        foreach (var npc in enemies)
        {
            foreach (var enemyId in npc.Enemies.ToList()) // ToList to avoid modification during iteration
            {
                var enemy = npcs.FirstOrDefault(n => n.Id == enemyId);
                if (enemy?.IsAlive == true && GD.Randf() < 0.05f) // 5% chance
                {
                    // Escalate the rivalry
                    if (npc.CurrentLocation == enemy.CurrentLocation)
                    {
                        GD.Print($"[WorldSim] Rivalry escalates between {npc.Name} and {enemy.Name}");
                        ExecuteAttack(npc, enemyId, new WorldState(npcs));
                    }
                }
            }
        }
    }
    
    public string GetSimulationStatus()
    {
        if (!isRunning) return "Simulation stopped";

        var aliveNPCs = npcs.Count(npc => npc.IsAlive);
        var gangs = npcs.Where(npc => npc.GangMembers.Count > 0).Count();
        var teams = npcs.Where(n => !string.IsNullOrEmpty(n.Team)).Select(n => n.Team).Distinct().Count();
        var turfController = npcs.FirstOrDefault(n => n.CTurf && !string.IsNullOrEmpty(n.Team))?.Team ?? "None";
        var totalRelationships = npcs.Sum(npc => npc.KnownCharacters.Count);

        return $"Active NPCs: {aliveNPCs}, Teams: {teams}, Turf: {turfController}, Gangs: {gangs}, Relationships: {totalRelationships}";
    }

    /// <summary>
    /// Get list of all active teams with their members
    /// </summary>
    public List<TeamInfo> GetActiveTeams()
    {
        if (npcs == null) return new List<TeamInfo>();

        return npcs
            .Where(n => !string.IsNullOrEmpty(n.Team) && n.IsAlive)
            .GroupBy(n => n.Team)
            .Select(g => new TeamInfo
            {
                TeamName = g.Key,
                MemberCount = g.Count(),
                TotalPower = g.Sum(m => m.Level + m.Strength + m.Defence),
                AverageLevel = (int)g.Average(m => m.Level),
                ControlsTurf = g.Any(m => m.CTurf),
                Members = g.Select(m => m.Name).ToList()
            })
            .OrderByDescending(t => t.TotalPower)
            .ToList();
    }

    /// <summary>
    /// Get teammates for a player's team
    /// </summary>
    public List<NPC> GetPlayerTeammates(string teamName)
    {
        if (npcs == null || string.IsNullOrEmpty(teamName))
            return new List<NPC>();

        return npcs.Where(n => n.Team == teamName && n.IsAlive).ToList();
    }
}

/// <summary>
/// Team information for display and queries
/// </summary>
public class TeamInfo
{
    public string TeamName { get; set; } = "";
    public int MemberCount { get; set; }
    public long TotalPower { get; set; }
    public int AverageLevel { get; set; }
    public bool ControlsTurf { get; set; }
    public List<string> Members { get; set; } = new();
} 
