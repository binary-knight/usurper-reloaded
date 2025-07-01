# Usurper Reloaded

A faithful recreation of the classic 1993 BBS door game "Usurper" by Jakob Dangarden, enhanced with revolutionary NPC AI systems while maintaining 100% Pascal source compatibility.

## 🎮 About

Usurper Reloaded brings the brutal medieval world of the original BBS classic to modern platforms. Every formula, every stat, every quirk from the original Pascal source has been meticulously preserved, while adding sophisticated AI systems that make the world feel truly alive.

**Original Creator**: Jakob Dangarden (1993)  
**Source Preservation**: Rick Parrish  
**Modern Recreation**: Built with Godot 4.2+ and C#

## ✨ Features

### **Authentic Classic Experience**
- 100% accurate recreation of all original game mechanics
- Complete 50-level dungeon with Pascal-exact combat formulas
- All 23 original locations faithfully recreated
- 325 daily turns with brutal permadeath difficulty
- Authentic 80x25 terminal display with ANSI art support

### **Revolutionary NPC AI System**
- **Personality-Driven Behavior**: NPCs with traits like aggression, greed, loyalty, and vengefulness
- **Dynamic Memory**: NPCs remember interactions and hold grudges
- **Emergent Stories**: Gang wars, betrayals, and alliances form naturally
- **Living World**: NPCs continue their lives even when you're not watching

### **Complete Game Systems**
- **Combat**: 6 different combat modes including magic and ranged
- **Economy**: Banking, shops, haggling, and dynamic pricing
- **Social**: Marriage, children, gangs, and relationship tracking
- **Religion**: 6 deities with unique powers and divine politics
- **Quests**: Dynamic quest generation with royal missions
- **Tournaments**: Multiple competition types with championships

## 🚀 Getting Started

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

## 🗺️ Roadmap

### **Early Access Launch** (Current Focus)
- [x] Core game systems (combat, locations, NPCs)
- [x] Save/load functionality
- [x] Basic Steam integration
- [ ] Initial balancing based on player feedback
- [ ] Bug fixes and stability improvements

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

## 🛠️ Development

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

## 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Areas We Need Help
- Balance testing and feedback
- ANSI art creation
- Sound effects and music
- Localization to other languages
- Documentation improvements

## 📊 Technical Details

- **Engine**: Godot 4.2+ with C#
- **Lines of Code**: 50,000+ across 100+ files
- **Pascal Compatibility**: 100% formula accuracy
- **Test Coverage**: 300+ test cases
- **Platforms**: Windows, Linux, macOS (Steam Deck verified)

## 📜 License

This is a fan remake created for preservation and educational purposes. 

**Original Game**: © 1993 Jakob Dangarden  
**Pascal Preservation**: Rick Parrish (GPL License)  
**This Remake**: Fair use for preservation and education

## 🙏 Acknowledgments

- **Jakob Dangarden**: For creating the original masterpiece
- **Rick Parrish**: For preserving the Pascal source code
- **Daniel Zingaro**: For tremendous help with the Paspal source code
- **BBS Community**: For keeping the spirit alive
- **Contributors**: Everyone who has helped test and improve

---

*"In the realm of Usurper, death is not the end—it's just another beginning. Will you rise to claim the throne, or will your bones join the countless others in the dungeon depths?"*

**Status**: Preparing for Steam Early Access 