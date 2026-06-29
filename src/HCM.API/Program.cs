using System.Text;
using System.Threading.RateLimiting;
using HCM.API.HealthChecks;
using HCM.API.Middleware;
using HCM.Application;
using HCM.Infrastructure;
using HCM.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// ════════════════════════════════════════════════════════════════════════════════
// SERVICE CONFIGURATION — builder phase (DI container setup)
// ════════════════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

// ── LOGGING: Serilog Configuration ──────────────────────────────────────────────
// WHY: Structured logging to console and Application Insights.
// Setup: Reads Serilog settings from appsettings.json, enriches logs with request
//        context (correlation IDs, user info), and outputs to console.
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)                    // Load Serilog config from appsettings.json
    .Enrich.FromLogContext()                                          // Add contextual info (e.g., request ID, user) to every log
    .WriteTo.Console()                                                // Write logs to console (also sends to App Insights in prod)
    .CreateLogger();

// Use Serilog as the host logger (captures framework startup logs)
builder.Host.UseSerilog();

// ── APPLICATION & INFRASTRUCTURE LAYERS ────────────────────────────────────────
// WHY: Register all business logic and persistence services via extension methods.
// This keeps Program.cs clean and organizes services by layer.
builder.Services.AddApplication();           // Register DataHandlers, repositories, and business logic
builder.Services.AddInfrastructure(builder.Configuration);  // Register DbContext, EF Core, token service, etc.

// ── JWT BEARER AUTHENTICATION ───────────────────────────────────────────────────
// WHY: Stateless auth using JWT tokens. Client sends token in Authorization header,
//      API validates signature and claims without hitting a session store.
// Process: User logs in → receives access + refresh tokens → includes access token
//          in every subsequent request in "Authorization: Bearer <token>" header.

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is required. Use dotnet user-secrets or Key Vault.");

// Add JWT authentication scheme to the service container
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Configure how to validate incoming JWT tokens
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,                                   // Verify token signature (prevents tampering)
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,                                             // Verify token was issued by us
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,                                           // Verify token is for this app (not another service)
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,                                           // Reject expired tokens
            ClockSkew = TimeSpan.Zero,                                         // No tolerance for clock drift (strict expiry)
        };
    });

// Add authorization policy support (required by [Authorize] attributes and role guards)
builder.Services.AddAuthorization();

// ── RATE LIMITING ───────────────────────────────────────────────────────────────
// WHY: Prevent brute force attacks (e.g., on /api/auth/login).
// Policy: Max 10 login requests per 60 seconds per IP address.
builder.Services.AddRateLimiter(options =>
{
    // Define a named rate limit policy for the login endpoint
    options.AddFixedWindowLimiter("login", cfg =>
    {
        cfg.PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:LoginMaxRequests", 10);  // Max 10 requests
        cfg.Window = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("RateLimiting:LoginWindowSeconds", 60));  // Per 60 seconds
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;  // FIFO queue for requests that exceed limit
        cfg.QueueLimit = 0;  // Don't queue excess requests; reject immediately
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;  // Return 429 when rate limit exceeded
});

// ── CORS (Cross-Origin Resource Sharing) ────────────────────────────────────────
// WHY: Allow the Angular SPA (running on a different origin) to make API requests.
// Without CORS: Browser blocks requests from https://swa-domain.com to https://api-domain.com
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
        policy.WithOrigins(allowedOrigins)              // Allow requests from these origins
              .AllowAnyHeader()                          // Accept any header (Content-Type, Authorization, etc.)
              .AllowAnyMethod()                          // Accept GET, POST, PUT, DELETE, etc.
              .AllowCredentials());                      // Allow cookies and Authorization headers
});

// ── HEALTH CHECKS ───────────────────────────────────────────────────────────────
// WHY: Azure Load Balancer and uptime monitors use /health to detect if the app is alive.
// If /health returns non-200, Azure will restart the app or remove it from the load balancer.
// WHAT: Registers health checks that verify the app and its dependencies (database) are working.
// HOW: The /health endpoint calls all registered health checks and returns their combined status.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");  // Custom check: Can the app connect to the database?

