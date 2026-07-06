# Coverage Report & CI Integration

## Setup Coverlet

```bash
# Add to each test project
dotnet add package coverlet.collector
dotnet add package ReportGenerator

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" \
  --results-directory ./coverage

# Generate HTML report
reportgenerator \
  -reports:"coverage/**/coverage.cobertura.xml" \
  -targetdir:"coverage/report" \
  -reporttypes:Html
```

## GitHub Actions step to add

```yaml
- name: Run tests + coverage
  run: |
    dotnet test \
      --collect:"XPlat Code Coverage" \
      --results-directory ./coverage

- name: Check coverage threshold
  run: |
    reportgenerator \
      -reports:coverage/**/coverage.cobertura.xml \
      -targetdir:coverage/report \
      -reporttypes:TextSummary
    grep "Line coverage" coverage/report/Summary.txt \
      | awk '{if($NF+0 < 70) exit 1}'

- name: OWASP Dependency Check
  uses: dependency-check/Dependency-Check_Action@main
  with:
    project: 'HCMS'
    path: '.'
    format: 'HTML'
```

## Tasks

| Task | Detail | Effort |
|---|---|---|
| Add Coverlet + ReportGenerator to both test projects | `coverlet.collector` (runtime) + `reportgenerator` (global tool or CI step) | Low |
| Gate GitHub Actions CI: fail build if Application layer coverage < 70% | Add TextSummary report + shell check in `build-api.yml` | Low |
| Add OWASP dependency-check step to CI | `dependency-check/Dependency-Check_Action@main` — uploads HTML report as artifact. Fail on CVSS ≥ 7.0 (High) | Low |
