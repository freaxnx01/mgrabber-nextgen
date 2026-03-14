#!/bin/bash
# Run all tests script

set -e

echo "=== Building solution ==="
dotnet build

echo ""
echo "=== Running Unit Tests ==="
dotnet test tests/DownloadApi.UnitTests --verbosity normal

echo ""
echo "=== Running Integration Tests ==="
dotnet test tests/DownloadApi.Integration --verbosity normal

echo ""
echo "=== Running E2E Tests ==="
echo "Make sure docker-compose is running: docker-compose up -d"
dotnet test tests/E2E --verbosity normal

echo ""
echo "=== All Tests Complete ==="
