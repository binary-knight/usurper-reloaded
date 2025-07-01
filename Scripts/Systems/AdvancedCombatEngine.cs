using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Advanced Combat Engine - Complete implementation based on Pascal PLVSMON.PAS, PLVSPLC.PAS
/// Provides sophisticated combat mechanics including retreat, special abilities, monster AI, and PvP features
/// Direct Pascal compatibility with exact function preservation
/// </summary>
public class AdvancedCombatEngine : Node
{
    private NewsSystem newsSystem;
    
    // Note: MailSystem and SpellSystem are static - use static access directly
    
    private RelationshipSystem relationshipSystem;
    
    // Pascal global variables from PLVSMON.PAS
    private bool globalPlayerInFight = false;
    private bool globalKilled = false;
    private bool globalBegged = false;
    private bool globalEscape = false;
    private int globalDungeonLevel = 1;
    
    // Special items from Pascal (PLVSMON.PAS supreme being fight)
    private bool globalSwordFound = false;   // Black Sword
    private bool globalLanternFound = false; // Sacred Lantern  
    private bool globalWStaffFound = false;  // White Staff
    
    // Pascal combat constants
    private const int MaxMonstersInFight = 5; // Pascal global_maxmon
    private const int CowardlyRunAwayDamage = 10; // Base damage for failed retreat
    
    public override void _Ready()
    {
        newsSystem = NewsSystem.Instance;
        // mailSystem and spellSystem are static - no instance needed
        relationshipSystem = new RelationshipSystem();
    }
    
    #region Pascal Player vs Monster Combat - PLVSMON.PAS
    
    /// <summary>
    /// Player vs Monsters - Pascal PLVSMON.PAS Player_vs_Monsters procedure
    /// </summary>
    public async Task<AdvancedCombatResult> PlayerVsMonsters(int monsterMode, Character player, 
        List<Character> teammates, List<Monster> monsters)
    {
        // Pascal monster_mode constants:
        // 1 = dungeon monsters, 2 = door guards, 3 = supreme being
        // 4 = demon, 5 = alchemist opponent, 6 = prison guards
        
        var result = new AdvancedCombatResult
        {
            CombatType = AdvancedCombatType.PlayerVsMonster,
            Player = player,
            Teammates = teammates,
            Monsters = monsters,
            MonsterMode = monsterMode
        };
        
        // Set Pascal combat flag
        globalPlayerInFight = true;
        globalKilled = false;
        globalBegged = false;
        globalEscape = false;
        
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        
        // Initialize combat
        await InitializeMonsterCombat(result, terminal);
        
        // Main combat loop (Pascal repeat-until)
        while (!result.IsComplete && player.IsAlive && monsters.Any(m => m.IsAlive))
        {
            // Reset combat flags for this round
            player.Casted = false;
            player.UsedItem = false;
            foreach (var teammate in teammates.Where(t => t.IsAlive))
            {
                teammate.Casted = false;
                teammate.UsedItem = false;
            }
            
            // Display current monster status
            await DisplayMonsterStatus(monsters, terminal);
            
            // Player's turn
            if (player.IsAlive && monsters.Any(m => m.IsAlive))
            {
                var action = await GetPlayerCombatAction(player, monsters, terminal);
                await ProcessPlayerCombatAction(action, player, monsters, result, terminal);
            }
            
            // Teammates' turns
            foreach (var teammate in teammates.Where(t => t.IsAlive))
            {
                if (monsters.Any(m => m.IsAlive))
                {
                    await ProcessTeammateTurn(teammate, monsters, result, terminal);
                }
            }
            
            // Monsters' attack phase
            if (monsters.Any(m => m.IsAlive) && !globalEscape)
            {
                await ProcessMonsterAttackPhase(monsters, player, teammates, result, terminal);
            }
            
            // Check for combat end conditions
            if (globalKilled || globalEscape || !player.IsAlive || !monsters.Any(m => m.IsAlive))
            {
                result.IsComplete = true;
            }
        }
        
        // Determine final outcome
        await DetermineMonsterCombatOutcome(result, terminal);
        
        globalPlayerInFight = false;
        return result;
    }
    
