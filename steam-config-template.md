# Steam Release Configuration Guide

This guide provides the necessary configuration files and setup instructions for deploying Usurper Remake to Steam.

## Steam SDK Setup

1. Download and install the Steam SDK from Steamworks
2. Create your Steam application page
3. Set up your depots for Windows, Linux, and macOS

## Required Steam IDs

Replace these placeholders in the CI/CD workflow:

- YOUR_STEAM_APP_ID - Your Steam application ID
- YOUR_WINDOWS_DEPOT_ID - Windows depot ID
- YOUR_LINUX_DEPOT_ID - Linux depot ID  
- YOUR_MAC_DEPOT_ID - macOS depot ID

## System Requirements

**Minimum:**
- OS: Windows 10, Ubuntu 18.04, macOS 10.15
- Processor: 2.0 GHz dual-core
- Memory: 2 GB RAM
- Graphics: DirectX 9 compatible
- Storage: 500 MB available space

**Recommended:**
- OS: Windows 11, Ubuntu 22.04, macOS 12.0
- Processor: 3.0 GHz quad-core
- Memory: 4 GB RAM
- Graphics: DirectX 11 compatible
- Storage: 1 GB available space

## Game Description

**Usurper Remake** - A complete recreation of the classic 1993 BBS door game with modern enhancements and revolutionary NPC AI systems.

### Key Features
- 100% Pascal Source Compatibility - Perfect recreation of original mechanics
- Enhanced NPC AI - Intelligent characters with memory and relationships
- Complete Medieval World - 21 fully implemented game systems
- Multi-platform Support - Windows, Linux, and macOS builds
- Comprehensive Testing - Over 300 test cases ensuring quality

### Steam Tags
RPG, Turn-Based Combat, Medieval, Retro, Text-Based, Multiplayer, Classic, Indie, Single-player, Strategy

### Deployment Process

1. Run GitHub Actions CI/CD pipeline
2. Download steam-build artifacts  
3. Configure VDF files with Steam IDs
4. Upload using SteamCMD
5. Test on Steam preview branch
6. Release to public

## Marketing Copy

Experience the complete medieval world of Usurper Remake - a faithful recreation of the 1993 BBS classic with enhanced AI and modern features.

Build your character from peasant to ruler in a living medieval world where every NPC has memory, relationships, and complex motivations. With 21 fully implemented game systems, Usurper Remake offers hundreds of hours of emergent gameplay.

Ready to seize the throne? The path of the Usurper awaits!
