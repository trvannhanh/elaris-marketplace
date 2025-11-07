using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using System.Text;

namespace ApiGateway.Middlewares
{
    public class SwaggerAggregatorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SwaggerAggregatorMiddleware> _logger;

        // Các service nội bộ và endpoint swagger.json của chúng
        private static readonly Dictionary<string, string> _swaggerSources = new()
        {
            { "identity",  "http://identityservice:8080/swagger/v1/swagger.json" },
            { "catalog",   "http://catalogservice:8080/swagger/v1/swagger.json" },
            { "order",     "http://orderservice:8080/swagger/v1/swagger.json" },
            { "basket",    "http://basketservice:8080/swagger/v1/swagger.json" },
            { "inventory", "http://inventoryservice:8080/swagger/v1/swagger.json" },
            { "payment",   "http://paymentservice:8080/swagger/v1/swagger.json" },
        };

        public SwaggerAggregatorMiddleware(RequestDelegate next, ILogger<SwaggerAggregatorMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Chỉ xử lý khi gọi /swagger/v1/swagger.json
            if (context.Request.Path.Equals("/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase))
            {
                var openApiDoc = new OpenApiDocument
                {
                    Info = new OpenApiInfo
                    {
                        Title = "Elaris Unified API Gateway",
                        Version = "v1",
                        Description = "Aggregated OpenAPI for all Elaris services"
                    },
                    Paths = new OpenApiPaths(),
                    Components = new OpenApiComponents()
                };

                using var httpClient = new HttpClient();

                foreach (var service in _swaggerSources)
                {
                    try
                    {
                        _logger.LogInformation("Fetching Swagger from {Service}...", service.Key);
                        var json = await httpClient.GetStringAsync(service.Value);
                        var reader = new OpenApiStringReader();
                        var doc = reader.Read(json, out _);

                        // Merge các Paths
                        foreach (var path in doc.Paths)
                        {
                            var newPath = $"/{service.Key}{path.Key}";
                            openApiDoc.Paths[newPath] = path.Value;
                        }

                        // Merge các Schemas
                        if (doc.Components?.Schemas != null)
                        {
                            foreach (var kv in doc.Components.Schemas)
                            {
                                var newKey = $"{service.Key}_{kv.Key}";
                                openApiDoc.Components.Schemas[newKey] = kv.Value;

                                // Cập nhật các $ref để không bị conflict
                                foreach (var p in openApiDoc.Paths.Values)
                                {
                                    foreach (var op in p.Operations.Values)
                                    {
                                        if (op.RequestBody?.Content != null)
                                        {
                                            foreach (var media in op.RequestBody.Content.Values)
                                            {
                                                if (media.Schema?.Reference?.Id == kv.Key)
                                                    media.Schema.Reference.Id = newKey;
                                            }
                                        }

                                        if (op.Responses != null)
                                        {
                                            foreach (var resp in op.Responses.Values)
                                            {
                                                foreach (var media in resp.Content.Values)
                                                {
                                                    if (media.Schema?.Reference?.Id == kv.Key)
                                                        media.Schema.Reference.Id = newKey;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        _logger.LogInformation("✅ Added {Service} APIs", service.Key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Failed to fetch Swagger from {Service}", service.Key);
                    }
                }

                // Thêm Bearer token vào global security
                openApiDoc.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Description = "Enter JWT token. Example: 'Bearer {your token}'",
                        Name = "Authorization"
                    }
                };

                openApiDoc.SecurityRequirements.Add(new OpenApiSecurityRequirement
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

                // Xuất ra file JSON tổng hợp
                context.Response.ContentType = "application/json";
                var sb = new StringBuilder();
                var writer = new OpenApiJsonWriter(new StringWriter(sb));
                openApiDoc.SerializeAsV3(writer);
                await context.Response.WriteAsync(sb.ToString());
                return;
            }

            await _next(context);
        }
    }
}
