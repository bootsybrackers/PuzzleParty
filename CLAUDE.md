# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PuzzleParty is a tile-based puzzle game with a Unity client and an ASP.NET Core backend server.

## Technology Stack

### Client (Unity)
- **Unity Version**: 6000.1.1f1
- **Language**: C# (.NET)
- **Key Dependencies**:
  - DOTween (animation library)
  - TextMesh Pro (UI text rendering)
  - Unity Visual Scripting

### Server (ASP.NET Core)
- **Framework**: .NET 8.0
- **Database**: MongoDB (configured via `MongoDbSettings` in appsettings.json)
- **Key Dependencies**:
  - Microsoft.EntityFrameworkCore (v9.0.0)
  - Microsoft.EntityFrameworkCore.Sqlite (v9.0.0)
  - MongoDB.Driver (v3.1.0)
  - Swashbuckle.AspNetCore (v7.2.0)

## Development Commands

### Unity Client

The Unity project is located in the `Client/` directory.

**Opening the Project:**
- Open Unity Hub and add the `Client/` directory as a Unity project
- Alternatively, open `Client/PuzzleParty.sln` in Unity Editor

**Main Scenes:**
- `Client/Assets/Scenes/GameScene.unity` - Primary game scene
- `Client/Assets/Scenes/GameSceneTest.unity` - Test scene
- `Client/Assets/Scenes/GameSceneTestGUI.unity` - GUI test scene

### Server API

The server is located in the `Server/` directory.

**Build the server:**
```bash
cd Server
dotnet build
```

**Run the server:**
```bash
cd Server
dotnet run
```

**Access Swagger UI (development only):**
- The server runs with Swagger enabled in development mode
- Navigate to `/swagger` endpoint after starting the server (typically https://localhost:5001/swagger or http://localhost:5000/swagger)

**Run tests (if present):**
```bash
cd Server
dotnet test
```

## Architecture

### Unity Client Architecture

The client follows a layered architecture with service-based dependency injection:

**Service Layer Pattern:**
- Uses a custom `ServiceLocator` pattern for dependency injection (Client/Assets/Scripts/Service/ServiceLocator.cs:6)
- Services are registered in `ServiceLocator.Configure()` (Client/Assets/Scripts/Service/ServiceLocator.cs:39)
- Current services: `ProgressionService`, `LevelService`

**Key Components:**

1. **Board Layer** (`Client/Assets/Scripts/Board/`)
   - `BoardController` - Main controller that initializes level and sets up board view
   - `BoardManager` - Manages game state and tile movements (also exists in root Scripts for legacy compatibility)
   - `BoardView` - Handles visual representation of the board
   - `Tile` - Individual tile behavior with drag-and-drop mechanics

2. **Level Layer** (`Client/Assets/Scripts/Level/`)
   - `LevelService` - Loads levels from StreamingAssets
   - `Level` - Data model for level information
   - `LevelConf` - Configuration schema (rows, columns, moves, holes)

3. **Progression Layer** (`Client/Assets/Scripts/Progression/`)
   - `ProgressionService` - Manages player progression via local JSON file
   - `Progression` - Stores lastBeatenLevel and coins

**Level Data Structure:**
- Levels are stored in `Client/Assets/StreamingAssets/levels/levelX/`
- Each level contains:
  - `levelX.json` - Configuration (name, rows, columns, moves, holes)
  - `levelX.png` - The image to be split into puzzle tiles

**Tile Movement:**
- Tiles use drag-and-drop input with threshold detection (Client/Assets/Scripts/Tile.cs:28)
- Movement is validated by `BoardManager.MoveTile()` (Client/Assets/Scripts/BoardManager.cs:249)
- Animations handled via DOTween (Client/Assets/Scripts/Tile.cs:109)

### Server Architecture

The server follows clean architecture principles with clear separation of concerns:

**Layers:**
1. **Controllers** (`Server/Controllers/`) - API endpoints (e.g., `UsersController`)
2. **Services** (`Server/Services/`) - Business logic layer
3. **Repositories** (`Server/Repositories/`) - Data access layer
4. **Models** (`Server/Models/`) - Data entities
5. **Configurations** (`Server/Configurations/`) - Settings classes (e.g., `MongoDbSettings`)

**Dependency Injection:**
- Configured in `Server/Program.cs`
- Services and repositories registered with ASP.NET Core DI container
- Scoped lifetime for services and repositories
- Singleton for MongoDB client

**Current Implementation Note:**
- MongoDB is configured but `UserRepository` currently uses an in-memory list instead of actual MongoDB (Server/Repositories/UserRepository.cs:10)
- To switch to MongoDB, implement MongoDB operations in the repository

**API Structure:**
- RESTful API with standard CRUD operations
- Controllers use async/await pattern
- Routes follow convention: `api/[controller]`

## Important Notes

### Unity Client
- The codebase has both legacy scripts (in root `Scripts/`) and newer architecture (in `Scripts/Board/`, `Scripts/Level/`, etc.)
- When adding new features, prefer the newer organized structure under subdirectories
- Level progression is saved locally via `ProgressionService` to `Application.persistentDataPath/progression.json`

### Server
- Logging is configured for Console and Debug output (Server/Program.cs:28-30)
- HTTPS redirection is enabled
- MongoDB connection string should be set in `appsettings.json` or `appsettings.Development.json`

### Adding New Levels
To add a new level to the game:
1. Create a new directory: `Client/Assets/StreamingAssets/levels/levelX/`
2. Add `levelX.png` (the puzzle image)
3. Add `levelX.json` with structure: `{"name": "...", "rows": "N", "columns": "N", "moves": "N", "holes": "N"}`
4. The `LevelService` will automatically load it based on player progression
