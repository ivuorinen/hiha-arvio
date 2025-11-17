# Hiha-Arvio (Sleeve Estimate) - Design Document

**Date:** 2025-11-18
**Project:** Hiha-Arvio
**Platforms:** iOS (primary), Web, macOS
**Technology:** .NET 8 MAUI, Blazor WebAssembly

## Overview

Hiha-Arvio (Finnish: "Sleeve Estimate") is a humor application that generates semi-random time estimates based on shake intensity. Users physically shake their phone (or mouse on desktop) to "pull an estimate from their sleeve" - a playful take on the Finnish expression for making educated guesses.

### Key Features

- **Dual estimation modes:** Work/project estimates and generic time durations (user-toggleable)
- **Intensity-based randomness:** Gentle shakes produce narrow, conservative ranges; vigorous shakes yield wide, unpredictable ranges
- **Hidden easter egg:** Shaking for >15 seconds triggers humorous/absurd estimates
- **Estimate history:** Last 10 estimates with timestamps (expandable for future stats)
- **Cross-platform:** Mobile-first (iOS), then web and macOS

## Architecture

### MVVM Pattern with Strict Separation

**Core Principle:** All business logic resides in ViewModels and Services for 100% testability. Views are thin, data-bound only.

### Models

```csharp
EstimateResult
├── Id: Guid
├── Timestamp: DateTimeOffset
├── EstimateText: string                // "2 weeks", "eventually"
├── Mode: EstimateMode                  // Work, Generic, Humorous
├── ShakeIntensity: double              // 0.0 to 1.0 normalized
└── ShakeDuration: TimeSpan

ShakeData
├── Intensity: double                   // Current shake strength
├── Duration: TimeSpan                  // How long shaking
└── IsShaking: bool

AppSettings
├── SelectedMode: EstimateMode          // Work or Generic toggle
└── MaxHistorySize: int                 // Default 10
```

### Services (All Injectable)

**IAccelerometerService**
- Abstracts platform accelerometer access (iOS) or mouse movement (desktop/web)
- Emits raw sensor data stream

**IShakeDetectionService**
- Processes accelerometer stream
- Detects shake start/stop events
- Calculates normalized intensity (0.0-1.0)
- Tracks shake duration

**IEstimateService**
- Generates estimates based on intensity, duration, and mode
- Implements easter egg logic (>15s → humorous mode)
- Manages estimate pools and range calculations

**IStorageService**
- Persists AppSettings (Preferences API)
- Stores EstimateResult history (SQLite)
- Auto-prunes history to max size

### ViewModels

**MainViewModel**
- Orchestrates shake detection and estimate generation
- Commands: ShakeStarted, ShakeStopped
- Properties: CurrentEstimate, IsShaking, History
- Updates UI, saves estimates to storage

**SettingsViewModel**
- Manages mode toggle and future preferences
- Persists settings changes

### Views

**MainPage**
- Large shake detection zone (80% screen)
- Minimal UI: pulsing animation during shake
- Estimate display with fade-in animation
- Mode toggle at bottom
- Settings/History icons in top bar

**HistoryPage**
- Scrollable list of last 10 estimates
- Each item: estimate text, timestamp, mode badge, intensity indicator
- Pull-to-refresh support

**SettingsPage**
- Mode toggle (Work ⟷ Generic)
- Future: sensitivity adjustment, max history size
- About/version info

## Data Flow

1. User shakes device → `IAccelerometerService` emits sensor data
2. `IShakeDetectionService` processes stream → detects shake, calculates intensity
3. On shake stop → `MainViewModel` invokes `IEstimateService.GenerateEstimate(intensity, duration, mode)`
4. Service checks easter egg (duration > 15s → force humorous mode)
5. Service calculates estimate range based on intensity
6. Returns random `EstimateResult` from calculated range
7. `MainViewModel` updates UI and calls `IStorageService.SaveEstimate()`
8. Storage auto-prunes history if exceeding max size

## Shake Detection Algorithm

### Accelerometer Processing (iOS)

