# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hiha-Arvio (Finnish: "Sleeve Estimate") is a .NET 10 MAUI cross-platform application that generates semi-random time estimates based on physical shake input (accelerometer on mobile, mouse movement on desktop). This is a humor app for "pulling an estimate from your sleeve."

**Platforms (in priority order):** iOS (primary) → Web (Blazor) → macOS

## Critical Requirements

### Specification Compliance

- **ALWAYS read `spec.md` before implementing features** — contains RFC 2119 formal requirements and estimate pools (§3.2.2)
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

- Testing stack: xUnit + NSubstitute (mocking) + FluentAssertions (assertions)
- All tests MUST use deterministic randomness (seeded RNG)
- Mock all external dependencies (sensors, database, file system)
- Use test data builders for complex objects

## Technology Stack

- **Framework:** .NET 10 MAUI
- **Language:** C# 13 with nullable reference types
- **Database:** SQLite via `sqlite-net-pcl`
- **MVVM:** CommunityToolkit.Mvvm (source generators)
- **Testing:** xUnit, NSubstitute, FluentAssertions, Coverlet

## Project Structure

```
HihaArvio.sln
├── src/HihaArvio/
│   ├── Converters/              # Value converters (bool→color, null checks, etc.)
│   ├── Models/                  # Plain data objects
│   ├── Platforms/               # MAUI platform bootstrapping (iOS, macOS, Android, etc.)
│   ├── Services/
│   │   ├── Interfaces/          # Service contracts
│   │   └── Platform/            # Platform-specific implementations
│   ├── ViewModels/              # Presentation logic (CommunityToolkit.Mvvm)
│   ├── Properties/
│   ├── Resources/
│   ├── MainPage.xaml            # Primary UI — estimate display + shake status
│   ├── HistoryPage.xaml         # Estimate history list
│   ├── SettingsPage.xaml        # Mode selection + settings
│   ├── AppShell.xaml            # Tab navigation (Estimate, History, Settings)
│   ├── MauiProgram.cs           # DI registration + platform wiring
│   └── HihaArvio.csproj
├── tests/HihaArvio.Tests/       # Unit + integration tests (189 tests)
├── .github/workflows/           # CI: build.yml, test.yml, publish.yml
├── docs/plans/                  # Design documents
└── spec.md                      # Formal specification (RFC 2119)
```

## Development Commands

### Build
- **All platforms:** `dotnet build HihaArvio.sln`
- **Specific framework:** `dotnet build HihaArvio.sln -f net10.0`
- **iOS:** `dotnet build HihaArvio.sln -f net10.0-ios`
- **macOS:** `dotnet build HihaArvio.sln -f net10.0-maccatalyst`

### Test
- **Run all tests:** `dotnet test tests/HihaArvio.Tests/HihaArvio.Tests.csproj`
- **Filter by class:** `dotnet test tests/HihaArvio.Tests/HihaArvio.Tests.csproj --filter "FullyQualifiedName~EstimateModeTests"`
- **Single test:** `dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"`
- **With coverage:** `dotnet test tests/HihaArvio.Tests/HihaArvio.Tests.csproj --collect:"XPlat Code Coverage"`

### Run
- **iOS Simulator:** `dotnet build src/HihaArvio/HihaArvio.csproj -t:Run -f net10.0-ios`
- **macOS:** `dotnet build src/HihaArvio/HihaArvio.csproj -t:Run -f net10.0-maccatalyst`

### Prerequisites
- Xcode installed and configured (`xcode-select -p` → Xcode.app)
- MAUI workload installed (`dotnet workload list` should show `maui`)
- All commands run from repository root

## Critical Implementation Notes

### Easter Egg Behavior
- Hidden feature: NO UI indication
- Trigger: shake duration >15 seconds → temporarily forces EstimateMode.Humorous
- Do NOT expose in Settings or UI

### Shake Detection Algorithm
1. Monitor sensor stream (accelerometer or mouse)
2. Calculate magnitude/velocity, detect start when exceeding threshold
3. Track peak intensity during session
4. Detect end: below threshold for 500ms continuous
5. Normalize intensity to [0.0, 1.0]

### Performance Requirements
- Shake response: <100ms latency
- Estimate display: <200ms after shake stop
- History load: <500ms
- All database operations: async, non-blocking

### Security
- Use `System.Security.Cryptography.RandomNumberGenerator` for estimate selection
- No external data transmission — all data stored locally only
- Request minimum required permissions

