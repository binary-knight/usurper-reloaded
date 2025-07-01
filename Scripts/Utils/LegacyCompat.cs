using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// This file collects ultra-lightweight stub implementations that legacy code still expects.
// They are NOT feature-complete – just enough to make the code compile.

#region GameConfig color & misc constants extensions
public static partial class GameConfig
{
    public const string LevelColor = "`6";
    public const string ClassColor = "`5";
    public const string RaceColor  = "`3";
    public const string RoyalColor = "`E";
    public const string HotkeyColor = "`A";
    public const string BrightColor = "`B";
    public const string MagicColor = "`C";
    public const string TypeColor = "`D";
    public const string PowerColor = "`E";
    public const string QuestColor = "`F";

    public static class GameLocation
    {
        public const int Bank = (int)global::GameLocation.Bank;
        public const int MainStreet = (int)global::GameLocation.MainStreet;
        public const int MagicShop = (int)global::GameLocation.MagicShop;
        public const int TheInn = (int)global::GameLocation.TheInn;
        public const int Temple = (int)global::GameLocation.Temple;
        // Add more mappings as needed by legacy code
    }

    // Pascal style inner Location (singular) constants
    public static class Location
    {
        public const int Prisoner = (int)global::GameLocation.Prisoner;
        public const int PrisonerOpen = (int)global::GameLocation.PrisonerOpen;
        public const int PrisonerExecution = (int)global::GameLocation.PrisonerExecution;
    }

    // Simple config string fetcher used by shop locations
    public static string GetConfigString(string key) => key switch
    {
        nameof(HotkeyColor) => HotkeyColor,
        nameof(BrightColor) => BrightColor,
        nameof(MagicColor) => MagicColor,
        nameof(TypeColor) => TypeColor,
        nameof(PowerColor) => PowerColor,
        nameof(QuestColor) => QuestColor,
        _ => ""
    };

    // Display name for Anchor Road used by AnchorRoadLocation menu
    public const string AnchorName = "Anchor Road";

    /// <summary>
    /// Legacy helper – many Pascal ports call GetConfigString with an integer index.  The real
    /// configuration subsystem is not wired yet, so we simply return an empty string which the
    /// caller will treat as "no value configured" (and therefore fall back to defaults).
    /// </summary>
    public static string GetConfigString(int index) => string.Empty;
}
#endregion

#region NewsSystem overloads & helpers
// Removed duplicate NewsSystem.Newsy(bool, string, params) overload – core NewsSystem already provides flexible variants.
#endregion

#region MailSystem quest helpers
public static partial class MailSystem
{
    public static void SendQuestClaimedMail(string player, string questName){}
    public static void SendQuestOfferMail(string player, string questName, long reward){}
    public static void SendQuestFailureMail(string player, string questName){}
    public static void SendQuestFailureNotificationMail(string player, string questName){}
    public static void SendQuestCompletionMail(string player, string questName, long reward){}
    // Overload used by older code that doesn't pass reward
    public static void SendQuestOfferMail(string player, string questName){}
}

// Lower-case alias used by Pascal-style code
public static class mailSystem
{
    public static void SendMail(string receiver, string subject, string line1, string line2 = "", string line3 = "", string line4 = "")
        => MailSystem.SendMail(receiver, subject, line1, line2, line3, line4);
}
#endregion

#region RelationshipSystem helpers
public partial class RelationshipSystem
{
    public int GetRelation(Character a, Character b) => GetRelationshipStatus(a, b);

    // Legacy overload used by Gym/Tournament code – returns a minimal RelationshipRecord
    public RelationshipRecord GetRelation(string name1, string name2)
    {
        // Build stub Character objects so we can reuse existing helper
        var char1 = new Character { Name1 = name1, Name2 = name1 };
        var char2 = new Character { Name1 = name2, Name2 = name2 };

        int relationValue = GetRelationshipStatus(char1, char2);

        return new RelationshipRecord
        {
            Name1 = name1,
            Name2 = name2,
            Relation1 = relationValue,
            Relation2 = relationValue
        };
    }

    // Placeholder that simply logs kill counts – full logic later
    public void UpdateKillStats(Character killer, Character victim) {}

    public List<string> GetAllRelationships() => new List<string>();
}
#endregion

#region TerminalEmulator colour helper
public partial class TerminalEmulator
{
    /// <summary>
    /// Convenience wrapper that lets older code pass a ConsoleColor instead of our string colour name.
    /// Defaults to writing with a trailing newline.
    /// </summary>
    public void WriteLine(string text, ConsoleColor color)
    {
        WriteLine(text, color.ToString().ToLower());
    }
}
#endregion

#region QuestSystem legacy wrappers removed – core QuestSystem static methods already available.
#endregion 