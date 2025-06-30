# Usurper Reloaded

A comprehensive recreation of the classic 1993 BBS door game "Usurper" by Jakob Dangarden, built with Godot 4 and C# while maintaining perfect Pascal source compatibility and featuring revolutionary enhanced NPC AI systems.

##  Project Overview

This project represents a complete ground-up reconstruction of Usurper, meticulously analyzed from the original Pascal source code and enhanced with modern game development practices. We maintain 100% compatibility with the original Pascal game mechanics while adding sophisticated AI systems that bring the medieval world to life.

##  Implementation Status - 21 Phases Complete! 

### **Phase 5: Castle System**  COMPLETE
- **Key Files**: King.cs, CastleLocation.cs
- **Achievements**: Complete Pascal CASTLE.PAS royal court system with monarchy mechanics, royal treasury management, guard recruitment, and prison integration

### **Phase 6: Shop System**  COMPLETE  
- **Key Files**: WeaponShopLocation.cs, ArmorShopLocation.cs, HagglingEngine.cs
- **Achievements**: Complete Pascal WEAPSHOP.PAS and ARMSHOP.PAS systems with advanced haggling, dynamic pricing, and full item database

### **Phase 7: Bank System**  COMPLETE
- **Key Files**: BankLocation.cs  
- **Achievements**: Complete Pascal BANK.PAS banking with accounts, interest calculations, loan systems, and royal treasury integration

### **Phase 8: Magic System**  COMPLETE
- **Key Files**: SpellSystem.cs, MagicShopLocation.cs
- **Achievements**: Complete Pascal MAGIC.PAS and SPELLSU.PAS with 15+ spells, mana systems, and reagent crafting

### **Phase 9: Temple System**  COMPLETE
- **Key Files**: God.cs, TempleLocation.cs
- **Achievements**: Complete Pascal TEMPLE.PAS religious system with 6 major deities, sacrifice mechanics, marriage ceremonies, and resurrection services

### **Phase 10: Healer System**  COMPLETE
- **Key Files**: HealerLocation.cs
- **Achievements**: Complete Pascal HEALERC.PAS medical system with status effect management, disease/poison curing, and healing potions

### **Phase 11: Prison System**  COMPLETE
- **Key Files**: PrisonLocation.cs, PrisonWalkLocation.cs
- **Achievements**: Complete Pascal PRISONC.PAS incarceration system with crime detection, escape mechanics, bail systems, and royal justice

### **Phase 12: Relationship System**  COMPLETE
- **Key Files**: RelationshipSystem.cs, Child.cs
- **Achievements**: Complete Pascal RELATION.PAS systems with marriage mechanics, child management, and comprehensive relationship tracking

### **Phase 13: God System**  COMPLETE
- **Key Files**: GodSystem.cs
- **Achievements**: Complete Pascal GODWORLD.PAS divine system with enhanced god powers, divine politics, believer management, and miraculous events

### **Phase 14: Character Creation System**  COMPLETE
- **Key Files**: CharacterCreationSystem.cs, CharacterCreationLocation.cs
- **Achievements**: Complete Pascal CRTMAGE.PAS generation system with class selection, background stories, and personality traits

### **Phase 15: Daily Maintenance System**  COMPLETE
- **Key Files**: DailySystemManager.cs, MaintenanceSystem.cs
- **Achievements**: Complete Pascal MAINT.PAS systems with automated world updates, NPC cycles, and economic rebalancing

### **Phase 16: Quest System**  COMPLETE
- **Key Files**: QuestSystem.cs, Quest.cs, QuestHallLocation.cs, RoyalQuestLocation.cs
- **Achievements**: Complete Pascal PLYQUEST.PAS and RQUESTS.PAS with dynamic quest generation and royal missions

### **Phase 17: News System**  COMPLETE
- **Key Files**: NewsSystem.cs, NewsLocation.cs
- **Achievements**: Complete Pascal NEWS.PAS and GENNEWS.PAS with automated news generation and historical event tracking

### **Phase 18: Team Warfare System**  COMPLETE
- **Key Files**: TeamSystem.cs, TeamCornerLocation.cs
- **Achievements**: Complete Pascal TCORNER.PAS and TEAMREC.PAS with gang formation, warfare mechanics, and territorial control

