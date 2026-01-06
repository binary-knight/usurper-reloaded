using UsurperRemake.Utils;
using UsurperRemake.Systems;
using UsurperRemake.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Dungeon Location - Room-based exploration with atmosphere and tension
/// Features: Procedural floors, room navigation, feature interaction, combat, events
/// </summary>
public class DungeonLocation : BaseLocation
{
    private List<Character> teammates = new();
    private int currentDungeonLevel = 1;
    private int maxDungeonLevel = 100;
    private Random dungeonRandom = new Random();
    private DungeonTerrain currentTerrain = DungeonTerrain.Underground;

    // Legacy encounter chances (for old ExploreLevel fallback)
    private const float MonsterEncounterChance = 0.90f;
    private const float SpecialEventChance = 0.10f;

    // Current floor state
    private DungeonFloor currentFloor;
    private bool inRoomMode = false; // Are we exploring a room?

    // Player state tracking for tension
    private int consecutiveMonsterRooms = 0;
    private int roomsExploredThisFloor = 0;
    private bool hasRestThisFloor = false;

    public DungeonLocation() : base(
        GameLocation.Dungeons,
        "The Dungeons",
        "You stand before the entrance to the ancient dungeons. Dark passages lead deep into the earth."
    ) { }

    protected override void SetupLocation()
    {
        base.SetupLocation();
        // Note: currentDungeonLevel is initialized in EnterLocation() when we have the actual player
    }

    public override async Task EnterLocation(Character player, TerminalEmulator term)
    {
        // Initialize dungeon level based on the actual player entering
        var playerLevel = player?.Level ?? 1;
        currentDungeonLevel = Math.Max(1, playerLevel);

        if (currentDungeonLevel > maxDungeonLevel)
            currentDungeonLevel = maxDungeonLevel;

        // Generate floor for this player's level
        bool isNewFloor = currentFloor == null || currentFloor.Level != currentDungeonLevel;
        if (isNewFloor)
        {
            currentFloor = DungeonGenerator.GenerateFloor(currentDungeonLevel);
            roomsExploredThisFloor = 0;
            hasRestThisFloor = false;

            // Track dungeon exploration statistics
            player.Statistics.RecordDungeonLevel(currentDungeonLevel);

            // Show dramatic dungeon entrance art for new floors
            term.ClearScreen();
            await UsurperRemake.UI.ANSIArt.DisplayArtAnimated(term, UsurperRemake.UI.ANSIArt.DungeonEntrance, 40);
            term.WriteLine("");
            term.SetColor("cyan");
            term.WriteLine($"  Floor {currentDungeonLevel} - {currentFloor.Theme}");
            term.SetColor("gray");
            term.WriteLine($"  {GetThemeDescription(currentFloor.Theme)}");
            term.WriteLine("");
            term.SetColor("darkgray");
            term.Write("  Press any key to continue...");
            await term.ReadKeyAsync();
        }

        // Refresh bounty board quests based on player level
        QuestSystem.RefreshBountyBoard(player?.Level ?? 1);

        // Check for story events at milestone floors
        await CheckFloorStoryEvents(player, term);

        // Add active companions to the teammates list
        await AddCompanionsToParty(player, term);

        // Call base to enter the location loop
        await base.EnterLocation(player, term);
    }

    /// <summary>
    /// Check and trigger story events when entering key dungeon floors
    /// This ensures the player experiences the main narrative at appropriate levels
    /// </summary>
    private async Task CheckFloorStoryEvents(Character player, TerminalEmulator term)
    {
        // Check if there's a Seal on this floor that the player hasn't collected
        var sealSystem = SevenSealsSystem.Instance;
        var sealType = sealSystem.GetSealForFloor(currentDungeonLevel);

        // Debug: Show seal status for seal floors
        int[] sealFloors = { 15, 30, 45, 60, 80, 99 };
        var story = StoryProgressionSystem.Instance;
        if (sealFloors.Contains(currentDungeonLevel))
        {
            // Count seal-appropriate rooms on this floor
            int sealRoomCount = currentFloor.Rooms.Count(r =>
                r.Type == RoomType.Shrine ||
                r.Type == RoomType.LoreLibrary ||
                r.Type == RoomType.SecretVault ||
                r.Type == RoomType.MeditationChamber);

            // Show seal info to player on seal floors (if seal not yet collected)
            if (sealType.HasValue)
            {
                term.SetColor("bright_yellow");
                term.WriteLine($"[!] This floor contains a Seal of Truth. Explore carefully.");
                term.WriteLine($"    Seal-discovery rooms available: {sealRoomCount}");
                term.SetColor("white");
            }
            else
            {
                term.SetColor("gray");
                term.WriteLine($"[i] The seal on this floor has already been collected.");
                term.SetColor("white");
            }
        }

        if (sealType.HasValue)
        {
            // Mark that this floor has an uncollected seal - will be found during exploration
            currentFloor.HasUncollectedSeal = true;
            currentFloor.SealType = sealType.Value;
        }

        // Trigger story events at milestone floors (first time only)
        string floorVisitedFlag = $"dungeon_floor_{currentDungeonLevel}_visited";

        if (!story.HasStoryFlag(floorVisitedFlag))
        {
            story.SetStoryFlag(floorVisitedFlag, true);

            // Story milestone events
            await TriggerFloorStoryEvent(player, term);
        }
    }

    /// <summary>
    /// Trigger narrative events at key dungeon floors
    /// </summary>
    private async Task TriggerFloorStoryEvent(Character player, TerminalEmulator term)
    {
        switch (currentDungeonLevel)
        {
            case 10:
                await ShowStoryMoment(term, "The Depths Begin",
                    new[] {
                        "As you descend to level 10, the air grows heavier.",
                        "The walls here are older, carved by hands that predate memory.",
                        "",
                        "You notice strange symbols repeated throughout - seven interlocking circles.",
                        "A pattern that tugs at something deep within you...",
                        "",
                        "Somewhere ahead, answers await."
                    }, "cyan");
                break;

            case 15:
                await ShowStoryMoment(term, "Ancient Battlefield",
                    new[] {
                        "The stones here are stained with something old.",
                        "Not rust. Not mortal blood. Something... golden.",
                        "",
                        "Weapons of impossible design litter the ground.",
                        "Too large for human hands. Too heavy for mortal arms.",
                        "",
                        "Something terrible happened here, long before history began.",
                        "",
                        "A SEAL OF TRUTH lies hidden on this floor.",
                        "Seek a shrine, library, or sacred chamber to find it."
                    }, "dark_red");
                StoryProgressionSystem.Instance.SetStoryFlag("knows_about_seals", true);
                break;

            case 25:
                await ShowStoryMoment(term, "The Whispers Begin",
                    new[] {
                        "You hear it now - a voice at the edge of perception.",
                        "Not speaking to you. Speaking AS you.",
                        "",
                        "\"Why do you descend, wave?\"",
                        "\"What do you seek in the deep?\"",
                        "",
                        "The voice knows your true name.",
                        "The name you have forgotten."
                    }, "bright_cyan");
                StoryProgressionSystem.Instance.SetStoryFlag("heard_whispers", true);
                // Moral Paradox: The Possessed Child
                if (MoralParadoxSystem.Instance.IsParadoxAvailable("possessed_child", player))
                {
                    await MoralParadoxSystem.Instance.PresentParadox("possessed_child", player, term);
                }
                break;

            case 30:
                await ShowStoryMoment(term, "Corrupted Shrine",
                    new[] {
                        "Seven statues stand in a circle, faces worn by time.",
                        "But something is wrong with them.",
                        "",
                        "The stone itself seems... sick. Twisted.",
                        "As if the sculptures were changed after they were made.",
                        "Beautiful forms warped into something grotesque.",
                        "",
                        "What force could corrupt stone itself?",
                        "",
                        "A SEAL OF TRUTH awaits those who seek answers here."
                    }, "dark_magenta");
                break;

            case 45:
                await ShowStoryMoment(term, "Prison of Ages",
                    new[] {
                        "Empty cells stretch into darkness.",
                        "Not prison cells for mortals - these are vast. Cathedral-sized.",
                        "",
                        "The bars are made of something that isn't metal.",
                        "Something that hums with power even now.",
                        "",
                        "Whatever was kept here was immense. And angry.",
                        "Claw marks scar the walls, deeper than any blade could cut.",
                        "",
                        "A SEAL OF TRUTH holds the answer to who was imprisoned here."
                    }, "gray");
                StoryProgressionSystem.Instance.AdvanceChapter(StoryChapter.TheWhispers);
                break;

            case 60:
                await ShowStoryMoment(term, "Oracle's Tomb",
                    new[] {
                        "A skeleton sits upon a throne of bone.",
                        "In death, she still clutches a crystal orb.",
                        "",
                        "The Oracle. The last mortal to speak with the gods.",
                        "She saw the future before she died.",
                        "And what she saw made her weep.",
                        "",
                        "Words are carved into the stone at her feet:",
                        "\"The Seal of Fate reveals what I could not speak.\"",
                        "",
                        "A SEAL OF TRUTH awaits. It holds her final prophecy."
                    }, "bright_cyan");
                StoryProgressionSystem.Instance.SetStoryFlag("knows_prophecy", true);

                // MAELKETH ENCOUNTER - First Old God boss
                if (OldGodBossSystem.Instance.CanEncounterBoss(player, OldGodType.Maelketh))
                {
                    term.WriteLine("");
                    term.WriteLine("The ground trembles. An ancient presence stirs...", "bright_red");
                    await Task.Delay(2000);

                    term.WriteLine("Do you wish to face Maelketh, the Broken Blade? (Y/N)", "yellow");
                    var response = await term.GetInput("> ");
                    if (response.Trim().ToUpper().StartsWith("Y"))
                    {
                        var result = await OldGodBossSystem.Instance.StartBossEncounter(player, OldGodType.Maelketh, term);
                        await HandleGodEncounterResult(result, player, term);
                    }
                    else
                    {
                        term.WriteLine("You sense the god retreating back into slumber... for now.", "gray");
                    }
                }
                break;

            case 65:
                // Moral Paradox: Veloura's Cure (requires Soulweaver's Loom)
                if (MoralParadoxSystem.Instance.IsParadoxAvailable("velouras_cure", player))
                {
                    await ShowStoryMoment(term, "The Soulweaver's Chamber",
                        new[] {
                            "The Soulweaver's Loom pulses with power in your hands.",
                            "And before you stands someone you never expected to see again...",
                        }, "bright_magenta");
                    await MoralParadoxSystem.Instance.PresentParadox("velouras_cure", player, term);
                }
                break;

            case 75:
                await ShowStoryMoment(term, "Chamber of Echoes",
                    new[] {
                        "You see yourself in the walls.",
                        "Not a reflection - a memory.",
                        "",
                        "You have been here before.",
                        "A thousand times. A million.",
                        "",
                        "The cycle repeats.",
                        "Manwe sends a fragment of himself to experience mortality.",
                        "To learn. To grow. To remember what it means to be small.",
                        "",
                        "And at the end, the fragment returns...",
                        "Unless this time, it doesn't."
                    }, "bright_magenta");
                AmnesiaSystem.Instance?.RecoverMemory(MemoryFragment.TheDecision);
                break;

            case 80:
                await ShowStoryMoment(term, "Chamber of Mourning",
                    new[] {
                        "The walls here are made of crystal.",
                        "Blue-grey. Cold. And wet.",
                        "",
                        "Not water. Something thicker. Saltier.",
                        "As if these crystals formed from tears.",
                        "",
                        "Oceans of tears, shed over millennia.",
                        "Frozen in stone. A monument to sorrow.",
                        "",
                        "Whose grief could fill a mountain?",
                        "",
                        "The SEAL OF TRUTH on this floor holds the answer."
                    }, "bright_blue");
                // Moral Paradox: Free Terravok (alternative to combat)
                if (MoralParadoxSystem.Instance.IsParadoxAvailable("free_terravok", player))
                {
                    await MoralParadoxSystem.Instance.PresentParadox("free_terravok", player, term);
                }
                // TERRAVOK ENCOUNTER - God of Earth
                else if (OldGodBossSystem.Instance.CanEncounterBoss(player, OldGodType.Terravok))
                {
                    term.WriteLine("");
                    term.WriteLine("The mountain itself seems to breathe. Stone shifts like flesh.", "yellow");
                    await Task.Delay(2000);

                    term.WriteLine("Terravok, the Worldbreaker, senses your presence.", "bright_yellow");
                    term.WriteLine("Do you wish to wake the Sleeping Mountain? (Y/N)", "yellow");
                    var response = await term.GetInput("> ");
                    if (response.Trim().ToUpper().StartsWith("Y"))
                    {
                        var result = await OldGodBossSystem.Instance.StartBossEncounter(player, OldGodType.Terravok, term);
                        await HandleGodEncounterResult(result, player, term);
                    }
                    else
                    {
                        term.WriteLine("The mountain settles. Terravok slumbers on.", "gray");
                    }
                }
                break;

            case 95:
                // Moral Paradox: Destroy Darkness (requires Sunforged Blade)
                if (MoralParadoxSystem.Instance.IsParadoxAvailable("destroy_darkness", player))
                {
                    await ShowStoryMoment(term, "The Purging Light",
                        new[] {
                            "Aurelion stands before you, radiant with divine light.",
                            "The Sunforged Blade burns bright in your hands.",
                            "",
                            "'You have earned this moment,' the God of Light speaks.",
                            "'I offer you the power to end all darkness forever.'"
                        }, "bright_yellow");
                    await MoralParadoxSystem.Instance.PresentParadox("destroy_darkness", player, term);
                }
                break;

            case 99:
                await ShowStoryMoment(term, "The Threshold",
                    new[] {
                        "You stand at the edge of understanding.",
                        "One floor above, Manwe waits.",
                        "",
                        "The Creator. Your creator.",
                        "Or perhaps... yourself?",
                        "",
                        "You are the wave that remembered it was the ocean.",
                        "And now the ocean must answer for what it has done.",
                        "",
                        "Collect the final seal if you have not.",
                        "Then face the truth."
                    }, "white");
                break;

            case 100:
                await ShowStoryMoment(term, "The End of All Things",
                    new[] {
                        "This is it.",
                        "",
                        "Beyond this threshold, Manwe dreams.",
                        "The Creator who made everything.",
                        "The god who broke his own children.",
                        "The ocean that forgot it was water.",
                        "",
                        "You have three choices:",
                        "  - DESTROY him and take his power (Usurper)",
                        "  - SAVE him and restore the gods (Savior)",
                        "  - DEFY him and forge your own path (Defiant)",
                        "",
                        "Or, if you collected all Seven Seals...",
                        "Perhaps something else is possible.",
                        "",
                        "Choose wisely. This is the only choice that matters."
                    }, "bright_yellow");
                StoryProgressionSystem.Instance.AdvanceChapter(StoryChapter.FinalConfrontation);

                // MANWE ENCOUNTER - The Creator, Final Boss
                if (OldGodBossSystem.Instance.CanEncounterBoss(player, OldGodType.Manwe))
                {
                    term.WriteLine("");
                    term.WriteLine("The dream trembles. The Dreamer stirs.", "white");
                    await Task.Delay(2000);

                    term.WriteLine("Manwe, the Creator of All, awaits your judgment.", "bright_white");
                    term.WriteLine("");
                    term.WriteLine("This is the final confrontation. Are you ready? (Y/N)", "bright_yellow");
                    var response = await term.GetInput("> ");
                    if (response.Trim().ToUpper().StartsWith("Y"))
                    {
                        var result = await OldGodBossSystem.Instance.StartBossEncounter(player, OldGodType.Manwe, term);
                        await HandleGodEncounterResult(result, player, term);

                        // After Manwe, trigger ending determination
                        if (result.Outcome != BossOutcome.Fled)
                        {
                            var endingType = EndingsSystem.Instance.DetermineEnding(player);
                            await ShowEnding(endingType, player, term);
                        }
                    }
                    else
                    {
                        term.WriteLine("You hesitate at the threshold. The Creator can wait.", "gray");
                        term.WriteLine("But not forever...", "dark_gray");
                    }
                }
                // Fallback: Moral Paradox if boss not available
                else if (MoralParadoxSystem.Instance.IsParadoxAvailable("final_choice", player))
                {
                    await MoralParadoxSystem.Instance.PresentParadox("final_choice", player, term);
                }
                break;
        }
    }

    /// <summary>
    /// Add active companions to the party's teammates list for combat
    /// </summary>
    private async Task AddCompanionsToParty(Character player, TerminalEmulator term)
    {
        var companionSystem = UsurperRemake.Systems.CompanionSystem.Instance;
        var companionCharacters = companionSystem.GetCompanionsAsCharacters();

        if (companionCharacters.Count == 0)
            return;

        // Remove any existing companion entries (in case of re-entry)
        teammates.RemoveAll(t => t.IsCompanion);

        // Add companions to teammates list
        foreach (var companion in companionCharacters)
        {
            if (companion.IsAlive)
            {
                teammates.Add(companion);
            }
        }

        // Show companion status if any are present
        if (companionCharacters.Count > 0)
        {
            term.WriteLine("");
            term.SetColor("bright_cyan");
            term.WriteLine("═══ YOUR COMPANIONS ═══");
            foreach (var companion in companionCharacters)
            {
                var companionData = companionSystem.GetCompanion(companion.CompanionId!.Value);
                term.SetColor("white");
                term.Write($"  {companion.DisplayName}");
                term.SetColor("gray");
                term.Write($" ({companionData?.CombatRole}) ");
                term.SetColor(companion.HP > companion.MaxHP / 2 ? "green" : "yellow");
                term.WriteLine($"HP: {companion.HP}/{companion.MaxHP}");
            }
            term.WriteLine("");
            await Task.Delay(1500);
        }
    }

