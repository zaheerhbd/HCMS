# Lighthouse Accessibility & Bundle Analysis

## Lighthouse Audit

Target: ≥ 90 accessibility score on 3 pages.

```bash
# Install Lighthouse CI
npm install -g @lhci/cli
```

```json
// lighthouserc.json (in project root)
{
  "ci": {
    "collect": {
      "startServerCommand": "ng serve",
      "url": [
        "http://localhost:4200/login",
        "http://localhost:4200/cases",
        "http://localhost:4200/cases/CASE-2026-00001"
      ]
    },
    "assert": {
      "assertions": {
        "categories:accessibility": ["error", {"minScore": 0.9}]
      }
    }
  }
}
```

> ⚠️ Common failures: missing `aria-label` on icon buttons, color contrast below 4.5:1, missing form labels. Angular Material usually passes — check custom components.

## Bundle Size Analysis

```bash
# Build with stats
ng build --configuration production --stats-json

# Visualise
npx webpack-bundle-analyzer \
  dist/hcm-web/stats.json
```

```json
// Budget check — angular.json
"budgets": [
  {
    "type": "initial",
    "maximumWarning": "2mb",
    "maximumError": "4mb"
  },
  {
    "type": "anyComponentStyle",
    "maximumWarning": "6kb",
    "maximumError": "10kb"
  }
]
```

Check for: duplicate Chart.js bundles, large moment.js locale pulls, unneeded Firely SDK in the frontend (should not be there).

## Tasks

| Task | Detail | Effort |
|---|---|---|
| Run Lighthouse CI on Login, Case List, Case Detail — fix any score < 90 | Common fixes: add `aria-label` to mat-icon buttons, verify color contrast on status badges, label all form controls | Medium |
| Run `ng build --stats-json` + webpack-bundle-analyzer — resolve warnings | Ensure initial bundle < 2 MB warning threshold. Lazy-loading is already wired for all feature modules — verify it's working by checking chunk names in `dist/` | Low |