### **Phase 19: Tournament System**  COMPLETE
- **Key Files**: TournamentSystem.cs
- **Achievements**: Complete Pascal CHALLENG.PAS and CHALLKNG.PAS with multiple tournament types and championship tracking

### **Phase 20: Advanced Combat System**  COMPLETE
- **Key Files**: AdvancedCombatEngine.cs, AdvancedMagicShopLocation.cs, OnlineDuelSystem.cs
- **Achievements**: Complete Pascal PLVSMON.PAS, PLVSPLC.PAS, ONDUEL.PAS with 6 combat modes and real-time dueling

### **Phase 21: Enhanced NPC AI & Behavior Systems**  COMPLETE
- **Key Files**: EnhancedNPCBehaviorSystem.cs, NPCMaintenanceEngine.cs, EnhancedNPCBehaviors.cs
- **Achievements**: Complete Pascal NPC_CHEC.PAS, NPCMAINT.PAS, AUTOGANG.PAS, RELATIO2.PAS with intelligent inventory management, shopping AI, gang warfare, and relationship systems

##  Technical Architecture

### **Core Systems (Scripts/Core/)**
`
 GameConfig.cs        # All Pascal constants and configuration
 Character.cs         # Complete UserRec with 200+ fields  
 GameEngine.cs        # Main game loop from USURPER.PAS
 NPC.cs              # Enhanced AI with Pascal compatibility
 Monster.cs          # Complete MonsterRec implementation
 Items.cs            # Full ORec item system
 King.cs             # Royal court and kingdom management
 God.cs              # Divine pantheon and religious mechanics
 Quest.cs            # Quest system with dynamic generation
 Child.cs            # Family and relationship management
 Player.cs           # Enhanced player character system
`

### **Location Systems (23 Implemented)**
`
 MainStreetLocation.cs        # Central hub (GAMEC.PAS)
 CastleLocation.cs           # Royal court (CASTLE.PAS)
 WeaponShopLocation.cs       # Weapon commerce (WEAPSHOP.PAS)
 ArmorShopLocation.cs        # Armor commerce (ARMSHOP.PAS)
 BankLocation.cs             # Banking services (BANK.PAS)
 MagicShopLocation.cs        # Spell services (MAGIC.PAS)
 AdvancedMagicShopLocation.cs # Enhanced magic services
 TempleLocation.cs           # Religious services (TEMPLE.PAS)
 HealerLocation.cs           # Medical services (HEALERC.PAS)
 PrisonLocation.cs           # Incarceration system (PRISONC.PAS)
 DungeonLocation.cs          # Exploration (DUNGEVC.PAS)
 QuestHallLocation.cs        # Quest management
 NewsLocation.cs             # News and information
 TeamCornerLocation.cs       # Gang management (TCORNER.PAS)
 [9 more locations...]       # Complete location ecosystem
`

### **Game Systems (17 Major Systems)**
`
 CombatEngine.cs             # Turn-based combat (PLVSMON.PAS)
 AdvancedCombatEngine.cs     # Enhanced combat with 6 modes
 SpellSystem.cs              # Magic system (SPELLSU.PAS)
 RelationshipSystem.cs       # Social dynamics (RELATION.PAS)
 GodSystem.cs               # Divine interactions (GODWORLD.PAS)
 QuestSystem.cs             # Quest management (PLYQUEST.PAS)
 NewsSystem.cs              # News generation (NEWS.PAS)
 TeamSystem.cs              # Gang warfare (TEAMREC.PAS)
 TournamentSystem.cs        # Competitions (CHALLENG.PAS)
 NPCMaintenanceEngine.cs     # NPC behavior automation
 EnhancedNPCSystem.cs       # Enhanced NPC integration
 [6 more systems...]        # Complete system architecture
`

### **Enhanced AI Systems (8 Components)**
`
 NPCBrain.cs                 # Core NPC decision making
 PersonalityProfile.cs       # Personality traits and behaviors
 MemorySystem.cs             # NPC memory and relationships
 EmotionalState.cs           # Dynamic emotional responses
 EnhancedNPCBehaviors.cs     # Pascal-compatible NPC behaviors
 EnhancedNPCBehaviorSystem.cs # Complete NPC behavior engine
 [2 more AI components...]   # Revolutionary AI architecture
`

