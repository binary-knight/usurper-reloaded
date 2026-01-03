using System;
using System.Threading.Tasks;

/// <summary>
/// Displays the history of Usurper and BBS culture from the 1990s.
/// Honors the original creators and explains the context of this remake.
/// </summary>
public class UsurperHistorySystem
{
    private static UsurperHistorySystem? _instance;
    public static UsurperHistorySystem Instance => _instance ??= new UsurperHistorySystem();

    public UsurperHistorySystem()
    {
        _instance = this;
    }

    /// <summary>
    /// Display the complete history of Usurper and BBS culture
    /// </summary>
    public async Task ShowHistory(TerminalEmulator terminal)
    {
        await ShowBBSCulturePage(terminal);
        await ShowDoorGamesPage(terminal);
        await ShowUsurperOriginPage(terminal);
        await ShowCreatorsPage(terminal);
        await ShowRemakePage(terminal);
    }

    /// <summary>
    /// Page 1: What was a BBS?
    /// </summary>
    private async Task ShowBBSCulturePage(TerminalEmulator terminal)
    {
        terminal.ClearScreen();

        terminal.SetColor("bright_cyan");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("|                    USURPER HISTORY - A Journey Through Time                 |");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("");

        terminal.SetColor("bright_yellow");
        terminal.WriteLine("                        ~ The Age of the BBS ~");
        terminal.WriteLine("                            (1978 - 1999)");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  Before the World Wide Web, before social media, before smartphones...");
        terminal.WriteLine("  there were Bulletin Board Systems.");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine("  A BBS was a computer you could dial into with your telephone modem. You'd");
        terminal.WriteLine("  hear the screech of the handshake, watch your screen fill with ANSI art,");
        terminal.WriteLine("  and suddenly you were connected to a community of people you'd never meet");
        terminal.WriteLine("  in person - but who became your friends, rivals, and companions.");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  By 1992, there were over 25,000 BBSes in the United States alone. Each");
        terminal.WriteLine("  one was a tiny digital world, run by a 'SysOp' (System Operator) from");
        terminal.WriteLine("  their basement or bedroom. You could post messages, download files, chat");
        terminal.WriteLine("  with other callers... and play games.");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("  Because most BBSes had only ONE phone line, you were typically alone when");
        terminal.WriteLine("  you called. But the games... the games let you exist in a shared world");
        terminal.WriteLine("  with everyone else who called that BBS.");
        terminal.WriteLine("");

        terminal.SetColor("yellow");
        terminal.WriteLine("                              [Press any key to continue]");
        await terminal.WaitForKey();
    }

    /// <summary>
    /// Page 2: Door Games
    /// </summary>
    private async Task ShowDoorGamesPage(TerminalEmulator terminal)
    {
        terminal.ClearScreen();

        terminal.SetColor("bright_cyan");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("|                    USURPER HISTORY - A Journey Through Time                 |");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("");

        terminal.SetColor("bright_green");
        terminal.WriteLine("                          ~ The Door Games ~");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  'Door Games' were external programs that BBSes could launch for callers.");
        terminal.WriteLine("  They were called 'doors' because the BBS software would open a 'door' to");
        terminal.WriteLine("  let you step into another program, then bring you back when you were done.");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine("  The constraints were brutal by modern standards:");
        terminal.WriteLine("");
        terminal.SetColor("cyan");
        terminal.WriteLine("    - Text only (or ANSI art at best)");
        terminal.WriteLine("    - Slow connections (2400-14400 baud - slower than a single image today)");
        terminal.WriteLine("    - One caller at a time (usually)");
        terminal.WriteLine("    - Limited play time (to share the phone line with others)");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  Yet from these constraints came innovation. Games like Trade Wars 2002,");
        terminal.WriteLine("  Legend of the Red Dragon, Solar Realms Elite, and Usurper created entire");
        terminal.WriteLine("  persistent worlds where your actions TODAY affected what other players");
        terminal.WriteLine("  found TOMORROW.");
        terminal.WriteLine("");

        terminal.SetColor("bright_yellow");
        terminal.WriteLine("  These games were the first massively multiplayer online games accessible");
        terminal.WriteLine("  to ordinary people - not just universities or corporations with mainframes.");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine("  The golden age of door games ran from roughly 1989 to 1996, when the");
        terminal.WriteLine("  rise of the Internet and the World Wide Web slowly made BBSes obsolete.");
        terminal.WriteLine("");

        terminal.SetColor("yellow");
        terminal.WriteLine("                              [Press any key to continue]");
        await terminal.WaitForKey();
    }

