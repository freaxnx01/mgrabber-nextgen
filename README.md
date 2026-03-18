# 🎵 MusicGrabber - .NET 10 Music Download Application

[![.NET Build and Test](https://github.com/freaxnx01/mgrabber-nextgen/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/freaxnx01/mgrabber-nextgen/actions/workflows/dotnet-build.yml)
[![Docker Build](https://github.com/freaxnx01/mgrabber-nextgen/actions/workflows/docker-build.yml/badge.svg)](https://github.com/freaxnx01/mgrabber-nextgen/actions/workflows/docker-build.yml)

A full-stack application for downloading audio from YouTube videos with Google OAuth authentication, quota management, and a Blazor Server frontend.

## ✨ Features

- 🔍 **YouTube Search** — Search videos with YouTube Data API v3
- 📋 **Playlist Downloads** — Download entire YouTube playlists
- 🎵 **MusicBrainz Integration** — Search artists, tracks, and releases
- 📻 **Radio Now Playing** — Download songs playing on SRG SSR radio stations
- 🔐 **Google OAuth** — Secure authentication
- 📊 **Quota Management** — Per-user storage quotas with email notifications
- 🛡️ **Whitelist** — Admin-managed user access control
- 📱 **Responsive UI** — Bootstrap-based Blazor Server frontend

## 🚀 Quick Start

```bash
# Clone and build
git clone <repo>
cd mgrabber-nextgen

# Configure environment
cp .env.example .env
# Edit .env with your API keys

# Run with Docker Compose
docker-compose up --build

# Or run locally (requires .NET 10 Preview)
dotnet run --project src/Frontend
dotnet run --project src/DownloadApi
```

## 🧪 Testing

```bash
# Run all tests
./run-tests.sh

# Run with code coverage
./run-tests.sh true

# View coverage report
coveragereport/index.html
```

## 📊 Code Coverage

| Project | Coverage |
|---------|----------|
| DownloadApi | ~25% |
| Frontend | ~5% |
| **Overall** | **~15%** |

*Coverage threshold: 60%* — Work in progress!

## 🏗️ Architecture

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────┐
│   Blazor Server │────▶│  Download API    │────▶│   yt-dlp    │
│   (Frontend)    │     │  (Minimal API)   │     │   ffmpeg    │
└─────────────────┘     └──────────────────┘     └─────────────┘
                               │
                               ▼
                        ┌──────────────┐
                        │   SQLite DB  │
                        └──────────────┘
```

## 🔧 Configuration

Required environment variables (see `.env.example`):

| Variable | Description |
|----------|-------------|
| `YOUTUBE_API_KEY` | YouTube Data API v3 key |
| `GOOGLE_CLIENT_ID` | Google OAuth client ID |
| `GOOGLE_CLIENT_SECRET` | Google OAuth client secret |
| `SMTP_HOST` | SMTP server for email notifications |
| `DOWNLOAD_API_KEY` | Internal API key |

## 📚 API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/health` | GET | Health check |
| `/api/search/youtube` | GET | Search YouTube |
| `/api/download/start` | POST | Start audio extraction |
| `/api/download/status/{id}` | GET | Get download status |
| `/api/playlist/info` | GET | Get playlist metadata |
| `/api/playlist/download` | POST | Download playlist |
| `/api/radio/stations` | GET | List radio stations |
| `/api/radio/now-playing` | GET | Current song on station |

## 🛠️ Tech Stack

- **Backend**: .NET 10 Minimal API, SQLite
- **Frontend**: Blazor Server, Bootstrap 5
- **Audio**: yt-dlp, ffmpeg
- **Auth**: Google OAuth 2.0
- **CI/CD**: GitHub Actions, Docker

## 📄 License

MIT
