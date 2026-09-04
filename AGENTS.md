# AI contribution contract

This file is mandatory for every coding agent working in this repository, including Codex, GitHub Copilot, Cursor, Claude Code, and future automation.

## Read before editing

Read these files completely and apply them in this order:

1. `PROJECT-MEMORY.md` — product scope, architecture, security boundaries, SOLID/KISS/DRY/YAGNI decisions.
2. `UI-STANDARDS.md` — required for browser-facing HTML, CSS, or JavaScript.
3. `CONTRIBUTING.md` — contribution, licensing, pull-request, and DCO requirements.

Do not modify a file until applicable documents have been read. If a request conflicts with them, explain conflict, obtain explicit approval before changing documented decision.

## Non-negotiable engineering rules

- Preserve architecture in `PROJECT-MEMORY.md`: `Program.cs` composes; endpoints own HTTP; services own file-system/application behavior; models own contracts; `wwwroot` owns browser presentation.
- Maintain token protection, safe server-side path resolution, hidden incomplete uploads. Never expose `Upload/` through static-file middleware.
- Treat all client input, paths, filenames, tokens as untrusted.
- Prefer smallest standard-library/browser solution. Do not add dependencies, roles, databases, queues, cloud integrations, speculative abstractions, configuration without accepted requirement.
- Keep validation, authorization, path handling, error mapping, localized strings centralized. Do not duplicate rules between layers.
- Never use browser-native `alert()`, `confirm()`, or `prompt()` dialogs. Destructive or decision-required browser actions must use an accessible, localized, application-owned modal; the native HTML `<dialog>` element is allowed.
- Preserve backward-compatible API behavior unless a versioned breaking change has explicit approval.
- Never commit `.env`, credentials, tokens, real uploads, private data, generated test reports, build artifacts.

## Required delivery loop

1. Work on focused topic branch; never push directly to `main`.
2. Make smallest complete change. Update English/Vietnamese wording, tests, docs when behavior changes.
3. For UI work, validate 320, 375, 430, 768, 1024, 1440 CSS-pixel widths; confirm no horizontal overflow, keyboard access, long-content handling, touch controls, reduced motion.
4. Run relevant checks before committing:

   ```powershell
   dotnet test FileWorkspace.Tests/FileWorkspace.Tests.csproj --configuration Release
   npm run test:architecture
   dotnet format --verify-no-changes --no-restore
   npm run test:ui
   ```

   If check cannot run, do not claim success. State exact blocker; let required GitHub checks run before merge.
5. Open focused pull request. Complete `.github/PULL_REQUEST_TEMPLATE.md`, resolve all conversations, merge only after required checks pass.

## Required stop points

Stop, request direction before breaking API/storage change, authorization weakening, dependency addition, data deletion/migration, license change, public deployment change, architecture deviation. Do not silently make those choices.

## Enforcement

`main` uses GitHub ruleset: no force-push/deletion, PR-only changes, linear history, resolved conversations, required `architecture-quality`, `dotnet-tests`, `playwright` checks. Architecture check verifies governance documents remain present alongside architecture/UI standards. Controls cannot make malicious repository administrator harmless; they make normal AI/contributor work reviewable, block noncompliant merges.
