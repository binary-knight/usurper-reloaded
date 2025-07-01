using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class WorldSimulator
{
    private List<NPC> npcs;
    private Godot.Timer? simulationTimer;
    private bool isRunning = false;
    
    private const float SIMULATION_INTERVAL = 60.0f; // seconds between simulation steps
    
    public void StartSimulation(List<NPC> worldNPCs)
    {
        npcs = worldNPCs;
        isRunning = true;
        
        // Create timer for periodic simulation
        simulationTimer = new Godot.Timer();
        simulationTimer.WaitTime = SIMULATION_INTERVAL;
        simulationTimer.TimeoutEvent += OnSimulationStep;
        simulationTimer.Autostart = true;
        
        GD.Print($"[WorldSim] Started simulation with {npcs.Count} NPCs");
    }
    
    public void StopSimulation()
    {
        isRunning = false;
        simulationTimer?.QueueFree();
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
    
    private void OnSimulationStep()
    {
        SimulateStep();
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
        var locations = new[] { "town_square", "tavern", "market", "castle", "temple", "dungeon" };
        var newLocation = locations[GD.RandRange(0, locations.Length - 1)];
        
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
                npc.AddRelationship(target.Id, RelationshipType.Friend);
                target.AddRelationship(npc.Id, RelationshipType.Friend);
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
            npc.AddRelationship(target.Id, RelationshipType.Enemy);
            target.AddRelationship(npc.Id, RelationshipType.Enemy);
            
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
            
            npc.AddRelationship(gangLeader.Id, RelationshipType.CloseFriend);
            gangLeader.AddRelationship(npc.Id, RelationshipType.Friend);
            
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
                    
                    member.AddRelationship(gangLeader.Id, RelationshipType.Enemy);
                    gangLeader.AddRelationship(member.Id, RelationshipType.Enemy);
                    
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
        var totalRelationships = npcs.Sum(npc => npc.KnownCharacters.Count);
        
        return $"Active NPCs: {aliveNPCs}, Gangs: {gangs}, Relationships: {totalRelationships}";
    }
} 
