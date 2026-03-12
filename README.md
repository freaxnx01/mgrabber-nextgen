# .NET 10 Preview Download API

A Minimal API for extracting audio from YouTube videos.

## Quick Start

```bash
# Set API key
export API_KEY=your-secret-key

# Build and run
docker-compose up --build

# Check health
curl http://localhost:8080/api/health
```

## Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/health` | GET | Health check with yt-dlp & ffmpeg status |
| `/api/search/youtube` | GET | YouTube search (coming) |
| `/api/download/start` | POST | Start audio extraction (coming) |

## Tech Stack

- .NET 10 Minimal API
- yt-dlp (standalone binary)
- ffmpeg (Alpine package)
- Docker + Docker Compose
