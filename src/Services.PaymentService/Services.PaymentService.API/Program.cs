
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Services.PaymentService.API.Grpc;
using Services.PaymentService.Application.Payments.Commands.CreatePayment;
using Services.PaymentService.Infrastructure.Extensions;
using Services.PaymentService.Infrastructure.Persistence;
using BuildingBlocks.Infrastucture.Authentication;
using Microsoft.OpenApi;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ==================== DATABASE CONNECTION ====================
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
           ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

builder.Services.AddInfrastructure(conn);

// ==================== MEDIATR ====================
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreatePaymentCommand).Assembly));

// ==================== OPENTELEMETRY ====================
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Services.PaymentService"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit")
        .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317")))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("MassTransit")
        .AddPrometheusExporter());

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317"));
});


/// ==================== JWT AUTHENTICATION ====================
builder.Services.AddJwtAuthentication(
    builder.Configuration,
    authorityUrl: "http://identityservice:8080",
    audience: "elaris.api"
);


// ==================== AUTHORIZATION POLICIES ====================
builder.Services.AddAuthorizationPolicies();


// ==================== SWAGGER ====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Payment Service API",
        Version = "v1",
        Description = "Payment service for Elaris e-commerce platform"
    });

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

    c.EnableAnnotations();

});

builder.Services.AddHttpClient<IPaymentRepository, PaymentRepository>();

// ==================== GRPC ====================
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// ==================== KESTREL - MULTIPLE PORTS ====================
builder.WebHost.ConfigureKestrel(options =>
{
    // Port 8081: gRPC endpoint nội bộ (HTTP/2)
    // Dùng cho giao tiếp giữa các services (OrderService → PaymentService)
    options.ListenAnyIP(8081, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);

    // Port 8080: REST API + Swagger endpoint (HTTP/1.1)
    // Dùng cho API Gateway và external clients
    options.ListenAnyIP(8080, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
});

var app = builder.Build();

// ==================== MIDDLEWARE PIPELINE ====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment API V1");
    });
}

// ==================== DATABASE MIGRATION ====================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

// ==================== AUTHENTICATION & AUTHORIZATION ====================
// THỨ TỰ MIDDLEWARE RẤT QUAN TRỌNG:
app.UseAuthentication();    // 1. Xác thực user (đọc và verify JWT token)
app.UseAuthorization();     // 2. Phân quyền (check policies)

// ==================== ENDPOINT MAPPING ====================
app.MapGrpcService<PaymentGrpcService>();  // gRPC endpoints trên port 8081
app.MapGet("/", () => "PaymentService is running...");
app.MapControllers();                      // REST API controllers

app.Run();