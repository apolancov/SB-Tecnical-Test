using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class SectorConfiguration : IEntityTypeConfiguration<Sector>
{
    private const int NameMaximumLength = 128;

    public void Configure(EntityTypeBuilder<Sector> builder)
    {
        builder.ToTable("Sectors");

        builder.HasKey(sector => sector.Id);

        builder.Property(sector => sector.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(sector => sector.Name)
            .HasColumnName("Name")
            .HasMaxLength(NameMaximumLength)
            .IsRequired();

        builder.HasIndex(sector => sector.Name)
            .IsUnique()
            .HasDatabaseName("UX_Sectors_Name");
    }
}