# GitHub Actions Workflows

This repository uses GitHub Actions for automated building, testing, and publishing.

## 🚀 Workflows

### 1. Docker Build (`docker-build.yml`)

**Triggers:**
- Push to `main` or `develop`
- Pull requests to `main`
- Tags (e.g., `v1.0.0`)
- Manual dispatch

**What it does:**
- Builds Docker images for Frontend and API
- Pushes images to GitHub Container Registry (ghcr.io)
- Creates downloadable artifacts (tar.gz files)
- Generates build attestations
- Supports multi-architecture builds (AMD64, ARM64)

**Images:**
- Frontend: `ghcr.io/freaxnx01/mgrabber-nextgen/frontend:TAG`
- API: `ghcr.io/freaxnx01/mgrabber-nextgen/api:TAG`

### 2. .NET Build (`dotnet-build.yml`)

**Triggers:**
- Push to `main` or `develop`
- Pull requests to `main`
- Manual dispatch

**What it does:**
- Builds the .NET solution
- Runs tests
- Publishes self-contained applications
- Creates downloadable archives (tar.gz and zip)

## 📥 Downloading Artifacts

### Method 1: GitHub Web Interface

1. Go to [Actions](../../actions) tab
2. Select a workflow run
3. Scroll down to "Artifacts" section
4. Click on artifact name to download

### Method 2: Using Docker Images

```bash
# Pull images from GitHub Container Registry
docker pull ghcr.io/freaxnx01/mgrabber-nextgen/frontend:main
docker pull ghcr.io/freaxnx01/mgrabber-nextgen/api:main

# Run containers
docker run -p 8086:8080 ghcr.io/freaxnx01/mgrabber-nextgen/frontend:main
docker run -p 8085:8080 ghcr.io/freaxnx01/mgrabber-nextgen/api:main
```

### Method 3: Download Published Binaries

```bash
# Download artifacts using GitHub CLI
gh run download RUN_ID --repo freaxnx01/mgrabber-nextgen

# Or download specific artifact
gh run download RUN_ID --name frontend-release-tar
```

## 🏷️ Image Tags

| Tag Pattern | Description | Example |
|------------|-------------|---------|
| `main` | Latest main branch | `frontend:main` |
| `develop` | Latest develop branch | `frontend:develop` |
| `v1.0.0` | Semantic version | `frontend:v1.0.0` |
| `1.0` | Major.Minor version | `frontend:1.0` |
| `abc1234` | Short SHA | `frontend:abc1234` |
| `pr-123` | Pull request | `frontend:pr-123` |

## 🔐 Authentication

To pull private images, you need to authenticate:

```bash
# Using GitHub CLI
gh auth token | docker login ghcr.io -u USERNAME --password-stdin

# Or using Personal Access Token
docker login ghcr.io -u USERNAME -p TOKEN
```

## 🛠️ Manual Workflow Trigger

You can manually trigger builds:

1. Go to [Actions](../../actions)
2. Select a workflow
3. Click "Run workflow"
4. Enter optional parameters
5. Click "Run workflow"

## 📊 Build Status

| Workflow | Status | Artifacts |
|----------|--------|-----------|
| Docker Build | ![Docker Build](../../actions/workflows/docker-build.yml/badge.svg) | Docker images + tar.gz |
| .NET Build | ![.NET Build](../../actions/workflows/dotnet-build.yml/badge.svg) | Binaries (tar.gz + zip) |

## 📝 Notes

- Artifacts are retained for 30 days (binaries) or 7 days (Docker tars)
- Docker images are retained indefinitely in ghcr.io
- Multi-architecture images support both x86_64 and ARM64
- Build cache is used to speed up subsequent builds
