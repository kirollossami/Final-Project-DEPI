using Business.Interfaces;
using Business.Services;
using Business.Settings;
using Domain.Entities;
using FluentValidation;
using Infrastructure.Context;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Base;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Cache;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using StudentHousingAPI.Validators;
using System.Text;

// ── Top-level catch: captures crashes that happen before the DI logger is ready ──
// Writes to both the rolling file and the IIS stdout stream.
var startupLogPath = Path.Combine(AppContext.BaseDirectory, "logs", $"startup-{DateTime.UtcNow:yyyy-MM-dd}.log");
Directory.CreateDirectory(Path.GetDirectoryName(startupLogPath)!);

void WriteStartupLog(string message, Exception? ex = null)
{
    try
    {
        var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} [STARTUP] {message}";
        if (ex != null) line += $"\nEXCEPTION: {ex}";
        File.AppendAllText(startupLogPath, line + Environment.NewLine);
        Console.WriteLine(line);
    }
    catch { /* must not throw */ }
}

WebApplication? app = null;

try
{
    WriteStartupLog("=== APPLICATION STARTING ===");

    var builder = WebApplication.CreateBuilder(args);

// ── Logging — Console + file
//    Standard ASP.NET Core logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

    #region cache
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, CacheService>();
    #endregion

    // Configure DbContext
    builder.Services.AddDbContext<StudentHousingDBContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            // Increase command timeout for slow remote / shared-hosting SQL Server.
            // The default 30 s is too short for large migrations or heavy queries.
            sqlOptions.CommandTimeout(300); // 5 minutes
        }));

// Configure Identity
builder.Services.AddIdentity<User, IdentityRole>(options => {
     options.Password.RequiredLength = 6;
     options.Password.RequireDigit = true;
     options.Password.RequireLowercase = true;
     options.Password.RequireUppercase = true;
     options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<StudentHousingDBContext>()
    .AddDefaultTokenProviders();

// Configure JWT Settings
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured in appsettings.json");
}

var authBuilder = builder.Services.AddAuthentication(options =>
{
    // JWT for API calls, Cookie for external login flow
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };

    // Optional: blacklist revoked tokens
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var tokenBlacklistService = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            if (tokenBlacklistService.IsTokenBlacklisted(token))
            {
                context.Fail("Token has been revoked");
            }
        }
    };
})
// 2️⃣ Cookie authentication – ONLY for the Google external‑login flow
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cfg =>
{
    cfg.Cookie.SameSite = SameSiteMode.None; // required for cross‑origin redirects
    cfg.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    var allowedOriginsForGoogle = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
    var frontendBaseUrl = (allowedOriginsForGoogle?.FirstOrDefault() ?? "http://localhost:4200").TrimEnd('/');

    // Register Google external provider – uses the Cookie scheme defined above
    authBuilder.AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = googleClientId;
        googleOptions.ClientSecret = googleClientSecret;
        googleOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        // Google middleware callback path (intercepted by middleware, must not be a controller action)
        googleOptions.CallbackPath = "/signin-google";
        googleOptions.SaveTokens = true;

        googleOptions.Scope.Add("openid");
        googleOptions.Scope.Add("profile");
        googleOptions.Scope.Add("email");

        // Gracefully handle any Google middleware failure (e.g. direct navigation to /signin-google,
        // invalid state/correlation cookie, or Google returning an error) instead of HTTP 500
        googleOptions.Events.OnRemoteFailure = context =>
        {
            var errorDescription = Uri.EscapeDataString(
                context.Failure?.Message ?? "google_remote_failure");
            context.Response.Redirect($"{frontendBaseUrl}/login?error={errorDescription}");
            context.HandleResponse(); // suppress the exception — do NOT rethrow
            return Task.CompletedTask;
        };

        // User clicked "Deny" on the Google consent screen
        googleOptions.Events.OnAccessDenied = context =>
        {
            context.Response.Redirect($"{frontendBaseUrl}/login?error=access_denied");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("LandLord"));
    options.AddPolicy("RequireStudentRole", policy => policy.RequireRole("Student"));
    options.AddPolicy("CanManageProperties", policy => policy.RequireRole("LandLord"));
    options.AddPolicy("CanViewBookings", policy => policy.RequireRole("Student", "LandLord"));
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
});

