# Hiha-Arvio Application Specification

**Version:** 1.0
**Date:** 2025-11-18
**Status:** Draft

## 1. Introduction

This document specifies the requirements for Hiha-Arvio (Sleeve Estimate), a cross-platform mobile and web application that generates semi-random time estimates based on physical shake input.

### 1.1 Terminology

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119).

### 1.2 Scope

This specification covers:
- Functional requirements for shake detection and estimate generation
- Data persistence and history management
- User interface requirements
- Platform-specific implementations
- Testing and quality requirements

## 2. Platform Requirements

### 2.1 Technology Stack

The application MUST be implemented using the following technologies:

- **Framework:** .NET 8 Multi-platform App UI (MAUI)
- **Language:** C# 12 with nullable reference types enabled
- **Minimum iOS version:** iOS 15.0
- **Minimum macOS version:** macOS 12.0 (Monterey)
- **Web target:** Blazor WebAssembly

### 2.2 Supported Platforms

The application MUST support the following platforms in order of priority:

1. **iOS** (PRIMARY) - Native iOS application
2. **Web** (SECONDARY) - Browser-based via Blazor WebAssembly
3. **macOS** (TERTIARY) - Native macOS application

The application MAY support additional platforms (Android, Windows) in future releases.

## 3. Functional Requirements

### 3.1 Shake Detection

#### 3.1.1 Mobile Platforms (iOS)

The application MUST:
- Monitor device accelerometer data using `Microsoft.Maui.Devices.Sensors.Accelerometer`
- Calculate shake magnitude as `sqrt(x² + y² + z²)` for each accelerometer reading
- Detect shake start when magnitude exceeds 2.5g threshold
- Track peak magnitude during entire shake session
- Detect shake end when magnitude remains below threshold for 500 milliseconds continuous
- Normalize shake intensity to range [0.0, 1.0] based on peak magnitude relative to maximum observed

The application SHOULD:
- Request motion sensor permissions on first launch
- Pause accelerometer monitoring when app is backgrounded
- Provide haptic feedback when shake is detected

#### 3.1.2 Desktop Platforms (Web, macOS)

The application MUST:
- Monitor mouse movement delta (Δx, Δy) over a 200-millisecond sliding window
- Calculate mouse velocity as `sqrt(Δx² + Δy²) / Δt`
- Detect "shake" when velocity exceeds platform-appropriate threshold
- Normalize shake intensity based on velocity relative to maximum observed

The application SHOULD:
- Display visual boundaries for the shake detection zone
- Provide alternative keyboard shortcut (Cmd+Shift+S on macOS) for manual shake trigger

The application MAY:
- Support Device Orientation API for mobile web browsers as alternative input method

### 3.2 Estimate Generation

#### 3.2.1 Estimation Modes

The application MUST support three estimation modes:

1. **Work Mode** - Project and work-related time estimates
2. **Generic Mode** - General-purpose duration estimates
3. **Humorous Mode** - Absurd and exaggerated estimates (easter egg)

The application MUST:
- Provide user interface toggle between Work Mode and Generic Mode
- Keep Humorous Mode hidden (no UI control)
- Activate Humorous Mode automatically when shake duration exceeds 15 seconds
- Persist user's mode selection (Work/Generic) across app sessions

#### 3.2.2 Estimate Pools

The application MUST maintain the following estimate pools:

**Work Mode Estimates:**
- Gentle shake pool: "2 hours", "4 hours", "1 day", "2 days", "3 days", "5 days", "1 week"
- Hard shake pool: "15 minutes", "30 minutes", "1 hour", "2 hours", "1 day", "3 days", "1 week", "2 weeks", "1 month", "3 months", "6 months", "1 year"

**Generic Mode Estimates:**
- Gentle shake pool: "1 minute", "5 minutes", "10 minutes", "15 minutes", "30 minutes", "1 hour", "2 hours", "3 hours"
- Hard shake pool: "30 seconds", "1 minute", "5 minutes", "15 minutes", "30 minutes", "1 hour", "2 hours", "6 hours", "12 hours", "1 day", "3 days", "1 week", "2 weeks", "1 month"