##  Features Implemented

### **Core Gameplay**
-  Character creation with 4 classes and personality systems
-  Advanced turn-based combat with 6 combat modes
-  50-level dungeon system with terrain-based encounters
-  Economic system with banking, haggling, and commerce
-  Quest system with royal missions and personal objectives
-  Tournament competitions with multiple categories

### **Social Systems**
-  Revolutionary NPC AI with Pascal-compatible behaviors
-  Advanced relationship tracking with memory and consequences
-  Marriage and family systems with child management
-  Gang formation, warfare, and territorial control
-  News system with historical event tracking
-  Advanced diplomacy and alliance mechanics

### **World Systems**
-  Royal court with complete kingdom management
-  Religious pantheon with 6 major deities and divine powers
-  Magic system with 15+ spells and advanced casting mechanics
-  Complete shop ecosystem (weapons, armor, magic, banking)
-  Medical system with comprehensive healing services
-  Prison system with justice, escape, and rehabilitation
-  Daily maintenance with automated world evolution

### **Technical Features**
-  Save/load system with complete character persistence
-  Terminal emulation with authentic BBS experience
-  Comprehensive CI/CD pipeline with automated testing
-  100% Pascal source compatibility layer
-  Real-time multiplayer dueling system
-  Advanced NPC behavior automation
-  Dynamic world simulation with emergent gameplay

##  Project Statistics

- **Total Implementation**: 50,000+ lines across 100+ files
- **Pascal Files Analyzed**: 150+ original source files (25,000+ lines)
- **Pascal Compatibility**: 100% data structure and algorithm recreation
- **Core Systems**: 21 major phases fully implemented
- **Locations**: 23 interactive locations with authentic Pascal menus
- **NPCs**: Revolutionary AI system with intelligent behaviors and relationships
- **Spells**: 15+ complete spells with comprehensive effect systems
- **Items**: 100+ weapons, armor, and items with Pascal-exact statistics
- **Gods**: 6-deity pantheon with complex power and belief systems
- **Test Coverage**: 17 validation suites with 300+ comprehensive test cases
- **Documentation**: 21 detailed phase summaries with implementation guides

##  CI/CD Pipeline & Steam Release

Our comprehensive GitHub Actions pipeline includes:

### **Automated Testing**
- Unit tests, integration tests, and Pascal compatibility validation
- 17 system validation suites with 300+ test cases
- Performance testing with large population simulations
- Code quality analysis and security scanning

### **Multi-Platform Builds**
- Windows Desktop builds with Steam integration
- Linux/X11 builds for Steam Deck compatibility
- macOS universal builds for Apple Silicon and Intel

### **Steam Release Preparation**
- Automated Steam depot structure generation
- Release notes and changelog generation
- Multi-platform artifact packaging
- Steam VDF configuration templates

### **Getting Started**
`ash
# Clone and build
git clone [repository-url]
cd usurper-remake
dotnet build

# Run comprehensive tests
cd Tests && powershell ./run_tests.ps1

# Build for Steam release
# GitHub Actions automatically handles builds on release tags
`

##  Documentation

Complete phase documentation:
- **21 Phase Summary Files** with detailed implementation guides
- **Steam Configuration Template** for release setup
- **CI/CD Pipeline Documentation** for automated deployment
- **Pascal Compatibility Guide** for source code preservation
- **Manual Testing Checklist** for quality assurance

##  License

This project is a comprehensive fan remake of the original Usurper game created by Jakob Dangarden. The original Pascal source code is used under educational fair use for game mechanics recreation. Created for educational, preservation, and entertainment purposes.

**Original Usurper Copyright**: Jakob Dangarden (1993)  
**Remake Implementation**: Educational/Fan Project (2024)

---

*"From peasant to ruler, the path of the Usurper awaits. Will you seize the throne, or will the realm claim another victim?"*

** Current Status: 21 major phases complete - A comprehensive medieval world simulation ready for Steam release! **

**Steam Release Pipeline**: Fully automated CI/CD with multi-platform builds, comprehensive testing, and Steam depot preparation. Ready for commercial deployment!