    /// <summary>
    /// Page 3: The Birth of Usurper
    /// </summary>
    private async Task ShowUsurperOriginPage(TerminalEmulator terminal)
    {
        terminal.ClearScreen();

        terminal.SetColor("bright_cyan");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("|                    USURPER HISTORY - A Journey Through Time                 |");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("");

        terminal.SetColor("bright_red");
        terminal.WriteLine("                    ~ The Birth of Usurper (1993) ~");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  In 1993, a programmer named Jakob Dangarden created Usurper.");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine("  At a time when Legend of the Red Dragon dominated the door game scene,");
        terminal.WriteLine("  Usurper dared to be different. While LORD focused on daily adventures");
        terminal.WriteLine("  and simple combat, Usurper offered something more complex:");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("    - Multiple races and classes with real mechanical differences");
        terminal.WriteLine("    - A deep dungeon with 100 levels to explore");
        terminal.WriteLine("    - Team/Gang warfare - players could form groups and compete");
        terminal.WriteLine("    - Political systems - become King and rule over other players");
        terminal.WriteLine("    - Real-time multiplayer - multiple players could be online together");
        terminal.WriteLine("    - Complex equipment, spells, and character progression");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  The game was set in the realm of Dorashire, where adventurers explored");
        terminal.WriteLine("  the Dungeons of Durunghins. The ultimate goal? Reach the deepest level,");
        terminal.WriteLine("  accumulate power, and perhaps... usurp the throne itself.");
        terminal.WriteLine("");

        terminal.SetColor("bright_yellow");
        terminal.WriteLine("  Usurper quickly gained a devoted following. It was dark, complex, and");
        terminal.WriteLine("  rewarded players who mastered its systems. The game received updates");
        terminal.WriteLine("  throughout the 1990s and early 2000s, eventually reaching version 0.20e.");
        terminal.WriteLine("");

        terminal.SetColor("yellow");
        terminal.WriteLine("                              [Press any key to continue]");
        await terminal.WaitForKey();
    }

    /// <summary>
    /// Page 4: The People Who Made It Possible
    /// </summary>
    private async Task ShowCreatorsPage(TerminalEmulator terminal)
    {
        terminal.ClearScreen();

        terminal.SetColor("bright_cyan");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("|                    USURPER HISTORY - A Journey Through Time                 |");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("");

        terminal.SetColor("bright_magenta");
        terminal.WriteLine("                      ~ Those Who Preserved the Dream ~");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  This remake would not exist without these three individuals:");
        terminal.WriteLine("");

        // Jakob Dangarden
        terminal.SetColor("bright_yellow");
        terminal.WriteLine("  +-----------------------------------------------------------------------+");
        terminal.WriteLine("  |                       JAKOB DANGARDEN                                 |");
        terminal.WriteLine("  |                    Creator of Usurper (1993)                          |");
        terminal.WriteLine("  +-----------------------------------------------------------------------+");
        terminal.SetColor("gray");
        terminal.WriteLine("  |  Jakob created the original masterpiece in Turbo Pascal. His vision   |");
        terminal.WriteLine("  |  of a complex, politically-driven RPG door game set Usurper apart     |");
        terminal.WriteLine("  |  from everything else in the BBS era. In 2004, he made the historic   |");
        terminal.WriteLine("  |  decision to release Usurper as open source under the GPL license,    |");
        terminal.WriteLine("  |  ensuring the game could live on forever.                             |");
        terminal.WriteLine("  +-----------------------------------------------------------------------+");
        terminal.WriteLine("");

        // Rick Parrish
        terminal.SetColor("bright_green");
        terminal.WriteLine("  +-----------------------------------------------------------------------+");
        terminal.WriteLine("  |                        RICK PARRISH                                   |");
        terminal.WriteLine("  |                  Preserver of the Source Code                         |");
        terminal.WriteLine("  +-----------------------------------------------------------------------+");
        terminal.SetColor("gray");
        terminal.WriteLine("  |  Rick of R&M Software took on the monumental task of porting the      |");
        terminal.WriteLine("  |  original Pascal source code to modern systems. He created 32-bit     |");
        terminal.WriteLine("  |  and 64-bit versions using Free Pascal/Lazarus, ensuring the game     |");
        terminal.WriteLine("  |  could run on Windows, Linux, and beyond. His GameSrv BBS server      |");
        terminal.WriteLine("  |  software keeps the entire door game era alive today.                 |");
        terminal.WriteLine("  +-----------------------------------------------------------------------+");
        terminal.WriteLine("");

        terminal.SetColor("yellow");
        terminal.WriteLine("                              [Press any key to continue]");
        await terminal.WaitForKey();

        // Continue with Dan Zingaro
        terminal.ClearScreen();

        terminal.SetColor("bright_cyan");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("|                    USURPER HISTORY - A Journey Through Time                 |");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("");

        terminal.SetColor("bright_magenta");
        terminal.WriteLine("                      ~ Those Who Preserved the Dream ~");
        terminal.WriteLine("");

        // Daniel Zingaro
        terminal.SetColor("bright_cyan");
        terminal.WriteLine("  +-----------------------------------------------------------------------+");
        terminal.WriteLine("  |                       DANIEL ZINGARO                                  |");
        terminal.WriteLine("  |                    The Bug Slayer Supreme                             |");
        terminal.WriteLine("  +-----------------------------------------------------------------------+");
        terminal.SetColor("gray");
        terminal.WriteLine("  |  Dan Zingaro provided 'tremendous help' (in Rick's words) with        |");
        terminal.WriteLine("  |  massive bug fixing efforts on the Pascal source code. His patient    |");
        terminal.WriteLine("  |  work tracking down edge cases and fixing issues in decades-old       |");
        terminal.WriteLine("  |  code helped make version 0.20e the most stable release ever.         |");
        terminal.WriteLine("  |  Without his dedication, many subtle bugs would have remained.        |");
        terminal.WriteLine("  +-----------------------------------------------------------------------+");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  Together, these three individuals ensured that Usurper didn't disappear");
        terminal.WriteLine("  into digital oblivion like so many other door games. The source code");
        terminal.WriteLine("  they preserved became the foundation for this modern remake.");
        terminal.WriteLine("");

        terminal.SetColor("bright_white");
        terminal.WriteLine("  We honor their work by carrying the torch forward.");
        terminal.WriteLine("");

        terminal.SetColor("yellow");
        terminal.WriteLine("                              [Press any key to continue]");
        await terminal.WaitForKey();
    }

