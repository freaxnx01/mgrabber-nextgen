# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj and restore
COPY DownloadApi.csproj .
RUN dotnet restore

# Copy source and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage with Alpine + ffmpeg
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS runtime
WORKDIR /app

# Install ffmpeg
RUN apk add --no-cache ffmpeg

# Install yt-dlp (pinned version)
ARG YTDLP_VERSION=2025.03.10
RUN wget -O /usr/local/bin/yt-dlp https://github.com/yt-dlp/yt-dlp/releases/download/${YTDLP_VERSION}/yt-dlp_linux && \
    chmod +x /usr/local/bin/yt-dlp

# Create storage directory
RUN mkdir -p /storage

# Copy published app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "DownloadApi.dll"]
