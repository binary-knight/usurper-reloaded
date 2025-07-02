# Usurper Reloaded

** FREE AND OPEN SOURCE SOFTWARE - GPL v2 Licensed**

A faithful recreation of the classic 1993 BBS door game "Usurper" by Jakob Dangarden, enhanced with revolutionary NPC AI systems while maintaining 100% Pascal source compatibility.

## About

Usurper Reloaded brings the brutal medieval world of the original BBS classic to modern platforms. Every formula, every stat, every quirk from the original Pascal source has been meticulously preserved, while adding sophisticated AI systems that make the world feel truly alive.

**Original Creator**: Jakob Dangarden (1993)  
**Source Preservation**: Rick Parrish  
**Modern Recreation**: Built with Godot 4.4+ and C#  
**License**: GNU General Public License v2 (GPL v2)

## Current Status & Implemented Features (Alpha 0.2 – Summer 2025)

Progress has moved from "bare foundations" to a **play-testable alpha**.  Highlights:

### Core Engine
* Godot 4.2 Mono build that launches a DOS-style 80×25 terminal.
* Full data layer – `Character`, `Player`, `NPC`, `Item`, `Monster`, `Quest` and a 1 100-line `GameConfig` mirroring Pascal globals.

### Playable Locations (16 / 23 complete)
* **Main Street, Inn, Bank, Magic Shop, Healer, Temple, Dungeon**
* NEW: **Church, Dark Alley, Level Master, Marketplace, Anchor Road hub, Hall of Recruitment, Dormitory** – all feature-complete ports of the original Pascal logic.

### NPC Renaissance
* 30+ NPCs loaded from `Data/npcs.json` (with stats, dialogue, starting locations).
* **Enhanced NPC AI**: personality-driven brains, goal system, memory & emotions.
* **WorldSimulator** now runs every 60 s in a background task → NPCs wander, trade, fight and socialise.  Enter a room and you'll meet living townsfolk.

### Systems & Tooling
* **Hall of Recruitment** supports hiring / bribing NPCs into your team.
* **Dormitory** provides full rest & daily reset.
* Trading board in **Marketplace**, shady services in **Dark Alley**, levelling with the **Level Master**, donations & blessings in **Church** – all with flavour text and original costs.
* Robust JSON loaders, test-suite (>300 cases) and CI builds.

### Still Missing (major milestones below)
* Deep dungeon crawl beyond basic monster fights, castle politics, child/marriage systems, etc.
* Spell casting, cursed item mechanics, full save persistence for new systems.
* Multiplayer / node support.

## Getting Started

### Prerequisites
- [Godot 4.2+ Mono version](https://godotengine.org/download)
- [.NET SDK 6.0+](https://dotnet.microsoft.com/download)
- Git

### Quick Start
```bash
# Clone the repository
git clone https://github.com/binary-knight/usurper-reloaded.git
cd usurper-reloaded

# Build the project
dotnet build

# Run tests
cd Tests && powershell ./run_tests.ps1

# Open in Godot
godot project.godot
```

### Building for Release
The project includes automated GitHub Actions for building:
- Push to `main` for development builds
- Tag with `v*` for release builds
- Artifacts available in Actions tab

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
- [ ] **Your Home** - Currently placeholder, needs personal dwelling features
- [ ] **Outside Prison** - Currently placeholder, needs prison break mechanics

#### Major System Gaps
- [ ] **Mail System** - "not yet implemented" throughout prison, main street
- [ ] **Message System** - Player-to-player communication missing
- [ ] **Item Transfer** - Player-to-player item trading missing
- [ ] **Team Management** - Many "not yet implemented" features in Team Corner
- [ ] **Combat Spells** - "Spell casting not yet implemented"
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
# Run all tests
cd Tests && powershell ./run_tests.ps1

# Run specific test suite
dotnet test Tests/CombatTests.cs
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

- **Engine**: Godot 4.2+ with C#
- **Lines of Code**: 50,000+ across 100+ files
- **Pascal Compatibility**: 100% formula accuracy
- **Test Coverage**: 300+ test cases
- **Platforms**: Windows, Linux, macOS (Steam Deck verified)

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
This distribution includes:
- `LICENSE` - Complete GPL v2 license text
- `GPL_NOTICE.txt` - Your rights and freedoms
- `usurper-reloaded-source.zip` - Complete source code archive
- `GPL_COMPLIANCE.md` - Detailed compliance information

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