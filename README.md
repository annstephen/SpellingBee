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

This file is never committed and lives outside the install directory, so it survives reinstalls/upgrades — the same folder already used for the SQLite DB and audio cache. The app runs fine without it; dictionary lookups just won't resolve. A future installer doesn't need to embed or prompt for secrets — this file is set once per machine.
