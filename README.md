# HihaArvio (Sleeve Estimate)

[![Test](https://github.com/ivuorinen/hiha-arvio/actions/workflows/test.yml/badge.svg)](https://github.com/ivuorinen/hiha-arvio/actions/workflows/test.yml)
[![Build](https://github.com/ivuorinen/hiha-arvio/actions/workflows/build.yml/badge.svg)](https://github.com/ivuorinen/hiha-arvio/actions/workflows/build.yml)
[![Publish](https://github.com/ivuorinen/hiha-arvio/actions/workflows/publish.yml/badge.svg)](https://github.com/ivuorinen/hiha-arvio/actions/workflows/publish.yml)

A playful Finnish take on agile estimation through shake gestures. Instead of pointing, just shake your device and get a hilariously honest estimate pulled straight from your sleeve (hiha).

## 🎯 What is HihaArvio?

"Hiha-arvio" literally means "sleeve estimate" in Finnish - those quick, informal estimates you pull out of thin air (or your sleeve) when someone asks "how long will this take?" This app embraces that chaos with a gesture-based estimation tool that responds to how vigorously you shake your device.

**Shake lightly**: Conservative estimates (minutes to hours)
**Shake vigorously**: Ambitious estimates (days to weeks)
**Shake like your life depends on it**: Comedy gold ("when hell freezes over", "3 lifetimes", "never")

## ✨ Features

- **Shake-Based Estimation**: Physical gesture drives estimate generation
- **Multiple Modes**:
  - **Work**: Professional estimates (minutes, hours, days, weeks)
  - **Generic**: General time frames
  - **Humorous**: For when reality needs a laugh track
- **Estimation History**: Track all your sleeve estimates with timestamps
- **Persistent Settings**: Your preferred mode and history size saved locally
- **Cross-Platform**: iOS and macOS (via Catalyst) native apps
- **Offline-First**: No internet required, all data stored locally

## 🏗️ Architecture

Built with **.NET 8 MAUI** using modern C# 12 and strict MVVM architecture:

- **Models**: Core domain models (EstimateResult, ShakeData, AppSettings)
- **Services**: Business logic layer
  - `EstimateService`: Generates estimates based on shake intensity
  - `ShakeDetectionService`: Processes accelerometer data
  - `StorageService`: SQLite persistence
  - `AccelerometerService`: Platform-specific sensor access
- **ViewModels**: Presentation layer with CommunityToolkit.Mvvm
- **Views**: XAML-based UI with data binding

### Technology Stack

- **.NET 8.0** with C# 12
- **MAUI** for cross-platform UI
- **CommunityToolkit.Mvvm** for MVVM patterns
- **SQLite** (sqlite-net-pcl) for local storage
- **xUnit + FluentAssertions + NSubstitute** for testing

## 🧪 Testing

**189 tests** covering all layers:
- **48 tests**: Models layer
- **71 tests**: Services layer
- **46 tests**: ViewModels layer
- **24 tests**: Platform-specific accelerometer implementations

```bash
# Run all tests
dotnet test HihaArvio.sln -f net8.0

# Run with detailed output
dotnet test HihaArvio.sln -f net8.0 --verbosity detailed

# Generate coverage report
dotnet test HihaArvio.sln -f net8.0 --collect:"XPlat Code Coverage"
```

## 🚀 Getting Started

### Prerequisites

- **.NET 8 SDK**: [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **MAUI Workload**: Install via `dotnet workload install maui`
- **Xcode 15+**: Required for iOS/macOS builds (macOS only)
- **Visual Studio 2022** or **VS Code** with C# extension

### Installation

```bash
# Clone the repository
git clone https://github.com/ivuorinen/hiha-arvio.git
cd hiha-arvio

# Restore dependencies
dotnet restore HihaArvio.sln

# Build the solution
dotnet build HihaArvio.sln -c Release

# Run tests
dotnet test HihaArvio.sln -f net8.0
```

### Running the App

```bash
# iOS Simulator
dotnet build src/HihaArvio/HihaArvio.csproj -f net8.0-ios -c Debug
# Then deploy via Xcode or Visual Studio

# macOS Catalyst
dotnet build src/HihaArvio/HihaArvio.csproj -f net8.0-maccatalyst -c Debug
# Then run the .app bundle from bin/Debug/net8.0-maccatalyst/
```

## 📦 CI/CD Workflows

Three automated workflows handle testing, building, and publishing:

### 🧪 Test Workflow
- Runs on every push/PR to `main` or `develop`
- Executes all 189 tests on Ubuntu runner
- Publishes test results and artifacts

### 🏗️ Build Workflow
- Builds for iOS and macOS Catalyst in parallel
- Runs on macOS-14 runners (Apple Silicon)
- Uploads build artifacts for review

### 🚀 Publish Workflow
- Triggered by version tags (`v1.0.0`, `v1.2.3-beta`)
- Creates GitHub releases with changelogs
- Builds and publishes iOS and macOS distributions
- Supports manual workflow dispatch

See [.github/workflows/README.md](.github/workflows/README.md) for detailed documentation.

## 📱 Supported Platforms

- **iOS**: 15.0+ (iPhone and iPad)
- **macOS**: 12.0+ (Monterey and later via Mac Catalyst)

Future platforms:
- **Web/Blazor**: Planned
- **Android**: Possible with MAUI support

## 🎮 How to Use

1. **Launch the app** on iOS or macOS
2. **Shake your device**:
   - Gentle shake → Conservative estimate
   - Vigorous shake → Ambitious estimate
   - Crazy shake → Comedic estimate
3. **View your estimate** displayed prominently
4. **Check history** to review past sleeve estimates
5. **Adjust settings** to change estimation mode or history size

## 🧬 Project Structure

```
hiha-arvio/
├── src/
│   └── HihaArvio/                 # Main application
│       ├── Models/                # Domain models
│       ├── Services/              # Business logic
│       │   ├── Interfaces/       # Service contracts
│       │   └── Platform/         # Platform-specific implementations
│       ├── ViewModels/           # Presentation layer
│       ├── Views/                # XAML UI
│       ├── Converters/           # Value converters
│       └── Resources/            # Images, fonts, assets
├── tests/
│   └── HihaArvio.Tests/          # All tests
│       ├── Models/               # Model tests
│       ├── Services/             # Service tests
│       ├── ViewModels/           # ViewModel tests
│       └── Integration/          # Integration tests
├── .github/
│   └── workflows/                # CI/CD workflows
└── CLAUDE.md                     # Development documentation
```

## 🔧 Development

### Code Quality Standards

- ✅ **0 warnings, 0 errors** policy enforced
- ✅ **TDD approach** for all features
- ✅ **MVVM architecture** strictly followed
- ✅ **Dependency injection** throughout
- ✅ **Nullable reference types** enabled
- ✅ **C# 12** with modern language features

### Building for Release

```bash
# iOS
dotnet publish src/HihaArvio/HihaArvio.csproj \
  -f net8.0-ios \
  -c Release \
  /p:ArchiveOnBuild=true

# macOS Catalyst
dotnet publish src/HihaArvio/HihaArvio.csproj \
  -f net8.0-maccatalyst \
  -c Release \
  /p:ArchiveOnBuild=true
```

### Creating a Release

```bash
# Tag the release
git tag v1.0.0
git push origin v1.0.0

# GitHub Actions will automatically:
# 1. Create a GitHub release
# 2. Build iOS and macOS versions
# 3. Upload distribution artifacts
```

## 📚 Documentation

- **[CLAUDE.md](CLAUDE.md)**: Detailed development documentation
- **[.github/workflows/README.md](.github/workflows/README.md)**: CI/CD workflow documentation
- **Code Comments**: Inline documentation throughout the codebase

## 🤝 Contributing

This is a personal project demonstrating TDD and MVVM patterns with .NET MAUI. Contributions are welcome!

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests first (TDD approach)
4. Implement the feature
5. Ensure all tests pass and no warnings
6. Commit your changes (`git commit -m 'feat: add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [.NET MAUI](https://dotnet.microsoft.com/apps/maui)
- MVVM powered by [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)
- Testing with [xUnit](https://xunit.net/), [FluentAssertions](https://fluentassertions.com/), and [NSubstitute](https://nsubstitute.github.io/)

## 🎭 Why "HihaArvio"?

In Finnish software development culture, "hiha-arvio" (sleeve estimate) is a tongue-in-cheek term for those quick estimates developers give when pressed for a timeline. This app celebrates that tradition by literally pulling estimates out of thin air - or rather, from the intensity of your shake gesture.

Perfect for:
- Sprint planning with a sense of humor
- Teaching the chaos of estimation
- Demonstrating .NET MAUI capabilities
- Having fun with accelerometer APIs
- Embracing Finnish developer culture

---

**Bundle ID**: `net.ivuorinen.HihaArvio`
**Version**: 1.0
**Platforms**: iOS 15.0+, macOS 12.0+
