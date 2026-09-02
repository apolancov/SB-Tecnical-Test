using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("Areas");

        builder.HasKey(area => area.Id);

        builder.Property(area => area.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(area => area.Name)
            .HasColumnName("Name")
            .HasMaxLength(Area.NameMaximumLength)
            .IsRequired();

        builder.Property(area => area.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        builder.HasIndex(area => area.Name)
            .IsUnique()
            .HasDatabaseName("UX_Areas_Name");

        builder.HasIndex(area => area.IsActive)
            .HasDatabaseName("IX_Areas_IsActive");
    }
}
