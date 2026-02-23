using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace IdealAkeWms.Services;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 32);

        byte[] result = new byte[48];
        Buffer.BlockCopy(salt, 0, result, 0, 16);
        Buffer.BlockCopy(hash, 0, result, 16, 32);
        return Convert.ToBase64String(result);
    }

    public bool VerifyPassword(string hashedPassword, string password)
    {
        byte[] decoded = Convert.FromBase64String(hashedPassword);
        if (decoded.Length != 48)
            return false;

        byte[] salt = new byte[16];
        Buffer.BlockCopy(decoded, 0, salt, 0, 16);

        byte[] expectedHash = new byte[32];
        Buffer.BlockCopy(decoded, 16, expectedHash, 0, 32);

        byte[] actualHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 32);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }
}
