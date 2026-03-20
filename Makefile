# HihaArvio Makefile
# Run `make` or `make help` to see available targets

SLN       := HihaArvio.sln
APP_PROJ  := src/HihaArvio/HihaArvio.csproj
TEST_PROJ := tests/HihaArvio.Tests/HihaArvio.Tests.csproj
CONFIG    ?= Debug

.PHONY: help restore build build-ios build-mac build-all \
        test test-filter coverage \
        run-ios run-mac \
        publish-ios publish-mac \
        clean format lint workload info

## help: Show this help message
help:
	@grep -E '^## ' $(MAKEFILE_LIST) | sed 's/^## //' | column -t -s ':'

# ── Core ──────────────────────────────────────────────

## restore: Restore NuGet packages
restore:
	dotnet restore $(SLN)

## build: Build for net10.0
build:
	dotnet build $(SLN) -f net10.0 -c $(CONFIG)

## build-ios: Build for iOS
build-ios:
	dotnet build $(APP_PROJ) -f net10.0-ios -c $(CONFIG)

## build-mac: Build for macOS Catalyst
build-mac:
	dotnet build $(APP_PROJ) -f net10.0-maccatalyst -c $(CONFIG)

## build-all: Build all platforms
build-all: build build-ios build-mac

# ── Testing ───────────────────────────────────────────

## test: Run all tests
test:
	dotnet test $(TEST_PROJ)

## test-filter: Run filtered tests (e.g. make test-filter FILTER=EstimateService)
test-filter:
	dotnet test $(TEST_PROJ) --filter "FullyQualifiedName~$(FILTER)"

## coverage: Run tests with coverage report → coverage/
coverage:
	dotnet test $(TEST_PROJ) --collect:"XPlat Code Coverage" --results-directory coverage/raw
	@if command -v reportgenerator >/dev/null 2>&1; then \
		reportgenerator \
			-reports:coverage/raw/**/coverage.cobertura.xml \
			-targetdir:coverage \
			-reporttypes:Html; \
		echo "Coverage report: coverage/index.html"; \
	else \
		echo "reportgenerator not installed — install with:"; \
		echo "  dotnet tool install -g dotnet-reportgenerator-globaltool"; \
		echo "Raw coverage data saved to coverage/raw/"; \
	fi

# ── Run ───────────────────────────────────────────────

## run-ios: Run on iOS simulator
run-ios:
	dotnet build $(APP_PROJ) -t:Run -f net10.0-ios

## run-mac: Run on macOS
run-mac:
	dotnet build $(APP_PROJ) -t:Run -f net10.0-maccatalyst

# ── Publish ───────────────────────────────────────────

## publish-ios: Publish iOS release build
publish-ios:
	dotnet publish $(APP_PROJ) -f net10.0-ios -c Release

## publish-mac: Publish macOS Catalyst release build
publish-mac:
	dotnet publish $(APP_PROJ) -f net10.0-maccatalyst -c Release

# ── Maintenance ───────────────────────────────────────

## clean: Clean build artifacts
clean:
	dotnet clean $(SLN)
	find . -type d \( -name bin -o -name obj \) -not -path '*/\.*' -exec rm -rf {} + 2>/dev/null || true
	rm -rf coverage/

## format: Format code per .editorconfig
format:
	dotnet format $(SLN)

## lint: Check formatting without modifying
lint:
	dotnet format $(SLN) --verify-no-changes

## workload: Install required MAUI workloads
workload:
	dotnet workload restore

## info: Show .NET SDK info
info:
	dotnet --info
