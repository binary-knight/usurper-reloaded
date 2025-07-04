using UsurperRemake.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Character Creation System - Complete Pascal USERHUNC.PAS implementation
/// Handles all aspects of new player creation with full Pascal compatibility
/// </summary>
public class CharacterCreationSystem
{
    private readonly TerminalEmulator terminal;
    private readonly Random random;
    
    public CharacterCreationSystem(TerminalEmulator terminal)
    {
        this.terminal = terminal;
        this.random = new Random();
    }
    
    /// <summary>
    /// Main character creation workflow (Pascal USERHUNC.PAS)
    /// </summary>
    public async Task<Character> CreateNewCharacter(string playerName)
    {
        terminal.WriteLine("");
        terminal.WriteLine("--- CHARACTER CREATION ---", "bright_green");
        terminal.WriteLine("");
        terminal.WriteLine($"Welcome, {playerName}!", "yellow");
        terminal.WriteLine("You are about to enter the medieval world of Usurper...");
        terminal.WriteLine("");
        
        // Create base character with Pascal defaults
        var character = CreateBaseCharacter(playerName);
        
        try
        {
            // Step 1: Choose character alias (Pascal alias selection)
            character.Name2 = await SelectCharacterAlias();
            if (string.IsNullOrEmpty(character.Name2))
            {
                return null; // User aborted
            }
            
            // Step 2: Select gender (Pascal gender selection)
            character.Sex = await SelectGender();
            
            // Step 3: Select race (Pascal race selection with help)
            character.Race = await SelectRace();
            
            // Step 4: Select class (Pascal class selection with validation)
            character.Class = await SelectClass(character.Race);
            
            // Step 5: Apply race and class bonuses (Pascal attribute calculation)
            ApplyRaceAndClassBonuses(character);
            
            // Step 6: Generate physical appearance (Pascal appearance generation)
            GeneratePhysicalAppearance(character);
            
            // Step 7: Set starting equipment and configuration
            SetStartingConfiguration(character);
            
            // Step 8: Player preferences (Pascal configuration questions)
            await ConfigurePlayerPreferences(character);
            
            // Step 9: Show character summary and confirm
            await ShowCharacterSummary(character);
            
            var confirm = await terminal.GetInputAsync("Create this character? (Y/N): ");
            if (confirm.ToUpper() != "Y")
            {
                terminal.WriteLine("Character creation aborted.", "red");
                return null;
            }
            
            terminal.WriteLine("");
            terminal.WriteLine("Character created successfully!", "green");
            terminal.WriteLine("Preparing to enter the realm...", "cyan");
            await Task.Delay(2000);
            
            return character;
        }
        catch (Exception ex)
        {
            terminal.WriteLine($"Error during character creation: {ex.Message}", "red");
            return null;
        }
    }
    
    /// <summary>
    /// Create base character with Pascal default values (USERHUNC.PAS)
    /// </summary>
    private Character CreateBaseCharacter(string playerName)
    {
        var character = new Character
        {
            Name1 = playerName,
            Name2 = playerName, // Will be changed in alias selection
            AI = CharacterAI.Human,
            Allowed = true,
            Level = GameConfig.DefaultStartingLevel,
            Gold = GameConfig.DefaultStartingGold,
            BankGold = 0,
            Experience = GameConfig.DefaultStartingExperience,
            Fights = GameConfig.DefaultDungeonFights,
            Healing = GameConfig.DefaultStartingHealing,
            AgePlus = 0,
            DarkNr = GameConfig.DefaultDarkDeeds,
            ChivNr = GameConfig.DefaultGoodDeeds,
            Chivalry = 0,
            Darkness = 0,
            PFights = GameConfig.DefaultPlayerFights,
            King = false,
            Location = GameConfig.OfflineLocationDormitory,
            Team = "",
            TeamPW = "",
            BGuard = 0,
            CTurf = false,
            GnollP = 0,
            Mental = GameConfig.DefaultMentalHealth,
            Addict = 0,
            WeapPow = 0,
            ArmPow = 0,
            AutoHeal = false,
            Loyalty = GameConfig.DefaultLoyalty,
            Haunt = 0,
            Master = '1',
            TFights = GameConfig.DefaultTournamentFights,
            Thiefs = GameConfig.DefaultThiefAttempts,
            Brawls = GameConfig.DefaultBrawls,
            Assa = GameConfig.DefaultAssassinAttempts,
            Poison = 0,
            Trains = 2,
            Immortal = false,
            BattleCry = "",
            BGuardNr = 0,
            Casted = false,
            Punch = 0,
            Deleted = false,
            Quests = 0,
            God = "",
            RoyQuests = 0,
            Resurrections = 3, // Default resurrections
            PickPocketAttempts = 3,
            BankRobberyAttempts = 3,
            ID = GenerateUniqueID()
        };
        
        // Initialize arrays with Pascal defaults
        InitializeCharacterArrays(character);
        
        return character;
    }
    
