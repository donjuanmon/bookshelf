# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Bookshelf is a fork of Readarr, an ebook and audiobook collection manager for Usenet and BitTorrent users. This fork removes analytics telemetry, adds native MyAnonaMouse support, supports self-hosted metadata providers (Goodreads via "softcover" tags, Hardcover via "hardcover" tags), and includes RSS import functionality.

**Key architectural note**: The codebase retains the "NzbDrone" namespace internally for compatibility with the original Readarr project, but the project is branded as "Readarr" in public-facing elements and "Bookshelf" in this fork's documentation.

## Build and Development Commands

### Backend (.NET)
```bash
# Build backend only
./build.sh --backend

# Build for specific runtime and framework
./build.sh --backend -r linux-x64 -f net6.0

# Build with extra platform support (FreeBSD, Linux x86)
./build.sh --backend --enable-extra-platforms

# Build everything (backend + frontend + packages)
./build.sh --all
```

### Frontend (React/TypeScript)
```bash
# Install dependencies
yarn install --frozen-lockfile --network-timeout 120000

# Build frontend for production
yarn build

# Watch mode for development
yarn watch

# Lint JavaScript/TypeScript
yarn lint

# Lint and auto-fix
yarn lint-fix

# Lint CSS (Linux)
yarn stylelint-linux

# Lint CSS (Windows)
yarn stylelint-windows
```

### Testing
```bash
# Run unit tests
./test.sh Linux Unit Test

# Run integration tests
./test.sh Linux Integration Test

# Run automation tests
./test.sh Linux Automation Test

# Run tests with coverage
./test.sh Linux Unit Coverage
```

### Docker
Two Docker image variants are built:
- **softcover**: Uses Goodreads metadata (backward-compatible with existing Readarr databases)
- **hardcover**: Uses Hardcover metadata (higher quality, requires fresh deployment)

