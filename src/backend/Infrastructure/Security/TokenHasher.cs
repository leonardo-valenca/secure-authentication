using System.Security.Cryptography;
using System.Text;
using Application.Abstractions.Security;

namespace Infrastructure.Security
{
    /// <summary>
    /// Refresh tokens are high-entropy random values, not user-chosen secrets, so a fast
    /// cryptographic hash is sufficient here, no need for the slow, salted hashing used for passwords.
    /// </summary>
    internal sealed class TokenHasher : ITokenHasher
    {
        public string Hash(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}
