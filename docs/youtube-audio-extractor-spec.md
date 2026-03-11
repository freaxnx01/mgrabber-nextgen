# YouTube Audio Extractor — Project Specification

## Project Overview

A web application that allows whitelisted users to search for YouTube videos (via MusicBrainz and YouTube), extract audio tracks, and download them in various formats. Built with a microservice architecture using .NET 10.

---

## Architecture

### Container 1: Frontend — Blazor Server (.NET 10)

- **Rendering**: Blazor Server (SSR) with SignalR for real-time updates
- **Responsibilities**:
  - MusicBrainz artist/album/track browsing
  - YouTube search (via YouTube Data API v3) and URL paste
  - Download queue management with real-time progress (SignalR)
  - User file area: list, download, delete extracted audio files
  - Quota display (e.g. "12.4 / 50 MB" with progress bar)
  - Google SSO authentication (OAuth 2.0)
  - Admin UI for whitelist management
- **Database**: SQLite (mounted as Docker volume) for:
  - User accounts and roles (admin/user)
  - Whitelist (Gmail addresses)
  - Quota tracking per user
  - Download history
- **Authentication**:
  - Google OAuth 2.0 (via `Microsoft.AspNetCore.Authentication.Google`)
  - Gmail-based whitelist — only whitelisted emails can access the app
  - Super Admin email hardcoded in config
  - Admin UI to add/remove users from whitelist
- **Not exposed**: Only this container is publicly accessible

### Container 2: Download API — Minimal API (.NET 10)

- **Responsibilities**:
  - YouTube search proxy (YouTube Data API v3)
  - MusicBrainz API proxy
  - Audio extraction via `yt-dlp` + `ffmpeg`
  - Supports multiple formats: MP3, FLAC, etc.
  - Parallel downloads per user (configurable, e.g. max 3 concurrent)
  - Quota enforcement before starting extraction
- **Endpoints** (REST):
  - `POST /api/search/youtube` — search YouTube videos
  - `POST /api/search/musicbrainz` — search MusicBrainz artists/albums/tracks
  - `POST /api/download/start` — start audio extraction job
  - `GET /api/download/status/{jobId}` — get job progress
  - `GET /api/files/{userId}` — list user's files
  - `GET /api/files/{userId}/{fileId}` — download a file
  - `DELETE /api/files/{userId}/{fileId}` — delete a file
- **Internal only**: Not exposed to the internet, only reachable via Docker network
- **Security**:
  - No port mapping to host — only reachable via internal Docker network
  - Shared API Key (`X-Api-Key` header) validated on every request
  - Frontend passes authenticated User-ID as header (`X-User-Id`) for quota/file scoping
- **Tools**: `yt-dlp` (standalone Linux binary, no Python required) and `ffmpeg` installed in container image
- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` + `ffmpeg` via apk — lightweight (~200 MB total)

### Docker Compose

- Internal Docker network between frontend and API
- SQLite database as volume mount on frontend container
- User file storage as shared volume mount: `/storage/{userId}/`
- Environment variables for secrets (Google OAuth, YouTube API Key, Shared API Key)
- Download API has **no port mapping** — only accessible internally

```yaml
# Simplified example
services:
  frontend:
    build: ./src/Frontend
    ports:
      - "443:8080"          # publicly accessible
    environment:
      - DOWNLOAD_API_KEY=${API_KEY}
    networks:
      - internal

  download-api:
    build: ./src/DownloadApi
    # NO ports: mapping — internal only
    environment:
      - API_KEY=${API_KEY}
    volumes:
      - storage:/storage
    networks:
      - internal

networks:
  internal:
    driver: bridge

volumes:
  storage:
