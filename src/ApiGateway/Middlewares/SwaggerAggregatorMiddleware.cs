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
            var path = context.Request.Path.Value?.TrimEnd('/').ToLowerInvariant();

            if (path?.StartsWith("/swagger/") == true && path.EndsWith("/swagger.json"))
            {
                var serviceKey = path.Split('/')[2]; // e.g. swagger/identity/swagger.json

                if (_swaggerSources.TryGetValue(serviceKey, out var swaggerUrl))
                {
                    using var httpClient = new HttpClient();
                    try
                    {
                        var json = await httpClient.GetStringAsync(swaggerUrl);
                        var reader = new OpenApiStringReader();
                        var doc = reader.Read(json, out _);

                        // Thêm JWT security vào từng document
                        doc.Components ??= new OpenApiComponents();
                        doc.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
                        {
                            ["Bearer"] = new OpenApiSecurityScheme
                            {
                                Type = SecuritySchemeType.Http,
                                Scheme = "bearer",
                                BearerFormat = "JWT",
                                In = ParameterLocation.Header,
                                Description = "Enter JWT token (Bearer {token})"
                            }
                        };
                        doc.SecurityRequirements.Add(new OpenApiSecurityRequirement
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

                        context.Response.ContentType = "application/json";
                        var sb = new StringBuilder();
                        var writer = new OpenApiJsonWriter(new StringWriter(sb));
                        doc.SerializeAsV3(writer);
                        await context.Response.WriteAsync(sb.ToString());
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to fetch swagger for {Service}", serviceKey);
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync($"Error fetching Swagger for {serviceKey}");
                        return;
                    }
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Swagger not found");
                    return;
                }
            }

            await _next(context);
        }
    }
}
