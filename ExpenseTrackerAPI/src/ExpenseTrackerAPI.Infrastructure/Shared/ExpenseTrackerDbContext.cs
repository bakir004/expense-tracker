using Microsoft.EntityFrameworkCore;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Shared;

namespace ExpenseTrackerAPI.Infrastructure.Shared;

/// <summary>
/// Entity Framework Core DbContext for the Expense Tracker application.
/// Manages database connections and entity mappings.
/// </summary>
public class ExpenseTrackerDbContext : DbContext
{
    public ExpenseTrackerDbContext(DbContextOptions<ExpenseTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionGroup> TransactionGroups { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureTransactionGroup(modelBuilder);
        ConfigureTransaction(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(e => e.InitialBalance).HasColumnName("initial_balance").HasColumnType("decimal(12,2)");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Email).IsUnique();
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Icon).HasColumnName("icon").HasMaxLength(100);

            entity.HasIndex(e => e.Name).IsUnique();
        });
    }

    private static void ConfigureTransactionGroup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransactionGroup>(entity =>
        {
            entity.ToTable("TransactionGroup");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            // Foreign key relationship
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
        });
    }

    private static void ConfigureTransaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transaction");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TransactionType)
                .HasColumnName("transaction_type")
                .HasMaxLength(20)
                .HasConversion(
                    v => v == TransactionType.Expense ? "EXPENSE" : "INCOME",
                    v => v == "EXPENSE" ? TransactionType.Expense : TransactionType.Income);
            
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(12,2)");
            entity.Property(e => e.SignedAmount).HasColumnName("signed_amount").HasColumnType("decimal(12,2)");
            entity.Property(e => e.CumulativeDelta).HasColumnName("cumulative_delta").HasColumnType("decimal(12,2)");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Subject).HasColumnName("subject").HasMaxLength(255);
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PaymentMethod)
                .HasColumnName("payment_method")
                .HasMaxLength(20)
                .HasConversion(
                    v => PaymentMethodHelper.ToDatabaseString(v),
                    v => PaymentMethodHelper.FromDatabaseString(v));
            
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.TransactionGroupId).HasColumnName("transaction_group_id");
            entity.Property(e => e.IncomeSource).HasColumnName("income_source").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            // Computed property - not mapped to database
            entity.Ignore(e => e.BalanceAfter);

            // Foreign key relationships
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<Category>()
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<TransactionGroup>()
                .WithMany()
                .HasForeignKey(e => e.TransactionGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(e => new { e.UserId, e.Date, e.Id });
            entity.HasIndex(e => new { e.UserId, e.TransactionType });
            entity.HasIndex(e => e.TransactionGroupId);
        });
    }
}