    /// <summary>
    /// Initialize character arrays to Pascal defaults
    /// </summary>
    private void InitializeCharacterArrays(Character character)
    {
        // Initialize inventory (Pascal: global_maxitem)
        character.Item = new List<int>();
        character.ItemType = new List<ObjType>();
        for (int i = 0; i < GameConfig.MaxItem; i++)
        {
            character.Item.Add(0);
            character.ItemType.Add(ObjType.Head);
        }
        
        // Initialize phrases (Pascal: 6 phrases)
        character.Phrases = new List<string>();
        for (int i = 0; i < 6; i++)
        {
            character.Phrases.Add("");
        }
        
        // Initialize description (Pascal: 4 lines)
        character.Description = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            character.Description.Add("");
        }
        
        // Initialize spells (Pascal: global_maxspells, 2 columns)
        character.Spell = new List<List<bool>>();
        for (int i = 0; i < GameConfig.MaxSpells; i++)
        {
            character.Spell.Add(new List<bool> { false, false });
        }
        // Starting spell (Pascal: player.spell[1, 1] := True)
        character.Spell[0][0] = true;
        
        // Initialize skills (Pascal: global_maxcombat)
        character.Skill = new List<int>();
        for (int i = 0; i < GameConfig.MaxCombat; i++)
        {
            character.Skill.Add(0);
        }
        
        // Initialize medals (Pascal: array[1..20])
        character.Medal = new List<bool>();
        for (int i = 0; i < 20; i++)
        {
            character.Medal.Add(false);
        }
        
