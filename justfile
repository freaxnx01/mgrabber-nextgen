# mgrabber-nextgen — common dev tasks (just recipes)
#
# `just` with no args prints the recipe list grouped by section.
#
# Requires just >= 1.20. Install:
#   Linux:   sudo apt install just  (or: cargo install just)
#   macOS:   brew install just
#   Windows: winget install Casey.Just  (or: scoop install just)

set windows-shell := ["pwsh", "-NoLogo", "-NonInteractive", "-Command"]
set dotenv-load
set positional-arguments

# ── Project Configuration ────────────────────────────────────────────────────

image_name      := "musicgrabber"
image_tag       := "local"
ghcr_image      := "ghcr.io/freaxnx01/mgrabber-nextgen:main"
compose         := "docker compose -f docker-compose.yml -f docker-compose.override.yml"
host_project    := "src/Host/Host.csproj"
startup_project := "src/Host"
props_file      := "Directory.Build.props"
log_dir         := "logs"
log_file        := log_dir + "/dev.log"

# ── Default ──────────────────────────────────────────────────────────────────

# Show this help (`just` with no args)
default:
    @just --list --unsorted

# ── Build & run ──────────────────────────────────────────────────────────────

# Build solution in Release mode
[group('build')]
build:
    mkdir -p {{log_dir}} && dotnet build -c Release 2>&1 | tee -a {{log_file}}

# Stop docker-compose and free port 8086
[group('build')]
stop:
    #!/usr/bin/env bash
    {{compose}} down 2>/dev/null || true
    PID=$(lsof -ti :8086 2>/dev/null)
    if [ -n "$PID" ]; then
        echo "── Killing process on port 8086 (PID $PID)"
        kill $PID 2>/dev/null || true
    fi

# Run Host locally (no Docker)
[group('build')]
run: stop _require-env
    @echo "── SSH tunnel (run in WSL2 on Win11): ssh -N -L 8086:localhost:8086 freax@192.168.1.108"
    @echo "── Logs: {{log_file}}"
    mkdir -p {{log_dir}} && ASPNETCORE_ENVIRONMENT=Development dotnet run --project {{host_project}} --urls http://localhost:8086 2>&1 | tee -a {{log_file}}

# Run Host with hot reload
[group('build')]
watch: stop _require-env
    @echo "── SSH tunnel (run in WSL2 on Win11): ssh -N -L 8086:localhost:8086 freax@192.168.1.108"
    @echo "── Logs: {{log_file}}"
    mkdir -p {{log_dir}} && ASPNETCORE_ENVIRONMENT=Development dotnet watch --non-interactive --project {{host_project}} --urls http://localhost:8086 2>&1 | tee -a {{log_file}}

# ── Testing ──────────────────────────────────────────────────────────────────

# Run all tests
[group('test')]
test:
    mkdir -p {{log_dir}} && dotnet test 2>&1 | tee -a {{log_file}}

# Run unit tests only
[group('test')]
test-unit:
    #!/usr/bin/env bash
    set -euo pipefail
    mkdir -p {{log_dir}}
    for proj in tests/Download.UnitTests tests/Discovery.UnitTests tests/Radio.UnitTests \
                tests/Quota.UnitTests tests/Identity.UnitTests tests/Shared.UnitTests; do
        dotnet test "$proj" 2>&1 | tee -a {{log_file}}
    done

# Run integration tests only
[group('test')]
test-integration:
    mkdir -p {{log_dir}} && dotnet test tests/Download.IntegrationTests 2>&1 | tee -a {{log_file}}

# Run all tests with coverage
[group('test')]
test-coverage:
    mkdir -p {{log_dir}} && dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage 2>&1 | tee -a {{log_file}}
    @echo "Coverage reports in ./coverage/"

# ── Docker (Compose) ─────────────────────────────────────────────────────────

# Run with docker-compose in foreground (Ctrl+C to stop)
[group('docker')]
docker-run: _require-env
    {{compose}} up --build

# Start in background with docker-compose
[group('docker')]
up: _require-env
    {{compose}} up -d --build

# Stop docker-compose
[group('docker')]
down:
    {{compose}} down

# Follow container logs
[group('docker')]
logs:
    {{compose}} logs -f

# Rebuild and restart
[group('docker')]
rebuild: down up

# Build Docker image
[group('docker')]
docker-build:
    docker build -t {{image_name}}:{{image_tag}} .

# Build and push Docker image to GHCR (skips CI)
[group('docker')]
push-image:
    docker build -t {{ghcr_image}} .
    docker push {{ghcr_image}}

