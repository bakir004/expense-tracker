using ErrorOr;
using ExpenseTrackerAPI.Domain.Entities;
using ExpenseTrackerAPI.Domain.Errors;

namespace ExpenseTrackerAPI.Contracts.Transactions;

/// <summary>
/// Utility class for parsing and validating transaction request string values to enums.
/// </summary>
public static class TransactionRequestParser
{
    private static readonly Dictionary<string, TransactionType> TransactionTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "EXPENSE", TransactionType.EXPENSE },
        { "INCOME", TransactionType.INCOME }
    };

    private static readonly Dictionary<string, PaymentMethod> PaymentMethodMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "CASH", PaymentMethod.CASH },
        { "DEBIT_CARD", PaymentMethod.DEBIT_CARD },
        { "CREDIT_CARD", PaymentMethod.CREDIT_CARD },
        { "BANK_TRANSFER", PaymentMethod.BANK_TRANSFER },
        { "MOBILE_PAYMENT", PaymentMethod.MOBILE_PAYMENT },
        { "PAYPAL", PaymentMethod.PAYPAL },
        { "CRYPTO", PaymentMethod.CRYPTO },
        { "OTHER", PaymentMethod.OTHER }
    };

    /// <summary>
    /// Parses a string value to TransactionType enum.
    /// </summary>
    /// <param name="value">The string value to parse (e.g., "EXPENSE" or "INCOME")</param>
    /// <returns>The parsed TransactionType or an error</returns>
    public static ErrorOr<TransactionType> ParseTransactionType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TransactionErrors.TransactionTypeRequired;
        }

        var trimmedValue = value.Trim();
        if (TransactionTypeMap.TryGetValue(trimmedValue, out var transactionType))
        {
            return transactionType;
        }

        return TransactionErrors.InvalidTransactionType;
    }

    /// <summary>
    /// Parses a string value to PaymentMethod enum.
    /// </summary>
    /// <param name="value">The string value to parse (e.g., "CASH", "CREDIT_CARD")</param>
    /// <returns>The parsed PaymentMethod or an error</returns>
    public static ErrorOr<PaymentMethod> ParsePaymentMethod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TransactionErrors.PaymentMethodRequired;
        }

        var trimmedValue = value.Trim();
        if (PaymentMethodMap.TryGetValue(trimmedValue, out var paymentMethod))
        {
            return paymentMethod;
        }

        return TransactionErrors.InvalidPaymentMethod;
    }

    /// <summary>
    /// Parses and validates a CreateTransactionRequest, converting string values to enums.
    /// </summary>
    /// <param name="request">The request to parse</param>
    /// <returns>A tuple of (TransactionType, PaymentMethod) or validation errors</returns>
    public static ErrorOr<(TransactionType TransactionType, PaymentMethod PaymentMethod)> ParseCreateRequest(
        CreateTransactionRequest request)
    {
        var errors = new List<Error>();

        var transactionTypeResult = ParseTransactionType(request.TransactionType);
        if (transactionTypeResult.IsError)
        {
            errors.AddRange(transactionTypeResult.Errors);
        }

        var paymentMethodResult = ParsePaymentMethod(request.PaymentMethod);
        if (paymentMethodResult.IsError)
        {
            errors.AddRange(paymentMethodResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return (transactionTypeResult.Value, paymentMethodResult.Value);
    }

    /// <summary>
    /// Parses and validates an UpdateTransactionRequest, converting string values to enums.
    /// </summary>
    /// <param name="request">The request to parse</param>
    /// <returns>A tuple of (TransactionType, PaymentMethod) or validation errors</returns>
    public static ErrorOr<(TransactionType TransactionType, PaymentMethod PaymentMethod)> ParseUpdateRequest(
        UpdateTransactionRequest request)
    {
        var errors = new List<Error>();

        var transactionTypeResult = ParseTransactionType(request.TransactionType);
        if (transactionTypeResult.IsError)
        {
            errors.AddRange(transactionTypeResult.Errors);
        }

        var paymentMethodResult = ParsePaymentMethod(request.PaymentMethod);
        if (paymentMethodResult.IsError)
        {
            errors.AddRange(paymentMethodResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return (transactionTypeResult.Value, paymentMethodResult.Value);
    }
}
