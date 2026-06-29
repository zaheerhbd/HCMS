# Phase 8 — Testing, Security & Polish

**Goal:** Production-quality build. Key flows covered by tests. Security hardened.

**Duration:** 2 weeks  
**Status:** `[ ]` Not Started  
**Last Updated:** 2026-06-24

---

## Backend Testing

- [ ] Unit tests (xUnit + Moq): all Application layer use cases / command handlers
- [ ] Integration tests (xUnit + `WebApplicationFactory` + Testcontainers SQL Server):
  - [ ] Auth flow: login → access protected route → refresh → logout
  - [ ] Patient CRUD
  - [ ] Case status machine: valid and invalid transitions
  - [ ] Document upload and SAS URL generation (mock Blob)
- [ ] Code coverage report: target ≥ 70% on Application layer
- [ ] `dotnet-security-scan` or OWASP dependency check in CI pipeline

## Frontend Testing

- [ ] Unit tests (Jasmine/Karma): `AuthService`, `JwtInterceptor`, form validators
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
