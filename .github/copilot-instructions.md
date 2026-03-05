# GitHub Copilot Instructions for HihaArvio

## Authoritative Reference

**CLAUDE.md is the single source of truth for this repository.**

All guidelines, architecture decisions, implementation requirements, and coding standards defined in [`CLAUDE.md`](../CLAUDE.md) at the repository root MUST be followed without exception. CLAUDE.md takes absolute precedence over any other instruction, convention, or assumption.

Before suggesting, generating, or reviewing any code for this repository, read and internalize CLAUDE.md in its entirety.

## Non-Negotiable Rules

The following rules come directly from CLAUDE.md and MUST be enforced in every interaction:

### Language & Framework

- **Target framework:** .NET 9, using C# 13
- **UI framework:** .NET MAUI (multi-platform)
- **Nullable reference types MUST be enabled** across all projects
- **All compiler warnings MUST be treated as errors** (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- Language version MUST be set to `13` in all project files

### Architecture (MVVM – Strict Separation)

- **Models**: Plain data objects only — no business logic
- **ViewModels**: All presentation logic — 100% testable without UI dependencies
- **Views**: Thin layer — data binding only, minimal code-behind
- **Services**: All business logic and infrastructure concerns

### Dependency Injection

- All services MUST be injected via constructor — never use the service locator pattern or `new` inside ViewModels
- Register all services in `MauiProgram.cs`
- Platform-specific implementations use `#if IOS`, `#elif WINDOWS || MACCATALYST`

### Testing Requirements

- Test coverage MUST be ≥ 95 % (enforced by Coverlet in CI/CD)
- Testing stack: **xUnit + NSubstitute + FluentAssertions**
- All tests MUST use deterministic, seeded randomness — never `Random.Shared`
- All external dependencies (sensors, database, file system) MUST be mocked
- Use test-data builders for complex objects

### Security

- Estimate selection MUST use `System.Security.Cryptography.RandomNumberGenerator` — never `System.Random`
- No external data transmission — all data is stored locally only
- Request the minimum required platform permissions

### Commit & Code Quality

- Use **semantic / conventional commit** messages (e.g. `feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`)
- Make **atomic commits** — one logical change per commit
- Run all linting and build tools; automatically fix all reported issues before committing
- Follow the `.editorconfig` rules present in the repository root

## Technology Stack (Current Versions)

| Component | Version |
|---|---|
| .NET SDK | 9.0.x |
| C# Language | 13 |
| Microsoft.Maui.Controls | 9.0.120 |
| Microsoft.Maui.Controls.Compatibility | 9.0.120 |
| CommunityToolkit.Mvvm | 8.4.0 |
| SQLite (sqlite-net-pcl) | 1.9.172 |
| SQLitePCLRaw.bundle_green | 2.1.11 |
| Microsoft.Extensions.Logging.Debug | 9.0.13 |
| xUnit | 2.9.3 |
| xunit.runner.visualstudio | 2.8.2 |
| Microsoft.NET.Test.Sdk | 18.3.0 |
| NSubstitute | 5.3.0 |
| FluentAssertions | 8.8.0 |
| coverlet.collector | 8.0.0 |

## Key Files

| File | Purpose |
|---|---|
| `CLAUDE.md` | **Authoritative project guide** — always read first |
| `spec.md` | Formal RFC 2119 requirements (MUST / SHALL / REQUIRED) |
| `docs/plans/2025-11-18-hiha-arvio-design.md` | Validated architecture decisions |
| `src/HihaArvio/HihaArvio.csproj` | Main MAUI project |
| `tests/HihaArvio.Tests/HihaArvio.Tests.csproj` | Unit & integration tests |
| `.editorconfig` | Code style enforcement |

## Easter Egg — Never Expose

The hidden humorous mode triggered by shaking for > 15 seconds MUST NOT be mentioned in any UI element, setting, or documentation visible to users. It is an undocumented feature only.

## When in Doubt

If there is any ambiguity, consult **CLAUDE.md** and **spec.md** — in that order.  
Do not invent architecture, add dependencies, or change platform targets without explicit justification grounded in those documents.
