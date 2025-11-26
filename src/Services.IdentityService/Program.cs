using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Services.IdentityService.Data;
using OpenTelemetry.Trace;
using Services.IdentityService.Security;
using Services.IdentityService;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ==================== LOGGING CONFIGURATION ====================
// Cấu hình Serilog cho structured logging
builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)  // Đọc config từ appsettings.json
       .Enrich.FromLogContext()                     // Thêm thông tin context vào log
       .WriteTo.Console()                           // Ghi log ra console
       .WriteTo.File("Logs/identity-.log",          // Ghi log ra file
                     rollingInterval: RollingInterval.Day);  // Tạo file mới mỗi ngày
});



// ==================== DATABASE CONFIGURATION ====================
// Lấy connection string từ appsettings.json hoặc biến môi trường
// Ưu tiên appsettings.json, fallback sang environment variable nếu không có
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

// Đăng ký DbContext với PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(conn, npgsql =>
        npgsql.EnableRetryOnFailure()));  // Tự động retry khi kết nối database thất bại



// ==================== ASP.NET IDENTITY CONFIGURATION ====================
// Cấu hình ASP.NET Identity để quản lý user, role, password
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Giảm độ phức tạp của password cho môi trường dev/test
    options.Password.RequireNonAlphanumeric = false;  // Không bắt buộc ký tự đặc biệt
    options.Password.RequireUppercase = false;        // Không bắt buộc chữ hoa
})
.AddEntityFrameworkStores<AppDbContext>()           // Lưu trữ user/role trong database
.AddPasswordValidator<PasswordValidator<AppUser>>() // Thêm password validator tùy chỉnh
.AddDefaultTokenProviders();                        // Token providers cho reset password, email confirmation...

// Override password hasher mặc định bằng Argon2 (bảo mật hơn)
// Argon2 là thuật toán hash hiện đại, chống được GPU/ASIC attacks tốt hơn PBKDF2
builder.Services.AddScoped<IPasswordHasher<AppUser>, Argon2PasswordHasher<AppUser>>();

// ==================== DUENDE IDENTITYSERVER CONFIGURATION ====================
// Cấu hình IdentityServer - OAuth2/OIDC server
builder.Services.AddIdentityServer(options =>
{
    // Thêm audience claim vào token (yêu cầu của một số client)
    options.EmitStaticAudienceClaim = true;

    // IssuerUri là URL mà client sẽ dùng để verify token
    // Phải là URL mà các services khác có thể truy cập được
    options.IssuerUri = builder.Configuration["IdentityServer:IssuerUri"]
                        ?? "http://identityservice:8080";

    // ===== DISABLE Automatic Key Management =====
    options.KeyManagement.Enabled = false;  // Tắt auto key rotation
})
.AddAspNetIdentity<AppUser>()  // Tích hợp với ASP.NET Identity để lấy thông tin user
.AddInMemoryIdentityResources(IdentityServerConfig.IdentityResources)  // Cấu hình Identity Resources (openid, profile, email)
.AddInMemoryApiResources(IdentityServerConfig.ApiResources)            // Cấu hình API Resources
.AddInMemoryApiScopes(IdentityServerConfig.ApiScopes)                  // Cấu hình API Scopes (elaris.api)
.AddInMemoryClients(IdentityServerConfig.Clients)                      // Cấu hình Clients (elaris_web, elaris_bff)
.AddSigningCredential(RsaKeyProvider.GetSigningCredentials());         // RSA key để ký JWT tokens

