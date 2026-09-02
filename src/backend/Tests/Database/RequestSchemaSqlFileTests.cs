using System.Text.RegularExpressions;
using Xunit;

namespace Database;

public class RequestSchemaSqlFileTests
{
    private static readonly string SchemaSqlPath =
        Path.Combine(AppContext.BaseDirectory, "schema.sql");

    private static string ReadSchemaSql() => File.ReadAllText(SchemaSqlPath);

    [Fact]
    public void SchemaSqlFile_Exists()
    {
        Assert.True(File.Exists(SchemaSqlPath), $"Expected schema file at '{SchemaSqlPath}'.");
    }

    [Fact]
    public void SchemaSqlFile_CreatesAreasTable()
    {
        var schemaSql = ReadSchemaSql();
        var match = Regex.Match(
            schemaSql,
            @"CREATE TABLE dbo\.Areas\s*\(\s*Id\s+uniqueidentifier NOT NULL,\s*Name\s+nvarchar\(128\)\s+NOT NULL,\s*IsActive\s+bit\s+NOT NULL,",
            RegexOptions.Singleline);

        Assert.True(match.Success, "Expected dbo.Areas table definition in schema.sql.");
    }

    [Fact]
    public void SchemaSqlFile_CreatesRequestTypesTable()
    {
        var schemaSql = ReadSchemaSql();
        var match = Regex.Match(
            schemaSql,
            @"CREATE TABLE dbo\.RequestTypes\s*\(\s*Id\s+uniqueidentifier NOT NULL,\s*Name\s+nvarchar\(128\)\s+NOT NULL,\s*Description\s+nvarchar\(512\)\s+NOT NULL,\s*IsActive\s+bit\s+NOT NULL,",
            RegexOptions.Singleline);

        Assert.True(match.Success, "Expected dbo.RequestTypes table definition in schema.sql.");
    }

    [Fact]
    public void SchemaSqlFile_CreatesRequestsTable()
    {
        var schemaSql = ReadSchemaSql();
        var match = Regex.Match(
            schemaSql,
            @"CREATE TABLE dbo\.Requests",
            RegexOptions.Singleline);

        Assert.True(match.Success, "Expected dbo.Requests table definition in schema.sql.");
    }

    [Fact]
    public void SchemaSqlFile_RequestsTableContainsExpectedColumns()
    {
        var schemaSql = ReadSchemaSql();

        Assert.Matches(@"Code\s+nvarchar\(32\)\s+NOT NULL", schemaSql);
        Assert.Matches(@"Title\s+nvarchar\(256\)\s+NOT NULL", schemaSql);
        Assert.Matches(@"Description\s+nvarchar\(4096\)\s+NOT NULL", schemaSql);
        Assert.Matches(@"Priority\s+nvarchar\(16\)\s+NOT NULL", schemaSql);
        Assert.Matches(@"Status\s+nvarchar\(16\)\s+NOT NULL", schemaSql);
        Assert.Matches(@"CreatedAt\s+datetime2\s+NOT NULL", schemaSql);
        Assert.Matches(@"DueDate\s+datetime2\s+NULL", schemaSql);
        Assert.Matches(@"EvidenceUrl\s+nvarchar\(2048\)\s+NULL", schemaSql);
        Assert.Matches(@"ClosedAt\s+datetime2\s+NULL", schemaSql);
        Assert.Matches(@"RequesterId\s+uniqueidentifier NOT NULL", schemaSql);
        Assert.Matches(@"ResponsibleId\s+uniqueidentifier NULL", schemaSql);
        Assert.Matches(@"AreaId\s+uniqueidentifier NOT NULL", schemaSql);
        Assert.Matches(@"RequestTypeId\s+uniqueidentifier NOT NULL", schemaSql);
    }

