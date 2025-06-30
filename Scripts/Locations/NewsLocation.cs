using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// News Location - Player news reading interface
/// Based on Pascal news display functionality from VARIOUS.PAS display_file() calls
/// Provides access to all news categories with Pascal-style menu system
/// </summary>
public class NewsLocation : BaseLocation
{
    private readonly NewsSystem _newsSystem;
    private const int MaxNewsDisplay = 50; // Maximum news lines to display at once
    private const int PageSize = 20; // Lines per page for paginated display

    public override string LocationName => GameConfig.DefaultNewsLocation;
    public override GameLocation LocationId => GameLocation.MainStreet; // Accessible from Main Street
    
    public NewsLocation()
    {
        _newsSystem = NewsSystem.Instance;
    }

    public override void HandlePlayerEntry(Player player)
    {
        try
        {
            DisplayNewsMenu(player);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error in NewsLocation.HandlePlayerEntry: {ex.Message}");
            player.SendMessage("An error occurred while accessing the news. Please try again later.");
            ReturnToMainStreet(player);
        }
    }

    public override bool HandlePlayerInput(Player player, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            DisplayNewsMenu(player);
            return true;
        }

        string choice = input.Trim().ToUpper();
        
        try
        {
            switch (choice)
            {
                case GameConfig.NewsMenuDaily:
                case "D":
                    DisplayDailyNews(player);
                    break;
                
                case GameConfig.NewsMenuRoyal:
                case "R":
                    DisplayRoyalNews(player);
                    break;
                
                case GameConfig.NewsMenuMarriage:
                case "M":
                    DisplayMarriageNews(player);
                    break;
                
                case GameConfig.NewsMenuBirth:
                case "B":
                    DisplayBirthNews(player);
                    break;
                
                case GameConfig.NewsMenuHoly:
                case "H":
                    DisplayHolyNews(player);
                    break;
                
                case GameConfig.NewsMenuYesterday:
                case "Y":
                    DisplayYesterdayNews(player);
                    break;
                
                case GameConfig.NewsMenuReturn:
                case "Q":
                case "QUIT":
                case "EXIT":
                case "RETURN":
                    ReturnToMainStreet(player);
                    return false;
                
                default:
                    player.SendMessage($"{GameConfig.NewsColorDefault}Invalid choice. Please try again.{GameConfig.NewsColorDefault}");
                    DisplayNewsMenu(player);
                    break;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error handling input '{input}' in NewsLocation: {ex.Message}");
            player.SendMessage("An error occurred while processing your request.");
            DisplayNewsMenu(player);
        }
        
        return true;
    }

    private void DisplayNewsMenu(Player player)
    {
        var menu = new List<string>
        {
            "",
            $"{GameConfig.NewsColorHighlight}╔═══════════════════════════════════════════════════════════════════════════════╗",
            $"║                          {GameConfig.NewsColorRoyal}USURPER DAILY NEWS{GameConfig.NewsColorHighlight}                            ║",
            $"╠═══════════════════════════════════════════════════════════════════════════════╣",
            $"║                                                                               ║",
            $"║  {GameConfig.NewsColorDefault}[{GameConfig.NewsColorHighlight}D{GameConfig.NewsColorDefault}] Daily News       - Today's events and happenings                    ║",
            $"║  {GameConfig.NewsColorDefault}[{GameConfig.NewsColorHighlight}R{GameConfig.NewsColorDefault}] Royal News       - Royal proclamations and decrees                ║",
            $"║  {GameConfig.NewsColorDefault}[{GameConfig.NewsColorHighlight}M{GameConfig.NewsColorDefault}] Marriage News     - Weddings, divorces, and relationships          ║",
            $"║  {GameConfig.NewsColorDefault}[{GameConfig.NewsColorHighlight}B{GameConfig.NewsColorDefault}] Birth News        - New arrivals and births                       ║",
            $"║  {GameConfig.NewsColorDefault}[{GameConfig.NewsColorHighlight}H{GameConfig.NewsColorDefault}] Holy News         - Divine events and god activities              ║",
            $"║  {GameConfig.NewsColorDefault}[{GameConfig.NewsColorHighlight}Y{GameConfig.NewsColorDefault}] Yesterday's News  - Previous day's archived news                 ║",
            $"║                                                                               ║",
            $"║  {GameConfig.NewsColorDefault}[{GameConfig.NewsColorHighlight}Q{GameConfig.NewsColorDefault}] Return           - Return to Main Street                         ║",
            $"║                                                                               ║",
            $"╚═══════════════════════════════════════════════════════════════════════════════╝",
            "",
            $"{GameConfig.NewsColorDefault}What would you like to read? "
        };

        foreach (string line in menu)
        {
            player.SendMessage(line);
        }
    }