// Commission settings
builder.Services.Configure<CommissionSettings>(
    builder.Configuration.GetSection(CommissionSettings.SectionName));

// Register Repositories
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBaseRepository<Conversation>>(sp =>
{
    var context = sp.GetRequiredService<Infrastructure.Context.StudentHousingDBContext>();
    return new Infrastructure.Repositories.Base.BaseRepository<Conversation>(context);
});
builder.Services.AddScoped<IBaseRepository<Message>>(sp =>
{
    var context = sp.GetRequiredService<Infrastructure.Context.StudentHousingDBContext>();
    return new Infrastructure.Repositories.Base.BaseRepository<Message>(context);
});
builder.Services.AddScoped<IBedRepository, BedRepository>();
builder.Services.AddScoped<ICommissionRecordRepository, CommissionRecordRepository>();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IHousingUnitRepository, HousingUnitRepository>();
builder.Services.AddScoped<ILandLordRepository, LandLordRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IUnitImageRepository, UnitImageRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IPaymentReceiptRepository, PaymentReceiptRepository>();
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<IEscrowTransactionRepository, EscrowTransactionRepository>();
builder.Services.AddScoped<IPaymentHistoryRepository, PaymentHistoryRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();

// Register Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAdminService, AdminService>();
//builder.Services.AddScoped<IAdminApprovalService, AdminApprovalService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITwoFactorAuthService, TwoFactorAuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ILandLordService, LandLordService>();
builder.Services.AddScoped<IHousingUnitService, HousingUnitService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IBedService, BedService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationDispatcher, StudentHousingAPI.Services.SignalRNotificationDispatcher>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IBookingConflictService, BookingConflictService>();
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddScoped<IChatService, ChatService>();

// Payment and Contract Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IContractWorkflowService, ContractWorkflowService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IEscrowService, EscrowService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IPaymentHistoryService, PaymentHistoryService>();
builder.Services.AddScoped<IBookingPaymentService, BookingPaymentService>();
builder.Services.AddScoped<IAdminApprovalService, AdminApprovalService>();
builder.Services.AddScoped<IBookingApprovalService, BookingApprovalService>();
builder.Services.AddScoped<IBalanceService, BalanceService>();

// Paymob Configuration
builder.Services.Configure<PaymobSettings>(
    builder.Configuration.GetSection(PaymobSettings.SectionName));

// Validate Paymob settings early to surface configuration issues immediately
var paymobConfig = builder.Configuration.GetSection(PaymobSettings.SectionName).Get<PaymobSettings>();
if (paymobConfig == null)
{
    // Paymob config missing — warn and continue. Payment endpoints will return errors until configured.
    Console.Error.WriteLine("Warning: Paymob configuration section is missing. Payment features will be disabled until configured.");
}
// Require either a valid secret key present in Paymob:ApiKey or Paymob:SecretKey or via environment
// ApiKey can be base64 encoded (service will decode it) or plain text starting with egy_sk_
var paymobHasSecret = paymobConfig != null && !string.IsNullOrWhiteSpace(paymobConfig.ApiKey);
var paymobSecretFromConfig = builder.Configuration["Paymob:SecretKey"];
var paymobHasEnv = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PAYMOB_SECRET"))
                  || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PAYMOB_APIKEY"))
                  || !string.IsNullOrWhiteSpace(builder.Configuration["PAYMOB_SECRET"]);

if (!paymobHasSecret && string.IsNullOrWhiteSpace(paymobSecretFromConfig) && !paymobHasEnv)
{
    // Log warning instead of failing startup so app can run without payments in development
    Console.Error.WriteLine("Warning: Paymob secret key is not configured. Set Paymob:ApiKey (base64 encoded or egy_sk_...), Paymob:SecretKey, or the PAYMOB_SECRET env var to enable payments.");
}

