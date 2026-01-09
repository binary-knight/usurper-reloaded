using UsurperRemake.Utils;
using UsurperRemake.Systems;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Anchor Road Location - Complete implementation based on Pascal CHALLENG.PAS
/// Central challenge hub providing access to various adventure activities including tournaments, gang wars,
/// bounty hunting, quests, and gym competitions
/// Direct Pascal compatibility with exact function preservation
/// </summary>
public class AnchorRoadLocation : BaseLocation
{
    private Random random = new Random();

    public AnchorRoadLocation() : base(GameLocation.AnchorRoad, "Anchor Road", "Conjunction of Destinies")
    {
    }

    protected override void SetupLocation()
    {
        PossibleExits = new List<GameLocation>
        {
            GameLocation.MainStreet,
            GameLocation.Castle
        };

        LocationActions = new List<string>
        {
            "Dormitory",
            "Bounty Hunting",
            "Quests",
            "Gang War",
            "Online War",
            "Altar of the Gods",
            "Claim Town",
            "Flee Town Control",
            "Status",
            "Kings Castle",
            "Prison Grounds"
        };
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();

        // Header with proper coloring
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                    ANCHOR ROAD - Conjunction of Destinies                  ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Atmospheric description
        terminal.SetColor("white");
        terminal.WriteLine("To the north you can see the Castle in all its might.");
        terminal.WriteLine("The Dormitory lies to the west.");
        terminal.WriteLine("The Red Fields are to the east.");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("It's time to be brave.");
        terminal.WriteLine("");

        // Show current status
        ShowChallengeStatus();
        terminal.WriteLine("");

        // Menu options with proper coloring
        terminal.SetColor("cyan");
        terminal.WriteLine("Challenges:");
        terminal.SetColor("white");
        WriteMenuRow("D", "Dormitory", "B", "Bounty Board", "Q", "Quest Hall");
        WriteMenuRow("G", "Gang War", "O", "Online War", "A", "Altar of the Gods");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("Town Control:");
        terminal.SetColor("white");
        WriteMenuRow("C", "Claim Town", "F", "Flee Town Control", "", "");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("Navigation:");
        terminal.SetColor("white");
        WriteMenuRow("S", "Status", "K", "Kings Castle", "", "");
        terminal.SetColor("white");
        WriteMenuOption("P", "Prison Grounds (attempt a jailbreak)");
        terminal.WriteLine("");
        WriteMenuOption("R", "Return to town");
        terminal.WriteLine("");
    }

    private void WriteMenuRow(string key1, string label1, string key2, string label2, string key3, string label3)
    {
        if (!string.IsNullOrEmpty(key1))
        {
            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_cyan");
            terminal.Write(key1);
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.Write(label1.PadRight(18));
        }

        if (!string.IsNullOrEmpty(key2))
        {
            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_cyan");
            terminal.Write(key2);
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.Write(label2.PadRight(18));
        }

        if (!string.IsNullOrEmpty(key3))
        {
            terminal.SetColor("darkgray");
            terminal.Write("[");
            terminal.SetColor("bright_cyan");
            terminal.Write(key3);
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.Write(label3);
        }
        terminal.WriteLine("");
    }

    private void WriteMenuOption(string key, string label)
    {
        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write(key);
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine(label);
    }

    private void ShowChallengeStatus()
    {
        terminal.SetColor("darkgray");
        terminal.WriteLine("─────────────────────────────────────────");

        // Show player fights remaining
        terminal.SetColor("white");
        terminal.Write("Player Fights: ");
        if (currentPlayer.PFights > 0)
        {
            terminal.SetColor("bright_green");
            terminal.WriteLine($"{currentPlayer.PFights}");
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("0 (exhausted)");
        }

        // Show team fights if in a team
        if (!string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.SetColor("white");
            terminal.Write("Team Fights: ");
            if (currentPlayer.TFights > 0)
            {
                terminal.SetColor("bright_green");
                terminal.WriteLine($"{currentPlayer.TFights}");
            }
            else
            {
                terminal.SetColor("red");
                terminal.WriteLine("0 (exhausted)");
            }

            terminal.SetColor("white");
            terminal.Write("Your Team: ");
            terminal.SetColor("bright_cyan");
            terminal.Write(currentPlayer.Team);

            if (currentPlayer.CTurf)
            {
                terminal.SetColor("bright_yellow");
                terminal.WriteLine(" * CONTROLS TOWN");
            }
            else
            {
                terminal.WriteLine("");
            }
        }

        // Show who controls the town
        var turfController = GetTurfControllerName();
        if (!string.IsNullOrEmpty(turfController))
        {
            terminal.SetColor("white");
            terminal.Write("Town Controlled By: ");
            terminal.SetColor("bright_yellow");
            terminal.WriteLine(turfController);
        }

        terminal.SetColor("darkgray");
        terminal.WriteLine("─────────────────────────────────────────");
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(choice))
            return false;

