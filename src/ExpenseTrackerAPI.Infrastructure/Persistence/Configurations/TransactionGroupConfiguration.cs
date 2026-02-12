using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Infrastructure.Persistence.Configurations;

public class TransactionGroupConfiguration : IEntityTypeConfiguration<TransactionGroup>
{
    public void Configure(EntityTypeBuilder<TransactionGroup> builder)
    {
        builder.ToTable("TransactionGroups");

        builder.HasKey(tg => tg.Id);

        builder.Property(tg => tg.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(tg => tg.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(tg => tg.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(tg => tg.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(tg => tg.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Index
        builder.HasIndex(tg => tg.UserId)
            .HasDatabaseName("idx_transaction_group_user");

        // Foreign key relationship
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(tg => tg.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