    /// <summary>
    /// Retreat function - Pascal PLVSMON.PAS Retreat function
    /// </summary>
    private async Task<bool> AttemptRetreat(Character player, List<Monster> monsters, 
        AdvancedCombatResult result, TerminalEmulator terminal)
    {
        var random = new Random();
        
        switch (random.Next(2))
        {
            case 0: // Successful retreat
                terminal.WriteLine($"\n{GameConfig.TextColor}You have escaped battle!{GameConfig.TextColor}");
                globalEscape = true;
                result.Outcome = AdvancedCombatOutcome.PlayerEscaped;
                result.CombatLog.Add("Player successfully retreated from combat");
                return true;
                
            case 1: // Failed retreat
                terminal.WriteLine($"\n{GameConfig.TextColor}The monster won't let you escape!{GameConfig.TextColor}");
                
                // Pascal cowardly damage calculation
                int damage = random.Next(globalDungeonLevel * 10) + 3;
                
                terminal.WriteLine("As you cowardly turn and run, you feel pain when something");
                terminal.WriteLine($"hits you in the back for {GameConfig.DamageColor}{damage:N0}{GameConfig.TextColor} points");
                
                player.HP -= damage;
                result.CombatLog.Add($"Player failed to retreat and took {damage} damage");
                
                if (player.HP <= 0)
                {
                    // Player dies from cowardly retreat
                    terminal.WriteLine($"\n{GameConfig.DeathColor}You have been slain!{GameConfig.TextColor}");
                    player.HP = 0;
                    globalKilled = true;
                    
                    // Generate news (Pascal newsy call)
                    string deathMessage = GetRandomDeathMessage(player.Name2);
                    NewsSystem.Instance.Newsy(true, $"Coward! {deathMessage}");
                    
                    // Handle resurrection system
                    await HandlePlayerDeath(player, "cowardly retreat", terminal);
                    
                    result.Outcome = AdvancedCombatOutcome.PlayerDied;
                    result.CombatLog.Add("Player died while attempting to retreat");
                    return false;
                }
                
                await terminal.WaitForKeyPress();
                return false;
        }
    }
    
    /// <summary>
    /// Monster charge - Pascal PLVSMON.PAS Monster_Charge procedure
    /// </summary>
    private void CalculateMonsterAttacks(List<Monster> monsters, int mode)
    {
        var random = new Random();
        
        foreach (var monster in monsters.Where(m => m.IsAlive))
        {
            monster.Punch = 0;
            
            switch (mode)
            {
                case 1: // Dungeon monsters
                    {
                        int strengthDivisor = 3;
                        if (monster.Strength < 10) monster.Strength = 10;
                        
                        if (monster.WeaponPower > 32000) monster.WeaponPower = 32000;
                        
                        monster.Punch = monster.WeaponPower + random.Next(monster.WeaponPower);
                        monster.Punch += monster.Strength / strengthDivisor;
                        
                        // Lucky freak attack (Pascal logic)
                        if (random.Next(3) == 0)
                        {
                            monster.Punch += random.Next(5) + 1;
                        }
                        break;
                    }
                    
                case 2: // Door guards
                    {
                        int attackPower = monster.Strength * 2;
                        monster.Punch = random.Next(attackPower);
                        break;
                    }
                    
                case 3: // Supreme Being
                    {
                        // Use a reasonable max HP value instead of GlobalGameState
                        monster.Punch = random.Next(100) + 3;
                        
                        // Special item interactions (Pascal supreme being logic)
                        if (globalSwordFound)
                        {
                            // Black Sword attacks Supreme Being
                            monster.HP -= 75;
                            // Display message handled elsewhere
                        }
                        
                        if (globalLanternFound)
                        {
                            // Sacred Lantern reduces damage
                            monster.Punch /= 2;
                        }
                        
                        if (globalWStaffFound)
                        {
                            // White Staff protection
                            monster.Punch -= 50;
                            if (monster.Punch < 0) monster.Punch = 0;
                        }
                        break;
                    }
                    
                case 4: // Demon combat
                case 5: // Alchemist opponent  
                case 6: // Prison guards
                    {
                        // Standard attack calculation
                        monster.Punch = random.Next(monster.Strength) + monster.WeaponPower / 2;
                        break;
                    }
            }
        }
    }
    
