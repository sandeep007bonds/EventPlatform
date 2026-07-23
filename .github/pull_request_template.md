<!--
  PR template. A linked issue is REQUIRED — no PR without one (golden rule).
  Keep PRs small and focused on a single issue.
-->

## Linked issue

Closes #<!-- issue number --> <!-- required: every PR must resolve/relate to a tracked issue -->

## Summary

<!-- What does this change do, and why? -->

## Type of change

- [ ] `feat` — new feature
- [ ] `fix` — bug fix
- [ ] `docs` — documentation
- [ ] `refactor` / `chore` / `ci` / `test`

## Checklist

- [ ] Linked to a tracking issue
- [ ] Build is warning-free (`dotnet build` — warnings are errors)
- [ ] StyleCop / analyzers pass
- [ ] Unit + integration tests added/updated and passing
- [ ] Public API has XML doc comments
- [ ] No secrets committed; config via Key Vault
- [ ] Respects layer boundaries (Domain does not depend on Infrastructure; no cross-service DB access)
- [ ] Idempotency + outbox considered for money/inventory changes
- [ ] ADR added/updated if this is a significant decision
- [ ] Service README / CLAUDE.md updated if behavior or contracts changed
