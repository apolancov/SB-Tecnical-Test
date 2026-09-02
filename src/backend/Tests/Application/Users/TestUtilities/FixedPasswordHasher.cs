using Application.Common.Authentication;

namespace Application.Users.TestUtilities;

public sealed class FixedPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return "HASH::" + password;
    }

    public bool Verify(string password, string storedHash)
    {
        return string.Equals(Hash(password), storedHash, StringComparison.Ordinal);
    }
}
