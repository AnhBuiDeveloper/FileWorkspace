# File Workspace instructions for GitHub Copilot

Before suggesting or editing code, read, follow repository-root [AGENTS.md](../AGENTS.md). It is authoritative AI contribution contract.

Read `PROJECT-MEMORY.md` before every change. Read `UI-STANDARDS.md` for browser-facing work, `CONTRIBUTING.md` for pull-request work. Follow documented architecture, security boundary, SOLID, KISS, DRY, YAGNI, localization, test, responsive-design requirements. Do not add dependencies or change authentication, API contracts, storage behavior, deployment without explicit approval.

Keep changes focused, update tests/docs with behavior changes, never include secrets, `.env`, real uploads, generated artifacts. Use topic branches/pull requests; do not target `main` directly.
