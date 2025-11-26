using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace BuildingBlocks.Infrastucture.Authentication
{
    public static class JwtAuthenticationHelper
    {
        /// <summary>
        /// Thêm JWT authentication với retry logic và manual key fetching
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            string authorityUrl = "http://identityservice:8080",
            string audience = "elaris.api")
        {
            // ==================== STEP 1: PRE-FETCH JWKS WITH RETRY ====================
            Console.WriteLine("[JwtAuth] Fetching JWKS from IdentityServer...");

            var oidcConfig = FetchOidcConfigurationWithRetry(
                authorityUrl,
                maxRetries: 10,
                retryDelay: TimeSpan.FromSeconds(3));

            if (oidcConfig == null)
            {
                throw new InvalidOperationException("Failed to fetch OIDC configuration from IdentityServer");
            }

            Console.WriteLine($"[JwtAuth] ✅ Successfully fetched OIDC config with {oidcConfig.SigningKeys.Count} signing keys");

            // ==================== STEP 2: CONFIGURE JWT BEARER ====================
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authorityUrl;
                    options.Audience = audience;
                    options.RequireHttpsMetadata = false;

                    // ===== Sử dụng pre-fetched configuration =====
                    options.Configuration = oidcConfig;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuers = new[] { authorityUrl },
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeys = oidcConfig.SigningKeys,
                        NameClaimType = "name",
                        RoleClaimType = "role",
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"[JwtAuth] ❌ Authentication failed: {context.Exception.Message}");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var userName = context.Principal?.FindFirst("name")?.Value;
                            var userId = context.Principal?.FindFirst("sub")?.Value;
                            Console.WriteLine($"[JwtAuth] ✅ Token validated for user: {userName} (ID: {userId})");
                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        /// <summary>
        /// Fetch OIDC configuration với retry logic
        /// </summary>
        private static OpenIdConnectConfiguration? FetchOidcConfigurationWithRetry(
            string authorityUrl,
            int maxRetries = 10,
            TimeSpan? retryDelay = null)
        {
            var delay = retryDelay ?? TimeSpan.FromSeconds(3);

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    Console.WriteLine($"[JwtAuth] Attempt {i + 1}/{maxRetries} to fetch OIDC configuration...");

                    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                    // Fetch JWKS
                    var jwksUrl = $"{authorityUrl}/.well-known/openid-configuration/jwks";
                    var jwksJson = httpClient.GetStringAsync(jwksUrl).Result;
                    var jwks = new JsonWebKeySet(jwksJson);

                    // Create configuration
                    var config = new OpenIdConnectConfiguration
                    {
                        Issuer = authorityUrl,
                        JwksUri = jwksUrl
                    };

                    // Add signing keys
                    foreach (var key in jwks.Keys)
                    {
                        config.SigningKeys.Add(key);
                    }

                    Console.WriteLine($"[JwtAuth] ✅ Fetched {jwks.Keys.Count} signing keys");
                    foreach (var key in jwks.Keys)
                    {
                        Console.WriteLine($"[JwtAuth]   - Key ID: {key.KeyId}");
                    }

                    return config;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[JwtAuth] ❌ Attempt {i + 1} failed: {ex.Message}");

                    if (i < maxRetries - 1)
                    {
                        Console.WriteLine($"[JwtAuth] Retrying in {delay.TotalSeconds} seconds...");
                        Thread.Sleep(delay);
                    }
                }
            }

            Console.WriteLine($"[JwtAuth] ❌ Failed to fetch OIDC configuration after {maxRetries} attempts");
            return null;
        }

        /// <summary>
        /// Thêm authorization policies chuẩn
        /// </summary>
        public static IServiceCollection AddStandardAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Policy: Chỉ Admin
                options.AddPolicy("AdminOnly", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("admin");
                    policy.RequireClaim("scope", "elaris.api");
                });

                // Policy: User hoặc Admin
                options.AddPolicy("UserOrAdmin", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole("user", "admin");
                    policy.RequireClaim("scope", "elaris.api");
                });

                // Policy: Chỉ cần có scope
                options.AddPolicy("ApiAccess", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", "elaris.api");
                });
            });

            return services;
        }
    }
}