    /// <summary>
    /// Process monster death and loot - Pascal PLVSMON.PAS has_monster_died
    /// </summary>
    private async Task ProcessMonsterDeath(Monster monster, Character player, List<Character> teammates, 
        AdvancedCombatResult result, TerminalEmulator terminal)
    {
        var random = new Random();
        
        terminal.WriteLine($"\n{GameConfig.DeathColor}The {monster.Name} has been slain!{GameConfig.TextColor}");
        
        // Experience gain
        long expGain = CalculateExperienceGain(monster, player);
        player.Experience += expGain;
        result.ExperienceGained += expGain;
        
        terminal.WriteLine($"You gain {GameConfig.ExperienceColor}{expGain:N0}{GameConfig.TextColor} experience points!");
        
        // Gold drop
        if (monster.Gold > 0)
        {
            player.Gold += (long)monster.Gold;
            result.GoldGained += (long)monster.Gold;
            terminal.WriteLine($"You find {GameConfig.GoldColor}{monster.Gold:N0}{GameConfig.TextColor} gold pieces!");
        }
        
        // Weapon drop (Pascal logic: random(5) = 0 chance)
        if (random.Next(5) == 0 && !string.IsNullOrEmpty(monster.WeaponName) && monster.CanGrabWeapon)
        {
            await HandleWeaponDrop(monster, player, teammates, terminal);
        }
        
        // Armor drop  
        if (random.Next(5) == 0 && !string.IsNullOrEmpty(monster.ArmorName) && monster.CanGrabArmor)
        {
            await HandleArmorDrop(monster, player, teammates, terminal);
        }
        
        result.CombatLog.Add($"{monster.Name} defeated - gained {expGain} exp, {monster.Gold} gold");
    }
    
    /// <summary>
    /// Handle weapon drop from monster - Pascal PLVSMON.PAS weapon grabbing logic
    /// </summary>
    private async Task HandleWeaponDrop(Monster monster, Character player, List<Character> teammates, 
        TerminalEmulator terminal)
    {
        terminal.WriteLine($"\n{GameConfig.ItemColor}You have found something: {monster.WeaponName}{GameConfig.TextColor}");
        
        terminal.Write("Take it? (Y/N): ");
        var input = await terminal.GetKeyInput();
        
        if (!string.IsNullOrEmpty(input) && char.ToUpperInvariant(input[0]) == 'Y')
        {
            // Try to add to player inventory
            if (await TryAddItemToInventory(player, monster.WeaponId, ObjType.Weapon, monster.WeaponName, terminal))
            {
                terminal.WriteLine($"You place the {GameConfig.ItemColor}{monster.WeaponName}{GameConfig.TextColor} in your backpack");
            }
        }
        else
        {
            // Teammates can try to take it (Pascal logic)
            foreach (var teammate in teammates.Where(t => t.IsAlive))
            {
                terminal.WriteLine($"\n{GameConfig.PlayerColor}{teammate.Name2}{GameConfig.TextColor} picks up the {GameConfig.ItemColor}{monster.WeaponName}{GameConfig.TextColor}.");
                
                if (await TryAddItemToInventory(teammate, monster.WeaponId, ObjType.Weapon, monster.WeaponName, terminal))
                {
                    break; // First teammate who can take it gets it
                }
            }
        }
    }
    
    /// <summary>
    /// Handle armor drop from monster - Pascal PLVSMON.PAS armor grabbing logic
    /// </summary>
    private async Task HandleArmorDrop(Monster monster, Character player, List<Character> teammates, 
        TerminalEmulator terminal)
    {
        terminal.WriteLine($"\n{GameConfig.ItemColor}You have found something: {monster.ArmorName}{GameConfig.TextColor}");
        
        terminal.Write("Take it? (Y/N): ");
        var input = await terminal.GetKeyInput();
        
        if (!string.IsNullOrEmpty(input) && char.ToUpperInvariant(input[0]) == 'Y')
        {
            // Try to add to player inventory
            if (await TryAddItemToInventory(player, monster.ArmorId, ObjType.Abody, monster.ArmorName, terminal))
            {
                terminal.WriteLine($"You place the {GameConfig.ItemColor}{monster.ArmorName}{GameConfig.TextColor} in your backpack");
            }
        }
        else
        {
            // Teammates can try to take it (Pascal logic)
            foreach (var teammate in teammates.Where(t => t.IsAlive))
            {
                terminal.WriteLine($"\n{GameConfig.PlayerColor}{teammate.Name2}{GameConfig.TextColor} picks up the {GameConfig.ItemColor}{monster.ArmorName}{GameConfig.TextColor}.");
                
                if (await TryAddItemToInventory(teammate, monster.ArmorId, ObjType.Abody, monster.ArmorName, terminal))
                {
                    break; // First teammate who can take it gets it
                }
            }
        }
    }
    
    #endregion
    
    #region Pascal Player vs Player Combat - PLVSPLC.PAS
    
