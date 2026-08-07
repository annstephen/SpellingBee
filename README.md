# SpellingBee

An ASP.NET Core web API for a spelling practice application. The backend is structured as a set of focused modules — Words, Sessions, and Progress — with a shared library for common types.

## Tech Stack

- **.NET 10** / ASP.NET Core Web API
- **xUnit** for unit testing
- **OpenAPI** (built-in ASP.NET Core support)

## Project Structure

```
SpellingBee.slnx
├── src/
│   ├── SpellingBee.API/          # Web API host
│   ├── SpellingBee.Shared/       # Shared types and utilities
│   └── Modules/
│       ├── SpellingBee.Words/    # Word management
│       ├── SpellingBee.Sessions/ # Practice session management
│       └── SpellingBee.Progress/ # Progress tracking
├── tests/
│   ├── SpellingBee.Words.Tests/
│   ├── SpellingBee.Sessions.Tests/
│   └── SpellingBee.Progress.Tests/
└── frontend/                     # (planned)
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run the API

```bash
dotnet run --project src/SpellingBee.API
```

The API will be available at `https://localhost:5001` (or the port shown in the console). OpenAPI docs are served at `/openapi/v1.json` in Development mode.

### Run Tests

```bash
dotnet test
```

## Configuration

Non-secret defaults live in [`src/SpellingBee.API/appsettings.json`](src/SpellingBee.API/appsettings.json). For local `dotnet run`/debugging, developer overrides (like a real `MerriamWebster:ApiKey`) go in a gitignored `src/SpellingBee.API/appsettings.Development.json`.

The packaged desktop app (`SpellingBee.Desktop`) always runs as `Production`, so it never reads `appsettings.Development.json`. Instead it loads an optional per-machine overlay file at:

```
%LOCALAPPDATA%\SpellingBee\appsettings.Local.json
```

e.g.

```json
{ "MerriamWebster": { "ApiKey": "<your key>" } }
```

This file is never committed and lives outside the install directory, so it survives reinstalls/upgrades — the same folder already used for the SQLite DB and audio cache. The app runs fine without it; dictionary lookups just won't resolve. The installer doesn't need to embed or prompt for secrets — this file is set once per machine.

## Build the Installer

Packages the desktop app into a single `SpellingBeeSetup-<version>.exe` that installs to Program Files, adds a Start Menu shortcut (and an optional Desktop shortcut), and registers an uninstaller in "Apps & Features".

### Prerequisite

- [Inno Setup 6](https://jrsoftware.org/isinfo.php): `winget install JRSoftware.InnoSetup`

### Build

```powershell
.\build-installer.ps1
```

This runs `build-desktop.ps1` to refresh `publish\`, then compiles [`installer\SpellingBee.iss`](installer/SpellingBee.iss) with Inno Setup. The resulting installer lands at `installer\output\SpellingBeeSetup-<version>.exe`. The app version comes from `<Version>` in [`src/SpellingBee.Desktop/SpellingBee.Desktop.csproj`](src/SpellingBee.Desktop/SpellingBee.Desktop.csproj) — bump it there before cutting a release.

The installer checks for the Microsoft Edge WebView2 Runtime on first run and offers to download/install it if missing. Uninstalling removes the install directory but leaves `%LOCALAPPDATA%\SpellingBee\` (SQLite DB, audio cache, config overlay) in place.
