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
                                var oldName = kv.Key;
                                var newKey = $"{service.Key}_{kv.Key}";
                                openApiDoc.Components.Schemas[newKey] = kv.Value;

                                FixAllReferences(openApiDoc, service.Key, oldName, newKey);

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

        // Thêm đoạn này vào cuối phần merge schemas (trước khi viết JSON)
        private static void FixAllReferences(OpenApiDocument mainDoc, string servicePrefix, string oldSchemaName, string newSchemaName)
        {
            var oldRef = $"#/components/schemas/{oldSchemaName}";
            var newRef = $"#/components/schemas/{newSchemaName}";

            // Duyệt tất cả paths → sửa $ref trong request/response body
            foreach (var path in mainDoc.Paths.Values)
            {
                foreach (var operation in path.Operations.Values)
                {
                    // RequestBody
                    if (operation.RequestBody?.Content != null)
                    {
                        foreach (var media in operation.RequestBody.Content.Values)
                        {
                            ReplaceRef(media.Schema, oldRef, newRef);
                        }
                    }

                    // Responses
                    if (operation.Responses != null)
                    {
                        foreach (var response in operation.Responses.Values)
                        {
                            if (response.Content != null)
                            {
                                foreach (var media in response.Content.Values)
                                {
                                    ReplaceRef(media.Schema, oldRef, newRef);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void ReplaceRef(OpenApiSchema schema, string oldRef, string newRef)
        {
            if (schema == null) return;

            // Kiểm tra nếu schema đang trỏ đến ref cũ
            if (schema.Reference != null && schema.Reference.ReferenceV3 == oldRef)
            {
                // TẠO MỚI TOÀN BỘ REFERENCE – ĐÂY LÀ CÁCH DUY NHẤT HOẠT ĐỘNG VỚI Microsoft.OpenApi >= 1.6
                schema.Reference = new OpenApiReference
                {
                    Type = ReferenceType.Schema,
                    Id = newRef.Split('/').Last(),           // ví dụ: "catalog_ProductStatus"
                                                             // Không cần gán ReferenceV3 – nó sẽ tự sinh từ Id + Type
                };
            }

            // Đệ quy cho tất cả schema con
            if (schema.Properties != null)
                foreach (var prop in schema.Properties.Values)
                    ReplaceRef(prop, oldRef, newRef);

            if (schema.Items != null)
                ReplaceRef(schema.Items, oldRef, newRef);

            if (schema.AllOf != null)
                foreach (var s in schema.AllOf)
                    ReplaceRef(s, oldRef, newRef);

            if (schema.OneOf != null)
                foreach (var s in schema.OneOf)
                    ReplaceRef(s, oldRef, newRef);

            if (schema.AnyOf != null)
                foreach (var s in schema.AnyOf)
                    ReplaceRef(s, oldRef, newRef);
        }
    }
}
