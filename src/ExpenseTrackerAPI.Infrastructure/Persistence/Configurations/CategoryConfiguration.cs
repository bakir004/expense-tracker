using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(c => c.Icon)
            .HasColumnName("icon")
            .HasMaxLength(100);

        // Unique constraint
        builder.HasIndex(c => c.Name)
            .IsUnique();
    }
}
