using ApiGateway.Middlewares;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddAuthentication("JwtBearer")
    .AddJwtBearer("JwtBearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "elaris.identity",
            ValidAudience = "elaris.clients",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("supersecretkey_please_change_this_in_prod"))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", b =>
        b.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader());
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

// Thêm Health check Để giám sát service qua Docker compose
builder.Services.AddHealthChecks();


var app = builder.Build();

// === Pipeline ===
app.MapHealthChecks("/health");

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SwaggerAggregatorMiddleware>();

app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/identity/swagger.json", "Identity Service");
    c.SwaggerEndpoint("/swagger/catalog/swagger.json", "Catalog Service");
    c.SwaggerEndpoint("/swagger/order/swagger.json", "Order Service");
    c.SwaggerEndpoint("/swagger/inventory/swagger.json", "Inventory Service");
    c.SwaggerEndpoint("/swagger/payment/swagger.json", "Payment Service");
    c.SwaggerEndpoint("/swagger/basket/swagger.json", "Basket Service");
});



app.MapReverseProxy();

app.Run();