**Humorous Mode Estimates:**
- "5 minutes", "tomorrow", "eventually", "next quarter", "when hell freezes over", "3 lifetimes", "Tuesday", "never", "your retirement"

The application MAY expand these pools in future versions.

#### 3.2.3 Estimate Selection Algorithm

The application MUST implement the following algorithm:

```
FUNCTION GenerateEstimate(intensity: double, duration: TimeSpan, mode: EstimateMode):
  1. IF duration > 15 seconds THEN
       SET mode = Humorous

  2. SELECT estimate_pool based on current mode

  3. CALCULATE range_bounds:
     IF intensity < 0.3 THEN
       range = first 20% of estimate_pool
     ELSE IF intensity < 0.7 THEN
       range = first 50% of estimate_pool
     ELSE
       range = entire estimate_pool

  4. SELECT random estimate from range using cryptographically secure RNG

  5. RETURN EstimateResult with:
       - Selected estimate text
       - Current timestamp
       - Active mode
       - Normalized intensity
       - Shake duration
```

The application MUST use cryptographically secure random number generation for estimate selection to ensure unpredictability.

### 3.3 History Management

#### 3.3.1 Storage Requirements

The application MUST:
- Persist the last 10 estimate results (REQUIRED minimum)
- Store each estimate with complete metadata: timestamp, estimate text, mode, intensity, duration
- Save estimates immediately after generation
- Load history on application startup
- Automatically prune oldest estimates when count exceeds maximum (10)

The application SHOULD:
- Store history in SQLite database using `sqlite-net-pcl` library
- Create timestamp index for efficient queries
- Perform all database operations asynchronously

The application MAY:
- Allow users to configure maximum history size (future enhancement)
- Export history to CSV or JSON format (future enhancement)

#### 3.3.2 Settings Persistence

The application MUST:
- Persist user's selected mode (Work/Generic) across sessions
- Load saved settings on application startup
- Use platform-native Preferences API for settings storage

### 3.4 User Interface

#### 3.4.1 Main Page

The application MUST display:
- Large shake detection zone occupying at least 80% of screen height
- Current estimate result with large, readable typography
- Mode toggle control labeled "Work Estimates ⟷ Generic Time"
- Navigation icons for Settings and History pages

The application MUST provide visual feedback:
- Subtle pulsing animation (scale 1.0 → 1.05 → 1.0) during active shake
- Smooth fade-in animation for estimate reveal (200-300ms duration)
- No visual timer or indicator for 15-second easter egg

The application MUST NOT:
- Display loading spinners or progress bars during shake
- Show shake intensity meter or real-time feedback
- Reveal the existence of Humorous Mode in UI

#### 3.4.2 History Page

The application MUST display for each history entry:
- Estimate text (large, bold typography)
- Relative timestamp (e.g., "2 minutes ago", "1 hour ago")
- Mode badge indicating Work/Generic/Humorous with color coding
- Visual intensity indicator (e.g., filled dots representing intensity level)

The application MUST:
- Display estimates in reverse chronological order (newest first)
- Show empty state message when history is empty: "No estimates yet. Give it a shake!"
- Support pull-to-refresh gesture for reloading

The application SHOULD:
- Limit visible history to 10 most recent entries
- Provide smooth scrolling for list navigation

#### 3.4.3 Settings Page

The application MUST provide:
- Mode toggle control (mirroring Main Page toggle)
- Application version information
- About section with attribution

The application MAY provide:
- Sensitivity adjustment controls (future enhancement)
- Maximum history size configuration (future enhancement)
- Reset/clear history button (future enhancement)

#### 3.4.4 Accessibility

The application MUST:
- Support full keyboard navigation on desktop platforms
- Provide VoiceOver/TalkBack descriptions for all interactive elements
- Maintain minimum touch target size of 44x44 points
- Meet WCAG 2.1 Level AA contrast ratio requirements (4.5:1 for normal text)

