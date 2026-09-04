# PuzzleParty — Setup Guide

Quick reference for getting the project running locally. For deeper architecture notes, see [CLAUDE.md](CLAUDE.md).

The project has two halves that run independently:
- **Client** — the Unity game (`Client/`)
- **Server** — an ASP.NET Core API backed by MongoDB, used for user progression sync and event tracking (`Server/`)

You can open and play the client without the server running — it just won't sync progression or send analytics events. You only need the server up if you're testing that sync/tracking behavior.

## Versions

| | Version |
|---|---|
| Unity | **6000.4.0f1** (see `Client/ProjectSettings/ProjectVersion.txt`) |
| .NET SDK | **8.0** (see `Server/webapi.csproj`) |
| Database | MongoDB (Atlas-hosted; no local install needed — see below) |

If you have Unity Hub installed, opening the project will offer to install the matching editor version automatically.

## Client (Unity)

1. Open **Unity Hub** → **Add** → select the `Client/` folder.
2. If prompted, let Hub install `6000.4.0f1`.
3. Open the project. Unity will import assets on first open — this takes a few minutes.

**Scene boot order** (as configured in Build Settings): `SplashScene` → `LoadingScene` → `MainMenuScene` → `GameScene`. To just iterate on gameplay, you can open `GameScene` directly and press Play — the Level Editor's "Test Level" (below) is the fastest way to jump straight into a specific level without going through the menu flow.

Two additional scenes exist for ad-hoc testing but aren't part of the build: `GameSceneTest.unity`, `GameSceneTestGUI.unity`.

Key third-party dependencies (already in the project, nothing to install separately): DOTween, TextMesh Pro, Unity Visual Scripting.

### Level & map editor

**Tools → PuzzleParty → Level Editor** (menu bar), backed by `Client/Assets/Editor/LevelEditorWindow.cs`.

What it does:
- Edit a level's fields, and its locked-tile / ice-row grid, visually
- Reorder levels (swaps position in sequence)
- Create a new level from a source image
- **Test Level** — launches Play mode straight into the selected level, bypassing normal progression (editor-only; has no effect in builds)
- A maps panel for renaming/resizing maps and keeping them a contiguous chain

Level data lives on disk at `Client/Assets/StreamingAssets/levels/levelX/` (a `levelX.json` config + `levelX.png` image per level); map groupings live in `Client/Assets/StreamingAssets/config/maps.json`. The editor reads/writes these directly, so changes show up immediately — no rebuild needed.

### In-game debug panel

At runtime, tap the **top-left corner of the main menu 3 times** to open a hidden debug panel. Lets you adjust last-beaten-level, streak, and coins directly, without touching the save file by hand.

## Server (ASP.NET Core)

```bash
cd Server
dotnet run
```

Runs at `http://localhost:5136` by default (matches the client's hardcoded `BaseUrl` in `Client/Assets/Scripts/Service/BackendSyncService.cs` — that's a `// TODO: change to your production URL before release` for whenever this ships). Swagger UI is available at `/swagger` in development.

MongoDB is already configured for local development in `Server/appsettings.Development.json` — it points at an Atlas-hosted cluster, so there's no local database to install or run. If you need your own database instance, add a `MongoDB` section (`ConnectionString`, `DatabaseName`, `UsersCollection`, `EventsCollection`) to `appsettings.Development.json`, or override via environment variables.

```bash
# From the Server directory
dotnet build   # build only
dotnet test    # run tests, if/when present
```

## Analytics

