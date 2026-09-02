using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Domain.Users;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidArguments_AssignsProperties()
    {
        var user = new User(
            username: "admin",
            email: "admin@example.local",
            passwordHash: "stored-hash-value",
            role: UserRole.Admin);

        Assert.Equal("admin", user.Username);
        Assert.Equal("admin@example.local", user.Email);
        Assert.Equal("stored-hash-value", user.PasswordHash);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.True(user.IsActive);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Constructor_TrimsUsernameEmailAndPasswordHash()
    {
        var user = new User(
            username: "  admin  ",
            email: "  admin@example.local  ",
            passwordHash: "  hash  ",
            role: UserRole.User);

        Assert.Equal("admin", user.Username);
        Assert.Equal("admin@example.local", user.Email);
        Assert.Equal("hash", user.PasswordHash);
    }

    [Fact]
    public void Constructor_AssignsCreatedAtToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var user = new User("admin", "admin@example.local", "hash", UserRole.Admin);

        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(user.CreatedAt, before, after);
    }

    [Fact]
    public void Constructor_WithIsActiveFalse_StoresAsInactive()
    {
        var user = new User("admin", "admin@example.local", "hash", UserRole.Admin, isActive: false);

        Assert.False(user.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyUsername_Throws(string? invalidUsername)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new User(invalidUsername!, "admin@example.local", "hash", UserRole.Admin));

        Assert.Equal("username", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyEmail_Throws(string? invalidEmail)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new User("admin", invalidEmail!, "hash", UserRole.Admin));

        Assert.Equal("email", exception.ParamName);
    }

    [Theory]
    [InlineData("plain-address")]
    [InlineData("@missing-local.com")]
    [InlineData("missing-at-sign.com")]
    [InlineData("trailing-at@")]
    [InlineData("spaces in@email.com")]
    public void Constructor_WithInvalidEmailFormat_Throws(string invalidEmail)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new User("admin", invalidEmail, "hash", UserRole.Admin));

        Assert.Equal("email", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyPasswordHash_Throws(string? invalidHash)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new User("admin", "admin@example.local", invalidHash!, UserRole.Admin));

        Assert.Equal("passwordHash", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithUsernameLongerThanMaximum_Throws()
    {
        var longUsername = new string('a', User.UsernameMaximumLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            new User(longUsername, "admin@example.local", "hash", UserRole.Admin));

        Assert.Equal("username", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmailLongerThanMaximum_Throws()
    {
        var longEmail = new string('a', User.EmailMaximumLength - 11) + "@example.com";

        var exception = Assert.Throws<ArgumentException>(() =>
            new User("admin", longEmail, "hash", UserRole.Admin));

        Assert.Equal("email", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithPasswordHashLongerThanMaximum_Throws()
    {
        var longHash = new string('a', User.PasswordHashMaximumLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            new User("admin", "admin@example.local", longHash, UserRole.Admin));

        Assert.Equal("passwordHash", exception.ParamName);
    }

    [Fact]
    public void VerifyPasswordHash_MatchingHash_ReturnsTrue()
    {
        var user = new User("admin", "admin@example.local", "hash", UserRole.Admin);

        Assert.True(user.VerifyPasswordHash("hash"));
    }

    [Fact]
    public void VerifyPasswordHash_TrimsCandidateBeforeComparison()
    {
        var user = new User("admin", "admin@example.local", "hash", UserRole.Admin);

        Assert.True(user.VerifyPasswordHash("  hash  "));
    }

    [Fact]
    public void VerifyPasswordHash_DifferentHash_ReturnsFalse()
    {
        var user = new User("admin", "admin@example.local", "hash", UserRole.Admin);

        Assert.False(user.VerifyPasswordHash("other"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VerifyPasswordHash_NullOrEmptyCandidate_ReturnsFalse(string? candidate)
    {
        var user = new User("admin", "admin@example.local", "hash", UserRole.Admin);

        Assert.False(user.VerifyPasswordHash(candidate!));
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        var user = new User("admin", "admin@example.local", "hash", UserRole.Admin);

        user.Deactivate();

        Assert.False(user.IsActive);
    }

    [Fact]
    public void EachInstance_HasUniqueIdentifier()
    {
        var first = new User("admin", "admin@example.local", "hash", UserRole.Admin);
        var second = new User("user", "user@example.local", "hash", UserRole.User);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("first.last@sub.domain.org")]
    [InlineData("user+tag@inbox.example")]
    public void IsSupportedEmail_WithValidAddress_ReturnsTrue(string validEmail)
    {
        Assert.True(User.IsSupportedEmail(validEmail));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("@missing-local.com")]
    [InlineData("spaces in@email.com")]
    public void IsSupportedEmail_WithInvalidAddress_ReturnsFalse(string? invalidEmail)
    {
        Assert.False(User.IsSupportedEmail(invalidEmail!));
    }
}
