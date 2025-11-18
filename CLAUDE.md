# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hiha-Arvio (Finnish: "Sleeve Estimate") is a .NET 8 MAUI cross-platform application that generates semi-random time estimates based on physical shake input (accelerometer on mobile, mouse movement on desktop). This is a humor app for "pulling an estimate from your sleeve."

**Platforms (in priority order):** iOS (primary) → Web (Blazor) → macOS

## Critical Requirements

### Specification Compliance

- **ALWAYS read `spec.md` before implementing features** - contains RFC 2119 formal requirements (MUST, REQUIRED, SHALL)
- **Design reference:** `docs/plans/2025-11-18-hiha-arvio-design.md` contains validated architecture decisions
- Nullable reference types MUST be enabled
- All compiler warnings MUST be treated as errors
- Minimum test coverage: 95% (enforced in CI/CD)

### Architecture Constraints

**MVVM Pattern with Strict Separation:**
- Models: Plain data objects only, no business logic
- ViewModels: All presentation logic, must be 100% testable without UI dependencies
- Views: Thin layer, data binding only, minimal presentation code
- Services: All business logic and infrastructure concerns

**Dependency Injection:**
- All services MUST be injected via constructor (no service locator, no `new` keyword in ViewModels)
- Register services in `MauiProgram.cs`
- Platform-specific implementations use conditional compilation (`#if IOS`, `#elif WINDOWS || MACCATALYST`)

### Testing Requirements

- Test coverage MUST be ≥95% (measured with Coverlet, enforced in CI/CD)
- Testing stack: xUnit + NSubstitute (mocking) + FluentAssertions (assertions)
- All tests MUST use deterministic randomness (seeded RNG)
- Mock all external dependencies (sensors, database, file system)
- Use test data builders for complex objects

## Technology Stack

- **Framework:** .NET 8 MAUI (LTS)
- **Language:** C# 12 with nullable reference types
- **Database:** SQLite via `sqlite-net-pcl`
- **MVVM:** CommunityToolkit.Mvvm (source generators)
- **Testing:** xUnit, NSubstitute, FluentAssertions, Coverlet

## Core Architecture

### Service Layer (All Injectable)

**IAccelerometerService**
- Platform abstraction: accelerometer (iOS) or mouse movement (desktop/web)
- Emits observable stream of sensor data

**IShakeDetectionService**
- Processes accelerometer stream
- Detects shake start/stop, calculates normalized intensity [0.0-1.0]
- Tracks shake duration for easter egg trigger (>15 seconds)

**IEstimateService**
- Generates estimates based on: intensity, duration, mode
- Implements intensity → range mapping:
  - Intensity <0.3: narrow range (first 20% of pool)
  - Intensity 0.3-0.7: medium range (first 50% of pool)
  - Intensity >0.7: full range (entire pool)
- Easter egg: duration >15s forces Humorous mode
- MUST use cryptographically secure RNG (`System.Security.Cryptography.RandomNumberGenerator`)

**IStorageService**
- Settings: Preferences API
- History: SQLite with auto-pruning (max 10 estimates)
- All operations MUST be async

### Key Models

```csharp
EstimateResult: Id, Timestamp, EstimateText, Mode, ShakeIntensity, ShakeDuration
EstimateMode enum: Work, Generic, Humorous
ShakeData: Intensity, Duration, IsShaking
AppSettings: SelectedMode, MaxHistorySize
```

### Estimate Pools (from spec.md §3.2.2)

**Work Mode:**
- Gentle: "2 hours", "4 hours", "1 day", "2 days", "3 days", "5 days", "1 week"
- Hard: adds "15 minutes", "30 minutes", "2 weeks", "1 month", "3 months", "6 months", "1 year"

**Generic Mode:**
- Gentle: "1 minute" → "3 hours"
- Hard: "30 seconds" → "1 month"

**Humorous Mode (easter egg):**
- "tomorrow", "eventually", "next quarter", "when hell freezes over", "3 lifetimes", "Tuesday", "never", "your retirement"

## Platform-Specific Implementation

