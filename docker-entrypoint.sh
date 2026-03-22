#!/bin/sh
set -e

echo "Running EF Core migrations..."
dotnet Host.dll --migrate

echo "Starting MusicGrabber..."
exec dotnet Host.dll