The application SHOULD:
- Support Dynamic Type on iOS
- Respect user's reduced motion preferences
- Provide alternative text for all icons

## 4. Architecture Requirements

### 4.1 Design Pattern

The application MUST implement Model-View-ViewModel (MVVM) pattern with strict separation:

- **Models** MUST be plain data objects with no business logic
- **ViewModels** MUST contain all presentation logic and be fully testable without UI
- **Views** MUST be thin, containing only data binding and minimal presentation code
- **Services** MUST encapsulate all business logic and infrastructure concerns

### 4.2 Dependency Injection

The application MUST:
- Use dependency injection for all service dependencies
- Register all services in `MauiProgram.cs` startup configuration
- Define service interfaces in `Services/Interfaces/` directory
- Inject dependencies via constructor injection

The application MUST NOT:
- Use service locator pattern
- Create service instances with `new` keyword in ViewModels
- Use static dependencies

### 4.3 Data Models

The application MUST define the following models:

```csharp
public class EstimateResult
{
    public Guid Id { get; set; }                      // REQUIRED
    public DateTimeOffset Timestamp { get; set; }     // REQUIRED
    public string EstimateText { get; set; }          // REQUIRED
    public EstimateMode Mode { get; set; }            // REQUIRED
    public double ShakeIntensity { get; set; }        // REQUIRED, range [0.0, 1.0]
    public TimeSpan ShakeDuration { get; set; }       // REQUIRED
}

public enum EstimateMode
{
    Work,      // REQUIRED
    Generic,   // REQUIRED
    Humorous   // REQUIRED
}

public class ShakeData
{
    public double Intensity { get; set; }    // REQUIRED, range [0.0, 1.0]
    public TimeSpan Duration { get; set; }   // REQUIRED
    public bool IsShaking { get; set; }      // REQUIRED
}

public class AppSettings
{
    public EstimateMode SelectedMode { get; set; }  // REQUIRED, default: Work
    public int MaxHistorySize { get; set; }         // REQUIRED, default: 10
}
```

### 4.4 Service Interfaces

The application MUST implement the following service contracts:

```csharp
public interface IAccelerometerService
{
    IObservable<AccelerometerData> DataStream { get; }  // REQUIRED
    void Start();                                        // REQUIRED
    void Stop();                                         // REQUIRED
}

public interface IShakeDetectionService
{
    IObservable<ShakeData> ShakeStream { get; }  // REQUIRED
    void StartMonitoring();                       // REQUIRED
    void StopMonitoring();                        // REQUIRED
}

public interface IEstimateService
{
    EstimateResult GenerateEstimate(              // REQUIRED
        double intensity,
        TimeSpan duration,
        EstimateMode mode
    );
}

public interface IStorageService
{
    Task SaveSettings(AppSettings settings);                    // REQUIRED
    Task<AppSettings> LoadSettings();                           // REQUIRED
    Task SaveEstimate(EstimateResult result);                   // REQUIRED
    Task<List<EstimateResult>> GetHistory(int count = 10);     // REQUIRED
    Task ClearHistory();                                        // REQUIRED
}
```

## 5. Testing Requirements

### 5.1 Code Coverage

The application MUST achieve minimum 95% code coverage across all projects.

The application MUST measure coverage using:
- Coverlet for .NET code coverage collection
- ReportGenerator for coverage report generation

The application MUST enforce coverage thresholds in CI/CD pipeline and MUST fail builds that fall below 95%.

### 5.2 Unit Testing

The application MUST provide unit tests for:
- All service implementations (`EstimateService`, `ShakeDetectionService`, `StorageService`)
- All ViewModels (`MainViewModel`, `SettingsViewModel`)
- All public methods in models (if containing logic)
- All estimate selection algorithm branches

The application MUST use:
- xUnit as testing framework
- NSubstitute for mocking dependencies
- FluentAssertions for assertion syntax

