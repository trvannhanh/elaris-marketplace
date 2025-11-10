using Duende.IdentityServer.Models;
using Duende.IdentityServer;

namespace Services.IdentityService
{

    //Cấu hình Duende IdentityServer: Identity Resources, API Scopes và Clients
    public static class IdentityServerConfig
    {
        /// Thông tin user sẽ được trả về trong ID Token
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(), // sub (user ID) - bắt buộc
                new IdentityResources.Profile(), // tên, ảnh, hồ sơ...
                new IdentityResources.Email(), // email + email_verified
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("elaris.api", "Elaris API") // scope cho toàn bộ backend
            };

        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client
                {
                    ClientId = "elaris_web",                                        // ID client
                    ClientName = "Elaris Web App",                                  // tên hiển thị
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,           // dùng username + password trực tiếp
                    AllowOfflineAccess = true,                                      // cho phép cấp refresh token
                    AccessTokenLifetime = 7200,                                     // access token sống 2 giờ
                    RefreshTokenExpiration = TokenExpiration.Sliding,               // refresh token gia hạn khi dùng
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,                     // refresh token chỉ dùng 1 lần
                    ClientSecrets = { new Secret("super_secret_client".Sha256()) }, // bí mật client (SHA256)
                    AllowedScopes = {                                               // các scope client được xin
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "elaris.api"
                    }
                } 
            };
    }
}
