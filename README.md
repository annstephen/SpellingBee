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
