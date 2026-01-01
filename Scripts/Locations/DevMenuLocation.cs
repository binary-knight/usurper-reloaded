using UsurperRemake.Utils;
using UsurperRemake.Systems;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Secret Developer Menu - allows editing character stats, equipment, and game state for testing
/// Protected by passcode "CHEATER"
/// Access by typing "DEV" on Main Street
/// </summary>
public class DevMenuLocation : BaseLocation
{
    private const string PASSCODE = "CHEATER";
    private bool _authenticated = false;

    public DevMenuLocation() : base(
        GameLocation.NoWhere, // Hidden location
        "Developer Menu",
        "A secret place where the fabric of reality can be bent to your will..."
    ) { }

    protected override void SetupLocation()
    {
        PossibleExits = new List<GameLocation> { GameLocation.MainStreet };
        LocationActions = new List<string>();
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                       ★ DEVELOPER MENU ★                                    ║");
        terminal.WriteLine("║                    For Testing Purposes Only                                 ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        if (!_authenticated)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("  This area is restricted. Enter passcode to continue.");
            terminal.WriteLine("");
            return;
        }

        ShowDevMenu();
    }

    private void ShowDevMenu()
    {
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("  ═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                        CHEAT OPTIONS");
        terminal.WriteLine("  ═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  CHARACTER:");
        terminal.SetColor("cyan");
        terminal.WriteLine("    [1] Edit Basic Stats (Str, Dex, Con, Int, Wis, Cha)");
        terminal.WriteLine("    [2] Edit Combat Stats (HP, Mana, WeapPow, ArmPow)");
        terminal.WriteLine("    [3] Edit Resources (Gold, Experience, Level)");
        terminal.WriteLine("    [4] Edit Alignment (Chivalry, Darkness)");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  EQUIPMENT:");
        terminal.SetColor("cyan");
        terminal.WriteLine("    [5] Set Weapon Power");
        terminal.WriteLine("    [6] Set Armor Power");
        terminal.WriteLine("    [7] Give All Equipment");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  GAME STATE:");
        terminal.SetColor("cyan");
        terminal.WriteLine("    [8] Set Story Progress");
        terminal.WriteLine("    [9] Unlock All Seals");
        terminal.WriteLine("    [A] Unlock All Artifacts");
        terminal.WriteLine("    [B] Set Old God Status");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  QUICK CHEATS:");
        terminal.SetColor("yellow");
        terminal.WriteLine("    [G] God Mode (Max everything)");
        terminal.WriteLine("    [H] Full Heal");
        terminal.WriteLine("    [M] Max Gold (1,000,000)");
        terminal.WriteLine("    [L] Level Up (to next level)");
        terminal.WriteLine("    [X] Max Level (100)");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine("    [Q] Return to Main Street");
        terminal.WriteLine("");

        ShowCurrentStats();
    }

    private void ShowCurrentStats()
    {
        terminal.SetColor("dark_cyan");
        terminal.WriteLine("  ═══════════════════════════════════════════════════════════════");
        terminal.SetColor("gray");
        terminal.WriteLine($"  Current: {currentPlayer.DisplayName} | Level {currentPlayer.Level} | HP {currentPlayer.HP}/{currentPlayer.MaxHP}");
        terminal.WriteLine($"  Str:{currentPlayer.Strength} Dex:{currentPlayer.Dexterity} Con:{currentPlayer.Constitution} Int:{currentPlayer.Intelligence} Wis:{currentPlayer.Wisdom} Cha:{currentPlayer.Charisma}");
        terminal.WriteLine($"  Gold:{currentPlayer.Gold:N0} | XP:{currentPlayer.Experience:N0} | WeapPow:{currentPlayer.WeapPow} | ArmPow:{currentPlayer.ArmPow}");
        terminal.WriteLine($"  Chivalry:{currentPlayer.Chivalry} | Darkness:{currentPlayer.Darkness}");
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        if (!_authenticated)
        {
            return await HandleAuthentication(choice);
        }

        var upperChoice = choice.ToUpper().Trim();

        switch (upperChoice)
        {
            case "1":
                await EditBasicStats();
                return false;
            case "2":
                await EditCombatStats();
                return false;
            case "3":
                await EditResources();
                return false;
            case "4":
                await EditAlignment();
                return false;
            case "5":
                await SetWeaponPower();
                return false;
            case "6":
                await SetArmorPower();
                return false;
            case "7":
                await GiveAllEquipment();
                return false;
            case "8":
                await SetStoryProgress();
                return false;
            case "9":
                await UnlockAllSeals();
                return false;
            case "A":
                await UnlockAllArtifacts();
                return false;
            case "B":
                await SetOldGodStatus();
                return false;
            case "G":
                await GodMode();
                return false;
            case "H":
                await FullHeal();
                return false;
            case "M":
                await MaxGold();
                return false;
            case "L":
                await LevelUp();
                return false;
            case "X":
                await MaxLevel();
                return false;
            case "Q":
                terminal.WriteLine("Returning to Main Street...", "cyan");
                await Task.Delay(500);
                throw new LocationExitException(GameLocation.MainStreet);
            default:
                terminal.WriteLine("Invalid option.", "red");
                await Task.Delay(500);
                return false;
        }
    }

