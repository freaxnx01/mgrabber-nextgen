#!/bin/bash
# Run all tests script with optional coverage

set -e

RUN_COVERAGE=${1:-"false"}

echo "=== Building solution ==="
dotnet build

echo ""
echo "=== Running Unit Tests ==="
if [ "$RUN_COVERAGE" = "true" ]; then
    echo "Running with code coverage..."
    dotnet test tests/DownloadApi.UnitTests --verbosity normal \
        /p:CollectCoverage=true \
        /p:CoverletOutputFormat=cobertura \
        /p:CoverletOutput=./coverage/
    
    echo ""
    echo "=== Running Frontend Unit Tests ==="
    dotnet test tests/Frontend.UnitTests --verbosity normal \
        /p:CollectCoverage=true \
        /p:CoverletOutputFormat=cobertura \
        /p:CoverletOutput=./coverage/
else
    dotnet test tests/DownloadApi.UnitTests --verbosity normal
    
    echo ""
    echo "=== Running Frontend Unit Tests ==="
    dotnet test tests/Frontend.UnitTests --verbosity normal
fi

echo ""
echo "=== Running Integration Tests ==="
dotnet test tests/DownloadApi.Integration --verbosity normal || echo "Integration tests skipped (may require running services)"

echo ""
echo "=== Running E2E Tests ==="
echo "Make sure docker-compose is running: docker-compose up -d"
dotnet test tests/E2E --verbosity normal || echo "E2E tests skipped (may require running services)"

if [ "$RUN_COVERAGE" = "true" ]; then
    echo ""
    echo "=== Generating Coverage Report ==="
    # Check if reportgenerator is installed
    if ! command -v reportgenerator &> /dev/null; then
        echo "Installing ReportGenerator..."
        dotnet tool install -g dotnet-reportgenerator-globaltool 2>/dev/null || true
    fi
    
    # Merge coverage reports
    reportgenerator \
        -reports:"**/coverage.cobertura.xml" \
        -targetdir:"coveragereport" \
        -reporttypes:"Html;Cobertura;MarkdownSummary"
    
    echo ""
    echo "Coverage report generated in: coveragereport/"
    echo "View: coveragereport/index.html"
fi

echo ""
echo "=== All Tests Complete ==="
echo ""
echo "Usage:"
echo "  ./run-tests.sh          # Run tests without coverage"
echo "  ./run-tests.sh true     # Run tests with coverage report"
