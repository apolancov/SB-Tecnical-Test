using Application.Common.Authentication;
using Infrastructure.Authentication;
using Xunit;

namespace Infrastructure.Authentication;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsStableFormatWithThreeSegments()
    {
        var hash = _hasher.Hash("AdminPass123!");

        var segments = hash.Split('.');

        Assert.Equal(3, segments.Length);
        Assert.True(int.TryParse(segments[0], out var iterations));
        Assert.True(iterations >= 100_000);
        Assert.NotEmpty(Convert.FromBase64String(segments[1]));
        Assert.NotEmpty(Convert.FromBase64String(segments[2]));
    }

    [Fact]
    public void Hash_ProducesDifferentHashForSameInputDueToSalt()
    {
        var first = _hasher.Hash("AdminPass123!");
        var second = _hasher.Hash("AdminPass123!");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_AcceptsHashOfSamePassword()
    {
        var storedHash = _hasher.Hash("AdminPass123!");

        Assert.True(_hasher.Verify("AdminPass123!", storedHash));
    }

    [Fact]
    public void Verify_RejectsDifferentPassword()
    {
        var storedHash = _hasher.Hash("AdminPass123!");

        Assert.False(_hasher.Verify("WrongPassword!", storedHash));
    }

    [Fact]
    public void Verify_AcceptsStoredHashFromSeedFile()
    {
        const string seededHash = "100000.6QOnL5OQWog6t+sqOKj1Jw==.TlpnX70o8/vt0ILOp6yTBTO1rqvGaBiLD3enFzkTnKw=";

        Assert.True(_hasher.Verify("AdminPass123!", seededHash));
        Assert.False(_hasher.Verify("UserPass123!", seededHash));
    }

    [Fact]
    public void Verify_AcceptsUserHashFromSeedFile()
    {
        const string seededHash = "100000./mjd64KeIP1pnKtE2OOtjw==.FdLolnrYj4O0dxnOTGdRodglspPMct37LJBKsfuwwHo=";

        Assert.True(_hasher.Verify("UserPass123!", seededHash));
        Assert.False(_hasher.Verify("AdminPass123!", seededHash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Verify_WithNullOrEmptyPassword_ReturnsFalse(string? password)
    {
        Assert.False(_hasher.Verify(password!, "100000.6QOnL5OQWog6t+sqOKj1Jw==.TlpnX70o8/vt0ILOp6yTBTO1rqvGaBiLD3enFzkTnKw="));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-real-hash")]
    [InlineData("100000")]
    [InlineData("100000.abc")]
    [InlineData("100000.abc.def.ghi")]
    [InlineData("zeroiterations.6QOnL5OQWog6t+sqOKj1Jw==.TlpnX70o8/vt0ILOp6yTBTO1rqvGaBiLD3enFzkTnKw=")]
    [InlineData("-1.6QOnL5OQWog6t+sqOKj1Jw==.TlpnX70o8/vt0ILOp6yTBTO1rqvGaBiLD3enFzkTnKw=")]
    public void Verify_WithInvalidStoredHash_ReturnsFalse(string? invalidHash)
    {
        Assert.False(_hasher.Verify("AdminPass123!", invalidHash!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Hash_WithNullOrEmptyPassword_Throws(string? password)
    {
        Assert.Throws<ArgumentException>(() => _hasher.Hash(password!));
    }
}
