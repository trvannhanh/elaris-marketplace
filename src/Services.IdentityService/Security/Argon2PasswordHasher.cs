using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Identity;
using System.Text;

namespace Services.IdentityService.Security
{
    public class Argon2PasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
    {
        public string HashPassword(TUser user, string password)
        {
            // Cấu hình Argon2 (có thể điều chỉnh theo môi trường)
            var config = new Argon2Config
            {
                Type = Argon2Type.DataDependentAddressing, // Argon2d (cho hiệu năng tốt hơn)
                Version = Argon2Version.Nineteen,
                TimeCost = 4,         // số vòng lặp tính toán
                MemoryCost = 1 << 12, // 4 MB RAM
                Lanes = 4,            // số luồng song song
                Threads = Environment.ProcessorCount,
                Salt = Argon2Generator.GenerateSalt(16),
                Password = Encoding.UTF8.GetBytes(password)
            };

            using var argon2 = new Argon2(config);
            return Argon2.Hash(config);
        }

        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
                return PasswordVerificationResult.Failed;

            return Argon2.Verify(hashedPassword, providedPassword)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }

    internal static class Argon2Generator
    {
        private static readonly Random _rng = new();
        public static byte[] GenerateSalt(int size)
        {
            var salt = new byte[size];
            _rng.NextBytes(salt);
            return salt;
        }
    }
}
