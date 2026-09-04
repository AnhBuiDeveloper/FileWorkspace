## Summary

Describe user-visible behavior and scope.

## Validation

- [ ] Read `AGENTS.md` and `PROJECT-MEMORY.md` before editing.
- [ ] Kept SOLID, KISS, DRY, YAGNI and documented architecture boundaries.
- [ ] Preserved token protection, safe paths, incomplete-upload handling, backward-compatible APIs.
- [ ] Added or updated unit/API/UI tests for behavior changes.
- [ ] Ran `dotnet test FileWorkspace.Tests/FileWorkspace.Tests.csproj --configuration Release`.
- [ ] Ran `npm run test:architecture`.
- [ ] Ran `dotnet format --verify-no-changes --no-restore`.
- [ ] Ran `npm run test:ui`, or documented exact local blocker below.
- [ ] Updated English/Vietnamese user-visible text/docs where required.

## UI review — complete only for browser-facing changes

- [ ] Read `UI-STANDARDS.md`; tested 320, 375, 430, 768, 1024, 1440 px.
- [ ] No horizontal overflow; long content remains usable.
- [ ] Keyboard, touch, 200% zoom, reduced-motion behavior reviewed.

## Exceptions / follow-up

State exact blocker, intentionally deferred work, or `None`.