    private async Task<bool> HandleAuthentication(string input)
    {
        if (input.ToUpper() == PASSCODE)
        {
            _authenticated = true;
            terminal.WriteLine("Access granted.", "green");
            await Task.Delay(500);
            return false;
        }
        else if (input.ToUpper() == "Q" || string.IsNullOrWhiteSpace(input))
        {
            terminal.WriteLine("Access denied. Returning to Main Street...", "red");
            await Task.Delay(1000);
            throw new LocationExitException(GameLocation.MainStreet);
        }
        else
        {
            terminal.WriteLine("Incorrect passcode.", "red");
            await Task.Delay(500);
            return false;
        }
    }

    private async Task EditBasicStats()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                      EDIT BASIC STATS");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine($"  Current Values:");
        terminal.WriteLine($"  [1] Strength:     {currentPlayer.Strength}");
        terminal.WriteLine($"  [2] Dexterity:    {currentPlayer.Dexterity}");
        terminal.WriteLine($"  [3] Constitution: {currentPlayer.Constitution}");
        terminal.WriteLine($"  [4] Intelligence: {currentPlayer.Intelligence}");
        terminal.WriteLine($"  [5] Wisdom:       {currentPlayer.Wisdom}");
        terminal.WriteLine($"  [6] Charisma:     {currentPlayer.Charisma}");
        terminal.WriteLine($"  [7] Stamina:      {currentPlayer.Stamina}");
        terminal.WriteLine($"  [8] Agility:      {currentPlayer.Agility}");
        terminal.WriteLine("");
        terminal.WriteLine("  [0] Back to Dev Menu");
        terminal.WriteLine("");

        var statChoice = await terminal.GetInput("Which stat to edit? ");

        if (statChoice == "0") return;

        var valueInput = await terminal.GetInput("Enter new value: ");
        if (!long.TryParse(valueInput, out long newValue))
        {
            terminal.WriteLine("Invalid number!", "red");
            await Task.Delay(1000);
            return;
        }

        switch (statChoice)
        {
            case "1":
                currentPlayer.Strength = newValue;
                currentPlayer.BaseStrength = newValue;
                terminal.WriteLine($"Strength set to {newValue}", "green");
                break;
            case "2":
                currentPlayer.Dexterity = newValue;
                currentPlayer.BaseDexterity = newValue;
                terminal.WriteLine($"Dexterity set to {newValue}", "green");
                break;
            case "3":
                currentPlayer.Constitution = newValue;
                currentPlayer.BaseConstitution = newValue;
                terminal.WriteLine($"Constitution set to {newValue}", "green");
                break;
            case "4":
                currentPlayer.Intelligence = newValue;
                currentPlayer.BaseIntelligence = newValue;
                terminal.WriteLine($"Intelligence set to {newValue}", "green");
                break;
            case "5":
                currentPlayer.Wisdom = newValue;
                currentPlayer.BaseWisdom = newValue;
                terminal.WriteLine($"Wisdom set to {newValue}", "green");
                break;
            case "6":
                currentPlayer.Charisma = newValue;
                currentPlayer.BaseCharisma = newValue;
                terminal.WriteLine($"Charisma set to {newValue}", "green");
                break;
            case "7":
                currentPlayer.Stamina = newValue;
                currentPlayer.BaseStamina = newValue;
                terminal.WriteLine($"Stamina set to {newValue}", "green");
                break;
            case "8":
                currentPlayer.Agility = newValue;
                currentPlayer.BaseAgility = newValue;
                terminal.WriteLine($"Agility set to {newValue}", "green");
                break;
            default:
                terminal.WriteLine("Invalid choice!", "red");
                break;
        }