        char ch = char.ToUpperInvariant(choice.Trim()[0]);

        switch (ch)
        {
            case 'D':
                await NavigateToDormitory();
                return false;

            case 'B':
                // Redirect to Quest Hall for bounty board
                await NavigateToLocation(GameLocation.QuestHall);
                return true;

            case 'Q':
                // Redirect to Quest Hall for proper quest management
                await NavigateToLocation(GameLocation.QuestHall);
                return true;

            case 'G':
                await StartGangWar();
                return false;

            case 'O':
                await StartOnlineWar();
                return false;

            case 'A':
                await NavigateToLocation(GameLocation.Temple);
                return true;

            case 'C':
                await ClaimTown();
                return false;

            case 'F':
                await FleeTownControl();
                return false;

            case 'S':
                await ShowStatus();
                return false;

            case 'K':
                await NavigateToLocation(GameLocation.Castle);
                return true;

            case 'P':
                await NavigateToPrisonGrounds();
                return false;

            case 'R':
                await NavigateToLocation(GameLocation.MainStreet);
                return true;

            case '?':
                return false;

            default:
                terminal.SetColor("red");
                terminal.WriteLine("Invalid choice! Press ? for menu.");
                await Task.Delay(1500);
                return false;
        }
    }

    #region Challenge Implementations

    /// <summary>
    /// Navigate to dormitory (character rest/management area)
    /// </summary>
    private async Task NavigateToDormitory()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                              THE DORMITORY                                  ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("A quiet place where adventurers rest between battles.");
        terminal.WriteLine("You find a comfortable bed and consider your options...");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("Would you like to:");
        terminal.SetColor("white");
        WriteMenuOption("R", "Rest and recover (costs 1 turn)");
        WriteMenuOption("V", "View your stats");
        WriteMenuOption("L", "Leave");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.Write("Choice: ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (!string.IsNullOrEmpty(input))
        {
            char dormChoice = char.ToUpperInvariant(input[0]);
            switch (dormChoice)
            {
                case 'R':
                    // Rest to recover HP
                    long healAmount = currentPlayer.MaxHP / 4;
                    currentPlayer.HP = Math.Min(currentPlayer.HP + healAmount, currentPlayer.MaxHP);
                    terminal.SetColor("bright_green");
                    terminal.WriteLine($"You rest peacefully and recover {healAmount} HP.");
                    terminal.WriteLine($"HP: {currentPlayer.HP}/{currentPlayer.MaxHP}");
                    break;

                case 'V':
                    await ShowStatus();
                    break;
            }
        }

        terminal.SetColor("darkgray");
        terminal.WriteLine("");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Start bounty hunting - hunt for criminals/NPCs with bounties
    /// </summary>
    private async Task StartBountyHunting()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_red");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                              BOUNTY HUNTING                                 ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        if (currentPlayer.PFights <= 0)
        {
            terminal.SetColor("red");
            terminal.WriteLine("You have no player fights left today!");
            terminal.WriteLine("Come back tomorrow when you're rested.");
            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        terminal.SetColor("white");
        terminal.WriteLine("You scan the bounty board for wanted criminals...");
        terminal.WriteLine("");

        // Get NPCs with high darkness (evil NPCs) as bounty targets
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        var bountyTargets = allNPCs
            .Where(n => n.IsAlive && n.Darkness > 200)
            .OrderByDescending(n => n.Darkness * 10)
            .Take(5)
            .ToList();

        if (bountyTargets.Count == 0)
        {
            // Generate some random bounty targets
            bountyTargets = allNPCs
                .Where(n => n.IsAlive && n.Level <= currentPlayer.Level + 5)
                .OrderBy(_ => random.Next())
                .Take(3)
                .ToList();

            if (bountyTargets.Count == 0)
            {
                terminal.SetColor("yellow");
                terminal.WriteLine("No bounties available at this time.");
                terminal.WriteLine("");
                terminal.SetColor("darkgray");
                terminal.WriteLine("Press any key to continue...");
                await terminal.ReadKeyAsync();
                return;
            }
        }

        terminal.SetColor("bright_yellow");
        terminal.WriteLine("WANTED - DEAD OR ALIVE:");
        terminal.SetColor("darkgray");
        terminal.WriteLine(new string('─', 60));
        terminal.SetColor("white");
        terminal.WriteLine($"{"#",-3} {"Name",-20} {"Level",-6} {"Bounty",-12} {"Crime",-15}");
        terminal.SetColor("darkgray");
        terminal.WriteLine(new string('─', 60));

        for (int i = 0; i < bountyTargets.Count; i++)
        {
            var target = bountyTargets[i];
            // Calculate bounty based on level and darkness (evil) rating
            long bounty = target.Level * 100 + (long)target.Darkness;
            string crime = target.Darkness > 500 ? "Murder" :
                          target.Darkness > 200 ? "Assault" : "Troublemaker";

            terminal.SetColor("white");
            terminal.WriteLine($"{i + 1,-3} {target.DisplayName,-20} {target.Level,-6} {bounty:N0}g{"",-5} {crime,-15}");
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Hunt which target? (0 to cancel): ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= bountyTargets.Count)
        {
            var target = bountyTargets[choice - 1];
            currentPlayer.PFights--;

            terminal.WriteLine("");
            terminal.SetColor("bright_red");
            terminal.WriteLine($"You track down {target.DisplayName}...");
            await Task.Delay(1000);

            // Simple combat simulation
            bool playerWon = SimulateBountyHuntCombat(currentPlayer, target);

            if (playerWon)
            {
                // Calculate bounty based on level and darkness (evil) rating
                long bounty = target.Level * 100 + (long)target.Darkness;
                long expGain = target.Level * 50;

                terminal.SetColor("bright_green");
                terminal.WriteLine("");
                terminal.WriteLine("═══════════════════════════════════════");
                terminal.WriteLine("           BOUNTY COLLECTED!");
                terminal.WriteLine("═══════════════════════════════════════");
                terminal.WriteLine($"You defeated {target.DisplayName}!");
                terminal.WriteLine($"Bounty Reward: {bounty:N0} gold");
                terminal.WriteLine($"Experience: {expGain:N0}");

                currentPlayer.Gold += bounty;
                currentPlayer.Experience += expGain;
                target.HP = 0; // Target is dead

                // News
                NewsSystem.Instance.Newsy(true, $"{currentPlayer.DisplayName} collected the bounty on {target.DisplayName}!");
            }
            else
            {
                long hpLost = currentPlayer.MaxHP / 4;
                currentPlayer.HP = Math.Max(1, currentPlayer.HP - hpLost);

                terminal.SetColor("red");
                terminal.WriteLine("");
                terminal.WriteLine($"{target.DisplayName} escaped!");
                terminal.WriteLine($"You lost {hpLost} HP in the fight.");
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Simple bounty hunt combat simulation
    /// </summary>
    private bool SimulateBountyHuntCombat(Character hunter, NPC target)
    {
        long hunterPower = hunter.Strength + hunter.WeapPow + hunter.Level * 5;
        long targetPower = target.Strength + target.WeapPow + target.Level * 5;

        // Add randomness
        hunterPower += random.Next(-20, 21);
        targetPower += random.Next(-20, 21);

        return hunterPower > targetPower;
    }

    /// <summary>
    /// Start quests - view and accept available quests
    /// </summary>
    private async Task StartQuests()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                               ROYAL QUESTS                                  ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("The royal quest board displays available missions...");
        terminal.WriteLine("");

        // Get available quests
        var availableQuests = QuestSystem.GetAvailableQuests(currentPlayer);

        if (availableQuests.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("No quests are currently available.");
            terminal.WriteLine("Check back later or visit the King's Castle.");
            terminal.WriteLine("");

            // Show player's active quests
            var playerQuests = QuestSystem.GetPlayerQuests(currentPlayer.Name2);
            if (playerQuests.Count > 0)
            {
                terminal.SetColor("cyan");
                terminal.WriteLine("Your Active Quests:");
                terminal.SetColor("darkgray");
                terminal.WriteLine(new string('─', 50));

                foreach (var quest in playerQuests)
                {
                    terminal.SetColor("white");
                    terminal.WriteLine($"  - {quest.Title ?? quest.GetTargetDescription()}");
                    terminal.SetColor("darkgray");
                    terminal.WriteLine($"    Days remaining: {quest.DaysToComplete - quest.OccupiedDays}");
                }
            }
        }
        else
        {
            terminal.SetColor("cyan");
            terminal.WriteLine("Available Quests:");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 60));
            terminal.SetColor("white");
            terminal.WriteLine($"{"#",-3} {"Quest",-30} {"Difficulty",-12} {"Reward",-10}");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 60));

            for (int i = 0; i < Math.Min(availableQuests.Count, 5); i++)
            {
                var quest = availableQuests[i];
                string difficulty = quest.GetDifficultyString();
                string rewardType = quest.RewardType.ToString();

                terminal.SetColor("white");
                terminal.WriteLine($"{i + 1,-3} {quest.GetTargetDescription(),-30} {difficulty,-12} {rewardType,-10}");
            }

            terminal.WriteLine("");
            terminal.SetColor("cyan");
            terminal.Write("Accept which quest? (0 to cancel): ");
            terminal.SetColor("white");
            string input = await terminal.ReadLineAsync();

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= availableQuests.Count)
            {
                var selectedQuest = availableQuests[choice - 1];
                var playerObj = currentPlayer as Player ?? new Player { Name2 = currentPlayer.Name2, Level = currentPlayer.Level };
                var result = QuestSystem.ClaimQuest(playerObj, selectedQuest);

                if (result == QuestClaimResult.CanClaim)
                {
                    terminal.SetColor("bright_green");
                    terminal.WriteLine("");
                    terminal.WriteLine($"Quest accepted: {selectedQuest.GetTargetDescription()}");
                    terminal.WriteLine($"You have {selectedQuest.DaysToComplete} days to complete it.");
                }
                else
                {
                    terminal.SetColor("red");
                    terminal.WriteLine($"Could not accept quest: {result}");
                }
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Start gang war - team vs team combat
    /// </summary>
    private async Task StartGangWar()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_red");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                                GANG WAR                                     ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        if (string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.SetColor("red");
            terminal.WriteLine("You must be in a team to participate in gang wars!");
            terminal.WriteLine("Visit Team Corner at the Inn to create or join a team.");
            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        if (currentPlayer.TFights <= 0)
        {
            terminal.SetColor("red");
            terminal.WriteLine("You have no team fights left today!");
            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        // Get all active teams
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        var teams = allNPCs
            .Where(n => !string.IsNullOrEmpty(n.Team) && n.IsAlive && n.Team != currentPlayer.Team)
            .GroupBy(n => n.Team)
            .Select(g => new
            {
                TeamName = g.Key,
                MemberCount = g.Count(),
                TotalPower = g.Sum(m => m.Level + (int)m.Strength + (int)m.Defence),
                ControlsTurf = g.Any(m => m.CTurf)
            })
            .OrderByDescending(t => t.TotalPower)
            .ToList();

        if (teams.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("No rival teams found to challenge!");
            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        terminal.SetColor("white");
        terminal.WriteLine($"Your Team: {currentPlayer.Team}");
        terminal.WriteLine($"Team Fights Remaining: {currentPlayer.TFights}");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("Rival Teams:");
        terminal.SetColor("darkgray");
        terminal.WriteLine(new string('─', 55));
        terminal.SetColor("white");
        terminal.WriteLine($"{"#",-3} {"Team Name",-24} {"Members",-8} {"Power",-8} {"Turf",-5}");
        terminal.SetColor("darkgray");
        terminal.WriteLine(new string('─', 55));

        for (int i = 0; i < teams.Count; i++)
        {
            var team = teams[i];
            string turfMark = team.ControlsTurf ? "*" : "-";

            if (team.ControlsTurf)
                terminal.SetColor("bright_yellow");
            else
                terminal.SetColor("white");

            terminal.WriteLine($"{i + 1,-3} {team.TeamName,-24} {team.MemberCount,-8} {team.TotalPower,-8} {turfMark,-5}");
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Challenge which team? (0 to cancel): ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= teams.Count)
        {
            var targetTeam = teams[choice - 1];
            currentPlayer.TFights--;

            terminal.WriteLine("");
            terminal.SetColor("bright_red");
            terminal.WriteLine($"Your team challenges {targetTeam.TeamName}!");
            terminal.WriteLine("");
            await Task.Delay(1000);

            // Get team members for battle
            var playerTeamMembers = allNPCs
                .Where(n => n.Team == currentPlayer.Team && n.IsAlive)
                .ToList();

            var enemyTeamMembers = allNPCs
                .Where(n => n.Team == targetTeam.TeamName && n.IsAlive)
                .ToList();

            // Calculate team powers
            long playerTeamPower = (long)currentPlayer.Strength + currentPlayer.WeapPow + currentPlayer.Level * 10;
            playerTeamPower += playerTeamMembers.Sum(m => m.Level + (int)m.Strength + (int)m.Defence);

            long enemyTeamPower = enemyTeamMembers.Sum(m => m.Level + (int)m.Strength + (int)m.Defence);

            // Add randomness
            playerTeamPower += random.Next(-50, 51);
            enemyTeamPower += random.Next(-50, 51);

            bool playerWon = playerTeamPower > enemyTeamPower;

            // Display battle
            terminal.SetColor("white");
            terminal.WriteLine("The battle rages...");
            await Task.Delay(500);
            terminal.WriteLine($"Your team power: {playerTeamPower}");
            terminal.WriteLine($"Enemy team power: {enemyTeamPower}");
            await Task.Delay(1000);

            if (playerWon)
            {
                long goldGain = targetTeam.TotalPower * 10;
                long expGain = targetTeam.TotalPower * 5;

                terminal.WriteLine("");
                terminal.SetColor("bright_green");
                terminal.WriteLine("═══════════════════════════════════════");
                terminal.WriteLine("              VICTORY!");
                terminal.WriteLine("═══════════════════════════════════════");
                terminal.WriteLine($"Your team defeated {targetTeam.TeamName}!");
                terminal.WriteLine($"Gold Plundered: {goldGain:N0}");
                terminal.WriteLine($"Experience: {expGain:N0}");

                currentPlayer.Gold += goldGain;
                currentPlayer.Experience += expGain;

                // Handle turf transfer
                if (targetTeam.ControlsTurf)
                {
                    terminal.SetColor("bright_yellow");
                    terminal.WriteLine("");
                    terminal.WriteLine("* YOUR TEAM NOW CONTROLS THE TOWN! *");

                    // Transfer turf control
                    foreach (var enemy in enemyTeamMembers)
                    {
                        enemy.CTurf = false;
                    }
                    currentPlayer.CTurf = true;
                    foreach (var ally in playerTeamMembers)
                    {
                        ally.CTurf = true;
                    }
                }

                NewsSystem.Instance.Newsy(true, $"Gang War! {currentPlayer.Team} defeated {targetTeam.TeamName}!");
            }
            else
            {
                long hpLost = currentPlayer.MaxHP / 3;
                currentPlayer.HP = Math.Max(1, currentPlayer.HP - hpLost);

                terminal.WriteLine("");
                terminal.SetColor("red");
                terminal.WriteLine("═══════════════════════════════════════");
                terminal.WriteLine("              DEFEAT!");
                terminal.WriteLine("═══════════════════════════════════════");
                terminal.WriteLine($"Your team was defeated by {targetTeam.TeamName}!");
                terminal.WriteLine($"You lost {hpLost} HP.");

                NewsSystem.Instance.Newsy(true, $"Gang War! {targetTeam.TeamName} repelled {currentPlayer.Team}!");
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Start online war - PvP combat against other players/NPCs
    /// </summary>
    private async Task StartOnlineWar()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                               ONLINE WAR                                    ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        if (currentPlayer.PFights <= 0)
        {
            terminal.SetColor("red");
            terminal.WriteLine("You have no player fights left today!");
            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        terminal.SetColor("white");
        terminal.WriteLine("You seek worthy opponents for combat...");
        terminal.WriteLine($"Player Fights Remaining: {currentPlayer.PFights}");
        terminal.WriteLine("");

        // Get potential opponents (NPCs around player's level)
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        var opponents = allNPCs
            .Where(n => n.IsAlive &&
                   n.Level >= currentPlayer.Level - 5 &&
                   n.Level <= currentPlayer.Level + 10)
            .OrderBy(n => Math.Abs(n.Level - currentPlayer.Level))
            .Take(8)
            .ToList();

        if (opponents.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("No worthy opponents found at this time.");
            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        terminal.SetColor("cyan");
        terminal.WriteLine("Available Opponents:");
        terminal.SetColor("darkgray");
        terminal.WriteLine(new string('─', 65));
        terminal.SetColor("white");
        terminal.WriteLine($"{"#",-3} {"Name",-20} {"Class",-12} {"Level",-6} {"Team",-15}");
        terminal.SetColor("darkgray");
        terminal.WriteLine(new string('─', 65));

        for (int i = 0; i < opponents.Count; i++)
        {
            var opp = opponents[i];
            string team = string.IsNullOrEmpty(opp.Team) ? "-" : opp.Team;
            if (team.Length > 14) team = team.Substring(0, 14);

            terminal.SetColor("white");
            terminal.WriteLine($"{i + 1,-3} {opp.DisplayName,-20} {opp.Class,-12} {opp.Level,-6} {team,-15}");
        }

        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.Write("Challenge which opponent? (0 to cancel): ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= opponents.Count)
        {
            var opponent = opponents[choice - 1];
            currentPlayer.PFights--;

            terminal.WriteLine("");
            terminal.SetColor("bright_magenta");
            terminal.WriteLine($"You challenge {opponent.DisplayName} to combat!");
            terminal.WriteLine("");

            // Combat simulation
            await SimulatePvPCombat(currentPlayer, opponent);
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Simulate PvP combat with display
    /// </summary>
    private async Task SimulatePvPCombat(Character player, NPC opponent)
    {
        long playerHP = player.HP;
        long opponentHP = opponent.HP;
        int round = 0;

        terminal.SetColor("white");
        terminal.WriteLine($"Your HP: {playerHP}/{player.MaxHP}");
        terminal.WriteLine($"{opponent.DisplayName}'s HP: {opponentHP}/{opponent.MaxHP}");
        terminal.WriteLine("");

        while (playerHP > 0 && opponentHP > 0 && round < 10)
        {
            round++;
            terminal.SetColor("darkgray");
            terminal.WriteLine($"--- Round {round} ---");

            // Player attacks
            long playerDamage = Math.Max(1, player.Strength + player.WeapPow - opponent.Defence);
            playerDamage += random.Next(1, (int)Math.Max(2, player.WeapPow / 3));
            opponentHP -= playerDamage;

            terminal.SetColor("bright_green");
            terminal.WriteLine($"You deal {playerDamage} damage!");

            if (opponentHP <= 0)
            {
                opponentHP = 0;
                break;
            }

            // Opponent attacks
            long oppDamage = Math.Max(1, opponent.Strength + opponent.WeapPow - player.Defence - player.ArmPow);
            oppDamage += random.Next(1, (int)Math.Max(2, opponent.WeapPow / 3));
            playerHP -= oppDamage;

            terminal.SetColor("red");
            terminal.WriteLine($"{opponent.DisplayName} deals {oppDamage} damage!");

            terminal.SetColor("white");
            terminal.WriteLine($"HP: You {Math.Max(0, playerHP)} | {opponent.DisplayName} {Math.Max(0, opponentHP)}");

            await Task.Delay(300);
        }

        terminal.WriteLine("");

        if (opponentHP <= 0)
        {
            // Player won
            long expGain = opponent.Level * 100;
            long goldGain = opponent.Gold / 4;

            player.HP = playerHP;
            opponent.HP = 0;

            terminal.SetColor("bright_green");
            terminal.WriteLine("═══════════════════════════════════════");
            terminal.WriteLine("              VICTORY!");
            terminal.WriteLine("═══════════════════════════════════════");
            terminal.WriteLine($"You defeated {opponent.DisplayName}!");
            terminal.WriteLine($"Experience: {expGain:N0}");
            terminal.WriteLine($"Gold Looted: {goldGain:N0}");

            player.Experience += expGain;
            player.Gold += goldGain;
            player.PKills++;

            NewsSystem.Instance.Newsy(true, $"{player.DisplayName} defeated {opponent.DisplayName} in combat!");
        }
        else if (playerHP <= 0)
        {
            // Player lost
            player.HP = 1; // Don't actually kill player

            terminal.SetColor("red");
            terminal.WriteLine("═══════════════════════════════════════");
            terminal.WriteLine("              DEFEAT!");
            terminal.WriteLine("═══════════════════════════════════════");
            terminal.WriteLine($"You were defeated by {opponent.DisplayName}!");
            terminal.WriteLine("You barely escaped with your life...");

            NewsSystem.Instance.Newsy(true, $"{opponent.DisplayName} defeated {player.DisplayName} in combat!");
        }
        else
        {
            // Draw
            player.HP = playerHP;

            terminal.SetColor("yellow");
            terminal.WriteLine("═══════════════════════════════════════");
            terminal.WriteLine("                DRAW!");
            terminal.WriteLine("═══════════════════════════════════════");
            terminal.WriteLine("Neither combatant could claim victory.");
        }
    }

    /// <summary>
    /// Claim town for your team
    /// </summary>
    private async Task ClaimTown()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                              CLAIM TOWN                                     ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        if (string.IsNullOrEmpty(currentPlayer.Team))
        {
            terminal.SetColor("red");
            terminal.WriteLine("You must be in a team to claim towns!");
            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        if (currentPlayer.CTurf)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("Your team already controls this town!");
            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.WriteLine("Press any key to continue...");
            await terminal.ReadKeyAsync();
            return;
        }

        // Check if anyone controls the town
        var turfController = GetTurfControllerName();

        if (string.IsNullOrEmpty(turfController))
        {
            // Nobody controls - easy claim
            terminal.SetColor("white");
            terminal.WriteLine("No team currently controls this town.");
            terminal.WriteLine("");
            terminal.SetColor("cyan");
            terminal.Write("Claim the town for your team? (Y/N): ");
            terminal.SetColor("white");
            string response = await terminal.ReadLineAsync();

            if (response?.ToUpper().StartsWith("Y") == true)
            {
                currentPlayer.CTurf = true;
                currentPlayer.TeamRec = 0;

                // Set for all team NPCs too
                var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
                foreach (var npc in allNPCs.Where(n => n.Team == currentPlayer.Team))
                {
                    npc.CTurf = true;
                }

                terminal.SetColor("bright_green");
                terminal.WriteLine("");
                terminal.WriteLine("* YOUR TEAM NOW CONTROLS THE TOWN! *");
                terminal.WriteLine("Rule wisely, for challengers will come...");

                NewsSystem.Instance.Newsy(true, $"{currentPlayer.Team} has taken control of the town!");
            }
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"The town is currently controlled by: {turfController}");
            terminal.WriteLine("");
            terminal.SetColor("white");
            terminal.WriteLine("To claim the town, you must defeat the controlling team in Gang War!");
            terminal.WriteLine("Use the (G)ang War option to challenge them.");
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Flee/abandon town control
    /// </summary>
    private async Task FleeTownControl()
    {
        if (!currentPlayer.CTurf)
        {
            terminal.SetColor("red");
            terminal.WriteLine("");
            terminal.WriteLine("Your team doesn't control any town!");
            terminal.WriteLine("");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine("");
        terminal.SetColor("yellow");
        terminal.WriteLine("Are you sure you want to abandon town control?");
        terminal.WriteLine("This will leave the town open for other teams to claim.");
        terminal.Write("Abandon control? (Y/N): ");
        terminal.SetColor("white");
        string response = await terminal.ReadLineAsync();

        if (response?.ToUpper().StartsWith("Y") == true)
        {
            // Remove turf control from all team members
            currentPlayer.CTurf = false;

            var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
            foreach (var npc in allNPCs.Where(n => n.Team == currentPlayer.Team))
            {
                npc.CTurf = false;
            }

            terminal.SetColor("bright_green");
            terminal.WriteLine("");
            terminal.WriteLine("Your team has abandoned control of the town.");
            terminal.WriteLine("The town is now free for the taking...");

            NewsSystem.Instance.Newsy(true, $"{currentPlayer.Team} abandoned control of the town!");
        }
        else
        {
            terminal.SetColor("white");
            terminal.WriteLine("Town control maintained.");
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    /// <summary>
    /// Navigate to prison grounds
    /// </summary>
    private async Task NavigateToPrisonGrounds()
    {
        terminal.ClearScreen();
        terminal.SetColor("darkgray");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                             PRISON GROUNDS                                  ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("You approach the forbidding prison walls...");
        terminal.WriteLine("Guards patrol the perimeter, watching for trouble.");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("Options:");
        terminal.SetColor("white");
        WriteMenuOption("J", "Attempt a Jailbreak (rescue a prisoner)");
        WriteMenuOption("V", "View Prisoners");
        WriteMenuOption("L", "Leave");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.Write("Choice: ");
        terminal.SetColor("white");
        string input = await terminal.ReadLineAsync();

        if (!string.IsNullOrEmpty(input))
        {
            char prisonChoice = char.ToUpperInvariant(input[0]);
            switch (prisonChoice)
            {
                case 'J':
                    await AttemptJailbreak();
                    break;

                case 'V':
                    await ViewPrisoners();
                    break;
            }
        }
    }

    private async Task AttemptJailbreak()
    {
        terminal.WriteLine("");
        terminal.SetColor("red");
        terminal.WriteLine("Jailbreaks are extremely dangerous!");
        terminal.WriteLine("You could end up in prison yourself if caught.");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.Write("Proceed with jailbreak? (Y/N): ");
        terminal.SetColor("white");
        string response = await terminal.ReadLineAsync();

        if (response?.ToUpper().StartsWith("Y") == true)
        {
            int successChance = 30 + currentPlayer.Level + (int)(currentPlayer.Agility / 5);
            bool success = random.Next(100) < successChance;

            if (success)
            {
                terminal.SetColor("bright_green");
                terminal.WriteLine("");
                terminal.WriteLine("You sneak past the guards and find a prisoner!");
                terminal.WriteLine("You help them escape through a secret passage.");
                terminal.WriteLine("");
                terminal.WriteLine("The prisoner thanks you and disappears into the night.");

                currentPlayer.Chivalry += 50;
                NewsSystem.Instance.Newsy(true, $"{currentPlayer.DisplayName} orchestrated a daring prison escape!");
            }
            else
            {
                terminal.SetColor("red");
                terminal.WriteLine("");
                terminal.WriteLine("CAUGHT!");
                terminal.WriteLine("The guards spotted you and gave chase!");

                // Damage and possible imprisonment
                long damage = currentPlayer.MaxHP / 5;
                currentPlayer.HP = Math.Max(1, currentPlayer.HP - damage);
                currentPlayer.Darkness += 25;

                terminal.WriteLine($"You barely escaped, losing {damage} HP in the process.");
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    private async Task ViewPrisoners()
    {
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("You peer through the iron bars...");
        terminal.WriteLine("");

        // Get imprisoned NPCs (those with prison status)
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        var prisoners = allNPCs
            .Where(n => n.CurrentLocation == "Prison" || n.PrisonsLeft > 0)
            .Take(5)
            .ToList();

        if (prisoners.Count == 0)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("The prison cells appear to be empty.");
        }
        else
        {
            terminal.SetColor("cyan");
            terminal.WriteLine("Prisoners:");
            terminal.SetColor("darkgray");
            terminal.WriteLine(new string('─', 40));

            foreach (var prisoner in prisoners)
            {
                terminal.SetColor("white");
                terminal.WriteLine($"  {prisoner.DisplayName} - Level {prisoner.Level} {prisoner.Class}");
            }
        }

        terminal.WriteLine("");
        terminal.SetColor("darkgray");
        terminal.WriteLine("Press any key to continue...");
        await terminal.ReadKeyAsync();
    }

    #endregion

    #region Utility Methods

    private string GetTurfControllerName()
    {
        // Check if player controls
        if (currentPlayer.CTurf && !string.IsNullOrEmpty(currentPlayer.Team))
        {
            return currentPlayer.Team;
        }

        // Check NPCs
        var allNPCs = NPCSpawnSystem.Instance.ActiveNPCs;
        var controller = allNPCs.FirstOrDefault(n => n.CTurf && !string.IsNullOrEmpty(n.Team));

        return controller?.Team;
    }

    #endregion
}
