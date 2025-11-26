using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Services.IdentityService.Security
{
    /// <summary>
    /// Provider cung cấp RSA key để ký và xác thực JWT tokens
    /// Sử dụng thuật toán mã hóa bất đối xứng RSA với độ dài key 2048 bit
    /// </summary>
    public static class RsaKeyProvider
    {
        /// <summary>
        /// RSA key duy nhất được tạo khi ứng dụng khởi động
        /// Sử dụng static để đảm bảo key không đổi trong suốt vòng đời của ứng dụng
        /// 2048 là độ dài key tính bằng bit (đủ an toàn cho production)
        /// </summary>
        private static readonly RSA _rsa = RSA.Create(2048);

        /// <summary>
        /// Lấy SigningCredentials để ký JWT tokens
        /// SigningCredentials bao gồm private key và thuật toán ký
        /// </summary>
        /// <returns>SigningCredentials với thuật toán RS256 (RSA-SHA256)</returns>
        public static SigningCredentials GetSigningCredentials()
        {
            // Tạo RsaSecurityKey từ RSA instance
            var key = new RsaSecurityKey(_rsa)
            {
                // KeyId là identifier duy nhất cho key này
                // Giúp phân biệt giữa các key khác nhau khi rotate keys
                KeyId = Guid.NewGuid().ToString()
            };

            // Trả về SigningCredentials với:
            // - key: RsaSecurityKey chứa private key để ký
            // - algorithm: RS256 (RSA signature với SHA-256)
            return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        }

        /// <summary>
        /// Lấy RsaSecurityKey để xác thực JWT tokens
        /// Key này chứa cả private và public key
        /// Public key sẽ được dùng để validate chữ ký của token
        /// </summary>
        /// <returns>RsaSecurityKey từ RSA instance</returns>
        public static RsaSecurityKey GetRsaKey() => new RsaSecurityKey(_rsa);
    }
}