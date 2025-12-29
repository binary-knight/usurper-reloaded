# Usurper Reloaded

** FREE AND OPEN SOURCE SOFTWARE - GPL v2 Licensed**

A faithful recreation of the classic 1993 BBS door game "Usurper" by Jakob Dangarden, enhanced with revolutionary NPC AI systems while maintaining 100% Pascal source compatibility.

## About

Usurper Reloaded brings the brutal medieval world of the original BBS classic to modern platforms. Every formula, every stat, every quirk from the original Pascal source has been meticulously preserved, while adding sophisticated AI systems that make the world feel truly alive.

**Original Creator**: Jakob Dangarden (1993)  
**Source Preservation**: Rick Parrish, Daniel Zingaro  
**Modern Recreation**: Built with Godot 4.4+ and C#  
**License**: GNU General Public License v2 (GPL v2)

## Current Status & Implemented Features (Alpha - Active Development)

Progress has moved from "bare foundations" to a **play-testable alpha** with continuous development.

### Core Engine
* Godot 4.4+ Mono build with DOS-style terminal interface
* Full data layer – `Character`, `Player`, `NPC`, `Item`, `Monster`, `Quest` and comprehensive `GameConfig`
* **Persistent Turn System** - Counts upward from 0, drives world simulation every 5 turns
* **Modern Save System** - Autosaves with rotation (keeps 5 most recent), manual saves, comprehensive state preservation

### Playable Locations (30 location files)
* **Main Street, Inn, Bank, Weapon Shop, Armor Shop, Magic Shop, Advanced Magic Shop, Healer, Temple**
* **Church, Dark Alley, Level Master, Marketplace, Anchor Road, Hall of Recruitment, Dormitory**
* **Castle, Prison, Love Corner, Team Corner, News, Quest Hall, Royal Quest, God World**
* **Dungeon** - 50+ level system with groups of 1-5 monsters (regular encounters) and rare boss fights
* **Home, Gym, Prison Walk** - Additional specialized locations

### Combat & Magic Systems
* **Advanced Combat Engine** - 6 combat modes (Attack, Defend, Power Attack, Precise Strike, Rage, Smite)
* **37 Combat Spells** - Complete spell system across 3 classes (Cleric, Magician, Sage)
* **Status Effects** - Blur, Stoneskin, Haste, damage absorption, AC bonuses
* **Group Combat** - Fight through groups of 1-5 monsters in dungeon encounters
* **Dynamic Difficulty** - Monster strength scales with dungeon level, balanced for winnable fights

### NPC Renaissance
* **50 Classic NPCs** - Original Usurper-style characters with classic names and personalities
* **Enhanced NPC AI**: personality-driven brains, goal system, memory & emotions
* **Continuous World Simulation** - NPCs act every 5 player turns, wander, trade, fight and socialize
* **Relationship System** - NPCs remember interactions and build relationships
* **Diverse Cast**: Fighters, Mages, Thieves, Paladins, Clerics, Sages, Rangers, and Mercenaries

### Systems & Tooling
* **Complete Save/Load** - Comprehensive state preservation including potions, equipment, stats, diseases, and character details
* **Silent Maintenance** - Background world processing without intrusive messages
* **Hall of Recruitment** - Hire and bribe NPCs into your team
* **Dormitory** - Full rest and recovery mechanics
* **Trading Board** - Marketplace trading system
* **Dark Alley** - Shady services and black market
* **Level Master** - Level advancement services
* **Church** - Donations, blessings, healing, marriages, confessions
* **Colorized Menus** - Fully colorized interface throughout all locations
* Robust JSON loaders, comprehensive test suite, and CI/CD builds

### Still Missing (major milestones)
* Mail system, message system, item transfer between players
* Team management features, gossip system completion
* Castle politics (throne challenge, prison management, royal quests)
* Child/marriage system completion
* Tournament system
* Multiplayer/node support for BBS-style gameplay

## Getting Started

