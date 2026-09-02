using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure;

public class ApplicationDbContextModelTests
{
    [Fact]
    public void DbContext_RecognizesInstitutionEntity()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Institution));

        Assert.NotNull(entityType);
    }

    [Fact]
    public void DbContext_RecognizesUserEntity()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(User));

        Assert.NotNull(entityType);
    }

    [Fact]
    public void DbContext_RecognizesCategoryEntity()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Category));

        Assert.NotNull(entityType);
        Assert.Equal("Categories", entityType!.GetTableName());
    }

    [Fact]
    public void DbContext_RecognizesStatePowerEntity()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(StatePower));

        Assert.NotNull(entityType);
        Assert.Equal("StatePowers", entityType!.GetTableName());
    }

    [Fact]
    public void DbContext_RecognizesSectorEntity()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Sector));

        Assert.NotNull(entityType);
        Assert.Equal("Sectors", entityType!.GetTableName());
    }

    [Fact]
    public void Institution_HasExpectedTableName()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Institution));

        Assert.NotNull(entityType);
        Assert.Equal("Institutions", entityType!.GetTableName());
    }

    [Fact]
    public void Institution_HasIdAsPrimaryKey()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Institution));
        var primaryKey = entityType!.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Single(primaryKey.Properties);
        Assert.Equal(nameof(Institution.Id), primaryKey.Properties[0].Name);
    }

    [Fact]
    public void Institution_NamePropertyIsRequiredWithExpectedMaxLength()
    {
        using var context = CreateSqliteContext();

        var nameProperty = context.Model.FindEntityType(typeof(Institution))!
            .FindProperty(nameof(Institution.Name))!;

        Assert.False(nameProperty.IsNullable);
        Assert.Equal(256, nameProperty.GetMaxLength());
    }

    [Fact]
    public void Institution_ForeignKeyPropertiesAreRequired()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Institution))!;

        foreach (var propertyName in new[]
        {
            nameof(Institution.CategoryId),
            nameof(Institution.StatePowerId),
            nameof(Institution.SectorId)
        })
        {
            var property = entityType.FindProperty(propertyName)!;
            Assert.False(property.IsNullable);
        }
    }

    [Fact]
    public void Institution_HasForeignKeyRelationships()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Institution))!;

        var categoryFk = entityType.GetForeignKeys()
            .Single(key => key.Properties.Single().Name == nameof(Institution.CategoryId));
        Assert.Equal("Categories", categoryFk.PrincipalEntityType.GetTableName());
        Assert.Equal(DeleteBehavior.Restrict, categoryFk.DeleteBehavior);

        var statePowerFk = entityType.GetForeignKeys()
            .Single(key => key.Properties.Single().Name == nameof(Institution.StatePowerId));
        Assert.Equal("StatePowers", statePowerFk.PrincipalEntityType.GetTableName());
        Assert.Equal(DeleteBehavior.Restrict, statePowerFk.DeleteBehavior);

        var sectorFk = entityType.GetForeignKeys()
            .Single(key => key.Properties.Single().Name == nameof(Institution.SectorId));
        Assert.Equal("Sectors", sectorFk.PrincipalEntityType.GetTableName());
        Assert.Equal(DeleteBehavior.Restrict, sectorFk.DeleteBehavior);
    }

    [Fact]
    public void Institution_HasUniqueIndexOnName()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Institution))!;
        var uniqueIndexes = entityType.GetIndexes()
            .Where(index => index.IsUnique)
            .ToList();

        Assert.Contains(uniqueIndexes, index =>
            index.Properties.Single().Name == nameof(Institution.Name));
    }

    [Fact]
    public void Institution_HasNonUniqueIndexesOnForeignKeyColumns()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Institution))!;
        var indexedColumns = entityType.GetIndexes()
            .Where(index => !index.IsUnique)
            .SelectMany(index => index.Properties)
            .Select(property => property.Name)
            .ToHashSet();

        Assert.Contains(nameof(Institution.CategoryId), indexedColumns);
        Assert.Contains(nameof(Institution.StatePowerId), indexedColumns);
        Assert.Contains(nameof(Institution.SectorId), indexedColumns);
    }

    [Fact]
    public void Institution_CreatedAtIsMappedToUtcDefault()
    {
        using var context = CreateSqliteContext();

        var createdAtProperty = context.Model.FindEntityType(typeof(Institution))!
            .FindProperty(nameof(Institution.CreatedAt))!;

        Assert.False(createdAtProperty.IsNullable);
        Assert.Equal("datetime2", createdAtProperty.GetColumnType());
        Assert.Equal("GETUTCDATE()", createdAtProperty.GetDefaultValueSql());
    }

    [Fact]
    public void LookupTables_HaveUniqueIndexOnName()
    {
        using var context = CreateSqliteContext();

        foreach (var type in new[] { typeof(Category), typeof(StatePower), typeof(Sector) })
        {
            var entityType = context.Model.FindEntityType(type)!;
            var nameProperty = entityType.FindProperty("Name")!;
            Assert.False(nameProperty.IsNullable);
            Assert.Equal(128, nameProperty.GetMaxLength());

            var uniqueIndexes = entityType.GetIndexes()
                .Where(index => index.IsUnique)
                .ToList();

            Assert.Contains(uniqueIndexes, index =>
                index.Properties.Single().Name == "Name");
        }
    }

    [Fact]
    public void User_HasExpectedTableName()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(User));

        Assert.NotNull(entityType);
        Assert.Equal("Users", entityType!.GetTableName());
    }

    [Fact]
    public void User_UsernameIsRequiredAndUnique()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(User))!;
        var usernameProperty = entityType.FindProperty(nameof(User.Username))!;

        Assert.False(usernameProperty.IsNullable);
        Assert.Equal(User.UsernameMaximumLength, usernameProperty.GetMaxLength());

        var uniqueIndexes = entityType.GetIndexes()
            .Where(index => index.IsUnique)
            .ToList();

        Assert.Contains(uniqueIndexes, index =>
            index.Properties.Single().Name == nameof(User.Username));
    }

    [Fact]
    public void User_EmailIsRequiredAndUnique()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(User))!;
        var emailProperty = entityType.FindProperty(nameof(User.Email))!;

        Assert.False(emailProperty.IsNullable);
        Assert.Equal(User.EmailMaximumLength, emailProperty.GetMaxLength());

        var uniqueIndexes = entityType.GetIndexes()
            .Where(index => index.IsUnique)
            .ToList();

        Assert.Contains(uniqueIndexes, index =>
            index.Properties.Single().Name == nameof(User.Email));
    }

    [Fact]
    public void User_PasswordHashIsRequired()
    {
        using var context = CreateSqliteContext();

        var passwordHashProperty = context.Model.FindEntityType(typeof(User))!
            .FindProperty(nameof(User.PasswordHash))!;

        Assert.False(passwordHashProperty.IsNullable);
        Assert.Equal(User.PasswordHashMaximumLength, passwordHashProperty.GetMaxLength());
    }

    [Fact]
    public void User_RoleIsMappedAsStringColumn()
    {
        using var context = CreateSqliteContext();

        var roleProperty = context.Model.FindEntityType(typeof(User))!
            .FindProperty(nameof(User.Role))!;

        Assert.False(roleProperty.IsNullable);
        Assert.Equal(32, roleProperty.GetMaxLength());
    }

    [Fact]
    public void DbContext_CanBuildModelWithoutErrors()
    {
        using var context = CreateSqliteContext();

        var model = context.Model;

        Assert.NotNull(model);
        Assert.NotEmpty(model.GetEntityTypes());
    }

    [Fact]
    public void DbContext_RecognizesAreaEntity()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Area));

        Assert.NotNull(entityType);
        Assert.Equal("Areas", entityType!.GetTableName());
    }

    [Fact]
    public void DbContext_RecognizesRequestTypeEntity()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(RequestType));

        Assert.NotNull(entityType);
        Assert.Equal("RequestTypes", entityType!.GetTableName());
    }

    [Fact]
    public void DbContext_RecognizesRequestEntity()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Request));

        Assert.NotNull(entityType);
        Assert.Equal("Requests", entityType!.GetTableName());
    }

    [Fact]
    public void DbContext_RecognizesRequestStatusHistoryEntity()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(RequestStatusHistoryEntry));

        Assert.NotNull(entityType);
        Assert.Equal("RequestStatusHistory", entityType!.GetTableName());
    }

    [Fact]
    public void DbContext_RecognizesRequestCommentEntity()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(RequestComment));

        Assert.NotNull(entityType);
        Assert.Equal("RequestComments", entityType!.GetTableName());
    }

    [Fact]
    public void DbContext_RecognizesRequestNotificationEntity()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(RequestNotification));

        Assert.NotNull(entityType);
        Assert.Equal("RequestNotifications", entityType!.GetTableName());
    }

    [Fact]
    public void Area_HasUniqueIndexOnName()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Area))!;
        var uniqueIndexes = entityType.GetIndexes()
            .Where(index => index.IsUnique)
            .ToList();

        Assert.Contains(uniqueIndexes, index => index.Properties.Single().Name == nameof(Area.Name));
    }

    [Fact]
    public void Request_HasUniqueIndexOnCode()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Request))!;
        var uniqueIndexes = entityType.GetIndexes()
            .Where(index => index.IsUnique)
            .ToList();

        Assert.Contains(uniqueIndexes, index => index.Properties.Single().Name == nameof(Request.Code));
    }

    [Fact]
    public void Request_HasExpectedNonUniqueIndexes()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Request))!;
        var indexedColumns = entityType.GetIndexes()
            .Where(index => !index.IsUnique)
            .SelectMany(index => index.Properties)
            .Select(property => property.Name)
            .ToHashSet();

        Assert.Contains(nameof(Request.Status), indexedColumns);
        Assert.Contains(nameof(Request.Priority), indexedColumns);
        Assert.Contains(nameof(Request.RequesterId), indexedColumns);
        Assert.Contains(nameof(Request.ResponsibleId), indexedColumns);
        Assert.Contains(nameof(Request.AreaId), indexedColumns);
        Assert.Contains(nameof(Request.RequestTypeId), indexedColumns);
        Assert.Contains(nameof(Request.CreatedAt), indexedColumns);
    }

    [Fact]
    public void Request_ForeignKeysAreConfiguredAsRestrict()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Request))!;

        foreach (var propertyName in new[]
        {
            nameof(Request.RequesterId),
            nameof(Request.ResponsibleId),
            nameof(Request.AreaId),
            nameof(Request.RequestTypeId)
        })
        {
            var fk = entityType.GetForeignKeys()
                .Single(key => key.Properties.Single().Name == propertyName);
            Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
        }
    }

    [Fact]
    public void RequestStatusHistory_ForeignKeysAreConfigured()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(RequestStatusHistoryEntry))!;

        var requestFk = entityType.GetForeignKeys()
            .Single(key => key.Properties.Single().Name == nameof(RequestStatusHistoryEntry.RequestId));
        Assert.Equal(DeleteBehavior.Cascade, requestFk.DeleteBehavior);

        var userFk = entityType.GetForeignKeys()
            .Single(key => key.Properties.Single().Name == nameof(RequestStatusHistoryEntry.ChangedById));
        Assert.Equal(DeleteBehavior.Restrict, userFk.DeleteBehavior);
    }

    [Fact]
    public void RequestComment_ForeignKeysAreConfigured()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(RequestComment))!;

        var requestFk = entityType.GetForeignKeys()
            .Single(key => key.Properties.Single().Name == nameof(RequestComment.RequestId));
        Assert.Equal(DeleteBehavior.Cascade, requestFk.DeleteBehavior);

        var userFk = entityType.GetForeignKeys()
            .Single(key => key.Properties.Single().Name == nameof(RequestComment.AuthorId));
        Assert.Equal(DeleteBehavior.Restrict, userFk.DeleteBehavior);
    }

    [Fact]
    public void RequestNotification_ForeignKeysAreConfigured()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(RequestNotification))!;

        var requestFk = entityType.GetForeignKeys()
            .Single(key => key.Properties.Single().Name == nameof(RequestNotification.RequestId));
        Assert.Equal(DeleteBehavior.Cascade, requestFk.DeleteBehavior);

        var userFk = entityType.GetForeignKeys()
            .Single(key => key.Properties.Single().Name == nameof(RequestNotification.DestinationUserId));
        Assert.Equal(DeleteBehavior.Restrict, userFk.DeleteBehavior);
    }

    [Fact]
    public void Request_HasExpectedStringMaxLengths()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Request))!;

        Assert.Equal(32, entityType.FindProperty(nameof(Request.Code))!.GetMaxLength());
        Assert.Equal(256, entityType.FindProperty(nameof(Request.Title))!.GetMaxLength());
        Assert.Equal(4096, entityType.FindProperty(nameof(Request.Description))!.GetMaxLength());
        Assert.Equal(16, entityType.FindProperty(nameof(Request.Priority))!.GetMaxLength());
        Assert.Equal(16, entityType.FindProperty(nameof(Request.Status))!.GetMaxLength());
    }

    [Fact]
    public void Request_EnumsAreStoredAsStrings()
    {
        using var context = CreateSqliteContext();

        var entityType = context.Model.FindEntityType(typeof(Request))!;

        var priorityProperty = entityType.FindProperty(nameof(Request.Priority))!;
        var statusProperty = entityType.FindProperty(nameof(Request.Status))!;

        Assert.Equal(typeof(RequestPriority), priorityProperty.ClrType);
        Assert.Equal(typeof(RequestStatus), statusProperty.ClrType);
        Assert.Equal(16, priorityProperty.GetMaxLength());
        Assert.Equal(16, statusProperty.GetMaxLength());
    }

    private static ApplicationDbContext CreateSqliteContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Filename=:memory:")
            .Options;

        return new ApplicationDbContext(options);
    }
}