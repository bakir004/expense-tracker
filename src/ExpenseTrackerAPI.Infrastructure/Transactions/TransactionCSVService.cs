using ErrorOr;
using CsvHelper;
using ExpenseTrackerAPI.Application.Transactions.Interfaces.Infrastructure;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Infrastructure.Persistence;
using CsvHelper.Configuration;

namespace ExpenseTrackerAPI.Infrastructure.Transactions;

public class TransactionCSVService : ITransactionCSVService 
{
    private readonly ApplicationDbContext _context;

    public TransactionCSVService(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ErrorOr<Stream>> ExportTransactionsToCSVAsync(List<Transaction> transactions, CancellationToken cancellationToken)
    {
        try 
        {
            var ms = new MemoryStream();

            await using var writer = new StreamWriter(ms, leaveOpen: true);
            await using var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

            csv.Context.RegisterClassMap<TransactionMap>();

            await csv.WriteRecordsAsync(transactions, cancellationToken);
            await writer.FlushAsync();

            ms.Position = 0; 
            return ms;
        }
        catch (Exception ex)
        {
            return Error.Failure("CSV.ExportError", ex.Message);
        }
    }
}

public sealed class TransactionMap : ClassMap<Transaction>
{
    public TransactionMap()
    {
        Map(m => m.Id).Name("ID");
        Map(m => m.Date).Name("Date");
        Map(m => m.Subject).Name("Subject");
        Map(m => m.Notes).Name("Notes");
        Map(m => m.Amount).Name("Amount");
        Map(m => m.SignedAmount).Name("Signed Amount");
        Map(m => m.TransactionType).Name("Transaction Type");
        Map(m => m.PaymentMethod).Name("Payment Method");
        Map(m => m.CategoryId).Name("Category ID");
        Map(m => m.TransactionGroupId).Name("Transaction Group ID");
        Map(m => m.CreatedAt).Name("Created At");
        Map(m => m.UpdatedAt).Name("Updated At");
    }
}
