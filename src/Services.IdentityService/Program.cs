using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Services.IdentityService.Data;
using Microsoft.IdentityModel.Tokens;
using Services.IdentityService.Utils;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Reflection;
using OpenTelemetry.Trace;
using Services.IdentityService.Security;
using Services.IdentityService;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// config db
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");


// Add DbContext + Identity
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(conn, npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddPasswordValidator<PasswordValidator<AppUser>>() 
.AddDefaultTokenProviders();

// Override password hasher bằng Argon2
builder.Services.AddScoped<IPasswordHasher<AppUser>, Argon2PasswordHasher<AppUser>>();

// ✅ Add Duende IdentityServer
builder.Services.AddIdentityServer(options =>
{
    options.EmitStaticAudienceClaim = true;
})
.AddAspNetIdentity<AppUser>()
.AddInMemoryIdentityResources(IdentityServerConfig.IdentityResources)
.AddInMemoryApiScopes(IdentityServerConfig.ApiScopes)
.AddInMemoryClients(IdentityServerConfig.Clients)
.AddSigningCredential(RsaKeyProvider.GetSigningCredentials());

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
})
.AddJwtBearer("JwtBearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddScoped<JwtTokenGenerator>();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Identity API", Version = "v1" });

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
        Url = "/identity" // 👈 quan trọng
    });

});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

// Cấu hình Serilog
builder.Host.UseSerilog((ctx, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .WriteTo.Console());

// Bật OpenTelemetry (trace/log)
builder.Services.AddOpenTelemetry()
    .WithTracing(b =>
    {
        b.AddAspNetCoreInstrumentation()
         .AddHttpClientInstrumentation()
         .AddConsoleExporter();
    });

builder.Services.AddHttpClient();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseDeveloperExceptionPage();
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    // URL Swagger JSON thông qua gateway (qua /identity)
    c.SwaggerEndpoint("/identity/swagger/v1/swagger.json", "Identity API V1");
    c.RoutePrefix = "swagger";

});


using (var scope = app.Services.CreateScope())
{
   
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    await SeedData.EnsureSeedDataAsync(scope.ServiceProvider);
}



app.UseIdentityServer();
app.UseAuthorization();

app.MapControllers();
app.Run();