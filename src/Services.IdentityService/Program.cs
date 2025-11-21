using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Services.IdentityService.Data;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;
using Services.IdentityService.Security;
using Services.IdentityService;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;

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

// ==================== BFF AUTHENTICATION CONFIGURATION ====================
// Cấu hình xác thực cho Backend-For-Frontend (BFF) pattern
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";          // Dùng cookie làm authentication scheme mặc định
    options.DefaultChallengeScheme = "oidc";    // Redirect đến OIDC khi cần xác thực
})
.AddCookie("Cookies", opts =>
{
    opts.Cookie.Name = "elaris.bff";            // Tên cookie
    opts.Cookie.SameSite = SameSiteMode.Strict; // Chống CSRF attacks
    opts.LoginPath = "/account/login";          // Đường dẫn trang login
    opts.LogoutPath = "/account/logout";        // Đường dẫn logout
})
.AddOpenIdConnect("oidc", opts =>
{
    // IdentityServer endpoint (chính service này đóng vai trò cả IdentityServer và BFF)
    opts.Authority = "http://localhost:5001";

    // Thông tin client đã config trong IdentityServerConfig
    opts.ClientId = "elaris_bff";
    opts.ClientSecret = "secret";

    // Authorization Code Flow - flow chuẩn OAuth2 cho web app
    opts.ResponseType = "code";

    // PKCE (Proof Key for Code Exchange) - bảo vệ chống code interception
    opts.UsePkce = true;

    // Tắt HTTPS requirement (CHỈ cho dev, production PHẢI bật)
    opts.RequireHttpsMetadata = false;

    // Các scope mà BFF yêu cầu
    opts.Scope.Add("openid");         // Bắt buộc
    opts.Scope.Add("profile");        // Thông tin profile
    opts.Scope.Add("email");          // Email
    opts.Scope.Add("elaris.api");     // Truy cập API
    opts.Scope.Add("offline_access"); // Refresh token

    // Lưu tokens vào cookie để dùng sau
    opts.SaveTokens = true;

    // Gọi UserInfo endpoint để lấy thêm claims
    opts.GetClaimsFromUserInfoEndpoint = true;

    // Mapping claims từ token vào ClaimsPrincipal
    opts.TokenValidationParameters.NameClaimType = "name";  // Claim "name" sẽ là User.Identity.Name
    opts.TokenValidationParameters.RoleClaimType = "role";  // Claim "role" sẽ dùng cho roles
});

// ==================== SWAGGER CONFIGURATION ====================
// Cấu hình Swagger để document API
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Identity API", Version = "v1" });

    // Thêm JWT Bearer authentication vào Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Áp dụng Bearer token cho tất cả endpoints trong Swagger
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] { }
        }
    });

    // Cấu hình base path khi service chạy sau API Gateway
    // Gateway sẽ route /identity/* tới service này
    c.AddServer(new OpenApiServer
    {
        Url = "/identity" // Base path khi truy cập qua gateway
    });
});

// ==================== AUTHORIZATION POLICIES ====================
// Định nghĩa các policy phân quyền
builder.Services.AddAuthorization(options =>
{
    // Policy cho Admin: phải đăng nhập + có role "admin" + có scope "elaris.api"
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();           // Bắt buộc đăng nhập
        policy.RequireRole("admin");                 // Role phải là "admin"
        policy.RequireClaim("scope", "elaris.api");  // Access token phải có scope "elaris.api"
    });

    // Policy cho User: phải đăng nhập + có role "user" + có scope "elaris.api"
    options.AddPolicy("UserOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("user");
        policy.RequireClaim("scope", "elaris.api");
    });
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
         .AllowAnyMethod());    // Cho phép mọi HTTP method
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
app.MapGet("/", () => Results.Redirect("/.well-known/openid-configuration"));

// Health check endpoint cho monitoring
app.MapHealthChecks("/health");

// ==================== START APPLICATION ====================
app.Run();