        await Task.Delay(1000);
    }

    private async Task EditCombatStats()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                      EDIT COMBAT STATS");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine($"  Current Values:");
        terminal.WriteLine($"  [1] Current HP:  {currentPlayer.HP}");
        terminal.WriteLine($"  [2] Max HP:      {currentPlayer.MaxHP}");
        terminal.WriteLine($"  [3] Current Mana: {currentPlayer.Mana}");
        terminal.WriteLine($"  [4] Max Mana:    {currentPlayer.MaxMana}");
        terminal.WriteLine($"  [5] Defence:     {currentPlayer.Defence}");
        terminal.WriteLine($"  [6] Weapon Power: {currentPlayer.WeapPow}");
        terminal.WriteLine($"  [7] Armor Power: {currentPlayer.ArmPow}");
        terminal.WriteLine("");
        terminal.WriteLine("  [0] Back to Dev Menu");
        terminal.WriteLine("");

        var statChoice = await terminal.GetInput("Which stat to edit? ");

        if (statChoice == "0") return;

        var valueInput = await terminal.GetInput("Enter new value: ");
        if (!long.TryParse(valueInput, out long newValue))
        {
            terminal.WriteLine("Invalid number!", "red");
            await Task.Delay(1000);
            return;
        }

        switch (statChoice)
        {
            case "1":
                currentPlayer.HP = Math.Min(newValue, currentPlayer.MaxHP);
                terminal.WriteLine($"HP set to {currentPlayer.HP}", "green");
                break;
            case "2":
                currentPlayer.MaxHP = newValue;
                currentPlayer.BaseMaxHP = newValue;
                terminal.WriteLine($"Max HP set to {newValue}", "green");
                break;
            case "3":
                currentPlayer.Mana = Math.Min(newValue, currentPlayer.MaxMana);
                terminal.WriteLine($"Mana set to {currentPlayer.Mana}", "green");
                break;
            case "4":
                currentPlayer.MaxMana = newValue;
                currentPlayer.BaseMaxMana = newValue;
                terminal.WriteLine($"Max Mana set to {newValue}", "green");
                break;
            case "5":
                currentPlayer.Defence = newValue;
                currentPlayer.BaseDefence = newValue;
                terminal.WriteLine($"Defence set to {newValue}", "green");
                break;
            case "6":
                currentPlayer.WeapPow = newValue;
                terminal.WriteLine($"Weapon Power set to {newValue}", "green");
                break;
            case "7":
                currentPlayer.ArmPow = newValue;
                terminal.WriteLine($"Armor Power set to {newValue}", "green");
                break;
            default:
                terminal.WriteLine("Invalid choice!", "red");
                break;
        }

        await Task.Delay(1000);
    }

    private async Task EditResources()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                      EDIT RESOURCES");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine($"  Current Values:");
        terminal.WriteLine($"  [1] Gold:       {currentPlayer.Gold:N0}");
        terminal.WriteLine($"  [2] Bank Gold:  {currentPlayer.BankGold:N0}");
        terminal.WriteLine($"  [3] Experience: {currentPlayer.Experience:N0}");
        terminal.WriteLine($"  [4] Level:      {currentPlayer.Level}");
        terminal.WriteLine($"  [5] Healing Potions: {currentPlayer.Healing}");
        terminal.WriteLine($"  [6] Dungeon Fights:  {currentPlayer.Fights}");
        terminal.WriteLine($"  [7] Player Fights:   {currentPlayer.PFights}");
        terminal.WriteLine("");
        terminal.WriteLine("  [0] Back to Dev Menu");
        terminal.WriteLine("");

        var statChoice = await terminal.GetInput("Which resource to edit? ");

        if (statChoice == "0") return;

        var valueInput = await terminal.GetInput("Enter new value: ");
        if (!long.TryParse(valueInput, out long newValue))
        {
            terminal.WriteLine("Invalid number!", "red");
            await Task.Delay(1000);
            return;
        }

        switch (statChoice)
        {
            case "1":
                currentPlayer.Gold = newValue;
                terminal.WriteLine($"Gold set to {newValue:N0}", "green");
                break;
            case "2":
                currentPlayer.BankGold = newValue;
                terminal.WriteLine($"Bank Gold set to {newValue:N0}", "green");
                break;
            case "3":
                currentPlayer.Experience = newValue;
                terminal.WriteLine($"Experience set to {newValue:N0}", "green");
                break;
            case "4":
                currentPlayer.Level = (int)Math.Min(newValue, GameConfig.MaxLevel);
                terminal.WriteLine($"Level set to {currentPlayer.Level}", "green");
                break;
            case "5":
                currentPlayer.Healing = newValue;
                terminal.WriteLine($"Healing Potions set to {newValue}", "green");
                break;
            case "6":
                currentPlayer.Fights = (int)newValue;
                terminal.WriteLine($"Dungeon Fights set to {newValue}", "green");
                break;
            case "7":
                currentPlayer.PFights = (int)newValue;
                terminal.WriteLine($"Player Fights set to {newValue}", "green");
                break;
            default:
                terminal.WriteLine("Invalid choice!", "red");
                break;
        }

        await Task.Delay(1000);
    }

    private async Task EditAlignment()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                      EDIT ALIGNMENT");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        int netAlignment = (int)(currentPlayer.Chivalry - currentPlayer.Darkness);
        string alignmentStr = netAlignment > 50 ? "Good" : netAlignment < -50 ? "Evil" : "Neutral";

        terminal.SetColor("white");
        terminal.WriteLine($"  Current Alignment: {alignmentStr} ({netAlignment:+#;-#;0})");
        terminal.WriteLine($"  [1] Chivalry (Good): {currentPlayer.Chivalry}");
        terminal.WriteLine($"  [2] Darkness (Evil): {currentPlayer.Darkness}");
        terminal.WriteLine("");
        terminal.WriteLine("  [3] Set Pure Good (Chivalry 1000, Darkness 0)");
        terminal.WriteLine("  [4] Set Pure Evil (Chivalry 0, Darkness 1000)");
        terminal.WriteLine("  [5] Set Neutral (Both 500)");
        terminal.WriteLine("");
        terminal.WriteLine("  [0] Back to Dev Menu");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("Choice: ");

        switch (choice)
        {
            case "0":
                return;
            case "1":
                var chivInput = await terminal.GetInput("Enter new Chivalry value: ");
                if (long.TryParse(chivInput, out long chivValue))
                {
                    currentPlayer.Chivalry = chivValue;
                    terminal.WriteLine($"Chivalry set to {chivValue}", "green");
                }
                break;
            case "2":
                var darkInput = await terminal.GetInput("Enter new Darkness value: ");
                if (long.TryParse(darkInput, out long darkValue))
                {
                    currentPlayer.Darkness = darkValue;
                    terminal.WriteLine($"Darkness set to {darkValue}", "green");
                }
                break;
            case "3":
                currentPlayer.Chivalry = 1000;
                currentPlayer.Darkness = 0;
                terminal.WriteLine("Set to Pure Good!", "bright_yellow");
                break;
            case "4":
                currentPlayer.Chivalry = 0;
                currentPlayer.Darkness = 1000;
                terminal.WriteLine("Set to Pure Evil!", "red");
                break;
            case "5":
                currentPlayer.Chivalry = 500;
                currentPlayer.Darkness = 500;
                terminal.WriteLine("Set to Neutral.", "gray");
                break;
            default:
                terminal.WriteLine("Invalid choice!", "red");
                break;
        }

        await Task.Delay(1000);
    }

    private async Task SetWeaponPower()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                      SET WEAPON POWER");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine($"  Current Weapon Power: {currentPlayer.WeapPow}");
        terminal.WriteLine("");
        terminal.WriteLine("  Suggested values:");
        terminal.WriteLine("    10-20   = Early game");
        terminal.WriteLine("    30-50   = Mid game");
        terminal.WriteLine("    60-100  = Late game");
        terminal.WriteLine("    100-200 = Endgame/Legendary");
        terminal.WriteLine("    200+    = God-tier");
        terminal.WriteLine("");

        var input = await terminal.GetInput("Enter new weapon power (0 to cancel): ");
        if (long.TryParse(input, out long newValue) && newValue > 0)
        {
            currentPlayer.WeapPow = newValue;
            terminal.WriteLine($"Weapon Power set to {newValue}", "green");
        }

        await Task.Delay(1000);
    }

    private async Task SetArmorPower()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                      SET ARMOR POWER");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine($"  Current Armor Power: {currentPlayer.ArmPow}");
        terminal.WriteLine("");
        terminal.WriteLine("  Suggested values:");
        terminal.WriteLine("    5-15    = Early game");
        terminal.WriteLine("    20-40   = Mid game");
        terminal.WriteLine("    50-80   = Late game");
        terminal.WriteLine("    80-150  = Endgame/Legendary");
        terminal.WriteLine("    150+    = God-tier");
        terminal.WriteLine("");

        var input = await terminal.GetInput("Enter new armor power (0 to cancel): ");
        if (long.TryParse(input, out long newValue) && newValue > 0)
        {
            currentPlayer.ArmPow = newValue;
            terminal.WriteLine($"Armor Power set to {newValue}", "green");
        }

        await Task.Delay(1000);
    }

    private async Task GiveAllEquipment()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                    GIVE ALL EQUIPMENT");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  This will set your weapon and armor power to maximum values");
        terminal.WriteLine("  simulating having the best equipment in the game.");
        terminal.WriteLine("");

        var confirm = await terminal.GetInput("Are you sure? (Y/N): ");
        if (confirm.ToUpper() == "Y")
        {
            currentPlayer.WeapPow = 250;
            currentPlayer.ArmPow = 200;
            currentPlayer.Defence += 50;
            currentPlayer.BaseDefence = currentPlayer.Defence;

            terminal.WriteLine("Equipment maxed out!", "green");
            terminal.WriteLine($"  Weapon Power: {currentPlayer.WeapPow}", "cyan");
            terminal.WriteLine($"  Armor Power: {currentPlayer.ArmPow}", "cyan");
        }
        else
        {
            terminal.WriteLine("Cancelled.", "gray");
        }

        await Task.Delay(1500);
    }

    private async Task SetStoryProgress()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                    SET STORY PROGRESS");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        var story = StoryProgressionSystem.Instance;

        terminal.SetColor("white");
        terminal.WriteLine($"  Current Chapter: {story.CurrentChapter}");
        terminal.WriteLine($"  Current Act: {story.CurrentAct}");
        terminal.WriteLine("");
        terminal.WriteLine("  Available Chapters:");

        var chapters = Enum.GetValues<StoryChapter>();
        for (int i = 0; i < chapters.Length; i++)
        {
            terminal.WriteLine($"    [{i}] {chapters[i]}");
        }

        terminal.WriteLine("");
        var input = await terminal.GetInput("Set chapter to (number): ");
        if (int.TryParse(input, out int chapterIndex) && chapterIndex >= 0 && chapterIndex < chapters.Length)
        {
            story.AdvanceChapter(chapters[chapterIndex]);
            terminal.WriteLine($"Chapter set to {chapters[chapterIndex]}", "green");
        }
        else
        {
            terminal.WriteLine("Invalid chapter number.", "red");
        }

        await Task.Delay(1500);
    }

    private async Task UnlockAllSeals()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                    UNLOCK ALL SEALS");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        var story = StoryProgressionSystem.Instance;
        var seals = Enum.GetValues<SealType>();

        foreach (var seal in seals)
        {
            story.CollectSeal(seal);
        }

        terminal.SetColor("bright_magenta");
        terminal.WriteLine("  All Seven Seals have been unlocked!");
        terminal.WriteLine("");
        terminal.WriteLine("  The complete history of the Old Gods is now known.");
        terminal.WriteLine("  The True Ending path is now available.");

        story.SetStoryFlag("all_seals_collected", true);
        story.SetStoryFlag("true_ending_possible", true);

        await Task.Delay(2000);
    }

    private async Task UnlockAllArtifacts()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                   UNLOCK ALL ARTIFACTS");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        var story = StoryProgressionSystem.Instance;
        var artifacts = Enum.GetValues<ArtifactType>();

        foreach (var artifact in artifacts)
        {
            story.CollectArtifact(artifact);
        }

        terminal.SetColor("bright_cyan");
        terminal.WriteLine("  All Seven Divine Artifacts have been collected!");
        terminal.WriteLine("");
        terminal.WriteLine("  Artifacts obtained:");
        foreach (var artifact in artifacts)
        {
            terminal.WriteLine($"    ★ {artifact}");
        }

        await Task.Delay(2000);
    }

    private async Task SetOldGodStatus()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                   SET OLD GOD STATUS");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        var story = StoryProgressionSystem.Instance;
        var gods = Enum.GetValues<OldGodType>();

        terminal.SetColor("white");
        terminal.WriteLine("  Current God Status:");
        foreach (var god in gods)
        {
            if (story.OldGodStates.TryGetValue(god, out var state))
            {
                terminal.WriteLine($"    {god}: {state.Status}");
            }
        }

        terminal.WriteLine("");
        terminal.WriteLine("  [1] Defeat All Gods");
        terminal.WriteLine("  [2] Save All Gods");
        terminal.WriteLine("  [3] Reset All Gods to Corrupted");
        terminal.WriteLine("  [0] Back");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("Choice: ");

        switch (choice)
        {
            case "1":
                foreach (var god in gods)
                {
                    if (god != OldGodType.Manwe)
                        story.UpdateGodState(god, GodStatus.Defeated);
                }
                terminal.WriteLine("All Old Gods have been defeated!", "red");
                break;
            case "2":
                foreach (var god in gods)
                {
                    if (god != OldGodType.Manwe)
                        story.UpdateGodState(god, GodStatus.Saved);
                }
                terminal.WriteLine("All Old Gods have been saved!", "bright_yellow");
                break;
            case "3":
                foreach (var god in gods)
                {
                    story.UpdateGodState(god, GodStatus.Corrupted);
                }
                terminal.WriteLine("All Old Gods reset to Corrupted.", "gray");
                break;
            case "0":
                return;
            default:
                terminal.WriteLine("Invalid choice.", "red");
                break;
        }

        await Task.Delay(1500);
    }

    private async Task GodMode()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("                       ★ GOD MODE ★");
        terminal.WriteLine("═══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  This will set all stats to maximum values!");
        terminal.WriteLine("");

        var confirm = await terminal.GetInput("Are you sure? (Y/N): ");
        if (confirm.ToUpper() != "Y")
        {
            terminal.WriteLine("Cancelled.", "gray");
            await Task.Delay(1000);
            return;
        }

        // Max all stats
        currentPlayer.Level = GameConfig.MaxLevel;
        currentPlayer.Experience = 999999999;

        currentPlayer.Strength = 500;
        currentPlayer.Dexterity = 500;
        currentPlayer.Constitution = 500;
        currentPlayer.Intelligence = 500;
        currentPlayer.Wisdom = 500;
        currentPlayer.Charisma = 500;
        currentPlayer.Stamina = 500;
        currentPlayer.Agility = 500;

        currentPlayer.BaseStrength = 500;
        currentPlayer.BaseDexterity = 500;
        currentPlayer.BaseConstitution = 500;
        currentPlayer.BaseIntelligence = 500;
        currentPlayer.BaseWisdom = 500;
        currentPlayer.BaseCharisma = 500;
        currentPlayer.BaseStamina = 500;
        currentPlayer.BaseAgility = 500;

        currentPlayer.MaxHP = 9999;
        currentPlayer.HP = 9999;
        currentPlayer.MaxMana = 9999;
        currentPlayer.Mana = 9999;
        currentPlayer.BaseMaxHP = 9999;
        currentPlayer.BaseMaxMana = 9999;

        currentPlayer.Defence = 300;
        currentPlayer.BaseDefence = 300;
        currentPlayer.WeapPow = 300;
        currentPlayer.ArmPow = 250;

        currentPlayer.Gold = 10000000;
        currentPlayer.BankGold = 100000000;
        currentPlayer.Healing = 99;

        currentPlayer.Fights = 999;
        currentPlayer.PFights = 99;

        terminal.SetColor("bright_magenta");
        terminal.WriteLine("");
        terminal.WriteLine("  ╔═══════════════════════════════════════════════════════╗");
        terminal.WriteLine("  ║          YOU HAVE ASCENDED TO GODHOOD!               ║");
        terminal.WriteLine("  ╚═══════════════════════════════════════════════════════╝");
        terminal.WriteLine("");
        terminal.SetColor("yellow");
        terminal.WriteLine("  All stats maximized. You are now unstoppable.");

        await Task.Delay(2000);
    }

    private async Task FullHeal()
    {
        currentPlayer.HP = currentPlayer.MaxHP;
        currentPlayer.Mana = currentPlayer.MaxMana;

        // Cure diseases
        currentPlayer.Blind = false;
        currentPlayer.Plague = false;
        currentPlayer.Smallpox = false;
        currentPlayer.Measles = false;
        currentPlayer.Leprosy = false;
        currentPlayer.Poison = 0;

        // Clear negative status effects
        currentPlayer.ClearAllStatuses();

        terminal.SetColor("bright_green");
        terminal.WriteLine("");
        terminal.WriteLine("  ★ Fully healed and cured of all ailments! ★");
        terminal.WriteLine($"  HP: {currentPlayer.HP}/{currentPlayer.MaxHP}");
        terminal.WriteLine($"  Mana: {currentPlayer.Mana}/{currentPlayer.MaxMana}");

        await Task.Delay(1500);
    }

    private async Task MaxGold()
    {
        currentPlayer.Gold = 1000000;
        currentPlayer.BankGold += 1000000;

        terminal.SetColor("bright_yellow");
        terminal.WriteLine("");
        terminal.WriteLine("  ★ 1,000,000 gold added! ★");
        terminal.WriteLine($"  Gold in hand: {currentPlayer.Gold:N0}");
        terminal.WriteLine($"  Gold in bank: {currentPlayer.BankGold:N0}");

        await Task.Delay(1500);
    }

    private async Task LevelUp()
    {
        if (currentPlayer.Level >= GameConfig.MaxLevel)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("  Already at maximum level!");
            await Task.Delay(1000);
            return;
        }

        int oldLevel = currentPlayer.Level;
        currentPlayer.Level++;

        // Give level-up bonuses
        int hpGain = 10 + (int)(currentPlayer.Constitution / 5);
        int manaGain = 5 + (int)(currentPlayer.Intelligence / 5);

        currentPlayer.MaxHP += hpGain;
        currentPlayer.BaseMaxHP += hpGain;
        currentPlayer.HP = currentPlayer.MaxHP;

        currentPlayer.MaxMana += manaGain;
        currentPlayer.BaseMaxMana += manaGain;
        currentPlayer.Mana = currentPlayer.MaxMana;

        // Stat gains
        currentPlayer.Strength += 2;
        currentPlayer.BaseStrength += 2;
        currentPlayer.Defence += 1;
        currentPlayer.BaseDefence += 1;

        terminal.SetColor("bright_yellow");
        terminal.WriteLine("");
        terminal.WriteLine($"  ★ LEVEL UP! {oldLevel} → {currentPlayer.Level} ★");
        terminal.WriteLine($"  HP: +{hpGain} → {currentPlayer.MaxHP}");
        terminal.WriteLine($"  Mana: +{manaGain} → {currentPlayer.MaxMana}");
        terminal.WriteLine($"  Strength: +2");
        terminal.WriteLine($"  Defence: +1");

        await Task.Delay(1500);
    }

    private async Task MaxLevel()
    {
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("");
        terminal.WriteLine("  Raising to maximum level...");

        int levelsGained = GameConfig.MaxLevel - currentPlayer.Level;

        currentPlayer.Level = GameConfig.MaxLevel;

        // Calculate cumulative bonuses
        int totalHpGain = levelsGained * (10 + (int)(currentPlayer.Constitution / 5));
        int totalManaGain = levelsGained * (5 + (int)(currentPlayer.Intelligence / 5));

        currentPlayer.MaxHP += totalHpGain;
        currentPlayer.BaseMaxHP += totalHpGain;
        currentPlayer.HP = currentPlayer.MaxHP;

        currentPlayer.MaxMana += totalManaGain;
        currentPlayer.BaseMaxMana += totalManaGain;
        currentPlayer.Mana = currentPlayer.MaxMana;

        currentPlayer.Strength += levelsGained * 2;
        currentPlayer.BaseStrength += levelsGained * 2;
        currentPlayer.Defence += levelsGained;
        currentPlayer.BaseDefence += levelsGained;

        // Set experience to a high value
        currentPlayer.Experience = 999999999;

        terminal.SetColor("bright_magenta");
        terminal.WriteLine("");
        terminal.WriteLine($"  ★ MAXIMUM LEVEL ACHIEVED: {currentPlayer.Level} ★");
        terminal.WriteLine($"  HP: {currentPlayer.MaxHP:N0}");
        terminal.WriteLine($"  Mana: {currentPlayer.MaxMana:N0}");
        terminal.WriteLine($"  Strength: {currentPlayer.Strength}");
        terminal.WriteLine($"  Defence: {currentPlayer.Defence}");

        await Task.Delay(2000);
    }
}
