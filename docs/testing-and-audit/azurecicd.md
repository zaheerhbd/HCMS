# Azure & CI/CD Production Setup

Pipeline flow: `Push to main (GitHub Actions) → Build + Test (dotnet test · ng test · coverage) → Manual Gate (Required approval before prod deploy) → Deploy to Prod (App Service + SWA)`

Application Insights Alerts to configure: Error Rate > 5% · Availability < 99% · p95 Latency > 2s · Budget > $20/mo.

## Production Tasks

| Task | Detail | Effort |
|---|---|---|
| Add manual approval gate to GitHub Actions prod deploy job | Add `environment: production` to the prod deploy step; configure Environment Protection Rule in GitHub repo Settings → Environments → production → Required reviewers | Low |
| Set up Application Insights alerts: error rate > 5%, availability < 99%, p95 latency > 2s | Azure Portal → Application Insights → Alerts → Create. Metric: `exceptions/count` (error rate), `availabilityResults/availabilityPercentage`, `requests/duration` (p95) | Medium |
| Add Azure budget alert at $20/month threshold | Azure Cost Management → Budgets → New. Scope: subscription. Alert recipients: your email | Low |
| Disable Swagger in production | Wrap `AddSwaggerGen()` + `UseSwagger()` in `if (app.Environment.IsDevelopment())` blocks. Verify `/swagger` returns 404 in prod (ZAP will flag this) | Low |