```

### Scaling (prepared, not required at launch)

The Download API can be horizontally scaled when needed:

```bash
docker compose up --scale download-api=3
```

**Requirements for scaling**:
- **Load Balancer**: Traefik (recommended, already in use in homelab) or Nginx in front of Download API instances
- **Shared Storage**: All instances must mount the same `/storage` volume
- **Shared Job State**: Job status must be stored in SQLite or Redis so any instance can report progress — not in-memory
- **Sticky sessions not required**: Requests are stateless with User-ID in header

**Recommendation**: Start with a single instance. One Download API instance can handle 10+ parallel downloads (yt-dlp is I/O-bound). Scale later if needed — the architecture supports it without code changes.

---

## Features

### Music Discovery & Search

- **MusicBrainz Integration** (primary):
  - Search artists, albums, tracks
  - Browse album tracklists with metadata
  - Select tracks → auto-search matching YouTube video
- **YouTube Search** (via YouTube Data API v3):
  - Free tier: 10,000 units/day (~100 searches/day)
  - Returns: title, thumbnail, duration, channel
  - Uses same Google Cloud project as OAuth
- **Direct URL**: Paste YouTube URL for extraction

### Audio Extraction

- Powered by `yt-dlp` + `ffmpeg`
- Supported formats: MP3, FLAC, and other common audio formats
- User selects desired format before extraction
- Batch download: select multiple videos/tracks at once

### User Area & Quota System

- **Storage quota**: 50 MB per user (default)
  - Quota configurable per user by admin
  - Displayed as progress bar in UI
  - Extraction blocked when quota exceeded
  - User must download files locally and delete from server to free space
- **Parallel downloads**: Max 3 concurrent downloads per user (configurable)
- **Auto-cleanup**: Files automatically deleted after 14 days
- **Manual cleanup**: Users can delete individual files anytime

### Authentication & Authorization

- **Google SSO**: OAuth 2.0, Gmail accounts only
- **Whitelist**: Only pre-approved Gmail addresses can log in
- **Roles**:
  - `SuperAdmin` — hardcoded email in appsettings, full access
  - `Admin` — can manage whitelist and user quotas
  - `User` — can search, extract, and manage own files
- **Admin UI**:
  - Add/remove Gmail addresses from whitelist
  - View user storage usage
  - Adjust per-user quotas
  - View download history

### Admin Statistics Dashboard

- **Per-User Stats**:
  - Total downloads (count), total data extracted (MB)
  - Downloads per day/week/month (chart)
  - Most downloaded artists/tracks
  - Current storage usage vs. quota
  - Last active timestamp
- **Global Stats**:
  - Total downloads across all users
  - Total storage used vs. available disk space
  - Downloads per day (trend chart)
  - Most popular artists/tracks across all users
  - Active users per day/week
  - yt-dlp success/failure rate
  - Average extraction time per format
- **Data source**: All tracked in SQLite `DownloadHistory` table (userId, url, title, artist, format, fileSize, duration, status, createdAt)

---

## Tech Stack

| Component         | Technology                              |
| ----------------- | --------------------------------------- |
| Frontend          | Blazor Server, .NET 10                  |
| Download API      | ASP.NET Core Minimal API, .NET 10       |
| Database          | SQLite (via EF Core)                    |
| Audio extraction  | yt-dlp (standalone binary) + ffmpeg     |
| Music metadata    | MusicBrainz API                         |
| YouTube search    | YouTube Data API v3                     |
| Auth              | Google OAuth 2.0                        |
| Containerization  | Docker + Docker Compose                 |
| Real-time updates | SignalR                                 |

---

## External Services (Setup Required)

### Google Cloud Console (free)

1. Create a new project
2. Enable **YouTube Data API v3**
3. Configure **OAuth Consent Screen**
4. Create **OAuth 2.0 Credentials** → Client ID + Client Secret
5. Create **API Key** for YouTube Data API

All free of charge for this usage level.

### MusicBrainz API

- Completely free, no API key required
- Rate limit: 1 request/second (with proper User-Agent header)
- Docs: https://musicbrainz.org/doc/MusicBrainz_API

---

## Configuration (appsettings.json / Environment Variables)

```jsonc
{
  "GoogleAuth": {
    "ClientId": "<from Google Cloud Console>",
    "ClientSecret": "<from Google Cloud Console>"
  },
  "YouTubeApi": {
    "ApiKey": "<from Google Cloud Console>"
  },
  "SuperAdmin": {
    "Email": "<your-gmail@gmail.com>"
  },
  "Quota": {
    "DefaultStorageLimitMB": 50,
    "MaxConcurrentDownloads": 3,
    "AutoCleanupDays": 14
  },
  "DownloadApi": {
    "BaseUrl": "http://download-api:8080",
    "ApiKey": "<random-generated-shared-secret>"
  }
}
```

---

## Suggested Solution Structure

```
YouTubeAudioExtractor/
├── docker-compose.yml
├── src/
│   ├── Frontend/                      # Blazor Server App
│   │   ├── Frontend.csproj
│   │   ├── Program.cs
│   │   ├── Components/
│   │   │   ├── Pages/
│   │   │   │   ├── Home.razor         # Dashboard
│   │   │   │   ├── Search.razor       # MusicBrainz + YouTube search
│   │   │   │   ├── Downloads.razor    # Active downloads / queue
│   │   │   │   ├── MyFiles.razor      # User file area + quota
│   │   │   │   └── Admin/
│   │   │   │       ├── Whitelist.razor
│   │   │   │       ├── Users.razor
│   │   │   │       └── Statistics.razor # Admin stats dashboard
│   │   │   └── Layout/
│   │   ├── Auth/
│   │   │   └── DevAuthHandler.cs      # Fake auth for development
│   │   ├── Services/                  # HTTP clients for Download API
│   │   ├── Data/                      # EF Core DbContext, entities
│   │   └── Dockerfile
│   ├── DownloadApi/                   # Minimal API
│   │   ├── DownloadApi.csproj
│   │   ├── Program.cs
│   │   ├── Endpoints/
│   │   │   ├── SearchEndpoints.cs
│   │   │   ├── DownloadEndpoints.cs
│   │   │   ├── FileEndpoints.cs
│   │   │   └── HealthEndpoints.cs     # yt-dlp/ffmpeg health check
│   │   ├── Services/
│   │   │   ├── IAudioExtractor.cs     # Abstraction interface
│   │   │   ├── YtDlpExtractor.cs      # yt-dlp implementation
│   │   │   ├── YouTubeSearchService.cs
│   │   │   ├── MusicBrainzService.cs
│   │   │   └── AudioExtractionService.cs
│   │   └── Dockerfile
│   └── Shared/                        # Shared DTOs
│       ├── Shared.csproj
│       └── Models/
│           ├── SearchResult.cs
│           ├── DownloadJob.cs
│           ├── AudioFile.cs
│           ├── QuotaInfo.cs
│           └── DownloadHistory.cs     # Stats tracking
└── README.md
```

---

## Development Notes

- **MusicBrainz → YouTube matching**: When a user selects a track from MusicBrainz, construct a YouTube search query from `"{artist} {track title} official audio"` or `"{artist} {track title} music video"` and let the user pick from the results.
- **Quota check flow**: Before starting any extraction, the API should estimate the output file size (based on video duration and format) and verify the user has enough quota remaining.
- **SignalR progress**: The frontend subscribes to a SignalR hub for real-time download progress. The frontend polls the Download API status endpoint and pushes updates to the connected client.
- **Internal API security**: The Download API is protected by network isolation (no external port mapping) plus a shared API key (`X-Api-Key` header). The frontend passes the authenticated user's ID via `X-User-Id` header — the Download API trusts this because only the frontend can reach it.
- **Scaling strategy**: Start with a single Download API instance. If needed, scale horizontally via `docker compose --scale`. Requires Traefik/Nginx for load balancing and shared volume + database for state. No code changes needed.

---

## yt-dlp Maintenance Strategy

### Abstraction Layer

Never call yt-dlp CLI directly from business logic. All interaction goes through a single abstracted service:

```csharp
public interface IAudioExtractor
{
    Task<ExtractionResult> ExtractAsync(string url, AudioFormat format, CancellationToken ct);
    Task<VideoInfo> GetInfoAsync(string url, CancellationToken ct);
    Task<string> GetVersionAsync();
}