if (paymobConfig != null && (string.IsNullOrWhiteSpace(paymobConfig.CardIntegrationId) || !int.TryParse(paymobConfig.CardIntegrationId, out _)))
{
    Console.Error.WriteLine("Warning: Paymob:CardIntegrationId is not configured or invalid. Some payment flows may not work.");
}

// Configure Paymob HttpClient with IHttpClientFactory
builder.Services.AddHttpClient("Paymob")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true })
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

builder.Services.AddScoped<IPaymobService, PaymobService>();

// Background service: expire bookings whose signature deadline has passed
builder.Services.AddHostedService<BookingExpirationService>();
builder.Services.AddHostedService<PaymentSyncService>();
#region Validators
builder.Services.AddValidatorsFromAssemblyContaining<LoginValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<StudentRegisterValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ReviewVerificationValidator>();
builder.Services.AddFluentValidationAutoValidation();
#endregion


// Add CORS — origins are driven entirely by appsettings.json "AllowedOrigins"
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? new[] { "https://unistay-shbs.vercel.app", "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecific", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    // Paymob webhooks are server-to-server: no Origin header is sent.
    // A separate open policy is applied only to the callback endpoints.
    options.AddPolicy("AllowPaymobWebhook", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // JWT bearer UI support
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
}).AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

    app = builder.Build();

    WriteStartupLog("=== builder.Build() succeeded ===");

// ── Application Lifetime Event Logging ───────────────────────────────────────
//    Logs application startup, shutdown, and any unhandled exceptions that
//    cause the application to terminate unexpectedly.
var lifetimeLogger = app.Services.GetRequiredService<ILogger<Program>>();
app.Lifetime.ApplicationStarted.Register(() =>
{
    lifetimeLogger.LogInformation("╔══════════════════════════════════════════════════════════════════╗");
    lifetimeLogger.LogInformation("║ APPLICATION STARTED                                                       ║");
    lifetimeLogger.LogInformation("╚══════════════════════════════════════════════════════════════════╝");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    lifetimeLogger.LogInformation("╔══════════════════════════════════════════════════════════════════╗");
    lifetimeLogger.LogInformation("║ APPLICATION STOPPING - Graceful shutdown initiated                 ║");
    lifetimeLogger.LogInformation("╚══════════════════════════════════════════════════════════════════╝");
});

app.Lifetime.ApplicationStopped.Register(() =>
{
    lifetimeLogger.LogInformation("╔══════════════════════════════════════════════════════════════════╗");
    lifetimeLogger.LogInformation("║ APPLICATION STOPPED                                                        ║");
    lifetimeLogger.LogInformation("╚══════════════════════════════════════════════════════════════════╝");
});

// ── Global Unhandled Exception Handler ─────────────────────────────────────
//    Catches any unhandled exceptions from background services or the main thread
//    that would otherwise cause the application to crash silently.
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    var exception = e.ExceptionObject as Exception;
    if (exception != null)
    {
        lifetimeLogger.LogCritical(exception, "╔══════════════════════════════════════════════════════════════════╗");
        lifetimeLogger.LogCritical("║ UNHANDLED EXCEPTION - Application crashing                            ║");
        lifetimeLogger.LogCritical("╚══════════════════════════════════════════════════════════════════╝");
        lifetimeLogger.LogCritical("Exception Type: {ExceptionType}", exception.GetType().FullName);
        lifetimeLogger.LogCritical("Exception Message: {Message}", exception.Message);
        lifetimeLogger.LogCritical("Exception StackTrace: {StackTrace}", exception.StackTrace);
        if (exception.InnerException != null)
        {
            lifetimeLogger.LogCritical("Inner Exception: {InnerMessage}", exception.InnerException.Message);
            lifetimeLogger.LogCritical("Inner StackTrace: {InnerStackTrace}", exception.InnerException.StackTrace);
        }
    }
};