Level/event tracking data lands in the `Events` MongoDB collection (see `Server/Analytics/`, which has ready-to-paste aggregation pipelines for D1 retention and level-difficulty dashboards — built for MongoDB Atlas Charts, but they're plain MongoDB aggregation pipelines and will run anywhere).

## Releasing

There's a project also called `Web/` at the repo root — a static marketing site (plain HTML/CSS, served via nginx in Docker), unrelated to the game client. Both it and the server ship as Docker images.

> `Web/` is planned to move out into its own repo eventually — the instructions below reflect where things stand today, and will need revisiting once that split happens (particularly `build.sh`/`deploy.sh`'s `web` target).

### Server & Web (Docker)

Two scripts at the repo root, both meant to be run from your machine (not CI):

```bash
./build.sh <version> [server|web|all]     # e.g. ./build.sh 1.0.3
./deploy.sh <env> <version> [server|web|all]   # e.g. ./deploy.sh stage 1.0.3
```

**`build.sh`**:
- Refuses to run if you have uncommitted changes to tracked files, or if the git tag `v<version>` already exists — it's meant to build from a clean, tagged commit.
- Builds each target as a multi-arch-safe `linux/amd64` image (`docker buildx build --platform linux/amd64`) — needed even from Apple Silicon, since the VPS is amd64.
- Pushes to Docker Hub as `bootsybrackers/puzzleparty-server` / `bootsybrackers/puzzleparty-web`, tagged both `v<version>` and `latest`.
- Tags the current git commit `v<version>` and pushes the tag.
- Requires you're logged into Docker Hub (`docker login`) with push access to those repos.

**`deploy.sh`**:
- SSHes into the VPS (`administrator@slamdunkinteractive.com`) and does a `docker pull` + stop/remove old container + `docker run` of the new tagged image.
- For a server deploy, you must have `MONGODB_CONNECTION_STRING` exported in your shell first — it's injected into the container at `docker run` time (`-e MongoDB__ConnectionString=...`), never baked into the image or committed anywhere. `appsettings.Development.json`'s connection string is local-dev-only and unrelated to this.
- `prod` deploys prompt for an explicit `yes` confirmation before doing anything; `stage` does not.

| | stage | prod |
|---|---|---|
| Server container | `puzzleparty-server-stage` : `8082` | `puzzleparty-server-prod` : `8081` |
| Web container | `puzzleparty-web-stage` : `8084` | `puzzleparty-web-prod` : `8083` |
| Database | `PuzzleParty-Stage` | `PuzzleParty` |
| Server URL | `stage.pp.slamdunkinteractive.com` | `pp.slamdunkinteractive.com` |
| Web URL | `stage.www.slamdunkinteractive.com` | `www.slamdunkinteractive.com` |

After deploying, `GET /api/health` (`Server/Controllers/HealthController.cs`) returns `{ status, version, serverTime }` — `version` comes straight from the `APP_VERSION` env var `deploy.sh` sets, so it's a quick way to confirm the right build actually landed.

**Dockerfiles**, if you need to build/run either locally without the scripts:
- `Server/Dockerfile` — multi-stage: .NET 8 SDK image builds + publishes, then copies into the smaller `aspnet:8.0` runtime image. Listens on `8081` internally (`ASPNETCORE_URLS`), regardless of which host port you map it to.
- `Web/Dockerfile` — just `nginx:alpine` serving the `Web/` directory's static files as-is; no build step.

### Client (Unity)

There's no scripted build/release pipeline for the client in this repo — it's a manual Unity build:

1. **File → Build Settings** in the Unity Editor, targeting iOS (bundle ID `com.Slamdunk-Interactive.PuzzleParty`, per `ProjectSettings.asset`).
2. Unity generates an Xcode project into `Client/ios/` (gitignored — never committed, regenerated fresh each build).
3. Open that Xcode project, bump the build number if needed (currently `1` in Unity's Player Settings), archive, and upload via Xcode/Transporter as usual for App Store/TestFlight.

Before shipping a build that talks to the real server, double-check `BackendSyncService.BaseUrl` (`Client/Assets/Scripts/Service/BackendSyncService.cs`) — it's still hardcoded to `http://localhost:5136` with a `// TODO: change to your production URL before release` comment, and would need to point at `https://pp.slamdunkinteractive.com` (or stage) instead.