Build args control metadata provider:
- `METADATA_URL`: Metadata API endpoint (default: https://api.bookinfo.pro)
- `HARDCOVER`: Set to `true` for Hardcover metadata support

## Architecture

### Core Technology Stack
- **Backend**: .NET 6.0 (C#)
- **Frontend**: React 17 with TypeScript, Redux for state management
- **Database**: SQLite (primary) or PostgreSQL with FluentMigrator for schema management
- **DI Container**: DryIoc
- **API**: ASP.NET Core with custom controller base classes
- **Build**: MSBuild for backend, Webpack for frontend

### Project Structure

#### Backend Projects (src/)
- **NzbDrone.Core** (`Readarr.Core.csproj`): Core business logic, organized by domain:
  - `Books/`: Author, Book, Edition management
  - `Indexers/`: Search providers (Newznab, Torznab, Gazelle, MyAnonaMouse, etc.)
  - `ImportLists/`: List import functionality (Goodreads/Bookshelf/Lists/OwnedBooks/Series, Rss/GoodreadsRss, LazyLibrarian, Readarr)
  - `MetadataSource/`: Metadata providers (Goodreads, BookInfo/Hardcover)
  - `Download/`: Download client integrations
  - `MediaFiles/`: File management and organization
  - `Datastore/`: Database layer with BasicRepository pattern
  - `DecisionEngine/`: Release quality and filtering logic
  - `Parser/`: Book/author name and release parsing

- **Readarr.Api.V1**: REST API controllers following `{Resource}Controller.cs` pattern
- **Readarr.Http**: HTTP infrastructure, middleware, authentication
- **NzbDrone.Host**: Application bootstrapping and hosting (DryIoc container setup)
- **NzbDrone.Common**: Shared utilities (disk I/O, HTTP, environment detection)
- **NzbDrone.SignalR**: Real-time notifications via SignalR

#### Frontend (frontend/src/)
- **App/**: Core application shell, routing, layout
- **Author/**, **Book/**, **Bookshelf/**: Domain-specific UI components
- **Store/**: Redux store configuration and slice definitions
- **Settings/**: Configuration UI for indexers, download clients, metadata, etc.
- **Components/**: Reusable UI components
- **Utilities/**: Frontend utilities and helpers

### Dependency Injection and Service Registration

Services are auto-registered by DryIoc scanning assemblies listed in `Bootstrap.ASSEMBLIES`:
- `Readarr.Host`
- `Readarr.Core`
- `Readarr.SignalR`
- `Readarr.Api.V1`
- `Readarr.Http`

Services follow naming conventions:
- Interfaces: `I{Name}Service`, `I{Name}Repository`, `I{Name}Factory`
- Implementations: `{Name}Service`, `{Name}Repository`, `{Name}Factory`

### Database Layer

The repository pattern is implemented via `BasicRepository<TModel>` in `NzbDrone.Core.Datastore`:
- Each domain entity has a corresponding repository (e.g., `AuthorRepository`, `BookRepository`)
- Repositories inherit from `BasicRepository<TModel>` which provides CRUD operations
- Database migrations are in `NzbDrone.Core.Datastore.Migration/`
- Supports both SQLite (default) and PostgreSQL via connection string configuration

### Metadata Providers

Two metadata sources are supported (controlled via `METADATA_URL` environment variable):
- **Goodreads** (`NzbDrone.Core.MetadataSource.Goodreads`): Lower quality, backward-compatible
- **BookInfo/Hardcover** (`NzbDrone.Core.MetadataSource.BookInfo`): Higher quality, requires fresh deployment

The active provider implements `IProvideBookInfo`, `IProvideAuthorInfo`, and `ISearchForNewBook`.

### Indexers (Search Providers)

Indexer implementations in `NzbDrone.Core.Indexers/`:
- Base classes: `IndexerBase<TSettings>` for all indexers, `HttpIndexerBase<TSettings>` for HTTP-based ones
- Each indexer has:
  - Settings class implementing `IIndexerSettings`
  - Request generator implementing `IIndexerRequestGenerator`
  - Response parser (often RSS-based via `RssParser` or `TorrentRssParser`)
- Native implementations: MyAnonaMouse, Newznab, Torznab, Gazelle, FileList, IPTorrents, Torrentleech, Nyaa

### API Controllers

Controllers in `Readarr.Api.V1/` follow these patterns:
- Inherit from `ProviderControllerBase<TProviderResource, TProvider, TProviderDefinition>` for provider-based resources (indexers, download clients, notifications)
- Standard REST endpoints: GET (list/detail), POST (create), PUT (update), DELETE
- Use `{Resource}Controller` naming (e.g., `AuthorController`, `BookController`)
- Resources are mapped from domain models using AutoMapper-style conventions

## Development Workflow

### Adding a New Indexer

1. Create folder in `src/NzbDrone.Core/Indexers/{IndexerName}/`
2. Implement:
   - `{IndexerName}.cs` inheriting `HttpIndexerBase<{IndexerName}Settings>`
   - `{IndexerName}Settings.cs` implementing `IIndexerSettings`
   - `{IndexerName}RequestGenerator.cs` implementing `IIndexerRequestGenerator`
   - `{IndexerName}Parser.cs` if custom parsing needed (or reuse `RssParser`/`TorrentRssParser`)
3. The indexer will be auto-discovered via DryIoc assembly scanning

### Adding a New API Endpoint

1. Create controller in `src/Readarr.Api.V1/{Domain}/{Resource}Controller.cs`
2. Create resource DTO class `{Resource}Resource.cs`
3. Implement controller inheriting from appropriate base (usually `ProviderControllerBase` or `ReadarrRestController`)
4. Add corresponding service/repository in `NzbDrone.Core/{Domain}/`

### Frontend Development

1. Run backend: `dotnet run --project src/NzbDrone/Readarr.csproj`
2. Start frontend watch mode: `yarn watch`
3. Frontend changes hot-reload, backend requires restart
4. API calls use relative URLs (proxied to backend in dev)

### Database Migrations

1. Create new migration class in `src/NzbDrone.Core/Datastore/Migration/`
2. Inherit from `NzbDrone.Common.Extensions.FluentMigratorExtensions.Migration`
3. Implement `Up()` method with schema changes
4. Migrations run automatically on application startup

## Important Notes

- **No analytics**: This fork has removed all Servarr analytics/telemetry
- **Immediate search on import**: When an import list sync adds authors or books, searches are queued immediately (in `ImportListSyncService.ProcessListItems`) rather than relying on AddOptions being re-read from the DB later. This avoids a timing issue present in upstream Readarr.
- **Metadata switching**: Changing between Goodreads (softcover) and Hardcover requires a fresh database
- **Output directories**:
  - Backend builds to `_output/`
  - Frontend builds to `_output/UI/`
  - Tests build to `_tests/`
- **Platform detection**: Build system auto-detects OS and architecture via `Directory.Build.props`
- **Node version**: Managed via Volta (see package.json), currently Node 20.11.1
- **.NET version**: 6.0 (see `DOTNET_VERSION` in `.github/workflows/build.yml`); upgrade to .NET 8 is planned but not yet complete

## Testing

- Unit tests: Fast, no external dependencies, exclude `Category=IntegrationTest` and `Category=AutomationTest`
- Integration tests: Include database, file system, marked with `Category=IntegrationTest`
- Automation tests: Full end-to-end tests, marked with `Category=AutomationTest`
- Manual tests excluded via `Category!=ManualTest` filter

## Running Locally

The application expects:
- Config directory: `~/.config/Readarr` (Linux/Mac) or `%ProgramData%\Readarr` (Windows)
- Default port: 8787
- Environment variable `METADATA_URL` to override metadata provider (optional)