public class YtDlpExtractor : IAudioExtractor
{
    // All yt-dlp switches and CLI args centralized HERE
}
```

If switches change, only one class needs updating.

### Version Pinning

Use the standalone Linux binary (no Python required) with a pinned version in the Dockerfile:

```dockerfile
# Standalone binary — no Python needed, ~40 MB
ARG YTDLP_VERSION=2025.03.15
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/download/${YTDLP_VERSION}/yt-dlp_linux \
    -o /usr/local/bin/yt-dlp && \
    chmod +x /usr/local/bin/yt-dlp

# ffmpeg via Alpine package manager
RUN apk add --no-cache ffmpeg
```

Never use `latest` — updates happen deliberately, not accidentally. The `ARG` makes it easy for Renovate to detect and bump the version.

### Update Process

1. **Detection**: Renovate Bot or GitHub Action checks for new yt-dlp releases (weekly)
2. **PR created**: Automated PR bumps the version in Dockerfile
3. **CI tests**: Pipeline runs integration tests against known YouTube URLs
4. **Manual review**: Verify test results, merge PR
5. **Deploy**: Rebuild and redeploy only the Download API container — frontend untouched

### Health Check & Monitoring

```
GET /api/health
```

Health endpoint checks:
- `yt-dlp --version` returns expected version
- `ffmpeg -version` is available
- Test extraction of a short, known-stable YouTube video (optional, configurable)

### Error Handling & Resilience

Differentiate failure types:
- **Transient errors** (network timeout, rate limit) → automatic retry with exponential backoff (max 3 retries)
- **yt-dlp broken** (YouTube changed something) → log error, notify admin (e.g. email or webhook), inform user with clear message
- **Video unavailable** (deleted, geo-blocked, private) → clear user-facing error, no retry
- **Extraction timeout** → configurable timeout per download (e.g. 10 min), kill process and report failure

### Rollback

If a new yt-dlp version breaks things:
```bash
# Revert to previous container image
docker compose up -d --no-deps download-api
```
Keep at least the last 3 container image versions tagged for fast rollback.

---

## Development & Testing Authentication

### The Problem

Google OAuth requires valid redirect URIs and a real Google Cloud project — this is friction during local development and CI testing.

### Environment-Based Auth Strategy

Use `IWebHostEnvironment` to switch between auth modes:

```csharp
if (builder.Environment.IsDevelopment())
{
    // Fake auth: auto-login as configurable test user
    builder.Services.AddAuthentication("DevAuth")
        .AddScheme<DevAuthHandler>("DevAuth", opts => { });
}
else
{
    // Real Google OAuth
    builder.Services.AddAuthentication()
        .AddGoogle(options => { ... });
}
```

### Development Mode (`ASPNETCORE_ENVIRONMENT=Development`)

- **DevAuthHandler**: Custom `AuthenticationHandler` that auto-authenticates as a configured test user
- No Google Cloud credentials needed
- Configurable test users in `appsettings.Development.json`:

```jsonc
{
  "DevAuth": {
    "Email": "dev@test.local",
    "Name": "Dev User",
    "Role": "SuperAdmin"  // easily switch roles for testing
  }
}
```

- Middleware skips whitelist check in dev mode
- UI shows a banner: "⚠ Development Mode — Auth bypassed"

### Integration Testing

- Use `WebApplicationFactory<Program>` with the same DevAuthHandler
- Test all roles: SuperAdmin, Admin, User
- Test whitelist rejection with unlisted emails
- No external dependencies needed in CI pipeline

### Staging / Pre-Production

- Use real Google OAuth but with a separate Google Cloud project
- Separate OAuth credentials in `appsettings.Staging.json`
- Whitelist limited to team members

### Configuration Matrix

| Environment | Auth Provider | Whitelist | Google Cloud needed |
| ----------- | ------------- | --------- | ------------------- |
| Development | DevAuthHandler | Disabled  | No                  |
| Testing/CI  | DevAuthHandler | Testable  | No                  |
| Staging     | Google OAuth   | Enabled   | Yes (separate)      |
| Production  | Google OAuth   | Enabled   | Yes                 |
