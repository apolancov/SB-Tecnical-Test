using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLog");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(entry => entry.Timestamp)
            .HasColumnName("Timestamp")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(entry => entry.Action)
            .HasColumnName("Action")
            .HasConversion<string>()
            .HasMaxLength(48)
            .IsRequired();

        builder.Property(entry => entry.Outcome)
            .HasColumnName("Outcome")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(entry => entry.EntityType)
            .HasColumnName("EntityType")
            .HasMaxLength(AuditLogEntry.EntityTypeMaximumLength)
            .IsRequired();

        builder.Property(entry => entry.EntityId)
            .HasColumnName("EntityId")
            .HasMaxLength(AuditLogEntry.EntityIdMaximumLength);

        builder.Property(entry => entry.Details)
            .HasColumnName("Details")
            .HasMaxLength(AuditLogEntry.DetailsMaximumLength);

        builder.Property(entry => entry.IpAddress)
            .HasColumnName("IpAddress")
            .HasMaxLength(AuditLogEntry.IpAddressMaximumLength);

        builder.Property(entry => entry.ActorUserId)
            .HasColumnName("ActorUserId");

        builder.Property(entry => entry.ActorUserName)
            .HasColumnName("ActorUserName")
            .HasMaxLength(AuditLogEntry.UserNameMaximumLength);

        builder.HasIndex(entry => entry.Timestamp)
            .HasDatabaseName("IX_AuditLog_Timestamp");

        builder.HasIndex(entry => entry.Action)
            .HasDatabaseName("IX_AuditLog_Action");

        builder.HasIndex(entry => entry.Outcome)
            .HasDatabaseName("IX_AuditLog_Outcome");

        builder.HasIndex(entry => entry.EntityType)
            .HasDatabaseName("IX_AuditLog_EntityType");

        builder.HasIndex(entry => entry.ActorUserId)
            .HasDatabaseName("IX_AuditLog_ActorUserId");

        builder.HasIndex(entry => entry.Timestamp)
            .HasDatabaseName("IX_AuditLog_Timestamp_Descending")
            .IsDescending();
    }
}