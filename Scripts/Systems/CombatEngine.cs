using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Combat Engine - Pascal-compatible combat system
/// Based on PLVSMON.PAS, MURDER.PAS, VARIOUS.PAS, and PLCOMP.PAS
/// </summary>
public class CombatEngine
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
        
        // Combat introduction
        await ShowCombatIntroduction(player, monster, result);
        
        // Main combat loop
        while (player.IsAlive && monster.IsAlive && !globalEscape && !globalKilled)
        {
            // Player's turn
            if (player.IsAlive && monster.IsAlive)
            {
                var playerAction = await GetPlayerAction(player, monster, result);
                await ProcessPlayerAction(playerAction, player, monster, result);
            }
            
            // Teammates' turns
            foreach (var teammate in result.Teammates)
            {
                if (teammate.IsAlive && monster.IsAlive)
                {
                    await ProcessTeammateAction(teammate, monster, result);
                }
            }
            
            // Monster's turn
            if (monster.IsAlive && !globalEscape)
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
        
        terminal.WriteLine("");
        
        // Combat menu - exact Pascal layout
        terminal.SetColor("white");
        terminal.WriteLine("(A)ttack  (H)eal  (Q)uick Heal  (F)ight to Death");
        terminal.WriteLine("(S)tatus  (B)eg for Mercy  (U)se Item");
        
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
            "H" => new CombatAction { Type = CombatActionType.Heal },
            "Q" => new CombatAction { Type = CombatActionType.QuickHeal },
            "F" => new CombatAction { Type = CombatActionType.FightToDeath },
            "S" => new CombatAction { Type = CombatActionType.Status },
            "B" => new CombatAction { Type = CombatActionType.BegForMercy },
            "U" => new CombatAction { Type = CombatActionType.UseItem },
            "C" => new CombatAction { Type = CombatActionType.CastSpell },
            "1" when player.Class == CharacterClass.Paladin => new CombatAction { Type = CombatActionType.SoulStrike },
            "1" when player.Class == CharacterClass.Assassin => new CombatAction { Type = CombatActionType.Backstab },
            "R" => new CombatAction { Type = CombatActionType.Retreat },
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
                
            case CombatActionType.Retreat:
                await ExecuteRetreat(player, monster, result);
                break;
        }
    }
    
    /// <summary>
    /// Execute attack - Pascal normal_attack calculation
    /// Based on normal_attack function from VARIOUS.PAS
    /// </summary>
    private async Task ExecuteAttack(Character attacker, Monster target, CombatResult result)
    {
        // Calculate attack power (Pascal-compatible)
        long attackPower = attacker.Strength;
        
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
            
            // Calculate defense absorption (Pascal-compatible)
            long defense = target.Defence + random.Next(0, Math.Max(1, target.Defence / 8));
            
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
        int successChance = player.Dexterity * 2;
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
        int mercyChance = player.Charisma * 2;
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
        
        // Monster attack calculation (Pascal-compatible)
        long monsterAttack = monster.GetAttackPower();
        
        // Add random variation
        monsterAttack += random.Next(0, 10);
        
        if (monsterAttack > 0)
        {
            terminal.WriteLine($"The {monster.Name} attacks you for {monsterAttack} damage!");
            
            // Player defense (Pascal-compatible)
            long playerDefense = player.Defence + random.Next(0, Math.Max(1, player.Defence / 8));
            
            if (player.ArmPow > 0)
            {
                playerDefense += random.Next(0, (int)player.ArmPow + 1);
            }
            
            long actualDamage = Math.Max(1, monsterAttack - playerDefense);
            
            if (playerDefense > 0 && playerDefense < monsterAttack)
            {
                terminal.WriteLine($"Your armor absorbed {playerDefense} points!");
            }
            
            player.HP = Math.Max(0, player.HP - actualDamage);
            terminal.WriteLine($"You take {actualDamage} damage!");
            
            result.CombatLog.Add($"{monster.Name} attacks player for {actualDamage} damage");
        }
        else
        {
            terminal.WriteLine($"The {monster.Name} attacks but misses!");
            result.CombatLog.Add($"{monster.Name} misses player");
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
        
        // Check for weapon/armor drops
        if (result.Monster.GrabWeap && !string.IsNullOrEmpty(result.Monster.Weapon))
        {
            terminal.WriteLine($"You can take the {result.Monster.Weapon}!");
            // TODO: Implement item pickup
        }
        
        if (result.Monster.GrabArm && !string.IsNullOrEmpty(result.Monster.Armor))
        {
            terminal.WriteLine($"You can take the {result.Monster.Armor}!");
            // TODO: Implement item pickup
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
    
    private async Task ExecuteUseItem(Character player, CombatResult result)
    {
        terminal.WriteLine("Item usage not yet implemented.", "gray");
        await Task.Delay(1000);
    }
    
    private async Task ExecuteCastSpell(Character player, Monster monster, CombatResult result)
    {
        terminal.WriteLine("Spell casting not yet implemented.", "gray");
        await Task.Delay(1000);
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
        // Simple AI attack
        terminal.WriteLine($"{computer.DisplayName} attacks!", "red");
        await Task.Delay(1000);
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
    private void ProcessSpellCasting(Character player, Character monster)
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
            var canCast = SpellSystem.CanCastSpell(player, spell.Level);
            var color = canCast ? ConsoleColor.White : ConsoleColor.DarkGray;
            
            terminal.SetColor(color);
            terminal.WriteLine($"{i + 1}. {spell.Name} (Level {spell.Level}) - {spell.ManaCost} mana");
            if (!canCast)
            {
                terminal.WriteLine("   (Not enough mana)");
            }
        }
        
        terminal.WriteLine("");
        terminal.WriteLine("Enter spell number (0 to cancel): ", ConsoleColor.Yellow, false);
        string input = terminal.GetInput();
        
        if (int.TryParse(input, out int spellChoice) && spellChoice > 0 && spellChoice <= availableSpells.Count)
        {
            var selectedSpell = availableSpells[spellChoice - 1];
            
            if (!SpellSystem.CanCastSpell(player, selectedSpell.Level))
            {
                terminal.WriteLine("You cannot cast this spell right now!", "red");
                terminal.PressAnyKey();
                return;
            }
            
            // Cast the spell
            var spellResult = SpellSystem.CastSpell(player, selectedSpell.Level, monster);
            
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
    private void ApplySpellEffects(Character caster, Character target, SpellSystem.SpellResult spellResult)
    {
        // Apply healing to caster
        if (spellResult.Healing > 0)
        {
            int oldHP = caster.HP;
            caster.HP = Math.Min(caster.HP + spellResult.Healing, caster.MaxHP);
            int actualHealing = caster.HP - oldHP;
            terminal.WriteLine($"{caster.DisplayName} heals {actualHealing} hitpoints!", "green");
        }
        
        // Apply damage to target
        if (spellResult.Damage > 0 && target != null)
        {
            target.HP = Math.Max(0, target.HP - spellResult.Damage);
            terminal.WriteLine($"{target.DisplayName} takes {spellResult.Damage} damage!", "red");
            
            if (target.HP <= 0)
            {
                terminal.WriteLine($"{target.DisplayName} has been slain by magic!", "dark_red");
                globalPlayerInFight = false;
            }
        }
        
        // Apply temporary buffs/debuffs (simplified for now)
        if (spellResult.ProtectionBonus > 0)
        {
            terminal.WriteLine($"{caster.DisplayName} gains +{spellResult.ProtectionBonus} protection!", "blue");
            // TODO: Implement temporary effect system
        }
        
        if (spellResult.AttackBonus > 0)
        {
            terminal.WriteLine($"{caster.DisplayName} gains +{spellResult.AttackBonus} attack power!", "red");
            // TODO: Implement temporary effect system
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
    private void HandleSpecialSpellEffect(Character caster, Character target, string effect)
    {
        switch (effect.ToLower())
        {
            case "poison":
                if (target != null)
                {
                    terminal.WriteLine($"{target.DisplayName} is poisoned!", "dark_green");
                    // TODO: Implement poison status effect
                }
                break;
                
            case "sleep":
                if (target != null)
                {
                    terminal.WriteLine($"{target.DisplayName} falls into a magical sleep!", "blue");
                    // TODO: Implement sleep status effect
                }
                break;
                
            case "freeze":
                if (target != null)
                {
                    terminal.WriteLine($"{target.DisplayName} is frozen solid!", "cyan");
                    // TODO: Implement freeze status effect
                }
                break;
                
            case "fear":
                if (target != null)
                {
                    terminal.WriteLine($"{target.DisplayName} is overwhelmed by supernatural fear!", "yellow");
                    // TODO: Implement fear status effect
                }
                break;
                
            case "escape":
                terminal.WriteLine($"{caster.DisplayName} attempts to escape using magic!", "magenta");
                if (random.Next(100) < 75) // 75% success rate
                {
                    terminal.WriteLine("The escape is successful!", "green");
                    globalEscape = true;
                }
                else
                {
                    terminal.WriteLine("The escape attempt fails!", "red");
                }
                break;
                
            case "steal":
                if (target != null)
                {
                    var goldStolen = random.Next(target.Gold / 10);
                    if (goldStolen > 0)
                    {
                        target.Gold -= goldStolen;
                        caster.Gold += goldStolen;
                        terminal.WriteLine($"{caster.DisplayName} steals {goldStolen} gold from {target.DisplayName}!", "yellow");
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
                    terminal.WriteLine($"{target.DisplayName} is touched by divine light!", "white");
                    // TODO: Implement conversion effect (monster may flee or become friendly)
                }
                break;
        }
    }
}

/// <summary>
/// Combat action types - Pascal menu options
/// </summary>
public enum CombatActionType
{
    Attack,
    Heal,
    QuickHeal,
    FightToDeath,
    Status,
    BegForMercy,
    UseItem,
    CastSpell,
    SoulStrike,     // Paladin ability
    Backstab,       // Assassin ability
    Retreat
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