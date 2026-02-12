using System.Globalization;
using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Parses incoming API values (query, route, body strings) into strongly-typed types.
/// All parsing from the API consumer is handled here; controllers call these and pass results to the application.
/// </summary>
public static class TransactionParsers
{
    public const string DateFormat = "yyyy-MM-dd";

    public static ErrorOr<TransactionType> ParseTransactionType(string? typeString)
    {
        if (string.IsNullOrWhiteSpace(typeString))
            return TransactionErrors.InvalidTransactionType;
        return typeString.Trim().ToUpperInvariant() switch
        {
            "EXPENSE" => TransactionType.EXPENSE,
            "INCOME" => TransactionType.INCOME,
            _ => TransactionErrors.InvalidTransactionType
        };
    }

    public static ErrorOr<PaymentMethod> ParsePaymentMethod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation("PaymentMethod", "Payment method is required.");
        var normalized = value.Trim().ToUpperInvariant().Replace(" ", "_");
        return normalized switch
        {
            "CASH" => PaymentMethod.CASH,
            "DEBIT_CARD" => PaymentMethod.DEBIT_CARD,
            "CREDIT_CARD" => PaymentMethod.CREDIT_CARD,
            "BANK_TRANSFER" => PaymentMethod.BANK_TRANSFER,
            "MOBILE_PAYMENT" => PaymentMethod.MOBILE_PAYMENT,
            "PAYPAL" => PaymentMethod.PAYPAL,
            "CRYPTO" => PaymentMethod.CRYPTO,
            "OTHER" => PaymentMethod.OTHER,
            _ => Error.Validation("PaymentMethod", "Invalid payment method.")
        };
    }

    public static ErrorOr<DateOnly> ParseDate(string? dateString, string format = DateFormat)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return Error.Validation("Date", $"Date is required. Expected format: {format}.");
        if (!DateOnly.TryParseExact(dateString.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return Error.Validation("Date", $"Invalid date format. Expected {format} (e.g. 2024-12-31).");
        return d;
    }

    /// <summary>
    /// Parses 'from' and 'to' date strings (e.g. query params) and validates from ≤ to.
    /// </summary>
    public static ErrorOr<(DateOnly From, DateOnly To)> ParseDateRange(string? from, string? to, string format = DateFormat)
    {
        var fromResult = ParseDate(from, format);
        if (fromResult.IsError) return fromResult.Errors;
        var toResult = ParseDate(to, format);
        if (toResult.IsError) return toResult.Errors;
        var start = fromResult.Value;
        var end = toResult.Value;
        if (start > end)
            return Error.Validation("DateRange", "'from' date must be before or equal to 'to' date.");
        return (start, end);
    }

    /// <summary>
    /// Builds and validates <see cref="TransactionQueryOptions"/> from API query parameters.
    /// </summary>
    public static ErrorOr<TransactionQueryOptions> BuildQueryOptions(TransactionQueryParameters? p)
    {
        if (p == null)
            return new TransactionQueryOptions { SortDescending = true };

        TransactionType? transactionType = null;
        if (!string.IsNullOrWhiteSpace(p.TransactionType))
        {
            var typeResult = ParseTransactionType(p.TransactionType.Trim());
            if (typeResult.IsError) return typeResult.Errors;
            transactionType = typeResult.Value;
        }

        DateOnly? dateFrom = null;
        if (!string.IsNullOrWhiteSpace(p.DateFrom))
        {
            var r = ParseDate(p.DateFrom.Trim(), DateFormat);
            if (r.IsError) return r.Errors;
            dateFrom = r.Value;
        }

        DateOnly? dateTo = null;
        if (!string.IsNullOrWhiteSpace(p.DateTo))
        {
            var r = ParseDate(p.DateTo.Trim(), DateFormat);
            if (r.IsError) return r.Errors;
            dateTo = r.Value;
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
