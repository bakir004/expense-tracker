using System.Globalization;
using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;

namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Parser and validator for transaction filter requests.
/// Converts <see cref="TransactionFilterRequest"/> to validated <see cref="TransactionFilter"/>.
/// </summary>
public static class TransactionFilterParser
{
    private const string DateFormat = "yyyy-MM-dd";
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    /// <summary>
    /// Parses and validates a <see cref="TransactionFilterRequest"/> into a <see cref="TransactionFilter"/>.
    /// Returns validation errors if the request contains invalid values.
    /// Uses default page size settings.
    /// </summary>
    /// <param name="request">The filter request to parse. Can be null for default filtering.</param>
    /// <returns>A validated <see cref="TransactionFilter"/> or validation errors.</returns>
    public static ErrorOr<TransactionFilter> Parse(TransactionFilterRequest? request)
    {
        return Parse(request, MaxPageSize, DefaultPageSize);
    }

    /// <summary>
    /// Parses and validates a <see cref="TransactionFilterRequest"/> into a <see cref="TransactionFilter"/>.
    /// Returns validation errors if the request contains invalid values.
    /// </summary>
    /// <param name="request">The filter request to parse. Can be null for default filtering.</param>
    /// <param name="maxPageSize">Maximum allowed page size.</param>
    /// <param name="defaultPageSize">Default page size when not specified.</param>
    /// <returns>A validated <see cref="TransactionFilter"/> or validation errors.</returns>
    public static ErrorOr<TransactionFilter> Parse(TransactionFilterRequest? request, int maxPageSize, int defaultPageSize)
    {
        if (request is null)
        {
            return new TransactionFilter();
        }

        var errors = new List<Error>();

        // Parse transaction type
        TransactionType? transactionType = null;
        if (!string.IsNullOrWhiteSpace(request.TransactionType))
        {
            var typeResult = ParseTransactionType(request.TransactionType);
            if (typeResult.IsError)
            {
                errors.AddRange(typeResult.Errors);
            }
            else
            {
                transactionType = typeResult.Value;
            }
        }

        // Validate amount range
        if (request.MinAmount.HasValue && request.MinAmount.Value < 0)
        {
            errors.Add(Error.Validation("MinAmount", "Minimum amount cannot be negative."));
        }

        if (request.MaxAmount.HasValue && request.MaxAmount.Value < 0)
        {
            errors.Add(Error.Validation("MaxAmount", "Maximum amount cannot be negative."));
        }

        if (request.MinAmount.HasValue && request.MaxAmount.HasValue && request.MinAmount.Value > request.MaxAmount.Value)
        {
            errors.Add(Error.Validation("AmountRange", "Minimum amount cannot be greater than maximum amount."));
        }

        // Parse dates
        DateOnly? dateFrom = null;
        DateOnly? dateTo = null;

        if (!string.IsNullOrWhiteSpace(request.DateFrom))
        {
            var dateResult = ParseDate(request.DateFrom, "DateFrom");
            if (dateResult.IsError)
            {
                errors.AddRange(dateResult.Errors);
            }
            else
            {
                dateFrom = dateResult.Value;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.DateTo))
        {
            var dateResult = ParseDate(request.DateTo, "DateTo");
            if (dateResult.IsError)
            {
                errors.AddRange(dateResult.Errors);
            }
            else
            {
                dateTo = dateResult.Value;
            }
        }

        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value > dateTo.Value)
        {
            errors.Add(Error.Validation("DateRange", "DateFrom cannot be after DateTo."));
        }

        // Parse payment methods
        IReadOnlyList<PaymentMethod>? paymentMethods = null;
        if (request.PaymentMethods is { Length: > 0 })
        {
            var paymentMethodsResult = ParsePaymentMethods(request.PaymentMethods);
            if (paymentMethodsResult.IsError)
            {
                errors.AddRange(paymentMethodsResult.Errors);
            }
            else
            {
                paymentMethods = paymentMethodsResult.Value;
            }
        }

        // Validate category IDs
        if (request.CategoryIds is { Length: > 0 })
        {
            foreach (var categoryId in request.CategoryIds)
            {
                if (categoryId <= 0)
                {
                    errors.Add(Error.Validation("CategoryIds", $"Invalid category ID: {categoryId}. Category IDs must be positive integers."));
                    break;
                }
            }
        }

        // Validate transaction group IDs
        if (request.TransactionGroupIds is { Length: > 0 })
        {
            foreach (var groupId in request.TransactionGroupIds)
            {
                if (groupId <= 0)
                {
                    errors.Add(Error.Validation("TransactionGroupIds", $"Invalid transaction group ID: {groupId}. Group IDs must be positive integers."));
                    break;
                }
            }
        }

