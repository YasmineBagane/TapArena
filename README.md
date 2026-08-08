# TapArena

A one-touch, skill-based minigame arcade.

## Status

🚧 **Early development — solo project.** Currently 2 of the 6 planned
games are playable prototypes, built against a shared UI Toolkit Core module
(hub navigation, `IMinigame`/`RunResult` contract) so each new game plugs in
without a rebuild.

## Current Games

### 🐍 Snake Solo
Classic growing snake on a bounded grid. The challenge is entirely your own
spatial planning as the arena gets faster and more crowded — one wrong turn
ends the run.

### 🧠 Memory Match
A 4×3 grid of colored cards briefly reveals itself, then flips face-down.
Tap two cards to flip them; matching pairs clear (their slot stays empty,
the grid never reshuffles), mismatches flip back. Clear the board before
the clock runs out.

*4 more titles (Perfect Stop, Precision Slingshot, Stack It, Rhythm
Runner) are planned.*

## Tech Stack

- **Engine:** Unity 6, C#
- **UI:** UI Toolkit (UXML/USS)
- **Target platforms (planned):** Android + iOS
- **Backend (planned):** Unity Gaming Services — Leaderboards, Cloud Save,
  Authentication, Cloud Code

## Project Structure

```
Assets/
  Games/
    _Shared/
      Core/       # IMinigame contract, RunResult
      Hub/        # Central menu — one scene per game, tap a tile to load it
    SnakeSolo/
    MemoryMatch/
```

Each game lives in its own scene and only loads when selected from the Hub,
so only one game's objects are ever active at a time.

## Getting Started

1. Clone the repo
2. Open the project in Unity 6 (LTS or newer)
3. Open `Assets/Games/_Shared/Hub/Scenes/Hub.unity`
4. Press Play — tap a tile to launch a game
5. To return to Hub, press Esc

## Development Notes

This is my first Unity/C# project — I'm learning the engine by building it,
with AI assistance (Claude) along the way for code, architecture, and
debugging help.
