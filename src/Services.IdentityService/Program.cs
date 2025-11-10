using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Services.IdentityService.Data;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;
using Services.IdentityService.Security;
using Services.IdentityService;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console()
       .WriteTo.File("Logs/identity-.log", rollingInterval: RollingInterval.Day);
});



// config db
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");


// DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(conn, npgsql => npgsql.EnableRetryOnFailure()));

// Identity
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

// Add Duende IdentityServer
builder.Services.AddIdentityServer(options =>
{
    options.EmitStaticAudienceClaim = true;
})
.AddAspNetIdentity<AppUser>()
.AddInMemoryIdentityResources(IdentityServerConfig.IdentityResources)
.AddInMemoryApiResources(IdentityServerConfig.ApiResources)
.AddInMemoryApiScopes(IdentityServerConfig.ApiScopes)
.AddInMemoryClients(IdentityServerConfig.Clients)
.AddSigningCredential(RsaKeyProvider.GetSigningCredentials());


// BFF
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies", opts =>
{
    opts.Cookie.Name = "elaris.bff";
    opts.Cookie.SameSite = SameSiteMode.Strict;
    opts.LoginPath = "/account/login";
    opts.LogoutPath = "/account/logout";
})
.AddOpenIdConnect("oidc", opts =>
{
    opts.Authority = "http://localhost:5001"; // chính IdentityServer (self)
    opts.ClientId = "elaris_bff";
    opts.ClientSecret = "secret";
    opts.ResponseType = "code";
    opts.UsePkce = true;
    opts.RequireHttpsMetadata = false;

    opts.Scope.Add("openid");
    opts.Scope.Add("profile");
    opts.Scope.Add("email");
    opts.Scope.Add("elaris.api");
    opts.Scope.Add("offline_access");

    opts.SaveTokens = true;
    opts.GetClaimsFromUserInfoEndpoint = true;

    opts.TokenValidationParameters.NameClaimType = "name";
    opts.TokenValidationParameters.RoleClaimType = "role";
});

// Swagger
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
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("admin");
        policy.RequireClaim("scope", "elaris.api");
    });

    options.AddPolicy("UserOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("user");
        policy.RequireClaim("scope", "elaris.api");
    });
});

// OpenTelemetry (trace/log)
builder.Services.AddOpenTelemetry()
    .WithTracing(b =>
    {
        b.AddAspNetCoreInstrumentation()
         .AddHttpClientInstrumentation()
         .AddConsoleExporter();
    });

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", b =>
        b.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseCors("AllowAll");
app.UseSerilogRequestLogging();

app.UseDeveloperExceptionPage();

app.UseRouting();


app.UseSwagger();

app.UseSwaggerUI(c =>
{
    // URL Swagger JSON thông qua gateway (qua /identity)
    c.SwaggerEndpoint("/identity/swagger/v1/swagger.json", "Identity API V1");
    c.RoutePrefix = "swagger";

});

// Migrate + Seed Data
using (var scope = app.Services.CreateScope())
{
   
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    await SeedData.EnsureSeedDataAsync(scope.ServiceProvider);
}




app.UseAuthentication();    // 1. Đọc cookie
app.UseIdentityServer();    // 2. IdentityServer
app.UseAuthorization();     // 3. Phân quyền

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/.well-known/openid-configuration"));
app.MapHealthChecks("/health");
app.Run();