        // Parse sort field
        var sortBy = TransactionSortField.Date;
        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var sortResult = ParseSortField(request.SortBy);
            if (sortResult.IsError)
            {
                errors.AddRange(sortResult.Errors);
            }
            else
            {
                sortBy = sortResult.Value;
            }
        }

        // Parse sort direction
        var sortDescending = true;
        if (!string.IsNullOrWhiteSpace(request.SortDirection))
        {
            var directionResult = ParseSortDirection(request.SortDirection);
            if (directionResult.IsError)
            {
                errors.AddRange(directionResult.Errors);
            }
            else
            {
                sortDescending = directionResult.Value;
            }
        }

        // Validate pagination
        var page = request.Page ?? 1;
        var pageSize = request.PageSize ?? defaultPageSize;

        if (page < 1)
        {
            errors.Add(Error.Validation("Page", "Page number must be at least 1."));
        }

        if (pageSize < 1)
        {
            errors.Add(Error.Validation("PageSize", "Page size must be at least 1."));
        }
        else if (pageSize > maxPageSize)
        {
            errors.Add(Error.Validation("PageSize", $"Page size cannot exceed {maxPageSize}."));
        }

        // Return errors if any
        if (errors.Count > 0)
        {
            return errors;
        }

        // Build validated filter
        return new TransactionFilter
        {
            TransactionType = transactionType,
            MinAmount = request.MinAmount,
            MaxAmount = request.MaxAmount,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SubjectContains = string.IsNullOrWhiteSpace(request.SubjectContains) ? null : request.SubjectContains.Trim(),
            NotesContains = string.IsNullOrWhiteSpace(request.NotesContains) ? null : request.NotesContains.Trim(),
            PaymentMethods = paymentMethods,
            CategoryIds = request.CategoryIds is { Length: > 0 } ? request.CategoryIds.ToList().AsReadOnly() : null,
            Uncategorized = request.Uncategorized ?? false,
            TransactionGroupIds = request.TransactionGroupIds is { Length: > 0 } ? request.TransactionGroupIds.ToList().AsReadOnly() : null,
            Ungrouped = request.Ungrouped ?? false,
            SortBy = sortBy,
            SortDescending = sortDescending,
            Page = page,
            PageSize = pageSize
        };
    }

    private static ErrorOr<TransactionType> ParseTransactionType(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "EXPENSE" => TransactionType.EXPENSE,
            "INCOME" => TransactionType.INCOME,
            _ => Error.Validation("TransactionType", $"Invalid transaction type: '{value}'. Valid values are: EXPENSE, INCOME.")
        };
    }

    private static ErrorOr<DateOnly> ParseDate(string value, string fieldName)
    {
        if (DateOnly.TryParseExact(value.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        return Error.Validation(fieldName, $"Invalid date format for {fieldName}: '{value}'. Expected format: {DateFormat}.");
    }

    private static ErrorOr<IReadOnlyList<PaymentMethod>> ParsePaymentMethods(string[] values)
    {
        var paymentMethods = new List<PaymentMethod>();
        var invalidValues = new List<string>();

        foreach (var value in values)
        {
            var normalized = value.Trim().ToUpperInvariant();
            if (Enum.TryParse<PaymentMethod>(normalized, ignoreCase: true, out var method))
            {
                if (!paymentMethods.Contains(method))
                {
                    paymentMethods.Add(method);
                }
            }
            else
            {
                invalidValues.Add(value);
            }
        }

        if (invalidValues.Count > 0)
        {
            var validValues = string.Join(", ", Enum.GetNames<PaymentMethod>());
            return Error.Validation(
                "PaymentMethods",
                $"Invalid payment method(s): {string.Join(", ", invalidValues)}. Valid values are: {validValues}.");
        }

        return paymentMethods.AsReadOnly();
    }

    private static ErrorOr<TransactionSortField> ParseSortField(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "date" => TransactionSortField.Date,
            "amount" => TransactionSortField.Amount,
            "subject" => TransactionSortField.Subject,
            "paymentmethod" or "payment_method" => TransactionSortField.PaymentMethod,
            "createdat" or "created_at" => TransactionSortField.CreatedAt,
            "updatedat" or "updated_at" => TransactionSortField.UpdatedAt,
            _ => Error.Validation("SortBy", $"Invalid sort field: '{value}'. Valid values are: date, amount, subject, paymentMethod, createdAt, updatedAt.")
        };
    }

    private static ErrorOr<bool> ParseSortDirection(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "asc" or "ascending" => false,
            "desc" or "descending" => true,
            _ => Error.Validation("SortDirection", $"Invalid sort direction: '{value}'. Valid values are: asc, desc.")
        };
    }
}
