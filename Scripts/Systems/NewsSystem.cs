using UsurperRemake.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;

/// <summary>
/// News System - Pascal NEWS.PAS and GENNEWS.PAS implementation
/// Handles all news writing, categorization, and file management for Usurper
/// 100% compatible with original Pascal newsy() and generic_news() functions
/// </summary>
public class NewsSystem
{
    private static NewsSystem _instance;
    private readonly Dictionary<GameConfig.NewsCategory, List<string>> _dailyNews;
    private readonly object _newsLock = new object();

    // Pascal-compatible file handles (simulated)
    private bool _newsFileAnsiOpen = false;
    private bool _newsFileAsciiOpen = false;

    public static NewsSystem Instance
    {
        get
        {
            if (_instance == null)
                _instance = new NewsSystem();
            return _instance;
        }
    }

    private NewsSystem()
    {
        _dailyNews = new Dictionary<GameConfig.NewsCategory, List<string>>();
        foreach (GameConfig.NewsCategory category in Enum.GetValues<GameConfig.NewsCategory>())
        {
            _dailyNews[category] = new List<string>();
        }
        
        // Initialize news files if they don't exist
        InitializeNewsFiles();
    }

    /// <summary>
    /// Pascal newsy() function - Write general daily news
    /// From NEWS.PAS: procedure newsy(news_to_ansi: boolean; s: s70);
    /// </summary>
    /// <param name="newsToAnsi">Write to ANSI news file (Pascal global_ansi)</param>
    /// <param name="message">News message text</param>
    public void Newsy(bool newsToAnsi, string message)
    {
        lock (_newsLock)
        {
            try
            {
                // Pascal: Format message with current time
                string timestamp = DateTime.Now.ToString(GameConfig.NewsTimeFormat);
                string formattedMessage = string.Format(GameConfig.NewsPrefixTime, timestamp) + message;
                
                // Add to daily news cache
                _dailyNews[GameConfig.NewsCategory.General].Add(formattedMessage);
                
                // Write to appropriate news file (Pascal implementation)
                if (newsToAnsi)
                {
                    // Write to ANSI file first (NEWS.ANS)
                    if (File.Exists(GameConfig.NewsAnsiFile) || !string.IsNullOrEmpty(GameConfig.NewsAnsiFile))
                    {
                        AppendToNewsFile(GameConfig.NewsAnsiFile, formattedMessage);
                    }
                }
                else
                {
                    // Write to ASCII file (NEWS.ASC)
                    if (File.Exists(GameConfig.NewsAsciiFile) || !string.IsNullOrEmpty(GameConfig.NewsAsciiFile))
                    {
                        AppendToNewsFile(GameConfig.NewsAsciiFile, formattedMessage);
                    }
                }
                
                // Pascal: Always try to write to both files if they exist
                if (newsToAnsi && File.Exists(GameConfig.NewsAsciiFile))
                {
                    AppendToNewsFile(GameConfig.NewsAsciiFile, StripAnsiCodes(formattedMessage));
                }
                else if (!newsToAnsi && File.Exists(GameConfig.NewsAnsiFile))
                {
                    AppendToNewsFile(GameConfig.NewsAnsiFile, formattedMessage);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"NewsSystem.Newsy() error: {ex.Message}");
                // Pascal equivalent: unable_to_access() procedure
                LogUnableToAccess(newsToAnsi ? GameConfig.NewsAnsiFile : GameConfig.NewsAsciiFile, ex.Message);
            }
        }
    }

