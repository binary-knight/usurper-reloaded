# Usurper Reborn

A faithful remake of the classic 1993 BBS door game "Usurper" with modern enhancements and sophisticated NPC AI.

## Features

- **Authentic Terminal Experience**: Faithful recreation of the original DOS/ANSI interface
- **Advanced NPC AI**: Sophisticated personality-driven NPCs that form gangs, seek revenge, and create emergent stories
- **Classic Gameplay**: All original Usurper mechanics including combat, character progression, and world domination
- **Modern Enhancements**: Save/load, improved UI, and expanded content while maintaining the original feel

## Getting Started

### Prerequisites
- Godot 4.2+ with .NET support
- .NET 6.0 or later

### Installation
1. Clone the repository
2. Open the project in Godot 4.2+
3. Build the project (Build → Build Solution)
4. Run the game

## Game Overview

Usurper is a medieval fantasy door game where players fight their way from peasant to ruler of the kingdom. The unique twist is that NPCs behave like real players - they level up, form alliances, seek revenge, and compete for the same goal: becoming the ultimate ruler.

### Key Systems
- **Character Classes**: Warrior, Thief, Cleric, Mage
- **Combat System**: Turn-based tactical combat with special abilities
- **NPC AI**: Personality-driven NPCs with memory, relationships, and goals
- **World Simulation**: Living world that continues to evolve
- **Gang System**: NPCs form alliances and betray each other
- **Economic System**: Dynamic market with NPC traders

## Project Structure

```
UsurperReborn/
├── Scenes/          # Godot scene files
├── Scripts/         # C# game logic
│   ├── Core/        # Core game systems
│   ├── AI/          # NPC AI system
│   ├── Locations/   # Game locations
│   ├── UI/          # User interface
│   ├── Systems/     # Game systems
│   └── Utils/       # Utilities
├── Data/            # JSON game data
├── Assets/          # Fonts, ASCII art, audio
└── Tests/           # Test suite
```

## Development

### Building
```bash
# Build from Godot editor or command line
dotnet build
```

### Testing
```bash
# Run test suite
cd Tests
./run_tests.ps1
```

## License

This project is a fan remake of the original Usurper game created by Jakob Dangarden. Created for educational and entertainment purposes. 