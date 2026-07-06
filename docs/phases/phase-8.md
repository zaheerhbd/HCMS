# Phase 8 — Testing, Security & Polish

**Goal:** Production-quality build. Key flows covered by tests. Security hardened.

**Duration:** 2 weeks  
**Status:** `[~]` In Progress  
**Last Updated:** 2026-07-06

---

## Backend Testing

- [x] Unit tests (xUnit + EF Core InMemory): Auth, Patient, Case status machine, Document, Notification DataHandlers — 43 tests, `src/HCM.Application.Tests`
- [~] Integration tests (xUnit + `WebApplicationFactory`; InMemory DB substituted for Testcontainers SQL Server — Docker daemon unavailable in dev environment):
  - [~] Auth flow: login → access protected route → refresh → logout (login/protected-route/lockout covered; refresh & logout endpoints are unimplemented stubs — [issue #5](https://github.com/zaheerhbd/HCMS/issues/5))
  - [x] Patient CRUD
  - [x] Case status machine: valid and invalid transitions
  - [ ] Document upload and SAS URL generation (mock Blob) — covered at unit level only; no integration-level multipart upload test yet
  - [x] RBAC verification across roles (bonus, not originally scoped): `src/HCM.API.Tests/Rbac`
- [ ] Code coverage report: target ≥ 70% on Application layer
- [ ] `dotnet-security-scan` or OWASP dependency check in CI pipeline

See [docs/testing-and-audit/](../testing-and-audit/) for per-section test docs and [test-results.html](../testing-and-audit/test-results.html) for the full 69/69 passing run. Frontend unit test results and a plain-language walkthrough are in [angular-testing-explained.html](../testing-and-audit/angular-testing-explained.html) (32/32 passing).

## Frontend Testing

- [x] Unit tests (Jasmine/Karma): `AuthService`, `JwtInterceptor`, `NotificationService`, `CaseFormComponent`/`PatientFormComponent` validation, `HasRoleDirective` — 32 tests, `src-ng/hcm-web/src/app/**/*.spec.ts`
- [ ] E2E tests (Playwright or Cypress): login flow, create patient, create case, upload document
- [ ] Run `ng build --configuration production` with `--stats-json`; analyse bundle size
- [ ] Accessibility audit: run Lighthouse on Login, Case List, Case Detail pages; target ≥ 90 accessibility score

## Security Hardening

- [ ] Verify no PHI (names, DOB, MRN) appears in Application Insights traces or logs
- [ ] Add `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options` headers to API responses
- [ ] Rotate JWT secret in Key Vault; confirm app picks up new version without redeploy (Key Vault reference auto-refresh)
- [ ] Confirm SAS URLs are not logged (check Application Insights live stream during a download)
- [ ] Run OWASP ZAP quick scan against staging API; resolve any Medium+ findings
- [ ] Confirm `AuditController` returns 403 when called with a non-Admin JWT

## Production Deploy & Polish

- [ ] Provision prod Azure resources (separate Resource Group from staging)
- [ ] Configure manual approval gate in GitHub Actions for prod deploy
- [ ] Set up Application Insights alerts: error rate > 5%, availability < 99%, p95 > 2s
- [ ] Add budget alert on Azure subscription ($20/month threshold for portfolio)
- [ ] Update `README.md`: architecture diagram, local setup steps, environment variable guide, screenshots
- [ ] Record a 5-minute demo walkthrough video for portfolio

## Definition of Done

- [ ] CI pipeline runs all tests; build fails if tests fail
- [ ] Lighthouse accessibility score ≥ 90 on primary pages
- [ ] OWASP ZAP scan shows no Medium or Critical findings
- [ ] Prod deployment is behind a manual approval gate
- [ ] Application Insights shows live traces from prod environment
- [ ] `README.md` is complete with setup instructions
