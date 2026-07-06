# OWASP ZAP Quick Scan

**Analogy:** ZAP is an automated burglar who tries every window and door while you're home. It won't pick every lock but catches unlocked doors (missing headers, open debug endpoints, reflected XSS) in minutes.

## ZAP Quick Scan — Docker

```bash
# Run ZAP baseline scan against your local or staging API
docker run --rm ghcr.io/zaproxy/zaproxy:stable \
  zap-baseline.py \
  -t https://hcm-prod-api.azurewebsites.net \
  -r zap-report.html \
  -I # don't fail on warnings
```

Expected findings to resolve: missing HSTS header (add to App Service), any reflected user input, dev endpoints exposed (`/swagger` in prod — disable or restrict).

## Tasks

| Task | Detail | Effort |
|---|---|---|
| Run OWASP ZAP baseline scan against staging/prod API — resolve Medium+ findings | Common: Swagger accessible in prod (disable with `builder.Services.AddSwaggerGen()` behind `if (app.Environment.IsDevelopment())`), HSTS missing (add in App Service TLS settings) | Medium |
