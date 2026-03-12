# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj and restore
COPY DownloadApi.csproj .
RUN dotnet restore

# Copy source and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage with Alpine + ffmpeg + Python for yt-dlp
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS runtime
WORKDIR /app

# Install ffmpeg and Python (yt-dlp needs Python)
RUN apk add --no-cache ffmpeg python3 py3-pip

# Install yt-dlp via pip (more reliable on Alpine)
RUN pip3 install --break-system-packages yt-dlp

# Create storage directory
RUN mkdir -p /storage

# Copy published app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "DownloadApi.dll"]
