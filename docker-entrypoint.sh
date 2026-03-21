#!/bin/sh
set -e

echo "Running EF Core migrations..."
dotnet MusicGrabber.Host.dll --migrate

echo "Starting MusicGrabber..."
exec dotnet MusicGrabber.Host.dll
