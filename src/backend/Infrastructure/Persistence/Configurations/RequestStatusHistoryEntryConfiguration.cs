using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RequestStatusHistoryEntryConfiguration : IEntityTypeConfiguration<RequestStatusHistoryEntry>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistoryEntry> builder)
    {
        builder.ToTable("RequestStatusHistory");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(entry => entry.RequestId)
            .HasColumnName("RequestId")
            .IsRequired();

        builder.Property(entry => entry.PreviousStatus)
            .HasColumnName("PreviousStatus")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(entry => entry.NewStatus)
            .HasColumnName("NewStatus")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(entry => entry.Date)
            .HasColumnName("Date")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(entry => entry.Comment)
            .HasColumnName("Comment")
            .HasMaxLength(RequestStatusHistoryEntry.CommentMaximumLength)
            .IsRequired();

        builder.Property(entry => entry.ChangedById)
            .HasColumnName("ChangedById")
            .IsRequired();

        builder.HasOne(entry => entry.ChangedBy)
            .WithMany()
            .HasForeignKey(entry => entry.ChangedById)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RequestStatusHistory_Users_ChangedById");

        builder.HasIndex(entry => entry.RequestId)
            .HasDatabaseName("IX_RequestStatusHistory_RequestId");

        builder.HasIndex(entry => entry.Date)
            .HasDatabaseName("IX_RequestStatusHistory_Date");
    }
}
