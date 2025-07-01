using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// THIS FILE BUNDLES LIGHT-WEIGHT COMPATIBILITY SHIMS ONLY – they do **not** implement
// full gameplay behaviour.  They merely unblock the build so that more meaningful
// refactoring work can continue.

#region NPC helpers
// Removed duplicate NPCActionType enum – already defined in NPC.cs

public static class NPCExtensions
{
    public static bool IsAllyOf(this NPC npc, string otherId) => false;
    public static bool IsEnemyOf(this NPC npc, string otherId) => false;
}
#endregion

#region Memory stubs
// Removed duplicate MemoryType, MemoryEvent, and conflicting MemorySystem method stubs – original implementations exist.

public partial class MemorySystem
{
    public MemorySystem() { }
    public MemorySystem(NPC owner) { }
}
#endregion

#region Relationship manager stubs
public partial class RelationshipManager
{
    public RelationshipManager() { }
    public RelationshipManager(NPC owner) { }
    public void UpdateRelationship(string otherId, InteractionType type) { }
    public int GetRelationshipWith(string otherId) => 0;
}
#endregion

#region EmotionalState & GoalSystem extras
public static class EmotionalStateCompat
{
    public static void AddEmotion(this EmotionalState state, EmotionType type, float intensity)
        => state.AddEmotion(type, intensity, 120); // Default 2 hours
}

public partial class EmotionalState
{
    public void ProcessInteraction(InteractionType type, Character other, float importance) { }
}

public partial class GoalSystem
{
    public GoalSystem() { }
    public GoalSystem(NPC owner, PersonalityProfile profile) { }
    public void ProcessInteractionFeedback(InteractionType type, Character other, float importance) { }
}
#endregion

#region Character creation helpers
public static class CharacterCompat
{
    public static bool LocationReadOnlyWorkaround(this NPC npc) => true;
}
#endregion

#region Terminal / UI shims
public partial class TerminalEmulator
{
    // Parameterless menu helper returns first option
    public async Task<int> GetMenuChoice() => 0;

    // WriteLine overload with ConsoleColor and optional newline
    public void WriteLine(string text, ConsoleColor color, bool newLine)
    {
        var colorName = color.ToString().ToLower();
        if (newLine) WriteLine(text, colorName); else Write(text, colorName);
    }
}
#endregion

#region ConfigManager overload without key
public static partial class ConfigManager
{
    public static T GetConfig<T>() => default!;

    public class GameSettings
    {
        public int StartingGold { get; set; } = 2000;
        public int StartingTurns { get; set; } = 325;
        public bool DeathPenalty { get; set; } = false;
        public float DeathPenaltyXP { get; set; } = 0.1f;
        public bool PermaDeath { get; set; } = false;
    }

    public static GameSettings GetConfig() => new GameSettings();
}
#endregion

#region NewsSystem helper overloads expecting (header, ansi, cat)
// Removed duplicate NewsSystem.Newsy overloads – similar signatures already present.
#endregion

#region MailSystem quest helpers
// Quest completion stub exists in LegacyCompat
#endregion

#region TeamSystem helpers
public partial class TeamSystem
{
    public void InitiateGangWar(string gang1, string gang2) { }
    public void ClaimTown(Character teamLeader) { }
    public void AbandonTownControl(Character teamLeader) { }
}
#endregion

#region QuestSystem helpers
public partial class QuestSystem
{
    public void ShowAvailableQuests(Character player) { }
}
#endregion

#region WorldState extra info
public partial class WorldState
{
    public static string TimeOfDay => "day";
    public static int PlayersInArea => 0;
    public static int NPCsInArea => 0;
    public static int DangerLevel => 0;
}
#endregion

#region Location exceptions
public class LocationChangeException : Exception
{
    public string Destination { get; }
    public LocationChangeException(string destination) : base($"Change to {destination}") => Destination = destination;
}
#endregion

#region Player helper stubs
public class PlayerSaveData { }
public class PlayerLevelUpTracker { }

public static class CharacterDataManager
{
    public class ClassData { public List<string> SpecialAbilities { get; set; } = new(); }
    public static ClassData? GetClassData(CharacterClass cls) => new ClassData();
}
#endregion

#region Quest stub helpers
public partial class Quest
{
    public bool CanPlayerClaim(Player p) => true;
    public bool OfferedTo(Player p) => false;
    public bool Forced(Player p) => false;
    public long Penalty => 0;
    public int PenaltyType => 0;
}
#endregion 