The application MUST:
- Mock all external dependencies (file system, database, sensors)
- Use test data builders for complex object creation
- Seed random number generators for deterministic tests
- Test all edge cases (null inputs, boundary values, invalid states)

### 5.3 Integration Testing

The application MUST provide integration tests for:
- Complete data flow from shake detection through estimate generation to storage
- ViewModel and multiple service interactions
- Database operations with actual SQLite instance

The application SHOULD:
- Use in-memory SQLite for faster test execution
- Parallelize integration tests where possible

### 5.4 UI Testing

The application SHOULD provide UI automation tests for:
- Critical user path: Shake → View estimate → Check history
- Mode toggle functionality
- History page display and navigation

The application MAY use Appium WebDriver for cross-platform UI testing.

## 6. Quality Requirements

### 6.1 Code Quality

The application MUST:
- Enable nullable reference types across all projects
- Treat all compiler warnings as errors
- Pass all enabled code analysis rules (StyleCop, built-in analyzers)
- Follow EditorConfig formatting rules

The application MUST NOT:
- Suppress warnings without documented justification
- Disable code analysis rules without review
- Commit code with compiler errors

### 6.2 Performance

The application MUST:
- Respond to shake detection within 100 milliseconds
- Display estimate result within 200 milliseconds after shake stop
- Load history page within 500 milliseconds
- Complete all database operations asynchronously

The application SHOULD:
- Minimize memory allocations during shake detection loop
- Cache estimate pools in memory
- Use lazy loading for history page

### 6.3 Error Handling

The application MUST:
- Handle sensor unavailability gracefully (show user-friendly message)
- Catch and log all exceptions in services
- Prevent crash from any single component failure
- Validate all user inputs

The application SHOULD:
- Implement retry logic for transient database errors
- Log errors to platform-specific logging systems

## 7. Platform-Specific Requirements

### 7.1 iOS Requirements

The application MUST:
- Target iOS 15.0 or higher
- Request motion sensor permissions via Info.plist `NSMotionUsageDescription`
- Pause sensor monitoring when app enters background
- Support iOS dark mode
- Follow iOS Human Interface Guidelines

The application SHOULD:
- Provide haptic feedback on shake detection
- Support iPad multitasking and split view

### 7.2 Web Requirements

The application MUST:
- Support modern browsers: Chrome 90+, Safari 14+, Firefox 88+, Edge 90+
- Implement responsive design for mobile and desktop viewports
- Function without server-side dependencies (static hosting)

The application SHOULD:
- Provide Progressive Web App (PWA) manifest for home screen install
- Cache application assets for offline functionality
- Support touch gestures on mobile browsers

The application MAY:
- Use Device Orientation API for mobile browser shake detection

### 7.3 macOS Requirements

The application MUST:
- Target macOS 12.0 (Monterey) or higher
- Follow macOS Human Interface Guidelines
- Support native window chrome and menu bar
- Implement keyboard shortcut Cmd+Shift+S for shake trigger

The application SHOULD:
- Support macOS dark mode and accent colors
- Integrate with macOS accessibility features

## 8. Security Requirements

The application MUST:
- Use cryptographically secure random number generation (System.Security.Cryptography.RandomNumberGenerator)
- Store all data locally on device (no cloud transmission)
- Request minimum required permissions

The application MUST NOT:
- Transmit any user data to external servers
- Store sensitive personal information
- Access device sensors without explicit permission

## 9. Build and Deployment Requirements

### 9.1 Build Configuration

The application MUST:
- Define separate build configurations for Debug, Release, and Testing
- Enable code optimization for Release builds
- Include debug symbols for all builds
- Version assemblies using semantic versioning (SemVer)

### 9.2 Continuous Integration

The application MUST:
- Execute automated builds on every commit
- Run all unit and integration tests in CI pipeline
- Enforce code coverage thresholds (≥95%)
- Generate coverage reports for each build

