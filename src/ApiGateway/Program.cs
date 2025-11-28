using ApiGateway.Middlewares;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Threading.RateLimiting;


// SERILOG CONFIGURATION 
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File("Logs/gateway-.log", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Information()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

//Serilog
builder.Host.UseSerilog();


// IdentityServer authority-based validation, BFF
//tự động fetch public key từ IdentityServer qua endpoint /.well-known/openid-configuration/jwks
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";         // BFF: Cookie lưu session người dùng
    options.DefaultChallengeScheme = "oidc";   // Khi cần login → dùng OpenID Connect
})
.AddCookie("Cookies")
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = "http://localhost:5001";  // IdentityService (Duende)
    options.ClientId = "elaris_bff";
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.RequireHttpsMetadata = false;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("elaris.api");
    options.Scope.Add("offline_access");
})
.AddJwtBearer("JwtBearer", options =>
{
    options.Authority = "http://identityservice:8080"; // dùng cho internal API call
    options.Audience = "elaris.api";
    options.RequireHttpsMetadata = false;
});

// Reverse Proxy 
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));


// Authorization
builder.Services.AddAuthorization();

//Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b =>
        b.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader());
});

// Output Cache
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy =>
        policy.Expire(TimeSpan.FromSeconds(10))
              .SetVaryByHeader("Accept"));

    options.AddPolicy("catalog-cache", policy =>
        policy.Expire(TimeSpan.FromSeconds(30)));

    options.AddPolicy("no-cache", b => b.NoCache());
});

// OpenTelemetry Setup 
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Elaris.ApiGateway"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://otel-collector:4317");
        }))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter());

builder.Logging.AddOpenTelemetry(o => {
    o.IncludeScopes = true;
    o.ParseStateValues = true;
    o.AddOtlpExporter(ot => ot.Endpoint = new Uri("http://otel-collector:4317"));
});


// Add Swagger UI cho Gateway
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Elaris Marketplace Gateway",
        Version = "v1",
        Description = "Unified API Gateway for all Elaris services"
    });

});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("fixed", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromSeconds(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }
        );
    });

    // 2️⃣ Giới hạn theo Client ID (token bucket)
    options.AddPolicy("token", httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? "anonymous",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,                 // tối đa 20 tokens
                TokensPerPeriod = 10,            // refill 10 tokens
                ReplenishmentPeriod = TimeSpan.FromSeconds(5),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }
        )
    );

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Thêm Health check Để giám sát service qua Docker compose
builder.Services.AddHealthChecks();


var app = builder.Build();

// Health Check
app.MapHealthChecks("/health");

// Cors
app.UseCors("AllowAll");

// Logging
app.UseMiddleware<LoggingMiddleware>();

// Output Cache
app.UseOutputCache();

// RateLimit
app.UseRateLimiter();

//Endpoint / connect/token là public (người dùng cần đăng nhập), nên nếu bạn bật UseAuthentication() toàn cục, cần thêm rule bypass.
app.Use(async (context, next) =>
{
    // Bỏ qua xác thực cho đường dẫn login của IdentityServer
    if (context.Request.Path.StartsWithSegments("/identity/connect/token"))
    {
        await next();
        return;
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();


// Swagger
app.UseMiddleware<SwaggerAggregatorMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Elaris Unified API");

    // Thêm tab cho từng service
    c.SwaggerEndpoint("/swagger/identity/swagger.json", "Identity Service");
    c.SwaggerEndpoint("/swagger/catalog/swagger.json", "Catalog Service");
    c.SwaggerEndpoint("/swagger/order/swagger.json", "Order Service");
    c.SwaggerEndpoint("/swagger/basket/swagger.json", "Basket Service");
    c.SwaggerEndpoint("/swagger/inventory/swagger.json", "Inventory Service");
    c.SwaggerEndpoint("/swagger/payment/swagger.json", "Payment Service");


    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Elaris Marketplace - All APIs";

});

app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path;

    if (path.StartsWithSegments("/swagger") ||
        path.StartsWithSegments("/health") ||
        path.StartsWithSegments("/identity/connect/token"))
    {
        // bypass rate-limit
        await next();
        return;
    }

    await next();
});

app.MapGet("/login", async context =>
{
    await context.ChallengeAsync("oidc", new AuthenticationProperties
    {
        RedirectUri = "/"
    });
}).RequireRateLimiting("fixed");

// Reverse Proxy
app.MapReverseProxy();

app.Run();
