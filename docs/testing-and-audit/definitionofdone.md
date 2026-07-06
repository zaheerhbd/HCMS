# Definition of Done — Phase 8

- [ ] CI pipeline runs all tests; build fails if tests fail or coverage < 70%
  GitHub Actions: both `build-api.yml` and `build-angular.yml` run test steps, fail the build on coverage threshold.
- [ ] Lighthouse accessibility score ≥ 90 on Login, Case List, Case Detail
  Run Lighthouse CI — pass the assert block in `lighthouserc.json`.
- [ ] OWASP ZAP scan shows no Medium or Critical findings
  Run ZAP baseline against prod API. Disable Swagger in prod. Add HSTS in App Service TLS settings.
- [ ] No PHI appears in Application Insights traces or logs
  Verify via live stream during E2E test run. Grep confirmed clean in source.
- [ ] Prod deployment is behind a manual approval gate
  GitHub repo → Settings → Environments → production → Required reviewers configured.
- [ ] Application Insights shows live traces from prod environment
  Open Live Metrics in Azure portal; trigger a login — verify request trace visible.
- [ ] README.md is complete with architecture diagram, setup instructions, screenshots, demo link
  README passes a "cold start" test: a new developer can run the app locally following only the README.
- [ ] All 4 Playwright E2E flows pass in CI headless mode
  Login, create patient, create case, upload document — green on GitHub Actions.