## Code Quality

- Nullable reference types enabled across all projects
- Treat warnings as errors
- StyleCop + built-in analyzers enabled
- CI/CD enforces 95% coverage threshold

## Project Status: COMPLETE

All 6 milestones implemented. 189 tests passing, 0 warnings/errors across all platforms (net10.0, iOS, macOS Catalyst). See git history for milestone details.

### Future Enhancements
- Web/Blazor platform support (mouse movement tracking)
- Keyboard shortcut for shake trigger (Cmd+Shift+S on macOS)
- Haptic feedback on iOS shake detection
- PWA manifest for web version

# context-mode — MANDATORY routing rules

You have context-mode MCP tools available. These rules are NOT optional — they protect your context window from flooding. A single unrouted command can dump 56 KB into context and waste the entire session.

## BLOCKED commands — do NOT attempt these

### curl / wget — BLOCKED
Any Bash command containing `curl` or `wget` is intercepted and replaced with an error message. Do NOT retry.
Instead use:
- `ctx_fetch_and_index(url, source)` to fetch and index web pages
- `ctx_execute(language: "javascript", code: "const r = await fetch(...)")` to run HTTP calls in sandbox

### Inline HTTP — BLOCKED
Any Bash command containing `fetch('http`, `requests.get(`, `requests.post(`, `http.get(`, or `http.request(` is intercepted and replaced with an error message. Do NOT retry with Bash.
Instead use:
- `ctx_execute(language, code)` to run HTTP calls in sandbox — only stdout enters context

### WebFetch — BLOCKED
WebFetch calls are denied entirely. The URL is extracted and you are told to use `ctx_fetch_and_index` instead.
Instead use:
- `ctx_fetch_and_index(url, source)` then `ctx_search(queries)` to query the indexed content

## REDIRECTED tools — use sandbox equivalents

### Bash (>20 lines output)
Bash is ONLY for: `git`, `mkdir`, `rm`, `mv`, `cd`, `ls`, `npm install`, `pip install`, and other short-output commands.
For everything else, use:
- `ctx_batch_execute(commands, queries)` — run multiple commands + search in ONE call
- `ctx_execute(language: "shell", code: "...")` — run in sandbox, only stdout enters context

### Read (for analysis)
If you are reading a file to **Edit** it → Read is correct (Edit needs content in context).
If you are reading to **analyze, explore, or summarize** → use `ctx_execute_file(path, language, code)` instead. Only your printed summary enters context. The raw file content stays in the sandbox.

### Grep (large results)
Grep results can flood context. Use `ctx_execute(language: "shell", code: "grep ...")` to run searches in sandbox. Only your printed summary enters context.

## Tool selection hierarchy

1. **GATHER**: `ctx_batch_execute(commands, queries)` — Primary tool. Runs all commands, auto-indexes output, returns search results. ONE call replaces 30+ individual calls.
2. **FOLLOW-UP**: `ctx_search(queries: ["q1", "q2", ...])` — Query indexed content. Pass ALL questions as array in ONE call.
3. **PROCESSING**: `ctx_execute(language, code)` | `ctx_execute_file(path, language, code)` — Sandbox execution. Only stdout enters context.
4. **WEB**: `ctx_fetch_and_index(url, source)` then `ctx_search(queries)` — Fetch, chunk, index, query. Raw HTML never enters context.
5. **INDEX**: `ctx_index(content, source)` — Store content in FTS5 knowledge base for later search.

## Subagent routing

When spawning subagents (Agent/Task tool), the routing block is automatically injected into their prompt. Bash-type subagents are upgraded to general-purpose so they have access to MCP tools. You do NOT need to manually instruct subagents about context-mode.

## Output constraints

- Keep responses under 500 words.
- Write artifacts (code, configs, PRDs) to FILES — never return them as inline text. Return only: file path + 1-line description.
- When indexing content, use descriptive source labels so others can `ctx_search(source: "label")` later.

## ctx commands

| Command       | Action                                                                                |
|---------------|---------------------------------------------------------------------------------------|
| `ctx stats`   | Call the `ctx_stats` MCP tool and display the full output verbatim                    |
| `ctx doctor`  | Call the `ctx_doctor` MCP tool, run the returned shell command, display as checklist  |
| `ctx upgrade` | Call the `ctx_upgrade` MCP tool, run the returned shell command, display as checklist |