# Build image and verify it starts + responds to health check
[group('docker')]
deploy-test:
    #!/usr/bin/env bash
    set -uo pipefail
    echo "── Building Docker image..."
    docker build -t {{image_name}}:{{image_tag}} .
    echo "── Starting container..."
    docker rm -f mgrabber-deploy-test 2>/dev/null || true
    docker run -d --name mgrabber-deploy-test \
        -p 18080:8080 \
        -e ConnectionStrings__Default="Data Source=/data/musicgrabber.db" \
        -e GOOGLE_CLIENT_ID=test \
        -e GOOGLE_CLIENT_SECRET=test \
        -v /tmp/mgrabber-test-data:/data \
        -v /tmp/mgrabber-test-storage:/storage \
        {{image_name}}:{{image_tag}}
    echo "── Waiting for startup..."
    for i in 1 2 3 4 5 6 7 8 9 10; do
        sleep 2
        STATUS=$(curl -sk -o /dev/null -w '%{http_code}' http://localhost:18080/health/live 2>/dev/null)
        if [ "$STATUS" = "200" ] || [ "$STATUS" = "500" ]; then
            echo "── Container responding (HTTP $STATUS)"
            break
        fi
        echo "── Waiting... (attempt $i)"
    done
    STATUS=$(curl -sk -o /dev/null -w '%{http_code}' http://localhost:18080/health/live 2>/dev/null)
    docker logs mgrabber-deploy-test 2>&1 | tail -5
    docker rm -f mgrabber-deploy-test > /dev/null 2>&1
    rm -rf /tmp/mgrabber-test-data /tmp/mgrabber-test-storage
    if [ "$STATUS" = "200" ]; then
        echo "── PASS: Health check returned 200"
    else
        echo "── FAIL: Health check returned $STATUS"
        exit 1
    fi

# ── Database ─────────────────────────────────────────────────────────────────

# Run all EF Core migrations (Download, Identity, Quota)
[group('database')]
migrate:
    #!/usr/bin/env bash
    set -euo pipefail
    mkdir -p {{log_dir}}
    cd {{startup_project}}
    dotnet ef database update --project ../Modules/Download/Infrastructure --context DownloadDbContext 2>&1 | tee -a ../../{{log_file}}
    dotnet ef database update --project ../Modules/Identity/Infrastructure --context IdentityDbContext 2>&1 | tee -a ../../{{log_file}}
    dotnet ef database update --project ../Modules/Quota/Infrastructure --context QuotaDbContext 2>&1 | tee -a ../../{{log_file}}

# Add migration. Usage: just migration-add Download AddSomething
[group('database')]
migration-add module name:
    dotnet ef migrations add {{name}} \
        --project src/Modules/{{module}}/Infrastructure \
        --startup-project {{startup_project}}

# ── Quality ──────────────────────────────────────────────────────────────────

# Check code formatting
[group('quality')]
lint:
    mkdir -p {{log_dir}} && dotnet format --verify-no-changes 2>&1 | tee -a {{log_file}}

# Check for outdated packages
[group('quality')]
outdated:
    mkdir -p {{log_dir}} && dotnet list package --outdated 2>&1 | tee -a {{log_file}}

# Check for vulnerable packages
[group('quality')]
vuln:
    mkdir -p {{log_dir}} && dotnet list package --vulnerable --include-transitive 2>&1 | tee -a {{log_file}}

# ── Verification ─────────────────────────────────────────────────────────────

# Verify YouTube Data API v3 key is valid
[group('verify')]
check-yt-key: _require-env
    #!/usr/bin/env bash
    set -uo pipefail
    YOUTUBE_API_KEY=$(grep -E '^YOUTUBE_API_KEY=' .env | cut -d= -f2-)
    if [ -z "$YOUTUBE_API_KEY" ]; then
        echo "ERROR: YOUTUBE_API_KEY is empty in .env"
        exit 1
    fi
    echo "── Testing YouTube Data API v3 key..."
    BODY=$(curl -s -w '\n%{http_code}' \
        "https://www.googleapis.com/youtube/v3/videos?part=id&id=dQw4w9WgXcQ&key=$YOUTUBE_API_KEY")
    RESPONSE=$(echo "$BODY" | tail -1)
    BODY=$(echo "$BODY" | sed '$d')
    if [ "$RESPONSE" = "200" ]; then
        echo "── PASS: API key is valid (HTTP 200)"
    elif [ "$RESPONSE" = "403" ]; then
        REASON=$(echo "$BODY" | grep -o '"reason":"[^"]*"' | head -1 | cut -d'"' -f4)
        if [ "$REASON" = "API_KEY_HTTP_REFERRER_BLOCKED" ]; then
            echo "── FAIL: API key has HTTP referrer restrictions in Google Cloud Console."
            echo "         Server-side keys should use IP restrictions (or none), not referrer restrictions."
            echo "         Fix at: https://console.cloud.google.com/apis/credentials"
        else
            MSG=$(echo "$BODY" | grep -o '"message":"[^"]*"' | head -1 | cut -d'"' -f4)
            echo "── FAIL: HTTP 403 — $MSG"
        fi
        exit 1
    elif [ "$RESPONSE" = "400" ]; then
        echo "── FAIL: Bad request — key may be malformed (HTTP 400)"
        exit 1
    else
        echo "── FAIL: Unexpected response (HTTP $RESPONSE)"
        exit 1
    fi

# ── Versioning (single source of truth: Directory.Build.props → <Version>) ───

# Show current version
[group('version')]
[unix]
version:
    @sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1

[group('version')]
[windows]
version:
    Write-Output ([xml](Get-Content {{props_file}})).Project.PropertyGroup.Version

# Set version explicitly (usage: just version-set 1.2.3)
[group('version')]
[unix]
version-set v:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    tmp=$(mktemp)
    sed "s|<Version>${cur}</Version>|<Version>{{v}}</Version>|" {{props_file}} > "$tmp"
    mv "$tmp" {{props_file}}
    echo "Version: ${cur} → {{v}}"

# Bump major version (1.2.3 → 2.0.0)
[group('version')]
[unix]
bump-major:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    major=$(echo "$cur" | cut -d. -f1)
    new=$((major + 1)).0.0
    tmp=$(mktemp)
    sed "s|<Version>${cur}</Version>|<Version>${new}</Version>|" {{props_file}} > "$tmp"
    mv "$tmp" {{props_file}}
    echo "Version: ${cur} → ${new}"

# Bump minor version (1.2.3 → 1.3.0)
[group('version')]
[unix]
bump-minor:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    major=$(echo "$cur" | cut -d. -f1)
    minor=$(echo "$cur" | cut -d. -f2)
    new=${major}.$((minor + 1)).0
    tmp=$(mktemp)
    sed "s|<Version>${cur}</Version>|<Version>${new}</Version>|" {{props_file}} > "$tmp"
    mv "$tmp" {{props_file}}
    echo "Version: ${cur} → ${new}"

# Bump patch version (1.2.3 → 1.2.4)
[group('version')]
[unix]
bump-patch:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    major=$(echo "$cur" | cut -d. -f1)
    minor=$(echo "$cur" | cut -d. -f2)
    patch=$(echo "$cur" | cut -d. -f3)
    new=${major}.${minor}.$((patch + 1))
    tmp=$(mktemp)
    sed "s|<Version>${cur}</Version>|<Version>${new}</Version>|" {{props_file}} > "$tmp"
    mv "$tmp" {{props_file}}
    echo "Version: ${cur} → ${new}"

# Bump version from Conventional Commits via git-cliff (minor/patch only)
[group('version')]
[unix]
bump-auto:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    new=$(git-cliff --bumped-version 2>/dev/null | sed 's/^v//')
    if [ -z "$new" ]; then
        echo "Error: git-cliff did not return a bumped version" >&2
        exit 1
    fi
    cur_major=$(echo "$cur" | cut -d. -f1)
    new_major=$(echo "$new" | cut -d. -f1)
    if [ "$cur_major" != "$new_major" ]; then
        echo "Error: auto-bump suggests major version change ($cur → $new)." >&2
        echo "  Major bumps require explicit action. Run: just bump-major" >&2
        exit 1
    fi
    if [ "$new" = "$cur" ]; then
        echo "Version unchanged: $cur (no bump-worthy commits since last tag)"
        exit 0
    fi
    tmp=$(mktemp)
    sed "s|<Version>${cur}</Version>|<Version>${new}</Version>|" {{props_file}} > "$tmp"
    mv "$tmp" {{props_file}}
    echo "Version: ${cur} → ${new} (auto)"

# ── Release ──────────────────────────────────────────────────────────────────

# Generate CHANGELOG.md from Conventional Commits (git-cliff)
[group('release')]
changelog:
    git-cliff --output CHANGELOG.md
    @echo "CHANGELOG.md updated."

# Tag release, regenerate changelog, and commit
[group('release')]
[unix]
release:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    if git tag -l "v${cur}" | grep -q .; then
        echo "Error: Tag v${cur} already exists." >&2
        exit 1
    fi
    echo "── Releasing v${cur}..."
    git-cliff --tag "v${cur}" --output CHANGELOG.md
    git add {{props_file}} CHANGELOG.md
    git commit -m "chore: release v${cur}"
    git tag -a "v${cur}" -m "release: v${cur}"
    echo "── Released v${cur}. Don't forget to push: just push-release"

# Auto-bump from commits, then release
[group('release')]
release-auto: bump-auto release

# Push main branch and current version tag to origin
[group('release')]
[unix]
push-release:
    #!/usr/bin/env bash
    set -euo pipefail
    cur=$(sed -n 's|.*<Version>\([^<]*\)</Version>.*|\1|p' {{props_file}} | head -1)
    git push origin main "v${cur}"

# ── Cleanup ──────────────────────────────────────────────────────────────────

# Remove build artifacts
[group('cleanup')]
clean:
    find . -type d \( -name bin -o -name obj -o -name publish \) -exec rm -rf {} + 2>/dev/null || true
    rm -rf coverage/

# ── Helpers ──────────────────────────────────────────────────────────────────

# Private: fail with a helpful message if .env is missing
_require-env:
    #!/usr/bin/env bash
    if [ ! -f .env ]; then
        echo "ERROR: .env file not found. Create one with your secrets:"
        echo ""
        echo "  GOOGLE_CLIENT_ID=..."
        echo "  GOOGLE_CLIENT_SECRET=..."
        echo "  YOUTUBE_API_KEY=..."
        echo "  SMTP_HOST=..."
        echo "  SMTP_PORT=..."
        echo "  SMTP_PASSWORD=..."
        echo "  SMTP_FROM=..."
        echo ""
        echo "See docker-compose.yml for all variables."
        exit 1
    fi
