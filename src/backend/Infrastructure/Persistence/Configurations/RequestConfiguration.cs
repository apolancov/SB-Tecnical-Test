using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RequestConfiguration : IEntityTypeConfiguration<Request>
{
    public void Configure(EntityTypeBuilder<Request> builder)
    {
        builder.ToTable("Requests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(request => request.Code)
            .HasColumnName("Code")
            .HasMaxLength(Request.CodeMaximumLength)
            .IsRequired();

        builder.Property(request => request.Title)
            .HasColumnName("Title")
            .HasMaxLength(Request.TitleMaximumLength)
            .IsRequired();

        builder.Property(request => request.Description)
            .HasColumnName("Description")
            .HasMaxLength(Request.DescriptionMaximumLength)
            .HasColumnType($"nvarchar({Request.DescriptionMaximumLength})")
            .IsRequired();

        builder.Property(request => request.Priority)
            .HasColumnName("Priority")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasColumnName("Status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(request => request.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.Property(request => request.DueDate)
            .HasColumnName("DueDate")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(request => request.EvidenceUrl)
            .HasColumnName("EvidenceUrl")
            .HasMaxLength(Request.EvidenceUrlMaximumLength)
            .HasColumnType($"nvarchar({Request.EvidenceUrlMaximumLength})")
            .IsRequired(false);

        builder.Property(request => request.ClosedAt)
            .HasColumnName("ClosedAt")
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(request => request.RequesterId)
            .HasColumnName("RequesterId")
            .IsRequired();

        builder.Property(request => request.ResponsibleId)
            .HasColumnName("ResponsibleId")
            .IsRequired(false);

        builder.Property(request => request.AreaId)
            .HasColumnName("AreaId")
            .IsRequired();

        builder.Property(request => request.RequestTypeId)
            .HasColumnName("RequestTypeId")
            .IsRequired();

        builder.HasOne(request => request.Requester)
            .WithMany()
            .HasForeignKey(request => request.RequesterId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Requests_Users_RequesterId");

        builder.HasOne(request => request.Responsible)
            .WithMany()
            .HasForeignKey(request => request.ResponsibleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Requests_Users_ResponsibleId");

        builder.HasOne(request => request.Area)
            .WithMany()
            .HasForeignKey(request => request.AreaId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Requests_Areas_AreaId");

        builder.HasOne(request => request.RequestType)
            .WithMany()
            .HasForeignKey(request => request.RequestTypeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Requests_RequestTypes_RequestTypeId");

        builder.HasMany(request => request.StatusHistory)
            .WithOne()
            .HasForeignKey(entry => entry.RequestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RequestStatusHistory_Requests_RequestId");

        builder.HasMany(request => request.Comments)
            .WithOne()
            .HasForeignKey(comment => comment.RequestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RequestComments_Requests_RequestId");

        builder.HasMany(request => request.Notifications)
            .WithOne()
            .HasForeignKey(notification => notification.RequestId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_RequestNotifications_Requests_RequestId");

        builder.HasIndex(request => request.Code)
            .IsUnique()
            .HasDatabaseName("UX_Requests_Code");

        builder.HasIndex(request => request.Status)
            .HasDatabaseName("IX_Requests_Status");

        builder.HasIndex(request => request.Priority)
            .HasDatabaseName("IX_Requests_Priority");

        builder.HasIndex(request => request.RequesterId)
            .HasDatabaseName("IX_Requests_RequesterId");

        builder.HasIndex(request => request.ResponsibleId)
            .HasDatabaseName("IX_Requests_ResponsibleId");

        builder.HasIndex(request => request.AreaId)
            .HasDatabaseName("IX_Requests_AreaId");

        builder.HasIndex(request => request.RequestTypeId)
            .HasDatabaseName("IX_Requests_RequestTypeId");

        builder.HasIndex(request => request.CreatedAt)
            .HasDatabaseName("IX_Requests_CreatedAt");

        builder.HasIndex(request => request.ClosedAt)
            .HasDatabaseName("IX_Requests_ClosedAt");

        builder.HasIndex(request => request.DueDate)
            .HasDatabaseName("IX_Requests_DueDate");
    }
}