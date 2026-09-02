using Domain.Enums;

namespace Domain.Entities;

public class User
{
    public const int UsernameMaximumLength = 64;
    public const int PasswordHashMaximumLength = 1024;
    public const int EmailMaximumLength = 256;

    public Guid Id { get; private set; }

    public string Username { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private User()
    {
        Username = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(string username, string email, string passwordHash, UserRole role)
        : this(username, email, passwordHash, role, isActive: true)
    {
    }

    public User(string username, string email, string passwordHash, UserRole role, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("PasswordHash cannot be null or empty.", nameof(passwordHash));
        }

        var normalizedUsername = username.Trim();
        var normalizedEmail = email.Trim();
        var normalizedHash = passwordHash.Trim();

        if (normalizedUsername.Length > UsernameMaximumLength)
        {
            throw new ArgumentException(
                $"Username cannot exceed {UsernameMaximumLength} characters.",
                nameof(username));
        }

        if (normalizedEmail.Length > EmailMaximumLength)
        {
            throw new ArgumentException(
                $"Email cannot exceed {EmailMaximumLength} characters.",
                nameof(email));
        }

        if (!IsSupportedEmail(normalizedEmail))
        {
            throw new ArgumentException(
                $"Email '{normalizedEmail}' is not in a valid format.",
                nameof(email));
        }

        if (normalizedHash.Length > PasswordHashMaximumLength)
        {
            throw new ArgumentException(
                $"PasswordHash cannot exceed {PasswordHashMaximumLength} characters.",
                nameof(passwordHash));
        }

        Id = Guid.NewGuid();
        Username = normalizedUsername;
        Email = normalizedEmail;
        PasswordHash = normalizedHash;
        Role = role;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }

    public bool VerifyPasswordHash(string candidateHash)
    {
        if (string.IsNullOrEmpty(candidateHash))
        {
            return false;
        }

        var normalizedCandidate = candidateHash.Trim();
        if (normalizedCandidate.Length == 0)
        {
            return false;
        }

        return string.Equals(
            PasswordHash,
            normalizedCandidate,
            StringComparison.Ordinal);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Update(string username, string email, UserRole role, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));
        }

        var normalizedUsername = username.Trim();
        var normalizedEmail = email.Trim();

        if (normalizedUsername.Length > UsernameMaximumLength)
        {
            throw new ArgumentException(
                $"Username cannot exceed {UsernameMaximumLength} characters.",
                nameof(username));
        }

        if (normalizedEmail.Length > EmailMaximumLength)
        {
            throw new ArgumentException(
                $"Email cannot exceed {EmailMaximumLength} characters.",
                nameof(email));
        }

        if (!IsSupportedEmail(normalizedEmail))
        {
            throw new ArgumentException(
                $"Email '{normalizedEmail}' is not in a valid format.",
                nameof(email));
        }

        Username = normalizedUsername;
        Email = normalizedEmail;
        Role = role;
        IsActive = isActive;
    }

    public void ChangePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException("PasswordHash cannot be null or empty.", nameof(newPasswordHash));
        }

        var normalizedHash = newPasswordHash.Trim();

        if (normalizedHash.Length > PasswordHashMaximumLength)
        {
            throw new ArgumentException(
                $"PasswordHash cannot exceed {PasswordHashMaximumLength} characters.",
                nameof(newPasswordHash));
        }

        PasswordHash = normalizedHash;
    }

    public static bool IsSupportedEmail(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        try
        {
            var address = new System.Net.Mail.MailAddress(candidate);
            return string.Equals(
                address.Address,
                candidate,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
