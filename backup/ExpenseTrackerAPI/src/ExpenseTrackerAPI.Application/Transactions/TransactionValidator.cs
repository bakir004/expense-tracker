using System.Globalization;
using ErrorOr;
using ExpenseTrackerAPI.Contracts.Transactions;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Application.Transactions;

/// <summary>
/// Validates transaction-related requests.
/// </summary>
public static class TransactionValidator
{
    public static ErrorOr<Success> ValidateCreateTransaction(
        TransactionType transactionType,
        decimal amount,
        DateOnly date,
        string subject,
        int? categoryId)
    {
        var errors = new List<Error>();

        // Subject is required and cannot be empty
        if (string.IsNullOrWhiteSpace(subject))
        {
            errors.Add(TransactionErrors.InvalidSubject);
        }

        // Amount must be positive
        if (amount <= 0)
        {
            errors.Add(TransactionErrors.InvalidAmount);
        }

        // Date cannot be too far in the future (allow 1 day for timezone differences)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (date > today.AddDays(1))
        {
            errors.Add(TransactionErrors.InvalidDate);
        }

        // CategoryId is optional for both expense and income

        if (errors.Count > 0)
        {
            return errors;
        }

        return Result.Success;
    }

    public static ErrorOr<TransactionType> ParseTransactionType(string typeString)
    {
        return typeString.ToUpperInvariant() switch
        {
            "EXPENSE" => TransactionType.EXPENSE,
            "INCOME" => TransactionType.INCOME,
            _ => TransactionErrors.InvalidTransactionType
        };
    }

    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Builds and validates <see cref="TransactionQueryOptions"/> from API query parameters.
    /// </summary>
    public static ErrorOr<TransactionQueryOptions> BuildQueryOptions(TransactionQueryParameters? p)
    {
        if (p == null)
        {
            return new TransactionQueryOptions { SortDescending = true };
        }

        TransactionType? transactionType = null;
        if (!string.IsNullOrWhiteSpace(p.TransactionType))
        {
            var typeResult = ParseTransactionType(p.TransactionType.Trim());
            if (typeResult.IsError)
                return typeResult.Errors;
            transactionType = typeResult.Value;
        }

        DateOnly? dateFrom = null;
        if (!string.IsNullOrWhiteSpace(p.DateFrom))
        {
            if (!DateOnly.TryParseExact(p.DateFrom.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return Error.Validation("TransactionQuery.DateFrom", $"Invalid date format. Expected {DateFormat}.");
            dateFrom = d;
        }

        DateOnly? dateTo = null;
        if (!string.IsNullOrWhiteSpace(p.DateTo))
        {
            if (!DateOnly.TryParseExact(p.DateTo.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return Error.Validation("TransactionQuery.DateTo", $"Invalid date format. Expected {DateFormat}.");
            dateTo = d;
        }

        if (dateFrom.HasValue && dateTo.HasValue && dateFrom > dateTo)
            return Error.Validation("TransactionQuery.DateRange", "'DateFrom' must be before or equal to 'DateTo'.");

        IReadOnlyList<PaymentMethod>? paymentMethods = null;
        if (p.PaymentMethods is { Length: > 0 })
        {
            var list = new List<PaymentMethod>();
            foreach (var i in p.PaymentMethods)
            {
                if (i < 0 || i > (int)PaymentMethod.OTHER)
                    return Error.Validation("TransactionQuery.PaymentMethods", $"Invalid payment method value: {i}.");
                list.Add((PaymentMethod)i);
            }
            paymentMethods = list;
        }

        string? sortBy = null;
        if (!string.IsNullOrWhiteSpace(p.SortBy))
        {
            var s = p.SortBy.Trim().ToLowerInvariant();
            if (s is "subject" or "paymentmethod" or "category" or "amount")
                sortBy = s;
            else
                return Error.Validation("TransactionQuery.SortBy", "Valid values: subject, paymentMethod, category, amount.");
        }

        var sortDescending = true;
        if (!string.IsNullOrWhiteSpace(p.SortDirection))
        {
            var dir = p.SortDirection.Trim().ToLowerInvariant();
            if (dir == "asc") sortDescending = false;
            else if (dir != "desc")
                return Error.Validation("TransactionQuery.SortDirection", "Valid values: asc, desc.");
        }

        return new TransactionQueryOptions
        {
            Subject = string.IsNullOrWhiteSpace(p.Subject) ? null : p.Subject.Trim(),
            CategoryIds = p.CategoryIds is { Length: > 0 } ? p.CategoryIds : null,
            PaymentMethods = paymentMethods,
            TransactionType = transactionType,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SortBy = sortBy,
            SortDescending = sortDescending
        };
    }
}