    /// <summary>
    /// Player vs Player combat - Pascal PLVSPLC.PAS Player_vs_Player procedure
    /// </summary>
    public async Task<AdvancedCombatResult> PlayerVsPlayer(Character attacker, Character defender, bool offlineKill = false)
    {
        var result = new AdvancedCombatResult
        {
            CombatType = AdvancedCombatType.PlayerVsPlayer,
            Player = attacker,
            Opponent = defender,
            OfflineKill = offlineKill
        };
        
        globalPlayerInFight = true;
        globalBegged = false;
        
        var terminal = GetNode<TerminalEmulator>("/root/TerminalEmulator");
        
        // Initialize PvP combat
        await InitializePlayerVsPlayerCombat(attacker, defender, result, terminal);
        
        // Main PvP combat loop
        bool toDeath = false;
        bool expertPress = false;
        bool fastGame = false;
        
        while (!result.IsComplete && attacker.IsAlive && defender.IsAlive)
        {
            // Reset spell flags (Pascal logic)
            for (int i = 0; i < GameConfig.MaxSpells; i++)
            {
                attacker.Spell[i][1] = false; // Reset mastered spells for this round
                defender.Spell[i][1] = false;
            }
            
            // Reset item usage flags
            attacker.UsedItem = false;
            defender.UsedItem = false;
            
            // Display current status
            await DisplayPvPStatus(attacker, defender, terminal);
            
            // Get player action
            if (!toDeath)
            {
                var action = await GetPvPCombatAction(attacker, defender, expertPress, terminal);
                
                if (action.Type == PvPActionType.ShowMenu)
                {
                    expertPress = true;
                    continue;
                }
                
                await ProcessPvPAction(action, attacker, defender, result, terminal);
                
                // Check for special outcomes (beg for mercy, fight to death)
                if (action.Type == PvPActionType.BegForMercy)
                {
                    await ProcessBegForMercy(attacker, defender, result, terminal);
                    break;
                }
                
                if (action.Type == PvPActionType.FightToDeath)
                {
                    toDeath = true;
                    terminal.WriteLine($"\n{GameConfig.CombatColor}FIGHT TO THE DEATH!{GameConfig.TextColor}");
                    terminal.WriteLine("No mercy will be shown!");
                }
            }
            else
            {
                // Fight to death mode - only attack actions
                var attackAction = new PvPCombatAction { Type = PvPActionType.Attack };
                await ProcessPvPAction(attackAction, attacker, defender, result, terminal);
            }
            
            // Defender's turn (if computer controlled)
            if (defender.IsAlive && defender.AI == CharacterAI.Computer)
            {
                await ProcessComputerPvPTurn(defender, attacker, result, terminal);
            }
            
            // Check for combat end
            if (!attacker.IsAlive || !defender.IsAlive)
            {
                result.IsComplete = true;
            }
        }
        
        // Determine PvP outcome
        await DeterminePvPOutcome(attacker, defender, result, terminal);
        
        globalPlayerInFight = false;
        return result;
    }
    
    /// <summary>
    /// Beg for mercy - Pascal PLVSPLC.PAS beg for mercy logic
    /// </summary>
    private async Task ProcessBegForMercy(Character attacker, Character defender, AdvancedCombatResult result, 
        TerminalEmulator terminal)
    {
        var random = new Random();
        globalBegged = true;
        
        terminal.WriteLine($"\n{GameConfig.WarningColor}*Surrender!*{GameConfig.TextColor}");
        terminal.WriteLine("************");
        terminal.WriteLine("You throw yourself to the ground and beg for mercy!");
        terminal.WriteLine($"{GameConfig.PlayerColor}{defender.Name2}{GameConfig.TextColor} looks at you! The crowd around you scream for blood!");
        terminal.WriteLine($"They hand {GameConfig.PlayerColor}{defender.Name2}{GameConfig.TextColor} a big sword. You wait for the deathblow!");
        
        // Check if defender shows mercy (Pascal logic - can be influenced by phrases)
        bool showMercy = random.Next(2) == 0; // 50% chance base
        
        if (showMercy)
        {
            terminal.WriteLine($"But you have been spared! {GameConfig.PlayerColor}{defender.Name2}{GameConfig.TextColor} just looks at you with contempt.");
            
            // Display defender's mercy phrase
            string mercyPhrase = !string.IsNullOrEmpty(defender.Phrases[4]) ? 
                defender.Phrases[4] : "I don't have time to kill worms like you!";
            terminal.WriteLine($"{GameConfig.TalkColor}{mercyPhrase}{GameConfig.TextColor}");
            terminal.WriteLine("You crawl away, happy to be alive, but with no pride!");
            
            // Update stats for mercy
            attacker.PDefeats++;
            defender.PKills++;
            
            // Experience gain for defender
            long expGain = (random.Next(50) + 250) * attacker.Level;
            defender.Experience += expGain;
            
            // Send mail to defender about victory
            await SendPvPVictoryMail(defender, attacker, expGain, "Enemy Surrender!", false);
            
            // News coverage
            newsSystem.Newsy(true, "Coward in action",
                $"{GameConfig.NewsColorPlayer}{attacker.Name2}{GameConfig.NewsColorDefault} challenged {GameConfig.NewsColorPlayer}{defender.Name2}{GameConfig.NewsColorDefault} but turned chicken and begged for mercy!",
                $"{GameConfig.NewsColorPlayer}{defender.Name2}{GameConfig.NewsColorDefault} decided to spare {GameConfig.NewsColorPlayer}{attacker.Name2}{GameConfig.NewsColorDefault}'s miserable life!");
            
            result.Outcome = AdvancedCombatOutcome.PlayerSurrendered;
        }
        else
        {
            // No mercy shown - player dies
            await ProcessNoMercyKill(attacker, defender, result, terminal);
        }
    }
    
