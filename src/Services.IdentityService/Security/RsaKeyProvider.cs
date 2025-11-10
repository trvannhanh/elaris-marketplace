using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Services.IdentityService.Security
{
    public static class RsaKeyProvider
    {
        private static readonly RSA _rsa = RSA.Create(2048);

        public static SigningCredentials GetSigningCredentials()
        {
            var key = new RsaSecurityKey(_rsa)
            {
                KeyId = Guid.NewGuid().ToString()
            };

            return new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        }

        public static RsaSecurityKey GetRsaKey() => new RsaSecurityKey(_rsa);
    }
}
