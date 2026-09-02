using Application.Common.Authentication;
using System.Security.Cryptography;

namespace Infrastructure.Authentication;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int DefaultIterationCount = 100_000;
    private const int SaltByteLength = 16;
    private const int HashByteLength = 32;
    private const int StoredHashSegmentCount = 3;
    private const int IterationIndex = 0;
    private const int SaltIndex = 1;
    private const int HashIndex = 2;

    public string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltByteLength);
        var derived = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            DefaultIterationCount,
            HashAlgorithmName.SHA256,
            HashByteLength);

        return FormatStoredHash(DefaultIterationCount, salt, derived);
    }

    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        if (!TryParseStoredHash(storedHash, out var iterations, out var salt, out var expectedHash))
        {
            return false;
        }

        byte[] actualHash;
        try
        {
            actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static string FormatStoredHash(int iterations, byte[] salt, byte[] hash)
    {
        return string.Join(
            '.',
            iterations.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    private static bool TryParseStoredHash(string storedHash, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = Array.Empty<byte>();
        hash = Array.Empty<byte>();

        var segments = storedHash.Trim().Split('.');
        if (segments.Length != StoredHashSegmentCount)
        {
            return false;
        }

        if (!int.TryParse(
                segments[IterationIndex],
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out iterations)
            || iterations <= 0)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(segments[SaltIndex]);
            hash = Convert.FromBase64String(segments[HashIndex]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length >= SaltByteLength && hash.Length >= HashByteLength;
    }
}