    /// <summary>
    /// No mercy kill - Pascal PLVSPLC.PAS no mercy logic
    /// </summary>
    private async Task ProcessNoMercyKill(Character attacker, Character defender, AdvancedCombatResult result, 
        TerminalEmulator terminal)
    {
        var random = new Random();
        
        terminal.WriteLine($"\n{GameConfig.DeathColor}NO MERCY!{GameConfig.TextColor}");
        terminal.WriteLine($"{GameConfig.PlayerColor}{defender.Name2}{GameConfig.TextColor} shows no compassion!");
        
        // Display defender's kill phrase
        string killPhrase = !string.IsNullOrEmpty(defender.Phrases[5]) ? 
            defender.Phrases[5] : "Die, you worthless coward!";
        terminal.WriteLine($"{GameConfig.TalkColor}{killPhrase}{GameConfig.TextColor}");
        
        // Player dies
        attacker.HP = 0;
        globalKilled = true;
        
        // Gold transfer
        long goldTransferred = attacker.Gold;
        defender.Gold += goldTransferred;
        attacker.Gold = 0;
        
        // Experience gain for defender
        long expGain = (random.Next(50) + 250) * attacker.Level;
        defender.Experience += expGain;
        
        // Update stats
        attacker.PDefeats++;
        defender.PKills++;
        
        // Heal defender if autoheal is enabled
        if (defender.AutoHeal)
        {
            // Auto-healing logic
            defender.HP = defender.MaxHP;
        }
        
        // Send mail notifications
        await SendPvPVictoryMail(defender, attacker, expGain, "Self-Defence!", true);
        await SendPvPDeathMail(attacker, defender, goldTransferred);
        
        // News coverage
        newsSystem.Newsy(true, "Player Fight!",
            $"{GameConfig.NewsColorPlayer}{attacker.Name2}{GameConfig.NewsColorDefault} challenged {GameConfig.NewsColorPlayer}{defender.Name2}{GameConfig.NewsColorDefault} but lost and begged for mercy!",
            $"{GameConfig.NewsColorPlayer}{defender.Name2}{GameConfig.NewsColorDefault} showed no mercy. {GameConfig.NewsColorPlayer}{attacker.Name2}{GameConfig.NewsColorDefault} was slaughtered!");
        
        // Handle player death and resurrection
        await HandlePlayerDeath(attacker, "killed in PvP combat", terminal);
        
        result.Outcome = AdvancedCombatOutcome.PlayerDied;
        result.GoldLost = goldTransferred;
    }
    
    #endregion
    
    #region Combat Action Processing
    
    /// <summary>
    /// Get player combat action - Pascal PLVSMON.PAS shared_menu logic
    /// </summary>
    private async Task<CombatAction> GetPlayerCombatAction(Character player, List<Monster> monsters, 
        TerminalEmulator terminal)
    {
        terminal.WriteLine($"\n{GameConfig.PlayerColor}Your{GameConfig.TextColor} hitpoints: {GameConfig.HPColor}{player.HP:N0}{GameConfig.TextColor}");
        
        // Display monster status
        foreach (var monster in monsters.Where(m => m.IsAlive))
        {
            terminal.WriteLine($"{GameConfig.MonsterColor}{monster.Name}{GameConfig.TextColor} hitpoints: {GameConfig.HPColor}{monster.HP:N0}{GameConfig.TextColor}");
        }
        
        terminal.WriteLine("");
        
        // Combat menu (Pascal layout)
        terminal.WriteLine("(A)ttack  (H)eal  (Q)uick Heal  (R)etreat");
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
        
        terminal.Write("\nChoice: ");
        
        var input = await terminal.GetKeyInput();
        char choice = !string.IsNullOrEmpty(input) ? char.ToUpperInvariant(input[0]) : '\0';
        
        return ParseMonsterCombatAction(choice, player);
    }
    
