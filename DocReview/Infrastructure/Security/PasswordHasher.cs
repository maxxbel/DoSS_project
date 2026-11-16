using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace DocReview.Infrastructure.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 1000;
    
    public static (string Hash, string Salt) Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, Iterations, HashSize);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verify(string password, string hash, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] expectedHash = Convert.FromBase64String(hash);
        
        byte[] actualHash = KeyDerivation.Pbkdf2(password, saltBytes, KeyDerivationPrf.HMACSHA256, Iterations, HashSize); 
        
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash); 
    }
}