    /// <summary>
    /// Display a story moment with dramatic formatting
    /// </summary>
    private async Task ShowStoryMoment(TerminalEmulator term, string title, string[] lines, string color)
    {
        term.ClearScreen();
        term.WriteLine("");
        term.SetColor(color);
        term.WriteLine($"╔{'═'.ToString().PadRight(58, '═')}╗");
        term.WriteLine($"║  {title.PadRight(55)} ║");
        term.WriteLine($"╚{'═'.ToString().PadRight(58, '═')}╝");
        term.WriteLine("");

        await Task.Delay(1000);

        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                term.WriteLine("");
            }
            else
            {
                term.SetColor("white");
                term.WriteLine($"  {line}");
            }
            await Task.Delay(200);
        }

        term.WriteLine("");
        term.SetColor("gray");
        await term.GetInputAsync("  Press Enter to continue...");
    }

    /// <summary>
    /// Handle the result of an Old God boss encounter
    /// </summary>
    private async Task HandleGodEncounterResult(BossEncounterResult result, Character player, TerminalEmulator term)
    {
        if (result == null || !result.Success) return;

        switch (result.Outcome)
        {
            case BossOutcome.Defeated:
                term.WriteLine("");
                term.WriteLine("The Old God has fallen. Their power flows into you.", "bright_yellow");

                // Grant artifact based on god defeated
                var artifactType = GetArtifactForGod(result.God);
                if (artifactType.HasValue)
                {
                    term.WriteLine($"You obtained: {artifactType.Value}!", "bright_magenta");
                    StoryProgressionSystem.Instance.CollectedArtifacts.Add(artifactType.Value);
                }

                // XP and gold reward
                if (result.XPGained > 0)
                {
                    player.Experience += result.XPGained;
                    term.WriteLine($"Experience gained: {result.XPGained}", "green");
                }
                if (result.GoldGained > 0)
                {
                    player.Gold += result.GoldGained;
                    term.WriteLine($"Gold gained: {result.GoldGained}", "yellow");
                }

                // Chivalry impact
                player.Darkness += 100;
                term.WriteLine("Your darkness deepens. You are becoming the Usurper.", "red");

                // Check achievements
                AchievementSystem.CheckAchievements(player);
                await AchievementSystem.ShowPendingNotifications(term);
                break;

            case BossOutcome.Saved:
                term.WriteLine("");
                term.WriteLine("The Old God's corruption lifts. They remember who they were.", "bright_cyan");
                term.WriteLine("A fragment of divine gratitude fills your heart.", "white");

                player.Chivalry += 150;
                term.WriteLine("Your chivalry grows. You are becoming the Savior.", "bright_green");

                // Ocean Philosophy moment
                OceanPhilosophySystem.Instance.CollectFragment(WaveFragment.TheCycle);
                break;

            case BossOutcome.Allied:
                term.WriteLine("");
                term.WriteLine("The Old God sees something in you. An understanding.", "bright_magenta");
                term.WriteLine("You have forged an alliance beyond mortal comprehension.", "white");

                player.Chivalry += 50;
                player.Wisdom += 2;
                break;

            case BossOutcome.Fled:
                term.WriteLine("");
                term.WriteLine("You retreat from the divine presence.", "gray");
                term.WriteLine("The Old God watches you go. They can wait.", "dark_gray");
                break;
        }

        await Task.Delay(3000);
    }

    /// <summary>
    /// Get the artifact dropped by a specific Old God
    /// </summary>
    private ArtifactType? GetArtifactForGod(OldGodType god)
    {
        return god switch
        {
            OldGodType.Maelketh => ArtifactType.CreatorsEye,
            OldGodType.Veloura => ArtifactType.SoulweaversLoom,
            OldGodType.Thorgrim => ArtifactType.ScalesOfLaw,
            OldGodType.Noctura => ArtifactType.ShadowCrown,
            OldGodType.Aurelion => ArtifactType.SunforgedBlade,
            OldGodType.Terravok => ArtifactType.Worldstone,
            OldGodType.Manwe => null, // Manwe is the creator, no artifact
            _ => null
        };
    }

    /// <summary>
    /// Display the ending sequence based on ending type
    /// </summary>
    private async Task ShowEnding(EndingType ending, Character player, TerminalEmulator term)
    {
        term.ClearScreen();
        term.WriteLine("");

        string color = ending switch
        {
            EndingType.Usurper => "bright_red",
            EndingType.Savior => "bright_green",
            EndingType.Defiant => "bright_yellow",
            EndingType.TrueEnding => "bright_cyan",
            EndingType.Secret => "bright_magenta",
            _ => "white"
        };

        string title = ending switch
        {
            EndingType.Usurper => "THE USURPER",
            EndingType.Savior => "THE SAVIOR",
            EndingType.Defiant => "THE DEFIANT",
            EndingType.TrueEnding => "THE AWAKENED",
            EndingType.Secret => "DISSOLUTION",
            _ => "THE END"
        };

        term.SetColor(color);
        term.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        term.WriteLine($"║                         {title.PadRight(35)}  ║");
        term.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        term.WriteLine("");

        await Task.Delay(2000);

        // Show ending text based on type
        switch (ending)
        {
            case EndingType.Usurper:
                await ShowUsurperEnding(term);
                break;
            case EndingType.Savior:
                await ShowSaviorEnding(term);
                break;
            case EndingType.Defiant:
                await ShowDefiantEnding(term);
                break;
            case EndingType.TrueEnding:
                await ShowTrueEnding(term);
                break;
            case EndingType.Secret:
                await ShowSecretEnding(term);
                break;
        }

        // Mark the ending
        StoryProgressionSystem.Instance.SetStoryFlag($"completed_{ending.ToString().ToLower()}", true);

        term.WriteLine("");
        term.SetColor("gray");
        term.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        term.WriteLine("║                    CONGRATULATIONS                               ║");
        term.WriteLine("║                                                                  ║");
        term.WriteLine("║        You have completed Usurper Reborn.                       ║");
        term.WriteLine("║                                                                  ║");
        term.WriteLine("║        Your journey is recorded in the annals of time.          ║");
        term.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        term.WriteLine("");

        await term.GetInputAsync("Press Enter to continue...");
    }

    private async Task ShowUsurperEnding(TerminalEmulator term)
    {
        string[] lines = {
            "You stand where Manwe once stood.",
            "The power of creation flows through you.",
            "",
            "The Old Gods bow to your will.",
            "The world trembles at your command.",
            "",
            "You have become what you were sent to destroy.",
            "The Usurper. The new Creator.",
            "",
            "But power, you discover, is a cold companion.",
            "And eternity stretches before you, empty and vast.",
            "",
            "Was it worth it?",
            "Only you can answer that now."
        };

        foreach (var line in lines)
        {
            term.SetColor(string.IsNullOrEmpty(line) ? "white" : "red");
            term.WriteLine($"  {line}");
            await Task.Delay(300);
        }
    }

    private async Task ShowSaviorEnding(TerminalEmulator term)
    {
        string[] lines = {
            "The corruption lifts from the world.",
            "The Old Gods remember who they were.",
            "",
            "Maelketh becomes the god of honorable combat once more.",
            "Veloura's love heals instead of destroying.",
            "Thorgrim judges with wisdom instead of tyranny.",
            "",
            "And you... you return to mortality.",
            "But not alone.",
            "",
            "Songs are sung of the one who saved the gods.",
            "Children speak your name with wonder.",
            "",
            "When you finally rest, paradise awaits.",
            "The gods remember their Savior."
        };

        foreach (var line in lines)
        {
            term.SetColor(string.IsNullOrEmpty(line) ? "white" : "bright_green");
            term.WriteLine($"  {line}");
            await Task.Delay(300);
        }
    }

    private async Task ShowDefiantEnding(TerminalEmulator term)
    {
        string[] lines = {
            "You reject them all.",
            "Manwe. The Old Gods. The cycles. The power.",
            "",
            "\"I am not your pawn,\" you declare.",
            "\"I am not your child. I am not your successor.\"",
            "",
            "\"I am myself. Nothing more. Nothing less.\"",
            "",
            "And with that, you walk away.",
            "Into the sunrise. Into the unknown.",
            "",
            "Behind you, the gods become mortal.",
            "Equals now, instead of masters.",
            "",
            "Freedom, you realize, is the greatest power of all."
        };

        foreach (var line in lines)
        {
            term.SetColor(string.IsNullOrEmpty(line) ? "white" : "bright_yellow");
            term.WriteLine($"  {line}");
            await Task.Delay(300);
        }
    }

    private async Task ShowTrueEnding(TerminalEmulator term)
    {
        string[] lines = {
            "You remember now.",
            "Not just who you were. What you are.",
            "",
            "You are the wave that remembered it was the ocean.",
            "You are the dream that woke within the Dreamer.",
            "",
            "The Seven Seals unlock the final truth:",
            "You and Manwe are one. Always were.",
            "",
            "Not god, not mortal, but something new.",
            "Something that can bridge the gap.",
            "",
            "The cycle doesn't end. It transcends.",
            "And you carry the best of both within you.",
            "",
            "This is not an ending.",
            "It is a new beginning."
        };

        foreach (var line in lines)
        {
            term.SetColor(string.IsNullOrEmpty(line) ? "white" : "bright_cyan");
            term.WriteLine($"  {line}");
            await Task.Delay(300);
        }
    }

    private async Task ShowSecretEnding(TerminalEmulator term)
    {
        string[] lines = {
            "Three cycles. Three lifetimes.",
            "Each time, a little more remembered.",
            "Each time, the boundaries grew thinner.",
            "",
            "And now...",
            "",
            "The wave dissolves.",
            "The ocean remembers.",
            "",
            "You are Manwe.",
            "You always were.",
            "",
            "And as the dream ends, you wake.",
            "Not as a fragment, but as the whole.",
            "",
            "The cycle is complete.",
            "The Dreamer opens their eyes.",
            "",
            "Thank you for playing."
        };

        foreach (var line in lines)
        {
            term.SetColor(string.IsNullOrEmpty(line) ? "white" : "bright_magenta");
            term.WriteLine($"  {line}");
            await Task.Delay(400);
        }
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();

        // Get current room
        var room = currentFloor?.GetCurrentRoom();

        if (room != null && inRoomMode)
        {
            DisplayRoomView(room);
        }
        else
        {
            DisplayFloorOverview();
        }
    }

    /// <summary>
    /// Display when player is in a specific room - the main exploration view
    /// </summary>
    private void DisplayRoomView(DungeonRoom room)
    {
        var player = GetCurrentPlayer();

        // Room header with theme color
        terminal.SetColor(GetThemeColor(currentFloor.Theme));
        terminal.WriteLine($"╔{new string('═', 55)}╗");
        terminal.WriteLine($"║  {room.Name.PadRight(52)} ║");
        terminal.WriteLine($"╚{new string('═', 55)}╝");

        // Show danger indicators
        ShowDangerIndicators(room);

        terminal.WriteLine("");

        // Room description
        terminal.SetColor("white");
        terminal.WriteLine(room.Description);
        terminal.WriteLine("");

        // Atmospheric text (builds tension)
        terminal.SetColor("gray");
        terminal.WriteLine(room.AtmosphereText);
        terminal.WriteLine("");

        // Show what's in the room
        ShowRoomContents(room);

        // Show exits
        ShowExits(room);

        // Show room actions
        ShowRoomActions(room);

        // Quick status bar
        ShowQuickStatus(player);
    }

    private void ShowDangerIndicators(DungeonRoom room)
    {
        terminal.SetColor("darkgray");
        terminal.Write($"Level {currentDungeonLevel} | ");

        // Show floor theme
        terminal.SetColor(GetThemeColor(currentFloor.Theme));
        terminal.Write($"{currentFloor.Theme} | ");

        // Danger rating
        terminal.SetColor(room.DangerRating >= 3 ? "red" : room.DangerRating >= 2 ? "yellow" : "green");
        terminal.Write($"Danger: ");
        for (int i = 0; i < room.DangerRating; i++) terminal.Write("*");
        for (int i = room.DangerRating; i < 3; i++) terminal.Write(".");

        // Room status
        if (room.IsCleared)
        {
            terminal.SetColor("green");
            terminal.Write(" [CLEARED]");
        }
        else if (room.HasMonsters)
        {
            terminal.SetColor("red");
            terminal.Write(" [DANGER]");
        }

        if (room.IsBossRoom)
        {
            terminal.SetColor("bright_red");
            terminal.Write(" [BOSS]");
        }

        terminal.WriteLine("");
    }

    private void ShowRoomContents(DungeonRoom room)
    {
        bool hasAnything = false;

        // Monsters present (not yet cleared)
        if (room.HasMonsters && !room.IsCleared)
        {
            terminal.SetColor("red");
            if (room.IsBossRoom)
            {
                terminal.WriteLine(">> A powerful presence dominates this room! <<");
            }
            else
            {
                var monsterHints = new[]
                {
                    "Shadows move at the edge of your torchlight...",
                    "You hear hostile sounds from the darkness...",
                    "Something is watching you from the shadows...",
                    "The air feels thick with menace..."
                };
                terminal.WriteLine(monsterHints[dungeonRandom.Next(monsterHints.Length)]);
            }
            hasAnything = true;
        }

        // Treasure
        if (room.HasTreasure && !room.TreasureLooted)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine(">> Something valuable glints in the darkness! <<");
            hasAnything = true;
        }

        // Trap (hidden hint)
        if (room.HasTrap && !room.TrapTriggered && dungeonRandom.NextDouble() < 0.3)
        {
            terminal.SetColor("magenta");
            terminal.WriteLine(">> You sense hidden danger... <<");
            hasAnything = true;
        }

        // Event
        if (room.HasEvent && !room.EventCompleted)
        {
            terminal.SetColor("cyan");
            terminal.WriteLine(GetEventHint(room.EventType));
            hasAnything = true;
        }

        // Stairs
        if (room.HasStairsDown)
        {
            terminal.SetColor("blue");
            terminal.WriteLine(">> Stairs lead down to a deeper level <<");
            hasAnything = true;
        }

        // Features to examine
        if (room.Features.Any(f => !f.IsInteracted))
        {
            terminal.SetColor("cyan");
            terminal.WriteLine("");
            terminal.WriteLine("You notice:");
            foreach (var feature in room.Features.Where(f => !f.IsInteracted))
            {
                terminal.Write("  - ");
                terminal.SetColor("white");
                terminal.WriteLine(feature.Name);
                terminal.SetColor("cyan");
            }
            hasAnything = true;
        }

        if (hasAnything)
            terminal.WriteLine("");
    }

    private string GetEventHint(DungeonEventType eventType)
    {
        return eventType switch
        {
            DungeonEventType.TreasureChest => ">> An old chest sits in the corner <<",
            DungeonEventType.Merchant => ">> You see a figure by a small campfire <<",
            DungeonEventType.Shrine => ">> A strange altar radiates energy <<",
            DungeonEventType.NPCEncounter => ">> Someone else is here <<",
            DungeonEventType.Puzzle => ">> Strange mechanisms cover one wall <<",
            DungeonEventType.RestSpot => ">> This area seems relatively safe <<",
            DungeonEventType.MysteryEvent => ">> Something unusual catches your eye <<",
            _ => ">> Something interesting is here <<"
        };
    }

    private void ShowExits(DungeonRoom room)
    {
        terminal.SetColor("white");
        terminal.WriteLine("Exits:");

        foreach (var exit in room.Exits)
        {
            var targetRoom = currentFloor.GetRoom(exit.Value.TargetRoomId);
            var dirKey = GetDirectionKey(exit.Key);

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("bright_cyan");
            terminal.Write(dirKey);
            terminal.SetColor("darkgray");
            terminal.Write("] ");

            terminal.SetColor("gray");
            terminal.Write(exit.Value.Description);

            // Show target room status
            if (targetRoom != null)
            {
                if (targetRoom.IsCleared)
                {
                    terminal.SetColor("green");
                    terminal.Write(" (cleared)");
                }
                else if (targetRoom.IsExplored)
                {
                    terminal.SetColor("yellow");
                    terminal.Write(" (explored)");
                }
                else
                {
                    terminal.SetColor("darkgray");
                    terminal.Write(" (unknown)");
                }
            }

            terminal.WriteLine("");
        }
        terminal.WriteLine("");
    }

    private void ShowRoomActions(DungeonRoom room)
    {
        terminal.SetColor("white");
        terminal.WriteLine("Actions:");

        // Fight monsters
        if (room.HasMonsters && !room.IsCleared)
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("red");
            terminal.Write("F");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Fight the monsters");
        }

        // Search for treasure (available if room is cleared OR has no monsters)
        if (room.HasTreasure && !room.TreasureLooted && (room.IsCleared || !room.HasMonsters))
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("yellow");
            terminal.Write("T");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Collect treasure");
        }

        // Interact with event
        if (room.HasEvent && !room.EventCompleted)
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("cyan");
            terminal.Write("E");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Investigate the event");
        }

        // Examine features
        if (room.Features.Any(f => !f.IsInteracted))
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("magenta");
            terminal.Write("X");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Examine features");
        }

        // Use stairs (available if room is cleared OR has no monsters)
        if (room.HasStairsDown && (room.IsCleared || !room.HasMonsters))
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("blue");
            terminal.Write("D");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Descend stairs");
        }

        // Rest (if safe - room cleared or no monsters)
        if ((room.IsCleared || !room.HasMonsters) && !hasRestThisFloor)
        {
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("green");
            terminal.Write("R");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Rest and recover");
        }

        // General options
        terminal.SetColor("darkgray");
        terminal.Write("  [");
        terminal.SetColor("cyan");
        terminal.Write("M");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Map  ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("cyan");
        terminal.Write("I");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Inventory  ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("cyan");
        terminal.Write("P");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.Write("Potions  ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("red");
        terminal.Write("Q");
        terminal.SetColor("darkgray");
        terminal.Write("] ");
        terminal.SetColor("white");
        terminal.WriteLine("Leave dungeon");

        terminal.WriteLine("");
    }

    private void ShowQuickStatus(Character player)
    {
        terminal.SetColor("darkgray");
        terminal.Write(new string('─', 57));
        terminal.WriteLine("");

        // Health bar
        terminal.SetColor("white");
        terminal.Write("HP: ");
        DrawBar(player.HP, player.MaxHP, 20, "red", "darkgray");
        terminal.Write($" {player.HP}/{player.MaxHP}");

        terminal.Write("  ");

        // Potions
        terminal.SetColor("green");
        terminal.Write($"Potions: {player.Healing}/{player.MaxPotions}");

        terminal.Write("  ");

        // Gold
        terminal.SetColor("yellow");
        terminal.Write($"Gold: {player.Gold:N0}");

        terminal.WriteLine("");
    }

    private void DrawBar(long current, long max, int width, string fillColor, string emptyColor)
    {
        int filled = max > 0 ? (int)((current * width) / max) : 0;
        filled = Math.Max(0, Math.Min(width, filled));

        terminal.Write("[");
        terminal.SetColor(fillColor);
        terminal.Write(new string('█', filled));
        terminal.SetColor(emptyColor);
        terminal.Write(new string('░', width - filled));
        terminal.SetColor("white");
        terminal.Write("]");
    }

    private string GetDirectionKey(Direction dir)
    {
        return dir switch
        {
            Direction.North => "N",
            Direction.South => "S",
            Direction.East => "E",
            Direction.West => "W",
            _ => "?"
        };
    }

    private string GetThemeDescription(DungeonTheme theme)
    {
        return theme switch
        {
            DungeonTheme.Catacombs => "Ancient burial chambers filled with restless dead",
            DungeonTheme.Sewers => "Fetid tunnels crawling with vermin and worse",
            DungeonTheme.Caverns => "Natural caves carved by underground rivers",
            DungeonTheme.AncientRuins => "Crumbling remnants of a forgotten civilization",
            DungeonTheme.DemonLair => "Hellish corridors reeking of brimstone",
            DungeonTheme.FrozenDepths => "Ice-encrusted halls where cold itself hunts",
            DungeonTheme.VolcanicPit => "Molten rivers and scorching heat await",
            DungeonTheme.AbyssalVoid => "Reality itself warps in these cursed depths",
            _ => "Dark passages wind into the unknown"
        };
    }

    private string GetThemeColor(DungeonTheme theme)
    {
        return theme switch
        {
            DungeonTheme.Catacombs => "gray",
            DungeonTheme.Sewers => "green",
            DungeonTheme.Caverns => "cyan",
            DungeonTheme.AncientRuins => "yellow",
            DungeonTheme.DemonLair => "red",
            DungeonTheme.FrozenDepths => "bright_cyan",
            DungeonTheme.VolcanicPit => "bright_red",
            DungeonTheme.AbyssalVoid => "magenta",
            _ => "white"
        };
    }

    /// <summary>
    /// Display floor overview before entering
    /// </summary>
    private void DisplayFloorOverview()
    {
        ShowBreadcrumb();

        // Header - standardized format
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔═════════════════════════════════════════════════════════════════════════════╗");
        terminal.SetColor("bright_yellow");
        terminal.WriteLine($"║                         DUNGEON LEVEL {currentDungeonLevel.ToString().PadLeft(3)}                                  ║");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╚═════════════════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");
        terminal.SetColor(GetThemeColor(currentFloor.Theme));
        terminal.WriteLine($"Theme: {currentFloor.Theme}");
        terminal.WriteLine("");

        // Floor stats
        terminal.SetColor("white");
        terminal.WriteLine($"Rooms: {currentFloor.Rooms.Count}");
        terminal.WriteLine($"Danger Level: {currentFloor.DangerLevel}/10");

        int explored = currentFloor.Rooms.Count(r => r.IsExplored);
        int cleared = currentFloor.Rooms.Count(r => r.IsCleared);
        terminal.WriteLine($"Explored: {explored}/{currentFloor.Rooms.Count}");
        terminal.WriteLine($"Cleared: {cleared}/{currentFloor.Rooms.Count}");
        terminal.WriteLine("");

        // Floor flavor
        terminal.SetColor("gray");
        terminal.WriteLine(GetFloorFlavorText(currentFloor.Theme));
        terminal.WriteLine("");

        // Team info
        terminal.SetColor("cyan");
        terminal.WriteLine($"Your Party: {1 + teammates.Count} member{(teammates.Count > 0 ? "s" : "")}");
        terminal.WriteLine("");

        // Show seal hint if this floor has an uncollected seal
        if (currentFloor.HasUncollectedSeal && !currentFloor.SealCollected)
        {
            terminal.SetColor("bright_yellow");
            terminal.WriteLine(">> A Seal of the Old Gods is hidden on this floor! <<");
            terminal.WriteLine("");
        }

        // Show floor-specific guidance
        ShowFloorGuidance(currentDungeonLevel);

        // Options - standardized format
        terminal.SetColor("cyan");
        terminal.WriteLine("Actions:");
        terminal.WriteLine("");

        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_green");
        terminal.Write("E");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("nter the dungeon      ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("J");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("ournal - Quest progress");

        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_yellow");
        terminal.Write("T");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("eam management        ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_cyan");
        terminal.Write("S");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("tatus");

        terminal.SetColor("darkgray");
        terminal.Write(" [");
        terminal.SetColor("bright_yellow");
        terminal.Write("L");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.Write("evel change (+/- 10)  ");

        terminal.SetColor("darkgray");
        terminal.Write("[");
        terminal.SetColor("bright_red");
        terminal.Write("Q");
        terminal.SetColor("darkgray");
        terminal.Write("]");
        terminal.SetColor("white");
        terminal.WriteLine("uit to town");
        terminal.WriteLine("");
    }

    private string GetFloorFlavorText(DungeonTheme theme)
    {
        return theme switch
        {
            DungeonTheme.Catacombs => "Ancient burial grounds stretch before you. The dead do not rest easy here.",
            DungeonTheme.Sewers => "The stench is overwhelming. Things lurk in the fetid waters below.",
            DungeonTheme.Caverns => "Natural caves twist into darkness. Bioluminescent life casts eerie shadows.",
            DungeonTheme.AncientRuins => "The ruins of a forgotten civilization. Magic still lingers in these stones.",
            DungeonTheme.DemonLair => "Hell has bled into this place. Tortured screams echo endlessly.",
            DungeonTheme.FrozenDepths => "Impossible cold. Your breath freezes. Things are preserved in the ice.",
            DungeonTheme.VolcanicPit => "Rivers of magma light the way. The heat is almost unbearable.",
            DungeonTheme.AbyssalVoid => "Reality breaks down here. What lurks beyond sanity itself?",
            _ => "Darkness awaits."
        };
    }

    /// <summary>
    /// Show floor-specific guidance to help players understand what's coming
    /// </summary>
    private void ShowFloorGuidance(int floor)
    {
        var story = StoryProgressionSystem.Instance;
        string hint = null;
        string color = "gray";

        // Special floor hints - Seal floors match SevenSealsSystem.cs
        // Seal 1 (Creation) = Temple (floor 0), Seal 2 (FirstWar) = 15, Seal 3 (Corruption) = 30
        // Seal 4 (Imprisonment) = 45, Seal 5 (Prophecy) = 60, Seal 6 (Regret) = 80, Seal 7 (Truth) = 99
        if (floor == 15 && !story.CollectedSeals.Contains(SealType.FirstWar))
        {
            hint = "LORE: The Second Seal awaits - it tells of the first war between gods.";
            color = "bright_cyan";
        }
        else if (floor == 25)
        {
            hint = "EVENT: The Possessed Child paradox may appear here. Choose wisely.";
            color = "bright_magenta";
        }
        else if (floor == 30 && !story.CollectedSeals.Contains(SealType.Corruption))
        {
            hint = "LORE: The Third Seal reveals how Manwe corrupted his own children.";
            color = "bright_cyan";
        }
        else if (floor == 45 && !story.CollectedSeals.Contains(SealType.Imprisonment))
        {
            hint = "LORE: The Fourth Seal tells of the eternal chains that bind the gods.";
            color = "bright_cyan";
        }
        else if (floor == 60)
        {
            if (!story.HasStoryFlag("maelketh_encountered"))
            {
                hint = "BOSS: Maelketh, God of War, can be challenged on this floor!";
                color = "bright_red";
            }
            else if (!story.CollectedSeals.Contains(SealType.Prophecy))
            {
                hint = "LORE: The Fifth Seal contains a prophecy about your coming.";
                color = "bright_cyan";
            }
        }
        else if (floor == 65)
        {
            hint = "EVENT: Veloura's Cure paradox may appear if you have the Soulweaver's Loom.";
            color = "bright_magenta";
        }
        else if (floor == 75)
        {
            hint = "MEMORY: Your forgotten past will surface here. Pay attention to dreams.";
            color = "bright_blue";
        }
        else if (floor == 80)
        {
            if (!story.HasStoryFlag("terravok_encountered"))
            {
                hint = "BOSS: Terravok, God of Earth, slumbers here. Will you wake him?";
                color = "bright_yellow";
            }
            else if (!story.CollectedSeals.Contains(SealType.Regret))
            {
                hint = "LORE: The Sixth Seal shows Manwe's regret - his tears crystallized.";
                color = "bright_cyan";
            }
        }
        else if (floor == 95)
        {
            hint = "EVENT: The Destroy Darkness paradox awaits those with the Sunforged Blade.";
            color = "bright_magenta";
        }
        else if (floor == 99 && !story.CollectedSeals.Contains(SealType.Truth))
        {
            hint = "LORE: The Final Seal awaits - the truth of the Ocean Philosophy.";
            color = "bright_cyan";
        }
        else if (floor == 100)
        {
            hint = "FINALE: Manwe awaits. Your choices will determine the ending.";
            color = "bright_white";
        }
        else if (floor >= 50 && floor < 60)
        {
            hint = $"TIP: Floor 60 has the first Old God. Prepare your equipment!";
            color = "yellow";
        }
        else if (floor >= 70 && floor < 80)
        {
            hint = $"TIP: Floor 80 has another Old God. Are you ready?";
            color = "yellow";
        }

        if (hint != null)
        {
            terminal.SetColor(color);
            terminal.WriteLine(hint);
            terminal.WriteLine("");
        }
    }

    protected override string GetBreadcrumbPath()
    {
        var room = currentFloor?.GetCurrentRoom();
        if (room != null && inRoomMode)
        {
            return $"Dungeons > Level {currentDungeonLevel} > {room.Name}";
        }
        return $"Main Street > Dungeons > Level {currentDungeonLevel}";
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        if (string.IsNullOrWhiteSpace(choice))
            return false;

        var upperChoice = choice.ToUpper().Trim();

        // Different handling based on whether we're in room mode or floor overview
        if (inRoomMode)
        {
            return await ProcessRoomChoice(upperChoice);
        }
        else
        {
            return await ProcessOverviewChoice(upperChoice);
        }
    }

    /// <summary>
    /// Process input when viewing floor overview
    /// </summary>
    private async Task<bool> ProcessOverviewChoice(string choice)
    {
        switch (choice)
        {
            case "E":
                // Enter the dungeon - go to first room
                inRoomMode = true;
                currentFloor.CurrentRoomId = currentFloor.EntranceRoomId;
                var entranceRoom = currentFloor.GetCurrentRoom();
                if (entranceRoom != null)
                {
                    entranceRoom.IsExplored = true;
                    roomsExploredThisFloor++;
                    // Auto-clear rooms without monsters
                    if (!entranceRoom.HasMonsters)
                    {
                        entranceRoom.IsCleared = true;
                    }
                }
                terminal.WriteLine("You enter the dungeon...", "gray");
                await Task.Delay(1500);

                // Rare encounter check on dungeon entry
                var player = GetCurrentPlayer();
                if (player != null)
                {
                    await RareEncounters.TryRareEncounter(
                        terminal,
                        player,
                        currentFloor.Theme,
                        currentDungeonLevel
                    );
                }
                return false;

            case "J":
                await ShowStoryJournal();
                return false;

            case "T":
                await ManageTeam();
                return false;

            case "S":
                await ShowStatus();
                return false;

            case "L":
                await ChangeDungeonLevel();
                return false;

            case "Q":
                await NavigateToLocation(GameLocation.MainStreet);
                return true;

            default:
                terminal.WriteLine("Invalid choice.", "red");
                await Task.Delay(1000);
                return false;
        }
    }

    /// <summary>
    /// Show the Story Journal - helps players understand their current objectives
    /// </summary>
    private async Task ShowStoryJournal()
    {
        var player = GetCurrentPlayer();
        var story = StoryProgressionSystem.Instance;
        var seals = SevenSealsSystem.Instance;

        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                    S T O R Y   J O U R N A L               ║");
        terminal.WriteLine("╚════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Current chapter
        terminal.SetColor("white");
        terminal.Write("Current Chapter: ");
        terminal.SetColor("yellow");
        terminal.WriteLine(GetChapterName(story.CurrentChapter));
        terminal.WriteLine("");

        // The main objective
        terminal.SetColor("bright_white");
        terminal.WriteLine("═══ YOUR QUEST ═══");
        terminal.SetColor("white");
        terminal.WriteLine(GetCurrentObjective(story, player));
        terminal.WriteLine("");

        // Seals progress
        terminal.SetColor("bright_yellow");
        terminal.WriteLine($"═══ SEALS OF THE OLD GODS ({story.CollectedSeals.Count}/7) ═══");
        terminal.SetColor("gray");

        foreach (var seal in seals.GetAllSeals())
        {
            if (story.CollectedSeals.Contains(seal.Type))
            {
                terminal.SetColor("green");
                terminal.WriteLine($"  [X] {seal.Name} - {seal.Title}");
            }
            else
            {
                terminal.SetColor("gray");
                // Floor 0 means it's hidden somewhere in town - let the hint guide them
                string locationText = seal.DungeonFloor == 0 ? "Hidden in Town" : $"Dungeon Floor {seal.DungeonFloor}";
                terminal.WriteLine($"  [ ] {seal.Name} - {locationText}");
                terminal.SetColor("dark_cyan");
                terminal.WriteLine($"      Hint: {seal.LocationHint}");
            }
        }
        terminal.WriteLine("");

        // What you know so far
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("═══ WHAT YOU'VE LEARNED ═══");
        terminal.SetColor("white");
        ShowKnownLore(story);
        terminal.WriteLine("");

        // Next steps
        terminal.SetColor("bright_green");
        terminal.WriteLine("═══ SUGGESTED NEXT STEPS ═══");
        terminal.SetColor("white");
        ShowSuggestedSteps(story, player, seals);
        terminal.WriteLine("");

        terminal.SetColor("gray");
        await terminal.GetInputAsync("Press Enter to continue...");
    }

    private string GetChapterName(StoryChapter chapter)
    {
        return chapter switch
        {
            StoryChapter.Awakening => "The Awakening - A stranger in Dorashire",
            StoryChapter.FirstBlood => "First Blood - Learning to fight",
            StoryChapter.TheStranger => "The Stranger - Meeting the mysterious guide",
            StoryChapter.FactionChoice => "Faction Choice - Choosing your allegiance",
            StoryChapter.RisingPower => "Rising Power - Building your strength",
            StoryChapter.TheWhispers => "The Whispers - The gods begin to stir",
            StoryChapter.FirstGod => "First God - Confronting divine power",
            StoryChapter.GodWar => "God War - The Old Gods awaken",
            StoryChapter.TheChoice => "The Choice - Deciding your fate",
            StoryChapter.Ascension => "Ascension - The final preparations",
            StoryChapter.FinalConfrontation => "Final Confrontation - Face the Creator",
            StoryChapter.Epilogue => "Epilogue - The aftermath",
            _ => "Unknown Chapter"
        };
    }

    private string GetCurrentObjective(StoryProgressionSystem story, Character player)
    {
        var level = player?.Level ?? 1;

        if (level < 10)
            return "Explore the dungeons and grow stronger. Something awaits in the depths...";
        if (level < 15)
            return "Descend to floor 15 where ancient blood stains the stones. A Seal awaits.";
        if (story.CollectedSeals.Count == 0)
            return "Find the Seals of the Old Gods hidden throughout the dungeon. They hold the truth.";
        if (level < 50 && story.CollectedSeals.Count < 4)
            return "Continue collecting Seals. Each one reveals more of the divine history.";
        if (level < 60)
            return "The prophecy awaits on floor 60. Discover what the Oracle foretold.";
        if (level < 75)
            return "Descend deeper. Your memories are returning. The truth is close.";
        if (level < 99)
            return "Reach floor 99 for the final Seal. Then face what waits on floor 100.";
        if (story.CollectedSeals.Count < 7)
            return "Collect all Seven Seals before facing Manwe to unlock the true ending.";
        return "Face Manwe, the Creator. Choose your ending. The story ends where it began.";
    }

    private void ShowKnownLore(StoryProgressionSystem story)
    {
        var lorePoints = new List<string>();

        if (story.HasStoryFlag("knows_about_seals"))
            lorePoints.Add("- Seven Seals contain the history of the Old Gods");
        if (story.HasStoryFlag("heard_whispers"))
            lorePoints.Add("- A voice calls you 'wave' - as if you are part of something larger");
        if (story.HasStoryFlag("knows_prophecy"))
            lorePoints.Add("- The prophecy speaks of 'one from beyond the veil' who will decide the gods' fate");
        if (story.CollectedSeals.Count >= 3)
            lorePoints.Add("- Manwe corrupted his own children - the Old Gods - to stop their war");
        if (story.CollectedSeals.Count >= 5)
            lorePoints.Add("- The gods have been imprisoned for ten thousand years, slowly going mad");
        if (story.CollectedSeals.Count >= 7)
            lorePoints.Add("- The 'Ocean Philosophy': You are not a wave fighting the ocean - you ARE the ocean");
        if (story.HasStoryFlag("all_seals_collected"))
            lorePoints.Add("- ALL SEALS COLLECTED - The true ending is now possible!");

        if (lorePoints.Count == 0)
        {
            terminal.WriteLine("  You have not yet discovered the deeper truths...");
            terminal.WriteLine("  Explore the dungeons. Find the Seals. Remember who you are.");
        }
        else
        {
            foreach (var point in lorePoints)
            {
                terminal.WriteLine($"  {point}");
            }
        }
    }

    private void ShowSuggestedSteps(StoryProgressionSystem story, Character player, SevenSealsSystem seals)
    {
        var level = player?.Level ?? 1;
        var steps = new List<string>();

        // Suggest next seal to find
        var nextSeal = seals.GetAllSeals()
            .Where(s => !story.CollectedSeals.Contains(s.Type))
            .OrderBy(s => s.DungeonFloor)
            .FirstOrDefault();

        if (nextSeal != null && nextSeal.DungeonFloor <= level + 10)
        {
            steps.Add($"- Find the {nextSeal.Name} on floor {nextSeal.DungeonFloor}");
        }

        // Level suggestions
        if (level < 15)
            steps.Add("- Reach level 15 to access the first Seal location");
        else if (level < 50)
            steps.Add("- Continue leveling to access deeper dungeon floors");
        else if (level < 100)
            steps.Add("- Push toward floor 100 for the final confrontation");

        // Story suggestions
        if (!story.HasStoryFlag("met_stranger"))
            steps.Add("- Look for 'The Stranger' - a mysterious NPC who knows more than they let on");
        if (story.CollectedSeals.Count < 7 && level >= 50)
            steps.Add("- Collect all 7 Seals to unlock the true ending");

        if (steps.Count == 0)
        {
            steps.Add("- You are ready. Face Manwe on floor 100.");
        }

        foreach (var step in steps.Take(4))
        {
            terminal.WriteLine($"  {step}");
        }
    }

    /// <summary>
    /// Process input when exploring a room
    /// </summary>
    private async Task<bool> ProcessRoomChoice(string choice)
    {
        var room = currentFloor.GetCurrentRoom();
        if (room == null) return false;

        // Check for directional movement
        var direction = choice switch
        {
            "N" => Direction.North,
            "S" => Direction.South,
            "E" when !room.HasEvent || room.EventCompleted => Direction.East, // E is also event
            "W" => Direction.West,
            _ => (Direction?)null
        };

        if (direction.HasValue && room.Exits.ContainsKey(direction.Value))
        {
            await MoveToRoom(room.Exits[direction.Value].TargetRoomId);
            return false;
        }

        // Action-based commands
        switch (choice)
        {
            case "F":
                if (room.HasMonsters && !room.IsCleared)
                {
                    await FightRoomMonsters(room);
                }
                return false;

            case "T":
                if (room.HasTreasure && !room.TreasureLooted && (room.IsCleared || !room.HasMonsters))
                {
                    await CollectTreasure(room);
                }
                return false;

            case "E":
                if (room.HasEvent && !room.EventCompleted)
                {
                    await HandleRoomEvent(room);
                }
                return false;

            case "X":
                if (room.Features.Any(f => !f.IsInteracted))
                {
                    await ExamineFeatures(room);
                }
                return false;

            case "D":
                if (room.HasStairsDown && (room.IsCleared || !room.HasMonsters))
                {
                    await DescendStairs();
                }
                return false;

            case "R":
                if ((room.IsCleared || !room.HasMonsters) && !hasRestThisFloor)
                {
                    await RestInRoom();
                }
                return false;

            case "M":
                await ShowDungeonMap();
                return false;

            case "I":
                await ShowStatus();
                return false;

            case "P":
                await UsePotions();
                return false;

            case "Q":
                // Leave dungeon
                inRoomMode = false;
                return false;

            default:
                terminal.WriteLine("Invalid choice. Use direction keys (N/S/E/W) or action keys.", "red");
                await Task.Delay(1000);
                return false;
        }
    }

    /// <summary>
    /// Move to another room
    /// </summary>
    private async Task MoveToRoom(string targetRoomId)
    {
        var targetRoom = currentFloor.GetRoom(targetRoomId);
        if (targetRoom == null) return;

        // Moving transition
        terminal.ClearScreen();
        terminal.SetColor("gray");
        terminal.WriteLine("You move through the passage...");
        await Task.Delay(800);

        // Check for trap on entering unexplored room
        if (!targetRoom.IsExplored && targetRoom.HasTrap && !targetRoom.TrapTriggered)
        {
            await TriggerTrap(targetRoom);
        }

        // Update current room
        currentFloor.CurrentRoomId = targetRoomId;

        if (!targetRoom.IsExplored)
        {
            targetRoom.IsExplored = true;
            roomsExploredThisFloor++;

            // Auto-clear rooms without monsters
            if (!targetRoom.HasMonsters)
            {
                targetRoom.IsCleared = true;
            }

            // Room discovery message
            terminal.SetColor(GetThemeColor(currentFloor.Theme));
            terminal.WriteLine($"You enter: {targetRoom.Name}");
            await Task.Delay(500);

            // Check for seal discovery on this floor
            var player = GetCurrentPlayer();
            if (player != null && await TryDiscoverSeal(player, targetRoom))
            {
                // Seal was found - give player time to process
                await Task.Delay(500);
            }

            // Auto-trigger riddles and puzzles when entering special rooms for the first time
            if (targetRoom.HasEvent && !targetRoom.EventCompleted)
            {
                // Riddle Gates and Puzzle Rooms require solving to proceed
                if (targetRoom.Type == RoomType.RiddleGate || targetRoom.Type == RoomType.PuzzleRoom)
                {
                    await HandleRoomEvent(targetRoom);
                }
            }

            // Rare encounter check on first visit to a room
            if (player != null)
            {
                bool hadEncounter = await RareEncounters.TryRareEncounter(
                    terminal,
                    player,
                    currentFloor.Theme,
                    currentDungeonLevel
                );

                if (hadEncounter)
                {
                    // Give a brief pause after rare encounter before showing room
                    await Task.Delay(500);
                }
            }
        }

        // If room has monsters and player enters, auto-engage (ambush chance)
        if (targetRoom.HasMonsters && !targetRoom.IsCleared)
        {
            consecutiveMonsterRooms++;

            if (dungeonRandom.NextDouble() < 0.3 && !targetRoom.IsBossRoom)
            {
                terminal.SetColor("red");
                terminal.WriteLine("AMBUSH! The monsters attack!");
                await Task.Delay(1000);
                await FightRoomMonsters(targetRoom);
            }
        }
        else
        {
            consecutiveMonsterRooms = 0;
        }
    }

    /// <summary>
    /// Trigger a trap when entering a room
    /// </summary>
    private async Task TriggerTrap(DungeonRoom room)
    {
        room.TrapTriggered = true;
        var player = GetCurrentPlayer();

        terminal.SetColor("red");
        terminal.WriteLine("*** TRAP! ***");
        await Task.Delay(500);

        var trapType = dungeonRandom.Next(6);
        switch (trapType)
        {
            case 0:
                var pitDmg = currentDungeonLevel * 3 + dungeonRandom.Next(10);
                player.HP -= pitDmg;
                terminal.WriteLine($"The floor gives way! You fall into a pit for {pitDmg} damage!");
                break;

            case 1:
                var dartDmg = currentDungeonLevel * 2 + dungeonRandom.Next(8);
                player.HP -= dartDmg;
                player.Poison = Math.Max(player.Poison, 1);
                terminal.WriteLine($"Poison darts! You take {dartDmg} damage and are poisoned!");
                break;

            case 2:
                var fireDmg = currentDungeonLevel * 4 + dungeonRandom.Next(12);
                player.HP -= fireDmg;
                terminal.WriteLine($"A gout of flame! You take {fireDmg} fire damage!");
                break;

            case 3:
                var goldLost = player.Gold / 10;
                player.Gold -= goldLost;
                terminal.WriteLine($"Acid sprays your belongings! You lose {goldLost} gold!");
                break;

            case 4:
                var expLost = currentDungeonLevel * 50;
                player.Experience = Math.Max(0, player.Experience - expLost);
                terminal.WriteLine($"A curse drains you! You lose {expLost} experience!");
                break;

            case 5:
                terminal.SetColor("green");
                terminal.WriteLine("The trap mechanism is broken. Nothing happens!");
                long bonusGold = currentDungeonLevel * 20;
                player.Gold += bonusGold;
                terminal.WriteLine($"You salvage {bonusGold} gold from the trap parts.");
                break;
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Fight the monsters in a room
    /// </summary>
    private async Task FightRoomMonsters(DungeonRoom room)
    {
        var player = GetCurrentPlayer();

        terminal.ClearScreen();
        terminal.SetColor("red");

        if (room.IsBossRoom)
        {
            terminal.WriteLine("╔═══════════════════════════════════════════════════╗");
            terminal.WriteLine("║              *** BOSS ENCOUNTER ***               ║");
            terminal.WriteLine("╚═══════════════════════════════════════════════════╝");
            terminal.WriteLine("");
            terminal.WriteLine(room.Description);

            // Check for Old God boss encounters on specific floors
            bool hadOldGodEncounter = await TryOldGodBossEncounter(player, room);
            if (hadOldGodEncounter)
            {
                return; // Old God encounter handled the room
            }
        }
        else
        {
            terminal.WriteLine("═══ COMBAT! ═══");
        }

        terminal.WriteLine("");
        await Task.Delay(1000);

        // Generate monsters appropriate for this room
        var monsters = MonsterGenerator.GenerateMonsterGroup(currentDungeonLevel, dungeonRandom);

        // Make boss room monsters tougher
        if (room.IsBossRoom)
        {
            foreach (var m in monsters)
            {
                m.HP = (long)(m.HP * 1.5);
                m.Strength = (int)(m.Strength * 1.3);
            }
            // Ensure there's a boss
            if (!monsters.Any(m => m.IsBoss))
            {
                monsters[0].IsBoss = true;
                monsters[0].Name = GetBossName(currentFloor.Theme);
            }
        }

        // Display what we're fighting
        if (monsters.Count == 1)
        {
            var monster = monsters[0];
            terminal.SetColor(monster.MonsterColor);
            terminal.WriteLine($"A {monster.Name} attacks!");
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"You face {monsters.Count} {monsters[0].Name}{(monsters.Count > 1 ? "s" : "")}!");
        }

        terminal.WriteLine("");
        await Task.Delay(1500);

        // Combat
        var combatEngine = new CombatEngine(terminal);
        await combatEngine.PlayerVsMonsters(player, monsters, teammates, offerMonkEncounter: true);

        // Check if player survived
        if (player.HP > 0)
        {
            room.IsCleared = true;
            currentFloor.MonstersKilled += monsters.Count;

            terminal.SetColor("green");
            terminal.WriteLine("The room is cleared!");

            if (room.IsBossRoom)
            {
                currentFloor.BossDefeated = true;
                terminal.SetColor("bright_yellow");
                terminal.WriteLine("*** BOSS DEFEATED! ***");
                terminal.WriteLine("");

                // Bonus rewards for boss
                long bossGold = currentDungeonLevel * 500 + dungeonRandom.Next(1000);
                long bossExp = currentDungeonLevel * 300;
                player.Gold += bossGold;
                player.Experience += bossExp;

                terminal.WriteLine($"Bonus: {bossGold} gold, {bossExp} experience!");

                // Artifact drop chance for specific floor bosses
                await CheckArtifactDrop(player, currentDungeonLevel);
            }

            await Task.Delay(2000);
        }

        await terminal.PressAnyKey();
    }

    private string GetBossName(DungeonTheme theme)
    {
        return theme switch
        {
            DungeonTheme.Catacombs => "Bone Lord",
            DungeonTheme.Sewers => "Sludge Abomination",
            DungeonTheme.Caverns => "Crystal Guardian",
            DungeonTheme.AncientRuins => "Awakened Golem",
            DungeonTheme.DemonLair => "Pit Fiend",
            DungeonTheme.FrozenDepths => "Frost Wyrm",
            DungeonTheme.VolcanicPit => "Magma Elemental",
            DungeonTheme.AbyssalVoid => "Void Horror",
            _ => "Dungeon Boss"
        };
    }

    /// <summary>
    /// Check for and handle Old God boss encounters on specific floors
    /// Returns true if an Old God encounter was triggered (regardless of outcome)
    /// </summary>
    private async Task<bool> TryOldGodBossEncounter(Character player, DungeonRoom room)
    {
        OldGodType? godType = currentDungeonLevel switch
        {
            25 => OldGodType.Maelketh,   // The Broken Blade - God of War
            40 => OldGodType.Veloura,    // The Fading Heart - Goddess of Love (saveable)
            55 => OldGodType.Thorgrim,   // The Unjust Judge - God of Law
            70 => OldGodType.Noctura,    // The Shadow Queen - Goddess of Shadows (ally-able)
            85 => OldGodType.Aurelion,   // The Dimming Light - God of Light (saveable)
            95 => OldGodType.Terravok,   // The Worldbreaker - God of Earth (awakenable)
            100 => OldGodType.Manwe,     // The Creator - Final Boss
            _ => null
        };

        if (godType == null)
            return false;

        if (!OldGodBossSystem.Instance.CanEncounterBoss(player, godType.Value))
            return false;

        // Display Old God encounter intro based on which god
        terminal.WriteLine("");
        await Task.Delay(1000);

        switch (godType.Value)
        {
            case OldGodType.Maelketh:
                terminal.SetColor("bright_red");
                terminal.WriteLine("The air grows thick with the scent of ancient battlefields...");
                await Task.Delay(1500);
                terminal.WriteLine("MAELKETH, THE BROKEN BLADE, rises before you!", "bright_red");
                terminal.WriteLine("");
                terminal.WriteLine("The God of War speaks:", "yellow");
                terminal.WriteLine("\"Another mortal seeking glory? I have broken ten thousand like you.\"", "red");
                break;

            case OldGodType.Veloura:
                terminal.SetColor("bright_magenta");
                terminal.WriteLine("The scent of dying roses fills the air...");
                await Task.Delay(1500);
                terminal.WriteLine("VELOURA, THE FADING HEART, appears before you!", "bright_magenta");
                terminal.WriteLine("");
                terminal.WriteLine("The Goddess of Love weeps:", "magenta");
                terminal.WriteLine("\"Another heart come to break? Or to be broken?\"", "bright_magenta");
                terminal.WriteLine("\"It matters not. Love always ends in pain.\"", "magenta");
                break;

            case OldGodType.Thorgrim:
                terminal.SetColor("white");
                terminal.WriteLine("The weight of judgment presses upon your soul...");
                await Task.Delay(1500);
                terminal.WriteLine("THORGRIM, THE UNJUST JUDGE, descends!", "white");
                terminal.WriteLine("");
                terminal.WriteLine("The God of Law pronounces:", "gray");
                terminal.WriteLine("\"You are guilty. All are guilty. That is the only truth.\"", "white");
                terminal.WriteLine("\"The sentence is death. It was always death.\"", "gray");
                break;

            case OldGodType.Noctura:
                terminal.SetColor("bright_cyan");
                terminal.WriteLine("Shadows coalesce, forming shapes that watch...");
                await Task.Delay(1500);
                terminal.WriteLine("NOCTURA, THE SHADOW QUEEN, emerges from darkness!", "bright_cyan");
                terminal.WriteLine("");
                terminal.WriteLine("The Goddess of Shadows whispers:", "cyan");
                terminal.WriteLine("\"Interesting. You see me. Most cannot.\"", "bright_cyan");
                terminal.WriteLine("\"I wonder... are you enemy or opportunity?\"", "cyan");
                break;

            case OldGodType.Aurelion:
                terminal.SetColor("bright_yellow");
                terminal.WriteLine("A faint light flickers in the darkness, struggling to persist...");
                await Task.Delay(1500);
                terminal.WriteLine("AURELION, THE DIMMING LIGHT, manifests weakly!", "bright_yellow");
                terminal.WriteLine("");
                terminal.WriteLine("The God of Light speaks faintly:", "yellow");
                terminal.WriteLine("\"You come seeking light, but I have so little left to give.\"", "bright_yellow");
                terminal.WriteLine("\"The darkness grows stronger. Even gods can fade.\"", "yellow");
                break;

            case OldGodType.Terravok:
                terminal.SetColor("bright_yellow");
                terminal.WriteLine("The mountain itself seems to breathe. Stone shifts like flesh...");
                await Task.Delay(1500);
                terminal.WriteLine("TERRAVOK, THE WORLDBREAKER, awakens!", "bright_yellow");
                terminal.WriteLine("");
                terminal.WriteLine("The God of Earth rumbles:", "yellow");
                terminal.WriteLine("\"You dare disturb my slumber? I will return you to the stone.\"", "yellow");
                break;

            case OldGodType.Manwe:
                terminal.SetColor("bright_white");
                terminal.WriteLine("Reality itself trembles. The dream knows it is being watched...");
                await Task.Delay(2000);
                terminal.WriteLine("MANWE, THE CREATOR OF ALL, manifests!", "bright_white");
                terminal.WriteLine("");
                terminal.SetColor("white");
                terminal.WriteLine("The Creator speaks in a voice that is all voices:");
                terminal.SetColor("bright_cyan");
                terminal.WriteLine("\"You have come at last, my wayward child.\"");
                terminal.WriteLine("\"I have watched you from the beginning.\"");
                terminal.WriteLine("\"I AM the beginning. And perhaps... the end.\"");
                break;
        }

        terminal.WriteLine("");
        await Task.Delay(1500);

        string godName = godType.Value switch
        {
            OldGodType.Maelketh => "Maelketh, the Broken Blade",
            OldGodType.Veloura => "Veloura, the Fading Heart",
            OldGodType.Thorgrim => "Thorgrim, the Unjust Judge",
            OldGodType.Noctura => "Noctura, the Shadow Queen",
            OldGodType.Aurelion => "Aurelion, the Dimming Light",
            OldGodType.Terravok => "Terravok, the Worldbreaker",
            OldGodType.Manwe => "Manwe, the Creator",
            _ => "the Old God"
        };

        terminal.SetColor("bright_yellow");
        terminal.WriteLine($"Do you wish to face {godName}? (Y/N)");
        var response = await terminal.GetInput("> ");

        if (response.Trim().ToUpper().StartsWith("Y"))
        {
            var result = await OldGodBossSystem.Instance.StartBossEncounter(player, godType.Value, terminal);
            await HandleGodEncounterResult(result, player, terminal);

            // Mark room as cleared if defeated or alternate outcome achieved
            if (result.Outcome != BossOutcome.Fled && result.Outcome != BossOutcome.PlayerDefeated)
            {
                room.IsCleared = true;
                currentFloor.BossDefeated = true;
            }

            // Special handling for Manwe - trigger ending
            if (godType.Value == OldGodType.Manwe && result.Outcome != BossOutcome.Fled)
            {
                var endingType = EndingsSystem.Instance.DetermineEnding(player);
                await ShowEnding(endingType, player, terminal);
            }

            await terminal.PressAnyKey();
            return true;
        }
        else
        {
            terminal.SetColor("gray");
            string retreatMessage = godType.Value switch
            {
                OldGodType.Maelketh => "You sense the god retreating back into slumber... for now.",
                OldGodType.Terravok => "The mountain settles. Terravok slumbers on.",
                OldGodType.Manwe => "The Creator's presence fades, but you feel his gaze upon you still...",
                _ => "The god withdraws... for now."
            };
            terminal.WriteLine(retreatMessage);
            terminal.WriteLine("The boss room remains unconquered.", "yellow");
            await terminal.PressAnyKey();
            return true; // Still return true - we don't want regular monsters
        }
    }

    /// <summary>
    /// Collect treasure from a cleared room
    /// </summary>
    private async Task CollectTreasure(DungeonRoom room)
    {
        var player = GetCurrentPlayer();
        room.TreasureLooted = true;
        currentFloor.TreasuresFound++;

        terminal.ClearScreen();

        // Display treasure art
        await UsurperRemake.UI.ANSIArt.DisplayArtAnimated(terminal, UsurperRemake.UI.ANSIArt.Treasure, 30);
        terminal.WriteLine("");

        // Scale rewards with level
        long goldFound = currentDungeonLevel * 100 + dungeonRandom.Next(currentDungeonLevel * 200);
        long expFound = currentDungeonLevel * 50 + dungeonRandom.Next(100);

        player.Gold += goldFound;
        player.Experience += expFound;

        terminal.WriteLine($"You find {goldFound} gold pieces!");
        terminal.WriteLine($"You gain {expFound} experience!");

        // Chance for bonus items
        if (dungeonRandom.NextDouble() < 0.3)
        {
            int potions = dungeonRandom.Next(1, 3);
            player.Healing = Math.Min(player.MaxPotions, player.Healing + potions);
            terminal.SetColor("green");
            terminal.WriteLine($"You also find {potions} healing potion{(potions > 1 ? "s" : "")}!");
        }

        await Task.Delay(2500);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Handle room-specific events
    /// </summary>
    private async Task HandleRoomEvent(DungeonRoom room)
    {
        room.EventCompleted = true;

        switch (room.EventType)
        {
            case DungeonEventType.TreasureChest:
                await TreasureChestEncounter();
                break;
            case DungeonEventType.Merchant:
                await MerchantEncounter();
                break;
            case DungeonEventType.Shrine:
                await MysteriousShrine();
                break;
            case DungeonEventType.NPCEncounter:
                await NPCEncounter();
                break;
            case DungeonEventType.Puzzle:
                await PuzzleEncounter();
                break;
            case DungeonEventType.RestSpot:
                await RestSpotEncounter();
                break;
            case DungeonEventType.MysteryEvent:
                await MysteryEventEncounter();
                break;
            case DungeonEventType.Riddle:
                await RiddleGateEncounter();
                break;
            case DungeonEventType.LoreDiscovery:
                await LoreLibraryEncounter();
                break;
            case DungeonEventType.MemoryFlash:
                await MemoryFragmentEncounter();
                break;
            case DungeonEventType.SecretBoss:
                await SecretBossEncounter();
                break;
            default:
                await RandomDungeonEvent();
                break;
        }
    }

    /// <summary>
    /// Examine room features
    /// </summary>
    private async Task ExamineFeatures(DungeonRoom room)
    {
        var unexamined = room.Features.Where(f => !f.IsInteracted).ToList();
        if (unexamined.Count == 0) return;

        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("What do you want to examine?");
        terminal.WriteLine("");

        for (int i = 0; i < unexamined.Count; i++)
        {
            terminal.SetColor("white");
            terminal.Write($"[{i + 1}] ");
            terminal.SetColor("yellow");
            terminal.Write(unexamined[i].Name);
            terminal.SetColor("gray");
            terminal.WriteLine($" ({unexamined[i].Interaction})");
        }

        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("[0] Cancel");
        terminal.WriteLine("");

        var input = await terminal.GetInput("Choice: ");

        if (int.TryParse(input, out int idx) && idx >= 1 && idx <= unexamined.Count)
        {
            await InteractWithFeature(unexamined[idx - 1]);
        }
    }

    /// <summary>
    /// Interact with a specific feature
    /// </summary>
    private async Task InteractWithFeature(RoomFeature feature)
    {
        feature.IsInteracted = true;
        var player = GetCurrentPlayer();

        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine($"You {feature.Interaction.ToString().ToLower()} the {feature.Name}...");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine(feature.Description);
        terminal.WriteLine("");

        await Task.Delay(1000);

        // Random outcome based on interaction type
        var outcome = dungeonRandom.NextDouble();

        switch (feature.Interaction)
        {
            case FeatureInteraction.Examine:
                if (outcome < 0.3)
                {
                    long expGain = currentDungeonLevel * 20;
                    player.Experience += expGain;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"You learn something useful! +{expGain} experience.");
                }
                else if (outcome < 0.5)
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("You find nothing of interest.");
                }
                else
                {
                    terminal.SetColor("cyan");
                    terminal.WriteLine("This might be important to remember...");
                }
                break;

            case FeatureInteraction.Open:
                if (outcome < 0.4)
                {
                    long goldFound = currentDungeonLevel * 50 + dungeonRandom.Next(100);
                    player.Gold += goldFound;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"Inside you find {goldFound} gold!");
                }
                else if (outcome < 0.6)
                {
                    int potions = dungeonRandom.Next(1, 3);
                    player.Healing = Math.Min(player.MaxPotions, player.Healing + potions);
                    terminal.SetColor("green");
                    terminal.WriteLine($"You find {potions} healing potion{(potions > 1 ? "s" : "")}!");
                }
                else if (outcome < 0.75)
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("It's empty.");
                }
                else
                {
                    var damage = currentDungeonLevel * 2;
                    player.HP -= damage;
                    terminal.SetColor("red");
                    terminal.WriteLine($"A trap! You take {damage} damage!");
                }
                break;

            case FeatureInteraction.Search:
                if (outcome < 0.5)
                {
                    long goldFound = currentDungeonLevel * 30 + dungeonRandom.Next(50);
                    player.Gold += goldFound;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"Hidden among the debris: {goldFound} gold!");
                }
                else
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("You find nothing useful.");
                }
                break;

            case FeatureInteraction.Read:
                long expBonus = currentDungeonLevel * 30;
                player.Experience += expBonus;
                terminal.SetColor("cyan");
                terminal.WriteLine($"Ancient knowledge! +{expBonus} experience.");
                break;

            case FeatureInteraction.Take:
                if (outcome < 0.6)
                {
                    long value = currentDungeonLevel * 40 + dungeonRandom.Next(80);
                    player.Gold += value;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"You pocket something worth {value} gold.");
                }
                else
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("It crumbles as you touch it. Worthless.");
                }
                break;

            case FeatureInteraction.Use:
                if (outcome < 0.3)
                {
                    player.HP = Math.Min(player.MaxHP, player.HP + player.MaxHP / 4);
                    terminal.SetColor("green");
                    terminal.WriteLine("A healing aura washes over you!");
                }
                else if (outcome < 0.6)
                {
                    long goldBonus = currentDungeonLevel * 100;
                    player.Gold += goldBonus;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"A hidden cache opens! {goldBonus} gold!");
                }
                else
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("Nothing happens.");
                }
                break;

            case FeatureInteraction.Break:
                if (outcome < 0.4)
                {
                    long goldFound = currentDungeonLevel * 60 + dungeonRandom.Next(100);
                    player.Gold += goldFound;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"Valuables spill out! {goldFound} gold!");
                }
                else if (outcome < 0.7)
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("Just rubble now.");
                }
                else
                {
                    var damage = currentDungeonLevel * 3;
                    player.HP -= damage;
                    terminal.SetColor("red");
                    terminal.WriteLine($"Something explodes! {damage} damage!");
                }
                break;

            case FeatureInteraction.Enter:
                // Secret passage - random bonus room
                terminal.SetColor("cyan");
                terminal.WriteLine("You squeeze through into a hidden space...");
                await Task.Delay(1000);
                if (outcome < 0.5)
                {
                    long treasureGold = currentDungeonLevel * 150 + dungeonRandom.Next(200);
                    long treasureExp = currentDungeonLevel * 75;
                    player.Gold += treasureGold;
                    player.Experience += treasureExp;
                    terminal.SetColor("yellow");
                    terminal.WriteLine($"A secret cache! {treasureGold} gold, {treasureExp} exp!");
                }
                else
                {
                    terminal.SetColor("gray");
                    terminal.WriteLine("Just a dead end with some old bones.");
                }
                break;
        }

        await Task.Delay(2000);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Descend to the next floor
    /// </summary>
    private async Task DescendStairs()
    {
        // No level cap - players can descend as deep as they dare
        if (currentDungeonLevel >= maxDungeonLevel)
        {
            terminal.WriteLine("You have reached the deepest level of the dungeon.", "red");
            terminal.WriteLine("There is nowhere left to descend.", "yellow");
            await Task.Delay(2000);
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor("blue");
        terminal.WriteLine("You descend the ancient stairs...");
        terminal.WriteLine("The darkness grows deeper.");
        terminal.WriteLine("The air grows colder.");
        await Task.Delay(2000);

        currentDungeonLevel++;
        currentFloor = DungeonGenerator.GenerateFloor(currentDungeonLevel);
        roomsExploredThisFloor = 0;
        hasRestThisFloor = false;
        consecutiveMonsterRooms = 0;

        // Update quest progress for reaching this floor
        var player = GetCurrentPlayer();
        if (player != null)
        {
            QuestSystem.OnDungeonFloorReached(player, currentDungeonLevel);
        }

        // Start in entrance room
        currentFloor.CurrentRoomId = currentFloor.EntranceRoomId;
        var entranceRoom = currentFloor.GetCurrentRoom();
        if (entranceRoom != null)
        {
            entranceRoom.IsExplored = true;
            roomsExploredThisFloor++;
            // Auto-clear rooms without monsters
            if (!entranceRoom.HasMonsters)
            {
                entranceRoom.IsCleared = true;
            }
        }

        terminal.SetColor(GetThemeColor(currentFloor.Theme));
        terminal.WriteLine("");
        terminal.WriteLine($"You arrive at Level {currentDungeonLevel}");
        terminal.WriteLine($"Theme: {currentFloor.Theme}");
        terminal.WriteLine("");
        terminal.SetColor("gray");
        terminal.WriteLine(GetFloorFlavorText(currentFloor.Theme));

        // Check for story events (seals, narrative moments) on this new floor
        await CheckFloorStoryEvents(player, terminal);

        await Task.Delay(2500);
    }

    /// <summary>
    /// Rest in a cleared room (once per floor)
    /// </summary>
    private async Task RestInRoom()
    {
        var player = GetCurrentPlayer();

        terminal.ClearScreen();
        terminal.SetColor("green");
        terminal.WriteLine("You find a defensible corner and rest...");
        terminal.WriteLine("");

        await Task.Delay(1500);

        // Heal 25% of max HP
        long healAmount = player.MaxHP / 4;
        player.HP = Math.Min(player.MaxHP, player.HP + healAmount);

        terminal.WriteLine($"You recover {healAmount} hit points.");
        terminal.WriteLine($"HP: {player.HP}/{player.MaxHP}");

        hasRestThisFloor = true;

        terminal.WriteLine("");
        terminal.SetColor("gray");
        terminal.WriteLine("You feel rested, but dare not linger too long.");

        await Task.Delay(2500);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Change dungeon level from overview
    /// </summary>
    private async Task ChangeDungeonLevel()
    {
        var playerLevel = GetCurrentPlayer()?.Level ?? 1;

        terminal.WriteLine("");
        terminal.WriteLine($"Current level: {currentDungeonLevel}", "white");
        terminal.WriteLine($"Your level: {playerLevel}", "cyan");
        terminal.WriteLine($"Accessible range: 1 - {maxDungeonLevel} (no restrictions)", "yellow");
        terminal.WriteLine("");

        var input = await terminal.GetInput("Enter target level (or +/- for relative): ");

        int targetLevel = currentDungeonLevel;

        if (input.StartsWith("+") && int.TryParse(input.Substring(1), out int plus))
        {
            targetLevel = currentDungeonLevel + plus;
        }
        else if (input.StartsWith("-") && int.TryParse(input.Substring(1), out int minus))
        {
            targetLevel = currentDungeonLevel - minus;
        }
        else if (int.TryParse(input, out int absolute))
        {
            targetLevel = absolute;
        }

        // Clamp to valid dungeon range (1 to max)
        targetLevel = Math.Max(1, Math.Min(maxDungeonLevel, targetLevel));

        if (targetLevel != currentDungeonLevel)
        {
            currentDungeonLevel = targetLevel;
            currentFloor = DungeonGenerator.GenerateFloor(currentDungeonLevel);
            roomsExploredThisFloor = 0;
            hasRestThisFloor = false;
            consecutiveMonsterRooms = 0;

            terminal.WriteLine($"Dungeon level set to {currentDungeonLevel}.", "green");

            // Check for story events (seals, narrative moments) on this new floor
            var player = GetCurrentPlayer();
            if (player != null)
            {
                await CheckFloorStoryEvents(player, terminal);
            }
        }
        else
        {
            terminal.WriteLine("No change to dungeon level.", "gray");
        }

        await Task.Delay(1500);
    }
    
    /// <summary>
    /// Main exploration mechanic - Pascal encounter system
    /// </summary>
    private async Task ExploreLevel()
    {
        var currentPlayer = GetCurrentPlayer();

        // No turn/fight limits in the new persistent system - explore freely!

        terminal.ClearScreen();
        terminal.SetColor("yellow");
        terminal.WriteLine("═══ EXPLORING ═══");
        terminal.WriteLine("");

        // Atmospheric exploration text
        await ShowExplorationText();

        // Determine encounter type: 90% monsters, 10% special events
        var encounterRoll = dungeonRandom.NextDouble();

        if (encounterRoll < MonsterEncounterChance)
        {
            await MonsterEncounter();
        }
        else
        {
            await SpecialEventEncounter();
        }

        await Task.Delay(1000);
        await terminal.PressAnyKey();
    }
    
    /// <summary>
    /// Monster encounter - Pascal DUNGEVC.PAS mechanics
    /// </summary>
    private async Task MonsterEncounter()
    {
        terminal.SetColor("red");
        terminal.WriteLine("▼ MONSTER ENCOUNTER ▼");
        terminal.WriteLine("");

        // Use new MonsterGenerator to create level-appropriate monsters
        var monsters = MonsterGenerator.GenerateMonsterGroup(currentDungeonLevel, dungeonRandom);

        var combatEngine = new CombatEngine(terminal);

        // Display encounter message with color
        if (monsters.Count == 1)
        {
            var monster = monsters[0];
            if (monster.IsBoss)
            {
                terminal.SetColor("bright_red");
                terminal.WriteLine($"⚠ A powerful [{monster.MonsterColor}]{monster.Name}[/] blocks your path! ⚠");
            }
            else
            {
                terminal.SetColor(monster.MonsterColor);
                terminal.WriteLine($"A [{monster.MonsterColor}]{monster.Name}[/] appears!");
            }
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.Write($"You encounter a group of [{monsters[0].MonsterColor}]{monsters.Count} {monsters[0].Name}");
            if (monsters[0].FamilyName != "")
            {
                terminal.Write($"[/] from the {monsters[0].FamilyName} family!");
            }
            else
            {
                terminal.Write("[/]!");
            }
            terminal.WriteLine("");
        }

        // Show difficulty assessment
        ShowDifficultyAssessment(monsters, GetCurrentPlayer());

        terminal.WriteLine("");
        await Task.Delay(2000);

        // Use new PlayerVsMonsters method - ALL monsters fight at once!
        // Monk will appear after ALL monsters are defeated
        await combatEngine.PlayerVsMonsters(GetCurrentPlayer(), monsters, teammates, offerMonkEncounter: true);
    }

    /// <summary>
    /// Show difficulty assessment before combat
    /// </summary>
    private void ShowDifficultyAssessment(List<Monster> monsters, Character player)
    {
        // Calculate total monster threat
        long totalMonsterHP = monsters.Sum(m => m.HP);
        long totalMonsterStr = monsters.Sum(m => m.Strength);
        int avgMonsterLevel = (int)monsters.Average(m => m.Level);

        // Calculate player power
        long playerPower = player.Strength + player.WeapPow + (player.Level * 5);
        long monsterPower = totalMonsterStr + avgMonsterLevel * 5;

        // Estimate difficulty
        float powerRatio = monsterPower > 0 ? (float)playerPower / monsterPower : 2f;
        float hpRatio = player.MaxHP > 0 ? (float)totalMonsterHP / player.MaxHP : 1f;

        string difficulty;
        string diffColor;
        string xpHint;

        // Calculate estimated XP
        long estXP = monsters.Sum(m => (long)(Math.Pow(m.Level, 1.5) * 15));
        estXP = DifficultySystem.ApplyExperienceMultiplier(estXP);

        if (powerRatio > 2.0f && hpRatio < 0.5f)
        {
            difficulty = "Trivial";
            diffColor = "darkgray";
            xpHint = $"~{estXP} XP (not worth your time)";
        }
        else if (powerRatio > 1.5f && hpRatio < 1.0f)
        {
            difficulty = "Easy";
            diffColor = "bright_green";
            xpHint = $"~{estXP} XP";
        }
        else if (powerRatio > 1.0f && hpRatio < 1.5f)
        {
            difficulty = "Fair";
            diffColor = "green";
            xpHint = $"~{estXP} XP";
        }
        else if (powerRatio > 0.7f && hpRatio < 2.5f)
        {
            difficulty = "Challenging";
            diffColor = "yellow";
            xpHint = $"~{estXP} XP (bring potions)";
        }
        else if (powerRatio > 0.5f)
        {
            difficulty = "Dangerous";
            diffColor = "bright_yellow";
            xpHint = $"~{estXP} XP (high risk)";
        }
        else
        {
            difficulty = "DEADLY";
            diffColor = "bright_red";
            xpHint = $"~{estXP} XP (flee recommended!)";
        }

        terminal.WriteLine("");
        terminal.SetColor("gray");
        terminal.Write("  Threat: ");
        terminal.SetColor(diffColor);
        terminal.Write(difficulty);
        terminal.SetColor("gray");
        terminal.WriteLine($"  |  {xpHint}");
    }
    
    /// <summary>
    /// Magic scroll encounter - Pascal scroll mechanics
    /// </summary>
    private async Task HandleMagicScroll()
    {
        terminal.WriteLine("You have found a scroll! It reads:");
        terminal.WriteLine("");
        
        var scrollType = dungeonRandom.Next(3);
        var currentPlayer = GetCurrentPlayer();
        
        switch (scrollType)
        {
            case 0: // Blessing scroll
                terminal.SetColor("bright_white");
                terminal.WriteLine("Utter: 'XAVARANTHE JHUSULMAX VASWIUN'");
                terminal.WriteLine("And you will receive a blessing.");
                break;
                
            case 1: // Undead summon scroll  
                terminal.SetColor("red");
                terminal.WriteLine("Utter: 'ZASHNIVANTHE ULIPMAN NO SEE'");
                terminal.WriteLine("And you will see ancient power rise again.");
                break;
                
            case 2: // Secret cave scroll
                terminal.SetColor("cyan");
                terminal.WriteLine("Utter: 'RANTVANTHI SHGELUUIM VARTHMIOPLXH'");
                terminal.WriteLine("And you will be given opportunities.");
                break;
        }
        
        terminal.WriteLine("");
        var recite = await terminal.GetInput("Recite the scroll? (Y/N): ");
        
        if (recite.ToUpper() == "Y")
        {
            await ExecuteScrollMagic(scrollType, currentPlayer);
        }
        else
        {
            terminal.WriteLine("You carefully store the scroll for later.", "gray");
        }
    }
    
    /// <summary>
    /// Execute scroll magic effects
    /// </summary>
    private async Task ExecuteScrollMagic(int scrollType, Character player)
    {
        terminal.WriteLine("");
        terminal.WriteLine("The ancient words resonate with power...", "bright_white");
        await Task.Delay(2000);
        
        switch (scrollType)
        {
            case 0: // Blessing
                {
                    long chivalryGain = dungeonRandom.Next(500) + 50;
                    player.Chivalry += chivalryGain;
                    
                    terminal.SetColor("bright_yellow");
                    terminal.WriteLine("Divine light surrounds you!");
                    terminal.WriteLine($"Your chivalry increases by {chivalryGain}!");
                }
                break;
                
            case 1: // Undead summon (triggers combat)
                {
                    terminal.SetColor("red");
                    terminal.WriteLine("The ground trembles as ancient evil awakens!");
                    await Task.Delay(2000);
                    
                    // Create undead monster
                    var undead = CreateUndeadMonster();
                    terminal.WriteLine($"You have summoned a {undead.Name}!");
                    
                    // Fight the undead
                    var combatEngine = new CombatEngine(terminal);
                    await combatEngine.PlayerVsMonster(player, undead, teammates);
                }
                break;
                
            case 2: // Secret opportunity
                {
                    terminal.SetColor("bright_cyan");
                    terminal.WriteLine("A hidden passage opens before you!");
                    
                    long bonusGold = currentDungeonLevel * 2000;
                    player.Gold += bonusGold;
                    
                    terminal.WriteLine($"You discover {bonusGold} gold in the secret chamber!");
                }
                break;
        }
    }
    
    /// <summary>
    /// Special event encounters - Based on Pascal DUNGEVC.PAS and DUNGEV2.PAS
    /// Includes positive, negative, and neutral events for variety
    /// </summary>
    private async Task SpecialEventEncounter()
    {
        // 12 different event types based on original Pascal
        var eventType = dungeonRandom.Next(12);

        switch (eventType)
        {
            case 0:
                await TreasureChestEncounter();
                break;
            case 1:
                await PotionCacheEncounter();
                break;
            case 2:
                await MerchantEncounter();
                break;
            case 3:
                await WitchDoctorEncounter();
                break;
            case 4:
                await BeggarEncounter();
                break;
            case 5:
                await StrangersEncounter();
                break;
            case 6:
                await HarassedWomanEncounter();
                break;
            case 7:
                await WoundedManEncounter();
                break;
            case 8:
                await MysteriousShrine();
                break;
            case 9:
                await TrapEncounter();
                break;
            case 10:
                await AncientScrollEncounter();
                break;
            case 11:
                await GamblingGhostEncounter();
                break;
        }
    }

    /// <summary>
    /// Treasure chest encounter - Classic Pascal treasure mechanics
    /// </summary>
    private async Task TreasureChestEncounter()
    {
        terminal.SetColor("yellow");
        terminal.WriteLine("* TREASURE CHEST *");
        terminal.WriteLine("");

        terminal.WriteLine("You discover an ancient chest hidden in the shadows!", "cyan");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(O)pen the chest or (L)eave it alone? ");

        var currentPlayer = GetCurrentPlayer();

        if (choice.ToUpper() == "O")
        {
            // 70% good, 20% trap, 10% mimic
            var chestRoll = dungeonRandom.Next(10);

            if (chestRoll < 7)
            {
                // Good treasure!
                long goldFound = currentDungeonLevel * 1500 + dungeonRandom.Next(5000);
                long expGained = currentDungeonLevel * 200 + dungeonRandom.Next(3000);

                terminal.SetColor("green");
                terminal.WriteLine("The chest opens to reveal glittering treasure!");
                terminal.WriteLine($"You find {goldFound} gold pieces!");
                terminal.WriteLine($"You gain {expGained} experience!");

                currentPlayer.Gold += goldFound;
                currentPlayer.Experience += expGained;
            }
            else if (chestRoll < 9)
            {
                // Trap!
                terminal.SetColor("red");
                terminal.WriteLine("CLICK! It's a trap!");

                var trapType = dungeonRandom.Next(3);
                switch (trapType)
                {
                    case 0:
                        var poisonDmg = currentDungeonLevel * 5;
                        currentPlayer.HP -= poisonDmg;
                        terminal.WriteLine($"Poison gas! You take {poisonDmg} damage!");
                        currentPlayer.Poison = Math.Max(currentPlayer.Poison, 1);
                        terminal.WriteLine("You have been poisoned!", "magenta");
                        break;
                    case 1:
                        var spikeDmg = currentDungeonLevel * 8;
                        currentPlayer.HP -= spikeDmg;
                        terminal.WriteLine($"Spikes shoot out! You take {spikeDmg} damage!");
                        break;
                    case 2:
                        var goldLost = currentPlayer.Gold / 10;
                        currentPlayer.Gold -= goldLost;
                        terminal.WriteLine($"Acid sprays your coin pouch! You lose {goldLost} gold!");
                        break;
                }
            }
            else
            {
                // Mimic! (triggers combat)
                terminal.SetColor("bright_red");
                terminal.WriteLine("The chest MOVES! It's a MIMIC!");
                await Task.Delay(1500);

                var mimic = Monster.CreateMonster(
                    currentDungeonLevel, "Mimic",
                    currentDungeonLevel * 15, currentDungeonLevel * 4, 0,
                    "Fooled you!", false, false, "Teeth", "Wooden Shell",
                    false, false, currentDungeonLevel * 5, currentDungeonLevel * 3, currentDungeonLevel * 3
                );
                mimic.IsBoss = true;
                mimic.Level = currentDungeonLevel;

                var combatEngine = new CombatEngine(terminal);
                await combatEngine.PlayerVsMonster(currentPlayer, mimic, teammates);
            }
        }
        else
        {
            terminal.WriteLine("You wisely leave the chest alone and continue on.", "gray");
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Strangers encounter - Band of rogues/orcs (from Pascal DUNGEV2.PAS)
    /// </summary>
    private async Task StrangersEncounter()
    {
        terminal.SetColor("red");
        terminal.WriteLine("=== STRANGERS APPROACH ===");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();
        var groupType = dungeonRandom.Next(4);
        string groupName;
        string[] memberTypes;

        switch (groupType)
        {
            case 0:
                groupName = "orcs";
                memberTypes = new[] { "Orc", "Half-Orc", "Orc Raider" };
                terminal.WriteLine("A group of orcs emerges from the shadows!", "gray");
                terminal.WriteLine("They are poorly armed with sticks and clubs.", "gray");
                break;
            case 1:
                groupName = "trolls";
                memberTypes = new[] { "Troll", "Half-Troll", "Lumber-Troll" };
                terminal.WriteLine("A band of trolls blocks your path!", "green");
                terminal.WriteLine("They carry clubs and spears.", "gray");
                break;
            case 2:
                groupName = "rogues";
                memberTypes = new[] { "Rogue", "Thief", "Pirate" };
                terminal.WriteLine("A gang of rogues surrounds you!", "cyan");
                terminal.WriteLine("They brandish knives and rapiers.", "gray");
                break;
            default:
                groupName = "dwarves";
                memberTypes = new[] { "Dwarf", "Dwarf Warrior", "Dwarf Scout" };
                terminal.WriteLine("A group of armed dwarves approaches!", "yellow");
                terminal.WriteLine("They carry swords and axes.", "gray");
                break;
        }

        terminal.WriteLine("");
        terminal.WriteLine("Their leader demands your gold!", "white");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(F)ight them, (P)ay them off, or try to (E)scape? ");

        if (choice.ToUpper() == "F")
        {
            terminal.WriteLine("You draw your weapon and prepare for battle!", "yellow");
            await Task.Delay(1500);

            // Create the group
            int groupSize = dungeonRandom.Next(3, 6);
            var monsters = new List<Monster>();

            for (int i = 0; i < groupSize; i++)
            {
                var name = memberTypes[dungeonRandom.Next(memberTypes.Length)];
                if (i == 0) name = name + " Leader";

                var monster = Monster.CreateMonster(
                    currentDungeonLevel, name,
                    currentDungeonLevel * (i == 0 ? 8 : 4),
                    currentDungeonLevel * 2, 0,
                    "Attack!", false, false, "Weapon", "Armor",
                    false, false, currentDungeonLevel * 2, currentDungeonLevel, currentDungeonLevel * 2
                );
                monster.Level = currentDungeonLevel;
                if (i == 0) monster.IsBoss = true;
                monsters.Add(monster);
            }

            var combatEngine = new CombatEngine(terminal);
            await combatEngine.PlayerVsMonsters(currentPlayer, monsters, teammates, offerMonkEncounter: false);
        }
        else if (choice.ToUpper() == "P")
        {
            long bribe = currentDungeonLevel * 500 + dungeonRandom.Next(1000);
            if (currentPlayer.Gold >= bribe)
            {
                currentPlayer.Gold -= bribe;
                terminal.WriteLine($"You reluctantly hand over {bribe} gold.", "yellow");
                terminal.WriteLine($"The {groupName} leave you in peace.", "gray");
            }
            else
            {
                terminal.WriteLine("You don't have enough gold!", "red");
                terminal.WriteLine("They attack anyway!", "red");
                await Task.Delay(1500);
                // Trigger simplified combat
                var monster = Monster.CreateMonster(
                    currentDungeonLevel, $"{groupName.Substring(0, 1).ToUpper()}{groupName.Substring(1)} Leader",
                    currentDungeonLevel * 10, currentDungeonLevel * 3, 0,
                    "No gold means death!", false, false, "Weapon", "Armor",
                    false, false, currentDungeonLevel * 3, currentDungeonLevel * 2, currentDungeonLevel * 2
                );
                var combatEngine = new CombatEngine(terminal);
                await combatEngine.PlayerVsMonster(currentPlayer, monster, teammates);
            }
        }
        else
        {
            // Escape attempt - 60% chance
            if (dungeonRandom.NextDouble() < 0.6)
            {
                terminal.WriteLine("You manage to slip away into the shadows!", "green");
            }
            else
            {
                terminal.WriteLine("They catch you trying to escape!", "red");
                long stolen = currentPlayer.Gold / 5;
                currentPlayer.Gold -= stolen;
                terminal.WriteLine($"They beat you and steal {stolen} gold!", "red");
            }
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Harassed woman encounter - Moral choice event (from Pascal DUNGEVC.PAS)
    /// </summary>
    private async Task HarassedWomanEncounter()
    {
        terminal.SetColor("magenta");
        terminal.WriteLine("♀ DAMSEL IN DISTRESS ♀");
        terminal.WriteLine("");

        terminal.WriteLine("You hear screams echoing through the corridor!", "white");
        terminal.WriteLine("A woman is being harassed by a band of ruffians.", "gray");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(H)elp her, (I)gnore the situation, or (J)oin the ruffians? ");

        var currentPlayer = GetCurrentPlayer();

        if (choice.ToUpper() == "H")
        {
            terminal.WriteLine("You rush to her defense!", "green");
            terminal.WriteLine("\"Unhand her, villains!\"", "yellow");
            await Task.Delay(1500);

            // Fight ruffians
            var monsters = new List<Monster>();
            int count = dungeonRandom.Next(2, 4);
            for (int i = 0; i < count; i++)
            {
                var name = i == 0 ? "Ruffian Leader" : "Ruffian";
                var monster = Monster.CreateMonster(
                    currentDungeonLevel, name,
                    currentDungeonLevel * (i == 0 ? 6 : 3), currentDungeonLevel * 2, 0,
                    "Mind your own business!", false, false, "Knife", "Rags",
                    false, false, currentDungeonLevel * 2, currentDungeonLevel, currentDungeonLevel
                );
                monster.Level = currentDungeonLevel;
                monsters.Add(monster);
            }

            var combatEngine = new CombatEngine(terminal);
            await combatEngine.PlayerVsMonsters(currentPlayer, monsters, teammates, offerMonkEncounter: false);

            if (currentPlayer.HP > 0)
            {
                terminal.WriteLine("");
                terminal.WriteLine("The woman thanks you profusely!", "cyan");
                long reward = currentDungeonLevel * 300 + dungeonRandom.Next(500);
                long chivGain = dungeonRandom.Next(50) + 30;

                terminal.WriteLine($"She rewards you with {reward} gold!", "yellow");
                terminal.WriteLine($"Your chivalry increases by {chivGain}!", "white");

                currentPlayer.Gold += reward;
                currentPlayer.Chivalry += chivGain;
            }
        }
        else if (choice.ToUpper() == "J")
        {
            terminal.SetColor("red");
            terminal.WriteLine("You join the ruffians in their villainy!");
            terminal.WriteLine("This is a shameful act!");

            long stolen = dungeonRandom.Next(200) + 50;
            long darkGain = dungeonRandom.Next(75) + 50;

            currentPlayer.Gold += stolen;
            currentPlayer.Darkness += darkGain;

            terminal.WriteLine($"You steal {stolen} gold from the woman.", "yellow");
            terminal.WriteLine($"Your darkness increases by {darkGain}!", "magenta");
        }
        else
        {
            terminal.WriteLine("You turn away and pretend not to notice.", "gray");
            terminal.WriteLine("Her cries fade as you continue your journey...", "gray");
        }

        await Task.Delay(2500);
    }

    /// <summary>
    /// Wounded man encounter - Healing quest (from Pascal DUNGEVC.PAS)
    /// </summary>
    private async Task WoundedManEncounter()
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("✚ WOUNDED STRANGER ✚");
        terminal.WriteLine("");

        terminal.WriteLine("You find a wounded man lying against the wall.", "white");
        terminal.WriteLine("He is bleeding heavily and begs for help.", "gray");
        terminal.WriteLine("\"Please... I need healing... I can pay...\"", "yellow");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(H)elp him, (R)ob him, or (L)eave him? ");

        var currentPlayer = GetCurrentPlayer();

        if (choice.ToUpper() == "H")
        {
            if (currentPlayer.Healing > 0)
            {
                currentPlayer.Healing--;
                terminal.WriteLine("You use a healing potion on the wounded stranger.", "green");
                terminal.WriteLine("He recovers enough to stand.", "white");

                long reward = currentDungeonLevel * 500 + dungeonRandom.Next(1000);
                long chivGain = dungeonRandom.Next(40) + 20;

                terminal.WriteLine($"\"Thank you, hero! Take this reward: {reward} gold!\"", "yellow");
                terminal.WriteLine($"Your chivalry increases by {chivGain}!", "white");

                currentPlayer.Gold += reward;
                currentPlayer.Chivalry += chivGain;
            }
            else
            {
                terminal.WriteLine("You have no healing potions to spare!", "red");
                terminal.WriteLine("You try to bandage his wounds with cloth...", "gray");

                if (dungeonRandom.NextDouble() < 0.5)
                {
                    terminal.WriteLine("It seems to help a little.", "green");
                    currentPlayer.Chivalry += 10;
                }
                else
                {
                    terminal.WriteLine("Unfortunately, he dies from his wounds.", "red");
                }
            }
        }
        else if (choice.ToUpper() == "R")
        {
            terminal.SetColor("red");
            terminal.WriteLine("You search the dying man's belongings...");

            long stolen = dungeonRandom.Next(500) + 100;
            long darkGain = dungeonRandom.Next(80) + 60;

            terminal.WriteLine($"You find {stolen} gold in his purse.", "yellow");
            terminal.WriteLine($"Your darkness increases by {darkGain}!", "magenta");

            currentPlayer.Gold += stolen;
            currentPlayer.Darkness += darkGain;

            terminal.WriteLine("He dies cursing your name...", "gray");
        }
        else
        {
            terminal.WriteLine("You step over the dying man and continue on.", "gray");
            terminal.WriteLine("His moans fade behind you...", "gray");
        }

        await Task.Delay(2500);
    }

    /// <summary>
    /// Mysterious shrine - Random buff or debuff
    /// Also handles Lyris companion recruitment on floor 15
    /// </summary>
    private async Task MysteriousShrine()
    {
        var currentPlayer = GetCurrentPlayer();

        // Check for Lyris companion encounter on floor 15
        if (currentDungeonLevel == 15 && await TryLyrisRecruitment(currentPlayer))
        {
            return; // Lyris encounter handled
        }

        terminal.SetColor("bright_cyan");
        terminal.WriteLine("✦ MYSTERIOUS SHRINE ✦");
        terminal.WriteLine("");

        terminal.WriteLine("You discover an ancient shrine glowing with strange light.", "white");
        terminal.WriteLine("Offerings of gold and bones surround a stone altar.", "gray");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(P)ray at the shrine, (D)esecrate it, or (L)eave? ");

        if (choice.ToUpper() == "P")
        {
            terminal.WriteLine("You kneel before the ancient shrine...", "cyan");
            await Task.Delay(1500);

            // Random blessing or curse
            var outcome = dungeonRandom.Next(6);
            switch (outcome)
            {
                case 0:
                    terminal.WriteLine("Divine light fills you!", "bright_yellow");
                    currentPlayer.HP = currentPlayer.MaxHP;
                    terminal.WriteLine("You are fully healed!", "green");
                    break;
                case 1:
                    var strBonus = dungeonRandom.Next(5) + 1;
                    currentPlayer.Strength += strBonus;
                    terminal.WriteLine($"You feel stronger! +{strBonus} Strength!", "green");
                    break;
                case 2:
                    var expBonus = currentDungeonLevel * 500;
                    currentPlayer.Experience += expBonus;
                    terminal.WriteLine($"Ancient wisdom flows into you! +{expBonus} EXP!", "yellow");
                    break;
                case 3:
                    terminal.WriteLine("The shrine is silent...", "gray");
                    terminal.WriteLine("Nothing happens.", "gray");
                    break;
                case 4:
                    var hpLoss = currentPlayer.HP / 4;
                    currentPlayer.HP -= hpLoss;
                    terminal.WriteLine("The shrine drains your life force!", "red");
                    terminal.WriteLine($"You lose {hpLoss} HP!", "red");
                    break;
                case 5:
                    var goldLoss = currentPlayer.Gold / 5;
                    currentPlayer.Gold -= goldLoss;
                    terminal.WriteLine("Your gold dissolves into the altar!", "red");
                    terminal.WriteLine($"You lose {goldLoss} gold!", "red");
                    break;
            }
        }
        else if (choice.ToUpper() == "D")
        {
            terminal.SetColor("red");
            terminal.WriteLine("You smash the shrine and steal the offerings!");

            long stolen = currentDungeonLevel * 200 + dungeonRandom.Next(500);
            long darkGain = dungeonRandom.Next(50) + 30;

            terminal.WriteLine($"You find {stolen} gold among the offerings!", "yellow");
            terminal.WriteLine($"Your darkness increases by {darkGain}!", "magenta");

            currentPlayer.Gold += stolen;
            currentPlayer.Darkness += darkGain;

            // Chance of angering spirits
            if (dungeonRandom.NextDouble() < 0.3)
            {
                terminal.WriteLine("");
                terminal.WriteLine("An angry spirit emerges from the ruined shrine!", "bright_red");
                await Task.Delay(1500);

                var spirit = Monster.CreateMonster(
                    currentDungeonLevel + 5, "Vengeful Spirit",
                    currentDungeonLevel * 12, currentDungeonLevel * 4, 0,
                    "You will pay for your sacrilege!", false, false, "Spectral Claws", "Ethereal Form",
                    false, false, currentDungeonLevel * 4, currentDungeonLevel * 3, currentDungeonLevel * 3
                );
                spirit.IsBoss = true;

                var combatEngine = new CombatEngine(terminal);
                await combatEngine.PlayerVsMonster(currentPlayer, spirit, teammates);
            }
        }
        else
        {
            terminal.WriteLine("You wisely leave the mysterious shrine alone.", "gray");
        }

        await Task.Delay(2000);
    }

    /// <summary>
    /// Try to recruit Lyris at a floor 15 shrine
    /// Returns true if the encounter was triggered (regardless of recruitment outcome)
    /// </summary>
    private async Task<bool> TryLyrisRecruitment(Character player)
    {
        var companionSystem = UsurperRemake.Systems.CompanionSystem.Instance;
        var lyris = companionSystem.GetCompanion(UsurperRemake.Systems.CompanionId.Lyris);

        // Check if Lyris can be recruited
        if (lyris == null || lyris.IsRecruited || lyris.IsDead || player.Level < lyris.RecruitLevel)
        {
            return false;
        }

        // Check if we've already encountered her on this playthrough (story flag)
        var story = StoryProgressionSystem.Instance;
        if (story.HasStoryFlag("lyris_shrine_encounter_complete"))
        {
            return false;
        }

        // Show the encounter
        terminal.ClearScreen();
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║              THE FORGOTTEN SHRINE                                 ║");
        terminal.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");
        await Task.Delay(1000);

        terminal.SetColor("white");
        terminal.WriteLine("Unlike the other shrines in this dungeon, this one feels different.");
        terminal.WriteLine("Older. Sadder. The air hums with faded divinity.");
        terminal.WriteLine("");
        await Task.Delay(1500);

        terminal.SetColor("gray");
        terminal.WriteLine("Before the altar kneels a woman.");
        terminal.WriteLine("Silver-streaked hair cascades past her shoulders.");
        terminal.WriteLine("Her eyes, when she turns to look at you, hold ancient sorrow.");
        terminal.WriteLine("");
        await Task.Delay(1500);

        terminal.SetColor("bright_cyan");
        terminal.WriteLine($"\"{lyris.DialogueHints[0]}\"");
        terminal.WriteLine("");
        await Task.Delay(2000);

        terminal.SetColor("white");
        terminal.WriteLine("She rises slowly, studying you with unnerving intensity.");
        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine($"\"{lyris.DialogueHints[1]}\"");
        terminal.WriteLine("");
        await Task.Delay(2000);

        // Show her details
        terminal.SetColor("yellow");
        terminal.WriteLine($"This is {lyris.Name}, {lyris.Title}.");
        terminal.WriteLine($"Role: {lyris.CombatRole}");
        terminal.WriteLine($"Abilities: {string.Join(", ", lyris.Abilities)}");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine(lyris.BackstoryBrief);
        terminal.WriteLine("");
        await Task.Delay(1500);

        terminal.SetColor("bright_yellow");
        terminal.WriteLine("[R] Ask her to join you");
        terminal.WriteLine("[T] Talk more");
        terminal.WriteLine("[L] Leave her to her prayers");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("Your choice: ");

        switch (choice.ToUpper())
        {
            case "R":
                bool success = await companionSystem.RecruitCompanion(
                    UsurperRemake.Systems.CompanionId.Lyris, player, terminal);
                if (success)
                {
                    terminal.SetColor("bright_green");
                    terminal.WriteLine("");
                    terminal.WriteLine($"{lyris.Name} rises from the altar.");
                    terminal.WriteLine("\"Perhaps... this is what I was waiting for.\"");
                    terminal.WriteLine("");
                    terminal.SetColor("yellow");
                    terminal.WriteLine("WARNING: Companions can die permanently. Guard her well.");

                    // Generate news
                    NewsSystem.Instance.Newsy(false, $"{player.Name2} found {lyris.Name} at a forgotten shrine in the dungeon.");
                }
                break;

            case "T":
                terminal.WriteLine("");
                terminal.SetColor("cyan");
                terminal.WriteLine($"{lyris.Name} speaks of her past...");
                terminal.WriteLine("");
                terminal.SetColor("white");
                terminal.WriteLine(lyris.Description);
                terminal.WriteLine("");
                if (!string.IsNullOrEmpty(lyris.PersonalQuestDescription))
                {
                    terminal.SetColor("bright_magenta");
                    terminal.WriteLine($"Personal Quest: {lyris.PersonalQuestName}");
                    terminal.WriteLine($"\"{lyris.PersonalQuestDescription}\"");
                    terminal.WriteLine("");
                }
                terminal.SetColor("cyan");
                terminal.WriteLine($"\"{lyris.DialogueHints[2]}\"");
                terminal.WriteLine("");

                var followUp = await terminal.GetInput("Ask her to join you? (Y/N): ");
                if (followUp.ToUpper() == "Y")
                {
                    await companionSystem.RecruitCompanion(
                        UsurperRemake.Systems.CompanionId.Lyris, player, terminal);
                }
                break;

            default:
                terminal.SetColor("gray");
                terminal.WriteLine("");
                terminal.WriteLine($"You nod to {lyris.Name} and continue on your way.");
                terminal.WriteLine("\"We will meet again,\" she whispers as you leave.");
                break;
        }

        // Mark encounter as complete
        story.SetStoryFlag("lyris_shrine_encounter_complete", true);
        await terminal.PressAnyKey();
        return true;
    }

    /// <summary>
    /// Trap encounter - Various dungeon hazards
    /// </summary>
    private async Task TrapEncounter()
    {
        terminal.SetColor("red");
        terminal.WriteLine("⚠ TRAP! ⚠");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();
        var trapType = dungeonRandom.Next(5);

        switch (trapType)
        {
            case 0:
                terminal.WriteLine("The floor gives way beneath you!", "white");
                var fallDmg = currentDungeonLevel * 3 + dungeonRandom.Next(10);
                currentPlayer.HP -= fallDmg;
                terminal.WriteLine($"You fall into a pit and take {fallDmg} damage!", "red");
                break;
            case 1:
                terminal.WriteLine("Poison darts shoot from the walls!", "white");
                var dartDmg = currentDungeonLevel * 2 + dungeonRandom.Next(8);
                currentPlayer.HP -= dartDmg;
                currentPlayer.Poison = Math.Max(currentPlayer.Poison, 1);
                terminal.WriteLine($"You take {dartDmg} damage and are poisoned!", "magenta");
                break;
            case 2:
                terminal.WriteLine("A magical rune explodes beneath your feet!", "bright_magenta");
                var runeDmg = currentDungeonLevel * 5 + dungeonRandom.Next(15);
                currentPlayer.HP -= runeDmg;
                terminal.WriteLine($"You take {runeDmg} magical damage!", "red");
                break;
            case 3:
                terminal.WriteLine("A net falls from above, trapping you!", "white");
                terminal.WriteLine("You struggle free, but lose time...", "gray");
                // Could implement time/turn penalty here
                break;
            case 4:
                terminal.WriteLine("You trigger a tripwire!", "white");
                terminal.WriteLine("But nothing happens... the trap is broken.", "green");
                terminal.WriteLine("You find some gold hidden near the mechanism.", "yellow");
                currentPlayer.Gold += currentDungeonLevel * 50;
                break;
        }

        await Task.Delay(2500);
    }

    /// <summary>
    /// Ancient scroll encounter - Magic scroll discovery
    /// </summary>
    private async Task AncientScrollEncounter()
    {
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("📜 ANCIENT SCROLL 📜");
        terminal.WriteLine("");

        terminal.WriteLine("You discover an ancient scroll tucked into a wall crevice.", "white");
        terminal.WriteLine("Strange symbols glow faintly on the parchment.", "gray");

        await HandleMagicScroll();
    }

    /// <summary>
    /// Gambling ghost encounter - Risk/reward minigame
    /// </summary>
    private async Task GamblingGhostEncounter()
    {
        terminal.SetColor("bright_white");
        terminal.WriteLine("=== GAMBLING GHOST ===");
        terminal.WriteLine("");

        terminal.WriteLine("A spectral figure materializes before you!", "cyan");
        terminal.WriteLine("\"Greetings, mortal! Care for a game of chance?\"", "yellow");
        terminal.WriteLine("The ghost produces a pair of ethereal dice.", "gray");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();
        long minBet = 100;
        long maxBet = currentPlayer.Gold / 2;

        if (currentPlayer.Gold < minBet)
        {
            terminal.WriteLine("\"Bah! You have no gold worth gambling for!\"", "yellow");
            terminal.WriteLine("The ghost fades away in disappointment.", "gray");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine($"\"Place your bet! (Minimum {minBet}, Maximum {maxBet})\"", "yellow");
        var betStr = await terminal.GetInput("Your bet (or 0 to decline): ");

        if (!long.TryParse(betStr, out long bet) || bet < minBet || bet > maxBet)
        {
            terminal.WriteLine("\"Coward! Perhaps next time...\"", "yellow");
            terminal.WriteLine("The ghost fades away.", "gray");
            await Task.Delay(2000);
            return;
        }

        terminal.WriteLine($"You bet {bet} gold!", "white");
        terminal.WriteLine("The ghost rolls the dice...", "gray");
        await Task.Delay(1500);

        var ghostRoll = dungeonRandom.Next(1, 7) + dungeonRandom.Next(1, 7);
        terminal.WriteLine($"Ghost rolls: {ghostRoll}", "cyan");

        terminal.WriteLine("Your turn to roll...", "gray");
        await Task.Delay(1000);

        var playerRoll = dungeonRandom.Next(1, 7) + dungeonRandom.Next(1, 7);
        terminal.WriteLine($"You roll: {playerRoll}", "yellow");

        if (playerRoll > ghostRoll)
        {
            terminal.SetColor("green");
            terminal.WriteLine("YOU WIN!");
            terminal.WriteLine($"The ghost begrudgingly pays you {bet} gold!", "yellow");
            currentPlayer.Gold += bet;
        }
        else if (playerRoll < ghostRoll)
        {
            terminal.SetColor("red");
            terminal.WriteLine("YOU LOSE!");
            terminal.WriteLine($"The ghost cackles as your gold vanishes!", "yellow");
            currentPlayer.Gold -= bet;
        }
        else
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("TIE!");
            terminal.WriteLine("\"Interesting... keep your gold, mortal. Until next time!\"", "yellow");
        }

        terminal.WriteLine("The ghost fades into the shadows...", "gray");
        await Task.Delay(2500);
    }

    /// <summary>
    /// Potion cache encounter - find random potions
    /// </summary>
    private async Task PotionCacheEncounter()
    {
        terminal.SetColor("bright_green");
        terminal.WriteLine("✚ POTION CACHE ✚");
        terminal.WriteLine("");

        var currentPlayer = GetCurrentPlayer();

        // Random potion messages
        string[] messages = new[]
        {
            "You discover an abandoned healer's satchel!",
            "A fallen adventurer's pack contains healing supplies!",
            "You find a monk's abandoned cache of potions!",
            "A hidden alcove reveals a stash of healing elixirs!",
            "The corpse of a cleric clutches a bag of potions!"
        };

        terminal.WriteLine(messages[dungeonRandom.Next(messages.Length)], "cyan");
        terminal.WriteLine("");

        // Give 1-5 potions, but don't exceed max
        int potionsFound = dungeonRandom.Next(1, 6);
        int currentPotions = (int)currentPlayer.Healing;
        int maxPotions = currentPlayer.MaxPotions;
        int roomAvailable = maxPotions - currentPotions;

        if (roomAvailable <= 0)
        {
            terminal.WriteLine("You already have the maximum number of potions!", "yellow");
            terminal.WriteLine("You leave the potions for another adventurer.", "gray");
        }
        else
        {
            int actualGained = Math.Min(potionsFound, roomAvailable);
            currentPlayer.Healing += actualGained;

            terminal.SetColor("green");
            terminal.WriteLine($"You collect {actualGained} healing potion{(actualGained > 1 ? "s" : "")}!");
            terminal.WriteLine($"Potions: {currentPlayer.Healing}/{currentPlayer.MaxPotions}", "cyan");

            if (actualGained < potionsFound)
            {
                terminal.WriteLine($"(You had to leave {potionsFound - actualGained} potion{(potionsFound - actualGained > 1 ? "s" : "")} behind - at maximum capacity)", "gray");
            }
        }

        await Task.Delay(2500);
    }
    
    /// <summary>
    /// Merchant encounter - Pascal DUNGEV2.PAS
    /// </summary>
    private async Task MerchantEncounter()
    {
        var player = GetCurrentPlayer();

        terminal.ClearScreen();
        terminal.SetColor("green");
        terminal.WriteLine("╔═══════════════════════════════════════════════════════╗");
        terminal.WriteLine("║            ♦ TRAVELING MERCHANT ♦                     ║");
        terminal.WriteLine("╚═══════════════════════════════════════════════════════╝");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("A traveling merchant appears from the shadows!");
        terminal.WriteLine("\"Greetings, brave adventurer! Care to trade?\"");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("(T)rade with merchant or (A)ttack for goods? ");

        if (choice.ToUpper() == "T")
        {
            await MerchantTradeMenu(player);
        }
        else if (choice.ToUpper() == "A")
        {
            terminal.SetColor("red");
            terminal.WriteLine("You decide to rob the poor merchant!");
            terminal.WriteLine("");
            await Task.Delay(1000);

            // Create merchant monster for combat
            var merchant = CreateMerchantMonster();
            var combatEngine = new CombatEngine(terminal);
            var result = await combatEngine.PlayerVsMonster(player, merchant, teammates);

            if (result.Outcome == CombatOutcome.Victory)
            {
                // Loot the merchant
                long loot = currentDungeonLevel * 100 + dungeonRandom.Next(200);
                player.Gold += loot;
                player.Healing = Math.Min(player.MaxPotions, player.Healing + 3);
                terminal.SetColor("yellow");
                terminal.WriteLine($"You loot {loot} gold and 3 healing potions from the merchant!");
            }

            // Evil deed
            player.Darkness += 10;
            terminal.SetColor("red");
            terminal.WriteLine("+10 Darkness for attacking an innocent merchant!");
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("The merchant waves goodbye and vanishes into the shadows.");
        }

        await terminal.PressAnyKey();
    }

    private async Task MerchantTradeMenu(Character player)
    {
        int potionPrice = 40 + (currentDungeonLevel * 5);
        int megaPotionPrice = currentDungeonLevel * 100;
        int antidotePrice = 75;

        // Generate rare items based on dungeon level
        var rareItems = GenerateMerchantRareItems(currentDungeonLevel);

        bool trading = true;
        while (trading)
        {
            terminal.ClearScreen();
            terminal.SetColor("green");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════╗");
            terminal.WriteLine("║            MERCHANT'S WARES                           ║");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════╝");
            terminal.WriteLine("");

            terminal.SetColor("yellow");
            terminal.WriteLine($"Your Gold: {player.Gold:N0}");
            terminal.WriteLine($"Your Potions: {player.Healing}/{player.MaxPotions}");
            terminal.SetColor("white");
            terminal.WriteLine($"Weapon Power: {player.WeapPow}  |  Armor Power: {player.ArmPow}");
            terminal.WriteLine("");

            terminal.SetColor("white");
            terminal.WriteLine("═══ SUPPLIES ═══");
            terminal.WriteLine("");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("green");
            terminal.Write("1");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine($"Healing Potion ({potionPrice}g)");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("bright_green");
            terminal.Write("2");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine($"Mega Potion ({megaPotionPrice}g) - Full heal!");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("cyan");
            terminal.Write("3");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine($"Antidote ({antidotePrice}g) - Cures poison");

            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("yellow");
            terminal.Write("4");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine($"Buy Max Potions ({potionPrice * (player.MaxPotions - player.Healing)}g)");

            terminal.WriteLine("");
            terminal.SetColor("bright_magenta");
            terminal.WriteLine("═══ RARE ITEMS (Dungeon Exclusive!) ═══");
            terminal.WriteLine("");

            for (int i = 0; i < rareItems.Count; i++)
            {
                var item = rareItems[i];
                terminal.SetColor("darkgray");
                terminal.Write("  [");
                terminal.SetColor("bright_magenta");
                terminal.Write($"{(char)('A' + i)}");
                terminal.SetColor("darkgray");
                terminal.Write("] ");
                terminal.SetColor(item.Sold ? "darkgray" : "bright_yellow");
                if (item.Sold)
                {
                    terminal.WriteLine($"{item.Name} - SOLD");
                }
                else
                {
                    terminal.WriteLine($"{item.Name} ({item.Price:N0}g) - {item.Description}");
                }
            }

            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("red");
            terminal.Write("L");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Leave shop");

            terminal.WriteLine("");
            terminal.SetColor("cyan");
            terminal.Write("Choice: ");
            terminal.SetColor("white");

            var choice = (await terminal.GetInput("")).Trim().ToUpper();

            switch (choice)
            {
                case "1":
                    if (player.Healing >= player.MaxPotions)
                    {
                        terminal.SetColor("yellow");
                        terminal.WriteLine("\"You can't carry any more potions, friend!\"");
                    }
                    else if (player.Gold >= potionPrice)
                    {
                        player.Gold -= potionPrice;
                        player.Healing++;
                        terminal.SetColor("green");
                        terminal.WriteLine($"Purchased 1 healing potion! ({player.Healing}/{player.MaxPotions})");
                    }
                    else
                    {
                        terminal.SetColor("red");
                        terminal.WriteLine("\"Not enough gold, friend.\"");
                    }
                    await Task.Delay(1500);
                    break;

                case "2":
                    if (player.Gold >= megaPotionPrice)
                    {
                        player.Gold -= megaPotionPrice;
                        player.HP = player.MaxHP;
                        terminal.SetColor("bright_green");
                        terminal.WriteLine("You drink the mega potion - FULL HEALTH RESTORED!");
                        terminal.WriteLine($"HP: {player.HP}/{player.MaxHP}");
                    }
                    else
                    {
                        terminal.SetColor("red");
                        terminal.WriteLine("\"Not enough gold, friend.\"");
                    }
                    await Task.Delay(1500);
                    break;

                case "3":
                    if (player.Poison <= 0)
                    {
                        terminal.SetColor("yellow");
                        terminal.WriteLine("\"You're not poisoned! Save your gold.\"");
                    }
                    else if (player.Gold >= antidotePrice)
                    {
                        player.Gold -= antidotePrice;
                        player.Poison = 0;
                        terminal.SetColor("green");
                        terminal.WriteLine("The poison drains from your body!");
                    }
                    else
                    {
                        terminal.SetColor("red");
                        terminal.WriteLine("\"Not enough gold, friend.\"");
                    }
                    await Task.Delay(1500);
                    break;

                case "4":
                    int potionsNeeded = player.MaxPotions - (int)player.Healing;
                    if (potionsNeeded <= 0)
                    {
                        terminal.SetColor("yellow");
                        terminal.WriteLine("\"You're already full on potions!\"");
                    }
                    else
                    {
                        long totalCost = potionsNeeded * potionPrice;
                        if (player.Gold >= totalCost)
                        {
                            player.Gold -= totalCost;
                            player.Healing = player.MaxPotions;
                            terminal.SetColor("green");
                            terminal.WriteLine($"Purchased {potionsNeeded} potions for {totalCost}g!");
                            terminal.WriteLine($"Potions: {player.Healing}/{player.MaxPotions}");
                        }
                        else
                        {
                            int canAfford = (int)(player.Gold / potionPrice);
                            if (canAfford > 0)
                            {
                                player.Gold -= canAfford * potionPrice;
                                player.Healing += canAfford;
                                terminal.SetColor("yellow");
                                terminal.WriteLine($"Could only afford {canAfford} potions.");
                                terminal.WriteLine($"Potions: {player.Healing}/{player.MaxPotions}");
                            }
                            else
                            {
                                terminal.SetColor("red");
                                terminal.WriteLine("\"Not enough gold, friend.\"");
                            }
                        }
                    }
                    await Task.Delay(1500);
                    break;

                case "A":
                case "B":
                case "C":
                case "D":
                    int itemIndex = choice[0] - 'A';
                    if (itemIndex >= 0 && itemIndex < rareItems.Count)
                    {
                        await PurchaseRareItem(player, rareItems[itemIndex]);
                    }
                    break;

                case "L":
                case "":
                    trading = false;
                    terminal.SetColor("gray");
                    terminal.WriteLine("\"Safe travels, adventurer!\"");
                    await Task.Delay(1000);
                    break;

                default:
                    terminal.SetColor("red");
                    terminal.WriteLine("Invalid choice.");
                    await Task.Delay(1000);
                    break;
            }
        }
    }

    private class MerchantRareItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public long Price { get; set; }
        public string Type { get; set; } = ""; // weapon, armor, ring, amulet, special
        public int Power { get; set; }
        public bool Sold { get; set; } = false;
        public Action<Character>? Effect { get; set; }
    }

    private List<MerchantRareItem> GenerateMerchantRareItems(int level)
    {
        var items = new List<MerchantRareItem>();
        int basePower = level + 5;
        long basePrice = level * 500;

        // Weapon options
        var weapons = new[]
        {
            ("Shadow Blade", "Strikes from darkness", basePower + 3),
            ("Flame Tongue", "Burns with eternal fire", basePower + 4),
            ("Frost Brand", "Chills to the bone", basePower + 4),
            ("Thunderclap", "Echoes with lightning", basePower + 5),
            ("Venom Fang", "Drips with poison", basePower + 3),
            ("Soul Reaver", "Drains life force", basePower + 6),
            ("Demon Slayer", "Bane of evil", basePower + 5),
            ("Dragon Tooth", "From an ancient wyrm", basePower + 7),
        };

        // Armor options
        var armors = new[]
        {
            ("Shadowmail", "Blends with darkness", basePower + 2),
            ("Dragonscale Vest", "Resistant to fire", basePower + 4),
            ("Mithril Chain", "Light as a feather", basePower + 3),
            ("Void Armor", "Absorbs magic", basePower + 5),
            ("Phoenix Plate", "Regenerates slowly", basePower + 4),
            ("Titan's Guard", "Immense protection", basePower + 6),
        };

        // Ring options
        var rings = new[]
        {
            ("Ring of Might", "+5 Strength", 5),
            ("Ring of Vitality", "+50 Max HP", 50),
            ("Ring of the Thief", "+5 Dexterity", 5),
            ("Ring of Wisdom", "+5 Intelligence", 5),
            ("Ring of Fortune", "+10% Gold Find", 10),
            ("Ring of Protection", "+3 Defense", 3),
        };

        // Amulet/Special options
        var specials = new[]
        {
            ("Amulet of Life", "+100 Max HP", 100),
            ("Charm of Speed", "+1 Attack per round", 1),
            ("Talisman of Power", "+10 All Stats", 10),
            ("Lucky Coin", "Extra gold from enemies", 0),
        };

        // Pick random items for this merchant
        var weaponChoice = weapons[dungeonRandom.Next(weapons.Length)];
        items.Add(new MerchantRareItem
        {
            Name = weaponChoice.Item1,
            Description = $"+{weaponChoice.Item3} Weapon Power - {weaponChoice.Item2}",
            Price = basePrice + (weaponChoice.Item3 * 100),
            Type = "weapon",
            Power = weaponChoice.Item3,
            Effect = (p) => { p.WeapPow += weaponChoice.Item3; }
        });

        var armorChoice = armors[dungeonRandom.Next(armors.Length)];
        items.Add(new MerchantRareItem
        {
            Name = armorChoice.Item1,
            Description = $"+{armorChoice.Item3} Armor Power - {armorChoice.Item2}",
            Price = basePrice + (armorChoice.Item3 * 80),
            Type = "armor",
            Power = armorChoice.Item3,
            Effect = (p) => { p.ArmPow += armorChoice.Item3; }
        });

        var ringChoice = rings[dungeonRandom.Next(rings.Length)];
        items.Add(new MerchantRareItem
        {
            Name = ringChoice.Item1,
            Description = ringChoice.Item2,
            Price = basePrice / 2 + (ringChoice.Item3 * 50),
            Type = "ring",
            Power = ringChoice.Item3,
            Effect = ringChoice.Item1 switch
            {
                "Ring of Might" => (p) => { p.Strength += 5; },
                "Ring of Vitality" => (p) => { p.MaxHP += 50; p.HP += 50; },
                "Ring of the Thief" => (p) => { p.Dexterity += 5; },
                "Ring of Wisdom" => (p) => { p.Intelligence += 5; },
                "Ring of Fortune" => (p) => { }, // passive effect, just mark as owned
                "Ring of Protection" => (p) => { p.ArmPow += 3; },
                _ => (p) => { }
            }
        });

        var specialChoice = specials[dungeonRandom.Next(specials.Length)];
        items.Add(new MerchantRareItem
        {
            Name = specialChoice.Item1,
            Description = specialChoice.Item2,
            Price = basePrice + 1000,
            Type = "special",
            Power = specialChoice.Item3,
            Effect = specialChoice.Item1 switch
            {
                "Amulet of Life" => (p) => { p.MaxHP += 100; p.HP += 100; },
                "Charm of Speed" => (p) => { p.Dexterity += 10; p.Agility += 10; },
                "Talisman of Power" => (p) => {
                    p.Strength += 10; p.Intelligence += 10; p.Wisdom += 10;
                    p.Dexterity += 10; p.Constitution += 10; p.Charisma += 10;
                },
                "Lucky Coin" => (p) => { }, // passive effect
                _ => (p) => { }
            }
        });

        return items;
    }

    private async Task PurchaseRareItem(Character player, MerchantRareItem item)
    {
        if (item.Sold)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("\"That item has already been sold, friend.\"");
            await Task.Delay(1500);
            return;
        }

        if (player.Gold < item.Price)
        {
            terminal.SetColor("red");
            terminal.WriteLine($"\"You need {item.Price:N0} gold for the {item.Name}.\"");
            terminal.WriteLine($"\"Come back when you have more coin!\"");
            await Task.Delay(2000);
            return;
        }

        terminal.SetColor("cyan");
        terminal.WriteLine($"Purchase {item.Name} for {item.Price:N0} gold? (Y/N)");
        var confirm = (await terminal.GetInput("")).Trim().ToUpper();

        if (confirm == "Y")
        {
            player.Gold -= item.Price;
            item.Sold = true;
            item.Effect?.Invoke(player);

            terminal.SetColor("bright_yellow");
            terminal.WriteLine("");
            terminal.WriteLine("═══════════════════════════════════════");
            terminal.WriteLine($"  * ACQUIRED: {item.Name.ToUpper()} *");
            terminal.WriteLine("═══════════════════════════════════════");
            terminal.SetColor("green");
            terminal.WriteLine($"{item.Description}");
            terminal.WriteLine("");
            terminal.SetColor("gray");
            terminal.WriteLine("\"A fine choice! Use it well.\"");
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("\"Perhaps another time.\"");
        }

        await Task.Delay(2000);
    }
    
    /// <summary>
    /// Witch Doctor encounter - Pascal DUNGEV2.PAS
    /// </summary>
    private async Task WitchDoctorEncounter()
    {
        terminal.SetColor("magenta");
        terminal.WriteLine("=== WITCH DOCTOR ENCOUNTER ===");
        terminal.WriteLine("");
        
        var currentPlayer = GetCurrentPlayer();
        long cost = currentPlayer.Level * 12500;
        
        if (currentPlayer.Gold >= cost)
        {
            terminal.WriteLine("You meet the evil Witch-Doctor Mbluta!");
            terminal.WriteLine($"He demands {cost} gold or he will curse you!");
            terminal.WriteLine("");
            
            var choice = await terminal.GetInput("(P)ay the witch doctor or (R)un away? ");
            
            if (choice.ToUpper() == "P")
            {
                currentPlayer.Gold -= cost;
                terminal.WriteLine("You reluctantly pay the witch doctor.", "yellow");
                terminal.WriteLine("He vanishes into the darkness...");
            }
            else
            {
                // 50% chance to escape
                if (dungeonRandom.Next(2) == 0)
                {
                    terminal.WriteLine("You manage to flee the evil witch doctor!", "green");
                }
                else
                {
                    terminal.WriteLine("You fail to escape and are cursed!", "red");
                    
                    // Random curse effect
                    var curseType = dungeonRandom.Next(3);
                    switch (curseType)
                    {
                        case 0:
                            var expLoss = currentPlayer.Level * 1500;
                            currentPlayer.Experience = Math.Max(0, currentPlayer.Experience - expLoss);
                            terminal.WriteLine($"You lose {expLoss} experience points!");
                            break;
                        case 1:
                            var fightLoss = dungeonRandom.Next(5) + 1;
                            currentPlayer.Fights = Math.Max(0, currentPlayer.Fights - fightLoss);
                            terminal.WriteLine($"You lose {fightLoss} dungeon fights!");
                            break;
                        case 2:
                            var pfightLoss = dungeonRandom.Next(3) + 1;
                            currentPlayer.PFights = Math.Max(0, currentPlayer.PFights - pfightLoss);
                            terminal.WriteLine($"You lose {pfightLoss} player fights!");
                            break;
                    }
                }
            }
        }
        else
        {
            terminal.WriteLine("A witch doctor appears but sees you have no gold and leaves.", "gray");
        }
        
        await Task.Delay(3000);
    }
    
    /// <summary>
    /// Create dungeon monster based on level and terrain
    /// </summary>
    private Monster CreateDungeonMonster(bool isLeader = false)
    {
        var monsterNames = GetMonsterNamesForTerrain(currentTerrain);
        var weaponArmor = GetWeaponArmorForTerrain(currentTerrain);
        
        var name = monsterNames[dungeonRandom.Next(monsterNames.Length)];
        var weapon = weaponArmor.weapons[dungeonRandom.Next(weaponArmor.weapons.Length)];
        var armor = weaponArmor.armor[dungeonRandom.Next(weaponArmor.armor.Length)];
        
        if (isLeader)
        {
            name = GetLeaderName(name);
        }
        
        // Smooth scaling factors – tuned for balanced difficulty curve
        float scaleFactor = 1f + (currentDungeonLevel / 20f); // every 20 levels → +100 %

        // Regular monsters are weaker, bosses are tougher (like the original game)
        float monsterMultiplier = isLeader ? 1.8f : 0.6f; // Regular monsters are 60% strength, bosses are 180%

        long hp = (long)(currentDungeonLevel * 4 * scaleFactor * monsterMultiplier); // survivability

        int strength = (int)(currentDungeonLevel * 1.5f * scaleFactor * monsterMultiplier); // base damage
        int punch    = (int)(currentDungeonLevel * 1.2f * scaleFactor * monsterMultiplier); // natural attacks
        int weapPow  = (int)(currentDungeonLevel * 0.9f * scaleFactor * monsterMultiplier); // weapon bonus
        int armPow   = (int)(currentDungeonLevel * 0.9f * scaleFactor * monsterMultiplier); // defense bonus

        var monster = Monster.CreateMonster(
            nr: currentDungeonLevel,
            name: name,
            hps: hp,
            strength: strength,
            defence: 0,
            phrase: GetMonsterPhrase(currentTerrain),
            grabweap: dungeonRandom.NextDouble() < 0.3,
            grabarm: false,
            weapon: weapon,
            armor: armor,
            poisoned: false,
            disease: false,
            punch: punch,
            armpow: armPow,
            weappow: weapPow
        );
        
        if (isLeader)
        {
            monster.IsBoss = true;
        }
        
        // Store level for other systems (initiative scaling etc.)
        monster.Level = currentDungeonLevel;
        
        return monster;
    }
    
    // Helper methods for monster creation
    private string[] GetMonsterNamesForTerrain(DungeonTerrain terrain)
    {
        return terrain switch
        {
            DungeonTerrain.Underground => new[] { "Orc", "Half-Orc", "Goblin", "Troll", "Skeleton" },
            DungeonTerrain.Mountains => new[] { "Mountain Bandit", "Hill Giant", "Stone Golem", "Dwarf Warrior" },
            DungeonTerrain.Desert => new[] { "Robber Knight", "Robber Squire", "Desert Nomad", "Sand Troll" },
            DungeonTerrain.Forest => new[] { "Tree Hunter", "Green Threat", "Forest Bandit", "Wild Beast" },
            DungeonTerrain.Caves => new[] { "Cave Troll", "Underground Drake", "Deep Dweller", "Rock Monster" },
            _ => new[] { "Monster", "Creature", "Beast", "Fiend" }
        };
    }
    
    private (string[] weapons, string[] armor) GetWeaponArmorForTerrain(DungeonTerrain terrain)
    {
        return terrain switch
        {
            DungeonTerrain.Underground => (
                new[] { "Sword", "Spear", "Axe", "Club" },
                new[] { "Leather", "Chain-mail", "Cloth" }
            ),
            DungeonTerrain.Mountains => (
                new[] { "War Hammer", "Battle Axe", "Mace" },
                new[] { "Chain-mail", "Scale Mail", "Plate" }
            ),
            DungeonTerrain.Desert => (
                new[] { "Lance", "Scimitar", "Javelin" },
                new[] { "Chain-Mail", "Leather", "Robes" }
            ),
            DungeonTerrain.Forest => (
                new[] { "Silver Dagger", "Sling", "Sharp Stick", "Bow" },
                new[] { "Cloth", "Leather", "Bark Armor" }
            ),
            _ => (
                new[] { "Rusty Sword", "Broken Spear", "Old Club" },
                new[] { "Torn Clothes", "Rags", "Nothing" }
            )
        };
    }
    
    private string GetLeaderName(string baseName)
    {
        return baseName + " Leader";
    }
    
    private string GetMonsterPhrase(DungeonTerrain terrain)
    {
        var phrases = terrain switch
        {
            DungeonTerrain.Underground => new[] { "Trespasser!", "Attack!", "Kill them!", "No mercy!" },
            DungeonTerrain.Mountains => new[] { "Give yourself up!", "Take no prisoners!", "For the clan!" },
            DungeonTerrain.Desert => new[] { "No prisoners!", "Your gold or your life!", "Die, infidel!" },
            DungeonTerrain.Forest => new[] { "Wrong way, lads!", "Protect the trees!", "Nature's revenge!" },
            _ => new[] { "Grrargh!", "Attack!", "Die!", "No escape!" }
        };
        
        return phrases[dungeonRandom.Next(phrases.Length)];
    }
    
    // Additional helper methods
    private async Task ShowExplorationText()
    {
        var explorationTexts = new[]
        {
            "You cautiously advance through the shadowy corridors...",
            "Your footsteps echo in the ancient stone passages...",
            "Flickering torchlight reveals mysterious doorways ahead...",
            "The air grows colder as you venture deeper into the dungeon...",
            "Strange sounds echo from the darkness beyond..."
        };
        
        terminal.WriteLine(explorationTexts[dungeonRandom.Next(explorationTexts.Length)], "gray");
        await Task.Delay(2000);
    }
    
    private string GetTerrainDescription(DungeonTerrain terrain)
    {
        return terrain switch
        {
            DungeonTerrain.Underground => "Ancient Underground Tunnels",
            DungeonTerrain.Mountains => "Rocky Mountain Passages",
            DungeonTerrain.Desert => "Desert Ruins and Tombs",
            DungeonTerrain.Forest => "Overgrown Forest Caves",
            DungeonTerrain.Caves => "Deep Natural Caverns",
            _ => "Unknown Territory"
        };
    }
    
    // Placeholder methods for features to implement
    private async Task DescendDeeper()
    {
        var player = GetCurrentPlayer();

        // No level cap - players can descend as deep as they dare
        if (currentDungeonLevel < maxDungeonLevel)
        {
            currentDungeonLevel++;
            terminal.WriteLine($"You descend to dungeon level {currentDungeonLevel}.", "yellow");

            // Update quest progress for reaching this floor
            if (player != null)
            {
                QuestSystem.OnDungeonFloorReached(player, currentDungeonLevel);
            }
        }
        else
        {
            terminal.WriteLine("You have reached the deepest level of the dungeon.", "red");
        }
        await Task.Delay(1500);
    }

    private async Task AscendToSurface()
    {
        // No level restrictions for ascending
        if (currentDungeonLevel > 1)
        {
            currentDungeonLevel--;
            terminal.WriteLine($"You ascend to dungeon level {currentDungeonLevel}.", "green");
        }
        else
        {
            await NavigateToLocation(GameLocation.MainStreet);
        }
        await Task.Delay(1500);
    }
    
    private async Task ManageTeam()
    {
        var player = GetCurrentPlayer();

        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔═══════════════════════════════════════════════════╗");
        terminal.WriteLine("║               TEAM MANAGEMENT                     ║");
        terminal.WriteLine("╚═══════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        // Check if player is in a team
        if (string.IsNullOrEmpty(player.Team))
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You are not in a team.");
            terminal.WriteLine("Visit the Team Corner in town to create or join a team!");
            terminal.WriteLine("");
            await terminal.PressAnyKey();
            return;
        }

        terminal.SetColor("white");
        terminal.WriteLine($"Team: {player.Team}");
        terminal.WriteLine($"Team controls turf: {(player.CTurf ? "Yes" : "No")}");
        terminal.WriteLine("");

        // Show current dungeon party
        terminal.SetColor("cyan");
        terminal.WriteLine("Current Dungeon Party:");
        terminal.WriteLine($"  1. {player.DisplayName} (You) - Level {player.Level} {player.Class}");
        for (int i = 0; i < teammates.Count; i++)
        {
            var tm = teammates[i];
            string status = tm.IsAlive ? $"HP: {tm.HP}/{tm.MaxHP}" : "[INJURED]";
            terminal.WriteLine($"  {i + 2}. {tm.DisplayName} - Level {tm.Level} {tm.Class} - {status}");
        }
        terminal.WriteLine("");

        // Get available NPC teammates from same team
        var npcTeammates = UsurperRemake.Systems.NPCSpawnSystem.Instance.ActiveNPCs
            .Where(n => n.Team == player.Team && n.IsAlive && !teammates.Contains(n))
            .ToList();

        // Add spouse as potential teammate (if married)
        NPC? spouseNpc = null;
        if (UsurperRemake.Systems.RomanceTracker.Instance.IsMarried)
        {
            var spouse = UsurperRemake.Systems.RomanceTracker.Instance.PrimarySpouse;
            if (spouse != null)
            {
                spouseNpc = UsurperRemake.Systems.NPCSpawnSystem.Instance.ActiveNPCs
                    .FirstOrDefault(n => n.ID == spouse.NPCId && n.IsAlive && !teammates.Contains(n) && !npcTeammates.Contains(n));
                if (spouseNpc != null)
                {
                    npcTeammates.Insert(0, spouseNpc); // Spouse first in list
                }
            }
        }

        if (npcTeammates.Count > 0)
        {
            terminal.SetColor("green");
            terminal.WriteLine("Available Allies (not in dungeon party):");
            for (int i = 0; i < npcTeammates.Count; i++)
            {
                var npc = npcTeammates[i];
                bool isSpouse = spouseNpc != null && npc.ID == spouseNpc.ID;
                if (isSpouse)
                {
                    terminal.SetColor("bright_magenta");
                    terminal.WriteLine($"  [{i + 1}] <3 {npc.DisplayName} (Spouse) - Level {npc.Level} {npc.Class} - HP: {npc.HP}/{npc.MaxHP}");
                    terminal.SetColor("green");
                }
                else
                {
                    terminal.WriteLine($"  [{i + 1}] {npc.DisplayName} - Level {npc.Level} {npc.Class} - HP: {npc.HP}/{npc.MaxHP}");
                }
            }
            terminal.WriteLine("");
        }
        else if (teammates.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("No team members available to join your dungeon party.");
            terminal.WriteLine("Recruit NPCs at the Team Corner or Hall of Recruitment!");
            terminal.WriteLine("");
        }

        // Show options
        terminal.SetColor("white");
        terminal.WriteLine("Options:");
        if (npcTeammates.Count > 0 && teammates.Count < 4) // Max 4 teammates + player = 5
        {
            terminal.WriteLine("  [A]dd a team member to dungeon party");
        }
        if (teammates.Count > 0)
        {
            terminal.WriteLine("  [R]emove a team member from party");
        }
        terminal.WriteLine("  [B]ack to dungeon menu");
        terminal.WriteLine("");

        var choice = await terminal.GetInput("Choice: ");
        choice = choice.ToUpper().Trim();

        switch (choice)
        {
            case "A":
                if (npcTeammates.Count > 0 && teammates.Count < 4)
                {
                    await AddTeammateToParty(npcTeammates);
                }
                else if (teammates.Count >= 4)
                {
                    terminal.WriteLine("Your dungeon party is full (max 4 teammates)!", "yellow");
                    await Task.Delay(1500);
                }
                else
                {
                    terminal.WriteLine("No team members available to add.", "gray");
                    await Task.Delay(1500);
                }
                break;

            case "R":
                if (teammates.Count > 0)
                {
                    await RemoveTeammateFromParty();
                }
                else
                {
                    terminal.WriteLine("No teammates to remove.", "gray");
                    await Task.Delay(1500);
                }
                break;

            case "B":
            default:
                break;
        }
    }

    private async Task AddTeammateToParty(List<NPC> available)
    {
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.Write("Enter number of team member to add (1-");
        terminal.Write($"{available.Count}");
        terminal.Write("): ");
        var input = await terminal.GetInput("");

        if (int.TryParse(input, out int index) && index >= 1 && index <= available.Count)
        {
            var npc = available[index - 1];
            teammates.Add(npc);

            // Move NPC to dungeon
            npc.UpdateLocation("Dungeon");

            terminal.SetColor("green");
            terminal.WriteLine($"{npc.DisplayName} joins your dungeon party!");
            terminal.WriteLine("They will fight alongside you against monsters.");

            // 15% team XP/gold bonus for having teammates
            terminal.SetColor("cyan");
            terminal.WriteLine("Team bonus: +15% XP and gold from battles!");
        }
        else
        {
            terminal.WriteLine("Invalid selection.", "red");
        }
        await Task.Delay(2000);
    }

    private async Task RemoveTeammateFromParty()
    {
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.Write("Enter number of teammate to remove (1-");
        terminal.Write($"{teammates.Count}");
        terminal.Write("): ");
        var input = await terminal.GetInput("");

        if (int.TryParse(input, out int index) && index >= 1 && index <= teammates.Count)
        {
            var member = teammates[index - 1];
            teammates.RemoveAt(index - 1);

            // Move NPC back to town (cast to NPC if applicable)
            if (member is NPC npc)
            {
                npc.UpdateLocation("Main Street");
            }

            terminal.SetColor("yellow");
            terminal.WriteLine($"{member.DisplayName} leaves the dungeon party and returns to town.");
        }
        else
        {
            terminal.WriteLine("Invalid selection.", "red");
        }
        await Task.Delay(1500);
    }
    
    private async Task ShowDungeonStatus()
    {
        await ShowStatus();
    }
    
    private async Task UsePotions()
    {
        var player = GetCurrentPlayer();

        while (true)
        {
            terminal.ClearScreen();
            terminal.SetColor("cyan");
            terminal.WriteLine("╔═══════════════════════════════════════════════════════╗");
            terminal.WriteLine("║                    POTIONS MENU                       ║");
            terminal.WriteLine("╚═══════════════════════════════════════════════════════╝");
            terminal.WriteLine("");

            // Show current status
            terminal.SetColor("white");
            terminal.Write("HP: ");
            DrawBar(player.HP, player.MaxHP, 25, "red", "darkgray");
            terminal.WriteLine($" {player.HP}/{player.MaxHP}");

            terminal.SetColor("yellow");
            terminal.WriteLine($"Healing Potions: {player.Healing}/{player.MaxPotions}");
            terminal.WriteLine($"Gold: {player.Gold:N0}");
            terminal.WriteLine("");

            // Calculate heal amount (potions heal 25% of max HP)
            long healAmount = player.MaxHP / 4;

            terminal.SetColor("white");
            terminal.WriteLine("Options:");
            terminal.WriteLine("");

            // Use potion option
            if (player.Healing > 0)
            {
                terminal.SetColor("darkgray");
                terminal.Write("  [");
                terminal.SetColor("green");
                terminal.Write("U");
                terminal.SetColor("darkgray");
                terminal.Write("] ");
                terminal.SetColor("white");
                terminal.WriteLine($"Use Healing Potion (heals ~{healAmount} HP)");
            }
            else
            {
                terminal.SetColor("darkgray");
                terminal.WriteLine("  [U] Use Healing Potion - NO POTIONS!");
            }

            // Buy potions option
            int costPerPotion = 50 + (player.Level * 10);
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("yellow");
            terminal.Write("B");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine($"Buy Potions from Monk ({costPerPotion}g each)");

            // Quick heal - use potions until full
            if (player.Healing > 0 && player.HP < player.MaxHP)
            {
                terminal.SetColor("darkgray");
                terminal.Write("  [");
                terminal.SetColor("bright_green");
                terminal.Write("H");
                terminal.SetColor("darkgray");
                terminal.Write("] ");
                terminal.SetColor("white");
                terminal.WriteLine("Heal to Full (use multiple potions)");
            }

            terminal.WriteLine("");
            terminal.SetColor("darkgray");
            terminal.Write("  [");
            terminal.SetColor("red");
            terminal.Write("Q");
            terminal.SetColor("darkgray");
            terminal.Write("] ");
            terminal.SetColor("white");
            terminal.WriteLine("Return to Dungeon");

            terminal.WriteLine("");
            terminal.SetColor("cyan");
            terminal.Write("Choice: ");
            terminal.SetColor("white");

            string choice = (await terminal.GetInput("")).Trim().ToUpper();

            switch (choice)
            {
                case "U":
                    if (player.Healing > 0)
                    {
                        await UseHealingPotion(player);
                    }
                    else
                    {
                        terminal.SetColor("red");
                        terminal.WriteLine("You don't have any healing potions!");
                        await Task.Delay(1500);
                    }
                    break;

                case "B":
                    await BuyPotionsFromMonk(player);
                    break;

                case "H":
                    if (player.Healing > 0 && player.HP < player.MaxHP)
                    {
                        await HealToFull(player);
                    }
                    break;

                case "Q":
                case "":
                    return;

                default:
                    terminal.SetColor("red");
                    terminal.WriteLine("Invalid choice.");
                    await Task.Delay(1000);
                    break;
            }
        }
    }

    private async Task UseHealingPotion(Character player)
    {
        if (player.Healing <= 0)
        {
            terminal.SetColor("red");
            terminal.WriteLine("You don't have any healing potions!");
            await Task.Delay(1500);
            return;
        }

        if (player.HP >= player.MaxHP)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You're already at full health!");
            await Task.Delay(1500);
            return;
        }

        // Use one potion
        player.Healing--;
        long healAmount = player.MaxHP / 4;
        long oldHP = player.HP;
        player.HP = Math.Min(player.MaxHP, player.HP + healAmount);
        long actualHeal = player.HP - oldHP;

        terminal.SetColor("bright_green");
        terminal.WriteLine("");
        terminal.WriteLine("*glug glug glug*");
        terminal.WriteLine($"You drink a healing potion and recover {actualHeal} HP!");
        terminal.Write("HP: ");
        DrawBar(player.HP, player.MaxHP, 25, "red", "darkgray");
        terminal.WriteLine($" {player.HP}/{player.MaxHP}");
        terminal.SetColor("gray");
        terminal.WriteLine($"Potions remaining: {player.Healing}/{player.MaxPotions}");

        await Task.Delay(2000);
    }

    private async Task HealToFull(Character player)
    {
        if (player.Healing <= 0)
        {
            terminal.SetColor("red");
            terminal.WriteLine("You don't have any healing potions!");
            await Task.Delay(1500);
            return;
        }

        if (player.HP >= player.MaxHP)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine("You're already at full health!");
            await Task.Delay(1500);
            return;
        }

        long healAmount = player.MaxHP / 4;
        int potionsNeeded = (int)Math.Ceiling((double)(player.MaxHP - player.HP) / healAmount);
        int potionsToUse = Math.Min(potionsNeeded, (int)player.Healing);

        terminal.SetColor("cyan");
        terminal.WriteLine($"This will use {potionsToUse} potion(s). Continue? (Y/N)");
        string confirm = (await terminal.GetInput("")).Trim().ToUpper();

        if (confirm != "Y")
        {
            terminal.SetColor("gray");
            terminal.WriteLine("Cancelled.");
            await Task.Delay(1000);
            return;
        }

        long oldHP = player.HP;
        for (int i = 0; i < potionsToUse; i++)
        {
            player.Healing--;
            player.HP = Math.Min(player.MaxHP, player.HP + healAmount);
            if (player.HP >= player.MaxHP) break;
        }
        long actualHeal = player.HP - oldHP;

        terminal.SetColor("bright_green");
        terminal.WriteLine("");
        terminal.WriteLine("*glug glug glug* *glug glug*");
        terminal.WriteLine($"You drink {potionsToUse} healing potion(s) and recover {actualHeal} HP!");
        terminal.Write("HP: ");
        DrawBar(player.HP, player.MaxHP, 25, "red", "darkgray");
        terminal.WriteLine($" {player.HP}/{player.MaxHP}");
        terminal.SetColor("gray");
        terminal.WriteLine($"Potions remaining: {player.Healing}/{player.MaxPotions}");

        await Task.Delay(2000);
    }

    private async Task BuyPotionsFromMonk(Character player)
    {
        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("");
        terminal.WriteLine("A wandering monk materializes from the shadows...");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("\"Greetings, traveler. I sense you need healing supplies.\"");
        terminal.WriteLine($"\"You carry {player.Healing} of {player.MaxPotions} potions.\"");
        terminal.WriteLine("");

        // Calculate cost per potion (scales with level)
        int costPerPotion = 50 + (player.Level * 10);

        terminal.SetColor("yellow");
        terminal.WriteLine($"Price: {costPerPotion} gold per potion");
        terminal.WriteLine($"Your gold: {player.Gold:N0}");
        terminal.WriteLine("");

        // Calculate max potions player can buy
        int roomForPotions = player.MaxPotions - (int)player.Healing;
        int maxAffordable = (int)(player.Gold / costPerPotion);
        int maxCanBuy = Math.Min(roomForPotions, maxAffordable);

        if (maxCanBuy <= 0)
        {
            if (roomForPotions <= 0)
            {
                terminal.SetColor("yellow");
                terminal.WriteLine("\"You already carry all the potions you can hold!\"");
            }
            else
            {
                terminal.SetColor("red");
                terminal.WriteLine("\"I'm afraid you lack the gold, my friend.\"");
            }
            await terminal.PressAnyKey();
            return;
        }

        terminal.SetColor("cyan");
        terminal.WriteLine($"How many potions would you like? (Max: {maxCanBuy}, 0 to cancel)");
        terminal.Write("> ");
        terminal.SetColor("white");

        var amountInput = await terminal.GetInput("");

        if (!int.TryParse(amountInput.Trim(), out int amount) || amount < 1)
        {
            terminal.SetColor("gray");
            terminal.WriteLine("\"Perhaps another time, then.\"");
            await Task.Delay(1500);
            return;
        }

        if (amount > maxCanBuy)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"\"I can only provide you with {maxCanBuy} potions.\"");
            amount = maxCanBuy;
        }

        // Complete the purchase
        long totalCost = amount * costPerPotion;
        player.Gold -= totalCost;
        player.Healing += amount;

        terminal.WriteLine("");
        terminal.SetColor("bright_green");
        terminal.WriteLine($"You purchase {amount} healing potion{(amount > 1 ? "s" : "")} for {totalCost:N0} gold.");
        terminal.SetColor("cyan");
        terminal.WriteLine($"Potions: {player.Healing}/{player.MaxPotions}");
        terminal.SetColor("yellow");
        terminal.WriteLine($"Gold remaining: {player.Gold:N0}");

        terminal.WriteLine("");
        terminal.SetColor("gray");
        terminal.WriteLine("The monk bows and fades back into the shadows...");
        await Task.Delay(2000);
    }
    
    private async Task ShowDungeonMap()
    {
        if (currentFloor == null)
        {
            terminal.WriteLine("No floor to map.", "gray");
            await Task.Delay(1500);
            return;
        }

        terminal.ClearScreen();
        terminal.SetColor(GetThemeColor(currentFloor.Theme));
        terminal.WriteLine($"╔═══════════════════════════════════════════════════════╗");
        terminal.WriteLine($"║  DUNGEON MAP - Level {currentDungeonLevel} ({currentFloor.Theme})".PadRight(56) + "║");
        terminal.WriteLine($"╚═══════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        var currentRoom = currentFloor.GetCurrentRoom();

        // Display rooms in a simple list format with connections
        foreach (var room in currentFloor.Rooms)
        {
            bool isCurrentRoom = room.Id == currentFloor.CurrentRoomId;

            // Room indicator
            if (isCurrentRoom)
            {
                terminal.SetColor("bright_yellow");
                terminal.Write(">> ");
            }
            else if (room.IsExplored)
            {
                terminal.SetColor("white");
                terminal.Write("   ");
            }
            else
            {
                terminal.SetColor("darkgray");
                terminal.Write("   ");
            }

            // Room status icons
            if (room.IsBossRoom)
            {
                terminal.SetColor("bright_red");
                terminal.Write("[B]");
            }
            else if (room.HasStairsDown)
            {
                terminal.SetColor("blue");
                terminal.Write("[v]");
            }
            else if (room.IsCleared)
            {
                terminal.SetColor("green");
                terminal.Write("[+]");
            }
            else if (room.HasMonsters && room.IsExplored)
            {
                terminal.SetColor("red");
                terminal.Write("[!]");
            }
            else if (room.IsExplored)
            {
                terminal.SetColor("cyan");
                terminal.Write("[.]");
            }
            else
            {
                terminal.SetColor("darkgray");
                terminal.Write("[?]");
            }

            // Room name
            terminal.SetColor(room.IsExplored ? "white" : "darkgray");
            terminal.Write($" {room.Name}");

            // Exits
            if (room.IsExplored)
            {
                terminal.SetColor("darkgray");
                terminal.Write(" - ");
                foreach (var exit in room.Exits)
                {
                    terminal.Write($"{GetDirectionKey(exit.Key)} ");
                }
            }

            terminal.WriteLine("");
        }

        terminal.WriteLine("");
        terminal.SetColor("gray");
        terminal.WriteLine("Legend: [B]=Boss [v]=Stairs [+]=Cleared [!]=Danger [.]=Safe [?]=Unknown");
        terminal.WriteLine("        >> = Your location");
        terminal.WriteLine("");

        // Floor stats
        int explored = currentFloor.Rooms.Count(r => r.IsExplored);
        int cleared = currentFloor.Rooms.Count(r => r.IsCleared);
        terminal.SetColor("white");
        terminal.WriteLine($"Explored: {explored}/{currentFloor.Rooms.Count}  Cleared: {cleared}/{currentFloor.Rooms.Count}");
        if (currentFloor.BossDefeated)
        {
            terminal.SetColor("green");
            terminal.WriteLine("BOSS DEFEATED!");
        }

        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// NPC encounter in dungeon
    /// </summary>
    private async Task NPCEncounter()
    {
        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("*** ANOTHER ADVENTURER ***");
        terminal.WriteLine("");

        var player = GetCurrentPlayer();
        var npcType = dungeonRandom.Next(5);

        switch (npcType)
        {
            case 0: // Friendly trader
                terminal.WriteLine("A fellow adventurer hails you!", "white");
                terminal.WriteLine("\"Greetings, friend! I have supplies for sale.\"", "yellow");
                terminal.WriteLine("");

                terminal.WriteLine("[B] Buy healing potions (500 gold each)");
                terminal.WriteLine("[I] Trade information (100 gold)");
                terminal.WriteLine("[L] Leave");

                var tradeChoice = await terminal.GetInput("Choice: ");
                if (tradeChoice.ToUpper() == "B" && player.Gold >= 500)
                {
                    player.Gold -= 500;
                    player.Healing = Math.Min(player.MaxPotions, player.Healing + 1);
                    terminal.WriteLine("You purchase a healing potion.", "green");
                }
                else if (tradeChoice.ToUpper() == "I" && player.Gold >= 100)
                {
                    player.Gold -= 100;
                    terminal.SetColor("cyan");
                    terminal.WriteLine("\"The boss room is to the far end of the dungeon.\"");
                    terminal.WriteLine("\"Watch for traps near treasure rooms.\"");
                    terminal.WriteLine("\"Resting recovers health, but only once per floor.\"");
                }
                break;

            case 1: // Wounded adventurer
                terminal.WriteLine("A wounded adventurer lies against the wall!", "red");
                terminal.WriteLine("\"Please... take my map... avenge me...\"", "yellow");
                terminal.WriteLine("");

                // Mark more rooms as explored
                foreach (var room in currentFloor.Rooms.Take(currentFloor.Rooms.Count / 2))
                {
                    room.IsExplored = true;
                }
                terminal.WriteLine("You gain knowledge of the dungeon layout!", "green");
                break;

            case 2: // Rival adventurer
                terminal.WriteLine("A rival adventurer blocks your path!", "red");
                terminal.WriteLine("\"This treasure is MINE! Get out!\"", "yellow");
                terminal.WriteLine("");

                var rivalChoice = await terminal.GetInput("(F)ight, (N)egotiate, or (L)eave? ");
                if (rivalChoice.ToUpper() == "F")
                {
                    var rival = Monster.CreateMonster(
                        currentDungeonLevel, "Rival Adventurer",
                        currentDungeonLevel * 10, currentDungeonLevel * 3, 0,
                        "Die!", false, false, "Steel Sword", "Chain Mail",
                        false, false, currentDungeonLevel * 3, currentDungeonLevel * 2, currentDungeonLevel * 2
                    );
                    rival.Level = currentDungeonLevel;

                    var combatEngine = new CombatEngine(terminal);
                    await combatEngine.PlayerVsMonster(player, rival, teammates);
                }
                else if (rivalChoice.ToUpper() == "N")
                {
                    long bribe = currentDungeonLevel * 200;
                    if (player.Gold >= bribe)
                    {
                        player.Gold -= bribe;
                        terminal.WriteLine($"You pay {bribe} gold to pass.", "yellow");
                    }
                    else
                    {
                        terminal.WriteLine("\"No gold? Then fight!\"", "red");
                    }
                }
                break;

            case 3: // Lost explorer
                terminal.WriteLine("A lost explorer stumbles towards you!", "white");
                terminal.WriteLine("\"Oh thank the gods! I've been lost for days!\"", "yellow");
                terminal.WriteLine("");
                terminal.WriteLine("You guide them to safety.", "green");
                long reward = currentDungeonLevel * 150;
                player.Gold += reward;
                player.Chivalry += 20;
                terminal.WriteLine($"They reward you with {reward} gold!", "yellow");
                terminal.WriteLine("Your chivalry increases!", "white");
                break;

            case 4: // Mysterious stranger
                terminal.WriteLine("A cloaked figure emerges from the shadows...", "magenta");
                terminal.WriteLine("\"Fate has brought us together...\"", "yellow");
                terminal.WriteLine("");

                if (dungeonRandom.NextDouble() < 0.5)
                {
                    terminal.WriteLine("They offer you a blessing!", "green");
                    player.HP = Math.Min(player.MaxHP, player.HP + player.MaxHP / 2);
                    terminal.WriteLine("You feel revitalized!");
                }
                else
                {
                    terminal.WriteLine("\"Beware the darkness ahead...\"", "red");
                    terminal.WriteLine("They vanish as mysteriously as they appeared.");
                }
                break;
        }

        await Task.Delay(2000);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Puzzle encounter
    /// </summary>
    private async Task PuzzleEncounter()
    {
        terminal.ClearScreen();
        var player = GetCurrentPlayer();

        // 50% chance for riddle, 50% for puzzle
        bool useRiddle = dungeonRandom.Next(100) < 50;

        if (useRiddle)
        {
            // Use the full RiddleDatabase
            await RiddleEncounter(player);
        }
        else
        {
            // Use the full PuzzleSystem
            await FullPuzzleEncounter(player);
        }
    }

    private async Task RiddleEncounter(Character player)
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("*** RIDDLE GATE ***");
        terminal.WriteLine("");

        // Get a riddle appropriate for this dungeon level
        int difficulty = Math.Min(5, 1 + (currentDungeonLevel / 20));
        var riddle = RiddleDatabase.Instance.GetRandomRiddle(difficulty, currentFloor?.Theme);

        // Present the riddle using the full system
        var result = await RiddleDatabase.Instance.PresentRiddle(riddle, player, terminal);

        if (result.Solved)
        {
            terminal.SetColor("green");
            terminal.WriteLine("");
            terminal.WriteLine("The ancient mechanism unlocks!");

            // Rewards scale with difficulty and level
            long goldReward = currentDungeonLevel * 200 * difficulty;
            long expReward = currentDungeonLevel * 75 * difficulty;
            player.Gold += goldReward;
            player.Experience += expReward;
            terminal.WriteLine($"You receive {goldReward} gold and {expReward} experience!");

            // Chance for Ocean Philosophy fragment on high difficulty riddles
            if (difficulty >= 3 && dungeonRandom.Next(100) < 30)
            {
                terminal.WriteLine("");
                terminal.SetColor("bright_cyan");
                terminal.WriteLine("As you solve the riddle, a deeper truth resonates within you...");
                // Grant a random uncollected wave fragment
                var fragments = Enum.GetValues<WaveFragment>()
                    .Where(f => !OceanPhilosophySystem.Instance.CollectedFragments.Contains(f))
                    .ToList();
                if (fragments.Count > 0)
                {
                    var fragment = fragments[dungeonRandom.Next(fragments.Count)];
                    OceanPhilosophySystem.Instance.CollectFragment(fragment);
                    terminal.WriteLine("You've gained insight into the Ocean's wisdom...", "bright_magenta");
                }
            }
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("");
            terminal.WriteLine("The gate remains sealed.");

            // Take damage on failure
            int damage = riddle.FailureDamage * currentDungeonLevel / 5;
            if (damage > 0)
            {
                player.HP -= damage;
                terminal.WriteLine($"A trap activates! You take {damage} damage!");
            }
        }

        await terminal.PressAnyKey();
    }

    private async Task FullPuzzleEncounter(Character player)
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("*** ANCIENT PUZZLE ***");
        terminal.WriteLine("");

        // Get puzzle type and difficulty based on floor level
        int difficulty = Math.Min(5, 1 + (currentDungeonLevel / 15));
        var puzzleType = PuzzleSystem.Instance.GetRandomPuzzleType(currentDungeonLevel);
        var theme = currentFloor?.Theme ?? DungeonTheme.Catacombs;

        var puzzle = PuzzleSystem.Instance.GeneratePuzzle(puzzleType, difficulty, theme);

        // Display puzzle description
        terminal.WriteLine(puzzle.Description, "white");
        terminal.WriteLine("");

        // Show hints/clues if available
        if (puzzle.Hints.Count > 0)
        {
            terminal.SetColor("cyan");
            foreach (var hint in puzzle.Hints)
            {
                // Don't add bullet points - the hint formatting is already handled
                terminal.WriteLine(hint);
            }
            terminal.WriteLine("");
        }

        bool solved = false;
        int attempts = puzzle.AttemptsRemaining;

        while (attempts > 0 && !solved)
        {
            terminal.SetColor("yellow");
            terminal.WriteLine($"Attempts remaining: {attempts}");
            terminal.WriteLine("");

            // Handle different puzzle types
            switch (puzzle.Type)
            {
                case PuzzleType.LeverSequence:
                    solved = await HandleLeverPuzzle(puzzle, player);
                    break;
                case PuzzleType.SymbolAlignment:
                    solved = await HandleSymbolPuzzle(puzzle, player);
                    break;
                case PuzzleType.NumberGrid:
                    solved = await HandleNumberPuzzle(puzzle, player);
                    break;
                case PuzzleType.MemoryMatch:
                    solved = await HandleMemoryPuzzle(puzzle, player);
                    break;
                default:
                    // Fallback to simple choice for other types
                    solved = await HandleSimplePuzzle(puzzle, player);
                    break;
            }

            if (!solved)
            {
                attempts--;
                if (attempts > 0)
                {
                    terminal.SetColor("red");
                    terminal.WriteLine("That's not quite right...");
                    terminal.WriteLine("");
                }
            }
        }

        if (solved)
        {
            terminal.SetColor("green");
            terminal.WriteLine("");
            terminal.WriteLine("*** PUZZLE SOLVED! ***");

            long goldReward = currentDungeonLevel * 150 * difficulty;
            long expReward = puzzle.SuccessXP * currentDungeonLevel / 10;
            player.Gold += goldReward;
            player.Experience += expReward;
            terminal.WriteLine($"You gain {goldReward} gold and {expReward} experience!");

            PuzzleSystem.Instance.MarkPuzzleSolved(currentDungeonLevel, puzzle.Title);
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("");
            terminal.WriteLine("The puzzle resets. You failed to solve it.");

            int damage = (int)(player.MaxHP * (puzzle.FailureDamagePercent / 100.0));
            if (damage > 0)
            {
                player.HP -= damage;
                terminal.WriteLine($"A trap springs! You take {damage} damage!");
            }
        }

        await terminal.PressAnyKey();
    }

    private async Task<bool> HandleLeverPuzzle(PuzzleInstance puzzle, Character player)
    {
        int leverCount = puzzle.Solution.Count;
        terminal.WriteLine($"There are {leverCount} levers. Enter the sequence (e.g., 1,2,3):", "white");

        var input = await terminal.GetInput("> ");
        var parts = input.Split(',', ' ').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        if (parts.Count != leverCount) return false;

        for (int i = 0; i < leverCount; i++)
        {
            if (!int.TryParse(parts[i], out int lever)) return false;
            if ((lever - 1).ToString() != puzzle.Solution[i]) return false;
        }

        return true;
    }

    private async Task<bool> HandleSymbolPuzzle(PuzzleInstance puzzle, Character player)
    {
        terminal.WriteLine("Available symbols: " + string.Join(", ", puzzle.AvailableChoices), "white");
        terminal.WriteLine($"Enter {puzzle.Solution.Count} symbols separated by commas:", "white");

        var input = await terminal.GetInput("> ");
        var parts = input.Split(',', ' ').Select(s => s.Trim().ToLower()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        if (parts.Count != puzzle.Solution.Count) return false;

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != puzzle.Solution[i].ToLower()) return false;
        }

        return true;
    }

    private async Task<bool> HandleNumberPuzzle(PuzzleInstance puzzle, Character player)
    {
        terminal.WriteLine($"Target sum: {puzzle.TargetNumber}", "white");
        terminal.WriteLine("Available numbers: " + string.Join(", ", puzzle.AvailableNumbers), "white");
        terminal.WriteLine("Enter numbers that sum to the target:", "white");

        var input = await terminal.GetInput("> ");
        var parts = input.Split(',', ' ', '+').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        int sum = 0;
        foreach (var part in parts)
        {
            if (int.TryParse(part, out int num))
                sum += num;
        }

        return sum == puzzle.TargetNumber;
    }

    private async Task<bool> HandleMemoryPuzzle(PuzzleInstance puzzle, Character player)
    {
        // Show the sequence briefly
        terminal.WriteLine("Memorize this sequence:", "yellow");
        terminal.WriteLine("");
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("  " + string.Join(" - ", puzzle.Solution));
        await Task.Delay(3000); // Show for 3 seconds

        // Clear the sequence
        terminal.ClearScreen();
        terminal.SetColor("cyan");
        terminal.WriteLine("*** MEMORY PUZZLE ***");
        terminal.WriteLine("");
        terminal.WriteLine("Enter the sequence you saw:", "white");

        var input = await terminal.GetInput("> ");
        var parts = input.Split(',', ' ', '-').Select(s => s.Trim().ToLower()).Where(s => !string.IsNullOrEmpty(s)).ToList();

        if (parts.Count != puzzle.Solution.Count) return false;

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != puzzle.Solution[i].ToLower()) return false;
        }

        return true;
    }

    private async Task<bool> HandleSimplePuzzle(PuzzleInstance puzzle, Character player)
    {
        // Generic handler for other puzzle types - use Intelligence check
        terminal.WriteLine("This puzzle requires careful thought...", "white");
        terminal.WriteLine("");
        terminal.WriteLine("[1] Examine carefully and deduce the answer", "white");
        terminal.WriteLine("[2] Try a random approach", "white");
        terminal.WriteLine("[3] Give up", "white");

        var choice = await terminal.GetInput("> ");

        if (choice == "1")
        {
            // Intelligence-based success chance
            int intBonus = (int)((player.Intelligence - 10) / 2); // Simple INT modifier
            int baseChance = 40 + intBonus;
            return dungeonRandom.Next(100) < baseChance;
        }
        else if (choice == "2")
        {
            // Low random chance
            return dungeonRandom.Next(100) < 20;
        }

        return false;
    }

    /// <summary>
    /// Rest spot encounter - now triggers dream sequences via AmnesiaSystem
    /// </summary>
    private async Task RestSpotEncounter()
    {
        terminal.ClearScreen();
        terminal.SetColor("green");
        terminal.WriteLine("*** SAFE HAVEN ***");
        terminal.WriteLine("");

        var player = GetCurrentPlayer();

        terminal.WriteLine("You discover a hidden sanctuary!", "white");
        terminal.WriteLine("The air here is calm, protected by ancient magic.", "gray");
        terminal.WriteLine("");

        if (!hasRestThisFloor)
        {
            terminal.WriteLine("You rest and recover your strength.", "green");
            long healAmount = player.MaxHP / 3;
            player.HP = Math.Min(player.MaxHP, player.HP + healAmount);
            terminal.WriteLine($"You recover {healAmount} HP!");

            // Cure poison
            if (player.Poison > 0)
            {
                player.Poison = 0;
                terminal.WriteLine("The sanctuary's magic cures your poison!", "cyan");
            }

            hasRestThisFloor = true;

            // Trigger dream sequences through the Amnesia System
            // Dreams reveal the player's forgotten past as a fragment of Manwe
            await Task.Delay(1500);
            terminal.WriteLine("");
            terminal.SetColor("gray");
            terminal.WriteLine("You close your eyes...");
            await Task.Delay(1000);

            await AmnesiaSystem.Instance.OnPlayerRest(terminal, player);
        }
        else
        {
            terminal.WriteLine("You've already rested on this floor.", "gray");
            terminal.WriteLine("The sanctuary offers no additional benefit.");
        }

        await Task.Delay(1500);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Mystery event encounter
    /// </summary>
    private async Task MysteryEventEncounter()
    {
        terminal.ClearScreen();
        terminal.SetColor("magenta");
        terminal.WriteLine("*** MYSTERIOUS OCCURRENCE ***");
        terminal.WriteLine("");

        var player = GetCurrentPlayer();
        var mysteryType = dungeonRandom.Next(5);

        switch (mysteryType)
        {
            case 0: // Vision
                terminal.WriteLine("A strange vision overtakes you...", "cyan");
                await Task.Delay(1500);
                terminal.WriteLine("You see the layout of this floor!", "yellow");
                foreach (var room in currentFloor.Rooms)
                {
                    room.IsExplored = true;
                }
                terminal.WriteLine("All rooms are now revealed on your map!", "green");
                break;

            case 1: // Time warp
                terminal.WriteLine("Reality warps around you!", "red");
                await Task.Delay(1000);
                terminal.SetColor("green");
                terminal.WriteLine("When it clears, you feel younger, stronger!");
                player.Experience += currentDungeonLevel * 200;
                terminal.WriteLine($"+{currentDungeonLevel * 200} experience!");
                break;

            case 2: // Ghostly message
                terminal.WriteLine("A ghostly figure appears!", "white");
                terminal.WriteLine("\"Seek the chamber of bones...\"", "yellow");
                terminal.WriteLine("\"There you will find what you seek...\"", "yellow");
                await Task.Delay(1500);
                terminal.WriteLine("The ghost points towards a direction and fades.", "gray");
                break;

            case 3: // Random teleport
                terminal.WriteLine("A magical portal suddenly opens beneath you!", "bright_magenta");
                await Task.Delay(1000);
                var randomRoom = currentFloor.Rooms[dungeonRandom.Next(currentFloor.Rooms.Count)];
                currentFloor.CurrentRoomId = randomRoom.Id;
                randomRoom.IsExplored = true;
                // Auto-clear rooms without monsters
                if (!randomRoom.HasMonsters)
                {
                    randomRoom.IsCleared = true;
                }
                terminal.WriteLine($"You are transported to: {randomRoom.Name}!", "yellow");
                break;

            case 4: // Treasure rain
                terminal.WriteLine("Gold coins rain from the ceiling!", "yellow");
                long goldRain = currentDungeonLevel * 100 + dungeonRandom.Next(500);
                player.Gold += goldRain;
                terminal.WriteLine($"You gather {goldRain} gold!");
                break;
        }

        await Task.Delay(2500);
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Random fallback dungeon event
    /// </summary>
    private async Task RandomDungeonEvent()
    {
        // Pick a random existing event
        var eventType = dungeonRandom.Next(6);
        switch (eventType)
        {
            case 0: await TreasureChestEncounter(); break;
            case 1: await PotionCacheEncounter(); break;
            case 2: await MysteriousShrine(); break;
            case 3: await GamblingGhostEncounter(); break;
            case 4: await BeggarEncounter(); break;
            case 5: await WoundedManEncounter(); break;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // OCEAN PHILOSOPHY ROOM ENCOUNTERS
    // These rooms reveal the Wave/Ocean truth through gameplay
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lore Library encounter - find Wave Fragments and Ocean Philosophy
    /// </summary>
    private async Task LoreLibraryEncounter()
    {
        terminal.ClearScreen();
        terminal.SetColor("dark_cyan");
        terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        terminal.WriteLine("              ANCIENT LORE LIBRARY");
        terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        terminal.WriteLine("");

        var player = GetCurrentPlayer();
        var ocean = OceanPhilosophySystem.Instance;

        terminal.SetColor("white");
        terminal.WriteLine("Dust motes dance in pale light from an unknown source.");
        terminal.WriteLine("Ancient tomes line walls that stretch beyond sight.");
        terminal.WriteLine("");
        await Task.Delay(1500);

        // Determine which fragment to reveal based on awakening level
        var availableFragments = OceanPhilosophySystem.FragmentData
            .Where(f => !ocean.CollectedFragments.Contains(f.Key))
            .Where(f => f.Value.RequiredAwakening <= ocean.AwakeningLevel + 2)
            .ToList();

        if (availableFragments.Count > 0)
        {
            var fragment = availableFragments[dungeonRandom.Next(availableFragments.Count)];
            var fragmentData = fragment.Value;

            terminal.SetColor("bright_cyan");
            terminal.WriteLine("A tome floats down from the shelves, opening before you...");
            terminal.WriteLine("");
            await Task.Delay(1000);

            terminal.SetColor("yellow");
            terminal.WriteLine($"  \"{fragmentData.Title}\"");
            terminal.WriteLine("");
            terminal.SetColor("cyan");

            // Display the lore text with dramatic pacing
            var words = fragmentData.Text.Split(' ');
            string currentLine = "  ";
            foreach (var word in words)
            {
                if (currentLine.Length + word.Length > 60)
                {
                    terminal.WriteLine(currentLine);
                    currentLine = "  " + word + " ";
                }
                else
                {
                    currentLine += word + " ";
                }
            }
            if (currentLine.Trim().Length > 0)
                terminal.WriteLine(currentLine);

            terminal.WriteLine("");
            await Task.Delay(2000);

            // Collect the fragment
            ocean.CollectFragment(fragment.Key);

            terminal.SetColor("bright_magenta");
            terminal.WriteLine("The words burn into your memory...");
            terminal.WriteLine($"(Wave Fragment collected: {fragmentData.Title})");

            // Grant awakening progress
            if (fragmentData.RequiredAwakening >= 5)
            {
                terminal.WriteLine("");
                terminal.SetColor("magenta");
                terminal.WriteLine("Something stirs in the depths of your consciousness...");
            }
        }
        else
        {
            // All fragments collected - give ambient wisdom instead
            terminal.SetColor("gray");
            terminal.WriteLine("You browse the ancient texts, but find nothing new.");
            terminal.WriteLine("The knowledge here already lives within you.");

            if (ocean.AwakeningLevel >= 5)
            {
                terminal.WriteLine("");
                terminal.SetColor("bright_cyan");
                terminal.WriteLine("...or perhaps it always did.");
            }
        }

        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Memory Fragment encounter - trigger Amnesia System memory recovery
    /// </summary>
    private async Task MemoryFragmentEncounter()
    {
        terminal.ClearScreen();
        terminal.SetColor("magenta");
        terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        terminal.WriteLine("              MEMORY CHAMBER");
        terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        terminal.WriteLine("");

        var player = GetCurrentPlayer();
        var amnesia = AmnesiaSystem.Instance;

        terminal.SetColor("white");
        terminal.WriteLine("The walls here are mirrors, but they don't show your reflection.");
        terminal.WriteLine("They show... someone else. Someone familiar.");
        terminal.WriteLine("");
        await Task.Delay(2000);

        // Check for floor-based memory triggers
        if (currentDungeonLevel >= 10 && currentDungeonLevel < 25)
        {
            amnesia.CheckMemoryTrigger(TriggerType.DungeonFloor10, player);
        }
        else if (currentDungeonLevel >= 25 && currentDungeonLevel < 50)
        {
            amnesia.CheckMemoryTrigger(TriggerType.DungeonFloor25, player);
        }
        else if (currentDungeonLevel >= 50 && currentDungeonLevel < 75)
        {
            amnesia.CheckMemoryTrigger(TriggerType.DungeonFloor50, player);
        }
        else if (currentDungeonLevel >= 75)
        {
            amnesia.CheckMemoryTrigger(TriggerType.DungeonFloor75, player);
        }

        // Display a recovered memory if available
        var newMemory = AmnesiaSystem.MemoryData
            .Where(m => amnesia.RecoveredMemories.Contains(m.Key))
            .OrderByDescending(m => m.Value.RequiredLevel)
            .FirstOrDefault();

        if (newMemory.Value != null)
        {
            terminal.SetColor("bright_magenta");
            terminal.WriteLine($"A memory surfaces: \"{newMemory.Value.Title}\"");
            terminal.WriteLine("");

            terminal.SetColor("magenta");
            foreach (var line in newMemory.Value.Lines)
            {
                terminal.WriteLine($"  {line}");
                await Task.Delay(1200);
            }

            terminal.WriteLine("");
            terminal.SetColor("gray");
            terminal.WriteLine("The vision fades, but the feeling remains.");
        }
        else
        {
            terminal.SetColor("gray");
            terminal.WriteLine("The mirrors show fragments of a life not quite your own.");
            terminal.WriteLine("You sense there is more to remember, but not here. Not yet.");
        }

        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Riddle Gate encounter - use RiddleDatabase for puzzle challenge
    /// </summary>
    private async Task RiddleGateEncounter()
    {
        terminal.ClearScreen();
        terminal.SetColor("yellow");
        terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        terminal.WriteLine("              THE RIDDLE GATE");
        terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        terminal.WriteLine("");

        var player = GetCurrentPlayer();
        var ocean = OceanPhilosophySystem.Instance;

        terminal.SetColor("white");
        terminal.WriteLine("A massive stone door blocks your path.");
        terminal.WriteLine("Carved into its surface: a face, ancient and knowing.");
        terminal.WriteLine("");
        await Task.Delay(1000);

        terminal.SetColor("bright_yellow");
        terminal.WriteLine("The stone lips move: 'Answer my riddle to pass.'");
        terminal.WriteLine("");
        await Task.Delay(500);

        // Get appropriate riddle based on level
        int difficulty = Math.Min(5, currentDungeonLevel / 20 + 1);

        // Use Ocean Philosophy riddle at high awakening levels
        Riddle riddle;
        if (ocean.AwakeningLevel >= 4 && dungeonRandom.Next(100) < 40)
        {
            riddle = RiddleDatabase.Instance.GetOceanPhilosophyRiddle();
        }
        else
        {
            riddle = RiddleDatabase.Instance.GetRandomRiddle(difficulty);
        }

        // Present the riddle
        var result = await RiddleDatabase.Instance.PresentRiddle(riddle, player, terminal);

        terminal.ClearScreen();
        if (result.Solved)
        {
            terminal.SetColor("green");
            terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            terminal.WriteLine("              RIDDLE SOLVED");
            terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            terminal.WriteLine("");
            terminal.WriteLine("The stone face smiles. 'Wisdom opens all doors.'");
            terminal.WriteLine("The gate rumbles open.");
            terminal.WriteLine("");

            // Reward based on riddle difficulty
            int xpReward = riddle.Difficulty * 100 * currentDungeonLevel;
            player.Experience += xpReward;
            terminal.WriteLine($"You gain {xpReward} experience!", "cyan");

            // Ocean philosophy riddles grant awakening insight
            if (riddle.IsOceanPhilosophy)
            {
                ocean.GainInsight(20);
                terminal.WriteLine("The riddle's deeper meaning resonates within you...", "magenta");
            }
        }
        else
        {
            terminal.SetColor("red");
            terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            terminal.WriteLine("              RIDDLE FAILED");
            terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            terminal.WriteLine("");
            terminal.WriteLine("'Foolish mortal. There is a price for failure.'");
            terminal.WriteLine("");

            // Trigger combat with a guardian
            terminal.SetColor("bright_red");
            terminal.WriteLine("The gate guardian manifests to punish your ignorance!");
            await Task.Delay(1500);

            // Create a riddle guardian monster and fight
            var guardian = CreateRiddleGuardian();
            var combatEngine = new CombatEngine(terminal);
            await combatEngine.PlayerVsMonster(player, guardian, teammates);
        }

        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }

    /// <summary>
    /// Create a riddle guardian monster
    /// </summary>
    private Monster CreateRiddleGuardian()
    {
        int level = currentDungeonLevel + 5;
        return Monster.CreateMonster(
            level,
            "Riddle Guardian",
            level * 50,
            level * 8,
            level * 4,
            "Your ignorance shall be your doom!",
            false,
            true, // Can cast spells
            "Stone Fist",
            "Ancient Stone",
            false, // canHurt
            false, // isUndead
            level * 30,  // armpow
            level * 20,  // weappow
            level * 5    // gold
        );
    }

    /// <summary>
    /// Secret Boss encounter - epic hidden bosses with deep lore
    /// </summary>
    private async Task SecretBossEncounter()
    {
        var player = GetCurrentPlayer();
        var bossMgr = SecretBossManager.Instance;

        // Check if there's a secret boss for this floor
        var bossType = bossMgr.GetBossForFloor(currentDungeonLevel);

        if (bossType == null)
        {
            // No boss for this floor - give atmospheric message instead
            terminal.ClearScreen();
            terminal.SetColor("dark_magenta");
            terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            terminal.WriteLine("              HIDDEN CHAMBER");
            terminal.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            terminal.WriteLine("");
            terminal.SetColor("gray");
            terminal.WriteLine("You sense great power once resided here.");
            terminal.WriteLine("But whatever dwelt in this place... has moved on.");
            terminal.WriteLine("");
            terminal.WriteLine("Perhaps deeper in the dungeon, you will find what you seek.");
            await terminal.PressAnyKey();
            return;
        }

        // Encounter the secret boss (displays intro and dialogue)
        var encounterResult = await bossMgr.EncounterBoss(bossType.Value, player, terminal);

        if (!encounterResult.Encountered)
            return;

        // Create the boss monster for actual combat
        var bossMonster = bossMgr.CreateBossMonster(bossType.Value, player.Level);

        // Engage in combat with the secret boss
        var combatEngine = new CombatEngine(terminal);
        await combatEngine.PlayerVsMonster(player, bossMonster, teammates);

        // Check if player won (player is still alive and boss is dead)
        if (player.HP > 0 && bossMonster.HP <= 0)
        {
            // Player won - handle victory through the SecretBossManager
            await bossMgr.HandleVictory(bossType.Value, player, terminal);

            // Additional memory trigger for secret boss defeat
            var bossData = bossMgr.GetBoss(bossType.Value);
            if (bossData?.TriggersMemoryFlash == true)
            {
                await Task.Delay(1000);
                terminal.ClearScreen();
                terminal.SetColor("bright_magenta");
                terminal.WriteLine("As the battle ends, something breaks open in your mind...");
                await Task.Delay(1500);
                AmnesiaSystem.Instance.CheckMemoryTrigger(TriggerType.SecretBossDefeated, player);
            }
        }
    }

    private async Task QuitToDungeon()
    {
        await NavigateToLocation(GameLocation.MainStreet);
    }
    
    private async Task ShowDungeonHelp()
    {
        terminal.ClearScreen();
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("Dungeon Help");
        terminal.WriteLine("============");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("E - Explore the current level");
        terminal.WriteLine("D - Descend to a deeper, more dangerous level");
        terminal.WriteLine("A - Ascend to a safer level or return to town");
        terminal.WriteLine("T - Manage your team members");
        terminal.WriteLine("S - View your character status");
        terminal.WriteLine("P - Buy potions from the wandering monk");
        terminal.WriteLine("M - View the dungeon map");
        terminal.WriteLine("Q - Quit and return to town");
        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
    
    /// <summary>
    /// Increase dungeon level directly up to +10 levels above the player (original mechanic).
    /// </summary>
    private async Task IncreaseDifficulty()
    {
        // No level cap - jump 10 floors deeper (capped at max dungeon level)
        int targetLevel = Math.Min(currentDungeonLevel + 10, maxDungeonLevel);

        if (targetLevel == currentDungeonLevel)
        {
            terminal.WriteLine("You have reached the deepest level of the dungeon.", "yellow");
        }
        else
        {
            currentDungeonLevel = targetLevel;
            terminal.WriteLine($"You steel your nerves. The dungeon now feels like level {currentDungeonLevel}!", "magenta");
        }

        await Task.Delay(1500);
    }
    
    // Additional encounter methods
    private async Task BeggarEncounter()
    {
        terminal.SetColor("cyan");
        terminal.WriteLine("☂ BEGGAR ENCOUNTER ☂");
        terminal.WriteLine("");
        terminal.WriteLine("A poor beggar approaches you with outstretched hands.");
        terminal.WriteLine("'Please, kind sir/madam, spare some gold for a poor soul?'");
        terminal.WriteLine("");
        
        var choice = await terminal.GetInput("(G)ive gold to beggar or (I)gnore them? ");
        
        if (choice.ToUpper() == "G")
        {
            var currentPlayer = GetCurrentPlayer();
            if (currentPlayer.Gold >= 10)
            {
                currentPlayer.Gold -= 10;
                currentPlayer.Chivalry += 5;
                terminal.WriteLine("The beggar thanks you profusely for your kindness!", "green");
                terminal.WriteLine("Your chivalry increases!");
            }
            else
            {
                terminal.WriteLine("You don't have enough gold to spare.", "red");
            }
        }
        else
        {
            terminal.WriteLine("You ignore the beggar and continue on your way.", "gray");
        }
        
        await Task.Delay(2000);
    }
    
    private Monster CreateMerchantMonster()
    {
        return Monster.CreateMonster(1, "Frightened Merchant", 30, 10, 0,
            "Help me!", false, false, "Walking Stick", "Robes", 
            false, false, 5, 1, 3);
    }
    
    private Monster CreateUndeadMonster()
    {
        var names = new[] { "Undead", "Zombie", "Skeleton Warrior" };
        var name = names[dungeonRandom.Next(names.Length)];

        return Monster.CreateMonster(currentDungeonLevel, name,
            currentDungeonLevel * 5, currentDungeonLevel * 2, 0,
            "...", false, false, "Rusty Sword", "Tattered Armor",
            false, false, currentDungeonLevel * 70, 0, currentDungeonLevel * 2);
    }

    /// <summary>
    /// Try to discover a Seal when exploring a room on a floor that has one
    /// Seals are found in special rooms (shrines, libraries, secret vaults) or boss rooms
    /// Guaranteed discovery after exploring 75%+ of the floor to prevent frustration
    /// </summary>
    private async Task<bool> TryDiscoverSeal(Character player, DungeonRoom room)
    {
        // Check if this floor has an uncollected seal
        if (!currentFloor.HasUncollectedSeal || currentFloor.SealCollected || !currentFloor.SealType.HasValue)
            return false;

        // Calculate exploration progress
        float explorationProgress = (float)currentFloor.Rooms.Count(r => r.IsExplored) / currentFloor.Rooms.Count;

        // GUARANTEED discovery after 75% exploration - player has earned it
        bool guaranteedDiscovery = explorationProgress >= 0.75;

        // Seals are found in thematically appropriate rooms
        bool isSealRoom = room.Type == RoomType.Shrine ||
                          room.Type == RoomType.LoreLibrary ||
                          room.Type == RoomType.SecretVault ||
                          room.Type == RoomType.MeditationChamber ||
                          room.IsBossRoom ||
                          (room.Type == RoomType.Chamber && dungeonRandom.NextDouble() < 0.20) ||
                          (room.HasEvent && room.EventType == DungeonEventType.Shrine);

        // Higher chance in cleared rooms and special rooms
        if (!isSealRoom && !guaranteedDiscovery)
        {
            // Scaling chance based on exploration: 15% at 50%, 25% at 60%, etc.
            if (explorationProgress < 0.5)
                return false;
            double scaledChance = 0.15 + (explorationProgress - 0.5) * 0.4; // 15% to 35% based on exploration
            if (dungeonRandom.NextDouble() > scaledChance)
                return false;
        }

        // Found the seal!
        currentFloor.SealCollected = true;
        currentFloor.SealRoomId = room.Id;

        // Dramatic discovery sequence
        terminal.ClearScreen();
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("");
        terminal.WriteLine("══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");
        terminal.WriteLine("       As you explore the room, an ancient power stirs...");
        terminal.WriteLine("");
        terminal.WriteLine("══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");
        await Task.Delay(2000);

        terminal.SetColor("white");
        terminal.WriteLine("  Hidden beneath the dust of ages, you find a stone tablet.");
        terminal.WriteLine("  It pulses with divine energy, warm to the touch.");
        terminal.WriteLine("");
        terminal.WriteLine("  This is one of the Seven Seals - the truth of the Old Gods.");
        terminal.WriteLine("");
        await Task.Delay(2000);

        // Collect the seal using the SevenSealsSystem
        var sealSystem = SevenSealsSystem.Instance;
        await sealSystem.CollectSeal(player, currentFloor.SealType.Value, terminal);

        return true;
    }

    /// <summary>
    /// Check if boss defeat drops an artifact based on floor level
    /// </summary>
    private async Task CheckArtifactDrop(Character player, int floorLevel)
    {
        // Artifacts drop from specific floor bosses (matching ArtifactSystem.DungeonFloor values)
        var artifactFloors = new Dictionary<int, UsurperRemake.Systems.ArtifactType>
        {
            { 25, UsurperRemake.Systems.ArtifactType.CreatorsEye },      // Maelketh, God of War
            { 40, UsurperRemake.Systems.ArtifactType.SoulweaversLoom },  // Shadow Realm (Noctura's Domain)
            { 50, UsurperRemake.Systems.ArtifactType.ScalesOfLaw },      // Thorgrim, God of Law
            { 60, UsurperRemake.Systems.ArtifactType.ShadowCrown },      // Noctura, Goddess of Shadows
            { 70, UsurperRemake.Systems.ArtifactType.SunforgedBlade },   // Aurelion, God of Light
            { 80, UsurperRemake.Systems.ArtifactType.Worldstone }        // Terravok, God of Earth
        };

        if (!artifactFloors.TryGetValue(floorLevel, out var artifactType))
            return;

        // Check if already collected
        if (UsurperRemake.Systems.ArtifactSystem.Instance.HasArtifact(artifactType))
            return;

        // Collect the artifact!
        terminal.WriteLine("");
        terminal.SetColor("bright_magenta");
        terminal.WriteLine("══════════════════════════════════════════════════════════════");
        terminal.WriteLine("      A DIVINE ARTIFACT PULSES WITH POWER!");
        terminal.WriteLine("══════════════════════════════════════════════════════════════");
        terminal.WriteLine("");

        await UsurperRemake.Systems.ArtifactSystem.Instance.CollectArtifact(player, artifactType, terminal);

        terminal.WriteLine("");
        await terminal.PressAnyKey();
    }
}

/// <summary>
/// Dungeon terrain types affecting encounters
/// </summary>
public enum DungeonTerrain
{
    Underground,
    Mountains, 
    Desert,
    Forest,
    Caves
} 