// ==================== AUTHENTICATION CONFIGURATION ====================
builder.Services.AddAuthentication(options =>
{
    // Default scheme cho browser-based requests
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies", opts =>
{
    opts.Cookie.Name = "elaris.bff";
    opts.Cookie.SameSite = SameSiteMode.Strict;
    opts.LoginPath = "/account/login";
    opts.LogoutPath = "/account/logout";
})
.AddOpenIdConnect("oidc", opts =>
{
    opts.Authority = builder.Configuration["IdentityServer:Authority"]
                ?? builder.Configuration["IdentityServer:IssuerUri"]
                ?? "http://identityservice:8080";
    opts.ClientId = "elaris_bff";
    opts.ClientSecret = "secret";
    opts.ResponseType = "code";
    opts.UsePkce = true;
    opts.RequireHttpsMetadata = false;
    opts.Scope.Add("openid");
    opts.Scope.Add("profile");
    opts.Scope.Add("email");
    opts.Scope.Add("elaris.api");
    opts.Scope.Add("offline_access");
    opts.SaveTokens = true;
    opts.GetClaimsFromUserInfoEndpoint = true;
    opts.TokenValidationParameters.NameClaimType = "name";
    opts.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
})
// ===== THÊM JWT BEARER AUTHENTICATION =====
.AddJwtBearer("Bearer", options =>
{
    // Authority là chính IdentityService này
    options.Authority = builder.Configuration["IdentityServer:IssuerUri"]
                        ?? "http://identityservice:8080";

    options.RequireHttpsMetadata = false;
    options.Audience = "elaris.api";

    // ===== Pre-fetch JWKS với retry =====
    Console.WriteLine("[IdentityService] Pre-fetching JWKS for Bearer authentication...");

    var oidcConfig = FetchOidcConfigurationWithRetry(
        "http://identityservice:8080",
        maxRetries: 5,
        retryDelay: TimeSpan.FromSeconds(2)
    );

    if (oidcConfig != null)
    {
        options.Configuration = oidcConfig;
        Console.WriteLine($"[IdentityService] ✅ JWKS fetched successfully with {oidcConfig.SigningKeys.Count} keys");
    }
    else
    {
        Console.WriteLine("[IdentityService] ⚠️ Failed to pre-fetch JWKS, will use lazy loading");
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuers = new[] { "http://identityservice:8080" },
        ValidateAudience = true,
        ValidAudience = "elaris.api",
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        NameClaimType = "name",
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[IdentityService] ❌ JWT Bearer auth failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userName = context.Principal?.FindFirst("name")?.Value;
            var userId = context.Principal?.FindFirst("sub")?.Value;
            Console.WriteLine($"[IdentityService] ✅ JWT Bearer validated for: {userName} ({userId})");
            return Task.CompletedTask;
        }
    };
});

// ===== Helper function để fetch JWKS =====
static OpenIdConnectConfiguration? FetchOidcConfigurationWithRetry(
    string authorityUrl,
    int maxRetries = 10,
    TimeSpan? retryDelay = null)
{
    var delay = retryDelay ?? TimeSpan.FromSeconds(3);

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            Console.WriteLine($"[IdentityService] Fetching JWKS (attempt {i + 1}/{maxRetries})...");

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            var jwksUrl = $"{authorityUrl}/.well-known/openid-configuration/jwks";
            var jwksJson = httpClient.GetStringAsync(jwksUrl).Result;
            var jwks = new Microsoft.IdentityModel.Tokens.JsonWebKeySet(jwksJson);

            var config = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
            {
                Issuer = authorityUrl,
                JwksUri = jwksUrl
            };

            foreach (var key in jwks.Keys)
            {
                config.SigningKeys.Add(key);
            }

            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[IdentityService] ❌ Attempt {i + 1} failed: {ex.Message}");

            if (i < maxRetries - 1)
            {
                Thread.Sleep(delay);
            }
        }
    }

    return null;
}

builder.Services.AddAuthorization(options =>
{
    // ==================== BUYER POLICIES ====================
    options.AddPolicy("Buyer", policy =>
    {
        policy.AuthenticationSchemes.Clear(); // ← xóa mặc định
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);


        policy.RequireAuthenticatedUser();
        policy.RequireRole("buyer");
        policy.RequireClaim("scope", "elaris.api");
    });

    // ==================== SELLER POLICIES ====================
    options.AddPolicy("Seller", policy =>
    {
        policy.AuthenticationSchemes.Clear(); // ← xóa mặc định
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);


        policy.RequireAuthenticatedUser();
        policy.RequireRole("seller");
        policy.RequireClaim("scope", "elaris.api");
    });

    // ==================== ADMIN POLICIES ====================
    options.AddPolicy("Admin", policy =>
    {
        policy.AuthenticationSchemes.Clear(); // ← xóa mặc định
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);

        policy.RequireAuthenticatedUser();
        policy.RequireRole("admin");
        policy.RequireClaim("scope", "elaris.api");
    });

    // ==================== COMBINED POLICIES ====================

    // Buyer hoặc Seller (đăng nhập rồi)
    options.AddPolicy("BuyerOrSeller", policy =>
    {
        policy.AuthenticationSchemes.Clear(); // ← xóa mặc định
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);

        policy.RequireAuthenticatedUser();
        policy.RequireRole("buyer", "seller");
        policy.RequireClaim("scope", "elaris.api");
    });

    // Seller hoặc Admin
    options.AddPolicy("SellerOrAdmin", policy =>
    {
        policy.AuthenticationSchemes.Clear(); // ← xóa mặc định
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);

        policy.RequireAuthenticatedUser();
        policy.RequireRole("seller", "admin");
        policy.RequireClaim("scope", "elaris.api");
    });

    // Bất kỳ ai đăng nhập (3 roles)
    options.AddPolicy("Authenticated", policy =>
    {
        policy.AuthenticationSchemes.Clear(); // ← xóa mặc định
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);

        policy.RequireAuthenticatedUser();
        policy.RequireRole("buyer", "seller", "admin");
        policy.RequireClaim("scope", "elaris.api");
    });
});