// ── ForwardedHeaders: must be the very first middleware so that all subsequent
//    middleware (HTTPS redirection, authentication, etc.) see the real scheme
//    and IP forwarded by IIS / the load balancer.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Global exception handling middleware - return JSON for all errors
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = new
            {
                message = ex.Message,
                type = ex.GetType().Name
            }
        });
    }
});

// ── Early Paymob callback logging ──────────────────────────────────────────
//    Fires BEFORE routing, so even if the controller never executes we
//    have evidence the request arrived at ASP.NET Core.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isCallback =
        path.StartsWithSegments("/api/BookingPayment/callback", StringComparison.OrdinalIgnoreCase);

    if (isCallback)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                         .CreateLogger("PaymobCallbackMiddleware");
        logger.LogInformation(
            "[PaymobCallback] {Method} {Path}{Query} received. Content-Type={Ct} User-Agent={Ua} IP={Ip}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            context.Request.ContentType ?? "(none)",
            context.Request.Headers.UserAgent.ToString(),
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }

    await next();

    if (isCallback)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                         .CreateLogger("PaymobCallbackMiddleware");
        logger.LogInformation(
            "[PaymobCallback] Response status {Status} for {Method} {Path}",
            context.Response.StatusCode,
            context.Request.Method,
            context.Request.Path);
    }
});

// Handle 404 and other status codes as JSON for API routes
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode >= 400 && context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.ContentType = "application/json";
        if (!context.Response.HasStarted)
        {
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    message = context.Response.StatusCode == 404 ? "Resource not found" : "An error occurred",
                    statusCode = context.Response.StatusCode
                }
            });
        }
    }
});

// NOTE: X-Forwarded-Proto is now handled by app.UseForwardedHeaders() above.
//       The manual middleware has been removed to avoid double-processing.

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enable buffering for webhook raw body reading
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student Housing API V1");
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   
}

app.MapGet("/", async context =>
{
    context.Response.Redirect("/swagger");
    await System.Threading.Tasks.Task.CompletedTask;
});

app.UseCors("AllowSpecific");

// Add CSP headers to allow payment gateway iframes
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Content-Security-Policy", 
        "frame-ancestors 'self' https://ap.gateway.mastercard.com https://accept.paymob.com;");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<StudentHousingAPI.Hubs.ChatHub>("/chatHub");
app.MapHub<StudentHousingAPI.Hubs.NotificationHub>("/notificationHub");

    WriteStartupLog("=== APPLICATION BUILT SUCCESSFULLY — calling app.Run() ===");

    // ── Auto-apply pending EF migrations on startup ──────────────────────────
    // Runs synchronously before the first request is served.
    // If the migration fails, logs the error and continues — the app will still
    // start; individual DB calls may fail but the process won't crash silently.
    try
    {
        WriteStartupLog("Applying pending EF Core migrations...");
        using var migrationScope = app.Services.CreateScope();
        var db = migrationScope.ServiceProvider.GetRequiredService<StudentHousingDBContext>();
        var pending = db.Database.GetPendingMigrations().ToList();
        if (pending.Count > 0)
        {
            WriteStartupLog($"Applying {pending.Count} pending migration(s): {string.Join(", ", pending)}");
            db.Database.Migrate();
            WriteStartupLog("Migrations applied successfully.");
        }
        else
        {
            WriteStartupLog("No pending migrations.");
        }
    }
    catch (Exception migEx)
    {
        WriteStartupLog("WARNING: EF migration failed — check the connection string and DB permissions.", migEx);
        // Do NOT rethrow — let the app start; the background services will retry DB calls
    }

    app.Run();
    WriteStartupLog("=== app.Run() returned normally ===");
}
catch (Exception ex)
{
    WriteStartupLog("=== FATAL STARTUP EXCEPTION — application could not start ===", ex);

    // Also try to write via the DI logger if it was already built
    try
    {
        var logger = app?.Services?.GetService<ILogger<Program>>();
        logger?.LogCritical(ex, "FATAL: Application failed to start");
    }
    catch { /* ignore — DI may not be available */ }

    // Re-throw so IIS/the process host records exit code != 0
    throw;
}
