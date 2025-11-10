using ApiGateway.Middlewares;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;


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

// Reverse Proxy 
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// IdentityServer authority-based validation
//tự động fetch public key từ IdentityServer qua endpoint /.well-known/openid-configuration/jwks
builder.Services.AddAuthentication("JwtBearer")
    .AddJwtBearer("JwtBearer", options =>
    {
        options.Authority = "http://identityservice:8080"; // địa chỉ nội bộ trong Docker
        options.Audience = "elaris.api";                   // scope khớp với IdentityServerConfig
        options.RequireHttpsMetadata = false;              // dev only
    });


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

// OpenTelemetry Setup 
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Elaris.ApiGateway"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://otel-collector:4317");
        }));

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

// Thêm Health check Để giám sát service qua Docker compose
builder.Services.AddHealthChecks();


var app = builder.Build();

// Health Check
app.MapHealthChecks("/health");

// Cors
app.UseCors("AllowAll");

// Logging
app.UseMiddleware<LoggingMiddleware>();

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

// Reverse Proxy
app.MapReverseProxy();

app.Run();