### iOS (Primary Platform)
- Accelerometer via `Microsoft.Maui.Devices.Sensors.Accelerometer`
- Shake detection: `magnitude = sqrt(x² + y² + z²)`, threshold 2.5g
- Must request motion permissions in Info.plist
- Haptic feedback on shake detection (recommended)
- Target iOS 15+

### Web (Blazor WebAssembly)
- Mouse movement simulation: track delta over 200ms window, calculate velocity
- PWA manifest for home screen install
- Support Device Orientation API for mobile browsers (optional)

### macOS
- Mouse movement tracking (similar to web)
- Keyboard shortcut: Cmd+Shift+S for manual shake trigger
- Native menu bar integration

## Project Structure (When Implemented)

```
HihaArvio.sln
├── src/HihaArvio/              # Main MAUI project
│   ├── Models/
│   ├── ViewModels/
│   ├── Views/
│   ├── Services/
│   │   ├── Interfaces/
│   │   └── Platform/         # Platform-specific implementations
│   └── MauiProgram.cs
├── tests/
│   ├── HihaArvio.Tests/                # Unit tests
│   ├── HihaArvio.IntegrationTests/     # Integration tests
│   └── HihaArvio.UITests/              # UI automation (future)
├── docs/plans/                          # Design documents
└── spec.md                              # Formal specification
```

## Development Commands

### Build Commands
- **Build all platforms:** `dotnet build HihaArvio.sln`
- **Build specific framework:** `dotnet build HihaArvio.sln -f net8.0`
- **Build iOS:** `dotnet build HihaArvio.sln -f net8.0-ios`
- **Build macOS:** `dotnet build HihaArvio.sln -f net8.0-maccatalyst`

### Test Commands
- **Run all tests:** `dotnet test tests/HihaArvio.Tests/HihaArvio.Tests.csproj`
- **Run specific test class:** `dotnet test tests/HihaArvio.Tests/HihaArvio.Tests.csproj --filter "FullyQualifiedName~EstimateModeTests"`
- **Run single test:** `dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"`

### Code Coverage
- **Generate coverage:** `dotnet test tests/HihaArvio.Tests/HihaArvio.Tests.csproj --collect:"XPlat Code Coverage"`
- **Coverage files:** Located in `tests/HihaArvio.Tests/TestResults/{guid}/coverage.cobertura.xml`

### Run Commands
- **iOS Simulator:** `dotnet build src/HihaArvio/HihaArvio.csproj -t:Run -f net8.0-ios`
- **macOS:** `dotnet build src/HihaArvio/HihaArvio.csproj -t:Run -f net8.0-maccatalyst`

### Notes
- All commands should be run from the repository root directory
- Xcode must be installed and configured (`xcode-select -p` should point to Xcode.app)
- MAUI workload must be installed (`dotnet workload list` should show `maui`)

## Critical Implementation Notes

### Easter Egg Behavior
- Hidden feature: NO UI indication
- Trigger: shake duration >15 seconds
- Effect: temporarily force EstimateMode.Humorous
- Do NOT expose in Settings or UI

### Shake Detection Algorithm
1. Monitor sensor stream (accelerometer or mouse)
2. Calculate magnitude/velocity
3. Detect start: exceeds threshold
4. Track peak intensity during session
5. Detect end: below threshold for 500ms continuous
6. Normalize to [0.0, 1.0]

### Performance Requirements
- Shake response: <100ms latency
- Estimate display: <200ms after shake stop
- History load: <500ms
- All database operations: async, non-blocking

### Security
- Use `System.Security.Cryptography.RandomNumberGenerator` for estimate selection
- No external data transmission
- All data stored locally only
- Request minimum required permissions

## Code Quality Enforcement

- Enable nullable reference types across all projects
- Treat warnings as errors
- Follow EditorConfig rules (when defined)
- Code analysis: StyleCop + built-in analyzers enabled
- CI/CD must enforce 95% coverage threshold and fail builds below this

## Implementation Status

### Completed Milestones

**Milestone 1: Project Setup & Core Models (✅ Complete)**
- Solution structure with src/HihaArvio and tests/HihaArvio.Tests
- Core models: EstimateMode, EstimateResult, ShakeData, AppSettings
- 48 tests, all passing
- Build verification: all platforms (net8.0, iOS, macOS)

