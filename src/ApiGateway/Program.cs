using Microsoft.IdentityModel.Tokens;
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

// Thêm Health check Để giám sát service qua Docker compose
builder.Services.AddHealthChecks();


var app = builder.Build();

app.MapHealthChecks("/health");

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();

app.Run();
