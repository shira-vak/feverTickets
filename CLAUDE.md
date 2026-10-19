# CLAUDE.md

Source of truth for all code generation in this project.
**Priority: CLAUDE.md → project structure → .NET / C# conventions.**

---

## Before Generating Code

1. Does this conflict with CLAUDE.md?
2. Are all types and nullability explicit and correct?
3. Is the solution simpler than what I first thought of?
4. Would a comment be needed to understand this — and if so, can I rename instead?
5. Am I adding a layer, interface, package, or pattern this script does not actually need?

---

## Project

A small C# console script run by GitHub Actions, triggered every 10 minutes by cron-job.org. It checks a ticketing page for availability and sends a notification when the requested tickets can be bought.

**This is a script, not an application.** Keep it small, readable, and direct. Senior-level code here means clean and correct — not enterprise architecture.

**This project is also C# practice for a TypeScript developer.** Unlike the general "no comments" rule below, add short comments that explain C# syntax or concepts a TS developer may not know (records, `using`, `out`, LINQ, pattern matching, primary constructors…), ideally with the TS equivalent. Keep them to one or two lines.

### Structure

```
src/FeverTickets/
├── Program.cs              ← entry point: wires everything, exit codes, the only catch-all
├── Constants.cs            ← every fixed value: env var names, defaults, formats, URLs, exit codes
├── Settings.cs             ← reads env vars (+ `.env` for local runs) into a typed record, validated once
├── TicketPageParser.cs     ← page HTML → TicketSession list (pure)
├── AvailabilityChecker.cs  ← sessions + targets → ZoneStatus list (pure)
├── NotificationBuilder.cs  ← statuses → NtfyMessage list (pure)
└── NtfyClient.cs           ← NtfyMessage + sends it to ntfy.sh
tests/FeverTickets.Tests/   ← xUnit tests for the pure parts + Fixtures/
.github/workflows/check-tickets.yml   ← workflow_dispatch trigger, heartbeat input, failure alert
```

- **Scheduling is external (cron-job.org → GitHub API `workflow_dispatch`).** The script never decides when to run, never stops itself, and has no end date. Do not add a `schedule:` trigger — GitHub's scheduler proved unreliable and would cause duplicate runs.
- **Failure alerts belong to the workflow** (`if: failure()` step), so build errors are covered too. The script just exits non-zero.

## Tech Stack

- **Runtime:** .NET (current LTS), C# latest language version
- **App type:** Console app (top-level `Program.cs`)
- **HTTP:** `HttpClient` from the BCL
- **JSON:** `System.Text.Json`
- **Tests:** xUnit
- **CI / scheduling:** GitHub Actions (`.github/workflows/`)

Prefer the BCL. Add a NuGet package only when the BCL genuinely cannot do the job reasonably (e.g. HTML parsing, SMTP with modern auth).

## Development Commands

```bash
dotnet build                # build everything (warnings are errors)
dotnet run --project src/FeverTickets   # run the script locally (reads .env)
dotnet test                 # run tests
dotnet format               # apply .editorconfig formatting
dotnet format --verify-no-changes   # what CI checks
```

---

## Project Settings

Every `.csproj` (or a shared `Directory.Build.props`) must have:

```xml
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<AnalysisLevel>latest-recommended</AnalysisLevel>
```

- Fix warnings — never suppress them with `#pragma` or `<NoWarn>` unless there is a written reason next to it.
- Formatting and style rules live in `.editorconfig`. Do not fight it.

---

## C# Language Rules

### Types & nullability

