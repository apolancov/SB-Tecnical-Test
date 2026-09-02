using System.Data;
using Application.Requests;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Requests;

public sealed class SqlServerRequestCodeGenerator : IRequestCodeGenerator
{
    internal const string CodePrefix = "SOL-";
    internal const int CodeDigits = 4;
    internal const long InitialSequenceValue = 1L;

    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(15);

    private readonly Persistence.ApplicationDbContext _context;
    private readonly ILogger<SqlServerRequestCodeGenerator> _logger;

    public SqlServerRequestCodeGenerator(
        Persistence.ApplicationDbContext context,
        ILogger<SqlServerRequestCodeGenerator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var connection = _context.Database.GetDbConnection();
        var ownsConnection = connection.State != ConnectionState.Open;
        try
        {
            if (ownsConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var sqlConnection = (SqlConnection)connection;
            var sequenceName = BuildSequenceName(year);
            await EnsureSequenceAsync(sqlConnection, sequenceName, cancellationToken);

            long nextValue;
            await using (var command = sqlConnection.CreateCommand())
            {
                command.CommandText = $"SELECT NEXT VALUE FOR dbo.{sequenceName};";
                command.CommandTimeout = (int)CommandTimeout.TotalSeconds;
                var raw = await command.ExecuteScalarAsync(cancellationToken);
                if (raw is null || !long.TryParse(raw.ToString(), out nextValue))
                {
                    throw new InvalidOperationException(
                        $"NEXT VALUE FOR dbo.{sequenceName} returned no scalar result.");
                }
            }

            return FormatCode(year, nextValue);
        }
        finally
        {
            if (ownsConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    internal static string BuildSequenceName(int year)
    {
        return $"RequestCodeSequence_{year}";
    }

    internal static string FormatCode(int year, long sequenceValue)
    {
        return CodePrefix
            + year.ToString("D4", System.Globalization.CultureInfo.InvariantCulture)
            + "-"
            + sequenceValue.ToString(
                "D" + CodeDigits.ToString(System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task EnsureSequenceAsync(
        SqlConnection connection,
        string sequenceName,
        CancellationToken cancellationToken)
    {
        await using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = $"SELECT OBJECT_ID(N'dbo.{sequenceName}', N'SO');";
        checkCommand.CommandTimeout = (int)CommandTimeout.TotalSeconds;
        var existing = await checkCommand.ExecuteScalarAsync(cancellationToken);

        if (existing is not null && existing is not DBNull && !string.IsNullOrEmpty(existing.ToString()))
        {
            return;
        }

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText =
            $"CREATE SEQUENCE dbo.{sequenceName} "
            + $"AS bigint START WITH {InitialSequenceValue} INCREMENT BY 1 "
            + $"MINVALUE {InitialSequenceValue} NO CYCLE;";
        createCommand.CommandTimeout = (int)CommandTimeout.TotalSeconds;
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}