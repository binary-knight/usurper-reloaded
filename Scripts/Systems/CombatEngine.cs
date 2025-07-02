using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

/// <summary>
/// Combat Engine - Pascal-compatible combat system
/// Based on PLVSMON.PAS, MURDER.PAS, VARIOUS.PAS, and PLCOMP.PAS
/// </summary>
public partial class CombatEngine
{
    private TerminalEmulator terminal;
    private Random random = new Random();
    
    // Combat state
    private bool globalPlayerInFight = false;
    private bool globalKilled = false;
    private bool globalBegged = false;
    private bool globalEscape = false;
    private bool globalNoBeg = false;
    
    public CombatEngine(TerminalEmulator term = null)
    {
        terminal = term;
    }
    
    /// <summary>
    /// Player vs Monster combat - main entry point
    /// Based on Player_vs_Monsters procedure from PLVSMON.PAS
    /// </summary>
    public async Task<CombatResult> PlayerVsMonster(Character player, Monster monster, List<Character> teammates = null)
    {
        // Reset temporary flags per battle
        player.IsRaging = false;

        var result = new CombatResult
        {
            Player = player,
            Monster = monster,
            Teammates = teammates ?? new List<Character>(),
            CombatLog = new List<string>()
        };
        
        // Initialize combat state
        globalPlayerInFight = true;
        globalKilled = false;
        globalBegged = false;
        globalEscape = false;
        
        // Surprise / ambush round before normal combat loop
        long playerInit = player.Dexterity + random.Next(1, 21);
        long monsterInit = monster.IQ + random.Next(1, 21);
        if (playerInit - monsterInit >= 10)
        {
            terminal.WriteLine("You catch the enemy off guard! Surprise round!", "yellow");
            await ExecuteAttack(player, monster, new CombatResult());
            if (monster.HP <= 0) // early kill
            {
                var earlyResult = new CombatResult { Player = player, Monster = monster, Outcome = CombatOutcome.Victory };
                await HandleVictory(earlyResult);
                return earlyResult;
            }
        }
        else if (monsterInit - playerInit >= 10)
        {
            terminal.WriteLine($"{monster.Name} ambushes you!", "red");
            await ProcessMonsterAction(monster, player, new CombatResult());
            if (player.HP <= 0)
            {
                var earlyResult = new CombatResult { Player = player, Monster = monster, Outcome = CombatOutcome.PlayerDied };
                await HandlePlayerDeath(earlyResult);
                return earlyResult;
            }
        }
        
        // Combat introduction
        await ShowCombatIntroduction(player, monster, result);
        
        // Main combat loop
        while (player.IsAlive && monster.IsAlive && !globalEscape && !globalKilled)
        {
            // Simple initiative roll: Dexterity + d20 vs monster IQ + d20 (as proxy)
            int playerInitiative = (int)player.Dexterity + random.Next(1, 21);
            int monsterInitiative = monster.IQ + random.Next(1, 21);

            bool playerActsFirst = playerInitiative >= monsterInitiative;

            if (!playerActsFirst && monster.IsAlive && !globalEscape)
            {
                await ProcessMonsterAction(monster, player, result);
                if (!player.IsAlive || globalEscape || globalKilled)
                    continue; // Player died or escaped before his turn
            }

            // Player's turn
            if (player.IsAlive && monster.IsAlive)
            {
                var playerAction = await GetPlayerAction(player, monster, result);
                await ProcessPlayerAction(playerAction, player, monster, result);
            }
            
            // Teammates' turns (remain after player regardless)
            foreach (var teammate in result.Teammates)
            {
                if (teammate.IsAlive && monster.IsAlive)
                {
                    await ProcessTeammateAction(teammate, monster, result);
                }
            }
            
            if (playerActsFirst && monster.IsAlive && !globalEscape)
            {
                await ProcessMonsterAction(monster, player, result);
            }
            
            // Check for combat end conditions
            if (!player.IsAlive || !monster.IsAlive || globalEscape || globalKilled)
                break;
        }
        
        // Determine combat outcome
        await DetermineCombatOutcome(result);
        
        globalPlayerInFight = false;
        return result;
    }
    
    /// <summary>
    /// Player vs Player combat
    /// Based on PLVSPLC.PAS and MURDER.PAS
    /// </summary>
    public async Task<CombatResult> PlayerVsPlayer(Character attacker, Character defender)
    {
        attacker.IsRaging = false;
        defender.IsRaging = false;

        var result = new CombatResult
        {
            Player = attacker,
            Opponent = defender,
            CombatLog = new List<string>()
        };
        
        // PvP combat introduction
        await ShowPvPIntroduction(attacker, defender, result);
        
        // Main PvP combat loop
        bool toDeath = false;
        
        while (attacker.IsAlive && defender.IsAlive && !globalEscape)
        {
            // Attacker's turn
            if (attacker.IsAlive && defender.IsAlive)
            {
                var attackerAction = await GetPlayerAction(attacker, null, result, defender);
                await ProcessPlayerVsPlayerAction(attackerAction, attacker, defender, result);
            }
            
            // Defender's turn (if AI controlled)
            if (defender.IsAlive && defender.AI == CharacterAI.Computer)
            {
                await ProcessComputerPlayerAction(defender, attacker, result);
            }
            
            // Check for combat end conditions
            if (!attacker.IsAlive || !defender.IsAlive || globalEscape)
                break;
        }
        
        await DeterminePvPOutcome(result);
        return result;
    }
    
    /// <summary>
    /// Show combat introduction - Pascal style
    /// </summary>
    private async Task ShowCombatIntroduction(Character player, Monster monster, CombatResult result)
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_red");
        terminal.WriteLine("═══ COMBAT ═══");
        terminal.WriteLine("");
        