        // Initialize equipment slots to empty (Pascal: 0 = no item)
        character.LHand = 0;
        character.RHand = 0;
        character.Head = 0;
        character.Body = 0;
        character.Arms = 0;
        character.LFinger = 0;
        character.RFinger = 0;
        character.Legs = 0;
        character.Feet = 0;
        character.Waist = 0;
        character.Neck = 0;
        character.Neck2 = 0;
        character.Face = 0;
        character.Shield = 0;
        character.Hands = 0;
        character.ABody = 0;
    }
    
    /// <summary>
    /// Select character alias with Pascal validation (USERHUNC.PAS)
    /// </summary>
    private async Task<string> SelectCharacterAlias()
    {
        string alias;
        bool validName = false;
        
        do
        {
            terminal.WriteLine("");
            terminal.WriteLine("Enter your character name (alias):", "cyan");
            terminal.WriteLine("This will be your identity in the realm.");
            terminal.WriteLine("");
            
            alias = await terminal.GetInputAsync("Character name: ");
            
            if (string.IsNullOrWhiteSpace(alias))
            {
                terminal.WriteLine("You must enter a name!", "red");
                continue;
            }
            
            alias = alias.Trim();
            
            // Pascal validation: Check for forbidden names
            var upperAlias = alias.ToUpper();
            if (GameConfig.ForbiddenNames.Contains(upperAlias))
            {
                terminal.WriteLine("I'm sorry, but that name is already being used.", "red");
                continue;
            }
            
            // TODO: Check against existing players and NPCs
            // For now, we'll skip this validation in the prototype
            
            terminal.WriteLine("");
            terminal.WriteLine($"{alias} is what you want? (Y/N)", "yellow");
            var confirm = await terminal.GetInputAsync("");
            
            if (confirm.ToUpper() == "Y")
            {
                validName = true;
            }
            
        } while (!validName);
        
        return alias;
    }
    
    /// <summary>
    /// Select character gender (Pascal USERHUNC.PAS gender selection)
    /// </summary>
    private async Task<CharacterSex> SelectGender()
    {
        while (true)
        {
            terminal.WriteLine("");
            terminal.WriteLine("Gender:", "cyan");
            terminal.WriteLine("(M)ale", "white");
            terminal.WriteLine("(F)emale", "white");
            
            var choice = await terminal.GetInputAsync("Choice: ");
            
            switch (choice.ToUpper())
            {
                case "M":
                    if (await ConfirmChoice("Play a Male character", false))
                        return CharacterSex.Male;
                    break;
                    
                case "F":
                    if (await ConfirmChoice("Play a Female character", false))
                        return CharacterSex.Female;
                    break;
                    
                default:
                    terminal.WriteLine("Please choose M or F.", "red");
                    break;
            }
        }
    }
    
    /// <summary>
    /// Select character race with help system (Pascal USERHUNC.PAS race selection)
    /// </summary>
    private async Task<CharacterRace> SelectRace()
    {
        string choice = "?";
        
        while (true)
        {
            if (choice == "?")
            {
                terminal.Clear();
                terminal.WriteLine("");
                terminal.WriteLine("Choose your Race:", "cyan");
                terminal.WriteLine("");
                
                // Pascal race menu layout
                terminal.WriteLine("(0) Human", "white");
                terminal.WriteLine("(1) Hobbit", "white");
                terminal.WriteLine("(2) Elf", "white");
                terminal.WriteLine("(3) Half-elf", "white");
                terminal.WriteLine("(4) Dwarf", "white");
                terminal.WriteLine("(5) Troll", "white");
                terminal.WriteLine("(6) Orc", "white");
                terminal.WriteLine("(7) Gnome", "white");
                terminal.WriteLine("(8) Gnoll *poisonous bite", "white");
                terminal.WriteLine("(9) Mutant", "white");
                terminal.WriteLine("(H) Help", "green");
                terminal.WriteLine("(A) Abort", "red");
                terminal.WriteLine("");
            }
            
            choice = await terminal.GetInputAsync("Your choice: ");
            
            // Handle help
            if (choice.ToUpper() == "H")
            {
                await ShowRaceHelp();
                choice = "?";
                continue;
            }
            
            // Handle abort
            if (choice.ToUpper() == "A")
            {
                if (await ConfirmChoice("Abort", false))
                {
                    throw new OperationCanceledException("Character creation aborted by user");
                }
                choice = "?";
                continue;
            }
            
            // Handle race selection
            if (int.TryParse(choice, out int raceChoice) && raceChoice >= 0 && raceChoice <= 9)
            {
                var race = (CharacterRace)raceChoice;
                var description = GameConfig.RaceDescriptions[race];
                
                terminal.WriteLine("");
                if (await ConfirmChoice($"Be {description}", false))
                {
                    return race;
                }
                
                choice = "?";
            }
            else
            {
                terminal.WriteLine("Invalid choice. Please select 0-9, H for help, or A to abort.", "red");
            }
        }
    }
    
    /// <summary>
    /// Select character class with race validation (Pascal USERHUNC.PAS class selection)
    /// </summary>
    private async Task<CharacterClass> SelectClass(CharacterRace race)
    {
        string choice = "?";
        
        while (true)
        {
            if (choice == "?")
            {
                terminal.Clear();
                terminal.WriteLine("");
                terminal.WriteLine("Choose your Class:", "cyan");
                terminal.WriteLine("");
                
                // Pascal class menu layout
                terminal.WriteLine("(0) Warrior", "white");
                terminal.WriteLine("(1) Paladin", "white");
                terminal.WriteLine("(2) Ranger", "white");
                terminal.WriteLine("(3) Assassin", "white");
                terminal.WriteLine("(4) Bard", "white");
                terminal.WriteLine("(5) Jester", "white");
                terminal.WriteLine("(6) Alchemist", "white");
                terminal.WriteLine("(7) Magician", "white");
                terminal.WriteLine("(8) Cleric", "white");
                terminal.WriteLine("(9) Sage", "white");
                terminal.WriteLine("(10) Barbarian", "white");
                terminal.WriteLine("(H) Help", "green");
                terminal.WriteLine("(A) Abort", "red");
                terminal.WriteLine("");
            }
            
            choice = await terminal.GetInputAsync("Your choice: ");
            
            // Handle help
            if (choice.ToUpper() == "H")
            {
                await ShowClassHelp();
                choice = "?";
                continue;
            }
            
            // Handle abort
            if (choice.ToUpper() == "A")
            {
                if (await ConfirmChoice("Abort", false))
                {
                    throw new OperationCanceledException("Character creation aborted by user");
                }
                choice = "?";
                continue;
            }
            
            // Handle class selection
            if (int.TryParse(choice, out int classChoice) && classChoice >= 0 && classChoice <= 10)
            {
                var characterClass = (CharacterClass)classChoice;
                
                // Pascal validation: Check invalid race/class combinations
                if (GameConfig.InvalidCombinations.ContainsKey(race) && 
                    GameConfig.InvalidCombinations[race].Contains(characterClass))
                {
                    terminal.WriteLine("");
                    terminal.WriteLine($"Sorry, {GameConfig.RaceNames[(int)race]} cannot be {GameConfig.ClassNames[(int)characterClass]}!", "red");
                    terminal.WriteLine("Some race/class combinations are impossible.", "yellow");
                    await Task.Delay(2000);
                    choice = "?";
                    continue;
                }
                
                var className = GameConfig.ClassNames[(int)characterClass];
                
                terminal.WriteLine("");
                if (await ConfirmChoice($"Be a {className}", false))
                {
                    return characterClass;
                }
                
                choice = "?";
            }
            else
            {
                terminal.WriteLine("Invalid choice. Please select 0-10, H for help, or A to abort.", "red");
            }
        }
    }
    
    /// <summary>
    /// Apply race and class bonuses (Pascal USERHUNC.PAS attribute calculation)
    /// </summary>
    private void ApplyRaceAndClassBonuses(Character character)
    {
        // Apply class starting attributes (Pascal class case statement)
        var classAttrib = GameConfig.ClassStartingAttributes[character.Class];
        character.HP = classAttrib.HP;
        character.Strength = classAttrib.Strength;
        character.Defence = classAttrib.Defence;
        character.Stamina = classAttrib.Stamina;
        character.Agility = classAttrib.Agility;
        character.Charisma = classAttrib.Charisma;
        character.Dexterity = classAttrib.Dexterity;
        character.Wisdom = classAttrib.Wisdom;
        character.Mana = classAttrib.Mana;
        character.MaxMana = classAttrib.MaxMana;
        
        // Apply race bonuses (Pascal race case statement)
        var raceAttrib = GameConfig.RaceAttributes[character.Race];
        character.HP += raceAttrib.HPBonus;
        character.Strength += raceAttrib.StrengthBonus;
        character.Defence += raceAttrib.DefenceBonus;
        character.Stamina += raceAttrib.StaminaBonus;
        
        // Set MaxHP equal to HP (Pascal: player.maxhps := player.hps)
        character.MaxHP = character.HP;
    }
    
    /// <summary>
    /// Generate physical appearance based on race (Pascal USERHUNC.PAS appearance generation)
    /// </summary>
    private void GeneratePhysicalAppearance(Character character)
    {
        var raceAttrib = GameConfig.RaceAttributes[character.Race];
        
        // Generate age (Pascal random range)
        character.Age = random.Next(raceAttrib.MinAge, raceAttrib.MaxAge + 1);
        
        // Generate height (Pascal random range)
        character.Height = random.Next(raceAttrib.MinHeight, raceAttrib.MaxHeight + 1);
        
        // Generate weight (Pascal random range)
        character.Weight = random.Next(raceAttrib.MinWeight, raceAttrib.MaxWeight + 1);
        
        // Generate eye color (Pascal: random(5) + 1)
        character.Eyes = random.Next(1, 6);
        
        // Generate skin color (Pascal race-specific or random for mutants)
        if (character.Race == CharacterRace.Mutant)
        {
            character.Skin = random.Next(1, 11); // Mutants have random skin (1-10)
        }
        else
        {
            character.Skin = raceAttrib.SkinColor;
        }
        
        // Generate hair color (Pascal race-specific or random for mutants)
        if (character.Race == CharacterRace.Mutant)
        {
            character.Hair = random.Next(1, 11); // Mutants have random hair (1-10)
        }
        else
        {
            // Select random hair color from race's possible colors
            if (raceAttrib.HairColors.Length > 0)
            {
                character.Hair = raceAttrib.HairColors[random.Next(raceAttrib.HairColors.Length)];
            }
            else
            {
                character.Hair = 1; // Default to black
            }
        }
    }
    
    /// <summary>
    /// Set starting configuration and status (Pascal USERHUNC.PAS defaults)
    /// </summary>
    private void SetStartingConfiguration(Character character)
    {
        // Set remaining Pascal defaults
        character.WellWish = false;
        character.MKills = 0;
        character.MDefeats = 0;
        character.PKills = 0;
        character.PDefeats = 0;
        character.Interest = 0;
        character.AliveBonus = 0;
        character.Expert = false;
        character.MaxTime = 60; // Default max time per session
        character.Ear = 1; // global_ear_all
        character.CastIn = ' ';
        character.Weapon = 0;
        character.Armor = 0;
        character.APow = 0;
        character.WPow = 0;
        character.DisRes = 0;
        character.AMember = false;
        character.BankGuard = false;
        character.BankWage = 0;
        character.WeapHag = 3;
        character.ArmHag = 3;
        character.AutoMenu = true; // Pascal: player.auto_meny := True
        character.RoyTaxPaid = 0;
        character.Wrestlings = 3;
        character.DrinksLeft = 3;
        character.DaysInPrison = 0;
        character.UmanBearTries = 0;
        character.Massage = 0;
        character.GymSessions = 3;
        character.GymOwner = 0;
        character.GymCard = 0;
        character.RoyQuestsToday = 0;
        character.KingVotePoll = 200;
        character.KingLastVote = 0;
        character.Married = false;
        character.Kids = 0;
        character.IntimacyActs = 5;
        character.Pregnancy = 0;
        character.FatherID = "";
        character.AutoHate = 1;
        character.TaxRelief = false;
        character.MarriedTimes = 0;
        character.BardSongsLeft = 5;
        character.PrisonEscapes = 2;
        
        // Disease status (all false by default)
        character.Blind = false;
        character.Plague = false;
        character.Smallpox = false;
        character.Measles = false;
        character.Leprosy = false;
        character.Mercy = 0;
        
        // Set last on date to current (Pascal: packed_date)
        character.LastOn = DateTimeOffset.Now.ToUnixTimeSeconds();
    }
    
    /// <summary>
    /// Configure player preferences (Pascal USERHUNC.PAS configuration questions)
    /// </summary>
    private async Task ConfigurePlayerPreferences(Character character)
    {
        terminal.WriteLine("");
        terminal.WriteLine("I will now ask you a few questions on how", "cyan");
        terminal.WriteLine("you would like your character to behave.", "cyan");
        terminal.WriteLine("You can always change these options later on.", "cyan");
        terminal.WriteLine("");
        
        // Auto-display menus (Pascal question)
        terminal.WriteLine("Would you like to auto-display menus? (Y/N)", "yellow");
        var autoMenu = await terminal.GetInputAsync("");
        character.AutoMenu = autoMenu.ToUpper() != "N";
        
        if (character.AutoMenu)
        {
            terminal.WriteLine("Auto-Display menus is ON.", "green");
        }
        else
        {
            terminal.WriteLine("Auto-Display menus is OFF.", "green");
        }
        
        terminal.WriteLine("");
        
        // Auto-hate setting (Pascal attitude question)
        terminal.WriteLine("Attitude when attacked:", "cyan");
        terminal.WriteLine("When you are attacked, do you want your relation", "white");
        terminal.WriteLine("with the attacker to automatically deteriorate?", "white");
        
        var autoHate = await terminal.GetInputAsync("(Y/N): ");
        character.AutoHate = (byte)(autoHate.ToUpper() == "Y" ? 1 : 0);
        
        terminal.WriteLine("");
    }
    
    /// <summary>
    /// Show character summary before creation (Pascal display)
    /// </summary>
    private async Task ShowCharacterSummary(Character character)
    {
        terminal.Clear();
        terminal.WriteLine("");
        terminal.WriteLine("--- CHARACTER SUMMARY ---", "bright_green");
        terminal.WriteLine("");
        
        terminal.WriteLine($"Name: {character.Name2}", "cyan");
        terminal.WriteLine($"Real Name: {character.Name1}", "white");
        terminal.WriteLine($"Race: {GameConfig.RaceNames[(int)character.Race]}", "yellow");
        terminal.WriteLine($"Class: {GameConfig.ClassNames[(int)character.Class]}", "yellow");
        terminal.WriteLine($"Sex: {(character.Sex == CharacterSex.Male ? "Male" : "Female")}", "white");
        terminal.WriteLine($"Age: {character.Age}", "white");
        terminal.WriteLine("");
        
        terminal.WriteLine("=== ATTRIBUTES ===", "green");
        terminal.WriteLine($"Hit Points: {character.HP}/{character.MaxHP}", "white");
        terminal.WriteLine($"Strength: {character.Strength}", "white");
        terminal.WriteLine($"Defence: {character.Defence}", "white");
        terminal.WriteLine($"Stamina: {character.Stamina}", "white");
        terminal.WriteLine($"Agility: {character.Agility}", "white");
        terminal.WriteLine($"Charisma: {character.Charisma}", "white");
        terminal.WriteLine($"Dexterity: {character.Dexterity}", "white");
        terminal.WriteLine($"Wisdom: {character.Wisdom}", "white");
        if (character.MaxMana > 0)
        {
            terminal.WriteLine($"Mana: {character.Mana}/{character.MaxMana}", "cyan");
        }
        terminal.WriteLine("");
        
        terminal.WriteLine("=== APPEARANCE ===", "green");
        terminal.WriteLine($"Height: {character.Height} cm", "white");
        terminal.WriteLine($"Weight: {character.Weight} kg", "white");
        terminal.WriteLine($"Eyes: {GameConfig.EyeColors[character.Eyes]}", "white");
        terminal.WriteLine($"Hair: {GameConfig.HairColors[character.Hair]}", "white");
        terminal.WriteLine($"Skin: {GameConfig.SkinColors[character.Skin]}", "white");
        terminal.WriteLine("");
        
        terminal.WriteLine("=== STARTING RESOURCES ===", "green");
        terminal.WriteLine($"Gold: {character.Gold}", "yellow");
        terminal.WriteLine($"Experience: {character.Experience}", "white");
        terminal.WriteLine($"Level: {character.Level}", "white");
        terminal.WriteLine($"Healing Potions: {character.Healing}", "white");
        terminal.WriteLine("");
        
        await terminal.GetInputAsync("Press Enter to continue...");
    }
    
    /// <summary>
    /// Show race help text (Pascal RACEHELP display)
    /// </summary>
    private async Task ShowRaceHelp()
    {
        terminal.Clear();
        terminal.WriteLine("");
        terminal.WriteLine("--- RACE INFORMATION ---", "bright_green");
        terminal.WriteLine("");
        terminal.WriteLine(GameConfig.RaceHelpText, "white");
        await terminal.GetInputAsync("Press Enter to continue...");
    }
    
    /// <summary>
    /// Show class help text (Pascal class help)
    /// </summary>
    private async Task ShowClassHelp()
    {
        terminal.Clear();
        terminal.WriteLine("");
        terminal.WriteLine("--- CLASS INFORMATION ---", "bright_green");
        terminal.WriteLine("");
        terminal.WriteLine(GameConfig.ClassHelpText, "white");
        await terminal.GetInputAsync("Press Enter to continue...");
    }
    
    /// <summary>
    /// Pascal confirm function implementation
    /// </summary>
    private async Task<bool> ConfirmChoice(string message, bool defaultYes)
    {
        var defaultText = defaultYes ? "Y" : "N";
        var response = await terminal.GetInputAsync($"{message}? ({defaultText}): ");
        
        if (string.IsNullOrEmpty(response))
        {
            return defaultYes;
        }
        
        return response.ToUpper() == "Y";
    }
    
    /// <summary>
    /// Generate unique player ID (Pascal crypt(15))
    /// </summary>
    private string GenerateUniqueID()
    {
        return Guid.NewGuid().ToString("N")[..15];
    }
}
