using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(t => t.TransactionType)
            .HasColumnName("transaction_type")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .IsRequired()
            .HasPrecision(12, 2);

        builder.Property(t => t.SignedAmount)
            .HasColumnName("signed_amount")
            .IsRequired()
            .HasPrecision(12, 2);

        builder.Property(t => t.Date)
            .HasColumnName("date")
            .IsRequired()
            .HasColumnType("date");

        builder.Property(t => t.Subject)
            .HasColumnName("subject")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(t => t.PaymentMethod)
            .HasColumnName("payment_method")
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.CumulativeDelta)
            .HasColumnName("cumulative_delta")
            .IsRequired()
            .HasPrecision(12, 2);

        builder.Property(t => t.CategoryId)
            .HasColumnName("category_id");

        builder.Property(t => t.TransactionGroupId)
            .HasColumnName("transaction_group_id");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes matching the database schema
        builder.HasIndex(t => new { t.UserId, t.Date, t.Id })
            .HasDatabaseName("idx_transaction_user_date")
            .IsDescending(false, true, true);

        builder.HasIndex(t => new { t.UserId, t.TransactionType })
            .HasDatabaseName("idx_transaction_type");

        builder.HasIndex(t => t.TransactionGroupId)
            .HasDatabaseName("idx_transaction_group");

        // Foreign key relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TransactionGroup>()
            .WithMany()
            .HasForeignKey(t => t.TransactionGroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
