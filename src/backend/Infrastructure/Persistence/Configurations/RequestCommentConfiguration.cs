using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class RequestCommentConfiguration : IEntityTypeConfiguration<RequestComment>
{
    public void Configure(EntityTypeBuilder<RequestComment> builder)
    {
        builder.ToTable("RequestComments");

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Id)
            .HasColumnName("Id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(comment => comment.RequestId)
            .HasColumnName("RequestId")
            .IsRequired();

        builder.Property(comment => comment.AuthorId)
            .HasColumnName("AuthorId")
            .IsRequired();

        builder.Property(comment => comment.Text)
            .HasColumnName("Text")
            .HasMaxLength(RequestComment.TextMaximumLength)
            .HasColumnType($"nvarchar({RequestComment.TextMaximumLength})")
            .IsRequired();

        builder.Property(comment => comment.Visibility)
            .HasColumnName("Visibility")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(comment => comment.Date)
            .HasColumnName("Date")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne(comment => comment.Author)
            .WithMany()
            .HasForeignKey(comment => comment.AuthorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RequestComments_Users_AuthorId");

        builder.HasIndex(comment => comment.RequestId)
            .HasDatabaseName("IX_RequestComments_RequestId");

        builder.HasIndex(comment => comment.AuthorId)
            .HasDatabaseName("IX_RequestComments_AuthorId");

        builder.HasIndex(comment => new { comment.RequestId, comment.Visibility })
            .HasDatabaseName("IX_RequestComments_RequestId_Visibility");
    }
}