**Milestone 2: Services Layer (✅ Complete)**
- IEstimateService + EstimateService (25 tests)
  - Estimate generation with intensity-based range selection
  - Easter egg logic (>15s → Humorous mode)
  - Cryptographically secure RNG
- IStorageService + StorageService (14 tests)
  - SQLite-based persistence
  - Settings and history storage
  - Auto-pruning based on MaxHistorySize
- IShakeDetectionService + ShakeDetectionService (22 tests)
  - Shake detection with 1.5g threshold
  - Intensity calculation and normalization (0.0-1.0)
  - Duration tracking
  - Event-based notification (ShakeDataChanged)
- Integration tests (10 tests)
  - Service interaction verification
  - Full flow: shake → estimate → storage
- **Total: 119 tests, all passing**
- **Coverage:** 51.28% line (low due to MAUI template), 87.5% branch
- **Build:** 0 warnings, 0 errors across all platforms

**Milestone 3: ViewModels Layer (✅ Complete)**
- MainViewModel (18 tests)
  - Coordinates shake detection and estimate generation
  - Subscribes to ShakeDetectionService.ShakeDataChanged event
  - Detects shake stop (transition from shaking → not shaking)
  - Mode selection and current estimate display
  - Implements IDisposable for cleanup
- HistoryViewModel (15 tests)
  - Manages estimate history display with ObservableCollection
  - LoadHistoryCommand for async history retrieval
  - ClearHistoryCommand for pruning
  - IsEmpty property for empty state handling
- SettingsViewModel (13 tests)
  - Settings management (SelectedMode, MaxHistorySize)
  - SaveSettingsCommand for persistence
  - Auto-loads settings on initialization
- All ViewModels use CommunityToolkit.Mvvm source generators
  - [ObservableProperty] for property change notifications
  - [RelayCommand] for commands
- **Total: 165 tests, all passing (119 services + 46 ViewModels)**
- **Build:** 0 warnings, 0 errors across all platforms

**Milestone 4: Views/UI Layer (✅ Complete)**
- Dependency Injection configuration in MauiProgram.cs
  - All services registered as Singleton
  - All ViewModels and Pages registered as Transient
  - SQLite database path configured with FileSystem.AppDataDirectory
- MainPage.xaml with data binding
  - Mode selector (Work/Generic)
  - Current estimate display with conditional visibility
  - Shake status indicator
  - Uses x:DataType for compile-time binding verification
- HistoryPage.xaml with data binding
  - CollectionView with ItemTemplate
  - Empty state when no history
  - Refresh and Clear All buttons
  - Auto-loads history on OnAppearing()
- SettingsPage.xaml with data binding
  - Picker for mode selection (Work/Generic only - Humorous is easter egg)
  - Stepper for MaxHistorySize (5-100, increment by 5)
  - Save Settings button
  - About section with easter egg hint
- Value Converters for UI logic
  - IsNullConverter / IsNotNullConverter (conditional visibility)
  - BoolToShakingConverter (status text)
  - BoolToColorConverter (status colors)
  - InvertedBoolConverter (boolean inversion)
  - All registered in App.xaml resources
- AppShell.xaml navigation
  - TabBar with 3 tabs: Estimate, History, Settings
  - Each tab uses ContentTemplate for lazy loading
- **Total: 165 tests still passing (no UI tests yet)**
- **Build:** 0 warnings, 0 errors across all platforms (net8.0, iOS, macOS Catalyst)

### Remaining Work

**Milestone 5: Platform-Specific Implementations** (Not Started)
- IAccelerometerService interface
- iOS implementation (real accelerometer)
- Desktop/Web implementation (mouse movement simulation)

**Milestone 6: Integration & Polish** (Not Started)
- Platform testing
- Performance optimization
- Documentation finalization

## Important Implementation Order

Per design document, phased development:
1. **Phase 1:** iOS app (primary platform, accelerometer-based)
2. **Phase 2:** Web app (Blazor, mouse simulation)
3. **Phase 3:** macOS app (native integration)

Focus on Phase 1 first to validate core shake detection and estimate generation logic.
