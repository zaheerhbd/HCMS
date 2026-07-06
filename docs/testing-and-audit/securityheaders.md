# Security Headers

**Analogy:** Security headers are like putting a "No photography" sign on a sensitive building — they don't stop a determined attacker but eliminate an entire class of opportunistic browser-based attacks (clickjacking, MIME sniffing, XSS via inline scripts) with zero application logic.

## Add Middleware to Program.cs

```csharp
// In Program.cs, after app.UseHttpsRedirection():
app.Use(async (context, next) =>
{
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "connect-src 'self' https://*.applicationinsights.azure.com; " +
        "frame-ancestors 'none';");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append(
        "Permissions-Policy",
        "camera=(), microphone=(), geolocation=()");
    await next();
});
```

## Header Reference

| Header | Value | What it prevents |
|---|---|---|
| `Content-Security-Policy` | `default-src 'self'` | XSS via injected inline scripts / external resources |
| `X-Frame-Options` | `DENY` | Clickjacking (embedding in iframe) |
| `X-Content-Type-Options` | `nosniff` | MIME-type sniffing attacks |
| `Referrer-Policy` | `no-referrer` | Leaking URL path (may contain IDs) to third parties |
| `Permissions-Policy` | `camera=(), microphone=()` | Prevents rogue JS from requesting device access |

## Tasks

| Task | Detail | Effort |
|---|---|---|
| Add security headers middleware to Program.cs | CSP + X-Frame-Options + X-Content-Type-Options + Referrer-Policy + Permissions-Policy | Low |
| Verify headers appear in prod API responses | `curl -I https://hcm-prod-api.azurewebsites.net/health` — check all 5 headers present. Also check SecurityHeaders.io score | Low |
