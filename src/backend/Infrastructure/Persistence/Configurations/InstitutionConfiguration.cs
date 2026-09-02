using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    private const int NameMaximumLength = Institution.NameMaximumLength;

    public void Configure(EntityTypeBuilder<Institution> builder)
    {
        builder.ToTable("Institutions");

        builder.HasKey(institution => institution.Id);

        builder.Property(institution => institution.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(institution => institution.Name)
            .HasColumnName("Name")
            .HasMaxLength(NameMaximumLength)
            .IsRequired();

        builder.Property(institution => institution.CategoryId)
            .HasColumnName("CategoryId")
            .IsRequired();

        builder.Property(institution => institution.StatePowerId)
            .HasColumnName("StatePowerId")
            .IsRequired();

        builder.Property(institution => institution.SectorId)
            .HasColumnName("SectorId")
            .IsRequired();

        builder.Property(institution => institution.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.HasOne(institution => institution.Category)
            .WithMany()
            .HasForeignKey(institution => institution.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Institutions_Categories_CategoryId");

        builder.HasOne(institution => institution.StatePower)
            .WithMany()
            .HasForeignKey(institution => institution.StatePowerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Institutions_StatePowers_StatePowerId");

        builder.HasOne(institution => institution.Sector)
            .WithMany()
            .HasForeignKey(institution => institution.SectorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Institutions_Sectors_SectorId");

        builder.HasIndex(institution => institution.Name)
            .IsUnique()
            .HasDatabaseName("UX_Institutions_Name");

        builder.HasIndex(institution => institution.CategoryId)
            .HasDatabaseName("IX_Institutions_CategoryId");

        builder.HasIndex(institution => institution.StatePowerId)
            .HasDatabaseName("IX_Institutions_StatePowerId");

        builder.HasIndex(institution => institution.SectorId)
            .HasDatabaseName("IX_Institutions_SectorId");
    }
}