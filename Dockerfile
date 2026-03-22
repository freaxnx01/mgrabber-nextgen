# syntax=docker/dockerfile:1

# ─── Build stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY MusicGrabber.sln .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY global.json .

# Copy all .csproj files preserving directory structure
COPY src/Host/Host.csproj src/Host/
COPY src/Shared/Shared.csproj src/Shared/
COPY src/Frontend/Frontend.csproj src/Frontend/
COPY src/Modules/Download/Domain/Download.Domain.csproj src/Modules/Download/Domain/
COPY src/Modules/Download/Application/Download.Application.csproj src/Modules/Download/Application/
COPY src/Modules/Download/Infrastructure/Download.Infrastructure.csproj src/Modules/Download/Infrastructure/
COPY src/Modules/Discovery/Domain/Discovery.Domain.csproj src/Modules/Discovery/Domain/
COPY src/Modules/Discovery/Application/Discovery.Application.csproj src/Modules/Discovery/Application/
COPY src/Modules/Discovery/Infrastructure/Discovery.Infrastructure.csproj src/Modules/Discovery/Infrastructure/
COPY src/Modules/Radio/Domain/Radio.Domain.csproj src/Modules/Radio/Domain/
COPY src/Modules/Radio/Application/Radio.Application.csproj src/Modules/Radio/Application/
COPY src/Modules/Radio/Infrastructure/Radio.Infrastructure.csproj src/Modules/Radio/Infrastructure/
COPY src/Modules/Quota/Domain/Quota.Domain.csproj src/Modules/Quota/Domain/
COPY src/Modules/Quota/Application/Quota.Application.csproj src/Modules/Quota/Application/
COPY src/Modules/Quota/Infrastructure/Quota.Infrastructure.csproj src/Modules/Quota/Infrastructure/
COPY src/Modules/Identity/Domain/Identity.Domain.csproj src/Modules/Identity/Domain/
COPY src/Modules/Identity/Application/Identity.Application.csproj src/Modules/Identity/Application/
COPY src/Modules/Identity/Infrastructure/Identity.Infrastructure.csproj src/Modules/Identity/Infrastructure/

# Restore NuGet packages (Host project only — excludes test projects)
RUN dotnet restore src/Host/Host.csproj

# Copy all source files
COPY src/ src/

# Publish Host project
RUN dotnet publish src/Host/Host.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ─── Runtime stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install yt-dlp and ffmpeg
RUN apk add --no-cache \
    ffmpeg \
    python3 \
    py3-pip \
    && pip3 install --break-system-packages yt-dlp \
    && yt-dlp --version

# Create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Copy published output
COPY --from=build /app/publish .

# Copy entrypoint script
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Ensure volume mount points exist and are writable by appuser
# (must come after adduser above)
RUN mkdir -p /data /storage && chown -R appuser:appgroup /data /storage

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["/docker-entrypoint.sh"]
