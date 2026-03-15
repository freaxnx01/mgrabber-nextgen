# E2E Testing Guide

## Prerequisites

### 1. Application Running
```bash
cd /home/ubuntu/.openclaw/workspace/mgrabber-nextgen
docker-compose up -d
```

### 2. YouTube API Key Configured
Add to `.env`:
```bash
YOUTUBE_API_KEY=your-youtube-api-key
```

### 3. Install Playwright Browsers
```bash
cd /home/ubuntu/.openclaw/workspace/mgrabber-nextgen/tests/E2E
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

## Running E2E Tests

### Run All E2E Tests
```bash
cd /home/ubuntu/.openclaw/workspace/mgrabber-nextgen
dotnet test tests/E2E
```

### Run Specific Test
```bash
dotnet test tests/E2E --filter "FullyQualifiedName~YouTubeE2ETests"
```

### Run with YouTube API Key
```bash
YOUTUBE_API_KEY=your-key dotnet test tests/E2E
```

### Run in Headed Mode (see browser)
```bash
dotnet test tests/E2E --settings tests/E2E/headed.runsettings
```

## Test Coverage

### YouTubeE2ETests.cs
- ✅ `SearchYouTube_ValidQuery_ShowsRealResults` - Verifies real YouTube search
- ✅ `SearchYouTube_NoResults_ShowsEmptyState` - Tests empty results handling
- ✅ `DownloadYouTubeVideo_FullFlow_Success` - End-to-end download flow
- ✅ `DownloadWithNormalization_OptionWorks` - Tests normalization option
- ✅ `Download_InvalidVideo_ShowsError` - Error handling

### MusicDownloaderE2ETests.cs
- ✅ Page loads successfully
- ✅ Search functionality
- ✅ Download flow
- ✅ Navigation to statistics
- ✅ Statistics page displays

## Configuration

### Environment Variables
| Variable | Required | Description |
|----------|----------|-------------|
| `YOUTUBE_API_KEY` | Yes for YouTube tests | YouTube Data API v3 key |
| `BASE_URL` | No | App URL (default: http://localhost:8086) |

### Test Settings
Create `tests/E2E/.runsettings`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <Playwright>
    <BrowserName>chromium</BrowserName>
    <LaunchOptions>
      <Headless>true</Headless>
    </LaunchOptions>
  </Playwright>
</RunSettings>
```

## Troubleshooting

### Tests Skip Automatically
If `YOUTUBE_API_KEY` is not set, YouTube tests will be skipped with message:
```
YouTube API key not configured - skipping E2E test
```

### Timeout Issues
Increase timeout in test or use shorter test videos for faster downloads.

### Browser Not Found
```bash
playwright install chromium
```

## CI/CD Integration

### GitHub Actions Example
```yaml
- name: Run E2E Tests
  env:
    YOUTUBE_API_KEY: ${{ secrets.YOUTUBE_API_KEY }}
  run: dotnet test tests/E2E --verbosity normal
```

## Test Data

Tests use these search queries:
- "Roxette The Look" - Well-known song, reliable results
- "test audio" - Generic test query
- "xyzabc123nonsense" - Should return no results
