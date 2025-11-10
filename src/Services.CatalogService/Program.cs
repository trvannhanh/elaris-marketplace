using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Services.CatalogService.Data;
using Services.CatalogService.Models;
using System.Text;
using BuildingBlocks.Contracts.Events;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using Serilog;
using Serilog.Enrichers.Span;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoContext>();



// Duende IdentityServer Authorize
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var auth = builder.Configuration.GetSection("Authentication");
        options.Authority = auth["Authority"];
        options.Audience = auth["Audience"];
        options.RequireHttpsMetadata = bool.Parse(auth["RequireHttpsMetadata"] ?? "false");

        // Đảm bảo role claim mapping chính xác
        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", //claim của duende identityserver nó như dậy thiệt ớ
            NameClaimType = "name"
        };

        // Log chi tiết token validation (debug)
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"❌ JWT Auth failed: {ctx.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                Console.WriteLine("✅ Token validated!");
                foreach (var c in ctx.Principal!.Claims)
                    Console.WriteLine($"CLAIM: {c.Type} = {c.Value}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();


// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

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

    c.AddServer(new OpenApiServer
    {
        Url = "/catalog" // 👈 quan trọng
    });

});

//Custom Role
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
       policy.RequireRole("admin"));
});

// Resource: Metadata cho service (hiển thị trong Jaeger/Tempo)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: builder.Environment.ApplicationName, serviceVersion: "1.0.0"))  // Thay "catalogservice" hoặc "orderservice"
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()  // Trace HTTP requests
        .AddHttpClientInstrumentation()  // Trace outgoing HTTP
        //.AddEntityFrameworkCoreInstrumentation()  // Trace DB (EF Core)
        .AddSource("MassTransit")  // Built-in MassTransit trace
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4317");  // OTLP/gRPC cho Collector (sẽ setup sau)
            options.Protocol = OtlpExportProtocol.Grpc;  // Hoặc Http2
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("MassTransit")  // MassTransit metrics (queue length, message count)
        .AddPrometheusExporter()  // Export sang Prometheus scrape endpoint /metrics
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4318");
        }));


//MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(Program).Assembly); // Nếu sau này có consumer ở Catalog

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});


// Serilog
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .Enrich.WithSpan() // 👈 Lấy trace/span id từ OpenTelemetry context
      .WriteTo.Console(outputTemplate:
          "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} (TraceId={TraceId}, SpanId={SpanId}){NewLine}{Exception}");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
    var indexKeys = Builders<Product>.IndexKeys
        .Text(p => p.Name)
        .Text(p => p.Description);
    await ctx.Products.Indexes.CreateOneAsync(new CreateIndexModel<Product>(indexKeys));

    var compoundKeys = Builders<Product>.IndexKeys
        .Ascending(p => p.Price)
        .Descending(p => p.CreatedAt);
    await ctx.Products.Indexes.CreateOneAsync(new CreateIndexModel<Product>(compoundKeys));
}

// Endpoint cho Prometheus scrape metrics
app.UseOpenTelemetryPrometheusScrapingEndpoint();  // /metrics

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// CRUD
app.MapGet("/api/products", async ([AsParameters] ProductQueryDto query, MongoContext db) =>
{
    var filterBuilder = Builders<Product>.Filter;
    var filter = filterBuilder.Eq(p => p.IsDeleted, false);

    // 🔍 Fulltext search (Name / Description)
    if (!string.IsNullOrEmpty(query.Search))
    {
        var textFilter = filterBuilder.Or(
            filterBuilder.Regex(p => p.Name, new MongoDB.Bson.BsonRegularExpression(query.Search, "i")),
            filterBuilder.Regex(p => p.Description, new MongoDB.Bson.BsonRegularExpression(query.Search, "i"))
        );
        filter &= textFilter;
    }

    // 💰 Price range
    if (query.MinPrice.HasValue)
        filter &= filterBuilder.Gte(p => p.Price, query.MinPrice.Value);
    if (query.MaxPrice.HasValue)
        filter &= filterBuilder.Lte(p => p.Price, query.MaxPrice.Value);

    // 🧾 Sorting
    var sortBuilder = Builders<Product>.Sort;
    var sortField = query.SortBy?.ToLowerInvariant() ?? "createdat";
    var sort = query.SortOrder?.ToLowerInvariant() == "asc"
        ? sortBuilder.Ascending(sortField)
        : sortBuilder.Descending(sortField);

    // 📄 Paging
    var skip = (query.Page - 1) * query.PageSize;

    var total = await db.Products.CountDocumentsAsync(filter);
    var items = await db.Products
        .Find(filter)
        .Sort(sort)
        .Skip(skip)
        .Limit(query.PageSize)
        .ToListAsync();

    var result = new
    {
        query.Page,
        query.PageSize,
        Total = total,
        TotalPages = (int)Math.Ceiling(total / (double)query.PageSize),
        Items = items
    };

    return Results.Ok(result);
});

app.MapGet("/api/products/{id}", async (MongoContext db, string id) =>
{
    var product = await db.Products
        .Find(x => x.Id == id && !x.IsDeleted)
        .FirstOrDefaultAsync();

    return product is not null
        ? Results.Ok(product)
        : Results.NotFound();
});

app.MapPost("/api/products", async (MongoContext db, Product p, IPublishEndpoint publisher) =>
{
    await db.Products.InsertOneAsync(p);
    await publisher.Publish(new ProductCreatedEvent(
        p.Id!,
        p.Name,
        p.Price,
        p.CreatedAt
    ));
    Log.Information("✅ ProductCreatedEvent Published for {ProductId}", p.Id);
    return Results.Created($"/api/products/{p.Id}", p);
}).RequireAuthorization("AdminOnly");

app.MapPut("/api/products/{id}", async (MongoContext db, string id, Product updated) =>
{
    var result = await db.Products.ReplaceOneAsync(x => x.Id == id, updated);
    return result.ModifiedCount > 0 ? Results.Ok(updated) : Results.NotFound();
}).RequireAuthorization("AdminOnly");

app.MapPatch("/api/products/{id}/price", async (string id, decimal newPrice, MongoContext db, IPublishEndpoint publisher) =>
{
    var product = await db.Products.Find(p => p.Id == id && !p.IsDeleted).FirstOrDefaultAsync();
    if (product == null) return Results.NotFound();

    var old = product.Price;
    var update = Builders<Product>.Update.Set(p => p.Price, newPrice);
    var res = await db.Products.UpdateOneAsync(p => p.Id == id, update);

    if (res.ModifiedCount > 0)
    {
        // publish event
        var ev = new ProductPriceUpdatedEvent(id, old, newPrice, DateTime.UtcNow);
        await publisher.Publish(ev);
        return Results.Ok(new { productId = id, oldPrice = old, newPrice });
    }

    return Results.BadRequest();
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/products/{id}", async (MongoContext db, string id) =>
{
    var update = Builders<Product>.Update.Set(p => p.IsDeleted, true);
    await db.Products.UpdateOneAsync(x => x.Id == id, update);
    return Results.NoContent();
}).RequireAuthorization("AdminOnly");



app.Run();