        // Monster appearance
        if (!string.IsNullOrEmpty(monster.Phrase) && monster.CanSpeak)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"The {monster.Name} says: \"{monster.Phrase}\"");
            terminal.WriteLine("");
        }
        
        terminal.SetColor("white");
        terminal.WriteLine($"You are facing: {monster.GetDisplayInfo()}");
        
        if (result.Teammates.Count > 0)
        {
            terminal.WriteLine("Fighting alongside you:");
            foreach (var teammate in result.Teammates)
            {
                if (teammate.IsAlive)
                {
                    terminal.WriteLine($"  • {teammate.DisplayName} (Level {teammate.Level})");
                }
            }
        }
        
        terminal.WriteLine("");
        await Task.Delay(2000);
        
        result.CombatLog.Add($"Combat begins against {monster.Name}!");
    }
    
    /// <summary>
    /// Get player action - Pascal-compatible menu
    /// Based on shared_menu from PLCOMP.PAS
    /// </summary>
    private async Task<CombatAction> GetPlayerAction(Character player, Monster monster, CombatResult result, Character pvpOpponent = null)
    {
        // Apply status ticks before player chooses action
        player.ProcessStatusEffects();

        terminal.SetColor("cyan");
        terminal.WriteLine($"Your HP: {player.HP}/{player.MaxHP}");
        
        if (monster != null)
        {
            terminal.WriteLine($"{monster.Name} HP: {monster.HP}");
        }
        else if (pvpOpponent != null)
        {
            terminal.WriteLine($"{pvpOpponent.DisplayName} HP: {pvpOpponent.HP}/{pvpOpponent.MaxHP}");
        }
        
        // Show currently active status effects on the player
        if (player.ActiveStatuses.Count > 0 || player.IsRaging)
        {
            var list = new List<string>();
            foreach (var kv in player.ActiveStatuses)
            {
                var label = kv.Key.ToString();
                if (kv.Value > 0)
                    label += $"({kv.Value})";
                list.Add(label);
            }

            // Rage is tracked with a separate flag but is also a status effect for display purposes
            if (player.IsRaging && !list.Any(s => s.StartsWith("Raging")))
                list.Add("Raging");

            terminal.SetColor("yellow");
            terminal.WriteLine($"Status: {string.Join(", ", list)}");
        }
        
        terminal.WriteLine("");
        
        // If stunned, skip turn
        if (player.HasStatus(StatusEffect.Stunned))
        {
            terminal.SetColor("gray");
            terminal.WriteLine("You are stunned and cannot act this round!");
            await Task.Delay(800);
            return new CombatAction { Type = CombatActionType.Status };
        }
        
        // Combat menu - exact Pascal layout
        terminal.SetColor("white");
        terminal.WriteLine("(A)ttack  (V)olley  (D)efend  (H)eal  (Q)uick Heal  (F)ight to Death");
        terminal.WriteLine("(S)tatus   (B)eg for Mercy   (U)se Item");
        terminal.WriteLine("(P)ower Attack   (E) Precise Strike");
        terminal.WriteLine("(I) Disarm   (T) Taunt   (L) Hide");
        
        if (player.Class == CharacterClass.Cleric || player.Class == CharacterClass.Magician || player.Class == CharacterClass.Sage)
        {
            terminal.WriteLine("(C)ast Spell");
        }
        
        if (player.Class == CharacterClass.Paladin)
        {
            terminal.WriteLine("(1) Soul Strike");
        }
        
        if (player.Class == CharacterClass.Assassin)
        {
            terminal.WriteLine("(1) Backstab");
        }
        
        if (monster != null)
        {
            terminal.WriteLine("(R)etreat");
        }
        
        // Class-specific options
        if (player.Class == CharacterClass.Barbarian)
        {
            if (!player.IsRaging)
                terminal.WriteLine("(G) Rage (2x damage, -4 AC)");
            else
                terminal.WriteLine("(G) [Raging]");
        }

        if (player.Class == CharacterClass.Paladin)
        {
            if (player.SmiteChargesRemaining == 0)
            {
                // Initialise charges from modifiers on first display
                var mods = player.GetClassCombatModifiers();
                player.SmiteChargesRemaining = mods.SmiteCharges;
            }

            if (player.SmiteChargesRemaining > 0)
            {
                terminal.WriteLine($"(2) Smite Evil ({player.SmiteChargesRemaining} left)");
            }
        }
        
        terminal.WriteLine("");
        
        var choice = await terminal.GetInput("Your choice: ");
        
        return ParseCombatAction(choice.ToUpper().Trim(), player);
    }
    
    /// <summary>
    /// Parse combat action from input
    /// </summary>
    private CombatAction ParseCombatAction(string choice, Character player)
    {
        return choice switch
        {
            "A" => new CombatAction { Type = CombatActionType.Attack },
            "V" => new CombatAction { Type = CombatActionType.RangedAttack },
            "D" => new CombatAction { Type = CombatActionType.Defend },
            "H" => new CombatAction { Type = CombatActionType.Heal },
            "Q" => new CombatAction { Type = CombatActionType.QuickHeal },
            "F" => new CombatAction { Type = CombatActionType.FightToDeath },
            "S" => new CombatAction { Type = CombatActionType.Status },
            "B" => new CombatAction { Type = CombatActionType.BegForMercy },
            "U" => new CombatAction { Type = CombatActionType.UseItem },
            "P" => new CombatAction { Type = CombatActionType.PowerAttack },
            "E" => new CombatAction { Type = CombatActionType.PreciseStrike },
            "C" => new CombatAction { Type = CombatActionType.CastSpell },
            "1" when player.Class == CharacterClass.Paladin => new CombatAction { Type = CombatActionType.SoulStrike },
            "1" when player.Class == CharacterClass.Assassin => new CombatAction { Type = CombatActionType.Backstab },
            "R" => new CombatAction { Type = CombatActionType.Retreat },
            "G" when player.Class == CharacterClass.Barbarian && !player.IsRaging => new CombatAction { Type = CombatActionType.Rage },
            "2" when player.Class == CharacterClass.Paladin => new CombatAction { Type = CombatActionType.Smite },
            "I" => new CombatAction { Type = CombatActionType.Disarm },
            "T" => new CombatAction { Type = CombatActionType.Taunt },
            "L" => new CombatAction { Type = CombatActionType.Hide },
            _ => new CombatAction { Type = CombatActionType.Attack } // Default to attack
        };
    }
    
    /// <summary>
    /// Process player action - Pascal combat mechanics
    /// </summary>
    private async Task ProcessPlayerAction(CombatAction action, Character player, Monster monster, CombatResult result)
    {
        player.UsedItem = false;
        player.Casted = false;
        
        switch (action.Type)
        {
            case CombatActionType.Attack:
                await ExecuteAttack(player, monster, result);
                break;
                
            case CombatActionType.Defend:
                await ExecuteDefend(player, result);
                break;
                
            case CombatActionType.Heal:
                await ExecuteHeal(player, result, false);
                break;
                
            case CombatActionType.QuickHeal:
                await ExecuteHeal(player, result, true);
                break;
                
            case CombatActionType.FightToDeath:
                await ExecuteFightToDeath(player, monster, result);
                break;
                
            case CombatActionType.Status:
                await ShowCombatStatus(player, result);
                break;
                
            case CombatActionType.BegForMercy:
                await ExecuteBegForMercy(player, monster, result);
                break;
                
            case CombatActionType.UseItem:
                await ExecuteUseItem(player, result);
                break;
                
            case CombatActionType.CastSpell:
                await ExecuteCastSpell(player, monster, result);
                break;
                
            case CombatActionType.SoulStrike:
                await ExecuteSoulStrike(player, monster, result);
                break;
                
            case CombatActionType.Backstab:
                await ExecuteBackstab(player, monster, result);
                break;
                
            case CombatActionType.PowerAttack:
                await ExecutePowerAttack(player, monster, result);
                break;
                
            case CombatActionType.PreciseStrike:
                await ExecutePreciseStrike(player, monster, result);
                break;
                
            case CombatActionType.Retreat:
                await ExecuteRetreat(player, monster, result);
                break;
                
            case CombatActionType.Rage:
                await ExecuteRage(player, result);
                break;
                
            case CombatActionType.Smite:
                await ExecuteSmite(player, monster, result);
                break;
                
            case CombatActionType.Disarm:
                await ExecuteDisarm(player, monster, result);
                break;
                
            case CombatActionType.Taunt:
                await ExecuteTaunt(player, monster, result);
                break;
                
            case CombatActionType.Hide:
                await ExecuteHide(player, result);
                break;
                
            case CombatActionType.RangedAttack:
                await ExecuteRangedAttack(player, monster, result);
                break;
        }
    }
    
    /// <summary>
    /// Execute attack - Pascal normal_attack calculation
    /// Based on normal_attack function from VARIOUS.PAS
    /// </summary>
    private async Task ExecuteAttack(Character attacker, Monster target, CombatResult result)
    {
        int swings = GetAttackCount(attacker);
        for (int s = 0; s < swings && target.HP > 0; s++)
        {
            await ExecuteSingleAttack(attacker, target, result, s > 0);
        }
    }
    
    private async Task ExecuteSingleAttack(Character attacker, Monster target, CombatResult result, bool isExtra)
    {
        long attackPower = attacker.Strength;

        // Apply class/status modifiers
        if (attacker.IsRaging)
            attackPower *= 2; // Rage doubles base strength contribution

        if (attacker.HasStatus(StatusEffect.PowerStance))
            attackPower = (long)(attackPower * 1.5);

        if (attacker.HasStatus(StatusEffect.Blessed))
            attackPower += 2;
        if (attacker.HasStatus(StatusEffect.Weakened))
            attackPower = Math.Max(1, attackPower - 4);
        
        // Add weapon power
        if (attacker.WeapPow > 0)
        {
            attackPower += attacker.WeapPow + random.Next(0, (int)attacker.WeapPow + 1);
        }
        
        // Random attack variation
        attackPower += random.Next(1, 21); // Pascal: random(20) + 1
        
        // Critical hit chance (Pascal: random(20) = 0)
        bool criticalHit = random.Next(20) == 0;
        if (criticalHit)
        {
            attackPower = (long)(attackPower * GameConfig.CriticalHitMultiplier);
            terminal.WriteLine("CRITICAL HIT!", "bright_red");
            await Task.Delay(1000);
        }
        
        // Store punch for display
        attacker.Punch = attackPower;
        
        if (attackPower > 0)
        {
            terminal.SetColor("green");
            terminal.WriteLine($"You hit the {target.Name} for {attackPower} damage!");
            
            // Stoneskin absorption handled later in this method
            
            // Calculate defense absorption (Pascal-compatible)
            long defense = target.Defence + random.Next(0, (int)Math.Max(1, target.Defence / 8));
            
            if (attacker.IsRaging)
            {
                // Rage lowers accuracy: simulate by giving target +4 AC (defence)
                defense += 4;
            }
            
            if (attacker.HasStatus(StatusEffect.PowerStance))
                defense = (long)(defense * 1.25); // less accurate
            
            if (target.ArmPow > 0)
            {
                defense += random.Next(0, (int)target.ArmPow + 1);
            }
            
            long actualDamage = Math.Max(1, attackPower - defense);
            
            if (defense > 0 && defense < attackPower)
            {
                terminal.SetColor("cyan");
                terminal.WriteLine($"{target.Name}'s armor absorbed {defense} points!");
            }
            
            // Apply damage
            target.HP = Math.Max(0, target.HP - actualDamage);

            terminal.SetColor("red");
            terminal.WriteLine($"{target.Name} takes {actualDamage} damage!");

            result.CombatLog.Add($"Player attacks {target.Name} for {actualDamage} damage");
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("You missed your blow!");
            result.CombatLog.Add($"Player misses {target.Name}");
        }
        
        await Task.Delay(1500);
    }
    
    /// <summary>
    /// Execute heal action
    /// </summary>
    private async Task ExecuteHeal(Character player, CombatResult result, bool quick)
    {
        if (player.HP >= player.MaxHP)
        {
            terminal.WriteLine("You are already at full health!", "yellow");
            await Task.Delay(1000);
            return;
        }
        
        long healAmount;
        if (quick)
        {
            healAmount = Math.Min(10, player.MaxHP - player.HP);
            terminal.WriteLine($"You quickly bandage your wounds for {healAmount} HP.", "green");
        }
        else
        {
            healAmount = Math.Min(25, player.MaxHP - player.HP);
            terminal.WriteLine($"You carefully tend your wounds for {healAmount} HP.", "green");
        }
        
        player.HP += healAmount;
        result.CombatLog.Add($"Player heals for {healAmount} HP");
        await Task.Delay(1500);
    }
    
    /// <summary>
    /// Execute backstab (Assassin special ability)
    /// Based on Pascal backstab mechanics
    /// </summary>
    private async Task ExecuteBackstab(Character player, Monster target, CombatResult result)
    {
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("You attempt to backstab!");
        await Task.Delay(1000);
        
        // Backstab calculation (Pascal-compatible)
        long backstabPower = player.Strength + player.WeapPow;
        backstabPower = (long)(backstabPower * GameConfig.BackstabMultiplier); // 3x damage
        
        // Backstab success chance based on dexterity
        // Dexterity is stored as a long – clamp and cast so the RNG upper-bound stays in the valid Int32 range
        int successChance = (int)Math.Min(int.MaxValue, player.Dexterity * 2L);
        if (random.Next(100) < successChance)
        {
            terminal.SetColor("bright_red");
            terminal.WriteLine($"BACKSTAB! You strike from the shadows for {backstabPower} damage!");
            
            target.HP = Math.Max(0, target.HP - backstabPower);
            result.CombatLog.Add($"Player backstabs {target.Name} for {backstabPower} damage");
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("Your backstab attempt fails!");
            result.CombatLog.Add($"Player backstab fails against {target.Name}");
        }
        
        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Execute Soul Strike (Paladin special ability)
    /// Based on Soul_Effect from VARIOUS.PAS
    /// </summary>
    private async Task ExecuteSoulStrike(Character player, Monster target, CombatResult result)
    {
        terminal.SetColor("bright_white");
        terminal.WriteLine("You channel divine power for a Soul Strike!");
        await Task.Delay(1000);
        
        // Soul Strike power based on chivalry and level
        long soulPower = (player.Chivalry / 10) + (player.Level * 5);
        
        if (soulPower > 0)
        {
            terminal.SetColor("bright_yellow");
            terminal.WriteLine($"Divine energy strikes for {soulPower} damage!");
            
            target.HP = Math.Max(0, target.HP - soulPower);
            result.CombatLog.Add($"Player Soul Strike hits {target.Name} for {soulPower} damage");
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("Your soul lacks the purity for this attack!");
            result.CombatLog.Add($"Player Soul Strike fails - insufficient chivalry");
        }
        
        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Execute retreat - Pascal retreat mechanics
    /// Based on Retreat function from PLVSMON.PAS
    /// </summary>
    private async Task ExecuteRetreat(Character player, Monster monster, CombatResult result)
    {
        terminal.SetColor("yellow");
        terminal.WriteLine("You attempt to flee from combat!");
        await Task.Delay(1000);
        
        // 50% chance to escape (Pascal: random(2))
        if (random.Next(2) == 0)
        {
            terminal.SetColor("green");
            terminal.WriteLine("You have escaped battle!");
            globalEscape = true;
            result.Outcome = CombatOutcome.PlayerEscaped;
            result.CombatLog.Add("Player successfully retreated");
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("The monster won't let you escape!");
            
            // Damage for failed escape (Pascal: random(global_dungeonlevel * 10) + 3)
            long escapeDamage = random.Next(10 * 3) + 3; // Simplified dungeon level
            
            terminal.WriteLine($"As you cowardly turn and run, you feel pain when something");
            terminal.WriteLine($"hits you in the back for {escapeDamage} points");
            
            player.HP = Math.Max(0, player.HP - escapeDamage);
            result.CombatLog.Add($"Player retreat fails, takes {escapeDamage} damage");
            
            if (player.HP <= 0)
            {
                terminal.SetColor("red");
                terminal.WriteLine("You have been slain!");
                globalKilled = true;
                result.Outcome = CombatOutcome.PlayerDied;
            }
        }
        
        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Execute beg for mercy
    /// </summary>
    private async Task ExecuteBegForMercy(Character player, Monster monster, CombatResult result)
    {
        if (globalNoBeg)
        {
            terminal.WriteLine("The monster shows no mercy!", "red");
            await Task.Delay(1500);
            return;
        }
        
        terminal.SetColor("yellow");
        terminal.WriteLine("You beg for mercy!");
        
        // Mercy chance based on charisma
        // Charisma is a long – clamp before cast to prevent overflow
        int mercyChance = (int)Math.Min(int.MaxValue, player.Charisma * 2L);
        if (random.Next(100) < mercyChance && !globalBegged)
        {
            terminal.SetColor("green");
            terminal.WriteLine("The monster takes pity on you and lets you live!");
            globalEscape = true;
            globalBegged = true;
            result.Outcome = CombatOutcome.PlayerEscaped;
            result.CombatLog.Add("Player successfully begged for mercy");
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("Your pleas fall on deaf ears!");
            result.CombatLog.Add("Player begging fails");
        }
        
        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Process monster action - Pascal AI
    /// Based on monster behavior from PLVSMON.PAS
    /// </summary>
    private async Task ProcessMonsterAction(Monster monster, Character player, CombatResult result)
    {
        if (!monster.IsAlive) return;
        
        terminal.SetColor("red");
        
        // Tick monster statuses
        if (monster.PoisonRounds > 0)
        {
            int dmg = random.Next(1, 5); // 1d4
            monster.HP = Math.Max(0, monster.HP - dmg);
            monster.PoisonRounds--;
            terminal.WriteLine($"Poison burns {monster.Name} for {dmg} damage!", "dark_green");
            if (monster.PoisonRounds == 0) monster.Poisoned = false;
        }

        if (monster.StunRounds > 0)
        {
            monster.StunRounds--;
            terminal.WriteLine($"{monster.Name} is stunned and cannot act!", "cyan");
            await Task.Delay(600);
            return; // Skip action
        }
        
        // Monster attack calculation (Pascal-compatible)
        long monsterAttack = monster.GetAttackPower();
        
        // Add random variation – scale with monster level so early foes hit lighter
        int variationMax = monster.Level <= 3 ? 6 : 10; // up to +5 for lvl1-3
        monsterAttack += random.Next(0, variationMax);
        
        if (monsterAttack > 0)
        {
            terminal.WriteLine($"The {monster.Name} attacks you for {monsterAttack} damage!");
            
            // Blur / duplicate miss chance (20%)
            if (player.HasStatus(StatusEffect.Blur))
            {
                if (random.Next(100) < 20)
                {
                    terminal.WriteLine($"The {monster.Name} strikes only illusory images!", "gray");
                    result.CombatLog.Add($"{monster.Name} misses due to blur");
                    await Task.Delay(800);
                    return;
                }
            }
            
            // Stoneskin absorption
            // Stoneskin absorption handled later in this method
            
            // Player defense (Pascal-compatible)
            long playerDefense = player.Defence + random.Next(0, (int)Math.Max(1, player.Defence / 8));
            // Add magical AC bonuses from spells (Shield / Prismatic Cage / Fog etc.)
            playerDefense += player.MagicACBonus;
            
            if (player.ArmPow > 0)
            {
                playerDefense += random.Next(0, (int)player.ArmPow + 1);
            }
            
            // Status modifications
            if (player.HasStatus(StatusEffect.Blessed))
                playerDefense += 2;
            if (player.IsRaging)
                playerDefense = Math.Max(0, playerDefense - 4);
            
            long actualDamage = Math.Max(1, monsterAttack - playerDefense);
            
            // Defending halves damage
            if (player.IsDefending)
            {
                actualDamage = (long)Math.Ceiling(actualDamage / 2.0);
            }

            // Apply damage
            player.HP = Math.Max(0, player.HP - actualDamage);

            terminal.SetColor("red");
            if (player.IsDefending)
            {
                terminal.WriteLine($"You brace for impact and only take {actualDamage} damage!", "bright_cyan");
            }
            else
            {
                terminal.WriteLine($"{player.DisplayName} takes {actualDamage} damage!");
            }

            result.CombatLog.Add($"{monster.Name} attacks player for {actualDamage} damage");
        }
        else
        {
            terminal.WriteLine($"The {monster.Name} attacks but misses!");
            result.CombatLog.Add($"{monster.Name} misses player");
        }
        
        // Defend stance expires after the first enemy attack
        if (player.IsDefending)
        {
            player.IsDefending = false;
            if (player.HasStatus(StatusEffect.Defending))
                player.ActiveStatuses.Remove(StatusEffect.Defending);
        }

        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Process teammate action (AI-controlled)
    /// </summary>
    private async Task ProcessTeammateAction(Character teammate, Monster monster, CombatResult result)
    {
        if (!teammate.IsAlive || !monster.IsAlive) return;
        
        // Simple AI: attack if healthy, heal if low HP
        if (teammate.HP < teammate.MaxHP / 3 && teammate.HP < teammate.MaxHP)
        {
            // Heal
            long healAmount = Math.Min(15, teammate.MaxHP - teammate.HP);
            teammate.HP += healAmount;
            terminal.WriteLine($"{teammate.DisplayName} heals for {healAmount} HP.", "green");
            result.CombatLog.Add($"{teammate.DisplayName} heals for {healAmount} HP");
        }
        else
        {
            // Attack
            long attackPower = teammate.Strength + teammate.WeapPow + random.Next(1, 16);
            long defense = monster.GetDefensePower();
            long damage = Math.Max(1, attackPower - defense);
            
            monster.HP = Math.Max(0, monster.HP - damage);
            terminal.WriteLine($"{teammate.DisplayName} attacks {monster.Name} for {damage} damage!", "cyan");
            result.CombatLog.Add($"{teammate.DisplayName} attacks {monster.Name} for {damage} damage");
        }
        
        await Task.Delay(1000);
    }
    
    /// <summary>
    /// Determine combat outcome and apply rewards/penalties
    /// </summary>
    private async Task DetermineCombatOutcome(CombatResult result)
    {
        if (globalEscape)
        {
            result.Outcome = CombatOutcome.PlayerEscaped;
            terminal.WriteLine("You have fled from combat.", "yellow");
        }
        else if (!result.Player.IsAlive)
        {
            result.Outcome = CombatOutcome.PlayerDied;
            await HandlePlayerDeath(result);
        }
        else if (!result.Monster.IsAlive)
        {
            result.Outcome = CombatOutcome.Victory;
            await HandleVictory(result);
        }
        
        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Handle player victory - Pascal rewards
    /// </summary>
    private async Task HandleVictory(CombatResult result)
    {
        terminal.SetColor("bright_green");
        terminal.WriteLine($"You have slain the {result.Monster.Name}!");
        terminal.WriteLine("");
        
        // Calculate rewards (Pascal-compatible)
        long expReward = result.Monster.GetExperienceReward();
        long goldReward = result.Monster.GetGoldReward();
        
        result.Player.Experience += expReward;
        result.Player.Gold += goldReward;
        result.Player.MKills++;
        
        terminal.SetColor("green");
        terminal.WriteLine($"You gain {expReward} experience!");
        terminal.WriteLine($"You find {goldReward} gold!");
        
        // Offer weapon pickup
        if (result.Monster.GrabWeap && !string.IsNullOrEmpty(result.Monster.Weapon))
        {
            terminal.WriteLine($"Do you want to pick up the {result.Monster.Weapon}? (Y/N)", "yellow");
            var input = await terminal.GetInput("> ");
            if (input.Trim().ToUpper().StartsWith("Y"))
            {
                Item lootItem;
                var baseWeapon = ItemManager.GetClassicWeapon((int)result.Monster.WeapNr);
                if (baseWeapon != null)
                {
                    lootItem = new Item
                    {
                        Name = baseWeapon.Name,
                        Type = ObjType.Weapon,
                        Value = baseWeapon.Value,
                        Attack = (int)baseWeapon.Power
                    };
                }
                else
                {
                    lootItem = new Item
                    {
                        Name = result.Monster.Weapon,
                        Type = ObjType.Weapon,
                        Value = 0,
                        Attack = (int)result.Monster.WeapPow
                    };
                }

                result.Player.Inventory.Add(lootItem);
                terminal.WriteLine($"You picked up {lootItem.Name}.", "bright_green");
                result.ItemsFound.Add(lootItem.Name);
            }
        }

        // Offer armor pickup
        if (result.Monster.GrabArm && !string.IsNullOrEmpty(result.Monster.Armor))
        {
            terminal.WriteLine($"Do you want to take the {result.Monster.Armor}? (Y/N)", "yellow");
            var input = await terminal.GetInput("> ");
            if (input.Trim().ToUpper().StartsWith("Y"))
            {
                Item lootItem;
                var baseArmor = ItemManager.GetClassicArmor((int)result.Monster.ArmNr);
                if (baseArmor != null)
                {
                    lootItem = new Item
                    {
                        Name = baseArmor.Name,
                        Type = ObjType.Body,
                        Value = baseArmor.Value,
                        Armor = (int)baseArmor.Power
                    };
                }
                else
                {
                    lootItem = new Item
                    {
                        Name = result.Monster.Armor,
                        Type = ObjType.Body,
                        Value = 0,
                        Armor = (int)result.Monster.ArmPow
                    };
                }

                result.Player.Inventory.Add(lootItem);
                terminal.WriteLine($"You picked up {lootItem.Name}.", "bright_green");
                result.ItemsFound.Add(lootItem.Name);
            }
        }
        
        result.CombatLog.Add($"Victory! Gained {expReward} exp and {goldReward} gold");
    }
    
    /// <summary>
    /// Handle player death
    /// </summary>
    private async Task HandlePlayerDeath(CombatResult result)
    {
        terminal.SetColor("red");
        terminal.WriteLine("You have been slain!");
        terminal.WriteLine("Darkness...");
        
        result.Player.HP = 0;
        result.Player.MDefeats++;
        
        // Handle resurrections (from Pascal)
        if (result.Player.Resurrections > 0)
        {
            terminal.WriteLine($"You have {result.Player.Resurrections} resurrections remaining.", "yellow");
            // TODO: Implement resurrection choice
        }
        
        result.CombatLog.Add($"Player killed by {result.Monster?.Name ?? "opponent"}");
    }
    
    /// <summary>
    /// Show combat status
    /// </summary>
    private async Task ShowCombatStatus(Character player, CombatResult result)
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("Combat Status");
        terminal.WriteLine("=============");
        terminal.WriteLine("");
        
        terminal.SetColor("white");
        terminal.WriteLine($"Name: {player.DisplayName}");
        terminal.WriteLine($"HP: {player.HP}/{player.MaxHP}");
        terminal.WriteLine($"Strength: {player.Strength}");
        terminal.WriteLine($"Defence: {player.Defence}");
        terminal.WriteLine($"Weapon Power: {player.WeapPow}");
        terminal.WriteLine($"Armor Power: {player.ArmPow}");
        
        // Surface active status effects here as well
        if (player.ActiveStatuses.Count > 0 || player.IsRaging)
        {
            var effects = new List<string>();
            foreach (var kv in player.ActiveStatuses)
            {
                effects.Add($"{kv.Key} ({kv.Value})");
            }
            if (player.IsRaging && !effects.Any(e => e.StartsWith("Raging")))
                effects.Add("Raging");

            terminal.WriteLine($"Active Effects: {string.Join(", ", effects)}");
        }
        
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    // Placeholder methods for additional features
    private async Task ExecuteFightToDeath(Character player, Monster monster, CombatResult result)
    {
        terminal.WriteLine("You enter a berserker rage!", "red");
        // TODO: Implement fight to death mechanics
        await Task.Delay(1000);
    }
    
    /// <summary>
    /// Allow the player to consume a usable item (potions/scrolls) during combat.
    /// Heuristic: any inventory item of ObjType.Potion OR Magic with Cure/HP/Mana positive is considered usable.
    /// </summary>
    private async Task ExecuteUseItem(Character player, CombatResult result)
    {
        // Build list of usable items
        var usable = new List<Item>();
        foreach (var itm in player.Inventory)
        {
            if (itm.Type == ObjType.Potion || itm.Type == ObjType.Magic)
            {
                if (itm.HP != 0 || itm.Mana != 0 || itm.Cure != Cures.Nothing || itm.Name.ToLower().Contains("bomb") || itm.Name.ToLower().Contains("scroll"))
                {
                    usable.Add(itm);
                }
            }
        }

        if (usable.Count == 0)
        {
            terminal.WriteLine("You have no usable combat items!", "yellow");
            await Task.Delay(1000);
            return;
        }

        terminal.WriteLine("Select item to use:");
        for (int i = 0; i < usable.Count; i++)
        {
            terminal.WriteLine($"  {i + 1}. {usable[i].GetDisplayName()}");
        }

        var input = await terminal.GetInput("Choice (number or Enter to cancel): ");
        if (!int.TryParse(input.Trim(), out int idx) || idx < 1 || idx > usable.Count)
        {
            terminal.WriteLine("Cancelled.", "gray");
            await Task.Delay(500);
            return;
        }

        var item = usable[idx - 1];

        // Apply effects
        if (item.HP > 0)
        {
            long healed = Math.Min(item.HP, player.MaxHP - player.HP);
            player.HP += healed;
            terminal.WriteLine($"You drink {item.Name} and recover {healed} HP!", "green");
            result.CombatLog.Add($"Player uses {item.Name} for {healed} HP");
        }

        if (item.Mana > 0)
        {
            long manaRestored = Math.Min(item.Mana, player.MaxMana - player.Mana);
            player.Mana += manaRestored;
            terminal.WriteLine($"Your mana is restored by {manaRestored} points!", "bright_blue");
            result.CombatLog.Add($"Player restores {manaRestored} mana with {item.Name}");
        }

        if (item.Cure != Cures.Nothing)
        {
            player.Poison = 0;
            terminal.WriteLine("You feel healthy again!", "bright_green");
        }

        // Escape items (smoke bomb)
        if (item.Name.ToLower().Contains("smoke bomb"))
        {
            terminal.WriteLine("A cloud of smoke envelops the area… you slip away!", "cyan");
            globalEscape = true;
            result.Outcome = CombatOutcome.PlayerEscaped;
        }

        // Remove item from inventory (consumable)
        player.Inventory.Remove(item);
    }
    
    /// <summary>
    /// Execute spell-casting action. Leverages the rich ProcessSpellCasting helper which already
    /// contains the Pascal-compatible spell selection UI and effect application logic.
    /// </summary>
    private async Task ExecuteCastSpell(Character player, Monster monster, CombatResult result)
    {
        // Prevent double-casting in a single round – mirrors original flag from VARIOUS.PAS
        if (player.Casted)
        {
            terminal.WriteLine("You have already cast a spell this round!", "yellow");
            await Task.Delay(1000);
            return;
        }

        // Delegate to the existing spell-handling UI/logic
        ProcessSpellCasting(player, monster);

        // Mark that the player used their casting action this turn so other systems (AI, etc.)
        // can react accordingly.
        player.Casted = true;

        // Add entry to combat log for post-battle analysis and testing.
        result.CombatLog.Add($"{player.DisplayName} casts a spell.");

        // Small delay to keep pacing consistent with other combat actions.
        await Task.Delay(500);
    }
    
    private async Task ShowPvPIntroduction(Character attacker, Character defender, CombatResult result)
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_red");
        terminal.WriteLine("═══ PLAYER FIGHT ═══");
        terminal.WriteLine("");
        
        terminal.SetColor("white");
        terminal.WriteLine($"{attacker.DisplayName} confronts {defender.DisplayName}!");
        await Task.Delay(2000);
    }
    
    private async Task ProcessPlayerVsPlayerAction(CombatAction action, Character attacker, Character defender, CombatResult result)
    {
        // Simplified PvP for now
        await ProcessPlayerAction(action, attacker, null, result);
    }
    
    private async Task ProcessComputerPlayerAction(Character computer, Character opponent, CombatResult result)
    {
        // Basic heuristic AI
        if (!computer.IsAlive || !opponent.IsAlive) return;

        // 1. Heal if low
        if (computer.HP < computer.MaxHP / 3 && computer.Healing > 0)
        {
            computer.Healing--;
            long heal = Math.Min(25, computer.MaxHP - computer.HP);
            computer.HP += heal;
            terminal.WriteLine($"{computer.DisplayName} quaffs a potion and heals {heal} HP!", "green");
            result.CombatLog.Add($"{computer.DisplayName} heals {heal}");
            await Task.Delay(800);
            return;
        }

        // 2. Cast spell if mage and enough mana
        if ((computer.Class == CharacterClass.Magician || computer.Class == CharacterClass.Sage || computer.Class == CharacterClass.Cleric) && computer.Mana > 0)
        {
            var spells = SpellSystem.GetAvailableSpells(computer)
                        .Where(s => SpellSystem.CanCastSpell(computer, s.Level) && s.SpellType == "Attack")
                        .ToList();
            if (spells.Count > 0 && new Random().Next(100) < 40)
            {
                var chosen = spells[new Random().Next(spells.Count)];
                var spellResult = SpellSystem.CastSpell(computer, chosen.Level, opponent);
                terminal.WriteLine(spellResult.Message, "magenta");
                // For now only self-affecting or damage spells ignored in PvP; skip Monster
                ApplySpellEffects(computer, null, spellResult);
                result.CombatLog.Add($"{computer.DisplayName} casts {chosen.Name}");
                await Task.Delay(1000);
                return;
            }
        }

        // 3. Default attack
        long attackPower = computer.Strength + computer.WeapPow + random.Next(1, 16);
        long defense = opponent.Defence + random.Next(0, (int)Math.Max(1, opponent.Defence / 8));
        long damage = Math.Max(1, attackPower - defense);
        opponent.HP = Math.Max(0, opponent.HP - damage);
        terminal.WriteLine($"{computer.DisplayName} strikes for {damage} damage!", "red");
        result.CombatLog.Add($"{computer.DisplayName} hits {opponent.DisplayName} for {damage}");
        await Task.Delay(800);
    }
    
    private async Task DeterminePvPOutcome(CombatResult result)
    {
        if (!result.Player.IsAlive)
        {
            result.Outcome = CombatOutcome.PlayerDied;
            terminal.WriteLine($"{result.Player.DisplayName} has been defeated!", "red");
        }
        else if (!result.Opponent.IsAlive)
        {
            result.Outcome = CombatOutcome.Victory;
            terminal.WriteLine($"{result.Player.DisplayName} is victorious!", "green");
        }
        
        await Task.Delay(2000);
    }

    /// <summary>
    /// Process spell casting during combat
    /// </summary>
    private void ProcessSpellCasting(Character player, Monster monster)
    {
        terminal.ClearScreen();
        terminal.SetColor("white");
        terminal.WriteLine("═══ Spell Casting ═══");
        
        var availableSpells = SpellSystem.GetAvailableSpells(player);
        if (availableSpells.Count == 0)
        {
            terminal.WriteLine($"{player.DisplayName} doesn't know any spells yet!", "red");
            terminal.PressAnyKey();
            return;
        }
        
        // Display available spells
        terminal.WriteLine("Available Spells:");
        for (int i = 0; i < availableSpells.Count; i++)
        {
            var spell = availableSpells[i];
            var manaCost = SpellSystem.CalculateManaCost(spell, player);
            var canCast = player.Mana >= manaCost && player.Level >= SpellSystem.GetLevelRequired(player.Class, spell.Level);
            var color = canCast ? ConsoleColor.White : ConsoleColor.DarkGray;
            
            terminal.SetColor(color);
            terminal.WriteLine($"{i + 1}. {spell.Name} (Level {spell.Level}) - {manaCost} mana");
            if (!canCast)
            {
                terminal.WriteLine("   (Not enough mana)");
            }
        }
        
        terminal.WriteLine("");
        terminal.WriteLine("Enter spell number (0 to cancel): ", ConsoleColor.Yellow, false);
        string input = terminal.GetInputSync();
        
        if (int.TryParse(input, out int spellChoice) && spellChoice > 0 && spellChoice <= availableSpells.Count)
        {
            var selectedSpell = availableSpells[spellChoice - 1];
            
            if (!SpellSystem.CanCastSpell(player, selectedSpell.Level))
            {
                terminal.WriteLine("You cannot cast this spell right now!", "red");
                terminal.PressAnyKey();
                return;
            }
            
            // Cast the spell – the SpellSystem API expects a Character target. We pass null and
            // handle damage application ourselves against the Monster instance further below.
            var spellResult = SpellSystem.CastSpell(player, selectedSpell.Level, null);
            
            terminal.WriteLine("");
            terminal.WriteLine(spellResult.Message);
            
            // Apply spell effects
            ApplySpellEffects(player, monster, spellResult);
            
            terminal.PressAnyKey();
        }
        else if (spellChoice != 0)
        {
            terminal.WriteLine("Invalid spell selection!", "red");
            terminal.PressAnyKey();
        }
    }
    
    /// <summary>
    /// Apply spell effects to combat
    /// </summary>
    private void ApplySpellEffects(Character caster, Monster target, SpellSystem.SpellResult spellResult)
    {
        // Apply healing to caster
        if (spellResult.Healing > 0)
        {
            long oldHP = caster.HP;
            caster.HP = Math.Min(caster.HP + spellResult.Healing, caster.MaxHP);
            long actualHealing = caster.HP - oldHP;
            terminal.WriteLine($"{caster.DisplayName} heals {actualHealing} hitpoints!", "green");
        }
        
        // Apply damage to target
        if (spellResult.Damage > 0 && target != null)
        {
            target.HP = Math.Max(0, target.HP - spellResult.Damage);
            terminal.WriteLine($"{target.Name} takes {spellResult.Damage} damage!", "red");
            
            if (target.HP <= 0)
            {
                terminal.WriteLine($"{target.Name} has been slain by magic!", "dark_red");
                globalPlayerInFight = false;
            }
        }
        
        // Convert buffs into status effects (basic mapping for now)
        if (spellResult.ProtectionBonus > 0)
        {
            int dur = spellResult.Duration > 0 ? spellResult.Duration : 999;
            caster.MagicACBonus = spellResult.ProtectionBonus;
            caster.ApplyStatus(StatusEffect.Blessed, dur);
            terminal.WriteLine($"{caster.DisplayName} is magically protected! (+{spellResult.ProtectionBonus} AC for {dur} rounds)", "blue");
        }

        if (spellResult.AttackBonus > 0)
        {
            // Use PowerStance to represent offensive boost (simplified)
            int dur = spellResult.Duration > 0 ? spellResult.Duration : 3;
            caster.ApplyStatus(StatusEffect.PowerStance, dur);
            terminal.WriteLine($"{caster.DisplayName}'s power surges! (+50% damage for {dur} rounds)", "red");
        }
        
        // Handle special effects
        if (!string.IsNullOrEmpty(spellResult.SpecialEffect))
        {
            HandleSpecialSpellEffect(caster, target, spellResult.SpecialEffect);
        }
    }
    
    /// <summary>
    /// Handle special spell effects
    /// </summary>
    private void HandleSpecialSpellEffect(Character caster, Monster target, string effect)
    {
        switch (effect.ToLower())
        {
            case "poison":
                if (target != null)
                {
                    target.Poisoned = true;
                    target.PoisonRounds = 5;
                    terminal.WriteLine($"{target.Name} is poisoned!", "dark_green");
                }
                break;
                
            case "sleep":
            case "freeze":
                if (target != null)
                {
                    int duration = 2;
                    target.StunRounds = duration;
                    terminal.WriteLine($"{target.Name} is stunned for {duration} rounds!", "blue");
                }
                break;
                
            case "fear":
                if (target != null)
                {
                    target.WeakenRounds = 3;
                    target.Strength = Math.Max(1, target.Strength - 4);
                    terminal.WriteLine($"{target.Name} is weakened by fear!", "yellow");
                }
                break;
                
            case "escape":
                terminal.WriteLine($"{caster.DisplayName} vanishes in a whirl of arcane energy!", "magenta");
                globalEscape = true;
                break;
                
            case "blur":
            case "fog":
            case "duplicate":
                caster.ApplyStatus(StatusEffect.Blur, 999);
                terminal.WriteLine($"{caster.DisplayName}'s outline shimmers and blurs!", "cyan");
                break;
                
            case "stoneskin":
                caster.DamageAbsorptionPool = 10 * caster.Level;
                caster.ApplyStatus(StatusEffect.Stoneskin, 999);
                terminal.WriteLine($"{caster.DisplayName}'s skin hardens to resilient stone!", "dark_gray");
                break;
                
            case "steal":
                if (target != null)
                {
                    int stealCap = (int)Math.Max(1, Math.Min(target.Gold / 10, int.MaxValue));
                    var goldStolen = random.Next(stealCap);
                    if (goldStolen > 0)
                    {
                        target.Gold -= goldStolen;
                        caster.Gold += goldStolen;
                        terminal.WriteLine($"{caster.DisplayName} steals {goldStolen} gold from {target.Name}!", "yellow");
                    }
                    else
                    {
                        terminal.WriteLine($"The steal attempt finds no gold!", "gray");
                    }
                }
                break;
                
            case "convert":
                if (target != null)
                {
                    terminal.WriteLine($"{target.Name} is touched by divine light!", "white");
                    // TODO: Implement conversion effect (monster may flee or become friendly)
                }
                break;
                
            case "haste":
                caster.ApplyStatus(StatusEffect.Haste, 3);
                break;
                
            case "slow":
                if (target != null)
                {
                    target.WeakenRounds = 3;
                }
                break;

            case "identify":
                terminal.WriteLine($"{caster.DisplayName} examines their belongings carefully...", "bright_white");
                foreach (var itm in caster.Inventory)
                {
                    terminal.WriteLine($" • {itm.Name}  (Type: {itm.Type}, Pow: {itm.Attack}/{itm.Armor})", "white");
                }
                break;
        }
    }

    /// <summary>
    /// Execute defend – player braces and gains 50% damage reduction for the next monster hit.
    /// </summary>
    private async Task ExecuteDefend(Character player, CombatResult result)
    {
        player.IsDefending = true;
        player.ApplyStatus(StatusEffect.Defending, 1);
        terminal.WriteLine("You raise your guard, preparing to deflect incoming blows.", "bright_cyan");
        result.CombatLog.Add("Player enters defensive stance (50% damage reduction)");
        await Task.Delay(1000);
    }

    private async Task ExecutePowerAttack(Character attacker, Monster target, CombatResult result)
    {
        // Apply PowerStance status so any extra attacks this round follow the same rules
        attacker.ApplyStatus(StatusEffect.PowerStance, 1);

        // Higher damage, lower accuracy – modelled via larger damage multiplier but higher chance of minimal absorption.
        long originalStrength = attacker.Strength;
        long attackPower = (long)(originalStrength * 1.5);

        if (attacker.WeapPow > 0)
        {
            attackPower += (long)(attacker.WeapPow * 1.5) + random.Next(0, (int)attacker.WeapPow + 1);
        }

        attackPower += random.Next(1, 21); // variation

        // Reduce "accuracy": enemy gains extra defense in calculation (25 % boost)
        long defense = target.Defence + random.Next(0, (int)Math.Max(1, target.Defence / 8));
        defense = (long)(defense * 1.25); // built-in accuracy penalty
        if (target.ArmPow > 0)
        {
            defense += random.Next(0, (int)target.ArmPow + 1);
        }

        long damage = Math.Max(1, attackPower - defense);

        terminal.SetColor("magenta");
        terminal.WriteLine($"POWER ATTACK! You smash the {target.Name} for {damage} damage!");

        target.HP = Math.Max(0, target.HP - damage);
        result.CombatLog.Add($"Player power-attacks {target.Name} for {damage} dmg (PowerStance)");

        await Task.Delay(1000);
    }

    private async Task ExecutePreciseStrike(Character attacker, Monster target, CombatResult result)
    {
        // Higher accuracy (+25 %) but normal damage.
        long attackPower = attacker.Strength;
        if (attacker.WeapPow > 0)
        {
            attackPower += attacker.WeapPow + random.Next(0, (int)attacker.WeapPow + 1);
        }
        attackPower += random.Next(1, 21);

        // Boost accuracy by 25 % via reducing target defense.
        long defense = target.Defence + random.Next(0, (int)Math.Max(1, target.Defence / 8));
        defense = (long)(defense * 0.75);
        if (target.ArmPow > 0)
        {
            defense += random.Next(0, (int)target.ArmPow + 1);
        }

        long damage = Math.Max(1, attackPower - defense);

        terminal.SetColor("bright_cyan");
        terminal.WriteLine($"Precise strike lands for {damage} damage.");

        target.HP = Math.Max(0, target.HP - damage);
        result.CombatLog.Add($"Player precise-strikes {target.Name} for {damage} dmg");

        await Task.Delay(1000);
    }

    private async Task ExecuteRangedAttack(Character attacker, Monster target, CombatResult result)
    {
        if (target == null)
        {
            await Task.Delay(500);
            return;
        }

        // Accuracy heavily Dex-weighted
        long attackScore = attacker.Dexterity + (attacker.Level / 2) + random.Next(1, 21);
        long defenseScore = target.Defence + random.Next(1, 21);

        if (attackScore > defenseScore)
        {
            long damage = attacker.Dexterity / 2 + random.Next(1, 7); // d6 based
            terminal.WriteLine($"You shoot an arrow for {damage} damage!", "bright_green");
            target.HP = Math.Max(0, target.HP - damage);
            result.CombatLog.Add($"Player ranged hits {target.Name} for {damage}");
        }
        else
        {
            terminal.WriteLine("Your missile misses the target.", "gray");
            result.CombatLog.Add("Player ranged misses");
        }

        await Task.Delay(800);
    }

    private async Task ExecuteRage(Character player, CombatResult result)
    {
        player.IsRaging = true;
        terminal.WriteLine("You fly into a bloodthirsty rage!", "bright_red");
        result.CombatLog.Add("Player enters Rage state");
        await Task.Delay(800);
    }

    private async Task ExecuteSmite(Character player, Monster target, CombatResult result)
    {
        if (player.SmiteChargesRemaining <= 0)
        {
            terminal.WriteLine("You are out of smite charges!", "gray");
            await Task.Delay(800);
            return;
        }

        player.SmiteChargesRemaining--;

        // Smite damage: 150 % of normal attack plus level bonus
        long damage = (long)(player.Strength * 1.5) + player.Level;
        if (player.WeapPow > 0)
            damage += (long)(player.WeapPow * 1.5);
        damage += random.Next(1, 21);

        long defense = target.Defence + random.Next(0, (int)Math.Max(1, target.Defence / 8));
        long actual = Math.Max(1, damage - defense);

        terminal.SetColor("yellow");
        terminal.WriteLine($"You SMITE the evil {target.Name} for {actual} holy damage!");

        target.HP = Math.Max(0, target.HP - actual);
        result.CombatLog.Add($"Player smites {target.Name} for {actual} dmg");
        await Task.Delay(1000);
    }

    private async Task ExecuteDisarm(Character player, Monster monster, CombatResult result)
    {
        if (monster == null || string.IsNullOrEmpty(monster.Weapon))
        {
            terminal.WriteLine("Nothing to disarm!", "gray");
            await Task.Delay(600);
            return;
        }

        long attackerScore = player.Dexterity + random.Next(1, 21);
        long defenderScore = (monster.Strength / 2) + random.Next(1, 21);

        if (attackerScore > defenderScore)
        {
            monster.WeapPow = 0;
            monster.Weapon = "";
            monster.WUser = false;
            terminal.WriteLine($"You knock the {monster.Name}'s weapon away!", "yellow");
            result.CombatLog.Add($"{player.DisplayName} disarmed {monster.Name}");
        }
        else
        {
            terminal.WriteLine("Disarm attempt failed!", "gray");
        }
        await Task.Delay(900);
    }

    private async Task ExecuteTaunt(Character player, Monster monster, CombatResult result)
    {
        if (monster == null)
        {
            await Task.Delay(500);
            return;
        }
        terminal.WriteLine($"You taunt {monster.Name}, drawing its ire!", "yellow");
        // Simple effect: lower monster defence for next round
        monster.Defence = Math.Max(0, monster.Defence - 2);
        result.CombatLog.Add($"{player.DisplayName} taunted {monster.Name}");
        await Task.Delay(700);
    }

    private async Task ExecuteHide(Character player, CombatResult result)
    {
        // Dexterity check
        long roll = player.Dexterity + random.Next(1, 21);
        if (roll >= 15)
        {
            player.ApplyStatus(StatusEffect.Hidden, 1);
            terminal.WriteLine("You melt into the shadows, ready to strike!", "dark_gray");
            result.CombatLog.Add("Player hides (next attack gains advantage)");
        }
        else
        {
            terminal.WriteLine("You fail to find cover and remain exposed.", "gray");
        }
        await Task.Delay(800);
    }

    private int GetAttackCount(Character attacker)
    {
        int attacks = 1;

        // Warrior extra swings
        var mods = attacker.GetClassCombatModifiers();
        attacks += mods.ExtraAttacks;

        // Haste doubles attacks
        if (attacker.HasStatus(StatusEffect.Haste))
            attacks *= 2;

        // Slow halves attacks (rounded down)
        if (attacker.HasStatus(StatusEffect.Slow))
            attacks = Math.Max(1, attacks / 2);

        return attacks;
    }
}

/// <summary>
/// Combat action types - Pascal menu options
/// </summary>
public enum CombatActionType
{
    Attack,
    Defend,
    Heal,
    QuickHeal,
    FightToDeath,
    Status,
    BegForMercy,
    UseItem,
    CastSpell,
    SoulStrike,     // Paladin ability
    Backstab,       // Assassin ability
    Retreat,
    PowerAttack,
    PreciseStrike,
    Rage,
    Smite,
    Disarm,
    Taunt,
    Hide,
    RangedAttack
}

/// <summary>
/// Combat action data
/// </summary>
public class CombatAction
{
    public CombatActionType Type { get; set; }
    public int SpellIndex { get; set; }
    public int ItemIndex { get; set; }
    public string TargetId { get; set; } = "";
}

/// <summary>
/// Combat result data
/// </summary>
public class CombatResult
{
    public Character Player { get; set; }
    public Monster Monster { get; set; }
    public Character Opponent { get; set; }           // For PvP
    public List<Character> Teammates { get; set; } = new();
    public CombatOutcome Outcome { get; set; }
    public List<string> CombatLog { get; set; } = new();
    public long ExperienceGained { get; set; }
    public long GoldGained { get; set; }
    public List<string> ItemsFound { get; set; } = new();
}

/// <summary>
/// Combat outcomes
/// </summary>
public enum CombatOutcome
{
    Victory,
    PlayerDied,
    PlayerEscaped,
    Stalemate,
    Interrupted
} 
