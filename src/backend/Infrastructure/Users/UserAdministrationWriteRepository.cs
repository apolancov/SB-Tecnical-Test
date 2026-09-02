using System.Reflection;
using Application.Users;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Users;

public sealed class UserAdministrationWriteRepository : IUserAdministrationWriteRepository
{
    private const string SqlServerExceptionTypeName = "Microsoft.Data.SqlClient.SqlException";
    private const int SqlServerUniqueConstraintViolation = 2627;
    private const int SqlServerDuplicateKeyViolation = 2601;

    private const string SqliteExceptionTypeName = "Microsoft.Data.Sqlite.SqliteException";
    private const int SqliteConstraint = 19;
    private const int SqliteUniqueConstraintExtended = 2067;

    private static readonly string[] UniqueIndexNames =
    {
        "UX_Users_Username",
        "UX_Users_Email",
    };

    private readonly ApplicationDbContext _context;

    public UserAdministrationWriteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception, out var field))
        {
            ResetChangeTracker();
            throw new DuplicateUserException(field, ResolveValue(user, field));
        }
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
        try
        {
            _context.SaveChanges();
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception, out var field))
        {
            ResetChangeTracker();
            throw new DuplicateUserException(field, ResolveValue(user, field));
        }
    }

    public void Remove(User user)
    {
        _context.Users.Remove(user);
        _context.SaveChanges();
    }

    private void ResetChangeTracker()
    {
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception, out string field)
    {
        field = string.Empty;

        var inner = exception.InnerException;
        while (inner is not null)
        {
            var typeName = inner.GetType().FullName;

            if (string.Equals(typeName, SqlServerExceptionTypeName, StringComparison.Ordinal))
            {
                var number = ReadInt32Property(inner, "Number");
                if (number is SqlServerUniqueConstraintViolation or SqlServerDuplicateKeyViolation)
                {
                    var message = inner.Message ?? string.Empty;
                    field = InferFieldFromMessage(message);
                    return true;
                }
            }

            if (string.Equals(typeName, SqliteExceptionTypeName, StringComparison.Ordinal))
            {
                var code = ReadInt32Property(inner, "SqliteErrorCode");
                var extendedCode = ReadInt32Property(inner, "SqliteExtendedErrorCode");
                if (code == SqliteConstraint && extendedCode == SqliteUniqueConstraintExtended)
                {
                    var message = inner.Message ?? string.Empty;
                    field = InferFieldFromMessage(message);
                    return true;
                }
            }

            inner = inner.InnerException;
        }

        return false;
    }

    private static string InferFieldFromMessage(string message)
    {
        foreach (var indexName in UniqueIndexNames)
        {
            if (message.Contains(indexName, StringComparison.OrdinalIgnoreCase))
            {
                return indexName switch
                {
                    "UX_Users_Username" => "username",
                    "UX_Users_Email" => "email",
                    _ => string.Empty,
                };
            }
        }

        return string.Empty;
    }

    private static string ResolveValue(User user, string field)
    {
        return field switch
        {
            "username" => user.Username,
            "email" => user.Email,
            _ => string.Empty,
        };
    }

    private static int? ReadInt32Property(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance);

        if (property is null || property.PropertyType != typeof(int))
        {
            return null;
        }

        return (int?)property.GetValue(instance);
    }
}
