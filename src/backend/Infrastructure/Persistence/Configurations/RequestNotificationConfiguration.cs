using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RequestNotificationConfiguration : IEntityTypeConfiguration<RequestNotification>
{
    public void Configure(EntityTypeBuilder<RequestNotification> builder)
    {
        builder.ToTable("RequestNotifications");

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(notification => notification.RequestId)
            .HasColumnName("RequestId")
            .IsRequired();

        builder.Property(notification => notification.DestinationUserId)
            .HasColumnName("DestinationUserId")
            .IsRequired();

        builder.Property(notification => notification.Channel)
            .HasColumnName("Channel")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(notification => notification.Status)
            .HasColumnName("Status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(notification => notification.Subject)
            .HasColumnName("Subject")
            .HasMaxLength(RequestNotification.SubjectMaximumLength)
            .IsRequired();

        builder.Property(notification => notification.Message)
            .HasColumnName("Message")
            .HasMaxLength(RequestNotification.MessageMaximumLength)
            .HasColumnType($"nvarchar({RequestNotification.MessageMaximumLength})")
            .IsRequired();

        builder.Property(notification => notification.Date)
            .HasColumnName("Date")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne(notification => notification.DestinationUser)
            .WithMany()
            .HasForeignKey(notification => notification.DestinationUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RequestNotifications_Users_DestinationUserId");

        builder.HasIndex(notification => notification.RequestId)
            .HasDatabaseName("IX_RequestNotifications_RequestId");

        builder.HasIndex(notification => notification.DestinationUserId)
            .HasDatabaseName("IX_RequestNotifications_DestinationUserId");

        builder.HasIndex(notification => notification.Status)
            .HasDatabaseName("IX_RequestNotifications_Status");

        builder.HasIndex(notification => notification.Date)
            .HasDatabaseName("IX_RequestNotifications_Date");
    }
}
