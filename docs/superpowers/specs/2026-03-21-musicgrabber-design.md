# MusicGrabber — Design Specification

**Date:** 2026-03-21
**Status:** Draft
**PRD:** [docs/PRD.md](../../PRD.md)
**Conventions:** [freaxnx01/dotnet-ai-instructions](https://github.com/freaxnx01/dotnet-ai-instructions)

---

## 1. Overview

MusicGrabber is a self-hosted web application for personal music collection management. Whitelisted users search, discover, and download audio from YouTube, with metadata enrichment from MusicBrainz and live radio integration from Swiss SRG SSR stations.

This spec describes a green-field redesign following Modular Monolith + Hexagonal architecture, replacing the original vibe-coded flat structure.

---

## 2. Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Architecture | Modular Monolith + Hexagonal per module | Clean boundaries, testable in isolation, single deployment |
| Deployment | Hybrid — Blazor embedded in Host, API also exposed | Single container, DI for internal calls, API for Bruno/future clients |
| Frontend | Blazor Interactive Server + MudBlazor | Server-side rendering with interactivity, DI-compatible, no WASM |
| Auth pages | Static SSR | No interactivity needed for login/error |
| Database | SQLite now, designed for PostgreSQL swap | Simple ops for self-hosted, EF Core abstracts the difference |
| Real-time | SignalR `DownloadHub` | Natural fit with Blazor Server, push-based progress updates |
| Background jobs | Hangfire (SQLite storage) | Dashboard, retry policies, persistence, continuation jobs |
| Concurrency | Global max 9 / per-user max 3 | Supports 3 users at full concurrency |
| Auth | ASP.NET Core Identity + Google OAuth external provider | Local user store, extensible to other providers |
| Roles | User + Admin | Simple, sufficient for household use |
| UI library | MudBlazor only | Per conventions — no raw HTML, no other component libraries |

---

## 3. Modules

Five modules, each self-contained with Domain / Application / Infrastructure layers:

| Module | Responsibility |
|--------|---------------|
| **Download** | YouTube search, download jobs, file management, yt-dlp/ffmpeg integration |
| **Discovery** | MusicBrainz search (artists, tracks, releases) |
| **Radio** | SRG SSR radio station now-playing and playlist |
| **Quota** | Per-user storage quotas, threshold enforcement, email notifications |
| **Identity** | ASP.NET Core Identity, Google OAuth, whitelist, user profiles/settings |

---

## 4. Project Structure

```
MusicGrabber.sln
├── src/
│   ├── Host/
│   │   ├── Program.cs
│   │   ├── Endpoints/
│   │   │   ├── DownloadEndpoints.cs
│   │   │   ├── DiscoveryEndpoints.cs
│   │   │   ├── RadioEndpoints.cs
│   │   │   ├── QuotaEndpoints.cs
│   │   │   ├── IdentityEndpoints.cs
│   │   │   └── AdminEndpoints.cs
│   │   └── Hubs/
│   │       └── DownloadHub.cs
│   │
│   ├── Shared/
│   │   ├── Contracts/
│   │   │   ├── IDownloadFacade.cs
│   │   │   ├── IQuotaFacade.cs
│   │   │   └── IUserFacade.cs
│   │   ├── DTOs/
│   │   │   ├── StartDownloadRequest.cs
│   │   │   ├── FileInfoDto.cs
│   │   │   ├── QuotaInfoDto.cs
│   │   │   ├── YouTubeSearchResultDto.cs
│   │   │   ├── RadioDownloadRequest.cs
│   │   │   ├── UserProfileDto.cs
│   │   │   ├── UserStatsDto.cs
│   │   │   └── GlobalStatsDto.cs
│   │   ├── IEventBus.cs
│   │   └── Events/
│   │       ├── DownloadCompletedEvent.cs
│   │       ├── FileDeletedEvent.cs
│   │       ├── UserWhitelistedEvent.cs
│   │       └── QuotaThresholdCrossedEvent.cs
│   │
│   ├── Modules/
│   │   ├── Download/
│   │   │   ├── Domain/
│   │   │   │   ├── DownloadJob.cs
│   │   │   │   ├── DownloadStatus.cs
│   │   │   │   └── AudioFormat.cs
│   │   │   ├── Application/
│   │   │   │   ├── Ports/
│   │   │   │   │   ├── Driving/IDownloadService.cs  ← module-internal driving port
│   │   │   │   │   │                                   (Shared/IDownloadFacade delegates to this)
│   │   │   │   │   └── Driven/
│   │   │   │   │       ├── IDownloadJobRepository.cs
│   │   │   │   │       ├── IAudioExtractor.cs
│   │   │   │   │       ├── IAudioNormalizer.cs
│   │   │   │   │       └── IYouTubeSearchService.cs
│   │   │   │   └── UseCases/
│   │   │   │       ├── SearchYouTube/
│   │   │   │       ├── StartDownload/
│   │   │   │       ├── StartPlaylistDownload/
│   │   │   │       ├── GetJobStatus/
│   │   │   │       ├── ListUserFiles/
│   │   │   │       ├── DeleteFile/
│   │   │   │       └── GetDownloadStats/
│   │   │   └── Infrastructure/
│   │   │       ├── Adapters/
│   │   │       │   ├── Persistence/
│   │   │       │   │   ├── DownloadDbContext.cs
│   │   │       │   │   ├── DownloadJobConfiguration.cs
│   │   │       │   │   └── Migrations/
│   │   │       │   ├── YtDlpExtractor.cs
│   │   │       │   ├── FfmpegNormalizer.cs
│   │   │       │   └── YouTubeDataApiService.cs
│   │   │       └── DependencyInjection.cs
│   │   │
│   │   ├── Discovery/
│   │   │   ├── Domain/
│   │   │   │   └── SearchResult.cs
│   │   │   ├── Application/
│   │   │   │   ├── Ports/
│   │   │   │   │   ├── Driving/IMusicBrainzService.cs
│   │   │   │   │   └── Driven/IMusicBrainzApi.cs
│   │   │   │   └── UseCases/
│   │   │   │       ├── SearchArtists/
│   │   │   │       ├── SearchTracks/
│   │   │   │       └── SearchReleases/
│   │   │   └── Infrastructure/
│   │   │       ├── Adapters/MusicBrainzApiClient.cs
│   │   │       └── DependencyInjection.cs
│   │   │
│   │   ├── Radio/
│   │   │   ├── Domain/
│   │   │   │   ├── RadioStation.cs
│   │   │   │   └── RadioSong.cs
│   │   │   ├── Application/
│   │   │   │   ├── Ports/
│   │   │   │   │   ├── Driving/IRadioService.cs
│   │   │   │   │   └── Driven/ISrgSsrApi.cs
│   │   │   │   └── UseCases/
│   │   │   │       ├── GetStations/
│   │   │   │       ├── GetNowPlaying/
│   │   │   │       └── GetPlaylist/
│   │   │   └── Infrastructure/
│   │   │       ├── Adapters/SrgSsrApiClient.cs
│   │   │       └── DependencyInjection.cs
│   │   │
│   │   ├── Quota/
│   │   │   ├── Domain/
│   │   │   │   ├── UserQuota.cs
│   │   │   │   └── QuotaThreshold.cs
│   │   │   ├── Application/
│   │   │   │   ├── Ports/
│   │   │   │   │   ├── Driving/IQuotaService.cs
│   │   │   │   │   └── Driven/
│   │   │   │   │       ├── IQuotaRepository.cs
│   │   │   │   │       └── IEmailService.cs
│   │   │   │   └── UseCases/
│   │   │   │       ├── CheckQuota/
│   │   │   │       ├── UpdateUsage/
│   │   │   │       └── SendThresholdNotification/
│   │   │   └── Infrastructure/
│   │   │       ├── Adapters/
│   │   │       │   ├── Persistence/
│   │   │       │   │   ├── QuotaDbContext.cs
│   │   │       │   │   └── Migrations/
│   │   │       │   └── SmtpEmailAdapter.cs
│   │   │       └── DependencyInjection.cs
│   │   │
│   │   └── Identity/
│   │       ├── Domain/
│   │       │   ├── ApplicationUser.cs
│   │       │   ├── WhitelistEntry.cs
│   │       │   └── UserSettings.cs
│   │       ├── Application/
│   │       │   ├── Ports/
│   │       │   │   ├── Driving/
│   │       │   │   │   ├── IWhitelistService.cs
│   │       │   │   │   └── IUserSettingsService.cs
│   │       │   │   └── Driven/
│   │       │   │       ├── IWhitelistRepository.cs
│   │       │   │       └── IUserSettingsRepository.cs
│   │       │   └── UseCases/
│   │       │       ├── ManageWhitelist/
│   │       │       ├── GetUserProfile/
│   │       │       └── UpdateSettings/
│   │       └── Infrastructure/
│   │           ├── Adapters/
│   │           │   ├── WhitelistClaimsTransformation.cs
│   │           │   └── Persistence/
│   │           │       ├── IdentityDbContext.cs
│   │           │       └── Migrations/
│   │           └── DependencyInjection.cs
│   │
│   └── Frontend/
│       ├── Layout/
│       │   ├── MainLayout.razor(.cs)
│       │   └── NavMenu.razor(.cs)
│       ├── Pages/
│       │   ├── Home.razor(.cs)
│       │   ├── PlaylistDownload.razor(.cs)
│       │   ├── MusicBrainzSearch.razor(.cs)
│       │   ├── Radio.razor(.cs)
│       │   ├── Profile.razor(.cs)
│       │   ├── Auth/
│       │   │   ├── Login.razor
│       │   │   ├── Logout.razor
│       │   │   └── AccessDenied.razor
│       │   └── Admin/
│       │       ├── Whitelist.razor(.cs)
│       │       └── Statistics.razor(.cs)
│       ├── Components/
│       │   ├── YouTubeResultsList.razor(.cs)
│       │   ├── DownloadProgressCard.razor(.cs)
│       │   ├── QuotaIndicator.razor(.cs)
│       │   └── StationSelector.razor(.cs)
│       └── Services/
│
├── tests/
│   ├── Download.UnitTests/
│   ├── Download.IntegrationTests/
│   ├── Discovery.UnitTests/
│   ├── Radio.UnitTests/
│   ├── Quota.UnitTests/
│   ├── Identity.UnitTests/
│   ├── Frontend.ComponentTests/
│   └── E2E/
│
├── bruno/
│   ├── bruno.json
│   ├── environments/
│   ├── download/
│   ├── discovery/
│   ├── radio/
│   ├── quota/
│   └── identity/
│
├── docs/
│   ├── PRD.md
│   ├── design/
│   │   ├── layout/wireframe.md
│   │   ├── home/wireframe.md
│   │   ├── playlist/wireframe.md
│   │   ├── musicbrainz/wireframe.md
│   │   ├── radio/wireframe.md
│   │   ├── profile/wireframe.md
│   │   ├── admin-whitelist/wireframe.md
│   │   ├── admin-statistics/wireframe.md
│   │   └── login/wireframe.md
│   ├── adr/
│   └── ai-notes/
│
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── docker-compose.yml
├── docker-compose.override.yml
├── CLAUDE.md
└── CHANGELOG.md
```

---

## 5. Cross-Module Communication

### Dependency Map

- **Download → Quota:** Synchronous check via `IQuotaFacade.CheckAsync()` before starting a download
- **Radio → Download:** Synchronous handoff via `IDownloadFacade.StartAsync()` for radio song extraction
- **Discovery → (none):** Stateless, no module dependencies. User navigates to Home page for YouTube download.

### Domain Events (In-Process)

| Event | Publisher | Subscriber | Action |
|-------|-----------|------------|--------|
| `DownloadCompletedEvent` | Download | Quota | Recalculate usage, check thresholds |
| `FileDeletedEvent` | Download | Quota | Recalculate usage, potentially unblock |
| `UserWhitelistedEvent` | Identity | Quota | Initialize user quota record |
| `QuotaThresholdCrossedEvent` | Quota | Quota (internal) | Enqueue `SendQuotaEmailJob` via Hangfire |

Implemented via a lightweight `IEventBus` in `Shared/`, registered in `Host/Program.cs`.

### Shared Contracts vs Module Ports

`Shared/Contracts/` contains **facade interfaces** (`IDownloadFacade`, `IQuotaFacade`, `IUserFacade`) for cross-module communication. Each module has its own **internal driving port** (e.g., `IDownloadService`) with the full API surface. The facade delegates to the internal port but exposes only the subset needed by other modules. This prevents modules from depending on each other's full API.

---

## 6. SignalR Real-Time Updates

Single `DownloadHub` in `Host/Hubs/`. Clients join a user-specific group on connect.

### Client Methods

| Method | Payload | Trigger |
|--------|---------|---------|
| `ReceiveProgress` | `jobId`, `progress`, `status` | yt-dlp reports extraction progress |
| `ReceiveCompleted` | `jobId`, `FileInfoDto` | File stored, job finalized |
| `ReceiveFailed` | `jobId`, `error` | Max retries exhausted |
| `ReceiveQuotaUpdate` | `QuotaInfoDto` | After download complete or file delete |

Radio page does **not** use SignalR — uses 30-second client-side `Timer` polling the Radio service.

---

## 7. Hangfire Background Jobs

### Job Types

| Job | Type | Retry | Description |
|-----|------|-------|-------------|
| `ExtractAudioJob` | Fire-and-forget | 3x exponential backoff | yt-dlp extraction, SignalR progress |
| `NormalizeAudioJob` | Continuation | 2x | ffmpeg two-pass loudnorm (EBU R128, -14 LUFS) |
| `UpdateQuotaJob` | Continuation | 1x | Recalculate user quota, fire threshold events |
| `SendQuotaEmailJob` | Fire-and-forget | 3x | SMTP notification (rate-limited 1/day/threshold) |
| `SendWelcomeEmailJob` | Fire-and-forget | 3x | Welcome email on whitelist addition |

### Concurrency

| Limit | Value |
|-------|-------|
| Global max concurrent extractions | 9 |
| Per-user max concurrent extractions | 3 |

Per-user limit enforced in `StartDownload` use case — excess jobs stay `Pending`.

### Playlist Downloads

Each video gets its own `ExtractAudioJob`. Failures don't block the batch. Retries work per-track.

### Configuration

- Storage: SQLite (same database, separate Hangfire tables)
- Dashboard: `/hangfire`, Admin role only
- Queue: Single `default` queue

---

## 8. Authentication & Identity

### Flow

1. User visits app → not authenticated → redirect to Static SSR Login page
2. Click Login with Google → Google OAuth 2.0 flow
3. On success → ASP.NET Core Identity creates/updates local user
4. Custom `IClaimsTransformation` checks whitelist → if active, assigns role (User/Admin)
5. If not whitelisted or disabled → access denied

### Components

| Concern | Location |
|---------|----------|
| Google OAuth config | `Host/Program.cs` |
| `ApplicationUser : IdentityUser` | `Identity/Domain/` |
| Whitelist entity + repository | `Identity/Domain/` + `Identity/Infrastructure/` |
| `IClaimsTransformation` | `Identity/Infrastructure/Adapters/` |
| Login/Logout/AccessDenied | `Frontend/Pages/Auth/` (Static SSR) |

---

## 9. Data Model

### Download Module

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `Guid` | PK |
| `UserId` | `string` | FK to Identity |
| `Url` | `string` | YouTube URL |
| `VideoId` | `string` | YouTube video ID |
| `Title` | `string` | |
| `Author` | `string` | |
| `Format` | `string` | Mp3, Flac, M4a, WebM |
| `Status` | `string` | Pending, Downloading, Normalizing, Completed, Failed |
| `Progress` | `int` | 0-100 |
| `OriginalFilename` | `string` | |
| `CorrectedFilename` | `string` | Cleaned filename |
| `FilePath` | `string` | |
| `FileSizeBytes` | `long` | |
| `NormalizeAudio` | `bool` | |
| `ErrorMessage` | `string` | nullable |
| `RetryCount` | `int` | |
| `PlaylistId` | `string` | nullable, links batch downloads |
| `CreatedAt` | `DateTime` | |
| `CompletedAt` | `DateTime` | nullable |
| `UpdatedAt` | `DateTime` | |

Indexes: `UserId`, `Status`, `VideoId`, `PlaylistId`

### Identity Module

**WhitelistEntries:**

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `Guid` | PK |
| `UserId` | `string` | UK, email address |
| `Role` | `string` | User, Admin |
| `AddedBy` | `string` | |
| `IsActive` | `bool` | |
| `WelcomeEmailSent` | `bool` | |
| `AddedAt` | `DateTime` | |

**UserSettings:**

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `Guid` | PK |
| `UserId` | `string` | UK, FK |
| `DefaultFormat` | `string` | Mp3, Flac, M4a, WebM |
| `EnableNormalization` | `bool` | |
| `NormalizationLufs` | `int` | -20 to -10, default -14 |
| `EmailNotifications` | `bool` | |
| `UpdatedAt` | `DateTime` | |

### Quota Module

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `Guid` | PK |
| `UserId` | `string` | UK |
| `QuotaBytes` | `long` | default 1 GB |
| `UsedBytes` | `long` | calculated |
| `FileCount` | `int` | |
| `CurrentThreshold` | `string` | Normal, Warning, Critical, Blocked |
| `LastEmailSentAt` | `DateTime` | nullable |
| `LastEmailThreshold` | `string` | nullable |
| `UpdatedAt` | `DateTime` | |

### Discovery & Radio Modules

No persistent storage. In-memory caching only:

| Module | Cache | TTL |
|--------|-------|-----|
| Discovery | MusicBrainz results | 5 minutes |
| Radio | Station list | 1 hour |
| Radio | Now-playing / playlist | 30 seconds |

---

## 10. API Endpoints

All endpoints in `Host/Endpoints/`, versioned under `/api/v1/`.

### Download

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/youtube/search?q=` | Search YouTube |
| POST | `/api/v1/downloads` | Start single download |
| GET | `/api/v1/downloads/{id}/status` | Job progress |
| GET | `/api/v1/downloads/users/{userId}` | User's jobs |
| GET | `/api/v1/downloads/users/{userId}/stats` | User download stats (total, completed, top artists, 30d trend) |
| GET | `/api/v1/playlists/info?url=` | Playlist metadata |
| GET | `/api/v1/playlists/videos?playlistId=` | Video list |
| POST | `/api/v1/playlists/download` | Batch download |
| GET | `/api/v1/files/users/{userId}` | Completed files |
| GET | `/api/v1/files/{id}/download` | Download to device |
| DELETE | `/api/v1/files/{id}` | Delete file |

### Discovery

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/musicbrainz/search?type=&q=` | Search artists/tracks/releases |
| GET | `/api/v1/musicbrainz/artists/{id}` | Artist details |

### Radio

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/radio/stations` | Station list |
| GET | `/api/v1/radio/stations/{id}/now-playing` | Current song |
| GET | `/api/v1/radio/stations/{id}/playlist?limit=` | Recent playlist |
| POST | `/api/v1/radio/download` | Download now-playing (body: `RadioDownloadRequest` with `stationId`, `artist`, `title`; auto-searches YouTube for best match) |

### Quota

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/quotas/users/{userId}` | Quota info |
| GET | `/api/v1/quotas/users/{userId}/check?bytes=` | Pre-download check |

### Identity

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/users/{userId}/profile` | User profile |
| GET | `/api/v1/users/{userId}/settings` | User settings |
| PUT | `/api/v1/users/{userId}/settings` | Update settings |

### Admin

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/admin/stats` | Global statistics |
| GET | `/api/v1/admin/stats/users` | All user stats |
| GET | `/api/v1/admin/stats/users/{userId}` | User detail stats |
| GET | `/api/v1/admin/whitelist` | List whitelist |
| POST | `/api/v1/admin/whitelist` | Add user |
| PUT | `/api/v1/admin/whitelist/{id}` | Toggle status |
| DELETE | `/api/v1/admin/whitelist/{id}` | Remove user |

### System

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health/live` | Liveness probe |
| GET | `/health/ready` | Readiness (DB + external APIs) |
| GET | `/scalar` | OpenAPI documentation |
| GET | `/hangfire` | Job dashboard (Admin) |
| GET | `/metrics` | Prometheus metrics |

---

## 11. Frontend Pages

| Route | Page | Render Mode | Wireframe |
|-------|------|-------------|-----------|
| `/` | Home | Interactive Server | [docs/design/home/wireframe.md](../../design/home/wireframe.md) |
| `/playlist` | PlaylistDownload | Interactive Server | [docs/design/playlist/wireframe.md](../../design/playlist/wireframe.md) |
| `/musicbrainz` | MusicBrainzSearch | Interactive Server | [docs/design/musicbrainz/wireframe.md](../../design/musicbrainz/wireframe.md) |
| `/radio` | Radio | Interactive Server | [docs/design/radio/wireframe.md](../../design/radio/wireframe.md) |
| `/profile` | Profile | Interactive Server | [docs/design/profile/wireframe.md](../../design/profile/wireframe.md) |
| `/admin/whitelist` | Whitelist | Interactive Server | [docs/design/admin-whitelist/wireframe.md](../../design/admin-whitelist/wireframe.md) |
| `/admin/statistics` | Statistics | Interactive Server | [docs/design/admin-statistics/wireframe.md](../../design/admin-statistics/wireframe.md) |
| `/login` | Login | Static SSR | [docs/design/login/wireframe.md](../../design/login/wireframe.md) |
| `/access-denied` | AccessDenied | Static SSR | — |
| `/error` | Error | Static SSR | — |

### Reusable Components

| Component | Used By | Purpose |
|-----------|---------|---------|
| `YouTubeResultsList` | Home, MusicBrainz, Radio | YouTube results with thumbnails + download |
| `DownloadProgressCard` | Home, Playlist | Real-time job progress via SignalR |
| `QuotaIndicator` | Playlist, Profile | Storage bar with threshold coloring |
| `StationSelector` | Radio | Radio station toggle group |

---

## 12. Error Handling

| Layer | Strategy |
|-------|----------|
| API boundary | FluentValidation → `Results.ValidationProblem()`. All errors as ProblemDetails (RFC 9457). |
| Use cases | `Result<T>` pattern — no exceptions for business logic |
| Infrastructure | Specific exception types, `ILogger<T>` structured logging |
| Hangfire jobs | Retry policy per job type. After max retries: `Failed` status, `ReceiveFailed` via SignalR |
| External APIs | `IHttpClientFactory` + Polly retry/circuit-breaker. MusicBrainz: 1 req/sec. YouTube: cache-first. SRG SSR: graceful degradation. |
| Frontend | `MudSnackbar` for errors. `MudSkeleton` for loading states. No raw exceptions shown. |

---

## 13. Testing Strategy

| Layer | Framework | Scope |
|-------|-----------|-------|
| Domain | xUnit + FluentAssertions | Entity behavior, value objects |
| Use Cases | xUnit + NSubstitute + FluentAssertions | Handler logic, mocked ports |
| Infrastructure | xUnit + SQLite in-memory | Repository queries, EF configs |
| Components | bUnit + NSubstitute | Blazor rendering, interactions |
| E2E | Playwright | Full user flows |
| API | Bruno collections | Manual/exploratory per endpoint |

TDD mandatory. Test naming: `MethodName_StateUnderTest_ExpectedBehavior`.

---

## 14. Deployment

Single Docker container (Hybrid approach).

| Aspect | Value |
|--------|-------|
| Runtime base | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` |
| Build base | `mcr.microsoft.com/dotnet/sdk:10.0-alpine` |
| User | Non-root `appuser` |
| Port | 8080 |
| Volumes | `/data` (SQLite + Hangfire), `/storage` (audio files) |
| Config | All via environment variables — no secrets in appsettings |
| Health | `/health/live`, `/health/ready` |
| Logging | Serilog structured JSON to stdout |
| Observability | OpenTelemetry traces, `/metrics` Prometheus endpoint |

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `YOUTUBE_API_KEY` | Yes | YouTube Data API v3 |
| `GOOGLE_CLIENT_ID` | Yes | Google OAuth |
| `GOOGLE_CLIENT_SECRET` | Yes | Google OAuth |
| `API_KEY` | Yes | Internal service auth |
| `SMTP_HOST` | Yes | Email notifications |
| `SMTP_PORT` | Yes | |
| `SMTP_PASSWORD` | Yes | |
| `SMTP_FROM` | Yes | Sender email address |
| `ConnectionStrings__Default` | Yes | SQLite connection string |
| `Serilog__MinimumLevel` | No | Log level override |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | No | OpenTelemetry collector |

---

## 15. Non-Functional Requirements

| Requirement | Target |
|-------------|--------|
| YouTube search cache TTL | 5 minutes |
| MusicBrainz rate limit | 1 request/second |
| Radio refresh interval | 30 seconds |
| Default storage quota | 1 GB per user |
| Download retry attempts | 3 (exponential backoff: 2^n seconds) |
| Normalization target | -14 LUFS (EBU R128) |
| Email rate limit | 1 per day per quota threshold |
| Audio formats | MP3, FLAC, M4A, WebM |
| Global concurrent extractions | 9 |
| Per-user concurrent extractions | 3 |

---

## 16. Database Migration Strategy

EF Core migrations are run as a **separate init step** before the app starts — never inside `app.Run()` (per 12-Factor compliance). In Docker, this is handled via an entrypoint script that runs `dotnet ef database update` before starting the host. Locally, migrations are applied via CLI.

---

## 17. CORS

CORS is configured in `Host/Program.cs` to allow requests from `localhost` origins during development (for Bruno and other API testing tools). In production, CORS is restricted to the app's own origin since the Blazor frontend is embedded in the same host.

---

## 18. Out of Scope (v1)

- **Download cancellation** — no way to cancel an in-progress yt-dlp extraction. May be added in a future version.
- **Multiple OAuth providers** — only Google for v1. ASP.NET Core Identity supports adding more later.
- **Mobile app / PWA** — API is exposed but no dedicated mobile client.
