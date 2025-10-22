using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Services.CatalogService.Data;
using Services.CatalogService.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoContext>();

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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
                Encoding.UTF8.GetBytes("supersecretkey_please_change_this_in_prod")),

            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// CRUD
app.MapGet("/api/products", async (MongoContext db) =>
{
    var products = await db.Products.Find(x => !x.IsDeleted).ToListAsync();
    return Results.Ok(products);
});

app.MapPost("/api/products", async (MongoContext db, Product p) =>
{
    await db.Products.InsertOneAsync(p);
    return Results.Created($"/api/products/{p.Id}", p);
}).RequireAuthorization("AdminOnly");

app.MapPut("/api/products/{id}", async (MongoContext db, string id, Product updated) =>
{
    var result = await db.Products.ReplaceOneAsync(x => x.Id == id, updated);
    return result.ModifiedCount > 0 ? Results.Ok(updated) : Results.NotFound();
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/products/{id}", async (MongoContext db, string id) =>
{
    var update = Builders<Product>.Update.Set(p => p.IsDeleted, true);
    await db.Products.UpdateOneAsync(x => x.Id == id, update);
    return Results.NoContent();
}).RequireAuthorization("AdminOnly");



app.Run();