- Nullable reference types are on. A `?` means "can genuinely be null" — handle it. No `?` means it is never null.
- **No null-forgiving operator (`!`)** to silence the compiler. Fix the flow instead (guard clause, pattern match, `?? throw`).
- No `dynamic`. No `object` as a catch-all type. When parsing unknown data, map it into a typed record as early as possible.
- Use `var` when the type is obvious from the right-hand side (`var client = new HttpClient();`). Write the type explicitly when it is not obvious (`TimeSpan timeout = GetTimeout();`).
- Method signatures always have explicit parameter and return types (C# enforces this — just don't hide meaning behind `Task<object>` or tuples with unnamed elements).

### Records, classes, enums

- Use `record` (or `record struct` for tiny value types) for data: config, DTOs, parse results. They are immutable by default — keep them that way (`init`, not `set`).
- Use `sealed class` for types with behavior. Only leave a class unsealed when inheritance is intended.
- Use primary constructors for simple dependency/field injection.
- Enums are fine in C# for closed sets of states (`TicketStatus.Available | SoldOut | NotFound`). Do not use magic strings or ints for state.
- Prefer `switch` expressions with pattern matching over `if/else` chains when mapping values.

### Async

- Every I/O call is `async`. Never use `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`.
- Async methods end with `Async` (`FetchPageAsync`).
- Bound every request with `HttpClient.Timeout` (plus the job's `timeout-minutes`). In this script that is enough — no `CancellationToken` plumbing.
- No `async void`. No fire-and-forget tasks.

### Dates & times

- Use `DateTimeOffset` (or `DateOnly` / `TimeOnly` when only one part matters). Never `DateTime.Now`.
- Be explicit about time zones when comparing event times — parse the offset from the source data, do not assume the runner's local zone (GitHub runners are UTC).

### Strings

- String interpolation over concatenation or `string.Format`.
- Comparisons specify intent: `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`. Never compare with `ToLower()`.
- Raw string literals (`"""`) for multi-line text.

### Collections & LINQ

- Expose `IReadOnlyList<T>` / `IReadOnlyCollection<T>` from methods, not `List<T>`.
- LINQ is welcome when it reads clearly. Switch to a `foreach` when the query becomes nested or hard to follow.
- Use collection expressions (`[]`, `[a, b]`) where supported.

### HttpClient

- One `HttpClient` instance for the whole run. Never create one per request.
- Set a timeout and a realistic `User-Agent`.
- Call `EnsureSuccessStatusCode()` (or check the status explicitly and report it) — never parse an error page as if it were real data.

---

## Clean Code

- Clear names over comments. If code needs a comment to be understood, rename first.
- Only write a comment when the business reason genuinely cannot be expressed in code (e.g. why a specific JSON key is used). No decorative section-divider comments.
- One method = one responsibility. One class per file, file named after the class.
- Small data records and enums live in the file of the class that produces them (e.g. `TicketSession` in `TicketPageParser.cs`). Don't create a file just for a record.
- Prefer fewer files: only add a new one for a genuinely separate responsibility.
- File-scoped namespaces (`namespace FeverTickets;`).
- Guard clauses and early returns over deep nesting.
- No over-engineering. No premature abstractions.
  - **No DI container, no Generic Host, no `ILogger` plumbing** — this is a script. Construct objects directly in `Program.cs`.
  - **No interface for a class with one implementation**, unless a test genuinely needs the seam. Prefer making the logic pure and testing it directly.
  - No repository / service / manager layers. A few well-named classes are enough.
- Prefer simple, predictable code over clever code.
- **Method length:** keep methods short. If one passes ~40 lines, extract named helpers. Hard limit 100 lines.
- Keep parsing (pure: input string → typed result) separate from I/O (HTTP, sending messages). Pure code is what gets tested.
- `static` methods for pure helpers that use no state. No static mutable state.

### Naming

| Item | Convention | Example |
| --- | --- | --- |
| Classes, records, enums, methods, properties | `PascalCase` | `TicketChecker`, `CheckAsync` |
| Interfaces | `I` + `PascalCase` | `INotifier` |
| Parameters, locals | `camelCase` | `sessionDate` |
| Private fields | `_camelCase` | `_httpClient` |
| Constants | `PascalCase` | `DefaultTimeoutSeconds` |
| Async methods | suffix `Async` | `SendAsync` |
| Booleans | `is` / `has` / `can` prefix | `hasAvailableTickets` |

---

## No Magic Values

Never use inline literals whose meaning is not obvious from context. Every value that carries domain meaning is named and lives in one place.

| Value type | Where it lives |
| --- | --- |
| Fixed value (env var names, defaults, formats, URLs, page markers, timeouts, exit codes) | `Constants.cs`, in the nested class for its topic (`Constants.Defaults`, `Constants.Http`…). Files use `using static FeverTickets.Constants;` to shorten access |
| Shared helper function | `Utils.cs` — **only** once a helper is used by two or more files. A helper used by one class stays a `private static` method in that class |
| Things the user may change (target date, time, zone, URL) | Configuration — environment variables set in the workflow file, read once into a typed settings `record` |
| Secrets (passwords, tokens, API keys) | **GitHub Secrets only**, read from environment variables. Never in code, config files, logs, or commit history |

- Read and validate all configuration once at startup in one place. Fail fast with a clear message naming the missing/invalid variable.
- A value defined once must not be duplicated. Reference it.
- Never log secret values — not even partially.

---

## Error Handling

- Let exceptions propagate to `Program.cs`. Catch only where you can do something meaningful (retry, add context, translate to a clear result).
- Never swallow exceptions (`catch { }`). Never catch `Exception` except at the top level in `Program.cs`.
- Throw specific exceptions with context: what was expected, what was found (`InvalidOperationException($"Zone '{zone}' not found for {date} {time}. Found: {string.Join(", ", zones)}")`).
- Distinguish **"tickets not available"** (a normal result, exit code `0`) from **"the check itself failed"** (page changed, network error, bad config → non-zero exit code so the GitHub Action run shows red).
- Exit codes are named constants, not literal numbers.

---

## Output / Logging

- Plain `Console.WriteLine` is the logger. GitHub Actions captures stdout.
- Every run prints a short, useful summary: what was checked, what was found, whether a notification was sent.
- Errors go to `Console.Error`.

---

## Testing

### What to test

- Pure logic: parsing the page/JSON, finding the target session, deciding availability.
- Each meaningful case: available, sold out, target date/time/zone missing, page structure changed.
- Do **not** test `HttpClient`, SMTP, or other framework/third-party code. Do not mock what you can avoid by keeping logic pure.

### Naming

```
public sealed class <ClassUnderTest>Tests
    [Fact] public void <Method>_When<Condition>_Should<Result>()
```

Use `[Theory]` + `[InlineData]` / `[MemberData]` for parameterised cases.

### Fixtures & constants

- Saved real HTML/JSON samples live in `tests/FeverTickets.Tests/Fixtures/` and are copied to output.
- Test method names use underscores (`Method_WhenX_ShouldY`); CA1707 is disabled for `tests/` in `.editorconfig` for this reason.
- No magic values in tests — IDs, dates, zone names are named constants shared across the test file (or a `TestConstants.cs`). Derive related values instead of writing independent literals that can drift.

---

## GitHub Actions

- Workflow files live in `.github/workflows/`. Keep them short and readable.
- Pin action versions (`actions/checkout@v4`), never `@main`.
- Secrets come from `${{ secrets.NAME }}` mapped into `env:` — never echoed.
- Give each job a `timeout-minutes` so a hung run does not burn minutes.
- `workflow_dispatch` is the only trigger: used by cron-job.org and by the manual "Run workflow" button.
- GitHub `schedule` runs are best-effort and can be delayed or skipped for hours — that is why this project does not use them.

---

## Errors, Warnings & Code Quality

- `dotnet build` passes with zero warnings (warnings are errors).
- `dotnet format --verify-no-changes` passes.
- `dotnet test` passes.
- No unused usings, variables, or parameters.
- Verify the script actually runs end to end before considering a task complete.

---

## Formatting

- Enforced by `.editorconfig` + `dotnet format`.
- 4-space indentation, Allman braces (C# default).
- Always use braces, even for single-line `if`.
- Max line length: 120.
- Trailing commas in multi-line initializers and collection expressions.
- `using` directives outside the namespace, `System.*` first.
