using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

/// <summary>
/// News Location - Player news reading interface
/// Based on Pascal news display functionality from VARIOUS.PAS display_file() calls
/// Provides access to all news categories with Pascal-style menu system
/// Rewritten for console mode compatibility
/// </summary>
public class NewsLocation : BaseLocation
{
    private const string NewsStandName = "Usurper Daily News";
    private readonly NewsSystem _newsSystem;
    private const int PageSize = 20; // Lines per page for paginated display

    public NewsLocation() : base(
        GameLocation.MainStreet, // News is accessed from Main Street
        NewsStandName,
        "The town crier has posted the latest news on the board."
    )
    {
        _newsSystem = NewsSystem.Instance;
    }

    protected override void SetupLocation()
    {
        PossibleExits = new List<GameLocation>
        {
            GameLocation.MainStreet
        };
    }

    protected override void DisplayLocation()
    {
        terminal.ClearScreen();
        terminal.WriteLine("");

        terminal.SetColor("bright_cyan");
        terminal.WriteLine("╔═══════════════════════════════════════════════════════════════════╗");
        terminal.WriteLine("║                      USURPER DAILY NEWS                           ║");
        terminal.WriteLine("╚═══════════════════════════════════════════════════════════════════╝");
        terminal.WriteLine("");

        terminal.SetColor("gray");
        terminal.WriteLine("Fresh copies of various news categories are available here.");
        terminal.WriteLine("The news keeper maintains up-to-date information about all happenings.");
        terminal.WriteLine("");

        ShowMenu();
        ShowNewsStats();
    }

    private void ShowMenu()
    {
        terminal.SetColor("yellow");
        terminal.WriteLine("Available News:");
        terminal.WriteLine("");

        terminal.SetColor("white");
        terminal.Write("  (");
        terminal.SetColor("bright_yellow");
        terminal.Write("D");
        terminal.SetColor("white");
        terminal.WriteLine(") Daily News - Today's events and happenings");

        terminal.Write("  (");
        terminal.SetColor("bright_yellow");
        terminal.Write("R");
        terminal.SetColor("white");
        terminal.WriteLine(") Royal News - Royal proclamations and decrees");

        terminal.Write("  (");
        terminal.SetColor("bright_yellow");
        terminal.Write("M");
        terminal.SetColor("white");
        terminal.WriteLine(") Marriage News - Weddings, divorces, and relationships");

        terminal.Write("  (");
        terminal.SetColor("bright_yellow");
        terminal.Write("B");
        terminal.SetColor("white");
        terminal.WriteLine(") Birth News - New arrivals and births");

        terminal.Write("  (");
        terminal.SetColor("bright_yellow");
        terminal.Write("H");
        terminal.SetColor("white");
        terminal.WriteLine(") Holy News - Divine events and god activities");

        terminal.Write("  (");
        terminal.SetColor("bright_yellow");
        terminal.Write("Y");
        terminal.SetColor("white");
        terminal.WriteLine(") Yesterday's News - Previous day's archived news");

        terminal.WriteLine("");

        terminal.Write("  (");
        terminal.SetColor("bright_cyan");
        terminal.Write("Q");
        terminal.SetColor("white");
        terminal.WriteLine(") Return to Main Street");

        terminal.WriteLine("");
    }

    private void ShowNewsStats()
    {
        try
        {
            var stats = _newsSystem.GetNewsStatistics();
            var todayGeneral = stats.ContainsKey("Today_General") ? (int)stats["Today_General"] : 0;
            var todayRoyal = stats.ContainsKey("Today_Royal") ? (int)stats["Today_Royal"] : 0;
            var todayMarriage = stats.ContainsKey("Today_Marriage") ? (int)stats["Today_Marriage"] : 0;
            var todayBirth = stats.ContainsKey("Today_Birth") ? (int)stats["Today_Birth"] : 0;
            var todayHoly = stats.ContainsKey("Today_Holy") ? (int)stats["Today_Holy"] : 0;

            terminal.SetColor("darkgray");
            terminal.WriteLine("─────────────────────────────────────────────────────────────────────");
            terminal.SetColor("gray");
            terminal.Write("Today's Headlines: ");
            terminal.SetColor("cyan");
            terminal.Write($"Daily:{todayGeneral} ");
            terminal.Write($"Royal:{todayRoyal} ");
            terminal.Write($"Marriage:{todayMarriage} ");
            terminal.Write($"Birth:{todayBirth} ");
            terminal.WriteLine($"Holy:{todayHoly}");
        }
        catch
        {
            // Silently ignore stats errors
        }
        terminal.WriteLine("");
    }

    protected override async Task<string> GetUserChoice()
    {
        terminal.SetColor("bright_white");
        return await terminal.GetInput("What would you like to read? ");
    }

    protected override async Task<bool> ProcessChoice(string choice)
    {
        switch (choice.ToUpper().Trim())
        {
            case "D":
                await DisplayDailyNews();
                return false;
            case "R":
                await DisplayRoyalNews();
                return false;
            case "M":
                await DisplayMarriageNews();
                return false;
            case "B":
                await DisplayBirthNews();
                return false;
            case "H":
                await DisplayHolyNews();
                return false;
            case "Y":
                await DisplayYesterdayNews();
                return false;
            case "Q":
            case "QUIT":
            case "EXIT":
            case "RETURN":
                terminal.SetColor("cyan");
                terminal.WriteLine("");
                terminal.WriteLine("Thank you for reading the Usurper Daily News!");
                await Task.Delay(1000);
                // Just return true to exit the location loop - we were called directly from MainStreet
                // so control will return there naturally without needing to throw LocationExitException
                return true;
            case "S":
                await ShowStatus();
                return false;
            case "?":
                // Re-display menu
                return false;
            default:
                terminal.SetColor("red");
                terminal.WriteLine($"Invalid choice: '{choice}'");
                await Task.Delay(1000);
                return false;
        }
    }

