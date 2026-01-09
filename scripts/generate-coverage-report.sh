#!/bin/bash
# Generate HTML coverage report from Cobertura XML using ReportGenerator

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
TESTS_DIR="$PROJECT_ROOT/ExpenseTrackerAPI/src/ExpenseTrackerAPI.Application.Tests"
COVERAGE_DIR="$TESTS_DIR/TestResults/CoverageReport"

# Find the most recent coverage XML file
COVERAGE_XML=$(find "$TESTS_DIR/TestResults" -name "coverage.cobertura.xml" -type f -printf '%T@ %p\n' 2>/dev/null | sort -rn | head -1 | cut -d' ' -f2-)

if [ -z "$COVERAGE_XML" ]; then
    echo "Error: No coverage.cobertura.xml file found in TestResults directory"
    echo "Run 'dotnet test --collect:\"XPlat Code Coverage\" --settings:coverlet.runsettings' first to generate coverage data"
    exit 1
fi

echo "Found coverage file: $COVERAGE_XML"
echo "Generating HTML report..."

# Use the global tool path (adjust if needed)
REPORTGEN="$HOME/.dotnet/tools/reportgenerator"

if [ ! -f "$REPORTGEN" ]; then
    echo "Error: ReportGenerator not found at $REPORTGEN"
    echo "Install it with: dotnet tool install -g dotnet-reportgenerator-globaltool"
    exit 1
fi

# Generate the report
"$REPORTGEN" \
    -reports:"$COVERAGE_XML" \
    -targetdir:"$COVERAGE_DIR" \
    -reporttypes:Html

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Coverage report generated successfully!"
    echo "  Open: $COVERAGE_DIR/index.html"
else
    echo "Error: Failed to generate coverage report"
    exit 1
fi

