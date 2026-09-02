using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(user => user.Username)
            .HasColumnName("Username")
            .HasMaxLength(User.UsernameMaximumLength)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasColumnName("Email")
            .HasMaxLength(User.EmailMaximumLength)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasColumnName("PasswordHash")
            .HasMaxLength(User.PasswordHashMaximumLength)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasColumnName("Role")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasColumnName("IsActive")
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.HasIndex(user => user.Username)
            .IsUnique()
            .HasDatabaseName("UX_Users_Username");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("UX_Users_Email");
    }
}