    private async Task DisplayDailyNews()
    {
        var news = _newsSystem.ReadNews(GameConfig.NewsCategory.General, GameConfig.Ansi);
        await DisplayNewsCategory("DAILY NEWS", news, "daily events");
    }

    private async Task DisplayRoyalNews()
    {
        var news = _newsSystem.ReadNews(GameConfig.NewsCategory.Royal, GameConfig.Ansi);
        await DisplayNewsCategory("ROYAL PROCLAMATIONS", news, "royal news");
    }

    private async Task DisplayMarriageNews()
    {
        var news = _newsSystem.ReadNews(GameConfig.NewsCategory.Marriage, GameConfig.Ansi);
        await DisplayNewsCategory("MARRIAGE NEWS", news, "marriage announcements");
    }

    private async Task DisplayBirthNews()
    {
        var news = _newsSystem.ReadNews(GameConfig.NewsCategory.Birth, GameConfig.Ansi);
        await DisplayNewsCategory("BIRTH ANNOUNCEMENTS", news, "birth news");
    }

    private async Task DisplayHolyNews()
    {
        var news = _newsSystem.ReadNews(GameConfig.NewsCategory.Holy, GameConfig.Ansi);
        await DisplayNewsCategory("HOLY NEWS", news, "divine events");
    }

    private async Task DisplayYesterdayNews()
    {
        var news = new List<string>();

        try
        {
            // Read from yesterday's news files (Pascal YNEWS.ASC/ANS)
            string yesterdayFile = GameConfig.Ansi ? GameConfig.YesterdayNewsAnsiFile : GameConfig.YesterdayNewsAsciiFile;

            if (File.Exists(yesterdayFile))
            {
                var lines = File.ReadAllLines(yesterdayFile);
                news.AddRange(lines);
            }
        }
        catch
        {
            // Silently fail, will show "no news" message
        }

        await DisplayNewsCategory("YESTERDAY'S NEWS", news, "yesterday's events");
    }

    private async Task DisplayNewsCategory(string header, List<string> newsLines, string categoryName)
    {
        terminal.ClearScreen();
        terminal.WriteLine("");

        terminal.SetColor("bright_yellow");
        terminal.WriteLine($"═══ {header} ═══");
        terminal.WriteLine("");

        if (newsLines == null || newsLines.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine($"No {categoryName} available at this time.");
            terminal.WriteLine("");
            terminal.WriteLine("The news board is empty for this category.");
            terminal.WriteLine("");
            await terminal.PressAnyKey();
            return;
        }

        // Filter out empty lines and header markers
        var displayLines = new List<string>();
        foreach (var line in newsLines)
        {
            if (!string.IsNullOrWhiteSpace(line) &&
                !line.Contains("===") &&
                !line.StartsWith("Initialized:"))
            {
                displayLines.Add(line);
            }
        }

        if (displayLines.Count == 0)
        {
            terminal.SetColor("gray");
            terminal.WriteLine($"No {categoryName} available at this time.");
            terminal.WriteLine("");
            await terminal.PressAnyKey();
            return;
        }

        // Display with pagination if needed
        if (displayLines.Count > PageSize)
        {
            await DisplayPaginatedNews(displayLines, categoryName);
        }
        else
        {
            // Display all news lines
            terminal.SetColor("white");
            foreach (string line in displayLines)
            {
                terminal.WriteLine(line);
            }
            terminal.WriteLine("");

            terminal.SetColor("cyan");
            terminal.WriteLine($"Total {categoryName}: {displayLines.Count} entries");
            terminal.WriteLine("");
            await terminal.PressAnyKey();
        }
    }

    private async Task DisplayPaginatedNews(List<string> newsLines, string categoryName)
    {
        int totalPages = (int)Math.Ceiling((double)newsLines.Count / PageSize);
        int currentPage = 1;

        while (currentPage <= totalPages)
        {
            terminal.ClearScreen();
            terminal.WriteLine("");

            // Display current page header
            terminal.SetColor("cyan");
            terminal.WriteLine($"─── Page {currentPage} of {totalPages} ───");
            terminal.WriteLine("");

            int startIndex = (currentPage - 1) * PageSize;
            int endIndex = Math.Min(startIndex + PageSize, newsLines.Count);

            terminal.SetColor("white");
            for (int i = startIndex; i < endIndex; i++)
            {
                terminal.WriteLine(newsLines[i]);
            }
            terminal.WriteLine("");

            // Show navigation options
            if (currentPage < totalPages)
            {
                terminal.SetColor("yellow");
                terminal.Write("[ENTER] Next page, [Q] Return to menu: ");

                string input = await terminal.GetInput("");

                if (!string.IsNullOrWhiteSpace(input) && input.Trim().ToUpper() == "Q")
                {
                    break;
                }
                currentPage++;
            }
            else
            {
                // Last page
                terminal.SetColor("cyan");
                terminal.WriteLine($"Total {categoryName}: {newsLines.Count} entries");
                terminal.WriteLine("");
                await terminal.PressAnyKey();
                break;
            }
        }
    }

    protected override string GetBreadcrumbPath()
    {
        return "Main Street > News Stand";
    }
}