### Prerequisites
- [Godot 4.4+ Mono version](https://godotengine.org/download) (tested with 4.4.1)
- [.NET SDK 6.0+](https://dotnet.microsoft.com/download) (works with .NET 9.0)
- Git

### Quick Start
```bash
# Clone the repository
git clone https://github.com/binary-knight/usurper-reloaded.git
cd usurper-remake

# Build the project
dotnet build

# Run the game (after opening in Godot)
godot project.godot

# Or run directly from Godot editor: Press F5
```

### Building Standalone Executables

#### Build for Windows
```bash
dotnet publish usurper-reloaded.csproj -c Release -r win-x64 -o publish/windows \
  -p:PublishSingleFile=true -p:SelfContained=true -p:InvariantGlobalization=true
```

#### Build for Linux
```bash
dotnet publish usurper-reloaded.csproj -c Release -r linux-x64 -o publish/linux \
  -p:PublishSingleFile=true -p:SelfContained=true -p:InvariantGlobalization=true
```

#### Build for macOS
```bash
dotnet publish usurper-reloaded.csproj -c Release -r osx-x64 -o publish/mac \
  -p:PublishSingleFile=true -p:SelfContained=true -p:InvariantGlobalization=true
```

### Automated Builds
The project includes GitHub Actions CI/CD pipeline (`.github/workflows/ci-cd.yml`):
- **Continuous Integration**: Builds and tests on every push to `main` or `develop`
- **Multi-Platform Builds**: Automatically builds Windows, Linux, and macOS executables
- **GPL Compliance**: Includes source code archive and license files with every build
- **Steam Preparation**: Generates Steam depot structure for releases
- **Artifacts**: Download built executables from the Actions tab on GitHub

## Roadmap

### **Early Access Launch Requirements** (Current Focus)

**CRITICAL BLOCKERS - Must Complete Before Early Access:**

#### Core Location Implementations
- [X] **Church** - Currently placeholder, needs healing/blessing mechanics
- [X] **Marketplace** - Currently placeholder, needs trading system
- [X] **Level Master** - Currently placeholder, needs level advancement
- [X] **Dark Alley** - Currently placeholder, needs shady dealings/black market
- [X] **Anchor Road** - Currently placeholder, needs challenge system (still need to implement challenge features when other subsystems are built)
- [X] **Hall of Recruitment** - Currently placeholder, needs NPC hiring
- [X] **Dormitory** - Currently placeholder, needs rest/recovery mechanics
- [X] **Your Home** - Currently placeholder, needs personal dwelling features
- [X] **Outside Prison** - Currently placeholder, needs prison break mechanics

#### Major System Gaps
- [ ] **Mail System** - "not yet implemented" throughout prison, main street
- [ ] **Message System** - Player-to-player communication missing
- [ ] **Item Transfer** - Player-to-player item trading missing
- [ ] **Team Management** - Many "not yet implemented" features in Team Corner
- [X] **Combat Spells** - COMPLETE! 37 spells across 3 classes (Cleric, Magician, Sage) fully integrated
- [ ] **Combat Items** - "Item usage not yet implemented"
- [ ] **Gossip System** - Love Corner gossip features incomplete
- [ ] **Child System** - Child examination and interaction incomplete
- [ ] **News Archive** - News system has placeholder archive functionality

#### Castle/Royal System Gaps
- [ ] **Throne Challenge** - "not yet implemented" combat system
- [ ] **Prison Management** - "not yet implemented" for royal functions
- [ ] **Royal Mail** - "not yet implemented"
- [ ] **Court Magician** - "not yet implemented"
- [ ] **Royal Quests** - "not yet implemented"
- [ ] **Royal Orphanage** - "not yet implemented" management
- [ ] **Tax Policy** - "not yet implemented"
- [ ] **Guard Management** - "not yet implemented"

#### Prison System Gaps  
- [ ] **Rescue Mechanism** - Prison rescue returns false stub
- [ ] **Prison Break Combat** - "not yet implemented"
- [ ] **Prisoner Database** - Currently returns empty placeholder lists
- [ ] **Online Player Tracking** - "not yet implemented"

#### Shop/Economy Issues
- [ ] **Magic Shop Class Restrictions** - TODO: implement when needed
- [ ] **Item Equipping System** - TODO: implement in magic shop
- [ ] **Cursed Item Removal** - TODO: integrate with actual item system
- [ ] **Item Identification** - Placeholder 33% random chance logic

#### Technical Infrastructure
* [X] **NPC AI Loops** – Enhanced brains & behaviour engine now live
* [X] **World Simulation** – `WorldSimulator` ticks every minute, moves NPCs & processes social/combat events
* [ ] **Relationship Persistence** – Save/load friendships, marriages, enmities
* [ ] **Quest Completion** – Resolve quests and save progress across sessions
* [ ] **Tournament System** – Automated combat events with rankings

**ESTIMATED WORK REMAINING: 1-2 months of core development**

#### Already Complete
- [x] Core game systems (combat, locations, NPCs) - *Basic framework*
- [x] Save/load functionality - *Comprehensive system implemented*
- [x] Terminal emulator and UI framework
- [x] Character creation and basic progression
- [x] Basic location navigation
- [x] Fundamental combat engine (needs spell/item integration)
- [x] Bank, Inn, Healer, Temple, Weapon Shop, Magic Shop basics
- [x] Prison system framework (needs completion)

#### Post-Early Access
- [ ] Steam integration and achievements
- [ ] Initial balancing based on player feedback  
- [ ] Bug fixes and stability improvements
- [ ] Audio and enhanced ANSI art

### **Phase 1: Enhanced Single Player** (Post-Launch)
- [ ] **Nemesis System**: NPCs who defeat you become legendary
- [ ] **Seasonal Events**: Weather and holidays affect gameplay
- [ ] **Legendary Artifacts**: Unique items with rich histories
- [ ] **Expanded Dungeon**: Levels 51-100 with new challenges
- [ ] **Prestige Classes**: Post-level 100 advancement paths

### **Phase 2: Quality of Life** 
- [ ] **NPC Journal**: Track your history with every character
- [ ] **Achievement System**: Steam achievements for epic feats
- [ ] **Mod Support**: Allow custom content creation
- [ ] **Multiple Save Slots**: Manage different characters
- [ ] **Difficulty Options**: From "Classic Brutal" to "Modern Casual"

### **Phase 3: Multiplayer** 
- [ ] **Private Servers**: Host games for 5-10 friends
- [ ] **Asynchronous Play**: Classic BBS-style daily turns
- [ ] **Official Servers**: Persistent worlds with 20-50 players
- [ ] **Spectator Mode**: Watch epic battles unfold
- [ ] **Cross-platform Play**: Steam, mobile, and web

### **Phase 4: Content Expansion**
- [ ] **New Locations**: Thieves Guild, Wizard Tower, Harbor District
- [ ] **Sea Travel**: Ships, pirates, and island exploration
- [ ] **Political System**: Elections, coups, and diplomacy
- [ ] **Crafting System**: Create unique items and potions
- [ ] **Pet System**: Companions that fight alongside you

### **Long-term Vision**
- [ ] **Mobile Version**: Full game on iOS/Android
- [ ] **Steam Workshop**: Share characters, stories, and mods
- [ ] **Usurper 2**: Sequel with expanded world and graphics
- [ ] **VR Mode**: Experience the dungeon in virtual reality

## Development

### Project Structure
```
usurper-reloaded/
├── Scripts/           # C# game logic
│   ├── Core/         # Game engine and systems
│   ├── Locations/    # All 23 game locations
│   ├── AI/          # NPC AI systems
│   └── UI/          # Terminal and interface
├── Scenes/           # Godot scene files
├── Data/            # Game data (items, NPCs, etc.)
├── Tests/           # Comprehensive test suite
└── .github/         # CI/CD workflows
```

### Testing
The project includes extensive testing to ensure Pascal compatibility:
```bash
# Build and run tests (if test project is set up)
dotnet build Tests/Tests.csproj
dotnet test Tests/Tests.csproj

# Note: Test suite is comprehensive with 300+ test cases
# covering combat, character progression, save/load, and Pascal formula accuracy
```

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Areas We Need Help
- Balance testing and feedback
- ANSI art creation
- Sound effects and music
- Localization to other languages
- Documentation improvements

## Technical Details

- **Engine**: Godot 4.4+ with C# (.NET 6.0)
- **Lines of Code**: 50,000+ across 126+ C# files
- **Pascal Compatibility**: 100% formula accuracy from original source
- **Test Coverage**: 300+ test cases
- **Platforms**: Windows, Linux, macOS
- **Architecture**: Single-player with persistent turn-based world simulation
- **Save System**: JSON-based with autosave rotation and manual saves

## License & Your Rights

**Usurper Reloaded is FREE SOFTWARE licensed under GPL v2**

### Your Rights
- ✅ **Use** - Run the game for any purpose (personal, commercial, educational)
- ✅ **Study** - Examine and learn from the complete source code
- ✅ **Share** - Distribute copies to anyone, anywhere
- ✅ **Modify** - Change the game and distribute your improvements
- ✅ **Commercial Use** - Even sell your own versions (under GPL v2)

### Source Code Access
- **Complete source code** included with every download
- **GitHub Repository**: https://github.com/binary-knight/usurper-reloaded
- **Build Scripts**: All tools and build processes included
- **No Hidden Code**: Everything needed to build and modify the game

### GPL v2 Compliance
All releases include:
- `LICENSE` - Complete GPL v2 license text
- `GPL_NOTICE.txt` - Your rights and freedoms
- `usurper-reloaded-source.zip` - Complete source code archive
- Full source code available on GitHub

**This is truly FREE software - you own it completely!**

### Legal Details
**Original Game**: © 1993 Jakob Dangarden  
**Pascal Preservation**: Rick Parrish, Daniel Zingaro (GPL License)  
**This Remake**: GPL v2 Licensed - Free for all to use and modify

See `LICENSE` file for complete GPL v2 terms and conditions.

## Acknowledgments

- **Jakob Dangarden**: For creating the original masterpiece
- **Rick Parrish**: For preserving the Pascal source code
- **Daniel Zingaro**: For tremendous help with the Paspal source code
- **BBS Community**: For keeping the spirit alive
- **Contributors**: Everyone who has helped test and improve

---

*"In the realm of Usurper, death is not the end—it's just another beginning. Will you rise to claim the throne, or will your bones join the countless others in the dungeon depths?"*

**Status**: Preparing for Steam Early Access 