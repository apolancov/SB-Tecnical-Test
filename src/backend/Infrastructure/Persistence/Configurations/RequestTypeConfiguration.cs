using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RequestTypeConfiguration : IEntityTypeConfiguration<RequestType>
{
    public void Configure(EntityTypeBuilder<RequestType> builder)
    {
        builder.ToTable("RequestTypes");

        builder.HasKey(requestType => requestType.Id);

        builder.Property(requestType => requestType.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(requestType => requestType.Name)
            .HasColumnName("Name")
            .HasMaxLength(RequestType.NameMaximumLength)
            .IsRequired();

        builder.Property(requestType => requestType.Description)
            .HasColumnName("Description")
            .HasMaxLength(RequestType.DescriptionMaximumLength)
            .IsRequired();

        builder.Property(requestType => requestType.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        builder.HasIndex(requestType => requestType.Name)
            .IsUnique()
            .HasDatabaseName("UX_RequestTypes_Name");

        builder.HasIndex(requestType => requestType.IsActive)
            .HasDatabaseName("IX_RequestTypes_IsActive");
    }
}
