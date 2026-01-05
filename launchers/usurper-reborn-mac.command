#!/bin/bash
# Usurper Reborn macOS Launcher
# This .command file will open in Terminal.app when double-clicked

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
GAME_EXE="$SCRIPT_DIR/UsurperRemake"

# Make sure the game is executable
chmod +x "$GAME_EXE" 2>/dev/null

# Run the game
"$GAME_EXE"

echo ""
echo "Press Enter to exit..."
read
