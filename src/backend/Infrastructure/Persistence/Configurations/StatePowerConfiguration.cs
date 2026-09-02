using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class StatePowerConfiguration : IEntityTypeConfiguration<StatePower>
{
    private const int NameMaximumLength = 128;

    public void Configure(EntityTypeBuilder<StatePower> builder)
    {
        builder.ToTable("StatePowers");

        builder.HasKey(statePower => statePower.Id);

        builder.Property(statePower => statePower.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(statePower => statePower.Name)
            .HasColumnName("Name")
            .HasMaxLength(NameMaximumLength)
            .IsRequired();

        builder.HasIndex(statePower => statePower.Name)
            .IsUnique()
            .HasDatabaseName("UX_StatePowers_Name");
    }
}