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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using StudentHousingAPI.Validators;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure DbContext
builder.Services.AddDbContext<StudentHousingDBContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

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
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IBookingConflictService, BookingConflictService>();
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddScoped<IChatService, ChatService>();

// Payment and Contract Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPaymentReceiptRepository, PaymentReceiptRepository>();
builder.Services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
builder.Services.AddScoped<IEscrowTransactionRepository, EscrowTransactionRepository>();
builder.Services.AddScoped<IContractRepository, ContractRepository>();

// Paymob Configuration
builder.Services.Configure<PaymobSettings>(
    builder.Configuration.GetSection(PaymobSettings.SectionName));
builder.Services.AddHttpClient<IPaymobService, PaymobService>();

// Payment and Contract Workflow Services
builder.Services.AddScoped<IPaymobService, PaymobService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IEscrowService, EscrowService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IPaymentHistoryService, PaymentHistoryService>();
builder.Services.AddScoped<IBookingPaymentService, BookingPaymentService>();
builder.Services.AddScoped<IAdminApprovalService, AdminApprovalService>();
#region Validators
builder.Services.AddValidatorsFromAssemblyContaining<LoginValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<StudentRegisterValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ReviewVerificationValidator>();
builder.Services.AddFluentValidationAutoValidation();
#endregion


// Add CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecific", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
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

builder.Services.AddSignalR();

var app = builder.Build();

// Absolute top of the pipeline: normalize request scheme to https when deployed under reverse proxy
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto))
    {
        context.Request.Scheme = proto.ToString();
    }
    else if (!context.Request.Host.Host.Contains("localhost"))
    {
        context.Request.Scheme = "https";
    }
    await next();
});

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<StudentHousingAPI.Hubs.ChatHub>("/chatHub");

app.Run();
