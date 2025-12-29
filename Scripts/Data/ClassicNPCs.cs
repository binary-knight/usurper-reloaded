using System.Collections.Generic;

namespace UsurperRemake.Data
{
    /// <summary>
    /// Classic Usurper NPCs with original-style names and personalities
    /// Based on the classic BBS door game NPCs
    /// </summary>
    public static class ClassicNPCs
    {
        public static List<NPCTemplate> GetClassicNPCs()
        {
            return new List<NPCTemplate>
            {
                // Fighters & Warriors (10)
                new() { Name = "Grok the Destroyer", Class = CharacterClass.Warrior, Race = CharacterRace.Orc,
                    Personality = "Aggressive", Alignment = "Evil", StartLevel = 5 },
                new() { Name = "Sir Galahad", Class = CharacterClass.Paladin, Race = CharacterRace.Human,
                    Personality = "Honorable", Alignment = "Good", StartLevel = 7 },
                new() { Name = "Blackthorne", Class = CharacterClass.Warrior, Race = CharacterRace.Human,
                    Personality = "Ruthless", Alignment = "Evil", StartLevel = 6 },
                new() { Name = "Ursula Ironheart", Class = CharacterClass.Warrior, Race = CharacterRace.Dwarf,
                    Personality = "Brave", Alignment = "Neutral", StartLevel = 5 },
                new() { Name = "Ragnar Bloodaxe", Class = CharacterClass.Barbarian, Race = CharacterRace.Human,
                    Personality = "Fierce", Alignment = "Neutral", StartLevel = 6 },
                new() { Name = "Lady Morgana", Class = CharacterClass.Paladin, Race = CharacterRace.Human,
                    Personality = "Noble", Alignment = "Good", StartLevel = 7 },
                new() { Name = "Grimbold Stonefist", Class = CharacterClass.Warrior, Race = CharacterRace.Dwarf,
                    Personality = "Stubborn", Alignment = "Good", StartLevel = 5 },
                new() { Name = "Vex the Merciless", Class = CharacterClass.Warrior, Race = CharacterRace.Orc,
                    Personality = "Cruel", Alignment = "Evil", StartLevel = 6 },
                new() { Name = "Thorn Blackblade", Class = CharacterClass.Assassin, Race = CharacterRace.Elf,
                    Personality = "Cunning", Alignment = "Evil", StartLevel = 8 },
                new() { Name = "Borin Hammerhand", Class = CharacterClass.Warrior, Race = CharacterRace.Dwarf,
                    Personality = "Loyal", Alignment = "Good", StartLevel = 5 },

                // Mages & Wizards (10)
                new() { Name = "Malachi the Dark", Class = CharacterClass.Magician, Race = CharacterRace.Human,
                    Personality = "Mysterious", Alignment = "Evil", StartLevel = 9 },
                new() { Name = "Elara Moonwhisper", Class = CharacterClass.Magician, Race = CharacterRace.Elf,
                    Personality = "Wise", Alignment = "Good", StartLevel = 8 },
                new() { Name = "Zoltar the Arcane", Class = CharacterClass.Magician, Race = CharacterRace.Human,
                    Personality = "Arrogant", Alignment = "Neutral", StartLevel = 10 },
                new() { Name = "Ravenna Shadowmoon", Class = CharacterClass.Magician, Race = CharacterRace.Elf,
                    Personality = "Scheming", Alignment = "Evil", StartLevel = 9 },
                new() { Name = "Aldric Stormcaller", Class = CharacterClass.Magician, Race = CharacterRace.Human,
                    Personality = "Eccentric", Alignment = "Neutral", StartLevel = 8 },
                new() { Name = "Seraphina Lightbringer", Class = CharacterClass.Cleric, Race = CharacterRace.Elf,
                    Personality = "Compassionate", Alignment = "Good", StartLevel = 7 },
                new() { Name = "Mord the Necromancer", Class = CharacterClass.Magician, Race = CharacterRace.Human,
                    Personality = "Sinister", Alignment = "Evil", StartLevel = 11 },
                new() { Name = "Thalia Starweaver", Class = CharacterClass.Magician, Race = CharacterRace.Elf,
                    Personality = "Curious", Alignment = "Neutral", StartLevel = 8 },
                new() { Name = "Caius Bloodstone", Class = CharacterClass.Magician, Race = CharacterRace.Human,
                    Personality = "Ambitious", Alignment = "Evil", StartLevel = 9 },
                new() { Name = "Lysandra the Pure", Class = CharacterClass.Cleric, Race = CharacterRace.Human,
                    Personality = "Devout", Alignment = "Good", StartLevel = 7 },

                // Thieves & Rogues (10)
                new() { Name = "Sly Fingers McGee", Class = CharacterClass.Assassin, Race = CharacterRace.Hobbit,
                    Personality = "Greedy", Alignment = "Neutral", StartLevel = 6 },
                new() { Name = "Shadow", Class = CharacterClass.Assassin, Race = CharacterRace.Human,
                    Personality = "Silent", Alignment = "Evil", StartLevel = 8 },
                new() { Name = "Nimble Nick", Class = CharacterClass.Assassin, Race = CharacterRace.Hobbit,
                    Personality = "Cowardly", Alignment = "Neutral", StartLevel = 5 },
                new() { Name = "Viper", Class = CharacterClass.Assassin, Race = CharacterRace.Elf,
                    Personality = "Deadly", Alignment = "Evil", StartLevel = 9 },
                new() { Name = "Quicksilver Quinn", Class = CharacterClass.Assassin, Race = CharacterRace.Human,
                    Personality = "Charming", Alignment = "Neutral", StartLevel = 6 },
                new() { Name = "Dagger Dee", Class = CharacterClass.Assassin, Race = CharacterRace.Hobbit,
                    Personality = "Sneaky", Alignment = "Neutral", StartLevel = 5 },
                new() { Name = "Raven Nightblade", Class = CharacterClass.Assassin, Race = CharacterRace.Elf,
                    Personality = "Professional", Alignment = "Neutral", StartLevel = 8 },
                new() { Name = "Pickpocket Pete", Class = CharacterClass.Assassin, Race = CharacterRace.Human,
                    Personality = "Nervous", Alignment = "Neutral", StartLevel = 4 },
                new() { Name = "Whisper", Class = CharacterClass.Assassin, Race = CharacterRace.Human,
                    Personality = "Cold", Alignment = "Evil", StartLevel = 10 },
                new() { Name = "Slick Rick", Class = CharacterClass.Assassin, Race = CharacterRace.Hobbit,
                    Personality = "Lucky", Alignment = "Neutral", StartLevel = 6 },

                // Paladins & Clerics (8)
                new() { Name = "Brother Benedict", Class = CharacterClass.Cleric, Race = CharacterRace.Human,
                    Personality = "Pious", Alignment = "Good", StartLevel = 7 },
                new() { Name = "Valeria the Righteous", Class = CharacterClass.Paladin, Race = CharacterRace.Human,
                    Personality = "Zealous", Alignment = "Good", StartLevel = 8 },
                new() { Name = "Father Grimwald", Class = CharacterClass.Cleric, Race = CharacterRace.Dwarf,
                    Personality = "Stern", Alignment = "Good", StartLevel = 7 },
                new() { Name = "Sister Mercy", Class = CharacterClass.Cleric, Race = CharacterRace.Elf,
                    Personality = "Kind", Alignment = "Good", StartLevel = 6 },
                new() { Name = "Sir Cedric the Pure", Class = CharacterClass.Paladin, Race = CharacterRace.Human,
                    Personality = "Righteous", Alignment = "Good", StartLevel = 9 },
                new() { Name = "Evangeline Lighttouch", Class = CharacterClass.Cleric, Race = CharacterRace.Human,
                    Personality = "Gentle", Alignment = "Good", StartLevel = 6 },
                new() { Name = "Thorgrim Holyhammer", Class = CharacterClass.Paladin, Race = CharacterRace.Dwarf,
                    Personality = "Resolute", Alignment = "Good", StartLevel = 8 },
                new() { Name = "Mother Superior", Class = CharacterClass.Cleric, Race = CharacterRace.Human,
                    Personality = "Strict", Alignment = "Good", StartLevel = 10 },

                // Monks & Rangers (6)
                new() { Name = "Master Wu", Class = CharacterClass.Sage, Race = CharacterRace.Human,
                    Personality = "Serene", Alignment = "Neutral", StartLevel = 12 },
                new() { Name = "Wildwood", Class = CharacterClass.Ranger, Race = CharacterRace.Elf,
                    Personality = "Solitary", Alignment = "Neutral", StartLevel = 7 },
                new() { Name = "Kai the Swift", Class = CharacterClass.Sage, Race = CharacterRace.Human,
                    Personality = "Disciplined", Alignment = "Good", StartLevel = 10 },
                new() { Name = "Hawkeye", Class = CharacterClass.Ranger, Race = CharacterRace.Hobbit,
                    Personality = "Sharp", Alignment = "Neutral", StartLevel = 6 },
                new() { Name = "Zen Master Lotus", Class = CharacterClass.Sage, Race = CharacterRace.Elf,
                    Personality = "Peaceful", Alignment = "Good", StartLevel = 14 },
                new() { Name = "Thornwood", Class = CharacterClass.Ranger, Race = CharacterRace.Human,
                    Personality = "Gruff", Alignment = "Neutral", StartLevel = 7 },

                // Mercenaries & Misc (6)
                new() { Name = "Scarface Sam", Class = CharacterClass.Warrior, Race = CharacterRace.Human,
                    Personality = "Brutal", Alignment = "Evil", StartLevel = 6 },
                new() { Name = "Gold Tooth Gary", Class = CharacterClass.Assassin, Race = CharacterRace.Human,
                    Personality = "Flashy", Alignment = "Neutral", StartLevel = 5 },
                new() { Name = "Mad Dog Morgan", Class = CharacterClass.Barbarian, Race = CharacterRace.Human,
                    Personality = "Insane", Alignment = "Evil", StartLevel = 7 },
                new() { Name = "The Executioner", Class = CharacterClass.Warrior, Race = CharacterRace.Orc,
                    Personality = "Merciless", Alignment = "Evil", StartLevel = 9 },
                new() { Name = "Lucky Lou", Class = CharacterClass.Assassin, Race = CharacterRace.Hobbit,
                    Personality = "Optimistic", Alignment = "Neutral", StartLevel = 4 },
                new() { Name = "Ironjaw", Class = CharacterClass.Warrior, Race = CharacterRace.Dwarf,
                    Personality = "Tough", Alignment = "Neutral", StartLevel = 8 }
            };
        }
    }

    public class NPCTemplate
    {
        public string Name { get; set; } = "";
        public CharacterClass Class { get; set; }
        public CharacterRace Race { get; set; }
        public string Personality { get; set; } = "";
        public string Alignment { get; set; } = "";
        public int StartLevel { get; set; } = 5;
    }
}