```csharp
1. Monitor accelerometer stream via Microsoft.Maui.Devices.Sensors.Accelerometer
2. For each reading (x, y, z):
   magnitude = sqrt(x² + y² + z²)
3. Shake start: magnitude > 2.5g threshold
4. Track peak magnitude during shake session
5. Shake end: magnitude < threshold for 500ms continuous
6. Normalize intensity: peak / max_observed → [0.0, 1.0]
```

### Mouse Movement Simulation (Desktop/Web)

```csharp
1. Track mouse delta (Δx, Δy) over sliding 200ms window
2. Calculate velocity: sqrt(Δx² + Δy²) / Δt
3. "Shake" detection: velocity exceeds threshold
4. Intensity: normalize velocity relative to max observed
```

## Estimate Calculation Logic

### Estimate Pools

**Work Mode (gentle shake):**
- Range: 2 hours, 4 hours, 1 day, 2 days, 3 days, 5 days, 1 week

**Work Mode (hard shake):**
- Range: 15 minutes, 30 minutes, 1 hour, 2 hours, 1 day, 3 days, 1 week, 2 weeks, 1 month, 3 months, 6 months, 1 year

**Generic Mode (gentle shake):**
- Range: 1 minute, 5 minutes, 10 minutes, 15 minutes, 30 minutes, 1 hour, 2 hours, 3 hours

**Generic Mode (hard shake):**
- Range: 30 seconds, 1 minute, 5 minutes, 15 minutes, 30 minutes, 1 hour, 2 hours, 6 hours, 12 hours, 1 day, 3 days, 1 week, 2 weeks, 1 month

**Humorous Mode (easter egg):**
- Mix: "5 minutes", "tomorrow", "eventually", "next quarter", "when hell freezes over", "3 lifetimes", "Tuesday", "never", "your retirement"

### Range Selection Algorithm

```csharp
GenerateEstimate(intensity, duration, mode):
  1. IF duration > 15 seconds THEN mode = Humorous

  2. SELECT estimate_pool based on mode

  3. CALCULATE range bounds:
     IF intensity < 0.3 THEN
       range = first 20% of pool (conservative)
     ELSE IF intensity < 0.7 THEN
       range = first 50% of pool (moderate)
     ELSE
       range = entire pool (wild)

  4. SELECT random estimate from range

  5. RETURN EstimateResult with metadata
```

## Data Persistence

### Storage Implementation

**Technology:**
- SQLite (via `sqlite-net-pcl`) for EstimateResult history
- Preferences API for AppSettings

**Schema:**
```sql
CREATE TABLE EstimateHistory (
  Id TEXT PRIMARY KEY,
  Timestamp INTEGER NOT NULL,
  EstimateText TEXT NOT NULL,
  Mode INTEGER NOT NULL,
  ShakeIntensity REAL NOT NULL,
  ShakeDuration INTEGER NOT NULL
);

CREATE INDEX idx_timestamp ON EstimateHistory(Timestamp DESC);
```

**Operations:**
- `SaveSettings(AppSettings)` - Immediate persist to Preferences
- `LoadSettings()` - On app start
- `SaveEstimate(EstimateResult)` - After each shake, auto-prune if > max size
- `GetHistory(int count = 10)` - Return newest first
- `ClearHistory()` - For future reset feature

### Platform Storage Locations

- **iOS:** App's Documents directory
- **Web:** LocalStorage (settings) + IndexedDB (history)
- **macOS:** Application Support directory

## UI/UX Design

### Visual Design

**Color Scheme:** Clean, playful, high contrast
**Typography:** Large, bold estimates for readability
**Animations:** Smooth, 200-300ms transitions

### MainPage Details

- **Shake zone:** 80% of screen height, touch/click anywhere to activate
- **Active shake:** Subtle scale pulse animation (1.0 → 1.05 → 1.0 at 2Hz)
- **Estimate reveal:** Fade in + slight slide up
- **Mode toggle:** Bottom-aligned, labeled "Work Estimates ⟷ Generic Time"
- **Top bar:** Settings (gear icon), History (clock icon)

