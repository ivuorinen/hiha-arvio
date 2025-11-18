# GitHub Workflows

This directory contains the CI/CD workflows for HihaArvio.

## Workflows

### 🧪 Test (`test.yml`)

**Trigger**: Push or PR to `main` or `develop` branches

**Purpose**: Run all unit and integration tests to ensure code quality.

**What it does**:
- Runs on Ubuntu (fastest, cheapest for tests)
- Sets up .NET 8.0
- Builds the solution for `net8.0` target
- Runs all 189 xUnit tests
- Publishes test results and coverage reports
- Uploads test artifacts for review

**Status Badge**:
```markdown
[![Test](https://github.com/ivuorinen/hiha-arvio/actions/workflows/test.yml/badge.svg)](https://github.com/ivuorinen/hiha-arvio/actions/workflows/test.yml)
```

---

### 🏗️ Build (`build.yml`)

**Trigger**: Push or PR to `main` or `develop` branches, or manual trigger

**Purpose**: Build the application for all supported platforms.

**What it does**:
- **iOS Job**: Builds for iOS 15.0+ on macOS-14 runner
- **macOS Catalyst Job**: Builds for macOS 12.0+ on macOS-14 runner
- Installs .NET MAUI workload
- Builds without code signing (for CI verification)
- Uploads build artifacts (`.app` bundles)
- Reports overall build status

**Platforms**:
- iOS (iossimulator-arm64)
- macOS Catalyst (maccatalyst-arm64)

**Status Badge**:
```markdown
[![Build](https://github.com/ivuorinen/hiha-arvio/actions/workflows/build.yml/badge.svg)](https://github.com/ivuorinen/hiha-arvio/actions/workflows/build.yml)
```

---

### 🚀 Publish (`publish.yml`)

**Trigger**:
- Push of version tags (e.g., `v1.0.0`, `v1.2.3-beta`)
- Manual workflow dispatch with version input

**Purpose**: Create GitHub releases with signed and distributable builds.

**What it does**:
1. **Create Release Job**:
   - Extracts version from tag or input
   - Generates changelog from git commits
   - Creates GitHub release (draft for pre-releases)

2. **Build iOS Job**:
   - Builds release version for iOS
   - Sets version numbers from tag/input
   - Creates `.zip` archive of `.app` bundle
   - Uploads to GitHub release

3. **Build macOS Job**:
   - Builds release version for macOS Catalyst
   - Sets version numbers from tag/input
   - Creates `.zip` archive of `.app` bundle
   - Uploads to GitHub release

4. **Status Job**:
   - Reports overall publish status
   - Creates summary in GitHub Actions UI

**Version Numbering**:
- `ApplicationDisplayVersion`: From git tag (e.g., `1.0.0`)
- `ApplicationVersion`: From GitHub run number (incremental build number)

**Pre-release Detection**:
Tags containing `alpha`, `beta`, or `rc` are marked as pre-releases.

**Status Badge**:
```markdown
[![Publish](https://github.com/ivuorinen/hiha-arvio/actions/workflows/publish.yml/badge.svg)](https://github.com/ivuorinen/hiha-arvio/actions/workflows/publish.yml)
```

---

## How to Use

### Running Tests Locally
```bash
dotnet test HihaArvio.sln -f net8.0
```

### Building Locally
```bash
# iOS
dotnet build src/HihaArvio/HihaArvio.csproj -f net8.0-ios -c Release

# macOS Catalyst
dotnet build src/HihaArvio/HihaArvio.csproj -f net8.0-maccatalyst -c Release
```

### Creating a Release

#### Automatic (via Git Tag)
```bash
# Create and push version tag
git tag v1.0.0
git push origin v1.0.0
```

#### Manual (via GitHub UI)
1. Go to Actions → Publish workflow
2. Click "Run workflow"
3. Enter version number (e.g., `1.0.0`)
4. Click "Run workflow"

The publish workflow will:
1. Create a GitHub release
2. Build iOS and macOS versions
3. Attach `.zip` artifacts to the release
4. Generate changelog from commits

---

## Workflow Dependencies

### Required GitHub Secrets
None required for current workflows (unsigned builds).

For signed releases, add:
- `APPLE_CERTIFICATE_BASE64`: iOS/macOS signing certificate
- `APPLE_CERTIFICATE_PASSWORD`: Certificate password
- `APPLE_PROVISIONING_PROFILE_BASE64`: Provisioning profile
- `APPLE_TEAM_ID`: Apple Developer Team ID

### Runner Requirements
- **Test**: `ubuntu-latest` (any Linux runner)
- **Build**: `macos-14` (Apple Silicon runner for MAUI workloads)
- **Publish**: `macos-14` (Apple Silicon runner for MAUI workloads)

### External Actions Used
- `actions/checkout@v4` - Checkout repository
- `actions/setup-dotnet@v4` - Setup .NET SDK
- `actions/upload-artifact@v4` - Upload build artifacts
- `actions/create-release@v1` - Create GitHub releases
- `actions/upload-release-asset@v1` - Upload release assets
- `dorny/test-reporter@v1` - Generate test reports

---

## Troubleshooting

### Tests Failing
Check test output in workflow logs. Run locally with:
```bash
dotnet test HihaArvio.sln -f net8.0 --verbosity detailed
```

### Build Failing
1. Check runner logs for specific errors
2. Verify .NET MAUI workload installed correctly
3. Ensure all NuGet packages are restored
4. Check for platform-specific compilation errors

### Publish Failing
1. Verify version tag format matches `v*.*.*`
2. Check that create-release step succeeded
3. Verify build artifacts were created
4. Check upload permissions and GitHub token

### Manual Workflow Trigger Not Working
- Ensure you have write permissions to the repository
- Check workflow YAML syntax is valid
- Verify `workflow_dispatch` event is properly configured

---

## Workflow Optimization

### Cost Considerations
- **Ubuntu runners**: $0.008/minute (cheapest)
- **macOS runners**: $0.08/minute (10x more expensive)

**Strategy**:
- Tests run on Ubuntu (fast, cheap)
- Builds run on macOS only when needed (platform requirement)
- Publish only on tagged releases (infrequent)

### Performance Tips
- Build jobs run in parallel (iOS and macOS simultaneously)
- Use `--no-restore` and `--no-build` flags to skip redundant steps
- Cache NuGet packages for faster restores
- Use artifacts for passing builds between jobs

---

## Future Enhancements

### Potential Additions
- [ ] Code coverage reporting (Codecov/Coveralls)
- [ ] Static code analysis (SonarCloud)
- [ ] Dependency scanning (Dependabot)
- [ ] Performance benchmarking
- [ ] TestFlight deployment for iOS
- [ ] Mac App Store submission automation
- [ ] Code signing for official releases
- [ ] Notarization for macOS builds

### Workflow Improvements
- [ ] Add caching for NuGet packages
- [ ] Matrix builds for multiple .NET versions
- [ ] Parallel test execution
- [ ] Automatic changelog generation from conventional commits
- [ ] Semantic versioning automation
- [ ] Slack/Discord notifications on release

---

## Maintenance

### Updating Workflows
1. Edit workflow YAML files
2. Test changes on feature branch
3. Verify workflows run successfully
4. Merge to main branch

### Monitoring
- Check Actions tab regularly for failures
- Review test reports for flaky tests
- Monitor build times for optimization opportunities
- Track artifact sizes for distribution planning

---

📝 **Note**: These workflows are designed for unsigned development builds. For App Store distribution, additional code signing configuration is required.
