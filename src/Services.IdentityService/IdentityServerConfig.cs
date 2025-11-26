using Duende.IdentityServer.Models;
using Duende.IdentityServer;

namespace Services.IdentityService
{
    /// <summary>
    /// Cấu hình Duende IdentityServer: Identity Resources, API Scopes, API Resources và Clients
    /// Class này định nghĩa các tài nguyên và client được phép truy cập vào hệ thống
    /// </summary>
    public static class IdentityServerConfig
    {
        /// <summary>
        /// Identity Resources - Định nghĩa các thông tin user sẽ được trả về trong ID Token
        /// ID Token chứa thông tin về danh tính của user
        /// </summary>
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                // OpenId: bắt buộc phải có, chứa claim "sub" (subject/user ID)
                new IdentityResources.OpenId(),
                
                // Profile: thông tin profile như tên đầy đủ, giới tính, ngày sinh, ảnh đại diện...
                new IdentityResources.Profile(),
                
                // Email: địa chỉ email và trạng thái xác thực email
                new IdentityResources.Email(),
            };

        /// <summary>
        /// API Scopes - Định nghĩa các phạm vi (scope) mà client có thể yêu cầu
        /// Scope xác định client được phép truy cập những phần nào của API
        /// </summary>
        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                // Scope cho toàn bộ Elaris API
                // Client phải có scope này mới được gọi API
                new ApiScope("elaris.api", "Elaris API")
            };

        /// <summary>
        /// API Resources - Định nghĩa các API resources và claims được bao gồm trong access token
        /// Giúp phân nhóm các scope và xác định claims nào sẽ có trong token
        /// </summary>
        public static IEnumerable<ApiResource> ApiResources =>
            new ApiResource[]
            {
                new ApiResource("elaris.api", "Elaris API")
                {
                    // Các scope mà API resource này bao gồm
                    Scopes = { "elaris.api" },
                    
                    // Các claims sẽ được tự động thêm vào access token
                    // Giúp API có thể biết user là ai và có role gì mà không cần query lại
                    UserClaims = { "role", "name", "email" }
                }
            };

        /// <summary>
        /// Clients - Định nghĩa các ứng dụng (client) được phép kết nối với IdentityServer
        /// Mỗi client đại diện cho một ứng dụng có thể yêu cầu token
        /// </summary>
        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                // ===== Client 1: Elaris Web App (Resource Owner Password Flow) =====
                // Dùng cho mobile app hoặc trusted first-party app
                new Client
                {
                    ClientId = "elaris_web",                                        // ID duy nhất của client
                    ClientName = "Elaris Web App",                                  // Tên hiển thị cho user
                    
                    // Resource Owner Password: Client gửi username + password trực tiếp
                    // CHỈ dùng cho first-party app (ứng dụng của chính công ty)
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

                    AllowOfflineAccess = true,                                      // Cho phép cấp refresh token để lấy token mới khi access token hết hạn
                    
                    AccessTokenLifetime = 7200,                                     // Access token có hiệu lực 2 giờ (7200 giây)
                    
                    // Sliding: Refresh token sẽ được gia hạn mỗi khi sử dụng
                    // User không phải đăng nhập lại nếu dùng app thường xuyên
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    
                    // OneTimeOnly: Mỗi refresh token chỉ được dùng 1 lần
                    // Sau khi dùng sẽ nhận được refresh token mới (bảo mật hơn)
                    RefreshTokenUsage = TokenUsage.OneTimeOnly,
                    
                    // Client secret được hash bằng SHA256 để bảo mật
                    // Client phải gửi secret này khi yêu cầu token
                    ClientSecrets = { new Secret("super_secret_client".Sha256()) },
                    
                    // Các scope mà client này được phép yêu cầu
                    AllowedScopes = {
                        IdentityServerConstants.StandardScopes.OpenId,      // Bắt buộc - claim "sub"
                        IdentityServerConstants.StandardScopes.Profile,     // Thông tin profile
                        "elaris.api"                                        // Quyền truy cập Elaris API
                    }
                },

                // ===== Client 2: Elaris BFF (Backend-For-Frontend) - Authorization Code Flow =====
                // Dùng cho web app với BFF pattern, bảo mật hơn Resource Owner Password
                new Client
                {
                    ClientId = "elaris_bff",                                // ID của BFF client
                    ClientName = "Elaris Web BFF Client",                   // Tên hiển thị
                    
                    // Client secret - BFF cần secret để xác thực với IdentityServer
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    
                    // Authorization Code: Flow chuẩn OAuth2 cho web app
                    // User đăng nhập trên trang IdentityServer, sau đó redirect về với code
                    AllowedGrantTypes = GrantTypes.Code,
                    
                    // PKCE (Proof Key for Code Exchange): Bảo vệ chống tấn công authorization code interception
                    RequirePkce = true,
                    
                    // Không yêu cầu client secret (vì PKCE đã bảo vệ)
                    // Phù hợp với SPA hoặc public client không thể giữ secret an toàn
                    RequireClientSecret = false,
                    
                    // URL mà IdentityServer sẽ redirect về sau khi đăng nhập thành công
                    RedirectUris = { "http://localhost:5000/signin-oidc" },
                    
                    // URL redirect sau khi đăng xuất
                    PostLogoutRedirectUris = { "http://localhost:5000/signout-callback-oidc" },
                    
                    // Cho phép CORS từ domain này (để SPA có thể gọi IdentityServer)
                    AllowedCorsOrigins = { "http://localhost:5000" },
                    
                    // Các scope được phép
                    AllowedScopes = {
                        "openid",           // Bắt buộc
                        "profile",          // Thông tin profile
                        "email",            // Email
                        "elaris.api",       // API access
                        "offline_access"    // Cho phép refresh token
                    },

                    AllowOfflineAccess = true,                              // Cho phép refresh token
                    
                    AccessTokenLifetime = 3600,                             // Access token sống 1 giờ
                    
                    // Luôn bao gồm user claims trong ID token (không cần gọi UserInfo endpoint)
                    // Giúp giảm số lần gọi API
                    AlwaysIncludeUserClaimsInIdToken = true
                }
            };
    }
}