    private void DisplayDailyNews(Player player)
    {
        var news = _newsSystem.ReadNews(GameConfig.NewsCategory.General, GameConfig.Ansi);
        DisplayNewsCategory(player, GameConfig.NewsHeaderDaily, news, "daily news");
    }

    private void DisplayRoyalNews(Player player)
    {
        var news = _newsSystem.ReadNews(GameConfig.NewsCategory.Royal, GameConfig.Ansi);
        DisplayNewsCategory(player, GameConfig.NewsHeaderRoyal, news, "royal proclamations");
    }

    private void DisplayMarriageNews(Player player)
    {
        var news = _newsSystem.ReadNews(GameConfig.NewsCategory.Marriage, GameConfig.Ansi);
        DisplayNewsCategory(player, GameConfig.NewsHeaderMarriage, news, "marriage and relationship news");
    }

    private void DisplayBirthNews(Player player)
    {
        var news = _newsSystem.ReadNews(GameConfig.NewsCategory.Birth, GameConfig.Ansi);
        DisplayNewsCategory(player, GameConfig.NewsHeaderBirth, news, "birth announcements");
    }

    private void DisplayHolyNews(Player player)
    {
        var news = _newsSystem.ReadNews(GameConfig.NewsCategory.Holy, GameConfig.Ansi);
        DisplayNewsCategory(player, GameConfig.NewsHeaderHoly, news, "holy events and divine activities");
    }