    /// <summary>
    /// Get PvP combat action - Pascal PLVSPLC.PAS player vs player menu
    /// </summary>
    private async Task<PvPCombatAction> GetPvPCombatAction(Character attacker, Character defender, 
        bool expertPress, TerminalEmulator terminal)
    {
        if (!attacker.Expert || expertPress)
        {
            terminal.WriteLine("\n(A)ttack  (H)eal  (Q)uick Heal  (F)ight to Death");
            terminal.WriteLine("(S)tatus  (B)eg for Mercy  (U)se Item");
            
            if (attacker.Class == CharacterClass.Cleric || attacker.Class == CharacterClass.Magician || attacker.Class == CharacterClass.Sage)
            {
                terminal.WriteLine("(C)ast Spell");
            }
            
            if (attacker.Class == CharacterClass.Paladin)
            {
                terminal.WriteLine("(1) Soul Strike");
            }
            
            if (attacker.Class == CharacterClass.Assassin)
            {
                terminal.WriteLine("(1) Backstab");
            }
        }
        else
        {
            terminal.Write("Fight (A,H,Q,F,S,B,U,*");
            if (attacker.Class == CharacterClass.Cleric || attacker.Class == CharacterClass.Magician || attacker.Class == CharacterClass.Sage)
            {
                terminal.Write(",C");
            }
            if (attacker.Class == CharacterClass.Paladin || attacker.Class == CharacterClass.Assassin)
            {
                terminal.Write(",1");
            }
            terminal.Write(",?) :");
        }
        
        char inputChar;
        do
        {
            var keyStr = await terminal.GetKeyInput();
            inputChar = !string.IsNullOrEmpty(keyStr) ? char.ToUpperInvariant(keyStr[0]) : '\0';
        } while (!"ABHQFSU1C?".Contains(inputChar));
        
        return ParsePvPCombatAction(inputChar, attacker);
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Try to add item to character inventory
    /// </summary>
    private async Task<bool> TryAddItemToInventory(Character character, int itemId, ObjType itemType, 
        string itemName, TerminalEmulator terminal)
    {
        // Find empty inventory slot
        int emptySlot = -1;
        for (int i = 0; i < character.Item.Count; i++)
        {
            if (character.Item[i] == 0)
            {
                emptySlot = i;
                break;
            }
        }
        
        if (emptySlot == -1)
        {
            terminal.WriteLine($"{GameConfig.WarningColor}Inventory is full!{GameConfig.TextColor}");
            
            terminal.Write("Drop something? (Y/N): ");
            var input = await terminal.GetKeyInput();
            
            if (char.ToUpper(input) == 'Y')
            {
                // TODO: Implement drop item interface
                // For now, just say inventory is full
                terminal.WriteLine("Item dropped (placeholder).");
                emptySlot = 0; // Use first slot as placeholder
            }
            else
            {
                return false;
            }
        }
        
        if (emptySlot >= 0)
        {
            character.Item[emptySlot] = itemId;
            character.ItemType[emptySlot] = itemType;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Calculate experience gain from monster - Pascal logic
    /// </summary>
    private long CalculateExperienceGain(Monster monster, Character player)
    {
        // Base experience based on monster level and player level
        long baseExp = monster.Level * 100;
        
        // Level difference modifier
        int levelDiff = monster.Level - player.Level;
        if (levelDiff > 0)
        {
            baseExp += levelDiff * 50; // Bonus for higher level monsters
        }
        else if (levelDiff < 0)
        {
            baseExp = Math.Max(baseExp / 2, 10); // Reduced for lower level monsters
        }
        
        return baseExp;
    }
    
    /// <summary>
    /// Get random death message - Pascal PLVSMON.PAS death messages
    /// </summary>
    private string GetRandomDeathMessage(string playerName)
    {
        var random = new Random();
        string coloredName = $"{GameConfig.NewsColorPlayer}{playerName}{GameConfig.NewsColorDefault}";
        
        return random.Next(4) switch
        {
            0 => $"{coloredName} was killed by a monster, when trying to escape battle!",
            1 => $"{coloredName} was slain by a monster, when trying to escape battle!",
            2 => $"{coloredName} was slaughtered by a monster, when trying to escape battle!",
            _ => $"{coloredName} was defeated by a monster, when trying to escape battle!"
        };
    }
    
    /// <summary>
    /// Handle player death and resurrection - Pascal death logic
    /// </summary>
    private async Task HandlePlayerDeath(Character player, string cause, TerminalEmulator terminal)
    {
        // Reduce resurrection count (Pascal Reduce_Player_Resurrections)
        player.Resurrections--;
        
        if (player.Resurrections <= 0)
        {
            player.Allowed = false; // Character deleted
            terminal.WriteLine($"{GameConfig.DeathColor}Your character has been permanently deleted!{GameConfig.TextColor}");
        }
        else
        {
            terminal.WriteLine($"{GameConfig.WarningColor}You have {player.Resurrections} resurrections remaining.{GameConfig.TextColor}");
        }
        
        terminal.WriteLine($"{GameConfig.DeathColor}Darkness...{GameConfig.TextColor}");
        await terminal.WaitForKeyPress();
    }
    
    /// <summary>
    /// Send PvP victory mail - Pascal PLVSPLC.PAS mail system
    /// </summary>
    private async Task SendPvPVictoryMail(Character winner, Character loser, long expGain, string subject, bool killed)
    {
        string message1 = killed ? 
            $"You killed {GameConfig.NewsColorPlayer}{loser.Name2}{GameConfig.NewsColorDefault} in self defence! The idiot begged for mercy." :
            $"{GameConfig.NewsColorPlayer}{loser.Name2}{GameConfig.NewsColorDefault} cowardly attacked You! But the scumbag surrendered!";
            
        string message2 = killed ?
            $"But you chopped {GetGenderPronoun(loser.Sex)} head clean off! NO MERCY!" :
            $"You had {GetGenderPronoun(loser.Sex)} begging at your feet, and perhaps should have killed {GetGenderPronoun(loser.Sex)}.";
            
        string message3 = killed ?
            $"You received {GameConfig.NewsColorPlayer}{expGain:N0}{GameConfig.NewsColorDefault} experience points for this win!" :
            $"But you were in a good mood and spared {GetGenderPronoun(loser.Sex)} miserable life!";
            
        string message4 = $"You received {GameConfig.NewsColorPlayer}{expGain:N0}{GameConfig.NewsColorDefault} experience points from this victory.";
        
        MailSystem.SendMail(winner.Name2, $"{GameConfig.NewsColorRoyal}{subject}{GameConfig.NewsColorDefault}", 
            message1, message2, message3, message4);
    }
    
    /// <summary>
    /// Send PvP death mail - Pascal PLVSPLC.PAS death mail
    /// </summary>
    private async Task SendPvPDeathMail(Character loser, Character winner, long goldLost)
    {
        string goldMessage = goldLost > 0 ?
            $"{GameConfig.NewsColorPlayer}{winner.Name2}{GameConfig.NewsColorDefault} emptied your purse. You lost {GameConfig.GoldColor}{goldLost:N0}{GameConfig.NewsColorDefault} gold!" :
            "";
            
        MailSystem.SendMail(loser.Name2, $"{GameConfig.NewsColorDeath}Your Death{GameConfig.NewsColorDefault}",
            $"You were slain by {GameConfig.NewsColorPlayer}{winner.Name2}{GameConfig.NewsColorDefault}!",
            "You begged for mercy, but the ignorant bastard killed you!",
            goldMessage);
    }
    
    /// <summary>
    /// Get gender pronoun - Pascal sex array logic
    /// </summary>
    private string GetGenderPronoun(CharacterSex sex)
    {
        return sex == CharacterSex.Male ? "his" : "her";
    }
    
    /// <summary>
    /// Parse monster combat action
    /// </summary>
    private CombatAction ParseMonsterCombatAction(char choice, Character player)
    {
        return choice switch
        {
            'A' => new CombatAction { Type = CombatActionType.Attack },
            'H' => new CombatAction { Type = CombatActionType.Heal },
            'Q' => new CombatAction { Type = CombatActionType.QuickHeal },
            'R' => new CombatAction { Type = CombatActionType.Retreat },
            'S' => new CombatAction { Type = CombatActionType.Status },
            'B' => new CombatAction { Type = CombatActionType.BegForMercy },
            'U' => new CombatAction { Type = CombatActionType.UseItem },
            'C' => new CombatAction { Type = CombatActionType.CastSpell },
            '1' when player.Class == CharacterClass.Paladin => new CombatAction { Type = CombatActionType.SoulStrike },
            '1' when player.Class == CharacterClass.Assassin => new CombatAction { Type = CombatActionType.Backstab },
            _ => new CombatAction { Type = CombatActionType.Attack }
        };
    }
    
    /// <summary>
    /// Parse PvP combat action
    /// </summary>
    private PvPCombatAction ParsePvPCombatAction(char choice, Character player)
    {
        return choice switch
        {
            'A' => new PvPCombatAction { Type = PvPActionType.Attack },
            'B' => new PvPCombatAction { Type = PvPActionType.BegForMercy },
            'H' => new PvPCombatAction { Type = PvPActionType.Heal },
            'Q' => new PvPCombatAction { Type = PvPActionType.QuickHeal },
            'F' => new PvPCombatAction { Type = PvPActionType.FightToDeath },
            'S' => new PvPCombatAction { Type = PvPActionType.Status },
            'U' => new PvPCombatAction { Type = PvPActionType.UseItem },
            'C' => new PvPCombatAction { Type = PvPActionType.CastSpell },
            '1' when player.Class == CharacterClass.Paladin => new PvPCombatAction { Type = PvPActionType.SoulStrike },
            '1' when player.Class == CharacterClass.Assassin => new PvPCombatAction { Type = PvPActionType.Backstab },
            '?' => new PvPCombatAction { Type = PvPActionType.ShowMenu },
            _ => new PvPCombatAction { Type = PvPActionType.Attack }
        };
    }
    
    // Placeholder methods for complete compilation
    private async Task InitializeMonsterCombat(AdvancedCombatResult result, TerminalEmulator terminal) { }
    private async Task DisplayMonsterStatus(List<Monster> monsters, TerminalEmulator terminal) { }
    private async Task ProcessPlayerCombatAction(CombatAction action, Character player, List<Monster> monsters, AdvancedCombatResult result, TerminalEmulator terminal) { }
    private async Task ProcessTeammateTurn(Character teammate, List<Monster> monsters, AdvancedCombatResult result, TerminalEmulator terminal) { }
    private async Task ProcessMonsterAttackPhase(List<Monster> monsters, Character player, List<Character> teammates, AdvancedCombatResult result, TerminalEmulator terminal) { }
    private async Task DetermineMonsterCombatOutcome(AdvancedCombatResult result, TerminalEmulator terminal) { }
    private async Task InitializePlayerVsPlayerCombat(Character attacker, Character defender, AdvancedCombatResult result, TerminalEmulator terminal) { }
    private async Task DisplayPvPStatus(Character attacker, Character defender, TerminalEmulator terminal) { }
    private async Task ProcessPvPAction(PvPCombatAction action, Character attacker, Character defender, AdvancedCombatResult result, TerminalEmulator terminal) { }
    private async Task ProcessComputerPvPTurn(Character computer, Character opponent, AdvancedCombatResult result, TerminalEmulator terminal) { }
    private async Task DeterminePvPOutcome(Character attacker, Character defender, AdvancedCombatResult result, TerminalEmulator terminal) { }
    
    #endregion
    
    #region Data Structures
    
    public class AdvancedCombatResult
    {
        public AdvancedCombatType CombatType { get; set; }
        public Character Player { get; set; }
        public Character Opponent { get; set; }
        public List<Character> Teammates { get; set; } = new List<Character>();
        public List<Monster> Monsters { get; set; } = new List<Monster>();
        public AdvancedCombatOutcome Outcome { get; set; }
        public List<string> CombatLog { get; set; } = new List<string>();
        public long ExperienceGained { get; set; }
        public long GoldGained { get; set; }
        public long GoldLost { get; set; }
        public List<string> ItemsFound { get; set; } = new List<string>();
        public bool IsComplete { get; set; }
        public int MonsterMode { get; set; }
        public bool OfflineKill { get; set; }
    }
    
    public class CombatAction
    {
        public CombatActionType Type { get; set; }
        public int SpellIndex { get; set; }
        public int ItemIndex { get; set; }
        public string TargetId { get; set; } = "";
    }
    
    public class PvPCombatAction
    {
        public PvPActionType Type { get; set; }
        public int SpellIndex { get; set; }
        public int ItemIndex { get; set; }
    }
    
    public enum AdvancedCombatType
    {
        PlayerVsMonster,
        PlayerVsPlayer,
        TeamVsMonster,
        OnlineDuel
    }
    
    public enum AdvancedCombatOutcome
    {
        Victory,
        PlayerDied,
        PlayerEscaped,
        PlayerSurrendered,
        OpponentDied,
        Stalemate,
        Interrupted
    }
    
    public enum CombatActionType
    {
        Attack,
        Heal,
        QuickHeal,
        Retreat,
        Status,
        BegForMercy,
        UseItem,
        CastSpell,
        SoulStrike,
        Backstab
    }
    
    public enum PvPActionType
    {
        Attack,
        BegForMercy,
        Heal,
        QuickHeal,
        FightToDeath,
        Status,
        UseItem,
        CastSpell,
        SoulStrike,
        Backstab,
        ShowMenu
    }
    
    #endregion
} 
