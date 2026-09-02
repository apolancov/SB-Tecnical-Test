using System.Reflection;
using Application.Institutions;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Institutions;

public sealed class InstitutionWriteRepository : IInstitutionWriteRepository
{
    private const string SqlServerExceptionTypeName = "Microsoft.Data.SqlClient.SqlException";
    private const int SqlServerUniqueConstraintViolation = 2627;
    private const int SqlServerDuplicateKeyViolation = 2601;

    private const string SqliteExceptionTypeName = "Microsoft.Data.Sqlite.SqliteException";
    private const int SqliteConstraint = 19;
    private const int SqliteUniqueConstraintExtended = 2067;

    private readonly ApplicationDbContext _context;

    public InstitutionWriteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Institution institution, CancellationToken cancellationToken)
    {
        await _context.Institutions.AddAsync(institution, cancellationToken);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            ResetChangeTracker();
            throw new DuplicateInstitutionNameException(institution.Name);
        }
    }

    public void Update(Institution institution)
    {
        _context.Institutions.Update(institution);
        try
        {
            _context.SaveChanges();
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            ResetChangeTracker();
            throw new DuplicateInstitutionNameException(institution.Name);
        }
    }

    public void Remove(Institution institution)
    {
        _context.Institutions.Remove(institution);
        _context.SaveChanges();
    }

    private void ResetChangeTracker()
    {
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        var inner = exception.InnerException;
        while (inner is not null)
        {
            var typeName = inner.GetType().FullName;

            if (string.Equals(typeName, SqlServerExceptionTypeName, StringComparison.Ordinal))
            {
                var number = ReadInt32Property(inner, "Number");
                if (number is SqlServerUniqueConstraintViolation or SqlServerDuplicateKeyViolation)
                {
                    return true;
                }
            }

            if (string.Equals(typeName, SqliteExceptionTypeName, StringComparison.Ordinal))
            {
                var code = ReadInt32Property(inner, "SqliteErrorCode");
                var extendedCode = ReadInt32Property(inner, "SqliteExtendedErrorCode");
                if (code == SqliteConstraint && extendedCode == SqliteUniqueConstraintExtended)
                {
                    return true;
                }
            }

            inner = inner.InnerException;
        }

        return false;
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