    private void DisplayYesterdayNews(Player player)
    {
        // Read yesterday's news from archived files
        var news = new List<string>();
        
        try
        {
            // Read from yesterday's news files (Pascal YNEWS.ASC/ANS)
            string yesterdayFile = GameConfig.Ansi ? GameConfig.YesterdayNewsAnsiFile : GameConfig.YesterdayNewsAsciiFile;
            
            if (System.IO.File.Exists(yesterdayFile))
            {
                var lines = System.IO.File.ReadAllLines(yesterdayFile);
                news.AddRange(lines);
            }
            else
            {
                news.Add("No yesterday's news available.");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error reading yesterday's news: {ex.Message}");
            news.Add("Error reading yesterday's news files.");
        }
        
        DisplayNewsCategory(player, GameConfig.NewsHeaderYesterday, news, "yesterday's news");
    }

    private void DisplayNewsCategory(Player player, string header, List<string> newsLines, string categoryName)
    {
        player.SendMessage("");
        player.SendMessage($"{GameConfig.NewsColorHighlight}{header}{GameConfig.NewsColorDefault}");
        player.SendMessage("");

        if (newsLines == null || newsLines.Count == 0)
        {
            player.SendMessage($"{GameConfig.NewsColorDefault}No {categoryName} available at this time.{GameConfig.NewsColorDefault}");
            player.SendMessage("");
            PromptForReturn(player);
            return;
        }

        // Filter out header lines and empty lines for display count
        var displayLines = newsLines
            .Where(line => !string.IsNullOrWhiteSpace(line) && 
                          !line.Contains("===") && 
                          !line.StartsWith("Initialized:"))
            .ToList();

        if (displayLines.Count == 0)
        {
            player.SendMessage($"{GameConfig.NewsColorDefault}No {categoryName} available at this time.{GameConfig.NewsColorDefault}");
            player.SendMessage("");
            PromptForReturn(player);
            return;
        }

        // Display news with pagination if needed
        if (displayLines.Count > MaxNewsDisplay)
        {
            DisplayPaginatedNews(player, displayLines, categoryName);
        }
        else
        {
            // Display all news lines
            foreach (string line in displayLines)
            {
                player.SendMessage($"{GameConfig.NewsColorDefault}{line}{GameConfig.NewsColorDefault}");
            }
        }

        player.SendMessage("");
        player.SendMessage($"{GameConfig.NewsColorTime}Total {categoryName}: {displayLines.Count} entries{GameConfig.NewsColorDefault}");
        player.SendMessage("");
        
        PromptForReturn(player);
    }

    private void DisplayPaginatedNews(Player player, List<string> newsLines, string categoryName)
    {
        int totalPages = (int)Math.Ceiling((double)newsLines.Count / PageSize);
        int currentPage = 1;
        
        while (currentPage <= totalPages)
        {
            // Display current page
            player.SendMessage($"{GameConfig.NewsColorTime}--- Page {currentPage} of {totalPages} ---{GameConfig.NewsColorDefault}");
            player.SendMessage("");
            
            int startIndex = (currentPage - 1) * PageSize;
            int endIndex = Math.Min(startIndex + PageSize, newsLines.Count);
            
            for (int i = startIndex; i < endIndex; i++)
            {
                player.SendMessage($"{GameConfig.NewsColorDefault}{newsLines[i]}{GameConfig.NewsColorDefault}");
            }
            
            player.SendMessage("");
            
            // Show navigation options
            if (currentPage < totalPages)
            {
                player.SendMessage($"{GameConfig.NewsColorHighlight}[ENTER] Next page, [Q] Return to menu: {GameConfig.NewsColorDefault}");
                string input = player.GetInput();
                
                if (string.IsNullOrWhiteSpace(input))
                {
                    currentPage++;
                }
                else if (input.Trim().ToUpper() == "Q")
                {
                    break;
                }
                else
                {
                    currentPage++;
                }
            }
            else
            {
                // Last page
                break;
            }
        }
    }

    private void PromptForReturn(Player player)
    {
        player.SendMessage($"{GameConfig.NewsColorHighlight}Press [ENTER] to return to news menu: {GameConfig.NewsColorDefault}");
        player.GetInput(); // Wait for user input
        DisplayNewsMenu(player);
    }

    private void ReturnToMainStreet(Player player)
    {
        player.SendMessage($"{GameConfig.NewsColorDefault}Thank you for reading the Usurper Daily News!{GameConfig.NewsColorDefault}");
        player.SendMessage("");
        
        // Return player to Main Street
        var mainStreet = LocationManager.Instance.GetLocation(GameLocation.MainStreet);
        if (mainStreet != null)
        {
            player.CurrentLocation = mainStreet;
            mainStreet.HandlePlayerEntry(player);
        }
        else
        {
            player.SendMessage("Error: Could not return to Main Street.");
        }
    }

    public override string GetLocationDescription(Player player)
    {
        var description = new List<string>
        {
            $"{GameConfig.NewsColorDefault}You stand before the {GameConfig.NewsColorHighlight}Usurper Daily News{GameConfig.NewsColorDefault} stand.",
            $"Fresh copies of various news categories are available here.",
            $"The news keeper maintains up-to-date information about all happenings in the realm.",
            ""
        };

        // Show news statistics
        try
        {
            var stats = _newsSystem.GetNewsStatistics();
            var todayGeneral = stats.ContainsKey("Today_General") ? (int)stats["Today_General"] : 0;
            var todayRoyal = stats.ContainsKey("Today_Royal") ? (int)stats["Today_Royal"] : 0;
            var todayMarriage = stats.ContainsKey("Today_Marriage") ? (int)stats["Today_Marriage"] : 0;
            var todayBirth = stats.ContainsKey("Today_Birth") ? (int)stats["Today_Birth"] : 0;
            var todayHoly = stats.ContainsKey("Today_Holy") ? (int)stats["Today_Holy"] : 0;

            description.Add($"{GameConfig.NewsColorTime}Today's News Summary:");
            description.Add($"Daily: {todayGeneral} • Royal: {todayRoyal} • Marriage: {todayMarriage} • Birth: {todayBirth} • Holy: {todayHoly}{GameConfig.NewsColorDefault}");
            description.Add("");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error getting news statistics: {ex.Message}");
        }

        return string.Join("\n", description);
    }

    public override List<string> GetAvailableCommands(Player player)
    {
        return new List<string>
        {
            $"{GameConfig.NewsMenuDaily} - Read daily news",
            $"{GameConfig.NewsMenuRoyal} - Read royal proclamations", 
            $"{GameConfig.NewsMenuMarriage} - Read marriage news",
            $"{GameConfig.NewsMenuBirth} - Read birth announcements",
            $"{GameConfig.NewsMenuHoly} - Read holy news",
            $"{GameConfig.NewsMenuYesterday} - Read yesterday's news",
            $"{GameConfig.NewsMenuReturn} - Return to Main Street"
        };
    }
} 