### HistoryPage Details

- **List items:**
  - Estimate text (large, bold)
  - Relative timestamp ("2 minutes ago")
  - Mode badge (color-coded: blue=Work, green=Generic, orange=Humorous)
  - Intensity indicator (3/5 filled dots)
- **Empty state:** "No estimates yet. Give it a shake!"
- **Pull-to-refresh:** Reload from storage

### Accessibility

- Full keyboard navigation support
- VoiceOver/TalkBack descriptions
- Minimum touch target: 44x44 points
- WCAG AA contrast ratios

## Testing Strategy

### Target Coverage: ~100%

### Unit Tests (xUnit + NSubstitute)

**Services:**
- `EstimateServiceTests`
  - Verify intensity → range mapping
  - Test each mode's estimate pools
  - Validate easter egg trigger (>15s)
  - Check randomness bounds
  - Edge cases: 0.0 intensity, 1.0 intensity

- `ShakeDetectionServiceTests`
  - Magnitude calculation accuracy
  - Threshold detection (start/stop)
  - Intensity normalization
  - Duration tracking

- `StorageServiceTests`
  - CRUD operations
  - History auto-pruning
  - Settings persistence
  - Concurrent access handling

**ViewModels:**
- `MainViewModelTests`
  - Command execution
  - Property change notifications
  - Service interaction mocking
  - History updates

- `SettingsViewModelTests`
  - Setting changes trigger persistence
  - Mode toggle behavior

### Integration Tests

- Test full data flow: ShakeDetection → EstimateService → Storage
- Mock only platform-specific dependencies (accelerometer)
- Verify ViewModel + Services integration

### UI Tests (Appium - Future)

- Critical path: Shake gesture → Estimate appears → Check history
- Mode toggle switches estimate pool
- Platform-specific: iOS shake, web mouse movement

### Test Data Builders

```csharp
EstimateResultBuilder
  .WithMode(EstimateMode.Work)
  .WithIntensity(0.5)
  .Build();

ShakeDataBuilder
  .WithIntensity(0.8)
  .WithDuration(TimeSpan.FromSeconds(5))
  .Build();
```

### Deterministic Testing

- Seed RNG in tests for reproducible randomness
- Time-based tests use injectable `ITimeProvider`

## Platform-Specific Implementation

### iOS (Primary Platform)

- **Accelerometer:** `Microsoft.Maui.Devices.Sensors.Accelerometer`
- **Permissions:** Add motion usage description to Info.plist
- **Background behavior:** Pause monitoring when backgrounded
- **Native feel:** iOS design patterns, haptic feedback on shake
- **Target:** iOS 15+ (MAUI .NET 8 requirement)

### Web (Blazor WebAssembly)

- **Mouse simulation:** Track delta movement, calculate velocity
- **Shake zone:** Visual boundary for mouse movement
- **Alternative:** Device Orientation API for mobile browsers
- **PWA:** Progressive Web App manifest for home screen install
- **Responsive:** Works on mobile browsers and desktops

### macOS

- **Input:** Mouse movement tracking (similar to web)
- **Native chrome:** macOS window style and menu bar
- **Keyboard shortcut:** Cmd+Shift+S to trigger manual shake
- **Accessibility:** Full keyboard navigation

### Dependency Injection Registration

```csharp
// MauiProgram.cs
#if IOS
builder.Services.AddSingleton<IAccelerometerService, IOSAccelerometerService>();
#elif WINDOWS || MACCATALYST
builder.Services.AddSingleton<IAccelerometerService, MouseMovementService>();
#endif

builder.Services.AddSingleton<IShakeDetectionService, ShakeDetectionService>();
builder.Services.AddSingleton<IEstimateService, EstimateService>();
builder.Services.AddSingleton<IStorageService, StorageService>();
```

## Project Structure