The application SHOULD:
- Use GitHub Actions for CI/CD automation
- Cache NuGet packages for faster builds
- Parallelize test execution

### 9.3 Release Process

The application MUST:
- Follow phased release approach: iOS → Web → macOS
- Increment version number for each release
- Tag releases in version control
- Generate release notes from commit history

## 10. Documentation Requirements

The application MUST provide:
- README.md with build instructions and prerequisites
- Architecture decision records (ADR) for significant technical choices
- API documentation for all public interfaces (XML comments)
- User guide covering all features

The application SHOULD provide:
- Contribution guidelines for external contributors
- Code style guide referencing EditorConfig rules
- Troubleshooting guide for common issues

## 11. Dependencies

### 11.1 Required Dependencies

The application MUST use the following NuGet packages:

- `Microsoft.Maui.Controls` (≥8.0.0, <9.0.0)
- `CommunityToolkit.Mvvm` (≥8.2.0)
- `sqlite-net-pcl` (≥1.9.0)
- `SQLitePCLRaw.bundle_green` (≥2.1.0)

### 11.2 Testing Dependencies

The application MUST use the following packages for testing:

- `xUnit` (≥2.6.0)
- `NSubstitute` (≥5.1.0)
- `FluentAssertions` (≥6.12.0)
- `Coverlet.Collector` (≥6.0.0)

### 11.3 Dependency Management

The application MUST:
- Pin major versions to prevent breaking changes
- Review and update dependencies quarterly
- Address security vulnerabilities within 30 days of disclosure

The application MUST NOT:
- Use pre-release or beta packages in production builds
- Include unnecessary dependencies

## 12. Compliance and Standards

### 12.1 Coding Standards

The application MUST adhere to:
- C# coding conventions (Microsoft official style guide)
- .NET naming conventions
- EditorConfig rules defined in repository

### 12.2 Accessibility Standards

The application MUST comply with:
- WCAG 2.1 Level AA
- Section 508 (U.S. accessibility requirements)
- iOS Accessibility Guidelines
- macOS Accessibility Guidelines

## 13. Future Enhancements

The following features MAY be implemented in future versions:

### 13.1 Statistics Dashboard
- Display aggregate usage statistics
- Track most common estimates
- Show humorous mode discovery rate
- Calculate average shake intensity

### 13.2 Customization
- Allow user-defined estimate pools
- Configurable sensitivity thresholds
- Custom color themes

### 13.3 Social Features
- Share estimates to clipboard
- Export history as text/image
- Team synchronization mode

### 13.4 Localization
- Finnish translation (fi-FI)
- Additional language support

## 14. Acceptance Criteria

For the application to be considered complete and ready for release, it MUST:

- ✅ Build successfully on all target platforms without errors
- ✅ Pass all automated tests with ≥95% code coverage
- ✅ Function correctly on iOS 15+ devices with accurate shake detection
- ✅ Persist settings and history across app restarts
- ✅ Display estimates within 200ms of shake completion
- ✅ Correctly implement easter egg (15-second humorous mode)
- ✅ Meet WCAG 2.1 Level AA accessibility standards
- ✅ Complete security review with no high/critical vulnerabilities
- ✅ Pass manual testing on representative devices
- ✅ Include complete user and developer documentation

## 15. Glossary

- **Shake Intensity:** Normalized measure of shake strength, range [0.0, 1.0]
- **Shake Duration:** Time elapsed from shake start to shake stop
- **Easter Egg:** Hidden humorous mode triggered by prolonged shaking (>15 seconds)
- **Estimate Pool:** Predefined collection of possible time estimates for a given mode
- **Gentle Shake:** Shake with intensity < 0.3
- **Hard Shake:** Shake with intensity ≥ 0.7
- **LTS:** Long-Term Support (.NET version with extended support period)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-18
**Status:** Draft - Pending Implementation
**Prepared by:** Claude Code
**Approved by:** [Pending]

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-18 | Claude Code | Initial specification document |

---

**END OF SPECIFICATION**