    [Fact]
    public void SchemaSqlFile_RequestsTableHasExpectedForeignKeys()
    {
        var schemaSql = ReadSchemaSql();

        Assert.Contains("CONSTRAINT FK_Requests_Areas_AreaId", schemaSql, StringComparison.Ordinal);
        Assert.Contains("CONSTRAINT FK_Requests_RequestTypes_RequestTypeId", schemaSql, StringComparison.Ordinal);
        Assert.Contains("CONSTRAINT FK_Requests_Users_RequesterId", schemaSql, StringComparison.Ordinal);
        Assert.Contains("CONSTRAINT FK_Requests_Users_ResponsibleId", schemaSql, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaSqlFile_RequestsTableHasExpectedIndexes()
    {
        var schemaSql = ReadSchemaSql();

        Assert.Matches(@"CREATE UNIQUE INDEX UX_Requests_Code", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_Requests_Status", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_Requests_Priority", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_Requests_RequesterId", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_Requests_ResponsibleId", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_Requests_AreaId", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_Requests_RequestTypeId", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_Requests_CreatedAt", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_Requests_DueDate", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_Requests_ClosedAt", schemaSql);
    }

    [Fact]
    public void SchemaSqlFile_RegistersMigrationHistoryRow()
    {
        var schemaSql = ReadSchemaSql();
        Assert.Contains("'20260901010000_AddRequestManagement'", schemaSql, StringComparison.Ordinal);
        Assert.Contains("'20260901120000_AddRequestEvidenceAndClosedAt'", schemaSql, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaSqlFile_RequestStatusHistoryTableIsCreated()
    {
        var schemaSql = ReadSchemaSql();
        Assert.Contains("CREATE TABLE dbo.RequestStatusHistory", schemaSql, StringComparison.Ordinal);
        Assert.Contains("FK_RequestStatusHistory_Requests_RequestId", schemaSql, StringComparison.Ordinal);
        Assert.Contains("FK_RequestStatusHistory_Users_ChangedById", schemaSql, StringComparison.Ordinal);
        Assert.Matches(@"CREATE INDEX IX_RequestStatusHistory_RequestId", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_RequestStatusHistory_Date", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_RequestStatusHistory_ChangedById", schemaSql);
    }

    [Fact]
    public void SchemaSqlFile_RequestCommentsTableIsCreated()
    {
        var schemaSql = ReadSchemaSql();
        Assert.Contains("CREATE TABLE dbo.RequestComments", schemaSql, StringComparison.Ordinal);
        Assert.Contains("FK_RequestComments_Requests_RequestId", schemaSql, StringComparison.Ordinal);
        Assert.Contains("FK_RequestComments_Users_AuthorId", schemaSql, StringComparison.Ordinal);
        Assert.Matches(@"CREATE INDEX IX_RequestComments_RequestId", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_RequestComments_AuthorId", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_RequestComments_RequestId_Visibility", schemaSql);
    }

    [Fact]
    public void SchemaSqlFile_RequestNotificationsTableIsCreated()
    {
        var schemaSql = ReadSchemaSql();
        Assert.Contains("CREATE TABLE dbo.RequestNotifications", schemaSql, StringComparison.Ordinal);
        Assert.Contains("FK_RequestNotifications_Requests_RequestId", schemaSql, StringComparison.Ordinal);
        Assert.Contains("FK_RequestNotifications_Users_DestinationUserId", schemaSql, StringComparison.Ordinal);
        Assert.Matches(@"CREATE INDEX IX_RequestNotifications_RequestId", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_RequestNotifications_DestinationUserId", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_RequestNotifications_Status", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_RequestNotifications_Date", schemaSql);
    }

    [Fact]
    public void SchemaSqlFile_AuditLogTableIsCreated()
    {
        var schemaSql = ReadSchemaSql();
        Assert.Contains("CREATE TABLE dbo.AuditLog", schemaSql, StringComparison.Ordinal);
        Assert.Matches(@"Timestamp\s+datetime2\s+NOT NULL", schemaSql);
        Assert.Matches(@"Action\s+nvarchar\(48\)\s+NOT NULL", schemaSql);
        Assert.Matches(@"Outcome\s+nvarchar\(16\)\s+NOT NULL", schemaSql);
        Assert.Matches(@"EntityType\s+nvarchar\(64\)\s+NOT NULL", schemaSql);
        Assert.Matches(@"ActorUserName\s+nvarchar\(64\)\s+NULL", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_AuditLog_Timestamp", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_AuditLog_Action", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_AuditLog_Outcome", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_AuditLog_EntityType", schemaSql);
        Assert.Matches(@"CREATE INDEX IX_AuditLog_ActorUserId", schemaSql);
    }

    [Fact]
    public void SchemaSqlFile_RegistersAuditLogMigration()
    {
        var schemaSql = ReadSchemaSql();
        Assert.Contains("'20260902000000_AddAuditLog'", schemaSql, StringComparison.Ordinal);
    }
}