```
HihaArvio.sln
├── src/
│   ├── HihaArvio/                    # Main MAUI project
│   │   ├── Models/
│   │   │   ├── EstimateResult.cs
│   │   │   ├── ShakeData.cs
│   │   │   └── AppSettings.cs
│   │   ├── ViewModels/
│   │   │   ├── MainViewModel.cs
│   │   │   └── SettingsViewModel.cs
│   │   ├── Views/
│   │   │   ├── MainPage.xaml
│   │   │   ├── HistoryPage.xaml
│   │   │   └── SettingsPage.xaml
│   │   ├── Services/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IAccelerometerService.cs
│   │   │   │   ├── IShakeDetectionService.cs
│   │   │   │   ├── IEstimateService.cs
│   │   │   │   └── IStorageService.cs
│   │   │   ├── EstimateService.cs
│   │   │   ├── ShakeDetectionService.cs
│   │   │   ├── StorageService.cs
│   │   │   └── Platform/
│   │   │       ├── IOSAccelerometerService.cs
│   │   │       └── MouseMovementService.cs
│   │   ├── MauiProgram.cs
│   │   └── App.xaml
│   ├── HihaArvio.Core/               # Shared business logic (optional)
│   └── HihaArvio.Web/                # Blazor WebAssembly (future)
└── tests/
    ├── HihaArvio.Tests/              # Unit tests
    ├── HihaArvio.IntegrationTests/   # Integration tests
    └── HihaArvio.UITests/            # UI automation (future)
```

## Dependencies

### Core Dependencies
- `Microsoft.Maui.Controls` (.NET 8)
- `CommunityToolkit.Mvvm` (Source generators, MVVM helpers)
- `sqlite-net-pcl` (Database)
- `SQLitePCLRaw.bundle_green` (SQLite runtime)

### Testing Dependencies
- `xUnit` (Test framework)
- `NSubstitute` (Mocking)
- `FluentAssertions` (Assertion library)
- `Coverlet.Collector` (Code coverage)
- `Appium.WebDriver` (UI tests - future)

### Development Tools
- `ReportGenerator` (Coverage reports)
- `StyleCop.Analyzers` (Code analysis)

## Build & Deployment

### CI/CD Pipeline (GitHub Actions)

```yaml
1. Build .NET MAUI solution
2. Run unit tests
3. Run integration tests
4. Collect code coverage (Coverlet)
5. Generate coverage report (ReportGenerator)
6. Enforce coverage threshold: 95%+
7. Build iOS app (on macOS runner)
8. Build web app (publish Blazor)
```

### Code Quality

- **Nullable reference types:** Enabled
- **Warnings as errors:** Enabled
- **Code analysis:** StyleCop + built-in analyzers
- **EditorConfig:** Consistent formatting rules

### Release Strategy

- **Phase 1:** iOS app (TestFlight beta)
- **Phase 2:** Web app (static hosting)
- **Phase 3:** macOS app (App Store)

## Future Enhancements

### Stats Dashboard (Post-MVP)
- Most common estimate
- Average shake intensity
- Total shakes count
- Humorous mode discovery rate

### Advanced Features
- Custom estimate pools (user-defined)
- Sensitivity adjustment slider
- Share estimate to clipboard/social
- Team mode (synchronized shaking)

### Localization
- Finnish translation (native language)
- English (default)
- Extensible for more languages

## Success Criteria

- ✅ Test coverage ≥95%
- ✅ iOS app functions on iOS 15+
- ✅ Shake detection feels responsive (<100ms latency)
- ✅ Estimate generation is visually randomized
- ✅ History persists across app restarts
- ✅ Easter egg discoverable but not obvious
- ✅ All three platforms build without errors

## Technical Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Accelerometer sensitivity varies by device | Adaptive normalization, user calibration option |
| Web mouse simulation feels unnatural | Add tutorial/demo, support touch on mobile web |
| SQLite performance with large history | Auto-prune, indexed queries, async operations |
| .NET MAUI platform quirks | Thorough platform-specific testing, community resources |
| Test coverage hard to reach 100% on Views | Keep Views minimal, test ViewModels exhaustively |

---

**Document Status:** Validated
**Next Steps:** Create formal specification, begin implementation planning