// ── MVC CONTROLLERS & SWAGGER ───────────────────────────────────────────────────
// WHY: Register controller routes and generate interactive API documentation.
builder.Services.AddControllers();  // Scan [ApiController] classes and register routes
builder.Services.AddEndpointsApiExplorer();  // Required for Swagger to discover endpoints
builder.Services.AddSwaggerGen(c =>
{
    // Swagger document metadata
    c.SwaggerDoc("v1", new() { Title = "HCMS API", Version = "v1" });

    // Add JWT bearer token input to Swagger UI (so you can test authenticated endpoints)
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    });

    // Apply the security scheme to all endpoints
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

// ════════════════════════════════════════════════════════════════════════════════
// MIDDLEWARE PIPELINE — app phase (request processing)
// The order here matters! Each middleware processes the request, then passes to next.
// ════════════════════════════════════════════════════════════════════════════════

var app = builder.Build();

// ── DATABASE SEEDING ────────────────────────────────────────────────────────────
// WHY: Populate roles and default admin user on first startup.
// Runs in all environments (Dev, Staging, Prod) so each environment has initial data.
await DataSeeder.SeedAsync(app.Services);

// ── MIDDLEWARE PIPELINE (in order of execution) ─────────────────────────────────

// 1. EXCEPTION HANDLER (outermost middleware)
//    WHY: Catches all unhandled exceptions from downstream middleware/endpoints.
//    Location: First, so it wraps everything else.
//    Result: Converts exceptions to RFC 7807 ProblemDetails JSON responses.
app.UseMiddleware<ExceptionHandlerMiddleware>();

// 2. REQUEST LOGGING
//    WHY: Log every HTTP request (method, path, response code, duration) for debugging and monitoring.
//    Location: After exception handler, so it also logs failed requests that hit the exception handler.
//    Output: Logs to console and Application Insights (configured in Serilog).
app.UseSerilogRequestLogging();

// 3. SWAGGER & SWAGGER UI
//    WHY: Generate and serve interactive API documentation at /swagger.
//    Note: Only enable in Development; disable in Production for security.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();        // Serve swagger.json at /swagger/v1/swagger.json
    app.UseSwaggerUI();      // Serve interactive UI at /swagger
}

// 4. HTTPS REDIRECTION
//    WHY: Redirect HTTP requests to HTTPS (security best practice).
//    Example: http://example.com/api/auth/login → https://example.com/api/auth/login
app.UseHttpsRedirection();

// 5. CORS
//    WHY: Apply CORS policy to allow cross-origin requests from the Angular SPA.
//    Order: Before routing, so it can inspect the request before it reaches controllers.
//    Policy: "Angular" (defined above) allows origins, headers, methods, credentials.
app.UseCors("Angular");

// 6. RATE LIMITING
//    WHY: Throttle requests to prevent abuse (e.g., brute force on /login).
//    Location: Before authentication, so rate limiting applies to all requests (even unauthenticated ones).
app.UseRateLimiter();

// 7. AUTHENTICATION
//    WHY: Parse the Authorization header and extract the JWT token.
//    Process: Validates token signature, claims, and expiry (configured above).
//    Result: Sets HttpContext.User to the authenticated user (if token is valid).
//    Note: Does NOT check [Authorize] attributes; just extracts user info.
app.UseAuthentication();

// 8. AUTHORIZATION
//    WHY: Check if the authenticated user has permission to access the endpoint.
//    Process: Enforces [Authorize], [Authorize(Roles = "Admin")], and policy-based rules.
//    Note: Runs on the User set by Authentication (step 7).
app.UseAuthorization();

// 9. ROUTE MAPPING (innermost middleware)
//    WHY: Dispatch requests to controller actions.
app.MapControllers();

// 10. HEALTH CHECK ENDPOINT
//     WHY: Uptime monitors and load balancers check this to detect if app is alive.
app.MapHealthChecks("/health");

// 11. SPECIAL ENDPOINT CONFIGURATION
//     Apply the "login" rate limit policy specifically to the /api/auth/login route.
//     This endpoint gets stricter rate limiting (10 req/min) to prevent brute force.
app.MapControllerRoute(
    name: "auth-login",
    pattern: "api/auth/login"
).RequireRateLimiting("login");

// ── START THE APPLICATION ───────────────────────────────────────────────────────
// Blocks here. The app listens on the configured port (default: https://localhost:5001)
// and processes requests using the middleware pipeline defined above.
app.Run();