    /// <summary>
    /// Pascal generic_news() function - Write specialized category news
    /// From GENNEWS.PAS: procedure generic_news(news_type: byte; news_to_ansi: boolean; s: s120);
    /// </summary>
    /// <param name="newsType">News category (1=Royal, 2=Marriage, 3=Birth, 4=Holy)</param>
    /// <param name="newsToAnsi">Write to ANSI news file</param>
    /// <param name="message">News message text</param>
    public void GenericNews(GameConfig.NewsCategory newsType, bool newsToAnsi, string message)
    {
        lock (_newsLock)
        {
            try
            {
                // Get appropriate file paths based on news type (Pascal GENNEWS.PAS logic)
                string ansiFile = GetNewsFileForCategory(newsType, true);
                string asciiFile = GetNewsFileForCategory(newsType, false);
                
                // Format message with timestamp and category prefix
                string timestamp = DateTime.Now.ToString(GameConfig.NewsTimeFormat);
                string prefix = GetNewsPrefixForCategory(newsType);
                string formattedMessage = string.Format(GameConfig.NewsPrefixTime, timestamp) + prefix + message;
                
                // Add to daily news cache
                _dailyNews[newsType].Add(formattedMessage);
                
                // Write to appropriate files (Pascal logic)
                if (newsToAnsi && !string.IsNullOrEmpty(ansiFile))
                {
                    AppendToNewsFile(ansiFile, formattedMessage);
                }
                
                if (!string.IsNullOrEmpty(asciiFile))
                {
                    string asciiMessage = newsToAnsi ? StripAnsiCodes(formattedMessage) : formattedMessage;
                    AppendToNewsFile(asciiFile, asciiMessage);
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"NewsSystem.GenericNews() error for {newsType}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Enhanced newsy function with category prefix support
    /// </summary>
    public void WriteNews(GameConfig.NewsCategory category, string message, bool includeTime = true)
    {
        string prefix = GetNewsPrefixForCategory(category);
        string fullMessage = prefix + message;
        
        if (category == GameConfig.NewsCategory.General)
        {
            Newsy(GameConfig.Ansi, fullMessage);
        }
        else
        {
            GenericNews(category, GameConfig.Ansi, fullMessage);
        }
    }

    /// <summary>
    /// Write player death news (Pascal combat integration)
    /// </summary>
    public void WriteDeathNews(string playerName, string killerName, string location)
    {
        string message = $"{GameConfig.NewsColorDeath}{playerName}{GameConfig.NewsColorDefault} was slain by {GameConfig.NewsColorPlayer}{killerName}{GameConfig.NewsColorDefault} at {location}!";
        WriteNews(GameConfig.NewsCategory.General, message);
    }

    /// <summary>
    /// Write player birth news (Pascal relationship system integration)
    /// </summary>
    public void WriteBirthNews(string motherName, string fatherName, string childName, bool isNPC = false)
    {
        string message = $"{GameConfig.NewsColorBirth}{motherName}{GameConfig.NewsColorDefault} and {GameConfig.NewsColorPlayer}{fatherName}{GameConfig.NewsColorDefault} are proud parents of {childName}!";
        WriteNews(GameConfig.NewsCategory.Birth, message);
    }

    /// <summary>
    /// Write marriage news (Pascal relationship system integration)
    /// </summary>
    public void WriteMarriageNews(string player1Name, string player2Name, string location = "Temple")
    {
        string message = $"{GameConfig.NewsColorPlayer}{player1Name}{GameConfig.NewsColorDefault} and {GameConfig.NewsColorPlayer}{player2Name}{GameConfig.NewsColorDefault} were married at the {location}!";
        WriteNews(GameConfig.NewsCategory.Marriage, message);
    }

    /// <summary>
    /// Write divorce news (Pascal relationship system integration)
    /// </summary>
    public void WriteDivorceNews(string player1Name, string player2Name)
    {
        string message = $"{GameConfig.NewsColorPlayer}{player1Name}{GameConfig.NewsColorDefault} and {GameConfig.NewsColorPlayer}{player2Name}{GameConfig.NewsColorDefault} have divorced!";
        WriteNews(GameConfig.NewsCategory.Marriage, message);
    }

    /// <summary>
    /// Write royal proclamation news (Pascal castle system integration)
    /// </summary>
    public void WriteRoyalNews(string kingName, string proclamation)
    {
        string message = $"{GameConfig.NewsColorRoyal}King {kingName}{GameConfig.NewsColorDefault} proclaims: {proclamation}";
        WriteNews(GameConfig.NewsCategory.Royal, message);
    }

    /// <summary>
    /// Write holy/god news (Pascal god system integration)
    /// </summary>
    public void WriteHolyNews(string godName, string event_description)
    {
        string message = $"{GameConfig.NewsColorHoly}{godName}{GameConfig.NewsColorDefault}: {event_description}";
        WriteNews(GameConfig.NewsCategory.Holy, message);
    }

    /// <summary>
    /// Write quest completion news (Pascal quest system integration)
    /// </summary>
    public void WriteQuestNews(string playerName, string questDescription, bool completed = true)
    {
        string status = completed ? "completed" : "failed";
        string color = completed ? GameConfig.NewsColorPlayer : GameConfig.NewsColorDeath;
        string message = $"{color}{playerName}{GameConfig.NewsColorDefault} {status} quest: {questDescription}";
        WriteNews(GameConfig.NewsCategory.General, message);
    }

    /// <summary>
    /// Write team/gang warfare news (Pascal team system integration)
    /// </summary>
    public void WriteTeamNews(string teamName, string event_description)
    {
        string message = $"Team {GameConfig.NewsColorHighlight}{teamName}{GameConfig.NewsColorDefault}: {event_description}";
        WriteNews(GameConfig.NewsCategory.General, message);
    }

    /// <summary>
    /// Write prison news (Pascal prison system integration)  
    /// </summary>
    public void WritePrisonNews(string playerName, string event_description)
    {
        string message = $"{GameConfig.NewsColorPlayer}{playerName}{GameConfig.NewsColorDefault}: {event_description}";
        WriteNews(GameConfig.NewsCategory.General, message);
    }

    /// <summary>
    /// Read news from specified category and file type
    /// </summary>
    public List<string> ReadNews(GameConfig.NewsCategory category, bool readAnsi = true)
    {
        string filePath = GetNewsFileForCategory(category, readAnsi);
        var news = new List<string>();
        
        try
        {
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                news.AddRange(lines);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error reading news from {filePath}: {ex.Message}");
        }
        
        return news;
    }

    /// <summary>
    /// Get today's cached news for a category
    /// </summary>
    public List<string> GetTodaysNews(GameConfig.NewsCategory category)
    {
        return new List<string>(_dailyNews[category]);
    }

    /// <summary>
    /// Daily maintenance - rotate news files (Pascal MAINT.PAS integration)
    /// </summary>
    public void ProcessDailyNewsMaintenance()
    {
        lock (_newsLock)
        {
            try
            {
                // Rotate current news to yesterday's news (Pascal MAINT.PAS logic)
                if (GameConfig.RotateNewsDaily)
                {
                    RotateNewsFile(GameConfig.NewsAnsiFile, GameConfig.YesterdayNewsAnsiFile);
                    RotateNewsFile(GameConfig.NewsAsciiFile, GameConfig.YesterdayNewsAsciiFile);
                }
                
                // Clear today's news cache
                foreach (var category in _dailyNews.Keys)
                {
                    _dailyNews[category].Clear();
                }
                
                // Archive old news files if configured
                if (GameConfig.ArchiveOldNews)
                {
                    ArchiveOldNewsFiles();
                }
                
                GD.Print("News system daily maintenance completed");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"News maintenance error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Get news statistics for system monitoring
    /// </summary>
    public Dictionary<string, object> GetNewsStatistics()
    {
        var stats = new Dictionary<string, object>();
        
        foreach (var category in Enum.GetValues<GameConfig.NewsCategory>())
        {
            stats[$"Today_{category}"] = _dailyNews[category].Count;
            
            // Count lines in news files
            string ansiFile = GetNewsFileForCategory(category, true);
            string asciiFile = GetNewsFileForCategory(category, false);
            
            if (File.Exists(ansiFile))
                stats[$"File_{category}_ANSI"] = File.ReadAllLines(ansiFile).Length;
            
            if (File.Exists(asciiFile))
                stats[$"File_{category}_ASCII"] = File.ReadAllLines(asciiFile).Length;
        }
        
        return stats;
    }

    #region Private Helper Methods

    private void InitializeNewsFiles()
    {
        // Create news directories if they don't exist
        string scoresDir = GameConfig.ScoreDir;
        if (!Directory.Exists(scoresDir))
        {
            Directory.CreateDirectory(scoresDir);
        }
        
        // Initialize all news files with headers if they don't exist
        InitializeNewsFile(GameConfig.NewsAnsiFile, GameConfig.NewsHeaderDaily);
        InitializeNewsFile(GameConfig.NewsAsciiFile, GameConfig.NewsHeaderDaily);
        InitializeNewsFile(GameConfig.MonarchNewsAnsiFile, GameConfig.NewsHeaderRoyal);
        InitializeNewsFile(GameConfig.MonarchNewsAsciiFile, GameConfig.NewsHeaderRoyal);
        InitializeNewsFile(GameConfig.GodsNewsAnsiFile, GameConfig.NewsHeaderHoly);
        InitializeNewsFile(GameConfig.GodsNewsAsciiFile, GameConfig.NewsHeaderHoly);
        InitializeNewsFile(GameConfig.MarriageNewsAnsiFile, GameConfig.NewsHeaderMarriage);
        InitializeNewsFile(GameConfig.MarriageNewsAsciiFile, GameConfig.NewsHeaderMarriage);
        InitializeNewsFile(GameConfig.BirthNewsAnsiFile, GameConfig.NewsHeaderBirth);
        InitializeNewsFile(GameConfig.BirthNewsAsciiFile, GameConfig.NewsHeaderBirth);
    }

    private void InitializeNewsFile(string filePath, string header)
    {
        if (!File.Exists(filePath) && !string.IsNullOrEmpty(filePath))
        {
            try
            {
                using (var writer = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var textWriter = new StreamWriter(writer, Encoding.UTF8))
                {
                    textWriter.WriteLine(header);
                    textWriter.WriteLine($"Initialized: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    textWriter.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error initializing news file {filePath}: {ex.Message}");
            }
        }
    }

    private string GetNewsFileForCategory(GameConfig.NewsCategory category, bool ansi)
    {
        return category switch
        {
            GameConfig.NewsCategory.General => ansi ? GameConfig.NewsAnsiFile : GameConfig.NewsAsciiFile,
            GameConfig.NewsCategory.Royal => ansi ? GameConfig.MonarchNewsAnsiFile : GameConfig.MonarchNewsAsciiFile,
            GameConfig.NewsCategory.Marriage => ansi ? GameConfig.MarriageNewsAnsiFile : GameConfig.MarriageNewsAsciiFile,
            GameConfig.NewsCategory.Birth => ansi ? GameConfig.BirthNewsAnsiFile : GameConfig.BirthNewsAsciiFile,
            GameConfig.NewsCategory.Holy => ansi ? GameConfig.GodsNewsAnsiFile : GameConfig.GodsNewsAsciiFile,
            GameConfig.NewsCategory.System => ansi ? GameConfig.NewsAnsiFile : GameConfig.NewsAsciiFile,
            _ => ansi ? GameConfig.NewsAnsiFile : GameConfig.NewsAsciiFile
        };
    }

    private string GetNewsPrefixForCategory(GameConfig.NewsCategory category)
    {
        return category switch
        {
            GameConfig.NewsCategory.Royal => GameConfig.NewsPrefixRoyal,
            GameConfig.NewsCategory.Marriage => GameConfig.NewsPrefixMarriage,
            GameConfig.NewsCategory.Birth => GameConfig.NewsPrefixBirth,
            GameConfig.NewsCategory.Holy => GameConfig.NewsPrefixHoly,
            GameConfig.NewsCategory.General => GameConfig.NewsPrefixCombat,
            GameConfig.NewsCategory.System => "",
            _ => ""
        };
    }

    private void AppendToNewsFile(string filePath, string message)
    {
        try
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Pascal-style file appending with locking
            using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
            {
                writer.WriteLine(message);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error appending to news file {filePath}: {ex.Message}");
            throw;
        }
    }

    private void RotateNewsFile(string currentFile, string yesterdayFile)
    {
        try
        {
            // Pascal MAINT.PAS: rename current news to yesterday's news
            if (File.Exists(currentFile) && !string.IsNullOrEmpty(yesterdayFile))
            {
                // Delete old yesterday file if it exists
                if (File.Exists(yesterdayFile))
                {
                    File.Delete(yesterdayFile);
                }
                
                // Move current to yesterday
                File.Move(currentFile, yesterdayFile);
                
                // Reinitialize current file
                string header = GetHeaderForFile(currentFile);
                InitializeNewsFile(currentFile, header);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error rotating news file {currentFile}: {ex.Message}");
        }
    }

    private string GetHeaderForFile(string filePath)
    {
        if (filePath.Contains("MONARCHS")) return GameConfig.NewsHeaderRoyal;
        if (filePath.Contains("GODS")) return GameConfig.NewsHeaderHoly;
        if (filePath.Contains("MARRHIST")) return GameConfig.NewsHeaderMarriage;
        if (filePath.Contains("BIRTHIST")) return GameConfig.NewsHeaderBirth;
        return GameConfig.NewsHeaderDaily;
    }

    private void ArchiveOldNewsFiles()
    {
        // Archive logic for old news files (beyond MaxNewsAge days)
        string archiveDate = DateTime.Now.AddDays(-GameConfig.MaxNewsAge).ToString("yyyyMMdd");
        
        // Implementation would archive files older than MaxNewsAge
        // This is a placeholder for full archive functionality
    }

    private string StripAnsiCodes(string text)
    {
        // Remove Pascal ANSI color codes (` prefixed codes)
        if (string.IsNullOrEmpty(text)) return text;
        
        var result = new StringBuilder();
        bool inAnsiCode = false;
        
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == GameConfig.AnsiControlChar)
            {
                inAnsiCode = true;
                continue;
            }
            
            if (inAnsiCode)
            {
                inAnsiCode = false;
                continue;
            }
            
            result.Append(text[i]);
        }
        
        return result.ToString();
    }

    private void LogUnableToAccess(string filename, string error)
    {
        // Pascal unable_to_access() equivalent
        GD.PrintErr($"Unable to access file: {filename} - {error}");
    }

    #endregion
} 