    /// <summary>
    /// Page 5: This Remake
    /// </summary>
    private async Task ShowRemakePage(TerminalEmulator terminal)
    {
        terminal.ClearScreen();

        terminal.SetColor("bright_cyan");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("|                    USURPER HISTORY - A Journey Through Time                 |");
        terminal.WriteLine("+=============================================================================+");
        terminal.WriteLine("");

        terminal.SetColor("bright_white");
        terminal.WriteLine("                         ~ Usurper Reborn ~");
        terminal.WriteLine("                           (2024 - Present)");
        terminal.WriteLine("");

        terminal.SetColor("bright_green");
        terminal.WriteLine("  +-----------------------------------------------------------------------+");
        terminal.WriteLine("  |                        JASON KNIGHT                                   |");
        terminal.WriteLine("  |                   Creator of Usurper Reborn                           |");
        terminal.WriteLine("  +-----------------------------------------------------------------------+");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.WriteLine("  This remake began with a simple question: what if Usurper had continued");
        terminal.WriteLine("  to evolve? What if the limitations of 1990s hardware were lifted, but");
        terminal.WriteLine("  the soul of the game remained intact?");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine("  Usurper Reborn is built in C# with modern architecture, but it stays");
        terminal.WriteLine("  true to the original vision. The core gameplay loops, the class system,");
        terminal.WriteLine("  the dungeon structure, and the political intrigue are all preserved.");
        terminal.WriteLine("");

        terminal.SetColor("cyan");
        terminal.WriteLine("  New features added to the remake:");
        terminal.WriteLine("");
        terminal.SetColor("white");
        terminal.WriteLine("    - 50 unique NPCs with distinct personalities and AI behaviors");
        terminal.WriteLine("    - Dynamic world simulation where NPCs live their own lives");
        terminal.WriteLine("    - Enhanced narrative with story progression and mysteries to uncover");
        terminal.WriteLine("    - Improved team/gang warfare systems");
        terminal.WriteLine("    - New Game+ cycles with persistent progress");
        terminal.WriteLine("    - Modern save system with multiple slots");
        terminal.WriteLine("    - Expanded dungeon with new monsters and challenges");
        terminal.WriteLine("");

        terminal.SetColor("bright_yellow");
        terminal.WriteLine("  The text-based interface is intentional. It's a love letter to an era");
        terminal.WriteLine("  when imagination filled in what graphics couldn't show. When the words");
        terminal.WriteLine("  on your screen became worlds in your mind.");
        terminal.WriteLine("");

        terminal.SetColor("bright_green");
        terminal.WriteLine("  Welcome to Usurper. Welcome to history. Welcome home.");
        terminal.WriteLine("");

        terminal.SetColor("yellow");
        terminal.WriteLine("                              [Press any key to return]");
        await terminal.WaitForKey();
    }
}