// ==================== SWAGGER CONFIGURATION ====================
// Cấu hình Swagger để document API
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Identity API", Version = "v1" });

    // Cấu hình base path khi service chạy sau API Gateway
    // Gateway sẽ route /identity/* tới service này
    c.AddServer(new OpenApiServer
    {
        Url = "/identity" // Base path khi truy cập qua gateway
    });

    c.EnableAnnotations();
});


// ==================== OPENTELEMETRY CONFIGURATION ====================
// Cấu hình OpenTelemetry cho observability (traces, metrics, logs)
builder.Services.AddOpenTelemetry()
    // Đặt tên service để phân biệt trong monitoring system
    .ConfigureResource(r => r.AddService("Services.IdentityService"))

    // Distributed Tracing - theo dõi request qua nhiều services
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()   // Trace ASP.NET Core requests
        .AddHttpClientInstrumentation()   // Trace HTTP client calls
        .AddSource("MassTransit")         // Trace MassTransit messages (nếu dùng)
        .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317")))  // Gửi traces tới OpenTelemetry Collector

    // Metrics - thu thập số liệu hiệu suất
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()   // Metrics từ ASP.NET Core (request count, duration...)
        .AddHttpClientInstrumentation()   // Metrics từ HTTP client
        .AddMeter("MassTransit")          // Metrics từ MassTransit
        .AddPrometheusExporter());        // Export metrics theo format Prometheus

// Logs - gửi logs tới OpenTelemetry Collector
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;       // Bao gồm logging scopes
    options.ParseStateValues = true;    // Parse structured log data
    options.AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317"));
});

// ==================== CORS CONFIGURATION ====================
// Cấu hình CORS để cho phép frontend gọi API từ domain khác
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", b =>
        b.AllowAnyOrigin()      // Cho phép mọi origin (CHỈ dùng dev, production cần restrict)
         .AllowAnyHeader()      // Cho phép mọi header
         .AllowAnyMethod());     // Cho phép mọi HTTP method
});

// ==================== BASIC SERVICES ====================
builder.Services.AddControllers();          // MVC Controllers
builder.Services.AddHttpClient();           // HttpClient factory
builder.Services.AddEndpointsApiExplorer(); // API Explorer cho Swagger
builder.Services.AddHealthChecks();         // Health check endpoint

// ==================== BUILD APPLICATION ====================
var app = builder.Build();

// ==================== MIDDLEWARE PIPELINE ====================
app.UseCors("AllowAll");            // CORS - phải đặt đầu pipeline
app.UseSerilogRequestLogging();     // Log mỗi HTTP request

app.UseDeveloperExceptionPage();    // Hiển thị exception chi tiết (CHỈ dùng dev)

app.UseRouting();                   // Routing middleware

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // URL Swagger JSON thông qua gateway
    // Gateway route /identity/* tới service này
    c.SwaggerEndpoint("/identity/swagger/v1/swagger.json", "Identity API V1");
    c.RoutePrefix = "swagger";  // Swagger UI tại /swagger
});

// ==================== DATABASE MIGRATION & SEED DATA ====================
// Migrate database và seed dữ liệu ban đầu khi ứng dụng khởi động
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Chạy migration để tạo/update database schema
    context.Database.Migrate();

    // Seed dữ liệu mẫu (users, roles...)
    await SeedData.EnsureSeedDataAsync(scope.ServiceProvider);
}

// ==================== AUTHENTICATION & AUTHORIZATION ====================
// THỨ TỰ middleware RẤT QUAN TRỌNG:
app.UseAuthentication();    // 1. Xác thực user (đọc cookie/token)
app.UseIdentityServer();    // 2. IdentityServer endpoints (/connect/token, /.well-known/...)
app.UseAuthorization();     // 3. Phân quyền (check policies)

// ==================== ENDPOINT MAPPING ====================
app.MapControllers();       // Map các controller endpoints

// Redirect root path tới OpenID configuration
// Giúp dev/client dễ dàng xem cấu hình OIDC
//app.MapGet("/", () => Results.Redirect("/.well-known/openid-configuration"));

// Health check endpoint cho monitoring
app.MapHealthChecks("/health");

// ==================== START APPLICATION ====================
app.Run();